using CredoCms.Application.Common;
using CredoCms.Domain.Auditing;

namespace CredoCms.Application.Auditing;

public interface IAuditLogRepository
{
    Task AddAsync(AuditLogEntry entry, CancellationToken ct = default);

    Task<PagedResult<AuditLogEntry>> ListAsync(AuditLogQuery query, CancellationToken ct = default);

    Task<AuditLogEntry?> GetAsync(Guid id, CancellationToken ct = default);
}

public sealed record AuditLogQuery(
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null,
    Guid? UserId = null,
    string? Action = null,
    string? EntityType = null,
    int Page = 1,
    int PageSize = 50);
