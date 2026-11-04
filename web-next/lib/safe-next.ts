// Validates a redirect destination after login.
// Only internal application paths are allowed to prevent unsafe redirects.
export function safeNext(value: unknown): string {

    // Reject values that are not strings or do not start with "/".
    // Also reject external-style paths, backslashes and control characters.
    if (
        typeof value !== "string" ||
        !value.startsWith("/") ||
        value.startsWith("//") ||
        /[\\\u0000-\u0020\u007f]/.test(value)
    ) {
        // Redirect to the stokvel list if the destination is unsafe.
        return "/stokvels";
    }

    try {
        // Resolve the requested path against a fixed, internal base URL.
        // This allows us to check whether the destination stays on our site.
        const url = new URL(value, "https://ronditrack.invalid");

        // Allow the redirect only when it resolves to our fixed origin.
        // Preserve the path, query parameters and URL fragment.
        return url.origin === "https://ronditrack.invalid"
            ? url.pathname + url.search + url.hash
            : "/stokvels";

    } catch {
        // Invalid URLs must never be used as redirect destinations.
        return "/stokvels";
    }
}