// The contribution form supports validation errors for these two fields.
type ContributionFieldErrors = Partial<
    Record<"amount" | "userId", string>
>;

// Convert backend validation errors into errors understood by the form.
//
// For example:
// Backend: "AmountInCents"
// Frontend: "amount"
//
// The input is unknown because API error responses cannot be trusted
// to always contain the expected structure.
export function contributionFieldErrors(
    problem: unknown
): ContributionFieldErrors {
    // A valid validation response must contain an errors property.
    if (
        !problem ||
        typeof problem !== "object" ||
        !("errors" in problem)
    ) {
        return {};
    }

    const errors = problem.errors;

    // Stop if the errors property is missing or has an invalid type.
    if (!errors || typeof errors !== "object") {
        return {};
    }

    // Store validation messages using the frontend's field names.
    const result: ContributionFieldErrors = {};

    // Examine each field returned by the backend.
    for (const [name, messages] of Object.entries(errors)) {
        // Extract the final property name.
        // For example, "request.AmountInCents" becomes "amountincents".
        const field = name.split(".").at(-1)?.toLowerCase();

        // Translate backend property names into frontend field names.
        const target =
            field === "amountincents" || field === "amount"
                ? "amount"
                : field === "userid"
                    ? "userId"
                    : undefined;

        // Backend validation messages may be an array or a string.
        // Ignore unexpected values rather than displaying invalid data.
        const message = Array.isArray(messages)
            ? messages
                .filter(
                    (value): value is string =>
                        typeof value === "string"
                )
                .join(" ")
            : typeof messages === "string"
                ? messages
                : "";

        // Store the message only when the field is recognized.
        if (target && message) {
            result[target] = message;
        }
    }

    return result;
}