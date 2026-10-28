import { useRef, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import {
    useMe,
    useMembers,
    useRecordContribution,
} from "../api/queries";
import { parseRandToCents } from "../utils/formatters";

const contributionSchema = z.object({
    userId: z.number().int().positive("Select a valid member.").optional(),
    amount: z.string().refine(
        (value) => parseRandToCents(value) !== null,
        "Enter a valid amount greater than R0.00."
    ),
});

type ContributionFormValues = z.infer<typeof contributionSchema>;

interface RecordContributionFormProps {
    stokvelId: number;
    cycleId: number;
    stokvelName: string;
    cycleNumber: number;
    cycleStatus: string;
    role: string;
}

interface PendingContribution {
    stokvelId: number;
    cycleId: number;
    userId: number;
    amountInCents: number;
    idempotencyKey: string;
}

export function RecordContributionForm({
    stokvelId,
    cycleId,
    stokvelName,
    cycleNumber,
    cycleStatus,
    role,
}: RecordContributionFormProps) {
    const me = useMe();
    const isTreasurer = role === "Treasurer";
    const members = useMembers(isTreasurer ? stokvelId : null);
    const mutation = useRecordContribution();

    const pendingContribution = useRef<PendingContribution | null>(null);
    const [message, setMessage] = useState("");

    const {
        register,
        handleSubmit,
        reset,
        formState: { errors },
    } = useForm<ContributionFormValues>({
        resolver: zodResolver(contributionSchema),
        defaultValues: {
            amount: "",
        },
    });

    const isActive = cycleStatus === "Active";
    const currentUserId = Number(me.data?.id);

    async function submitContribution(contribution: PendingContribution) {
        setMessage("");

        try {
            await mutation.mutateAsync(contribution);

            pendingContribution.current = null;
            reset();

            setMessage("Contribution recorded successfully.");
        } catch {
            setMessage(
                "The contribution could not be confirmed. " +
                "You can retry the same submission safely."
            );
        }
    }

    function onSubmit(values: ContributionFormValues) {
        if (!isActive || mutation.isPending) return;

        // Do not create another submission while the previous result is uncertain.
        if (pendingContribution.current !== null) return;

        const amountInCents = parseRandToCents(values.amount);

        if (amountInCents === null) return;

        const userId = isTreasurer ? values.userId : currentUserId;

        if (
            userId === undefined ||
            !Number.isSafeInteger(userId) ||
            userId <= 0
        ) {
            return;
        }

        const contribution: PendingContribution = {
            stokvelId,
            cycleId,
            userId,
            amountInCents,
            idempotencyKey: crypto.randomUUID(),
        };

        // Keep the same key until the result is confirmed.
        pendingContribution.current = contribution;

        void submitContribution(contribution);
    }

    function retrySubmission() {
        if (mutation.isPending || !pendingContribution.current) return;

        void submitContribution(pendingContribution.current);
    }

    const hasPendingSubmission = pendingContribution.current !== null;

    return (
        <section aria-labelledby="record-contribution-heading">
            <h3 id="record-contribution-heading">Record Contribution</h3>

            <p>
                <strong>Stokvel:</strong> {stokvelName}
            </p>

            <p>
                <strong>Cycle:</strong> {cycleNumber}
            </p>

            {!isActive && (
                <p role="alert">
                    Contributions cannot be recorded because this cycle is not active.
                </p>
            )}

            {me.isError && (
                <p role="alert">Unable to identify the logged-in member.</p>
            )}

            {isTreasurer && members.isError && (
                <p role="alert">Unable to load stokvel members.</p>
            )}

            <form onSubmit={handleSubmit(onSubmit)}>
                {isTreasurer ? (
                    <>
                        <label htmlFor="contribution-member">Member</label>

                        <select
                            id="contribution-member"
                            disabled={
                                !isActive ||
                                !members.isSuccess ||
                                mutation.isPending ||
                                hasPendingSubmission
                            }
                            aria-invalid={Boolean(errors.userId)}
                            {...register("userId", { valueAsNumber: true })}
                        >
                            <option value="">Select a member</option>

                            {(members.data ?? []).map((member) => (
                                <option key={member.id} value={Number(member.id)}>
                                    {member.firstName} {member.lastName}
                                </option>
                            ))}
                        </select>

                        {errors.userId && (
                            <p role="alert">{errors.userId.message}</p>
                        )}
                    </>
                ) : (
                    <p>
                        <strong>Member:</strong>{" "}
                        {me.data
                            ? `${me.data.firstName} ${me.data.lastName}`
                            : "Loading..."}
                    </p>
                )}

                <label htmlFor="contribution-amount">Amount (ZAR)</label>

                <input
                    id="contribution-amount"
                    type="text"
                    inputMode="decimal"
                    placeholder="150.50"
                    disabled={
                        !isActive ||
                        mutation.isPending ||
                        hasPendingSubmission
                    }
                    aria-invalid={Boolean(errors.amount)}
                    {...register("amount")}
                />

                {errors.amount && (
                    <p role="alert">{errors.amount.message}</p>
                )}

                <button
                    type="submit"
                    disabled={
                        !isActive ||
                        !me.isSuccess ||
                        (isTreasurer && !members.isSuccess) ||
                        mutation.isPending ||
                        hasPendingSubmission
                    }
                >
                    {mutation.isPending
                        ? "Recording..."
                        : "Record Contribution"}
                </button>
            </form>

            {message && (
                <p role="status">{message}</p>
            )}

            {hasPendingSubmission && !mutation.isPending && (
                <button
                    type="button"
                    onClick={retrySubmission}
                >
                    Retry Submission
                </button>
            )}
        </section>
    );
}