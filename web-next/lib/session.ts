import "server-only";
import { cookies } from "next/headers";
import { EncryptJWT, jwtDecrypt } from "jose";

const secret = Buffer.from(process.env.SESSION_SECRET!, "base64url");

export type Session = {
    accessToken: string;
    refreshToken: string;
};

export async function createSession(session: Session) {
    const encrypted = await new EncryptJWT(session)
        .setProtectedHeader({ alg: "dir", enc: "A256GCM" })
        .setIssuedAt()
        .setExpirationTime("7d")
        .encrypt(secret);

    (await cookies()).set("session", encrypted, {
        httpOnly: true,
        secure: process.env.NODE_ENV === "production",
        sameSite: "lax",
        path: "/",
        maxAge: 60 * 60 * 24 * 7,
    });
}

export async function getSession(): Promise<Session | null> {
    const encrypted = (await cookies()).get("session")?.value;

    if (!encrypted) return null;

    try {
        const { payload } = await jwtDecrypt(encrypted, secret);

        return {
            accessToken: payload.accessToken as string,
            refreshToken: payload.refreshToken as string,
        };
    } catch {
        return null;
    }
}

export async function deleteSession() {
    (await cookies()).delete("session");
}