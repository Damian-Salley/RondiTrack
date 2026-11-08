"use client";

import { useActionState } from "react";
import { useSearchParams } from "next/navigation";

import { safeNext } from "@/lib/safe-next";

import {
    loginWithState,
    type LoginState,
} from "@/app/actions/auth";

// Initial state before the user attempts to sign in.
const initialState: LoginState = {
    error: "",
};

// Display the login form and handle authentication.
export default function LoginForm() {
    // Read query parameters from the current page URL.
    const params = useSearchParams();

    // Determine whether the user was redirected because
    // their previous session expired.
    const sessionExpired = params.get("expired") === "1";

    // Connect the form to the login Server Action.
    //
    // state: The result of the most recent login attempt.
    // formAction: The function called when the form is submitted.
    // isPending: Whether the login request is still processing.
    const [state, formAction, isPending] = useActionState(
        loginWithState,
        initialState
    );

    return (
        <form
            action={formAction}
            className="mt-6 space-y-4"
        >
            {/* Preserve a safe internal destination after login. */}
            <input
                type="hidden"
                name="next"
                value={safeNext(params.get("next"))}
            />

            {/* Explain why the user was redirected to login. */}
            {sessionExpired && !state.error && (
                <p
                    role="status"
                    className="text-sm text-amber-700"
                >
                    Your session has expired. Please sign in again.
                </p>
            )}

            {/* Collect the user's email address. */}
            <div>
                <label
                    htmlFor="email"
                    className="block text-sm"
                >
                    Email
                </label>

                <input
                    id="email"
                    name="email"
                    type="email"
                    autoComplete="username"
                    required
                    aria-invalid={Boolean(state.error)}
                    aria-describedby={
                        state.error ? "login-error" : undefined
                    }
                    className="mt-1 w-full rounded border p-2"
                />
            </div>

            {/* Collect the user's password securely. */}
            <div>
                <label
                    htmlFor="password"
                    className="block text-sm"
                >
                    Password
                </label>

                <input
                    id="password"
                    name="password"
                    type="password"
                    autoComplete="current-password"
                    required
                    aria-invalid={Boolean(state.error)}
                    aria-describedby={
                        state.error ? "login-error" : undefined
                    }
                    className="mt-1 w-full rounded border p-2"
                />
            </div>

            {/* Announce authentication errors to screen readers. */}
            {state.error && (
                <p
                    id="login-error"
                    role="alert"
                    className="text-sm text-red-600"
                >
                    {state.error}
                </p>
            )}

            {/* Prevent duplicate login attempts while processing. */}
            <button
                type="submit"
                disabled={isPending}
                className="rounded bg-blue-600 px-4 py-2 text-white disabled:opacity-50"
            >
                {isPending ? "Signing in..." : "Sign in"}
            </button>
        </form>
    );
}