import Link from "next/link";
import RegisterForm from "./RegisterForm";

export default function RegisterPage() {
    return (
        <main className="mx-auto max-w-md p-6">
            <h1 className="text-2xl font-bold">
                Create your RondiTrack account
            </h1>

            <RegisterForm />

            <p className="mt-6 text-sm">
                Already have an account?{" "}
                <Link href="/login" className="text-blue-500 underline">
                    Sign in
                </Link>
            </p>
        </main>
    );
}