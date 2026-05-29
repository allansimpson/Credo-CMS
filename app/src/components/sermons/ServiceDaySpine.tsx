import type { ServiceDay } from "@/lib/api/publicSermons";

export function ServiceDaySpine({ day }: { day: ServiceDay }) {
  const d = new Date(day.date + "T12:00:00");
  const dayOfWeek = d.toLocaleDateString("en-US", { weekday: "long" }).toUpperCase();
  const dayNum = d.getDate();
  const monthYear = d.toLocaleDateString("en-US", { month: "short", year: "numeric" });
  const count = day.sermons.length;
  const isWednesday = day.kind === "wednesday";

  return (
    <div className="hidden shrink-0 border-r border-border-soft pr-7 md:block" style={{ width: 180 }}>
      <p className="font-mono text-[11px] tracking-[0.18em]">
        {isWednesday ? (
          <span className="uppercase text-accent">
            {dayOfWeek} <span className="opacity-70">· Midweek</span>
          </span>
        ) : (
          <span className="uppercase text-muted">{dayOfWeek}</span>
        )}
      </p>
      <p
        className="font-heading text-accent"
        style={{ fontSize: 110, fontWeight: 600, letterSpacing: "-0.04em", lineHeight: 0.9 }}
      >
        {String(dayNum).padStart(2, "0")}
      </p>
      <p className="font-heading text-[22px] font-semibold">{monthYear}</p>
      <p className="mt-4 font-mono text-[11px] uppercase tracking-[0.12em] text-muted">
        {count} {count === 1 ? "service" : "services"}
      </p>
    </div>
  );
}

export function ServiceDaySpineMobile({ day }: { day: ServiceDay }) {
  const d = new Date(day.date + "T12:00:00");
  const dayOfWeek = d.toLocaleDateString("en-US", { weekday: "short" }).toUpperCase();
  const dayNum = d.getDate();
  const monthYear = d.toLocaleDateString("en-US", { month: "short", year: "numeric" });
  const isWednesday = day.kind === "wednesday";

  return (
    <div className="flex items-baseline gap-3 md:hidden">
      <span
        className="font-heading text-4xl font-semibold text-accent"
        style={{ letterSpacing: "-0.04em", lineHeight: 1 }}
      >
        {String(dayNum).padStart(2, "0")}
      </span>
      <span className="font-mono text-[11px] uppercase tracking-[0.14em] text-muted">
        {isWednesday ? (
          <span className="text-accent">{dayOfWeek} · Midweek</span>
        ) : (
          dayOfWeek
        )}
        {" · "}{monthYear}
      </span>
    </div>
  );
}
