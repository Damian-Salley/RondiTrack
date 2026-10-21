import type { components } from "@/api/schema";

export type Contribution =
    components["schemas"]["ContributionResponse"];

// The data needed to display a cycle.
export interface Cycle {
    id: number;
    number: number;
    status: string;
    dueDateUtc: string;
    totalCollectedCents: number;
    amountDueCents: number;
}

export interface ContributionGroup {
    cycle: Cycle | null;
    contributions: Contribution[];
}

// Group contributions by their cycle database ID.
export function groupContributionsByCycle(
    cycles: Cycle[],
    contributions: Contribution[],
): ContributionGroup[] {
    // Show newest cycles first, including empty cycles.
    const groups: ContributionGroup[] = [...cycles]
        .sort((a, b) => b.number - a.number)
        .map((cycle) => ({
            cycle,
            contributions: contributions
                .filter((contribution) => Number(contribution.cycle) === cycle.id)
                .sort(
                    (a, b) =>
                        new Date(b.paidAtUtc ?? 0).getTime() -
                        new Date(a.paidAtUtc ?? 0).getTime(),
                ),
        }));

    // Keep contributions whose cycle is not in the cycle list.
    const unknownContributions = contributions.filter(
        (contribution) =>
            !cycles.some(
                (cycle) => cycle.id === Number(contribution.cycle),
            ),
    );

    if (unknownContributions.length > 0) {
        groups.push({
            cycle: null,
            contributions: unknownContributions,
        });
    }

    return groups;
}