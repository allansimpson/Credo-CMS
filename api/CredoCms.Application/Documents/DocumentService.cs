using CredoCms.Application.Common;
using CredoCms.Application.Storage;
using CredoCms.Domain.Documents;
using FluentValidation;

namespace CredoCms.Application.Documents;

public sealed record DocumentDto(
    Guid Id, string Title, string? Description, string Category,
    string BlobUrl, string? OriginalFilename, long SizeBytes,
    bool IsPublished, bool IsMembersOnly, bool IsDeleted,
    DateTimeOffset CreatedAt, DateTimeOffset ModifiedAt, Guid? ModifiedByUserId);

public sealed record PublicDocumentDto(
    Guid Id, string Title, string? Description, string Category,
    long SizeBytes, bool IsMembersOnly, DateTimeOffset ModifiedAt);

public sealed record CreateDocumentRequest(
    string Title, string? Description, string Category,
    string BlobUrl, string? OriginalFilename, long SizeBytes,
    bool IsPublished, bool IsMembersOnly);

public sealed record UpdateDocumentMetadataRequest(
    string Title, string? Description, string Category,
    bool IsPublished, bool IsMembersOnly);

public interface IDocumentRepository
{
    Task<List<Document>> ListAsync(string? category, bool includeDeleted, CancellationToken ct = default);
    Task<Document?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default);
    Task<List<PublicDocumentDto>> ListPublicAsync(bool includeMembersOnly, CancellationToken ct = default);
    Task AddAsync(Document doc, CancellationToken ct = default);
    Task UpdateAsync(Document doc, CancellationToken ct = default);
    Task<bool> HardDeleteAsync(Guid id, CancellationToken ct = default);
}

public sealed record DocumentOperationResult(bool Succeeded, string[] Errors, DocumentDto? Item);

public interface IDocumentService
{
    Task<List<DocumentDto>> ListAsync(string? category, bool includeDeleted, CancellationToken ct = default);
    Task<DocumentDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<List<PublicDocumentDto>> ListPublicAsync(bool includeMembersOnly, CancellationToken ct = default);
    Task<Document?> GetForStreamingAsync(Guid id, bool includeMembersOnly, CancellationToken ct = default);
    Task<DocumentOperationResult> CreateAsync(CreateDocumentRequest request, CancellationToken ct = default);
    Task<DocumentOperationResult> UpdateMetadataAsync(Guid id, UpdateDocumentMetadataRequest request, CancellationToken ct = default);
    Task<DocumentOperationResult> ReplaceBlobAsync(Guid id, string newBlobUrl, string? filename, long sizeBytes, CancellationToken ct = default);
    Task<DocumentOperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default);
    Task<DocumentOperationResult> RestoreAsync(Guid id, CancellationToken ct = default);
    Task<DocumentOperationResult> HardDeleteAsync(Guid id, CancellationToken ct = default);
}

public sealed class CreateDocumentRequestValidator : AbstractValidator<CreateDocumentRequest>
{
    public CreateDocumentRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.BlobUrl).NotEmpty().MaximumLength(2000);
    }
}

public sealed class UpdateDocumentMetadataRequestValidator : AbstractValidator<UpdateDocumentMetadataRequest>
{
    public UpdateDocumentMetadataRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(500);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
    }
}

public sealed class DocumentService : IDocumentService
{
    private readonly IDocumentRepository _repo;
    private readonly IAuditLogger _audit;
    private readonly IBlobCleanupService _cleanup;
    private readonly IValidator<CreateDocumentRequest> _createValidator;
    private readonly IValidator<UpdateDocumentMetadataRequest> _updateValidator;

    public DocumentService(
        IDocumentRepository repo, IAuditLogger audit, IBlobCleanupService cleanup,
        IValidator<CreateDocumentRequest> createValidator,
        IValidator<UpdateDocumentMetadataRequest> updateValidator)
    {
        _repo = repo; _audit = audit; _cleanup = cleanup;
        _createValidator = createValidator; _updateValidator = updateValidator;
    }

    public async Task<List<DocumentDto>> ListAsync(string? category, bool includeDeleted, CancellationToken ct = default)
        => (await _repo.ListAsync(category, includeDeleted, ct).ConfigureAwait(false)).Select(ToDto).ToList();

