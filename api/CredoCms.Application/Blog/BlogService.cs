using CredoCms.Application.Caching;
using CredoCms.Application.Common;
using CredoCms.Application.Search;
using CredoCms.Application.Tags;
using CredoCms.Domain.Blog;
using CredoCms.Domain.Common;
using CredoCms.Domain.Identity;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace CredoCms.Application.Blog;

public sealed class BlogService : IBlogService
{
    private readonly IBlogRepository _repo;
    private readonly UserManager<ApplicationUser> _users;
    private readonly ITagService _tags;
    private readonly ITagRepository _tagRepo;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _audit;
    private readonly IOutputCacheInvalidator _cache;
    private readonly ISearchIndexer? _search;
    private readonly IValidator<CreateBlogPostRequest> _createValidator;
    private readonly IValidator<UpdateBlogPostRequest> _updateValidator;

    public BlogService(
        IBlogRepository repo,
        UserManager<ApplicationUser> users,
        ITagService tags,
        ITagRepository tagRepo,
        ICurrentUserService currentUser,
        IAuditLogger audit,
        IOutputCacheInvalidator cache,
        IValidator<CreateBlogPostRequest> createValidator,
        IValidator<UpdateBlogPostRequest> updateValidator,
        ISearchIndexer? search = null)
    {
        _repo = repo;
        _users = users;
        _tags = tags;
        _tagRepo = tagRepo;
        _currentUser = currentUser;
        _audit = audit;
        _cache = cache;
        _search = search;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    private async Task IndexAsync(BlogPost p, CancellationToken ct)
    {
        if (_search is null) return;
        var bodyText = ExtractText(p.BodyJson);
        var index = (p.Title ?? string.Empty) + " " + bodyText + " " + (p.Excerpt ?? string.Empty);
        await _search.UpsertAsync(new SearchUpsertCommand(
            EntityType: nameof(BlogPost), EntityId: p.Id,
            Title: p.Title ?? string.Empty,
            BodyText: index,
            Url: "/blog/" + p.Slug,
            IsPublished: p.IsPublished
                && p.PublishedAt is { } at && at <= DateTimeOffset.UtcNow,
            IsMembersOnly: p.IsMembersOnly), ct).ConfigureAwait(false);
    }

    private bool IsAdmin => _currentUser.Roles.Contains(SystemConstants.Roles.Administrator);
    private bool IsEditor => _currentUser.Roles.Contains(SystemConstants.Roles.Editor);
    private bool IsAdminShell => IsAdmin || IsEditor;
    private bool IsAuthenticated => _currentUser.IsAuthenticated && _currentUser.UserId != SystemConstants.SystemUserId;

    // ---- public reads ----------------------------------------------------

    public async Task<PagedResult<BlogPostListItemDto>> ListPublicAsync(
        string? category, int page, int pageSize, CancellationToken ct = default)
    {
        var safePage = Math.Max(1, page);
        var safeSize = Math.Clamp(pageSize, 1, 50);
        var rows = await _repo.ListPublicAsync(category, IsAuthenticated, safePage, safeSize, ct).ConfigureAwait(false);
        var items = await Task.WhenAll(rows.Items.Select(p => ToListItemAsync(p, ct))).ConfigureAwait(false);
        return new PagedResult<BlogPostListItemDto>(items, rows.TotalCount, safePage, safeSize);
    }

    public async Task<BlogPostDetailDto?> GetPublicBySlugAsync(string slug, CancellationToken ct = default)
    {
        var post = await _repo.GetBySlugAsync(slug, ct).ConfigureAwait(false);
        if (post is null) return null;
        // Public visibility gate: must be published, non-future, and either
        // public or members-only-with-authenticated-viewer.
        if (!post.IsPublished
            || post.PublishedAt is null
            || post.PublishedAt > DateTimeOffset.UtcNow) return null;
        if (post.IsMembersOnly && !IsAuthenticated) return null;
        return await ToDetailAsync(post, ct).ConfigureAwait(false);
    }

    public async Task<List<BlogPostListItemDto>> ListPublicByAuthorAsync(Guid authorUserId, CancellationToken ct = default)
    {
        var rows = await _repo.ListByAuthorAsync(authorUserId, publicOnly: !IsAuthenticated, ct).ConfigureAwait(false);
        // For authenticated members, we also need to filter out unpublished
        // posts here because ListByAuthorAsync returns everything when
        // publicOnly=false.
        if (IsAuthenticated)
        {
            var now = DateTimeOffset.UtcNow;
            rows = rows.Where(p => p.IsPublished && p.PublishedAt is { } at && at <= now).ToList();
        }
        var items = await Task.WhenAll(rows.Select(p => ToListItemAsync(p, ct))).ConfigureAwait(false);
        return items.ToList();
    }

    // ---- admin reads -----------------------------------------------------

    public async Task<PagedResult<BlogPostListItemDto>> ListAdminAsync(BlogListQuery query, CancellationToken ct = default)
    {
        if (!IsAdminShell) return new PagedResult<BlogPostListItemDto>(Array.Empty<BlogPostListItemDto>(), 0, 1, query.PageSize);
        var safe = query with
        {
            Page = Math.Max(1, query.Page),
            PageSize = Math.Clamp(query.PageSize, 1, 100),
        };
        var rows = await _repo.ListAdminAsync(safe, ct).ConfigureAwait(false);
        var items = await Task.WhenAll(rows.Items.Select(p => ToListItemAsync(p, ct))).ConfigureAwait(false);
        return new PagedResult<BlogPostListItemDto>(items, rows.TotalCount, safe.Page, safe.PageSize);
    }

    public async Task<BlogPostDetailDto?> GetAdminAsync(Guid id, CancellationToken ct = default)
    {
        if (!IsAdminShell) return null;
        var post = await _repo.GetAsync(id, ct).ConfigureAwait(false);
        return post is null ? null : await ToDetailAsync(post, ct).ConfigureAwait(false);
    }

    // ---- admin writes ----------------------------------------------------

    public async Task<BlogMutationResult> CreateAsync(CreateBlogPostRequest request, CancellationToken ct = default)
    {
        if (!IsAdminShell) return BlogMutationResult.Failure("Only editors and administrators can create posts.");

        var v = await _createValidator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!v.IsValid) return BlogMutationResult.Failure([.. v.Errors.Select(e => e.ErrorMessage)]);

        if (await _repo.SlugExistsAsync(request.Slug, null, ct).ConfigureAwait(false))
        {
            return BlogMutationResult.Failure($"A blog post with slug \"{request.Slug}\" already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        var entity = new BlogPost
        {
            Id = Guid.NewGuid(),
            Slug = request.Slug,
            Title = request.Title,
            BodyJson = request.BodyJson,
            Excerpt = !string.IsNullOrWhiteSpace(request.Excerpt)
                ? request.Excerpt
                : DeriveExcerpt(request.BodyJson),
            HeroImageBlobUrl = request.HeroImageBlobUrl,
            HeroImageWebpBlobUrl = request.HeroImageWebpBlobUrl,
            HeroImageAltText = request.HeroImageAltText,
            AuthorUserId = _currentUser.UserId,
            Category = request.Category,
            RelatedSermonId = request.RelatedSermonId,
            IsPublished = request.IsPublished,
            IsMembersOnly = request.IsMembersOnly,
            IsPinned = request.IsPinned,
            PublishedAt = request.IsPublished ? (request.PublishedAt ?? now) : request.PublishedAt,
            ScheduledPublishAt = request.ScheduledPublishAt,
            ReadingTimeMinutes = ComputeReadingMinutes(request.BodyJson),
            MetaDescription = request.MetaDescription,
            CreatedAt = now,
            ModifiedAt = now,
            ModifiedByUserId = _currentUser.UserId,
        };

        var tagIds = await ResolveTagIdsAsync(request.Tags, ct).ConfigureAwait(false);
        await _repo.AddAsync(entity, tagIds, ct).ConfigureAwait(false);

        await _audit.WriteAsync("BlogPost.Created", nameof(BlogPost), entity.Id.ToString(),
            new { entity.Slug, entity.Title, entity.IsPublished }, ct).ConfigureAwait(false);
        await _cache.InvalidateAsync(OutputCacheTags.Blog, ct).ConfigureAwait(false);
        await IndexAsync(entity, ct).ConfigureAwait(false);

        return BlogMutationResult.Success(await ToDetailAsync(entity, ct).ConfigureAwait(false));
    }

    public async Task<BlogMutationResult> UpdateAsync(Guid id, UpdateBlogPostRequest request, CancellationToken ct = default)
    {
        if (!IsAdminShell) return BlogMutationResult.Failure("Only editors and administrators can edit posts.");

        var v = await _updateValidator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!v.IsValid) return BlogMutationResult.Failure([.. v.Errors.Select(e => e.ErrorMessage)]);

        var entity = await _repo.GetAsync(id, ct).ConfigureAwait(false);
        if (entity is null) return BlogMutationResult.Failure("Post not found.");

        if (await _repo.SlugExistsAsync(request.Slug, id, ct).ConfigureAwait(false))
        {
            return BlogMutationResult.Failure($"Slug \"{request.Slug}\" is already in use.");
        }

        var wasPublished = entity.IsPublished;
        var now = DateTimeOffset.UtcNow;

        entity.Slug = request.Slug;
        entity.Title = request.Title;
        entity.BodyJson = request.BodyJson;
        entity.Excerpt = !string.IsNullOrWhiteSpace(request.Excerpt)
            ? request.Excerpt
            : DeriveExcerpt(request.BodyJson);
        entity.HeroImageBlobUrl = request.HeroImageBlobUrl;
        entity.HeroImageWebpBlobUrl = request.HeroImageWebpBlobUrl;
        entity.HeroImageAltText = request.HeroImageAltText;
        entity.Category = request.Category;
        entity.RelatedSermonId = request.RelatedSermonId;
        entity.IsPublished = request.IsPublished;
        entity.IsMembersOnly = request.IsMembersOnly;
        entity.IsPinned = request.IsPinned;
        // PublishedAt logic: stamp first-publish, otherwise carry forward the
        // caller's value (allows admins to backdate or pin a future publish).
        if (request.IsPublished && !wasPublished && request.PublishedAt is null)
        {
            entity.PublishedAt = now;
        }
        else
        {
            entity.PublishedAt = request.PublishedAt;
        }
        entity.ScheduledPublishAt = request.ScheduledPublishAt;
        entity.ReadingTimeMinutes = ComputeReadingMinutes(request.BodyJson);
        entity.MetaDescription = request.MetaDescription;
        entity.ModifiedAt = now;
        entity.ModifiedByUserId = _currentUser.UserId;

        var tagIds = await ResolveTagIdsAsync(request.Tags, ct).ConfigureAwait(false);
        await _repo.UpdateAsync(entity, tagIds, ct).ConfigureAwait(false);

        await _audit.WriteAsync("BlogPost.Updated", nameof(BlogPost), entity.Id.ToString(),
            new { entity.Slug, entity.Title, entity.IsPublished }, ct).ConfigureAwait(false);
        await _cache.InvalidateAsync(OutputCacheTags.Blog, ct).ConfigureAwait(false);
        await IndexAsync(entity, ct).ConfigureAwait(false);

        return BlogMutationResult.Success(await ToDetailAsync(entity, ct).ConfigureAwait(false));
    }

    public async Task<BlogMutationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (!IsAdminShell) return BlogMutationResult.Failure("Only editors and administrators can delete posts.");
        var entity = await _repo.GetAsync(id, ct).ConfigureAwait(false);
        if (entity is null) return BlogMutationResult.Failure("Post not found.");

        await _repo.SoftDeleteAsync(id, _currentUser.UserId, ct).ConfigureAwait(false);
        await _audit.WriteAsync("BlogPost.SoftDeleted", nameof(BlogPost), id.ToString(),
            new { entity.Slug, entity.Title }, ct).ConfigureAwait(false);
        await _cache.InvalidateAsync(OutputCacheTags.Blog, ct).ConfigureAwait(false);
        if (_search is not null)
        {
            await _search.RemoveAsync(nameof(BlogPost), id, ct).ConfigureAwait(false);
        }

        return BlogMutationResult.Success(await ToDetailAsync(entity, ct).ConfigureAwait(false));
    }

