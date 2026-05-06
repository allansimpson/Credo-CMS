using CredoCms.Application.Common;
using CredoCms.Application.RealTime;
using CredoCms.Domain.Common;
using CredoCms.Domain.Email;

namespace CredoCms.Application.Email;

public interface IEmailBroadcastService
{
    Task<EmailBroadcast> CreateDraftAsync(BroadcastDraftInput input, CancellationToken ct = default);
    Task<EmailBroadcast> UpdateDraftAsync(Guid id, BroadcastDraftInput input, CancellationToken ct = default);
    Task<EmailBroadcast> ScheduleAsync(Guid id, DateTimeOffset sendAt, CancellationToken ct = default);
    Task<EmailBroadcast> SendNowAsync(Guid id, CancellationToken ct = default);
    Task CancelAsync(Guid id, CancellationToken ct = default);
    Task<RecipientPreview> PreviewRecipientsAsync(Guid id, CancellationToken ct = default);

    /// <summary>Worker entry-point: actually dispatches the broadcast via
    /// IEmailService. Resolves recipients at send time, persists per-recipient
    /// rows, calls the email service, persists outcomes, transitions Status.</summary>
    Task ExecuteSendAsync(Guid id, CancellationToken ct = default);
}

public sealed record BroadcastDraftInput(
    string Subject,
    string Body,
    string? PlainTextBody,
    BroadcastTargetMode TargetMode,
    IReadOnlyCollection<Guid>? TargetGroupIds,
    EmailCategory Category = EmailCategory.Broadcast);

public sealed class EmailBroadcastService : IEmailBroadcastService
{
    private readonly IEmailBroadcastRepository _broadcasts;
    private readonly IEmailBroadcastRecipientRepository _recipients;
    private readonly IRecipientResolver _resolver;
    private readonly IEmailService _email;
    private readonly IRealtimeNotifier _notifier;
    private readonly IAuditLogger _audit;

    public EmailBroadcastService(
        IEmailBroadcastRepository broadcasts,
        IEmailBroadcastRecipientRepository recipients,
        IRecipientResolver resolver,
        IEmailService email,
        IRealtimeNotifier notifier,
        IAuditLogger audit)
    {
        _broadcasts = broadcasts;
        _recipients = recipients;
        _resolver = resolver;
        _email = email;
        _notifier = notifier;
        _audit = audit;
    }

    public async Task<EmailBroadcast> CreateDraftAsync(BroadcastDraftInput input, CancellationToken ct = default)
    {
        var b = new EmailBroadcast
        {
            Id = Guid.NewGuid(),
            Subject = input.Subject,
            Body = input.Body,
            PlainTextBody = input.PlainTextBody,
            TargetMode = input.TargetMode,
            TargetGroupIdsJson = SerializeGuids(input.TargetGroupIds),
            SendMode = BroadcastSendMode.SendNow,
            Status = BroadcastStatus.Draft,
            Category = input.Category,
            CreatedAt = DateTimeOffset.UtcNow,
            ModifiedAt = DateTimeOffset.UtcNow,
        };
        await _broadcasts.AddAsync(b, ct).ConfigureAwait(false);
        await _audit.WriteAsync("EmailBroadcast.DraftCreated", nameof(EmailBroadcast), b.Id.ToString(), cancellationToken: ct).ConfigureAwait(false);
        return b;
    }

    public async Task<EmailBroadcast> UpdateDraftAsync(Guid id, BroadcastDraftInput input, CancellationToken ct = default)
    {
        var b = await GetEditable(id, ct).ConfigureAwait(false);
        b.Subject = input.Subject;
        b.Body = input.Body;
        b.PlainTextBody = input.PlainTextBody;
        b.TargetMode = input.TargetMode;
        b.TargetGroupIdsJson = SerializeGuids(input.TargetGroupIds);
        b.Category = input.Category;
        b.ModifiedAt = DateTimeOffset.UtcNow;
        await _broadcasts.UpdateAsync(b, ct).ConfigureAwait(false);
        return b;
    }

