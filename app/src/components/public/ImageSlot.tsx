export type ImageSlotRatio = "1:1" | "3:2" | "4:3" | "4:5" | "5:4" | "16:9" | "21:9";
export type ImageSlotTone = "default" | "inverse";

export interface ImageSlotProps {
  /** Optional CMS-supplied source. When set, renders a <picture> with
   * a WebP source + the supplied URL as JPEG fallback (per Phase 2's
   * upload pipeline that emits both variants). When null, renders the
   * labelled placeholder. */
  src?: string | null;
  /** Sibling WebP URL from the upload pipeline. */
  webpSrc?: string | null;
  /** Aspect ratio. Defaults to 3:2. */
  ratio?: ImageSlotRatio;
  /** Required alt text for accessibility. Pass empty string for purely
   * decorative imagery. */
  alt: string;
  /** Mono uppercase label rendered inside the placeholder. Match the
   * prototype's `[ HERO PHOTO ]` style. */
  label?: string;
  /** Background tone — `default` uses --panel-alt; `inverse` uses an
   * --inset-on-inset treatment for placeholders embedded in dark
   * bands. */
  tone?: ImageSlotTone;
  /** Lazy-load by default; opt out for above-the-fold hero images
   * where LCP is critical. */
  loading?: "eager" | "lazy";
  className?: string;
}

const RATIO_CLASSES: Record<ImageSlotRatio, string> = {
  "1:1": "aspect-square",
  "3:2": "aspect-[3/2]",
  "4:3": "aspect-[4/3]",
  "4:5": "aspect-[4/5]",
  "5:4": "aspect-[5/4]",
  "16:9": "aspect-video",
  "21:9": "aspect-[21/9]",
};

/**
 * Image slot — renders a <picture> with WebP+fallback when src is
 * supplied, or a labelled hairline-border placeholder when null.
 * The placeholder treatment matches the prototype's ImageSlot look
 * (mono caps caption like "[ HERO PHOTO ]") rather than a broken-image
 * icon. Phase 2's ImageUpload pipeline generates the WebP sibling
 * automatically, so callers only need to wire the storage URL pair.
 */
export function ImageSlot({
  src,
  webpSrc,
  ratio = "3:2",
  alt,
  label,
  tone = "default",
  loading = "lazy",
  className,
}: ImageSlotProps) {
  const ratioClass = RATIO_CLASSES[ratio];
  const wrapper = ["relative w-full overflow-hidden", ratioClass, className ?? ""].join(" ");

  if (src) {
    return (
      <picture className={wrapper}>
        {webpSrc ? <source srcSet={webpSrc} type="image/webp" /> : null}
        <img
          src={src}
          alt={alt}
          loading={loading}
          decoding="async"
          className="h-full w-full object-cover"
        />
      </picture>
    );
  }

  // Placeholder — labelled hairline-border box, mono caps caption.
  const placeholderTone =
    tone === "inverse"
      ? "border border-inset-foreground/30 bg-inset/40 text-inset-foreground/60"
      : "border border-border bg-panel-alt text-muted";
  const fallbackLabel = (label ?? alt ?? "image").toUpperCase();
  return (
    <div
      role={alt ? "img" : "presentation"}
      aria-label={alt || undefined}
      className={[wrapper, "flex items-center justify-center", placeholderTone].join(" ")}
    >
      <span className="font-mono text-xs tracking-[0.18em]">[ {fallbackLabel} ]</span>
    </div>
  );
}
