using System.Text;
using CredoCms.Domain.Tags;

namespace CredoCms.Application.Tags;

public sealed record TagDto(Guid Id, string Name, string Slug, int UsageCount);

public interface ITagRepository
{
    Task<Tag?> GetByNameInsensitiveAsync(string name, CancellationToken ct = default);
    Task<List<Tag>> SearchAsync(string query, int limit, CancellationToken ct = default);
    Task<List<Tag>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task AddAsync(Tag tag, CancellationToken ct = default);
    Task UpdateUsageAsync(Guid id, int delta, CancellationToken ct = default);
}

public interface ITagService
{
    /// <summary>
    /// Normalizes and either returns the existing matching <see cref="Tag"/>
    /// or creates a new one. Case-insensitive name match.
    /// </summary>
    Task<Tag> NormalizeAndUpsertAsync(string name, CancellationToken ct = default);

    Task<List<TagDto>> SearchAsync(string query, int limit = 20, CancellationToken ct = default);

    Task IncrementUsageAsync(Guid id, CancellationToken ct = default);
    Task DecrementUsageAsync(Guid id, CancellationToken ct = default);
}

public sealed class TagService : ITagService
{
    private readonly ITagRepository _repo;

    public TagService(ITagRepository repo) => _repo = repo;

    public async Task<Tag> NormalizeAndUpsertAsync(string name, CancellationToken ct = default)
    {
        var trimmed = (name ?? string.Empty).Trim();
        if (trimmed.Length == 0)
            throw new ArgumentException("Tag name cannot be empty.", nameof(name));
        if (trimmed.Length > 100)
            trimmed = trimmed[..100];

        var canonical = ToTitleCase(trimmed);

        var existing = await _repo.GetByNameInsensitiveAsync(canonical, ct).ConfigureAwait(false);
        if (existing is not null) return existing;

        var tag = new Tag
        {
            Id = Guid.NewGuid(),
            Name = canonical,
            Slug = Slugify(canonical),
            CreatedAt = DateTimeOffset.UtcNow,
            UsageCount = 0,
        };
        await _repo.AddAsync(tag, ct).ConfigureAwait(false);
        return tag;
    }

    public async Task<List<TagDto>> SearchAsync(string query, int limit = 20, CancellationToken ct = default)
    {
        var rows = await _repo.SearchAsync(query ?? string.Empty, Math.Clamp(limit, 1, 100), ct)
            .ConfigureAwait(false);
        return rows.Select(t => new TagDto(t.Id, t.Name, t.Slug, t.UsageCount)).ToList();
    }

    public Task IncrementUsageAsync(Guid id, CancellationToken ct = default)
        => _repo.UpdateUsageAsync(id, +1, ct);

    public Task DecrementUsageAsync(Guid id, CancellationToken ct = default)
        => _repo.UpdateUsageAsync(id, -1, ct);

    /// <summary>Title-cases a tag name. Preserves all-uppercase short tokens
    /// (treated as acronyms — "OT" stays "OT").</summary>
    internal static string ToTitleCase(string s)
    {
        var parts = s.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var sb = new StringBuilder();
        for (int i = 0; i < parts.Length; i++)
        {
            if (i > 0) sb.Append(' ');
            var p = parts[i];
            if (p.Length <= 3 && string.Equals(p, p.ToUpperInvariant(), StringComparison.Ordinal))
            {
                sb.Append(p);
                continue;
            }
            sb.Append(char.ToUpperInvariant(p[0]));
            if (p.Length > 1) sb.Append(p[1..].ToLowerInvariant());
        }
        return sb.ToString();
    }

    internal static string Slugify(string name)
    {
        var lower = name.ToLowerInvariant();
        var sb = new StringBuilder(lower.Length);
        foreach (var c in lower) sb.Append(char.IsLetterOrDigit(c) ? c : '-');
        var slug = sb.ToString().Trim('-');
        while (slug.Contains("--", StringComparison.Ordinal))
            slug = slug.Replace("--", "-", StringComparison.Ordinal);
        if (slug.Length == 0) slug = "tag";
        if (slug.Length > 120) slug = slug[..120];
        return slug;
    }
}
