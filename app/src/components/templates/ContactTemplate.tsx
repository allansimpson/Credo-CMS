import { useState } from "react";
import { useSiteSettings } from "@/lib/SiteSettingsContext";
import { SeoTags } from "@/components/shared/SeoTags";
import { Eyebrow, Headline, ImageSlot } from "@/components/public";
import { Phone, Mail, MapPin, ArrowRight } from "lucide-react";
import { parsePageSections, introSection, extractParagraphTexts } from "@/lib/parsePageSections";
import type { PublicPage } from "@/types/api";

export default function ContactTemplate({ page }: { page: PublicPage }) {
  const { settings } = useSiteSettings();
  const [form, setForm] = useState({ name: "", email: "", subject: "", message: "" });
  const [submitted, setSubmitted] = useState(false);

  const sections = parsePageSections(page.bodyJson);
  const intro = introSection(sections);
  const introTexts = intro ? extractParagraphTexts(intro.nodes) : [];

  const phone = settings?.contactPhone ?? "(319) 555-0184";
  const email = settings?.contactEmail ?? "office@hopecommunity.church";
  const address = settings?.contactAddress ?? "412 Maple Avenue\nCedar Falls, IA 50613";

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setSubmitted(true);
  };

  const field = (name: keyof typeof form, label: string, type = "text", multiline = false) => (
    <div>
      <label className="block text-[11px] font-medium uppercase tracking-[0.14em] text-muted">{label}</label>
      {multiline ? (
        <textarea
          value={form[name]}
          onChange={(e) => setForm((f) => ({ ...f, [name]: e.target.value }))}
          rows={6}
          className="mt-1.5 w-full border border-border-soft bg-panel px-4 py-3 text-sm placeholder:text-muted focus:border-foreground focus:outline-none"
        />
      ) : (
        <input
          type={type}
          value={form[name]}
          onChange={(e) => setForm((f) => ({ ...f, [name]: e.target.value }))}
          className="mt-1.5 h-11 w-full border border-border-soft bg-panel px-4 text-sm placeholder:text-muted focus:border-foreground focus:outline-none"
        />
      )}
    </div>
  );

  return (
    <article>
      <SeoTags
        title={`${page.title} · ${settings?.churchName ?? ""}`}
        description={page.excerpt ?? "Drop us a line."}
      />

      <header className="mx-auto max-w-7xl px-6 py-12 md:py-16">
        <Eyebrow accent>Get in touch</Eyebrow>
        <Headline as="h1" size="display" className="mt-3">
          Drop us a line.
        </Headline>
        {introTexts.map((p, i) => (
          <p key={i} className="mt-4 max-w-2xl text-fg-soft leading-relaxed">{p}</p>
        ))}
      </header>

      <hr className="border-border-soft" />

      <section className="mx-auto max-w-7xl px-6 py-12 md:py-16">
        <div className="grid gap-12 md:grid-cols-[1fr_1fr]">
          <div>
            <Eyebrow accent>Send a message</Eyebrow>
            {submitted ? (
              <div className="mt-6 border border-border-soft bg-panel-alt p-6">
                <p className="font-semibold">Thanks for reaching out!</p>
                <p className="mt-1 text-sm text-fg-soft">We&rsquo;ll get back to you as soon as we can.</p>
              </div>
            ) : (
              <form onSubmit={handleSubmit} className="mt-6 space-y-5">
                {field("name", "Your name")}
                {field("email", "Email", "email")}
                {field("subject", "Subject")}
                {field("message", "Message", "text", true)}
                <button type="submit" className="inline-flex items-center gap-2 bg-primary px-5 py-2.5 text-sm font-semibold text-primary-foreground hover:bg-primary/90">
                  Send message
                  <ArrowRight aria-hidden="true" strokeWidth={1.75} className="h-4 w-4 translate-y-px" />
                </button>
              </form>
            )}
          </div>
          <div className="space-y-8">
            <div className="border border-border-soft bg-[#F0EDE5] p-6">
              <Eyebrow accent>Office Hours</Eyebrow>
              <dl className="mt-4 space-y-2 text-sm">
                <div className="flex justify-between border-b border-dashed border-border-soft pb-2">
                  <dt>Mon – Thu</dt><dd className="font-mono text-xs">9:00 AM – 4:00 PM</dd>
                </div>
                <div className="flex justify-between border-b border-dashed border-border-soft pb-2">
                  <dt>Friday</dt><dd className="font-mono text-xs">9:00 AM – 12:00 PM</dd>
                </div>
                <div className="flex justify-between">
                  <dt>Sat – Sun</dt><dd className="font-mono text-xs">Closed (Sunday services)</dd>
                </div>
              </dl>
            </div>
            <div className="border border-border-soft p-6">
              <Eyebrow accent>Direct Lines</Eyebrow>
              <ul className="mt-4 space-y-3 text-sm">
                <li className="flex items-center gap-2.5">
                  <Phone size={16} strokeWidth={1.5} className="shrink-0 text-accent" />
                  <a href={`tel:${phone.replace(/[^\d+]/g, "")}`} className="hover:underline">{phone}</a>
                </li>
                <li className="flex items-center gap-2.5">
                  <Mail size={16} strokeWidth={1.5} className="shrink-0 text-accent" />
                  <a href={`mailto:${email}`} className="font-mono text-xs hover:underline">{email}</a>
                </li>
                <li className="flex items-start gap-2.5">
                  <MapPin size={16} strokeWidth={1.5} className="mt-0.5 shrink-0 text-accent" />
                  <span>{address}</span>
                </li>
              </ul>
            </div>
          </div>
        </div>
      </section>

      <section className="mx-auto max-w-7xl px-6 pb-12">
        <ImageSlot ratio="21:9" label={`Map · ${address.split("\n")[0]}, Cedar Falls`} alt="Map" />
      </section>
    </article>
  );
}
