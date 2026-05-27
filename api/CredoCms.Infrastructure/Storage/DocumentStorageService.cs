using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Application.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CredoCms.Infrastructure.Storage;

public sealed class DocumentStorageService : IDocumentStorageService
{
    // PDF magic bytes: %PDF-
    private static readonly byte[] PdfHeader = { 0x25, 0x50, 0x44, 0x46, 0x2D };

    private readonly StorageOptions _options;
    private readonly ISiteSettingsRepository _settings;
    private readonly ILogger<DocumentStorageService> _logger;
    private readonly Lazy<BlobContainerClient?> _container;

    public DocumentStorageService(
        IOptions<StorageOptions> options,
        ISiteSettingsRepository settings,
        ILogger<DocumentStorageService> logger)
    {
        _options = options.Value;
        _settings = settings;
        _logger = logger;
        _container = new Lazy<BlobContainerClient?>(CreateClient);
    }

    private BlobContainerClient? CreateClient()
    {
        if (string.IsNullOrWhiteSpace(_options.BlobConnectionString))
        {
            _logger.LogWarning(
                "Storage:BlobConnectionString is empty — document uploads will fail until configured.");
            return null;
        }
        var service = new BlobServiceClient(_options.BlobConnectionString);
        // Documents go in their own container so cleanup + cache headers can
        // diverge from images later.
        return service.GetBlobContainerClient("documents");
    }

    public async Task<DocumentUploadResult> UploadAsync(
        string filename,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(contentType, "application/pdf", StringComparison.OrdinalIgnoreCase))
            throw new DocumentValidationException("Only application/pdf is allowed.");

        var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        buffer.Position = 0;

        var siteSettings = await _settings.GetAsync(cancellationToken).ConfigureAwait(false);
        var maxBytes = siteSettings.MaxDocumentSizeBytes;
        if (buffer.Length == 0)
            throw new DocumentValidationException("Document is empty.");
        if (buffer.Length > maxBytes)
            throw new DocumentValidationException(
                $"Document is {buffer.Length / (1024 * 1024)} MB; the max is {maxBytes / (1024 * 1024)} MB.");

        if (!IsPdf(buffer))
            throw new DocumentValidationException("File is not a PDF.");
        buffer.Position = 0;

        var container = _container.Value
            ?? throw new InvalidOperationException(
                "Blob storage is not configured. Set Storage:BlobConnectionString.");
        await container.CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var safeFilename = NormalizeFilename(filename);
        var blobName = $"{DateTime.UtcNow:yyyyMM}/{Guid.NewGuid():n}-{safeFilename}";
        var blob = container.GetBlobClient(blobName);
        await blob.UploadAsync(
            buffer,
            new BlobHttpHeaders { ContentType = "application/pdf" },
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return new DocumentUploadResult(blob.Uri.ToString(), filename, buffer.Length);
    }

    public async Task<Stream> OpenReadAsync(string blobUrl, CancellationToken cancellationToken = default)
    {
        var container = _container.Value
            ?? throw new InvalidOperationException("Blob storage is not configured.");
        var uri = new Uri(blobUrl);
        // Last segment of the path after /<container>/<...>
        var pathSegments = uri.AbsolutePath.TrimStart('/').Split('/', 2);
        var blobName = pathSegments.Length == 2 ? pathSegments[1] : pathSegments[0];

        var blob = container.GetBlobClient(blobName);
        var response = await blob.DownloadStreamingAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return response.Value.Content;
    }

    internal static bool IsPdf(Stream stream)
    {
        Span<byte> header = stackalloc byte[PdfHeader.Length];
        var read = stream.Read(header);
        return read == PdfHeader.Length && header.SequenceEqual(PdfHeader);
    }

    internal static string NormalizeFilename(string filename)
    {
        var stem = Path.GetFileNameWithoutExtension(filename ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(stem)) stem = "document";
        var sb = new System.Text.StringBuilder(stem.Length);
        foreach (var c in stem.ToLowerInvariant())
            sb.Append(char.IsLetterOrDigit(c) ? c : '-');
        var slug = sb.ToString().Trim('-');
        if (string.IsNullOrEmpty(slug)) slug = "document";
        if (slug.Length > 60) slug = slug[..60];
        return slug + ".pdf";
    }
}
