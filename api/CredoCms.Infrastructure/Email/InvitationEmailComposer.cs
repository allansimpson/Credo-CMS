using System.Net;
using System.Web;
using CredoCms.Application.Common;
using CredoCms.Application.UserManagement;
using CredoCms.Domain.Email;
using CredoCms.Domain.Identity;
using CredoCms.Infrastructure.Configuration;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace CredoCms.Infrastructure.Email;

/// <summary>
/// Composes invitation and password-reset email messages. Reads the public site
/// base URL from configuration so the links resolve correctly in production.
/// </summary>
public sealed class InvitationEmailComposer : IInvitationEmailComposer
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly PublicSiteOptions _siteOptions;

    public InvitationEmailComposer(
        UserManager<ApplicationUser> userManager,
        IOptions<PublicSiteOptions> siteOptions)
    {
        _userManager = userManager;
        _siteOptions = siteOptions.Value;
    }

    public Task<EmailMessage> ComposeInvitationAsync(
        ApplicationUser user, string invitationToken, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var url = $"{_siteOptions.BaseUrl.TrimEnd('/')}/accept-invitation"
                  + $"?email={WebUtility.UrlEncode(user.Email!)}"
                  + $"&token={WebUtility.UrlEncode(invitationToken)}";

        var html = $$"""
            <p>Hi {{HttpUtility.HtmlEncode(user.FirstName)}},</p>
            <p>You have been invited to join the Credo CMS site. Click the link below
               to set a password and complete your account.</p>
            <p><a href="{{url}}">Accept your invitation</a></p>
            <p>If you weren't expecting this email, you can safely ignore it.</p>
            """;

        var text = $"Hi {user.FirstName},\n\nYou have been invited to join the Credo CMS site.\n"
                 + $"Open this link to set a password and complete your account:\n{url}\n\n"
                 + "If you weren't expecting this email, you can safely ignore it.";

        var msg = new EmailMessage(
            ToAddress: user.Email!,
            ToName: $"{user.FirstName} {user.LastName}".Trim(),
            Subject: "You're invited to Credo CMS",
            HtmlBody: html,
            PlainTextBody: text,
            UserId: user.Id,
            Category: EmailCategory.Transactional);

        return Task.FromResult(msg);
    }

    public Task<EmailMessage> ComposePasswordResetAsync(
        ApplicationUser user, string resetToken, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        var url = $"{_siteOptions.BaseUrl.TrimEnd('/')}/reset-password"
                  + $"?email={WebUtility.UrlEncode(user.Email!)}"
                  + $"&token={WebUtility.UrlEncode(resetToken)}";

        var html = $$"""
            <p>Hi {{HttpUtility.HtmlEncode(user.FirstName)}},</p>
            <p>A password reset was requested for your Credo CMS account. Click the link
               below to choose a new password.</p>
            <p><a href="{{url}}">Reset your password</a></p>
            <p>If you didn't request this, no action is needed.</p>
            """;

        var text = $"Hi {user.FirstName},\n\nA password reset was requested for your Credo CMS account.\n"
                 + $"Open this link to choose a new password:\n{url}\n\n"
                 + "If you didn't request this, no action is needed.";

        var msg = new EmailMessage(
            ToAddress: user.Email!,
            ToName: $"{user.FirstName} {user.LastName}".Trim(),
            Subject: "Reset your Credo CMS password",
            HtmlBody: html,
            PlainTextBody: text,
            UserId: user.Id,
            Category: EmailCategory.Transactional);

        return Task.FromResult(msg);
    }
}
