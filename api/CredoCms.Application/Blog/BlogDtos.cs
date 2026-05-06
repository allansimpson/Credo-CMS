namespace CredoCms.Application.Blog;

public sealed record BlogPostListItemDto(
    Guid Id,
    string Slug,
    string Title,
    string? Excerpt,
    string? HeroImageBlobUrl,
    string? HeroImageWebpBlobUrl,
    string? HeroImageAltText,
    string Category,
    string AuthorDisplayName,
    bool IsPublished,
    bool IsMembersOnly,
    bool IsPinned,
    DateTimeOffset? PublishedAt,
    int ReadingTimeMinutes,
    DateTimeOffset ModifiedAt);

public sealed record BlogPostDetailDto(
    Guid Id,
    string Slug,
    string Title,
    string BodyJson,
    string? Excerpt,
    string? HeroImageBlobUrl,
    string? HeroImageWebpBlobUrl,
    string? HeroImageAltText,
    string Category,
    Guid AuthorUserId,
    string AuthorDisplayName,
    Guid? RelatedSermonId,
    bool IsPublished,
    bool IsMembersOnly,
    bool IsPinned,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? ScheduledPublishAt,
    int ReadingTimeMinutes,
    string? MetaDescription,
    IReadOnlyList<string> Tags,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt);

public sealed record CreateBlogPostRequest(
    string Slug,
    string Title,
    string BodyJson,
    string? Excerpt,
    string? HeroImageBlobUrl,
    string? HeroImageWebpBlobUrl,
    string? HeroImageAltText,
    string Category,
    Guid? RelatedSermonId,
    bool IsPublished,
    bool IsMembersOnly,
    bool IsPinned,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? ScheduledPublishAt,
    string? MetaDescription,
    IReadOnlyList<string>? Tags);

public sealed record UpdateBlogPostRequest(
    string Slug,
    string Title,
    string BodyJson,
    string? Excerpt,
    string? HeroImageBlobUrl,
    string? HeroImageWebpBlobUrl,
    string? HeroImageAltText,
    string Category,
    Guid? RelatedSermonId,
    bool IsPublished,
    bool IsMembersOnly,
    bool IsPinned,
    DateTimeOffset? PublishedAt,
    DateTimeOffset? ScheduledPublishAt,
    string? MetaDescription,
    IReadOnlyList<string>? Tags);

public sealed record BlogMutationResult(
    bool Succeeded,
    IReadOnlyList<string> Errors,
    BlogPostDetailDto? Post = null)
{
    public static BlogMutationResult Success(BlogPostDetailDto post) => new(true, Array.Empty<string>(), post);
    public static BlogMutationResult Failure(params string[] errors) => new(false, errors, null);
}

public sealed record BlogListQuery(
    string? Search = null,
    string? Category = null,
    Guid? AuthorUserId = null,
    bool? IsPublished = null,
    bool IncludeDeleted = false,
    int Page = 1,
    int PageSize = 20);
