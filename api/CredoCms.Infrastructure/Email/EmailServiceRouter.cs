using CredoCms.Application.Common;
using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Domain.Email;
using CredoCms.Domain.Settings;
using Microsoft.Extensions.Logging;

namespace CredoCms.Infrastructure.Email;

/// <summary>
/// <see cref="IEmailService"/> dispatch facade. Reads
/// <see cref="SiteSettings.EmailProvider"/> on each call and forwards to
/// the matching concrete impl (LoggingEmailService / SendGridEmailService /
/// SmtpEmailService). Falls back to <see cref="LoggingEmailService"/> when
/// the configured provider is missing the credentials it needs — emits a
/// WARN log so the gap is visible without crashing the calling flow.
///
/// <para>Per-call resolution (rather than wired-once-at-startup) is the
/// project's standard pattern for SiteSettings-driven choices: a config
/// change takes effect on the next request without a restart.</para>
/// </summary>
public sealed class EmailServiceRouter : IEmailService
{
    private readonly LoggingEmailService _logging;
    private readonly SendGridEmailService _sendGrid;
    private readonly SmtpEmailService _smtp;
    private readonly ISiteSettingsRepository _settings;
    private readonly ILogger<EmailServiceRouter> _logger;

    public EmailServiceRouter(
        LoggingEmailService logging,
        SendGridEmailService sendGrid,
        SmtpEmailService smtp,
        ISiteSettingsRepository settings,
        ILogger<EmailServiceRouter> logger)
    {
        _logging = logging;
        _sendGrid = sendGrid;
        _smtp = smtp;
        _settings = settings;
        _logger = logger;
    }

    public async Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default)
    {
        var impl = await ResolveAsync(cancellationToken).ConfigureAwait(false);
        return await impl.IsConfiguredAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SendTransactionalAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        var impl = await ResolveAsync(cancellationToken).ConfigureAwait(false);
        await impl.SendTransactionalAsync(message, cancellationToken).ConfigureAwait(false);
    }

    public async Task<BroadcastSendResult> SendBroadcastAsync(BroadcastEmailMessage message, CancellationToken cancellationToken = default)
    {
        var impl = await ResolveAsync(cancellationToken).ConfigureAwait(false);
        return await impl.SendBroadcastAsync(message, cancellationToken).ConfigureAwait(false);
    }

    private async Task<IEmailService> ResolveAsync(CancellationToken ct)
    {
        var s = await _settings.GetAsync(ct).ConfigureAwait(false);
        return s.EmailProvider switch
        {
            EmailProvider.SendGrid when string.IsNullOrWhiteSpace(s.SendGridApiKey) =>
                FallbackTo(EmailProvider.SendGrid, "SendGridApiKey"),
            EmailProvider.SendGrid => _sendGrid,

            EmailProvider.Smtp when string.IsNullOrWhiteSpace(s.SmtpHost) =>
                FallbackTo(EmailProvider.Smtp, "SmtpHost"),
            EmailProvider.Smtp => _smtp,

            _ => _logging,
        };
    }

    private LoggingEmailService FallbackTo(EmailProvider configuredProvider, string missingField)
    {
        _logger.LogWarning(
            "[EmailServiceRouter] Provider configured as {Provider} but {Missing} is not set — falling back to LoggingEmailService",
            configuredProvider, missingField);
        return _logging;
    }
}
