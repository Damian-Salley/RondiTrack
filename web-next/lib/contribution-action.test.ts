import {
    afterAll,
    afterEach,
    beforeAll,
    beforeEach,
    describe,
    expect,
    it,
    vi,
} from "vitest";

import { http, HttpResponse } from "msw";
import { setupServer } from "msw/node";

import { recordContribution } from "@/app/actions/contributions";
import { revalidatePath } from "next/cache";
import { getSession } from "@/lib/session";

// Mock Next.js server-only modules.
vi.mock("server-only", () => ({}));

vi.mock("next/cache", () => ({
    revalidatePath: vi.fn(),
}));

// Mock the session so tests can simulate authentication.
vi.mock("@/lib/session", () => ({
    getSession: vi.fn(),
}));

// Mock the current authenticated user.
vi.mock("@/lib/api", () => ({
    getMe: vi.fn(async () => ({ id: 7 })),
}));

// Fake backend address used only during testing.
const base = "http://assignment92.test";

// Create a mock backend with a default treasurer membership.
const server = setupServer(
    http.get(`${base}/api/v1/me/home`, () =>
        HttpResponse.json([
            {
                stokvelId: 1,
                role: "Treasurer",
            },
        ])
    )
);

// Start the mock server before the tests.
beforeAll(() => {
    vi.stubEnv("API_BASE_URL", base);
    server.listen();
});

// Provide an authenticated session before each test.
beforeEach(() => {
    vi.mocked(getSession).mockResolvedValue({
        accessToken: "test-token",
        refreshToken: "test-refresh",
    } as Awaited<ReturnType<typeof getSession>>);

    vi.mocked(revalidatePath).mockClear();
});

// Remove temporary API handlers after each test.
afterEach(() => {
    server.resetHandlers();
});

// Shut down the mock server after testing.
afterAll(() => {
    server.close();
    vi.unstubAllEnvs();
});

// Create the form data submitted by the contribution form.
function data(): FormData {
    const form = new FormData();

    form.set("stokvelId", "1");
    form.set("cycleId", "2");
    form.set("userId", "7");
    form.set("amount", "R150,50");
    form.set("idempotencyKey", "test-key");

    return form;
}

// Initial state used by React's useActionState.
const initialState = {
    success: false,
    message: "",
};

describe("recordContribution", () => {
    // Check that HTTP 400 and 422 validation errors
    // are mapped to the correct form fields.
    it.each([
        [400, "AmountInCents", "amount", "Amount exceeds the maximum."],
        [400, "UserId", "userId", "Select a valid member."],
        [422, "AmountInCents", "amount", "Amount exceeds the maximum."],
        [422, "UserId", "userId", "Select a valid member."],
    ])(
        "maps HTTP %i field %s to %s",
        async (status, apiField, field, message) => {
            server.use(
                http.post(
                    `${base}/api/v1/stokvels/1/cycles/2/contributions`,
                    () =>
                        HttpResponse.json(
                            {
                                errors: {
                                    [apiField]: [message],
                                },
                            },
                            {
                                status,
                                headers: {
                                    "X-Correlation-ID": "ref-validation",
                                },
                            }
                        )
                )
            );

            const result = await recordContribution(
                initialState,
                data()
            );

            expect(result.success).toBe(false);

            expect(result.fieldErrors).toEqual({
                [field]: message,
            });

            expect(result.correlationId).toBe("ref-validation");
        }
    );

    // Verify exact integer cents and idempotency headers.
    it("sends exact cents and the original idempotency key", async () => {
        server.use(
            http.post(
                `${base}/api/v1/stokvels/1/cycles/2/contributions`,
                async ({ request }) => {
                    expect(
                        request.headers.get("Idempotency-Key")
                    ).toBe("test-key");

                    expect(await request.json()).toEqual({
                        userId: 7,
                        amountInCents: 15050,
                    });

                    return HttpResponse.json({}, { status: 201 });
                }
            )
        );

        const result = await recordContribution(
            initialState,
            data()
        );

        expect(result.success).toBe(true);
        expect(result.message).toBe(
            "Contribution recorded successfully."
        );

        expect(revalidatePath).toHaveBeenCalledWith("/stokvels/1");
    });

    // A conflict must be handled as a confirmed rejection.
    it("handles HTTP 409 conflicts", async () => {
        server.use(
            http.post(
                `${base}/api/v1/stokvels/1/cycles/2/contributions`,
                () =>
                    HttpResponse.json(
                        {},
                        {
                            status: 409,
                            headers: {
                                "X-Correlation-ID": "ref-conflict",
                            },
                        }
                    )
            )
        );

        const result = await recordContribution(
            initialState,
            data()
        );

        expect(result.success).toBe(false);
        expect(result.uncertain).not.toBe(true);
        expect(result.message).toContain("conflicts");
        expect(result.correlationId).toBe("ref-conflict");
    });

    // Forbidden requests must not be treated as network failures.
    it("handles HTTP 403 permission failures", async () => {
        server.use(
            http.post(
                `${base}/api/v1/stokvels/1/cycles/2/contributions`,
                () => new HttpResponse(null, { status: 403 })
            )
        );

        const result = await recordContribution(
            initialState,
            data()
        );

        expect(result.success).toBe(false);
        expect(result.uncertain).not.toBe(true);
        expect(result.message).toContain("permission");
    });

    // Missing sessions must prevent contribution requests.
    it("rejects submissions without a session", async () => {
        vi.mocked(getSession).mockResolvedValue(null);

        const result = await recordContribution(
            initialState,
            data()
        );

        expect(result.success).toBe(false);
        expect(result.message).toContain("session has expired");
    });

    // Members must not be able to submit contributions
    // on behalf of another user.
    it("ignores another user ID for ordinary members", async () => {
        server.use(
            http.get(`${base}/api/v1/me/home`, () =>
                HttpResponse.json([
                    {
                        stokvelId: 1,
                        role: "Member",
                    },
                ])
            ),

            http.post(
                `${base}/api/v1/stokvels/1/cycles/2/contributions`,
                async ({ request }) => {
                    expect(await request.json()).toEqual({
                        userId: 7,
                        amountInCents: 15050,
                    });

                    return HttpResponse.json({}, { status: 201 });
                }
            )
        );

        const form = data();

        form.set("userId", "999");

        const result = await recordContribution(
            initialState,
            form
        );

        expect(result.success).toBe(true);
    });

    // A dropped POST may have succeeded on the backend.
    // The frontend must treat the outcome as uncertain.
    it("marks a dropped POST as uncertain", async () => {
        server.use(
            http.post(
                `${base}/api/v1/stokvels/1/cycles/2/contributions`,
                () => HttpResponse.error()
            )
        );

        const result = await recordContribution(
            initialState,
            data()
        );

        expect(result.success).toBe(false);
        expect(result.uncertain).toBe(true);

        expect(result.message).toContain(
            "Could not confirm the result"
        );
    });

    // Zero contributions must fail validation.
    it("rejects a zero contribution", async () => {
        const form = data();

        form.set("amount", "R0,00");

        const result = await recordContribution(
            initialState,
            form
        );

        expect(result.success).toBe(false);
        expect(result.fieldErrors?.amount).toBeDefined();
    });
});