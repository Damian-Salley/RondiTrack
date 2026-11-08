// Convert a rand amount into integer cents without floating-point arithmetic.
//
// Supported examples:
// "1500"       -> 150000
// "1 500"      -> 150000
// "1 500,50"   -> 150050
// "R1500"      -> 150000
// "R 1 500,50" -> 150050
// "19,99"      -> 1999
// "0"          -> 0
//
// Invalid formats return null.
export function parseRandToCents(input: string): number | null {
    // Remove surrounding whitespace and an optional rand symbol.
    // Convert non-breaking spaces into ordinary spaces.
    const value = input
        .trim()
        .replace(/^R\s*/, "")
        .replace(/[\u00A0\u202F]/g, " ");

    // Accept whole numbers, correctly grouped thousands,
    // and an optional decimal comma or point with 1–2 digits.
    //
    // Reject ambiguous formats such as "1,500.50".
    const validFormat =
        /^(?:\d+|\d{1,3}(?: \d{3})+)(?:[.,]\d{1,2})?$/;

    if (!validFormat.test(value)) {
        return null;
    }

    // Remove thousands separators and normalize decimal commas.
    const normalized = value.replace(/ /g, "").replace(",", ".");

    // Separate the whole rand amount from the decimal portion.
    const [randPart, centsPart = ""] = normalized.split(".");

    // Convert directly to integer cents using BigInt.
    // This avoids floating-point rounding errors.
    const cents =
        BigInt(randPart) * 100n +
        BigInt(centsPart.padEnd(2, "0") || "0");

    // Reject values exceeding JavaScript's maximum safe integer.
    // This matches the backend's ContributionLimits.MaximumCents.
    if (cents > BigInt(Number.MAX_SAFE_INTEGER)) {
        return null;
    }

    // Zero is a valid monetary value for parsing and formatting.
    // Contribution validation separately rejects zero contributions.
    return Number(cents);
}

// Format integer cents as South African rand.
//
// Examples:
// 150050 -> "R 1 500,50"
// 1999   -> "R 19,99"
// 0      -> "R 0,00"
//
// Accepts a safe integer or an integer string.
export function formatCents(cents: number | string): string {
    // Reject unsafe numbers before converting them to BigInt.
    // Once a number loses precision, BigInt cannot restore it.
    if (
        typeof cents === "number" &&
        !Number.isSafeInteger(cents)
    ) {
        throw new RangeError("Cents must be a safe integer.");
    }

    const amount = BigInt(cents);

    // Preserve negative values for balances or adjustments.
    const negative = amount < 0n;
    const absolute = negative ? -amount : amount;

    // Separate whole rand from the remaining cents.
    const rand = absolute / 100n;
    const remainder = absolute % 100n;

    // Format thousands according to South African conventions.
    // Normalize non-breaking spaces for consistent output.
    const formattedRand = new Intl.NumberFormat("en-ZA")
        .format(rand)
        .replace(/[\u00A0\u202F]/g, " ");

    // Always display exactly two decimal digits.
    const formattedCents = remainder.toString().padStart(2, "0");

    return `${negative ? "-" : ""}R\u00A0${formattedRand},${formattedCents}`;
}

// Format a UTC timestamp for display in South African local time.
export function formatDate(
    dateUtc: string | null | undefined
): string {
    // Handle missing timestamps.
    if (!dateUtc) {
        return "Not available";
    }

    const date = new Date(dateUtc);

    // Avoid displaying misleading information for invalid dates.
    if (Number.isNaN(date.getTime())) {
        return "Invalid date";
    }

    // Always use Johannesburg time, regardless of the device timezone.
    return new Intl.DateTimeFormat("en-ZA", {
        timeZone: "Africa/Johannesburg",
        year: "numeric",
        month: "short",
        day: "numeric",
        hour: "2-digit",
        minute: "2-digit",
        hour12: false,
    }).format(date);
}

// Format a contribution cycle deadline as a Johannesburg due date.
//
// The existing application treats a deadline as an exclusive boundary.
// Subtracting one millisecond displays the final calendar day
// before the deadline.
export function formatDueDate(dueDateUtc: string): string {
    const deadline = new Date(dueDateUtc);

    // Handle invalid deadline values.
    if (Number.isNaN(deadline.getTime())) {
        return "Invalid date";
    }

    // Display the calendar day immediately before the deadline.
    const dueDay = new Date(deadline.getTime() - 1);

    return new Intl.DateTimeFormat("en-ZA", {
        timeZone: "Africa/Johannesburg",
        year: "numeric",
        month: "short",
        day: "numeric",
    }).format(dueDay);
}