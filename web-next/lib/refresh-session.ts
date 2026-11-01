import "server-only";

import { getSession, createSession, type Session } from "@/lib/session";

// Ask the .NET backend to rotate the refresh token.
export async function refreshSession(): Promise<Session | null> {
    const currentSession = await getSession();

    if (!currentSession) {
        return null;
    }

    try {
        const response = await fetch(
            `${process.env.API_BASE_URL}/api/v1/auth/refresh`,
            {
                method: "POST",
                headers: {
                    Cookie: `refreshToken=${currentSession.refreshToken}`,
                },
                cache: "no-store",
            }
        );

        if (!response.ok) {
            return null;
        }

        const data = (await response.json()) as {
            accessToken: string;
        };

        // The backend returns the replacement refresh token in Set-Cookie.
        const refreshCookie = response.headers
            .getSetCookie()
            .find((cookie) => cookie.startsWith("refreshToken="));

        const newRefreshToken = refreshCookie
            ?.split(";")[0]
            .substring("refreshToken=".length);

        if (!data.accessToken || !newRefreshToken) {
            return null;
        }

        const updatedSession: Session = {
            accessToken: data.accessToken,
            refreshToken: newRefreshToken,
        };

        // Save both replacement tokens in our encrypted session cookie.
        await createSession(updatedSession);

        return updatedSession;
    } catch {
        return null;
    }
}