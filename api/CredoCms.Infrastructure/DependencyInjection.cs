using CredoCms.Application.Auditing;
using CredoCms.Application.Auth;
using CredoCms.Application.Common;
using CredoCms.Application.Leaders;
using CredoCms.Application.News;
using CredoCms.Application.Pages;
using CredoCms.Application.Services;
using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Application.Storage;
using CredoCms.Application.UserManagement;
using CredoCms.Domain.Identity;
using CredoCms.Infrastructure.Auditing;
using CredoCms.Infrastructure.Auth;
using CredoCms.Infrastructure.BackgroundServices;
using CredoCms.Infrastructure.Configuration;
using CredoCms.Infrastructure.Email;
using CredoCms.Infrastructure.Identity;
using CredoCms.Infrastructure.Leaders;
using CredoCms.Infrastructure.News;
using CredoCms.Infrastructure.Pages;
using CredoCms.Infrastructure.Services;
using CredoCms.Infrastructure.Persistence;
using CredoCms.Infrastructure.Persistence.Interceptors;
using CredoCms.Infrastructure.Seeding;
using CredoCms.Infrastructure.SiteSettingsManagement;
using CredoCms.Infrastructure.Storage;
using CredoCms.Infrastructure.UserManagement;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CredoCms.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<IdentitySeedOptions>()
            .Bind(configuration.GetSection(IdentitySeedOptions.SectionName))
            .ValidateOnStart();

        services.AddOptions<PublicSiteOptions>()
            .Bind(configuration.GetSection(PublicSiteOptions.SectionName))
            .ValidateOnStart();

        services.AddOptions<CookieAuthOptions>()
            .Bind(configuration.GetSection(CookieAuthOptions.SectionName))
            .ValidateOnStart();

        services.AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName));

        services.AddSingleton<IBlobStorageService, BlobStorageService>();
        services.AddScoped<IImageStorageService, ImageStorageService>();
        services.AddSingleton<IBlobCleanupService, BlobCleanupService>();

        services.AddHttpContextAccessor();

        // Interceptor is scoped because it depends on the scoped ICurrentUserService.
        // EF Core's DbContext is itself scoped by default, so resolving the
        // interceptor inside the DbContext options lambda below uses the request scope.
        services.AddScoped<VersioningInterceptor>();

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException(
                    "ConnectionStrings:DefaultConnection is required.");

            options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
            options.AddInterceptors(sp.GetRequiredService<VersioningInterceptor>());
        });

        services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());

        services.AddScoped<ICurrentUserService, CurrentUserService>();

        // Audit logging — single concrete class implements both the high-level writer
        // and the read repository.
        services.AddScoped<AuditLogger>();
        services.AddScoped<IAuditLogger>(sp => sp.GetRequiredService<AuditLogger>());
        services.AddScoped<IAuditLogRepository>(sp => sp.GetRequiredService<AuditLogger>());

        services.AddScoped<ISiteSettingsRepository, SiteSettingsRepository>();
        services.AddScoped<IUserAdminQueries, UserAdminQueries>();
        services.AddScoped<IPageRepository, PageRepository>();
        services.AddScoped<INewsRepository, NewsRepository>();
        services.AddScoped<IServiceTimeRepository, ServiceTimeRepository>();
        services.AddScoped<ILeaderRepository, LeaderRepository>();

        services.AddScoped<IInvitationEmailComposer, InvitationEmailComposer>();
        services.AddScoped<IEmailService, LoggingEmailService>();
        services.AddScoped<IAuthService, AuthService>();

        services.AddScoped<DataSeeder>();

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

        services.AddHostedService<VersioningTrimBackgroundService>();

        return services;
    }
}
