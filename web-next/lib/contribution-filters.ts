import {
  createLoader,
  createParser,
  createSearchParamsCache,
  parseAsInteger,
} from "nuqs/server";

// Check whether a string represents a real calendar date.
function validDate(value: string): boolean {
  // Require the exact YYYY-MM-DD format.
  if (!/^\d{4}-\d{2}-\d{2}$/.test(value)) {
    return false;
  }

  const [year, month, day] = value.split("-").map(Number);

  // Construct a date to check whether the day actually exists.
  const date = new Date(Date.UTC(year, month - 1, day));

  return (
    date.getUTCFullYear() === year &&
    date.getUTCMonth() === month - 1 &&
    date.getUTCDate() === day
  );
}

// Accept valid calendar dates without converting them to UTC timestamps.
const calendarDateParser = createParser({
  parse(value: string) {
    // Invalid dates fall back to the parser's default.
    return validDate(value) ? value : null;
  },

  // Keep dates in YYYY-MM-DD format when writing them to the URL.
  serialize(value: string) {
    return value;
  },
}).withDefault("").withOptions({
  clearOnDefault: true,
});

// Define the URL filters used by the contribution history.
export const contributionFilterParsers = {
  // Zero means all members. Negative IDs are rejected.
  member: parseAsInteger.withDefault(0).withOptions({
    clearOnDefault: true,
  }),

  // Store calendar dates as strings until the API request needs UTC.
  from: calendarDateParser,
  to: calendarDateParser,
};

// Allow Server Components to read the filters.
export const contributionFilterCache = createSearchParamsCache(
  contributionFilterParsers
);

// Load filters from URL search parameters in a Server Component.
export const loadContributionFilters = createLoader(
  contributionFilterParsers
);

// Ensure member IDs and dates have valid values.
export function normalizeContributionFilters(filters: {
  member: number;
  from: string;
  to: string;
}) {
  return {
    member:
      Number.isInteger(filters.member) && filters.member > 0
        ? filters.member
        : 0,

    from: validDate(filters.from) ? filters.from : "",
    to: validDate(filters.to) ? filters.to : "",
  };
}