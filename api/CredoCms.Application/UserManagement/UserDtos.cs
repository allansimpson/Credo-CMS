using CredoCms.Application.Common;

namespace CredoCms.Application.UserManagement;

public sealed record UserListItemDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string DisplayName,
    bool IsActive,
    bool EmailConfirmed,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    IReadOnlyList<string> Roles);

public sealed record UserDetailDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string DisplayName,
    bool IsActive,
    bool EmailConfirmed,
    bool LockoutEnabled,
    DateTimeOffset? LockoutEndUtc,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastLoginAt,
    IReadOnlyList<string> Roles);

public sealed record CreateUserRequest(
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyList<string> Roles,
    bool SendInvitation);

public sealed record UpdateUserRequest(
    string Email,
    string FirstName,
    string LastName,
    IReadOnlyList<string> Roles,
    bool IsActive);

public sealed record UserListQuery(
    string? Search = null,
    string? Role = null,
    bool? IsActive = null,
    int Page = 1,
    int PageSize = 50);

public sealed record UserMutationResult(bool Succeeded, IReadOnlyList<string> Errors, UserDetailDto? User = null)
{
    public static UserMutationResult Success(UserDetailDto user) => new(true, Array.Empty<string>(), user);
    public static UserMutationResult Failure(params string[] errors) => new(false, errors, null);
}

public sealed record HardDeleteUserRequest(string ConfirmDisplayName);

/// <summary>
/// Patch shape for the admin user-edit screen. Mirrors the four sections of
/// the member-self profile API (personal / directory / notifications) plus
/// any field the user themselves cannot edit (e.g. names — which still flow
/// through the admin edit endpoint, not this profile-fields endpoint).
/// </summary>
public sealed record UpdateUserProfileFieldsRequest(
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
    bool IsListedInDirectory,
    bool ShowEmailInDirectory,
    bool ShowPhoneInDirectory,
    bool ShowAddressInDirectory,
    bool ShowPhotoInDirectory,
    bool ReceiveNewsEmails,
    bool ReceiveBlogEmails,
    bool ReceiveBroadcastEmails,
    bool ReceiveGroupEmailsGlobal);

/// <summary>
/// Aggregate read-side payload that powers the admin user-detail screen.
/// Counts come from a single DB round-trip via <c>IUserAdminQueries</c>.
/// </summary>
public sealed record AdminUserNotesDto(
    Guid UserId,
    int GroupMembershipCount,
    int ActiveGroupMembershipCount,
    int PrayerRequestCount,
    int EventRegistrationCount);

