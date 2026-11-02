import { describe, expect, it } from "vitest";
import { normalizeContributionFilters } from "./contribution-filters";

describe("Contribution URL filters", () => {
  it("keeps valid filters", () => {
    expect(
      normalizeContributionFilters({
        member: 2,
        from: "2026-10-01",
        to: "2026-10-31",
      })
    ).toEqual({
      member: 2,
      from: "2026-10-01",
      to: "2026-10-31",
    });
  });

  it("replaces invalid dates with defaults", () => {
    expect(
      normalizeContributionFilters({
        member: 0,
        from: "2026-02-30",
        to: "yesterday",
      })
    ).toEqual({
      member: 0,
      from: "",
      to: "",
    });
  });

  it("rejects invalid member IDs", () => {
    expect(
      normalizeContributionFilters({
        member: -5,
        from: "",
        to: "",
      }).member
    ).toBe(0);
  });
});