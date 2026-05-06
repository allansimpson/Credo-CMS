using CredoCms.Application.Caching;
using CredoCms.Application.Common;
using CredoCms.Application.RealTime;
using CredoCms.Domain.Common;
using CredoCms.Domain.Groups;
using CredoCms.Domain.Identity;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace CredoCms.Application.Groups;

public sealed class GroupService : IGroupService
{
    private readonly IGroupRepository _groups;
    private readonly IGroupMembershipRepository _memberships;
    private readonly UserManager<ApplicationUser> _users;
    private readonly ICurrentUserService _currentUser;
    private readonly IRealtimeNotifier _notifier;
    private readonly IAuditLogger _audit;
    private readonly IOutputCacheInvalidator _cache;
    private readonly IValidator<CreateGroupRequest> _createValidator;
    private readonly IValidator<UpdateGroupRequest> _updateValidator;

    public GroupService(
        IGroupRepository groups,
        IGroupMembershipRepository memberships,
        UserManager<ApplicationUser> users,
        ICurrentUserService currentUser,
        IRealtimeNotifier notifier,
        IAuditLogger audit,
        IOutputCacheInvalidator cache,
        IValidator<CreateGroupRequest> createValidator,
        IValidator<UpdateGroupRequest> updateValidator)
    {
        _groups = groups;
        _memberships = memberships;
        _users = users;
        _currentUser = currentUser;
        _notifier = notifier;
        _audit = audit;
        _cache = cache;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    private bool IsAdmin => _currentUser.Roles.Contains(SystemConstants.Roles.Administrator);
    private bool IsEditor => _currentUser.Roles.Contains(SystemConstants.Roles.Editor);
    private bool IsAuthenticated => _currentUser.IsAuthenticated && _currentUser.UserId != SystemConstants.SystemUserId;

    // ---- admin reads -------------------------------------------------------

    public async Task<List<AdminGroupListItemDto>> ListAdminAsync(string? search, bool includeInactive, CancellationToken ct = default)
    {
        var groups = await _groups.ListAdminAsync(search, includeInactive, ct).ConfigureAwait(false);
        var items = new List<AdminGroupListItemDto>(groups.Count);
        foreach (var g in groups)
        {
            var active = await _memberships.CountActiveAsync(g.Id, ct).ConfigureAwait(false);
            var pending = await _memberships.CountPendingAsync(g.Id, ct).ConfigureAwait(false);
            items.Add(new AdminGroupListItemDto(g.Id, g.Slug, g.Name, g.Visibility, g.Joinability,
                g.IsActive, active, pending, g.ModifiedAt));
        }
        return items;
    }

    public async Task<AdminGroupDetailDto?> GetAdminAsync(Guid id, CancellationToken ct = default)
    {
        var g = await _groups.GetAsync(id, ct).ConfigureAwait(false);
        return g is null ? null : ToAdminDetail(g);
    }

    public async Task<List<AdminMembershipDto>> ListMembershipsAsync(Guid groupId, GroupMembershipStatus? status, CancellationToken ct = default)
    {
        var rows = await _memberships.ListForGroupAsync(groupId, status, ct).ConfigureAwait(false);
        return await HydrateMembershipsAsync(rows, ct).ConfigureAwait(false);
    }

    // ---- admin writes ------------------------------------------------------

    public async Task<GroupMutationResult> CreateAsync(CreateGroupRequest request, CancellationToken ct = default)
    {
        if (!IsAdmin) return GroupMutationResult.Failure("Only administrators can create groups.");

        var v = await _createValidator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!v.IsValid) return GroupMutationResult.Failure([.. v.Errors.Select(e => e.ErrorMessage)]);

        if (await _groups.SlugExistsAsync(request.Slug, null, ct).ConfigureAwait(false))
        {
            return GroupMutationResult.Failure($"A group with slug \"{request.Slug}\" already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        var entity = new Group
        {
            Id = Guid.NewGuid(),
            Slug = request.Slug,
            Name = request.Name,
            DescriptionJson = request.DescriptionJson,
            ImageBlobUrl = request.ImageBlobUrl,
            ImageWebpBlobUrl = request.ImageWebpBlobUrl,
            ImageAltText = request.ImageAltText,
            ContactEmail = request.ContactEmail,
            MeetingInfo = request.MeetingInfo,
            Visibility = request.Visibility,
            Joinability = request.Joinability,
            RequiresMessageOnJoinRequest = request.RequiresMessageOnJoinRequest,
            RosterVisibility = request.RosterVisibility,
            IsActive = request.IsActive,
            CreatedAt = now,
            ModifiedAt = now,
            ModifiedByUserId = _currentUser.UserId,
        };
        await _groups.AddAsync(entity, ct).ConfigureAwait(false);

        await _audit.WriteAsync("Group.Created", nameof(Group), entity.Id.ToString(),
            new { entity.Slug, entity.Name, entity.Visibility }, ct).ConfigureAwait(false);
        await _cache.InvalidateAsync(OutputCacheTags.Groups, ct).ConfigureAwait(false);

        return GroupMutationResult.Success(ToAdminDetail(entity));
    }

    public async Task<GroupMutationResult> UpdateAsync(Guid id, UpdateGroupRequest request, CancellationToken ct = default)
    {
        if (!IsAdmin) return GroupMutationResult.Failure("Only administrators can edit groups.");

        var v = await _updateValidator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!v.IsValid) return GroupMutationResult.Failure([.. v.Errors.Select(e => e.ErrorMessage)]);

        var entity = await _groups.GetAsync(id, ct).ConfigureAwait(false);
        if (entity is null) return GroupMutationResult.Failure("Group not found.");

        if (await _groups.SlugExistsAsync(request.Slug, id, ct).ConfigureAwait(false))
        {
            return GroupMutationResult.Failure($"Slug \"{request.Slug}\" is already in use.");
        }

        entity.Slug = request.Slug;
        entity.Name = request.Name;
        entity.DescriptionJson = request.DescriptionJson;
        entity.ImageBlobUrl = request.ImageBlobUrl;
        entity.ImageWebpBlobUrl = request.ImageWebpBlobUrl;
        entity.ImageAltText = request.ImageAltText;
        entity.ContactEmail = request.ContactEmail;
        entity.MeetingInfo = request.MeetingInfo;
        entity.Visibility = request.Visibility;
        entity.Joinability = request.Joinability;
        entity.RequiresMessageOnJoinRequest = request.RequiresMessageOnJoinRequest;
        entity.RosterVisibility = request.RosterVisibility;
        entity.IsActive = request.IsActive;
        entity.ModifiedAt = DateTimeOffset.UtcNow;
        entity.ModifiedByUserId = _currentUser.UserId;
        await _groups.UpdateAsync(entity, ct).ConfigureAwait(false);

        await _audit.WriteAsync("Group.Updated", nameof(Group), entity.Id.ToString(),
            new { entity.Slug, entity.Name, entity.Visibility, entity.IsActive }, ct).ConfigureAwait(false);
        await _cache.InvalidateAsync(OutputCacheTags.Groups, ct).ConfigureAwait(false);

        return GroupMutationResult.Success(ToAdminDetail(entity));
    }

    public async Task<GroupMutationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (!IsAdmin) return GroupMutationResult.Failure("Only administrators can delete groups.");

        var entity = await _groups.GetAsync(id, ct).ConfigureAwait(false);
        if (entity is null) return GroupMutationResult.Failure("Group not found.");

        await _groups.SoftDeleteAsync(id, _currentUser.UserId, ct).ConfigureAwait(false);
        await _audit.WriteAsync("Group.SoftDeleted", nameof(Group), id.ToString(),
            new { entity.Slug, entity.Name }, ct).ConfigureAwait(false);
        await _cache.InvalidateAsync(OutputCacheTags.Groups, ct).ConfigureAwait(false);

        return GroupMutationResult.Success(ToAdminDetail(entity));
    }

