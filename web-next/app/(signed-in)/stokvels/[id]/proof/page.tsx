import { Suspense } from "react";
import Link from "next/link";

import { workflowRequest } from "@/lib/workflow-api";
import Feedback from "@/app/components/Feedback";
import ProofForm from "./ProofForm";

// Next.js provides the stokvel ID through the route
// and the cycle/member IDs through URL query parameters.
type Props = {
    params: Promise<{
        id: string;
    }>;

    searchParams: Promise<{
        cycle?: string;
        member?: string;
    }>;
};

// Main page for viewing and managing proof of payment.
export default function ProofPage(props: Props) {
    return (
        <main className="mx-auto max-w-xl space-y-4 p-6">

            {/* Main heading for the proof-of-payment page. */}
            <h1 className="text-2xl font-bold">
                Proof of payment
            </h1>

            {/* Show a loading message while contribution details are retrieved. */}
            <Suspense fallback={<p>Loading contribution...</p>}>
                <ProofContent {...props} />
            </Suspense>

        </main>
    );
}

// Loads the contribution's proof details from the backend API.
async function ProofContent({
    params,
    searchParams
}: Props) {

    // Read the route and query parameters together.
    const [{ id }, query] = await Promise.all([
        params,
        searchParams
    ]);

    // Convert the IDs from URL strings into numbers.
    const stokvelId = Number(id);
    const cycleId = Number(query.cycle);
    const memberId = Number(query.member);

    // Reject missing, invalid or non-positive IDs.
    if (
        ![stokvelId, cycleId, memberId].every(
            n => Number.isSafeInteger(n) && n > 0
        )
    ) {
        return <p>Contribution not found.</p>;
    }

    // Request proof information from the ASP.NET Core backend.
    // The backend checks whether the current user has permission
    // to access the contribution's proof details.
    const { result, response } = await workflowRequest(
        `/stokvels/${stokvelId}/cycles/${cycleId}/contributions/${memberId}/proof/details`
    );

    // Display a friendly error if the backend request fails.
    if (!response) {
        return <Feedback result={result} />;
    }

    // Read the proof status and permissions returned by the backend.
    const details = await response.json() as {
        hasProof: boolean;
        canUpload: boolean;
        locked: boolean;
    };

    return (
        <>

            {/* Return to the stokvel details page. */}
            <Link
                className="underline"
                href={`/stokvels/${stokvelId}`}
            >
                Back to stokvel
            </Link>

            {/* Display the proof upload/download interface.
                The backend determines whether uploading is allowed. */}
            <ProofForm
                stokvelId={stokvelId}
                cycleId={cycleId}
                memberId={memberId}
                {...details}
            />

        </>
    );
}