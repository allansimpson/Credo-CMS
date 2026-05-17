using CredoCms.Application.Caching;
using CredoCms.Application.Common;
using CredoCms.Application.Pages;
using CredoCms.Domain.Events;
using FluentValidation;

namespace CredoCms.Application.Events;

public sealed record EventListItemDto(
    Guid Id, string Slug, string Title,
    DateTimeOffset StartsAt, DateTimeOffset? EndsAt, bool AllDay,
    string? Location,
    EventVisibility? Visibility,
    EventRegistrationMode RegistrationMode,
    bool HasRecurrence,
    bool IsPublished,
    bool IsDeleted,
    DateTimeOffset ModifiedAt);

public sealed record EventDetailDto(
    Guid Id, string Slug, string Title, string? DescriptionJson,
    DateTimeOffset StartsAt, DateTimeOffset? EndsAt, bool AllDay,
    string? Location,
    string? HeroImageUrl, string? HeroImageWebpUrl, string? HeroImageAlt,
    EventVisibility? Visibility,
    string? RecurrenceRule, DateTimeOffset? RecurrenceEndDate, int? RecurrenceCount,
    EventRegistrationMode RegistrationMode,
    int? Capacity, bool WaitlistEnabled,
    DateTimeOffset? RegistrationOpensAt, DateTimeOffset? RegistrationClosesAt,
    string? RegistrationConfirmationMessageJson,
    string? ExternalRegistrationUrl,
    bool IsPublished, bool IsDeleted,
    DateTimeOffset CreatedAt, DateTimeOffset ModifiedAt, Guid? ModifiedByUserId,
    DateTimeOffset? DeletedAt);

public sealed record PublicEventListItemDto(
    Guid Id, string Slug, string Title,
    DateTimeOffset StartsAt, DateTimeOffset? EndsAt, bool AllDay,
    string? Location,
    string? HeroImageUrl, string? HeroImageWebpUrl, string? HeroImageAlt,
    EventVisibility? Visibility,
    EventRegistrationMode RegistrationMode,
    string? RecurrenceRule,
    DateTimeOffset NextOccurrenceAt);

public sealed record PublicEventDto(
    Guid Id, string Slug, string Title, string? DescriptionJson,
    DateTimeOffset StartsAt, DateTimeOffset? EndsAt, bool AllDay,
    string? Location,
    string? HeroImageUrl, string? HeroImageWebpUrl, string? HeroImageAlt,
    EventVisibility? Visibility,
    string? RecurrenceRule, DateTimeOffset? RecurrenceEndDate, int? RecurrenceCount,
    EventRegistrationMode RegistrationMode,
    int? Capacity, bool WaitlistEnabled,
    DateTimeOffset? RegistrationOpensAt, DateTimeOffset? RegistrationClosesAt,
    string? RegistrationConfirmationMessageJson,
    string? ExternalRegistrationUrl,
    IReadOnlyList<DateTimeOffset> NextOccurrences);

public sealed record CreateEventRequest(
    string Slug, string Title, string? DescriptionJson,
    DateTimeOffset StartsAt, DateTimeOffset? EndsAt, bool AllDay,
    string? Location,
    string? HeroImageUrl, string? HeroImageWebpUrl, string? HeroImageAlt,
    EventVisibility? Visibility,
    string? RecurrenceRule, DateTimeOffset? RecurrenceEndDate, int? RecurrenceCount,
    EventRegistrationMode RegistrationMode,
    int? Capacity, bool WaitlistEnabled,
    DateTimeOffset? RegistrationOpensAt, DateTimeOffset? RegistrationClosesAt,
    string? RegistrationConfirmationMessageJson,
    string? ExternalRegistrationUrl,
    bool IsPublished);

