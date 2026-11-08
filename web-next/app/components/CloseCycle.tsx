"use client";

import { useRef, useState } from "react";

import { closeCycle, loadClosePreview } from "@/app/actions/closing";
import { formatCents } from "@/lib/formatters";
import type { ActionResult, ClosePreview } from "@/lib/workflow-types";
import Feedback from "./Feedback";

// Displays the treasurer's confirmation dialog for closing a cycle.
export default function CloseCycle({
    cycleId
}: {
    cycleId: number;
}) {

    // References used to control the dialog and keyboard focus.
    const dialog = useRef<HTMLDialogElement>(null);
    const trigger = useRef<HTMLButtonElement>(null);
    const cancel = useRef<HTMLButtonElement>(null);

    // Keep the same idempotency key when retrying an uncertain request.
    const key = useRef("");

    // Prevent duplicate closing requests.
    const submitting = useRef(false);

    // Track the preview version associated with the current key.
    const lastVersion = useRef("");

    // Track whether a preview request is already running.
    const loading = useRef(false);

    // Store the backend-calculated arrears preview.
    const [preview, setPreview] = useState<ClosePreview>();

    // Track loading and submission activity.
    const [busy, setBusy] = useState(false);

    // Store success and error messages.
    const [result, setResult] = useState<ActionResult>({
        status: 0,
        message: ""
    });

    // Load the latest closing preview from the backend.
    async function load() {

        // Avoid sending duplicate preview requests.
        if (loading.current) {
            return;
        }

        loading.current = true;
        setBusy(true);
        setPreview(undefined);

        try {

            // Request the latest arrears calculation.
            const data = await loadClosePreview(cycleId);

            setPreview(data.preview);

            // A new preview version represents a different operation.
            // Generate a new idempotency key only when the version changes.
            if (
                data.preview &&
                data.preview.version !== lastVersion.current
            ) {
                key.current = crypto.randomUUID();
                lastVersion.current = data.preview.version;
            }

            // Display any error returned by the backend.
            if (!data.preview) {
                setResult(data.result);
            }

        } catch {

            // Recover if the Server Action fails unexpectedly.
            setResult({
                status: 500,
                message: "Unable to load the closing preview."
            });

        } finally {

            // Always restore the loading state.
            loading.current = false;
            setBusy(false);
        }
    }

    // Confirm that the treasurer wants to close the cycle.
    async function confirm() {

        // Require a preview and prevent duplicate submissions.
        if (!preview || submitting.current || busy) {
            return;
        }

        submitting.current = true;
        setBusy(true);

        try {

            // Send the preview version and idempotency key.
            //
            // The backend rejects stale versions and ensures
            // repeated requests cannot create duplicate arrears.
            const answer = await closeCycle(
                cycleId,
                preview.version,
                key.current
            );

            setResult(answer);

            if (answer.status === 200) {

                // The cycle has been closed successfully.
                setPreview(undefined);

            } else if (answer.status === 409) {

                // The figures changed since the preview was loaded.
                // A new preview requires another explicit confirmation.
                key.current = crypto.randomUUID();

                await load();

                setResult({
                    status: 409,
                    message:
                        "The contribution figures changed. Review the updated amounts before confirming again."
                });

                // Return keyboard focus to the Cancel button.
                cancel.current?.focus();
            }

        } catch {

            // An unexpected failure may have occurred after the
            // backend processed the request.
            //
            // Keep the current version and idempotency key so
            // a retry can safely use the same operation.
            setResult({
                status: 500,
                message:
                    "The request could not be confirmed. Please retry using the same preview.",
                uncertain: true
            });

        } finally {

            // Always allow further interaction after the request ends.
            submitting.current = false;
            setBusy(false);
        }
    }

    return (
        <>
            {/* Open the treasurer's closing confirmation dialog. */}
            <button
                ref={trigger}
                type="button"
                className="rounded border p-2"
                onClick={() => {

                    dialog.current?.showModal();

                    // Place keyboard focus on Cancel.
                    cancel.current?.focus();

                    // Preserve the existing preview and key when
                    // retrying an uncertain closing operation.
                    if (!result.uncertain || !preview) {
                        setResult({
                            status: 0,
                            message: ""
                        });

                        void load();
                    }
                }}
            >
                Close cycle
            </button>

            {/* Native dialog provides modal behaviour and Escape support. */}
            <dialog
                ref={dialog}
                role="alertdialog"
                aria-labelledby="close-title"
                aria-describedby="close-consequence"
                className="m-auto w-full max-w-xl rounded border bg-gray-950 p-6 text-white backdrop:bg-black/70"
                onClose={() => trigger.current?.focus()}
                onCancel={event => {

                    // Do not dismiss the dialog during a closing request.
                    if (submitting.current) {
                        event.preventDefault();
                    }
                }}
            >

                {/* Describe the operation being confirmed. */}
                <h2
                    id="close-title"
                    className="text-xl font-bold"
                >
                    Close contribution cycle
                </h2>

                {/* Explain the permanent consequences of closing. */}
                <p
                    id="close-consequence"
                    className="my-3"
                >
                    Closing records these arrears and locks payment proofs.
                    This cannot be undone.
                </p>

                {/* Show skeleton rows while the preview loads. */}
                {busy && !preview ? (
                    <div
                        aria-label="Loading close preview"
                        role="status"
                        className="space-y-3"
                    >
                        <p>Loading close preview...</p>

                        {[1, 2, 3].map(row => (
                            <div
                                key={row}
                                className="flex justify-between gap-8 animate-pulse"
                            >
                                <span className="h-6 w-40 rounded bg-gray-700" />
                                <span className="h-6 w-24 rounded bg-gray-700" />
                            </div>
                        ))}
                    </div>
                ) : preview ? (

                    // Display the backend-calculated arrears.
                    <>
                        <p>
                            {preview.cycleName} / As of{" "}
                            {preview.asOfDate} (South Africa)
                        </p>

                        {preview.members.length ? (
                            <ul className="my-3 space-y-2">
                                {preview.members.map(member => (
                                    <li
                                        key={member.userId}
                                        className="flex justify-between gap-4"
                                    >
                                        <span>{member.name}</span>

                                        <span>
                                            {formatCents(member.amountCents)}
                                        </span>
                                    </li>
                                ))}
                            </ul>
                        ) : (
                            <p className="my-3">
                                Nobody goes into arrears.
                            </p>
                        )}

                        <p className="font-bold">
                            Total: {formatCents(preview.totalCents)}
                        </p>
                    </>
                ) : null}

                {/* Display success, failure or stale-preview feedback. */}
                <Feedback result={result} />

                {/* Dialog actions. */}
                <div className="mt-5 flex gap-3">

                    {/* Cancel is the initial focus target. */}
                    <button
                        ref={cancel}
                        autoFocus
                        type="button"
                        disabled={busy && Boolean(preview)}
                        className="rounded border p-2"
                        onClick={() => dialog.current?.close()}
                    >
                        Cancel
                    </button>

                    {/* Require a valid preview before closing the cycle. */}
                    <button
                        type="button"
                        disabled={
                            busy ||
                            !preview ||
                            result.status === 200
                        }
                        className="rounded border p-2 disabled:opacity-50"
                        onClick={() => void confirm()}
                    >
                        {preview
                            ? `Close ${preview.cycleName} and record ${formatCents(preview.totalCents)} in arrears`
                            : "Close cycle"}
                    </button>

                </div>
            </dialog>
        </>
    );
}
