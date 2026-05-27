using CredoCms.Application.Volunteers;
using CredoCms.Domain.Volunteers;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CredoCms.Infrastructure.Volunteers;

public sealed class EventVolunteerRoleRepository : IEventVolunteerRoleRepository
{
    private readonly ApplicationDbContext _db;
    public EventVolunteerRoleRepository(ApplicationDbContext db) => _db = db;

    public Task<EventVolunteerRole?> GetAsync(Guid id, CancellationToken ct = default) =>
        _db.EventVolunteerRoles.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<List<EventVolunteerRole>> ListByEventAsync(Guid eventId, CancellationToken ct = default) =>
        _db.EventVolunteerRoles.Where(r => r.EventId == eventId)
            .OrderBy(r => r.DisplayOrder).ThenBy(r => r.RoleName)
            .ToListAsync(ct);

    public async Task AddAsync(EventVolunteerRole role, CancellationToken ct = default)
    {
        _db.EventVolunteerRoles.Add(role);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(EventVolunteerRole role, CancellationToken ct = default)
    {
        _db.EventVolunteerRoles.Update(role);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task SoftDeleteAsync(Guid id, Guid byUserId, CancellationToken ct = default)
    {
        var role = await _db.EventVolunteerRoles.FirstOrDefaultAsync(r => r.Id == id, ct).ConfigureAwait(false);
        if (role is null) return;
        role.IsDeleted = true;
        role.DeletedAt = DateTimeOffset.UtcNow;
        role.DeletedByUserId = byUserId;
        role.ModifiedAt = role.DeletedAt.Value;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
