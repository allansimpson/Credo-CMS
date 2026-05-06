using CredoCms.Application.Announcements;
using CredoCms.Application.Auditing;
using CredoCms.Application.Classes;
using CredoCms.Application.Homepage;
using CredoCms.Application.Documents;
using CredoCms.Application.Events;
using CredoCms.Application.Groups;
using CredoCms.Application.Leaders;
using CredoCms.Application.Members;
using CredoCms.Application.News;
using CredoCms.Application.Pages;
using CredoCms.Application.Prayer;
using CredoCms.Application.Profile;
using CredoCms.Application.Scripture;
using CredoCms.Application.Sermons;
using CredoCms.Application.Services;
using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Application.Tags;
using CredoCms.Application.UserManagement;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CredoCms.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers Application-layer services and FluentValidation validators.
    /// Must be called from the API's composition root.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ISiteSettingsService, SiteSettingsService>();
        services.AddScoped<IAuditLogService, AuditLogService>();
        services.AddScoped<IUserAdminService, UserAdminService>();
        services.AddScoped<IProfileService, ProfileService>();
        services.AddScoped<IMembersDirectoryService, MembersDirectoryService>();
        services.AddScoped<IPageService, PageService>();
        services.AddScoped<INewsService, NewsService>();
        services.AddScoped<IServiceTimeService, ServiceTimeService>();
        services.AddScoped<ILeaderService, LeaderService>();
        services.AddScoped<IDocumentService, DocumentService>();
        services.AddScoped<IAnnouncementBannerService, AnnouncementBannerService>();
        services.AddScoped<IHomepageService, HomepageService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<IScriptureReferenceService, ScriptureReferenceService>();
        services.AddScoped<ISermonSeriesService, SermonSeriesService>();
        services.AddScoped<ISermonService, SermonService>();
        services.AddSingleton<IEventOccurrenceExpander, EventOccurrenceExpander>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<IEventRegistrationService, EventRegistrationService>();
        services.AddScoped<IGroupService, GroupService>();
        services.AddScoped<IClassService, ClassService>();
        services.AddScoped<IPrayerRequestService, PrayerRequestService>();
        // RegistrationTokenSigner + options bound from configuration in
        // CredoCms.Infrastructure.DependencyInjection (IConfiguration lives there).

        services.AddValidatorsFromAssemblyContaining<UpdateSiteSettingsRequestValidator>(includeInternalTypes: true);

        return services;
    }
}
