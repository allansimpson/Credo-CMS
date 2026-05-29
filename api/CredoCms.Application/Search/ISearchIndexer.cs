namespace CredoCms.Application.Search;

public sealed record SearchUpsertCommand(
    string EntityType, Guid EntityId,
    string Title, string BodyText, string Url,
    bool IsPublished, bool IsMembersOnly);

public sealed record SearchResultItem(
    string EntityType, Guid EntityId, string Title, string Snippet, string Url, bool IsMembersOnly);

public sealed record SearchResults(
    IReadOnlyList<SearchResultItem> Items,
    int TotalCount,
    int Page,
    int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling((double)TotalCount / PageSize);
}

public interface ISearchIndexer
{
    Task UpsertAsync(SearchUpsertCommand command, CancellationToken ct = default);
    Task RemoveAsync(string entityType, Guid entityId, CancellationToken ct = default);
    Task SetPublishedAsync(string entityType, Guid entityId, bool isPublished, CancellationToken ct = default);
    Task RebuildAllAsync(CancellationToken ct = default);
    Task<SearchResults> SearchAsync(string query, bool includeMembersOnly, int page, int pageSize, CancellationToken ct = default);
    /// <summary>Admin variant — also includes unpublished entries.
    /// Members-only filter is implicitly off; admins see everything.</summary>
    Task<SearchResults> SearchAllAsync(string query, int page, int pageSize, CancellationToken ct = default);
    Task<int> CountAsync(CancellationToken ct = default);
}
