using CredoCms.Application.Common;
using CredoCms.Domain.Pages;
using FluentValidation;

namespace CredoCms.Application.Pages;

public sealed class PageService : IPageService
{
    private readonly IPageRepository _repo;
    private readonly IAuditLogger _audit;
    private readonly IValidator<CreatePageRequest> _createValidator;
    private readonly IValidator<UpdatePageRequest> _updateValidator;

    public PageService(
        IPageRepository repo,
        IAuditLogger audit,
        IValidator<CreatePageRequest> createValidator,
        IValidator<UpdatePageRequest> updateValidator)
    {
        _repo = repo;
        _audit = audit;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public Task<PagedResult<PageListItemDto>> ListAsync(PageListQuery query, CancellationToken ct = default)
        => _repo.ListAsync(query, ct);

    public async Task<PageDetailDto?> GetAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default)
    {
        var page = await _repo.GetByIdAsync(id, includeDeleted, ct).ConfigureAwait(false);
        return page is null ? null : ToDetail(page);
    }

    public async Task<PublicPageDto?> GetPublicBySlugAsync(string slug, bool includeMembersOnly, CancellationToken ct = default)
    {
        var page = await _repo.GetBySlugAsync(slug, ct).ConfigureAwait(false);
        if (page is null || !page.IsPublished) return null;
        if (page.IsMembersOnly && !includeMembersOnly) return null;
        return ToPublic(page);
    }

    public Task<List<PublicPageDto>> ListPublicAsync(bool includeMembersOnly, CancellationToken ct = default)
        => _repo.ListPublicAsync(includeMembersOnly, ct);

    public async Task<PageOperationResult> CreateAsync(CreatePageRequest request, CancellationToken ct = default)
    {
        var v = await _createValidator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!v.IsValid)
            return new PageOperationResult(false, v.Errors.Select(e => e.ErrorMessage).ToArray(), null);

        if (await _repo.SlugExistsAsync(request.Slug, excludingId: null, ct).ConfigureAwait(false))
            return new PageOperationResult(false, new[] { $"A page with slug '{request.Slug}' already exists." }, null);

        var now = DateTimeOffset.UtcNow;
        var page = new Page
        {
            Id = Guid.NewGuid(),
            Slug = request.Slug,
            Title = request.Title,
            BodyJson = request.BodyJson,
            Excerpt = request.Excerpt ?? AutoExcerpt(request.BodyJson),
            HeroImageUrl = request.HeroImageUrl,
            HeroImageWebpUrl = request.HeroImageWebpUrl,
            HeroImageAlt = request.HeroImageAlt,
            MetaDescription = request.MetaDescription,
            IsPublished = request.IsPublished,
            IsMembersOnly = request.IsMembersOnly,
            CreatedAt = now,
            ModifiedAt = now,
            PublishedAt = request.IsPublished ? now : null,
        };

        await _repo.AddAsync(page, ct).ConfigureAwait(false);
        await _audit.WriteAsync("Page.Created", nameof(Page), page.Id.ToString(),
            details: new { page.Slug, page.Title, page.IsPublished, page.IsMembersOnly },
            cancellationToken: ct).ConfigureAwait(false);

