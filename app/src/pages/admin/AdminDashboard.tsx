import { Link } from "react-router-dom";
import { FileText, Newspaper, Mic, Calendar } from "lucide-react";
import { useAuth } from "@/hooks/useAuth";

/**
 * Editorial dashboard. Per DESIGN_HANDOFF.md §5.1 + Claude Design clarification #6,
 * this ships with placeholder/sample data; backend integration is a separate
 * workstream. Each data block is marked with `// TODO: wire endpoint`.
 */
export function AdminDashboard() {
  const { user } = useAuth();

  // TODO: wire endpoint — replace with `useDashboardSummary()` hook fed by
  // GET /api/admin/dashboard/summary (counts + tone metadata).
  const stats: StatCard[] = [
    { label: "Pages", value: "12", sub: "10 published · 2 drafts", tone: "accent" },
    { label: "News posts", value: "47", sub: "3 drafts waiting", tone: "warn" },
    { label: "Sermons", value: "184", sub: "across 12 series" },
    { label: "Upcoming events", value: "06", sub: "next 30 days" },
  ];

  // TODO: wire endpoint — GET /api/admin/dashboard/activity (last 5 entries).
  const activity: ActivityEntry[] = [
    { person: "Allan Simpson", verb: "published", target: "About Us", relative: "12 min ago" },
    { person: "Marcia Lee", verb: "drafted", target: "Easter Schedule news post", relative: "2 hr ago" },
    { person: "Pastor James", verb: "uploaded", target: "Sermon · Romans 8 (audio)", relative: "Yesterday" },
    { person: "Allan Simpson", verb: "edited", target: "Service Times", relative: "Yesterday" },
    { person: "Beth Carter", verb: "added", target: "Youth Camp event", relative: "2 days ago" },
  ];

  // TODO: wire endpoint — GET /api/admin/dashboard/this-sunday (next-up sermon).
  const thisSunday = {
    seriesLabel: "Series 03 · Romans",
    title: "More than conquerors",
    scripture: "Romans 8:31–39",
    preacher: "Pastor James Whitlock",
  };

  // TODO: wire endpoint — GET /api/admin/dashboard/tend-to (action queue).
  const tendTo: TendToItem[] = [
    { tone: "warn", text: "Three news drafts are waiting on you." },
    { tone: "accent", text: "Easter schedule needs review before Sunday." },
    { tone: "danger", text: "One audit-log warning from yesterday's import." },
  ];

  const todayLabel = new Date().toLocaleDateString("en-US", {
    weekday: "long",
    day: "numeric",
    month: "long",
    year: "numeric",
  });

  return (
    <div className="space-y-8">
      {/* PageHeader (eyebrow + title + actions) */}
      <header className="flex flex-wrap items-end justify-between gap-4 border-b border-border-soft pb-6">
        <div>
          <p className="text-xs font-medium uppercase tracking-[0.2em] text-muted">
            {todayLabel}
          </p>
          <h1 className="mt-2 font-heading text-3xl font-semibold tracking-tight">
            Welcome back, {user?.firstName}.
          </h1>
          <p className="mt-2 max-w-2xl text-sm text-fg-soft">
            Three drafts are waiting on you and the site is healthy.
          </p>
        </div>
        <div className="flex gap-2">
          <Link
            to="/admin/news/new"
            className="inline-flex h-10 items-center border border-border bg-panel px-4 text-sm font-medium hover:bg-panel-alt"
          >
            Quick add
          </Link>
          <Link
            to="/admin/pages/new"
            className="inline-flex h-10 items-center bg-foreground px-4 text-sm font-semibold text-background hover:opacity-90"
          >
            Compose page
          </Link>
        </div>
      </header>

      {/* Stat strip */}
      <section className="grid grid-cols-1 divide-y divide-border-soft border border-border bg-panel sm:grid-cols-2 sm:divide-y-0 sm:divide-x lg:grid-cols-4">
        {stats.map((s) => <StatColumn key={s.label} {...s} />)}
      </section>

      {/* Two-column grid: 1.5fr / 1fr */}
      <section className="grid grid-cols-1 gap-6 lg:grid-cols-[1.5fr_1fr]">
        {/* Recent activity */}
        <article className="border border-border bg-panel">
          <header className="flex items-center justify-between border-b border-border-soft px-5 py-4">
            <h2 className="font-heading text-lg font-semibold">Recent activity</h2>
            <Link to="/admin/audit-log" className="text-xs font-medium uppercase tracking-wider text-accent hover:underline">
              See all
            </Link>
          </header>
          <ul className="divide-y divide-border-soft">
            {activity.map((a, i) => (
              <li key={i} className="flex items-center gap-4 px-5 py-3">
                <span aria-hidden className="grid h-8 w-8 place-items-center bg-panel-alt text-xs font-bold text-fg-soft">
                  {a.person.split(" ").map((n) => n[0]).join("").slice(0, 2)}
                </span>
                <p className="flex-1 text-sm">
                  <span className="font-medium">{a.person}</span>{" "}
                  <span className="text-fg-soft">{a.verb}</span>{" "}
                  <span>{a.target}</span>
                </p>
                <span className="font-mono text-xs text-muted">{a.relative}</span>
              </li>
            ))}
          </ul>
        </article>

        {/* Right column stack */}
        <aside className="space-y-6">
          {/* This Sunday — dark inset */}
          <article className="relative overflow-hidden bg-sidebar p-6 text-background">
            <span aria-hidden className="absolute inset-y-0 left-0 w-[3px] bg-accent" />
            <p className="text-xs font-medium uppercase tracking-[0.2em] text-background/60">
              This Sunday · {thisSunday.seriesLabel}
            </p>
            <p className="mt-3 font-heading text-2xl font-semibold leading-tight text-background">
              "{thisSunday.title}"
            </p>
            <p className="mt-3 font-mono text-xs text-background/70">{thisSunday.scripture}</p>
            <p className="mt-1 text-sm text-background/80">{thisSunday.preacher}</p>
          </article>

          {/* Tend to */}
          <article className="border border-border bg-panel">
            <header className="border-b border-border-soft px-5 py-4">
              <h2 className="font-heading text-lg font-semibold">Tend to</h2>
            </header>
            <ul className="divide-y divide-border-soft">
              {tendTo.map((t, i) => (
                <li key={i} className="flex gap-3 px-5 py-3">
                  <span aria-hidden className={`mt-1 h-full w-[3px] shrink-0 ${toneBar(t.tone)}`} />
                  <p className="text-sm">{t.text}</p>
                </li>
              ))}
            </ul>
          </article>
        </aside>
      </section>

      {/* Shortcut tiles — kept for navigation continuity. */}
      <section className="grid grid-cols-2 gap-3 sm:grid-cols-4">
        <Shortcut to="/admin/pages" icon={FileText} label="Pages" />
        <Shortcut to="/admin/news" icon={Newspaper} label="News" />
        <Shortcut to="/admin/sermons" icon={Mic} label="Sermons" />
        <Shortcut to="/admin/events" icon={Calendar} label="Events" />
      </section>
    </div>
  );
}

