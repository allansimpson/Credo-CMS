using CredoCms.Application.Common;
using CredoCms.Domain.Auditing;

namespace CredoCms.Application.Auditing;

public interface IAuditLogService
{
    Task<PagedResult<AuditLogEntryDto>> ListAsync(AuditLogQuery query, CancellationToken ct = default);
    Task<AuditLogEntryDto?> GetAsync(Guid id, CancellationToken ct = default);
}

public sealed class AuditLogService : IAuditLogService
{
    private readonly IAuditLogRepository _repo;

    public AuditLogService(IAuditLogRepository repo) => _repo = repo;

    public async Task<PagedResult<AuditLogEntryDto>> ListAsync(AuditLogQuery query, CancellationToken ct = default)
    {
        // Hard-cap the page size so a malicious client can't request a million rows.
        var safeQuery = query with { PageSize = Math.Clamp(query.PageSize, 1, 200), Page = Math.Max(1, query.Page) };
        var page = await _repo.ListAsync(safeQuery, ct);
        return new PagedResult<AuditLogEntryDto>(
            [.. page.Items.Select(ToDto)],
            page.TotalCount,
            page.Page,
            page.PageSize);
    }

    public async Task<AuditLogEntryDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var entry = await _repo.GetAsync(id, ct);
        return entry is null ? null : ToDto(entry);
    }

    private static AuditLogEntryDto ToDto(AuditLogEntry e) => new(
        e.Id, e.Timestamp, e.UserId, e.UserDisplayNameSnapshot,
        e.Action, e.EntityType, e.EntityId, e.DetailsJson, e.IpAddress);
}
