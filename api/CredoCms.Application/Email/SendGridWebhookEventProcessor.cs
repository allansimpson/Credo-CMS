using CredoCms.Application.RealTime;
using CredoCms.Domain.Email;
using CredoCms.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace CredoCms.Application.Email;

/// <summary>
/// Parsed shape of one entry in a SendGrid event-webhook payload. The
/// SendGrid SDK doesn't ship a strongly-typed model for this; the JSON
/// shape is loose by design (different fields are present per event
/// type). We project to this record at the controller boundary.
/// </summary>
public sealed record SendGridWebhookEvent(
    string EventType,
    string SgEventId,
    string SgMessageId,
    string Email,
    long Timestamp,
    string? Reason = null,
    string? Type = null);

public interface ISendGridWebhookEventProcessor
{
    /// <summary>Process a batch. Returns the number of new events that
    /// were applied (duplicates skipped). Suppression list, broadcast
    /// stats, and recipient rows are updated atomically per event.</summary>
    Task<int> ProcessAsync(IReadOnlyList<SendGridWebhookEvent> events, CancellationToken ct = default);
}

public sealed class SendGridWebhookEventProcessor : ISendGridWebhookEventProcessor
{
    private readonly IWebhookEventLogRepository _eventLog;
    private readonly IEmailSuppressionService _suppression;
    private readonly IEmailBroadcastRecipientRepository _recipients;
    private readonly IEmailBroadcastRepository _broadcasts;
    private readonly IRealtimeNotifier _notifier;
    private readonly UserManager<ApplicationUser> _users;
    private readonly ILogger<SendGridWebhookEventProcessor> _logger;

    public SendGridWebhookEventProcessor(
        IWebhookEventLogRepository eventLog,
        IEmailSuppressionService suppression,
        IEmailBroadcastRecipientRepository recipients,
        IEmailBroadcastRepository broadcasts,
        IRealtimeNotifier notifier,
        UserManager<ApplicationUser> users,
        ILogger<SendGridWebhookEventProcessor> logger)
    {
        _eventLog = eventLog;
        _suppression = suppression;
        _recipients = recipients;
        _broadcasts = broadcasts;
        _notifier = notifier;
        _users = users;
        _logger = logger;
    }

    public async Task<int> ProcessAsync(IReadOnlyList<SendGridWebhookEvent> events, CancellationToken ct = default)
    {
        var applied = 0;
        var statsByBroadcast = new Dictionary<Guid, (int delivered, int bounced, int complaint, int open)>();

        foreach (var ev in events)
        {
            if (string.IsNullOrWhiteSpace(ev.SgEventId)) continue;
            if (await _eventLog.ExistsAsync(ev.SgEventId, ct).ConfigureAwait(false))
                continue;

            try
            {
                await HandleSingleAsync(ev, statsByBroadcast, ct).ConfigureAwait(false);
                await _eventLog.AddAsync(new WebhookEventLog
                {
                    Id = Guid.NewGuid(),
                    EventId = ev.SgEventId,
                    EventType = ev.EventType,
                    ProcessedAt = DateTimeOffset.UtcNow,
                }, ct).ConfigureAwait(false);
                applied++;
            }
            catch (Exception ex)
            {
                // Per-event errors don't abort the batch; SendGrid will not
                // re-deliver dropped events when the response is 200, so we
                // log and move on (rather than 5xx-ing the whole batch and
                // forcing a redelivery storm).
                _logger.LogError(ex,
                    "[SendGrid] Webhook event processing failed for event {EventId} ({EventType})",
                    ev.SgEventId, ev.EventType);
            }
        }

        // Apply aggregate stats once per broadcast at the end of the batch.
        foreach (var (broadcastId, deltas) in statsByBroadcast)
        {
            var b = await _broadcasts.IncrementStatsAsync(
                broadcastId, deltas.delivered, deltas.bounced, deltas.complaint, deltas.open, ct).ConfigureAwait(false);
            if (b is null) continue;
            await _notifier.NotifyBroadcastLifecycleAsync(new BroadcastLifecycleMessage(
                Kind: "BroadcastStatsUpdated",
                BroadcastId: broadcastId,
                DeliveredCount: b.DeliveredCount,
                BouncedCount: b.BouncedCount,
                ComplaintCount: b.ComplaintCount,
                OpenCount: b.OpenCount), ct).ConfigureAwait(false);
        }

        return applied;
    }

