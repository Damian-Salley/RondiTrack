import { z } from "zod";

import { parseRandToCents } from "@/lib/formatters";

// Validate the values submitted by the contribution form.
export const contributionSchema = z.object({
    // Accept only valid, positive South African rand amounts.
    //
    // The money parser supports:
    // - Optional rand symbols (R)
    // - Spaces as thousands separators
    // - Decimal commas and decimal points
    // - Exact conversion to integer cents
    //
    // The parser accepts zero as a valid monetary value,
    // but contributions must always be greater than zero.
    amount: z.string().refine(
        (value) => {
            const cents = parseRandToCents(value);

            return cents !== null && cents > 0;
        },
        "Enter a valid amount greater than R0.00."
    ),

    // Treasurers may select another member when recording a contribution.
    // Regular members do not need to provide a member ID.
    //
    // When supplied, the ID must be a positive integer.
    userId: z
        .number()
        .int()
        .positive("Select a valid member.")
        .optional(),
});

// Generate the TypeScript type directly from the validation schema.
// This keeps the form's type consistent with its validation rules.
export type ContributionFormValues = z.infer<
    typeof contributionSchema
>;