    public async Task<MembershipMutationResult> AddMemberAsync(Guid groupId, AddMemberRequest request, CancellationToken ct = default)
    {
        if (!IsAdmin && !IsEditor) return MembershipMutationResult.Failure("You don't have permission to add members.");

        // Promote-to-leader at add-time is admin-only — same rule as the
        // explicit promote endpoint.
        if (request.IsLeader && !IsAdmin)
        {
            return MembershipMutationResult.Failure("Only administrators can designate leaders.");
        }

        var group = await _groups.GetAsync(groupId, ct).ConfigureAwait(false);
        if (group is null) return MembershipMutationResult.Failure("Group not found.");

        var existing = await _memberships.GetLiveMembershipAsync(groupId, request.UserId, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            return MembershipMutationResult.Failure(existing.Status == GroupMembershipStatus.Active
                ? "User is already an active member."
                : "User already has a pending request — approve it instead.");
        }

        var now = DateTimeOffset.UtcNow;
        var membership = new GroupMembership
        {
            Id = Guid.NewGuid(),
            GroupId = groupId,
            UserId = request.UserId,
            Status = GroupMembershipStatus.Active,
            IsLeader = request.IsLeader,
            JoinedAt = now,
            ProcessedByUserId = _currentUser.UserId,
            ProcessedAt = now,
        };
        await _memberships.AddAsync(membership, ct).ConfigureAwait(false);

        await _audit.WriteAsync("Group.MemberAdded", nameof(GroupMembership), membership.Id.ToString(),
            new { groupId, request.UserId, request.IsLeader }, ct).ConfigureAwait(false);

        return MembershipMutationResult.Success(await ToAdminMembership(membership, ct).ConfigureAwait(false));
    }

