"use client";

import Link from "next/link";
import { useActionState, useState } from "react";

import { createInvite } from "@/app/actions/join";
import Feedback from "./Feedback";

// Allows a treasurer to create a seven-day invitation link.
export default function CreateInvite({
    stokvelId
}: {
    stokvelId: number;
}) {

    // Track the result of creating an invitation.
    const [state, action, pending] = useActionState(
        createInvite,
        {
            status: 0,
            message: ""
        }
    );

    // Display feedback after copying the invitation link.
    const [copied, setCopied] = useState(false);

    // Copy the generated invitation link to the clipboard.
    async function copyInvite() {
        try {
            // Convert the relative invitation URL into a complete URL.
            const inviteUrl = new URL(
                state.message,
                window.location.origin
            ).toString();

            // Copy the URL so it can be shared with another member.
            await navigator.clipboard.writeText(inviteUrl);

            setCopied(true);

        } catch {
            // Display a useful message if clipboard access fails.
            setCopied(false);
        }
    }

    return (
        <details className="my-4 rounded border p-4">

            {/* Expand or collapse the invitation form. */}
            <summary className="cursor-pointer">
                Create a seven-day invite
            </summary>

            {/* Submit the rules and schedule to the Server Action. */}
            <form
                action={action}
                className="mt-3 space-y-3"
            >

                {/* Identify which stokvel the invitation belongs to. */}
                <input
                    type="hidden"
                    name="stokvelId"
                    value={stokvelId}
                />

                {/* Require the treasurer to provide the stokvel rules. */}
                <label className="block">
                    Rules

                    <textarea
                        name="rules"
                        required
                        maxLength={4000}
                        className="block w-full rounded border p-2"
                    />
                </label>

                {/* Require a contribution schedule for prospective members. */}
                <label className="block">
                    Contribution schedule

                    <input
                        name="schedule"
                        required
                        maxLength={200}
                        placeholder="Monthly, by the last day of the month"
                        className="block w-full rounded border p-2"
                    />
                </label>

                {/* Prevent duplicate submissions while creating the invite. */}
                <button
                    type="submit"
                    disabled={pending}
                    className="rounded border p-2"
                >
                    {pending ? "Creating..." : "Create invite"}
                </button>

                {/* Show the invitation link after successful creation. */}
                {state.status === 200 ? (
                    <div className="space-y-2">

                        <p>Invitation created successfully.</p>

                        <p className="break-all">
                            <Link
                                className="underline"
                                href={state.message}
                            >
                                {state.message}
                            </Link>
                        </p>

                        {/* Copy the full invitation URL for sharing. */}
                        <button
                            type="button"
                            onClick={() => void copyInvite()}
                            className="rounded border p-2"
                        >
                            Copy invite link
                        </button>

                        {/* Announce successful copying to screen readers. */}
                        <p role="status">
                            {copied ? "Link copied!" : ""}
                        </p>

                    </div>
                ) : (

                    // Display errors returned by the Server Action.
                    <Feedback result={state} />

                )}

            </form>
        </details>
    );
}