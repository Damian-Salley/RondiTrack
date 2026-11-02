import Link from "next/link";
import { Suspense } from "react";

import { getStokvels } from "@/lib/api";
import { logout } from "@/app/actions/auth";

import PinStokvelButton from "@/app/components/PinStokvelButton";
import PinnedStokvelSidebar from "@/app/components/PinnedStokvelSidebar";
import SignOutButton from "@/app/components/SignOutButton";
import SyncPinnedStokvels from "@/app/components/SyncPinnedStokvels";

export default function StokvelsPage() {
    return (
        <main className="mx-auto max-w-6xl p-6">
            <div className="mb-8 flex flex-wrap items-center justify-between gap-4">
                <div>
                    <h1 className="text-3xl font-bold">
                        My Stokvels
                    </h1>

                    <p className="mt-2 text-gray-500">
                        View and manage your stokvel memberships.
                    </p>
                </div>

                <form action={logout}>
                    <SignOutButton />
                </form>
            </div>

            <div className="mb-8 flex flex-wrap gap-3">
                <Link
                    href="/dashboard/member"
                    className="rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700"
                >
                    Member Dashboard
                </Link>

                <Link
                    href="/dashboard/treasurer"
                    className="rounded-lg border border-gray-600 px-4 py-2 text-sm font-medium hover:bg-gray-800"
                >
                    Treasurer Dashboard
                </Link>

                <Link
                    href="/activity"
                    className="rounded-lg border border-gray-600 px-4 py-2 text-sm font-medium hover:bg-gray-800"
                >
                    Activity Feed
                </Link>
            </div>

            <PinnedStokvelSidebar />

            <Suspense fallback={<StokvelsLoading />}>
                <StokvelsContent />
            </Suspense>
        </main>
    );
}

async function StokvelsContent() {
    const stokvels = await getStokvels();

    const pinSync = (
        <SyncPinnedStokvels
            stokvels={stokvels.map((stokvel) => ({
                id: stokvel.id,
                name: stokvel.name,
            }))}
        />
    );

    if (stokvels.length === 0) {
        return (
            <>
                {pinSync}

                <div className="rounded-xl border border-gray-700 p-6">
                    <p>
                        You are not a member of any stokvels yet.
                    </p>
                </div>
            </>
        );
    }

    return (
        <>
            {pinSync}

            <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
                {stokvels.map((stokvel) => (
                    <div
                        key={stokvel.id}
                        className="rounded-xl border border-gray-700 p-6 shadow-sm"
                    >
                        <h2 className="mb-4 text-xl font-semibold">
                            {stokvel.name}
                        </h2>

                        <div className="mb-4">
                            <PinStokvelButton
                                id={stokvel.id}
                                name={stokvel.name}
                            />
                        </div>

                        <div className="space-y-4">
                            <div className="rounded-lg bg-green-500/10 p-4">
                                <p className="text-sm text-gray-400">
                                    Contribution amount
                                </p>

                                <p className="mt-1 text-2xl font-semibold text-green-400">
                                    R{stokvel.contributionAmount}
                                </p>
                            </div>

                            <div className="rounded-lg border border-gray-700 p-4">
                                <p className="text-sm text-gray-400">
                                    Total members
                                </p>

                                <p className="mt-1 text-xl font-semibold">
                                    {stokvel.memberCount}
                                </p>
                            </div>
                        </div>

                        <Link
                            href={`/stokvels/${stokvel.id}`}
                            className="mt-6 inline-block rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700"
                        >
                            View stokvel →
                        </Link>
                    </div>
                ))}
            </div>
        </>
    );
}

function StokvelsLoading() {
    return (
        <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
            {[1, 2, 3].map((item) => (
                <div
                    key={item}
                    className="h-64 animate-pulse rounded-xl border border-gray-700 bg-gray-800/30"
                />
            ))}
        </div>
    );
}