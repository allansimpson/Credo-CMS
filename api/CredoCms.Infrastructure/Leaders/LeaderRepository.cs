using CredoCms.Application.Leaders;
using CredoCms.Domain.Leaders;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CredoCms.Infrastructure.Leaders;

public sealed class LeaderRepository : ILeaderRepository
{
    private readonly ApplicationDbContext _db;
    public LeaderRepository(ApplicationDbContext db) => _db = db;

    public Task<List<Leader>> ListAsync(CancellationToken ct = default)
        => _db.Leaders
            .OrderBy(l => l.Category)
            .ThenBy(l => l.DisplayOrder)
            .ThenBy(l => l.FullName)
            .ToListAsync(ct);

    public Task<Leader?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _db.Leaders.FirstOrDefaultAsync(l => l.Id == id, ct);

    public async Task AddAsync(Leader leader, CancellationToken ct = default)
    {
        _db.Leaders.Add(leader);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Leader leader, CancellationToken ct = default)
    {
        _db.Leaders.Update(leader);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var leader = await _db.Leaders.FirstOrDefaultAsync(l => l.Id == id, ct).ConfigureAwait(false);
        if (leader is null) return false;
        _db.Leaders.Remove(leader);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }
}
