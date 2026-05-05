namespace CredoCms.Application.Storage;

/// <summary>
/// Sweeps blobs that are no longer referenced by any persisted entity. Phase 2
/// ships an interface stub plus a logging-only implementation; the real
/// reconciliation runs in a later phase. The interface exists now so content
/// services can call it on delete without a forward-incompatible refactor.
/// </summary>
public interface IBlobCleanupService
{
    /// <summary>Schedules a blob for cleanup after a content entity stops referencing it.</summary>
    Task EnqueueAsync(string blobUrl, string reason, CancellationToken cancellationToken = default);
}
