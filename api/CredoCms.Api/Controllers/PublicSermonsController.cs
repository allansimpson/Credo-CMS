using CredoCms.Application.Common;
using CredoCms.Application.Sermons;
using CredoCms.Domain.Bible;
using CredoCms.Domain.Common;
using CredoCms.Domain.Sermons;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers;

[ApiController]
[Route("api/public/sermons")]
public sealed class PublicSermonsController : ControllerBase
{
    private readonly ISermonService _svc;
    public PublicSermonsController(ISermonService svc) => _svc = svc;

    [HttpGet]
    public Task<PagedResult<SermonListItemDto>> ListAsync(
        [FromQuery] string? search,
        [FromQuery] Guid? sermonSeriesId,
        [FromQuery] string? tagSlug,
        [FromQuery] Guid? speakerLeaderId,
        [FromQuery] int? bookFilter,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 12,
        CancellationToken ct = default)
    {
        var query = new SermonListQuery(
            Search: search,
            SermonSeriesId: sermonSeriesId,
            TagSlug: tagSlug,
            SpeakerLeaderId: speakerLeaderId,
            BookFilter: bookFilter,
            PublishedOnly: true,
            Page: page,
            PageSize: pageSize);
        return _svc.ListPublicAsync(query, IsAuthenticatedMember(), ct);
    }

    [HttpGet("{slug}")]
    public async Task<ActionResult<PublicSermonDto>> GetAsync(string slug, CancellationToken ct)
    {
        var sermon = await _svc.GetPublicBySlugAsync(slug, IsAuthenticatedMember(), ct);
        return sermon is null ? NotFound() : Ok(sermon);
    }

    public sealed record BookCount(int BookValue, string Slug, string Name, string Testament, int Count);

    [HttpGet("by-book")]
    public async Task<List<BookCount>> ListByBookAsync(CancellationToken ct)
    {
        var counts = await _svc.CountByBookAsync(IsAuthenticatedMember(), ct);
        var byBook = counts.ToDictionary(c => c.BookValue, c => c.Count);
        return BibleBooks.All.Select(b => new BookCount(
            (int)b.Book, b.Slug, b.Name, b.Testament.ToString(),
            byBook.TryGetValue((int)b.Book, out var n) ? n : 0)).ToList();
    }

    [HttpGet("by-book/{bookSlug}")]
    public Task<PagedResult<SermonListItemDto>> ListBookAsync(
        string bookSlug,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken ct = default)
    {
        var info = BibleBooks.FindBySlug(bookSlug);
        if (info is null)
            return Task.FromResult(new PagedResult<SermonListItemDto>(Array.Empty<SermonListItemDto>(), 0, page, pageSize));
        var query = new SermonListQuery(
            BookFilter: (int)info.Book, PublishedOnly: true, Page: page, PageSize: pageSize);
        return _svc.ListPublicAsync(query, IsAuthenticatedMember(), ct);
    }

    [HttpGet("by-day")]
    public Task<SermonsByDayResponse> ListByDayAsync(
        [FromQuery] string? search,
        [FromQuery] string? tagSlug,
        [FromQuery] ServiceType? serviceType,
        [FromQuery] int? year,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var query = new SermonsByDayQuery(
            Search: search,
            TagSlug: tagSlug,
            ServiceType: serviceType,
            Year: year,
            Page: page,
            PageSize: pageSize);
        return _svc.ListPublicByDayAsync(query, IsAuthenticatedMember(), ct);
    }

    /// <summary>
    /// Year + month rollup that drives the public sermons archive's side-rail.
    /// Counts are viewer-scoped — anonymous visitors get totals that exclude
    /// members-only sermons.
    /// </summary>
    [HttpGet("years")]
    public Task<YearsResponse> ListYearsAsync(CancellationToken ct = default)
        => _svc.ListYearStatsAsync(IsAuthenticatedMember(), ct);

    private bool IsAuthenticatedMember()
    {
        if (User?.Identity?.IsAuthenticated != true) return false;
        return User.IsInRole(SystemConstants.Roles.Member)
            || User.IsInRole(SystemConstants.Roles.Editor)
            || User.IsInRole(SystemConstants.Roles.Administrator);
    }
}