public sealed record UpdateEventRequest(
    string Slug, string Title, string? DescriptionJson,
    DateTimeOffset StartsAt, DateTimeOffset? EndsAt, bool AllDay,
    string? Location,
    string? HeroImageUrl, string? HeroImageWebpUrl, string? HeroImageAlt,
    EventVisibility? Visibility,
    string? RecurrenceRule, DateTimeOffset? RecurrenceEndDate, int? RecurrenceCount,
    EventRegistrationMode RegistrationMode,
    int? Capacity, bool WaitlistEnabled,
    DateTimeOffset? RegistrationOpensAt, DateTimeOffset? RegistrationClosesAt,
    string? RegistrationConfirmationMessageJson,
    string? ExternalRegistrationUrl,
    bool IsPublished);

public sealed record EventListQuery(
    string? Search = null,
    EventVisibility? Visibility = null,
    EventRegistrationMode? RegistrationMode = null,
    bool? HasRecurrence = null,
    bool IncludeDeleted = false,
    int Page = 1,
    int PageSize = 25);

public interface IEventRepository
{
    Task<PagedResult<EventListItemDto>> ListAsync(EventListQuery query, CancellationToken ct = default);
    Task<Event?> GetByIdAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default);
    Task<Event?> GetBySlugAsync(string slug, CancellationToken ct = default);
    Task<bool> SlugExistsAsync(string slug, Guid? excludingId, CancellationToken ct = default);
    Task<List<EventRecurrenceException>> GetExceptionsAsync(Guid eventId, CancellationToken ct = default);
    Task<List<EventOccurrenceOverride>> GetOverridesAsync(Guid eventId, CancellationToken ct = default);
    Task AddAsync(Event evt, CancellationToken ct = default);
    Task UpdateAsync(Event evt, CancellationToken ct = default);
    Task<bool> HardDeleteAsync(Guid id, CancellationToken ct = default);
    Task AddExceptionAsync(EventRecurrenceException ex, CancellationToken ct = default);
    Task RemoveExceptionAsync(Guid id, CancellationToken ct = default);
    Task UpsertOverrideAsync(EventOccurrenceOverride ov, CancellationToken ct = default);
    Task RemoveOverrideAsync(Guid id, CancellationToken ct = default);
    Task<List<Event>> ListPublicAsync(bool includeMembersOnly, CancellationToken ct = default);
}

public sealed record EventOperationResult(bool Succeeded, string[] Errors, EventDetailDto? Event);

public interface IEventService
{
    Task<PagedResult<EventListItemDto>> ListAsync(EventListQuery query, CancellationToken ct = default);
    Task<EventDetailDto?> GetAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default);
    Task<PublicEventDto?> GetPublicBySlugAsync(string slug, bool includeMembersOnly, CancellationToken ct = default);
    Task<PagedResult<PublicEventListItemDto>> ListPublicAsync(int page, int pageSize, bool includeMembersOnly, CancellationToken ct = default);

    Task<EventOperationResult> CreateAsync(CreateEventRequest request, CancellationToken ct = default);
    Task<EventOperationResult> UpdateAsync(Guid id, UpdateEventRequest request, CancellationToken ct = default);
    Task<EventOperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default);
    Task<EventOperationResult> RestoreAsync(Guid id, CancellationToken ct = default);
    Task<EventOperationResult> HardDeleteAsync(Guid id, CancellationToken ct = default);

    Task SkipOccurrenceAsync(Guid eventId, DateOnly occurrenceDate, string? reason, CancellationToken ct = default);
    Task SaveOccurrenceOverrideAsync(EventOccurrenceOverride ov, CancellationToken ct = default);
}

