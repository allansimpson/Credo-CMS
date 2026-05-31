namespace CredoCms.Application.Auth;

public sealed record LoginRequest(string Email, string Password);

public sealed record ForgotPasswordRequest(string Email);

public sealed record ResetPasswordRequest(string Email, string Token, string NewPassword);

public sealed record AcceptInvitationRequest(string Email, string Token, string NewPassword);

public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);

/// <summary>
/// Returned by <c>GET /api/auth/me</c> and on a successful login. Includes
/// <see cref="ExpiresAtUtc"/> so the SPA can schedule its session-expiry warning
/// without an extra round-trip.
/// </summary>
public sealed record CurrentUserDto(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    string DisplayName,
    bool IsActive,
    bool RequirePasswordChangeOnFirstLogin,
    IReadOnlyList<string> Roles,
    DateTimeOffset? ExpiresAtUtc);

public sealed record LoginResultDto(CurrentUserDto User);

/// <summary>
/// Tagged status of an invitation token, returned by
/// <c>GET /api/auth/invitation-preview</c> so the SPA can branch on
/// edge-states before rendering the set-password form.
/// </summary>
public enum InvitationPreviewStatus
{
    /// <summary>Token is good. Show the set-password form.</summary>
    Valid = 0,
    /// <summary>Token has lapsed. Show "request a new invitation" copy.</summary>
    Expired = 1,
    /// <summary>The account has already been activated. Direct user to Sign In.</summary>
    Consumed = 2,
    /// <summary>Email unknown, token malformed, or any other failure. Don't leak which.</summary>
    Invalid = 3,
}

/// <summary>
/// Read-only preview of an invitation. Validates the token WITHOUT consuming it
/// and returns enough context for the credential face + countdown. The fields
/// past <see cref="Status"/> are populated only when status is
/// <see cref="InvitationPreviewStatus.Valid"/> or <see cref="InvitationPreviewStatus.Consumed"/>;
/// for <see cref="InvitationPreviewStatus.Expired"/> and
/// <see cref="InvitationPreviewStatus.Invalid"/> they remain null/empty so we
/// don't leak whether the email is a real account.
/// </summary>
public sealed record InvitationPreviewResult(
    InvitationPreviewStatus Status,
    string? FirstName,
    string? LastName,
    string? Email,
    string? Role,
    string? InvitedBy,
    string? ChurchName,
    string? ChurchInitials,
    string? CredentialNumber,
    DateTimeOffset? ExpiresAt);

