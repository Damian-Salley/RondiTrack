import LoginForm from "./LoginForm";
import Link from "next/link";

import ClearPinsOnLogin from "@/app/components/ClearPinsOnLogin";

export default function LoginPage() {
    return (
        <main className="mx-auto max-w-md p-6">
            <ClearPinsOnLogin />

            <h1 className="text-2xl font-bold">
                Sign in to RondiTrack
            </h1>

            <LoginForm />

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