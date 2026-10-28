import { describe, expect, it } from "vitest";
import { formatCents, formatDate, parseRandToCents } from "./formatters";

describe("parseRandToCents", () => {
  it.each([
    ["150", 15000],
    ["150.5", 15050],
    ["150.50", 15050],
    ["150,50", 15050],
    ["1 500,50", 150050],
    ["0.99", 99],
    ["100000", 10000000],
  ])("converts %s into %i cents", (input, expected) => {
    expect(parseRandToCents(input)).toBe(expected);
  });

  it.each([
    "",
    "0",
    "-150",
    "150.555",
    "150,555",
    "abc",
    "1 50,50",
    "R150",
    "1.500,50",
  ])("rejects invalid input %s", (input) => {
    expect(parseRandToCents(input)).toBeNull();
  });

  it("rejects amounts larger than JavaScript's safe integer range", () => {
    expect(parseRandToCents("999999999999999")).toBeNull();
  });

  it("preserves cents when converting back to rand", () => {
    // Check every cent value from R0.01 to R100.00.
    for (let cents = 1; cents <= 10_000; cents++) {
      const randInput =
        `${Math.floor(cents / 100)}.${String(cents % 100).padStart(2, "0")}`;

      expect(parseRandToCents(randInput)).toBe(cents);
    }

    // Check larger amounts, including the R100,000 boundary.
    for (const cents of [100_001, 150_050, 999_999, 10_000_000]) {
      const randInput =
        `${Math.floor(cents / 100)}.${String(cents % 100).padStart(2, "0")}`;

      expect(parseRandToCents(randInput)).toBe(cents);
    }
  });

}); // Closes describe("parseRandToCents")

describe("formatCents", () => {
  it("formats zero cents", () => {
    expect(formatCents(0)).toBe("R\u00A00,00");
  });

  it("formats ten cents correctly", () => {
    expect(formatCents(10)).toBe("R\u00A00,10");
  });

  it("formats a normal rand amount", () => {
    expect(formatCents(12345)).toBe("R\u00A0123,45");
  });

  it("formats large integer amounts without losing precision", () => {
    expect(formatCents("9007199254740993"))
      .toBe("R\u00A090 071 992 547 409,93");
  });

  it("formats negative amounts", () => {
    expect(formatCents(-1050)).toBe("-R\u00A010,50");
  });
});

describe("formatDate", () => {
  it("converts UTC to Johannesburg time", () => {
    const result = formatDate("2026-10-31T22:30:00Z");

    expect(result).toContain("1 Nov 2026");
    expect(result).toContain("00:30");
  });

  it("handles missing dates", () => {
    expect(formatDate(null)).toBe("Not available");
  });

  it("handles invalid dates", () => {
    expect(formatDate("invalid")).toBe("Invalid date");
  });
});