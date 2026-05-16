using System.ComponentModel.DataAnnotations;

namespace CredoCms.Domain.Settings;

/// <summary>
/// Single-row table holding all per-church configuration. The row's primary key is
/// fixed at <see cref="Common.SystemConstants.SiteSettingsId"/> so the application
/// can always read it by a known id without searching.
/// </summary>
public class SiteSettings
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string ChurchName { get; set; } = string.Empty;

    [MaxLength(300)]
    public string? Tagline { get; set; }

    [MaxLength(2000)]
    public string? LogoUrl { get; set; }

    /// <summary>Hex colour, e.g. "#1e3a8a".</summary>
    [Required]
    [MaxLength(9)]
    public string PrimaryColor { get; set; } = "#1e3a8a";

    /// <summary>Hex colour, e.g. "#f59e0b".</summary>
    [Required]
    [MaxLength(9)]
    public string AccentColor { get; set; } = "#f59e0b";

    [MaxLength(254)]
    public string? ContactEmail { get; set; }

    [MaxLength(50)]
    public string? ContactPhone { get; set; }

    [MaxLength(500)]
    public string? ContactAddress { get; set; }

    [MaxLength(500)]
    public string? FacebookUrl { get; set; }

    [MaxLength(500)]
    public string? InstagramUrl { get; set; }

    [MaxLength(500)]
    public string? YouTubeUrl { get; set; }

    [MaxLength(500)]
    public string? XUrl { get; set; }

    [MaxLength(500)]
    public string? TikTokUrl { get; set; }

    [MaxLength(50)]
    public string? OtherSocialLabel { get; set; }

    [MaxLength(500)]
    public string? OtherSocialUrl { get; set; }

    [MaxLength(500)]
    public string? FooterText { get; set; }

    /// <summary>
    /// Default count-based retention applied to versioned entities unless overridden
    /// per-entity. Range is enforced server-side: 5–50 inclusive.
    /// </summary>
    public int DefaultVersionRetentionCount { get; set; } = 20;

    // ---- Phase 2 additions ------------------------------------------------

    /// <summary>Public label for the Leaders page (e.g. "Our Leaders", "Elders").</summary>
    [Required]
    [MaxLength(100)]
    public string LeadersPageLabel { get; set; } = "Our Leaders";

    /// <summary>JSON-encoded list of leader category names. Drives the admin
    /// category dropdown and the public grouping on /leaders.</summary>
    [Required]
    public string LeaderCategoriesJson { get; set; } =
        "[\"Pastoral Staff\",\"Elders\",\"Deacons\",\"Ministry Directors\"]";

    /// <summary>JSON-encoded list of document category names.</summary>
    [Required]
    public string DocumentCategoriesJson { get; set; } =
        "[\"Bulletins\",\"Forms\",\"Policies\",\"Board Minutes\",\"Resources\"]";

    /// <summary>Maximum size in bytes for an uploaded PDF (default 25 MB).</summary>
    public long MaxDocumentSizeBytes { get; set; } = 25L * 1024 * 1024;

    /// <summary>Maximum size in bytes for an uploaded image before compression
    /// (default 10 MB).</summary>
    public long MaxImageSizeBytes { get; set; } = 10L * 1024 * 1024;

    /// <summary>If wider than this, images are resized down on upload.</summary>
    public int ImageMaxWidth { get; set; } = 2400;

    /// <summary>JPEG / WebP quality for the optimized variant (60–95).</summary>
    public int ImageQuality { get; set; } = 82;

    /// <summary>Optional ProseMirror JSON shown in the homepage members-only
    /// welcome block; rendered only to authenticated Member+ viewers.</summary>
    public string? MembersWelcomeText { get; set; }

    [MaxLength(100)]
    public string HomepageHeroCtaLabel { get; set; } = "Join us Sunday";

    [MaxLength(500)]
    public string HomepageHeroCtaLink { get; set; } = "#service-times";

    /// <summary>Default meta-description used when an entity-level override
    /// and excerpt are both absent. Important for SEO consistency.</summary>
    [MaxLength(300)]
    public string? DefaultMetaDescription { get; set; }

    // ---- Phase 3: YouTube integration -------------------------------------
    // Stored plain in DB, masked in admin UI per BUILD_PLAN Q-2 #5.
    // Data Protection encrypt-at-rest queued in PHASE_3_BACKLOG.md.

    [MaxLength(50)]
    public string? YouTubeChannelId { get; set; }

    [MaxLength(200)]
    public string? YouTubeApiKey { get; set; }

    [MaxLength(500)]
    public string? YouTubeOAuthRefreshToken { get; set; }

    public bool YouTubeSyncEnabled { get; set; }

    public int YouTubeSyncIntervalMinutes { get; set; } = 360;

    public bool YouTubeAutoPublishOnSync { get; set; }

    /// <summary>JSON array of default tag names applied to every auto-imported sermon.</summary>
    public string YouTubeDefaultTagsJson { get; set; } = "[]";

    public DateTimeOffset? YouTubeLastSuccessfulSyncAt { get; set; }

    [MaxLength(500)]
    public string? YouTubeLastSyncStatus { get; set; }

    public int? YouTubeLastSyncImportedCount { get; set; }

    // ---- Phase 4: Members + Community -------------------------------------

    [Required]
    [MaxLength(100)]
    public string GetInvolvedPageLabel { get; set; } = "Get Involved";

    [Required]
    [MaxLength(100)]
    public string ClassesPageLabel { get; set; } = "Classes";

    /// <summary>JSON list of audience age groups for ClassSlots.</summary>
    [Required]
    public string ClassAudienceAgeGroupsJson { get; set; } =
        "[\"Nursery (0-2)\",\"Preschool (3-5)\",\"Elementary (K-5)\",\"Middle School\",\"High School\",\"Young Adults\",\"Adults\",\"Seniors\",\"All Ages\"]";

    public bool ShowRecentPastOnPublicClasses { get; set; }

    public int RecentPastClassesLookbackDays { get; set; } = 30;

    /// <summary>JSON list of Blog category names.</summary>
    [Required]
    public string BlogCategoriesJson { get; set; } =
        "[\"Devotional\",\"Sermon Notes\",\"Missions\",\"Pastor's Reflections\",\"Announcements\"]";

    [Required]
    [MaxLength(100)]
    public string BlogPageLabel { get; set; } = "Blog";

    /// <summary>Newline-delimited custom profanity wordlist merged on top of the
    /// built-in <c>ProfanityFilter</c> wordlist.</summary>
    public string? ProfanityWordlist { get; set; }

    /// <summary>Newline-delimited allowlist that suppresses matches in the merged
    /// wordlist (false-positive recovery).</summary>
    public string? ProfanityAllowlist { get; set; }

    public int PrayerRequestArchiveDays { get; set; } = 30;

    /// <summary>Captured for future use; Phase 4 keeps direct posting.</summary>
    public bool PrayerRequestRequireApproval { get; set; }

    /// <summary>JSON list of interest checkbox labels for the Connect Card form.</summary>
    [Required]
    public string ConnectCardInterestsJson { get; set; } =
        "[\"Becoming a member\",\"Children's programs\",\"Youth programs\",\"Bible studies / classes\",\"Volunteering\",\"Prayer\",\"Speaking with a pastor\"]";

    /// <summary>ProseMirror JSON for the connect-card acknowledgment email body.</summary>
    public string? ConnectCardAcknowledgmentMessageJson { get; set; }

    [Required]
    [MaxLength(100)]
    public string ConnectCardPageLabel { get; set; } = "Connect with us";

    [MaxLength(200)]
    public string? CloudflareTurnstileSiteKey { get; set; }

    [MaxLength(200)]
    public string? CloudflareTurnstileSecretKey { get; set; }

    [MaxLength(200)]
    public string? FacebookOAuthAppId { get; set; }

    [MaxLength(200)]
    public string? FacebookOAuthAppSecret { get; set; }

    public bool FacebookLoginEnabled { get; set; }

    // -- Phase 5: Communications -------------------------------------------

    /// <summary>Provider selection for outbound email. <c>None</c> forces
    /// <see cref="EmailEnabled"/>=false regardless of UI state.</summary>
    public Email.EmailProvider EmailProvider { get; set; } = Email.EmailProvider.None;

    [MaxLength(200)]
    public string EmailFromAddress { get; set; } = "noreply@example.org";

    [MaxLength(200)]
    public string EmailFromName { get; set; } = "Church Communications";

    [MaxLength(200)]
    public string? EmailReplyToAddress { get; set; }

    [MaxLength(200)]
    public string? SendGridApiKey { get; set; }

    [MaxLength(200)]
    public string? SendGridWebhookSecret { get; set; }

    [MaxLength(200)]
    public string? SmtpHost { get; set; }

    public int SmtpPort { get; set; } = 587;

    [MaxLength(200)]
    public string? SmtpUsername { get; set; }

    [MaxLength(500)]
    public string? SmtpPassword { get; set; }

    public bool SmtpUseSsl { get; set; } = true;

    /// <summary>Master kill switch. When false, all <c>IEmailService</c> calls
    /// log + return successfully without dispatching. Defaults to false so
    /// initial deployments cannot accidentally email storms.</summary>
    public bool EmailEnabled { get; set; }

    /// <summary>Staging override: when set, ALL outbound email goes here
    /// regardless of intended recipient. Cleared in production.</summary>
    [MaxLength(200)]
    public string? TestEmailRecipient { get; set; }

    public Email.BroadcastTargetMode NewsEmailTargetMode { get; set; } = Email.BroadcastTargetMode.AllMembers;

    /// <summary>JSON array of Group GUIDs. Used when
    /// <see cref="NewsEmailTargetMode"/>=<c>SpecificGroups</c>.</summary>
    public string NewsEmailTargetGroupIdsJson { get; set; } = "[]";

    public Email.BroadcastTargetMode BlogEmailTargetMode { get; set; } = Email.BroadcastTargetMode.AllMembers;

    public string BlogEmailTargetGroupIdsJson { get; set; } = "[]";

    [MaxLength(50)]
    public string EmailSubjectPrefixNews { get; set; } = "[News]";

    [MaxLength(50)]
    public string EmailSubjectPrefixBlog { get; set; } = "[Blog]";

    /// <summary>Default frequency for admin-notification digests. Per-user
    /// override on <c>ApplicationUser</c> beats this.</summary>
    public Email.AdminNotificationFrequency AdminNotificationFrequency { get; set; } = Email.AdminNotificationFrequency.Every30Minutes;

    /// <summary>HMAC key for signing one-click unsubscribe tokens.
    /// Auto-generated on first read if blank.</summary>
    [MaxLength(200)]
    public string? UnsubscribeSigningKey { get; set; }

    public Email.SmsProvider SmsProvider { get; set; } = Email.SmsProvider.None;

    [MaxLength(200)]
    public string? TwilioAccountSid { get; set; }

    [MaxLength(500)]
    public string? TwilioAuthToken { get; set; }

    [MaxLength(50)]
    public string? TwilioFromNumber { get; set; }

    // -- Phase 6: Analytics + cookie consent ------------------------------

    /// <summary>Selects the analytics provider. <c>Ga4</c> triggers the
    /// SPA's cookie consent banner; tracking only loads after Accept.</summary>
    public AnalyticsProvider AnalyticsProvider { get; set; } = AnalyticsProvider.None;

    /// <summary>GA4 measurement ID, format "G-XXXXXXXXXX". Only used when
    /// <see cref="AnalyticsProvider"/> is <see cref="AnalyticsProvider.Ga4"/>.</summary>
    [MaxLength(50)]
    public string? Ga4MeasurementId { get; set; }

    /// <summary>Whether to render the cookie consent banner. When the
    /// provider is <see cref="AnalyticsProvider.Ga4"/> and this is false,
    /// tracking is silently disabled (forces consent dialog suppression).
    /// Default true.</summary>
    public bool Ga4ConsentBannerEnabled { get; set; } = true;

    /// <summary>Where the consent banner appears.</summary>
    public ConsentBannerPosition Ga4ConsentBannerPosition { get; set; } = ConsentBannerPosition.BottomRight;

    /// <summary>Optional cookie-policy page link. The banner copy renders
    /// "See our Cookie Policy" only when set. Resolved to a slug
    /// server-side before public exposure.</summary>
    public Guid? CookiePolicyPageId { get; set; }

    // -- Public site template (Public Site design handoff) ----------------

    /// <summary>Selects the public-facing visual template. Content
    /// shape is identical across templates; only visual treatment
    /// differs. The SPA reads this from the public bootstrap and sets
    /// <c>data-template</c> on the church theme root.</summary>
    public PublicTemplate Template { get; set; } = PublicTemplate.Editorial;

    // -----------------------------------------------------------------------

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ModifiedAt { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    /// <summary>EF Core optimistic-concurrency token.</summary>
    public byte[] RowVersion { get; set; } = [];
}
