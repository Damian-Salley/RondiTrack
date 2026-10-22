import { describe, expect, it, vi, afterEach } from "vitest";
import { describeError, parseRetryAfter } from "./errors";

describe("API error handling", () => {
    afterEach(() => vi.useRealTimers());

    it("explains an expired session", () => {
        expect(describeError(401)).toContain("sign in again");
    });

    it("explains forbidden access", () => {
        expect(describeError(403)).toContain("permission");
    });

    it("includes the correlation ID", () => {
        expect(describeError(500, "abc-123")).toContain("abc-123");
    });

    it("parses Retry-After seconds", () => {
        expect(parseRetryAfter("30")).toBe(30);
    });

    it("parses Retry-After HTTP dates", () => {
        vi.useFakeTimers();
        vi.setSystemTime(new Date("2026-10-22T12:00:00Z"));

        expect(
            parseRetryAfter("Thu, 22 Oct 2026 12:01:00 GMT")
        ).toBe(60);
    });

    it("handles invalid Retry-After values", () => {
        expect(parseRetryAfter("invalid")).toBeNull();
    });

    it("shows the retry delay for rate limiting", () => {
        expect(describeError(429, null, "15")).toContain("15 seconds");
    });
});