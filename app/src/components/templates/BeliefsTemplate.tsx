import { Link } from "react-router-dom";
import { ArrowRight } from "lucide-react";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { SeoTags } from "@/components/shared/SeoTags";
import { BigNum, Eyebrow, Headline } from "@/components/public";
import {
  parsePageSections,
  introSection,
  findSection,
  extractParagraphTexts,
} from "@/lib/parsePageSections";
import type { PublicPage } from "@/types/api";

export default function BeliefsTemplate({ page }: { page: PublicPage }) {
  const { settings } = useSiteSettings();
  const sections = parsePageSections(page.bodyJson);

  const intro = introSection(sections);
  const introTexts = intro ? extractParagraphTexts(intro.nodes) : [];

  const creedSection = findSection(sections, /creed/i);
  const creedTexts = creedSection ? extractParagraphTexts(creedSection.nodes) : [];

  const beliefSections = sections.filter(
    (s) => s.heading !== null && s !== creedSection && s !== intro
  );

  return (
    <article>
      <SeoTags
        title={`${page.title} · ${settings?.churchName ?? ""}`}
        description={page.excerpt ?? page.metaDescription ?? undefined}
      />

      <header className="mx-auto max-w-7xl px-6 py-12 md:py-16">
        <Eyebrow accent>What we believe</Eyebrow>
        <Headline as="h1" size="display" className="mt-3 max-w-3xl">
          An old faith, plainly said.
        </Headline>
        {introTexts.map((p, i) => (
          <p key={i} className="mt-5 max-w-xl text-lg text-fg-soft leading-relaxed">{p}</p>
        ))}
      </header>

      {beliefSections.length > 0 && (
        <section className="mx-auto max-w-7xl px-6 py-8 md:py-12">
          <div className="divide-y divide-border-soft border-y border-border-soft">
            {beliefSections.map((s, i) => {
              const texts = extractParagraphTexts(s.nodes);
              return (
                <div key={i} className="grid items-start gap-x-8 gap-y-2 py-8 md:grid-cols-[5rem_12rem_1fr] md:py-10">
                  <BigNum size="xl" tone="accent">{String(i + 1).padStart(2, "0")}</BigNum>
                  <h3 className="text-xl font-semibold md:text-2xl">{s.heading}</h3>
                  <div className="space-y-2 text-fg-soft leading-relaxed">
                    {texts.map((t, j) => <p key={j}>{t}</p>)}
                  </div>
                </div>
              );
            })}
          </div>
        </section>
      )}

      {creedSection && (
        <section className="bg-panel-alt">
          <div className="mx-auto max-w-3xl px-6 py-16 text-center md:py-20">
            <Eyebrow accent className="justify-center">{creedSection.heading}</Eyebrow>
            <Headline as="h2" size="h2" className="mt-4">
              We confess with the whole Church.
            </Headline>
            {creedTexts.map((t, i) => (
              <p key={i} className="mx-auto mt-4 max-w-xl text-fg-soft leading-relaxed">{t}</p>
            ))}
            <div className="mt-8">
              <Link
                to="/what-we-believe"
                className="inline-flex items-center gap-2 bg-primary px-5 py-2.5 text-sm font-semibold text-primary-foreground hover:bg-primary/90"
              >
                Read the full statement of faith
                <ArrowRight aria-hidden="true" strokeWidth={1.75} className="h-4 w-4 translate-y-px" />
              </Link>
            </div>
          </div>
        </section>
      )}
    </article>
  );
}
