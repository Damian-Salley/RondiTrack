// Read the expiry time from a JWT access token.
// We only inspect the expiry here; the backend remains responsible
// for verifying the token's signature and permissions.
export function getAccessTokenExpiry(accessToken: string): number | null {
    try {
        const parts = accessToken.split(".");

        if (parts.length !== 3) {
            return null;
        }

        const payload = JSON.parse(
            Buffer.from(parts[1], "base64url").toString("utf8")
        ) as { exp?: unknown };

        if (typeof payload.exp !== "number") {
            return null;
        }

        // JWT expiry is measured in seconds since 1970.
        // JavaScript Date.now() uses milliseconds.
        return payload.exp * 1000;
    } catch {
        return null;
    }
}

// Refresh shortly before the access token expires.
export function shouldRefreshAccessToken(accessToken: string): boolean {
    const expiry = getAccessTokenExpiry(accessToken);

    if (expiry === null) {
        return true;
    }

    // Refresh when fewer than 2 minutes remain.
    return expiry - Date.now() < 2 * 60 * 1000;
}