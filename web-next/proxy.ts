import { NextRequest, NextResponse } from "next/server";

import {
    decryptSession,
    encryptSession,
    sessionCookieOptions,
} from "@/lib/session-crypto";

import { coordinatedRefresh } from "@/lib/refresh-coordinator";
import { shouldRefreshAccessToken } from "@/lib/token";

// Routes that require an authenticated session.
const protectedPaths = [
    "/stokvels",
    "/dashboard",
    "/activity",
    "/bff",
];

// Check whether a request targets a protected route.
//
// Matching the exact path and its children prevents unrelated
// paths such as "/stokvels-public" from being protected accidentally.
function isProtectedPath(pathname: string): boolean {
    return protectedPaths.some(
        (path) =>
            pathname === path ||
            pathname.startsWith(`${path}/`)
    );
}

// Identify requests to the Backend-for-Frontend (BFF).
//
// BFF requests must receive JSON authentication errors,
// rather than redirects to an HTML login page.
function isBffPath(pathname: string): boolean {
    return (
        pathname === "/bff" ||
        pathname.startsWith("/bff/")
    );
}

// Prevent private BFF responses from being indexed.
function protectBffResponse(
    response: NextResponse,
    pathname: string
): NextResponse {
    if (isBffPath(pathname)) {
        response.headers.set(
            "X-Robots-Tag",
            "noindex, nofollow"
        );
    }

    return response;
}

// Redirect an unauthenticated user to the login page.
function redirectToLogin(
    request: NextRequest,
    expired: boolean
): NextResponse {
    const url = new URL("/login", request.url);

    // Preserve the original page and query parameters so
    // the user can return to their destination after login.
    url.searchParams.set(
        "next",
        request.nextUrl.pathname + request.nextUrl.search
    );

    // Tell the login page whether the previous session expired.
    if (expired) {
        url.searchParams.set("expired", "1");
    }

    const response = NextResponse.redirect(url);

    // Remove the invalid session cookie from the browser.
    response.cookies.delete("session");

    return response;
}

// Return the appropriate response when authentication fails.
function authenticationFailure(
    request: NextRequest,
    expired: boolean
): NextResponse {
    const pathname = request.nextUrl.pathname;

    // BFF requests must return JSON with HTTP 401.
    if (isBffPath(pathname)) {
        const response = NextResponse.json(
            {
                error: "Your session has expired. Please sign in again.",
            },
            {
                status: 401,
            }
        );

        // Clear the invalid browser session.
        response.cookies.delete("session");

        return protectBffResponse(response, pathname);
    }

    // Browser page requests redirect to login.
    return redirectToLogin(request, expired);
}

// Next.js runs this proxy before matching requests reach
// their page or route handler.
export async function proxy(request: NextRequest) {
    const pathname = request.nextUrl.pathname;

    // Public routes do not require authentication.
    if (!isProtectedPath(pathname)) {
        return NextResponse.next();
    }

    // Read and decrypt the session stored in the browser cookie.
    const encrypted = request.cookies.get("session")?.value;
    const session = await decryptSession(encrypted);

    // Missing or invalid sessions must not access private routes.
    if (!session) {
        return authenticationFailure(request, false);
    }

    // Continue normally when the access token is still valid.
    if (!shouldRefreshAccessToken(session.accessToken)) {
        return protectBffResponse(
            NextResponse.next(),
            pathname
        );
    }

    // Refresh the access token before it expires.
    //
    // The refresh coordinator helps avoid simultaneous refresh
    // attempts using the same rotating refresh token.
    const updatedSession = await coordinatedRefresh(
        session.refreshToken
    );

    // If refresh fails, require the user to sign in again.
    if (!updatedSession) {
        return authenticationFailure(request, true);
    }

    // Encrypt the refreshed session for the browser cookie.
    const updatedCookie = await encryptSession(updatedSession);

    // Update the incoming request headers so Server Components
    // and route handlers can use the refreshed session immediately.
    const requestHeaders = new Headers(request.headers);

    const existingCookies = requestHeaders.get("cookie") ?? "";

    // Keep existing cookies except the outdated session cookie.
    const remainingCookies = existingCookies
        .split(";")
        .map((cookie) => cookie.trim())
        .filter(
            (cookie) =>
                cookie.length > 0 &&
                !cookie.startsWith("session=")
        );

    // Add the refreshed session cookie to the incoming request.
    remainingCookies.push(`session=${updatedCookie}`);

    requestHeaders.set(
        "cookie",
        remainingCookies.join("; ")
    );

    // Continue the request using the refreshed session.
    const response = NextResponse.next({
        request: {
            headers: requestHeaders,
        },
    });

    // Save the refreshed session in the browser for future requests.
    response.cookies.set(
        "session",
        updatedCookie,
        sessionCookieOptions
    );

    // Ensure BFF responses are not indexed by search engines.
    return protectBffResponse(response, pathname);
}

// Only run the proxy for routes that may require authentication.
//
// The join route remains public, but is included in the matcher
// so it can be handled here if its access rules change later.
export const config = {
    matcher: [
        "/join/:path*",
        "/stokvels/:path*",
        "/dashboard/:path*",
        "/activity/:path*",
        "/bff/:path*",
    ],
};