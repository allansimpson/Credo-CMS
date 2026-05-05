namespace CredoCms.Application.Pages;

public sealed record PageListItemDto(
    Guid Id,
    string Slug,
    string Title,
    string? Excerpt,
    bool IsPublished,
    bool IsMembersOnly,
    bool IsSystemPage,
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
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
    Guid? ModifiedByUserId,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? DeletedAt);

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
    bool IsMembersOnly);

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
    bool IsMembersOnly);

public sealed record PageListQuery(
    string? Search = null,
    bool IncludeDeleted = false,
    int Page = 1,
    int PageSize = 25);
