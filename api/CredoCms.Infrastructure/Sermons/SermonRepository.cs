using CredoCms.Application.Common;
using CredoCms.Application.Scripture;
using CredoCms.Application.Sermons;
using CredoCms.Application.Tags;
using CredoCms.Domain.Bible;
using CredoCms.Domain.Sermons;
using CredoCms.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CredoCms.Infrastructure.Sermons;

public sealed class SermonRepository : ISermonRepository
{
    private const string ScriptureParentEntityType = nameof(Sermon);

    private readonly ApplicationDbContext _db;
    public SermonRepository(ApplicationDbContext db) => _db = db;

    public async Task<PagedResult<SermonListItemDto>> ListAsync(SermonListQuery query, CancellationToken ct = default)
    {
        IQueryable<Sermon> q = query.IncludeDeleted ? _db.Sermons.IgnoreQueryFilters() : _db.Sermons;
        if (query.IncludeDeleted) q = q.Where(s => s.IsDeleted);

        if (query.PublishedOnly == true) q = q.Where(s => s.IsPublished);
        if (query.SermonSeriesId is { } seriesId) q = q.Where(s => s.SermonSeriesId == seriesId);
        if (query.SpeakerLeaderId is { } leaderId) q = q.Where(s => s.SpeakerLeaderId == leaderId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            q = q.Where(s => EF.Functions.Like(s.Title, pattern) || EF.Functions.Like(s.Slug, pattern));
        }

        if (!string.IsNullOrWhiteSpace(query.TagSlug))
        {
            var tag = await _db.Tags.FirstOrDefaultAsync(t => t.Slug == query.TagSlug, ct).ConfigureAwait(false);
            if (tag is null)
                return new PagedResult<SermonListItemDto>(Array.Empty<SermonListItemDto>(), 0, query.Page, query.PageSize);
            var tagId = tag.Id;
            q = q.Where(s => _db.SermonTags.Any(t => t.SermonId == s.Id && t.TagId == tagId));
        }

        if (query.BookFilter is { } bookValue)
        {
            var book = (BibleBook)bookValue;
            q = q.Where(s => _db.ScriptureReferences.Any(r =>
                r.ParentEntityType == ScriptureParentEntityType
                && r.ParentEntityId == s.Id
                && r.Book == book));
        }

        var total = await q.CountAsync(ct).ConfigureAwait(false);
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var rows = await q
            .OrderByDescending(s => s.PublishedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(s => new
            {
                s.Id,
                s.Slug,
                s.Title,
                s.ThumbnailBlobUrl,
                s.ThumbnailWebpBlobUrl,
                s.PublishedAt,
                s.IsPublished,
                s.IsMembersOnly,
                s.IsDeleted,
                s.SermonSeriesId,
                SeriesTitle = s.SermonSeriesId == null ? null
                    : _db.SermonSeries.Where(ss => ss.Id == s.SermonSeriesId).Select(ss => ss.Title).FirstOrDefault(),
                SpeakerLeaderName = s.SpeakerLeaderId == null ? null
                    : _db.Leaders.Where(l => l.Id == s.SpeakerLeaderId).Select(l => l.FullName).FirstOrDefault(),
                s.SpeakerNameFreeText,
            })
            .ToListAsync(ct).ConfigureAwait(false);

        var items = rows.Select(r => new SermonListItemDto(
            r.Id, r.Slug, r.Title, r.ThumbnailBlobUrl, r.ThumbnailWebpBlobUrl,
            r.PublishedAt, r.IsPublished, r.IsMembersOnly, r.IsDeleted,
            r.SpeakerLeaderName ?? r.SpeakerNameFreeText,
            r.SeriesTitle, r.SermonSeriesId)).ToList();

        return new PagedResult<SermonListItemDto>(items, total, page, pageSize);
    }

    public Task<Sermon?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default)
    {
        var q = includeDeleted ? _db.Sermons.IgnoreQueryFilters() : _db.Sermons;
        return q.FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public Task<Sermon?> GetBySlugAsync(string slug, CancellationToken ct = default)
        => _db.Sermons.FirstOrDefaultAsync(s => s.Slug == slug, ct);

    public Task<Sermon?> GetByYouTubeVideoIdAsync(string videoId, bool includeDeleted = false, CancellationToken ct = default)
    {
        var q = includeDeleted ? _db.Sermons.IgnoreQueryFilters() : _db.Sermons;
        return q.FirstOrDefaultAsync(s => s.YouTubeVideoId == videoId, ct);
    }

    public Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken ct = default)
    {
        var q = _db.Sermons.AsQueryable();
        if (excludingId is not null) q = q.Where(s => s.Id != excludingId);
        return q.AnyAsync(s => s.Slug == slug, ct);
    }

