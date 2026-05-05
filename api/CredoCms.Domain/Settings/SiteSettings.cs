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

    // -----------------------------------------------------------------------

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ModifiedAt { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    /// <summary>EF Core optimistic-concurrency token.</summary>
    public byte[] RowVersion { get; set; } = [];
}