    // ---- helpers ---------------------------------------------------------

    private async Task<List<Guid>> ResolveTagIdsAsync(IReadOnlyList<string>? tags, CancellationToken ct)
    {
        if (tags is null || tags.Count == 0) return new List<Guid>();
        var ids = new List<Guid>();
        foreach (var name in tags.Where(t => !string.IsNullOrWhiteSpace(t)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var tag = await _tags.NormalizeAndUpsertAsync(name, ct).ConfigureAwait(false);
            ids.Add(tag.Id);
        }
        return ids;
    }

    private async Task<BlogPostListItemDto> ToListItemAsync(BlogPost p, CancellationToken ct)
    {
        var author = await _users.FindByIdAsync(p.AuthorUserId.ToString()).ConfigureAwait(false);
        return new BlogPostListItemDto(
            p.Id, p.Slug, p.Title, p.Excerpt,
            p.HeroImageBlobUrl, p.HeroImageWebpBlobUrl, p.HeroImageAltText,
            p.Category,
            author?.DisplayName ?? "(unknown)",
            p.IsPublished, p.IsMembersOnly, p.IsPinned,
            p.PublishedAt, p.ReadingTimeMinutes, p.ModifiedAt);
    }

    private async Task<BlogPostDetailDto> ToDetailAsync(BlogPost p, CancellationToken ct)
    {
        var author = await _users.FindByIdAsync(p.AuthorUserId.ToString()).ConfigureAwait(false);
        var tagIds = await _repo.GetTagIdsAsync(p.Id, ct).ConfigureAwait(false);
        var tags = tagIds.Count == 0
            ? new List<string>()
            : (await _tagRepo.GetByIdsAsync(tagIds, ct).ConfigureAwait(false))
                .Select(t => t.Name).OrderBy(n => n).ToList();

        return new BlogPostDetailDto(
            p.Id, p.Slug, p.Title, p.BodyJson, p.Excerpt,
            p.HeroImageBlobUrl, p.HeroImageWebpBlobUrl, p.HeroImageAltText,
            p.Category,
            p.AuthorUserId, author?.DisplayName ?? "(unknown)",
            p.RelatedSermonId,
            p.IsPublished, p.IsMembersOnly, p.IsPinned,
            p.PublishedAt, p.ScheduledPublishAt,
            p.ReadingTimeMinutes, p.MetaDescription,
            tags,
            p.CreatedAt, p.ModifiedAt);
    }

    /// <summary>
    /// Reading time = max(1, ceil(words / 250)). ProseMirror JSON is parsed
    /// shallowly — concat all string "text" leaves, split on whitespace.
    /// </summary>
    internal static int ComputeReadingMinutes(string bodyJson)
    {
        if (string.IsNullOrWhiteSpace(bodyJson)) return 1;
        var words = CountWords(bodyJson);
        if (words <= 0) return 1;
        return Math.Max(1, (int)Math.Ceiling(words / 250.0));
    }

    /// <summary>
    /// Pulls the first ~280 chars of plain text from the body, used as the
    /// fallback excerpt when the editor leaves the field blank.
    /// </summary>
    internal static string? DeriveExcerpt(string bodyJson)
    {
        if (string.IsNullOrWhiteSpace(bodyJson)) return null;
        var text = ExtractText(bodyJson);
        if (string.IsNullOrWhiteSpace(text)) return null;
        var trimmed = text.Trim();
        return trimmed.Length <= 280 ? trimmed : trimmed[..280] + "…";
    }

    private static int CountWords(string bodyJson)
    {
        var text = ExtractText(bodyJson);
        return string.IsNullOrWhiteSpace(text)
            ? 0
            : text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Length;
    }

    private static string ExtractText(string bodyJson)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(bodyJson);
            var sb = new System.Text.StringBuilder();
            Walk(doc.RootElement, sb);
            return sb.ToString();
        }
        catch
        {
            return string.Empty;
        }
    }

    private static void Walk(System.Text.Json.JsonElement el, System.Text.StringBuilder sb)
    {
        switch (el.ValueKind)
        {
            case System.Text.Json.JsonValueKind.Object:
                if (el.TryGetProperty("text", out var t) && t.ValueKind == System.Text.Json.JsonValueKind.String)
                {
                    sb.Append(t.GetString()).Append(' ');
                }
                if (el.TryGetProperty("content", out var c))
                {
                    Walk(c, sb);
                }
                break;
            case System.Text.Json.JsonValueKind.Array:
                foreach (var child in el.EnumerateArray()) Walk(child, sb);
                break;
        }
    }
}
