using CredoCms.Application.ConnectCard;
using CredoCms.Domain.ConnectCard;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CredoCms.Infrastructure.ConnectCard;

public sealed class ConnectCardRepository : IConnectCardRepository
{
    private readonly ApplicationDbContext _db;
    public ConnectCardRepository(ApplicationDbContext db) => _db = db;

    public Task<ConnectCardSubmission?> GetAsync(Guid id, CancellationToken ct = default) =>
        _db.ConnectCardSubmissions.FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<List<ConnectCardSubmission>> ListAsync(AdminConnectCardListQuery query, CancellationToken ct = default)
    {
        var q = _db.ConnectCardSubmissions.AsNoTracking().AsQueryable();
        if (query.Status is { } status) q = q.Where(s => s.Status == status);
        if (query.IsFirstTimeVisitor is { } first) q = q.Where(s => s.IsFirstTimeVisitor == first);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            q = q.Where(s =>
                EF.Functions.Like(s.Name, $"%{term}%")
                || (s.Email != null && EF.Functions.Like(s.Email, $"%{term}%")));
        }
        return await q.OrderByDescending(s => s.SubmittedAt).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task AddAsync(ConnectCardSubmission entity, CancellationToken ct = default)
    {
        _db.ConnectCardSubmissions.Add(entity);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(ConnectCardSubmission entity, CancellationToken ct = default)
    {
        _db.ConnectCardSubmissions.Update(entity);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
