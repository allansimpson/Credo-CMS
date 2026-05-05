using CredoCms.Application.Common;
using CredoCms.Domain.Announcements;
using CredoCms.Domain.Common;
using FluentValidation;

namespace CredoCms.Application.Announcements;

public sealed record AnnouncementBannerDto(
    bool IsActive, AnnouncementSeverity Severity, string Message,
    string? LinkUrl, string? LinkLabel,
    DateTimeOffset? StartsAt, DateTimeOffset? EndsAt,
    DateTimeOffset CreatedAt, DateTimeOffset ModifiedAt, Guid? ModifiedByUserId);

public sealed record PublicAnnouncementBannerDto(
    AnnouncementSeverity Severity, string Message, string? LinkUrl, string? LinkLabel);

public sealed record UpdateAnnouncementBannerRequest(
    bool IsActive, AnnouncementSeverity Severity, string Message,
    string? LinkUrl, string? LinkLabel,
    DateTimeOffset? StartsAt, DateTimeOffset? EndsAt);

public interface IAnnouncementBannerRepository
{
    Task<AnnouncementBanner> GetAsync(CancellationToken ct = default);
    Task UpdateAsync(AnnouncementBanner banner, CancellationToken ct = default);
}

public interface IAnnouncementBannerService
{
    Task<AnnouncementBannerDto> GetAsync(CancellationToken ct = default);
    Task<PublicAnnouncementBannerDto?> GetActivePublicAsync(CancellationToken ct = default);
    Task<AnnouncementBannerDto> UpdateAsync(UpdateAnnouncementBannerRequest request, CancellationToken ct = default);
}

public sealed class UpdateAnnouncementBannerRequestValidator : AbstractValidator<UpdateAnnouncementBannerRequest>
{
    public UpdateAnnouncementBannerRequestValidator()
    {
        RuleFor(x => x.Message).NotEmpty().MaximumLength(500);
        RuleFor(x => x.LinkUrl).MaximumLength(2000);
        RuleFor(x => x.LinkLabel).MaximumLength(100);
        RuleFor(x => x.EndsAt).Must((req, end) => end is null || req.StartsAt is null || end > req.StartsAt)
            .WithMessage("EndsAt must be after StartsAt.");
    }
}

public sealed class AnnouncementBannerService : IAnnouncementBannerService
{
    private readonly IAnnouncementBannerRepository _repo;
    private readonly IAuditLogger _audit;
    private readonly IValidator<UpdateAnnouncementBannerRequest> _validator;

    public AnnouncementBannerService(
        IAnnouncementBannerRepository repo, IAuditLogger audit,
        IValidator<UpdateAnnouncementBannerRequest> validator)
    {
        _repo = repo; _audit = audit; _validator = validator;
    }

    public async Task<AnnouncementBannerDto> GetAsync(CancellationToken ct = default)
        => ToDto(await _repo.GetAsync(ct).ConfigureAwait(false));

    public async Task<PublicAnnouncementBannerDto?> GetActivePublicAsync(CancellationToken ct = default)
    {
        var b = await _repo.GetAsync(ct).ConfigureAwait(false);
        if (!b.IsActive) return null;
        var now = DateTimeOffset.UtcNow;
        if (b.StartsAt is not null && b.StartsAt > now) return null;
        if (b.EndsAt is not null && b.EndsAt <= now) return null;
        return new PublicAnnouncementBannerDto(b.Severity, b.Message, b.LinkUrl, b.LinkLabel);
    }

    public async Task<AnnouncementBannerDto> UpdateAsync(UpdateAnnouncementBannerRequest request, CancellationToken ct = default)
    {
        var v = await _validator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!v.IsValid)
            throw new ValidationException(string.Join("; ", v.Errors.Select(e => e.ErrorMessage)));

        var b = await _repo.GetAsync(ct).ConfigureAwait(false);
        b.IsActive = request.IsActive;
        b.Severity = request.Severity;
        b.Message = request.Message;
        b.LinkUrl = request.LinkUrl;
        b.LinkLabel = request.LinkLabel;
        b.StartsAt = request.StartsAt;
        b.EndsAt = request.EndsAt;
        b.ModifiedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(b, ct).ConfigureAwait(false);

        await _audit.WriteAsync("AnnouncementBanner.Updated", nameof(AnnouncementBanner),
            SystemConstants.AnnouncementBannerId.ToString(),
            details: new { b.IsActive, b.Severity, MessagePreview = b.Message[..Math.Min(80, b.Message.Length)] },
            cancellationToken: ct).ConfigureAwait(false);

        return ToDto(b);
    }

    internal static AnnouncementBannerDto ToDto(AnnouncementBanner b) => new(
        b.IsActive, b.Severity, b.Message, b.LinkUrl, b.LinkLabel,
        b.StartsAt, b.EndsAt, b.CreatedAt, b.ModifiedAt, b.ModifiedByUserId);
}
