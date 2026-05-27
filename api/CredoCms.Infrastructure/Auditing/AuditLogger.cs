using System.Text.Json;
using CredoCms.Application.Auditing;
using CredoCms.Application.Common;
using CredoCms.Domain.Auditing;
using CredoCms.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace CredoCms.Infrastructure.Auditing;

/// <summary>
/// Writes audit-log entries directly to the database, capturing the acting user's
/// snapshot fields and IP automatically.
/// </summary>
public sealed class AuditLogger : IAuditLogger, IAuditLogRepository
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    private readonly ApplicationDbContext _db;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<AuditLogger> _logger;

    public AuditLogger(
        ApplicationDbContext db,
        ICurrentUserService currentUser,
        ILogger<AuditLogger> logger)
    {
        _db = db;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task WriteAsync(
        string action,
        string entityType,
        string? entityId = null,
        object? details = null,
        CancellationToken cancellationToken = default)
    {
        var entry = new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTimeOffset.UtcNow,
            UserId = _currentUser.IsAuthenticated ? _currentUser.UserId : null,
            UserDisplayNameSnapshot = _currentUser.DisplayName,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            DetailsJson = details is null ? null : JsonSerializer.Serialize(details, JsonOpts),
            IpAddress = _currentUser.IpAddress,
        };

        _db.AuditLog.Add(entry);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        _logger.LogInformation(
            "Audit: {Action} on {EntityType} {EntityId} by {Actor}",
            action, entityType, entityId, entry.UserDisplayNameSnapshot);
    }

    public async Task AddAsync(AuditLogEntry entry, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        _db.AuditLog.Add(entry);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<PagedResult<AuditLogEntry>> ListAsync(AuditLogQuery query, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(query);

        var q = _db.AuditLog.AsQueryable();

        if (query.FromUtc is { } from) q = q.Where(e => e.Timestamp >= from);
        if (query.ToUtc is { } to) q = q.Where(e => e.Timestamp <= to);
        if (query.UserId is { } uid) q = q.Where(e => e.UserId == uid);
        if (!string.IsNullOrWhiteSpace(query.Action))
            q = q.Where(e => e.Action == query.Action);
        if (!string.IsNullOrWhiteSpace(query.EntityType))
            q = q.Where(e => e.EntityType == query.EntityType);

        var totalCount = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .CountAsync(q, ct).ConfigureAwait(false);

        var items = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .ToListAsync(
                q.OrderByDescending(e => e.Timestamp)
                 .Skip((query.Page - 1) * query.PageSize)
                 .Take(query.PageSize),
                ct)
            .ConfigureAwait(false);

        return new PagedResult<AuditLogEntry>(items, totalCount, query.Page, query.PageSize);
    }

    public async Task<AuditLogEntry?> GetAsync(Guid id, CancellationToken ct = default)
    {
        return await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
            .FirstOrDefaultAsync(_db.AuditLog, e => e.Id == id, ct)
            .ConfigureAwait(false);
    }
}
