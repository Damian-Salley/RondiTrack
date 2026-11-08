import "server-only";

import { redirect } from "next/navigation";
import { getSession } from "@/lib/session";

const baseUrl = process.env.API_BASE_URL;

export type Stokvel = {
    id: number;
    name: string;
    contributionAmount: number;
    memberCount: number;
};

export async function apiFetch(path: string): Promise<Response> {
    const session = await getSession();

    if (!session) {
        redirect("/login");
    }

    const response = await fetch(`${baseUrl}${path}`, {
        headers: {
            Authorization: `Bearer ${session.accessToken}`,
        },
        cache: "no-store",
    });

    if (response.status === 401) {
        redirect("/login");
    }

    return response;
}

export async function getStokvels(): Promise<Stokvel[]> {
    const response = await apiFetch("/api/v1/stokvels");

    if (!response.ok) {
        throw new Error("Failed to load stokvels.");
    }

    return (await response.json()) as Stokvel[];
}

export async function getStokvel(id: number): Promise<Stokvel | null> {
    const response = await apiFetch(`/api/v1/stokvels/${id}`);

    if (response.status === 404) {
        return null;
    }

    if (!response.ok) {
        throw new Error("Failed to load stokvel.");
    }

    return (await response.json()) as Stokvel;
}

export type HomeStokvel = {
    stokvelId: number;
    stokvelName: string;
    role: string;
    currentCycle: number | null;
    cycleStatus: string | null;
    paidThisCycle: number;
    totalCollectedCents?: string;
    stillOwes: number;
    arrearsCents: number;
    nextPayoutDate: string | null;
};

export async function getHomeStokvels(): Promise<HomeStokvel[]> {
    const response = await apiFetch("/api/v1/me/home");

    if (!response.ok) {
        throw new Error("Failed to load dashboard.");
    }

    return (await response.json()) as HomeStokvel[];
}

// Matches the ContributionCycleResponse DTO returned by the C# backend.
export type ContributionCycle = {
    id: number;
    stokvelId: number;
    number: number;
    targetAmount: number;
};

export type StokvelMember = {
    id: number;
    firstName: string;
    lastName: string;
    email: string;
};

export async function getContributionCycles(
    stokvelId: number
): Promise<ContributionCycle[]> {
    const response = await apiFetch("/api/v1/contributioncycles");

    if (!response.ok) {
        throw new Error("Failed to load contribution cycles.");
    }

    const cycles = (await response.json()) as ContributionCycle[];

    return cycles.filter((cycle) => cycle.stokvelId === stokvelId);
}

export async function getStokvelMembers(
    stokvelId: number
): Promise<StokvelMember[]> {
    const response = await apiFetch(
        `/api/v1/stokvels/${stokvelId}/members`
    );

    if (!response.ok) {
        throw new Error("Failed to load stokvel members.");
    }

    return (await response.json()) as StokvelMember[];
}

// Represents a single contribution returned by the backend.
export type Contribution = {
    id: string;
    userId: number;
    stokvelId: number;
    cycle: number;
    cycleNumber: number;
    amountCents: number;
    status: string;
    paidAtUtc: string | null;
    memberName: string;
};

// Represents a paginated list of contributions.
export type ContributionsPage = {
    items: Contribution[];
    nextPageToken: string | null;
};

// Retrieve the first page of recent contributions for a stokvel.
export async function getRecentContributions(
    stokvelId: number
): Promise<ContributionsPage> {

    // Request contributions for the stokvel's active cycle.
    // The backend determines which cycle is currently active.
    const response = await apiFetch(
        `/api/v1/stokvels/${stokvelId}/contributions`
    );

    // Stop if the API request fails.
    if (!response.ok) {
        throw new Error("Failed to load recent contributions.");
    }

    // Return the contributions and pagination information.
    return (await response.json()) as ContributionsPage;
}

// Retrieve a page of contribution history using the selected filters.
export async function getContributionHistory(
    stokvelId: number,
    filters: {
        member: number;
        from?: string;
        to?: string;
        pageToken?: string;
    }
): Promise<ContributionsPage> {

    // Build the query parameters sent to the backend.
    const params = new URLSearchParams();

    // Filter by member only when a specific member is selected.
    if (filters.member > 0) {
        params.set("userId", filters.member.toString());
    }

    // The backend expects UTC timestamps.
    // These dates must already be converted before calling this function.
    if (filters.from) {
        params.set("from", filters.from);
    }

    if (filters.to) {
        params.set("to", filters.to);
    }

    // Request the next page when a pagination token is available.
    if (filters.pageToken) {
        params.set("pageToken", filters.pageToken);
    }

    // Request 20 contributions per page.
    params.set("pageSize", "20");

    // Send the request from the server using our existing API helper.
    const response = await apiFetch(
        `/api/v1/stokvels/${stokvelId}/contributions?${params.toString()}`
    );

    // Stop if the backend rejects the request.
    if (!response.ok) {
        throw new Error("Failed to load contribution history.");
    }

    // Return the contributions and the next-page token.
    return (await response.json()) as ContributionsPage;
}

// Represents the authenticated user returned by GET /api/v1/me.
export interface Me {
    id: number;
    firstName: string;
    lastName: string;
    email: string;
}

// Retrieve the authenticated user's identity from the backend.
export async function getMe(): Promise<Me> {
    // apiFetch automatically includes the user's access token.
    const response = await apiFetch("/api/v1/me");

    // Stop if the backend cannot retrieve the user's identity.
    if (!response.ok) {
        throw new Error("Failed to retrieve the authenticated user.");
    }

    // Convert the JSON response into our Me TypeScript type.
    return (await response.json()) as Me;
}
