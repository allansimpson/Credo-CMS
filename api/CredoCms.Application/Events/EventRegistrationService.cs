using System.Globalization;
using System.Text;
using System.Text.Json;
using CredoCms.Application.Common;
using CredoCms.Domain.Events;

namespace CredoCms.Application.Events;

public sealed record RegistrationFieldDto(
    Guid Id, int DisplayOrder, string Label, EventRegistrationFieldType FieldType,
    bool Required, string? HelpText, IReadOnlyList<string>? Options,
    int? TextMaxLength, decimal? NumberMin, decimal? NumberMax);

public sealed record CreateRegistrationFieldRequest(
    string Label, EventRegistrationFieldType FieldType, bool Required,
    string? HelpText, IReadOnlyList<string>? Options,
    int? TextMaxLength, decimal? NumberMin, decimal? NumberMax,
    int DisplayOrder);

public sealed record RegistrationDto(
    Guid Id, Guid EventId, DateOnly? OccurrenceDate,
    string SubmitterName, string SubmitterEmail, string? SubmitterPhone,
    EventRegistrationStatus Status,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? CanceledAt, string? CancelReason,
    IReadOnlyDictionary<string, object?> FieldValues);

public sealed record MyRegistrationDto(
    Guid Id, Guid EventId, string EventSlug, string EventTitle,
    DateTimeOffset EventStartsAt,
    DateOnly? OccurrenceDate,
    EventRegistrationStatus Status,
    DateTimeOffset SubmittedAt);

public sealed record SubmitRegistrationRequest(
    DateOnly? OccurrenceDate,
    string SubmitterName, string SubmitterEmail, string? SubmitterPhone,
    IReadOnlyDictionary<string, object?> FieldValues,
    /// <summary>Honeypot field — must be empty/null on real submissions.</summary>
    string? Hp,
    /// <summary>Milliseconds elapsed between form open and submit. Must be ≥ 5000.</summary>
    long FormOpenedElapsedMs);

public sealed record SubmitRegistrationResult(
    bool Succeeded, string[] Errors, RegistrationDto? Registration, string? CancelToken);

public interface IEventRegistrationRepository
{
    Task<List<EventRegistrationField>> ListFieldsAsync(Guid eventId, CancellationToken ct = default);
    Task<EventRegistrationField?> GetFieldAsync(Guid id, CancellationToken ct = default);
    Task AddFieldAsync(EventRegistrationField field, CancellationToken ct = default);
    Task UpdateFieldAsync(EventRegistrationField field, CancellationToken ct = default);
    Task<bool> RemoveFieldAsync(Guid id, CancellationToken ct = default);

    Task<EventRegistration?> GetAsync(Guid id, CancellationToken ct = default);
    Task<List<EventRegistration>> ListForEventAsync(Guid eventId, EventRegistrationStatus? status, CancellationToken ct = default);
    Task<List<EventRegistration>> ListForUserAsync(Guid userId, CancellationToken ct = default);
    Task<int> CountConfirmedAsync(Guid eventId, DateOnly? occurrenceDate, CancellationToken ct = default);
    Task<EventRegistration?> NextWaitlistedAsync(Guid eventId, DateOnly? occurrenceDate, CancellationToken ct = default);
    Task AddAsync(EventRegistration registration, CancellationToken ct = default);
    Task UpdateAsync(EventRegistration registration, CancellationToken ct = default);
}

public interface IEventRegistrationService
{
    Task<List<RegistrationFieldDto>> ListFieldsAsync(Guid eventId, CancellationToken ct = default);
    Task<RegistrationFieldDto?> AddFieldAsync(Guid eventId, CreateRegistrationFieldRequest req, CancellationToken ct = default);
    Task<RegistrationFieldDto?> UpdateFieldAsync(Guid fieldId, CreateRegistrationFieldRequest req, CancellationToken ct = default);
    Task<bool> RemoveFieldAsync(Guid fieldId, CancellationToken ct = default);

