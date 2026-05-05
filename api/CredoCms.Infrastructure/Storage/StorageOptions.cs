namespace CredoCms.Infrastructure.Storage;

/// <summary>Bound from the <c>Storage</c> configuration section.</summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>
    /// Azure Blob Storage connection string. Empty disables the upload pipeline
    /// (uploads will fail fast with a clear error). For local dev, Azurite's
    /// well-known string <c>UseDevelopmentStorage=true</c> is supported.
    /// </summary>
    public string BlobConnectionString { get; set; } = string.Empty;

    public string ImagesContainer { get; set; } = "images";
}
