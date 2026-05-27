using System.ComponentModel.DataAnnotations;

namespace CredoCms.Domain.Email;

/// <summary>
/// Idempotency record for SendGrid webhook events. The provider may retry
/// a webhook callback if our endpoint returns non-2xx; we record each
/// processed <c>sg_event_id</c> here and skip duplicates.
/// </summary>
public sealed class WebhookEventLog
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string EventId { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string EventType { get; set; } = string.Empty;

    public DateTimeOffset ProcessedAt { get; set; }
}
