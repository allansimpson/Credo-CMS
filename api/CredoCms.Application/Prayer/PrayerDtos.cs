using CredoCms.Domain.Prayer;

namespace CredoCms.Application.Prayer;

/// <summary>
/// Member-facing prayer request projection. <c>SubmitterDisplayName</c> is
/// null when the request is anonymous (the system always knows the submitter
/// internally; only the public display is anonymized). Editor/Admin callers
/// see the real submitter via <see cref="AdminPrayerRequestDto"/>.
/// </summary>
public sealed record MemberPrayerRequestDto(
    Guid Id,
    string Title,
    string BodyJson,
    string? SubmitterDisplayName,
    bool IsAnonymous,
    PrayerRequestStatus Status,
    DateTimeOffset CreatedAt,
    int PrayedForCount,
    bool ViewerHasPrayed,
    bool ViewerCanEdit,
    IReadOnlyList<PrayerRequestUpdateDto> Updates);

public sealed record AdminPrayerRequestDto(
    Guid Id,
    string Title,
    string BodyJson,
    Guid SubmittedByUserId,
    string SubmitterDisplayName,
    bool IsAnonymous,
    PrayerRequestStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset ModifiedAt,
    int PrayedForCount,
    IReadOnlyList<PrayerRequestUpdateDto> Updates);

public sealed record PrayerRequestUpdateDto(
    Guid Id,
    Guid PrayerRequestId,
    string BodyJson,
    string PostedByLabel,
    DateTimeOffset CreatedAt);

public sealed record PrayerRequestListItemDto(
    Guid Id,
    string Title,
    string BodyJson,
    string? SubmitterDisplayName,
    bool IsAnonymous,
    PrayerRequestStatus Status,
    DateTimeOffset CreatedAt,
    int PrayedForCount,
    bool ViewerHasPrayed,
    bool ViewerCanEdit,
    int UpdateCount);

// ---- requests --------------------------------------------------------

public sealed record SubmitPrayerRequestRequest(
    string Title,
    string BodyJson,
    bool IsAnonymous);

public sealed record EditPrayerRequestRequest(
    string Title,
    string BodyJson,
    bool IsAnonymous);

public sealed record AddPrayerUpdateRequest(string BodyJson);

public sealed record ChangePrayerStatusRequest(PrayerRequestStatus Status);

public sealed record PrayerMutationResult(
    bool Succeeded,
    IReadOnlyList<string> Errors,
    AdminPrayerRequestDto? Request = null)
{
    public static PrayerMutationResult Success(AdminPrayerRequestDto r) => new(true, Array.Empty<string>(), r);
    public static PrayerMutationResult Failure(params string[] errors) => new(false, errors, null);
}

public sealed record AdminPrayerListQuery(
    PrayerRequestStatus? Status = null,
    bool? IsAnonymous = null,
    string? Search = null);
