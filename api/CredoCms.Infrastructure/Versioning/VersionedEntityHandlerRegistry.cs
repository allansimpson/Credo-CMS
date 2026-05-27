using CredoCms.Application.Versioning;

namespace CredoCms.Infrastructure.Versioning;

public sealed class VersionedEntityHandlerRegistry : IVersionedEntityHandlerRegistry
{
    private readonly Dictionary<string, IVersionedEntityHandler> _byType;

    public VersionedEntityHandlerRegistry(IEnumerable<IVersionedEntityHandler> handlers)
    {
        _byType = handlers.ToDictionary(h => h.EntityType, StringComparer.OrdinalIgnoreCase);
    }

    public IVersionedEntityHandler? Resolve(string entityType)
        => _byType.TryGetValue(entityType, out var h) ? h : null;
}
