using CredoCms.Application.Announcements;
using CredoCms.Application.Events;
using CredoCms.Application.News;
using CredoCms.Application.Sermons;
using CredoCms.Application.Services;
using CredoCms.Application.SiteSettingsManagement;

namespace CredoCms.Application.Homepage;

public sealed record HomepageDto(
    PublicSiteSettingsDto Site,
    IReadOnlyList<PublicServiceTimeDto> ServiceTimes,
    IReadOnlyList<PublicNewsItemDto> LatestNews,
    string? MembersWelcomeText,
    PublicAnnouncementBannerDto? Banner,
    // Public Site PR #2 — Home additions. Both are optional; the SPA
    // degrades to ImageSlot placeholders / empty-state copy when null or
    // empty so a fresh deployment with no content still renders the full
    // layout.
    SermonListItemDto? LatestSermon,
    IReadOnlyList<PublicEventListItemDto> UpcomingEvents);

public interface IHomepageService
{
    Task<HomepageDto> GetAsync(bool isAuthenticatedMember, CancellationToken ct = default);
}

public sealed class HomepageService : IHomepageService
{
    private readonly ISiteSettingsService _settings;
    private readonly IServiceTimeService _serviceTimes;
    private readonly INewsService _news;
    private readonly IAnnouncementBannerService _banner;
    private readonly ISiteSettingsRepository _settingsRepo;
    private readonly ISermonService _sermons;
    private readonly IEventService _events;

    public HomepageService(
        ISiteSettingsService settings,
        IServiceTimeService serviceTimes,
        INewsService news,
        IAnnouncementBannerService banner,
        ISiteSettingsRepository settingsRepo,
        ISermonService sermons,
        IEventService events)
    {
        _settings = settings;
        _serviceTimes = serviceTimes;
        _news = news;
        _banner = banner;
        _settingsRepo = settingsRepo;
        _sermons = sermons;
        _events = events;
    }

    public async Task<HomepageDto> GetAsync(bool isAuthenticatedMember, CancellationToken ct = default)
    {
        var site = await _settings.GetPublicAsync(ct).ConfigureAwait(false);
        var serviceTimes = await _serviceTimes.ListPublicAsync(ct).ConfigureAwait(false);
        var newsResults = await _news.ListPublicAsync(isAuthenticatedMember, page: 1, pageSize: 3, ct).ConfigureAwait(false);
        var banner = await _banner.GetActivePublicAsync(ct).ConfigureAwait(false);

        // Latest published sermon — used as "This Sunday" on the home page.
        // PR #2 default: most-recently-published. Members get members-only
        // sermons in the pick; anonymous visitors only see public sermons.
        var sermonResults = await _sermons.ListPublicAsync(
            new SermonListQuery(PublishedOnly: true, Page: 1, PageSize: 1),
            includeMembersOnly: isAuthenticatedMember,
            ct).ConfigureAwait(false);
        var latestSermon = sermonResults.Items.Count > 0 ? sermonResults.Items[0] : null;

        // Upcoming events — up to 4 for the home grid. The events service's
        // ListPublicAsync orders by NextOccurrenceAt asc internally.
        var eventsResults = await _events.ListPublicAsync(
            page: 1, pageSize: 4,
            includeMembersOnly: isAuthenticatedMember,
            ct).ConfigureAwait(false);

        // Members welcome text only flows down when the caller is auth'd.
        string? welcome = null;
        if (isAuthenticatedMember)
        {
            var fullSettings = await _settingsRepo.GetAsync(ct).ConfigureAwait(false);
            welcome = fullSettings.MembersWelcomeText;
        }

        return new HomepageDto(
            site, serviceTimes, newsResults.Items, welcome, banner,
            latestSermon, eventsResults.Items);
    }
}
