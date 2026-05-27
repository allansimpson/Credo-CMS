using CredoCms.Application.Volunteers;
using CredoCms.Domain.Volunteers;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CredoCms.Infrastructure.Volunteers;

public sealed class EventVolunteerSignupRepository : IEventVolunteerSignupRepository
{
    private readonly ApplicationDbContext _db;
    public EventVolunteerSignupRepository(ApplicationDbContext db) => _db = db;

    public Task<EventVolunteerSignup?> GetAsync(Guid id, CancellationToken ct = default) =>
        _db.EventVolunteerSignups.FirstOrDefaultAsync(s => s.Id == id, ct);

    public Task<List<EventVolunteerSignup>> ListActiveForRoleOccurrenceAsync(
        Guid roleId, DateOnly occurrenceDate, CancellationToken ct = default) =>
        _db.EventVolunteerSignups
            .Where(s => s.EventVolunteerRoleId == roleId && s.OccurrenceDate == occurrenceDate && s.CanceledAt == null)
            .ToListAsync(ct);

    public Task<List<EventVolunteerSignup>> ListUpcomingForUserAsync(
        Guid userId, DateOnly fromDate, CancellationToken ct = default) =>
        _db.EventVolunteerSignups
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.CanceledAt == null && s.OccurrenceDate >= fromDate)
            .OrderBy(s => s.OccurrenceDate)
            .ToListAsync(ct);

    public Task<List<EventVolunteerSignup>> ListByEventAsync(
        Guid eventId, DateOnly? fromDate, DateOnly? toDate, CancellationToken ct = default)
    {
        var q = _db.EventVolunteerSignups.AsNoTracking().Where(s => s.EventId == eventId);
        if (fromDate is { } f) q = q.Where(s => s.OccurrenceDate >= f);
        if (toDate is { } t) q = q.Where(s => s.OccurrenceDate <= t);
        return q.OrderBy(s => s.OccurrenceDate).ToListAsync(ct);
    }

    public Task<List<EventVolunteerSignup>> ListDueForReminderAsync(DateOnly today, CancellationToken ct = default)
    {
        var lower = today.AddDays(1);
        var upper = today.AddDays(2);
        return _db.EventVolunteerSignups
            .Where(s => s.CanceledAt == null
                && s.ReminderEmailSentAt == null
                && s.OccurrenceDate >= lower && s.OccurrenceDate <= upper)
            .ToListAsync(ct);
    }

    public async Task AddAsync(EventVolunteerSignup signup, CancellationToken ct = default)
    {
        _db.EventVolunteerSignups.Add(signup);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(EventVolunteerSignup signup, CancellationToken ct = default)
    {
        _db.EventVolunteerSignups.Update(signup);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
