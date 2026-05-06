namespace CredoCms.Application.Profile;

/// <summary>
/// The authenticated user's own profile, returned by <c>GET /api/profile</c>.
/// Read-only fields (Email, FirstName, LastName) are included so the SPA can
/// render the profile header without a separate me-call.
/// </summary>
public sealed record ProfileDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string DisplayName,
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
/// Personal-information fields the user can edit themselves. First/last name
/// are intentionally omitted — name changes go through admin to keep the
/// audit log meaningful.
/// </summary>
public sealed record UpdatePersonalInfoRequest(
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
    string? PublicAuthorBio);

/// <summary>
/// Directory opt-in toggles. Master toggle (<see cref="IsListedInDirectory"/>)
/// gates the per-field toggles — the service forces them off when the master
/// is off so the DB can never disagree with the public-facing rule.
/// </summary>
public sealed record UpdateDirectoryRequest(
    bool IsListedInDirectory,
    bool ShowEmailInDirectory,
    bool ShowPhoneInDirectory,
    bool ShowAddressInDirectory,
    bool ShowPhotoInDirectory);

public sealed record UpdateNotificationsRequest(
    bool ReceiveNewsEmails,
    bool ReceiveBlogEmails,
    bool ReceiveBroadcastEmails,
    bool ReceiveGroupEmailsGlobal);

/// <summary>
/// Outcome of a profile mutation. <see cref="Errors"/> is non-empty only when
/// <see cref="Succeeded"/> is false.
/// </summary>
public sealed record ProfileMutationResult(bool Succeeded, IReadOnlyList<string> Errors, ProfileDto? Profile = null)
{
    public static ProfileMutationResult Success(ProfileDto profile) => new(true, Array.Empty<string>(), profile);
    public static ProfileMutationResult Failure(params string[] errors) => new(false, errors, null);
}
