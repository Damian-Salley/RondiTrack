"use server";

import { redirect } from "next/navigation";
import { registerSchema } from "@/lib/auth-validation";

// Describe the result returned to the registration form.
export type RegisterState = {
    error: string;
};

// Process a registration request.
export async function registerUser(
    _previousState: RegisterState,
    formData: FormData
): Promise<RegisterState> {
    // Read and validate the submitted fields.
    const result = registerSchema.safeParse({
        firstName: formData.get("firstName"),
        lastName: formData.get("lastName"),
        email: formData.get("email"),
        phoneNumber: formData.get("phoneNumber"),
        password: formData.get("password"),
    });

    if (!result.success) {
        return {
            error: result.error.issues[0]?.message ?? "Invalid registration details.",
        };
    }

    try {
        // Send validated data to the backend.
        const response = await fetch(
            `${process.env.API_BASE_URL}/api/v1/auth/register`,
            {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify(result.data),
                cache: "no-store",
            }
        );

        // Handle duplicate email addresses.
        if (response.status === 409) {
            return { error: "This email address is already registered." };
        }

        // Show the actual HTTP status and backend error while debugging.
        if (!response.ok) {
            const errorBody = await response.text();

            console.error("Registration failed:", {
                status: response.status,
                body: errorBody,
            });

            return {
                error: `Registration failed (HTTP ${response.status}). Check the frontend terminal.`,
            };
        }
    } catch {
        return { error: "Unable to connect to the server. Please try again." };
    }

    // Registration succeeded. The user can now sign in.
    redirect("/login?registered=1");
}