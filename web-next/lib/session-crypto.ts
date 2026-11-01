import { EncryptJWT, jwtDecrypt } from "jose";

export type Session = {
    accessToken: string;
    refreshToken: string;
};

const SESSION_MAX_AGE = 60 * 60 * 24 * 7;

function getSecret(): Buffer {
    const configuredSecret = process.env.SESSION_SECRET;

    if (!configuredSecret) {
        throw new Error("SESSION_SECRET is not configured.");
    }

    const secret = Buffer.from(configuredSecret, "base64url");

    if (secret.length !== 32) {
        throw new Error("SESSION_SECRET must decode to exactly 32 bytes.");
    }

    return secret;
}

export async function encryptSession(session: Session): Promise<string> {
    return new EncryptJWT(session)
        .setProtectedHeader({ alg: "dir", enc: "A256GCM" })
        .setIssuedAt()
        .setExpirationTime("7d")
        .encrypt(getSecret());
}

export async function decryptSession(
    encrypted: string | undefined
): Promise<Session | null> {
    if (!encrypted) {
        return null;
    }

    try {
        const { payload } = await jwtDecrypt(encrypted, getSecret());

        if (
            typeof payload.accessToken !== "string" ||
            typeof payload.refreshToken !== "string"
        ) {
            return null;
        }

        return {
            accessToken: payload.accessToken,
            refreshToken: payload.refreshToken,
        };
    } catch {
        return null;
    }
}

export const sessionCookieOptions = {
    httpOnly: true,
    secure: process.env.NODE_ENV === "production",
    sameSite: "lax" as const,
    path: "/",
    maxAge: SESSION_MAX_AGE,
};