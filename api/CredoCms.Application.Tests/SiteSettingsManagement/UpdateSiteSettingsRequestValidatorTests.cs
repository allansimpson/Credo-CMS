using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Domain.Email;
using CredoCms.Domain.Settings;
using FluentValidation.TestHelper;

namespace CredoCms.Application.Tests.SiteSettingsManagement;

public sealed class UpdateSiteSettingsRequestValidatorTests
{
    private readonly UpdateSiteSettingsRequestValidator _v = new();

    private static UpdateSiteSettingsRequest Valid() => new(
        ChurchName: "Hope Community",
        Tagline: "Welcome",
        LogoUrl: null,
        PrimaryColor: "#1e3a8a",
        AccentColor: "#f59e0b",
        ContactEmail: "info@example.org",
        ContactPhone: null,
        ContactAddress: null,
        FacebookUrl: null,
        InstagramUrl: null,
        YouTubeUrl: null,
        XUrl: null,
        TikTokUrl: null,
        OtherSocialLabel: null,
        OtherSocialUrl: null,
        FooterText: null,
        DefaultVersionRetentionCount: 20,
        LeadersPageLabel: "Our Leaders",
        LeaderCategoriesJson: "[\"Pastoral Staff\",\"Elders\"]",
        DocumentCategoriesJson: "[\"Bulletins\",\"Forms\"]",
        MaxDocumentSizeBytes: 25L * 1024 * 1024,
        MaxImageSizeBytes: 10L * 1024 * 1024,
        ImageMaxWidth: 2400,
        ImageQuality: 82,
        MembersWelcomeText: null,
        HomepageHeroCtaLabel: "Join us Sunday",
        HomepageHeroCtaLink: "#service-times",
        DefaultMetaDescription: null,
        // Phase 4 fields — defaults match SiteSettings entity defaults.
        GetInvolvedPageLabel: "Get Involved",
        ClassesPageLabel: "Classes",
        ClassAudienceAgeGroupsJson: "[\"Adults\"]",
        ShowRecentPastOnPublicClasses: false,
        RecentPastClassesLookbackDays: 30,
        BlogCategoriesJson: "[\"Devotional\"]",
        BlogPageLabel: "Blog",
        ProfanityWordlist: null,
        ProfanityAllowlist: null,
        PrayerRequestArchiveDays: 30,
        PrayerRequestRequireApproval: false,
        ConnectCardInterestsJson: "[\"Prayer\"]",
        ConnectCardAcknowledgmentMessageJson: null,
        ConnectCardPageLabel: "Connect with us",
        CloudflareTurnstileSiteKey: null,
        CloudflareTurnstileSecretKey: null,
        FacebookOAuthAppId: null,
        FacebookOAuthAppSecret: null,
        FacebookLoginEnabled: false,
        // Phase 5 fields — defaults match SiteSettings entity defaults.
        EmailProvider: EmailProvider.None,
        EmailFromAddress: "noreply@example.org",
        EmailFromName: "Church Communications",
        EmailReplyToAddress: null,
        SendGridApiKey: null,
        SendGridWebhookSecret: null,
        SmtpHost: null,
        SmtpPort: 587,
        SmtpUsername: null,
        SmtpPassword: null,
        SmtpUseSsl: true,
        EmailEnabled: false,
        TestEmailRecipient: null,
        NewsEmailTargetMode: BroadcastTargetMode.AllMembers,
        NewsEmailTargetGroupIdsJson: "[]",
        BlogEmailTargetMode: BroadcastTargetMode.AllMembers,
        BlogEmailTargetGroupIdsJson: "[]",
        EmailSubjectPrefixNews: "[News]",
        EmailSubjectPrefixBlog: "[Blog]",
        AdminNotificationFrequency: AdminNotificationFrequency.Every30Minutes,
        SmsProvider: SmsProvider.None,
        TwilioAccountSid: null,
        TwilioAuthToken: null,
        TwilioFromNumber: null,
        // Phase 6 fields — defaults match SiteSettings entity defaults.
        AnalyticsProvider: AnalyticsProvider.None,
        Ga4MeasurementId: null,
        Ga4ConsentBannerEnabled: true,
        Ga4ConsentBannerPosition: ConsentBannerPosition.BottomRight,
        CookiePolicyPageId: null,
        RowVersion: "AAAAAAAAB9E=");

    [Fact]
    public void Valid_request_passes()
    {
        _v.TestValidate(Valid()).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void ChurchName_is_required()
    {
        var result = _v.TestValidate(Valid() with { ChurchName = "" });
        result.ShouldHaveValidationErrorFor(x => x.ChurchName);
    }

    [Theory]
    [InlineData("not-hex")]
    [InlineData("#zzz")]
    [InlineData("#12345")]
    public void Bad_hex_color_fails(string bad)
    {
        var result = _v.TestValidate(Valid() with { PrimaryColor = bad });
        result.ShouldHaveValidationErrorFor(x => x.PrimaryColor);
    }

    [Theory]
    [InlineData(4)]
    [InlineData(0)]
    [InlineData(51)]
    public void Out_of_range_retention_fails(int n)
    {
        var result = _v.TestValidate(Valid() with { DefaultVersionRetentionCount = n });
        result.ShouldHaveValidationErrorFor(x => x.DefaultVersionRetentionCount);
    }

    [Theory]
    [InlineData("not-a-url")]
    [InlineData("ftp://x")]
    public void Bad_social_url_fails(string bad)
    {
        var result = _v.TestValidate(Valid() with { FacebookUrl = bad });
        result.ShouldHaveValidationErrorFor(x => x.FacebookUrl);
    }

    [Theory]
    [InlineData("")]
    [InlineData("[]")]
    [InlineData("[\"\"]")]
    [InlineData("not-json")]
    [InlineData("[1,2,3]")]
    [InlineData("{\"a\":1}")]
    public void Bad_leader_categories_json_fails(string bad)
    {
        var result = _v.TestValidate(Valid() with { LeaderCategoriesJson = bad });
        result.ShouldHaveValidationErrorFor(x => x.LeaderCategoriesJson);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(799)]
    [InlineData(5001)]
    public void Bad_image_max_width_fails(int width)
    {
        var result = _v.TestValidate(Valid() with { ImageMaxWidth = width });
        result.ShouldHaveValidationErrorFor(x => x.ImageMaxWidth);
    }

    [Theory]
    [InlineData(59)]
    [InlineData(96)]
    public void Image_quality_outside_60_to_95_fails(int q)
    {
        var result = _v.TestValidate(Valid() with { ImageQuality = q });
        result.ShouldHaveValidationErrorFor(x => x.ImageQuality);
    }
}
