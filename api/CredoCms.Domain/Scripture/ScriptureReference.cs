using System.ComponentModel.DataAnnotations;
using CredoCms.Domain.Bible;

namespace CredoCms.Domain.Scripture;

/// <summary>
/// A polymorphic Scripture reference attached to either a Sermon
/// (<c>ParentEntityType="Sermon"</c>) or a SermonSeries
/// (<c>ParentEntityType="SermonSeries"</c>). The polymorphic shape lets
/// "find all sermons referencing Romans" queries span both ownerships;
/// integrity is maintained at the service layer rather than via a real
/// FK. Decision documented in IMPLEMENTATION_NOTES.md.
/// </summary>
public sealed class ScriptureReference
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(40)]
    public string ParentEntityType { get; set; } = string.Empty;

    public Guid ParentEntityId { get; set; }

    public BibleBook Book { get; set; }

    public int ChapterStart { get; set; }

    /// <summary>Null = whole chapter.</summary>
    public int? VerseStart { get; set; }

    /// <summary>Null = same as <see cref="ChapterStart"/>.</summary>
    public int? ChapterEnd { get; set; }

    /// <summary>Null = end of <see cref="ChapterEnd"/>.</summary>
    public int? VerseEnd { get; set; }

    /// <summary>For parents with multiple references, ascending = primary first.</summary>
    public int DisplayOrder { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ModifiedAt { get; set; }

    public Guid? ModifiedByUserId { get; set; }
}
