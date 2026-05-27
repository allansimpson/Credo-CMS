namespace CredoCms.Application.Storage;

/// <summary>
/// Generic blob-storage abstraction. Currently only the upload + delete
/// surface is used; later additions may include SAS-token issuance or copy/rename helpers.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Uploads the supplied stream to the configured images container under the
    /// supplied (already-normalized) blob name. Returns the public URL.
    /// Throws <see cref="InvalidOperationException"/> if storage isn't configured.
    /// </summary>
    Task<string> UploadAsync(
        string blobName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>Deletes a blob. Returns false if the blob did not exist.</summary>
    Task<bool> DeleteAsync(string blobName, CancellationToken cancellationToken = default);
}
