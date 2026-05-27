using CredoCms.Domain.Common;
using Microsoft.AspNetCore.OutputCaching;

namespace CredoCms.Api.Caching;

/// <summary>
/// Output-cache policy that adds an auth-tier discriminator to the cache
/// key so a member's view of an endpoint never gets served to an anonymous
/// caller (and vice versa). Also forbids caching for any path under
/// <c>/api/admin/</c> or <c>/api/auth/</c>, regardless of attribute.
/// </summary>
public sealed class MemberAuthVaryPolicy : IOutputCachePolicy
{
    public ValueTask CacheRequestAsync(OutputCacheContext context, CancellationToken cancellation)
    {
        var path = context.HttpContext.Request.Path.Value ?? string.Empty;
        if (path.StartsWith("/api/admin/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/auth/", StringComparison.OrdinalIgnoreCase))
        {
            context.EnableOutputCaching = false;
            return ValueTask.CompletedTask;
        }

        var user = context.HttpContext.User;
        var tier = "anon";
        if (user.Identity?.IsAuthenticated == true)
        {
            if (user.IsInRole(SystemConstants.Roles.Administrator)) tier = "admin";
            else if (user.IsInRole(SystemConstants.Roles.Editor)) tier = "editor";
            else if (user.IsInRole(SystemConstants.Roles.Member)) tier = "member";
            else tier = "auth";
        }
        context.CacheVaryByRules.VaryByValues["auth-tier"] = tier;

        return ValueTask.CompletedTask;
    }

    public ValueTask ServeFromCacheAsync(OutputCacheContext context, CancellationToken cancellation)
        => ValueTask.CompletedTask;

    public ValueTask ServeResponseAsync(OutputCacheContext context, CancellationToken cancellation)
        => ValueTask.CompletedTask;
}
