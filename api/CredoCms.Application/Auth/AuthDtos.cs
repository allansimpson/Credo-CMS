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
