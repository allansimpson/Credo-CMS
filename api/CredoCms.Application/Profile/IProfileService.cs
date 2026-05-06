namespace CredoCms.Application.Profile;

/// <summary>
/// Service-layer guard rail for self-profile mutations. Every method takes the
/// caller-supplied <paramref name="callerUserId"/> explicitly rather than
/// reading from <c>HttpContext.User</c>; the controller resolves the caller
/// from claims and passes it in. This keeps the service unit-testable and
/// makes the per-user authorization check impossible to forget.
/// </summary>
public interface IProfileService
{
    /// <summary>Returns the current user's profile, or null if the account
    /// has been hard-deleted out from under the session.</summary>
    Task<ProfileDto?> GetProfileAsync(Guid callerUserId, CancellationToken ct = default);

    Task<ProfileMutationResult> UpdatePersonalInfoAsync(
        Guid callerUserId,
        UpdatePersonalInfoRequest request,
        CancellationToken ct = default);

    Task<ProfileMutationResult> UpdateDirectoryAsync(
        Guid callerUserId,
        UpdateDirectoryRequest request,
        CancellationToken ct = default);

    Task<ProfileMutationResult> UpdateNotificationsAsync(
        Guid callerUserId,
        UpdateNotificationsRequest request,
        CancellationToken ct = default);
}
