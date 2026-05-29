using System.ComponentModel.DataAnnotations;
using CredoCms.Domain.Common;

namespace CredoCms.Domain.Pages;

/// <summary>
/// A standalone content page rendered at <c>/{slug}</c>. Body is stored as
/// ProseMirror JSON (the editor is TipTap; the storage shape is unchanged).
///
/// System pages (Privacy, Terms, etc.) are seeded with <see cref="IsSystemPage"/>
/// = true. The service layer prevents hard-deleting system pages and forbids
/// changing their slug.
/// </summary>
public sealed class Page : IVersionedEntity
{
    public Guid Id { get; set; }

    /// <summary>URL slug. Lower-case, dash-separated.</summary>
    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>ProseMirror JSON serialized as a string.</summary>
    [Required]
    public string BodyJson { get; set; } = string.Empty;

    /// <summary>Auto-generated short summary used for listings + meta description fallback.</summary>
    [MaxLength(500)]
    public string? Excerpt { get; set; }

    [MaxLength(2000)]
    public string? HeroImageUrl { get; set; }

    [MaxLength(2000)]
    public string? HeroImageWebpUrl { get; set; }

    [MaxLength(300)]
    public string? HeroImageAlt { get; set; }

    [MaxLength(300)]
    public string? MetaDescription { get; set; }

    public bool IsPublished { get; set; }

    public bool IsMembersOnly { get; set; }

    public bool IsDeleted { get; set; }

    public bool IsSystemPage { get; set; }

    public PageTemplate Template { get; set; } = PageTemplate.Standard;

    // ── Draft / publish workflow ─────────────────────────────────────────
    // When HasUnpublishedDraft is true, the Draft* columns hold work-in-
    // progress edits that have not yet been promoted to the live page.
    // The public endpoint always reads the non-Draft columns, so visitors
    // see the last published version until Publish copies draft → live.

    public bool HasUnpublishedDraft { get; set; }

    public string? DraftTitle { get; set; }
    public string? DraftBodyJson { get; set; }
    [MaxLength(500)] public string? DraftExcerpt { get; set; }
    [MaxLength(2000)] public string? DraftHeroImageUrl { get; set; }
    [MaxLength(2000)] public string? DraftHeroImageWebpUrl { get; set; }
    [MaxLength(300)] public string? DraftHeroImageAlt { get; set; }
    [MaxLength(300)] public string? DraftMetaDescription { get; set; }
    public bool? DraftIsMembersOnly { get; set; }
    public PageTemplate? DraftTemplate { get; set; }
    public DateTimeOffset? DraftSavedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    public DateTimeOffset ModifiedAt { get; set; }

    public DateTimeOffset? PublishedAt { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }
}
