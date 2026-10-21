import { describe, expect, it } from "vitest";
import {
    groupContributionsByCycle,
    type Contribution,
    type Cycle,
} from "./groupContributionsByCycle";

const cycles: Cycle[] = [
    {
        id: 2,
        number: 2,
        status: "Active",
        dueDateUtc: "2026-10-31T22:00:00Z",
        totalCollectedCents: 10000,
        amountDueCents: 20000,
    },
    {
        id: 1,
        number: 1,
        status: "Closed",
        dueDateUtc: "2026-09-30T22:00:00Z",
        totalCollectedCents: 5000,
        amountDueCents: 20000,
    },
];

const contributions = [
    {
        id: "test-1",
        cycle: 2,
        paidAtUtc: "2026-10-20T10:00:00Z",
    },
    {
        id: "test-2",
        cycle: 2,
        paidAtUtc: "2026-10-21T10:00:00Z",
    },
    {
        id: "test-3",
        cycle: 99,
        paidAtUtc: null,
    },
] as Contribution[];

describe("groupContributionsByCycle", () => {
    it("sorts cycles newest first", () => {
        const result = groupContributionsByCycle(cycles, contributions);

        expect(result[0].cycle?.number).toBe(2);
        expect(result[1].cycle?.number).toBe(1);
    });

    it("sorts contributions newest first", () => {
        const result = groupContributionsByCycle(cycles, contributions);

        expect(result[0].contributions.map((item) => item.id)).toEqual([
            "test-2",
            "test-1",
        ]);
    });

    it("preserves empty cycles", () => {
        const result = groupContributionsByCycle(cycles, contributions);

        expect(result[1].contributions).toHaveLength(0);
    });

    it("preserves contributions with unknown cycles", () => {
        const result = groupContributionsByCycle(cycles, contributions);

        expect(result[2].cycle).toBeNull();
        expect(result[2].contributions[0].id).toBe("test-3");
    });
});