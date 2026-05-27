using CredoCms.Application.Common;
using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Domain.Email;
using CredoCms.Domain.Settings;
using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace CredoCms.Infrastructure.Email;

/// <summary>
/// SendGrid-backed <see cref="IEmailService"/>. Single-personalization for
/// transactional sends; per-batch personalizations (up to
/// <see cref="BroadcastChunkSize"/>) for broadcasts so per-recipient merge
/// fields and the X-Message-Id can be tracked.
///
/// <para>The X-Message-Id header returned on the HTTP response is shared
/// across the whole batch. SendGrid emits per-recipient
/// <c>sg_message_id</c> values via webhook events, formatted as
/// <c>&lt;batch-id&gt;.&lt;suffix&gt;</c>; the webhook handler matches
/// against the stored prefix.</para>
///
/// <para>Honors the <see cref="SiteSettings.EmailEnabled"/> kill switch
/// the same way <c>LoggingEmailService</c> does. When the API key is
/// missing, returns false from <see cref="IsConfiguredAsync"/> so the
/// startup health check + admin UI surface a meaningful error.</para>
/// </summary>
public sealed class SendGridEmailService : IEmailService
{
    /// <summary>SendGrid permits up to 1,000 personalizations per request.
    /// We chunk at 500 to leave headroom and to keep retries cheap.</summary>
    public const int BroadcastChunkSize = 500;

    private readonly ISendGridClientFactory _factory;
    private readonly ISiteSettingsRepository _settings;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(
        ISendGridClientFactory factory,
        ISiteSettingsRepository settings,
        ILogger<SendGridEmailService> logger)
    {
        _factory = factory;
        _settings = settings;
        _logger = logger;
    }

