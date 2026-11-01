import { z } from "zod";
import { parseRandToCents } from "@/lib/formatters";

// Validate contribution amounts using our existing money parser.
export const contributionSchema = z.object({
    // The amount must represent a valid, positive rand value.
    amount: z.string().refine(
        (value) => parseRandToCents(value) !== null,
        "Enter a valid amount greater than R0.00."
    ),

    // A Treasurer may select a member when recording a contribution.
    // Regular members won't need to select another member.
    userId: z.number()
        .int()
        .positive("Select a valid member.")
        .optional(),
});

// Infer the TypeScript type directly from the validation rules.
// This keeps the type consistent with the schema.
export type ContributionFormValues = z.infer<
    typeof contributionSchema
>;