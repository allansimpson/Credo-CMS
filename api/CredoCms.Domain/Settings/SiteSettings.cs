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

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ModifiedAt { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    /// <summary>EF Core optimistic-concurrency token.</summary>
    public byte[] RowVersion { get; set; } = [];
}
