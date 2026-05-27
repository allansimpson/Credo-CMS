using System.ComponentModel.DataAnnotations;
using CredoCms.Domain.Common;

namespace CredoCms.Domain.Email;

/// <summary>
/// A composed broadcast email. Holds metadata + body; per-recipient
/// delivery state is on <see cref="EmailBroadcastRecipient"/>.
///
/// <para>Versioned for audit ("what was sent, who composed it, when") —
/// per project decision retained indefinitely (no <c>IsDeleted</c>). When
/// an <c>ApplicationUser</c> is hard-deleted, references in
/// <see cref="EmailBroadcastRecipient.UserId"/> are nulled but the rows
/// (and <c>EmailAddressSnapshot</c>) are preserved.</para>
/// </summary>
public sealed class EmailBroadcast : IVersionedEntity
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>TipTap ProseMirror JSON.</summary>
    [Required]
    public string Body { get; set; } = string.Empty;

    /// <summary>Plain-text fallback; auto-derived from <see cref="Body"/>
    /// when null.</summary>
    public string? PlainTextBody { get; set; }

    public BroadcastTargetMode TargetMode { get; set; }

    /// <summary>JSON array of Group GUIDs. Empty/null when
    /// <see cref="TargetMode"/> is <see cref="BroadcastTargetMode.AllMembers"/>.</summary>
    public string? TargetGroupIdsJson { get; set; }

    public BroadcastSendMode SendMode { get; set; }

    public DateTimeOffset? ScheduledSendAt { get; set; }

    public BroadcastStatus Status { get; set; } = BroadcastStatus.Draft;

    public DateTimeOffset? SentAt { get; set; }

    [MaxLength(2000)]
    public string? FailureReason { get; set; }

    public int? RecipientCountAtSend { get; set; }

    public int DeliveredCount { get; set; }

    public int BouncedCount { get; set; }

    public int ComplaintCount { get; set; }

    public int OpenCount { get; set; }

    /// <summary>Category drives the recipient-resolver's preference filter.
    /// Always <see cref="EmailCategory.Broadcast"/> for manual broadcasts;
    /// auto-broadcasts from News/Blog publish set this to <c>News</c> /
    /// <c>Blog</c> so the right preference flag is consulted.</summary>
    public EmailCategory Category { get; set; } = EmailCategory.Broadcast;

    /// <summary>For auto-broadcasts triggered by email-on-publish, the
    /// originating entity's id. Null for manually-composed broadcasts.</summary>
    public Guid? SourceEntityId { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    public DateTimeOffset ModifiedAt { get; set; }
}

/// <summary>
/// Per-recipient row recording the outcome of a single broadcast message.
/// Append-only; not versioned (event-record style).
/// </summary>
public sealed class EmailBroadcastRecipient
{
    public Guid Id { get; set; }

    public Guid BroadcastId { get; set; }

    /// <summary>Nullable so hard-deleting a user doesn't cascade-delete this
    /// audit row. <see cref="EmailAddressSnapshot"/> + <see cref="DisplayNameSnapshot"/>
    /// are captured at send time so the row remains meaningful after the
    /// referenced user is gone.</summary>
    public Guid? UserId { get; set; }

    [Required]
    [MaxLength(200)]
    public string EmailAddressSnapshot { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string DisplayNameSnapshot { get; set; } = string.Empty;

    public RecipientStatus Status { get; set; } = RecipientStatus.Pending;

    public DateTimeOffset? DeliveredAt { get; set; }

    public DateTimeOffset? OpenedAt { get; set; }

    public DateTimeOffset? ClickedAt { get; set; }

    public DateTimeOffset? BouncedAt { get; set; }

    [MaxLength(1000)]
    public string? BounceReason { get; set; }

    /// <summary>SendGrid's per-message ID; correlates inbound webhook events.</summary>
    [MaxLength(200)]
    public string? SendGridMessageId { get; set; }
}
