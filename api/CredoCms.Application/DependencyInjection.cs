using CredoCms.Application.Auditing;
using CredoCms.Application.News;
using CredoCms.Application.Pages;
using CredoCms.Application.SiteSettingsManagement;
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
        services.AddScoped<IPageService, PageService>();
        services.AddScoped<INewsService, NewsService>();

        services.AddValidatorsFromAssemblyContaining<UpdateSiteSettingsRequestValidator>(includeInternalTypes: true);

        return services;
    }
}
