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
}
