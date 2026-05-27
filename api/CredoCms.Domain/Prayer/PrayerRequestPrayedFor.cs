namespace CredoCms.Domain.Prayer;

/// <summary>
/// "I prayed for this" toggle row. Each member can mark a given request once
/// (unique constraint on PrayerRequestId+UserId). Not versioned.
/// </summary>
public sealed class PrayerRequestPrayedFor
{
    public Guid Id { get; set; }

    public Guid PrayerRequestId { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset PrayedAt { get; set; }
}
