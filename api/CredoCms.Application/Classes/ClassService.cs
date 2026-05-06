using CredoCms.Application.Caching;
using CredoCms.Application.Common;
using CredoCms.Application.Leaders;
using CredoCms.Domain.Classes;
using CredoCms.Domain.Common;
using FluentValidation;

namespace CredoCms.Application.Classes;

public sealed class ClassService : IClassService
{
    private readonly IClassSlotRepository _slots;
    private readonly IClassOfferingRepository _offerings;
    private readonly ILeaderRepository _leaders;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuditLogger _audit;
    private readonly IOutputCacheInvalidator _cache;
    private readonly IValidator<CreateClassSlotRequest> _createSlotValidator;
    private readonly IValidator<UpdateClassSlotRequest> _updateSlotValidator;
    private readonly IValidator<CreateClassOfferingRequest> _createOfferingValidator;
    private readonly IValidator<UpdateClassOfferingRequest> _updateOfferingValidator;

    public ClassService(
        IClassSlotRepository slots,
        IClassOfferingRepository offerings,
        ILeaderRepository leaders,
        ICurrentUserService currentUser,
        IAuditLogger audit,
        IOutputCacheInvalidator cache,
        IValidator<CreateClassSlotRequest> createSlotValidator,
        IValidator<UpdateClassSlotRequest> updateSlotValidator,
        IValidator<CreateClassOfferingRequest> createOfferingValidator,
        IValidator<UpdateClassOfferingRequest> updateOfferingValidator)
    {
        _slots = slots;
        _offerings = offerings;
        _leaders = leaders;
        _currentUser = currentUser;
        _audit = audit;
        _cache = cache;
        _createSlotValidator = createSlotValidator;
        _updateSlotValidator = updateSlotValidator;
        _createOfferingValidator = createOfferingValidator;
        _updateOfferingValidator = updateOfferingValidator;
    }

    private bool IsAdmin => _currentUser.Roles.Contains(SystemConstants.Roles.Administrator);

    // ---- admin reads --------------------------------------------------

    public async Task<List<AdminClassSlotListItemDto>> ListSlotsAdminAsync(string? search, bool includeInactive, CancellationToken ct = default)
    {
        var slots = await _slots.ListAdminAsync(search, includeInactive, ct).ConfigureAwait(false);
        var items = new List<AdminClassSlotListItemDto>(slots.Count);
        foreach (var s in slots)
        {
            var count = await _slots.CountOfferingsAsync(s.Id, ct).ConfigureAwait(false);
            items.Add(new AdminClassSlotListItemDto(
                s.Id, s.Slug, s.Name, s.AudienceAgeGroup,
                s.IsActive, s.DisplayOrder, count, s.ModifiedAt));
        }
        return items;
    }

    public async Task<AdminClassSlotDetailDto?> GetSlotAdminAsync(Guid id, CancellationToken ct = default)
    {
        var s = await _slots.GetAsync(id, ct).ConfigureAwait(false);
        return s is null ? null : ToAdminSlotDetail(s);
    }

    public async Task<List<AdminClassOfferingDto>> ListOfferingsAdminAsync(AdminClassOfferingsQuery query, CancellationToken ct = default)
    {
        var rows = await _offerings.ListAdminAsync(query, ct).ConfigureAwait(false);
        var output = new List<AdminClassOfferingDto>(rows.Count);
        foreach (var o in rows)
        {
            var slot = await _slots.GetAsync(o.ClassSlotId, ct).ConfigureAwait(false);
            output.Add(ToAdminOffering(o, slot?.Name ?? "(deleted slot)"));
        }
        return output;
    }

    public async Task<AdminClassOfferingDto?> GetOfferingAdminAsync(Guid id, CancellationToken ct = default)
    {
        var o = await _offerings.GetAsync(id, ct).ConfigureAwait(false);
        if (o is null) return null;
        var slot = await _slots.GetAsync(o.ClassSlotId, ct).ConfigureAwait(false);
        return ToAdminOffering(o, slot?.Name ?? "(deleted slot)");
    }

    // ---- admin writes -------------------------------------------------

