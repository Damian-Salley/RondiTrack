import { NextRequest, NextResponse } from "next/server";

import {
    decryptSession,
    encryptSession,
    sessionCookieOptions,
} from "@/lib/session-crypto";

import { coordinatedRefresh } from "@/lib/refresh-coordinator";
import { shouldRefreshAccessToken } from "@/lib/token";

const protectedPaths = [
    "/stokvels",
    "/dashboard",
];

function isProtectedPath(pathname: string): boolean {
    return protectedPaths.some(
        (path) =>
            pathname === path ||
            pathname.startsWith(`${path}/`)
    );
}

function redirectToLogin(
    request: NextRequest,
    expired: boolean
): NextResponse {
    const url = new URL("/login", request.url);

    // Preserve the original destination for after sign-in.
    url.searchParams.set(
        "next",
        request.nextUrl.pathname + request.nextUrl.search
    );

    if (expired) {
        url.searchParams.set("expired", "1");
    }

    const response = NextResponse.redirect(url);

    response.cookies.delete("session");

    return response;
}

export async function proxy(request: NextRequest) {
    const pathname = request.nextUrl.pathname;

    if (!isProtectedPath(pathname)) {
        return NextResponse.next();
    }

    const encrypted = request.cookies.get("session")?.value;
    const session = await decryptSession(encrypted);

    if (!session) {
        return redirectToLogin(request, false);
    }

    if (!shouldRefreshAccessToken(session.accessToken)) {
        return NextResponse.next();
    }

    const updatedSession = await coordinatedRefresh(
        session.refreshToken
    );

    if (!updatedSession) {
        return redirectToLogin(request, true);
    }

    const updatedCookie = await encryptSession(updatedSession);

    // Update the incoming request cookie so Server Components
    // receive the new tokens during this same request.
    const requestHeaders = new Headers(request.headers);

    const existingCookies = requestHeaders.get("cookie") ?? "";

    const remainingCookies = existingCookies
        .split(";")
        .map((cookie) => cookie.trim())
        .filter(
            (cookie) =>
                cookie.length > 0 &&
                !cookie.startsWith("session=")
        );

    remainingCookies.push(`session=${updatedCookie}`);

    requestHeaders.set(
        "cookie",
        remainingCookies.join("; ")
    );

    const response = NextResponse.next({
        request: {
            headers: requestHeaders,
        },
    });

    // Update the browser cookie for subsequent requests.
    response.cookies.set(
        "session",
        updatedCookie,
        sessionCookieOptions
    );

    return response;
}

export const config = {
    matcher: [
        "/stokvels/:path*",
        "/dashboard/:path*",
    ],
};