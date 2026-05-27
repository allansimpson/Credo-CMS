using CredoCms.Domain.Bible;
using CredoCms.Domain.Scripture;

namespace CredoCms.Application.Scripture;

public sealed record ScriptureReferenceDto(
    Guid Id,
    BibleBook Book,
    int ChapterStart,
    int? VerseStart,
    int? ChapterEnd,
    int? VerseEnd,
    int DisplayOrder);

public sealed record ScriptureReferenceInput(
    BibleBook Book,
    int ChapterStart,
    int? VerseStart,
    int? ChapterEnd,
    int? VerseEnd);

public sealed class ScriptureReferenceValidationException : Exception
{
    public ScriptureReferenceValidationException(string message) : base(message) { }
}

public interface IScriptureReferenceRepository
{
    Task<List<ScriptureReference>> ListForParentAsync(string parentEntityType, Guid parentEntityId, CancellationToken ct = default);
    Task ReplaceAllAsync(string parentEntityType, Guid parentEntityId, IEnumerable<ScriptureReference> next, CancellationToken ct = default);
    Task DeleteAllForParentAsync(string parentEntityType, Guid parentEntityId, CancellationToken ct = default);
}

public interface IScriptureReferenceService
{
    Task<List<ScriptureReferenceDto>> ListForParentAsync(string parentEntityType, Guid parentEntityId, CancellationToken ct = default);

    /// <summary>
    /// Replace-all-on-save semantics: deletes existing references for the
    /// parent and inserts the new set. Validates each input against book
    /// metadata before persisting.
    /// </summary>
    Task ReplaceAllAsync(string parentEntityType, Guid parentEntityId, IList<ScriptureReferenceInput> inputs, CancellationToken ct = default);

    Task DeleteAllForParentAsync(string parentEntityType, Guid parentEntityId, CancellationToken ct = default);

    /// <summary>Validates a single reference against book metadata.</summary>
    static (bool Ok, string? Error) Validate(ScriptureReferenceInput input)
    {
        var info = BibleBooks.Get(input.Book);
        if (input.ChapterStart < 1 || input.ChapterStart > info.ChapterCount)
            return (false, $"Chapter {input.ChapterStart} is out of range for {info.Name} (1–{info.ChapterCount}).");
        if (input.VerseStart is { } vs && vs < 1)
            return (false, "Starting verse must be ≥ 1.");

        var endChapter = input.ChapterEnd ?? input.ChapterStart;
        if (endChapter < input.ChapterStart)
            return (false, "Ending chapter must be ≥ starting chapter.");
        if (endChapter > info.ChapterCount)
            return (false, $"Chapter {endChapter} is out of range for {info.Name} (1–{info.ChapterCount}).");

        if (input.VerseEnd is { } ve)
        {
            if (ve < 1) return (false, "Ending verse must be ≥ 1.");
            if (input.ChapterStart == endChapter && input.VerseStart is { } vs2 && ve < vs2)
                return (false, "Ending verse must be ≥ starting verse within the same chapter.");
        }

        return (true, null);
    }
}

public sealed class ScriptureReferenceService : IScriptureReferenceService
{
    private readonly IScriptureReferenceRepository _repo;
    public ScriptureReferenceService(IScriptureReferenceRepository repo) => _repo = repo;

    public async Task<List<ScriptureReferenceDto>> ListForParentAsync(string parentEntityType, Guid parentEntityId, CancellationToken ct = default)
    {
        var rows = await _repo.ListForParentAsync(parentEntityType, parentEntityId, ct).ConfigureAwait(false);
        return rows
            .OrderBy(r => r.DisplayOrder)
            .Select(r => new ScriptureReferenceDto(r.Id, r.Book, r.ChapterStart, r.VerseStart, r.ChapterEnd, r.VerseEnd, r.DisplayOrder))
            .ToList();
    }

    public async Task ReplaceAllAsync(string parentEntityType, Guid parentEntityId, IList<ScriptureReferenceInput> inputs, CancellationToken ct = default)
    {
        for (var i = 0; i < inputs.Count; i++)
        {
            var (ok, err) = IScriptureReferenceService.Validate(inputs[i]);
            if (!ok) throw new ScriptureReferenceValidationException($"Reference {i + 1}: {err}");
        }

        var now = DateTimeOffset.UtcNow;
        var rows = inputs.Select((input, i) => new ScriptureReference
        {
            Id = Guid.NewGuid(),
            ParentEntityType = parentEntityType,
            ParentEntityId = parentEntityId,
            Book = input.Book,
            ChapterStart = input.ChapterStart,
            VerseStart = input.VerseStart,
            ChapterEnd = input.ChapterEnd,
            VerseEnd = input.VerseEnd,
            DisplayOrder = i,
            CreatedAt = now,
            ModifiedAt = now,
        }).ToList();

        await _repo.ReplaceAllAsync(parentEntityType, parentEntityId, rows, ct).ConfigureAwait(false);
    }

    public Task DeleteAllForParentAsync(string parentEntityType, Guid parentEntityId, CancellationToken ct = default)
        => _repo.DeleteAllForParentAsync(parentEntityType, parentEntityId, ct);
}
