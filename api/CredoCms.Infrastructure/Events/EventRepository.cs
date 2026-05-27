using CredoCms.Application.Common;
using CredoCms.Application.Events;
using CredoCms.Domain.Events;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CredoCms.Infrastructure.Events;

public sealed class EventRepository : IEventRepository
{
    private readonly ApplicationDbContext _db;
    public EventRepository(ApplicationDbContext db) => _db = db;

    public async Task<PagedResult<EventListItemDto>> ListAsync(EventListQuery query, CancellationToken ct = default)
    {
        IQueryable<Event> q = query.IncludeDeleted ? _db.Events.IgnoreQueryFilters() : _db.Events;
        if (query.IncludeDeleted) q = q.Where(e => e.IsDeleted);

        if (query.Visibility is { } v) q = q.Where(e => e.Visibility == v);
        if (query.RegistrationMode is { } rm) q = q.Where(e => e.RegistrationMode == rm);
        if (query.HasRecurrence is { } hr)
            q = hr ? q.Where(e => e.RecurrenceRule != null) : q.Where(e => e.RecurrenceRule == null);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            q = q.Where(e => EF.Functions.Like(e.Title, pattern) || EF.Functions.Like(e.Slug, pattern));
        }

        var total = await q.CountAsync(ct).ConfigureAwait(false);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var items = await q
            .OrderBy(e => e.StartsAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(e => new EventListItemDto(
                e.Id, e.Slug, e.Title,
                e.StartsAt, e.EndsAt, e.AllDay,
                e.Location, e.Visibility, e.RegistrationMode,
                e.RecurrenceRule != null,
                e.IsPublished, e.IsDeleted, e.ModifiedAt))
            .ToListAsync(ct).ConfigureAwait(false);

        return new PagedResult<EventListItemDto>(items, total, page, pageSize);
    }

    public Task<Event?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default)
    {
        var q = includeDeleted ? _db.Events.IgnoreQueryFilters() : _db.Events;
        return q.FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public Task<Event?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => _db.Events.FirstOrDefaultAsync(e => e.Slug == slug, ct);

    public Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken ct = default)
    {
        var q = _db.Events.AsQueryable();
        if (excludingId is not null) q = q.Where(e => e.Id != excludingId);
        return q.AnyAsync(e => e.Slug == slug, ct);
    }

    public Task<List<EventRecurrenceException>> GetExceptionsAsync(Guid eventId, CancellationToken ct = default)
        => _db.EventRecurrenceExceptions.Where(e => e.EventId == eventId).ToListAsync(ct);

    public Task<List<EventOccurrenceOverride>> GetOverridesAsync(Guid eventId, CancellationToken ct = default)
        => _db.EventOccurrenceOverrides.Where(o => o.EventId == eventId).ToListAsync(ct);

    public async Task AddAsync(Event evt, CancellationToken ct = default)
    {
        _db.Events.Add(evt);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Event evt, CancellationToken ct = default)
    {
        _db.Events.Update(evt);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<bool> HardDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var evt = await _db.Events.IgnoreQueryFilters().FirstOrDefaultAsync(e => e.Id == id, ct).ConfigureAwait(false);
        if (evt is null) return false;
        await _db.EventRecurrenceExceptions.Where(x => x.EventId == id).ExecuteDeleteAsync(ct).ConfigureAwait(false);
        await _db.EventOccurrenceOverrides.Where(o => o.EventId == id).ExecuteDeleteAsync(ct).ConfigureAwait(false);
        _db.Events.Remove(evt);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    public async Task AddExceptionAsync(EventRecurrenceException ex, CancellationToken ct = default)
    {
        _db.EventRecurrenceExceptions.Add(ex);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task RemoveExceptionAsync(Guid id, CancellationToken ct = default)
    {
        await _db.EventRecurrenceExceptions.Where(e => e.Id == id).ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }

    public async Task UpsertOverrideAsync(EventOccurrenceOverride ov, CancellationToken ct = default)
    {
        var existing = await _db.EventOccurrenceOverrides
            .FirstOrDefaultAsync(o => o.EventId == ov.EventId
                && o.OriginalOccurrenceDate == ov.OriginalOccurrenceDate, ct).ConfigureAwait(false);
        if (existing is null)
        {
            if (ov.Id == Guid.Empty) ov.Id = Guid.NewGuid();
            _db.EventOccurrenceOverrides.Add(ov);
        }
        else
        {
            existing.OverrideStartsAt = ov.OverrideStartsAt;
            existing.OverrideEndsAt = ov.OverrideEndsAt;
            existing.OverrideLocation = ov.OverrideLocation;
            existing.OverrideDescriptionJson = ov.OverrideDescriptionJson;
            existing.IsCanceled = ov.IsCanceled;
        }
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task RemoveOverrideAsync(Guid id, CancellationToken ct = default)
    {
        await _db.EventOccurrenceOverrides.Where(o => o.Id == id).ExecuteDeleteAsync(ct).ConfigureAwait(false);
    }

    public Task<List<Event>> ListPublicAsync(bool includeMembersOnly, CancellationToken ct = default)
    {
        var q = _db.Events.Where(e => e.IsPublished);
        if (!includeMembersOnly) q = q.Where(e => e.Visibility != EventVisibility.MembersOnly);
        return q.OrderBy(e => e.StartsAt).ToListAsync(ct);
    }
}
