"use client";

import { useQueryStates } from "nuqs";
import {
    contributionFilterParsers,
} from "@/lib/contribution-filters";

import type { StokvelMember } from "@/lib/api";

// Members are loaded by the Server Component and passed to this component.
type Props = {
    members: StokvelMember[];
};

export default function ContributionFilters({ members }: Props) {
    // Keep the selected filters synchronized with the URL.
    const [filters, setFilters] = useQueryStates(
        contributionFilterParsers,
        {
            // Refresh the Server Component when filters change.
            shallow: false,

            // Replace the current URL rather than adding a history entry
            // for every filter adjustment.
            history: "replace",
        }
    );

    return (
        <div className="mt-6 flex flex-wrap gap-4">
            {/* Filter contributions by member. */}
            <label className="flex flex-col gap-1">
                <span>Member</span>

                <select
                    className="rounded border bg-black p-2"
                    value={filters.member}
                    onChange={(event) =>
                        void setFilters({
                            member: Number(event.target.value),
                        })
                    }
                >
                    <option value={0}>All members</option>

                    {members.map((member) => (
                        <option key={member.id} value={member.id}>
                            {member.firstName} {member.lastName}
                        </option>
                    ))}
                </select>
            </label>

            {/* Filter from the beginning of a South African calendar day. */}
            <label className="flex flex-col gap-1">
                <span>From date</span>

                <input
                    type="date"
                    className="rounded border bg-black p-2"
                    value={filters.from}
                    onChange={(event) =>
                        void setFilters({
                            from: event.target.value,
                        })
                    }
                />
            </label>

            {/* Include contributions through the selected South African day. */}
            <label className="flex flex-col gap-1">
                <span>To date</span>

                <input
                    type="date"
                    className="rounded border bg-black p-2"
                    value={filters.to}
                    onChange={(event) =>
                        void setFilters({
                            to: event.target.value,
                        })
                    }
                />
            </label>

            {/* Remove all selected filters. */}
            <button
                type="button"
                className="self-end rounded border px-4 py-2"
                onClick={() =>
                    void setFilters({
                        member: 0,
                        from: "",
                        to: "",
                    })
                }
            >
                Reset filters
            </button>
        </div>
    );
}