using System.Security.Claims;
using CredoCms.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace CredoCms.Api.Hubs;

/// <summary>
/// Real-time notification hub. Provides <c>JoinAdminGroup</c> for
/// admin-shell content-change notifications and the per-user channel
/// <c>UserGroup({userId})</c> for direct messaging — used by group
/// join-request leader notifications and approval responses.
/// </summary>
[Authorize]
public sealed class NotificationHub : Hub
{
    public const string AdminGroup = "admins";

    /// <summary>Members + Editors + Administrators (Editors/Admins are
    /// auto-joined so admin shells get the same prayer-request stream the
    /// member list shows).</summary>
    public const string MembersGroup = "members";

    /// <summary>SignalR group name for messaging a single user across all
    /// their open connections.</summary>
    public static string UserGroup(Guid userId) => $"user-{userId:N}";

    public override async Task OnConnectedAsync()
    {
        // Always auto-join the per-user channel for any authenticated caller.
        // Group memberships are managed via UserGroup; per-user delivery is
        // the primary cross-feature channel.
        var user = Context.User;
        if (user?.Identity?.IsAuthenticated == true
            && Guid.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, UserGroup(userId)).ConfigureAwait(false);

            // Members + Editors + Administrators all receive the prayer-
            // request stream. Auto-joining covers the common case; the
            // explicit JoinAdminGroup remains for the admin-only stream.
            if (user.IsInRole(SystemConstants.Roles.Member)
                || user.IsInRole(SystemConstants.Roles.Editor)
                || user.IsInRole(SystemConstants.Roles.Administrator))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, MembersGroup).ConfigureAwait(false);
            }
        }
        await base.OnConnectedAsync().ConfigureAwait(false);
    }

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
