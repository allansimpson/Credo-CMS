using CredoCms.Domain.Pages;

namespace CredoCms.Application.Pages;

public sealed record PageListItemDto(
    Guid Id,
    string Slug,
    string Title,
    string? Excerpt,
    bool IsPublished,
    bool IsMembersOnly,
    bool IsSystemPage,
    PageTemplate Template,
    DateTimeOffset ModifiedAt,
    Guid? ModifiedByUserId);

public sealed record PageDetailDto(
    Guid Id,
    string Slug,
    string Title,
    string BodyJson,
    string? Excerpt,
    string? HeroImageUrl,
    string? HeroImageWebpUrl,
    string? HeroImageAlt,
    string? MetaDescription,
    bool IsPublished,
    bool IsMembersOnly,
    bool IsDeleted,
    bool IsSystemPage,
    PageTemplate Template,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
    Guid? ModifiedByUserId,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? DeletedAt,
    bool HasUnpublishedDraft,
    PageDraftDto? Draft);

/// <summary>Pending edits on top of a published page. Null when no draft
/// exists. Each property mirrors the live-content field that it overrides
/// when Publish is invoked.</summary>
public sealed record PageDraftDto(
    string Title,
    string BodyJson,
    string? Excerpt,
    string? HeroImageUrl,
    string? HeroImageWebpUrl,
    string? HeroImageAlt,
    string? MetaDescription,
    bool IsMembersOnly,
    PageTemplate Template,
    DateTimeOffset SavedAt);

/// <summary>Public-facing payload, used by the SPA's /{slug} route.</summary>
public sealed record PublicPageDto(
    Guid Id,
    string Slug,
    string Title,
    string BodyJson,
    string? Excerpt,
    string? HeroImageUrl,
    string? HeroImageWebpUrl,
    string? HeroImageAlt,
    string? MetaDescription,
    bool IsMembersOnly,
    PageTemplate Template,
    DateTimeOffset PublishedAt);

public sealed record CreatePageRequest(
    string Slug,
    string Title,
    string BodyJson,
    string? Excerpt,
    string? HeroImageUrl,
    string? HeroImageWebpUrl,
    string? HeroImageAlt,
    string? MetaDescription,
    bool IsPublished,
    bool IsMembersOnly,
    PageTemplate Template = PageTemplate.Standard);

public sealed record UpdatePageRequest(
    string Slug,
    string Title,
    string BodyJson,
    string? Excerpt,
    string? HeroImageUrl,
    string? HeroImageWebpUrl,
    string? HeroImageAlt,
    string? MetaDescription,
    bool IsPublished,
    bool IsMembersOnly,
    PageTemplate Template = PageTemplate.Standard);

public sealed record PageListQuery(
    string? Search = null,
    bool IncludeDeleted = false,
    int Page = 1,
    int PageSize = 25);
