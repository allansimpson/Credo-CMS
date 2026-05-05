using CredoCms.Application.Calendar;
using CredoCms.Application.Events;
using CredoCms.Domain.Events;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CredoCms.Infrastructure.Calendar;

public sealed class CalendarQueryService : ICalendarQueryService
{
    private readonly ApplicationDbContext _db;
    private readonly IEventRepository _events;
    private readonly IEventOccurrenceExpander _expander;

    public CalendarQueryService(
        ApplicationDbContext db,
        IEventRepository events,
        IEventOccurrenceExpander expander)
    {
        _db = db;
        _events = events;
        _expander = expander;
    }

    public async Task<IReadOnlyList<CalendarItem>> ListAsync(
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEndExclusive,
        bool includeMembersOnly,
        CancellationToken ct = default)
    {
        var items = new List<CalendarItem>();

        // Events — expand each into concrete occurrences within the range.
        var allEvents = await _events.ListPublicAsync(includeMembersOnly, ct).ConfigureAwait(false);
        foreach (var evt in allEvents)
        {
            var exceptions = await _events.GetExceptionsAsync(evt.Id, ct).ConfigureAwait(false);
            var overrides = await _events.GetOverridesAsync(evt.Id, ct).ConfigureAwait(false);
            foreach (var occ in _expander.Expand(evt, exceptions, overrides, rangeStart, rangeEndExclusive))
            {
                items.Add(new CalendarItem(
                    EntityType: "Event",
                    EntityId: evt.Id,
                    Title: occ.Title,
                    Start: occ.StartsAt,
                    End: occ.EndsAt,
                    AllDay: occ.AllDay,
                    Url: $"/events/{evt.Slug}",
                    Location: occ.Location,
                    HeroImageUrl: evt.HeroImageUrl,
                    MembersOnly: occ.Visibility == EventVisibility.MembersOnly));
            }
        }

        // News — items with CalendarDate falling in the range.
        var news = await _db.News
            .Where(n => n.IsPublished
                && n.CalendarDate != null
                && n.CalendarDate >= rangeStart
                && n.CalendarDate < rangeEndExclusive
                && (includeMembersOnly || !n.IsMembersOnly))
            .Select(n => new
            {
                n.Id, n.Slug, n.Title, n.CalendarDate,
                n.HeroImageUrl, n.IsMembersOnly,
            })
            .ToListAsync(ct).ConfigureAwait(false);

        foreach (var n in news)
        {
            items.Add(new CalendarItem(
                EntityType: "News",
                EntityId: n.Id,
                Title: n.Title,
                Start: n.CalendarDate!.Value,
                End: null,
                AllDay: true,
                Url: $"/news/{n.Slug}",
                Location: null,
                HeroImageUrl: n.HeroImageUrl,
                MembersOnly: n.IsMembersOnly));
        }

        return items.OrderBy(i => i.Start).ToList();
    }
}
