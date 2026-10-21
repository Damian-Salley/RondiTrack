// Johannesburg uses UTC+2 throughout the year.
const SA_OFFSET_MS = 2 * 60 * 60 * 1000;
const DAY_MS = 24 * 60 * 60 * 1000;

// Convert a date-only value into the end of that Johannesburg calendar day.
// Example: 2026-10-31 -> 2026-10-31T22:00:00Z.
export function parseDueDate(value: string): Date {
    if (/^\d{4}-\d{2}-\d{2}$/.test(value)) {
        const [year, month, day] = value.split("-").map(Number);

        return new Date(
            Date.UTC(year, month - 1, day + 1) - SA_OFFSET_MS,
        );
    }

    // Full timestamps already identify an exact moment.
    return new Date(value);
}

// Returns the number of calendar days between now and the deadline,
// using Johannesburg calendar dates rather than elapsed 24-hour periods.
export function getDaysUntilDue(dueDate: string, now: Date): number {
    const deadline = parseDueDate(dueDate);

    const todaySA = Math.floor(
        (now.getTime() + SA_OFFSET_MS) / DAY_MS,
    );

    const dueDaySA = Math.floor(
        (deadline.getTime() + SA_OFFSET_MS - 1) / DAY_MS,
    );

    return dueDaySA - todaySA;
}

export function getCountdownText(dueDate: string, now: Date): string {
    const days = getDaysUntilDue(dueDate, now);

    if (days === 0) return "Due today";
    if (days === 1) return "Due tomorrow";
    if (days > 1) return `Due in ${days} days`;

    return `Overdue by ${Math.abs(days)} day${days === -1 ? "" : "s"}`;
}

// Schedule the next update at Johannesburg midnight.
export function msUntilNextSAMidnight(now: Date): number {
    const elapsedSA = (now.getTime() + SA_OFFSET_MS) % DAY_MS;

    return DAY_MS - elapsedSA;
}