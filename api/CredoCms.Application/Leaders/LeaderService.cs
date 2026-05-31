using CredoCms.Application.Common;
using CredoCms.Application.Pages;
using CredoCms.Application.Search;
using CredoCms.Domain.Leaders;
using FluentValidation;

namespace CredoCms.Application.Leaders;

public sealed record LeaderDto(
    Guid Id, string FullName, string? Title, string Category, string? BioJson,
    string? Email, string? PhotoUrl, string? PhotoWebpUrl, string? PhotoAlt,
    int DisplayOrder, Guid? UserId, DateTimeOffset CreatedAt, DateTimeOffset ModifiedAt);

public sealed record PublicLeaderDto(
    Guid Id, string FullName, string? Title, string Category, string? BioJson, string? Email,
    string? PhotoUrl, string? PhotoWebpUrl, string? PhotoAlt, int DisplayOrder);

public sealed record CreateLeaderRequest(
    string FullName, string? Title, string Category, string? BioJson, string? Email,
    string? PhotoUrl, string? PhotoWebpUrl, string? PhotoAlt, int DisplayOrder,
    Guid? UserId = null);

public sealed record UpdateLeaderRequest(
    string FullName, string? Title, string Category, string? BioJson, string? Email,
    string? PhotoUrl, string? PhotoWebpUrl, string? PhotoAlt, int DisplayOrder,
    Guid? UserId = null);

public interface ILeaderRepository
{
    Task<List<Leader>> ListAsync(CancellationToken ct = default);
    Task<Leader?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Looks up the (at most one) Leader linked to the supplied
    /// ApplicationUser. Returns null when the user isn't a leader.
    /// Underpins the byline lookup on Blog + News.</summary>
    Task<Leader?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    Task AddAsync(Leader leader, CancellationToken ct = default);
    Task UpdateAsync(Leader leader, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

public sealed record LeaderOperationResult(bool Succeeded, string[] Errors, LeaderDto? Item);

public interface ILeaderService
{
    Task<List<LeaderDto>> ListAsync(CancellationToken ct = default);
    Task<LeaderDto?> GetAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns the leader profile (if any) attached to the given user
    /// account. Used by Blog and News to compose the author byline.</summary>
    Task<LeaderDto?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    Task<List<PublicLeaderDto>> ListPublicAsync(CancellationToken ct = default);
    Task<PublicLeaderDto?> GetPublicAsync(Guid id, CancellationToken ct = default);
    Task<LeaderOperationResult> CreateAsync(CreateLeaderRequest request, CancellationToken ct = default);
    Task<LeaderOperationResult> UpdateAsync(Guid id, UpdateLeaderRequest request, CancellationToken ct = default);
    Task<LeaderOperationResult> DeleteAsync(Guid id, CancellationToken ct = default);
}

public sealed class CreateLeaderRequestValidator : AbstractValidator<CreateLeaderRequest>
{
    public CreateLeaderRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Title).MaximumLength(150);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email)).MaximumLength(254);
        RuleFor(x => x.PhotoUrl).MaximumLength(2000);
        RuleFor(x => x.PhotoWebpUrl).MaximumLength(2000);
        RuleFor(x => x.PhotoAlt).MaximumLength(300);
    }
}

public sealed class UpdateLeaderRequestValidator : AbstractValidator<UpdateLeaderRequest>
{
    public UpdateLeaderRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Title).MaximumLength(150);
        RuleFor(x => x.Category).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).EmailAddress().When(x => !string.IsNullOrWhiteSpace(x.Email)).MaximumLength(254);
        RuleFor(x => x.PhotoUrl).MaximumLength(2000);
        RuleFor(x => x.PhotoWebpUrl).MaximumLength(2000);
        RuleFor(x => x.PhotoAlt).MaximumLength(300);
    }
}

public sealed class LeaderService : ILeaderService
{
    private readonly ILeaderRepository _repo;
    private readonly IAuditLogger _audit;
    private readonly IValidator<CreateLeaderRequest> _createValidator;
    private readonly IValidator<UpdateLeaderRequest> _updateValidator;

    private readonly ISearchIndexer? _search;

    public LeaderService(
        ILeaderRepository repo,
        IAuditLogger audit,
        IValidator<CreateLeaderRequest> createValidator,
        IValidator<UpdateLeaderRequest> updateValidator,
        ISearchIndexer? search = null)
    {
        _repo = repo; _audit = audit;
        _createValidator = createValidator; _updateValidator = updateValidator;
        _search = search;
    }

    private async Task IndexAsync(Leader l, CancellationToken ct)
    {
        if (_search is null) return;
        await _search.UpsertAsync(new SearchUpsertCommand(
            EntityType: nameof(Leader), EntityId: l.Id,
            Title: l.FullName,
            BodyText: $"{l.Title} {l.Category} {PageService.AutoExcerpt(l.BioJson ?? string.Empty, 8000)}",
            Url: "/leaders/" + l.Id,
            IsPublished: true, IsMembersOnly: false), ct).ConfigureAwait(false);
    }

    public async Task<List<LeaderDto>> ListAsync(CancellationToken ct = default)
        => (await _repo.ListAsync(ct).ConfigureAwait(false)).Select(ToDto).ToList();

