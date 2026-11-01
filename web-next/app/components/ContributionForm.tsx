"use client";

import { useActionState, useEffect, useRef } from "react";
// Import the Server Action and its TypeScript state definition.
import {
    recordContribution,
    type ContributionActionState,
} from "@/app/actions/contributions";

// Define the initial form state inside the Client Component.
// Server Action files must not export ordinary objects.
const initialContributionState: ContributionActionState = {
    success: false,
    message: "",
};

// Describe the information the stokvel page supplies to this form.
type ContributionFormProps = {
    stokvelId: number;
    cycleId: number;
    members: Array<{
        id: number;
        firstName: string;
        lastName: string;
    }>;
    role: string;
};

// Display a form for recording contributions.
export default function ContributionForm({
    stokvelId,
    cycleId,
    members,
    role,
}: ContributionFormProps) {
    // React manages the Server Action result and pending submission state.
    const [state, formAction, isPending] = useActionState(
        recordContribution,
        initialContributionState
    );

    // Keep the same key across retries until the form details change.
    const idempotencyKey = useRef(crypto.randomUUID());

    // Remember the last submitted contribution details.
    const previousDetails = useRef("");

    // Remember whether the previous request had an uncertain outcome.
    const uncertainOutcome = useRef(false);

    // Keep a reference to the form so we can clear it after success.
    const formRef = useRef<HTMLFormElement>(null);

    // Track which successful action result we have already processed.
    const lastSuccessfulState = useRef<typeof state | null>(null);

    useEffect(() => {
        // Preserve the key when the backend result is uncertain.
        if (state.uncertain) {
            uncertainOutcome.current = true;
            return;
        }

        // Reset the form only after a newly confirmed success.
        if (state.success && lastSuccessfulState.current !== state) {
            idempotencyKey.current = crypto.randomUUID();
            previousDetails.current = "";
            uncertainOutcome.current = false;

            // Clear the member selection and amount.
            formRef.current?.reset();

            lastSuccessfulState.current = state;
        }
    }, [state]);

    // Prepare a stable idempotency key for each distinct contribution.
    function prepareSubmission(formData: FormData) {
        const details = JSON.stringify({
            stokvelId,
            cycleId,
            amount: String(formData.get("amount") ?? ""),
            userId: String(formData.get("userId") ?? ""),
        });

        // A changed contribution requires a new idempotency key.
        if (details !== previousDetails.current) {
            idempotencyKey.current = crypto.randomUUID();
            previousDetails.current = details;
            uncertainOutcome.current = false;
        }

        // An unchanged retry keeps its original key.
        formData.set("idempotencyKey", idempotencyKey.current);

        return formAction(formData);
    }

    // Only Treasurers can choose which member receives the contribution.
    const isTreasurer = role === "Treasurer";

    return (
        <form
            ref={formRef}
            action={prepareSubmission}
            className="space-y-4 rounded-lg border p-4"
        >
            <h2 className="text-lg font-semibold">
                Record Contribution
            </h2>

            {/* Identify the stokvel and contribution cycle. */}
            <input type="hidden" name="stokvelId" value={stokvelId} />
            <input type="hidden" name="cycleId" value={cycleId} />

            {/* Treasurers may record contributions for other members. */}
            {isTreasurer && (
                <div>
                    <label
                        htmlFor="contribution-user"
                        className="mb-1 block text-sm font-medium"
                    >
                        Member
                    </label>

                    <select
                        id="contribution-user"
                        name="userId"
                        required
                        className="w-full rounded border p-2"
                    >
                        <option value="">Select a member</option>

                        {members.map((member) => (
                            <option key={member.id} value={member.id}>
                                {member.firstName} {member.lastName}
                            </option>
                        ))}
                    </select>
                </div>
            )}

            {/* Accept the amount in rand; the Server Action converts it to cents. */}
            <div>
                <label
                    htmlFor="contribution-amount"
                    className="mb-1 block text-sm font-medium"
                >
                    Amount (R)
                </label>

                <input
                    id="contribution-amount"
                    name="amount"
                    type="text"
                    inputMode="decimal"
                    placeholder="250.00"
                    required
                    className="w-full rounded border p-2"
                />
            </div>

            {/* Show the result returned by the Server Action. */}
            {state.message && (
                <p
                    role="status"
                    className={
                        state.success
                            ? "text-sm text-green-700"
                            : "text-sm text-red-700"
                    }
                >
                    {state.message}
                </p>
            )}

            {/* Show the backend reference to help investigate failed requests. */}
            {state.correlationId && (
                <p className="text-xs text-gray-400">
                    Reference: {state.correlationId}
                </p>
            )}

            {/* Disable the button while the request is processing. */}
            <button
                type="submit"
                disabled={isPending}
                className="rounded bg-blue-600 px-4 py-2 text-white disabled:opacity-50"
            >
                {isPending ? "Recording..." : "Record Contribution"}
            </button>
        </form>
    );
}