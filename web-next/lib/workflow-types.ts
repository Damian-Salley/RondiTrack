// Represents the result returned by a Server Action.
//
// Used for displaying success messages, validation errors,
// authorization failures and unexpected request failures.
export type ActionResult = {

    // HTTP status code returned by the operation.
    // Examples: 200 (success), 400 (invalid request),
    // 403 (forbidden), 409 (conflict), 503 (service unavailable).
    status: number;

    // User-friendly message describing the result.
    message: string;

    // Optional reference used to investigate failed requests.
    correlationId?: string;

    // Indicates that the request may have succeeded even
    // though the frontend could not confirm its outcome.
    //
    // Important for retrying financial operations safely.
    uncertain?: boolean;

    // Optional stokvel ID returned after operations such as joining.
    stokvelId?: number;
};

// Represents the backend-calculated preview shown before
// a treasurer closes a contribution cycle.
export type ClosePreview = {

    // Identifies the contribution cycle being closed.
    cycleId: number;

    // Name displayed in the closing confirmation dialog.
    cycleName: string;

    // Date associated with the calculation.
    // The backend provides this using South African time.
    asOfDate: string;

    // Version representing the current contribution data.
    //
    // The backend checks this version when closing the cycle
    // to prevent confirmation using outdated arrears amounts.
    version: string;

    // Individual members and their outstanding contributions.
    members: {

        // Identifies the member.
        userId: number;

        // Member name displayed in the preview.
        name: string;

        // Outstanding amount expressed in cents.
        // Using cents avoids floating-point currency calculations.
        amountCents: number;

    }[];

    // Total outstanding contributions across all members,
    // expressed in cents.
    totalCents: number;
};