"use server";

import { redirect } from "next/navigation";
import {
    createSession,
    deleteSession,
    getSession
} from "@/lib/session";

import { safeNext } from "@/lib/safe-next";

// Backend API address configured in the environment variables.
const baseUrl = process.env.API_BASE_URL;

// Represents the result returned to the login form.
export type LoginState = {
    error: string;
};

// Handles login submissions that use React's useActionState.
export async function loginWithState(
    _previousState: LoginState,
    formData: FormData
): Promise<LoginState> {

    // Read the email and password submitted by the user.
    const email = String(formData.get("email") ?? "").trim();
    const password = String(formData.get("password") ?? "");

    // Reject missing or obviously invalid credentials.
    if (!email.includes("@") || !password) {
        return {
            error: "Enter a valid email address and password."
        };
    }

    try {
        // Send the credentials to the ASP.NET Core authentication API.
        const response = await fetch(
            `${baseUrl}/api/v1/auth/login`,
            {
                method: "POST",
                headers: {
                    "Content-Type": "application/json"
                },
                body: JSON.stringify({
                    email,
                    password
                }),
                cache: "no-store"
            }
        );

        // Return a friendly message if authentication fails.
        if (!response.ok) {
            return {
                error: "Invalid email or password."
            };
        }

        // Read the access token returned by the backend.
        const data = (await response.json()) as {
            accessToken: string;
        };

        // Find the refresh token in the backend's Set-Cookie headers.
        const refreshCookie = response.headers
            .getSetCookie()
            .find(cookie => cookie.startsWith("refreshToken="));

        // Extract the refresh token value from the cookie.
        const refreshToken = refreshCookie
            ?.split(";")[0]
            .substring("refreshToken=".length);

        // Both tokens are required to establish a session.
        if (!refreshToken) {
            return {
                error: "Unable to create a session."
            };
        }

        // Store the tokens in the application's encrypted session cookie.
        await createSession({
            accessToken: data.accessToken,
            refreshToken
        });

    } catch {
        // Handle connection failures without exposing backend details.
        return {
            error: "Unable to connect. Please try again."
        };
    }

    // Redirect after the session has been created.
    // Validate the destination again because form inputs can be modified.
    // Keep redirect outside try/catch because Next.js uses a thrown redirect.
    redirect(safeNext(formData.get("next")));
}

// Existing login action used by other parts of the application.
export async function login(formData: FormData) {

    // Read the submitted credentials.
    const email = String(formData.get("email") ?? "");
    const password = String(formData.get("password") ?? "");

    // Request authentication from the backend.
    const response = await fetch(
        `${baseUrl}/api/v1/auth/login`,
        {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                email,
                password
            }),
            cache: "no-store"
        }
    );

    // Return to the login page if authentication fails.
    if (!response.ok) {
        redirect("/login?error=1");
    }

    // Read the access token.
    const data = (await response.json()) as {
        accessToken: string;
    };

    // Retrieve the refresh token from the response cookie.
    const refreshCookie = response.headers
        .getSetCookie()
        .find(cookie => cookie.startsWith("refreshToken="));

    const refreshToken = refreshCookie
        ?.split(";")[0]
        .substring("refreshToken=".length);

    // A missing refresh token means the session cannot be created.
    if (!refreshToken) {
        throw new Error("Backend did not return a refresh token.");
    }

    // Create the encrypted application session.
    await createSession({
        accessToken: data.accessToken,
        refreshToken
    });

    // Redirect only to a validated internal application path.
    redirect(safeNext(formData.get("next")));
}

// Logs the user out of the backend and the Next.js application.
export async function logout() {

    // Read the current session to obtain the refresh token.
    const session = await getSession();

    try {
        if (session) {

            // Ask the backend to invalidate the refresh token.
            await fetch(
                `${baseUrl}/api/v1/auth/logout`,
                {
                    method: "POST",
                    headers: {
                        Cookie: `refreshToken=${session.refreshToken}`
                    },
                    cache: "no-store"
                }
            );
        }

    } finally {

        // Always clear the local session, even if the backend is unavailable.
        await deleteSession();
    }

    // Return the user to the login page.
    redirect("/login");
}