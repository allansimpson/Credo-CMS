using CredoCms.Application.Common;
using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Domain.Email;
using CredoCms.Domain.Settings;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace CredoCms.Infrastructure.Email;

/// <summary>
/// Generic-SMTP <see cref="IEmailService"/> via MailKit. Connects per send
/// (no long-lived connection); reads host/port/credentials/SSL from
/// <c>SiteSettings</c>. Sends one MimeMessage per recipient — vanilla SMTP
/// has no batch API, so a 500-recipient broadcast fires 500 SMTP
/// transactions. Acceptable at church scale.
///
/// <para>Honors <see cref="SiteSettings.EmailEnabled"/>. Sets Reply-To
/// when configured. Emits <see cref="BroadcastEmailMessage.AdditionalHeaders"/>
/// verbatim — that's the seam R7/R12 use to inject <c>List-Unsubscribe</c>
/// + <c>List-Unsubscribe-Post</c>.</para>
/// </summary>
public sealed class SmtpEmailService : IEmailService
{
    private readonly IMailKitSmtpClientFactory _factory;
    private readonly ISiteSettingsRepository _settings;
    private readonly ILogger<SmtpEmailService> _logger;

    public SmtpEmailService(
        IMailKitSmtpClientFactory factory,
        ISiteSettingsRepository settings,
        ILogger<SmtpEmailService> logger)
    {
        _factory = factory;
        _settings = settings;
        _logger = logger;
    }