    public async Task<LeaderDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repo.GetByIdAsync(id, ct).ConfigureAwait(false);
        return item is null ? null : ToDto(item);
    }

    public async Task<LeaderDto?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var item = await _repo.GetByUserIdAsync(userId, ct).ConfigureAwait(false);
        return item is null ? null : ToDto(item);
    }

    public async Task<List<PublicLeaderDto>> ListPublicAsync(CancellationToken ct = default)
        => (await _repo.ListAsync(ct).ConfigureAwait(false)).Select(ToPublic).ToList();

    public async Task<PublicLeaderDto?> GetPublicAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repo.GetByIdAsync(id, ct).ConfigureAwait(false);
        return item is null ? null : ToPublic(item);
    }

    public async Task<LeaderOperationResult> CreateAsync(CreateLeaderRequest request, CancellationToken ct = default)
    {
        var v = await _createValidator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!v.IsValid) return new(false, v.Errors.Select(e => e.ErrorMessage).ToArray(), null);

        if (request.UserId is { } uid)
        {
            var alreadyLinked = await _repo.GetByUserIdAsync(uid, ct).ConfigureAwait(false);
            if (alreadyLinked is not null)
                return new(false, new[] { "That user is already linked to another leader profile." }, null);
        }

        var now = DateTimeOffset.UtcNow;
        var leader = new Leader
        {
            Id = Guid.NewGuid(),
            FullName = request.FullName,
            Title = request.Title,
            Category = request.Category,
            BioJson = request.BioJson,
            Email = request.Email,
            PhotoUrl = request.PhotoUrl,
            PhotoWebpUrl = request.PhotoWebpUrl,
            PhotoAlt = request.PhotoAlt,
            DisplayOrder = request.DisplayOrder,
            UserId = request.UserId,
            CreatedAt = now,
            ModifiedAt = now,
        };
        await _repo.AddAsync(leader, ct).ConfigureAwait(false);
        await IndexAsync(leader, ct).ConfigureAwait(false);
        await _audit.WriteAsync("Leader.Created", nameof(Leader), leader.Id.ToString(),
            details: new { leader.FullName, leader.Category }, cancellationToken: ct).ConfigureAwait(false);
        return new(true, Array.Empty<string>(), ToDto(leader));
    }

    public async Task<LeaderOperationResult> UpdateAsync(Guid id, UpdateLeaderRequest request, CancellationToken ct = default)
    {
        var v = await _updateValidator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!v.IsValid) return new(false, v.Errors.Select(e => e.ErrorMessage).ToArray(), null);
        var leader = await _repo.GetByIdAsync(id, ct).ConfigureAwait(false);
        if (leader is null) return new(false, new[] { "Leader not found." }, null);

        if (request.UserId is { } uid && uid != leader.UserId)
        {
            var alreadyLinked = await _repo.GetByUserIdAsync(uid, ct).ConfigureAwait(false);
            if (alreadyLinked is not null && alreadyLinked.Id != id)
                return new(false, new[] { "That user is already linked to another leader profile." }, null);
        }

        leader.FullName = request.FullName; leader.Title = request.Title;
        leader.Category = request.Category; leader.BioJson = request.BioJson;
        leader.Email = request.Email;
        leader.PhotoUrl = request.PhotoUrl; leader.PhotoWebpUrl = request.PhotoWebpUrl; leader.PhotoAlt = request.PhotoAlt;
        leader.DisplayOrder = request.DisplayOrder;
        leader.UserId = request.UserId;
        leader.ModifiedAt = DateTimeOffset.UtcNow;

        await _repo.UpdateAsync(leader, ct).ConfigureAwait(false);
        await IndexAsync(leader, ct).ConfigureAwait(false);
        await _audit.WriteAsync("Leader.Updated", nameof(Leader), id.ToString(),
            details: new { leader.FullName, leader.Category }, cancellationToken: ct).ConfigureAwait(false);
        return new(true, Array.Empty<string>(), ToDto(leader));
    }

    public async Task<LeaderOperationResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var leader = await _repo.GetByIdAsync(id, ct).ConfigureAwait(false);
        if (leader is null) return new(false, new[] { "Leader not found." }, null);
        await _repo.DeleteAsync(id, ct).ConfigureAwait(false);
        if (_search is not null)
            await _search.RemoveAsync(nameof(Leader), id, ct).ConfigureAwait(false);
        await _audit.WriteAsync("Leader.Deleted", nameof(Leader), id.ToString(),
            details: new { leader.FullName, leader.Category }, cancellationToken: ct).ConfigureAwait(false);
        return new(true, Array.Empty<string>(), null);
    }

    internal static LeaderDto ToDto(Leader l) => new(
        l.Id, l.FullName, l.Title, l.Category, l.BioJson,
        l.Email, l.PhotoUrl, l.PhotoWebpUrl, l.PhotoAlt,
        l.DisplayOrder, l.UserId, l.CreatedAt, l.ModifiedAt);

    internal static PublicLeaderDto ToPublic(Leader l) => new(
        l.Id, l.FullName, l.Title, l.Category, l.BioJson, l.Email,
        l.PhotoUrl, l.PhotoWebpUrl, l.PhotoAlt, l.DisplayOrder);
}