    public async Task<EmailBroadcast> ScheduleAsync(Guid id, DateTimeOffset sendAt, CancellationToken ct = default)
    {
        if (sendAt <= DateTimeOffset.UtcNow.AddMinutes(1))
            throw new InvalidOperationException("Scheduled send time must be at least 1 minute in the future.");
        var b = await GetEditable(id, ct).ConfigureAwait(false);
        b.SendMode = BroadcastSendMode.Scheduled;
        b.ScheduledSendAt = sendAt;
        b.Status = BroadcastStatus.Scheduled;
        b.ModifiedAt = DateTimeOffset.UtcNow;
        await _broadcasts.UpdateAsync(b, ct).ConfigureAwait(false);
        await _audit.WriteAsync("EmailBroadcast.Scheduled", nameof(EmailBroadcast), id.ToString(),
            details: new { sendAt }, cancellationToken: ct).ConfigureAwait(false);
        return b;
    }

    public async Task<EmailBroadcast> SendNowAsync(Guid id, CancellationToken ct = default)
    {
        var b = await GetEditable(id, ct).ConfigureAwait(false);
        b.SendMode = BroadcastSendMode.SendNow;
        b.ScheduledSendAt = null;
        b.Status = BroadcastStatus.Sending;
        b.ModifiedAt = DateTimeOffset.UtcNow;
        await _broadcasts.UpdateAsync(b, ct).ConfigureAwait(false);
        await _audit.WriteAsync("EmailBroadcast.SendStarted", nameof(EmailBroadcast), id.ToString(), cancellationToken: ct).ConfigureAwait(false);
        // Dispatch happens on the worker thread; the worker picks up
        // Sending broadcasts and resumes/starts dispatch.
        return b;
    }

