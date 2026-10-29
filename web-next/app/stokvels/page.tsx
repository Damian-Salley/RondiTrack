import Link from "next/link";
import { getStokvels } from "@/lib/api";
import { logout } from "@/app/actions/auth";

export default async function StokvelsPage() {
    const stokvels = await getStokvels();

    return (
        <main>
            <h1>My Stokvels</h1>

            <form action={logout}>
                <button type="submit">Sign out</button>
            </form>

            {stokvels.length === 0 ? (
                <p>You are not a member of any stokvels yet.</p>
            ) : (
                <ul>
                    {stokvels.map((stokvel) => (
                        <li key={stokvel.id}>
                            <h2>
                                <Link href={`/stokvels/${stokvel.id}`}>
                                    {stokvel.name}
                                </Link>
                            </h2>
                            <p>Contribution amount: R{stokvel.contributionAmount}</p>
                            <p>Members: {stokvel.memberCount}</p>
                        </li>
                    ))}
                </ul>
            )}
        </main>
    );
}