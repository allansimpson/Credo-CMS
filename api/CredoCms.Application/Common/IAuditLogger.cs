namespace CredoCms.Application.Common;

/// <summary>
/// High-level writer for audit-log entries. Captures the acting user's display name
/// and IP address from <see cref="ICurrentUserService"/> automatically — callers
/// only need to supply the action, entity type, entity id, and any structured
/// detail payload.
/// </summary>
public interface IAuditLogger
{
    Task WriteAsync(
        string action,
        string entityType,
        string? entityId = null,
        object? details = null,
        CancellationToken cancellationToken = default);
}
