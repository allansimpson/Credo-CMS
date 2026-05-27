using CredoCms.Domain.Common;

namespace CredoCms.Api.Composition;

internal static class AuthorizationConfiguration
{
    public static void AddAuthorizationPolicies(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AuthorizationPolicies.AdministratorOnly, p =>
                p.RequireRole(SystemConstants.Roles.Administrator));
            options.AddPolicy(AuthorizationPolicies.AdminShell, p =>
                p.RequireRole(SystemConstants.Roles.AdminShellRoles.ToArray()));
            options.AddPolicy(AuthorizationPolicies.AnyAuthenticated, p =>
                p.RequireAuthenticatedUser());
        });
    }
}
