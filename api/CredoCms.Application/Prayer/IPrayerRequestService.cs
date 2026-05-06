namespace CredoCms.Application.Prayer;

/// <summary>
/// Prayer request service. Permission rules:
///   submit                    any authenticated user
///   edit own / delete own     submitter (Pending or Active only)
///   edit any / delete any     Editor + Administrator
///   add update                Editor + Administrator (Members do NOT comment)
///   change status             Editor + Administrator
///   mark / unmark prayed      any authenticated user (idempotent)
///   anonymous display         submitter name hidden in DTOs when IsAnonymous
///                             is true; Editor/Admin admin DTO always sees
///                             the real submitter for moderation
/// Profanity check runs on submit and edit; blocks before persistence.
/// </summary>
public interface IPrayerRequestService
{
    // ---- member surface ---------------------------------------------------

    Task<PrayerMutationResult> SubmitAsync(SubmitPrayerRequestRequest request, CancellationToken ct = default);
    Task<PrayerMutationResult> EditAsync(Guid id, EditPrayerRequestRequest request, CancellationToken ct = default);
    Task<PrayerMutationResult> DeleteAsync(Guid id, CancellationToken ct = default);

    Task<List<PrayerRequestListItemDto>> ListMemberVisibleAsync(CancellationToken ct = default);
    Task<MemberPrayerRequestDto?> GetMemberAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Idempotent mark/unmark. Returns the new <c>PrayedForCount</c> after the
    /// toggle so the SPA doesn't have to round-trip a follow-up read.
    /// </summary>
    Task<int> MarkPrayedForAsync(Guid id, CancellationToken ct = default);
    Task<int> UnmarkPrayedForAsync(Guid id, CancellationToken ct = default);

    // ---- editor/admin surface --------------------------------------------

    Task<List<AdminPrayerRequestDto>> ListAdminAsync(AdminPrayerListQuery query, CancellationToken ct = default);
    Task<AdminPrayerRequestDto?> GetAdminAsync(Guid id, CancellationToken ct = default);

    Task<PrayerMutationResult> AddUpdateAsync(Guid id, AddPrayerUpdateRequest request, CancellationToken ct = default);
    Task<PrayerMutationResult> ChangeStatusAsync(Guid id, ChangePrayerStatusRequest request, CancellationToken ct = default);

    /// <summary>Bulk archive a set of requests (status → Archived). No-ops on
    /// rows the caller doesn't have permission to touch.</summary>
    Task<int> BulkArchiveAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default);
}
