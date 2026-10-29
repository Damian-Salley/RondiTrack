import Link from "next/link";
import { getHomeStokvels } from "@/lib/api";
import { getTreasurerStokvels } from "@/lib/roles";

export default async function TreasurerDashboard() {
    const stokvels = getTreasurerStokvels(await getHomeStokvels());

    return (
        <main>
            <h1>Treasurer Dashboard</h1>

            <Link href="/stokvels">Back to my stokvels</Link>

            {stokvels.length === 0 ? (
                <p>You are not a Treasurer of any stokvels.</p>
            ) : (
                <ul>
                    {stokvels.map((stokvel) => (
                        <li key={stokvel.stokvelId}>
                            <h2>{stokvel.stokvelName}</h2>

                            <p>Current cycle: {stokvel.currentCycle ?? "None"}</p>

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