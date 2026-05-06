using CredoCms.Application.Common;
using CredoCms.Application.Pages;
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
    private readonly IPageRepository? _pages;

    public SiteSettingsService(ISiteSettingsRepository repo, IAuditLogger audit, IPageRepository? pages = null)
    {
        _repo = repo;
        _audit = audit;
        _pages = pages;
    }

    public async Task<PublicSiteSettingsDto> GetPublicAsync(CancellationToken ct = default)
    {
        var s = await _repo.GetAsync(ct);

        // Resolve cookie-policy page slug server-side so the SPA's banner
        // gets a ready link instead of a Page id.
        string? cookiePolicySlug = null;
        if (s.CookiePolicyPageId is { } pid && _pages is not null)
        {
            var page = await _pages.GetByIdAsync(pid, includeDeleted: false, ct).ConfigureAwait(false);
            cookiePolicySlug = page?.IsPublished == true ? page.Slug : null;
        }

        return new PublicSiteSettingsDto(
            s.ChurchName, s.Tagline, s.LogoUrl, s.PrimaryColor, s.AccentColor,
            s.ContactEmail, s.ContactPhone, s.ContactAddress,
            s.FacebookUrl, s.InstagramUrl, s.YouTubeUrl, s.XUrl, s.TikTokUrl,
            s.OtherSocialLabel, s.OtherSocialUrl, s.FooterText,
            s.LeadersPageLabel, s.HomepageHeroCtaLabel, s.HomepageHeroCtaLink,
            s.FacebookLoginEnabled,
            // Phase 6
            s.AnalyticsProvider, s.Ga4MeasurementId,
            s.Ga4ConsentBannerEnabled, s.Ga4ConsentBannerPosition,
            cookiePolicySlug);
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

        // Phase 4 fields.
        s.GetInvolvedPageLabel = request.GetInvolvedPageLabel;
        s.ClassesPageLabel = request.ClassesPageLabel;
        s.ClassAudienceAgeGroupsJson = request.ClassAudienceAgeGroupsJson;
        s.ShowRecentPastOnPublicClasses = request.ShowRecentPastOnPublicClasses;
        s.RecentPastClassesLookbackDays = request.RecentPastClassesLookbackDays;
        s.BlogCategoriesJson = request.BlogCategoriesJson;
        s.BlogPageLabel = request.BlogPageLabel;
        s.ProfanityWordlist = request.ProfanityWordlist;
        s.ProfanityAllowlist = request.ProfanityAllowlist;
        s.PrayerRequestArchiveDays = request.PrayerRequestArchiveDays;
        s.PrayerRequestRequireApproval = request.PrayerRequestRequireApproval;
        s.ConnectCardInterestsJson = request.ConnectCardInterestsJson;
        s.ConnectCardAcknowledgmentMessageJson = request.ConnectCardAcknowledgmentMessageJson;
        s.ConnectCardPageLabel = request.ConnectCardPageLabel;
        s.CloudflareTurnstileSiteKey = request.CloudflareTurnstileSiteKey;
        s.CloudflareTurnstileSecretKey = request.CloudflareTurnstileSecretKey;
        s.FacebookOAuthAppId = request.FacebookOAuthAppId;
        s.FacebookOAuthAppSecret = request.FacebookOAuthAppSecret;
        s.FacebookLoginEnabled = request.FacebookLoginEnabled;

        // Phase 5
        s.EmailProvider = request.EmailProvider;
        s.EmailFromAddress = request.EmailFromAddress;
        s.EmailFromName = request.EmailFromName;
        s.EmailReplyToAddress = request.EmailReplyToAddress;
        s.SendGridApiKey = request.SendGridApiKey;
        s.SendGridWebhookSecret = request.SendGridWebhookSecret;
        s.SmtpHost = request.SmtpHost;
        s.SmtpPort = request.SmtpPort;
        s.SmtpUsername = request.SmtpUsername;
        s.SmtpPassword = request.SmtpPassword;
        s.SmtpUseSsl = request.SmtpUseSsl;
        // EmailProvider=None forces EmailEnabled=false regardless of UI state.
        s.EmailEnabled = request.EmailProvider != Domain.Email.EmailProvider.None && request.EmailEnabled;
        s.TestEmailRecipient = request.TestEmailRecipient;
        s.NewsEmailTargetMode = request.NewsEmailTargetMode;
        s.NewsEmailTargetGroupIdsJson = request.NewsEmailTargetGroupIdsJson;
        s.BlogEmailTargetMode = request.BlogEmailTargetMode;
        s.BlogEmailTargetGroupIdsJson = request.BlogEmailTargetGroupIdsJson;
        s.EmailSubjectPrefixNews = request.EmailSubjectPrefixNews;
        s.EmailSubjectPrefixBlog = request.EmailSubjectPrefixBlog;
        s.AdminNotificationFrequency = request.AdminNotificationFrequency;
        s.SmsProvider = request.SmsProvider;
        s.TwilioAccountSid = request.TwilioAccountSid;
        s.TwilioAuthToken = request.TwilioAuthToken;
        s.TwilioFromNumber = request.TwilioFromNumber;

        // Phase 6
        s.AnalyticsProvider = request.AnalyticsProvider;
        s.Ga4MeasurementId = request.Ga4MeasurementId;
        s.Ga4ConsentBannerEnabled = request.Ga4ConsentBannerEnabled;
        s.Ga4ConsentBannerPosition = request.Ga4ConsentBannerPosition;
        s.CookiePolicyPageId = request.CookiePolicyPageId;

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
        s.GetInvolvedPageLabel, s.ClassesPageLabel, s.ClassAudienceAgeGroupsJson,
        s.ShowRecentPastOnPublicClasses, s.RecentPastClassesLookbackDays,
        s.BlogCategoriesJson, s.BlogPageLabel,
        s.ProfanityWordlist, s.ProfanityAllowlist,
        s.PrayerRequestArchiveDays, s.PrayerRequestRequireApproval,
        s.ConnectCardInterestsJson, s.ConnectCardAcknowledgmentMessageJson,
        s.ConnectCardPageLabel,
        s.CloudflareTurnstileSiteKey, s.CloudflareTurnstileSecretKey,
        s.FacebookOAuthAppId, s.FacebookOAuthAppSecret,
        s.FacebookLoginEnabled,
        // Phase 5
        s.EmailProvider, s.EmailFromAddress, s.EmailFromName, s.EmailReplyToAddress,
        s.SendGridApiKey, s.SendGridWebhookSecret,
        s.SmtpHost, s.SmtpPort, s.SmtpUsername, s.SmtpPassword, s.SmtpUseSsl,
        s.EmailEnabled, s.TestEmailRecipient,
        s.NewsEmailTargetMode, s.NewsEmailTargetGroupIdsJson,
        s.BlogEmailTargetMode, s.BlogEmailTargetGroupIdsJson,
        s.EmailSubjectPrefixNews, s.EmailSubjectPrefixBlog,
        s.AdminNotificationFrequency,
        s.SmsProvider, s.TwilioAccountSid, s.TwilioAuthToken, s.TwilioFromNumber,
        // Phase 6
        s.AnalyticsProvider, s.Ga4MeasurementId,
        s.Ga4ConsentBannerEnabled, s.Ga4ConsentBannerPosition, s.CookiePolicyPageId,
        s.CreatedAt, s.ModifiedAt, s.ModifiedByUserId,
        Convert.ToBase64String(s.RowVersion));
}
