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
}