public sealed class CreateEventRequestValidator : AbstractValidator<CreateEventRequest>
{
    public CreateEventRequestValidator()
    {
        RuleFor(x => x.Slug).ValidSlug();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EndsAt).Must((req, end) => end is null || end > req.StartsAt)
            .WithMessage("End time must be after start time.");
        RuleFor(x => x.RecurrenceCount).Must((req, count) =>
                !(count.HasValue && req.RecurrenceEndDate.HasValue))
            .WithMessage("Provide either a recurrence end date OR a count, not both.");
        RuleFor(x => x.RegistrationClosesAt).Must((req, close) =>
                !(close.HasValue && req.RegistrationOpensAt.HasValue && close < req.RegistrationOpensAt))
            .WithMessage("Registration close must be after open.");
        RuleFor(x => x.Visibility).NotNull().When(x => x.IsPublished)
            .WithMessage("Visibility must be set before publishing.");
    }
}

public sealed class UpdateEventRequestValidator : AbstractValidator<UpdateEventRequest>
{
    public UpdateEventRequestValidator()
    {
        RuleFor(x => x.Slug).ValidSlug();
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.EndsAt).Must((req, end) => end is null || end > req.StartsAt)
            .WithMessage("End time must be after start time.");
        RuleFor(x => x.RecurrenceCount).Must((req, count) =>
                !(count.HasValue && req.RecurrenceEndDate.HasValue))
            .WithMessage("Provide either a recurrence end date OR a count, not both.");
        RuleFor(x => x.RegistrationClosesAt).Must((req, close) =>
                !(close.HasValue && req.RegistrationOpensAt.HasValue && close < req.RegistrationOpensAt))
            .WithMessage("Registration close must be after open.");
        RuleFor(x => x.Visibility).NotNull().When(x => x.IsPublished)
            .WithMessage("Visibility must be set before publishing.");
    }
}

public sealed class EventService : IEventService
{
    public const string EntityType = nameof(Event);
    private static readonly string[] InvalidationTags =
        { OutputCacheTags.Events, OutputCacheTags.Calendar, OutputCacheTags.Sitemap };

    private readonly IEventRepository _repo;
    private readonly IEventOccurrenceExpander _expander;
    private readonly IAuditLogger _audit;
    private readonly IOutputCacheInvalidator _cache;
    private readonly IValidator<CreateEventRequest> _createValidator;
    private readonly IValidator<UpdateEventRequest> _updateValidator;

