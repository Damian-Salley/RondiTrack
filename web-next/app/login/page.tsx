import { Suspense } from "react";
import LoginForm from "./LoginForm";
import Link from "next/link";

import ClearPinsOnLogin from "@/app/components/ClearPinsOnLogin";

// Displays the login page for users who want to sign in to RondiTrack.
export default function LoginPage() {
    return (
        <main className="mx-auto max-w-md p-6">

            {/* Clear previously stored login-related PIN state. */}
            <ClearPinsOnLogin />

            {/* Main heading for the login page. */}
            <h1 className="text-2xl font-bold">
                Sign in to RondiTrack
            </h1>

            {/* 
                Display the login form.

                Suspense shows a temporary loading message while
                the login form is waiting for asynchronous content.
            */}
            <Suspense fallback={<p>Loading sign in...</p>}>
                <LoginForm />
            </Suspense>

            {/* Provide a registration link for users without an account. */}
            <p className="mt-6 text-sm">
                Don&apos;t have an account?{" "}

                <Link
                    href="/register"
                    className="text-blue-500 underline"
                >
                    Create an account
                </Link>
            </p>

        </main>
    );
}