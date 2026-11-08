import Link from "next/link";
import { Suspense } from "react";
import { getHomeStokvels } from "@/lib/api";
import { formatDate } from "@/lib/formatters";

export default function MemberDashboard() {
    return (
        <main className="mx-auto max-w-6xl p-6">
            <div className="mb-8">
                <h1 className="text-3xl font-bold">Member Dashboard</h1>
                <p className="mt-2 text-gray-500">
                    View your stokvel memberships and contributions.
                </p>

                <Link
                    href="/stokvels"
                    className="mt-4 inline-block text-blue-500 hover:underline"
                >
                    ← Back to my stokvels
                </Link>
            </div>

            <Suspense fallback={<DashboardLoading />}>
                <MemberDashboardContent />
            </Suspense>
        </main>
    );
}

async function MemberDashboardContent() {
    const stokvels = await getHomeStokvels();

    if (stokvels.length === 0) {
        return (
            <div className="rounded-xl border border-gray-700 p-6">
                You are not a member of any stokvels yet.
            </div>
        );
    }

    return (
        <div className="grid gap-6 md:grid-cols-2">
            {stokvels.map((stokvel) => (
                <div
                    key={stokvel.stokvelId}
                    className="rounded-xl border border-gray-700 p-6 shadow-sm"
                >
                    <div className="mb-4 flex items-center justify-between">
                        <h2 className="text-xl font-semibold">
                            {stokvel.stokvelName}
                        </h2>

                        <span className="rounded-full bg-blue-500/10 px-3 py-1 text-sm text-blue-400">
                            {stokvel.role}
                        </span>
                    </div>

                    <div className="space-y-2 text-sm">
                        <p>Current cycle: {stokvel.currentCycle ?? "None"}</p>
                        <p>Cycle status: {stokvel.cycleStatus ?? "None"}</p>

                        <div className="my-4 grid grid-cols-2 gap-3">
                            <div className="rounded-lg bg-green-500/10 p-3">
                                <p className="text-gray-400">Paid</p>
                                <p className="text-lg font-semibold text-green-400">
                                    R{(stokvel.paidThisCycle / 100).toFixed(2)}
                                </p>
                            </div>

                            <div className="rounded-lg bg-orange-500/10 p-3">
                                <p className="text-gray-400">Still owes</p>
                                <p className="text-lg font-semibold text-orange-400">
                                    R{(stokvel.stillOwes / 100).toFixed(2)}
                                </p>
                            </div>
                        </div>

                        <p>
                            Arrears: R{(stokvel.arrearsCents / 100).toFixed(2)}
                        </p>

                        <p>
                            Next payout:{" "}
                            {stokvel.nextPayoutDate
                                ? formatDate(stokvel.nextPayoutDate)
                                : "Not scheduled"}
                        </p>
                    </div>

                    <Link
                        href={`/stokvels/${stokvel.stokvelId}`}
                        className="mt-6 inline-block rounded-lg bg-blue-600 px-4 py-2 text-sm font-medium text-white hover:bg-blue-700"
                    >
                        View stokvel →
                    </Link>
                </div>
            ))}
        </div>
    );
}

function DashboardLoading() {
    return (
        <div className="grid gap-6 md:grid-cols-2">
            {[1, 2].map((item) => (
                <div
                    key={item}
                    className="h-64 animate-pulse rounded-xl border border-gray-700 bg-gray-800/30"
                />
            ))}
        </div>
    );
}
