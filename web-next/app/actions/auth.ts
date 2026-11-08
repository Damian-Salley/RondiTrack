"use server";

import { redirect } from "next/navigation";

import {
    createSession,
    deleteSession,
    getSession,
} from "@/lib/session";

import { safeNext } from "@/lib/safe-next";

// Backend API address configured in the environment variables.
const baseUrl = process.env.API_BASE_URL;

// Represents the result returned to the login form.
export type LoginState = {
    error: string;
};

// Authenticate a user and create an encrypted application session.
//
// Both login actions use this helper so authentication behaviour
// remains consistent throughout the application.
async function authenticate(
    email: string,
    password: string
): Promise<string | null> {
    // Reject missing or obviously invalid credentials.
    if (!email.includes("@") || !password) {
        return "Enter a valid email address and password.";
    }

    try {
        // Send the credentials to the ASP.NET Core authentication API.
        const response = await fetch(
            `${baseUrl}/api/v1/auth/login`,
            {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify({
                    email,
                    password,
                }),
                cache: "no-store",
            }
        );

        // Do not reveal which credential was incorrect.
        if (!response.ok) {
            return "Invalid email or password.";
        }

        // Read the response without assuming its structure is valid.
        const data: unknown = await response.json();

        // The backend must return a non-empty access token.
        if (
            typeof data !== "object" ||
            data === null ||
            !("accessToken" in data) ||
            typeof data.accessToken !== "string" ||
            data.accessToken.length === 0
        ) {
            return "Unable to create a session.";
        }

        // Find the refresh token in the backend's Set-Cookie headers.
        const refreshCookie = response.headers
            .getSetCookie()
            .find((cookie) =>
                cookie.startsWith("refreshToken=")
            );

        // Extract the refresh token without its cookie attributes.
        const refreshToken = refreshCookie
            ?.split(";")[0]
            .substring("refreshToken=".length);

        // Both tokens are required to create a session.
        if (!refreshToken) {
            return "Unable to create a session.";
        }

        // Encrypt the tokens and store them in the session cookie.
        await createSession({
            accessToken: data.accessToken,
            refreshToken,
        });

        // Null indicates successful authentication.
        return null;
    } catch {
        // Handle backend or network failures without exposing details.
        return "Unable to connect. Please try again.";
    }
}

// Handles login submissions using React's useActionState.
export async function loginWithState(
    _previousState: LoginState,
    formData: FormData
): Promise<LoginState> {
    // Read the submitted credentials.
    const email = String(formData.get("email") ?? "").trim();
    const password = String(formData.get("password") ?? "");

    // Authenticate the user.
    const error = await authenticate(email, password);

    // Display authentication errors without redirecting.
    if (error) {
        return { error };
    }

    // Redirect after the session has been created.
    //
    // Validate the destination because hidden form inputs
    // can be modified by the browser.
    //
    // Keep redirect outside try/catch because Next.js
    // implements redirects by throwing internally.
    redirect(safeNext(formData.get("next")));
}

// Existing login action used elsewhere in the application.
//
// Keep this export to avoid breaking existing forms or callers.
export async function login(
    formData: FormData
): Promise<void> {
    const email = String(formData.get("email") ?? "").trim();
    const password = String(formData.get("password") ?? "");

    // Reuse the same authentication logic as loginWithState.
    const error = await authenticate(email, password);

    if (error) {
        // Preserve the existing login action's redirect behaviour.
        redirect("/login?error=1");
    }

    // Only redirect to a safe internal application destination.
    redirect(safeNext(formData.get("next")));
}

// Log the user out of the backend and the Next.js application.
export async function logout(): Promise<void> {
    // Read the session so the backend can invalidate its refresh token.
    const session = await getSession();

    try {
        if (session) {
            // Ask the backend to invalidate the refresh token.
            await fetch(
                `${baseUrl}/api/v1/auth/logout`,
                {
                    method: "POST",
                    headers: {
                        Cookie: `refreshToken=${session.refreshToken}`,
                    },
                    cache: "no-store",
                }
            );
        }
    } catch {
        // A network failure must not prevent local logout.
        //
        // The backend refresh token might remain valid until
        // it expires, but the browser session will be removed.
    } finally {
        // Always remove the local session, even if the backend fails.
        await deleteSession();
    }

    // Redirect after the session has been deleted.
    redirect("/login");
}