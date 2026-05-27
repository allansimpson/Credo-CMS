using CredoCms.Application.Documents;
using CredoCms.Application.Storage;
using CredoCms.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace CredoCms.Api.Controllers;

[ApiController]
[Route("api/public/documents")]
public sealed class PublicDocumentsController : ControllerBase
{
    private readonly IDocumentService _docs;
    private readonly IDocumentStorageService _storage;

    public PublicDocumentsController(IDocumentService docs, IDocumentStorageService storage)
    {
        _docs = docs; _storage = storage;
    }

    [HttpGet]
    public Task<List<PublicDocumentDto>> ListAsync(CancellationToken ct)
        => _docs.ListPublicAsync(IsAuthenticatedMember(), ct);

    /// <summary>
    /// Streams the PDF file. Members-only documents 404 for anonymous
    /// callers via the service's auth-tier check.
    /// </summary>
    [HttpGet("{id:guid}/file")]
    public async Task<IActionResult> StreamFileAsync(Guid id, CancellationToken ct)
    {
        var doc = await _docs.GetForStreamingAsync(id, IsAuthenticatedMember(), ct);
        if (doc is null) return NotFound();

        var stream = await _storage.OpenReadAsync(doc.BlobUrl, ct);
        var filename = doc.OriginalFilename ?? $"{doc.Title}.pdf";
        // inline (not attachment) so <embed>/<iframe> can preview it.
        Response.Headers["Content-Disposition"] = $"inline; filename=\"{filename}\"";
        return File(stream, "application/pdf");
    }

    private bool IsAuthenticatedMember()
    {
        if (User?.Identity?.IsAuthenticated != true) return false;
        return User.IsInRole(SystemConstants.Roles.Member)
            || User.IsInRole(SystemConstants.Roles.Editor)
            || User.IsInRole(SystemConstants.Roles.Administrator);
    }
}
