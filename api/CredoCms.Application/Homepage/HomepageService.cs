using CredoCms.Application.Announcements;
using CredoCms.Application.News;
using CredoCms.Application.Services;
using CredoCms.Application.SiteSettingsManagement;

namespace CredoCms.Application.Homepage;

public sealed record HomepageDto(
    PublicSiteSettingsDto Site,
    IReadOnlyList<PublicServiceTimeDto> ServiceTimes,
    IReadOnlyList<PublicNewsItemDto> LatestNews,
    string? MembersWelcomeText,
    PublicAnnouncementBannerDto? Banner);

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

    public HomepageService(
        ISiteSettingsService settings,
        IServiceTimeService serviceTimes,
        INewsService news,
        IAnnouncementBannerService banner,
        ISiteSettingsRepository settingsRepo)
    {
        _settings = settings;
        _serviceTimes = serviceTimes;
        _news = news;
        _banner = banner;
        _settingsRepo = settingsRepo;
    }

    public async Task<HomepageDto> GetAsync(bool isAuthenticatedMember, CancellationToken ct = default)
    {
        var site = await _settings.GetPublicAsync(ct).ConfigureAwait(false);
        var serviceTimes = await _serviceTimes.ListPublicAsync(ct).ConfigureAwait(false);
        var newsResults = await _news.ListPublicAsync(isAuthenticatedMember, page: 1, pageSize: 2, ct).ConfigureAwait(false);
        var banner = await _banner.GetActivePublicAsync(ct).ConfigureAwait(false);

        // Members welcome text only flows down when the caller is auth'd.
        string? welcome = null;
        if (isAuthenticatedMember)
        {
            var fullSettings = await _settingsRepo.GetAsync(ct).ConfigureAwait(false);
            welcome = fullSettings.MembersWelcomeText;
        }

        return new HomepageDto(site, serviceTimes, newsResults.Items, welcome, banner);
    }
}
