export function parseRetryAfter(value: string | null): number | null {
    if (!value) return null;

    const seconds = Number(value);

    if (Number.isFinite(seconds) && seconds >= 0) {
        return Math.ceil(seconds);
    }

    const date = Date.parse(value);

    if (Number.isNaN(date)) return null;

    return Math.max(0, Math.ceil((date - Date.now()) / 1000));
}

export function describeError(
    status: number,
    correlationId?: string | null,
    retryAfter?: string | null
): string {
    let message: string;

    switch (status) {
        case 400:
            message = "Invalid request. Please check your information.";
            break;
        case 401:
            message = "Your session has expired. Please sign in again.";
            break;
        case 403:
            message = "You do not have permission to access this information.";
            break;
        case 404:
            message = "The requested information could not be found.";
            break;
        case 409:
            message = "This request conflicts with existing information.";
            break;
        case 429:
            message = "Too many requests. Please try again later.";
            break;
        default:
            message =
                status >= 500
                    ? "The server is temporarily unavailable."
                    : "Something went wrong. Please try again.";
    }

    const seconds = parseRetryAfter(retryAfter);

    if (status === 429 && seconds !== null) {
        message += ` Try again in ${seconds} seconds.`;
    }

    if (correlationId) {
        message += ` Reference: ${correlationId}`;
    }

    return message;
}