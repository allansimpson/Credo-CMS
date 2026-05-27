using CredoCms.Application.Tags;
using CredoCms.Domain.Tags;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CredoCms.Infrastructure.Tags;

public sealed class TagRepository : ITagRepository
{
    private readonly ApplicationDbContext _db;
    public TagRepository(ApplicationDbContext db) => _db = db;

    public Task<Tag?> GetByNameInsensitiveAsync(string name, CancellationToken ct = default)
        => _db.Tags.FirstOrDefaultAsync(t => EF.Functions.Like(t.Name, name), ct);

    public Task<List<Tag>> SearchAsync(string query, int limit, CancellationToken ct = default)
    {
        IQueryable<Tag> q = _db.Tags;
        if (!string.IsNullOrWhiteSpace(query))
        {
            var term = $"%{query.Trim()}%";
            q = q.Where(t => EF.Functions.Like(t.Name, term) || EF.Functions.Like(t.Slug, term));
        }
        return q.OrderByDescending(t => t.UsageCount)
            .ThenBy(t => t.Name)
            .Take(limit)
            .ToListAsync(ct);
    }

    public Task<List<Tag>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var idList = ids.ToList();
        return _db.Tags.Where(t => idList.Contains(t.Id)).ToListAsync(ct);
    }

    public async Task AddAsync(Tag tag, CancellationToken ct = default)
    {
        _db.Tags.Add(tag);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateUsageAsync(Guid id, int delta, CancellationToken ct = default)
    {
        await _db.Tags.Where(t => t.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.UsageCount, t => t.UsageCount + delta), ct)
            .ConfigureAwait(false);
    }
}
