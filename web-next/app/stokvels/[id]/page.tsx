import Link from "next/link";
import { notFound } from "next/navigation";
import { getStokvel } from "@/lib/api";

export default async function StokvelDetailsPage({
    params,
}: {
    params: Promise<{ id: string }>;
}) {
    const { id } = await params;
    const stokvelId = Number(id);

    if (!Number.isInteger(stokvelId) || stokvelId <= 0) {
        notFound();
    }

    const stokvel = await getStokvel(stokvelId);

    if (!stokvel) {
        notFound();
    }

    return (
        <main>
            <Link href="/stokvels">Back to my stokvels</Link>

            <h1>{stokvel.name}</h1>

            <p>Stokvel ID: {stokvel.id}</p>
            <p>Contribution amount: R{stokvel.contributionAmount}</p>
            <p>Members: {stokvel.memberCount}</p>
        </main>
    );
}