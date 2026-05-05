using CredoCms.Domain.Events;

namespace CredoCms.Application.Events;

/// <summary>
/// One concrete occurrence of an event for a specific date. Already has
/// any per-occurrence overrides applied; canceled occurrences are not
/// emitted.
/// </summary>
public sealed record EventOccurrence(
    Guid EventId,
    string Slug,
    string Title,
    DateTimeOffset StartsAt,
    DateTimeOffset? EndsAt,
    bool AllDay,
    string? Location,
    string? DescriptionJson,
    EventVisibility? Visibility,
    bool IsOverride,
    DateOnly OriginalOccurrenceDate);

public interface IEventOccurrenceExpander
{
    /// <summary>
    /// Expands a single event into concrete occurrences within
    /// [<paramref name="rangeStart"/>, <paramref name="rangeEndExclusive"/>),
    /// applying RRULE expansion + EXDATE exceptions + per-occurrence
    /// overrides.
    /// </summary>
    IEnumerable<EventOccurrence> Expand(
        Event evt,
        IReadOnlyList<EventRecurrenceException> exceptions,
        IReadOnlyList<EventOccurrenceOverride> overrides,
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEndExclusive);
}

public sealed class EventOccurrenceExpander : IEventOccurrenceExpander
{
    public IEnumerable<EventOccurrence> Expand(
        Event evt,
        IReadOnlyList<EventRecurrenceException> exceptions,
        IReadOnlyList<EventOccurrenceOverride> overrides,
        DateTimeOffset rangeStart,
        DateTimeOffset rangeEndExclusive)
    {
        var skipDates = exceptions.Select(e => e.OccurrenceDate).ToHashSet();
        var overrideByDate = overrides.ToDictionary(o => o.OriginalOccurrenceDate);

        // Non-recurring: single occurrence test.
        if (string.IsNullOrWhiteSpace(evt.RecurrenceRule))
        {
            if (evt.StartsAt < rangeEndExclusive
                && (evt.EndsAt is null || evt.EndsAt >= rangeStart))
            {
                yield return BuildOccurrence(evt, evt.StartsAt, evt.EndsAt, overrideByDate, skipDates, materialize: false);
            }
            yield break;
        }

        foreach (var startsAt in IterateRRuleOccurrences(evt, rangeStart, rangeEndExclusive))
        {
            var occurrenceDate = DateOnly.FromDateTime(startsAt.UtcDateTime);
            if (skipDates.Contains(occurrenceDate)) continue;

            DateTimeOffset? endsAt = evt.EndsAt is null ? null
                : startsAt + (evt.EndsAt.Value - evt.StartsAt);

            yield return BuildOccurrence(evt, startsAt, endsAt, overrideByDate, skipDates, materialize: false, occurrenceDate);
        }
    }

    private static EventOccurrence BuildOccurrence(
        Event evt,
        DateTimeOffset startsAt,
        DateTimeOffset? endsAt,
        Dictionary<DateOnly, EventOccurrenceOverride> overrides,
        HashSet<DateOnly> skipDates,
        bool materialize,
        DateOnly? overrideKey = null)
    {
        var occurrenceDate = overrideKey ?? DateOnly.FromDateTime(startsAt.UtcDateTime);
        if (overrides.TryGetValue(occurrenceDate, out var ov))
        {
            if (ov.IsCanceled)
                return new EventOccurrence(evt.Id, evt.Slug, evt.Title,
                    startsAt, endsAt, evt.AllDay, evt.Location, evt.DescriptionJson, evt.Visibility,
                    IsOverride: true, OriginalOccurrenceDate: occurrenceDate)
                { /* caller filters */ };

            return new EventOccurrence(
                evt.Id, evt.Slug, evt.Title,
                ov.OverrideStartsAt ?? startsAt,
                ov.OverrideEndsAt ?? endsAt,
                evt.AllDay,
                ov.OverrideLocation ?? evt.Location,
                ov.OverrideDescriptionJson ?? evt.DescriptionJson,
                evt.Visibility,
                IsOverride: true,
                OriginalOccurrenceDate: occurrenceDate);
        }
        return new EventOccurrence(
            evt.Id, evt.Slug, evt.Title,
            startsAt, endsAt, evt.AllDay,
            evt.Location, evt.DescriptionJson, evt.Visibility,
            IsOverride: false,
            OriginalOccurrenceDate: occurrenceDate);
    }