    public async Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default)
    {
        var s = await _settings.GetAsync(cancellationToken).ConfigureAwait(false);
        return s.EmailEnabled
            && s.EmailProvider == EmailProvider.Smtp
            && !string.IsNullOrWhiteSpace(s.SmtpHost)
            && !string.IsNullOrWhiteSpace(s.EmailFromAddress);
    }

    public async Task SendTransactionalAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var settings = await _settings.GetAsync(cancellationToken).ConfigureAwait(false);
        if (!ShouldDispatch(settings, message.Category)) return;

        var mime = BuildMimeMessage(settings, message.Subject, message.HtmlBody, message.PlainTextBody,
            ResolveTo(settings, message.ToAddress, message.ToName), additionalHeaders: null);

        try
        {
            await SendOneAsync(settings, mime, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[SMTP] Transactional send failed for {To}", message.ToAddress);
            throw;
        }
    }

    public async Task<BroadcastSendResult> SendBroadcastAsync(BroadcastEmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var settings = await _settings.GetAsync(cancellationToken).ConfigureAwait(false);
        if (!ShouldDispatch(settings, message.Category))
        {
            return new BroadcastSendResult(message.Recipients
                .Select(r => new RecipientSendResult(r.UserId, r.Address, true, null, null))
                .ToList());
        }

        // SMTP has no batch API; one transaction per recipient. Single
        // long-lived client across the loop minimizes reconnect overhead
        // (typical SMTP servers permit hundreds of messages per session).
        var results = new List<RecipientSendResult>(message.Recipients.Count);
        using var client = _factory.Create();
        try
        {
            await ConnectAndAuthAsync(client, settings, cancellationToken).ConfigureAwait(false);

            foreach (var r in message.Recipients)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var rendered = ApplySubstitutions(message.HtmlBody, r.MergeFields) ?? string.Empty;
                var renderedText = ApplySubstitutions(message.PlainTextBody, r.MergeFields);
                var mime = BuildMimeMessage(settings, message.Subject, rendered, renderedText,
                    ResolveTo(settings, r.Address, r.Name), message.AdditionalHeaders);

                try
                {
                    var smtpId = await client.SendAsync(mime, cancellationToken).ConfigureAwait(false);
                    results.Add(new RecipientSendResult(r.UserId, r.Address, true, smtpId, null));
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "[SMTP] Broadcast {BroadcastId} send to {Address} failed",
                        message.BroadcastId, r.Address);
                    results.Add(new RecipientSendResult(r.UserId, r.Address, false, null, ex.Message));
                }
            }
        }
        catch (Exception ex)
        {
            // Connect/auth failure aborts every still-pending recipient;
            // anything sent before the throw retains its individual outcome.
            _logger.LogError(ex, "[SMTP] Broadcast {BroadcastId} aborted at session level", message.BroadcastId);
            for (var i = results.Count; i < message.Recipients.Count; i++)
            {
                var r = message.Recipients[i];
                results.Add(new RecipientSendResult(r.UserId, r.Address, false, null, ex.Message));
            }
        }
        finally
        {
            // CancellationToken.None: a cancelled disconnect leaks the socket and
            // we're already past every send the caller cares about.
            try { await client.DisconnectAsync(true, CancellationToken.None).ConfigureAwait(false); }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "[SMTP] Disconnect after broadcast {BroadcastId} failed", message.BroadcastId);
            }
        }

        return new BroadcastSendResult(results);
    }

    // -- Helpers ------------------------------------------------------------

    private bool ShouldDispatch(SiteSettings settings, EmailCategory category)
    {
        if (!settings.EmailEnabled)
        {
            _logger.LogInformation("[SMTP] EmailEnabled=false — skipping ({Category})", category);
            return false;
        }
        if (settings.EmailProvider != EmailProvider.Smtp)
        {
            _logger.LogWarning("[SMTP] Provider is {Provider}, not SMTP — skipping", settings.EmailProvider);
            return false;
        }
        if (string.IsNullOrWhiteSpace(settings.SmtpHost))
        {
            _logger.LogWarning("[SMTP] SmtpHost not configured — skipping");
            return false;
        }
        return true;
    }

    private async Task SendOneAsync(SiteSettings settings, MimeMessage mime, CancellationToken ct)
    {
        using var client = _factory.Create();
        await ConnectAndAuthAsync(client, settings, ct).ConfigureAwait(false);
        try
        {
            await client.SendAsync(mime, ct).ConfigureAwait(false);
        }
        finally
        {
            try { await client.DisconnectAsync(true, CancellationToken.None).ConfigureAwait(false); }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "[SMTP] Disconnect after transactional send failed");
            }
        }
    }

    private static async Task ConnectAndAuthAsync(IMailKitSmtpClient client, SiteSettings s, CancellationToken ct)
    {
        var options = ResolveSecureSocketOptions(s);
        await client.ConnectAsync(s.SmtpHost!, s.SmtpPort, options, ct).ConfigureAwait(false);
        if (!string.IsNullOrWhiteSpace(s.SmtpUsername))
        {
            await client.AuthenticateAsync(s.SmtpUsername, s.SmtpPassword ?? string.Empty, ct).ConfigureAwait(false);
        }
    }

    /// <summary>Most operators run port 587 with STARTTLS (the modern
    /// standard); port 465 implies SslOnConnect (legacy SMTPS); the SSL
    /// toggle controls whether to enforce TLS at all.</summary>
    private static SecureSocketOptions ResolveSecureSocketOptions(SiteSettings s)
    {
        if (!s.SmtpUseSsl) return SecureSocketOptions.None;
        return s.SmtpPort switch
        {
            465 => SecureSocketOptions.SslOnConnect,
            587 => SecureSocketOptions.StartTls,
            _ => SecureSocketOptions.Auto,
        };
    }

    private static MimeMessage BuildMimeMessage(
        SiteSettings settings,
        string subject,
        string htmlBody,
        string? plainTextBody,
        MailboxAddress to,
        IReadOnlyDictionary<string, string>? additionalHeaders)
    {
        var msg = new MimeMessage();
        msg.From.Add(new MailboxAddress(settings.EmailFromName, settings.EmailFromAddress));
        msg.To.Add(to);
        if (!string.IsNullOrWhiteSpace(settings.EmailReplyToAddress))
            msg.ReplyTo.Add(new MailboxAddress(string.Empty, settings.EmailReplyToAddress));
        msg.Subject = subject;

        var builder = new BodyBuilder { HtmlBody = htmlBody };
        if (!string.IsNullOrWhiteSpace(plainTextBody)) builder.TextBody = plainTextBody;
        msg.Body = builder.ToMessageBody();

        if (additionalHeaders is { Count: > 0 })
        {
            foreach (var kv in additionalHeaders)
            {
                // Replace any existing header of the same name to avoid
                // duplicates if we re-add (e.g., during retry).
                msg.Headers.Remove(kv.Key);
                msg.Headers.Add(kv.Key, kv.Value);
            }
        }

        return msg;
    }

    private static MailboxAddress ResolveTo(SiteSettings settings, string address, string name)
    {
        if (!string.IsNullOrWhiteSpace(settings.TestEmailRecipient))
            return new MailboxAddress("[Staging Override] " + name, settings.TestEmailRecipient);
        return new MailboxAddress(name, address);
    }

    /// <summary>Substitutes <c>{{key}}</c> placeholders with the recipient's
    /// merge-field values. SMTP can't lean on a server-side substitution
    /// engine like SendGrid does — we render per recipient.</summary>
    private static string? ApplySubstitutions(string? body, IReadOnlyDictionary<string, string>? mergeFields)
    {
        if (body is null || mergeFields is null || mergeFields.Count == 0) return body;
        var result = body;
        foreach (var kv in mergeFields)
        {
            result = result.Replace("{{" + kv.Key + "}}", kv.Value, StringComparison.Ordinal);
        }
        return result;
    }
}
