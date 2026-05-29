import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi, beforeEach } from "vitest";
import { MemoryRouter } from "react-router-dom";
import { HomePage } from "./HomePage";
import { SiteSettingsContext } from "@/lib/SiteSettingsContext";
import type { PublicSiteSettings, PublicTemplate } from "@/types/api";

// homepageApi.get is called on mount; we want both templates to render the
// full layout from the same data shape so the visual-treatment difference
// is the only thing under test.
vi.mock("@/lib/api/homepage", () => ({
  homepageApi: {
    get: () => Promise.resolve({
      site: {
        churchName: "Hope Community",
        tagline: "A church for everyone",
        homepageHeroCtaLabel: "Plan your visit",
        homepageHeroCtaLink: "#service-times",
      },
      serviceTimes: [
        { dayOfWeek: "Sunday", startTime: "10:00:00", endTime: "11:30:00", name: "Worship", location: "Main hall" },
      ],
      latestNews: [],
      membersWelcomeText: null,
      banner: null,
      latestSermon: null,
      upcomingEvents: [],
    }),
  },
}));

function makeSettings(template: PublicTemplate): PublicSiteSettings {
  return {
    churchName: "Hope Community",
    tagline: "A church for everyone",
    logoUrl: null,
    primaryColor: "#1e3a8a",
    accentColor: "#b8531a",
    contactEmail: null, contactPhone: null, contactAddress: null,
    facebookUrl: null, instagramUrl: null, youTubeUrl: null,
    xUrl: null, tikTokUrl: null,
    otherSocialLabel: null, otherSocialUrl: null,
    footerText: null,
    leadersPageLabel: "Our Leaders",
    homepageHeroCtaLabel: "Plan your visit",
    homepageHeroCtaLink: "#service-times",
    facebookLoginEnabled: false,
    analyticsProvider: "None",
    ga4MeasurementId: null,
    ga4ConsentBannerEnabled: true,
    ga4ConsentBannerPosition: "BottomRight",
    cookiePolicyPageSlug: null,
    template,
  };
}

function renderHome(template: PublicTemplate) {
  return render(
    <MemoryRouter>
      <SiteSettingsContext.Provider value={{ settings: makeSettings(template), reload: async () => {} }}>
        <HomePage />
      </SiteSettingsContext.Provider>
    </MemoryRouter>,
  );
}

describe("HomePage template variants", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("Editorial template renders the I'm New strip as a dark inset band", () => {
    renderHome(0);
    const strip = screen.getByTestId("im-new-strip");
    expect(strip.getAttribute("data-template")).toBe("editorial");
    // The dark-inset signature is the bg-inset utility — content-shape
    // assertion, not pixel comparison.
    expect(strip.className).toContain("bg-inset");
  });

  it("Quiet template renders the I'm New strip as a panel with rule dividers", () => {
    renderHome(1);
    const strip = screen.getByTestId("im-new-strip");
    expect(strip.getAttribute("data-template")).toBe("quiet");
    // Quiet treatment uses border-top + no dark inset.
    expect(strip.className).toContain("border-t");
    expect(strip.className).not.toContain("bg-inset");
  });

  it("renders the same content shape across both templates (heading + body)", () => {
    const { unmount } = renderHome(0);
    const editorialHeading = screen.getByRole("heading", { name: /we saved you a seat/i });
    expect(editorialHeading).toBeTruthy();
    unmount();

    renderHome(1);
    const quietHeading = screen.getByRole("heading", { name: /we saved you a seat/i });
    expect(quietHeading).toBeTruthy();
  });
});
