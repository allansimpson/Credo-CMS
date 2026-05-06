using CredoCms.Application.Common;
using CredoCms.Application.Email;
using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Domain.Email;
using CredoCms.Domain.Settings;
using Microsoft.Extensions.Logging;

namespace CredoCms.Infrastructure.Email;

/// <summary>
/// Builds a transient <see cref="IEmailService"/> from the in-flight
/// candidate config (rather than from the persisted SiteSettings) so the
/// admin can validate creds before saving them. Reuses the production
/// send pipeline (<see cref="SendGridEmailService"/> /
/// <see cref="SmtpEmailService"/>) by wrapping the candidate config in an
/// in-memory <see cref="ISiteSettingsRepository"/>.
/// </summary>
public sealed class TestEmailService : ITestEmailService
{
    private readonly ISendGridClientFactory _sendGridFactory;
    private readonly IMailKitSmtpClientFactory _smtpFactory;
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<TestEmailService> _logger;

    public TestEmailService(
        ISendGridClientFactory sendGridFactory,
        IMailKitSmtpClientFactory smtpFactory,
        ILoggerFactory loggerFactory)
    {
        _sendGridFactory = sendGridFactory;
        _smtpFactory = smtpFactory;
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<TestEmailService>();
    }

    public async Task<TestEmailResult> SendAsync(
        TestEmailConfig config,
        string toAddress,
        string toName,
        CancellationToken ct = default)
    {
        if (config.Provider == EmailProvider.None)
        {
            return new TestEmailResult(
                Success: true,
                ErrorMessage: null,
                Note: "EmailProvider=None — no message was dispatched. Pick SendGrid or SMTP to test live delivery.");
        }

        // Build a SiteSettings shaped from the request, force EmailEnabled=true
        // (the test send must dispatch even when the admin hasn't yet flipped
        // the master switch — that's the whole point of testing).
        var transientSettings = new SiteSettings
        {
            EmailProvider = config.Provider,
            EmailFromAddress = config.EmailFromAddress,
            EmailFromName = config.EmailFromName,
            EmailReplyToAddress = config.EmailReplyToAddress,
            SendGridApiKey = config.SendGridApiKey,
            SmtpHost = config.SmtpHost,
            SmtpPort = config.SmtpPort,
            SmtpUsername = config.SmtpUsername,
            SmtpPassword = config.SmtpPassword,
            SmtpUseSsl = config.SmtpUseSsl,
            TestEmailRecipient = config.TestEmailRecipient,
            EmailEnabled = true,
        };

        IEmailService impl = config.Provider switch
        {
            EmailProvider.SendGrid => new SendGridEmailService(
                _sendGridFactory,
                new InMemorySiteSettingsRepository(transientSettings),
                _loggerFactory.CreateLogger<SendGridEmailService>()),
            EmailProvider.Smtp => new SmtpEmailService(
                _smtpFactory,
                new InMemorySiteSettingsRepository(transientSettings),
                _loggerFactory.CreateLogger<SmtpEmailService>()),
            _ => throw new InvalidOperationException($"Unsupported provider: {config.Provider}"),
        };

        var msg = new EmailMessage(
            ToAddress: toAddress,
            ToName: toName,
            Subject: "Credo CMS — email configuration test",
            HtmlBody: "<p>Your email provider is configured correctly. This message was sent via the admin Test Send button.</p>",
            PlainTextBody: "Your email provider is configured correctly. This message was sent via the admin Test Send button.",
            UserId: null,
            Category: EmailCategory.Transactional);

        try
        {
            await impl.SendTransactionalAsync(msg, ct).ConfigureAwait(false);
            return new TestEmailResult(true, null, $"Message dispatched via {config.Provider}.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[TestEmailService] {Provider} test send failed", config.Provider);
            return new TestEmailResult(false, ex.Message, null);
        }
    }

    /// <summary>One-shot read-only repo over a static SiteSettings.
    /// <see cref="UpdateAsync"/> throws — test sends never persist.</summary>
    private sealed class InMemorySiteSettingsRepository : ISiteSettingsRepository
    {
        private readonly SiteSettings _value;
        public InMemorySiteSettingsRepository(SiteSettings value) => _value = value;
        public Task<SiteSettings> GetAsync(CancellationToken cancellationToken = default) => Task.FromResult(_value);
        public Task UpdateAsync(SiteSettings settings, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("Test-send pseudo-repo is read-only.");
    }
}