    Task<SubmitRegistrationResult> SubmitAsync(Guid eventId, SubmitRegistrationRequest req, Guid? userId, CancellationToken ct = default);
    Task<bool> CancelAsync(Guid registrationId, string? reason, CancellationToken ct = default);
    Task<RegistrationDto?> GetAsync(Guid id, CancellationToken ct = default);
    Task<List<RegistrationDto>> ListForEventAsync(Guid eventId, EventRegistrationStatus? status, CancellationToken ct = default);
    Task<List<RegistrationDto>> ListForUserAsync(Guid userId, CancellationToken ct = default);
    Task<List<MyRegistrationDto>> ListMyRegistrationsAsync(Guid userId, CancellationToken ct = default);
    Task<bool> CancelMyRegistrationAsync(Guid userId, Guid registrationId, string? reason, CancellationToken ct = default);
    Task<bool> ResendConfirmationAsync(Guid registrationId, CancellationToken ct = default);
    Task<string> ExportCsvAsync(Guid eventId, CancellationToken ct = default);
}

public sealed class EventRegistrationService : IEventRegistrationService
{
    private static readonly JsonSerializerOptions Json = new() { WriteIndented = false };

    private readonly IEventRegistrationRepository _repo;
    private readonly IEventRepository _events;
    private readonly IAuditLogger _audit;

    public EventRegistrationService(
        IEventRegistrationRepository repo,
        IEventRepository events,
        IAuditLogger audit)
    {
        _repo = repo;
        _events = events;
        _audit = audit;
    }

    public async Task<List<RegistrationFieldDto>> ListFieldsAsync(Guid eventId, CancellationToken ct = default)
    {
        var fields = await _repo.ListFieldsAsync(eventId, ct).ConfigureAwait(false);
        return fields.OrderBy(f => f.DisplayOrder).Select(ToDto).ToList();
    }

