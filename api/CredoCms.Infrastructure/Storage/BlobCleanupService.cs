using CredoCms.Application.Storage;
using Microsoft.Extensions.Logging;

namespace CredoCms.Infrastructure.Storage;

/// <summary>
/// Phase 2 stub. Logs the orphan-blob intent so operators can see what would
/// be reclaimed; the real reconciliation job is deferred to a later phase
/// per BUILD_PLAN P-6.
/// </summary>
public sealed class BlobCleanupService : IBlobCleanupService
{
    private readonly ILogger<BlobCleanupService> _logger;

    public BlobCleanupService(ILogger<BlobCleanupService> logger)
    {
        _logger = logger;
    }

    public Task EnqueueAsync(string blobUrl, string reason, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Blob cleanup queued (no-op stub): url={BlobUrl}, reason={Reason}",
            blobUrl, reason);
        return Task.CompletedTask;
    }
}
