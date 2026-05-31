import { useEffect, useMemo, useState } from "react";
import { Link, useNavigate } from "react-router-dom";
import { Heart, Plus, MessageSquare, HandHeart } from "lucide-react";
import {
  memberPrayerApi,
  PrayerRequestStatus,
  type PrayerRequestListItem,
} from "@/lib/api/prayerRequests";
import {
  Content,
  EmptyState,
  InlineError,
  PageHead,
  Panel,
  SegTabs,
  SkeletonCard,
} from "@/components/members/portal-primitives";

type Tab = "mine" | "others";

/**
 * Prayer list — Mine / Others tabs only. No "Answered" surface anywhere
 * per resolution #4. Archived requests are filtered out of the member
 * surface entirely; the terminal state never reads to members.
 */
export function PrayerListPage() {
  const navigate = useNavigate();
  const [tab, setTab] = useState<Tab>("mine");
  const [items, setItems] = useState<PrayerRequestListItem[] | null>(null);
  const [error, setError] = useState(false);

  useEffect(() => {
    let cancelled = false;
    setError(false);
    memberPrayerApi.list()
      .then((d) => { if (!cancelled) setItems(d); })
      .catch(() => { if (!cancelled) setError(true); });
    return () => { cancelled = true; };
  }, []);

  const { mine, others } = useMemo(() => {
    const active = (items ?? []).filter((i) => i.status !== PrayerRequestStatus.Archived);
    return {
      mine: active.filter((i) => i.viewerCanEdit),
      others: active.filter((i) => !i.viewerCanEdit),
    };
  }, [items]);

  const list = tab === "mine" ? mine : others;

  return (
    <Content>
      <PageHead
        title="Prayer Requests"
        sub="Lift up the requests on your heart. Members can submit and add prayers; staff post updates."
        actions={
          <button
            type="button"
            onClick={() => navigate("/members/prayer/new")}
            className="inline-flex items-center gap-2 bg-accent px-4 py-2 text-sm font-semibold text-accent-foreground hover:bg-accent/90"
          >
            <Plus strokeWidth={1.75} className="h-4 w-4" /> Submit prayer
          </button>
        }
      />

      <SegTabs
        tabs={[
          { id: "mine", label: "Mine" },
          { id: "others", label: "Others" },
        ]}
        active={tab}
        onChange={(id) => setTab(id as Tab)}
      />

      {error && <InlineError onRetry={() => location.reload()} />}

      {!error && items === null && (
        <div className="space-y-3">
          <SkeletonCard /><SkeletonCard /><SkeletonCard />
        </div>
      )}

      {items && list.length === 0 && (
        <EmptyState
          icon={<HandHeart strokeWidth={1.6} className="h-5 w-5" />}
          title={tab === "mine" ? "No prayer requests yet" : "Nothing here yet"}
          body={
            tab === "mine"
              ? "When you submit a request, it'll show here."
              : "When others submit requests, they'll appear here for you to pray over."
          }
          action={
            tab === "mine" && (
              <button
                type="button"
                onClick={() => navigate("/members/prayer/new")}
                className="inline-flex items-center gap-2 bg-accent px-4 py-2 text-sm font-semibold text-accent-foreground hover:bg-accent/90"
              >
                <Plus strokeWidth={1.75} className="h-4 w-4" /> Submit prayer
              </button>
            )
          }
        />
      )}

      {items && list.length > 0 && (
        <ul className="space-y-3">
          {list.map((p) => <PrayerRow key={p.id} item={p} />)}
        </ul>
      )}
    </Content>
  );
}

function PrayerRow({ item }: { item: PrayerRequestListItem }) {
  const excerpt = stripProseMirror(item.bodyJson, 200);
  const submitter = item.isAnonymous ? "Anonymous" : item.submitterDisplayName ?? "Member";
  return (
    <li>
      <Link
        to={`/members/prayer/${item.id}`}
        className="block border border-border bg-panel p-4 transition-colors hover:bg-panel-alt"
      >
        <div className="flex items-start gap-4">
          <div className="min-w-0 flex-1">
            <h3 className="truncate font-heading text-base font-semibold">{item.title}</h3>
            {excerpt && (
              <p className="mt-1 line-clamp-2 text-sm leading-relaxed text-fg-soft">{excerpt}</p>
            )}
            <p className="mt-2 font-mono text-[10.5px] uppercase tracking-[0.12em] text-muted">
              {submitter}
            </p>
          </div>
          <div className="flex shrink-0 items-center gap-3 font-mono text-[11px] tabular-nums text-muted">
            <span className="inline-flex items-center gap-1">
              <Heart
                strokeWidth={1.75}
                className={`h-3.5 w-3.5 ${item.viewerHasPrayed ? "fill-accent text-accent" : ""}`}
              />
              {item.prayedForCount}
            </span>
            {item.updateCount > 0 && (
              <span className="inline-flex items-center gap-1">
                <MessageSquare strokeWidth={1.75} className="h-3.5 w-3.5" />
                {item.updateCount}
              </span>
            )}
          </div>
        </div>
      </Link>
    </li>
  );
}

function stripProseMirror(json: string | null, max = 280): string {
  if (!json) return "";
  try {
    const parsed = JSON.parse(json);
    const texts: string[] = [];
    const walk = (node: { text?: string; content?: unknown[] }) => {
      if (node.text) texts.push(node.text);
      if (Array.isArray(node.content)) {
        node.content.forEach((c) => walk(c as Parameters<typeof walk>[0]));
      }
    };
    walk(parsed);
    const flat = texts.join(" ").trim();
    return flat.length > max ? flat.slice(0, max).trimEnd() + "…" : flat;
  } catch {
    return "";
  }
}
