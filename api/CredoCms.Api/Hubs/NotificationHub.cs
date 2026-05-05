using CredoCms.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CredoCms.Api.Hubs;

/// <summary>
/// Real-time notification hub. Phase 2 adds <c>JoinAdminGroup</c> for
/// admin-shell content-change notifications. Phase 4 features (prayer
/// requests, moderation queue) will register additional groups.
/// </summary>
[Authorize]
public sealed class NotificationHub : Hub
{
    public const string AdminGroup = "admins";

    /// <summary>Adds the calling connection to the <c>admins</c> group so
    /// it receives <c>ContentChanged</c> broadcasts. Requires
    /// Editor or Administrator role.</summary>
    public async Task JoinAdminGroup()
    {
        var user = Context.User;
        if (user?.Identity?.IsAuthenticated != true) return;
        if (!user.IsInRole(SystemConstants.Roles.Administrator)
            && !user.IsInRole(SystemConstants.Roles.Editor))
        {
            return;
        }
        await Groups.AddToGroupAsync(Context.ConnectionId, AdminGroup);
    }
}
