using CredoCms.Application.Common;
using CredoCms.Application.Email;
using CredoCms.Domain.Email;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CredoCms.Infrastructure.Email;

public sealed class EmailSuppressionRepository : IEmailSuppressionRepository
{
    private readonly ApplicationDbContext _db;
    public EmailSuppressionRepository(ApplicationDbContext db) => _db = db;

    public Task<EmailSuppression?> GetByEmailAsync(string emailAddress, CancellationToken ct = default) =>
        _db.EmailSuppressions
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.EmailAddress == emailAddress, ct);

    public async Task<IReadOnlyDictionary<string, EmailSuppression>> BulkLookupAsync(
        IReadOnlyCollection<string> emailAddresses,
        CancellationToken ct = default)
    {
        if (emailAddresses.Count == 0)
            return new Dictionary<string, EmailSuppression>();

        var hits = await _db.EmailSuppressions
            .AsNoTracking()
            .Where(s => emailAddresses.Contains(s.EmailAddress))
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return hits.ToDictionary(h => h.EmailAddress, h => h);
    }

    public async Task<PagedResult<EmailSuppression>> ListAsync(
        string? search,
        SuppressionType? type,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var q = _db.EmailSuppressions.AsNoTracking().AsQueryable();
        if (type is { } t) q = q.Where(s => s.SuppressionType == t);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLowerInvariant();
            q = q.Where(s => EF.Functions.Like(s.EmailAddress, $"%{term}%"));
        }

        var total = await q.CountAsync(ct).ConfigureAwait(false);
        var items = await q
            .OrderByDescending(s => s.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct)
            .ConfigureAwait(false);

        return new PagedResult<EmailSuppression>(items, total, page, pageSize);
    }

    public async Task UpsertAsync(EmailSuppression suppression, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(suppression);
        var existing = await _db.EmailSuppressions
            .FirstOrDefaultAsync(s => s.EmailAddress == suppression.EmailAddress, ct)
            .ConfigureAwait(false);
        if (existing is not null)
        {
            // Already suppressed — keep the original context (more
            // informative than the latest event).
            return;
        }

        _db.EmailSuppressions.Add(suppression);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task RemoveAsync(string emailAddress, CancellationToken ct = default)
    {
        var existing = await _db.EmailSuppressions
            .FirstOrDefaultAsync(s => s.EmailAddress == emailAddress, ct)
            .ConfigureAwait(false);
        if (existing is null) return;
        _db.EmailSuppressions.Remove(existing);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
