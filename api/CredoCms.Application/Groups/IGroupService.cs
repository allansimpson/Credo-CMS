using CredoCms.Application.Common;

namespace CredoCms.Application.Groups;

/// <summary>
/// Group service. Owns ALL permission checks; the controllers do nothing
/// but plumbing. Permission rules:
///
///   Group create / edit / soft-delete       Administrator only
///   Add member directly                     Administrator + Editor
///   Promote to / demote from leader         Administrator only
///   Approve / decline pending request       Administrator + Editor + active leader
///   Remove member                           Administrator + Editor + active leader
///   Submit join request                     any authenticated user
///                                           (subject to GroupVisibility +
///                                            GroupJoinability gates)
///   Leave own membership                    any authenticated user
/// </summary>
public interface IGroupService
{
    // ---- admin reads -------------------------------------------------------

    Task<List<AdminGroupListItemDto>> ListAdminAsync(string? search, bool includeInactive, CancellationToken ct = default);
    Task<AdminGroupDetailDto?> GetAdminAsync(Guid id, CancellationToken ct = default);
    Task<List<AdminMembershipDto>> ListMembershipsAsync(Guid groupId, Domain.Groups.GroupMembershipStatus? status, CancellationToken ct = default);

    // ---- admin writes ------------------------------------------------------

    Task<GroupMutationResult> CreateAsync(CreateGroupRequest request, CancellationToken ct = default);
    Task<GroupMutationResult> UpdateAsync(Guid id, UpdateGroupRequest request, CancellationToken ct = default);
    Task<GroupMutationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default);

    Task<MembershipMutationResult> AddMemberAsync(Guid groupId, AddMemberRequest request, CancellationToken ct = default);
    Task<MembershipMutationResult> RemoveMemberAsync(Guid groupId, Guid userId, CancellationToken ct = default);
    Task<MembershipMutationResult> SetLeaderAsync(Guid groupId, Guid userId, bool isLeader, CancellationToken ct = default);
    Task<MembershipMutationResult> ApproveJoinRequestAsync(Guid membershipId, CancellationToken ct = default);
    Task<MembershipMutationResult> DeclineJoinRequestAsync(Guid membershipId, CancellationToken ct = default);

    // ---- public reads ------------------------------------------------------

    Task<List<PublicGroupListItemDto>> ListPublicAsync(CancellationToken ct = default);
    Task<PublicGroupDetailDto?> GetPublicBySlugAsync(string slug, CancellationToken ct = default);

    // ---- member writes -----------------------------------------------------

    Task<MembershipMutationResult> SubmitJoinRequestAsync(string slug, JoinRequestRequest request, CancellationToken ct = default);
    Task<MembershipMutationResult> LeaveAsync(Guid groupId, CancellationToken ct = default);

    // ---- profile reads -----------------------------------------------------

    Task<List<ProfileMembershipDto>> ListMyMembershipsAsync(CancellationToken ct = default);
}