    public EventService(
        IEventRepository repo,
        IEventOccurrenceExpander expander,
        IAuditLogger audit,
        IOutputCacheInvalidator cache,
        IValidator<CreateEventRequest> createValidator,
        IValidator<UpdateEventRequest> updateValidator)
    {
        _repo = repo;
        _expander = expander;
        _audit = audit;
        _cache = cache;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public Task<PagedResult<EventListItemDto>> ListAsync(EventListQuery query, CancellationToken ct = default)
        => _repo.ListAsync(query, ct);

    public async Task<PagedResult<PublicEventListItemDto>> ListPublicAsync(
        int page, int pageSize, bool includeMembersOnly, CancellationToken ct = default)
    {
        var pageNum = Math.Max(1, page);
        var size = Math.Clamp(pageSize, 1, 100);

        var all = await _repo.ListPublicAsync(includeMembersOnly, ct).ConfigureAwait(false);

        // Compute next-occurrence for each event so list ordering reflects the
        // upcoming schedule rather than original StartsAt.
        var horizon = DateTimeOffset.UtcNow.AddYears(2);
        var now = DateTimeOffset.UtcNow;

        var withNext = new List<(Event evt, DateTimeOffset nextAt)>(all.Count);
        foreach (var evt in all)
        {
            var exceptions = await _repo.GetExceptionsAsync(evt.Id, ct).ConfigureAwait(false);
            var overrides = await _repo.GetOverridesAsync(evt.Id, ct).ConfigureAwait(false);
            var next = _expander.Expand(evt, exceptions, overrides, now, horizon)
                .Select(o => (DateTimeOffset?)o.StartsAt).FirstOrDefault();
            if (next is null) continue;
            withNext.Add((evt, next.Value));
        }

        var ordered = withNext
            .OrderBy(t => t.nextAt)
            .ToList();
        var total = ordered.Count;
        var pageItems = ordered
            .Skip((pageNum - 1) * size).Take(size)
            .Select(t => new PublicEventListItemDto(
                t.evt.Id, t.evt.Slug, t.evt.Title,
                t.evt.StartsAt, t.evt.EndsAt, t.evt.AllDay,
                t.evt.Location,
                t.evt.HeroImageUrl, t.evt.HeroImageWebpUrl, t.evt.HeroImageAlt,
                t.evt.Visibility, t.evt.RegistrationMode,
                t.evt.RecurrenceRule, t.nextAt))
            .ToList();
        return new PagedResult<PublicEventListItemDto>(pageItems, total, pageNum, size);
    }

    public async Task<EventDetailDto?> GetAsync(Guid id, bool includeDeleted = false, CancellationToken ct = default)
    {
        var evt = await _repo.GetByIdAsync(id, includeDeleted, ct).ConfigureAwait(false);
        return evt is null ? null : ToDetail(evt);
    }

    public async Task<PublicEventDto?> GetPublicBySlugAsync(string slug, bool includeMembersOnly, CancellationToken ct = default)
    {
        var evt = await _repo.GetBySlugAsync(slug, ct).ConfigureAwait(false);
        if (evt is null || !evt.IsPublished) return null;
        if (evt.Visibility == EventVisibility.MembersOnly && !includeMembersOnly) return null;

        var exceptions = await _repo.GetExceptionsAsync(evt.Id, ct).ConfigureAwait(false);
        var overrides = await _repo.GetOverridesAsync(evt.Id, ct).ConfigureAwait(false);
        var horizon = DateTimeOffset.UtcNow.AddMonths(6);
        var nextOccurrences = _expander
            .Expand(evt, exceptions, overrides, DateTimeOffset.UtcNow, horizon)
            .Take(10)
            .Select(o => o.StartsAt)
            .ToList();

        return new PublicEventDto(
            evt.Id, evt.Slug, evt.Title, evt.DescriptionJson,
            evt.StartsAt, evt.EndsAt, evt.AllDay, evt.Location,
            evt.HeroImageUrl, evt.HeroImageWebpUrl, evt.HeroImageAlt,
            evt.Visibility, evt.RecurrenceRule, evt.RecurrenceEndDate, evt.RecurrenceCount,
            evt.RegistrationMode, evt.Capacity, evt.WaitlistEnabled,
            evt.RegistrationOpensAt, evt.RegistrationClosesAt,
            evt.RegistrationConfirmationMessageJson, evt.ExternalRegistrationUrl,
            nextOccurrences);
    }

    public async Task<EventOperationResult> CreateAsync(CreateEventRequest request, CancellationToken ct = default)
    {
        var v = await _createValidator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!v.IsValid) return new(false, v.Errors.Select(e => e.ErrorMessage).ToArray(), null);

        if (await _repo.SlugExistsAsync(request.Slug, null, ct).ConfigureAwait(false))
            return new(false, new[] { $"Slug '{request.Slug}' is already in use." }, null);

        var now = DateTimeOffset.UtcNow;
        var evt = new Event
        {
            Id = Guid.NewGuid(),
            Slug = request.Slug,
            Title = request.Title,
            DescriptionJson = request.DescriptionJson,
            StartsAt = request.StartsAt,
            EndsAt = request.EndsAt,
            AllDay = request.AllDay,
            Location = request.Location,
            HeroImageUrl = request.HeroImageUrl,
            HeroImageWebpUrl = request.HeroImageWebpUrl,
            HeroImageAlt = request.HeroImageAlt,
            Visibility = request.Visibility,
            RecurrenceRule = request.RecurrenceRule,
            RecurrenceEndDate = request.RecurrenceEndDate,
            RecurrenceCount = request.RecurrenceCount,
            RegistrationMode = request.RegistrationMode,
            Capacity = request.Capacity,
            WaitlistEnabled = request.WaitlistEnabled,
            RegistrationOpensAt = request.RegistrationOpensAt,
            RegistrationClosesAt = request.RegistrationClosesAt,
            RegistrationConfirmationMessageJson = request.RegistrationConfirmationMessageJson,
            ExternalRegistrationUrl = request.ExternalRegistrationUrl,
            IsPublished = request.IsPublished,
            CreatedAt = now,
            ModifiedAt = now,
        };
        await _repo.AddAsync(evt, ct).ConfigureAwait(false);
        await _audit.WriteAsync("Event.Created", EntityType, evt.Id.ToString(),
            details: new { evt.Slug, evt.Title }, cancellationToken: ct).ConfigureAwait(false);
        await _cache.InvalidateAsync(InvalidationTags, ct).ConfigureAwait(false);
        return new(true, Array.Empty<string>(), ToDetail(evt));
    }

