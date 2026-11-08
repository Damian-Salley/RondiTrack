import "server-only";

import { cookies } from "next/headers";

import {
    decryptSession,
    encryptSession,
    sessionCookieOptions,
    type Session,
} from "@/lib/session-crypto";

// Make the Session type available to other server-side files.
export type { Session };

// Create a new authenticated session.
//
// This is normally called after the user successfully logs in.
export async function createSession(
    session: Session
): Promise<void> {
    // Encrypt the access token and refresh token.
    const encrypted = await encryptSession(session);

    // Store the encrypted session in an HTTP-only browser cookie.
    // Security settings are defined in session-crypto.ts.
    (await cookies()).set(
        "session",
        encrypted,
        sessionCookieOptions
    );
}

// Retrieve the current user's authenticated session.
export async function getSession(): Promise<Session | null> {
    // Read the encrypted session cookie from the incoming request.
    const encrypted = (await cookies()).get("session")?.value;

    // Decrypt and validate the session.
    //
    // Returns null if the cookie is missing, invalid, or expired.
    return decryptSession(encrypted);
}

// Delete the current user's session.
//
// This is normally called when the user logs out.
export async function deleteSession(): Promise<void> {
    // Remove the session cookie from the browser.
    (await cookies()).delete("session");
}