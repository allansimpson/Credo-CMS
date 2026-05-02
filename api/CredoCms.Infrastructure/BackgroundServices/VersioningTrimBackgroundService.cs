using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CredoCms.Infrastructure.BackgroundServices;

/// <summary>
/// Nightly job that trims temporal-table history rows on versioned entities to the
/// configured retention count.
/// </summary>
/// <remarks>
/// <b>Phase 1:</b> registered but inert — no entities implement <c>IVersionedEntity</c>
/// yet, so each tick logs a debug message and exits. Phase 2 will iterate the
/// versioned-entity registry and apply per-entity retention.
/// </remarks>
public sealed class VersioningTrimBackgroundService : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(24);
    private readonly ILogger<VersioningTrimBackgroundService> _logger;

    public VersioningTrimBackgroundService(ILogger<VersioningTrimBackgroundService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("VersioningTrimBackgroundService started (Phase 1: inert)");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _logger.LogDebug("VersioningTrim tick: no versioned entities yet — nothing to do");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "VersioningTrim tick failed");
            }

            try
            {
                await Task.Delay(Interval, stoppingToken).ConfigureAwait(false);
            }
            catch (TaskCanceledException) { /* shutting down */ }
        }
    }
}