        return new PageOperationResult(true, Array.Empty<string>(), ToDetail(page));
    }

    public async Task<PageOperationResult> UpdateAsync(Guid id, UpdatePageRequest request, CancellationToken ct = default)
    {
        var v = await _updateValidator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!v.IsValid)
            return new PageOperationResult(false, v.Errors.Select(e => e.ErrorMessage).ToArray(), null);

        var page = await _repo.GetByIdAsync(id, includeDeleted: true, ct).ConfigureAwait(false);
        if (page is null) return new PageOperationResult(false, new[] { "Page not found." }, null);

        if (page.IsSystemPage && !string.Equals(page.Slug, request.Slug, StringComparison.Ordinal))
            return new PageOperationResult(false,
                new[] { "System page slugs cannot be changed." }, null);

        if (!string.Equals(page.Slug, request.Slug, StringComparison.Ordinal)
            && await _repo.SlugExistsAsync(request.Slug, excludingId: id, ct).ConfigureAwait(false))
        {
            return new PageOperationResult(false,
                new[] { $"A page with slug '{request.Slug}' already exists." }, null);
        }

        var wasPublished = page.IsPublished;

        page.Slug = request.Slug;
        page.Title = request.Title;
        page.BodyJson = request.BodyJson;
        page.Excerpt = request.Excerpt ?? AutoExcerpt(request.BodyJson);
        page.HeroImageUrl = request.HeroImageUrl;
        page.HeroImageWebpUrl = request.HeroImageWebpUrl;
        page.HeroImageAlt = request.HeroImageAlt;
        page.MetaDescription = request.MetaDescription;
        page.IsPublished = request.IsPublished;
        page.IsMembersOnly = request.IsMembersOnly;
        page.ModifiedAt = DateTimeOffset.UtcNow;
        if (request.IsPublished && page.PublishedAt is null)
            page.PublishedAt = page.ModifiedAt;
        if (!request.IsPublished && wasPublished)
        {
            // Unpublish — keep PublishedAt as the last-published date for audit clarity.
        }

        await _repo.UpdateAsync(page, ct).ConfigureAwait(false);
        await _audit.WriteAsync("Page.Updated", nameof(Page), page.Id.ToString(),
            details: new { page.Slug, page.Title, page.IsPublished, page.IsMembersOnly },
            cancellationToken: ct).ConfigureAwait(false);

        return new PageOperationResult(true, Array.Empty<string>(), ToDetail(page));
    }

    public async Task<PageOperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var page = await _repo.GetByIdAsync(id, includeDeleted: false, ct).ConfigureAwait(false);
        if (page is null) return new PageOperationResult(false, new[] { "Page not found." }, null);

        page.IsDeleted = true;
        page.IsPublished = false;
        page.DeletedAt = DateTimeOffset.UtcNow;
        page.ModifiedAt = page.DeletedAt.Value;

        await _repo.UpdateAsync(page, ct).ConfigureAwait(false);
        await _audit.WriteAsync("Page.SoftDeleted", nameof(Page), id.ToString(),
            details: new { page.Slug, page.Title }, cancellationToken: ct).ConfigureAwait(false);

        return new PageOperationResult(true, Array.Empty<string>(), ToDetail(page));
    }

    public async Task<PageOperationResult> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        var page = await _repo.GetByIdAsync(id, includeDeleted: true, ct).ConfigureAwait(false);
        if (page is null) return new PageOperationResult(false, new[] { "Page not found." }, null);

        if (await _repo.SlugExistsAsync(page.Slug, excludingId: id, ct).ConfigureAwait(false))
            return new PageOperationResult(false,
                new[] { $"Cannot restore — another page already uses slug '{page.Slug}'." }, null);

        page.IsDeleted = false;
        page.DeletedAt = null;
        page.ModifiedAt = DateTimeOffset.UtcNow;

        await _repo.UpdateAsync(page, ct).ConfigureAwait(false);
        await _audit.WriteAsync("Page.Restored", nameof(Page), id.ToString(),
            details: new { page.Slug, page.Title }, cancellationToken: ct).ConfigureAwait(false);

        return new PageOperationResult(true, Array.Empty<string>(), ToDetail(page));
    }

    public async Task<PageOperationResult> HardDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var page = await _repo.GetByIdAsync(id, includeDeleted: true, ct).ConfigureAwait(false);
        if (page is null) return new PageOperationResult(false, new[] { "Page not found." }, null);
        if (page.IsSystemPage)
            return new PageOperationResult(false, new[] { "System pages cannot be hard-deleted." }, null);
        if (!page.IsDeleted)
            return new PageOperationResult(false,
                new[] { "Soft-delete the page first, then hard-delete from the deleted tab." }, null);

        await _repo.HardDeleteAsync(id, ct).ConfigureAwait(false);
        await _audit.WriteAsync("Page.HardDeleted", nameof(Page), id.ToString(),
            details: new { page.Slug, page.Title }, cancellationToken: ct).ConfigureAwait(false);

        return new PageOperationResult(true, Array.Empty<string>(), null);
    }

    /// <summary>
    /// Pulls the first ~280 chars of plain text from a ProseMirror JSON document
    /// for use as an excerpt fallback. Walks <c>type:"text"</c> nodes only and
    /// joins them with spaces.
    /// </summary>
    internal static string AutoExcerpt(string bodyJson, int maxLength = 280)
    {
        if (string.IsNullOrWhiteSpace(bodyJson)) return string.Empty;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(bodyJson);
            var sb = new System.Text.StringBuilder(maxLength + 16);
            CollectText(doc.RootElement, sb, maxLength);
            var text = sb.ToString().Trim();
            return text.Length <= maxLength ? text : text[..maxLength].TrimEnd() + "…";
        }
        catch (System.Text.Json.JsonException)
        {
            return string.Empty;
        }
    }

    private static void CollectText(System.Text.Json.JsonElement el, System.Text.StringBuilder sb, int maxLength)
    {
        if (sb.Length >= maxLength) return;
        if (el.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            if (el.TryGetProperty("type", out var type)
                && type.ValueKind == System.Text.Json.JsonValueKind.String
                && type.GetString() == "text"
                && el.TryGetProperty("text", out var text)
                && text.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                if (sb.Length > 0 && sb[^1] != ' ') sb.Append(' ');
                sb.Append(text.GetString());
            }
            if (el.TryGetProperty("content", out var content)
                && content.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                foreach (var child in content.EnumerateArray()) CollectText(child, sb, maxLength);
            }
        }
        else if (el.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var child in el.EnumerateArray()) CollectText(child, sb, maxLength);
        }
    }

    internal static PageDetailDto ToDetail(Page p) => new(
        p.Id, p.Slug, p.Title, p.BodyJson, p.Excerpt,
        p.HeroImageUrl, p.HeroImageWebpUrl, p.HeroImageAlt, p.MetaDescription,
        p.IsPublished, p.IsMembersOnly, p.IsDeleted, p.IsSystemPage,
        p.CreatedAt, p.ModifiedAt, p.ModifiedByUserId, p.PublishedAt, p.DeletedAt);

    internal static PublicPageDto ToPublic(Page p) => new(
        p.Id, p.Slug, p.Title, p.BodyJson, p.Excerpt,
        p.HeroImageUrl, p.HeroImageWebpUrl, p.HeroImageAlt, p.MetaDescription,
        p.IsMembersOnly, p.PublishedAt ?? p.ModifiedAt);
}
