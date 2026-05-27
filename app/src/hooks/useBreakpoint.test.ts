import { act, renderHook } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { useBreakpoint } from "./useBreakpoint";

function setWindowWidth(width: number) {
  Object.defineProperty(window, "innerWidth", {
    writable: true,
    configurable: true,
    value: width,
  });
}

describe("useBreakpoint", () => {
  it("returns 'mobile' below 768px", () => {
    setWindowWidth(375);
    const { result } = renderHook(() => useBreakpoint());
    act(() => {
      window.dispatchEvent(new Event("resize"));
    });
    expect(result.current).toBe("mobile");
  });

  it("returns 'tablet' between 768 and 1280px", () => {
    setWindowWidth(900);
    const { result } = renderHook(() => useBreakpoint());
    act(() => {
      window.dispatchEvent(new Event("resize"));
    });
    expect(result.current).toBe("tablet");
  });

  it("returns 'desktop' at 1280px and above", () => {
    setWindowWidth(1440);
    const { result } = renderHook(() => useBreakpoint());
    act(() => {
      window.dispatchEvent(new Event("resize"));
    });
    expect(result.current).toBe("desktop");
  });

  it("updates on window resize", () => {
    setWindowWidth(1440);
    const { result } = renderHook(() => useBreakpoint());
    act(() => {
      window.dispatchEvent(new Event("resize"));
    });
    expect(result.current).toBe("desktop");

    act(() => {
      setWindowWidth(500);
      window.dispatchEvent(new Event("resize"));
    });
    expect(result.current).toBe("mobile");
  });
});
