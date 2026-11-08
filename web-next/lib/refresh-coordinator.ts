import "server-only";

import { createHash } from "node:crypto";

import type { Session } from "@/lib/session-crypto";

// A refresh either returns updated tokens or fails.
type RefreshResult = Session | null;

// Store an active refresh or a recently completed successful refresh.
type RefreshEntry = {
    promise: Promise<RefreshResult>;
    expiresAt: number;
};

// Track refresh requests by a hash of their refresh token.
const refreshes = new Map<string, RefreshEntry>();

// Allow requests carrying the previous cookie to reuse
// a successful refresh result for 30 seconds.
const RESULT_REUSE_MS = 30_000;

// Stop waiting if the backend does not respond within 10 seconds.
const REFRESH_TIMEOUT_MS = 10_000;

// Hash the refresh token so it is not stored directly as a map key.
function getRefreshKey(refreshToken: string): string {
    return createHash("sha256")
        .update(refreshToken)
        .digest("hex");
}

// Request a new access token and rotated refresh token.
async function requestRefresh(
    refreshToken: string
): Promise<RefreshResult> {
    const response = await fetch(
        `${process.env.API_BASE_URL}/api/v1/auth/refresh`,
        {
            method: "POST",

            // The backend expects the refresh token in a cookie.
            headers: {
                Cookie: `refreshToken=${refreshToken}`,
            },

            // Never cache authentication requests.
            cache: "no-store",

            // Prevent a stalled backend request from waiting indefinitely.
            signal: AbortSignal.timeout(REFRESH_TIMEOUT_MS),
        }
    );

    // An unsuccessful response means refresh was not completed.
    if (!response.ok) {
        return null;
    }

    // Read the new access token from the response body.
    const data: unknown = await response.json();

    if (
        typeof data !== "object" ||
        data === null ||
        !("accessToken" in data) ||
        typeof data.accessToken !== "string"
    ) {
        return null;
    }

    // The backend rotates the refresh token and sends the
    // replacement in its Set-Cookie response header.
    const refreshCookie = response.headers
        .getSetCookie()
        .find((cookie) => cookie.startsWith("refreshToken="));

    // Extract the new refresh token without cookie attributes.
    const newRefreshToken = refreshCookie
        ?.split(";")[0]
        .substring("refreshToken=".length);

    // Both tokens are required for a valid refreshed session.
    if (!newRefreshToken) {
        return null;
    }

    return {
        accessToken: data.accessToken,
        refreshToken: newRefreshToken,
    };
}

// Coordinate refresh requests that use the same refresh token.
export function coordinatedRefresh(
    refreshToken: string
): Promise<RefreshResult> {
    const key = getRefreshKey(refreshToken);
    const now = Date.now();

    const existing = refreshes.get(key);

    // Reuse an active refresh or a recently completed successful one.
    if (existing && existing.expiresAt > now) {
        return existing.promise;
    }

    // Start a new refresh request.
    //
    // Convert network failures and timeouts into null so callers
    // can handle them as authentication failures.
    const promise = requestRefresh(refreshToken).catch(() => null);

    const entry: RefreshEntry = {
        promise,

        // Keep the entry available while the request is running.
        expiresAt: Number.POSITIVE_INFINITY,
    };

    refreshes.set(key, entry);

    // Update the cache after the refresh finishes.
    void promise.then((result) => {
        // Do not cache failed refresh attempts.
        if (!result) {
            if (refreshes.get(key) === entry) {
                refreshes.delete(key);
            }

            return;
        }

        // A successful result can be reused briefly by requests
        // that still carry the old session cookie.
        entry.expiresAt = Date.now() + RESULT_REUSE_MS;

        // Remove the cached result when the reuse period expires.
        const timer = setTimeout(() => {
            if (refreshes.get(key) === entry) {
                refreshes.delete(key);
            }
        }, RESULT_REUSE_MS);

        // Do not keep the Node.js process alive just for this timer.
        timer.unref();
    });

    return promise;
}