    public async Task<MembershipMutationResult> RemoveMemberAsync(Guid groupId, Guid userId, CancellationToken ct = default)
    {
        var allowed = IsAdmin || IsEditor || await IsCurrentUserLeaderOf(groupId, ct).ConfigureAwait(false);
        if (!allowed) return MembershipMutationResult.Failure("You don't have permission to remove members.");

        var membership = await _memberships.GetLiveMembershipAsync(groupId, userId, ct).ConfigureAwait(false);
        if (membership is null) return MembershipMutationResult.Failure("Membership not found.");

        membership.Status = GroupMembershipStatus.Removed;
        membership.IsLeader = false;
        membership.ProcessedByUserId = _currentUser.UserId;
        membership.ProcessedAt = DateTimeOffset.UtcNow;
        await _memberships.UpdateAsync(membership, ct).ConfigureAwait(false);

        await _audit.WriteAsync("Group.MemberRemoved", nameof(GroupMembership), membership.Id.ToString(),
            new { groupId, userId }, ct).ConfigureAwait(false);

        return MembershipMutationResult.Success(await ToAdminMembership(membership, ct).ConfigureAwait(false));
    }

    public async Task<MembershipMutationResult> SetLeaderAsync(Guid groupId, Guid userId, bool isLeader, CancellationToken ct = default)
    {
        if (!IsAdmin) return MembershipMutationResult.Failure("Only administrators can promote members to leader.");

        var membership = await _memberships.GetLiveMembershipAsync(groupId, userId, ct).ConfigureAwait(false);
        if (membership is null || membership.Status != GroupMembershipStatus.Active)
        {
            return MembershipMutationResult.Failure("Member must be active before promoting to leader.");
        }

        membership.IsLeader = isLeader;
        await _memberships.UpdateAsync(membership, ct).ConfigureAwait(false);

        await _audit.WriteAsync(isLeader ? "Group.LeaderPromoted" : "Group.LeaderDemoted",
            nameof(GroupMembership), membership.Id.ToString(),
            new { groupId, userId }, ct).ConfigureAwait(false);

        return MembershipMutationResult.Success(await ToAdminMembership(membership, ct).ConfigureAwait(false));
    }

