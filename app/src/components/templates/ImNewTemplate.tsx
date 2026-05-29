import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { SeoTags } from "@/components/shared/SeoTags";
import { BigNum, BtnLink, Eyebrow, Headline } from "@/components/public";
import {
  parsePageSections,
  introSection,
  findSection,
  extractH3Items,
  extractBoldQA,
  extractParagraphTexts,
  extractText,
} from "@/lib/parsePageSections";
import type { PublicPage } from "@/types/api";

export default function ImNewTemplate({ page }: { page: PublicPage }) {
  const { settings } = useSiteSettings();
  const sections = parsePageSections(page.bodyJson);

  const intro = introSection(sections);
  const scheduleSection = findSection(sections, /visit|hour/i);
  const faqSection = findSection(sections, /ask|faq|question/i);

  const introTexts = intro ? extractParagraphTexts(intro.nodes) : [];
  const scheduleItems = scheduleSection ? extractH3Items(scheduleSection.nodes) : [];
  const faqItems = faqSection ? extractBoldQA(faqSection.nodes) : [];

  return (
    <article>
      <SeoTags
        title={`${page.title} · ${settings?.churchName ?? ""}`}
        description={page.excerpt ?? page.metaDescription ?? undefined}
      />

      <header className="mx-auto max-w-7xl px-6 py-12 md:py-16">
        <Eyebrow accent>For first-time visitors</Eyebrow>
        <Headline as="h1" size="display" className="mt-3">
          We saved you a seat.
        </Headline>
        {introTexts.map((p, i) => (
          <p key={i} className="mt-4 max-w-2xl text-lg text-fg-soft">{p}</p>
        ))}
      </header>

      {scheduleItems.length > 0 && (
        <section className="border-t border-border-soft">
          <div className="mx-auto max-w-7xl px-6 py-12 md:py-16">
            <Eyebrow accent>{scheduleSection?.heading ?? "The visit, hour by hour"}</Eyebrow>
            <div className="mt-10 divide-y divide-border-soft">
              {scheduleItems.map((item) => {
                const timeMatch = item.title.match(/^(\d{1,2}:\d{2})\s*[—–-]\s*(.+)/);
                const time = timeMatch?.[1] ?? "";
                const label = timeMatch?.[2] ?? item.title;
                const desc = extractParagraphTexts(item.bodyNodes).join(" ");
                return (
                  <div key={item.title} className="grid grid-cols-[4.5rem_1fr] gap-x-6 py-6 md:grid-cols-[6rem_1fr] md:py-8">
                    <BigNum size="lg" tone="accent">{time}</BigNum>
                    <div>
                      <h3 className="text-lg font-semibold">{label}</h3>
                      {desc && <p className="mt-1 text-fg-soft">{desc}</p>}
                    </div>
                  </div>
                );
              })}
            </div>
          </div>
        </section>
      )}

      {faqItems.length > 0 && (
        <section className="border-t border-border-soft bg-panel-alt">
          <div className="mx-auto max-w-7xl px-6 py-12 md:py-16">
            <Eyebrow accent>FAQ</Eyebrow>
            <Headline as="h2" size="h2" className="mt-3">
              {faqSection?.heading ?? "Things people ask us."}
            </Headline>
            <div className="mt-10 grid gap-x-12 gap-y-8 md:grid-cols-2">
              {faqItems.map((item) => (
                <div key={item.question} className="border-t border-border-soft pt-4">
                  <h3 className="font-semibold">{item.question}</h3>
                  <p className="mt-1 text-fg-soft">{item.answer}</p>
                </div>
              ))}
            </div>
          </div>
        </section>
      )}

      <section className="border-t border-border-soft">
        <div className="mx-auto max-w-7xl px-6 py-12 text-center md:py-16">
          <Headline as="h2" size="h3">We&rsquo;d love to meet you.</Headline>
          <p className="mx-auto mt-3 max-w-xl text-fg-soft">
            Have questions? Drop us a line or just show up Sunday morning.
          </p>
          <div className="mt-6 flex flex-wrap justify-center gap-4">
            <BtnLink to="/contact" variant="primary" size="lg">Get in touch</BtnLink>
            <BtnLink to="/events" variant="secondary" size="lg">Upcoming events</BtnLink>
          </div>
        </div>
      </section>
    </article>
  );
}
