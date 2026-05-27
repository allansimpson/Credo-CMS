using CredoCms.Application.Events;
using CredoCms.Domain.Events;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CredoCms.Infrastructure.Events;

public sealed class EventRegistrationRepository : IEventRegistrationRepository
{
    private readonly ApplicationDbContext _db;
    public EventRegistrationRepository(ApplicationDbContext db) => _db = db;

    public Task<List<EventRegistrationField>> ListFieldsAsync(Guid eventId, CancellationToken ct = default)
        => _db.EventRegistrationFields
            .Where(f => f.EventId == eventId)
            .OrderBy(f => f.DisplayOrder)
            .ToListAsync(ct);

    public Task<EventRegistrationField?> GetFieldAsync(Guid id, CancellationToken ct = default)
        => _db.EventRegistrationFields.FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task AddFieldAsync(EventRegistrationField field, CancellationToken ct = default)
    {
        _db.EventRegistrationFields.Add(field);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateFieldAsync(EventRegistrationField field, CancellationToken ct = default)
    {
        _db.EventRegistrationFields.Update(field);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<bool> RemoveFieldAsync(Guid id, CancellationToken ct = default)
    {
        var rows = await _db.EventRegistrationFields.Where(f => f.Id == id)
            .ExecuteDeleteAsync(ct).ConfigureAwait(false);
        return rows > 0;
    }

    public Task<EventRegistration?> GetAsync(Guid id, CancellationToken ct = default)
        => _db.EventRegistrations.FirstOrDefaultAsync(r => r.Id == id, ct);

    public Task<List<EventRegistration>> ListForEventAsync(Guid eventId, EventRegistrationStatus? status, CancellationToken ct = default)
    {
        var q = _db.EventRegistrations.Where(r => r.EventId == eventId);
        if (status is { } s) q = q.Where(r => r.Status == s);
        return q.OrderBy(r => r.SubmittedAt).ToListAsync(ct);
    }

    public Task<List<EventRegistration>> ListForUserAsync(Guid userId, CancellationToken ct = default)
        => _db.EventRegistrations
            .Where(r => r.UserId == userId && r.Status != EventRegistrationStatus.Canceled)
            .OrderByDescending(r => r.SubmittedAt)
            .ToListAsync(ct);

    public Task<int> CountConfirmedAsync(Guid eventId, DateOnly? occurrenceDate, CancellationToken ct = default)
    {
        var q = _db.EventRegistrations.Where(r => r.EventId == eventId
            && r.Status == EventRegistrationStatus.Confirmed);
        if (occurrenceDate is { } d) q = q.Where(r => r.OccurrenceDate == d);
        return q.CountAsync(ct);
    }

    public Task<EventRegistration?> NextWaitlistedAsync(Guid eventId, DateOnly? occurrenceDate, CancellationToken ct = default)
    {
        var q = _db.EventRegistrations.Where(r => r.EventId == eventId
            && r.Status == EventRegistrationStatus.Waitlisted);
        if (occurrenceDate is { } d) q = q.Where(r => r.OccurrenceDate == d);
        return q.OrderBy(r => r.SubmittedAt).FirstOrDefaultAsync(ct);
    }

    public async Task AddAsync(EventRegistration registration, CancellationToken ct = default)
    {
        _db.EventRegistrations.Add(registration);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(EventRegistration registration, CancellationToken ct = default)
    {
        _db.EventRegistrations.Update(registration);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }
}
