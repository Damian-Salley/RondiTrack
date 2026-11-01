import "server-only";

import { cookies } from "next/headers";

import {
    decryptSession,
    encryptSession,
    sessionCookieOptions,
    type Session,
} from "@/lib/session-crypto";

export type { Session };

export async function createSession(session: Session): Promise<void> {
    const encrypted = await encryptSession(session);

    (await cookies()).set("session", encrypted, sessionCookieOptions);
}

export async function getSession(): Promise<Session | null> {
    const encrypted = (await cookies()).get("session")?.value;

    return decryptSession(encrypted);
}

export async function deleteSession(): Promise<void> {
    (await cookies()).delete("session");
}