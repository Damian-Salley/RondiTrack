"use server";

import { revalidatePath } from "next/cache";
import { workflowRequest } from "@/lib/workflow-api";
import type { ActionResult } from "@/lib/workflow-types";

// Joins a user to a stokvel after validating the invitation code
// and confirming that the user has accepted the stokvel rules.
export async function joinStokvel(
    _state: ActionResult,
    data: FormData
): Promise<ActionResult> {

    // Read the invitation code submitted by the form.
    const code = String(data.get("code") ?? "");

    // Invitation codes must contain exactly 32 hexadecimal characters.
    if (!/^[a-fA-F0-9]{32}$/.test(code)) {
        return {
            status: 400,
            message: "Check the code and try again."
        };
    }

    // Prevent joining through this Server Action without accepting the rules.
    if (data.get("acceptedRules") !== "on") {
        return {
            status: 400,
            message: "You must accept the rules before joining."
        };
    }

    // Send the join request to the backend API.
    // The backend handles authentication and prevents duplicate memberships.
    const { result, response } = await workflowRequest(
        `/invites/${encodeURIComponent(code)}/join`,
        { method: "POST" }
    );

    // Return the backend error if the request was unsuccessful.
    if (!response) {
        return result;
    }

    // Read the stokvel ID returned after joining successfully.
    const joined = await response.json() as { stokvelId: number };

    // Refresh cached stokvel pages so the new membership appears.
    revalidatePath("/stokvels");

    // Return a success message and the joined stokvel's ID.
    return {
        status: 200,
        message: "You're in. Your membership is confirmed.",
        stokvelId: joined.stokvelId
    };
}

// Allows an authorized treasurer to generate a stokvel invitation.
export async function createInvite(
    _state: ActionResult,
    data: FormData
): Promise<ActionResult> {

    // Read and validate the stokvel ID from the form.
    const id = Number(data.get("stokvelId"));

    if (!Number.isSafeInteger(id) || id <= 0) {
        return {
            status: 400,
            message: "Invalid stokvel."
        };
    }

    // Send the invitation rules and contribution schedule to the backend.
    // The backend verifies that the current user is the stokvel treasurer.
    const { result, response } = await workflowRequest(
        `/stokvels/${id}/invites`,
        {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                rules: String(data.get("rules") ?? ""),
                schedule: String(data.get("schedule") ?? "")
            })
        }
    );

    // Return the backend error if invitation creation failed.
    if (!response) {
        return result;
    }

    // Read the newly generated invitation code.
    const invite = await response.json() as { code: string };

    // Return the invitation URL so the treasurer can share it.
    return {
        status: 200,
        message: `/join?code=${invite.code}&step=rules`
    };
}