import { describe, expect, it } from "vitest";

import {
  parseRandToCents,
  formatCents,
  formatDate,
  formatDueDate,
} from "./formatters";

import { contributionSchema } from "./contribution-validation";
import { contributionFieldErrors } from "./contribution-errors";

// Test exact conversion between South African rand and integer cents.
describe("exact rand parsing", () => {
  // Valid amounts must convert to the correct number of cents.
  it.each([
    ["1500", 150000],
    ["1 500", 150000],
    ["1 500,50", 150050],
    ["1 500.50", 150050],
    ["R1500", 150000],
    ["R 1 500,50", 150050],
    ["1\u00a0500,50", 150050],
    ["R\u202f1\u202f500,5", 150050],
    ["19,99", 1999],
    ["19.99", 1999],
    ["0,5", 50],
    ["0", 0],
    ["90071992547409,91", Number.MAX_SAFE_INTEGER],
  ])(
    "converts %s into %i cents",
    (input, expected) => {
      expect(parseRandToCents(input)).toBe(expected);
    }
  );

  // Invalid or ambiguous formats must not be accepted.
  it.each([
    "",
    " ",
    "-1",
    "1.001",
    "1,500.50",
    "abc",
    "1 50",
    "1e3",
    "Infinity",
    "90071992547409,92",
    "92233720368547758,08",
  ])("rejects invalid amount %s", (input) => {
    expect(parseRandToCents(input)).toBeNull();
  });

  // Formatting an amount and parsing it again must preserve
  // the exact original number of cents.
  it.each([
    0,
    1,
    99,
    100,
    150050,
    Number.MAX_SAFE_INTEGER,
  ])("round-trips %i cents without precision loss", (cents) => {
    const formatted = formatCents(cents);

    expect(parseRandToCents(formatted)).toBe(cents);
  });

  // The parser accepts zero, but the contribution form must reject it.
  it("rejects zero as a contribution", () => {
    expect(
      contributionSchema.safeParse({
        amount: "R0,00",
      }).success
    ).toBe(false);
  });

  // Contributions must also reject invalid and negative amounts.
  it.each([
    "-10",
    "1,500.50",
    "12.999",
    "90071992547409,92",
  ])("rejects %s as a contribution", (amount) => {
    expect(
      contributionSchema.safeParse({ amount }).success
    ).toBe(false);
  });

  // Positive amounts in supported formats should pass validation.
  it.each([
    "19,99",
    "R 1 500,50",
    "1500.50",
  ])("accepts valid contribution %s", (amount) => {
    expect(
      contributionSchema.safeParse({ amount }).success
    ).toBe(true);
  });

  // Unsafe numeric values must not be formatted as exact money.
  it("rejects unsafe numeric values when formatting", () => {
    expect(() =>
      formatCents(Number.MAX_SAFE_INTEGER + 1)
    ).toThrow(RangeError);
  });

  // Backend validation errors must map to frontend field names.
  it("maps backend validation errors to form fields", () => {
    const problem = {
      errors: {
        AmountInCents: ["Too large"],
        UserId: ["Select member"],
        CycleId: ["Unknown"],
      },
    };

    expect(contributionFieldErrors(problem)).toEqual({
      amount: "Too large",
      userId: "Select member",
    });
  });
});

// Test South African date formatting independently of the
// timezone configured on the machine running the tests.
describe("Johannesburg dates regardless of process timezone", () => {
  // 21:30 UTC is 23:30 in Johannesburg.
  it("displays the last day of the South African month", () => {
    expect(formatDate("2026-10-31T21:30:00Z")).toContain(
      "31 Oct 2026"
    );
  });

  // 22:00 UTC is midnight the following day in Johannesburg.
  it("displays the following month after midnight", () => {
    expect(formatDate("2026-10-31T22:00:00Z")).toContain(
      "01 Nov 2026"
    );
  });

  // A midnight deadline belongs to the calendar day that ended.
  it("displays the correct due date for a midnight deadline", () => {
    expect(formatDueDate("2026-10-31T22:00:00Z")).toContain(
      "31 Oct 2026"
    );
  });

  // Missing and invalid timestamps must be handled safely.
  it("handles missing and invalid dates", () => {
    expect(formatDate(null)).toBe("Not available");
    expect(formatDate("wrong")).toBe("Invalid date");
  });
});