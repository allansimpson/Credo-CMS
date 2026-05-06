using CredoCms.Domain.ConnectCard;

namespace CredoCms.Application.ConnectCard;

/// <summary>
/// Public submission shape. Anti-bot fields (<see cref="HoneypotValue"/>,
/// <see cref="ClientLoadedAt"/>, <see cref="TurnstileToken"/>) are inspected
/// at the service layer and stripped before persistence.
/// </summary>
public sealed record SubmitConnectCardRequest(
    string Name,
    string? Email,
    string? Phone,
    bool IsFirstTimeVisitor,
    DateOnly? ServiceDate,
    string HowDidYouHear,
    string? Comments,
    IReadOnlyList<string>? Interests,
    /// <summary>Hidden field that should always be empty for human submitters.</summary>
    string? HoneypotValue,
    /// <summary>Client-reported load timestamp (UTC ISO-8601). The service
    /// rejects submissions younger than 5 seconds since load.</summary>
    DateTimeOffset? ClientLoadedAt,
    string? TurnstileToken);

public sealed record SubmitConnectCardResult(bool Succeeded, IReadOnlyList<string> Errors)
{
    public static SubmitConnectCardResult Success() => new(true, Array.Empty<string>());
    public static SubmitConnectCardResult Failure(params string[] errors) => new(false, errors);
}

public sealed record AdminConnectCardListItemDto(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    bool IsFirstTimeVisitor,
    DateOnly? ServiceDate,
    ConnectCardStatus Status,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? AcknowledgmentEmailSentAt);

public sealed record AdminConnectCardDetailDto(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    bool IsFirstTimeVisitor,
    DateOnly? ServiceDate,
    string HowDidYouHear,
    string? Comments,
    IReadOnlyList<string> Interests,
    ConnectCardStatus Status,
    string? AdminNotes,
    DateTimeOffset SubmittedAt,
    DateTimeOffset? AcknowledgmentEmailSentAt,
    DateTimeOffset? StatusChangedAt);

public sealed record UpdateStatusRequest(ConnectCardStatus Status);

public sealed record UpdateNotesRequest(string? AdminNotes);

public sealed record AdminConnectCardListQuery(
    ConnectCardStatus? Status = null,
    bool? IsFirstTimeVisitor = null,
    string? Search = null);

public sealed record ConnectCardSummary(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    DateTimeOffset SubmittedAt);
