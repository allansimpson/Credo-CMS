using CredoCms.Application.Storage;
using Microsoft.Extensions.Logging;

namespace CredoCms.Infrastructure.Storage;

/// <summary>
/// Stub. Logs the orphan-blob intent so operators can see what would
/// be reclaimed; the real reconciliation job is deferred.
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
