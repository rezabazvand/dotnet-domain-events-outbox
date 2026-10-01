using System.Text.Json;
using SampleOutbox.Application.Abstractions;
using SampleOutbox.Infrastructure.Events;
using SampleOutbox.Infrastructure.Persistence;

namespace SampleOutbox.Infrastructure.Outbox;

public sealed class OutboxWriter(SampleOutboxDbContext db, EventTypeRegistry registry) : IOutbox
{
    public void Add(OutboxKind kind, object payload, string? handlerKey = null)
    {
        var payloadType = payload.GetType();
        var payloadKey = registry.GetKey(payloadType);
        var json = JsonSerializer.Serialize(payload, payloadType);

        db.OutboxMessages.Add(new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Kind = kind,
            PayloadKey = payloadKey,
            Payload = json,
            HandlerKey = handlerKey,
            OccurredOn = DateTime.UtcNow,
            ProcessedOn = null,
            NextAttemptOn = DateTime.UtcNow,
            AttemptCount = 0,
            Error = null,
            IsDeadLettered = false
        });
    }
}
