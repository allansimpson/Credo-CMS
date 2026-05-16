/**
 * Public-site shared primitives. Used by every public page; both
 * templates (Editorial Warm + Quiet Sanctuary) consume the same
 * components — template differences are CSS-variable-driven via the
 * `data-template` attribute on the church theme root.
 *
 * Add to this file when introducing a new primitive that's used by
 * 2+ pages.
 */
export { Btn, BtnLink } from "./Btn";
export type { PublicBtnVariant, PublicBtnSize } from "./Btn";

export { BigNum } from "./BigNum";
export type { BigNumSize, BigNumProps } from "./BigNum";

export { Chip } from "./Chip";
export type { ChipTone, ChipProps } from "./Chip";

export { Eyebrow } from "./Eyebrow";
export type { EyebrowProps } from "./Eyebrow";

export { Headline } from "./Headline";
export type { HeadlineProps, HeadlineSize } from "./Headline";

export { ImageSlot } from "./ImageSlot";
export type { ImageSlotProps, ImageSlotRatio, ImageSlotTone } from "./ImageSlot";

export { PIcon } from "./PIcon";
export type { PIconProps, PIconSize } from "./PIcon";

export { PublicPage } from "./PublicPage";
export type { PublicPageProps, PublicActivePage } from "./PublicPage";

export { PublicHeader } from "./PublicHeader";
export { PublicFooter } from "./PublicFooter";
