using System.Text.Json;
using CredoCms.Application.Versioning;
using CredoCms.Domain.Pages;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CredoCms.Infrastructure.Versioning;

public sealed class PageVersionHandler : IVersionedEntityHandler
{
    public string EntityType => nameof(Page);

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = false };
    private readonly ApplicationDbContext _db;
    public PageVersionHandler(ApplicationDbContext db) => _db = db;

    public async Task<List<VersionListItem>?> ListAsync(Guid id, CancellationToken ct = default)
    {
        var current = await _db.Pages.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id, ct).ConfigureAwait(false);
        if (current is null) return null;

        var rows = await _db.Pages.TemporalAll()
            .IgnoreQueryFilters()
            .Where(p => p.Id == id)
            .OrderByDescending(p => EF.Property<DateTime>(p, "ValidFrom"))
            .Select(p => new
            {
                ValidFrom = EF.Property<DateTime>(p, "ValidFrom"),
                ValidTo = EF.Property<DateTime>(p, "ValidTo"),
                p.Title,
                p.ModifiedByUserId,
            })
            .ToListAsync(ct).ConfigureAwait(false);

        return rows.Select(r => new VersionListItem(
            new DateTimeOffset(r.ValidFrom, TimeSpan.Zero),
            new DateTimeOffset(r.ValidTo, TimeSpan.Zero),
            r.Title, r.ModifiedByUserId)).ToList();
    }

    public async Task<VersionSnapshot?> GetAsOfAsync(Guid id, DateTimeOffset asOfUtc, CancellationToken ct = default)
    {
        var snapshot = await _db.Pages.TemporalAsOf(asOfUtc.UtcDateTime)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == id, ct).ConfigureAwait(false);
        if (snapshot is null) return null;
        return new VersionSnapshot(
            asOfUtc, asOfUtc, snapshot.Title, snapshot.ModifiedByUserId,
            JsonSerializer.Serialize(snapshot, JsonOptions));
    }

    public async Task<VersionRestoreResult> RestoreAsync(Guid id, DateTimeOffset asOfUtc, CancellationToken ct = default)
    {
        var snapshot = await _db.Pages.TemporalAsOf(asOfUtc.UtcDateTime)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == id, ct).ConfigureAwait(false);
        if (snapshot is null) return new VersionRestoreResult(false, new[] { "No version at that timestamp." });

        var current = await _db.Pages.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id, ct).ConfigureAwait(false);
        if (current is null) return new VersionRestoreResult(false, new[] { "Page no longer exists." });

        // Copy scalars (per VERSIONING.md §7); do NOT copy Id/CreatedAt/system flags.
        current.Slug = snapshot.Slug;
        current.Title = snapshot.Title;
        current.BodyJson = snapshot.BodyJson;
        current.Excerpt = snapshot.Excerpt;
        current.HeroImageUrl = snapshot.HeroImageUrl;
        current.HeroImageWebpUrl = snapshot.HeroImageWebpUrl;
        current.HeroImageAlt = snapshot.HeroImageAlt;
        current.MetaDescription = snapshot.MetaDescription;
        current.IsPublished = snapshot.IsPublished;
        current.IsMembersOnly = snapshot.IsMembersOnly;
        current.ModifiedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return new VersionRestoreResult(true, Array.Empty<string>());
    }
}
