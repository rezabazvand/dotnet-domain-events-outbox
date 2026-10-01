using SampleOutbox.Application.Abstractions;

namespace SampleOutbox.Infrastructure.Outbox;

public sealed class OutboxMessage
{
    public Guid Id { get; set; }

    public OutboxKind Kind { get; set; }

    public string PayloadKey { get; set; } = null!;

    public string Payload { get; set; } = null!;

    public string? HandlerKey { get; set; }

    public DateTime OccurredOn { get; set; }

    public DateTime? ProcessedOn { get; set; }

    public DateTime NextAttemptOn { get; set; }

    public int AttemptCount { get; set; }

    public string? Error { get; set; }

    public bool IsDeadLettered { get; set; }
}
