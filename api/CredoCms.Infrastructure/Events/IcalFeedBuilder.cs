using System.Text;
using CredoCms.Application.Events;
using CredoCms.Domain.Events;
using Ical.Net.CalendarComponents;
using Ical.Net.DataTypes;
using Ical.Net.Serialization;
using IcalCalendar = Ical.Net.Calendar;

namespace CredoCms.Infrastructure.Events;

public sealed class IcalFeedBuilder : IIcalFeedBuilder
{
    public string BuildSingleEventIcs(
        Event evt,
        IReadOnlyList<EventRecurrenceException> exceptions,
        IReadOnlyList<EventOccurrenceOverride> overrides)
    {
        var cal = new IcalCalendar();
        AddEvent(cal, evt, exceptions, overrides);
        return Serialize(cal);
    }

    public string BuildFeedIcs(
        IEnumerable<(Event Event, IReadOnlyList<EventRecurrenceException> Exceptions, IReadOnlyList<EventOccurrenceOverride> Overrides)> events,
        string calendarName)
    {
        var cal = new IcalCalendar();
        cal.AddProperty("X-WR-CALNAME", calendarName);
        cal.AddProperty("METHOD", "PUBLISH");
        foreach (var (e, ex, ov) in events) AddEvent(cal, e, ex, ov);
        return Serialize(cal);
    }

    private static void AddEvent(
        IcalCalendar cal,
        Event evt,
        IReadOnlyList<EventRecurrenceException> exceptions,
        IReadOnlyList<EventOccurrenceOverride> overrides)
    {
        var ev = new CalendarEvent
        {
            Uid = $"event-{evt.Id:N}@credo-cms",
            Summary = evt.Title,
            Location = evt.Location,
            Description = StripHtml(evt.DescriptionJson),
            Start = ToCalDateTime(evt.StartsAt, evt.AllDay),
            End = evt.EndsAt is { } endsAt ? ToCalDateTime(endsAt, evt.AllDay) : null,
            DtStamp = ToCalDateTime(evt.ModifiedAt, allDay: false),
            LastModified = ToCalDateTime(evt.ModifiedAt, allDay: false),
        };

        if (!string.IsNullOrWhiteSpace(evt.RecurrenceRule))
        {
            // Pass through the RFC 5545 RRULE the editor stored. Ical.Net
            // accepts a string and parses it.
            ev.RecurrenceRules.Add(new RecurrencePattern(evt.RecurrenceRule));

            if (evt.RecurrenceEndDate is { } until)
            {
                ev.RecurrenceRules[^1].Until = until.UtcDateTime;
            }
            if (evt.RecurrenceCount is { } count)
            {
                ev.RecurrenceRules[^1].Count = count;
            }

            // EXDATE — skipped occurrences.
            if (exceptions.Count > 0)
            {
                var exDates = new PeriodList();
                foreach (var ex in exceptions)
                {
                    var d = ex.OccurrenceDate.ToDateTime(TimeOnly.FromDateTime(evt.StartsAt.UtcDateTime),
                        DateTimeKind.Utc);
                    exDates.Add(new CalDateTime(d) { HasTime = !evt.AllDay });
                }
                ev.ExceptionDates.Add(exDates);
            }
        }

        cal.Events.Add(ev);

        // Per-occurrence overrides — emit a separate VEVENT with the same UID
        // and a RECURRENCE-ID matching the original date.
        foreach (var ov in overrides)
        {
            if (ov.IsCanceled) continue; // EXDATE handles cancellations
            var origStart = ov.OriginalOccurrenceDate.ToDateTime(
                TimeOnly.FromDateTime(evt.StartsAt.UtcDateTime), DateTimeKind.Utc);
            var overrideEvent = new CalendarEvent
            {
                Uid = ev.Uid,
                Summary = evt.Title,
                Location = ov.OverrideLocation ?? evt.Location,
                Description = StripHtml(ov.OverrideDescriptionJson ?? evt.DescriptionJson),
                Start = ToCalDateTime(ov.OverrideStartsAt ?? evt.StartsAt, evt.AllDay),
                End = (ov.OverrideEndsAt ?? evt.EndsAt) is { } e
                    ? ToCalDateTime(e, evt.AllDay)
                    : null,
                RecurrenceId = new CalDateTime(origStart) { HasTime = !evt.AllDay },
                DtStamp = ToCalDateTime(evt.ModifiedAt, allDay: false),
            };
            cal.Events.Add(overrideEvent);
        }
    }

    private static CalDateTime ToCalDateTime(DateTimeOffset value, bool allDay)
    {
        if (allDay)
        {
            return new CalDateTime(value.UtcDateTime.Date) { HasTime = false };
        }
        return new CalDateTime(value.UtcDateTime, "UTC") { HasTime = true };
    }

    private static string Serialize(IcalCalendar cal)
    {
        var serializer = new CalendarSerializer();
        return serializer.SerializeToString(cal) ?? string.Empty;
    }

    /// <summary>
    /// Best-effort plaintext extraction from a TipTap JSON string. iCal
    /// description should be plain — keep this simple.
    /// </summary>
    internal static string? StripHtml(string? tipTapJson)
    {
        if (string.IsNullOrWhiteSpace(tipTapJson)) return null;
        var sb = new StringBuilder();
        ExtractText(System.Text.Json.JsonDocument.Parse(tipTapJson).RootElement, sb);
        var s = sb.ToString().Trim();
        return s.Length == 0 ? null : s;
    }

    private static void ExtractText(System.Text.Json.JsonElement el, StringBuilder sb)
    {
        if (el.ValueKind == System.Text.Json.JsonValueKind.Object)
        {
            if (el.TryGetProperty("text", out var t) && t.ValueKind == System.Text.Json.JsonValueKind.String)
                sb.Append(t.GetString());
            if (el.TryGetProperty("content", out var inner)) ExtractText(inner, sb);
            if (el.TryGetProperty("type", out var typ) && typ.ValueKind == System.Text.Json.JsonValueKind.String
                && typ.GetString() == "paragraph") sb.AppendLine();
        }
        else if (el.ValueKind == System.Text.Json.JsonValueKind.Array)
        {
            foreach (var child in el.EnumerateArray()) ExtractText(child, sb);
        }
    }
}
