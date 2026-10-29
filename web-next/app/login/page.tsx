import { login } from "@/app/actions/auth";

export default function LoginPage() {
    return (
        <main>
            <h1>Sign in to RondiTrack</h1>

            <form action={login}>
                <div>
                    <label htmlFor="email">Email</label>
                    <input
                        id="email"
                        name="email"
                        type="email"
                        autoComplete="email"
                        required
                    />
                </div>

                <div>
                    <label htmlFor="password">Password</label>
                    <input
                        id="password"
                        name="password"
                        type="password"
                        autoComplete="current-password"
                        required
                    />
                </div>

                <button type="submit">Sign in</button>
            </form>
        </main>
    );
}