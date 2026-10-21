import { useState } from "react";
import { ChevronDown } from "lucide-react";
import { Button } from "@/components/ui/button";
import { DueCountdown } from "./DueCountdown";

import {
    Collapsible,
    CollapsibleContent,
    CollapsibleTrigger,
} from "@/components/ui/collapsible";
import {
    groupContributionsByCycle,
    type Cycle,
    type Contribution,
} from "@/lib/groupContributionsByCycle";
import { formatCents, formatDueDate } from "@/utils/formatters";
import { ContributionCard } from "./ContributionCard";
import { StatusBadge } from "./StatusBadge";

interface ContributionListProps {
    cycles: Cycle[];
    contributions: Contribution[];
    stokvelNames: Record<number, string>;
}

export function ContributionList({
    cycles,
    contributions,
    stokvelNames,
}: ContributionListProps) {
    const groups = groupContributionsByCycle(cycles, contributions);

    // Open cycles are expanded initially.
    const [expandedIds, setExpandedIds] = useState<number[]>(
        () =>
            cycles
                .filter((cycle) => cycle.status === "Active" || cycle.status === "Open")
                .map((cycle) => cycle.id),
    );

    const allExpanded = groups.every((group) =>
        expandedIds.includes(group.cycle?.id ?? -1),
    );

    function toggleAll() {
        setExpandedIds(
            allExpanded ? [] : groups.map((group) => group.cycle?.id ?? -1),
        );
    }

    return (
        <div className="space-y-4">
            <Button type="button" variant="outline" onClick={toggleAll}>
                {allExpanded ? "Collapse all" : "Expand all"}
            </Button>

            {groups.map((group) => {
                const id = group.cycle?.id ?? -1;
                const expanded = expandedIds.includes(id);

                return (
                    <Collapsible
                        key={id}
                        open={expanded}
                        onOpenChange={(open) =>
                            setExpandedIds((previous) =>
                                open
                                    ? [...previous, id]
                                    : previous.filter((value) => value !== id),
                            )
                        }
                    >
                        <div className="rounded-lg border p-4">
                            <CollapsibleTrigger asChild>
                                <button
                                    type="button"
                                    aria-expanded={expanded}
                                    className="flex w-full items-center justify-between gap-3 text-left"
                                >
                                    <span className="font-semibold">
                                        {group.cycle
                                            ? `${stokvelNames[group.cycle.id] ?? "Stokvel"} — Cycle ${group.cycle.number}`
                                            : "Unknown cycle"}
                                    </span>

                                    <ChevronDown
                                        aria-hidden="true"
                                        className={`size-4 transition-transform ${expanded ? "rotate-180" : ""
                                            }`}
                                    />
                                </button>
                            </CollapsibleTrigger>

                            {group.cycle && (
                                <div className="mt-3 space-y-2 text-sm">
                                    <StatusBadge
                                        kind="cycle"
                                        status={group.cycle.status}

                                    />
                                    <DueCountdown
                                        dueDate={group.cycle.dueDateUtc}
                                        status={group.cycle.status}
                                    />
                                    <p>
                                        <strong>Due date:</strong>{" "}
                                        {formatDueDate(group.cycle.dueDateUtc)}
                                    </p>

                                    <p>
                                        <strong>Total collected:</strong>{" "}
                                        {formatCents(group.cycle.totalCollectedCents)}
                                    </p>

                                    <p>
                                        <strong>Amount due:</strong>{" "}
                                        {formatCents(group.cycle.amountDueCents)}
                                    </p>
                                </div>
                            )}

                            <CollapsibleContent>
                                <div className="mt-4 grid gap-3">
                                    {group.contributions.length === 0 ? (
                                        <p className="text-muted-foreground text-sm">
                                            No contributions recorded for this cycle.
                                        </p>
                                    ) : (
                                        group.contributions.map((contribution) => (
                                            <ContributionCard
                                                key={contribution.id}
                                                contribution={contribution}
                                            />
                                        ))
                                    )}
                                </div>
                            </CollapsibleContent>
                        </div>
                    </Collapsible>
                );
            })}
        </div>
    );
}