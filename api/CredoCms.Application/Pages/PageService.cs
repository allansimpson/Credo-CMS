using CredoCms.Application.Caching;
using CredoCms.Application.Common;
using CredoCms.Application.Search;
using CredoCms.Domain.Pages;
using FluentValidation;

namespace CredoCms.Application.Pages;

public sealed class PageService : IPageService
{
    private readonly IPageRepository _repo;
    private readonly IAuditLogger _audit;
    private readonly ISearchIndexer? _search;
    private readonly IValidator<CreatePageRequest> _createValidator;
    private readonly IValidator<UpdatePageRequest> _updateValidator;

    private readonly IOutputCacheInvalidator? _cache;

    private static readonly string[] PageInvalidationTags =
        [OutputCacheTags.Pages, OutputCacheTags.Homepage, OutputCacheTags.Sitemap, OutputCacheTags.Search];

    public PageService(
        IPageRepository repo,
        IAuditLogger audit,
        IValidator<CreatePageRequest> createValidator,
        IValidator<UpdatePageRequest> updateValidator,
        ISearchIndexer? search = null,
        IOutputCacheInvalidator? cache = null)
    {
        _repo = repo;
        _audit = audit;
        _search = search;
        _cache = cache;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    private Task InvalidateCacheAsync(CancellationToken ct) =>
        _cache?.InvalidateAsync(PageInvalidationTags, ct) ?? Task.CompletedTask;

    private async Task IndexAsync(Page p, CancellationToken ct)
    {
        if (_search is null) return;
        await _search.UpsertAsync(new SearchUpsertCommand(
            EntityType: nameof(Page), EntityId: p.Id,
            Title: p.Title,
            BodyText: AutoExcerpt(p.BodyJson, 8000) + " " + (p.Excerpt ?? ""),
            Url: "/" + p.Slug,
            IsPublished: p.IsPublished, IsMembersOnly: p.IsMembersOnly), ct).ConfigureAwait(false);
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

    public async Task<PublicPageDto?> GetPreviewBySlugAsync(string slug, CancellationToken ct = default)
    {
        var page = await _repo.GetBySlugAsync(slug, ct).ConfigureAwait(false);
        if (page is null || page.IsDeleted) return null;
        // Preview prefers the unpublished draft if one exists; otherwise
        // falls back to live content (so previewing a published page with no
        // pending changes still works).
        return page.HasUnpublishedDraft ? ToPublicFromDraft(page) : ToPublic(page);
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
            Template = request.Template,
            CreatedAt = now,
            ModifiedAt = now,
            PublishedAt = request.IsPublished ? now : null,
        };

        await _repo.AddAsync(page, ct).ConfigureAwait(false);
        await IndexAsync(page, ct).ConfigureAwait(false);
        await InvalidateCacheAsync(ct).ConfigureAwait(false);
        await _audit.WriteAsync("Page.Created", nameof(Page), page.Id.ToString(),
            details: new { page.Slug, page.Title, page.IsPublished, page.IsMembersOnly },
            cancellationToken: ct).ConfigureAwait(false);

        return new PageOperationResult(true, Array.Empty<string>(), ToDetail(page));
    }

    /// <summary>
    /// "Save draft" semantics. If the page is currently published, the edits
    /// land on the Draft* columns and the live page is untouched. If the page
    /// is a draft (never-published or previously unpublished), the edits land
    /// directly on the live columns since there is no live version to protect.
    /// Slug, IsPublished, and system-page rules apply identically in both
    /// cases — slug is an identity field, not editorial content.
    /// </summary>
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

        var now = DateTimeOffset.UtcNow;
        page.Slug = request.Slug;
        page.ModifiedAt = now;

        if (page.IsPublished)
        {
            // Published page — stash edits into the Draft* columns. The live
            // /{slug} continues to serve the unchanged published content.
            page.HasUnpublishedDraft = true;
            page.DraftTitle = request.Title;
            page.DraftBodyJson = request.BodyJson;
            page.DraftExcerpt = request.Excerpt ?? AutoExcerpt(request.BodyJson);
            page.DraftHeroImageUrl = request.HeroImageUrl;
            page.DraftHeroImageWebpUrl = request.HeroImageWebpUrl;
            page.DraftHeroImageAlt = request.HeroImageAlt;
            page.DraftMetaDescription = request.MetaDescription;
            page.DraftIsMembersOnly = request.IsMembersOnly;
            page.DraftTemplate = request.Template;
            page.DraftSavedAt = now;
        }
        else
        {
            // Unpublished page — write straight to the live columns. There's
            // nothing live to protect, so the draft staging area would just
            // be needless indirection.
            page.Title = request.Title;
            page.BodyJson = request.BodyJson;
            page.Excerpt = request.Excerpt ?? AutoExcerpt(request.BodyJson);
            page.HeroImageUrl = request.HeroImageUrl;
            page.HeroImageWebpUrl = request.HeroImageWebpUrl;
            page.HeroImageAlt = request.HeroImageAlt;
            page.MetaDescription = request.MetaDescription;
            page.IsMembersOnly = request.IsMembersOnly;
            page.Template = request.Template;
        }

        await _repo.UpdateAsync(page, ct).ConfigureAwait(false);
        // Search index reflects published state only — reindex when live
        // columns changed (i.e. unpublished page), otherwise skip.
        if (!page.HasUnpublishedDraft)
            await IndexAsync(page, ct).ConfigureAwait(false);
        await InvalidateCacheAsync(ct).ConfigureAwait(false);
        await _audit.WriteAsync(page.HasUnpublishedDraft ? "Page.DraftSaved" : "Page.Updated",
            nameof(Page), page.Id.ToString(),
            details: new { page.Slug, page.Title, page.IsPublished, page.HasUnpublishedDraft },
            cancellationToken: ct).ConfigureAwait(false);

        return new PageOperationResult(true, Array.Empty<string>(), ToDetail(page));
    }

