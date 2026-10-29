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