namespace CredoCms.Application.RealTime;

public sealed record ContentChangedMessage(string EntityType, Guid EntityId, string Action);

/// <summary>Group join-request submission. Sent to <c>admins</c> + each group leader.</summary>
public sealed record GroupJoinRequestMessage(Guid GroupId, string GroupName, Guid RequesterUserId, string RequesterDisplayName);

/// <summary>Group join-request resolution. Sent to <c>user-{requesterId}</c>.</summary>
public sealed record GroupMembershipDecisionMessage(Guid GroupId, string GroupName, bool Approved);

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
}
