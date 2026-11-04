import { workflowRequest } from "@/lib/workflow-api";

// Handles authenticated proof-of-payment downloads.
//
// The browser requests this Next.js route instead of
// accessing the ASP.NET Core API directly.
export async function GET(
    _request: Request,
    context: {
        params: Promise<{
            id: string;
            cycleId: string;
            memberId: string;
        }>;
    }
) {

    // Read the stokvel, cycle and member IDs from the URL.
    const { id, cycleId, memberId } = await context.params;

    // Reject invalid IDs before contacting the backend.
    if (
        ![id, cycleId, memberId].every(
            value => /^[1-9]\d*$/.test(value)
        )
    ) {
        return new Response(null, {
            status: 404
        });
    }

    // Security headers applied to every response.
    //
    // private, no-store prevents browsers and shared caches
    // from storing sensitive proof-of-payment documents.
    //
    // nosniff prevents browsers from guessing a different
    // content type from the file's contents.
    const headers = new Headers({
        "Cache-Control": "private, no-store",
        "X-Content-Type-Options": "nosniff"
    });

    // Request the proof from the authenticated backend API.
    //
    // The backend checks whether the current user is
    // the uploader or an authorized stokvel treasurer.
    const { response } = await workflowRequest(
        `/stokvels/${id}/cycles/${cycleId}/contributions/${memberId}/proof`
    );

    // Return the same response for missing or unauthorized proofs.
    // This prevents the download route from revealing whether
    // a particular contribution has an existing proof.
    if (!response) {
        return new Response(null, {
            status: 404,
            headers
        });
    }

    // Read the content type supplied by the backend.
    const type =
        response.headers.get("Content-Type") ??
        "application/octet-stream";

    // Choose a download extension based on the file type.
    const extension =
        type === "application/pdf"
            ? "pdf"
            : type === "image/png"
                ? "png"
                : type === "image/jpeg"
                    ? "jpg"
                    : "bin";

    // Preserve the file's content type.
    headers.set("Content-Type", type);

    // Force the browser to download the file instead
    // of displaying it directly in the browser.
    headers.set(
        "Content-Disposition",
        `attachment; filename="proof.${extension}"`
    );

    // Stream the file from the backend to the browser.
    // This avoids loading the entire document into memory.
    return new Response(response.body, {
        status: 200,
        headers
    });
}