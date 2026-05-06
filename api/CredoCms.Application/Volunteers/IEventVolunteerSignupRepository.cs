using CredoCms.Domain.Volunteers;

namespace CredoCms.Application.Volunteers;

public interface IEventVolunteerSignupRepository
{
    Task<EventVolunteerSignup?> GetAsync(Guid id, CancellationToken ct = default);

    /// <summary>Active signups for a role on a specific occurrence.
    /// Excludes canceled.</summary>
    Task<List<EventVolunteerSignup>> ListActiveForRoleOccurrenceAsync(
        Guid roleId,
        DateOnly occurrenceDate,
        CancellationToken ct = default);

    /// <summary>Active signups for a single user across all upcoming events.
    /// Used by <c>/profile/volunteer</c>.</summary>
    Task<List<EventVolunteerSignup>> ListUpcomingForUserAsync(
        Guid userId,
        DateOnly fromDate,
        CancellationToken ct = default);

    /// <summary>All signups (including canceled) for an event — admin view.</summary>
    Task<List<EventVolunteerSignup>> ListByEventAsync(
        Guid eventId,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken ct = default);

    /// <summary>Signups due for a 24-48h reminder. Filter:
    /// <c>OccurrenceDate</c> in [today+1, today+2], <c>CanceledAt IS NULL</c>,
    /// <c>ReminderEmailSentAt IS NULL</c>.</summary>
    Task<List<EventVolunteerSignup>> ListDueForReminderAsync(DateOnly today, CancellationToken ct = default);

    Task AddAsync(EventVolunteerSignup signup, CancellationToken ct = default);
    Task UpdateAsync(EventVolunteerSignup signup, CancellationToken ct = default);
}
