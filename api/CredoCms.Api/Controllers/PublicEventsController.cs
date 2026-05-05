using System.Text;
using CredoCms.Application.Caching;
using CredoCms.Application.Common;
using CredoCms.Application.Events;
using CredoCms.Domain.Common;
using CredoCms.Domain.Events;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace CredoCms.Api.Controllers;

[ApiController]
[Route("api/public/events")]
public sealed class PublicEventsController : ControllerBase
{
    private readonly IEventService _events;
    private readonly IEventRegistrationService _registrations;
    private readonly IIcalFeedBuilder _ical;
    private readonly IEventRepository _eventRepo;

    public PublicEventsController(
        IEventService events,
        IEventRegistrationService registrations,
        IIcalFeedBuilder ical,
        IEventRepository eventRepo)
    {
        _events = events;
        _registrations = registrations;
        _ical = ical;
        _eventRepo = eventRepo;
    }

    [HttpGet]
    [OutputCache(PolicyName = "MembersAuthVary", Duration = 120,
        Tags = new[] { OutputCacheTags.Events })]
    public Task<PagedResult<PublicEventListItemDto>> ListAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken ct = default)
        => _events.ListPublicAsync(page, pageSize, IsAuthenticatedMember(), ct);

    [HttpGet("{slug}")]
    [OutputCache(PolicyName = "MembersAuthVary", Duration = 120,
        Tags = new[] { OutputCacheTags.Events })]
    public async Task<ActionResult<PublicEventDto>> GetAsync(string slug, CancellationToken ct)
    {
        var evt = await _events.GetPublicBySlugAsync(slug, IsAuthenticatedMember(), ct);
        return evt is null ? NotFound() : Ok(evt);
    }

    [HttpGet("{slug}/registration-fields")]
    public async Task<ActionResult<List<RegistrationFieldDto>>> ListFieldsAsync(string slug, CancellationToken ct)
    {
        var evt = await _events.GetPublicBySlugAsync(slug, IsAuthenticatedMember(), ct);
        if (evt is null) return NotFound();
        var fields = await _registrations.ListFieldsAsync(evt.Id, ct);
        return Ok(fields);
    }

    [HttpGet("{slug}/ics")]
    public async Task<IActionResult> SingleEventIcsAsync(string slug, CancellationToken ct)
    {
        var includeMembersOnly = IsAuthenticatedMember();
        var evt = await _eventRepo.GetBySlugAsync(slug, ct);
        if (evt is null || !evt.IsPublished) return NotFound();
        if (evt.Visibility == EventVisibility.MembersOnly && !includeMembersOnly) return NotFound();

        var exceptions = await _eventRepo.GetExceptionsAsync(evt.Id, ct);
        var overrides = await _eventRepo.GetOverridesAsync(evt.Id, ct);
        var ics = _ical.BuildSingleEventIcs(evt, exceptions, overrides);
        return File(Encoding.UTF8.GetBytes(ics), "text/calendar", $"{evt.Slug}.ics");
    }

    private bool IsAuthenticatedMember()
    {
        if (User?.Identity?.IsAuthenticated != true) return false;
        return User.IsInRole(SystemConstants.Roles.Member)
            || User.IsInRole(SystemConstants.Roles.Editor)
            || User.IsInRole(SystemConstants.Roles.Administrator);
    }
}
