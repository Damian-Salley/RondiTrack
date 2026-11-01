"use server";

import { redirect } from "next/navigation";
import { createSession, deleteSession, getSession } from "@/lib/session";

const baseUrl = process.env.API_BASE_URL;

// Describe the result returned to the login form.
export type LoginState = {
    error: string;
};

// Validate login details and report authentication errors to the form.
export async function loginWithState(
    _previousState: LoginState,
    formData: FormData
): Promise<LoginState> {
    // Read and normalize the submitted email address.
    const email = String(formData.get("email") ?? "").trim();
    const password = String(formData.get("password") ?? "");

    // Reject missing or malformed credentials.
    if (!email.includes("@") || !password) {
        return { error: "Enter a valid email address and password." };
    }

    try {
        // Ask the backend to authenticate the user.
        const response = await fetch(
            `${process.env.API_BASE_URL}/api/v1/auth/login`,
            {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify({ email, password }),
                cache: "no-store",
            }
        );

        // Keep authentication failures visible without redirecting.
        if (!response.ok) {
            return { error: "Invalid email or password." };
        }

        // Read the short-lived access token.
        const data = (await response.json()) as {
            accessToken: string;
        };

        // Extract the refresh token returned by the backend.
        const refreshCookie = response.headers
            .getSetCookie()
            .find((cookie) => cookie.startsWith("refreshToken="));

        const refreshToken = refreshCookie
            ?.split(";")[0]
            .substring("refreshToken=".length);

        if (!refreshToken) {
            return { error: "Unable to create a session." };
        }

        // Store both tokens inside the encrypted HttpOnly session cookie.
        await createSession({
            accessToken: data.accessToken,
            refreshToken,
        });
    } catch {
        // Handle backend connection failures without exposing internal details.
        return { error: "Unable to connect. Please try again." };
    }

    // Redirect outside try/catch because Next.js redirects throw internally.
    redirect("/stokvels");
}

export async function login(formData: FormData) {
    const email = String(formData.get("email") ?? "");
    const password = String(formData.get("password") ?? "");

    const response = await fetch(`${baseUrl}/api/v1/auth/login`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({ email, password }),
        cache: "no-store",
    });

    if (!response.ok) {
        redirect("/login?error=1");
    }

    const data = (await response.json()) as {
        accessToken: string;
    };

    const refreshCookie = response.headers
        .getSetCookie()
        .find((cookie) => cookie.startsWith("refreshToken="));

    const refreshToken = refreshCookie
        ?.split(";")[0]
        .substring("refreshToken=".length);

    if (!refreshToken) {
        throw new Error("Backend did not return a refresh token.");
    }

    await createSession({
        accessToken: data.accessToken,
        refreshToken,
    });

    redirect("/stokvels");
}

export async function logout() {
    const session = await getSession();

    if (session) {
        await fetch(`${baseUrl}/api/v1/auth/logout`, {
            method: "POST",
            headers: {
                Cookie: `refreshToken=${session.refreshToken}`,
            },
            cache: "no-store",
        });
    }

    await deleteSession();

    redirect("/login");
}