    public async Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default)
    {
        var s = await _settings.GetAsync(cancellationToken).ConfigureAwait(false);
        return s.EmailEnabled
            && s.EmailProvider == EmailProvider.SendGrid
            && !string.IsNullOrWhiteSpace(s.SendGridApiKey)
            && !string.IsNullOrWhiteSpace(s.EmailFromAddress);
    }

    public async Task SendTransactionalAsync(EmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var settings = await _settings.GetAsync(cancellationToken).ConfigureAwait(false);
        if (!ShouldDispatch(settings, message.Category, out var apiKey)) return;

        var client = _factory.Create(apiKey);
        var sg = BuildSingleRecipientMessage(settings, message);
        var response = await SendWithRetryAsync(client, sg, cancellationToken).ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            var body = await SafeReadBodyAsync(response).ConfigureAwait(false);
            _logger.LogWarning(
                "[SendGrid] Transactional send failed for {To} status={Status} body={Body}",
                message.ToAddress, (int)response.StatusCode, body);
            // Transactional failures bubble up so the caller can decide
            // (e.g., retry password-reset link generation).
            throw new InvalidOperationException(
                $"SendGrid rejected transactional send (status {(int)response.StatusCode}).");
        }
    }

    public async Task<BroadcastSendResult> SendBroadcastAsync(BroadcastEmailMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        var settings = await _settings.GetAsync(cancellationToken).ConfigureAwait(false);
        if (!ShouldDispatch(settings, message.Category, out var apiKey))
        {
            // Master kill switch / unconfigured — report success-without-id
            // so the caller can decide how to mark the recipient rows.
            return new BroadcastSendResult(message.Recipients
                .Select(r => new RecipientSendResult(r.UserId, r.Address, true, null, null))
                .ToList());
        }

        var client = _factory.Create(apiKey);
        var results = new List<RecipientSendResult>(message.Recipients.Count);

        foreach (var chunk in Chunk(message.Recipients, BroadcastChunkSize))
        {
            var sg = BuildBroadcastChunkMessage(settings, message, chunk);
            try
            {
                var response = await SendWithRetryAsync(client, sg, cancellationToken).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    var batchId = ExtractMessageId(response);
                    foreach (var r in chunk)
                        results.Add(new RecipientSendResult(r.UserId, r.Address, true, batchId, null));
                }
                else
                {
                    var body = await SafeReadBodyAsync(response).ConfigureAwait(false);
                    var reason = $"SendGrid status {(int)response.StatusCode}: {body}";
                    _logger.LogWarning(
                        "[SendGrid] Broadcast {BroadcastId} chunk of {Count} failed: {Reason}",
                        message.BroadcastId, chunk.Count, reason);
                    foreach (var r in chunk)
                        results.Add(new RecipientSendResult(r.UserId, r.Address, false, null, reason));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "[SendGrid] Broadcast {BroadcastId} chunk of {Count} threw",
                    message.BroadcastId, chunk.Count);
                foreach (var r in chunk)
                    results.Add(new RecipientSendResult(r.UserId, r.Address, false, null, ex.Message));
            }
        }

        return new BroadcastSendResult(results);
    }

    // -- Helpers ------------------------------------------------------------

    private bool ShouldDispatch(SiteSettings settings, EmailCategory category, out string apiKey)
    {
        apiKey = settings.SendGridApiKey ?? string.Empty;
        if (!settings.EmailEnabled)
        {
            _logger.LogInformation("[SendGrid] EmailEnabled=false — skipping send ({Category})", category);
            return false;
        }
        if (settings.EmailProvider != EmailProvider.SendGrid)
        {
            _logger.LogWarning("[SendGrid] Provider is {Provider}, not SendGrid — skipping", settings.EmailProvider);
            return false;
        }
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            _logger.LogWarning("[SendGrid] SendGridApiKey not configured — skipping");
            return false;
        }
        return true;
    }

    private static SendGridMessage BuildSingleRecipientMessage(SiteSettings settings, EmailMessage m)
    {
        var to = ResolveTo(settings, m.ToAddress, m.ToName);
        var sg = new SendGridMessage
        {
            From = new EmailAddress(settings.EmailFromAddress, settings.EmailFromName),
            Subject = m.Subject,
            HtmlContent = m.HtmlBody,
            PlainTextContent = m.PlainTextBody,
        };
        sg.AddTo(to);
        if (!string.IsNullOrWhiteSpace(settings.EmailReplyToAddress))
            sg.ReplyTo = new EmailAddress(settings.EmailReplyToAddress);
        sg.AddCustomArg("category", m.Category.ToString());
        if (m.UserId is { } uid) sg.AddCustomArg("userId", uid.ToString("N"));
        return sg;
    }

    private static SendGridMessage BuildBroadcastChunkMessage(
        SiteSettings settings, BroadcastEmailMessage m, IReadOnlyList<EmailRecipient> chunk)
    {
        var sg = new SendGridMessage
        {
            From = new EmailAddress(settings.EmailFromAddress, settings.EmailFromName),
            Subject = m.Subject,
            HtmlContent = m.HtmlBody,
            PlainTextContent = m.PlainTextBody,
        };
        if (!string.IsNullOrWhiteSpace(settings.EmailReplyToAddress))
            sg.ReplyTo = new EmailAddress(settings.EmailReplyToAddress);
        sg.AddCustomArg("category", m.Category.ToString());
        sg.AddCustomArg("broadcastId", m.BroadcastId.ToString("N"));

        // Per-broadcast top-level headers (e.g., List-Unsubscribe set by
        // R7/R12). SendGrid forwards these verbatim to recipients.
        if (m.AdditionalHeaders is { Count: > 0 } extra)
        {
            foreach (var kv in extra) sg.AddHeader(kv.Key, kv.Value);
        }

        // One Personalization block per recipient. Substitutions wrap the
        // merge tokens in {{...}} which SendGrid replaces server-side at
        // send time, so the body is templated once and reused.
        // Build the list locally and assign once — SendGridMessage's
        // Personalizations property has historically had quirks around
        // mid-chain mutation.
        var personalizations = new List<Personalization>(chunk.Count);
        foreach (var r in chunk)
        {
            var p = new Personalization
            {
                Tos = new List<EmailAddress> { ResolveTo(settings, r.Address, r.Name) },
            };
            if (r.MergeFields is { Count: > 0 } mf)
            {
                p.Substitutions = new Dictionary<string, string>(mf.Count);
                foreach (var kv in mf)
                    p.Substitutions["{{" + kv.Key + "}}"] = kv.Value;
            }
            personalizations.Add(p);
        }
        sg.Personalizations = personalizations;

        return sg;
    }

    /// <summary>Resolves the actual recipient — when
    /// <see cref="SiteSettings.TestEmailRecipient"/> is set (staging),
    /// EVERY outbound message goes there regardless of intent.</summary>
    private static EmailAddress ResolveTo(SiteSettings settings, string address, string name)
    {
        if (!string.IsNullOrWhiteSpace(settings.TestEmailRecipient))
            return new EmailAddress(settings.TestEmailRecipient, "[Staging Override] " + name);
        return new EmailAddress(address, name);
    }

    private static IEnumerable<IReadOnlyList<EmailRecipient>> Chunk(IReadOnlyList<EmailRecipient> source, int size)
    {
        for (var i = 0; i < source.Count; i += size)
        {
            var take = Math.Min(size, source.Count - i);
            var chunk = new EmailRecipient[take];
            for (var j = 0; j < take; j++) chunk[j] = source[i + j];
            yield return chunk;
        }
    }

    /// <summary>Single retry on 5xx with linear back-off (250ms). The
    /// SendGrid SDK doesn't auto-retry; for the modest church-scale send
    /// volume one retry is enough — webhook bounce events surface
    /// permanent failures regardless.</summary>
    private static async Task<Response> SendWithRetryAsync(ISendGridClient client, SendGridMessage msg, CancellationToken ct)
    {
        var first = await client.SendEmailAsync(msg, ct).ConfigureAwait(false);
        if ((int)first.StatusCode < 500) return first;

        await Task.Delay(TimeSpan.FromMilliseconds(250), ct).ConfigureAwait(false);
        return await client.SendEmailAsync(msg, ct).ConfigureAwait(false);
    }

    private static string? ExtractMessageId(Response response)
    {
        if (response.Headers is null) return null;
        if (response.Headers.TryGetValues("X-Message-Id", out var values))
            return values.FirstOrDefault();
        return null;
    }

    private static async Task<string> SafeReadBodyAsync(Response response)
    {
        try
        {
            if (response.Body is null) return "(no body)";
            return await response.Body.ReadAsStringAsync().ConfigureAwait(false);
        }
        catch
        {
            return "(unreadable)";
        }
    }
}
