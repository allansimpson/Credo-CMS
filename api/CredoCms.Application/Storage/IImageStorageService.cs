namespace CredoCms.Application.Storage;

public sealed record ImageUploadResult(
    string BlobUrl,
    string WebpBlobUrl,
    int Width,
    int Height,
    long SizeBytes);

public sealed class ImageValidationException : Exception
{
    public ImageValidationException(string message) : base(message) { }
}

/// <summary>
/// Higher-level image upload pipeline used by the admin image upload
/// endpoint and any future content-creation paths that accept images.
///
/// Validates content-type + magic bytes + max size, loads via ImageSharp,
/// resizes if wider than <c>SiteSettings.ImageMaxWidth</c>, writes an
/// optimized JPEG/PNG variant alongside a WebP variant, uploads both to
/// blob storage, and returns the public URLs and dimensions.
/// </summary>
public interface IImageStorageService
{
    /// <param name="filename">Original filename (used to derive an extension and audit name).</param>
    /// <param name="contentType">MIME type from the uploading client.</param>
    /// <param name="content">A seekable stream of the uploaded file.</param>
    Task<ImageUploadResult> UploadAsync(
        string filename,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default);
}
