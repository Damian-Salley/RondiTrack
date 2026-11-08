// Default destination when a redirect is missing or unsafe.
const DEFAULT_REDIRECT = "/stokvels";

// Fixed origin used only to validate redirect destinations.
// This is not the application's actual production URL.
const INTERNAL_ORIGIN = "https://ronditrack.invalid";

// Validate the destination used after successful login.
//
// Only internal application paths are allowed.
// This prevents attackers from using the login page
// to redirect users to external websites.
export function safeNext(value: unknown): string {
    // Reject values that are not strings.
    if (typeof value !== "string") {
        return DEFAULT_REDIRECT;
    }

    // A valid internal destination must begin with "/".
    if (!value.startsWith("/")) {
        return DEFAULT_REDIRECT;
    }

    // Reject protocol-relative URLs such as "//example.com".
    if (value.startsWith("//")) {
        return DEFAULT_REDIRECT;
    }

    // Reject backslashes, whitespace and control characters.
    //
    // Backslashes can be interpreted as forward slashes by URL parsers,
    // potentially turning an internal-looking path into an external URL.
    if (/[\\\u0000-\u0020\u007f]/.test(value)) {
        return DEFAULT_REDIRECT;
    }

    try {
        // Resolve the destination against a fixed internal origin.
        const url = new URL(value, INTERNAL_ORIGIN);

        // Reject destinations that resolve to another website.
        if (url.origin !== INTERNAL_ORIGIN) {
            return DEFAULT_REDIRECT;
        }

        // Preserve the internal path, query parameters and fragment.
        return url.pathname + url.search + url.hash;
    } catch {
        // Invalid URLs must never be used as redirect destinations.
        return DEFAULT_REDIRECT;
    }
}