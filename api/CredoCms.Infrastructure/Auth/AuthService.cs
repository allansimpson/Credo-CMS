using CredoCms.Application.Auth;
using CredoCms.Application.Common;
using CredoCms.Application.UserManagement;
using CredoCms.Domain.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace CredoCms.Infrastructure.Auth;

public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IInvitationEmailComposer _emailComposer;
    private readonly IEmailService _emailService;
    private readonly IAuditLogger _audit;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IInvitationEmailComposer emailComposer,
        IEmailService emailService,
        IAuditLogger audit,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuthService> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailComposer = emailComposer;
        _emailService = emailService;
        _audit = audit;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public async Task<AuthOperationResult<CurrentUserDto>> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);
        if (user is null)
        {
            await _audit.WriteAsync("Auth.LoginFailed", "ApplicationUser",
                details: new { request.Email, Reason = "UnknownEmail" },
                cancellationToken: ct).ConfigureAwait(false);
            return AuthOperationResults.Failure<CurrentUserDto>("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            await _audit.WriteAsync("Auth.LoginFailed", "ApplicationUser", user.Id.ToString(),
                details: new { Reason = "Deactivated" }, cancellationToken: ct).ConfigureAwait(false);
            return AuthOperationResults.Failure<CurrentUserDto>("This account is deactivated. Contact an administrator.");
        }

        var signInResult = await _signInManager.PasswordSignInAsync(
            user, request.Password, isPersistent: true, lockoutOnFailure: true).ConfigureAwait(false);

        if (signInResult.IsLockedOut)
        {
            await _audit.WriteAsync("Auth.LoginFailed", "ApplicationUser", user.Id.ToString(),
                details: new { Reason = "LockedOut" }, cancellationToken: ct).ConfigureAwait(false);
            return AuthOperationResults.Failure<CurrentUserDto>("Your account is temporarily locked. Try again later.");
        }

        if (signInResult.IsNotAllowed)
        {
            await _audit.WriteAsync("Auth.LoginFailed", "ApplicationUser", user.Id.ToString(),
                details: new { Reason = "EmailNotConfirmed" }, cancellationToken: ct).ConfigureAwait(false);
            return AuthOperationResults.Failure<CurrentUserDto>("Please confirm your email before signing in.");
        }

        if (!signInResult.Succeeded)
        {
            await _audit.WriteAsync("Auth.LoginFailed", "ApplicationUser", user.Id.ToString(),
                details: new { Reason = "BadPassword" }, cancellationToken: ct).ConfigureAwait(false);
            return AuthOperationResults.Failure<CurrentUserDto>("Invalid email or password.");
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;
        var update = await _userManager.UpdateAsync(user).ConfigureAwait(false);
        if (!update.Succeeded)
        {
            _logger.LogWarning("Failed to update LastLoginAt for {UserId}", user.Id);
        }

        await _audit.WriteAsync("Auth.LoginSucceeded", "ApplicationUser", user.Id.ToString(),
            cancellationToken: ct).ConfigureAwait(false);

        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        return AuthOperationResults.Success(BuildCurrentUserDto(user, roles));
    }

    public async Task LogoutAsync(CancellationToken ct = default)
    {
        var user = await _userManager.GetUserAsync(_httpContextAccessor.HttpContext?.User!).ConfigureAwait(false);
        await _signInManager.SignOutAsync().ConfigureAwait(false);

        if (user is not null)
        {
            await _audit.WriteAsync("Auth.LoggedOut", "ApplicationUser", user.Id.ToString(),
                cancellationToken: ct).ConfigureAwait(false);
        }
    }

    public async Task<CurrentUserDto?> GetCurrentUserAsync(CancellationToken ct = default)
    {
        var http = _httpContextAccessor.HttpContext;
        if (http?.User?.Identity?.IsAuthenticated != true) return null;

        var user = await _userManager.GetUserAsync(http.User).ConfigureAwait(false);
        if (user is null) return null;

        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        return BuildCurrentUserDto(user, roles);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);
        if (user is null || !user.IsActive)
        {
            // Always pretend success — never reveal whether the email is known.
            await _audit.WriteAsync("Auth.ForgotPasswordRequested", "ApplicationUser",
                details: new { request.Email, Found = false }, cancellationToken: ct).ConfigureAwait(false);
            return;
        }

        var token = await _userManager.GeneratePasswordResetTokenAsync(user).ConfigureAwait(false);
        var msg = await _emailComposer.ComposePasswordResetAsync(user, token, ct).ConfigureAwait(false);
        await _emailService.SendTransactionalAsync(msg, ct).ConfigureAwait(false);

        await _audit.WriteAsync("Auth.ForgotPasswordRequested", "ApplicationUser", user.Id.ToString(),
            details: new { Found = true }, cancellationToken: ct).ConfigureAwait(false);
    }

    public async Task<AuthOperationResult> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);
        if (user is null)
        {
            return AuthOperationResults.Failure("Invalid token or email.");
        }

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return AuthOperationResults.Failure([.. result.Errors.Select(e => e.Description)]);
        }

        // Clear lockout state and rotate stamp so any other open session is invalidated.
        await _userManager.SetLockoutEndDateAsync(user, null).ConfigureAwait(false);
        await _userManager.ResetAccessFailedCountAsync(user).ConfigureAwait(false);
        await _userManager.UpdateSecurityStampAsync(user).ConfigureAwait(false);

        if (user.RequirePasswordChangeOnFirstLogin)
        {
            user.RequirePasswordChangeOnFirstLogin = false;
            await _userManager.UpdateAsync(user).ConfigureAwait(false);
        }

        await _audit.WriteAsync("Auth.PasswordReset", "ApplicationUser", user.Id.ToString(),
            cancellationToken: ct).ConfigureAwait(false);

        return AuthOperationResults.Success();
    }

    public async Task<AuthOperationResult> AcceptInvitationAsync(AcceptInvitationRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _userManager.FindByEmailAsync(request.Email).ConfigureAwait(false);
        if (user is null)
        {
            return AuthOperationResults.Failure("Invalid invitation.");
        }

        if (user.EmailConfirmed)
        {
            return AuthOperationResults.Failure("This invitation has already been accepted. Please sign in.");
        }

        var confirm = await _userManager.ConfirmEmailAsync(user, request.Token).ConfigureAwait(false);
        if (!confirm.Succeeded)
        {
            return AuthOperationResults.Failure([.. confirm.Errors.Select(e => e.Description)]);
        }

        var hasPassword = await _userManager.HasPasswordAsync(user).ConfigureAwait(false);
        var setPasswordResult = hasPassword
            ? await _userManager.ChangePasswordAsync(user, currentPassword: string.Empty, request.NewPassword).ConfigureAwait(false)
            : await _userManager.AddPasswordAsync(user, request.NewPassword).ConfigureAwait(false);

        if (!setPasswordResult.Succeeded)
        {
            return AuthOperationResults.Failure([.. setPasswordResult.Errors.Select(e => e.Description)]);
        }

        user.RequirePasswordChangeOnFirstLogin = false;
        await _userManager.UpdateAsync(user).ConfigureAwait(false);

        await _audit.WriteAsync("Auth.InvitationAccepted", "ApplicationUser", user.Id.ToString(),
            cancellationToken: ct).ConfigureAwait(false);

        return AuthOperationResults.Success();
    }

    public async Task<AuthOperationResult> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var user = await _userManager.FindByIdAsync(userId.ToString()).ConfigureAwait(false);
        if (user is null) return AuthOperationResults.Failure("User not found.");

        var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword).ConfigureAwait(false);
        if (!result.Succeeded)
        {
            return AuthOperationResults.Failure([.. result.Errors.Select(e => e.Description)]);
        }

        if (user.RequirePasswordChangeOnFirstLogin)
        {
            user.RequirePasswordChangeOnFirstLogin = false;
            await _userManager.UpdateAsync(user).ConfigureAwait(false);
        }

        await _audit.WriteAsync("Auth.PasswordChanged", "ApplicationUser", user.Id.ToString(),
            cancellationToken: ct).ConfigureAwait(false);

        return AuthOperationResults.Success();
    }

    private CurrentUserDto BuildCurrentUserDto(ApplicationUser user, IList<string> roles)
    {
        // Compute the cookie expiry from the active auth scheme's options. The cookie
        // sliding expiration is 8h; we expose the ticket's expiry so the SPA can warn
        // the user 5 minutes before the cookie would actually be rejected.
        DateTimeOffset? expiresAt = null;
        var authResult = _httpContextAccessor.HttpContext?.AuthenticateAsync(IdentityConstants.ApplicationScheme).GetAwaiter().GetResult();
        if (authResult?.Properties?.ExpiresUtc is { } exp) expiresAt = exp;

        return new CurrentUserDto(
            user.Id,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            user.DisplayName,
            user.IsActive,
            user.RequirePasswordChangeOnFirstLogin,
            [.. roles.OrderBy(r => r, StringComparer.Ordinal)],
            expiresAt);
    }
}
