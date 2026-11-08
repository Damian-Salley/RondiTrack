// Refresh access tokens two minutes before they expire.
const REFRESH_BEFORE_EXPIRY_MS = 2 * 60 * 1000;

// Read the expiration time from a JWT access token.
//
// We only inspect the token's expiry here.
// The backend remains responsible for verifying its
// signature, validity, and permissions.
export function getAccessTokenExpiry(
    accessToken: string
): number | null {
    try {
        // A JWT contains three parts:
        // header.payload.signature
        const parts = accessToken.split(".");

        // Reject values that do not have the expected JWT structure.
        if (parts.length !== 3) {
            return null;
        }

        // Decode the payload from Base64URL into JSON.
        const payload: unknown = JSON.parse(
            Buffer.from(parts[1], "base64url").toString("utf8")
        );

        // The payload must be an object containing an expiry value.
        if (
            typeof payload !== "object" ||
            payload === null ||
            !("exp" in payload)
        ) {
            return null;
        }

        const expiry = payload.exp;

        // JWT expiration must be a positive, finite number.
        if (
            typeof expiry !== "number" ||
            !Number.isFinite(expiry) ||
            expiry <= 0
        ) {
            return null;
        }

        // JWT expiry is measured in seconds since 1970.
        // JavaScript Date.now() uses milliseconds.
        const expiryMilliseconds = expiry * 1000;

        // Reject timestamps that cannot be represented safely.
        if (!Number.isSafeInteger(expiryMilliseconds)) {
            return null;
        }

        return expiryMilliseconds;
    } catch {
        // Invalid JWT encoding or JSON means we cannot
        // determine the token's expiration.
        return null;
    }
}

// Determine whether the access token needs refreshing.
export function shouldRefreshAccessToken(
    accessToken: string
): boolean {
    const expiry = getAccessTokenExpiry(accessToken);

    // If the expiry cannot be determined, request a refresh
    // rather than assuming the token is valid.
    if (expiry === null) {
        return true;
    }

    // Refresh when fewer than two minutes remain.
    return expiry - Date.now() < REFRESH_BEFORE_EXPIRY_MS;
}