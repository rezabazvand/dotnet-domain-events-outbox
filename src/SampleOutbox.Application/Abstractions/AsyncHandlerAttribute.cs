namespace SampleOutbox.Application.Abstractions;

[AttributeUsage(AttributeTargets.Class)]
public sealed class AsyncHandlerAttribute : Attribute
{
}
