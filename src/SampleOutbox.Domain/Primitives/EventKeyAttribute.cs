namespace SampleOutbox.Domain.Primitives;

[AttributeUsage(AttributeTargets.Class)]
public sealed class EventKeyAttribute(string key) : Attribute
{
    public string Key { get; } = key;
}
