"use server";

import { revalidatePath } from "next/cache";
import { getSession } from "@/lib/session";
import { getMe } from "@/lib/api";
import { contributionSchema } from "@/lib/contribution-validation";
import { parseRandToCents } from "@/lib/formatters";

// Describe the result returned to the contribution form.
export type ContributionActionState = {
    success: boolean;
    message: string;

    // Distinguish an uncertain outcome from a confirmed rejection.
    uncertain?: boolean;

    // Display a backend reference when one is available.
    correlationId?: string;
};



// Record a contribution through the authenticated backend API.
export async function recordContribution(
    _previousState: ContributionActionState,
    formData: FormData
): Promise<ContributionActionState> {
    // Read the submitted form values.
    const amount = String(formData.get("amount") ?? "");
    const stokvelId = Number(formData.get("stokvelId"));
    const cycleId = Number(formData.get("cycleId"));
    const submittedUserId = Number(formData.get("userId"));
    const idempotencyKey = String(formData.get("idempotencyKey") ?? "");

    // Validate the contribution amount using our Zod schema.
    const validation = contributionSchema.safeParse({ amount });

    if (!validation.success) {
        return {
            success: false,
            message: "Enter a valid contribution amount.",
        };
    }

    // Convert the validated rand amount into integer cents.
    const amountInCents = parseRandToCents(amount);

    if (amountInCents === null) {
        return {
            success: false,
            message: "Invalid contribution amount.",
        };
    }

    // Reject invalid stokvel IDs, cycle IDs and idempotency keys.
    if (
        !Number.isSafeInteger(stokvelId) ||
        stokvelId <= 0 ||
        !Number.isSafeInteger(cycleId) ||
        cycleId <= 0 ||
        !idempotencyKey
    ) {
        return {
            success: false,
            message: "Invalid contribution request.",
        };
    }

    // Retrieve the authenticated user's existing session.
    const session = await getSession();

    if (!session) {
        return {
            success: false,
            message: "Your session has expired. Please sign in again.",
        };
    }

    // Identify the signed-in user through the backend.
    const me = await getMe();

    // Retrieve the user's role specifically for this stokvel.
    const homeResponse = await fetch(
        `${process.env.API_BASE_URL}/api/v1/me/home`,
        {
            headers: {
                Authorization: `Bearer ${session.accessToken}`,
            },
            cache: "no-store",
        }
    );

    if (!homeResponse.ok) {
        return {
            success: false,
            message: "Unable to verify your stokvel membership.",
        };
    }

    const stokvels = (await homeResponse.json()) as Array<{
        stokvelId: number;
        role: string;
    }>;

    const membership = stokvels.find(
        (stokvel) => stokvel.stokvelId === stokvelId
    );

    if (!membership) {
        return {
            success: false,
            message: "You are not a member of this stokvel.",
        };
    }

    // Members submit for themselves; Treasurers may select another member.
    const userId =
        membership.role === "Treasurer" ? submittedUserId : me.id;

    if (!Number.isSafeInteger(userId) || userId <= 0) {
        return {
            success: false,
            message: "Select a valid member.",
        };
    }

    try {
        // Submit the contribution using the backend's existing API contract.
        const response = await fetch(
            `${process.env.API_BASE_URL}/api/v1/stokvels/${stokvelId}/cycles/${cycleId}/contributions`,
            {
                method: "POST",
                headers: {
                    Authorization: `Bearer ${session.accessToken}`,
                    "Content-Type": "application/json",
                    "Idempotency-Key": idempotencyKey,
                },
                body: JSON.stringify({
                    userId,
                    amountInCents,
                }),
                cache: "no-store",
            }
        );
        // Handle confirmed backend rejections separately from network failures.
        if (!response.ok) {
            const messages: Record<number, string> = {
                400: "Invalid contribution request.",
                401: "Your session has expired. Please sign in again.",
                403: "You do not have permission to record this contribution.",
                404: "The stokvel, cycle or member was not found.",
                409: "This contribution conflicts with an existing record.",
                422: "The contribution could not be accepted.",
                429: "Too many requests. Please try again later.",
            };

            // Retrieve the request reference if the backend supplies one.
            const correlationId =
                response.headers.get("X-Correlation-ID") ??
                response.headers.get("X-Request-ID") ??
                undefined;

            return {
                success: false,
                message:
                    messages[response.status] ??
                    "Unable to record the contribution.",
                correlationId,
            };
        }
        // Refresh the stokvel page so it displays the latest contribution.
        revalidatePath(`/stokvels/${stokvelId}`);

        return {
            success: true,
            message: "Contribution recorded successfully.",
        };
    } catch {
        // The request may have reached the backend before the connection failed.
        // The form must preserve its idempotency key when retrying.
        return {
            success: false,
            message:
                "Could not confirm the result. Retry with the same contribution details.",
        };
    }
}