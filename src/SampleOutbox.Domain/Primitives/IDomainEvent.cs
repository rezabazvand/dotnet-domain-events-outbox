namespace SampleOutbox.Domain.Primitives;

public interface IDomainEvent
{
    Guid EventId { get; }

    DateTime OccurredOn { get; }
}