    public async Task<DocumentDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var d = await _repo.GetByIdAsync(id, includeDeleted: true, ct).ConfigureAwait(false);
        return d is null ? null : ToDto(d);
    }

    public Task<List<PublicDocumentDto>> ListPublicAsync(bool includeMembersOnly, CancellationToken ct = default)
        => _repo.ListPublicAsync(includeMembersOnly, ct);

    public async Task<Document?> GetForStreamingAsync(Guid id, bool includeMembersOnly, CancellationToken ct = default)
    {
        var d = await _repo.GetByIdAsync(id, false, ct).ConfigureAwait(false);
        if (d is null || !d.IsPublished) return null;
        if (d.IsMembersOnly && !includeMembersOnly) return null;
        return d;
    }

    public async Task<DocumentOperationResult> CreateAsync(CreateDocumentRequest request, CancellationToken ct = default)
    {
        var v = await _createValidator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!v.IsValid) return new(false, v.Errors.Select(e => e.ErrorMessage).ToArray(), null);

        var now = DateTimeOffset.UtcNow;
        var doc = new Document
        {
            Id = Guid.NewGuid(),
            Title = request.Title, Description = request.Description, Category = request.Category,
            BlobUrl = request.BlobUrl, OriginalFilename = request.OriginalFilename, SizeBytes = request.SizeBytes,
            IsPublished = request.IsPublished, IsMembersOnly = request.IsMembersOnly,
            CreatedAt = now, ModifiedAt = now,
        };
        await _repo.AddAsync(doc, ct).ConfigureAwait(false);
        await _audit.WriteAsync("Document.Created", nameof(Document), doc.Id.ToString(),
            details: new { doc.Title, doc.Category }, cancellationToken: ct).ConfigureAwait(false);
        return new(true, Array.Empty<string>(), ToDto(doc));
    }

    public async Task<DocumentOperationResult> UpdateMetadataAsync(Guid id, UpdateDocumentMetadataRequest request, CancellationToken ct = default)
    {
        var v = await _updateValidator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!v.IsValid) return new(false, v.Errors.Select(e => e.ErrorMessage).ToArray(), null);
        var doc = await _repo.GetByIdAsync(id, includeDeleted: true, ct).ConfigureAwait(false);
        if (doc is null) return new(false, new[] { "Document not found." }, null);

        doc.Title = request.Title; doc.Description = request.Description; doc.Category = request.Category;
        doc.IsPublished = request.IsPublished; doc.IsMembersOnly = request.IsMembersOnly;
        doc.ModifiedAt = DateTimeOffset.UtcNow;

        await _repo.UpdateAsync(doc, ct).ConfigureAwait(false);
        await _audit.WriteAsync("Document.Updated", nameof(Document), id.ToString(),
            details: new { doc.Title, doc.Category }, cancellationToken: ct).ConfigureAwait(false);
        return new(true, Array.Empty<string>(), ToDto(doc));
    }

    public async Task<DocumentOperationResult> ReplaceBlobAsync(Guid id, string newBlobUrl, string? filename, long sizeBytes, CancellationToken ct = default)
    {
        var doc = await _repo.GetByIdAsync(id, includeDeleted: true, ct).ConfigureAwait(false);
        if (doc is null) return new(false, new[] { "Document not found." }, null);

        var oldBlob = doc.BlobUrl;
        doc.BlobUrl = newBlobUrl;
        doc.OriginalFilename = filename ?? doc.OriginalFilename;
        doc.SizeBytes = sizeBytes;
        doc.ModifiedAt = DateTimeOffset.UtcNow;

        await _repo.UpdateAsync(doc, ct).ConfigureAwait(false);
        await _cleanup.EnqueueAsync(oldBlob, $"Document {doc.Id} blob replaced", ct).ConfigureAwait(false);
        await _audit.WriteAsync("Document.BlobReplaced", nameof(Document), id.ToString(),
            details: new { doc.Title, doc.SizeBytes }, cancellationToken: ct).ConfigureAwait(false);
        return new(true, Array.Empty<string>(), ToDto(doc));
    }

    public async Task<DocumentOperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var doc = await _repo.GetByIdAsync(id, false, ct).ConfigureAwait(false);
        if (doc is null) return new(false, new[] { "Document not found." }, null);
        doc.IsDeleted = true; doc.IsPublished = false;
        doc.DeletedAt = DateTimeOffset.UtcNow; doc.ModifiedAt = doc.DeletedAt.Value;
        await _repo.UpdateAsync(doc, ct).ConfigureAwait(false);
        await _audit.WriteAsync("Document.SoftDeleted", nameof(Document), id.ToString(),
            details: new { doc.Title }, cancellationToken: ct).ConfigureAwait(false);
        return new(true, Array.Empty<string>(), ToDto(doc));
    }

    public async Task<DocumentOperationResult> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        var doc = await _repo.GetByIdAsync(id, true, ct).ConfigureAwait(false);
        if (doc is null) return new(false, new[] { "Document not found." }, null);
        doc.IsDeleted = false; doc.DeletedAt = null;
        doc.ModifiedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(doc, ct).ConfigureAwait(false);
        await _audit.WriteAsync("Document.Restored", nameof(Document), id.ToString(),
            details: new { doc.Title }, cancellationToken: ct).ConfigureAwait(false);
        return new(true, Array.Empty<string>(), ToDto(doc));
    }

    public async Task<DocumentOperationResult> HardDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var doc = await _repo.GetByIdAsync(id, true, ct).ConfigureAwait(false);
        if (doc is null) return new(false, new[] { "Document not found." }, null);
        if (!doc.IsDeleted) return new(false, new[] { "Soft-delete first, then hard-delete." }, null);

        var blob = doc.BlobUrl;
        await _repo.HardDeleteAsync(id, ct).ConfigureAwait(false);
        await _cleanup.EnqueueAsync(blob, $"Document {id} hard-deleted", ct).ConfigureAwait(false);
        await _audit.WriteAsync("Document.HardDeleted", nameof(Document), id.ToString(),
            details: new { doc.Title }, cancellationToken: ct).ConfigureAwait(false);
        return new(true, Array.Empty<string>(), null);
    }

    internal static DocumentDto ToDto(Document d) => new(
        d.Id, d.Title, d.Description, d.Category,
        d.BlobUrl, d.OriginalFilename, d.SizeBytes,
        d.IsPublished, d.IsMembersOnly, d.IsDeleted,
        d.CreatedAt, d.ModifiedAt, d.ModifiedByUserId);
}
