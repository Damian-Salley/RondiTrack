// South Africa uses UTC+02:00 throughout the year.
const SAST_OFFSET_HOURS = 2;

function parseCalendarDate(value: string): Date {
  // Only accept the exact YYYY-MM-DD format.
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) {
    throw new Error("Invalid calendar date.");
  }

  const [year, month, day] = value.split("-").map(Number);

  // Construct the date in UTC to validate its calendar components.
  const date = new Date(Date.UTC(year, month - 1, day));

  if (
    date.getUTCFullYear() !== year ||
    date.getUTCMonth() !== month - 1 ||
    date.getUTCDate() !== day
  ) {
    throw new Error("Invalid calendar date.");
  }

  return date;
}

export function southAfricanDaysToUtcRange(
  from?: string | null,
  to?: string | null
): { from?: string; to?: string } {
  const start = from ? parseCalendarDate(from) : undefined;
  const end = to ? parseCalendarDate(to) : undefined;

  if (start && end && start > end) {
    throw new Error("From date cannot be after To date.");
  }

  const offsetMilliseconds = SAST_OFFSET_HOURS * 60 * 60 * 1000;

  return {
    // Inclusive start of the selected South African day.
    from: start
      ? new Date(start.getTime() - offsetMilliseconds).toISOString()
      : undefined,

    // Exclusive start of the day AFTER the selected end date.
    to: end
      ? new Date(
          end.getTime() + 24 * 60 * 60 * 1000 - offsetMilliseconds
        ).toISOString()
      : undefined,
  };
}