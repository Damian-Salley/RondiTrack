import { z } from "zod";

// Validate all registration fields on the server.
export const registerSchema = z.object({
    firstName: z.string().trim().min(1, "First name is required."),
    lastName: z.string().trim().min(1, "Last name is required."),
    email: z.email("Enter a valid email address."),
    phoneNumber: z.string().trim().min(1, "Phone number is required."),
    password: z.string().min(12, "Password must contain at least 12 characters."),
});