    public async Task<MembershipMutationResult> ApproveJoinRequestAsync(Guid membershipId, CancellationToken ct = default)
    {
        var membership = await _memberships.GetAsync(membershipId, ct).ConfigureAwait(false);
        if (membership is null || membership.Status != GroupMembershipStatus.Pending)
        {
            return MembershipMutationResult.Failure("Pending request not found.");
        }

        var allowed = IsAdmin || IsEditor || await IsCurrentUserLeaderOf(membership.GroupId, ct).ConfigureAwait(false);
        if (!allowed) return MembershipMutationResult.Failure("You don't have permission to approve this request.");

        membership.Status = GroupMembershipStatus.Active;
        membership.JoinedAt = DateTimeOffset.UtcNow;
        membership.ProcessedByUserId = _currentUser.UserId;
        membership.ProcessedAt = DateTimeOffset.UtcNow;
        await _memberships.UpdateAsync(membership, ct).ConfigureAwait(false);

        var group = await _groups.GetAsync(membership.GroupId, ct).ConfigureAwait(false);
        await _audit.WriteAsync("Group.JoinApproved", nameof(GroupMembership), membership.Id.ToString(),
            new { membership.GroupId, membership.UserId }, ct).ConfigureAwait(false);

        if (group is not null)
        {
            await _notifier.NotifyGroupMembershipDecisionAsync(
                membership.UserId,
                new GroupMembershipDecisionMessage(group.Id, group.Name, Approved: true),
                ct).ConfigureAwait(false);
        }

        return MembershipMutationResult.Success(await ToAdminMembership(membership, ct).ConfigureAwait(false));
    }

    public async Task<MembershipMutationResult> DeclineJoinRequestAsync(Guid membershipId, CancellationToken ct = default)
    {
        var membership = await _memberships.GetAsync(membershipId, ct).ConfigureAwait(false);
        if (membership is null || membership.Status != GroupMembershipStatus.Pending)
        {
            return MembershipMutationResult.Failure("Pending request not found.");
        }

        var allowed = IsAdmin || IsEditor || await IsCurrentUserLeaderOf(membership.GroupId, ct).ConfigureAwait(false);
        if (!allowed) return MembershipMutationResult.Failure("You don't have permission to decline this request.");

        membership.Status = GroupMembershipStatus.Declined;
        membership.ProcessedByUserId = _currentUser.UserId;
        membership.ProcessedAt = DateTimeOffset.UtcNow;
        await _memberships.UpdateAsync(membership, ct).ConfigureAwait(false);

        var group = await _groups.GetAsync(membership.GroupId, ct).ConfigureAwait(false);
        await _audit.WriteAsync("Group.JoinDeclined", nameof(GroupMembership), membership.Id.ToString(),
            new { membership.GroupId, membership.UserId }, ct).ConfigureAwait(false);

        if (group is not null)
        {
            await _notifier.NotifyGroupMembershipDecisionAsync(
                membership.UserId,
                new GroupMembershipDecisionMessage(group.Id, group.Name, Approved: false),
                ct).ConfigureAwait(false);
        }

        return MembershipMutationResult.Success(await ToAdminMembership(membership, ct).ConfigureAwait(false));
    }

    // ---- public reads ------------------------------------------------------

    public async Task<List<PublicGroupListItemDto>> ListPublicAsync(CancellationToken ct = default)
    {
        var groups = await _groups.ListPublicAsync(IsAuthenticated, ct).ConfigureAwait(false);
        return groups.Select(g => new PublicGroupListItemDto(
            g.Id, g.Slug, g.Name,
            g.ImageBlobUrl, g.ImageWebpBlobUrl, g.ImageAltText,
            g.MeetingInfo, g.Visibility, g.Joinability)).ToList();
    }

