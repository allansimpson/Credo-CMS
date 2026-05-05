/**
 * The four prompt-specified recurrence patterns. The recurrence builder UI
 * emits an RRULE string from these; the server-side EventOccurrenceExpander
 * parses the same patterns for occurrence expansion.
 */
export type RecurrencePattern = "none" | "daily" | "weekly" | "monthly";

export type Weekday = "MO" | "TU" | "WE" | "TH" | "FR" | "SA" | "SU";

export interface RecurrenceState {
  pattern: RecurrencePattern;
  weekday: Weekday | null;       // for weekly
  monthDay: number | null;        // for monthly
}

export const WEEKDAYS: { value: Weekday; label: string }[] = [
  { value: "SU", label: "Sunday" }, { value: "MO", label: "Monday" },
  { value: "TU", label: "Tuesday" }, { value: "WE", label: "Wednesday" },
  { value: "TH", label: "Thursday" }, { value: "FR", label: "Friday" },
  { value: "SA", label: "Saturday" },
];

export function buildRRule(state: RecurrenceState): string | null {
  switch (state.pattern) {
    case "none": return null;
    case "daily": return "FREQ=DAILY";
    case "weekly":
      return state.weekday ? `FREQ=WEEKLY;BYDAY=${state.weekday}` : "FREQ=WEEKLY";
    case "monthly":
      return state.monthDay ? `FREQ=MONTHLY;BYMONTHDAY=${state.monthDay}` : "FREQ=MONTHLY";
  }
}

/** Inverse of buildRRule for editing existing rules. */
export function parseRRule(rule: string | null): RecurrenceState {
  if (!rule) return { pattern: "none", weekday: null, monthDay: null };
  const params = Object.fromEntries(
    rule.split(";").map((p) => p.split("=", 2)).filter((p) => p.length === 2)
      .map(([k, v]) => [k.toUpperCase(), v.toUpperCase()] as const));
  switch (params.FREQ) {
    case "DAILY": return { pattern: "daily", weekday: null, monthDay: null };
    case "WEEKLY":
      return { pattern: "weekly",
        weekday: (params.BYDAY as Weekday | undefined) ?? null,
        monthDay: null };
    case "MONTHLY":
      return { pattern: "monthly",
        weekday: null,
        monthDay: params.BYMONTHDAY ? parseInt(params.BYMONTHDAY, 10) : null };
    default:
      return { pattern: "none", weekday: null, monthDay: null };
  }
}
