using CredoCms.Domain.Email;

namespace CredoCms.Application.Common;

/// <summary>
/// Outbound email abstraction. Phase 5 splits the surface in two:
/// <see cref="SendTransactionalAsync"/> for account / system mail (which
/// bypasses the suppression list and per-user notification preferences) and
/// <see cref="SendBroadcastAsync"/> for bulk member mail (which respects
/// both). <see cref="IsConfiguredAsync"/> lets startup health checks and
/// the admin "send test email" UI surface a meaningful error before
/// invoking either send method.
///
/// <para>When <c>SiteSettings.EmailEnabled</c> is false (default on initial
/// deployment) the implementation logs and returns successfully without
/// dispatching — preventing accidental email storms during setup.</para>
/// </summary>
public interface IEmailService
{
    /// <summary>One-recipient send for account-related mail (invitation,
    /// password reset, registration confirmation, ack receipts). Bypasses
    /// the suppression list per CAN-SPAM transactional exemption.</summary>
    Task SendTransactionalAsync(EmailMessage message, CancellationToken cancellationToken = default);

    /// <summary>Bulk send for member mail (broadcasts, news/blog email-on-
    /// publish, group communication). Recipients are filtered against the
    /// suppression list before dispatch; preference filtering is the
    /// caller's responsibility (handled by the recipient resolver).</summary>
    Task SendBroadcastAsync(BroadcastEmailMessage message, CancellationToken cancellationToken = default);

    /// <summary>True when the configured provider has the credentials it
    /// needs to actually dispatch mail. <c>LoggingEmailService</c> always
    /// returns true (it doesn't need credentials). SendGrid / SMTP impls
    /// return false when the relevant key/host is missing.</summary>
    Task<bool> IsConfiguredAsync(CancellationToken cancellationToken = default);
}

/// <summary>Single-recipient outbound email payload.</summary>
/// <param name="ToAddress">Recipient address.</param>
/// <param name="ToName">Recipient display name.</param>
/// <param name="Subject">Subject line.</param>
/// <param name="HtmlBody">HTML body. Required.</param>
/// <param name="PlainTextBody">Plain-text fallback. Auto-derived from
/// <paramref name="HtmlBody"/> when null and the impl supports it.</param>
/// <param name="UserId">Optional <c>ApplicationUser</c> id; null for
/// non-account-bound recipients (e.g., Connect Card auto-acknowledgment to
/// a public submitter).</param>
/// <param name="Category">Drives suppression-list checks: only
/// <see cref="EmailCategory.Transactional"/> bypasses the list.</param>
/// <param name="Attachments">Optional file attachments.</param>
public sealed record EmailMessage(
    string ToAddress,
    string ToName,
    string Subject,
    string HtmlBody,
    string? PlainTextBody = null,
    Guid? UserId = null,
    EmailCategory Category = EmailCategory.Transactional,
    IReadOnlyList<EmailAttachment>? Attachments = null);

/// <summary>An attachment for <see cref="EmailMessage"/>.</summary>
public sealed record EmailAttachment(
    string FileName,
    string ContentType,
    byte[] Content);

/// <summary>Multi-recipient outbound email payload (broadcast). Recipients
/// already passed through the resolver — preference + suppression filters
/// applied. Each <see cref="EmailRecipient"/> may carry its own merge fields
/// for personalization.</summary>
public sealed record BroadcastEmailMessage(
    string Subject,
    string HtmlBody,
    string? PlainTextBody,
    IReadOnlyList<EmailRecipient> Recipients,
    Guid BroadcastId,
    EmailCategory Category);

/// <summary>One recipient of a broadcast send.</summary>
public sealed record EmailRecipient(
    string Address,
    string Name,
    Guid? UserId,
    IReadOnlyDictionary<string, string>? MergeFields = null);