    public async Task<PublicGroupDetailDto?> GetPublicBySlugAsync(string slug, CancellationToken ct = default)
    {
        var group = await _groups.GetBySlugAsync(slug, ct).ConfigureAwait(false);
        if (group is null || !group.IsActive) return null;

        // Visibility gate: anonymous → Public only; authenticated → Public + MembersOnly.
        // Hidden groups are 404 to everyone here (admins manage them via the admin route).
        if (group.Visibility == GroupVisibility.Hidden) return null;
        if (group.Visibility == GroupVisibility.MembersOnly && !IsAuthenticated) return null;

        var membership = IsAuthenticated
            ? await _memberships.GetLiveMembershipAsync(group.Id, _currentUser.UserId, ct).ConfigureAwait(false)
            : null;

        var viewerIsMember = membership is { Status: GroupMembershipStatus.Active };
        var viewerHasPending = membership is { Status: GroupMembershipStatus.Pending };
        var viewerIsLeader = viewerIsMember && membership!.IsLeader;

        // Roster visibility:
        //   LeadersOnly       → only this group's active leaders (or admins/editors)
        //   AllGroupMembers   → any active member of this group (or admins/editors)
        var canSeeRoster = group.RosterVisibility switch
        {
            RosterVisibility.LeadersOnly => IsAdmin || IsEditor || viewerIsLeader,
            RosterVisibility.AllGroupMembers => IsAdmin || IsEditor || viewerIsMember,
            _ => false,
        };

        IReadOnlyList<GroupRosterEntryDto>? roster = null;
        if (canSeeRoster)
        {
            var actives = await _memberships.ListForGroupAsync(group.Id, GroupMembershipStatus.Active, ct).ConfigureAwait(false);
            var entries = new List<GroupRosterEntryDto>(actives.Count);
            foreach (var m in actives)
            {
                var user = await _users.FindByIdAsync(m.UserId.ToString()).ConfigureAwait(false);
                if (user is null) continue;
                entries.Add(new GroupRosterEntryDto(
                    m.UserId,
                    user.DisplayName,
                    m.IsLeader,
                    user.ShowPhotoInDirectory ? user.PhotoBlobUrl : null,
                    user.ShowPhotoInDirectory ? user.PhotoWebpBlobUrl : null,
                    user.ShowPhotoInDirectory ? user.PhotoAltText : null));
            }
            roster = entries;
        }

        return new PublicGroupDetailDto(
            group.Id, group.Slug, group.Name,
            group.DescriptionJson, group.ImageBlobUrl, group.ImageWebpBlobUrl, group.ImageAltText,
            group.ContactEmail, group.MeetingInfo,
            group.Visibility, group.Joinability, group.RequiresMessageOnJoinRequest,
            roster, viewerIsMember, viewerHasPending);
    }

    // ---- member writes -----------------------------------------------------

    public async Task<MembershipMutationResult> SubmitJoinRequestAsync(string slug, JoinRequestRequest request, CancellationToken ct = default)
    {
        if (!IsAuthenticated) return MembershipMutationResult.Failure("Sign in to request to join.");

        var group = await _groups.GetBySlugAsync(slug, ct).ConfigureAwait(false);
        if (group is null || !group.IsActive || group.Visibility == GroupVisibility.Hidden)
        {
            return MembershipMutationResult.Failure("Group not found.");
        }

        if (group.Visibility == GroupVisibility.MembersOnly && !IsAuthenticated)
        {
            return MembershipMutationResult.Failure("Group not found.");
        }

        if (group.Joinability != GroupJoinability.Open)
        {
            return MembershipMutationResult.Failure(group.Joinability == GroupJoinability.Closed
                ? "This group is not currently accepting new members."
                : "This group is invite-only.");
        }

        if (group.RequiresMessageOnJoinRequest == MessageOnJoinRequest.Required
            && string.IsNullOrWhiteSpace(request.Message))
        {
            return MembershipMutationResult.Failure("A message is required for this group.");
        }

        var existing = await _memberships.GetLiveMembershipAsync(group.Id, _currentUser.UserId, ct).ConfigureAwait(false);
        if (existing is not null)
        {
            return MembershipMutationResult.Failure(existing.Status == GroupMembershipStatus.Active
                ? "You are already a member."
                : "You already have a pending request.");
        }

        var now = DateTimeOffset.UtcNow;
        var membership = new GroupMembership
        {
            Id = Guid.NewGuid(),
            GroupId = group.Id,
            UserId = _currentUser.UserId,
            Status = GroupMembershipStatus.Pending,
            JoinRequestMessage = string.IsNullOrWhiteSpace(request.Message) ? null : request.Message.Trim(),
            RequestedAt = now,
        };
        await _memberships.AddAsync(membership, ct).ConfigureAwait(false);

        await _audit.WriteAsync("Group.JoinRequested", nameof(GroupMembership), membership.Id.ToString(),
            new { group.Id, _currentUser.UserId }, ct).ConfigureAwait(false);

        // SignalR → admins + each leader of the group.
        var leaders = await _memberships.ListLeaderUserIdsAsync(group.Id, ct).ConfigureAwait(false);
        await _notifier.NotifyGroupJoinRequestSubmittedAsync(
            new GroupJoinRequestMessage(group.Id, group.Name, _currentUser.UserId, _currentUser.DisplayName),
            leaders,
            ct).ConfigureAwait(false);

        return MembershipMutationResult.Success(await ToAdminMembership(membership, ct).ConfigureAwait(false));
    }

