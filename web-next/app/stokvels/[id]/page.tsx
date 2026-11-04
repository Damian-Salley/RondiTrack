import Link from "next/link";
import { Suspense } from "react";
import { notFound } from "next/navigation";
// Import the API functions needed to load stokvel information.
import {
    getMe,
    getStokvel,
    getContributionCycles,
    getStokvelMembers,
    getRecentContributions,
    getHomeStokvels,
} from "@/lib/api";
// Import the Server Component that displays recent contributions.
import RecentContributions from "./RecentContributions";
// Import the client form used to record contributions.
import ContributionForm from "@/app/components/ContributionForm";
import CreateInvite from "@/app/components/CreateInvite";
import CloseCycle from "@/app/components/CloseCycle";
import PinStokvelButton from "@/app/components/PinStokvelButton";



export default function StokvelDetailsPage({
    params,
}: {
    params: Promise<{ id: string }>;
}) {
    return (
        <main className="mx-auto max-w-6xl p-6">
            <Link
                href="/stokvels"
                className="mb-6 inline-block text-blue-500 hover:underline"
            >
                ← Back to my stokvels
            </Link>

            <Suspense fallback={<StokvelLoading />}>
                <StokvelDetailsContent params={params} />
            </Suspense>
        </main>
    );
}

async function StokvelDetailsContent({
    params,
}: {
    params: Promise<{ id: string }>;
}) {
    const { id } = await params;
    const stokvelId = Number(id);

    if (!Number.isInteger(stokvelId) || stokvelId <= 0) {
        notFound();
    }

    // Check access before requesting private stokvel information.
    const stokvel = await getStokvel(stokvelId);

    if (!stokvel) {
        notFound();
    }

    // The user can access this stokvel, so start loading its data.
    const contributionsPromise = getRecentContributions(stokvelId);

    // Fetch the remaining information concurrently.
    const [cycles, members, homeStokvels, me] = await Promise.all([
        getContributionCycles(stokvelId),
        getStokvelMembers(stokvelId),
        getHomeStokvels(),
        getMe(),
    ]);

    // Find this stokvel's dashboard information.
    const homeStokvel = homeStokvels.find(
        (item) => item.stokvelId === stokvelId
    );

    // Match the backend's current cycle number to its cycle details.
    const activeCycle = cycles.find(
        (cycle) =>
            cycle.stokvelId === stokvelId &&
            cycle.number === homeStokvel?.currentCycle
    );

    return (
        <>
            <div className="mb-8">
                <h1 className="text-3xl font-bold">{stokvel.name}</h1>

                <div className="mt-3">
                    <PinStokvelButton
                        id={stokvel.id}
                        name={stokvel.name}
                    />
                </div>

                <p className="mt-2 text-gray-500">
                    View your stokvel information and contributions.
                </p>
            </div>

            <div className="grid gap-6 md:grid-cols-3">
                <div className="rounded-xl border border-gray-700 p-6 shadow-sm">
                    <p className="text-sm text-gray-400">Stokvel ID</p>

                    <p className="mt-2 text-2xl font-semibold">
                        {stokvel.id}
                    </p>
                </div>

                <div className="rounded-xl border border-gray-700 p-6 shadow-sm">
                    <p className="text-sm text-gray-400">
                        Contribution amount
                    </p>

                    <p className="mt-2 text-2xl font-semibold text-green-400">
                        R{stokvel.contributionAmount}
                    </p>
                </div>

                {/* Display the member count returned by the members endpoint. */}
                <div className="rounded-xl border border-gray-700 p-6 shadow-sm">
                    <p className="text-sm text-gray-400">Total members</p>

                    <p className="mt-2 text-2xl font-semibold">
                        {members.length}
                    </p>
                </div>
            </div>
            {/* Display the active contribution cycle, if one exists. */}
            <div className="mt-6 rounded-xl border border-gray-700 p-6 shadow-sm">
                <h2 className="mb-4 text-xl font-semibold">
                    Current Contribution Cycle
                </h2>

                {activeCycle ? (
                    <div className="space-y-2">
                        <p>Cycle number: {activeCycle.number}</p>

                        <p>
                            Status:{" "}
                            <span className="font-medium text-green-400">
                                {homeStokvel?.cycleStatus ?? "Unknown"}
                            </span>
                        </p>

                        <p>Target amount: R{activeCycle.targetAmount}</p>
                    </div>
                ) : (
                    <p className="text-gray-400">
                        No active contribution cycle.
                    </p>
                )}
            </div>

            {homeStokvel?.role === "Treasurer" && <section className="mt-6">
                <CreateInvite stokvelId={stokvelId} />
                {activeCycle && homeStokvel.cycleStatus === "Active" && <CloseCycle cycleId={activeCycle.id} />}
            </section>}

            {/* Only display the form when an active contribution cycle exists. */}
            {activeCycle && homeStokvel?.cycleStatus === "Active" && (
                <section className="mt-6">
                    <ContributionForm
                        // Identify the stokvel receiving the contribution.
                        stokvelId={stokvelId}

                        // Use the actual database cycle ID, not its display number.
                        cycleId={activeCycle.id}

                        // Supply the members for the Treasurer's dropdown.
                        members={members}

                        // Use the signed-in user's role within this stokvel.
                        role={homeStokvel.role}
                    />
                </section>
            )}

            <Link className="mt-6 block underline" href={`/stokvels/${stokvelId}/contributions`}>Contribution history and payment proofs</Link>

            {/* Load recent contributions separately from the main stokvel details. */}
            <Suspense fallback={<ContributionsLoading />}>
                {/* Pass the already-started request to the streamed component. */}
                <RecentContributions contributionsPromise={contributionsPromise} userId={me.id} role={homeStokvel?.role ?? "Member"} />
            </Suspense>
        </>
    );
}

function StokvelLoading() {
    return (
        <div className="space-y-6">
            <div className="h-10 w-64 animate-pulse rounded-lg bg-gray-800" />

            <div className="grid gap-6 md:grid-cols-3">
                {[1, 2, 3].map((item) => (
                    <div
                        key={item}
                        className="h-32 animate-pulse rounded-xl border border-gray-700 bg-gray-800/30"
                    />
                ))}
            </div>
        </div>
    );

}
// Displays a placeholder while recent contributions are loading.
function ContributionsLoading() {
    return (
        <section className="mt-6 rounded-xl border border-gray-700 p-6">
            <h2 className="mb-4 text-xl font-semibold">
                Recent Contributions
            </h2>

            {/* Skeleton cards match the layout of actual contributions. */}
            <div className="space-y-3">
                {[1, 2, 3].map((item) => (
                    <div
                        key={item}
                        className="h-20 animate-pulse rounded-lg bg-gray-800/50"
                    />
                ))}
            </div>
        </section>
    );
}