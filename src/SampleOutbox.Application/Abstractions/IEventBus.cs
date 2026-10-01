namespace SampleOutbox.Application.Abstractions;

public interface IEventBus
{
    Task PublishAsync(object integrationEvent, string eventKey, CancellationToken ct);
}
