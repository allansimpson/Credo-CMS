using CredoCms.Application.Caching;
using CredoCms.Application.Calendar;
using CredoCms.Domain.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace CredoCms.Api.Controllers;

[ApiController]
[Route("api/public/calendar")]
public sealed class PublicCalendarController : ControllerBase
{
    private readonly ICalendarQueryService _calendar;

    public PublicCalendarController(ICalendarQueryService calendar) => _calendar = calendar;

    [HttpGet]
    [OutputCache(PolicyName = "MembersAuthVary", Duration = 120,
        Tags = new[] { OutputCacheTags.Calendar, OutputCacheTags.Events, OutputCacheTags.News })]
    public Task<IReadOnlyList<CalendarItem>> ListAsync(
        [FromQuery] DateTimeOffset start,
        [FromQuery] DateTimeOffset end,
        CancellationToken ct = default)
    {
        if (end <= start) end = start.AddMonths(2);
        // Cap at one year to keep expansion bounded.
        if (end - start > TimeSpan.FromDays(366)) end = start.AddDays(366);

        var includeMembersOnly = User?.Identity?.IsAuthenticated == true
            && (User.IsInRole(SystemConstants.Roles.Member)
                || User.IsInRole(SystemConstants.Roles.Editor)
                || User.IsInRole(SystemConstants.Roles.Administrator));

        return _calendar.ListAsync(start, end, includeMembersOnly, ct);
    }
}
