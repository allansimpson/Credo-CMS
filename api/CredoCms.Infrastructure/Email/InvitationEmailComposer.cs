using System.Globalization;
using System.Net;
using CredoCms.Application.Common;
using CredoCms.Application.Email;
using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Application.UserManagement;
using CredoCms.Domain.Email;
using CredoCms.Domain.Identity;
using CredoCms.Infrastructure.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CredoCms.Infrastructure.Email;

/// <summary>
/// Composes invitation, password-reset and welcome email messages by
/// running the corresponding DB-backed email template (Subject + HtmlBody)
/// through <see cref="IEmailTemplateRenderer"/>. Building the absolute
/// links and the per-template context dictionary is the only thing this
/// class does — the rendered HTML lives in the EmailTemplate table.
/// </summary>
public sealed class InvitationEmailComposer : IInvitationEmailComposer
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly PublicSiteOptions _siteOptions;
    private readonly IEmailTemplateRenderer _renderer;
    private readonly ISiteSettingsRepository _siteSettings;

    public InvitationEmailComposer(
        UserManager<ApplicationUser> userManager,
        IOptions<PublicSiteOptions> siteOptions,
        IEmailTemplateRenderer renderer,
        ISiteSettingsRepository siteSettings)
    {
        _userManager = userManager;
        _siteOptions = siteOptions.Value;
        _renderer = renderer;
        _siteSettings = siteSettings;
    }

    public async Task<EmailMessage> ComposeInvitationAsync(
        ApplicationUser user, string invitationToken, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var acceptUrl = $"{_siteOptions.BaseUrl.TrimEnd('/')}/accept-invitation"
                        + $"?email={WebUtility.UrlEncode(user.Email!)}"
                        + $"&token={WebUtility.UrlEncode(invitationToken)}";

        var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
        var primaryRole = roles.FirstOrDefault() ?? "Member";

        // invited_by / expiry_days don't live on the user record today —
        // expose them as the composer's responsibility so callers can tune
        // them later without touching the renderer. Reasonable defaults
        // until UserAdminService threads richer context through.
        var context = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["first_name"] = user.FirstName ?? string.Empty,
            ["account_email"] = user.Email ?? string.Empty,
            ["role"] = primaryRole,
            ["invited_by"] = "An administrator",
            ["accept_url"] = acceptUrl,
            ["expiry_days"] = "1 day",
        };

        var rendered = await _renderer.RenderAsync("InvitationEmail", context, ct).ConfigureAwait(false);

        return new EmailMessage(
            ToAddress: user.Email!,
            ToName: $"{user.FirstName} {user.LastName}".Trim(),
            Subject: rendered.Subject,
            HtmlBody: rendered.HtmlBody,
            PlainTextBody: rendered.PlainTextBody,
            UserId: user.Id,
            Category: EmailCategory.Transactional);
    }

    public async Task<EmailMessage> ComposePasswordResetAsync(
        ApplicationUser user, string resetToken, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var resetUrl = $"{_siteOptions.BaseUrl.TrimEnd('/')}/reset-password"
                       + $"?email={WebUtility.UrlEncode(user.Email!)}"
                       + $"&token={WebUtility.UrlEncode(resetToken)}";

        var settings = await _siteSettings.GetAsync(ct).ConfigureAwait(false);
        var supportEmail = string.IsNullOrWhiteSpace(settings.ContactEmail)
            ? user.Email!
            : settings.ContactEmail!;

        // Request device/location require an HttpContext + GeoIP service that
        // aren't wired here yet. Pass blanks so the strict renderer doesn't throw;
        // the template degrades gracefully when these are empty.
        var context = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["first_name"] = user.FirstName ?? string.Empty,
            ["reset_url"] = resetUrl,
            ["expiry_minutes"] = "60",
            ["request_time"] = DateTime.UtcNow.ToString("MMM d, yyyy 'at' h:mm tt 'UTC'", CultureInfo.InvariantCulture),
            ["request_device"] = string.Empty,
            ["request_location"] = string.Empty,
            ["support_email"] = supportEmail,
        };

        var rendered = await _renderer.RenderAsync("PasswordReset", context, ct).ConfigureAwait(false);

        return new EmailMessage(
            ToAddress: user.Email!,
            ToName: $"{user.FirstName} {user.LastName}".Trim(),
            Subject: rendered.Subject,
            HtmlBody: rendered.HtmlBody,
            PlainTextBody: rendered.PlainTextBody,
            UserId: user.Id,
            Category: EmailCategory.Transactional);
    }

    public async Task<EmailMessage> ComposeWelcomeAsync(
        ApplicationUser user, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var portalUrl = $"{_siteOptions.BaseUrl.TrimEnd('/')}/members";

        var context = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["first_name"] = user.FirstName ?? string.Empty,
            ["portal_url"] = portalUrl,
        };

        var rendered = await _renderer.RenderAsync("AccountActivated", context, ct).ConfigureAwait(false);

        return new EmailMessage(
            ToAddress: user.Email!,
            ToName: $"{user.FirstName} {user.LastName}".Trim(),
            Subject: rendered.Subject,
            HtmlBody: rendered.HtmlBody,
            PlainTextBody: rendered.PlainTextBody,
            UserId: user.Id,
            Category: EmailCategory.Transactional);
    }
}
