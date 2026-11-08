"use client";

import {
    startTransition,
    useActionState,
    useEffect,
    useRef,
    useState,
    type FormEvent,
} from "react";

import {
    recordContribution,
    type ContributionActionState,
} from "@/app/actions/contributions";

// Initial state before the Server Action returns a result.
const initialContributionState: ContributionActionState = {
    success: false,
    message: "",
};

// Information supplied by the parent stokvel page.
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

// Form for recording a contribution in the current cycle.
export default function ContributionForm({
    stokvelId,
    cycleId,
    members,
    role,
}: ContributionFormProps) {
    // Track the result and pending state of the Server Action.
    const [state, formAction, isPending] = useActionState(
        recordContribution,
        initialContributionState
    );

    // Preserve user input when a contribution fails.
    const [amount, setAmount] = useState("");
    const [userId, setUserId] = useState("");

    // Track the idempotency key and its associated contribution.
    const idempotencyKey = useRef(crypto.randomUUID());
    const previousDetails = useRef("");

    // Track the last successful result to avoid duplicate resets.
    const lastSuccessfulState = useRef<ContributionActionState | null>(
        null
    );

    // Keep a reference to the member dropdown.
    const memberSelectRef = useRef<HTMLSelectElement>(null);

    const isTreasurer = role === "Treasurer";

    // Handle completed Server Action results.
    useEffect(() => {
        if (state.success) {
            // Only process each successful result once.
            if (lastSuccessfulState.current === state) {
                return;
            }

            lastSuccessfulState.current = state;

            // A new contribution requires a new idempotency key.
            idempotencyKey.current = crypto.randomUUID();
            previousDetails.current = "";

            // Clear the fields after confirmed success.
            setAmount("");
            setUserId("");

            if (memberSelectRef.current) {
                memberSelectRef.current.value = "";
            }

            return;
        }

        // Restore the selected member after an unsuccessful submission.
        // This also handles cases where React resets the native select.
        if (memberSelectRef.current) {
            memberSelectRef.current.value = userId;
        }
    }, [state, userId]);

    // Prepare and submit the contribution.
    function handleSubmit(event: FormEvent<HTMLFormElement>) {
        // Prevent normal browser submission.
        event.preventDefault();

        // Do not allow duplicate submissions while pending.
        if (isPending) {
            return;
        }

        const formData = new FormData(event.currentTarget);

        // Identify the contribution being submitted.
        const details = JSON.stringify({
            stokvelId,
            cycleId,
            amount: String(formData.get("amount") ?? ""),
            userId: String(formData.get("userId") ?? ""),
        });

        // Different details require a different idempotency key.
        if (details !== previousDetails.current) {
            idempotencyKey.current = crypto.randomUUID();
            previousDetails.current = details;
        }

        // Retrying the same details reuses the existing key.
        formData.set("idempotencyKey", idempotencyKey.current);

        // Execute the action inside a transition to preserve pending state.
        startTransition(() => {
            formAction(formData);
        });
    }

    return (
        <form
            onSubmit={handleSubmit}
            className="space-y-4 rounded-lg border p-4"
        >
            <h2 className="text-lg font-semibold">
                Record Contribution
            </h2>

            {/* Announce submission results to screen readers. */}
            {state.message && (
                <div role={state.success ? "status" : "alert"}>
                    <p>{state.message}</p>

                    {state.correlationId && (
                        <p>Reference: {state.correlationId}</p>
                    )}
                </div>
            )}

            {/* Identify the stokvel and cycle. */}
            <input
                type="hidden"
                name="stokvelId"
                value={stokvelId}
            />

            <input
                type="hidden"
                name="cycleId"
                value={cycleId}
            />

            {/* Only treasurers can select another member. */}
            {isTreasurer && (
                <div>
                    <label
                        htmlFor="contribution-user"
                        className="mb-1 block text-sm font-medium"
                    >
                        Member
                    </label>

                    <select
                        ref={memberSelectRef}
                        id="contribution-user"
                        name="userId"
                        required
                        value={userId}
                        onChange={(event) => setUserId(event.target.value)}
                        aria-invalid={Boolean(state.fieldErrors?.userId)}
                        aria-describedby={
                            state.fieldErrors?.userId
                                ? "contribution-user-error"
                                : undefined
                        }
                        className="w-full rounded border p-2"
                    >
                        <option value="">Select a member</option>

                        {members.map((member) => (
                            <option key={member.id} value={member.id}>
                                {member.firstName} {member.lastName}
                            </option>
                        ))}
                    </select>

                    {/* Associate member errors with the dropdown. */}
                    {state.fieldErrors?.userId && (
                        <p id="contribution-user-error" role="alert">
                            {state.fieldErrors.userId}
                        </p>
                    )}
                </div>
            )}

            {/* Enter the contribution amount in rand. */}
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
                    value={amount}
                    onChange={(event) => setAmount(event.target.value)}
                    aria-invalid={Boolean(state.fieldErrors?.amount)}
                    aria-describedby={
                        state.fieldErrors?.amount
                            ? "contribution-amount-error"
                            : undefined
                    }
                    className="w-full rounded border p-2"
                />

                {/* Associate amount errors with the input. */}
                {state.fieldErrors?.amount && (
                    <p id="contribution-amount-error" role="alert">
                        {state.fieldErrors.amount}
                    </p>
                )}
            </div>

            {/* Prevent duplicate submissions while pending. */}
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