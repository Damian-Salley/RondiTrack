import type { components } from "../api/schema";

// Use the response type generated from the backend.
type AuthTokenResponse = components["schemas"]["AuthTokenResponse"];

// Read the backend address from the Vite environment.
const baseUrl = import.meta.env.VITE_API_BASE_URL;

// Keep the access token in memory only.
let accessToken: string | null = null;

// Return the current access token.
export function getAccessToken(): string | null {
    return accessToken;
}

// Log in and save the returned access token.
export async function login(
    email: string,
    password: string
): Promise<void> {
    const response = await fetch(`${baseUrl}/api/v1/auth/login`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        credentials: "include",
        body: JSON.stringify({ email, password })
    });

    if (!response.ok) {
        throw new Error(`Login failed: ${response.status}`);
    }

    const data = (await response.json()) as AuthTokenResponse;

    accessToken = data.accessToken;
}

// Share one refresh request between simultaneous callers.
let refreshPromise: Promise<string | null> | null = null;

// Refresh the access token using the HttpOnly cookie.
export function refreshAccessToken(): Promise<string | null> {
    if (refreshPromise) {
        return refreshPromise;
    }

    refreshPromise = (async () => {
        try {
            const response = await fetch(`${baseUrl}/api/v1/auth/refresh`, {
                method: "POST",
                credentials: "include"
            });

            if (!response.ok) {
                accessToken = null;
                return null;
            }

            const data = (await response.json()) as AuthTokenResponse;
            accessToken = data.accessToken;

            return accessToken;
        } catch {
            accessToken = null;
            return null;
        }
    })().finally(() => {
        refreshPromise = null;
    });

    return refreshPromise;
}

// Log out and clear the in-memory access token.
export async function logout(): Promise<void> {
    try {
        await fetch(`${baseUrl}/api/v1/auth/logout`, {
            method: "POST",
            credentials: "include"
        });
    } finally {
        accessToken = null;
    }
}

// Make an authenticated API request.
export async function authFetch(
    input: string,
    init: RequestInit = {}
): Promise<Response> {
    async function sendRequest(token: string | null): Promise<Response> {
        const headers = new Headers(init.headers);

        if (token) {
            headers.set("Authorization", `Bearer ${token}`);
        }

        return fetch(`${baseUrl}${input}`, {
            ...init,
            headers,
            credentials: "include"
        } as RequestInit);
    }

    const response = await sendRequest(accessToken);

    if (response.status !== 401) {
        return response;
    }

    const newToken = await refreshAccessToken();

    if (!newToken) {
        return response;
    }

    return sendRequest(newToken);
}
