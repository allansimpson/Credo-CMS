using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CredoCms.Api.Hubs;

/// <summary>
/// Real-time notification hub. Phase 1 ships the hub itself but with no methods —
/// the seam is in place so Phase 4 features (prayer-request notifications,
/// moderation queue alerts) can plug in without changing infrastructure.
/// </summary>
[Authorize]
public sealed class NotificationHub : Hub
{
    // Methods land in later phases.
}
