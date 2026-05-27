using System.Text;
using CredoCms.Application.Calendar;
using CredoCms.Application.Events;
using CredoCms.Domain.Events;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers;

[ApiController]
[Route("calendar")]
public sealed class PublicCalendarFeedController : ControllerBase
{
    private readonly IEventRepository _events;
    private readonly IIcalFeedBuilder _ical;
    private readonly ICalendarFeedTokenService _tokens;

    public PublicCalendarFeedController(
        IEventRepository events,
        IIcalFeedBuilder ical,
        ICalendarFeedTokenService tokens)
    {
        _events = events;
        _ical = ical;
        _tokens = tokens;
    }

    /// <summary>Anonymous, public-only feed.</summary>
    [HttpGet("feed.ics")]
    public Task<IActionResult> PublicFeedAsync(CancellationToken ct)
        => BuildFeedAsync(includeMembersOnly: false, "public", ct);

    /// <summary>Per-member feed. Token is opaque; revoking re-issues a new URL.</summary>
    [HttpGet("feed/{token}.ics")]
    public async Task<IActionResult> MemberFeedAsync(string token, CancellationToken ct)
    {
        var userId = await _tokens.ResolveAsync(token, ct);
        if (userId is null) return NotFound();
        return await BuildFeedAsync(includeMembersOnly: true, $"member-{userId:N}", ct);
    }

    private async Task<IActionResult> BuildFeedAsync(bool includeMembersOnly, string label, CancellationToken ct)
    {
        var allEvents = await _events.ListPublicAsync(includeMembersOnly, ct);
        var bundle = new List<(Event, IReadOnlyList<EventRecurrenceException>, IReadOnlyList<EventOccurrenceOverride>)>();
        foreach (var e in allEvents)
        {
            var ex = await _events.GetExceptionsAsync(e.Id, ct);
            var ov = await _events.GetOverridesAsync(e.Id, ct);
            bundle.Add((e, ex, ov));
        }
        var ics = _ical.BuildFeedIcs(bundle, $"Credo Calendar ({label})");
        return File(Encoding.UTF8.GetBytes(ics), "text/calendar", $"calendar-{label}.ics");
    }
}
