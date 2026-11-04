"use client";

import { useState } from "react";
import Link from "next/link";
import { usePathname, useSearchParams } from "next/navigation";

import type { ActionResult } from "@/lib/workflow-types";

// Displays feedback from Server Actions.
//
// Expected errors appear next to the relevant interface.
// Successful operations can display a toast.
// Unexpected server failures always display persistent,
// explicitly dismissible notifications.
export default function Feedback({
    result,
    successToast = false
}: {
    result: ActionResult;
    successToast?: boolean;
}) {

    // Track which notification the user dismissed.
    const [dismissed, setDismissed] = useState<ActionResult | null>(null);

    // Read the current location so users can return after signing in.
    const pathname = usePathname();
    const searchParams = useSearchParams();

    // Preserve the current path and query parameters.
    const currentPath = searchParams.toString()
        ? `${pathname}?${searchParams.toString()}`
        : pathname;

    // Build a login URL with a safe, relative return destination.
    const loginUrl = `/login?${new URLSearchParams({
        next: currentPath
    }).toString()}`;

    // Do not display empty or previously dismissed feedback.
    if (!result.message || dismissed === result) {
        return null;
    }

    // Unexpected failures always use persistent toasts.
    // Successful results use toasts when requested by the caller.
    const toast =
        result.status >= 500 ||
        (
            successToast &&
            result.status >= 200 &&
            result.status < 300
        );

    return (
        <div
            role={result.status >= 400 ? "alert" : "status"}
            className={
                toast
                    ? "fixed bottom-4 right-4 z-50 max-w-md rounded border bg-gray-950 p-4 text-white shadow-lg"
                    : "my-3"
            }
        >

            {/* Display the result message. */}
            <p>{result.message}</p>

            {/* Show a reference for unexpected server failures. */}
            {result.status >= 500 && (
                <p className="text-sm">
                    Reference: {result.correlationId ?? "unavailable"}
                </p>
            )}

            {/* Keep toast notifications visible until dismissed. */}
            {toast && (
                <button
                    type="button"
                    className="mt-2 underline"
                    onClick={() => setDismissed(result)}
                >
                    Dismiss notification
                </button>
            )}

            {/* Allow users with expired sessions to sign in again. */}
            {result.status === 401 && (
                <Link
                    className="underline"
                    href={loginUrl}
                >
                    Sign in again
                </Link>
            )}

        </div>
    );
}