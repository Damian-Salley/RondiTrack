import { describe, expect, it } from "vitest";
import { formatCents, formatDate } from "./formatters";

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