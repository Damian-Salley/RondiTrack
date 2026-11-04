import { describe, expect, it } from "vitest";
import { joinUrl, transition } from "./join-flow";
import { safeNext } from "./safe-next";
import { PROOF_LIMIT, proofError } from "./proof-validation";

describe("join navigation", () => {
    it("preserves the code when moving backwards and forwards", () => {
        expect(transition("code", "next")).toBe("rules");
        expect(transition("rules", "next")).toBe("confirm");
        expect(transition("confirm", "back")).toBe("rules");
        expect(transition("rules", "back")).toBe("code");
        expect(joinUrl("A&B", "rules")).toBe("/join?code=A%26B&step=rules");
    });
    it("keeps the invite query string after signing in", () => {
        expect(safeNext("/join?code=ABC&step=confirm")).toBe("/join?code=ABC&step=confirm");
    });
    it.each(["//evil.example", "https://evil.example", "/\\evil.example", "/\n/evil.example", null])("rejects external or unsafe return destinations: %s", value => {
        expect(safeNext(value)).toBe("/stokvels");
    });
});
describe("browser upload checks", () => {
    it("accepts a supported file just under the API limit", () => {
        expect(proofError({ size: PROOF_LIMIT - 1, type: "application/pdf" })).toBe("");
    });
    it("rejects empty, oversized and unsupported files before submission", () => {
        expect(proofError(null)).toBe("Choose a file.");
        expect(proofError({ size: 6 * 1024 * 1024, type: "application/pdf" })).toContain("5 MiB");
        expect(proofError({ size: 10, type: "text/plain" })).toContain("JPEG, PNG or PDF");
    });
});
