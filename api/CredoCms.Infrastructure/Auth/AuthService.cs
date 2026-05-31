using CredoCms.Application.Auth;
using CredoCms.Application.Common;
using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Application.UserManagement;
using CredoCms.Domain.Identity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
    private readonly ISiteSettingsRepository _siteSettings;
    private readonly IOptions<DataProtectionTokenProviderOptions> _dataProtectionTokenOptions;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IInvitationEmailComposer emailComposer,
        IEmailService emailService,
        IAuditLogger audit,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuthService> logger,
        ISiteSettingsRepository siteSettings,
        IOptions<DataProtectionTokenProviderOptions> dataProtectionTokenOptions)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _emailComposer = emailComposer;
        _emailService = emailService;
        _audit = audit;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
        _siteSettings = siteSettings;
        _dataProtectionTokenOptions = dataProtectionTokenOptions;
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

        // Fire the Welcome email as a best-effort send — a transient SMTP failure
        // here would be confusing UX (user just set their password successfully).
        try
        {
            var welcome = await _emailComposer.ComposeWelcomeAsync(user, ct).ConfigureAwait(false);
            await _emailService.SendTransactionalAsync(welcome, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send Welcome email to {UserId} after invitation acceptance.", user.Id);
        }

        // Auto-sign-in. The user just proved they own the email by setting a
        // password — making them re-enter creds adds friction with no security
        // benefit. Issues the same cookie as the regular login flow.
        await _signInManager.SignInAsync(user, isPersistent: true).ConfigureAwait(false);

        await _audit.WriteAsync("Auth.InvitationAccepted", "ApplicationUser", user.Id.ToString(),
            cancellationToken: ct).ConfigureAwait(false);

        return AuthOperationResults.Success();
    }

    public async Task<InvitationPreviewResult> GetInvitationPreviewAsync(
        string email, string token, CancellationToken ct = default)
    {
        // Defensive: any malformed input → Invalid (never leak whether the
        // email is a real account by tailoring the response shape).
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
        {
            return EmptyPreview(InvitationPreviewStatus.Invalid);
        }

        ApplicationUser? user;
        try
        {
            user = await _userManager.FindByEmailAsync(email).ConfigureAwait(false);
        }
        catch
        {
            return EmptyPreview(InvitationPreviewStatus.Invalid);
        }

        if (user is null || !user.IsActive)
        {
            return EmptyPreview(InvitationPreviewStatus.Invalid);
        }

        if (user.EmailConfirmed)
        {
            // Account exists and has already been activated. Returning the
            // preview fields here is fine — the email-owner is the one who
            // clicked the link, and the SPA needs the credential face for
            // the "already accepted, sign in" state.
            return await BuildPreviewAsync(user, InvitationPreviewStatus.Consumed, ct).ConfigureAwait(false);
        }

        // Validate the token WITHOUT consuming it. Matches the call shape
        // ConfirmEmailAsync uses internally, just without the "mark confirmed"
        // side-effect.
        var tokenOk = await _userManager.VerifyUserTokenAsync(
            user,
            _userManager.Options.Tokens.EmailConfirmationTokenProvider,
            UserManager<ApplicationUser>.ConfirmEmailTokenPurpose,
            token).ConfigureAwait(false);

        if (!tokenOk)
        {
            // Treat both "expired" and "malformed/wrong" as Expired here —
            // the SPA shows the same "request a new invitation" CTA either
            // way, and we don't want to differentiate timing for attackers.
            return EmptyPreview(InvitationPreviewStatus.Expired);
        }

        return await BuildPreviewAsync(user, InvitationPreviewStatus.Valid, ct).ConfigureAwait(false);
    }

    private async Task<InvitationPreviewResult> BuildPreviewAsync(
        ApplicationUser user, InvitationPreviewStatus status, CancellationToken ct)
    {
        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        var primaryRole = roles.FirstOrDefault() ?? "Member";

        var settings = await _siteSettings.GetAsync(ct).ConfigureAwait(false);
        var churchName = settings.ChurchName;
        var churchInitials = ChurchInitials(churchName);
        var credentialNumber = $"{churchInitials}-{user.Id.ToString("N").ToUpperInvariant()[^6..]}";

        // TODO: when InvitationSentAt is added to ApplicationUser (to support
        // resend timestamps accurately), swap user.CreatedAt for it here.
        // For now, approximate against creation time using the configured
        // token lifespan (default 1 day). Floor the result so a stale row
        // never reports an already-expired countdown for an otherwise-valid
        // token — the backend is the source of truth on expiry.
        var lifespan = _dataProtectionTokenOptions.Value.TokenLifespan;
        var rawExpiry = user.CreatedAt + lifespan;
        var nowFloor = DateTimeOffset.UtcNow.AddMinutes(5);
        var expiresAt = rawExpiry > nowFloor ? rawExpiry : nowFloor;

        return new InvitationPreviewResult(
            Status: status,
            FirstName: user.FirstName,
            LastName: user.LastName,
            Email: user.Email,
            Role: primaryRole,
            InvitedBy: "An administrator", // until UserAdminService threads invoker through
            ChurchName: churchName,
            ChurchInitials: churchInitials,
            CredentialNumber: credentialNumber,
            ExpiresAt: status == InvitationPreviewStatus.Valid ? expiresAt : null);
    }

    private static InvitationPreviewResult EmptyPreview(InvitationPreviewStatus status) =>
        new(status, null, null, null, null, null, null, null, null, null);

    /// <summary>Initials of each word in the church name, uppercased, max 4 chars.
    /// Fallback "CMS" when empty so the credential never reads "{empty}-…".</summary>
    private static string ChurchInitials(string? churchName)
    {
        if (string.IsNullOrWhiteSpace(churchName)) return "CMS";
        var initials = string.Concat(
            churchName.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(w => char.ToUpperInvariant(w[0])));
        if (initials.Length == 0) return "CMS";
        return initials.Length > 4 ? initials[..4] : initials;
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
