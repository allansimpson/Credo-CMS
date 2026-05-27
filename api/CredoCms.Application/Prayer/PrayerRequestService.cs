using CredoCms.Application.Common;
using CredoCms.Application.Profanity;
using CredoCms.Application.RealTime;
using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Domain.Common;
using CredoCms.Domain.Identity;
using CredoCms.Domain.Prayer;
using FluentValidation;
using Microsoft.AspNetCore.Identity;

namespace CredoCms.Application.Prayer;

public sealed class PrayerRequestService : IPrayerRequestService
{
    private readonly IPrayerRequestRepository _repo;
    private readonly UserManager<ApplicationUser> _users;
    private readonly ICurrentUserService _currentUser;
    private readonly IProfanityCheckService _profanity;
    private readonly ISiteSettingsRepository _settings;
    private readonly IRealtimeNotifier _notifier;
    private readonly IAuditLogger _audit;
    private readonly IValidator<SubmitPrayerRequestRequest> _submitValidator;
    private readonly IValidator<EditPrayerRequestRequest> _editValidator;
    private readonly IValidator<AddPrayerUpdateRequest> _updateValidator;

    public PrayerRequestService(
        IPrayerRequestRepository repo,
        UserManager<ApplicationUser> users,
        ICurrentUserService currentUser,
        IProfanityCheckService profanity,
        ISiteSettingsRepository settings,
        IRealtimeNotifier notifier,
        IAuditLogger audit,
        IValidator<SubmitPrayerRequestRequest> submitValidator,
        IValidator<EditPrayerRequestRequest> editValidator,
        IValidator<AddPrayerUpdateRequest> updateValidator)
    {
        _repo = repo;
        _users = users;
        _currentUser = currentUser;
        _profanity = profanity;
        _settings = settings;
        _notifier = notifier;
        _audit = audit;
        _submitValidator = submitValidator;
        _editValidator = editValidator;
        _updateValidator = updateValidator;
    }

    private bool IsAdmin => _currentUser.Roles.Contains(SystemConstants.Roles.Administrator);
    private bool IsEditor => _currentUser.Roles.Contains(SystemConstants.Roles.Editor);
    private bool IsAuthenticated => _currentUser.IsAuthenticated && _currentUser.UserId != SystemConstants.SystemUserId;

    // ---- member surface ---------------------------------------------------

    public async Task<PrayerMutationResult> SubmitAsync(SubmitPrayerRequestRequest request, CancellationToken ct = default)
    {
        if (!IsAuthenticated) return PrayerMutationResult.Failure("Sign in to submit a prayer request.");

        var v = await _submitValidator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!v.IsValid) return PrayerMutationResult.Failure([.. v.Errors.Select(e => e.ErrorMessage)]);

        if (await _profanity.ContainsProfanityAsync(request.Title, ct).ConfigureAwait(false)
            || await _profanity.ContainsProfanityAsync(request.BodyJson, ct).ConfigureAwait(false))
        {
            return PrayerMutationResult.Failure("Please revise the language in your request before submitting.");
        }

