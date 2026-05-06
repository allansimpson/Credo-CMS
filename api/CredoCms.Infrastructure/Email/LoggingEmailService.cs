using CredoCms.Application.Common;
using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Domain.Email;
using Microsoft.Extensions.Logging;

namespace CredoCms.Infrastructure.Email;

/// <summary>
/// Default <see cref="IEmailService"/> impl: logs each send to Serilog
/// instead of dispatching. Used as the dev-mode fallback and any time the
/// configured provider has missing credentials. Always considered
/// "configured" — logging needs no setup.
///
/// <para>Honors the master <c>SiteSettings.EmailEnabled</c> kill switch:
/// when false, both send methods log and return without doing anything
/// else. This is the production-safe default after initial deployment.</para>
/// </summary>
public sealed class LoggingEmailService : IEmailService
{
    private readonly ILogger<LoggingEmailService> _logger;
    private readonly ISiteSettingsRepository _settings;

    public LoggingEmailService(
        ILogger<LoggingEmailService> logger,
        ISiteSettingsRepository settings)
    {
        _logger = logger;
        _settings = settings;
    }

    public Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default) => Task.FromResult(true);

    public async Task SendTransactionalAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!await EmailEnabledAsync(message.Category, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogInformation(
                "[LoggingEmailService] EmailEnabled=false — skipping transactional send to {To} ({Category})",
                message.ToAddress, message.Category);
            return;
        }

        _logger.LogInformation(
            "[LoggingEmailService] Transactional → {To} <{Address}> · {Category} · subject={Subject}\n--- HTML ---\n{Html}\n--- TEXT ---\n{Text}",
            message.ToName,
            message.ToAddress,
            message.Category,
            message.Subject,
            message.HtmlBody,
            message.PlainTextBody ?? "(none)");
    }

    public async Task SendBroadcastAsync(BroadcastEmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!await EmailEnabledAsync(message.Category, cancellationToken).ConfigureAwait(false))
        {
            _logger.LogInformation(
                "[LoggingEmailService] EmailEnabled=false — skipping broadcast {BroadcastId} ({Recipients} recipients, {Category})",
                message.BroadcastId, message.Recipients.Count, message.Category);
            return;
        }

        _logger.LogInformation(
            "[LoggingEmailService] Broadcast {BroadcastId} → {Recipients} recipients · {Category} · subject={Subject}",
            message.BroadcastId, message.Recipients.Count, message.Category, message.Subject);

        // For dev visibility, log each recipient at Debug. Body logged once
        // at Information (per-recipient body is identical pre-merge).
        foreach (var r in message.Recipients)
        {
            _logger.LogDebug(
                "[LoggingEmailService] Broadcast {BroadcastId} recipient {Name} <{Address}> userId={UserId}",
                message.BroadcastId, r.Name, r.Address, r.UserId);
        }
    }

    /// <summary>Honors the SiteSettings master kill switch. Transactional
    /// mail is gated too — when EmailEnabled is off, the entire system is
    /// in "no outbound mail" mode (typical for initial deployments before
    /// provider config is verified).</summary>
    private async Task<bool> EmailEnabledAsync(EmailCategory _, CancellationToken ct)
    {
        var settings = await _settings.GetAsync(ct).ConfigureAwait(false);
        return settings.EmailEnabled;
    }
}