    /// <summary>
    /// Promotes the page to published. If a draft exists, copies the Draft*
    /// values onto the live columns and clears the draft state. If no draft
    /// exists, simply flips IsPublished to true (covers the "first publish"
    /// case for a brand-new page).
    /// </summary>
    public async Task<PageOperationResult> PublishAsync(Guid id, CancellationToken ct = default)
    {
        var page = await _repo.GetByIdAsync(id, includeDeleted: false, ct).ConfigureAwait(false);
        if (page is null) return new PageOperationResult(false, new[] { "Page not found." }, null);

        var now = DateTimeOffset.UtcNow;
        if (page.HasUnpublishedDraft)
        {
            // Promote draft → live. Falls back to current live value if a
            // draft field is null (shouldn't happen given how UpdateAsync
            // writes, but defensive).
            page.Title = page.DraftTitle ?? page.Title;
            page.BodyJson = page.DraftBodyJson ?? page.BodyJson;
            page.Excerpt = page.DraftExcerpt ?? page.Excerpt;
            page.HeroImageUrl = page.DraftHeroImageUrl;
            page.HeroImageWebpUrl = page.DraftHeroImageWebpUrl;
            page.HeroImageAlt = page.DraftHeroImageAlt;
            page.MetaDescription = page.DraftMetaDescription;
            page.IsMembersOnly = page.DraftIsMembersOnly ?? page.IsMembersOnly;
            page.Template = page.DraftTemplate ?? page.Template;

            page.HasUnpublishedDraft = false;
            page.DraftTitle = null;
            page.DraftBodyJson = null;
            page.DraftExcerpt = null;
            page.DraftHeroImageUrl = null;
            page.DraftHeroImageWebpUrl = null;
            page.DraftHeroImageAlt = null;
            page.DraftMetaDescription = null;
            page.DraftIsMembersOnly = null;
            page.DraftTemplate = null;
            page.DraftSavedAt = null;
        }

        page.IsPublished = true;
        page.PublishedAt ??= now;
        page.ModifiedAt = now;

        await _repo.UpdateAsync(page, ct).ConfigureAwait(false);
        await IndexAsync(page, ct).ConfigureAwait(false);
        await InvalidateCacheAsync(ct).ConfigureAwait(false);
        await _audit.WriteAsync("Page.Published", nameof(Page), page.Id.ToString(),
            details: new { page.Slug, page.Title, page.IsMembersOnly },
            cancellationToken: ct).ConfigureAwait(false);

        return new PageOperationResult(true, Array.Empty<string>(), ToDetail(page));
    }

