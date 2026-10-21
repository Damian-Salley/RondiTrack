// Format an integer amount of cents as South African rand.
// Uses integer arithmetic to avoid floating-point rounding errors.
export function formatCents(cents: number | string): string {
    const amount = BigInt(cents);

    const negative = amount < 0n;
    const absolute = negative ? -amount : amount;

    // Split the amount into whole rand and remaining cents.
    const rand = absolute / 100n;
    const remainder = absolute % 100n;

    // Normalize thousands separators to ordinary spaces for consistent output.
    const formattedRand = new Intl.NumberFormat("en-ZA")
        .format(rand)
        .replace(/[\u00A0\u202F]/g, " ");

    // Always display exactly two decimal places.
    const formattedCents = remainder.toString().padStart(2, "0");

    return `${negative ? "-" : ""}R\u00A0${formattedRand},${formattedCents}`;
}

// Format a UTC timestamp for display in South Africa.
export function formatDate(dateUtc: string | null | undefined): string {
    if (!dateUtc) {
        return "Not available";
    }

    const date = new Date(dateUtc);

    if (Number.isNaN(date.getTime())) {
        return "Invalid date";
    }

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
// Format a cycle deadline as its Johannesburg due date.
// A deadline at midnight belongs to the preceding calendar day.
export function formatDueDate(dueDateUtc: string): string {
    const deadline = new Date(dueDateUtc);

    if (Number.isNaN(deadline.getTime())) {
        return "Invalid date";
    }

    // Subtract one millisecond so a midnight deadline
    // displays the calendar day that just ended.
    const dueDay = new Date(deadline.getTime() - 1);

    return new Intl.DateTimeFormat("en-ZA", {
        timeZone: "Africa/Johannesburg",
        year: "numeric",
        month: "short",
        day: "numeric",
    }).format(dueDay);
}