import { describe, it, expect } from "vitest";
import {
  formatScriptureReference,
  validateScriptureReference,
  type ScriptureReference,
} from "./scripture";

const ref = (
  book: number,
  chapterStart: number,
  verseStart: number | null = null,
  chapterEnd: number | null = null,
  verseEnd: number | null = null,
): ScriptureReference => ({ book, chapterStart, verseStart, chapterEnd, verseEnd });

describe("formatScriptureReference", () => {
  it("formats whole chapter (book 45 = Romans)", () => {
    expect(formatScriptureReference(ref(45, 8))).toBe("Romans 8");
  });

  it("formats Psalms as 'Psalm' for a single chapter (book 19 = Psalms)", () => {
    expect(formatScriptureReference(ref(19, 23))).toBe("Psalm 23");
  });

  it("formats Psalms as 'Psalms' for a chapter range", () => {
    expect(formatScriptureReference(ref(19, 22, null, 24, null))).toBe("Psalms 22–24");
  });

  it("formats single verse (book 1 = Genesis)", () => {
    expect(formatScriptureReference(ref(1, 1, 1))).toBe("Genesis 1:1");
  });

  it("formats within-chapter verse range (book 62 = 1 John)", () => {
    expect(formatScriptureReference(ref(62, 2, 15, null, 17))).toBe("1 John 2:15–17");
  });

  it("formats cross-chapter range with verses (book 40 = Matthew)", () => {
    expect(formatScriptureReference(ref(40, 5, 1, 7, 29))).toBe("Matthew 5:1–7:29");
  });

  it("uses an en-dash for ranges, not a hyphen", () => {
    const out = formatScriptureReference(ref(45, 1, 1, null, 17));
    expect(out).toContain("–");
    expect(out).not.toContain("-");
  });
});

describe("validateScriptureReference", () => {
  it("accepts a valid reference", () => {
    expect(validateScriptureReference(ref(45, 8))).toBeNull();
  });

  it("rejects chapter past the book max (Matthew has 28)", () => {
    expect(validateScriptureReference(ref(40, 50))).toMatch(/out of range/);
  });

  it("rejects ending chapter < starting", () => {
    expect(validateScriptureReference(ref(45, 8, null, 5, null))).toMatch(/Ending chapter/);
  });

  it("rejects ending verse < starting verse within the same chapter", () => {
    expect(validateScriptureReference(ref(62, 2, 17, null, 15))).toMatch(/Ending verse/);
  });
});