    /// <summary>Clears the Draft* columns and HasUnpublishedDraft flag. The
    /// live page is untouched. Intended for "discard changes" in the editor.</summary>
    public async Task<PageOperationResult> DiscardDraftAsync(Guid id, CancellationToken ct = default)
    {
        var page = await _repo.GetByIdAsync(id, includeDeleted: true, ct).ConfigureAwait(false);
        if (page is null) return new PageOperationResult(false, new[] { "Page not found." }, null);

        if (!page.HasUnpublishedDraft)
            return new PageOperationResult(true, Array.Empty<string>(), ToDetail(page));

        page.HasUnpublishedDraft = false;
        page.DraftTitle = null;
        page.DraftBodyJson = null;
        page.DraftExcerpt = null;
        page.DraftHeroImageUrl = null;
        page.DraftHeroImageWebpUrl = null;
        page.DraftHeroImageAlt = null;
        page.DraftMetaDescription = null;
        page.DraftIsMembersOnly = null;
        page.DraftTemplate = null;
        page.DraftSavedAt = null;
        page.ModifiedAt = DateTimeOffset.UtcNow;

        await _repo.UpdateAsync(page, ct).ConfigureAwait(false);
        await _audit.WriteAsync("Page.DraftDiscarded", nameof(Page), page.Id.ToString(),
            details: new { page.Slug, page.Title }, cancellationToken: ct).ConfigureAwait(false);

        return new PageOperationResult(true, Array.Empty<string>(), ToDetail(page));
    }

    /// <summary>Moves a published page back to draft state without touching
    /// content. Public visitors will 404 until Publish is invoked again.</summary>
    public async Task<PageOperationResult> UnpublishAsync(Guid id, CancellationToken ct = default)
    {
        var page = await _repo.GetByIdAsync(id, includeDeleted: false, ct).ConfigureAwait(false);
        if (page is null) return new PageOperationResult(false, new[] { "Page not found." }, null);

        page.IsPublished = false;
        page.ModifiedAt = DateTimeOffset.UtcNow;

        await _repo.UpdateAsync(page, ct).ConfigureAwait(false);
        if (_search is not null)
            await _search.SetPublishedAsync(nameof(Page), id, false, ct).ConfigureAwait(false);
        await InvalidateCacheAsync(ct).ConfigureAwait(false);
        await _audit.WriteAsync("Page.Unpublished", nameof(Page), page.Id.ToString(),
            details: new { page.Slug, page.Title }, cancellationToken: ct).ConfigureAwait(false);

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
        if (_search is not null)
            await _search.SetPublishedAsync(nameof(Page), id, false, ct).ConfigureAwait(false);
        await InvalidateCacheAsync(ct).ConfigureAwait(false);
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
        await IndexAsync(page, ct).ConfigureAwait(false);
        await InvalidateCacheAsync(ct).ConfigureAwait(false);
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
        if (_search is not null)
            await _search.RemoveAsync(nameof(Page), id, ct).ConfigureAwait(false);
        await InvalidateCacheAsync(ct).ConfigureAwait(false);
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
        p.IsPublished, p.IsMembersOnly, p.IsDeleted, p.IsSystemPage, p.Template,
        p.CreatedAt, p.ModifiedAt, p.ModifiedByUserId, p.PublishedAt, p.DeletedAt,
        p.HasUnpublishedDraft,
        p.HasUnpublishedDraft
            ? new PageDraftDto(
                Title: p.DraftTitle ?? p.Title,
                BodyJson: p.DraftBodyJson ?? p.BodyJson,
                Excerpt: p.DraftExcerpt ?? p.Excerpt,
                HeroImageUrl: p.DraftHeroImageUrl,
                HeroImageWebpUrl: p.DraftHeroImageWebpUrl,
                HeroImageAlt: p.DraftHeroImageAlt,
                MetaDescription: p.DraftMetaDescription,
                IsMembersOnly: p.DraftIsMembersOnly ?? p.IsMembersOnly,
                Template: p.DraftTemplate ?? p.Template,
                SavedAt: p.DraftSavedAt ?? p.ModifiedAt)
            : null);

    internal static PublicPageDto ToPublic(Page p) => new(
        p.Id, p.Slug, p.Title, p.BodyJson, p.Excerpt,
        p.HeroImageUrl, p.HeroImageWebpUrl, p.HeroImageAlt, p.MetaDescription,
        p.IsMembersOnly, p.Template, p.PublishedAt ?? p.ModifiedAt);

    /// <summary>Used by the admin preview endpoint when HasUnpublishedDraft
    /// is true — projects the Draft* columns into the same public DTO shape
    /// so the renderer doesn't need to care which slot the data came from.</summary>
    internal static PublicPageDto ToPublicFromDraft(Page p) => new(
        p.Id, p.Slug,
        p.DraftTitle ?? p.Title,
        p.DraftBodyJson ?? p.BodyJson,
        p.DraftExcerpt ?? p.Excerpt,
        p.DraftHeroImageUrl,
        p.DraftHeroImageWebpUrl,
        p.DraftHeroImageAlt,
        p.DraftMetaDescription,
        p.DraftIsMembersOnly ?? p.IsMembersOnly,
        p.DraftTemplate ?? p.Template,
        p.DraftSavedAt ?? p.ModifiedAt);
}
