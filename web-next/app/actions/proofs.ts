"use server";

import { revalidatePath } from "next/cache";
import { workflowRequest } from "@/lib/workflow-api";
import { proofError } from "@/lib/proof-validation";
import type { ActionResult } from "@/lib/workflow-types";

// Uploads or removes proof of payment for a contribution.
//
// The same Server Action handles both operations:
// - Upload: Sends a selected file to the backend.
// - Remove: Deletes the existing proof through the backend.
export async function saveProof(
    _state: ActionResult,
    data: FormData
): Promise<ActionResult> {

    // Read the stokvel, contribution cycle and member IDs.
    const stokvel = Number(data.get("stokvelId"));
    const cycle = Number(data.get("cycleId"));
    const member = Number(data.get("memberId"));

    // All three IDs must be valid positive integers.
    if (
        ![stokvel, cycle, member].every(
            id => Number.isSafeInteger(id) && id > 0
        )
    ) {
        return {
            status: 400,
            message: "Invalid contribution."
        };
    }

    // Determine whether the user wants to remove or upload proof.
    const remove = data.get("operation") === "remove";

    // Read the uploaded file from the form.
    const file = data.get("file");

    // Only validate the file when uploading.
    // Deleting proof does not require a file.
    if (!remove) {

        // Check the file's size and browser-reported MIME type.
        const error = proofError(
            file instanceof File ? file : null
        );

        // Return a friendly error before contacting the backend.
        if (error) {
            return {
                status: 400,
                message: error
            };
        }
    }

    // Prepare the multipart form data for the backend upload.
    const body = new FormData();

    if (file instanceof File) {
        body.set("file", file);
    }

    // Send the request to the ASP.NET Core backend.
    //
    // POST uploads or replaces proof.
    // DELETE removes existing proof.
    //
    // The backend checks permissions, file signatures,
    // file size and whether the contribution cycle is closed.
    const { result, response } = await workflowRequest(
        `/stokvels/${stokvel}/cycles/${cycle}/contributions/${member}/proof`,
        {
            method: remove ? "DELETE" : "POST",
            body: remove ? undefined : body
        }
    );

    // Return any error reported by the backend.
    if (!response) {
        return result;
    }

    // Refresh cached data for the stokvel's proof page.
    revalidatePath(`/stokvels/${stokvel}/proof`);

    // Return a success message for the completed operation.
    return {
        status: 200,
        message: remove
            ? "Proof removed."
            : "Proof uploaded successfully."
    };
}