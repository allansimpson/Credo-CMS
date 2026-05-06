namespace CredoCms.Application.RealTime;

public sealed record ContentChangedMessage(string EntityType, Guid EntityId, string Action);

/// <summary>Group join-request submission. Sent to <c>admins</c> + each group leader.</summary>
public sealed record GroupJoinRequestMessage(Guid GroupId, string GroupName, Guid RequesterUserId, string RequesterDisplayName);

/// <summary>Group join-request resolution. Sent to <c>user-{requesterId}</c>.</summary>
public sealed record GroupMembershipDecisionMessage(Guid GroupId, string GroupName, bool Approved);

/// <summary>Prayer request events. Sent to the <c>members</c> SignalR group;
/// Editors and Administrators are also in <c>members</c> so admin shells
/// receive the same stream.</summary>
public sealed record PrayerRequestEventMessage(
    string Kind,
    Guid PrayerRequestId,
    string Title,
    int? PrayedForCount = null);

/// <summary>Connect-card submission summary. Sent to <c>admins</c> on every
/// successful submit so admin shells get a real-time toast.</summary>
public sealed record ConnectCardSummaryMessage(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    DateTimeOffset SubmittedAt);

/// <summary>Phase 5 broadcast send lifecycle / aggregate-stats events.
/// Sent to <c>admins</c> so admin shells watching the broadcast detail
/// page get live updates as webhook events roll in.</summary>
public sealed record BroadcastLifecycleMessage(
    string Kind,
    Guid BroadcastId,
    int? RecipientCount = null,
    int? DeliveredCount = null,
    int? BouncedCount = null,
    int? ComplaintCount = null,
    int? OpenCount = null);

/// <summary>Phase 5 volunteer slot lifecycle events.
/// Sent to <c>admins</c> so event organizers see slots fill in real time.</summary>
public sealed record VolunteerSlotMessage(
    string Kind,
    Guid EventId,
    Guid RoleId,
    DateOnly OccurrenceDate,
    string RoleName,
    int FilledSlots,
    int SlotsNeeded);

public interface IRealtimeNotifier
{
    Task NotifyContentChangedAsync(ContentChangedMessage message, CancellationToken ct = default);

    /// <summary>Broadcast a group join request to admin shell + each group
    /// leader's per-user channel.</summary>
    Task NotifyGroupJoinRequestSubmittedAsync(
        GroupJoinRequestMessage message,
        IReadOnlyCollection<Guid> leaderUserIds,
        CancellationToken ct = default);

    /// <summary>Notify the requester their join request was approved or declined.</summary>
    Task NotifyGroupMembershipDecisionAsync(
        Guid requesterUserId,
        GroupMembershipDecisionMessage message,
        CancellationToken ct = default);

    /// <summary>Broadcast a prayer-request event to the <c>members</c> group.
    /// The same channel powers the member list page and the admin moderation
    /// queue.</summary>
    Task NotifyPrayerRequestEventAsync(PrayerRequestEventMessage message, CancellationToken ct = default);

    /// <summary>Notify the admin shell of a new connect-card submission.</summary>
    Task NotifyConnectCardSubmittedAsync(ConnectCardSummaryMessage message, CancellationToken ct = default);

    /// <summary>Phase 5: broadcast-send and stats-update events.</summary>
    Task NotifyBroadcastLifecycleAsync(BroadcastLifecycleMessage message, CancellationToken ct = default);

    /// <summary>Phase 5: volunteer slot filled / opened.</summary>
    Task NotifyVolunteerSlotAsync(VolunteerSlotMessage message, CancellationToken ct = default);
}
