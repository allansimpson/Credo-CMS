namespace CredoCms.Application.Classes;

/// <summary>
/// Class slots + offerings service. Slot/offering CRUD is Administrator only;
/// the public-read API returns either the public-safe DTO or the member-
/// augmented DTO depending on the caller's role. Member-only fields
/// (DefaultRoom, TeacherLeaderId, TeacherFreeText, DetailedScheduleJson,
/// MaterialsNeeded) are stripped at the service boundary so a misconfigured
/// controller cannot leak them.
/// </summary>
public interface IClassService
{
    // ---- admin reads --------------------------------------------------

    Task<List<AdminClassSlotListItemDto>> ListSlotsAdminAsync(string? search, bool includeInactive, CancellationToken ct = default);
    Task<AdminClassSlotDetailDto?> GetSlotAdminAsync(Guid id, CancellationToken ct = default);

    Task<List<AdminClassOfferingDto>> ListOfferingsAdminAsync(AdminClassOfferingsQuery query, CancellationToken ct = default);
    Task<AdminClassOfferingDto?> GetOfferingAdminAsync(Guid id, CancellationToken ct = default);

    // ---- admin writes -------------------------------------------------

    Task<ClassSlotMutationResult> CreateSlotAsync(CreateClassSlotRequest request, CancellationToken ct = default);
    Task<ClassSlotMutationResult> UpdateSlotAsync(Guid id, UpdateClassSlotRequest request, CancellationToken ct = default);
    Task<ClassSlotMutationResult> SoftDeleteSlotAsync(Guid id, CancellationToken ct = default);

    Task<ClassOfferingMutationResult> CreateOfferingAsync(CreateClassOfferingRequest request, CancellationToken ct = default);
    Task<ClassOfferingMutationResult> UpdateOfferingAsync(Guid id, UpdateClassOfferingRequest request, CancellationToken ct = default);
    Task<ClassOfferingMutationResult> SoftDeleteOfferingAsync(Guid id, CancellationToken ct = default);

    // ---- public reads -------------------------------------------------

    /// <summary>
    /// Returns the public-facing slot list for the landing page. When
    /// <paramref name="memberView"/> is true, member-only fields are
    /// populated; when false, only public-safe fields are returned.
    /// </summary>
    Task<List<PublicClassSlotDto>> ListPublicAsync(bool showRecentPast, int recentPastLookbackDays, CancellationToken ct = default);
    Task<List<MemberClassSlotDto>> ListMemberAsync(bool showRecentPast, int recentPastLookbackDays, CancellationToken ct = default);

    Task<PublicClassSlotDto?> GetPublicBySlugAsync(string slug, bool showRecentPast, int recentPastLookbackDays, CancellationToken ct = default);
    Task<MemberClassSlotDto?> GetMemberBySlugAsync(string slug, bool showRecentPast, int recentPastLookbackDays, CancellationToken ct = default);
}
