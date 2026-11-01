// Import the new login form that uses React's useActionState.
import LoginForm from "./LoginForm";
import Link from "next/link";

// Display the login page.
export default function LoginPage() {
    return (
        <main className="mx-auto max-w-md p-6">
            <h1 className="text-2xl font-bold">
                Sign in to RondiTrack
            </h1>

            {/* Display the interactive login form. */}
            <LoginForm />

            <p className="mt-6 text-sm">
                Don&apos;t have an account?{" "}
                <Link href="/register" className="text-blue-500 underline">
                    Create an account
                </Link>
            </p>

        </main>

    );
}