    public async Task<List<Guid>> GetTagIdsAsync(Guid sermonId, CancellationToken ct = default)
        => await _db.SermonTags.Where(t => t.SermonId == sermonId).Select(t => t.TagId).ToListAsync(ct).ConfigureAwait(false);

    public async Task ReplaceTagsAsync(Guid sermonId, IEnumerable<Guid> tagIds, CancellationToken ct = default)
    {
        var nextSet = tagIds.Distinct().ToHashSet();
        var existing = await _db.SermonTags.Where(t => t.SermonId == sermonId).ToListAsync(ct).ConfigureAwait(false);

        var existingSet = existing.Select(t => t.TagId).ToHashSet();
        var toRemove = existing.Where(t => !nextSet.Contains(t.TagId)).ToList();
        var toAdd = nextSet.Where(id => !existingSet.Contains(id)).ToList();

        if (toRemove.Count > 0)
        {
            _db.SermonTags.RemoveRange(toRemove);
            // Decrement usage counts for removed tag links.
            foreach (var t in toRemove)
            {
                await _db.Tags.Where(tag => tag.Id == t.TagId)
                    .ExecuteUpdateAsync(s => s.SetProperty(tag => tag.UsageCount, tag => tag.UsageCount - 1), ct)
                    .ConfigureAwait(false);
            }
        }
        if (toAdd.Count > 0)
        {
            _db.SermonTags.AddRange(toAdd.Select(tagId => new SermonTag { SermonId = sermonId, TagId = tagId }));
            foreach (var tagId in toAdd)
            {
                await _db.Tags.Where(tag => tag.Id == tagId)
                    .ExecuteUpdateAsync(s => s.SetProperty(tag => tag.UsageCount, tag => tag.UsageCount + 1), ct)
                    .ConfigureAwait(false);
            }
        }
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<List<Guid>> GetAttachmentIdsAsync(Guid sermonId, CancellationToken ct = default)
        => await _db.SermonAttachments.Where(a => a.SermonId == sermonId)
            .OrderBy(a => a.DisplayOrder).Select(a => a.DocumentId).ToListAsync(ct).ConfigureAwait(false);

    public async Task ReplaceAttachmentsAsync(Guid sermonId, IEnumerable<Guid> documentIds, CancellationToken ct = default)
    {
        await _db.SermonAttachments.Where(a => a.SermonId == sermonId)
            .ExecuteDeleteAsync(ct).ConfigureAwait(false);

        var ids = documentIds.ToList();
        for (int i = 0; i < ids.Count; i++)
        {
            _db.SermonAttachments.Add(new SermonAttachment
            {
                SermonId = sermonId,
                DocumentId = ids[i],
                DisplayOrder = i,
            });
        }
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<bool> AreAllPublicPdfsAsync(IEnumerable<Guid> documentIds, CancellationToken ct = default)
    {
        var ids = documentIds.Distinct().ToList();
        if (ids.Count == 0) return true;
        var matchCount = await _db.Documents
            .Where(d => ids.Contains(d.Id) && !d.IsMembersOnly && d.IsPublished)
            .CountAsync(ct).ConfigureAwait(false);
        return matchCount == ids.Count;
    }

    public async Task AddAsync(Sermon sermon, CancellationToken ct = default)
    {
        _db.Sermons.Add(sermon);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task UpdateAsync(Sermon sermon, CancellationToken ct = default)
    {
        _db.Sermons.Update(sermon);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
    }

    public async Task<bool> HardDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var sermon = await _db.Sermons.IgnoreQueryFilters().FirstOrDefaultAsync(s => s.Id == id, ct).ConfigureAwait(false);
        if (sermon is null) return false;
        _db.Sermons.Remove(sermon);
        await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        return true;
    }

    public async Task<List<SermonsByBookCount>> CountByBookAsync(bool includeMembersOnly, CancellationToken ct = default)
    {
        // Inner-join sermons + scripture refs; count distinct sermons per book.
        var q =
            from r in _db.ScriptureReferences
            where r.ParentEntityType == ScriptureParentEntityType
            join s in _db.Sermons on r.ParentEntityId equals s.Id
            where s.IsPublished && (includeMembersOnly || !s.IsMembersOnly)
            group r by r.Book into g
            select new SermonsByBookCount((int)g.Key, g.Select(x => x.ParentEntityId).Distinct().Count());

        return await q.ToListAsync(ct).ConfigureAwait(false);
    }

    public async Task<SermonDetailDto?> ToDetailAsync(Sermon sermon, CancellationToken ct = default)
    {
        var tagIds = await GetTagIdsAsync(sermon.Id, ct).ConfigureAwait(false);
        var tags = await _db.Tags.Where(t => tagIds.Contains(t.Id))
            .Select(t => new TagDto(t.Id, t.Name, t.Slug, t.UsageCount))
            .ToListAsync(ct).ConfigureAwait(false);

        var attachmentDocs = await (from a in _db.SermonAttachments
                                    join d in _db.Documents on a.DocumentId equals d.Id
                                    where a.SermonId == sermon.Id
                                    orderby a.DisplayOrder
                                    select new SermonAttachmentDto(d.Id, d.Title, a.DisplayOrder))
            .ToListAsync(ct).ConfigureAwait(false);

        var refs = await _db.ScriptureReferences
            .Where(r => r.ParentEntityType == ScriptureParentEntityType && r.ParentEntityId == sermon.Id)
            .OrderBy(r => r.DisplayOrder)
            .Select(r => new ScriptureReferenceDto(r.Id, r.Book, r.ChapterStart, r.VerseStart, r.ChapterEnd, r.VerseEnd, r.DisplayOrder))
            .ToListAsync(ct).ConfigureAwait(false);

        return new SermonDetailDto(
            sermon.Id, sermon.Slug, sermon.Title, sermon.DescriptionJson,
            sermon.YouTubeVideoId, sermon.YouTubeChannelId,
            sermon.ThumbnailBlobUrl, sermon.ThumbnailWebpBlobUrl,
            sermon.PublishedAt, sermon.YouTubePublishedAt, sermon.DurationSeconds,
            sermon.Transcript, sermon.TranscriptSource,
            sermon.SpeakerLeaderId, sermon.SpeakerNameFreeText,
            sermon.SermonSeriesId,
            sermon.IsPublished, sermon.IsMembersOnly, sermon.IsDeleted,
            tags, attachmentDocs, refs,
            sermon.CreatedAt, sermon.ModifiedAt, sermon.ModifiedByUserId, sermon.DeletedAt);
    }

    public async Task<PublicSermonDto?> ToPublicAsync(Sermon sermon, CancellationToken ct = default)
    {
        var detail = await ToDetailAsync(sermon, ct).ConfigureAwait(false);
        if (detail is null) return null;

        string? speakerName = null;
        if (sermon.SpeakerLeaderId is { } leaderId)
        {
            speakerName = await _db.Leaders.Where(l => l.Id == leaderId)
                .Select(l => l.FullName).FirstOrDefaultAsync(ct).ConfigureAwait(false);
        }
        speakerName ??= sermon.SpeakerNameFreeText;

        string? seriesTitle = null, seriesSlug = null;
        if (sermon.SermonSeriesId is { } seriesId)
        {
            var s = await _db.SermonSeries.Where(x => x.Id == seriesId)
                .Select(x => new { x.Title, x.Slug })
                .FirstOrDefaultAsync(ct).ConfigureAwait(false);
            seriesTitle = s?.Title;
            seriesSlug = s?.Slug;
        }

        return new PublicSermonDto(
            sermon.Id, sermon.Slug, sermon.Title, sermon.DescriptionJson,
            sermon.YouTubeVideoId, sermon.ThumbnailBlobUrl, sermon.ThumbnailWebpBlobUrl,
            sermon.PublishedAt, sermon.DurationSeconds,
            sermon.Transcript,
            sermon.SpeakerLeaderId, speakerName,
            sermon.SermonSeriesId, seriesTitle, seriesSlug,
            sermon.IsMembersOnly,
            detail.Tags, detail.Attachments, detail.ScriptureReferences);
    }
}
