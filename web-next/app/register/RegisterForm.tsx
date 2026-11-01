"use client";

import { useActionState } from "react";
import {
    registerUser,
    type RegisterState,
} from "@/app/actions/register";

const initialState: RegisterState = {
    error: "",
};

export default function RegisterForm() {
    const [state, formAction, isPending] = useActionState(
        registerUser,
        initialState
    );

    return (
        <form action={formAction} className="mt-6 space-y-4">
            <div>
                <label htmlFor="firstName" className="block">
                    First name
                </label>
                <input
                    id="firstName"
                    name="firstName"
                    required
                    className="w-full rounded border p-2"
                />
            </div>

            <div>
                <label htmlFor="lastName" className="block">
                    Last name
                </label>
                <input
                    id="lastName"
                    name="lastName"
                    required
                    className="w-full rounded border p-2"
                />
            </div>

            <div>
                <label htmlFor="email" className="block">
                    Email
                </label>
                <input
                    id="email"
                    name="email"
                    type="email"
                    autoComplete="email"
                    required
                    className="w-full rounded border p-2"
                />
            </div>

            <div>
                <label htmlFor="phoneNumber" className="block">
                    Phone number
                </label>
                <input
                    id="phoneNumber"
                    name="phoneNumber"
                    type="tel"
                    autoComplete="tel"
                    required
                    className="w-full rounded border p-2"
                />
            </div>

            <div>
                <label htmlFor="password" className="block">
                    Password
                </label>
                <input
                    id="password"
                    name="password"
                    type="password"
                    autoComplete="new-password"
                    minLength={12}
                    required
                    className="w-full rounded border p-2"
                />
                <p className="mt-1 text-sm text-gray-500">
                    Use at least 12 characters.
                </p>
            </div>

            {state.error && (
                <p role="alert" className="text-sm text-red-500">
                    {state.error}
                </p>
            )}

            <button
                type="submit"
                disabled={isPending}
                className="rounded bg-blue-600 px-4 py-2 text-white disabled:opacity-50"
            >
                {isPending ? "Creating account..." : "Create account"}
            </button>
        </form>
    );
}