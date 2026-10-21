import { describe, expect, it } from "vitest";
import {
    parseDueDate,
    getCountdownText,
    msUntilNextSAMidnight,
} from "./dueCountdown";

describe("DueCountdown", () => {
    it("converts a date-only value to the correct UTC deadline", () => {
        expect(parseDueDate("2026-10-31").toISOString())
            .toBe("2026-10-31T22:00:00.000Z");
    });

    it("preserves an existing UTC timestamp", () => {
        expect(parseDueDate("2026-10-31T22:00:00Z").toISOString())
            .toBe("2026-10-31T22:00:00.000Z");
    });

    it("displays due today", () => {
        expect(
            getCountdownText(
                "2026-10-21",
                new Date("2026-10-21T10:00:00Z"),
            ),
        ).toBe("Due today");
    });

    it("displays due tomorrow", () => {
        expect(
            getCountdownText(
                "2026-10-22",
                new Date("2026-10-21T10:00:00Z"),
            ),
        ).toBe("Due tomorrow");
    });

    it("displays overdue days", () => {
        expect(
            getCountdownText(
                "2026-10-19",
                new Date("2026-10-21T10:00:00Z"),
            ),
        ).toBe("Overdue by 2 days");
    });

    it("calculates the next Johannesburg midnight", () => {
        expect(
            msUntilNextSAMidnight(new Date("2026-10-21T21:00:00Z")),
        ).toBe(3_600_000);
    });
});