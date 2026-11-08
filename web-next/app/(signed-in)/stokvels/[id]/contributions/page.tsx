import Link from "next/link";
import { Suspense } from "react";
import { notFound } from "next/navigation";
import ContributionHistoryList from "./ContributionHistoryList";

import {
    getMe,
    getHomeStokvels,
    getStokvel,
    getStokvelMembers,
    getContributionHistory,
} from "@/lib/api";

import ContributionFilters from "./ContributionFilters";

import {
    loadContributionFilters,
    normalizeContributionFilters,
} from "@/lib/contribution-filters";

import { southAfricanDaysToUtcRange } from "@/lib/south-african-dates";

// Next.js supplies the stokvel ID and URL search parameters.
type PageProps = {
    params: Promise<{ id: string }>;
    searchParams: Promise<{
        [key: string]: string | string[] | undefined;
    }>;
};

// Render the page while the contribution history loads separately.
export default function ContributionHistoryPage({
    params,
    searchParams,
}: PageProps) {
    return (
        <main className="mx-auto max-w-6xl p-6">
            <Suspense fallback={<p>Loading contributions...</p>}>
                <ContributionHistoryContent
                    params={params}
                    searchParams={searchParams}
                />
            </Suspense>
        </main>
    );
}

// Load and display contributions using the filters in the URL.
async function ContributionHistoryContent({
    params,
    searchParams,
}: PageProps) {
    // Read the stokvel ID from the route.
    const { id } = await params;
    const stokvelId = Number(id);

    // Reject invalid stokvel IDs.
    if (!Number.isInteger(stokvelId) || stokvelId <= 0) {
        notFound();
    }

    // Check whether the current user can access this stokvel.
    const stokvel = await getStokvel(stokvelId);
    // Retrieve members for the contribution filter dropdown.
    const members = await getStokvelMembers(stokvelId);

    if (!stokvel) {
        notFound();
    }

    const [me, home] = await Promise.all([getMe(), getHomeStokvels()]);
    const role = home.find(s => s.stokvelId === stokvelId)?.role ?? "Member";

    // Read and validate the contribution filters from the URL.
    const parsedFilters = await loadContributionFilters(searchParams);

    const filters = normalizeContributionFilters(parsedFilters);

    // Check whether the selected calendar dates are in the correct order.
    const invalidDateRange =
        filters.from !== "" &&
        filters.to !== "" &&
        filters.from > filters.to;

    // Only convert dates when the range is valid.
    const utcRange = invalidDateRange
        ? null
        : southAfricanDaysToUtcRange(
            filters.from || undefined,
            filters.to || undefined
        );

    // Avoid requesting contributions when the date range is invalid.
    const page = invalidDateRange
        ? { items: [], nextPageToken: null }
        : await getContributionHistory(stokvelId, {
            member: filters.member,
            from: utcRange?.from,
            to: utcRange?.to,
        });

    return (
        <>
            {/* Navigation back to the stokvel details page. */}
            <Link
                href={`/stokvels/${stokvelId}`}
                className="text-blue-500 hover:underline"
            >
                ← Back to stokvel
            </Link>

            {/* Display the selected stokvel's name. */}
            <h1 className="mt-6 text-3xl font-bold">
                {stokvel.name} — Contribution History
            </h1>

            {/* Allow users to filter contributions by member and date. */}
            <ContributionFilters members={members} />

            {/* Explain why no results are shown for a reversed date range. */}
            {invalidDateRange && (
                <p role="alert" className="mt-4 text-red-400">
                    The From date cannot be later than the To date.
                </p>
            )}

            {/* Display page one and load additional pages when needed. */}
            {
                <ContributionHistoryList
                    key={`${stokvelId}-${filters.member}-${filters.from}-${filters.to}`}
                    stokvelId={stokvelId}
                    member={filters.member}
                    from={utcRange?.from}
                    to={utcRange?.to}
                    initialPage={page}
                    userId={me.id}
                    role={role}
                />
            }

        </>
    );
}