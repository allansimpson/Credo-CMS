namespace CredoCms.Application.Storage;

public sealed record DocumentUploadResult(string BlobUrl, string OriginalFilename, long SizeBytes);

public sealed class DocumentValidationException : Exception
{
    public DocumentValidationException(string message) : base(message) { }
}

/// <summary>
/// Validates + uploads PDFs to blob storage, returning the public URL.
/// Magic-byte-sniffed; only PDFs are accepted; max size from
/// <c>SiteSettings.MaxDocumentSizeBytes</c>.
/// </summary>
public interface IDocumentStorageService
{
    Task<DocumentUploadResult> UploadAsync(
        string filename,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default);

    /// <summary>Streams a previously-uploaded PDF to the caller.</summary>
    Task<Stream> OpenReadAsync(string blobUrl, CancellationToken cancellationToken = default);
}
