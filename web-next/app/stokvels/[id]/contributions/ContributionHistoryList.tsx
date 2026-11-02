"use client";

import { useEffect, useRef } from "react";
import { useInfiniteQuery } from "@tanstack/react-query";

import type {
    Contribution,
    ContributionsPage,
} from "@/lib/api";

type Props = {
    stokvelId: number;
    member: number;
    from?: string;
    to?: string;
    initialPage: ContributionsPage;
};

// Display a contribution using South African local time.
function ContributionRow({
    contribution,
}: {
    contribution: Contribution;
}) {
    return (
        <div className="rounded-xl border border-gray-700 p-4">
            <p className="font-semibold">
                {contribution.memberName}
            </p>

            <p className="text-gray-400">
                Cycle {contribution.cycleNumber}
            </p>

            <p className="text-green-400">
                R{(contribution.amountCents / 100).toFixed(2)}
            </p>

            <p className="text-sm text-gray-400">
                {contribution.paidAtUtc
                    ? new Date(contribution.paidAtUtc).toLocaleString(
                          "en-ZA",
                          { timeZone: "Africa/Johannesburg" }
                      )
                    : "Payment date unavailable"}
            </p>
        </div>
    );
}

// Load page one from the server, then request additional pages from the BFF.
export default function ContributionHistoryList({
    stokvelId,
    member,
    from,
    to,
    initialPage,
}: Props) {
    const loadMoreRef = useRef<HTMLDivElement>(null);

    const {
        data,
        fetchNextPage,
        hasNextPage,
        isFetchingNextPage,
        isError,
    } = useInfiniteQuery({
        // Changing filters creates a different query and discards old pages.
        queryKey: ["contribution-history", stokvelId, member, from, to],

        // The first page has no pagination token.
        initialPageParam: null as string | null,

        // Use the server-rendered first page immediately.
        initialData: {
            pages: [initialPage],
            pageParams: [null],
        },

        // Retrieve additional pages through our authenticated BFF.
        queryFn: async ({ pageParam }): Promise<ContributionsPage> => {
            const params = new URLSearchParams();

            if (member > 0) {
                params.set("userId", String(member));
            }

            if (from) {
                params.set("from", from);
            }

            if (to) {
                params.set("to", to);
            }

            if (pageParam) {
                params.set("pageToken", pageParam);
            }

            params.set("pageSize", "20");

            const response = await fetch(
                `/bff/stokvels/${stokvelId}/contributions?${params}`
            );

            if (!response.ok) {
                throw new Error("Failed to load contributions.");
            }

            return (await response.json()) as ContributionsPage;
        },

        // An absent token means there are no additional pages.
        getNextPageParam: (lastPage) =>
            lastPage.nextPageToken || undefined,
    });

    // Observe the bottom of the list and load another page when visible.
    useEffect(() => {
        const element = loadMoreRef.current;

        if (!element || !hasNextPage) {
            return;
        }

        const observer = new IntersectionObserver(
            ([entry]) => {
                if (entry.isIntersecting && !isFetchingNextPage) {
                    void fetchNextPage();
                }
            },
            { rootMargin: "200px" }
        );

        observer.observe(element);

        return () => observer.disconnect();
    }, [fetchNextPage, hasNextPage, isFetchingNextPage]);

    const contributions =
        data?.pages.flatMap((page) => page.items) ?? [];

    return (
        <section className="mt-6">
            {/* Show an empty state when no contributions match. */}
            {contributions.length === 0 ? (
                <p className="text-gray-400">
                    No contributions match your selected filters.
                </p>
            ) : (
                <div className="space-y-4">
                    {contributions.map((contribution, index) => (
                        <ContributionRow
                            key={`${contribution.userId}-${contribution.cycle}-${index}`}
                            contribution={contribution}
                        />
                    ))}
                </div>
            )}

            {/* The observer triggers pagination near the bottom. */}
            <div ref={loadMoreRef} />

            {/* Provide a keyboard-accessible alternative to scrolling. */}
            {hasNextPage && (
                <button
                    type="button"
                    disabled={isFetchingNextPage}
                    onClick={() => void fetchNextPage()}
                    className="mt-4 rounded border px-4 py-2 disabled:opacity-50"
                >
                    {isFetchingNextPage ? "Loading..." : "Load more"}
                </button>
            )}

            {/* Announce the number of loaded contributions. */}
            <p role="status" className="mt-4 text-sm text-gray-400">
                Showing {contributions.length} contributions.
                {!hasNextPage && " All contributions loaded."}
            </p>

            {/* Display pagination errors without removing loaded results. */}
            {isError && (
                <p role="alert" className="mt-2 text-red-400">
                    Could not load more contributions.
                </p>
            )}
        </section>
    );
}