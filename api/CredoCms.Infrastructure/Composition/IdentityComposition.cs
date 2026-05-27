using CredoCms.Domain.Identity;
using CredoCms.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CredoCms.Infrastructure.Composition;

internal static class IdentityComposition
{
    public static IServiceCollection AddIdentity(this IServiceCollection services)
    {
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            options.Password.RequiredLength = 12;
            options.Password.RequireUppercase = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireDigit = true;
            options.Password.RequireNonAlphanumeric = true;

            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
            options.Lockout.AllowedForNewUsers = true;

            options.User.RequireUniqueEmail = true;

            options.SignIn.RequireConfirmedEmail = true;
            options.SignIn.RequireConfirmedAccount = true;
        })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.Configure<SecurityStampValidatorOptions>(o =>
        {
            // Short interval so administrative force-logout takes effect quickly.
            o.ValidationInterval = TimeSpan.FromMinutes(1);
        });

        return services;
    }
}
