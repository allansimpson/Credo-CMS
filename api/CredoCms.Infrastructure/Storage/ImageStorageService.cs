using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Application.Storage;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Processing;

namespace CredoCms.Infrastructure.Storage;

public sealed class ImageStorageService : IImageStorageService
{
    // Allowed MIME types (lower-cased) the API will accept.
    private static readonly HashSet<string> AllowedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/jpg", "image/png", "image/webp",
    };

    // First-bytes signatures (magic numbers) we'll sniff to defend against
    // mismatched content-type headers. Each entry covers one allowed format.
    private static readonly (byte[] Signature, string Mime)[] MagicBytes =
    {
        // JPEG: FF D8 FF
        (new byte[] { 0xFF, 0xD8, 0xFF }, "image/jpeg"),
        // PNG: 89 50 4E 47 0D 0A 1A 0A
        (new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, "image/png"),
        // WebP: "RIFF" .... "WEBP"  (we sniff "RIFF" + "WEBP" at offset 8)
    };

    private readonly IBlobStorageService _blobs;
    private readonly ISiteSettingsRepository _settings;
    private readonly ILogger<ImageStorageService> _logger;

    public ImageStorageService(
        IBlobStorageService blobs,
        ISiteSettingsRepository settings,
        ILogger<ImageStorageService> logger)
    {
        _blobs = blobs;
        _settings = settings;
        _logger = logger;
    }

    public async Task<ImageUploadResult> UploadAsync(
        string filename,
        string contentType,
        Stream content,
        CancellationToken cancellationToken = default)
    {
        if (!AllowedTypes.Contains(contentType))
            throw new ImageValidationException($"Content-type '{contentType}' is not allowed. Use JPEG, PNG, or WebP.");

        // Buffer the incoming stream so we can sniff bytes, then re-read for ImageSharp.
        var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
        buffer.Position = 0;

        var siteSettings = await _settings.GetAsync(cancellationToken).ConfigureAwait(false);
        var maxBytes = siteSettings.MaxImageSizeBytes;
        if (buffer.Length > maxBytes)
            throw new ImageValidationException(
                $"Image is {buffer.Length / (1024 * 1024)} MB; the max is {maxBytes / (1024 * 1024)} MB.");
        if (buffer.Length == 0)
            throw new ImageValidationException("Image is empty.");

        if (!IsKnownImageFormat(buffer))
            throw new ImageValidationException(
                "File contents do not match a supported image format (JPEG, PNG, or WebP).");
        buffer.Position = 0;

        // Decode + (optionally) resize.
        using var image = await Image.LoadAsync(buffer, cancellationToken).ConfigureAwait(false);
        var maxWidth = siteSettings.ImageMaxWidth;
        if (image.Width > maxWidth)
        {
            var ratio = (double)maxWidth / image.Width;
            var newHeight = (int)Math.Round(image.Height * ratio);
            image.Mutate(x => x.Resize(maxWidth, newHeight));
        }

        var quality = siteSettings.ImageQuality;
        var (optimizedBytes, optimizedExtension, optimizedMime) =
            await EncodeOptimizedAsync(image, contentType, quality, cancellationToken).ConfigureAwait(false);
        var (webpBytes, _, _) = await EncodeWebpAsync(image, quality, cancellationToken).ConfigureAwait(false);

        var stem = MakeBlobStem(filename);
        var optimizedName = $"{stem}.{optimizedExtension}";
        var webpName = $"{stem}.webp";

        await using var optimizedStream = new MemoryStream(optimizedBytes);
        await using var webpStream = new MemoryStream(webpBytes);
        var optimizedUrl = await _blobs.UploadAsync(optimizedName, optimizedStream, optimizedMime, cancellationToken).ConfigureAwait(false);
        var webpUrl = await _blobs.UploadAsync(webpName, webpStream, "image/webp", cancellationToken).ConfigureAwait(false);

        return new ImageUploadResult(
            BlobUrl: optimizedUrl,
            WebpBlobUrl: webpUrl,
            Width: image.Width,
            Height: image.Height,
            SizeBytes: optimizedBytes.LongLength);
    }

    private static async Task<(byte[] Bytes, string Extension, string Mime)> EncodeOptimizedAsync(
        Image image,
        string sourceContentType,
        int quality,
        CancellationToken ct)
    {
        await using var ms = new MemoryStream();
        // PNG → keep PNG (lossless). JPEG / WebP → re-encode as JPEG with the
        // configured quality (smaller files for the typical photo case).
        if (string.Equals(sourceContentType, "image/png", StringComparison.OrdinalIgnoreCase))
        {
            await image.SaveAsync(ms, new PngEncoder(), ct).ConfigureAwait(false);
            return (ms.ToArray(), "png", "image/png");
        }

        await image.SaveAsync(ms, new JpegEncoder { Quality = quality }, ct).ConfigureAwait(false);
        return (ms.ToArray(), "jpg", "image/jpeg");
    }

    private static async Task<(byte[] Bytes, string Extension, string Mime)> EncodeWebpAsync(
        Image image,
        int quality,
        CancellationToken ct)
    {
        await using var ms = new MemoryStream();
        await image.SaveAsync(ms, new WebpEncoder { Quality = quality }, ct).ConfigureAwait(false);
        return (ms.ToArray(), "webp", "image/webp");
    }

    /// <summary>
    /// Stable, URL-safe filename stem composed of the original stem (lower-cased,
    /// non-alphanumerics replaced with `-`) plus a short random suffix to avoid
    /// collisions across uploads.
    /// </summary>
    internal static string MakeBlobStem(string filename)
    {
        var stem = Path.GetFileNameWithoutExtension(filename ?? string.Empty).Trim();
        if (string.IsNullOrEmpty(stem)) stem = "image";

        var safe = new System.Text.StringBuilder(stem.Length);
        foreach (var c in stem.ToLowerInvariant())
        {
            safe.Append(char.IsLetterOrDigit(c) ? c : '-');
        }
        var slug = safe.ToString().Trim('-');
        if (string.IsNullOrEmpty(slug)) slug = "image";
        if (slug.Length > 60) slug = slug[..60];

        var rnd = Guid.NewGuid().ToString("n")[..8];
        return $"{DateTime.UtcNow:yyyyMM}/{slug}-{rnd}";
    }

    /// <summary>Sniffs the first bytes of <paramref name="stream"/> for a known image header.</summary>
    internal static bool IsKnownImageFormat(Stream stream)
    {
        Span<byte> header = stackalloc byte[12];
        var read = stream.Read(header);
        if (read < 4) return false;
        var slice = header[..read];

        foreach (var (sig, _) in MagicBytes)
        {
            if (slice.Length >= sig.Length && slice[..sig.Length].SequenceEqual(sig)) return true;
        }

        // WebP: bytes 0..3 == "RIFF" and 8..11 == "WEBP"
        if (slice.Length >= 12
            && slice[0] == (byte)'R' && slice[1] == (byte)'I' && slice[2] == (byte)'F' && slice[3] == (byte)'F'
            && slice[8] == (byte)'W' && slice[9] == (byte)'E' && slice[10] == (byte)'B' && slice[11] == (byte)'P')
        {
            return true;
        }

        return false;
    }
}