interface StatCard {
  label: string;
  value: string;
  sub: string;
  tone?: "accent" | "warn";
}

function StatColumn({ label, value, sub, tone }: StatCard) {
  return (
    <div className="relative px-5 py-5">
      {tone && (
        <span
          aria-hidden
          className={`absolute inset-y-3 left-0 w-[3px] ${tone === "accent" ? "bg-accent" : "bg-warn"}`}
        />
      )}
      <p className="text-xs font-medium uppercase tracking-[0.2em] text-muted">{label}</p>
      <p className="mt-2 font-heading text-[42px] font-semibold leading-none tracking-tight">
        {value}
      </p>
      <p className="mt-2 text-xs text-fg-soft">{sub}</p>
    </div>
  );
}

interface ActivityEntry {
  person: string;
  verb: string;
  target: string;
  relative: string;
}

interface TendToItem {
  tone: "warn" | "accent" | "danger";
  text: string;
}

function toneBar(tone: TendToItem["tone"]) {
  if (tone === "warn") return "bg-warn";
  if (tone === "danger") return "bg-danger";
  return "bg-accent";
}

function Shortcut({
  to,
  icon: Icon,
  label,
}: {
  to: string;
  icon: React.ComponentType<{ className?: string }>;
  label: string;
}) {
  return (
    <Link
      to={to}
      className="flex items-center gap-3 border border-border bg-panel px-4 py-3 text-sm font-medium hover:bg-panel-alt"
    >
      <Icon className="h-4 w-4 text-accent" />
      {label}
    </Link>
  );
}
