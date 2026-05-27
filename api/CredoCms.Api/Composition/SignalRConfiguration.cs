using CredoCms.Api.RealTime;
using CredoCms.Application.RealTime;

namespace CredoCms.Api.Composition;

internal static class SignalRConfiguration
{
    public static void AddSignalRWithRealtimeNotifier(this WebApplicationBuilder builder)
    {
        var signalRBuilder = builder.Services.AddSignalR();
        var azureSignalR = builder.Configuration.GetConnectionString("AzureSignalR");
        if (!string.IsNullOrWhiteSpace(azureSignalR))
        {
            signalRBuilder.AddAzureSignalR(azureSignalR);
        }
        builder.Services.AddSingleton<IRealtimeNotifier, SignalRRealtimeNotifier>();
    }
}
