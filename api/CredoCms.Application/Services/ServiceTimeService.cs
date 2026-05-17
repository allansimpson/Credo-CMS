using CredoCms.Application.Common;
using CredoCms.Domain.Services;
using FluentValidation;

namespace CredoCms.Application.Services;

public interface IServiceTimeRepository
{
    Task<List<ServiceTime>> ListAsync(bool includeDeleted, CancellationToken ct = default);
    Task<ServiceTime?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default);
    Task<List<PublicServiceTimeDto>> ListPublicAsync(CancellationToken ct = default);
    Task AddAsync(ServiceTime item, CancellationToken ct = default);
    Task UpdateAsync(ServiceTime item, CancellationToken ct = default);
    Task<bool> HardDeleteAsync(Guid id, CancellationToken ct = default);
}

public sealed record ServiceTimeOperationResult(bool Succeeded, string[] Errors, ServiceTimeDto? Item);

public interface IServiceTimeService
{
    Task<List<ServiceTimeDto>> ListAsync(bool includeDeleted, CancellationToken ct = default);
    Task<ServiceTimeDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<List<PublicServiceTimeDto>> ListPublicAsync(CancellationToken ct = default);
    Task<ServiceTimeOperationResult> CreateAsync(CreateServiceTimeRequest request, CancellationToken ct = default);
    Task<ServiceTimeOperationResult> UpdateAsync(Guid id, UpdateServiceTimeRequest request, CancellationToken ct = default);
    Task<ServiceTimeOperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default);
    Task<ServiceTimeOperationResult> RestoreAsync(Guid id, CancellationToken ct = default);
    Task<ServiceTimeOperationResult> HardDeleteAsync(Guid id, CancellationToken ct = default);
}

public sealed class CreateServiceTimeRequestValidator : AbstractValidator<CreateServiceTimeRequest>
{
    public CreateServiceTimeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.EndTime).Must((req, end) => end is null || end > req.StartTime)
            .WithMessage("End time must be later than start time.");
    }
}

public sealed class UpdateServiceTimeRequestValidator : AbstractValidator<UpdateServiceTimeRequest>
{
    public UpdateServiceTimeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Location).MaximumLength(200);
        RuleFor(x => x.Notes).MaximumLength(500);
        RuleFor(x => x.EndTime).Must((req, end) => end is null || end > req.StartTime)
            .WithMessage("End time must be later than start time.");
    }
}

public sealed class ServiceTimeService : IServiceTimeService
{
    private readonly IServiceTimeRepository _repo;
    private readonly IAuditLogger _audit;
    private readonly IValidator<CreateServiceTimeRequest> _createValidator;
    private readonly IValidator<UpdateServiceTimeRequest> _updateValidator;

    public ServiceTimeService(
        IServiceTimeRepository repo,
        IAuditLogger audit,
        IValidator<CreateServiceTimeRequest> createValidator,
        IValidator<UpdateServiceTimeRequest> updateValidator)
    {
        _repo = repo;
        _audit = audit;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<List<ServiceTimeDto>> ListAsync(bool includeDeleted, CancellationToken ct = default)
    {
        var items = await _repo.ListAsync(includeDeleted, ct).ConfigureAwait(false);
        return items.Select(ToDto).ToList();
    }

    public async Task<ServiceTimeDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repo.GetByIdAsync(id, includeDeleted: true, ct).ConfigureAwait(false);
        return item is null ? null : ToDto(item);
    }

    public Task<List<PublicServiceTimeDto>> ListPublicAsync(CancellationToken ct = default)
        => _repo.ListPublicAsync(ct);

    public async Task<ServiceTimeOperationResult> CreateAsync(CreateServiceTimeRequest request, CancellationToken ct = default)
    {
        var v = await _createValidator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!v.IsValid) return new(false, v.Errors.Select(e => e.ErrorMessage).ToArray(), null);

