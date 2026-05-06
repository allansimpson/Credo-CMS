using CredoCms.Domain.Groups;

namespace CredoCms.Application.Groups;

/// <summary>
/// Internal application-layer projection of the <see cref="Group"/> entity. Kept
/// distinct from outward-facing DTOs because admin/public/profile DTOs each
/// strip different fields based on caller authorization.
/// </summary>
public sealed record GroupRow(
    Guid Id,
    string Slug,
    string Name,
    string? DescriptionJson,
    string? ImageBlobUrl,
    string? ImageWebpBlobUrl,
    string? ImageAltText,
    string? ContactEmail,
    string? MeetingInfo,
    GroupVisibility Visibility,
    GroupJoinability Joinability,
    MessageOnJoinRequest RequiresMessageOnJoinRequest,
    RosterVisibility RosterVisibility,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt);

public sealed record AdminGroupListItemDto(
    Guid Id,
    string Slug,
    string Name,
    GroupVisibility Visibility,
    GroupJoinability Joinability,
    bool IsActive,
    int ActiveMemberCount,
    int PendingRequestCount,
    DateTimeOffset ModifiedAt);

public sealed record AdminGroupDetailDto(
    Guid Id,
    string Slug,
    string Name,
    string? DescriptionJson,
    string? ImageBlobUrl,
    string? ImageWebpBlobUrl,
    string? ImageAltText,
    string? ContactEmail,
    string? MeetingInfo,
    GroupVisibility Visibility,
    GroupJoinability Joinability,
    MessageOnJoinRequest RequiresMessageOnJoinRequest,
    RosterVisibility RosterVisibility,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt);

public sealed record PublicGroupListItemDto(
    Guid Id,
    string Slug,
    string Name,
    string? ImageBlobUrl,
    string? ImageWebpBlobUrl,
    string? ImageAltText,
    string? MeetingInfo,
    GroupVisibility Visibility,
    GroupJoinability Joinability);

public sealed record PublicGroupDetailDto(
    Guid Id,
    string Slug,
    string Name,
    string? DescriptionJson,
    string? ImageBlobUrl,
    string? ImageWebpBlobUrl,
    string? ImageAltText,
    string? ContactEmail,
    string? MeetingInfo,
    GroupVisibility Visibility,
    GroupJoinability Joinability,
    MessageOnJoinRequest RequiresMessageOnJoinRequest,
    /// <summary>Roster, populated only when the caller may see it per
    /// RosterVisibility + membership rules. Null when withheld.</summary>
    IReadOnlyList<GroupRosterEntryDto>? Roster,
    /// <summary>True when the caller has an Active membership in this group.</summary>
    bool ViewerIsMember,
    /// <summary>True when the caller has any Pending request for this group.</summary>
    bool ViewerHasPendingRequest);

public sealed record GroupRosterEntryDto(
    Guid UserId,
    string DisplayName,
    bool IsLeader,
    string? PhotoBlobUrl,
    string? PhotoWebpBlobUrl,
    string? PhotoAltText);

public sealed record AdminMembershipDto(
    Guid Id,
    Guid GroupId,
    Guid UserId,
    string UserDisplayName,
    string? UserEmail,
    GroupMembershipStatus Status,
    bool IsLeader,
    string? JoinRequestMessage,
    DateTimeOffset? RequestedAt,
    DateTimeOffset? JoinedAt,
    DateTimeOffset? ProcessedAt,
    Guid? ProcessedByUserId);

public sealed record ProfileMembershipDto(
    Guid GroupId,
    string GroupSlug,
    string GroupName,
    bool IsLeader,
    GroupMembershipStatus Status,
    DateTimeOffset? JoinedAt,
    DateTimeOffset? RequestedAt);

// ---- request shapes ------------------------------------------------------

public sealed record CreateGroupRequest(
    string Slug,
    string Name,
    string? DescriptionJson,
    string? ImageBlobUrl,
    string? ImageWebpBlobUrl,
    string? ImageAltText,
    string? ContactEmail,
    string? MeetingInfo,
    GroupVisibility Visibility,
    GroupJoinability Joinability,
    MessageOnJoinRequest RequiresMessageOnJoinRequest,
    RosterVisibility RosterVisibility,
    bool IsActive);

public sealed record UpdateGroupRequest(
    string Slug,
    string Name,
    string? DescriptionJson,
    string? ImageBlobUrl,
    string? ImageWebpBlobUrl,
    string? ImageAltText,
    string? ContactEmail,
    string? MeetingInfo,
    GroupVisibility Visibility,
    GroupJoinability Joinability,
    MessageOnJoinRequest RequiresMessageOnJoinRequest,
    RosterVisibility RosterVisibility,
    bool IsActive);

public sealed record JoinRequestRequest(string? Message);

public sealed record AddMemberRequest(Guid UserId, bool IsLeader);

public sealed record GroupMutationResult(
    bool Succeeded,
    IReadOnlyList<string> Errors,
    AdminGroupDetailDto? Group = null)
{
    public static GroupMutationResult Success(AdminGroupDetailDto g) => new(true, Array.Empty<string>(), g);
    public static GroupMutationResult Failure(params string[] errors) => new(false, errors, null);
}

public sealed record MembershipMutationResult(
    bool Succeeded,
    IReadOnlyList<string> Errors,
    AdminMembershipDto? Membership = null)
{
    public static MembershipMutationResult Success(AdminMembershipDto m) => new(true, Array.Empty<string>(), m);
    public static MembershipMutationResult Failure(params string[] errors) => new(false, errors, null);
}
