using System.Reflection;
using SampleOutbox.Domain.Primitives;

namespace SampleOutbox.Infrastructure.Events;

public sealed class EventTypeRegistry
{
    private readonly Dictionary<string, Type> _typesByKey = new();
    private readonly Dictionary<Type, string> _keysByType = new();

    public EventTypeRegistry(IEnumerable<Assembly> assemblies)
    {
        var decoratedTypes = assemblies
            .SelectMany(a => a.GetTypes())
            .Where(t => t.IsClass && !t.IsAbstract)
            .Select(t => (Type: t, Attribute: t.GetCustomAttribute<EventKeyAttribute>()))
            .Where(x => x.Attribute is not null);

        foreach (var (type, attribute) in decoratedTypes)
        {
            var key = attribute!.Key;

            if (_typesByKey.TryGetValue(key, out var existing))
            {
                throw new InvalidOperationException(
                    $"Duplicate EventKey '{key}' on both '{existing.FullName}' and '{type.FullName}'.");
            }

            _typesByKey.Add(key, type);
            _keysByType.Add(type, key);
        }
    }

    public string GetKey(Type type)
    {
        if (!_keysByType.TryGetValue(type, out var key))
        {
            throw new InvalidOperationException($"Type '{type.FullName}' has no [EventKey] attribute registered.");
        }

        return key;
    }

    public Type Resolve(string key)
    {
        if (!_typesByKey.TryGetValue(key, out var type))
        {
            throw new InvalidOperationException($"No type registered for EventKey '{key}'.");
        }

        return type;
    }
}