        var now = DateTimeOffset.UtcNow;
        var item = new ServiceTime
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            Location = request.Location,
            Notes = request.Notes,
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive,
            CreatedAt = now,
            ModifiedAt = now,
        };
        await _repo.AddAsync(item, ct).ConfigureAwait(false);
        await _audit.WriteAsync("ServiceTime.Created", nameof(ServiceTime), item.Id.ToString(),
            details: new { item.Name, item.DayOfWeek, item.IsActive }, cancellationToken: ct).ConfigureAwait(false);
        return new(true, Array.Empty<string>(), ToDto(item));
    }

    public async Task<ServiceTimeOperationResult> UpdateAsync(Guid id, UpdateServiceTimeRequest request, CancellationToken ct = default)
    {
        var v = await _updateValidator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!v.IsValid) return new(false, v.Errors.Select(e => e.ErrorMessage).ToArray(), null);

        var item = await _repo.GetByIdAsync(id, includeDeleted: true, ct).ConfigureAwait(false);
        if (item is null) return new(false, new[] { "Service time not found." }, null);

        item.Name = request.Name;
        item.DayOfWeek = request.DayOfWeek;
        item.StartTime = request.StartTime;
        item.EndTime = request.EndTime;
        item.Location = request.Location;
        item.Notes = request.Notes;
        item.DisplayOrder = request.DisplayOrder;
        item.IsActive = request.IsActive;
        item.ModifiedAt = DateTimeOffset.UtcNow;

        await _repo.UpdateAsync(item, ct).ConfigureAwait(false);
        await _audit.WriteAsync("ServiceTime.Updated", nameof(ServiceTime), id.ToString(),
            details: new { item.Name, item.DayOfWeek, item.IsActive }, cancellationToken: ct).ConfigureAwait(false);
        return new(true, Array.Empty<string>(), ToDto(item));
    }

    public async Task<ServiceTimeOperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repo.GetByIdAsync(id, false, ct).ConfigureAwait(false);
        if (item is null) return new(false, new[] { "Service time not found." }, null);
        item.IsDeleted = true;
        item.IsActive = false;
        item.DeletedAt = DateTimeOffset.UtcNow;
        item.ModifiedAt = item.DeletedAt.Value;
        await _repo.UpdateAsync(item, ct).ConfigureAwait(false);
        await _audit.WriteAsync("ServiceTime.SoftDeleted", nameof(ServiceTime), id.ToString(),
            details: new { item.Name }, cancellationToken: ct).ConfigureAwait(false);
        return new(true, Array.Empty<string>(), ToDto(item));
    }

    public async Task<ServiceTimeOperationResult> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repo.GetByIdAsync(id, true, ct).ConfigureAwait(false);
        if (item is null) return new(false, new[] { "Service time not found." }, null);
        item.IsDeleted = false;
        item.DeletedAt = null;
        item.ModifiedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(item, ct).ConfigureAwait(false);
        await _audit.WriteAsync("ServiceTime.Restored", nameof(ServiceTime), id.ToString(),
            details: new { item.Name }, cancellationToken: ct).ConfigureAwait(false);
        return new(true, Array.Empty<string>(), ToDto(item));
    }

    public async Task<ServiceTimeOperationResult> HardDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _repo.GetByIdAsync(id, true, ct).ConfigureAwait(false);
        if (item is null) return new(false, new[] { "Service time not found." }, null);
        if (!item.IsDeleted)
            return new(false, new[] { "Soft-delete first, then hard-delete." }, null);
        await _repo.HardDeleteAsync(id, ct).ConfigureAwait(false);
        await _audit.WriteAsync("ServiceTime.HardDeleted", nameof(ServiceTime), id.ToString(),
            details: new { item.Name }, cancellationToken: ct).ConfigureAwait(false);
        return new(true, Array.Empty<string>(), null);
    }

    internal static ServiceTimeDto ToDto(ServiceTime s) => new(
        s.Id, s.Name, s.DayOfWeek, s.StartTime, s.EndTime, s.Location, s.Notes,
        s.DisplayOrder, s.IsActive, s.IsDeleted,
        s.CreatedAt, s.ModifiedAt, s.ModifiedByUserId);
}
