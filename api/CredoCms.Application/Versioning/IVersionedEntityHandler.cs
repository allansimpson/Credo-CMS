namespace CredoCms.Application.Versioning;

public sealed record VersionListItem(
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidTo,
    string Title,
    Guid? ModifiedByUserId);

public sealed record VersionSnapshot(
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidTo,
    string Title,
    Guid? ModifiedByUserId,
    /// <summary>JSON snapshot of the historical row (scalars only).</summary>
    string PayloadJson);

public sealed record VersionRestoreResult(bool Succeeded, string[] Errors);

/// <summary>
/// Per-entity adapter for the generic version-history admin controller.
/// Implementations live next to the entity's service (PageVersionHandler,
/// NewsVersionHandler, etc.).
/// </summary>
public interface IVersionedEntityHandler
{
    /// <summary>One of <c>Page</c>, <c>NewsItem</c>, <c>ServiceTime</c>,
    /// <c>Document</c>, <c>AnnouncementBanner</c>.</summary>
    string EntityType { get; }

    Task<List<VersionListItem>?> ListAsync(Guid id, CancellationToken ct = default);

    Task<VersionSnapshot?> GetAsOfAsync(Guid id, DateTimeOffset asOfUtc, CancellationToken ct = default);

    Task<VersionRestoreResult> RestoreAsync(Guid id, DateTimeOffset asOfUtc, CancellationToken ct = default);
}

public interface IVersionedEntityHandlerRegistry
{
    IVersionedEntityHandler? Resolve(string entityType);
}
