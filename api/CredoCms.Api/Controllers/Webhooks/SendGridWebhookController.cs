using System.Text.Json;
using System.Text.Json.Serialization;
using CredoCms.Application.Email;
using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Infrastructure.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers.Webhooks;

/// <summary>
/// Endpoint SendGrid POSTs event-webhook payloads to. Anonymous-but-signed:
/// rejects 401 unless ECDSA signature + timestamp verify against the
/// configured public key. Returns 200 on success, even with per-event
/// errors inside the batch — non-2xx triggers SendGrid's redelivery
/// schedule which compounds problems.
/// </summary>
[ApiController]
[Route("api/webhooks/sendgrid")]
[AllowAnonymous]
public sealed class SendGridWebhookController : ControllerBase
{
    /// <summary>SendGrid's signed-event headers (legacy "Twilio" prefix —
    /// SendGrid is a Twilio company, the prefix is real).</summary>
    public const string SignatureHeader = "X-Twilio-Email-Event-Webhook-Signature";

    public const string TimestampHeader = "X-Twilio-Email-Event-Webhook-Timestamp";

    /// <summary>Reject events older than this — protects against replay.</summary>
    private static readonly TimeSpan MaxClockSkew = TimeSpan.FromMinutes(5);

    private readonly ISendGridWebhookVerifier _verifier;
    private readonly ISiteSettingsRepository _settings;
    private readonly ISendGridWebhookEventProcessor _processor;

    public SendGridWebhookController(
        ISendGridWebhookVerifier verifier,
        ISiteSettingsRepository settings,
        ISendGridWebhookEventProcessor processor)
    {
        _verifier = verifier;
        _settings = settings;
        _processor = processor;
    }

    [HttpPost]
    public async Task<IActionResult> ReceiveAsync(CancellationToken ct)
    {
        // Read raw body — verification needs the exact bytes SendGrid signed.
        Request.EnableBuffering();
        Request.Body.Position = 0;
        using var reader = new StreamReader(Request.Body, leaveOpen: true);
        var body = await reader.ReadToEndAsync(ct).ConfigureAwait(false);

        var signature = Request.Headers[SignatureHeader].ToString();
        var timestamp = Request.Headers[TimestampHeader].ToString();

        var settings = await _settings.GetAsync(ct).ConfigureAwait(false);
        var publicKey = settings.SendGridWebhookSecret ?? string.Empty;

        if (string.IsNullOrWhiteSpace(publicKey)
            || string.IsNullOrWhiteSpace(signature)
            || string.IsNullOrWhiteSpace(timestamp))
        {
            return Unauthorized();
        }

        if (!IsTimestampFresh(timestamp))
        {
            return Unauthorized();
        }

        if (!_verifier.Verify(publicKey, body, signature, timestamp))
        {
            return Unauthorized();
        }

        var events = ParseEvents(body);
        var applied = await _processor.ProcessAsync(events, ct).ConfigureAwait(false);
        return Ok(new { applied });
    }

    /// <summary>SendGrid sends Unix-seconds timestamps. Window enforced
    /// against the server clock to bound replay risk.</summary>
    private static bool IsTimestampFresh(string timestamp)
    {
        if (!long.TryParse(timestamp, out var unixSeconds)) return false;
        var sent = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        var skew = DateTimeOffset.UtcNow - sent;
        return skew.Duration() <= MaxClockSkew;
    }

    private static List<SendGridWebhookEvent> ParseEvents(string body)
    {
        var result = new List<SendGridWebhookEvent>();
        if (string.IsNullOrWhiteSpace(body)) return result;
        try
        {
            var raws = JsonSerializer.Deserialize<List<RawEvent>>(body, JsonOptions);
            if (raws is null) return result;
            foreach (var r in raws)
            {
                if (r.@event is null) continue;
                result.Add(new SendGridWebhookEvent(
                    EventType: r.@event,
                    SgEventId: r.sg_event_id ?? string.Empty,
                    SgMessageId: r.sg_message_id ?? string.Empty,
                    Email: r.email ?? string.Empty,
                    Timestamp: r.timestamp,
                    Reason: r.reason,
                    Type: r.type));
            }
        }
        catch (JsonException)
        {
            // Malformed body — surface as zero events, signature already passed.
        }
        return result;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = null,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Loose JSON shape — SendGrid's payload has many more fields,
    /// but only these are needed downstream. Lower-cased property names
    /// match the wire format exactly to avoid any naming-policy surprises.</summary>
    private sealed record RawEvent(
        string? @event,
        string? sg_event_id,
        string? sg_message_id,
        string? email,
        long timestamp,
        string? reason,
        string? type);
}
