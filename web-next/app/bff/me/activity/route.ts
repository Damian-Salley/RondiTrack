import { NextRequest, NextResponse } from "next/server";
import { getSession } from "@/lib/session";

// Proxy activity pagination requests through the authenticated session.
export async function GET(request: NextRequest) {
    const session = await getSession();

    // API requests return JSON 401 instead of redirecting to login.
    if (!session) {
        return NextResponse.json(
            { error: "Not authenticated." },
            { status: 401 }
        );
    }

    const params = new URLSearchParams();

    // Forward only the pagination parameters supported by the backend.
    for (const key of ["pageSize", "pageToken"]) {
        const value = request.nextUrl.searchParams.get(key);

        if (value !== null) {
            params.set(key, value);
        }
    }

    // Access tokens remain on the server.
    const response = await fetch(
        `${process.env.API_BASE_URL}/api/v1/me/activity?${params}`,
        {
            headers: {
                Authorization: `Bearer ${session.accessToken}`,
            },
            cache: "no-store",
        }
    );

    // Preserve the backend's response and HTTP status.
    const body = await response.json();

    return NextResponse.json(body, {
        status: response.status,
    });
}