import { beforeEach, describe, expect, it, vi } from "vitest";
import { authFetch, getAccessToken, login, logout } from "./session";

// Create a successful authentication response.
const tokenResponse = (token: string) =>
    new Response(JSON.stringify({
        accessToken: token,
        tokenType: "Bearer",
        expiresIn: 900
    }), { status: 200 });

// Create an unauthorized response.
const unauthorized = () => new Response(null, { status: 401 });

// Support both string URLs and Request objects passed to fetch().
function getRequest(input: RequestInfo | URL): Request {
    return new Request(input);
}

describe("Authentication session", () => {
    beforeEach(() => {
        vi.restoreAllMocks();
        vi.unstubAllGlobals();
    });

    it("uses one refresh for simultaneous 401 responses", async () => {
        let refreshCount = 0;

        vi.stubGlobal("fetch", vi.fn(async (input: RequestInfo | URL) => {
            const request = getRequest(input);

            if (request.url.endsWith("/auth/login")) {
                return tokenResponse("old-token");
            }

            if (request.url.endsWith("/auth/refresh")) {
                refreshCount++;

                // Keep the refresh pending while other requests receive 401.
                await new Promise(resolve => setTimeout(resolve, 10));

                return tokenResponse("new-token");
            }

            return request.headers.get("Authorization") === "Bearer new-token"
                ? new Response(null, { status: 200 })
                : unauthorized();
        }));

        await login("demo@example.com", "test-password");

        const responses = await Promise.all([
            authFetch("/api/v1/me"),
            authFetch("/api/v1/me/home"),
            authFetch("/api/v1/stokvels")
        ]);

        expect(responses.every(response => response.status === 200)).toBe(true);
        expect(refreshCount).toBe(1);
    });

    it("refreshes again after a later token expiry", async () => {
        let refreshCount = 0;
        let expireNextRequest = false;

        vi.stubGlobal("fetch", vi.fn(async (input: RequestInfo | URL) => {
            const request = getRequest(input);

            if (request.url.endsWith("/auth/login")) {
                return tokenResponse("old-token");
            }

            if (request.url.endsWith("/auth/refresh")) {
                refreshCount++;
                return tokenResponse(`new-token-${refreshCount}`);
            }

            if (expireNextRequest) {
                expireNextRequest = false;
                return unauthorized();
            }

            const currentToken = `Bearer new-token-${refreshCount}`;

            return refreshCount > 0 &&
                request.headers.get("Authorization") === currentToken
                ? new Response(null, { status: 200 })
                : unauthorized();
        }));

        await login("demo@example.com", "test-password");

        // The original token expires, triggering the first refresh.
        const firstResponse = await authFetch("/api/v1/me");

        expect(firstResponse.status).toBe(200);
        expect(refreshCount).toBe(1);

        // Simulate the replacement token expiring later.
        expireNextRequest = true;

        const secondResponse = await authFetch("/api/v1/me/home");

        expect(secondResponse.status).toBe(200);
        expect(refreshCount).toBe(2);
    });

    it("stops retrying when refresh fails", async () => {
        let apiCalls = 0;
        let refreshCalls = 0;

        vi.stubGlobal("fetch", vi.fn(async (input: RequestInfo | URL) => {
            const request = getRequest(input);

            if (request.url.endsWith("/auth/login")) {
                return tokenResponse("old-token");
            }

            if (request.url.endsWith("/auth/refresh")) {
                refreshCalls++;
                return unauthorized();
            }

            apiCalls++;
            return unauthorized();
        }));

        await login("demo@example.com", "test-password");

        const response = await authFetch("/api/v1/me");

        expect(response.status).toBe(401);
        expect(apiCalls).toBe(1);
        expect(refreshCalls).toBe(1);
        expect(getAccessToken()).toBeNull();
    });

    it("does not loop when the refresh cookie is missing", async () => {
        let refreshCalls = 0;
        let apiCalls = 0;

        vi.stubGlobal("fetch", vi.fn(async (input: RequestInfo | URL) => {
            const request = getRequest(input);

            if (request.url.endsWith("/auth/login")) {
                return tokenResponse("old-token");
            }

            // Simulate the server rejecting refresh without a valid cookie.
            if (request.url.endsWith("/auth/refresh")) {
                refreshCalls++;
                return unauthorized();
            }

            apiCalls++;
            return unauthorized();
        }));

        await login("demo@example.com", "test-password");

        const response = await authFetch("/api/v1/me");

        expect(response.status).toBe(401);
        expect(refreshCalls).toBe(1);
        expect(apiCalls).toBe(1);
    });

    it("preserves a POST body after refreshing", async () => {
        const bodies: string[] = [];

        vi.stubGlobal("fetch", vi.fn(async (input: RequestInfo | URL) => {
            const request = getRequest(input);

            if (request.url.endsWith("/auth/login")) {
                return tokenResponse("old-token");
            }

            if (request.url.endsWith("/auth/refresh")) {
                return tokenResponse("new-token");
            }

            bodies.push(await request.text());

            return request.headers.get("Authorization") === "Bearer new-token"
                ? new Response(null, { status: 200 })
                : unauthorized();
        }));

        await login("demo@example.com", "test-password");

        const body = JSON.stringify({ contributionAmount: 150 });

        const response = await authFetch("/api/v1/contributions", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body
        });

        expect(response.status).toBe(200);
        expect(bodies).toEqual([body, body]);
    });

    it("clears the access token on logout", async () => {
        vi.stubGlobal("fetch", vi.fn(async (input: RequestInfo | URL) => {
            const request = getRequest(input);

            if (request.url.endsWith("/auth/login")) {
                return tokenResponse("old-token");
            }

            return new Response(null, { status: 200 });
        }));

        await login("demo@example.com", "test-password");

        expect(getAccessToken()).toBe("old-token");

        await logout();

        expect(getAccessToken()).toBeNull();
    });
});