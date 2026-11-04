import Link from "next/link";

// Import the type describing the paginated API response.
import type { ContributionsPage } from "@/lib/api";

// Receive an already-started request so contributions load concurrently.
export default async function RecentContributions({
    contributionsPromise, userId, role,
}: {
    contributionsPromise: Promise<ContributionsPage>;
    userId: number; role: string;
}) {
    // Wait for the request that the parent component started earlier.
    const page = await contributionsPromise;

    return (
        <section className="mt-6 rounded-xl border border-gray-700 p-6 shadow-sm">
            <h2 className="mb-4 text-xl font-semibold">
                Recent Contributions
            </h2>

            {/* Show a message when the active cycle has no contributions. */}
            {page.items.length === 0 ? (
                <p className="text-gray-400">
                    No contributions recorded for the current cycle.
                </p>
            ) : (
                <div className="space-y-3">
                    {/* Display each contribution in its own card. */}
                    {page.items.map((contribution) => (
                        <div
                            key={contribution.id}
                            className="flex flex-wrap items-center justify-between gap-4 rounded-lg border border-gray-700 p-4"
                        >
                            <div>
                                <p className="font-medium">
                                    {contribution.memberName}
                                </p>

                                <p className="mt-1 text-sm text-gray-400">
                                    Cycle {contribution.cycleNumber}
                                </p>

                                <p className="mt-1 text-sm text-gray-400">
                                    {contribution.paidAtUtc
                                        ? new Date(
                                            contribution.paidAtUtc
                                        ).toLocaleDateString("en-ZA", {
                                            timeZone: "UTC",
                                        })
                                        : "Payment date unavailable"}
                                </p>
                            </div>

                            <div className="text-right">
                                {/* Convert cents into rand for display. */}
                                <p className="text-lg font-semibold">
                                    R{(contribution.amountCents / 100).toFixed(2)}
                                </p>

                                <p className="text-sm text-gray-400">
                                    {contribution.status}
                                </p>
                                {(contribution.userId === userId || role === "Treasurer") && <Link className="text-sm underline" href={`/stokvels/${contribution.stokvelId}/proof?cycle=${contribution.cycle}&member=${contribution.userId}`}>Proof of payment</Link>}
                            </div>
                        </div>
                    ))}
                </div>
            )}
        </section>
    );
}