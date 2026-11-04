// Maximum allowed proof-of-payment file size.
//
// 5 * 1024 * 1024 = 5,242,880 bytes (5 MiB).
// This must match the maximum file size enforced by the backend.
export const PROOF_LIMIT = 5 * 1024 * 1024;

// Validates a selected proof-of-payment file before uploading.
//
// Returns an error message if the file is invalid.
// Returns an empty string ("") if the file passes validation.
export function proofError(
    file: { size: number; type: string } | null
): string {

    // Reject missing files and empty files.
    if (!file || file.size === 0) {
        return "Choose a file.";
    }

    // Reject files larger than the allowed 5 MiB limit.
    if (file.size > PROOF_LIMIT) {
        return "Choose a file up to 5 MiB.";
    }

    // Only JPEG images, PNG images and PDF documents are supported.
    // The backend also checks the file's actual bytes for security.
    if (
        ![
            "image/jpeg",
            "image/png",
            "application/pdf"
        ].includes(file.type)
    ) {
        return "Choose a JPEG, PNG or PDF file.";
    }

    // An empty error message means the file passed frontend validation.
    return "";
}