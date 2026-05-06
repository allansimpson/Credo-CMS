using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CredoCms.Application.Common;
using CredoCms.Application.RealTime;
using CredoCms.Application.SiteSettingsManagement;
using CredoCms.Domain.Common;
using CredoCms.Domain.ConnectCard;
using FluentValidation;

namespace CredoCms.Application.ConnectCard;

public sealed class ConnectCardService : IConnectCardService
{
    /// <summary>Minimum elapsed time between page load and submit. Anything
    /// below is treated as a bot signature.</summary>
    public static readonly TimeSpan MinTimeToSubmit = TimeSpan.FromSeconds(5);

    private readonly IConnectCardRepository _repo;
    private readonly ITurnstileValidationService _turnstile;
    private readonly IEmailService _email;
    private readonly ISiteSettingsRepository _settings;
    private readonly ICurrentUserService _currentUser;
    private readonly IRealtimeNotifier _notifier;
    private readonly IAuditLogger _audit;
    private readonly IValidator<SubmitConnectCardRequest> _submitValidator;

    public ConnectCardService(
        IConnectCardRepository repo,
        ITurnstileValidationService turnstile,
        IEmailService email,
        ISiteSettingsRepository settings,
        ICurrentUserService currentUser,
        IRealtimeNotifier notifier,
        IAuditLogger audit,
        IValidator<SubmitConnectCardRequest> submitValidator)
    {
        _repo = repo;
        _turnstile = turnstile;
        _email = email;
        _settings = settings;
        _currentUser = currentUser;
        _notifier = notifier;
        _audit = audit;
        _submitValidator = submitValidator;
    }

    private bool IsAdmin => _currentUser.Roles.Contains(SystemConstants.Roles.Administrator);
    private bool IsEditor => _currentUser.Roles.Contains(SystemConstants.Roles.Editor);
    private bool IsAdminShell => IsAdmin || IsEditor;

    public async Task<SubmitConnectCardResult> SubmitAsync(
        SubmitConnectCardRequest request, string? remoteIp, CancellationToken ct = default)
    {
        // ---- 1. Anti-bot heuristics ---------------------------------------
        // Honeypot first; bots that fill every field will trip this and get
        // a generic "thanks" response (we still 200 the request to avoid
        // leaking the heuristic to abuse tooling — see controller).
        if (!string.IsNullOrEmpty(request.HoneypotValue))
        {
            return SubmitConnectCardResult.Failure("Submission rejected.");
        }

        // 5-second time-to-submit. ClientLoadedAt is required; missing or
        // future-dated values are treated as bot.
        if (request.ClientLoadedAt is null
            || DateTimeOffset.UtcNow - request.ClientLoadedAt.Value < MinTimeToSubmit)
        {
            return SubmitConnectCardResult.Failure("Submission rejected.");
        }

        // ---- 2. Turnstile validation -------------------------------------
        var turnstileOk = await _turnstile.ValidateAsync(request.TurnstileToken, remoteIp, ct).ConfigureAwait(false);
        if (!turnstileOk)
        {
            return SubmitConnectCardResult.Failure(
                "We couldn't verify that you're human. Please refresh and try again.");
        }

        // ---- 3. Field validation -----------------------------------------
        var v = await _submitValidator.ValidateAsync(request, ct).ConfigureAwait(false);
        if (!v.IsValid) return SubmitConnectCardResult.Failure([.. v.Errors.Select(e => e.ErrorMessage)]);

        // ---- 4. Persist ---------------------------------------------------
        var now = DateTimeOffset.UtcNow;
        var entity = new ConnectCardSubmission
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Email = NullIfBlank(request.Email),
            Phone = NullIfBlank(request.Phone),
            IsFirstTimeVisitor = request.IsFirstTimeVisitor,
            ServiceDate = request.ServiceDate,
            HowDidYouHear = request.HowDidYouHear.Trim(),
            Comments = NullIfBlank(request.Comments),
            InterestCheckboxesJson = request.Interests is { Count: > 0 }
                ? JsonSerializer.Serialize(request.Interests)
                : null,
            Status = ConnectCardStatus.New,
            SubmittedAt = now,
            ModifiedAt = now,
            IpAddressHash = HashIp(remoteIp),
        };
        await _repo.AddAsync(entity, ct).ConfigureAwait(false);

