using CredoCms.Api.Hubs;
using CredoCms.Application.RealTime;
using Microsoft.AspNetCore.SignalR;

namespace CredoCms.Api.RealTime;

public sealed class SignalRRealtimeNotifier : IRealtimeNotifier
{
    private readonly IHubContext<NotificationHub> _hub;
    public SignalRRealtimeNotifier(IHubContext<NotificationHub> hub) => _hub = hub;

    public Task NotifyContentChangedAsync(ContentChangedMessage message, CancellationToken ct = default)
        => _hub.Clients.Group(NotificationHub.AdminGroup)
            .SendAsync("ContentChanged", message, ct);

    public async Task NotifyGroupJoinRequestSubmittedAsync(
        GroupJoinRequestMessage message,
        IReadOnlyCollection<Guid> leaderUserIds,
        CancellationToken ct = default)
    {
        // Admin shell first, then each leader's per-user channel. Admins are
        // already in the admins group; leaders may not be (Members can lead),
        // so they need the user-scoped channel.
        await _hub.Clients.Group(NotificationHub.AdminGroup)
            .SendAsync("GroupJoinRequestSubmitted", message, ct).ConfigureAwait(false);

        foreach (var leaderId in leaderUserIds)
        {
            await _hub.Clients.Group(NotificationHub.UserGroup(leaderId))
                .SendAsync("GroupJoinRequestSubmitted", message, ct).ConfigureAwait(false);
        }
    }

    public Task NotifyGroupMembershipDecisionAsync(
        Guid requesterUserId,
        GroupMembershipDecisionMessage message,
        CancellationToken ct = default)
        => _hub.Clients.Group(NotificationHub.UserGroup(requesterUserId))
            .SendAsync(message.Approved ? "GroupMembershipApproved" : "GroupMembershipDeclined", message, ct);
}