    public async Task<ClassSlotMutationResult> CreateSlotAsync(CreateClassSlotRequest request, CancellationToken ct = default)
    {
        if (!IsAdmin) return ClassSlotMutationResult.Failure("Only administrators can create class slots.");

        var v = await _createSlotValidator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!v.IsValid) return ClassSlotMutationResult.Failure([.. v.Errors.Select(e => e.ErrorMessage)]);

        if (await _slots.SlugExistsAsync(request.Slug, null, ct).ConfigureAwait(false))
        {
            return ClassSlotMutationResult.Failure($"A class slot with slug \"{request.Slug}\" already exists.");
        }

        var now = DateTimeOffset.UtcNow;
        var entity = new ClassSlot
        {
            Id = Guid.NewGuid(),
            Slug = request.Slug,
            Name = request.Name,
            AudienceAgeGroup = request.AudienceAgeGroup,
            GeneralMeetingTime = request.GeneralMeetingTime,
            DefaultRoom = request.DefaultRoom,
            DescriptionJson = request.DescriptionJson,
            ImageBlobUrl = request.ImageBlobUrl,
            ImageWebpBlobUrl = request.ImageWebpBlobUrl,
            ImageAltText = request.ImageAltText,
            IsActive = request.IsActive,
            DisplayOrder = request.DisplayOrder,
            CreatedAt = now,
            ModifiedAt = now,
            ModifiedByUserId = _currentUser.UserId,
        };
        await _slots.AddAsync(entity, ct).ConfigureAwait(false);
        await _audit.WriteAsync("ClassSlot.Created", nameof(ClassSlot), entity.Id.ToString(),
            new { entity.Slug, entity.Name }, ct).ConfigureAwait(false);
        await _cache.InvalidateAsync(OutputCacheTags.Classes, ct).ConfigureAwait(false);
        return ClassSlotMutationResult.Success(ToAdminSlotDetail(entity));
    }

    public async Task<ClassSlotMutationResult> UpdateSlotAsync(Guid id, UpdateClassSlotRequest request, CancellationToken ct = default)
    {
        if (!IsAdmin) return ClassSlotMutationResult.Failure("Only administrators can edit class slots.");

        var v = await _updateSlotValidator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!v.IsValid) return ClassSlotMutationResult.Failure([.. v.Errors.Select(e => e.ErrorMessage)]);

        var entity = await _slots.GetAsync(id, ct).ConfigureAwait(false);
        if (entity is null) return ClassSlotMutationResult.Failure("Class slot not found.");

        if (await _slots.SlugExistsAsync(request.Slug, id, ct).ConfigureAwait(false))
        {
            return ClassSlotMutationResult.Failure($"Slug \"{request.Slug}\" is already in use.");
        }

        entity.Slug = request.Slug;
        entity.Name = request.Name;
        entity.AudienceAgeGroup = request.AudienceAgeGroup;
        entity.GeneralMeetingTime = request.GeneralMeetingTime;
        entity.DefaultRoom = request.DefaultRoom;
        entity.DescriptionJson = request.DescriptionJson;
        entity.ImageBlobUrl = request.ImageBlobUrl;
        entity.ImageWebpBlobUrl = request.ImageWebpBlobUrl;
        entity.ImageAltText = request.ImageAltText;
        entity.IsActive = request.IsActive;
        entity.DisplayOrder = request.DisplayOrder;
        entity.ModifiedAt = DateTimeOffset.UtcNow;
        entity.ModifiedByUserId = _currentUser.UserId;

        await _slots.UpdateAsync(entity, ct).ConfigureAwait(false);
        await _audit.WriteAsync("ClassSlot.Updated", nameof(ClassSlot), id.ToString(),
            new { entity.Slug, entity.Name }, ct).ConfigureAwait(false);
        await _cache.InvalidateAsync(OutputCacheTags.Classes, ct).ConfigureAwait(false);
        return ClassSlotMutationResult.Success(ToAdminSlotDetail(entity));
    }

    public async Task<ClassSlotMutationResult> SoftDeleteSlotAsync(Guid id, CancellationToken ct = default)
    {
        if (!IsAdmin) return ClassSlotMutationResult.Failure("Only administrators can delete class slots.");
        var entity = await _slots.GetAsync(id, ct).ConfigureAwait(false);
        if (entity is null) return ClassSlotMutationResult.Failure("Class slot not found.");
        await _slots.SoftDeleteAsync(id, _currentUser.UserId, ct).ConfigureAwait(false);
        await _audit.WriteAsync("ClassSlot.SoftDeleted", nameof(ClassSlot), id.ToString(),
            new { entity.Slug, entity.Name }, ct).ConfigureAwait(false);
        await _cache.InvalidateAsync(OutputCacheTags.Classes, ct).ConfigureAwait(false);
        return ClassSlotMutationResult.Success(ToAdminSlotDetail(entity));
    }

    public async Task<ClassOfferingMutationResult> CreateOfferingAsync(CreateClassOfferingRequest request, CancellationToken ct = default)
    {
        if (!IsAdmin) return ClassOfferingMutationResult.Failure("Only administrators can create offerings.");
        var v = await _createOfferingValidator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!v.IsValid) return ClassOfferingMutationResult.Failure([.. v.Errors.Select(e => e.ErrorMessage)]);

        var slot = await _slots.GetAsync(request.ClassSlotId, ct).ConfigureAwait(false);
        if (slot is null) return ClassOfferingMutationResult.Failure("Class slot not found.");

        var now = DateTimeOffset.UtcNow;
        var entity = new ClassOffering
        {
            Id = Guid.NewGuid(),
            ClassSlotId = request.ClassSlotId,
            Subject = request.Subject,
            DescriptionJson = request.DescriptionJson,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            TeacherLeaderId = request.TeacherLeaderId,
            TeacherFreeText = request.TeacherFreeText,
            DetailedScheduleJson = request.DetailedScheduleJson,
            MaterialsNeeded = request.MaterialsNeeded,
            CreatedAt = now,
            ModifiedAt = now,
            ModifiedByUserId = _currentUser.UserId,
        };
        await _offerings.AddAsync(entity, ct).ConfigureAwait(false);
        await _audit.WriteAsync("ClassOffering.Created", nameof(ClassOffering), entity.Id.ToString(),
            new { entity.ClassSlotId, entity.Subject, entity.StartDate, entity.EndDate }, ct).ConfigureAwait(false);
        await _cache.InvalidateAsync(OutputCacheTags.Classes, ct).ConfigureAwait(false);
        return ClassOfferingMutationResult.Success(ToAdminOffering(entity, slot.Name));
    }

    public async Task<ClassOfferingMutationResult> UpdateOfferingAsync(Guid id, UpdateClassOfferingRequest request, CancellationToken ct = default)
    {
        if (!IsAdmin) return ClassOfferingMutationResult.Failure("Only administrators can edit offerings.");
        var v = await _updateOfferingValidator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!v.IsValid) return ClassOfferingMutationResult.Failure([.. v.Errors.Select(e => e.ErrorMessage)]);

        var entity = await _offerings.GetAsync(id, ct).ConfigureAwait(false);
        if (entity is null) return ClassOfferingMutationResult.Failure("Offering not found.");

        var slot = await _slots.GetAsync(request.ClassSlotId, ct).ConfigureAwait(false);
        if (slot is null) return ClassOfferingMutationResult.Failure("Class slot not found.");

        entity.ClassSlotId = request.ClassSlotId;
        entity.Subject = request.Subject;
        entity.DescriptionJson = request.DescriptionJson;
        entity.StartDate = request.StartDate;
        entity.EndDate = request.EndDate;
        entity.TeacherLeaderId = request.TeacherLeaderId;
        entity.TeacherFreeText = request.TeacherFreeText;
        entity.DetailedScheduleJson = request.DetailedScheduleJson;
        entity.MaterialsNeeded = request.MaterialsNeeded;
        entity.ModifiedAt = DateTimeOffset.UtcNow;
        entity.ModifiedByUserId = _currentUser.UserId;

        await _offerings.UpdateAsync(entity, ct).ConfigureAwait(false);
        await _audit.WriteAsync("ClassOffering.Updated", nameof(ClassOffering), id.ToString(),
            new { entity.ClassSlotId, entity.Subject, entity.StartDate, entity.EndDate }, ct).ConfigureAwait(false);
        await _cache.InvalidateAsync(OutputCacheTags.Classes, ct).ConfigureAwait(false);
        return ClassOfferingMutationResult.Success(ToAdminOffering(entity, slot.Name));
    }

    public async Task<ClassOfferingMutationResult> SoftDeleteOfferingAsync(Guid id, CancellationToken ct = default)
    {
        if (!IsAdmin) return ClassOfferingMutationResult.Failure("Only administrators can delete offerings.");
        var entity = await _offerings.GetAsync(id, ct).ConfigureAwait(false);
        if (entity is null) return ClassOfferingMutationResult.Failure("Offering not found.");
        await _offerings.SoftDeleteAsync(id, _currentUser.UserId, ct).ConfigureAwait(false);
        await _audit.WriteAsync("ClassOffering.SoftDeleted", nameof(ClassOffering), id.ToString(),
            new { entity.ClassSlotId, entity.Subject }, ct).ConfigureAwait(false);
        await _cache.InvalidateAsync(OutputCacheTags.Classes, ct).ConfigureAwait(false);
        var slot = await _slots.GetAsync(entity.ClassSlotId, ct).ConfigureAwait(false);
        return ClassOfferingMutationResult.Success(ToAdminOffering(entity, slot?.Name ?? "(deleted slot)"));
    }

    // ---- public reads -------------------------------------------------

    public async Task<List<PublicClassSlotDto>> ListPublicAsync(bool showRecentPast, int recentPastLookbackDays, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var slots = await _slots.ListPublicAsync(ct).ConfigureAwait(false);
        var output = new List<PublicClassSlotDto>(slots.Count);
        foreach (var s in slots)
        {
            var current = await _offerings.GetCurrentForSlotAsync(s.Id, today, ct).ConfigureAwait(false);
            var upcoming = current is null
                ? await _offerings.GetUpcomingForSlotAsync(s.Id, today, ct).ConfigureAwait(false)
                : null;
            var recent = showRecentPast && current is null
                ? await _offerings.GetRecentPastForSlotAsync(s.Id, today, recentPastLookbackDays, ct).ConfigureAwait(false)
                : null;
            output.Add(ToPublicSlot(s,
                current is null ? null : ToPublicOffering(current),
                upcoming is null ? null : ToPublicOffering(upcoming),
                recent is null ? null : ToPublicOffering(recent)));
        }
        return output;
    }

    public async Task<List<MemberClassSlotDto>> ListMemberAsync(bool showRecentPast, int recentPastLookbackDays, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var slots = await _slots.ListPublicAsync(ct).ConfigureAwait(false);
        var output = new List<MemberClassSlotDto>(slots.Count);
        foreach (var s in slots)
        {
            var current = await _offerings.GetCurrentForSlotAsync(s.Id, today, ct).ConfigureAwait(false);
            var upcoming = current is null
                ? await _offerings.GetUpcomingForSlotAsync(s.Id, today, ct).ConfigureAwait(false)
                : null;
            var recent = showRecentPast && current is null
                ? await _offerings.GetRecentPastForSlotAsync(s.Id, today, recentPastLookbackDays, ct).ConfigureAwait(false)
                : null;
            output.Add(await ToMemberSlot(s, current, upcoming, recent, ct).ConfigureAwait(false));
        }
        return output;
    }

    public async Task<PublicClassSlotDto?> GetPublicBySlugAsync(string slug, bool showRecentPast, int recentPastLookbackDays, CancellationToken ct = default)
    {
        var s = await _slots.GetBySlugAsync(slug, ct).ConfigureAwait(false);
        if (s is null || !s.IsActive) return null;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var current = await _offerings.GetCurrentForSlotAsync(s.Id, today, ct).ConfigureAwait(false);
        var upcoming = current is null
            ? await _offerings.GetUpcomingForSlotAsync(s.Id, today, ct).ConfigureAwait(false)
            : null;
        var recent = showRecentPast && current is null
            ? await _offerings.GetRecentPastForSlotAsync(s.Id, today, recentPastLookbackDays, ct).ConfigureAwait(false)
            : null;
        return ToPublicSlot(s,
            current is null ? null : ToPublicOffering(current),
            upcoming is null ? null : ToPublicOffering(upcoming),
            recent is null ? null : ToPublicOffering(recent));
    }

    public async Task<MemberClassSlotDto?> GetMemberBySlugAsync(string slug, bool showRecentPast, int recentPastLookbackDays, CancellationToken ct = default)
    {
        var s = await _slots.GetBySlugAsync(slug, ct).ConfigureAwait(false);
        if (s is null || !s.IsActive) return null;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var current = await _offerings.GetCurrentForSlotAsync(s.Id, today, ct).ConfigureAwait(false);
        var upcoming = current is null
            ? await _offerings.GetUpcomingForSlotAsync(s.Id, today, ct).ConfigureAwait(false)
            : null;
        var recent = showRecentPast && current is null
            ? await _offerings.GetRecentPastForSlotAsync(s.Id, today, recentPastLookbackDays, ct).ConfigureAwait(false)
            : null;
        return await ToMemberSlot(s, current, upcoming, recent, ct).ConfigureAwait(false);
    }

    // ---- mappers ------------------------------------------------------

    private static AdminClassSlotDetailDto ToAdminSlotDetail(ClassSlot s) => new(
        s.Id, s.Slug, s.Name, s.AudienceAgeGroup,
        s.GeneralMeetingTime, s.DefaultRoom,
        s.DescriptionJson, s.ImageBlobUrl, s.ImageWebpBlobUrl, s.ImageAltText,
        s.IsActive, s.DisplayOrder, s.CreatedAt, s.ModifiedAt);

    private static AdminClassOfferingDto ToAdminOffering(ClassOffering o, string slotName) => new(
        o.Id, o.ClassSlotId, slotName,
        o.Subject, o.DescriptionJson, o.StartDate, o.EndDate,
        o.TeacherLeaderId, o.TeacherFreeText, o.DetailedScheduleJson, o.MaterialsNeeded,
        o.CreatedAt, o.ModifiedAt);

    private static PublicClassSlotDto ToPublicSlot(ClassSlot s,
        PublicClassOfferingDto? current, PublicClassOfferingDto? upcoming, PublicClassOfferingDto? recent) => new(
        s.Id, s.Slug, s.Name, s.AudienceAgeGroup, s.GeneralMeetingTime,
        s.DescriptionJson, s.ImageBlobUrl, s.ImageWebpBlobUrl, s.ImageAltText,
        s.DisplayOrder, current, upcoming, recent);

    private static PublicClassOfferingDto ToPublicOffering(ClassOffering o) =>
        new(o.Id, o.Subject, o.DescriptionJson, o.StartDate, o.EndDate);

    private async Task<MemberClassSlotDto> ToMemberSlot(
        ClassSlot s, ClassOffering? current, ClassOffering? upcoming, ClassOffering? recent, CancellationToken ct)
    {
        return new MemberClassSlotDto(
            s.Id, s.Slug, s.Name, s.AudienceAgeGroup, s.GeneralMeetingTime,
            s.DescriptionJson, s.ImageBlobUrl, s.ImageWebpBlobUrl, s.ImageAltText,
            s.DisplayOrder, s.DefaultRoom,
            current is null ? null : await ToMemberOffering(current, ct).ConfigureAwait(false),
            upcoming is null ? null : await ToMemberOffering(upcoming, ct).ConfigureAwait(false),
            recent is null ? null : await ToMemberOffering(recent, ct).ConfigureAwait(false));
    }

    private async Task<MemberClassOfferingDto> ToMemberOffering(ClassOffering o, CancellationToken ct)
    {
        string? leaderName = null;
        if (o.TeacherLeaderId is { } leaderId)
        {
            var leader = await _leaders.GetByIdAsync(leaderId, ct).ConfigureAwait(false);
            leaderName = leader?.FullName;
        }
        return new MemberClassOfferingDto(
            o.Id, o.Subject, o.DescriptionJson, o.StartDate, o.EndDate,
            o.TeacherLeaderId, leaderName, o.TeacherFreeText,
            o.DetailedScheduleJson, o.MaterialsNeeded);
    }
}
