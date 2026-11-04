"use client";

import Link from "next/link";
import Form from "next/form";
import { useActionState, useEffect, useRef } from "react";

import { joinStokvel } from "@/app/actions/join";
import {
    joinUrl,
    transition,
    type InvitePreview,
    type JoinStep
} from "@/lib/join-flow";

import { formatCents } from "@/lib/formatters";
import type { ActionResult } from "@/lib/workflow-types";
import Feedback from "@/app/components/Feedback";

// Properties received from the server-rendered join page.
type JoinFlowProps = {
    code: string;
    step: JoinStep;
    preview?: InvitePreview;
    error: string;
    failure?: ActionResult;
};

// Displays the three-step stokvel joining process.
export default function JoinFlow({
    code,
    step,
    preview,
    error,
    failure
}: JoinFlowProps) {

    // Track the join Server Action's result and loading state.
    const [state, action, pending] = useActionState(
        joinStokvel,
        { status: 0, message: "" }
    );

    // Reference to the page heading for keyboard accessibility.
    const heading = useRef<HTMLHeadingElement>(null);

    // The three steps in the joining process.
    const steps: JoinStep[] = ["code", "rules", "confirm"];

    // Find the current step's position.
    const index = steps.indexOf(step);

    // Move keyboard focus to the heading when the step changes.
    useEffect(() => {
        heading.current?.focus();
    }, [step, code]);

    return (
        <div className="space-y-5">

            {/* Show progress through the three joining steps. */}
            <ol className="flex gap-6" aria-label="Join progress">
                {steps.map((item, i) => (
                    <li
                        key={item}
                        aria-current={step === item ? "step" : undefined}
                    >
                        {i + 1}. {item}
                    </li>
                ))}
            </ol>

            {/* Display the current step and provide a focus target. */}
            <h1
                ref={heading}
                tabIndex={-1}
                className="text-2xl font-bold"
            >
                Step {index + 1} of 3:{" "}
                {step === "code"
                    ? "Invite code"
                    : step === "rules"
                        ? "Rules and contributions"
                        : "Confirm your membership"}
            </h1>

            {/* Display invitation errors, such as expired or invalid codes. */}
            {error && (
                <p role="alert">{error}</p>
            )}

            {/* Display unexpected errors returned by the backend. */}
            {failure && <Feedback result={failure} />}

            {step === "code" ? (

                // STEP 1: Allow the user to enter an invitation code.
                <Form
                    action="/join"
                    replace={false}
                    className="space-y-3"
                >
                    {/* Continue to the rules step after submitting a code. */}
                    <input
                        type="hidden"
                        name="step"
                        value="rules"
                    />

                    <label
                        htmlFor="invite-code"
                        className="block"
                    >
                        Invite code
                    </label>

                    <input
                        key={code}
                        id="invite-code"
                        name="code"
                        defaultValue={code}
                        required
                        className="w-full border rounded p-2"
                    />

                    <button className="border rounded p-2">
                        Check invite
                    </button>
                </Form>

            ) : preview && (
                <>

                    {/* Display the stokvel information without private member data. */}
                    <h2 className="text-xl">
                        {preview.name}
                    </h2>

                    <p>
                        {formatCents(preview.contributionAmountCents)}
                        {" / "}
                        {preview.schedule}
                    </p>

                    <p>
                        {preview.memberCount} members
                    </p>

                    {/* Preserve line breaks in the stokvel rules. */}
                    <p className="whitespace-pre-wrap">
                        {preview.rules}
                    </p>

                    {preview.alreadyMember ? (

                        // Users who already belong to the stokvel cannot join again.
                        <p>
                            You&apos;re already a member.{" "}
                            <Link
                                className="underline"
                                href={`/stokvels/${preview.stokvelId}`}
                            >
                                Open stokvel
                            </Link>
                        </p>

                    ) : step === "rules" ? (

                        // STEP 2: Show the rules before moving to confirmation.
                        <Link
                            className="inline-block border rounded p-2"
                            href={joinUrl(code, transition(step, "next"))}
                        >
                            I agree, continue
                        </Link>

                    ) : (

                        // STEP 3: Require explicit acceptance before joining.
                        <form action={action}>

                            {/* Send the invitation code to the Server Action. */}
                            <input
                                type="hidden"
                                name="code"
                                value={code}
                            />

                            <p className="mb-3">
                                Join under these rules and contribution schedule?
                            </p>

                            {/* The user must accept the rules to submit this form. */}
                            <label className="mb-3 flex items-center gap-2">
                                <input
                                    type="checkbox"
                                    name="acceptedRules"
                                    required
                                />
                                I have read and accept the stokvel rules.
                            </label>

                            {/* Disable submission while joining or after success. */}
                            <button
                                disabled={pending || state.status === 200}
                                className="border rounded p-2 disabled:opacity-50"
                            >
                                {pending ? "Joining..." : "Confirm and join"}
                            </button>

                            {/* Provide a link to the stokvel after successful joining. */}
                            {state.stokvelId && (
                                <Link
                                    className="block underline"
                                    href={`/stokvels/${state.stokvelId}`}
                                >
                                    Open your stokvel
                                </Link>
                            )}

                        </form>
                    )}
                </>
            )}

            {/* Display success messages or Server Action validation errors. */}
            <Feedback result={state} />

            {/* Allow users to return to the previous step. */}
            {step !== "code" && (
                <Link
                    className="block underline"
                    href={joinUrl(code, transition(step, "back"))}
                >
                    Back
                </Link>
            )}

        </div>
    );
}