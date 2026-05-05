using CredoCms.Domain.Events;

namespace CredoCms.Application.Events;

/// <summary>
/// Builds RFC 5545 iCal text from event entities. Single-event downloads
/// emit one VEVENT (with RRULE/EXDATE/RECURRENCE-ID); calendar feeds emit
/// many.
/// </summary>
public interface IIcalFeedBuilder
{
    string BuildSingleEventIcs(
        Event evt,
        IReadOnlyList<EventRecurrenceException> exceptions,
        IReadOnlyList<EventOccurrenceOverride> overrides);

    string BuildFeedIcs(
        IEnumerable<(Event Event, IReadOnlyList<EventRecurrenceException> Exceptions, IReadOnlyList<EventOccurrenceOverride> Overrides)> events,
        string calendarName);
}
