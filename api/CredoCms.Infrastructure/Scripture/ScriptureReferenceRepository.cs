using CredoCms.Application.Scripture;
using CredoCms.Domain.Scripture;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CredoCms.Infrastructure.Scripture;

public sealed class ScriptureReferenceRepository : IScriptureReferenceRepository
{
    private readonly ApplicationDbContext _db;
    public ScriptureReferenceRepository(ApplicationDbContext db) => _db = db;

    public Task<List<ScriptureReference>> ListForParentAsync(string parentEntityType, Guid parentEntityId, CancellationToken ct = default)
        => _db.ScriptureReferences
            .Where(r => r.ParentEntityType == parentEntityType && r.ParentEntityId == parentEntityId)
            .OrderBy(r => r.DisplayOrder)
            .ToListAsync(ct);

    public async Task ReplaceAllAsync(string parentEntityType, Guid parentEntityId, IEnumerable<ScriptureReference> next, CancellationToken ct = default)
    {
        await _db.ScriptureReferences
            .Where(r => r.ParentEntityType == parentEntityType && r.ParentEntityId == parentEntityId)
            .ExecuteDeleteAsync(ct).ConfigureAwait(false);
        _db.ScriptureReferences.AddRange(next);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task DeleteAllForParentAsync(string parentEntityType, Guid parentEntityId, CancellationToken ct = default)
    {
        await _db.ScriptureReferences
            .Where(r => r.ParentEntityType == parentEntityType && r.ParentEntityId == parentEntityId)
            .ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }
}
