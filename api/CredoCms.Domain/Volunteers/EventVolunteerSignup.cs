namespace CredoCms.Domain.Volunteers;

/// <summary>
/// A member's commitment to fill one slot of a volunteer role on a specific
/// occurrence date. Append-only; cancellation sets <see cref="CanceledAt"/>
/// rather than deleting the row, so the slot can be re-claimed without
/// losing audit history.
///
/// <para>Filtered unique constraint on
/// <c>(EventVolunteerRoleId, OccurrenceDate, UserId) WHERE CanceledAt IS NULL</c>
/// prevents a member from holding the same slot twice while still allowing
/// them to re-sign-up after canceling.</para>
/// </summary>
public sealed class EventVolunteerSignup
{
    public Guid Id { get; set; }

    public Guid EventVolunteerRoleId { get; set; }

    /// <summary>Denormalized for query convenience (e.g.,
    /// "all my upcoming volunteer commitments"). Always consistent with
    /// the role's <c>EventId</c>.</summary>
    public Guid EventId { get; set; }

    public DateOnly OccurrenceDate { get; set; }

    public Guid UserId { get; set; }

    public DateTimeOffset SignedUpAt { get; set; }

    public DateTimeOffset? CanceledAt { get; set; }

    public DateTimeOffset? ReminderEmailSentAt { get; set; }
}
