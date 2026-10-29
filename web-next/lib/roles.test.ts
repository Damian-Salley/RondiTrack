import { describe, expect, it } from "vitest";
import { getTreasurerStokvels } from "./roles";
import type { HomeStokvel } from "./api";

const stokvels: HomeStokvel[] = [
    {
        stokvelId: 1,
        stokvelName: "Stokvel A",
        role: "Treasurer",
        currentCycle: 1,
        cycleStatus: "Active",
        paidThisCycle: 50000,
        stillOwes: 0,
        arrearsCents: 0,
        nextPayoutDate: null,
    },
    {
        stokvelId: 2,
        stokvelName: "Stokvel B",
        role: "Member",
        currentCycle: 1,
        cycleStatus: "Active",
        paidThisCycle: 0,
        stillOwes: 50000,
        arrearsCents: 0,
        nextPayoutDate: null,
    },
];

describe("getTreasurerStokvels", () => {
    it("returns only stokvels where the user is Treasurer", () => {
        const result = getTreasurerStokvels(stokvels);

        expect(result).toHaveLength(1);
        expect(result[0].stokvelId).toBe(1);
    });

    it("returns an empty list when the user is not Treasurer", () => {
        const result = getTreasurerStokvels([stokvels[1]]);

        expect(result).toEqual([]);
    });

    it("returns an empty list when there are no stokvels", () => {
        expect(getTreasurerStokvels([])).toEqual([]);
    });
});