    /// <summary>
    /// Hand-rolled expander covering the four prompt-specified RRULE
    /// patterns: FREQ=DAILY, FREQ=WEEKLY (with BYDAY), FREQ=MONTHLY (with
    /// BYMONTHDAY). End condition via UNTIL date (event.RecurrenceEndDate)
    /// or COUNT (event.RecurrenceCount).
    ///
    /// We avoid Ical.Net's full evaluator here for predictability; iCal
    /// emission still uses Ical.Net for round-trip fidelity in the feed
    /// (Stage Q14).
    /// </summary>
    private static IEnumerable<DateTimeOffset> IterateRRuleOccurrences(
        Event evt, DateTimeOffset rangeStart, DateTimeOffset rangeEndExclusive)
    {
        var rule = ParseRRule(evt.RecurrenceRule!);
        var horizon = evt.RecurrenceEndDate ?? rangeEndExclusive;
        if (horizon > rangeEndExclusive) horizon = rangeEndExclusive;

        var startsAt = evt.StartsAt;
        int emitted = 0;
        int totalCap = evt.RecurrenceCount ?? int.MaxValue;
        // Hard ceiling so a runaway rule doesn't loop forever.
        int hardCeiling = 5000;

        while (startsAt < horizon && emitted < totalCap && hardCeiling-- > 0)
        {
            if (rule.Frequency == RRuleFrequency.Weekly)
            {
                // BYDAY filter: only emit if the day-of-week is included.
                if (rule.ByDays is { Count: > 0 } && !rule.ByDays.Contains(startsAt.DayOfWeek))
                {
                    startsAt = startsAt.AddDays(1);
                    continue;
                }
            }
            else if (rule.Frequency == RRuleFrequency.Monthly && rule.ByMonthDay is { } day)
            {
                if (startsAt.Day != day)
                {
                    startsAt = startsAt.AddDays(1);
                    continue;
                }
            }

            if (startsAt >= rangeStart) yield return startsAt;
            emitted++;

            startsAt = rule.Frequency switch
            {
                RRuleFrequency.Daily => startsAt.AddDays(1),
                RRuleFrequency.Weekly => startsAt.AddDays(1),       // walk daily; BYDAY filters
                RRuleFrequency.Monthly => startsAt.AddDays(1),
                _ => startsAt.AddDays(1),
            };
        }
    }

    internal enum RRuleFrequency { Daily, Weekly, Monthly }

    internal sealed record ParsedRRule(RRuleFrequency Frequency, HashSet<DayOfWeek>? ByDays, int? ByMonthDay);

    /// <summary>Minimal RRULE parser handling the 4 patterns the recurrence builder UI emits.</summary>
    internal static ParsedRRule ParseRRule(string rrule)
    {
        var freq = RRuleFrequency.Daily;
        HashSet<DayOfWeek>? byDays = null;
        int? byMonthDay = null;

        foreach (var part in rrule.Split(';', StringSplitOptions.RemoveEmptyEntries))
        {
            var kv = part.Split('=', 2);
            if (kv.Length != 2) continue;
            switch (kv[0].Trim().ToUpperInvariant())
            {
                case "FREQ":
                    freq = kv[1].Trim().ToUpperInvariant() switch
                    {
                        "DAILY" => RRuleFrequency.Daily,
                        "WEEKLY" => RRuleFrequency.Weekly,
                        "MONTHLY" => RRuleFrequency.Monthly,
                        _ => RRuleFrequency.Daily,
                    };
                    break;
                case "BYDAY":
                    byDays = new HashSet<DayOfWeek>(
                        kv[1].Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(s => s.Trim().ToUpperInvariant())
                            .Select(MapWeekday)
                            .Where(d => d.HasValue)
                            .Select(d => d!.Value));
                    break;
                case "BYMONTHDAY":
                    if (int.TryParse(kv[1].Trim(), out var day)) byMonthDay = day;
                    break;
            }
        }
        return new ParsedRRule(freq, byDays, byMonthDay);
    }

    private static DayOfWeek? MapWeekday(string s) => s switch
    {
        "SU" => DayOfWeek.Sunday,
        "MO" => DayOfWeek.Monday,
        "TU" => DayOfWeek.Tuesday,
        "WE" => DayOfWeek.Wednesday,
        "TH" => DayOfWeek.Thursday,
        "FR" => DayOfWeek.Friday,
        "SA" => DayOfWeek.Saturday,
        _ => null,
    };
}
