import type { ServiceDay } from "@/lib/api/publicSermons";
import { getHeroServiceType } from "./serviceTypeLabels";
import { ServiceDaySpine, ServiceDaySpineMobile } from "./ServiceDaySpine";
import { ServiceHeroCard } from "./ServiceHeroCard";
import { ServiceSiblingRow } from "./ServiceSiblingRow";

export function ServiceDayBlock({ day }: { day: ServiceDay }) {
  if (day.sermons.length === 0) return null;

  const heroType = getHeroServiceType(day.sermons);
  const hero = day.sermons.find((s) => s.serviceType === heroType) ?? day.sermons[0];
  const siblings = day.sermons.filter((s) => s.id !== hero.id);

  return (
    <div className="py-11" style={{ paddingTop: 44, paddingBottom: 44 }}>
      {/* Mobile spine */}
      <ServiceDaySpineMobile day={day} />

      <div className="mt-4 grid gap-10 md:mt-0" style={{ gridTemplateColumns: "180px 1fr" }}>
        {/* Desktop spine */}
        <ServiceDaySpine day={day} />

        {/* Content column */}
        <div className="col-start-2 hidden md:block">
          <ServiceHeroCard sermon={hero} />

          {siblings.length > 0 && (
            <div className="mt-6">
              <div className="flex items-center gap-3">
                <span className="font-mono text-[11px] uppercase tracking-[0.18em] text-muted">
                  Also this {day.kind === "wednesday" ? "Wednesday" : "Sunday"}
                </span>
                <span className="h-px flex-1 bg-border-soft" />
              </div>
              <div className="mt-2 divide-y divide-border-soft">
                {siblings.map((s) => (
                  <ServiceSiblingRow key={s.id} sermon={s} />
                ))}
              </div>
            </div>
          )}
        </div>

        {/* Mobile: full-width content below the mobile spine */}
        <div className="col-span-full md:hidden">
          <ServiceHeroCard sermon={hero} />

          {siblings.length > 0 && (
            <div className="mt-6">
              <div className="flex items-center gap-3">
                <span className="font-mono text-[11px] uppercase tracking-[0.18em] text-muted">
                  Also this {day.kind === "wednesday" ? "Wednesday" : "Sunday"}
                </span>
                <span className="h-px flex-1 bg-border-soft" />
              </div>
              <div className="mt-2 space-y-3">
                {siblings.map((s) => (
                  <ServiceSiblingRow key={s.id} sermon={s} />
                ))}
              </div>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}
