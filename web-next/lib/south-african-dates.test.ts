import { describe, expect, it } from "vitest";
import { southAfricanDaysToUtcRange } from "./south-african-dates";

describe("southAfricanDaysToUtcRange", () => {
  it("converts October 2026 into the correct UTC range", () => {
    expect(
      southAfricanDaysToUtcRange("2026-10-01", "2026-10-31")
    ).toEqual({
      from: "2026-09-30T22:00:00.000Z",
      to: "2026-10-31T22:00:00.000Z",
    });
  });

  it("includes October 31 at 23:30 SAST", () => {
    const range = southAfricanDaysToUtcRange(
      "2026-10-01",
      "2026-10-31"
    );

    const payment = new Date("2026-10-31T21:30:00.000Z");

    expect(payment.toISOString() >= range.from!).toBe(true);
    expect(payment.toISOString() < range.to!).toBe(true);
  });

  it("excludes November 1 at midnight SAST", () => {
    const range = southAfricanDaysToUtcRange(
      "2026-10-01",
      "2026-10-31"
    );

    const payment = new Date("2026-10-31T22:00:00.000Z");

    expect(payment.toISOString() < range.to!).toBe(false);
  });

  it("handles a single selected day", () => {
    expect(
      southAfricanDaysToUtcRange("2026-10-15", "2026-10-15")
    ).toEqual({
      from: "2026-10-14T22:00:00.000Z",
      to: "2026-10-15T22:00:00.000Z",
    });
  });

  it("supports a start date without an end date", () => {
    expect(
      southAfricanDaysToUtcRange("2026-10-01")
    ).toEqual({
      from: "2026-09-30T22:00:00.000Z",
      to: undefined,
    });
  });

  it("supports an end date without a start date", () => {
    expect(
      southAfricanDaysToUtcRange(undefined, "2026-10-31")
    ).toEqual({
      from: undefined,
      to: "2026-10-31T22:00:00.000Z",
    });
  });

  it("rejects impossible calendar dates", () => {
    expect(() =>
      southAfricanDaysToUtcRange("2026-02-30")
    ).toThrow();
  });

  it("rejects reversed date ranges", () => {
    expect(() =>
      southAfricanDaysToUtcRange("2026-11-01", "2026-10-01")
    ).toThrow();
  });
});