    public async Task<EventOperationResult> UpdateAsync(Guid id, UpdateEventRequest request, CancellationToken ct = default)
    {
        var v = await _updateValidator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!v.IsValid) return new(false, v.Errors.Select(e => e.ErrorMessage).ToArray(), null);

        var evt = await _repo.GetByIdAsync(id, includeDeleted: true, ct).ConfigureAwait(false);
        if (evt is null) return new(false, new[] { "Event not found." }, null);

        if (!string.Equals(evt.Slug, request.Slug, StringComparison.Ordinal)
            && await _repo.SlugExistsAsync(request.Slug, id, ct).ConfigureAwait(false))
        {
            return new(false, new[] { $"Slug '{request.Slug}' is already in use." }, null);
        }

        evt.Slug = request.Slug; evt.Title = request.Title; evt.DescriptionJson = request.DescriptionJson;
        evt.StartsAt = request.StartsAt; evt.EndsAt = request.EndsAt; evt.AllDay = request.AllDay;
        evt.Location = request.Location;
        evt.HeroImageUrl = request.HeroImageUrl; evt.HeroImageWebpUrl = request.HeroImageWebpUrl; evt.HeroImageAlt = request.HeroImageAlt;
        evt.Visibility = request.Visibility;
        evt.RecurrenceRule = request.RecurrenceRule;
        evt.RecurrenceEndDate = request.RecurrenceEndDate;
        evt.RecurrenceCount = request.RecurrenceCount;
        evt.RegistrationMode = request.RegistrationMode;
        evt.Capacity = request.Capacity; evt.WaitlistEnabled = request.WaitlistEnabled;
        evt.RegistrationOpensAt = request.RegistrationOpensAt;
        evt.RegistrationClosesAt = request.RegistrationClosesAt;
        evt.RegistrationConfirmationMessageJson = request.RegistrationConfirmationMessageJson;
        evt.ExternalRegistrationUrl = request.ExternalRegistrationUrl;
        evt.IsPublished = request.IsPublished;
        evt.ModifiedAt = DateTimeOffset.UtcNow;

