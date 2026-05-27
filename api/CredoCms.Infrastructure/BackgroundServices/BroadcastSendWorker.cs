using CredoCms.Application.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CredoCms.Infrastructure.BackgroundServices;

/// <summary>
/// Polls for broadcasts in <c>Sending</c> state (immediate dispatches) and
/// <c>Scheduled</c> with <c>ScheduledSendAt &lt;= now</c>, executes the
/// send pipeline once per broadcast. Single-instance App Service is the
/// v1 deployment target; the RowVersion-guarded Status flips in
/// EmailBroadcastService keep the design forward-compat for scale-out.
/// </summary>
public sealed class BroadcastSendWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BroadcastSendWorker> _logger;
    private static readonly TimeSpan TickInterval = TimeSpan.FromSeconds(15);

    public BroadcastSendWorker(IServiceScopeFactory scopeFactory, ILogger<BroadcastSendWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("BroadcastSendWorker started, tick={Tick}", TickInterval);

        // Resume in-flight broadcasts (worker may have been killed mid-send).
        await ResumeInFlightAsync(stoppingToken).ConfigureAwait(false);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "BroadcastSendWorker tick threw");
            }
            await Task.Delay(TickInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task ResumeInFlightAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var broadcasts = scope.ServiceProvider.GetRequiredService<IEmailBroadcastRepository>();
        var service = scope.ServiceProvider.GetRequiredService<IEmailBroadcastService>();

        var inFlight = await broadcasts.ListInFlightAsync(ct).ConfigureAwait(false);
        foreach (var b in inFlight)
        {
            _logger.LogInformation("Resuming in-flight broadcast {Id}", b.Id);
            await service.ExecuteSendAsync(b.Id, ct).ConfigureAwait(false);
        }
    }

    private async Task TickAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var broadcasts = scope.ServiceProvider.GetRequiredService<IEmailBroadcastRepository>();
        var service = scope.ServiceProvider.GetRequiredService<IEmailBroadcastService>();

        var due = await broadcasts.ListDueScheduledAsync(DateTimeOffset.UtcNow, ct).ConfigureAwait(false);
        foreach (var b in due)
        {
            // Flip Scheduled → Sending so we don't reprocess on the next tick.
            await service.SendNowAsync(b.Id, ct).ConfigureAwait(false);
            await service.ExecuteSendAsync(b.Id, ct).ConfigureAwait(false);
        }
    }
}
