using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SampleOutbox.Application.Abstractions;
using SampleOutbox.Domain.Primitives;
using SampleOutbox.Infrastructure.Events;
using SampleOutbox.Infrastructure.Persistence;

namespace SampleOutbox.Infrastructure.Outbox;

public sealed class OutboxProcessor(IServiceScopeFactory scopeFactory, ILogger<OutboxProcessor> logger) : BackgroundService
{
    private const int BatchSize = 50;
    private const int MaxAttempts = 8;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Outbox batch processing failed unexpectedly.");
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task ProcessBatchAsync(CancellationToken ct)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SampleOutboxDbContext>();
        var registry = scope.ServiceProvider.GetRequiredService<EventTypeRegistry>();
        var eventBus = scope.ServiceProvider.GetRequiredService<IEventBus>();

        var now = DateTime.UtcNow;

        var batch = await db.OutboxMessages
            .Where(m => m.ProcessedOn == null && !m.IsDeadLettered && m.NextAttemptOn <= now)
            .OrderBy(m => m.NextAttemptOn)
            .Take(BatchSize)
            .ToListAsync(ct);

        foreach (var message in batch)
        {
            try
            {
                await ProcessMessageAsync(message, scope.ServiceProvider, registry, eventBus, ct);
                message.ProcessedOn = DateTime.UtcNow;
                message.Error = null;
            }
            catch (Exception ex)
            {
                message.AttemptCount++;
                message.Error = ex.Message;

                if (message.AttemptCount >= MaxAttempts)
                {
                    message.IsDeadLettered = true;
                    logger.LogError(ex, "Outbox message {MessageId} dead-lettered after {Attempts} attempts.", message.Id, message.AttemptCount);
                }
                else
                {
                    var backoff = TimeSpan.FromSeconds(Math.Pow(2, message.AttemptCount));
                    message.NextAttemptOn = DateTime.UtcNow.Add(backoff);
                    logger.LogWarning(ex, "Outbox message {MessageId} failed (attempt {Attempts}); retrying at {NextAttemptOn}.", message.Id, message.AttemptCount, message.NextAttemptOn);
                }
            }
        }

        if (batch.Count > 0)
        {
            await db.SaveChangesWithoutDispatchAsync(ct);
        }
    }

    private static async Task ProcessMessageAsync(
        OutboxMessage message,
        IServiceProvider scopedProvider,
        EventTypeRegistry registry,
        IEventBus eventBus,
        CancellationToken ct)
    {
        var payloadType = registry.Resolve(message.PayloadKey);
        var payload = JsonSerializer.Deserialize(message.Payload, payloadType)
            ?? throw new InvalidOperationException($"Outbox message {message.Id} payload deserialized to null.");

        if (message.Kind == OutboxKind.DomainEvent)
        {
            if (message.HandlerKey is null)
            {
                throw new InvalidOperationException($"Outbox message {message.Id} is a DomainEvent row without a HandlerKey.");
            }

            var handlerType = registry.Resolve(message.HandlerKey);
            var handler = scopedProvider.GetRequiredService(handlerType);

            await DomainEventDispatcher.InvokeAsync(handler, (IDomainEvent)payload, ct);
        }
        else
        {
            await eventBus.PublishAsync(payload, message.PayloadKey, ct);
        }
    }
}
