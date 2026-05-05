using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Application.Storage;
using CredoCms.Domain.Settings;
using CredoCms.Infrastructure.Storage;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace CredoCms.Infrastructure.Tests.Storage;

public sealed class ImageStorageServiceTests
{
    [Fact]
    public void IsKnownImageFormat_accepts_jpeg_png_webp_signatures()
    {
        // JPEG header bytes
        var jpeg = new MemoryStream(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10 });
        ImageStorageService.IsKnownImageFormat(jpeg).Should().BeTrue();

        // PNG header bytes
        var png = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00 });
        ImageStorageService.IsKnownImageFormat(png).Should().BeTrue();

        // WebP: "RIFF" .... "WEBP"
        var webp = new MemoryStream(new byte[]
        {
            (byte)'R', (byte)'I', (byte)'F', (byte)'F',
            0, 0, 0, 0,
            (byte)'W', (byte)'E', (byte)'B', (byte)'P',
        });
        ImageStorageService.IsKnownImageFormat(webp).Should().BeTrue();
    }

    [Fact]
    public void IsKnownImageFormat_rejects_text_and_pdf()
    {
        var text = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("hello world"));
        ImageStorageService.IsKnownImageFormat(text).Should().BeFalse();

        // PDF magic bytes — must be rejected by the image sniffer.
        var pdf = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D });
        ImageStorageService.IsKnownImageFormat(pdf).Should().BeFalse();
    }

    [Fact]
    public void MakeBlobStem_normalizes_filename_and_includes_year_month()
    {
        var stem = ImageStorageService.MakeBlobStem("My Photo  (Final).JPG");
        // yyyyMM/<slug>-<8-hex>
        stem.Should().MatchRegex(@"^\d{6}/[a-z0-9-]+-[0-9a-f]{8}$");
        stem.Should().Contain("my-photo");
        stem.Should().Contain("final");
    }

    [Fact]
    public void MakeBlobStem_falls_back_to_image_when_filename_yields_empty_slug()
    {
        var stem = ImageStorageService.MakeBlobStem("....!!!.jpg");
        stem.Should().MatchRegex(@"^\d{6}/image-[0-9a-f]{8}$");
    }

    [Fact]
    public async Task UploadAsync_rejects_non_image_content_type()
    {
        var sut = MakeSut(out _, out _);
        var act = async () => await sut.UploadAsync("file.pdf", "application/pdf",
            new MemoryStream(new byte[] { 1, 2, 3 }));
        await act.Should().ThrowAsync<ImageValidationException>()
            .WithMessage("*not allowed*");
    }

    [Fact]
    public async Task UploadAsync_rejects_when_magic_bytes_do_not_match_content_type()
    {
        var sut = MakeSut(out _, out _);
        // Claims to be JPEG, but bytes are clearly text.
        var content = new MemoryStream(System.Text.Encoding.UTF8.GetBytes("this is not an image"));
        var act = async () => await sut.UploadAsync("a.jpg", "image/jpeg", content);
        await act.Should().ThrowAsync<ImageValidationException>()
            .WithMessage("*do not match*");
    }

    [Fact]
    public async Task UploadAsync_rejects_when_too_large()
    {
        var settings = new SiteSettings { MaxImageSizeBytes = 100, ImageMaxWidth = 2400, ImageQuality = 82 };
        var sut = MakeSut(out _, out _, settings);
        // 200 bytes of valid JPEG header + filler — header passes the magic check
        // but total size exceeds the cap.
        var bytes = new byte[200];
        bytes[0] = 0xFF; bytes[1] = 0xD8; bytes[2] = 0xFF;
        var act = async () => await sut.UploadAsync("a.jpg", "image/jpeg", new MemoryStream(bytes));
        await act.Should().ThrowAsync<ImageValidationException>()
            .WithMessage("*the max is*");
    }

    [Fact]
    public async Task UploadAsync_rejects_empty_stream()
    {
        var sut = MakeSut(out _, out _);
        var act = async () => await sut.UploadAsync("a.jpg", "image/jpeg", new MemoryStream());
        await act.Should().ThrowAsync<ImageValidationException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public async Task UploadAsync_resizes_oversized_jpeg_and_emits_optimized_plus_webp_blobs()
    {
        var settings = new SiteSettings { MaxImageSizeBytes = 50_000_000, ImageMaxWidth = 800, ImageQuality = 75 };
        var sut = MakeSut(out var blobs, out _, settings);

        // Build a real 1200×600 JPEG in-memory so ImageSharp can decode it.
        using var img = new Image<Rgba32>(1200, 600);
        await using var jpegStream = new MemoryStream();
        await img.SaveAsync(jpegStream, new JpegEncoder { Quality = 90 });
        jpegStream.Position = 0;

        var result = await sut.UploadAsync("hero.jpg", "image/jpeg", jpegStream);

        result.Width.Should().Be(800);
        result.Height.Should().Be(400);
        result.BlobUrl.Should().EndWith(".jpg");
        result.WebpBlobUrl.Should().EndWith(".webp");
        result.SizeBytes.Should().BeGreaterThan(0);

        blobs.Verify(x => x.UploadAsync(
            It.Is<string>(name => name.EndsWith(".jpg")),
            It.IsAny<Stream>(),
            "image/jpeg",
            It.IsAny<CancellationToken>()), Times.Once);
        blobs.Verify(x => x.UploadAsync(
            It.Is<string>(name => name.EndsWith(".webp")),
            It.IsAny<Stream>(),
            "image/webp",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UploadAsync_keeps_png_as_png_for_optimized_variant()
    {
        var settings = new SiteSettings { MaxImageSizeBytes = 50_000_000, ImageMaxWidth = 2400, ImageQuality = 82 };
        var sut = MakeSut(out var blobs, out _, settings);

        using var img = new Image<Rgba32>(120, 120);
        await using var pngStream = new MemoryStream();
        await img.SaveAsync(pngStream, new PngEncoder());
        pngStream.Position = 0;

        var result = await sut.UploadAsync("logo.png", "image/png", pngStream);

        result.BlobUrl.Should().EndWith(".png");
        result.WebpBlobUrl.Should().EndWith(".webp");

        blobs.Verify(x => x.UploadAsync(
            It.Is<string>(name => name.EndsWith(".png")),
            It.IsAny<Stream>(),
            "image/png",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    private static ImageStorageService MakeSut(
        out Mock<IBlobStorageService> blobs,
        out Mock<ISiteSettingsRepository> settingsRepo,
        SiteSettings? settings = null)
    {
        settings ??= new SiteSettings { MaxImageSizeBytes = 10_000_000, ImageMaxWidth = 2400, ImageQuality = 82 };

        blobs = new Mock<IBlobStorageService>();
        blobs.Setup(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
             .ReturnsAsync((string name, Stream _, string _, CancellationToken _) => $"https://blob.test/{name}");

        settingsRepo = new Mock<ISiteSettingsRepository>();
        settingsRepo.Setup(x => x.GetAsync(It.IsAny<CancellationToken>())).ReturnsAsync(settings);

        return new ImageStorageService(blobs.Object, settingsRepo.Object, NullLogger<ImageStorageService>.Instance);
    }
}
