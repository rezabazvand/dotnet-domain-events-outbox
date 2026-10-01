namespace SampleOutbox.Application.Abstractions;

public interface IOutbox
{
    void Add(OutboxKind kind, object payload, string? handlerKey = null);
}