    public async Task CancelAsync(Guid id, CancellationToken ct = default)
    {
        var b = await _broadcasts.GetAsync(id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Broadcast {id} not found.");
        if (b.Status is not BroadcastStatus.Scheduled and not BroadcastStatus.Draft)
            throw new InvalidOperationException("Only Draft or Scheduled broadcasts can be canceled.");
        b.Status = BroadcastStatus.Canceled;
        b.ModifiedAt = DateTimeOffset.UtcNow;
        await _broadcasts.UpdateAsync(b, ct).ConfigureAwait(false);
        await _audit.WriteAsync("EmailBroadcast.Canceled", nameof(EmailBroadcast), id.ToString(), cancellationToken: ct).ConfigureAwait(false);
    }

    public async Task<RecipientPreview> PreviewRecipientsAsync(Guid id, CancellationToken ct = default)
    {
        var b = await _broadcasts.GetAsync(id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Broadcast {id} not found.");
        return await _resolver.PreviewAsync(b.TargetMode, DeserializeGuids(b.TargetGroupIdsJson), b.Category, ct: ct).ConfigureAwait(false);
    }

    public async Task ExecuteSendAsync(Guid id, CancellationToken ct = default)
    {
        var b = await _broadcasts.GetAsync(id, ct).ConfigureAwait(false);
        if (b is null) return;
        if (b.Status is not (BroadcastStatus.Sending or BroadcastStatus.Scheduled)) return;

        // Resolve recipients fresh — captures group-membership changes
        // between compose-time and send-time.
        var recipients = await _resolver.ResolveAsync(
            b.TargetMode, DeserializeGuids(b.TargetGroupIdsJson), b.Category, ct).ConfigureAwait(false);
        b.RecipientCountAtSend = recipients.Count;

        await _notifier.NotifyBroadcastLifecycleAsync(new BroadcastLifecycleMessage(
            "BroadcastSendStarted", b.Id, RecipientCount: recipients.Count), ct).ConfigureAwait(false);

        if (recipients.Count == 0)
        {
            b.Status = BroadcastStatus.Sent;
            b.SentAt = DateTimeOffset.UtcNow;
            b.ModifiedAt = DateTimeOffset.UtcNow;
            await _broadcasts.UpdateAsync(b, ct).ConfigureAwait(false);
            await _notifier.NotifyBroadcastLifecycleAsync(new BroadcastLifecycleMessage(
                "BroadcastSendCompleted", b.Id, RecipientCount: 0), ct).ConfigureAwait(false);
            return;
        }

        // Persist Pending rows up front so the worker can resume on crash.
        var rows = recipients.Select(r => new EmailBroadcastRecipient
        {
            Id = Guid.NewGuid(),
            BroadcastId = b.Id,
            UserId = r.UserId,
            EmailAddressSnapshot = r.Address,
            DisplayNameSnapshot = r.Name,
            Status = RecipientStatus.Pending,
        }).ToList();
        await _recipients.BulkInsertAsync(rows, ct).ConfigureAwait(false);

        // Dispatch.
        var msg = new BroadcastEmailMessage(
            Subject: b.Subject,
            HtmlBody: b.Body,
            PlainTextBody: b.PlainTextBody,
            Recipients: recipients,
            BroadcastId: b.Id,
            Category: b.Category,
            // R12 will populate List-Unsubscribe headers here.
            AdditionalHeaders: null);

        BroadcastSendResult result;
        try
        {
            result = await _email.SendBroadcastAsync(msg, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            b.Status = BroadcastStatus.Failed;
            b.FailureReason = ex.Message;
            b.ModifiedAt = DateTimeOffset.UtcNow;
            await _broadcasts.UpdateAsync(b, ct).ConfigureAwait(false);
            return;
        }

        // Update each recipient row with the dispatch outcome.
        var byAddress = rows.ToDictionary(r => r.EmailAddressSnapshot, r => r, StringComparer.OrdinalIgnoreCase);
        foreach (var r in result.Recipients)
        {
            if (!byAddress.TryGetValue(r.Address, out var row)) continue;
            if (r.Success)
            {
                // Status stays Pending until SendGrid's "delivered" webhook
                // event arrives. The provider message id is what the
                // webhook events correlate against.
                row.SendGridMessageId = r.SendGridMessageId;
            }
            else
            {
                row.Status = RecipientStatus.Failed;
                row.BounceReason = r.ErrorMessage;
            }
            await _recipients.UpdateAsync(row, ct).ConfigureAwait(false);
        }

        b.Status = BroadcastStatus.Sent;
        b.SentAt = DateTimeOffset.UtcNow;
        b.ModifiedAt = DateTimeOffset.UtcNow;
        await _broadcasts.UpdateAsync(b, ct).ConfigureAwait(false);

        await _notifier.NotifyBroadcastLifecycleAsync(new BroadcastLifecycleMessage(
            "BroadcastSendCompleted", b.Id, RecipientCount: recipients.Count), ct).ConfigureAwait(false);
    }

    private async Task<EmailBroadcast> GetEditable(Guid id, CancellationToken ct)
    {
        var b = await _broadcasts.GetAsync(id, ct).ConfigureAwait(false)
            ?? throw new InvalidOperationException($"Broadcast {id} not found.");
        if (b.Status is BroadcastStatus.Sent or BroadcastStatus.Sending or BroadcastStatus.Canceled)
            throw new InvalidOperationException($"Broadcast {id} is {b.Status} and cannot be modified.");
        return b;
    }

    private static string? SerializeGuids(IReadOnlyCollection<Guid>? ids)
    {
        if (ids is null || ids.Count == 0) return null;
        return System.Text.Json.JsonSerializer.Serialize(ids);
    }

    internal static IReadOnlyCollection<Guid>? DeserializeGuids(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(json);
        }
        catch (System.Text.Json.JsonException)
        {
            return null;
        }
    }
}
