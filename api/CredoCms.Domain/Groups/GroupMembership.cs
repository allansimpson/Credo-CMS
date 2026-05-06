using System.ComponentModel.DataAnnotations;

namespace CredoCms.Domain.Groups;

public enum GroupMembershipStatus
{
    /// <summary>Self-requested join; awaiting Leader/Editor/Admin approval.</summary>
    Pending = 0,
    /// <summary>Approved; counts toward roster.</summary>
    Active = 1,
    /// <summary>Approver explicitly declined the request.</summary>
    Declined = 2,
    /// <summary>Member left or was removed; historical record retained.</summary>
    Removed = 3,
}

/// <summary>
/// Roster row for a single user-group pair. Not versioned (audit log captures
/// status changes). The (GroupId, UserId) pair is unique among non-Removed rows;
/// re-joining after Removed creates a new row.
/// </summary>
public sealed class GroupMembership
{
    public Guid Id { get; set; }

    public Guid GroupId { get; set; }

    public Guid UserId { get; set; }

    public bool IsLeader { get; set; }

    public GroupMembershipStatus Status { get; set; } = GroupMembershipStatus.Pending;

    [MaxLength(1000)]
    public string? JoinRequestMessage { get; set; }

    public DateTimeOffset? JoinedAt { get; set; }

    public DateTimeOffset? RequestedAt { get; set; }

    public Guid? ProcessedByUserId { get; set; }

    public DateTimeOffset? ProcessedAt { get; set; }

    /// <summary>Per-group notification override. Null means use the user's
    /// global <c>ReceiveGroupEmailsGlobal</c> default.</summary>
    public bool? ReceiveGroupEmails { get; set; }
}
