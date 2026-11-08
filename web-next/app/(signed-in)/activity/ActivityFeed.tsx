"use client";

import { useEffect, useRef } from "react";
import { useInfiniteQuery } from "@tanstack/react-query";

import type {
    Contribution,
    ContributionsPage,
} from "@/lib/api";

type Props = {
    initialPage: ContributionsPage;
};

// Display one activity entry using South African local time.
function ActivityRow({
    contribution,
}: {
    contribution: Contribution;
}) {
    return (
        <article className="rounded-xl border border-gray-700 p-4">
            <p className="font-semibold">
                {contribution.memberName}
            </p>

            <p className="text-sm text-gray-400">
                Stokvel #{contribution.stokvelId}
                {" · "}
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
        </article>
    );
}

// Server-render page one; load later pages through the authenticated BFF.
export default function ActivityFeed({ initialPage }: Props) {
    const loadMoreRef = useRef<HTMLDivElement>(null);

    const {
        data,
        fetchNextPage,
        hasNextPage,
        isFetchingNextPage,
        isError,
    } = useInfiniteQuery({
        queryKey: ["activity"],

        // The first page starts without a continuation token.
        initialPageParam: null as string | null,

        // Reuse the page already fetched on the server.
        initialData: {
            pages: [initialPage],
            pageParams: [null],
        },

        // Fetch additional pages without exposing the access token.
        queryFn: async ({ pageParam }): Promise<ContributionsPage> => {
            const params = new URLSearchParams({
                pageSize: "20",
            });

            if (pageParam) {
                params.set("pageToken", pageParam);
            }

            const response = await fetch(
                `/bff/me/activity?${params}`
            );

            if (!response.ok) {
                throw new Error("Failed to load activity.");
            }

            return (await response.json()) as ContributionsPage;
        },

        // Stop pagination when the backend returns no continuation token.
        getNextPageParam: (lastPage) =>
            lastPage.nextPageToken || undefined,
    });

    // Automatically fetch another page when the bottom becomes visible.
    useEffect(() => {
        const element = loadMoreRef.current;

        if (!element || !hasNextPage || isError) {
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
    }, [fetchNextPage, hasNextPage, isFetchingNextPage, isError]);

    const contributions =
        data?.pages.flatMap((page) => page.items) ?? [];

    return (
        <section>
            {contributions.length === 0 ? (
                <p className="text-gray-400">
                    No recent contribution activity.
                </p>
            ) : (
                <div className="space-y-4">
                    {contributions.map((contribution) => (
                        <ActivityRow
                            key={contribution.id}
                            contribution={contribution}
                        />
                    ))}
                </div>
            )}

            {/* Triggers automatic loading near the bottom of the list. */}
            <div ref={loadMoreRef} />

            {/* Keyboard-accessible alternative to automatic scrolling. */}
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

            {/* Announce the number of contributions loaded. */}
            <p role="status" className="mt-4 text-sm text-gray-400">
                Showing {contributions.length} contributions.
                {!hasNextPage && !isError && " All activity loaded."}
            </p>

            {/* Keep existing entries visible if pagination fails. */}
            {isError && (
                <p role="alert" className="mt-2 text-red-400">
                    Could not load activity. Try loading again.
                </p>
            )}
        </section>
    );
}