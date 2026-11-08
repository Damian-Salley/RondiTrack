import { EncryptJWT, jwtDecrypt } from "jose";

// The session contains the tokens needed to authenticate
// requests and refresh an expired access token.
export type Session = {
    accessToken: string;
    refreshToken: string;
};

// Keep the encrypted browser session valid for seven days.
const SESSION_MAX_AGE = 60 * 60 * 24 * 7;

// Read the encryption key from the environment.
function getSecret(): Buffer {
    const configuredSecret = process.env.SESSION_SECRET;

    // Never allow session encryption without a configured secret.
    if (!configuredSecret) {
        throw new Error("SESSION_SECRET is not configured.");
    }

    // The secret must be a Base64URL-encoded 32-byte key.
    const secret = Buffer.from(configuredSecret, "base64url");

    // AES-256-GCM requires a 256-bit (32-byte) encryption key.
    if (secret.length !== 32) {
        throw new Error(
            "SESSION_SECRET must decode to exactly 32 bytes."
        );
    }

    return secret;
}

// Encrypt the session before storing it in a browser cookie.
export async function encryptSession(
    session: Session
): Promise<string> {
    return new EncryptJWT(session)
        // Use direct encryption with AES-256-GCM.
        .setProtectedHeader({
            alg: "dir",
            enc: "A256GCM",
        })

        // Record when the session was created.
        .setIssuedAt()

        // Automatically expire the encrypted session after seven days.
        .setExpirationTime("7d")

        // Encrypt the session using the configured secret.
        .encrypt(getSecret());
}

// Decrypt and validate the session stored in the browser cookie.
export async function decryptSession(
    encrypted: string | undefined
): Promise<Session | null> {
    // A missing cookie means the user has no session.
    if (!encrypted) {
        return null;
    }

    try {
        // Decrypt the session and verify its expiration.
        const { payload } = await jwtDecrypt(
            encrypted,
            getSecret()
        );

        // Both authentication tokens must exist and be strings.
        if (
            typeof payload.accessToken !== "string" ||
            typeof payload.refreshToken !== "string"
        ) {
            return null;
        }

        // Empty tokens cannot be used for authentication.
        if (
            payload.accessToken.length === 0 ||
            payload.refreshToken.length === 0
        ) {
            return null;
        }

        // Return the validated session.
        return {
            accessToken: payload.accessToken,
            refreshToken: payload.refreshToken,
        };
    } catch {
        // Invalid, corrupted, or expired sessions are rejected.
        return null;
    }
}

// Security settings for the encrypted session cookie.
export const sessionCookieOptions = {
    // Prevent browser JavaScript from accessing the session.
    httpOnly: true,

    // Require HTTPS in production.
    secure: process.env.NODE_ENV === "production",

    // Restrict when browsers send cookies on cross-site requests.
    sameSite: "lax" as const,

    // Make the cookie available throughout the application.
    path: "/",

    // Expire the browser cookie after seven days.
    maxAge: SESSION_MAX_AGE,
};