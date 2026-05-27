using CredoCms.Application.Blog;
using CredoCms.Application.Common;
using CredoCms.Domain.Blog;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CredoCms.Infrastructure.Blog;

public sealed class BlogRepository : IBlogRepository
{
    private readonly ApplicationDbContext _db;
    public BlogRepository(ApplicationDbContext db) => _db = db;

    public Task<BlogPost?> GetAsync(Guid id, CancellationToken ct = default) =>
        _db.BlogPosts.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<BlogPost?> GetBySlugAsync(string slug, CancellationToken ct = default) =>
        _db.BlogPosts.FirstOrDefaultAsync(p => p.Slug == slug, ct);

    public Task<bool> SlugExistsAsync(string slug, Guid? excludeId, CancellationToken ct = default)
    {
        var q = _db.BlogPosts.Where(p => p.Slug == slug);
        if (excludeId is { } id) q = q.Where(p => p.Id != id);
        return q.AnyAsync(ct);
    }

    public async Task<PagedResult<BlogPost>> ListAdminAsync(BlogListQuery query, CancellationToken ct = default)
    {
        // Soft-delete filter is applied by the model query filter; passing
        // includeDeleted=true requires an explicit IgnoreQueryFilters call.
        var q = (query.IncludeDeleted
            ? _db.BlogPosts.IgnoreQueryFilters().AsNoTracking()
            : _db.BlogPosts.AsNoTracking()).AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(p => EF.Functions.Like(p.Title, $"%{s}%") || EF.Functions.Like(p.Slug, $"%{s}%"));
        }
        if (!string.IsNullOrWhiteSpace(query.Category)) q = q.Where(p => p.Category == query.Category);
        if (query.AuthorUserId is { } authorId) q = q.Where(p => p.AuthorUserId == authorId);
        if (query.IsPublished is { } published) q = q.Where(p => p.IsPublished == published);

        var total = await q.CountAsync(ct).ConfigureAwait(false);
        var page = await q
            .OrderByDescending(p => p.IsPinned)
            .ThenByDescending(p => p.PublishedAt)
            .ThenByDescending(p => p.ModifiedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct).ConfigureAwait(false);
        return new PagedResult<BlogPost>(page, total, query.Page, query.PageSize);
    }

    public async Task<PagedResult<BlogPost>> ListPublicAsync(
        string? category, bool includeMembersOnly, int page, int pageSize, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var q = _db.BlogPosts.AsNoTracking()
            .Where(p => p.IsPublished
                && p.PublishedAt != null
                && p.PublishedAt <= now);

        if (!includeMembersOnly) q = q.Where(p => !p.IsMembersOnly);
        if (!string.IsNullOrWhiteSpace(category)) q = q.Where(p => p.Category == category);

        var total = await q.CountAsync(ct).ConfigureAwait(false);
        var rows = await q
            .OrderByDescending(p => p.IsPinned)
            .ThenByDescending(p => p.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct).ConfigureAwait(false);
        return new PagedResult<BlogPost>(rows, total, page, pageSize);
    }

    public async Task<List<BlogPost>> ListByAuthorAsync(Guid authorUserId, bool publicOnly, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        var q = _db.BlogPosts.AsNoTracking().Where(p => p.AuthorUserId == authorUserId);
        if (publicOnly)
        {
            q = q.Where(p => p.IsPublished
                && !p.IsMembersOnly
                && p.PublishedAt != null
                && p.PublishedAt <= now);
        }
        return await q.OrderByDescending(p => p.PublishedAt ?? p.ModifiedAt)
            .ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task AddAsync(BlogPost post, IReadOnlyCollection<Guid> tagIds, CancellationToken ct = default)
    {
        _db.BlogPosts.Add(post);
        foreach (var tagId in tagIds)
        {
            _db.BlogPostTags.Add(new Domain.Blog.BlogPostTag { BlogPostId = post.Id, TagId = tagId });
        }
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(BlogPost post, IReadOnlyCollection<Guid> tagIds, CancellationToken ct = default)
    {
        _db.BlogPosts.Update(post);
        // Replace tag links wholesale: simpler than diffing for the expected
        // tag-set sizes (single-digit per post).
        var existing = await _db.BlogPostTags.Where(t => t.BlogPostId == post.Id).ToListAsync(ct).ConfigureAwait(false);
        _db.BlogPostTags.RemoveRange(existing);
        foreach (var tagId in tagIds)
        {
            _db.BlogPostTags.Add(new Domain.Blog.BlogPostTag { BlogPostId = post.Id, TagId = tagId });
        }
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task SoftDeleteAsync(Guid id, Guid byUserId, CancellationToken ct = default)
    {
        var entity = await _db.BlogPosts.FirstOrDefaultAsync(p => p.Id == id, ct).ConfigureAwait(false);
        if (entity is null) return;
        entity.IsDeleted = true;
        entity.DeletedAt = DateTimeOffset.UtcNow;
        entity.DeletedByUserId = byUserId;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<List<Guid>> GetTagIdsAsync(Guid blogPostId, CancellationToken ct = default) =>
        await _db.BlogPostTags.AsNoTracking()
            .Where(t => t.BlogPostId == blogPostId)
            .Select(t => t.TagId)
            .ToListAsync(ct).ConfigureAwait(false);
}
