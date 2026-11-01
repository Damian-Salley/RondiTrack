import "server-only";

import { createHash } from "node:crypto";

import type { Session } from "@/lib/session-crypto";

type RefreshResult = Session | null;

type RefreshEntry = {
    promise: Promise<RefreshResult>;
    expiresAt: number;
};

const refreshes = new Map<string, RefreshEntry>();

// Allow requests carrying the old cookie to reuse the result briefly.
const RESULT_REUSE_MS = 30_000;

function getRefreshKey(refreshToken: string): string {
    return createHash("sha256")
        .update(refreshToken)
        .digest("hex");
}

async function requestRefresh(
    refreshToken: string
): Promise<RefreshResult> {
    const response = await fetch(
        `${process.env.API_BASE_URL}/api/v1/auth/refresh`,
        {
            method: "POST",
            headers: {
                Cookie: `refreshToken=${refreshToken}`,
            },
            cache: "no-store",
        }
    );

    if (!response.ok) {
        return null;
    }

    const data: unknown = await response.json();

    if (
        typeof data !== "object" ||
        data === null ||
        !("accessToken" in data) ||
        typeof data.accessToken !== "string"
    ) {
        return null;
    }

    const refreshCookie = response.headers
        .getSetCookie()
        .find((cookie) => cookie.startsWith("refreshToken="));

    const newRefreshToken = refreshCookie
        ?.split(";")[0]
        .substring("refreshToken=".length);

    if (!newRefreshToken) {
        return null;
    }

    return {
        accessToken: data.accessToken,
        refreshToken: newRefreshToken,
    };
}

export function coordinatedRefresh(
    refreshToken: string
): Promise<RefreshResult> {
    const key = getRefreshKey(refreshToken);
    const now = Date.now();

    const existing = refreshes.get(key);

    // Reuse an in-progress refresh or its recently completed result.
    if (existing && existing.expiresAt > now) {
        return existing.promise;
    }

    const promise = requestRefresh(refreshToken).catch(() => null);

    const entry: RefreshEntry = {
        promise,
        // Keep the entry valid while the request is running.
        expiresAt: Number.POSITIVE_INFINITY,
    };

    refreshes.set(key, entry);

    void promise.then(() => {
        // Start the reuse window only after the refresh completes.
        entry.expiresAt = Date.now() + RESULT_REUSE_MS;

        const timer = setTimeout(() => {
            if (refreshes.get(key) === entry) {
                refreshes.delete(key);
            }
        }, RESULT_REUSE_MS);

        timer.unref();
    });

    return promise;
}