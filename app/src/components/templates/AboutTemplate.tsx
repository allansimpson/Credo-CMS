import { ArrowRight } from "lucide-react";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { SeoTags } from "@/components/shared/SeoTags";
import { BigNum, Eyebrow, Headline, ImageSlot } from "@/components/public";
import {
  parsePageSections,
  introSection,
  findSection,
  extractParagraphTexts,
  extractText,
} from "@/lib/parsePageSections";
import type { PublicPage } from "@/types/api";

const STATS: { value: string; label: string }[] = [
  { value: "1894", label: "Founded" },
  { value: "320", label: "Members" },
  { value: "46", label: "Small Groups" },
  { value: "12", label: "Mission Partners" },
];

export default function AboutTemplate({ page }: { page: PublicPage }) {
  const { settings } = useSiteSettings();
  const sections = parsePageSections(page.bodyJson);
  const intro = introSection(sections);
  const historySection = findSection(sections, /history/i);
  const beliefsSection = findSection(sections, /hold to|believe|shaped/i);

  const introParagraphs = intro ? extractParagraphTexts(intro.nodes) : [];
  const historyParagraphs = historySection ? extractParagraphTexts(historySection.nodes) : [];
  const beliefItems = beliefsSection ? extractParagraphTexts(beliefsSection.nodes) : [];

  const midCol = historyParagraphs.slice(0, Math.ceil(historyParagraphs.length / 2));
  const rightCol = historyParagraphs.slice(Math.ceil(historyParagraphs.length / 2));

  return (
    <article>
      <SeoTags
        title={`${page.title} · ${settings?.churchName ?? ""}`}
        description={page.excerpt ?? page.metaDescription ?? undefined}
      />

      <header className="mx-auto max-w-7xl px-6 py-12 md:py-16">
        <div className="flex flex-wrap items-center gap-x-3 gap-y-1">
          <Eyebrow accent>Our Story</Eyebrow>
          <span className="text-[11px] font-medium uppercase tracking-[0.14em] text-muted">
            · Est. 1894 · 130 Years · 412 Maple Avenue
          </span>
        </div>
        <Headline as="h1" size="display" className="mt-4 max-w-4xl">
          {page.title === "About Us"
            ? "One hundred and thirty years of stubborn, ordinary hope."
            : page.title}
        </Headline>
        {introParagraphs.map((p, i) => (
          <p key={i} className="mt-4 max-w-2xl text-lg text-fg-soft leading-relaxed">{p}</p>
        ))}
      </header>

      <section className="mx-auto max-w-7xl px-6">
        <ImageSlot ratio="16:9" label="The Sanctuary, 1962" alt="The sanctuary in 1962" />
        <div className="mt-2 flex justify-between text-[11px] font-medium uppercase tracking-[0.14em] text-muted">
          <span>Fig. 01 &mdash; The sanctuary, 1962</span>
          <span>Hope Community Archive</span>
        </div>
      </section>

      {historyParagraphs.length > 0 && (
        <section className="mx-auto max-w-7xl px-6 py-16 md:py-20">
          <div className="grid gap-8 md:grid-cols-[1fr_1fr_1fr]">
            <div>
              <Eyebrow accent>{historySection?.heading ?? "How we got here"}</Eyebrow>
              <Headline as="h2" size="h2" className="mt-3">A short history.</Headline>
            </div>
            <div className="space-y-5 text-fg-soft leading-relaxed">
              {midCol.map((p, i) => (
                <p key={i}>{i === 0 ? <><span className="float-left mr-1 mt-1 text-2xl font-semibold leading-none text-foreground">{p[0]}</span>{p.slice(1)}</> : p}</p>
              ))}
            </div>
            <div className="space-y-5 text-fg-soft leading-relaxed">
              {rightCol.map((p, i) => <p key={i}>{p}</p>)}
            </div>
          </div>
        </section>
      )}

      <section className="border-y border-border-soft">
        <div className="mx-auto grid max-w-7xl grid-cols-2 gap-6 px-6 py-10 md:grid-cols-4 md:py-14">
          {STATS.map((s) => (
            <div key={s.label}>
              <BigNum size="xl" tone="default">{s.value}</BigNum>
              <p className="mt-1 text-[11px] font-medium uppercase tracking-[0.14em] text-muted">{s.label}</p>
            </div>
          ))}
        </div>
      </section>

      {beliefsSection && beliefItems.length > 0 && (
        <section className="mx-auto max-w-7xl px-6 py-16 md:py-20">
          <Eyebrow accent>What we hold to</Eyebrow>
          <Headline as="h2" size="h1" className="mt-3 max-w-3xl">
            {beliefsSection.heading ?? "Five things that have shaped who we are."}
          </Headline>
          <div className="mt-8 grid gap-6 md:grid-cols-2">
            {beliefItems.map((text, i) => (
              <div key={i} className="flex gap-5 border-t border-border-soft pt-5">
                <BigNum size="lg" tone="accent">{String(i + 1).padStart(2, "0")}</BigNum>
                <p className="text-sm text-fg-soft leading-relaxed">{text}</p>
              </div>
            ))}
          </div>
          <div className="mt-10">
            <a href="/what-we-believe" className="inline-flex items-center gap-2 border border-border-soft px-5 py-2.5 text-sm font-medium hover:bg-panel-alt">
              Read our full statement of faith
              <ArrowRight aria-hidden="true" strokeWidth={1.75} className="h-4 w-4 translate-y-px" />
            </a>
          </div>
        </section>
      )}
    </article>
  );
}