    public async Task<RegistrationFieldDto?> AddFieldAsync(Guid eventId, CreateRegistrationFieldRequest req, CancellationToken ct = default)
    {
        var f = new EventRegistrationField
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            DisplayOrder = req.DisplayOrder,
            Label = req.Label,
            FieldType = req.FieldType,
            Required = req.Required,
            HelpText = req.HelpText,
            OptionsJson = req.Options is null ? null : JsonSerializer.Serialize(req.Options, Json),
            TextMaxLength = req.TextMaxLength,
            NumberMin = req.NumberMin,
            NumberMax = req.NumberMax,
        };
        await _repo.AddFieldAsync(f, ct).ConfigureAwait(false);
        return ToDto(f);
    }

    public async Task<RegistrationFieldDto?> UpdateFieldAsync(Guid fieldId, CreateRegistrationFieldRequest req, CancellationToken ct = default)
    {
        var f = await _repo.GetFieldAsync(fieldId, ct).ConfigureAwait(false);
        if (f is null) return null;
        f.DisplayOrder = req.DisplayOrder;
        f.Label = req.Label;
        f.FieldType = req.FieldType;
        f.Required = req.Required;
        f.HelpText = req.HelpText;
        f.OptionsJson = req.Options is null ? null : JsonSerializer.Serialize(req.Options, Json);
        f.TextMaxLength = req.TextMaxLength;
        f.NumberMin = req.NumberMin;
        f.NumberMax = req.NumberMax;
        await _repo.UpdateFieldAsync(f, ct).ConfigureAwait(false);
        return ToDto(f);
    }

    public Task<bool> RemoveFieldAsync(Guid fieldId, CancellationToken ct = default)
        => _repo.RemoveFieldAsync(fieldId, ct);

    public async Task<SubmitRegistrationResult> SubmitAsync(Guid eventId, SubmitRegistrationRequest req, Guid? userId, CancellationToken ct = default)
    {
        // Defenses: honeypot + time-to-submit. Turnstile deferred.
        if (!string.IsNullOrEmpty(req.Hp))
            return new(false, new[] { "Submission rejected." }, null, null);
        if (req.FormOpenedElapsedMs < 5000)
            return new(false, new[] { "Form was submitted too quickly. Please try again." }, null, null);

        var evt = await _events.GetByIdAsync(eventId, ct: ct).ConfigureAwait(false);
        if (evt is null || !evt.IsPublished)
            return new(false, new[] { "Event not found." }, null, null);

        if (evt.RegistrationMode == EventRegistrationMode.None)
            return new(false, new[] { "This event does not accept registrations." }, null, null);

        var now = DateTimeOffset.UtcNow;
        if (evt.RegistrationOpensAt is { } opens && opens > now)
            return new(false, new[] { "Registration has not opened yet." }, null, null);
        if (evt.RegistrationClosesAt is { } closes && closes <= now)
            return new(false, new[] { "Registration is closed." }, null, null);

        // Standard-field validation.
        if (string.IsNullOrWhiteSpace(req.SubmitterName))
            return new(false, new[] { "Name is required." }, null, null);
        if (!IsValidEmail(req.SubmitterEmail))
            return new(false, new[] { "A valid email is required." }, null, null);

        // Dynamic-field validation.
        var fields = await _repo.ListFieldsAsync(eventId, ct).ConfigureAwait(false);
        var validation = ValidateFieldValues(fields, req.FieldValues);
        if (validation.Length > 0) return new(false, validation, null, null);

        // Capacity / waitlist.
        var status = EventRegistrationStatus.Confirmed;
        if (evt.Capacity is { } cap)
        {
            var confirmedCount = await _repo.CountConfirmedAsync(eventId, req.OccurrenceDate, ct).ConfigureAwait(false);
            if (confirmedCount >= cap)
            {
                if (!evt.WaitlistEnabled)
                    return new(false, new[] { "This event is full." }, null, null);
                status = EventRegistrationStatus.Waitlisted;
            }
        }

        var registration = new EventRegistration
        {
            Id = Guid.NewGuid(),
            EventId = eventId,
            OccurrenceDate = req.OccurrenceDate,
            UserId = userId,
            SubmitterName = req.SubmitterName.Trim(),
            SubmitterEmail = req.SubmitterEmail.Trim(),
            SubmitterPhone = req.SubmitterPhone?.Trim(),
            FieldValuesJson = SerializeFieldValues(req.FieldValues),
            Status = status,
            SubmittedAt = now,
        };
        await _repo.AddAsync(registration, ct).ConfigureAwait(false);

        await _audit.WriteAsync("EventRegistration.Submitted",
            nameof(EventRegistration), registration.Id.ToString(),
            details: new { eventId, status = status.ToString(), email = req.SubmitterEmail },
            cancellationToken: ct).ConfigureAwait(false);

        return new(true, Array.Empty<string>(), ToDto(registration), null);
    }

    public async Task<bool> CancelAsync(Guid registrationId, string? reason, CancellationToken ct = default)
    {
        var reg = await _repo.GetAsync(registrationId, ct).ConfigureAwait(false);
        if (reg is null) return false;
        if (reg.Status == EventRegistrationStatus.Canceled) return true;

        var wasConfirmed = reg.Status == EventRegistrationStatus.Confirmed;
        reg.Status = EventRegistrationStatus.Canceled;
        reg.CanceledAt = DateTimeOffset.UtcNow;
        reg.CancelReason = reason;
        await _repo.UpdateAsync(reg, ct).ConfigureAwait(false);

        await _audit.WriteAsync("EventRegistration.Canceled",
            nameof(EventRegistration), registrationId.ToString(),
            details: new { reg.EventId, reason }, cancellationToken: ct).ConfigureAwait(false);

        // Promote oldest waitlisted entry if a confirmed seat just opened.
        if (wasConfirmed)
        {
            var next = await _repo.NextWaitlistedAsync(reg.EventId, reg.OccurrenceDate, ct).ConfigureAwait(false);
            if (next is not null)
            {
                next.Status = EventRegistrationStatus.Confirmed;
                await _repo.UpdateAsync(next, ct).ConfigureAwait(false);
                await _audit.WriteAsync("EventRegistration.PromotedFromWaitlist",
                    nameof(EventRegistration), next.Id.ToString(),
                    details: new { next.EventId }, cancellationToken: ct).ConfigureAwait(false);
            }
        }
        return true;
    }

    public async Task<RegistrationDto?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var r = await _repo.GetAsync(id, ct).ConfigureAwait(false);
        return r is null ? null : ToDto(r);
    }

    public async Task<List<RegistrationDto>> ListForEventAsync(Guid eventId, EventRegistrationStatus? status, CancellationToken ct = default)
    {
        var rows = await _repo.ListForEventAsync(eventId, status, ct).ConfigureAwait(false);
        return rows.Select(ToDto).ToList();
    }

    public async Task<List<RegistrationDto>> ListForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var rows = await _repo.ListForUserAsync(userId, ct).ConfigureAwait(false);
        return rows.Select(ToDto).ToList();
    }

    public async Task<List<MyRegistrationDto>> ListMyRegistrationsAsync(Guid userId, CancellationToken ct = default)
    {
        var rows = await _repo.ListForUserAsync(userId, ct).ConfigureAwait(false);
        var result = new List<MyRegistrationDto>(rows.Count);
        foreach (var r in rows)
        {
            var evt = await _events.GetByIdAsync(r.EventId, ct: ct).ConfigureAwait(false);
            if (evt is null) continue;
            result.Add(new MyRegistrationDto(
                r.Id, r.EventId, evt.Slug, evt.Title, evt.StartsAt,
                r.OccurrenceDate, r.Status, r.SubmittedAt));
        }
        return result.OrderBy(m => m.EventStartsAt).ToList();
    }

    public async Task<bool> CancelMyRegistrationAsync(Guid userId, Guid registrationId, string? reason, CancellationToken ct = default)
    {
        var reg = await _repo.GetAsync(registrationId, ct).ConfigureAwait(false);
        if (reg is null || reg.UserId != userId) return false;
        return await CancelAsync(registrationId, reason, ct).ConfigureAwait(false);
    }

    public async Task<bool> ResendConfirmationAsync(Guid registrationId, CancellationToken ct = default)
    {
        var reg = await _repo.GetAsync(registrationId, ct).ConfigureAwait(false);
        if (reg is null) return false;
        reg.ConfirmationEmailSentAt = DateTimeOffset.UtcNow;
        await _repo.UpdateAsync(reg, ct).ConfigureAwait(false);
        await _audit.WriteAsync("EventRegistration.ConfirmationResent",
            nameof(EventRegistration), registrationId.ToString(),
            cancellationToken: ct).ConfigureAwait(false);
        return true;
    }

    public async Task<string> ExportCsvAsync(Guid eventId, CancellationToken ct = default)
    {
        var fields = await _repo.ListFieldsAsync(eventId, ct).ConfigureAwait(false);
        var rows = await _repo.ListForEventAsync(eventId, status: null, ct).ConfigureAwait(false);

        var sb = new StringBuilder();
        var headers = new List<string> { "Id", "OccurrenceDate", "Status", "SubmittedAt", "Name", "Email", "Phone" };
        headers.AddRange(fields.OrderBy(f => f.DisplayOrder).Select(f => f.Label));
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));

        foreach (var r in rows)
        {
            var values = new List<string>
            {
                r.Id.ToString(),
                r.OccurrenceDate?.ToString("yyyy-MM-dd") ?? "",
                r.Status.ToString(),
                r.SubmittedAt.ToString("o", CultureInfo.InvariantCulture),
                r.SubmitterName,
                r.SubmitterEmail,
                r.SubmitterPhone ?? "",
            };
            var fieldValues = ParseFieldValues(r.FieldValuesJson);
            foreach (var f in fields.OrderBy(x => x.DisplayOrder))
            {
                fieldValues.TryGetValue(f.Id.ToString(), out var v);
                values.Add(v?.ToString() ?? "");
            }
            sb.AppendLine(string.Join(",", values.Select(EscapeCsv)));
        }
        return sb.ToString();
    }

    private static string SerializeFieldValues(IReadOnlyDictionary<string, object?> values)
        => JsonSerializer.Serialize(values, Json);

    private static Dictionary<string, object?> ParseFieldValues(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, object?>();
        try
        {
            var dict = JsonSerializer.Deserialize<Dictionary<string, object?>>(json);
            return dict ?? new Dictionary<string, object?>();
        }
        catch { return new Dictionary<string, object?>(); }
    }

    private static string[] ValidateFieldValues(
        IReadOnlyList<EventRegistrationField> fields,
        IReadOnlyDictionary<string, object?> values)
    {
        var errors = new List<string>();
        foreach (var f in fields)
        {
            values.TryGetValue(f.Id.ToString(), out var v);
            var hasValue = v switch
            {
                null => false,
                string s => !string.IsNullOrWhiteSpace(s),
                JsonElement el => el.ValueKind != JsonValueKind.Null
                    && (el.ValueKind != JsonValueKind.String || !string.IsNullOrWhiteSpace(el.GetString())),
                _ => true,
            };
            if (f.Required && !hasValue)
            {
                errors.Add($"{f.Label} is required.");
                continue;
            }
            if (!hasValue) continue;

            switch (f.FieldType)
            {
                case EventRegistrationFieldType.ShortText:
                case EventRegistrationFieldType.LongText:
                    if (f.TextMaxLength is { } cap && v?.ToString() is { } s && s.Length > cap)
                        errors.Add($"{f.Label} exceeds {cap} characters.");
                    break;
                case EventRegistrationFieldType.Email:
                    if (v?.ToString() is { } e && !IsValidEmail(e))
                        errors.Add($"{f.Label} must be a valid email.");
                    break;
                case EventRegistrationFieldType.Number:
                    if (decimal.TryParse(v?.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var n))
                    {
                        if (f.NumberMin is { } min && n < min) errors.Add($"{f.Label} must be ≥ {min}.");
                        if (f.NumberMax is { } max && n > max) errors.Add($"{f.Label} must be ≤ {max}.");
                    }
                    else
                    {
                        errors.Add($"{f.Label} must be a number.");
                    }
                    break;
            }
        }
        return errors.ToArray();
    }

    private static bool IsValidEmail(string s)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(s);
            return addr.Address == s;
        }
        catch { return false; }
    }

    private static readonly System.Buffers.SearchValues<char> CsvSpecialChars =
        System.Buffers.SearchValues.Create(",\"\n\r");

    private static string EscapeCsv(string s)
    {
        if (s.AsSpan().IndexOfAny(CsvSpecialChars) < 0) return s;
        return "\"" + s.Replace("\"", "\"\"") + "\"";
    }

    internal static RegistrationFieldDto ToDto(EventRegistrationField f)
    {
        IReadOnlyList<string>? options = null;
        if (!string.IsNullOrWhiteSpace(f.OptionsJson))
        {
            try { options = JsonSerializer.Deserialize<List<string>>(f.OptionsJson); }
            catch { options = null; }
        }
        return new RegistrationFieldDto(
            f.Id, f.DisplayOrder, f.Label, f.FieldType, f.Required, f.HelpText,
            options, f.TextMaxLength, f.NumberMin, f.NumberMax);
    }

    internal static RegistrationDto ToDto(EventRegistration r)
        => new(
            r.Id, r.EventId, r.OccurrenceDate,
            r.SubmitterName, r.SubmitterEmail, r.SubmitterPhone,
            r.Status, r.SubmittedAt, r.CanceledAt, r.CancelReason,
            ParseFieldValues(r.FieldValuesJson));
}
