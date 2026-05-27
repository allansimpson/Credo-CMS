using CredoCms.Application.Announcements;
using CredoCms.Application.Auditing;
using CredoCms.Application.Auth;
using CredoCms.Application.Blog;
using CredoCms.Application.Calendar;
using CredoCms.Application.Classes;
using CredoCms.Application.Common;
using CredoCms.Application.ConnectCard;
using CredoCms.Application.Documents;
using CredoCms.Application.Events;
using CredoCms.Application.Groups;
using CredoCms.Application.Leaders;
using CredoCms.Application.Members;
using CredoCms.Application.News;
using CredoCms.Application.Pages;
using CredoCms.Application.Prayer;
using CredoCms.Application.Profanity;
using CredoCms.Application.Scripture;
using CredoCms.Application.Sermons;
using CredoCms.Application.Services;
using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Application.Tags;
using CredoCms.Application.UserManagement;
using CredoCms.Infrastructure.Announcements;
using CredoCms.Infrastructure.Auditing;
using CredoCms.Infrastructure.Auth;
using CredoCms.Infrastructure.Blog;
using CredoCms.Infrastructure.Calendar;
using CredoCms.Infrastructure.Classes;
using CredoCms.Infrastructure.Configuration;
using CredoCms.Infrastructure.ConnectCard;
using CredoCms.Infrastructure.Documents;
using CredoCms.Infrastructure.Events;
using CredoCms.Infrastructure.Groups;
using CredoCms.Infrastructure.Identity;
using CredoCms.Infrastructure.Leaders;
using CredoCms.Infrastructure.Members;
using CredoCms.Infrastructure.News;
using CredoCms.Infrastructure.Pages;
using CredoCms.Infrastructure.Persistence;
using CredoCms.Infrastructure.Persistence.Interceptors;
using CredoCms.Infrastructure.Prayer;
using CredoCms.Infrastructure.Profanity;
using CredoCms.Infrastructure.Scripture;
using CredoCms.Infrastructure.Sermons;
using CredoCms.Infrastructure.Services;
using CredoCms.Infrastructure.SiteSettingsManagement;
using CredoCms.Infrastructure.Tags;
using CredoCms.Infrastructure.UserManagement;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CredoCms.Infrastructure.Composition;

internal static class PersistenceComposition
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<IdentitySeedOptions>()
            .Bind(configuration.GetSection(IdentitySeedOptions.SectionName))
            .ValidateOnStart();
        services.AddOptions<PublicSiteOptions>()
            .Bind(configuration.GetSection(PublicSiteOptions.SectionName))
            .ValidateOnStart();
        services.AddOptions<CookieAuthOptions>()
            .Bind(configuration.GetSection(CookieAuthOptions.SectionName))
            .ValidateOnStart();

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

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

        // Audit logger — single concrete implements both the writer + read repository.
        services.AddScoped<AuditLogger>();
        services.AddScoped<IAuditLogger>(sp => sp.GetRequiredService<AuditLogger>());
        services.AddScoped<IAuditLogRepository>(sp => sp.GetRequiredService<AuditLogger>());

        services.AddScoped<ISiteSettingsRepository, SiteSettingsRepository>();
        services.AddScoped<IUserAdminQueries, UserAdminQueries>();
        services.AddScoped<IMembersDirectoryQueries, MembersDirectoryQueries>();
        services.AddScoped<IPageRepository, PageRepository>();
        services.AddScoped<INewsRepository, NewsRepository>();
        services.AddScoped<IServiceTimeRepository, ServiceTimeRepository>();
        services.AddScoped<ILeaderRepository, LeaderRepository>();
        services.AddScoped<IDocumentRepository, DocumentRepository>();
        services.AddScoped<IAnnouncementBannerRepository, AnnouncementBannerRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IScriptureReferenceRepository, ScriptureReferenceRepository>();
        services.AddScoped<ISermonSeriesRepository, SermonSeriesRepository>();
        services.AddScoped<ISermonRepository, SermonRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IEventRegistrationRepository, EventRegistrationRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();
        services.AddScoped<IGroupMembershipRepository, GroupMembershipRepository>();
        services.AddScoped<IClassSlotRepository, ClassSlotRepository>();
        services.AddScoped<IClassOfferingRepository, ClassOfferingRepository>();
        services.AddScoped<IPrayerRequestRepository, PrayerRequestRepository>();
        services.AddScoped<IProfanityCheckService, ProfanityCheckService>();
        services.AddScoped<IConnectCardRepository, ConnectCardRepository>();
        services.AddScoped<IBlogRepository, BlogRepository>();
        services.AddSingleton<IIcalFeedBuilder, IcalFeedBuilder>();
        services.AddScoped<ICalendarQueryService, CalendarQueryService>();
        services.AddScoped<ICalendarFeedTokenService, CalendarFeedTokenService>();

        services.AddScoped<IAuthService, AuthService>();

        return services;
    }
}
