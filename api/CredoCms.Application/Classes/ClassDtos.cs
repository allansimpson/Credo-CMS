namespace CredoCms.Application.Classes;

// ---- Public DTOs (anonymous-safe) -------------------------------------

/// <summary>
/// Public class-slot summary. Fields here are safe for anonymous viewers;
/// member-only fields (Default room, teacher, detailed schedule, materials)
/// live on <see cref="MemberClassSlotDto"/> / <see cref="MemberClassOfferingDto"/>
/// and are populated by the service only when the caller is authenticated.
/// </summary>
public sealed record PublicClassSlotDto(
    Guid Id,
    string Slug,
    string Name,
    string AudienceAgeGroup,
    string? GeneralMeetingTime,
    string? DescriptionJson,
    string? ImageBlobUrl,
    string? ImageWebpBlobUrl,
    string? ImageAltText,
    int DisplayOrder,
    PublicClassOfferingDto? CurrentOffering,
    PublicClassOfferingDto? UpcomingOffering,
    /// <summary>Most-recently-ended past offering, surfaced when SiteSettings
    /// <c>ShowRecentPastOnPublicClasses</c> is on and the offering ended within
    /// the configured lookback window. Null otherwise.</summary>
    PublicClassOfferingDto? RecentPastOffering);

public sealed record PublicClassOfferingDto(
    Guid Id,
    string Subject,
    string? DescriptionJson,
    DateOnly StartDate,
    DateOnly EndDate);

// ---- Member-augmented DTOs --------------------------------------------

/// <summary>
/// Wraps the public DTO plus member-only fields. The service returns this
/// shape when the caller has a Member+ role; otherwise it returns the public
/// shape and the member fields stay null in transit (the API is shaped so
/// JSON keys are simply absent for anonymous callers).
/// </summary>
public sealed record MemberClassSlotDto(
    Guid Id,
    string Slug,
    string Name,
    string AudienceAgeGroup,
    string? GeneralMeetingTime,
    string? DescriptionJson,
    string? ImageBlobUrl,
    string? ImageWebpBlobUrl,
    string? ImageAltText,
    int DisplayOrder,
    string? DefaultRoom,
    MemberClassOfferingDto? CurrentOffering,
    MemberClassOfferingDto? UpcomingOffering,
    MemberClassOfferingDto? RecentPastOffering);

public sealed record MemberClassOfferingDto(
    Guid Id,
    string Subject,
    string? DescriptionJson,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? TeacherLeaderId,
    string? TeacherLeaderName,
    string? TeacherFreeText,
    string? DetailedScheduleJson,
    string? MaterialsNeeded);

// ---- Admin DTOs --------------------------------------------------------

public sealed record AdminClassSlotListItemDto(
    Guid Id,
    string Slug,
    string Name,
    string AudienceAgeGroup,
    bool IsActive,
    int DisplayOrder,
    int OfferingCount,
    DateTimeOffset ModifiedAt);

public sealed record AdminClassSlotDetailDto(
    Guid Id,
    string Slug,
    string Name,
    string AudienceAgeGroup,
    string? GeneralMeetingTime,
    string? DefaultRoom,
    string? DescriptionJson,
    string? ImageBlobUrl,
    string? ImageWebpBlobUrl,
    string? ImageAltText,
    bool IsActive,
    int DisplayOrder,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt);

public sealed record AdminClassOfferingDto(
    Guid Id,
    Guid ClassSlotId,
    string ClassSlotName,
    string Subject,
    string? DescriptionJson,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? TeacherLeaderId,
    string? TeacherFreeText,
    string? DetailedScheduleJson,
    string? MaterialsNeeded,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt);

// ---- Request shapes ----------------------------------------------------

public sealed record CreateClassSlotRequest(
    string Slug,
    string Name,
    string AudienceAgeGroup,
    string? GeneralMeetingTime,
    string? DefaultRoom,
    string? DescriptionJson,
    string? ImageBlobUrl,
    string? ImageWebpBlobUrl,
    string? ImageAltText,
    bool IsActive,
    int DisplayOrder);

public sealed record UpdateClassSlotRequest(
    string Slug,
    string Name,
    string AudienceAgeGroup,
    string? GeneralMeetingTime,
    string? DefaultRoom,
    string? DescriptionJson,
    string? ImageBlobUrl,
    string? ImageWebpBlobUrl,
    string? ImageAltText,
    bool IsActive,
    int DisplayOrder);

public sealed record CreateClassOfferingRequest(
    Guid ClassSlotId,
    string Subject,
    string? DescriptionJson,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? TeacherLeaderId,
    string? TeacherFreeText,
    string? DetailedScheduleJson,
    string? MaterialsNeeded);

public sealed record UpdateClassOfferingRequest(
    Guid ClassSlotId,
    string Subject,
    string? DescriptionJson,
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? TeacherLeaderId,
    string? TeacherFreeText,
    string? DetailedScheduleJson,
    string? MaterialsNeeded);

public sealed record ClassSlotMutationResult(
    bool Succeeded, IReadOnlyList<string> Errors, AdminClassSlotDetailDto? Slot = null)
{
    public static ClassSlotMutationResult Success(AdminClassSlotDetailDto s) => new(true, Array.Empty<string>(), s);
    public static ClassSlotMutationResult Failure(params string[] errors) => new(false, errors, null);
}

public sealed record ClassOfferingMutationResult(
    bool Succeeded, IReadOnlyList<string> Errors, AdminClassOfferingDto? Offering = null)
{
    public static ClassOfferingMutationResult Success(AdminClassOfferingDto o) => new(true, Array.Empty<string>(), o);
    public static ClassOfferingMutationResult Failure(params string[] errors) => new(false, errors, null);
}

public sealed record AdminClassOfferingsQuery(
    Guid? ClassSlotId = null,
    DateOnly? FromDate = null,
    DateOnly? ToDate = null,
    OfferingStatusFilter Status = OfferingStatusFilter.All);

public enum OfferingStatusFilter
{
    All = 0,
    Current = 1,
    Upcoming = 2,
    Past = 3,
}
