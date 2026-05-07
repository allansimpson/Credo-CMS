import { describe, it, expect, beforeEach } from "vitest";
import { readConsent, writeConsent, clearConsent } from "./CookieConsentBanner";

describe("cookie consent helpers", () => {
  beforeEach(() => {
    clearConsent();
  });

  it("returns null when no cookie is set", () => {
    expect(readConsent()).toBeNull();
  });

  it("round-trips accepted", () => {
    writeConsent("accepted");
    expect(readConsent()).toBe("accepted");
  });

  it("round-trips declined", () => {
    writeConsent("declined");
    expect(readConsent()).toBe("declined");
  });

  it("clearConsent removes the cookie", () => {
    writeConsent("accepted");
    expect(readConsent()).toBe("accepted");
    clearConsent();
    expect(readConsent()).toBeNull();
  });

  it("ignores unknown values", () => {
    document.cookie = "cms_consent=garbage; path=/";
    expect(readConsent()).toBeNull();
  });
});
