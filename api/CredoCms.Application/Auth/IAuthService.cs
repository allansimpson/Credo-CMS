namespace CredoCms.Application.Auth;

public interface IAuthService
{
    Task<AuthOperationResult<CurrentUserDto>> LoginAsync(LoginRequest request, CancellationToken ct = default);

    Task LogoutAsync(CancellationToken ct = default);

    Task<CurrentUserDto?> GetCurrentUserAsync(CancellationToken ct = default);

    /// <summary>
    /// Always returns a successful result regardless of whether the email exists,
    /// to prevent account-enumeration attacks.
    /// </summary>
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default);

    Task<AuthOperationResult> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);

    Task<AuthOperationResult> AcceptInvitationAsync(AcceptInvitationRequest request, CancellationToken ct = default);

    Task<AuthOperationResult> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default);
}

/// <summary>
/// Discriminated result for auth operations. Carries either success (optionally with
/// a payload) or a list of error messages suitable for surfacing to the client.
/// </summary>
public sealed record AuthOperationResult(bool Succeeded, IReadOnlyList<string> Errors);

public sealed record AuthOperationResult<T>(bool Succeeded, T? Value, IReadOnlyList<string> Errors);

/// <summary>Factory helpers for <see cref="AuthOperationResult"/> family.</summary>
public static class AuthOperationResults
{
    public static AuthOperationResult Success() => new(true, Array.Empty<string>());
    public static AuthOperationResult Failure(params string[] errors) => new(false, errors);
    public static AuthOperationResult<T> Success<T>(T value) => new(true, value, Array.Empty<string>());
    public static AuthOperationResult<T> Failure<T>(params string[] errors) => new(false, default, errors);
}
