import Link from "next/link";
import { getHomeStokvels } from "@/lib/api";

export default async function MemberDashboard() {
    const stokvels = await getHomeStokvels();

    return (
        <main>
            <h1>Member Dashboard</h1>

            <Link href="/stokvels">Back to my stokvels</Link>

            {stokvels.length === 0 ? (
                <p>You are not a member of any stokvels yet.</p>
            ) : (
                <ul>
                    {stokvels.map((stokvel) => (
                        <li key={stokvel.stokvelId}>
                            <h2>{stokvel.stokvelName}</h2>

                            <p>Role: {stokvel.role}</p>
                            <p>Current cycle: {stokvel.currentCycle ?? "None"}</p>
                            <p>Cycle status: {stokvel.cycleStatus ?? "None"}</p>

                            <p>
                                Paid this cycle: R
                                {(stokvel.paidThisCycle / 100).toFixed(2)}
                            </p>

                            <p>
                                Still owes: R
                                {(stokvel.stillOwes / 100).toFixed(2)}
                            </p>

                            <p>
                                Arrears: R
                                {(stokvel.arrearsCents / 100).toFixed(2)}
                            </p>

                            <p>
                                Next payout:{" "}
                                {stokvel.nextPayoutDate
                                    ? new Date(stokvel.nextPayoutDate).toLocaleDateString("en-ZA")
                                    : "Not scheduled"}
                            </p>

                            <Link href={`/stokvels/${stokvel.stokvelId}`}>
                                View stokvel
                            </Link>
                        </li>
                    ))}
                </ul>
            )}
        </main>
    );
}