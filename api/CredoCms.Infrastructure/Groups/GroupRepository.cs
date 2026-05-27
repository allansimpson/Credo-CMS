using CredoCms.Application.Groups;
using CredoCms.Domain.Groups;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CredoCms.Infrastructure.Groups;

public sealed class GroupRepository : IGroupRepository
{
    private readonly ApplicationDbContext _db;
    public GroupRepository(ApplicationDbContext db) => _db = db;

    public Task<Group?> GetAsync(Guid id, CancellationToken ct = default) =>
        _db.Groups.FirstOrDefaultAsync(g => g.Id == id, ct);

    public Task<Group?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        _db.Groups.FirstOrDefaultAsync(g => g.Slug == slug, ct);

    public async Task<List<Group>> ListAdminAsync(string? search, bool includeInactive, CancellationToken ct = default)
    {
        var q = _db.Groups.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim();
            q = q.Where(g => EF.Functions.Like(g.Name, $"%{s}%") || EF.Functions.Like(g.Slug, $"%{s}%"));
        }
        if (!includeInactive)
        {
            q = q.Where(g => g.IsActive);
        }
        return await q.OrderBy(g => g.Name).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<List<Group>> ListPublicAsync(bool includeMembersOnly, CancellationToken ct = default)
    {
        // Hidden groups never appear in either anonymous or members lists; the
        // soft-delete query filter on Group already excludes deleted rows.
        var q = _db.Groups.AsNoTracking().Where(g => g.IsActive);
        q = includeMembersOnly
            ? q.Where(g => g.Visibility != GroupVisibility.Hidden)
            : q.Where(g => g.Visibility == GroupVisibility.Public);
        return await q.OrderBy(g => g.Name).ToListAsync(ct).ConfigureAwait(false);
    }

    public Task<bool> SlugExistsAsync(string slug, Guid? excludeId, CancellationToken ct = default)
    {
        var q = _db.Groups.Where(g => g.Slug == slug);
        if (excludeId is { } id) q = q.Where(g => g.Id != id);
        return q.AnyAsync(ct);
    }

    public async Task AddAsync(Group group, CancellationToken ct = default)
    {
        _db.Groups.Add(group);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Group group, CancellationToken ct = default)
    {
        _db.Groups.Update(group);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task SoftDeleteAsync(Guid id, Guid byUserId, CancellationToken ct = default)
    {
        var entity = await _db.Groups.FirstOrDefaultAsync(g => g.Id == id, ct).ConfigureAwait(false);
        if (entity is null) return;
        entity.IsDeleted = true;
        entity.DeletedAt = DateTimeOffset.UtcNow;
        entity.DeletedByUserId = byUserId;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}

public sealed class GroupMembershipRepository : IGroupMembershipRepository
{
    private readonly ApplicationDbContext _db;
    public GroupMembershipRepository(ApplicationDbContext db) => _db = db;

    public Task<GroupMembership?> GetAsync(Guid id, CancellationToken ct = default) =>
        _db.GroupMemberships.FirstOrDefaultAsync(m => m.Id == id, ct);

    public Task<GroupMembership?> GetLiveMembershipAsync(Guid groupId, Guid userId, CancellationToken ct = default) =>
        _db.GroupMemberships
            .Where(m => m.GroupId == groupId && m.UserId == userId
                && (m.Status == GroupMembershipStatus.Active || m.Status == GroupMembershipStatus.Pending))
            .OrderByDescending(m => m.RequestedAt ?? m.JoinedAt)
            .FirstOrDefaultAsync(ct);

    public async Task<List<GroupMembership>> ListForGroupAsync(
        Guid groupId,
        GroupMembershipStatus? status,
        CancellationToken ct = default)
    {
        var q = _db.GroupMemberships.AsNoTracking().Where(m => m.GroupId == groupId);
        if (status is { } s) q = q.Where(m => m.Status == s);
        return await q.OrderByDescending(m => m.JoinedAt ?? m.RequestedAt).ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<List<GroupMembership>> ListActiveForUserAsync(Guid userId, CancellationToken ct = default) =>
        await _db.GroupMemberships.AsNoTracking()
            .Where(m => m.UserId == userId && m.Status == GroupMembershipStatus.Active)
            .ToListAsync(ct).ConfigureAwait(false);

    public async Task<List<GroupMembership>> ListAllForUserAsync(Guid userId, CancellationToken ct = default) =>
        await _db.GroupMemberships.AsNoTracking()
            .Where(m => m.UserId == userId
                && (m.Status == GroupMembershipStatus.Active || m.Status == GroupMembershipStatus.Pending))
            .ToListAsync(ct).ConfigureAwait(false);

    public async Task<List<Guid>> ListLeaderUserIdsAsync(Guid groupId, CancellationToken ct = default) =>
        await _db.GroupMemberships.AsNoTracking()
            .Where(m => m.GroupId == groupId
                && m.Status == GroupMembershipStatus.Active
                && m.IsLeader)
            .Select(m => m.UserId)
            .ToListAsync(ct).ConfigureAwait(false);

    public Task<int> CountActiveAsync(Guid groupId, CancellationToken ct = default) =>
        _db.GroupMemberships.CountAsync(
            m => m.GroupId == groupId && m.Status == GroupMembershipStatus.Active, ct);

    public Task<int> CountPendingAsync(Guid groupId, CancellationToken ct = default) =>
        _db.GroupMemberships.CountAsync(
            m => m.GroupId == groupId && m.Status == GroupMembershipStatus.Pending, ct);

    public async Task AddAsync(GroupMembership membership, CancellationToken ct = default)
    {
        _db.GroupMemberships.Add(membership);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(GroupMembership membership, CancellationToken ct = default)
    {
        _db.GroupMemberships.Update(membership);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
