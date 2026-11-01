"use client";

import { useActionState } from "react";
import {
    loginWithState,
    type LoginState,
} from "@/app/actions/auth";

// The form starts without an authentication error.
const initialState: LoginState = {
    error: "",
};

// Use React to manage login submission and error messages.
export default function LoginForm() {
    const [state, formAction, isPending] = useActionState(
        loginWithState,
        initialState
    );

    return (
        <form action={formAction} className="mt-6 space-y-4">
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

            {/* Show authentication failures without navigating away. */}
            {state.error && (
                <p role="alert" className="text-sm text-red-500">
                    {state.error}
                </p>
            )}

            {/* Prevent repeated submissions while authentication runs. */}
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