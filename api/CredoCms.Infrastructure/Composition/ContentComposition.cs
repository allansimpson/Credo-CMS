using CredoCms.Application.Caching;
using CredoCms.Application.ConnectCard;
using CredoCms.Application.Events;
using CredoCms.Application.Pages;
using CredoCms.Application.Search;
using CredoCms.Application.Storage;
using CredoCms.Application.Versioning;
using CredoCms.Application.Volunteers;
using CredoCms.Application.YouTube;
using CredoCms.Infrastructure.Caching;
using CredoCms.Infrastructure.ConnectCard;
using CredoCms.Infrastructure.Pages;
using CredoCms.Infrastructure.Search;
using CredoCms.Infrastructure.Seeding;
using CredoCms.Infrastructure.Storage;
using CredoCms.Infrastructure.Versioning;
using CredoCms.Infrastructure.Volunteers;
using CredoCms.Infrastructure.YouTube;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CredoCms.Infrastructure.Composition;

internal static class ContentComposition
{
    public static IServiceCollection AddContentServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<StorageOptions>()
            .Bind(configuration.GetSection(StorageOptions.SectionName));

        services.AddSingleton<IBlobStorageService, BlobStorageService>();
        services.AddScoped<IImageStorageService, ImageStorageService>();
        services.AddScoped<IDocumentStorageService, DocumentStorageService>();
        services.AddSingleton<IBlobCleanupService, BlobCleanupService>();

        // Registration token signer — secret bound from EventRegistration:* section.
        var tokenOptions = new RegistrationTokenSignerOptions();
        configuration.GetSection(RegistrationTokenSignerOptions.SectionName).Bind(tokenOptions);
        services.AddSingleton(tokenOptions);
        services.AddSingleton<IRegistrationTokenSigner, RegistrationTokenSigner>();

        services.AddScoped<IYouTubeApiClient, YouTubeApiClient>();
        services.AddHttpClient(YouTubeTranscriptClient.HttpClientName, c =>
        {
            c.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddScoped<IYouTubeTranscriptClient, YouTubeTranscriptClient>();
        services.AddSingleton<YouTubeSyncService>();

        services.AddHttpClient(TurnstileValidationService.HttpClientName, c =>
        {
            c.Timeout = TimeSpan.FromSeconds(10);
        });
        services.AddTransient<ITurnstileValidationService, TurnstileValidationService>();

        services.AddScoped<IEventVolunteerRoleRepository, EventVolunteerRoleRepository>();
        services.AddScoped<IEventVolunteerSignupRepository, EventVolunteerSignupRepository>();
        services.AddScoped<IEventVolunteerService, EventVolunteerService>();

        services.AddScoped<DataSeeder>();

        services.AddSingleton<ISearchIndexer, SearchIndexer>();
        services.AddScoped<IOutputCacheInvalidator, OutputCacheInvalidator>();

        // Version-history handlers. Only the Pages handler ships today; News /
        // ServiceTime / Document / AnnouncementBanner handlers follow the
        // same pattern and can be added without touching the controller.
        services.AddScoped<IVersionedEntityHandler, PageVersionHandler>();
        services.AddScoped<IVersionedEntityHandlerRegistry, VersionedEntityHandlerRegistry>();

        return services;
    }
}
