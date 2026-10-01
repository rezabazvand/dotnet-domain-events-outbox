using SampleOutbox.Domain.Primitives;

namespace SampleOutbox.Application.Abstractions;

public interface IDomainEventHandler<in TEvent> where TEvent : IDomainEvent
{
    Task Handle(TEvent e, CancellationToken ct);
}