    public async Task<MembershipMutationResult> LeaveAsync(Guid groupId, CancellationToken ct = default)
    {
        if (!IsAuthenticated) return MembershipMutationResult.Failure("Sign in to manage your memberships.");

        var membership = await _memberships.GetLiveMembershipAsync(groupId, _currentUser.UserId, ct).ConfigureAwait(false);
        if (membership is null) return MembershipMutationResult.Failure("Membership not found.");

        membership.Status = GroupMembershipStatus.Removed;
        membership.IsLeader = false;
        membership.ProcessedByUserId = _currentUser.UserId;
        membership.ProcessedAt = DateTimeOffset.UtcNow;
        await _memberships.UpdateAsync(membership, ct).ConfigureAwait(false);

        await _audit.WriteAsync("Group.MemberLeft", nameof(GroupMembership), membership.Id.ToString(),
            new { groupId, _currentUser.UserId }, ct).ConfigureAwait(false);

        return MembershipMutationResult.Success(await ToAdminMembership(membership, ct).ConfigureAwait(false));
    }

    // ---- profile reads -----------------------------------------------------

    public async Task<List<ProfileMembershipDto>> ListMyMembershipsAsync(CancellationToken ct = default)
    {
        if (!IsAuthenticated) return new List<ProfileMembershipDto>();

        var rows = await _memberships.ListAllForUserAsync(_currentUser.UserId, ct).ConfigureAwait(false);
        var output = new List<ProfileMembershipDto>(rows.Count);
        foreach (var m in rows)
        {
            var group = await _groups.GetAsync(m.GroupId, ct).ConfigureAwait(false);
            if (group is null) continue;
            output.Add(new ProfileMembershipDto(
                group.Id, group.Slug, group.Name, m.IsLeader,
                m.Status, m.JoinedAt, m.RequestedAt));
        }
        return output;
    }

    // ---- helpers -----------------------------------------------------------

    private async Task<bool> IsCurrentUserLeaderOf(Guid groupId, CancellationToken ct)
    {
        if (!IsAuthenticated) return false;
        var membership = await _memberships.GetLiveMembershipAsync(groupId, _currentUser.UserId, ct).ConfigureAwait(false);
        return membership is { Status: GroupMembershipStatus.Active, IsLeader: true };
    }

    private async Task<List<AdminMembershipDto>> HydrateMembershipsAsync(
        IEnumerable<GroupMembership> rows, CancellationToken ct)
    {
        var output = new List<AdminMembershipDto>();
        foreach (var m in rows)
        {
            output.Add(await ToAdminMembership(m, ct).ConfigureAwait(false));
        }
        return output;
    }

    private async Task<AdminMembershipDto> ToAdminMembership(GroupMembership m, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(m.UserId.ToString()).ConfigureAwait(false);
        return new AdminMembershipDto(
            m.Id, m.GroupId, m.UserId,
            user?.DisplayName ?? "(unknown)",
            user?.Email,
            m.Status, m.IsLeader,
            m.JoinRequestMessage,
            m.RequestedAt, m.JoinedAt,
            m.ProcessedAt, m.ProcessedByUserId);
    }

    private static AdminGroupDetailDto ToAdminDetail(Group g) => new(
        g.Id, g.Slug, g.Name,
        g.DescriptionJson, g.ImageBlobUrl, g.ImageWebpBlobUrl, g.ImageAltText,
        g.ContactEmail, g.MeetingInfo,
        g.Visibility, g.Joinability, g.RequiresMessageOnJoinRequest, g.RosterVisibility,
        g.IsActive, g.CreatedAt, g.ModifiedAt);
}
