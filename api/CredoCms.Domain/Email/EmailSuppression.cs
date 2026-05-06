using System.ComponentModel.DataAnnotations;

namespace CredoCms.Domain.Email;

/// <summary>
/// An email address that should not receive non-transactional mail. Populated
/// from SendGrid webhook events (hard bounce, spam complaint, unsubscribe),
/// member-driven one-click unsubscribe, or admin manual entry.
///
/// <para>Transactional sends bypass this list — security and account-function
/// emails are exempted from CAN-SPAM as well.</para>
/// </summary>
public sealed class EmailSuppression
{
    public Guid Id { get; set; }

    /// <summary>Always stored lowercase. Indexed unique.</summary>
    [Required]
    [MaxLength(200)]
    public string EmailAddress { get; set; } = string.Empty;

    public SuppressionType SuppressionType { get; set; }

    [MaxLength(1000)]
    public string? Reason { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public SuppressionSource CreatedSource { get; set; }
}
