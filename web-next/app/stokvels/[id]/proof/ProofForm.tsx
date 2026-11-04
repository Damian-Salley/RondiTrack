"use client";

import { useActionState, useEffect, useRef, useState } from "react";
import { useRouter } from "next/navigation";

// Server Action responsible for uploading and removing proof.
import { saveProof } from "@/app/actions/proofs";

// Validates the selected file's size and type.
import { proofError } from "@/lib/proof-validation";

// Displays success messages and errors.
import Feedback from "@/app/components/Feedback";

// Defines the result returned by the proof Server Action.
import type { ActionResult } from "@/lib/workflow-types";

// Information received from the backend through the proof page.
type ProofFormProps = {
    stokvelId: number;
    cycleId: number;
    memberId: number;
    hasProof: boolean;
    canUpload: boolean;
    locked: boolean;
};

// Allows authorized users to upload, replace, remove or download proof.
export default function ProofForm({
    stokvelId,
    cycleId,
    memberId,
    hasProof,
    canUpload,
    locked
}: ProofFormProps) {

    // Access Next.js navigation so we can refresh server-rendered data.
    const router = useRouter();

    // Track the Server Action's result and submission state.
    const [state, action, pending] = useActionState(
        saveProof,
        { status: 0, message: "" }
    );

    // Store frontend file-validation errors.
    const [error, setError] = useState("");

    // Reference the file input so we can inspect and clear its value.
    const fileInput = useRef<HTMLInputElement>(null);

    // Remember the last successful result we handled.
    const lastHandledResult = useRef<ActionResult | null>(null);

    // Refresh proof information after a successful upload or removal.
    useEffect(() => {

        // Ignore unsuccessful results and results already handled.
        if (
            state.status !== 200 ||
            lastHandledResult.current === state
        ) {
            return;
        }

        // Mark this result as handled to avoid repeated refreshes.
        lastHandledResult.current = state;

        // Clear the selected file and any previous validation error.
        if (fileInput.current) {
            fileInput.current.value = "";
        }

        setError("");

        // Refresh the server-rendered page to retrieve updated proof details.
        router.refresh();

    }, [state, router]);

    return (
        <div className="space-y-4">

            {/* Display a secure download link when proof exists. */}
            {hasProof ? (
                <a
                    className="block underline"
                    href={`/bff/stokvels/${stokvelId}/cycles/${cycleId}/contributions/${memberId}/proof`}
                >
                    Download proof of payment
                </a>
            ) : (
                <p>No proof uploaded yet.</p>
            )}

            {/* Explain why proof can no longer be changed. */}
            {(locked || state.status === 409) && (
                <p>
                    The cycle is closed. Proof is locked.
                </p>
            )}

            {/* Only display upload controls when the user has permission. */}
            {canUpload && !locked && state.status !== 409 && (
                <form
                    action={action}
                    className="space-y-3"
                    onSubmit={event => {

                        // Identify whether the user clicked the Remove button.
                        const submitter = (
                            event.nativeEvent as SubmitEvent
                        ).submitter;

                        // Removing proof does not require file validation.
                        if (
                            submitter?.getAttribute("value") === "remove"
                        ) {
                            return;
                        }

                        // Validate the selected file before submission.
                        const message = proofError(
                            fileInput.current?.files?.[0] ?? null
                        );

                        // Display the validation message, if any.
                        setError(message);

                        // Prevent uploading an invalid file.
                        if (message) {
                            event.preventDefault();
                        }
                    }}
                >

                    {/* Identify the contribution being updated. */}
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

                    <input
                        type="hidden"
                        name="memberId"
                        value={memberId}
                    />

                    {/* Explain the accepted file types and maximum size. */}
                    <label
                        htmlFor="proof-file"
                        className="block"
                    >
                        JPEG, PNG or PDF, up to 5 MiB
                    </label>

                    {/* Allow users to select a proof-of-payment file. */}
                    <input
                        ref={fileInput}
                        id="proof-file"
                        name="file"
                        type="file"
                        disabled={pending}
                        accept="image/jpeg,image/png,application/pdf"
                        aria-describedby="proof-error"
                        aria-invalid={!!error || state.status === 400}
                        onChange={event => {

                            // Validate the file immediately after selection.
                            setError(
                                proofError(
                                    event.target.files?.[0] ?? null
                                )
                            );
                        }}
                    />

                    {/* Display file-validation errors near the input. */}
                    <p id="proof-error" role="alert">
                        {error || (
                            state.status === 400
                                ? state.message
                                : ""
                        )}
                    </p>

                    {/* Provide upload, replacement and removal controls. */}
                    <div className="flex gap-3">

                        <button
                            type="submit"
                            disabled={pending}
                            className="rounded border p-2"
                        >
                            {pending
                                ? "Saving..."
                                : hasProof
                                    ? "Replace proof"
                                    : "Upload proof"}
                        </button>

                        {/* Only show Remove when proof already exists. */}
                        {hasProof && (
                            <button
                                type="submit"
                                name="operation"
                                value="remove"
                                disabled={pending}
                                className="rounded border p-2"
                            >
                                Remove proof
                            </button>
                        )}

                    </div>
                </form>
            )}

            {/* Display success messages and backend errors. */}
            {state.status !== 400 && (
                <Feedback
                    result={state}
                    successToast
                />
            )}

        </div>
    );
}