namespace CredoCms.Application.Calendar;

/// <summary>
/// One item rendered on the public/admin calendar — events expanded into
/// concrete occurrences, plus News items with a CalendarDate.
/// </summary>
public sealed record CalendarItem(
    string EntityType,
    Guid EntityId,
    string Title,
    DateTimeOffset Start,
    DateTimeOffset? End,
    bool AllDay,
    string Url,
    string? Location,
    string? HeroImageUrl,
    bool MembersOnly);

public interface ICalendarQueryService
{
    Task<IReadOnlyList<CalendarItem>> ListAsync(
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEndExclusive,
        bool includeMembersOnly,
        CancellationToken ct = default);
}
