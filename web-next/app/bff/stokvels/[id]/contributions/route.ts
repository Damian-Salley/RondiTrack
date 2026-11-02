import { NextRequest, NextResponse } from "next/server";
import { getSession } from "@/lib/session";

// The BFF requests additional contribution pages on behalf of the user.
export async function GET(
    request: NextRequest,
    context: { params: Promise<{ id: string }> }
) {
    // Read the user's existing authenticated session.
    const session = await getSession();

    // Return JSON instead of redirecting when the session is missing.
    if (!session) {
        return NextResponse.json(
            { error: "Not authenticated." },
            { status: 401 }
        );
    }

    // Validate the stokvel ID supplied in the URL.
    const { id } = await context.params;
    const stokvelId = Number(id);

    if (!Number.isInteger(stokvelId) || stokvelId <= 0) {
        return NextResponse.json(
            { error: "Invalid stokvel ID." },
            { status: 400 }
        );
    }

    // Forward only the supported contribution query parameters.
    const params = new URLSearchParams();

    for (const key of [
        "userId",
        "from",
        "to",
        "pageSize",
        "pageToken",
    ]) {
        const value = request.nextUrl.searchParams.get(key);

        if (value !== null) {
            params.set(key, value);
        }
    }

    // Call the existing .NET API with the server-side access token.
    const response = await fetch(
        `${process.env.API_BASE_URL}/api/v1/stokvels/${stokvelId}/contributions?${params}`,
        {
            headers: {
                Authorization: `Bearer ${session.accessToken}`,
            },
            cache: "no-store",
        }
    );

    // Preserve the backend's response status, including validation errors.
    const body = await response.json();

    return NextResponse.json(body, {
        status: response.status,
    });
}