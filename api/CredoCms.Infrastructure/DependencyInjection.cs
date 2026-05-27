using CredoCms.Infrastructure.Composition;
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

        services.AddPersistence(configuration);
        services.AddIdentity();
        services.AddCommunications();
        services.AddContentServices(configuration);
        services.AddBackgroundWorkers();

        return services;
    }
}