        var now = DateTimeOffset.UtcNow;
        var entity = new PrayerRequest
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            BodyJson = request.BodyJson,
            SubmittedByUserId = _currentUser.UserId,
            IsAnonymous = request.IsAnonymous,
            Status = PrayerRequestStatus.Active,
            CreatedAt = now,
            ModifiedAt = now,
            ModifiedByUserId = _currentUser.UserId,
        };
        await _repo.AddAsync(entity, ct).ConfigureAwait(false);

        await _audit.WriteAsync("PrayerRequest.Submitted", nameof(PrayerRequest), entity.Id.ToString(),
            new { entity.IsAnonymous }, ct).ConfigureAwait(false);

        await _notifier.NotifyPrayerRequestEventAsync(
            new PrayerRequestEventMessage("PrayerRequestCreated", entity.Id, entity.Title), ct)
            .ConfigureAwait(false);

        return PrayerMutationResult.Success(await ToAdminAsync(entity, ct).ConfigureAwait(false));
    }

    public async Task<PrayerMutationResult> EditAsync(Guid id, EditPrayerRequestRequest request, CancellationToken ct = default)
    {
        var entity = await _repo.GetAsync(id, ct).ConfigureAwait(false);
        if (entity is null) return PrayerMutationResult.Failure("Prayer request not found.");

        var allowed = IsAdmin || IsEditor || (IsAuthenticated && entity.SubmittedByUserId == _currentUser.UserId);
        if (!allowed) return PrayerMutationResult.Failure("You don't have permission to edit this request.");

        var v = await _editValidator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!v.IsValid) return PrayerMutationResult.Failure([.. v.Errors.Select(e => e.ErrorMessage)]);

        if (await _profanity.ContainsProfanityAsync(request.Title, ct).ConfigureAwait(false)
            || await _profanity.ContainsProfanityAsync(request.BodyJson, ct).ConfigureAwait(false))
        {
            return PrayerMutationResult.Failure("Please revise the language in your request before saving.");
        }

        entity.Title = request.Title;
        entity.BodyJson = request.BodyJson;
        entity.IsAnonymous = request.IsAnonymous;
        entity.ModifiedAt = DateTimeOffset.UtcNow;
        entity.ModifiedByUserId = _currentUser.UserId;
        await _repo.UpdateAsync(entity, ct).ConfigureAwait(false);

        await _audit.WriteAsync("PrayerRequest.Updated", nameof(PrayerRequest), entity.Id.ToString(),
            new { entity.IsAnonymous }, ct).ConfigureAwait(false);

        await _notifier.NotifyPrayerRequestEventAsync(
            new PrayerRequestEventMessage("PrayerRequestUpdated", entity.Id, entity.Title), ct)
            .ConfigureAwait(false);

        return PrayerMutationResult.Success(await ToAdminAsync(entity, ct).ConfigureAwait(false));
    }

    public async Task<PrayerMutationResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await _repo.GetAsync(id, ct).ConfigureAwait(false);
        if (entity is null) return PrayerMutationResult.Failure("Prayer request not found.");

        var allowed = IsAdmin || IsEditor || (IsAuthenticated && entity.SubmittedByUserId == _currentUser.UserId);
        if (!allowed) return PrayerMutationResult.Failure("You don't have permission to delete this request.");

        await _repo.SoftDeleteAsync(id, _currentUser.UserId, ct).ConfigureAwait(false);
        await _audit.WriteAsync("PrayerRequest.SoftDeleted", nameof(PrayerRequest), id.ToString(),
            new { entity.IsAnonymous }, ct).ConfigureAwait(false);

        return PrayerMutationResult.Success(await ToAdminAsync(entity, ct).ConfigureAwait(false));
    }

    public async Task<List<PrayerRequestListItemDto>> ListMemberVisibleAsync(CancellationToken ct = default)
    {
        if (!IsAuthenticated) return new List<PrayerRequestListItemDto>();

        var settings = await _settings.GetAsync(ct).ConfigureAwait(false);
        var rows = await _repo.ListMemberVisibleAsync(settings.PrayerRequestArchiveDays, ct).ConfigureAwait(false);

        var output = new List<PrayerRequestListItemDto>(rows.Count);
        foreach (var r in rows)
        {
            var count = await _repo.PrayedForCountAsync(r.Id, ct).ConfigureAwait(false);
            var hasPrayed = await _repo.HasPrayedAsync(r.Id, _currentUser.UserId, ct).ConfigureAwait(false);
            var updates = await _repo.ListUpdatesForAsync(r.Id, ct).ConfigureAwait(false);
            output.Add(new PrayerRequestListItemDto(
                r.Id, r.Title, r.BodyJson,
                await DisplayNameForViewerAsync(r, ct).ConfigureAwait(false),
                r.IsAnonymous, r.Status, r.CreatedAt,
                count, hasPrayed,
                ViewerCanEdit: IsAdmin || IsEditor || r.SubmittedByUserId == _currentUser.UserId,
                updates.Count));
        }
        return output;
    }

    public async Task<MemberPrayerRequestDto?> GetMemberAsync(Guid id, CancellationToken ct = default)
    {
        if (!IsAuthenticated) return null;

        var entity = await _repo.GetAsync(id, ct).ConfigureAwait(false);
        if (entity is null) return null;

        var count = await _repo.PrayedForCountAsync(id, ct).ConfigureAwait(false);
        var hasPrayed = await _repo.HasPrayedAsync(id, _currentUser.UserId, ct).ConfigureAwait(false);
        var updates = await _repo.ListUpdatesForAsync(id, ct).ConfigureAwait(false);

        return new MemberPrayerRequestDto(
            entity.Id, entity.Title, entity.BodyJson,
            await DisplayNameForViewerAsync(entity, ct).ConfigureAwait(false),
            entity.IsAnonymous, entity.Status, entity.CreatedAt,
            count, hasPrayed,
            ViewerCanEdit: IsAdmin || IsEditor || entity.SubmittedByUserId == _currentUser.UserId,
            updates.Select(ToUpdateDto).ToList());
    }

    public async Task<int> MarkPrayedForAsync(Guid id, CancellationToken ct = default)
    {
        if (!IsAuthenticated) return 0;
        var entity = await _repo.GetAsync(id, ct).ConfigureAwait(false);
        if (entity is null) return 0;

        await _repo.AddPrayedForAsync(id, _currentUser.UserId, ct).ConfigureAwait(false);
        var count = await _repo.PrayedForCountAsync(id, ct).ConfigureAwait(false);

        await _notifier.NotifyPrayerRequestEventAsync(
            new PrayerRequestEventMessage("PrayerRequestPrayedForCountChanged", id, entity.Title, count), ct)
            .ConfigureAwait(false);
        return count;
    }

    public async Task<int> UnmarkPrayedForAsync(Guid id, CancellationToken ct = default)
    {
        if (!IsAuthenticated) return 0;
        var entity = await _repo.GetAsync(id, ct).ConfigureAwait(false);
        if (entity is null) return 0;

        await _repo.RemovePrayedForAsync(id, _currentUser.UserId, ct).ConfigureAwait(false);
        var count = await _repo.PrayedForCountAsync(id, ct).ConfigureAwait(false);

        await _notifier.NotifyPrayerRequestEventAsync(
            new PrayerRequestEventMessage("PrayerRequestPrayedForCountChanged", id, entity.Title, count), ct)
            .ConfigureAwait(false);
        return count;
    }

    // ---- admin surface ----------------------------------------------------

    public async Task<List<AdminPrayerRequestDto>> ListAdminAsync(AdminPrayerListQuery query, CancellationToken ct = default)
    {
        if (!IsAdmin && !IsEditor) return new List<AdminPrayerRequestDto>();
        var rows = await _repo.ListAdminAsync(query, ct).ConfigureAwait(false);
        var output = new List<AdminPrayerRequestDto>(rows.Count);
        foreach (var r in rows)
        {
            output.Add(await ToAdminAsync(r, ct).ConfigureAwait(false));
        }
        return output;
    }

    public async Task<AdminPrayerRequestDto?> GetAdminAsync(Guid id, CancellationToken ct = default)
    {
        if (!IsAdmin && !IsEditor) return null;
        var entity = await _repo.GetAsync(id, ct).ConfigureAwait(false);
        return entity is null ? null : await ToAdminAsync(entity, ct).ConfigureAwait(false);
    }

    public async Task<PrayerMutationResult> AddUpdateAsync(Guid id, AddPrayerUpdateRequest request, CancellationToken ct = default)
    {
        if (!IsAdmin && !IsEditor)
            return PrayerMutationResult.Failure("Only editors and administrators can post updates.");

        var entity = await _repo.GetAsync(id, ct).ConfigureAwait(false);
        if (entity is null) return PrayerMutationResult.Failure("Prayer request not found.");

        var v = await _updateValidator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!v.IsValid) return PrayerMutationResult.Failure([.. v.Errors.Select(e => e.ErrorMessage)]);

        if (await _profanity.ContainsProfanityAsync(request.BodyJson, ct).ConfigureAwait(false))
        {
            return PrayerMutationResult.Failure("Please revise the language before posting.");
        }

        var now = DateTimeOffset.UtcNow;
        var update = new PrayerRequestUpdate
        {
            Id = Guid.NewGuid(),
            PrayerRequestId = id,
            BodyJson = request.BodyJson,
            PostedByUserId = _currentUser.UserId,
            PostedByLabel = _currentUser.DisplayName,
            CreatedAt = now,
            ModifiedAt = now,
            ModifiedByUserId = _currentUser.UserId,
        };
        await _repo.AddUpdateAsync(update, ct).ConfigureAwait(false);

        await _audit.WriteAsync("PrayerRequest.UpdateAdded", nameof(PrayerRequestUpdate), update.Id.ToString(),
            new { PrayerRequestId = id }, ct).ConfigureAwait(false);

        await _notifier.NotifyPrayerRequestEventAsync(
            new PrayerRequestEventMessage("PrayerRequestUpdateAdded", id, entity.Title), ct)
            .ConfigureAwait(false);

        return PrayerMutationResult.Success(await ToAdminAsync(entity, ct).ConfigureAwait(false));
    }

    public async Task<PrayerMutationResult> ChangeStatusAsync(Guid id, ChangePrayerStatusRequest request, CancellationToken ct = default)
    {
        if (!IsAdmin && !IsEditor)
            return PrayerMutationResult.Failure("Only editors and administrators can change status.");

        var entity = await _repo.GetAsync(id, ct).ConfigureAwait(false);
        if (entity is null) return PrayerMutationResult.Failure("Prayer request not found.");

        entity.Status = request.Status;
        entity.ModifiedAt = DateTimeOffset.UtcNow;
        entity.ModifiedByUserId = _currentUser.UserId;
        await _repo.UpdateAsync(entity, ct).ConfigureAwait(false);

        await _audit.WriteAsync("PrayerRequest.StatusChanged", nameof(PrayerRequest), entity.Id.ToString(),
            new { Status = request.Status.ToString() }, ct).ConfigureAwait(false);

        await _notifier.NotifyPrayerRequestEventAsync(
            new PrayerRequestEventMessage("PrayerRequestStatusChanged", entity.Id, entity.Title), ct)
            .ConfigureAwait(false);

        return PrayerMutationResult.Success(await ToAdminAsync(entity, ct).ConfigureAwait(false));
    }

    public async Task<int> BulkArchiveAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default)
    {
        if (!IsAdmin && !IsEditor) return 0;
        var moved = 0;
        foreach (var id in ids)
        {
            var entity = await _repo.GetAsync(id, ct).ConfigureAwait(false);
            if (entity is null) continue;
            if (entity.Status == PrayerRequestStatus.Archived) continue;
            entity.Status = PrayerRequestStatus.Archived;
            entity.ModifiedAt = DateTimeOffset.UtcNow;
            entity.ModifiedByUserId = _currentUser.UserId;
            await _repo.UpdateAsync(entity, ct).ConfigureAwait(false);
            moved += 1;
        }
        if (moved > 0)
        {
            await _audit.WriteAsync("PrayerRequest.BulkArchived", nameof(PrayerRequest), null,
                new { count = moved, ids }, ct).ConfigureAwait(false);
        }
        return moved;
    }

    // ---- helpers ---------------------------------------------------------

    /// <summary>
    /// Resolves the display name shown in member-facing DTOs:
    ///   • IsAnonymous + viewer is NOT submitter / NOT admin/editor → null
    ///   • Otherwise → the user's display name
    /// Editor and Administrator viewers always see the real submitter (so
    /// moderation works); their member endpoint is the same shape as
    /// regular members but with the gate widened.
    /// </summary>
    private async Task<string?> DisplayNameForViewerAsync(PrayerRequest entity, CancellationToken ct)
    {
        var viewerIsPrivileged = IsAdmin || IsEditor || entity.SubmittedByUserId == _currentUser.UserId;
        if (entity.IsAnonymous && !viewerIsPrivileged) return null;

        var user = await _users.FindByIdAsync(entity.SubmittedByUserId.ToString()).ConfigureAwait(false);
        return user?.DisplayName;
    }

    private async Task<AdminPrayerRequestDto> ToAdminAsync(PrayerRequest r, CancellationToken ct)
    {
        var user = await _users.FindByIdAsync(r.SubmittedByUserId.ToString()).ConfigureAwait(false);
        var count = await _repo.PrayedForCountAsync(r.Id, ct).ConfigureAwait(false);
        var updates = await _repo.ListUpdatesForAsync(r.Id, ct).ConfigureAwait(false);
        return new AdminPrayerRequestDto(
            r.Id, r.Title, r.BodyJson,
            r.SubmittedByUserId, user?.DisplayName ?? "(unknown)",
            r.IsAnonymous, r.Status, r.CreatedAt, r.ModifiedAt,
            count, updates.Select(ToUpdateDto).ToList());
    }

    private static PrayerRequestUpdateDto ToUpdateDto(PrayerRequestUpdate u) =>
        new(u.Id, u.PrayerRequestId, u.BodyJson, u.PostedByLabel, u.CreatedAt);
}
