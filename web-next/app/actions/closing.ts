"use server";

import { revalidatePath } from "next/cache";

import { workflowRequest } from "@/lib/workflow-api";

import type {
    ActionResult,
    ClosePreview
} from "@/lib/workflow-types";

// Retrieves the latest arrears preview for a contribution cycle.
//
// The backend calculates outstanding amounts for each member.
// The frontend only displays those calculations.
export async function loadClosePreview(
    cycleId: number
): Promise<{
    result: ActionResult;
    preview?: ClosePreview;
}> {

    // Reject invalid contribution cycle IDs.
    if (
        !Number.isSafeInteger(cycleId) ||
        cycleId <= 0
    ) {
        return {
            result: {
                status: 400,
                message: "Invalid cycle."
            }
        };
    }

    // Request the latest closing preview from the backend.
    const { result, response } = await workflowRequest(
        `/contributioncycles/${cycleId}/close-preview`
    );

    // Return the backend result and the preview, if available.
    return {
        result,
        preview: response
            ? await response.json() as ClosePreview
            : undefined
    };
}

// Closes a contribution cycle after the treasurer confirms the preview.
//
// The preview version protects against stale contribution data.
// The idempotency key prevents duplicate processing when a request
// is repeated or its outcome is uncertain.
export async function closeCycle(
    cycleId: number,
    version: string,
    key: string
): Promise<ActionResult> {

    // Validate the contribution cycle ID.
    if (
        !Number.isSafeInteger(cycleId) ||
        cycleId <= 0
    ) {
        return {
            status: 400,
            message: "Invalid cycle."
        };
    }

    // Require a non-empty preview version.
    if (
        typeof version !== "string" ||
        !version.trim()
    ) {
        return {
            status: 400,
            message: "Invalid closing preview."
        };
    }

    // Require an idempotency key for safe retries.
    if (
        typeof key !== "string" ||
        !key.trim()
    ) {
        return {
            status: 400,
            message: "Invalid closing request."
        };
    }

    // Ask the ASP.NET Core backend to close the cycle.
    //
    // The backend checks:
    // - Whether the user is an authorized treasurer.
    // - Whether the preview version is still current.
    // - Whether the cycle is already closed.
    // - Whether the idempotency key has already been processed.
    const { result, response } = await workflowRequest(
        `/contributioncycles/${cycleId}/close`,
        {
            method: "POST",

            headers: {
                "Content-Type": "application/json",
                "Idempotency-Key": key
            },

            body: JSON.stringify({
                version
            })
        }
    );

    // Preserve backend errors, including stale-preview conflicts.
    //
    // workflowRequest also determines whether an unexpected
    // failure has an uncertain outcome.
    if (!response) {
        return result;
    }

    // Refresh cached pages that display cycle or arrears data.
    revalidatePath("/stokvels", "layout");
    revalidatePath("/dashboard", "layout");
    revalidatePath("/activity");

    // Confirm that the backend completed the closing operation.
    return {
        status: 200,
        message: "Cycle closed. Arrears recorded exactly once."
    };
}