using CredoCms.Application.Common;
using CredoCms.Application.Documents;
using CredoCms.Application.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers.Admin;

[ApiController]
[Route("api/admin/documents")]
[Authorize(Policy = AuthorizationPolicies.AdminShell)]
public sealed class DocumentsController : ControllerBase
{
    private readonly IDocumentService _docs;
    private readonly IDocumentStorageService _storage;
    private readonly IAuditLogger _audit;

    public DocumentsController(IDocumentService docs, IDocumentStorageService storage, IAuditLogger audit)
    {
        _docs = docs; _storage = storage; _audit = audit;
    }

    [HttpGet]
    public Task<List<DocumentDto>> ListAsync(
        [FromQuery] string? category = null,
        [FromQuery] bool includeDeleted = false,
        CancellationToken ct = default)
        => _docs.ListAsync(category, includeDeleted, ct);

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<DocumentDto>> GetAsync(Guid id, CancellationToken ct)
    {
        var item = await _docs.GetAsync(id, ct);
        return item is null ? NotFound() : Ok(item);
    }

    /// <summary>Upload a new PDF and create the metadata row.</summary>
    [HttpPost("upload")]
    [RequestSizeLimit(220_000_000)] // hard ceiling; service enforces SiteSettings cap.
    public async Task<ActionResult<DocumentDto>> UploadAsync(
        IFormFile? file,
        [FromForm] string title,
        [FromForm] string category,
        [FromForm] string? description,
        [FromForm] bool isPublished,
        [FromForm] bool isMembersOnly,
        CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { errors = new[] { "No file uploaded." } });

        try
        {
            await using var stream = file.OpenReadStream();
            var uploaded = await _storage.UploadAsync(file.FileName, file.ContentType, stream, ct);
            var result = await _docs.CreateAsync(new CreateDocumentRequest(
                title, description, category,
                uploaded.BlobUrl, uploaded.OriginalFilename, uploaded.SizeBytes,
                isPublished, isMembersOnly), ct);
            return result.Succeeded
                ? CreatedAtAction(nameof(GetAsync), new { id = result.Item!.Id }, result.Item)
                : BadRequest(new { errors = result.Errors });
        }
        catch (DocumentValidationException ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpPut("{id:guid}/metadata")]
    public async Task<ActionResult<DocumentDto>> UpdateMetadataAsync(Guid id, [FromBody] UpdateDocumentMetadataRequest req, CancellationToken ct)
    {
        var result = await _docs.UpdateMetadataAsync(id, req, ct);
        if (result.Succeeded) return Ok(result.Item);
        if (result.Errors.Any(e => e.Contains("not found", StringComparison.OrdinalIgnoreCase)))
            return NotFound(new { errors = result.Errors });
        return BadRequest(new { errors = result.Errors });
    }

    [HttpPost("{id:guid}/replace")]
    public async Task<ActionResult<DocumentDto>> ReplaceAsync(Guid id, IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { errors = new[] { "No file uploaded." } });
        try
        {
            await using var stream = file.OpenReadStream();
            var uploaded = await _storage.UploadAsync(file.FileName, file.ContentType, stream, ct);
            var result = await _docs.ReplaceBlobAsync(id, uploaded.BlobUrl, uploaded.OriginalFilename, uploaded.SizeBytes, ct);
            return result.Succeeded ? Ok(result.Item) : BadRequest(new { errors = result.Errors });
        }
        catch (DocumentValidationException ex)
        {
            return BadRequest(new { errors = new[] { ex.Message } });
        }
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> SoftDeleteAsync(Guid id, CancellationToken ct)
    {
        var result = await _docs.SoftDeleteAsync(id, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors });
    }

    [HttpPost("{id:guid}/restore")]
    public async Task<ActionResult<DocumentDto>> RestoreAsync(Guid id, CancellationToken ct)
    {
        var result = await _docs.RestoreAsync(id, ct);
        return result.Succeeded ? Ok(result.Item) : BadRequest(new { errors = result.Errors });
    }

    [HttpDelete("{id:guid}/hard")]
    public async Task<ActionResult> HardDeleteAsync(Guid id, CancellationToken ct)
    {
        var result = await _docs.HardDeleteAsync(id, ct);
        return result.Succeeded ? NoContent() : BadRequest(new { errors = result.Errors });
    }
}
