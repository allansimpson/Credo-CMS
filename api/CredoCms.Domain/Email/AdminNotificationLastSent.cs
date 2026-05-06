namespace CredoCms.Domain.Email;

/// <summary>
/// Tracks the last time the admin-notification digest service emailed a
/// given user for a given category. Combined with the per-user frequency
/// preference (or the site-wide default), this gates whether a digest is
/// due. Compound-unique on <c>(UserId, NotificationCategory)</c>.
/// </summary>
public sealed class AdminNotificationLastSent
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public AdminNotificationCategory NotificationCategory { get; set; }

    public DateTimeOffset LastSentAt { get; set; }
}
