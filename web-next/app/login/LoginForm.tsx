"use client";

import { useSearchParams } from "next/navigation";
import { safeNext } from "@/lib/safe-next";
import { useActionState } from "react";

import {
    loginWithState,
    type LoginState,
} from "@/app/actions/auth";

// Initial state before the user attempts to sign in.
const initialState: LoginState = {
    error: "",
};

// Displays the login form and handles its submission.
export default function LoginForm() {

    // Read query parameters from the current page URL.
    // The "next" parameter tells us where to go after login.
    const params = useSearchParams();

    // Connect the form to the login Server Action.
    // React tracks the result and whether the request is still running.
    const [state, formAction, isPending] = useActionState(
        loginWithState,
        initialState
    );

    return (
        <form action={formAction} className="mt-6 space-y-4">

            {/* Preserve a safe internal destination after login. */}
            <input
                type="hidden"
                name="next"
                value={safeNext(params.get("next"))}
            />

            {/* Collect the user's email address. */}
            <div>
                <label htmlFor="email" className="block text-sm">
                    Email
                </label>

                <input
                    id="email"
                    name="email"
                    type="email"
                    autoComplete="email"
                    required
                    className="mt-1 w-full rounded border p-2"
                />
            </div>

            {/* Collect the password without displaying its contents. */}
            <div>
                <label htmlFor="password" className="block text-sm">
                    Password
                </label>

                <input
                    id="password"
                    name="password"
                    type="password"
                    autoComplete="current-password"
                    required
                    className="mt-1 w-full rounded border p-2"
                />
            </div>

            {/* Display authentication errors without leaving the page. */}
            {state.error && (
                <p
                    role="alert"
                    className="text-sm text-red-500"
                >
                    {state.error}
                </p>
            )}

            {/* Prevent repeated submissions while login is processing. */}
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