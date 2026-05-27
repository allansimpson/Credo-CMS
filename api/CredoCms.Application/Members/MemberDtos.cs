namespace CredoCms.Application.Members;

/// <summary>
/// Compact list-page projection. Optional fields are populated only when the
/// member opted in (<c>ShowEmailInDirectory</c> etc.) — the service strips
/// non-opted-in values before this DTO ever leaves the application boundary.
/// </summary>
public sealed record MemberListItemDto(
    Guid Id,
    string FirstName,
    string LastName,
    string DisplayName,
    string? Email,
    string? PhoneNumber,
    string? PhotoBlobUrl,
    string? PhotoWebpBlobUrl,
    string? PhotoAltText);

public sealed record MemberDetailDto(
    Guid Id,
    string FirstName,
    string LastName,
    string DisplayName,
    string? Email,
    string? PhoneNumber,
    string? AddressLine1,
    string? AddressLine2,
    string? City,
    string? StateOrRegion,
    string? PostalCode,
    string? Country,
    string? PhotoBlobUrl,
    string? PhotoWebpBlobUrl,
    string? PhotoAltText,
    string? PublicAuthorBio,
    IReadOnlyList<MemberGroupMembershipDto> GroupMemberships);

/// <summary>
/// Group memberships shown on a member's directory profile. Only Public and
/// MembersOnly groups appear; Hidden groups are filtered at the query layer.
/// </summary>
public sealed record MemberGroupMembershipDto(
    Guid GroupId,
    string GroupSlug,
    string GroupName,
    bool IsLeader);

public sealed record MembersDirectoryQuery(
    string? Search = null,
    int Page = 1,
    int PageSize = 24);