        await _repo.UpdateAsync(evt, ct).ConfigureAwait(false);
        await _audit.WriteAsync("Event.Updated", EntityType, id.ToString(),
            details: new { evt.Slug, evt.Title, evt.IsPublished }, cancellationToken: ct).ConfigureAwait(false);
        await _cache.InvalidateAsync(InvalidationTags, ct).ConfigureAwait(false);
        return new(true, Array.Empty<string>(), ToDetail(evt));
    }

    public async Task<EventOperationResult> SoftDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var evt = await _repo.GetByIdAsync(id, false, ct).ConfigureAwait(false);
        if (evt is null) return new(false, new[] { "Event not found." }, null);
        evt.IsDeleted = true;
        evt.IsPublished = false;
        evt.DeletedAt = DateTimeOffset.UtcNow;
        evt.ModifiedAt = evt.DeletedAt.Value;
        await _repo.UpdateAsync(evt, ct).ConfigureAwait(false);
        await _audit.WriteAsync("Event.SoftDeleted", EntityType, id.ToString(),
            details: new { evt.Slug, evt.Title }, cancellationToken: ct).ConfigureAwait(false);
        await _cache.InvalidateAsync(InvalidationTags, ct).ConfigureAwait(false);
        return new(true, Array.Empty<string>(), ToDetail(evt));
    }

    public async Task<EventOperationResult> RestoreAsync(Guid id, CancellationToken ct = default)
    {
        var evt = await _repo.GetByIdAsync(id, true, ct).ConfigureAwait(false);
        if (evt is null) return new(false, new[] { "Event not found." }, null);
        evt.IsDeleted = false; evt.DeletedAt = null;
        evt.ModifiedAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(evt, ct).ConfigureAwait(false);
        await _audit.WriteAsync("Event.Restored", EntityType, id.ToString(),
            details: new { evt.Slug, evt.Title }, cancellationToken: ct).ConfigureAwait(false);
        await _cache.InvalidateAsync(InvalidationTags, ct).ConfigureAwait(false);
        return new(true, Array.Empty<string>(), ToDetail(evt));
    }

    public async Task<EventOperationResult> HardDeleteAsync(Guid id, CancellationToken ct = default)
    {
        var evt = await _repo.GetByIdAsync(id, true, ct).ConfigureAwait(false);
        if (evt is null) return new(false, new[] { "Event not found." }, null);
        if (!evt.IsDeleted)
            return new(false, new[] { "Soft-delete first, then hard-delete." }, null);
        await _repo.HardDeleteAsync(id, ct).ConfigureAwait(false);
        await _audit.WriteAsync("Event.HardDeleted", EntityType, id.ToString(),
            details: new { evt.Slug, evt.Title }, cancellationToken: ct).ConfigureAwait(false);
        await _cache.InvalidateAsync(InvalidationTags, ct).ConfigureAwait(false);
        return new(true, Array.Empty<string>(), null);
    }

    public async Task SkipOccurrenceAsync(Guid eventId, DateOnly occurrenceDate, string? reason, CancellationToken ct = default)
    {
        await _repo.AddExceptionAsync(new EventRecurrenceException
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            OccurrenceDate = occurrenceDate,
            Reason = reason,
        }, ct).ConfigureAwait(false);
        await _audit.WriteAsync("Event.OccurrenceCanceled", EntityType, eventId.ToString(),
            details: new { occurrenceDate, reason }, cancellationToken: ct).ConfigureAwait(false);
        await _cache.InvalidateAsync(InvalidationTags, ct).ConfigureAwait(false);
    }

    public async Task SaveOccurrenceOverrideAsync(EventOccurrenceOverride ov, CancellationToken ct = default)
    {
        await _repo.UpsertOverrideAsync(ov, ct).ConfigureAwait(false);
        await _audit.WriteAsync("Event.OccurrenceOverridden", EntityType, ov.EventId.ToString(),
            details: new { ov.OriginalOccurrenceDate, ov.IsCanceled }, cancellationToken: ct).ConfigureAwait(false);
        await _cache.InvalidateAsync(InvalidationTags, ct).ConfigureAwait(false);
    }

    private static EventDetailDto ToDetail(Event e) => new(
        e.Id, e.Slug, e.Title, e.DescriptionJson,
        e.StartsAt, e.EndsAt, e.AllDay, e.Location,
        e.HeroImageUrl, e.HeroImageWebpUrl, e.HeroImageAlt,
        e.Visibility,
        e.RecurrenceRule, e.RecurrenceEndDate, e.RecurrenceCount,
        e.RegistrationMode, e.Capacity, e.WaitlistEnabled,
        e.RegistrationOpensAt, e.RegistrationClosesAt,
        e.RegistrationConfirmationMessageJson, e.ExternalRegistrationUrl,
        e.IsPublished, e.IsDeleted,
        e.CreatedAt, e.ModifiedAt, e.ModifiedByUserId, e.DeletedAt);
}
