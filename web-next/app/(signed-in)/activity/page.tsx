import { Suspense } from "react";
import { apiFetch, type ContributionsPage } from "@/lib/api";
import ActivityFeed from "./ActivityFeed";

// Fetch the first activity page on the server.
async function ActivityContent() {
    const response = await apiFetch(
        "/api/v1/me/activity?pageSize=20"
    );

    if (!response.ok) {
        throw new Error("Failed to load activity.");
    }

    const initialPage =
        (await response.json()) as ContributionsPage;

    return <ActivityFeed initialPage={initialPage} />;
}

// Suspense provides a loading state while the server fetches activity.
export default function ActivityPage() {
    return (
        <main className="mx-auto max-w-4xl p-6">
            <h1 className="mb-6 text-2xl font-bold">
                Activity
            </h1>

            <Suspense fallback={<p>Loading activity...</p>}>
                <ActivityContent />
            </Suspense>
        </main>
    );
}