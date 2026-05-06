using System.ComponentModel.DataAnnotations;
using CredoCms.Domain.Common;

namespace CredoCms.Domain.Email;

/// <summary>
/// A reusable email body addressable by a stable <see cref="TemplateKey"/>.
/// Used for both transactional sends (invitation, password reset, ack
/// receipts) and operational digests (admin notifications). System
/// templates are seeded by the application; admins can edit subject and
/// body but cannot change the key or delete the row.
///
/// <para>Versioned: temporal-table history preserves prior wording so an
/// administrator can recover an earlier draft.</para>
/// </summary>
public sealed class EmailTemplate : IVersionedEntity
{
    public Guid Id { get; set; }

    /// <summary>Immutable lookup key (e.g., <c>"InvitationEmail"</c>). Indexed unique.</summary>
    [Required]
    [MaxLength(100)]
    public string TemplateKey { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Subject { get; set; } = string.Empty;

    /// <summary>TipTap ProseMirror JSON.</summary>
    [Required]
    public string HtmlBody { get; set; } = string.Empty;

    /// <summary>Plain-text fallback. Auto-derived from <see cref="HtmlBody"/>
    /// when null, but the editor may override here.</summary>
    public string? PlainTextBody { get; set; }

    /// <summary>JSON array of merge-field names available to this template
    /// (documentation only — the renderer doesn't enforce this list, just
    /// surfaces it to the admin UI).</summary>
    [Required]
    public string AvailableMergeFieldsJson { get; set; } = "[]";

    /// <summary>System templates are seeded; admins can edit subject + body
    /// but cannot delete the row, and the <see cref="TemplateKey"/> is
    /// immutable.</summary>
    public bool IsSystemTemplate { get; set; } = true;

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    public DateTimeOffset ModifiedAt { get; set; }
}
