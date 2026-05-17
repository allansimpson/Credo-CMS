using CredoCms.Infrastructure.BackgroundServices;
using CredoCms.Infrastructure.Search;
using CredoCms.Infrastructure.YouTube;
using Microsoft.Extensions.DependencyInjection;

namespace CredoCms.Infrastructure.Composition;

internal static class BackgroundWorkersComposition
{
    public static IServiceCollection AddBackgroundWorkers(this IServiceCollection services)
    {
        services.AddHostedService<BroadcastSendWorker>();
        services.AddHostedService<ScheduledPublishingService>();
        services.AddHostedService<AdminNotificationDigestService>();
        services.AddHostedService<EventVolunteerReminderService>();
        services.AddHostedService<VersioningTrimBackgroundService>();
        services.AddHostedService<SearchIndexBootstrapService>();
        services.AddHostedService(sp => sp.GetRequiredService<YouTubeSyncService>());

        return services;
    }
}
