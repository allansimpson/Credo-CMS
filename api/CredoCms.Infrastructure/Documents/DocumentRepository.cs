using CredoCms.Application.Documents;
using CredoCms.Domain.Documents;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CredoCms.Infrastructure.Documents;

public sealed class DocumentRepository : IDocumentRepository
{
    private readonly ApplicationDbContext _db;
    public DocumentRepository(ApplicationDbContext db) => _db = db;

    public async Task<List<Document>> ListAsync(string? category, bool includeDeleted, CancellationToken ct = default)
    {
        IQueryable<Document> q = includeDeleted ? _db.Documents.IgnoreQueryFilters() : _db.Documents;
        if (includeDeleted) q = q.Where(d => d.IsDeleted);
        if (!string.IsNullOrWhiteSpace(category)) q = q.Where(d => d.Category == category);
        return await q.OrderBy(d => d.Category).ThenByDescending(d => d.ModifiedAt).ToListAsync(ct).ConfigureAwait(false);
    }

    public Task<Document?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default)
    {
        var q = includeDeleted ? _db.Documents.IgnoreQueryFilters() : _db.Documents;
        return q.FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task<List<PublicDocumentDto>> ListPublicAsync(bool includeMembersOnly, CancellationToken ct = default)
    {
        var q = _db.Documents.Where(d => d.IsPublished);
        if (!includeMembersOnly) q = q.Where(d => !d.IsMembersOnly);
        return await q.OrderBy(d => d.Category).ThenBy(d => d.Title)
            .Select(d => new PublicDocumentDto(d.Id, d.Title, d.Description, d.Category,
                d.SizeBytes, d.IsMembersOnly, d.ModifiedAt))
            .ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task AddAsync(Document doc, CancellationToken ct = default)
    {
        _db.Documents.Add(doc);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Document doc, CancellationToken ct = default)
    {
        _db.Documents.Update(doc);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<bool> HardDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var doc = await _db.Documents.IgnoreQueryFilters()
            .FirstOrDefaultAsync(d => d.Id == id, ct).ConfigureAwait(false);
        if (doc is null) return false;
        _db.Documents.Remove(doc);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }
}
