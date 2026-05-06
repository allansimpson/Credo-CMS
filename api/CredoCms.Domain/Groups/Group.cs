using System.ComponentModel.DataAnnotations;
using CredoCms.Domain.Common;

namespace CredoCms.Domain.Groups;

public enum GroupVisibility
{
    /// <summary>Visible on Get Involved page to anonymous and members.</summary>
    Public = 0,
    /// <summary>Visible on Get Involved page to authenticated members only.</summary>
    MembersOnly = 1,
    /// <summary>Not browseable; admin/leader manages roster directly.</summary>
    Hidden = 2,
}

public enum GroupJoinability
{
    /// <summary>Eligible viewers can self-request to join.</summary>
    Open = 0,
    /// <summary>Admin/leader adds members directly; no self-request UI.</summary>
    InviteOnly = 1,
    /// <summary>Currently not accepting new members; "Closed" badge shown.</summary>
    Closed = 2,
}

public enum MessageOnJoinRequest
{
    /// <summary>No message field shown.</summary>
    Hidden = 0,
    /// <summary>Optional message field.</summary>
    Optional = 1,
    /// <summary>Required message field.</summary>
    Required = 2,
}

public enum RosterVisibility
{
    /// <summary>Only group leaders see the roster on the public-facing detail page.</summary>
    LeadersOnly = 0,
    /// <summary>All active members of the group see the roster.</summary>
    AllGroupMembers = 1,
}

/// <summary>
/// A church group / ministry. Replaces the "Ministries" content type concept from
/// the original spec. Admin-created with optional Group Leader designation,
/// configurable join-request flow, and a public-facing "Get Involved" landing page.
/// </summary>
public sealed class Group : IVersionedEntity
{
    public Guid Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Slug { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    /// <summary>ProseMirror JSON.</summary>
    public string? DescriptionJson { get; set; }

    [MaxLength(2000)]
    public string? ImageBlobUrl { get; set; }

    [MaxLength(2000)]
    public string? ImageWebpBlobUrl { get; set; }

    [MaxLength(500)]
    public string? ImageAltText { get; set; }

    [MaxLength(200)]
    public string? ContactEmail { get; set; }

    [MaxLength(500)]
    public string? MeetingInfo { get; set; }

    public GroupVisibility Visibility { get; set; } = GroupVisibility.MembersOnly;

    public GroupJoinability Joinability { get; set; } = GroupJoinability.Open;

    public MessageOnJoinRequest RequiresMessageOnJoinRequest { get; set; } = MessageOnJoinRequest.Optional;

    public RosterVisibility RosterVisibility { get; set; } = RosterVisibility.LeadersOnly;

    public bool IsActive { get; set; } = true;

    public DateTimeOffset CreatedAt { get; set; }

    public Guid? ModifiedByUserId { get; set; }

    public DateTimeOffset ModifiedAt { get; set; }

    public bool IsDeleted { get; set; }

    public DateTimeOffset? DeletedAt { get; set; }

    public Guid? DeletedByUserId { get; set; }
}
