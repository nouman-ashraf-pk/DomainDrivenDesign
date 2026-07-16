using OrderManagement.Domain.Common;

namespace OrderManagement.Infrastructure.Outbox;

// Maps the string we stored (Type.FullName) back to a real CLR Type so we
// can JsonSerializer.Deserialize into the concrete event, not just IDomainEvent.
//
// Deliberately uses FullName + a one-time assembly scan rather than
// AssemblyQualifiedName: an assembly-qualified name bakes in the assembly
// version, so a routine version bump on the Domain project would strand any
// outbox rows written before the bump. FullName only breaks if you rename or
// move an event class - rare, and something you'd do deliberately.
public static class DomainEventTypeResolver
{
    private static readonly Lazy<IReadOnlyDictionary<string, Type>> TypesByName = new(() =>
        typeof(IDomainEvent).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IDomainEvent).IsAssignableFrom(t))
            .ToDictionary(t => t.FullName!, t => t));

    public static Type Resolve(string typeName) =>
        TypesByName.Value.TryGetValue(typeName, out var type)
            ? type
            : throw new InvalidOperationException(
                $"Cannot resolve domain event type '{typeName}'. Was it renamed or moved to a different assembly?");
}