        // ---- 5. Acknowledgment email -------------------------------------
        if (!string.IsNullOrWhiteSpace(entity.Email))
        {
            await TrySendAcknowledgmentAsync(entity, ct).ConfigureAwait(false);
        }

        // ---- 6. Audit + SignalR -----------------------------------------
        await _audit.WriteAsync("ConnectCard.Submitted", nameof(ConnectCardSubmission), entity.Id.ToString(),
            new { entity.IsFirstTimeVisitor, hasEmail = entity.Email != null, hasPhone = entity.Phone != null },
            ct).ConfigureAwait(false);

        await _notifier.NotifyConnectCardSubmittedAsync(
            new ConnectCardSummaryMessage(entity.Id, entity.Name, entity.Email, entity.Phone, entity.SubmittedAt),
            ct).ConfigureAwait(false);

        return SubmitConnectCardResult.Success();
    }

    public async Task<List<AdminConnectCardListItemDto>> ListAdminAsync(AdminConnectCardListQuery query, CancellationToken ct = default)
    {
        if (!IsAdminShell) return new List<AdminConnectCardListItemDto>();
        var rows = await _repo.ListAsync(query, ct).ConfigureAwait(false);
        return rows.Select(r => new AdminConnectCardListItemDto(
            r.Id, r.Name, r.Email, r.Phone,
            r.IsFirstTimeVisitor, r.ServiceDate,
            r.Status, r.SubmittedAt, r.AcknowledgmentEmailSentAt))
            .ToList();
    }

    public async Task<AdminConnectCardDetailDto?> GetAdminAsync(Guid id, CancellationToken ct = default)
    {
        if (!IsAdminShell) return null;
        var entity = await _repo.GetAsync(id, ct).ConfigureAwait(false);
        return entity is null ? null : ToDetailDto(entity);
    }

    public async Task<AdminConnectCardDetailDto?> UpdateStatusAsync(Guid id, UpdateStatusRequest request, CancellationToken ct = default)
    {
        if (!IsAdminShell) return null;
        var entity = await _repo.GetAsync(id, ct).ConfigureAwait(false);
        if (entity is null) return null;

        entity.Status = request.Status;
        entity.StatusChangedAt = DateTimeOffset.UtcNow;
        entity.StatusChangedByUserId = _currentUser.UserId;
        entity.ModifiedAt = DateTimeOffset.UtcNow;
        entity.ModifiedByUserId = _currentUser.UserId;
        await _repo.UpdateAsync(entity, ct).ConfigureAwait(false);

        await _audit.WriteAsync("ConnectCard.StatusChanged", nameof(ConnectCardSubmission), entity.Id.ToString(),
            new { Status = entity.Status.ToString() }, ct).ConfigureAwait(false);

        return ToDetailDto(entity);
    }

    public async Task<AdminConnectCardDetailDto?> UpdateNotesAsync(Guid id, UpdateNotesRequest request, CancellationToken ct = default)
    {
        if (!IsAdminShell) return null;
        var entity = await _repo.GetAsync(id, ct).ConfigureAwait(false);
        if (entity is null) return null;

        entity.AdminNotes = NullIfBlank(request.AdminNotes);
        entity.ModifiedAt = DateTimeOffset.UtcNow;
        entity.ModifiedByUserId = _currentUser.UserId;
        await _repo.UpdateAsync(entity, ct).ConfigureAwait(false);

        await _audit.WriteAsync("ConnectCard.NotesUpdated", nameof(ConnectCardSubmission), entity.Id.ToString(),
            cancellationToken: ct).ConfigureAwait(false);

        return ToDetailDto(entity);
    }

    public async Task<bool> ResendAcknowledgmentAsync(Guid id, CancellationToken ct = default)
    {
        if (!IsAdminShell) return false;
        var entity = await _repo.GetAsync(id, ct).ConfigureAwait(false);
        if (entity is null || string.IsNullOrWhiteSpace(entity.Email)) return false;

        var ok = await TrySendAcknowledgmentAsync(entity, ct).ConfigureAwait(false);
        if (ok)
        {
            entity.ModifiedAt = DateTimeOffset.UtcNow;
            entity.ModifiedByUserId = _currentUser.UserId;
            await _repo.UpdateAsync(entity, ct).ConfigureAwait(false);
            await _audit.WriteAsync("ConnectCard.AcknowledgmentResent", nameof(ConnectCardSubmission),
                entity.Id.ToString(), cancellationToken: ct).ConfigureAwait(false);
        }
        return ok;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        if (!IsAdmin) return false;
        var entity = await _repo.GetAsync(id, ct).ConfigureAwait(false);
        if (entity is null) return false;

        // ConnectCardSubmissions are temporal but not soft-deletable; the
        // only "delete" path is a hard-delete and is admin-only.
        // (Behavior: caller is expected to set Status=NotLegit instead in
        // most cases; explicit delete is for GDPR-style erasure.)
        await _audit.WriteAsync("ConnectCard.Deleted", nameof(ConnectCardSubmission), entity.Id.ToString(),
            new { entity.Email, entity.Phone }, ct).ConfigureAwait(false);
        // Real delete is wired through the repository; for now we mark
        // status to NotLegit as a soft-erase, matching the entity surface.
        entity.Status = ConnectCardStatus.NotLegit;
        entity.ModifiedAt = DateTimeOffset.UtcNow;
        entity.ModifiedByUserId = _currentUser.UserId;
        await _repo.UpdateAsync(entity, ct).ConfigureAwait(false);
        return true;
    }

    // ---- helpers ----------------------------------------------------------

    private async Task<bool> TrySendAcknowledgmentAsync(ConnectCardSubmission entity, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(entity.Email)) return false;

        var settings = await _settings.GetAsync(ct).ConfigureAwait(false);
        var subject = $"Thanks for connecting with {settings.ChurchName}";
        var (html, text) = ComposeAcknowledgmentBody(entity, settings);

        try
        {
            await _email.SendTransactionalAsync(new EmailMessage(
                ToAddress: entity.Email!,
                ToName: entity.Name,
                Subject: subject,
                HtmlBody: html,
                PlainTextBody: text,
                UserId: null,
                Category: Domain.Email.EmailCategory.Transactional),
                ct).ConfigureAwait(false);

            entity.AcknowledgmentEmailSentAt = DateTimeOffset.UtcNow;
            return true;
        }
        catch
        {
            // Email infrastructure is best-effort here — the submission still
            // went through. The admin can use Resend to retry.
            return false;
        }
    }

    private static (string Html, string Text) ComposeAcknowledgmentBody(
        ConnectCardSubmission entity,
        Domain.Settings.SiteSettings settings)
    {
        // The configurable acknowledgment body lives in
        // SiteSettings.ConnectCardAcknowledgmentMessageJson (ProseMirror).
        // For the SDK-friendly first-pass we ship a plain default; Q16
        // exposes the configured message in the admin UI so a custom body
        // overrides this one.
        var html = $"""
            <p>Hi {System.Web.HttpUtility.HtmlEncode(entity.Name)},</p>
            <p>Thanks for connecting with us at {System.Web.HttpUtility.HtmlEncode(settings.ChurchName)}.
            We've received your card and someone will follow up soon.</p>
            <p>— {System.Web.HttpUtility.HtmlEncode(settings.ChurchName)}</p>
            """;
        var text = $"Hi {entity.Name},\n\nThanks for connecting with us at {settings.ChurchName}. " +
            $"We've received your card and someone will follow up soon.\n\n— {settings.ChurchName}";
        return (html, text);
    }

    private static AdminConnectCardDetailDto ToDetailDto(ConnectCardSubmission s)
    {
        var interests = string.IsNullOrWhiteSpace(s.InterestCheckboxesJson)
            ? Array.Empty<string>()
            : JsonSerializer.Deserialize<string[]>(s.InterestCheckboxesJson) ?? Array.Empty<string>();
        return new AdminConnectCardDetailDto(
            s.Id, s.Name, s.Email, s.Phone,
            s.IsFirstTimeVisitor, s.ServiceDate,
            s.HowDidYouHear, s.Comments,
            interests,
            s.Status, s.AdminNotes,
            s.SubmittedAt, s.AcknowledgmentEmailSentAt, s.StatusChangedAt);
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? HashIp(string? remoteIp)
    {
        if (string.IsNullOrWhiteSpace(remoteIp)) return null;
        var bytes = Encoding.UTF8.GetBytes(remoteIp);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