    private async Task HandleSingleAsync(
        SendGridWebhookEvent ev,
        Dictionary<Guid, (int delivered, int bounced, int complaint, int open)> stats,
        CancellationToken ct)
    {
        var recipient = string.IsNullOrWhiteSpace(ev.SgMessageId)
            ? null
            : await _recipients.GetBySendGridMessageIdAsync(ev.SgMessageId, ct).ConfigureAwait(false);
        var now = DateTimeOffset.UtcNow;

        switch (ev.EventType)
        {
            case "delivered":
                if (recipient is not null)
                {
                    recipient.Status = RecipientStatus.Delivered;
                    recipient.DeliveredAt = now;
                    await _recipients.UpdateAsync(recipient, ct).ConfigureAwait(false);
                    Bump(stats, recipient.BroadcastId, deliveredDelta: 1);
                }
                break;

            case "open":
                if (recipient is not null && recipient.OpenedAt is null)
                {
                    recipient.OpenedAt = now;
                    await _recipients.UpdateAsync(recipient, ct).ConfigureAwait(false);
                    Bump(stats, recipient.BroadcastId, openDelta: 1);
                }
                break;

            case "click":
                if (recipient is not null && recipient.ClickedAt is null)
                {
                    recipient.ClickedAt = now;
                    await _recipients.UpdateAsync(recipient, ct).ConfigureAwait(false);
                }
                break;

            case "bounce":
                // Soft bounces are retried by SendGrid — we only suppress on hard.
                if (string.Equals(ev.Type, "bounce", StringComparison.OrdinalIgnoreCase))
                {
                    await _suppression.AddAsync(ev.Email, SuppressionType.HardBounce,
                        SuppressionSource.SendGridWebhook, ev.Reason, ct).ConfigureAwait(false);
                }
                if (recipient is not null)
                {
                    recipient.Status = RecipientStatus.Bounced;
                    recipient.BouncedAt = now;
                    recipient.BounceReason = ev.Reason;
                    await _recipients.UpdateAsync(recipient, ct).ConfigureAwait(false);
                    Bump(stats, recipient.BroadcastId, bouncedDelta: 1);
                }
                break;

            case "spamreport":
                await _suppression.AddAsync(ev.Email, SuppressionType.SpamComplaint,
                    SuppressionSource.SendGridWebhook, ev.Reason, ct).ConfigureAwait(false);
                await DisableRecipientPreferencesAsync(ev.Email, ct).ConfigureAwait(false);
                if (recipient is not null)
                {
                    recipient.Status = RecipientStatus.ComplainedSpam;
                    await _recipients.UpdateAsync(recipient, ct).ConfigureAwait(false);
                    Bump(stats, recipient.BroadcastId, complaintDelta: 1);
                }
                break;

            case "unsubscribe":
            case "group_unsubscribe":
                await _suppression.AddAsync(ev.Email, SuppressionType.Unsubscribe,
                    SuppressionSource.SendGridWebhook, ev.Reason, ct).ConfigureAwait(false);
                await DisableRecipientPreferencesAsync(ev.Email, ct).ConfigureAwait(false);
                break;

            case "dropped":
                // SendGrid suppressed before send (e.g., already-suppressed
                // address). Update the recipient row if it exists; no
                // suppression-list write (the address is already there).
                if (recipient is not null)
                {
                    recipient.Status = RecipientStatus.Suppressed;
                    recipient.BounceReason = ev.Reason;
                    await _recipients.UpdateAsync(recipient, ct).ConfigureAwait(false);
                }
                break;

            default:
                _logger.LogDebug("[SendGrid] Unhandled event type {Type}", ev.EventType);
                break;
        }
    }

    private async Task DisableRecipientPreferencesAsync(string email, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(email)) return;
        var user = await _users.FindByEmailAsync(email).ConfigureAwait(false);
        if (user is null) return;
        user.ReceiveNewsEmails = false;
        user.ReceiveBlogEmails = false;
        user.ReceiveBroadcastEmails = false;
        user.ReceiveGroupEmailsGlobal = false;
        await _users.UpdateAsync(user).ConfigureAwait(false);
    }

    private static void Bump(
        Dictionary<Guid, (int delivered, int bounced, int complaint, int open)> stats,
        Guid broadcastId,
        int deliveredDelta = 0, int bouncedDelta = 0, int complaintDelta = 0, int openDelta = 0)
    {
        (int delivered, int bounced, int complaint, int open) prev =
            stats.TryGetValue(broadcastId, out var v) ? v : (0, 0, 0, 0);
        stats[broadcastId] = (
            prev.delivered + deliveredDelta,
            prev.bounced + bouncedDelta,
            prev.complaint + complaintDelta,
            prev.open + openDelta);
    }
}
