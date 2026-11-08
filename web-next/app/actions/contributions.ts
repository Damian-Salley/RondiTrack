"use server";

import { revalidatePath } from "next/cache";

import { getSession } from "@/lib/session";
import { getMe } from "@/lib/api";
import { contributionSchema } from "@/lib/contribution-validation";
import { parseRandToCents } from "@/lib/formatters";
import { contributionFieldErrors } from "@/lib/contribution-errors";

// Describe the result returned to the contribution form.
export type ContributionActionState = {
  success: boolean;
  message: string;

  // True when the request may have reached the backend,
  // but the frontend could not confirm its result.
  uncertain?: boolean;

  // Reference supplied by the backend for troubleshooting.
  correlationId?: string;

  // Validation errors displayed beside individual form fields.
  fieldErrors?: Partial<Record<"amount" | "userId", string>>;
};

// Record a contribution through the authenticated backend API.
export async function recordContribution(
  _previousState: ContributionActionState,
  formData: FormData
): Promise<ContributionActionState> {
  // Read the values submitted by the form.
  const amount = String(formData.get("amount") ?? "");
  const stokvelId = Number(formData.get("stokvelId"));
  const cycleId = Number(formData.get("cycleId"));
  const submittedUserId = Number(formData.get("userId"));
  const idempotencyKey = String(formData.get("idempotencyKey") ?? "");

  // Validate the amount before sending anything to the backend.
  const validation = contributionSchema.safeParse({ amount });

  if (!validation.success) {
    return {
      success: false,
      message: "Enter a valid contribution amount.",
      fieldErrors: {
        amount: "Enter a valid amount greater than R0.00.",
      },
    };
  }

  // Convert rand to integer cents to avoid floating-point errors.
  const amountInCents = parseRandToCents(amount);

  if (amountInCents === null) {
    return {
      success: false,
      message: "Invalid contribution amount.",
      fieldErrors: {
        amount: "Enter a valid contribution amount.",
      },
    };
  }

  // Check that the request contains valid identifiers and a key.
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

  // Retrieve the authenticated user's session.
  const session = await getSession();

  if (!session) {
    return {
      success: false,
      message: "Your session has expired. Please sign in again.",
    };
  }

  // Verify the user's identity and membership before submitting.
  let userId: number;

  try {
    // Identify the signed-in user.
    const me = await getMe();

    // Retrieve the user's role for each stokvel.
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

    // Find the membership associated with this stokvel.
    const membership = stokvels.find(
      (stokvel) => stokvel.stokvelId === stokvelId
    );

    if (!membership) {
      return {
        success: false,
        message: "You are not a member of this stokvel.",
      };
    }

    // Members contribute for themselves.
    // Treasurers may select another member.
    userId =
      membership.role === "Treasurer"
        ? submittedUserId
        : me.id;

    if (!Number.isSafeInteger(userId) || userId <= 0) {
      return {
        success: false,
        message: "Select a valid member.",
        fieldErrors: {
          userId: "Select a valid member.",
        },
      };
    }
  } catch {
    // No contribution request has been sent yet.
    // This is a verification failure, not an uncertain contribution.
    return {
      success: false,
      message:
        "Unable to verify your account or membership. Please try again.",
    };
  }

  try {
    // Send the contribution to the backend.
    // Repeated requests with the same key must not create duplicates.
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

    // Handle backend rejections without clearing the form.
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

      // Read the backend's request reference when available.
      const correlationId =
        response.headers.get("X-Correlation-ID") ??
        response.headers.get("X-Request-ID") ??
        undefined;

      // ProblemDetails responses may contain field-specific errors.
      const problem: unknown = await response
        .json()
        .catch(() => null);

      return {
        success: false,
        message:
          messages[response.status] ??
          "Unable to record the contribution.",
        correlationId,

        // Both 400 and 422 may contain validation errors.
        fieldErrors:
          response.status === 400 || response.status === 422
            ? contributionFieldErrors(problem)
            : undefined,
      };
    }

    // Refresh the stokvel page so it displays the new contribution.
    revalidatePath(`/stokvels/${stokvelId}`);

    return {
      success: true,
      message: "Contribution recorded successfully.",
    };
  } catch {
    // The backend may have processed the request before the
    // connection failed. The form must reuse its idempotency key.
    return {
      success: false,
      message:
        "Could not confirm the result. Retry with the same contribution details.",
      uncertain: true,
    };
  }
}