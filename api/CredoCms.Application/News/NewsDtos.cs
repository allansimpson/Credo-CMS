namespace CredoCms.Application.News;

public sealed record NewsListItemDto(
    Guid Id,
    string Slug,
    string Title,
    string? Excerpt,
    bool IsPublished,
    bool IsMembersOnly,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? ExpiresAt,
    DateTimeOffset ModifiedAt);

public sealed record NewsDetailDto(
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
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? CalendarDate,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
    Guid? ModifiedByUserId,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? DeletedAt);

public sealed record PublicNewsItemDto(
    Guid Id,
    string Slug,
    string Title,
    string? Excerpt,
    string? HeroImageUrl,
    string? HeroImageWebpUrl,
    string? HeroImageAlt,
    bool IsMembersOnly,
    DateTimeOffset PublishedAt,
    DateTimeOffset? CalendarDate);

public sealed record PublicNewsDetailDto(
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
    DateTimeOffset PublishedAt,
    DateTimeOffset? CalendarDate);

public sealed record CreateNewsItemRequest(
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
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? CalendarDate);

public sealed record UpdateNewsItemRequest(
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
    DateTimeOffset? ExpiresAt,
    DateTimeOffset? CalendarDate);

public sealed record NewsListQuery(
    string? Search = null,
    bool IncludeDeleted = false,
    int Page = 1,
    int PageSize = 25);
