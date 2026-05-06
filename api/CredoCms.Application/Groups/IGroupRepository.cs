using CredoCms.Domain.Groups;

namespace CredoCms.Application.Groups;

/// <summary>
/// Read/write access to <see cref="Group"/>. The repository is
/// <see cref="GroupVisibility"/>-aware; consumers ask for "public" listings
/// (auth-gated rules applied at the service layer).
/// </summary>
public interface IGroupRepository
{
    Task<Group?> GetAsync(Guid id, CancellationToken ct = default);
    Task<Group?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<List<Group>> ListAdminAsync(string? search, bool includeInactive, CancellationToken ct = default);

    /// <summary>
    /// Returns groups visible to anonymous (Public only) or authenticated
    /// members (Public + MembersOnly). Hidden groups never appear here. Sort
    /// is alphabetical by Name.
    /// </summary>
    Task<List<Group>> ListPublicAsync(bool includeMembersOnly, CancellationToken ct = default);

    Task<bool> SlugExistsAsync(string slug, Guid? excludeId, CancellationToken ct = default);
    Task AddAsync(Group group, CancellationToken ct = default);
    Task UpdateAsync(Group group, CancellationToken ct = default);

    /// <summary>Soft delete — sets IsDeleted, DeletedAt, DeletedByUserId.</summary>
    Task SoftDeleteAsync(Guid id, Guid byUserId, CancellationToken ct = default);
}

public interface IGroupMembershipRepository
{
    Task<GroupMembership?> GetAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Returns the most-recent live membership row for (groupId, userId), where
    /// "live" means Pending or Active. Removed/Declined rows are historical
    /// and ignored — re-joining after a Removed row creates a fresh row.
    /// </summary>
    Task<GroupMembership?> GetLiveMembershipAsync(Guid groupId, Guid userId, CancellationToken ct = default);

    Task<List<GroupMembership>> ListForGroupAsync(
        Guid groupId,
        GroupMembershipStatus? status,
        CancellationToken ct = default);

    Task<List<GroupMembership>> ListActiveForUserAsync(Guid userId, CancellationToken ct = default);

    Task<List<GroupMembership>> ListAllForUserAsync(Guid userId, CancellationToken ct = default);

    /// <summary>
    /// Returns active leaders for the group. Used by the SignalR notifier and
    /// the join-approval permission check.
    /// </summary>
    Task<List<Guid>> ListLeaderUserIdsAsync(Guid groupId, CancellationToken ct = default);

    Task<int> CountActiveAsync(Guid groupId, CancellationToken ct = default);
    Task<int> CountPendingAsync(Guid groupId, CancellationToken ct = default);

    Task AddAsync(GroupMembership membership, CancellationToken ct = default);
    Task UpdateAsync(GroupMembership membership, CancellationToken ct = default);
}
