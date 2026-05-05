using CredoCms.Application.Documents;
using CredoCms.Application.Search;
using CredoCms.Domain.Search;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CredoCms.Infrastructure.Search;

/// <summary>
/// FTS-or-LIKE fallback search indexer. Probes for SQL Server full-text
/// search availability the first time it's used; if absent, falls back to
/// <c>LIKE '%term%'</c> across all terms. Either way the same SearchIndex
/// table backs both modes.
///
/// Uses <see cref="IServiceScopeFactory"/> instead of injecting a scoped
/// DbContext so the rebuild background service can call into us without a
/// scope of its own.
/// </summary>
public sealed class SearchIndexer : ISearchIndexer, IDisposable
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<SearchIndexer> _logger;
    private bool? _ftsAvailable;
    private readonly SemaphoreSlim _ftsLock = new(1, 1);

    public SearchIndexer(IServiceScopeFactory scopes, ILogger<SearchIndexer> logger)
    {
        _scopes = scopes;
        _logger = logger;
    }

    public void Dispose() => _ftsLock.Dispose();

    public async Task UpsertAsync(SearchUpsertCommand command, CancellationToken ct = default)
    {
        await using var scope = _scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var existing = await db.SearchIndex.FirstOrDefaultAsync(
            x => x.EntityType == command.EntityType && x.EntityId == command.EntityId, ct).ConfigureAwait(false);

        if (existing is null)
        {
            db.SearchIndex.Add(new SearchIndexEntry
            {
                Id = Guid.NewGuid(),
                EntityType = command.EntityType, EntityId = command.EntityId,
                Title = command.Title, BodyText = command.BodyText, Url = command.Url,
                IsPublished = command.IsPublished, IsMembersOnly = command.IsMembersOnly,
                IndexedAt = DateTimeOffset.UtcNow,
            });
        }
        else
        {
            existing.Title = command.Title;
            existing.BodyText = command.BodyText;
            existing.Url = command.Url;
            existing.IsPublished = command.IsPublished;
            existing.IsMembersOnly = command.IsMembersOnly;
            existing.IndexedAt = DateTimeOffset.UtcNow;
        }
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task RemoveAsync(string entityType, Guid entityId, CancellationToken ct = default)
    {
        await using var scope = _scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var entry = await db.SearchIndex.FirstOrDefaultAsync(
            x => x.EntityType == entityType && x.EntityId == entityId, ct).ConfigureAwait(false);
        if (entry is null) return;
        db.SearchIndex.Remove(entry);
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task SetPublishedAsync(string entityType, Guid entityId, bool isPublished, CancellationToken ct = default)
    {
        await using var scope = _scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var entry = await db.SearchIndex.FirstOrDefaultAsync(
            x => x.EntityType == entityType && x.EntityId == entityId, ct).ConfigureAwait(false);
        if (entry is null) return;
        entry.IsPublished = isPublished;
        await db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task RebuildAllAsync(CancellationToken ct = default)
    {
        await using var scope = _scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var docs = scope.ServiceProvider.GetRequiredService<IDocumentService>();

        // Wipe existing rows (a rebuild is a full replace).
        await db.SearchIndex.ExecuteDeleteAsync(ct).ConfigureAwait(false);

        var now = DateTimeOffset.UtcNow;

        // Pages
        var pages = await db.Pages.IgnoreQueryFilters().Where(p => !p.IsDeleted).ToListAsync(ct).ConfigureAwait(false);
        foreach (var p in pages)
        {
            db.SearchIndex.Add(new SearchIndexEntry
            {
                Id = Guid.NewGuid(),
                EntityType = "Page", EntityId = p.Id,
                Title = p.Title, BodyText = ExtractText(p.BodyJson) + " " + (p.Excerpt ?? ""),
                Url = "/" + p.Slug,
                IsPublished = p.IsPublished, IsMembersOnly = p.IsMembersOnly,
                IndexedAt = now,
            });
        }

        // News
        var news = await db.News.IgnoreQueryFilters().Where(n => !n.IsDeleted).ToListAsync(ct).ConfigureAwait(false);
        foreach (var n in news)
        {
            db.SearchIndex.Add(new SearchIndexEntry
            {
                Id = Guid.NewGuid(),
                EntityType = "NewsItem", EntityId = n.Id,
                Title = n.Title, BodyText = ExtractText(n.BodyJson) + " " + (n.Excerpt ?? ""),
                Url = "/news/" + n.Slug,
                IsPublished = n.IsPublished, IsMembersOnly = n.IsMembersOnly,
                IndexedAt = now,
            });
        }

        // Leaders
        var leaders = await db.Leaders.ToListAsync(ct).ConfigureAwait(false);
        foreach (var l in leaders)
        {
            db.SearchIndex.Add(new SearchIndexEntry
            {
                Id = Guid.NewGuid(),
                EntityType = "Leader", EntityId = l.Id,
                Title = l.FullName,
                BodyText = $"{l.Title} {l.Category} {ExtractText(l.BioJson)}",
                Url = "/leaders/" + l.Id,
                IsPublished = true, IsMembersOnly = false,
                IndexedAt = now,
            });
        }

        // Documents (metadata only)
        var documents = await docs.ListAsync(category: null, includeDeleted: false, ct).ConfigureAwait(false);
        foreach (var d in documents)
        {
            db.SearchIndex.Add(new SearchIndexEntry
            {
                Id = Guid.NewGuid(),
                EntityType = "Document", EntityId = d.Id,
                Title = d.Title,
                BodyText = $"{d.Description} {d.Category} {d.OriginalFilename}",
                Url = "/documents/" + d.Id,
                IsPublished = d.IsPublished, IsMembersOnly = d.IsMembersOnly,
                IndexedAt = now,
            });
        }

        await db.SaveChangesAsync(ct).ConfigureAwait(false);
        _logger.LogInformation("Search index rebuilt: pages={P}, news={N}, leaders={L}, documents={D}",
            pages.Count, news.Count, leaders.Count, documents.Count);
    }

    public async Task<SearchResults> SearchAsync(string query, bool includeMembersOnly, int page, int pageSize, CancellationToken ct = default)
    {
        await using var scope = _scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);

        if (string.IsNullOrWhiteSpace(query))
            return new SearchResults(Array.Empty<SearchResultItem>(), 0, page, pageSize);

        var trimmed = query.Trim();

        IQueryable<SearchIndexEntry> q = db.SearchIndex.Where(x => x.IsPublished);
        if (!includeMembersOnly) q = q.Where(x => !x.IsMembersOnly);

        var fts = await IsFtsAvailableAsync(db, ct).ConfigureAwait(false);

        if (fts)
        {
            try
            {
                var ftsTerms = ToFtsTerms(trimmed);
                q = q.Where(x => EF.Functions.Contains(x.Title, ftsTerms) || EF.Functions.Contains(x.BodyText, ftsTerms));
            }
            catch
            {
                // Fall back if FTS query throws (e.g. invalid term shape).
                fts = false;
            }
        }

        if (!fts)
        {
            // LIKE-based fallback. Split on whitespace; require all terms to
            // appear in either Title or BodyText.
            var terms = trimmed.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var term in terms)
            {
                var pattern = $"%{term}%";
                q = q.Where(x => EF.Functions.Like(x.Title, pattern) || EF.Functions.Like(x.BodyText, pattern));
            }
        }

        var total = await q.CountAsync(ct).ConfigureAwait(false);
        var rows = await q
            .OrderByDescending(x => x.IndexedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct).ConfigureAwait(false);

        var items = rows.Select(r => new SearchResultItem(
            r.EntityType, r.EntityId, r.Title,
            BuildSnippet(r.BodyText, trimmed), r.Url, r.IsMembersOnly)).ToList();

        return new SearchResults(items, total, page, pageSize);
    }

    public async Task<int> CountAsync(CancellationToken ct = default)
    {
        await using var scope = _scopes.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await db.SearchIndex.CountAsync(ct).ConfigureAwait(false);
    }

    private async Task<bool> IsFtsAvailableAsync(ApplicationDbContext db, CancellationToken ct)
    {
        if (_ftsAvailable.HasValue) return _ftsAvailable.Value;
        await _ftsLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_ftsAvailable.HasValue) return _ftsAvailable.Value;
            try
            {
                // SERVERPROPERTY('IsFullTextInstalled') = 1 in environments
                // where the FTS service is available.
                var installed = await db.Database
                    .SqlQuery<int>($"SELECT CAST(SERVERPROPERTY('IsFullTextInstalled') AS int) AS Value")
                    .FirstOrDefaultAsync(ct).ConfigureAwait(false);
                _ftsAvailable = installed == 1;
                _logger.LogInformation("FTS availability probe: {Available}", _ftsAvailable);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "FTS probe failed; falling back to LIKE.");
                _ftsAvailable = false;
            }
            return _ftsAvailable.Value;
        }
        finally
        {
            _ftsLock.Release();
        }
    }

    private static string ToFtsTerms(string query)
    {
        // Wrap each term in quotes and join with AND so a multi-word search
        // requires all terms.
        var terms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(t => $"\"{t.Replace("\"", "")}\"");
        return string.Join(" AND ", terms);
    }

    private static string BuildSnippet(string body, string query, int maxLength = 180)
    {
        if (string.IsNullOrWhiteSpace(body)) return string.Empty;
        var firstTerm = query.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        var idx = firstTerm is null ? -1
            : body.IndexOf(firstTerm, StringComparison.OrdinalIgnoreCase);
        var start = idx < 0 ? 0 : Math.Max(0, idx - 40);
        var window = body[start..Math.Min(body.Length, start + maxLength)];
        return (start > 0 ? "…" : "") + window.Trim() + (start + maxLength < body.Length ? "…" : "");
    }

    /// <summary>Crude ProseMirror-JSON-to-text extractor mirroring
    /// <c>PageService.AutoExcerpt</c> — lifted to avoid the cross-namespace
    /// dependency.</summary>
    private static string ExtractText(string? json, int max = 8000)
    {
        if (string.IsNullOrWhiteSpace(json)) return string.Empty;
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(json);
            var sb = new System.Text.StringBuilder(Math.Min(max, 1024));
            Walk(doc.RootElement, sb, max);
            var text = sb.ToString().Trim();
            return text.Length <= max ? text : text[..max];
        }
        catch (System.Text.Json.JsonException)
        {
            return string.Empty;
        }
    }

    private static void Walk(System.Text.Json.JsonElement el, System.Text.StringBuilder sb, int max)
    {
        if (sb.Length >= max) return;
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
                foreach (var c in content.EnumerateArray()) Walk(c, sb, max);
            }
        }
        else if (el.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var c in el.EnumerateArray()) Walk(c, sb, max);
        }
    }
}
