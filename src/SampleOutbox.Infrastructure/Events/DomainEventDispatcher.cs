using Microsoft.Extensions.DependencyInjection;
using SampleOutbox.Application.Abstractions;
using SampleOutbox.Domain.Primitives;
using SampleOutbox.Infrastructure.Persistence;

namespace SampleOutbox.Infrastructure.Events;

public sealed class DomainEventDispatcher(IServiceProvider serviceProvider, EventTypeRegistry registry, IOutbox outbox)
{
    private const int MaxDispatchRounds = 10;

    public async Task DispatchAsync(SampleOutboxDbContext db, CancellationToken ct)
    {
        for (var round = 0; round < MaxDispatchRounds; round++)
        {
            var trackedEntities = db.ChangeTracker.Entries<Entity>().Select(e => e.Entity).ToList();

            var domainEvents = trackedEntities.SelectMany(e => e.DomainEvents).ToList();

            foreach (var entity in trackedEntities)
            {
                entity.ClearDomainEvents();
            }

            if (domainEvents.Count == 0)
            {
                return;
            }

            foreach (var domainEvent in domainEvents)
            {
                await DispatchOneAsync(domainEvent, ct);
            }
        }
    }

    private async Task DispatchOneAsync(IDomainEvent domainEvent, CancellationToken ct)
    {
        var eventType = domainEvent.GetType();
        var handlerInterfaceType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
        var handlers = serviceProvider.GetServices(handlerInterfaceType);

        foreach (var handler in handlers)
        {
            if (handler is null)
            {
                continue;
            }

            var handlerType = handler.GetType();
            var isAsyncHandler = handlerType.GetCustomAttributes(typeof(AsyncHandlerAttribute), inherit: false).Length > 0;

            if (isAsyncHandler)
            {
                outbox.Add(OutboxKind.DomainEvent, domainEvent, handlerKey: registry.GetKey(handlerType));
            }
            else
            {
                await InvokeAsync(handler, domainEvent, ct);
            }
        }
    }

    internal static Task InvokeAsync(object handler, IDomainEvent domainEvent, CancellationToken ct)
    {
        var eventType = domainEvent.GetType();
        var handlerInterfaceType = typeof(IDomainEventHandler<>).MakeGenericType(eventType);
        var method = handlerInterfaceType.GetMethod(nameof(IDomainEventHandler<IDomainEvent>.Handle))!;

        return (Task)method.Invoke(handler, [domainEvent, ct])!;
    }
}
