using CredoCms.Application.Common;
using CredoCms.Application.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/images")]
[Authorize(Policy = AuthorizationPolicies.AdminShell)]
public sealed class ImagesController : ControllerBase
{
    private readonly IImageStorageService _images;
    private readonly IAuditLogger _audit;

    public ImagesController(IImageStorageService images, IAuditLogger audit)
    {
        _images = images;
        _audit = audit;
    }

    public sealed record ImageUploadResponse(
        string BlobUrl,
        string WebpBlobUrl,
        int Width,
        int Height,
        long SizeBytes);

    /// <summary>Multipart-form image upload. Field name is <c>file</c>.</summary>
    [HttpPost("upload")]
    [RequestSizeLimit(60_000_000)] // hard ceiling regardless of SiteSettings; service enforces the configured cap.
    public async Task<ActionResult<ImageUploadResponse>> UploadAsync(
        IFormFile? file,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { errors = new[] { "No file uploaded." } });

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _images.UploadAsync(file.FileName, file.ContentType, stream, ct)
                .ConfigureAwait(false);

            await _audit.WriteAsync(
                "Image.Uploaded",
                "Image",
                entityId: result.BlobUrl,
                details: new
                {
                    OriginalFilename = file.FileName,
                    result.Width,
                    result.Height,
                    result.SizeBytes,
                },
                cancellationToken: ct).ConfigureAwait(false);

            return Ok(new ImageUploadResponse(
                result.BlobUrl, result.WebpBlobUrl, result.Width, result.Height, result.SizeBytes));
        }
        catch (ImageValidationException ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }
}
