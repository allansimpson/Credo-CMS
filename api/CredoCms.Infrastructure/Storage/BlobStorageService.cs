using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CredoCms.Application.Storage;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CredoCms.Infrastructure.Storage;

public sealed class BlobStorageService : IBlobStorageService
{
    private readonly StorageOptions _options;
    private readonly ILogger<BlobStorageService> _logger;
    private readonly Lazy<BlobContainerClient?> _container;

    public BlobStorageService(IOptions<StorageOptions> options, ILogger<BlobStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _container = new Lazy<BlobContainerClient?>(CreateClient);
    }

    private BlobContainerClient? CreateClient()
    {
        if (string.IsNullOrWhiteSpace(_options.BlobConnectionString))
        {
            _logger.LogWarning(
                "Storage:BlobConnectionString is empty — image uploads will fail until configured.");
            return null;
        }

        var service = new BlobServiceClient(_options.BlobConnectionString);
        var container = service.GetBlobContainerClient(_options.ImagesContainer);
        // CreateIfNotExistsAsync would require an async ctor — call once on first use below.
        return container;
    }

    public async Task<string> UploadAsync(
        string blobName,
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var container = _container.Value
            ?? throw new InvalidOperationException(
                "Blob storage is not configured. Set Storage:BlobConnectionString.");

        await container.CreateIfNotExistsAsync(PublicAccessType.Blob, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        var blob = container.GetBlobClient(blobName);
        await blob.UploadAsync(
            content,
            new BlobHttpHeaders { ContentType = contentType },
            cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return blob.Uri.ToString();
    }

    public async Task<bool> DeleteAsync(string blobName, CancellationToken cancellationToken = default)
    {
        var container = _container.Value;
        if (container is null) return false;

        var blob = container.GetBlobClient(blobName);
        var response = await blob.DeleteIfExistsAsync(cancellationToken: cancellationToken)
            .ConfigureAwait(false);
        return response.Value;
    }
}
