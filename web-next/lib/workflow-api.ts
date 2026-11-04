import "server-only";

import { getSession } from "@/lib/session";
import type { ActionResult } from "@/lib/workflow-types";

// Sends authenticated requests from Next.js to the ASP.NET Core API.
//
// This helper runs only on the server, so access tokens
// are never passed directly to browser components.
export async function workflowRequest(
    path: string,
    init: RequestInit = {}
): Promise<{
    result: ActionResult;
    response?: Response;
}> {

    // Retrieve the currently authenticated user's session.
    const session = await getSession();

    // Reject the request when the user is not signed in.
    if (!session) {
        return {
            result: {
                status: 401,
                message: "Your session expired. Please sign in again."
            }
        };
    }

    // Generate a unique reference for tracing this request.
    const reference = crypto.randomUUID();

    try {

        // Start with any headers supplied by the caller.
        const headers = new Headers(init.headers);

        // Authenticate the backend request using the user's JWT.
        headers.set(
            "Authorization",
            `Bearer ${session.accessToken}`
        );

        // Include the correlation ID for troubleshooting.
        headers.set("X-Correlation-ID", reference);

        // Forward the request to the ASP.NET Core backend.
        const response = await fetch(
            `${process.env.API_BASE_URL}/api/v1${path}`,
            {
                ...init,
                headers,
                cache: "no-store"
            }
        );

        // Prefer the correlation ID returned by the backend.
        const correlationId =
            response.headers.get("X-Correlation-ID") ?? reference;

        // Return successful responses to the calling Server Action.
        if (response.ok) {
            return {
                result: {
                    status: response.status,
                    message: "Done."
                },
                response
            };
        }

        // Read the backend's Problem Details response.
        // If the response is not valid JSON, use an empty object.
        const problem: Record<string, unknown> =
            await response.json().catch(() => ({}));

        // Provide clear messages for common authorization failures.
        const messages: Record<number, string> = {
            401: "Your session expired. Please sign in again.",
            403: "Only this stokvel's treasurers can do this.",
            429: "Too many requests. Please wait and try again."
        };

        // Only accept string values from backend error details.
        // This prevents unexpected objects appearing in the UI.
        const detail =
            typeof problem.detail === "string"
                ? problem.detail
                : undefined;

        const title =
            typeof problem.title === "string"
                ? problem.title
                : undefined;

        // Prefer a known friendly message.
        // For validation errors, use the backend's safe description.
        // For server errors, avoid exposing internal implementation details.
        const message =
            messages[response.status] ??
            (
                response.status < 500
                    ? detail ?? title ?? "Request refused."
                    : "The API could not complete the request. Please retry."
            );

        // Use the backend's correlation ID when it is a valid string.
        const problemCorrelationId =
            typeof problem.correlationId === "string"
                ? problem.correlationId
                : correlationId;

        // Return the error without exposing the raw backend response.
        return {
            result: {
                status: response.status,
                message,
                correlationId: problemCorrelationId,

                // A server error may leave the outcome of a
                // state-changing request uncertain.
                uncertain: response.status >= 500
            }
        };

    } catch {

        // A network failure may happen after the backend
        // successfully processes a state-changing request.
        //
        // The caller should preserve its idempotency key
        // and retry the same operation safely.
        return {
            result: {
                status: 503,
                message: "Could not confirm the result. Please retry.",
                correlationId: reference,
                uncertain: true
            }
        };
    }
}