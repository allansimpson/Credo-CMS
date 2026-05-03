using CredoCms.Application.Common;
using CredoCms.Domain.Settings;

namespace CredoCms.Application.SiteSettingsManagement;

public interface ISiteSettingsService
{
    Task<PublicSiteSettingsDto> GetPublicAsync(CancellationToken ct = default);
    Task<SiteSettingsDto> GetAsync(CancellationToken ct = default);
    Task<SiteSettingsDto> UpdateAsync(UpdateSiteSettingsRequest request, CancellationToken ct = default);
}

public sealed class SiteSettingsService : ISiteSettingsService
{
    private readonly ISiteSettingsRepository _repo;
    private readonly IAuditLogger _audit;

    public SiteSettingsService(ISiteSettingsRepository repo, IAuditLogger audit)
    {
        _repo = repo;
        _audit = audit;
    }

    public async Task<PublicSiteSettingsDto> GetPublicAsync(CancellationToken ct = default)
    {
        var s = await _repo.GetAsync(ct);
        return new PublicSiteSettingsDto(
            s.ChurchName, s.Tagline, s.LogoUrl, s.PrimaryColor, s.AccentColor,
            s.ContactEmail, s.ContactPhone, s.ContactAddress,
            s.FacebookUrl, s.InstagramUrl, s.YouTubeUrl, s.XUrl, s.TikTokUrl,
            s.OtherSocialLabel, s.OtherSocialUrl, s.FooterText,
            s.LeadersPageLabel, s.HomepageHeroCtaLabel, s.HomepageHeroCtaLink);
    }

    public async Task<SiteSettingsDto> GetAsync(CancellationToken ct = default)
    {
        var s = await _repo.GetAsync(ct);
        return ToDto(s);
    }

    public async Task<SiteSettingsDto> UpdateAsync(UpdateSiteSettingsRequest request, CancellationToken ct = default)
    {
        var s = await _repo.GetAsync(ct);

        s.ChurchName = request.ChurchName;
        s.Tagline = request.Tagline;
        s.LogoUrl = request.LogoUrl;
        s.PrimaryColor = request.PrimaryColor;
        s.AccentColor = request.AccentColor;
        s.ContactEmail = request.ContactEmail;
        s.ContactPhone = request.ContactPhone;
        s.ContactAddress = request.ContactAddress;
        s.FacebookUrl = request.FacebookUrl;
        s.InstagramUrl = request.InstagramUrl;
        s.YouTubeUrl = request.YouTubeUrl;
        s.XUrl = request.XUrl;
        s.TikTokUrl = request.TikTokUrl;
        s.OtherSocialLabel = request.OtherSocialLabel;
        s.OtherSocialUrl = request.OtherSocialUrl;
        s.FooterText = request.FooterText;
        s.DefaultVersionRetentionCount = request.DefaultVersionRetentionCount;

        // Phase 2 fields.
        s.LeadersPageLabel = request.LeadersPageLabel;
        s.LeaderCategoriesJson = request.LeaderCategoriesJson;
        s.DocumentCategoriesJson = request.DocumentCategoriesJson;
        s.MaxDocumentSizeBytes = request.MaxDocumentSizeBytes;
        s.MaxImageSizeBytes = request.MaxImageSizeBytes;
        s.ImageMaxWidth = request.ImageMaxWidth;
        s.ImageQuality = request.ImageQuality;
        s.MembersWelcomeText = request.MembersWelcomeText;
        s.HomepageHeroCtaLabel = request.HomepageHeroCtaLabel;
        s.HomepageHeroCtaLink = request.HomepageHeroCtaLink;
        s.DefaultMetaDescription = request.DefaultMetaDescription;

        s.ModifiedAt = DateTimeOffset.UtcNow;
        s.RowVersion = Convert.FromBase64String(request.RowVersion);

        await _repo.UpdateAsync(s, ct);

        await _audit.WriteAsync(
            "SiteSettings.Updated",
            nameof(SiteSettings),
            s.Id.ToString(),
            details: new
            {
                s.ChurchName,
                s.PrimaryColor,
                s.AccentColor,
                s.DefaultVersionRetentionCount,
                s.LeadersPageLabel,
                s.ImageMaxWidth,
                s.ImageQuality,
                s.MaxDocumentSizeBytes,
            },
            cancellationToken: ct);

        return ToDto(s);
    }

    private static SiteSettingsDto ToDto(SiteSettings s) => new(
        s.ChurchName, s.Tagline, s.LogoUrl, s.PrimaryColor, s.AccentColor,
        s.ContactEmail, s.ContactPhone, s.ContactAddress,
        s.FacebookUrl, s.InstagramUrl, s.YouTubeUrl, s.XUrl, s.TikTokUrl,
        s.OtherSocialLabel, s.OtherSocialUrl, s.FooterText,
        s.DefaultVersionRetentionCount,
        s.LeadersPageLabel, s.LeaderCategoriesJson, s.DocumentCategoriesJson,
        s.MaxDocumentSizeBytes, s.MaxImageSizeBytes,
        s.ImageMaxWidth, s.ImageQuality,
        s.MembersWelcomeText,
        s.HomepageHeroCtaLabel, s.HomepageHeroCtaLink,
        s.DefaultMetaDescription,
        s.CreatedAt, s.ModifiedAt, s.ModifiedByUserId,
        Convert.ToBase64String(s.RowVersion));
}
