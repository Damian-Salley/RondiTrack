// @vitest-environment jsdom

// Import testing tools.
import { afterEach, beforeAll, describe, expect, it, vi } from "vitest";
import { cleanup, fireEvent, render, screen } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import App from "./App";

// Mock authentication so we don't need a real login.
vi.mock("./auth/session", () => ({
    login: vi.fn(),
    logout: vi.fn(),
    refreshAccessToken: vi.fn(async () => "test-token"),
}));

// Mock API responses for two different stokvels.
vi.mock("./api/queries", () => ({
    // Return the two stokvels available to the user.
    useHome: () => ({
        data: [
            {
                stokvelId: 9002,
                stokvelName: "Community Savings",
                role: "Member",
                currentCycle: 1,
                cycleStatus: "Active",
                paidThisCycle: 25000,
                stillOwes: 0,
            },
            {
                stokvelId: 9003,
                stokvelName: "Family Savings",
                role: "Member",
                currentCycle: 1,
                cycleStatus: "Active",
                paidThisCycle: 0,
                stillOwes: 10000,
            },
        ],
        isPending: false,
        isError: false,
    }),

    // Return the selected stokvel's contribution cycle.
    useCycles: (id: number | null) => ({
        data: [
            {
                id: id === 9002 ? 2 : 3,
                number: 1,
                status: "Active",
                dueDateUtc: "2026-10-31T22:00:00Z",
                totalCollectedCents: 0,
                amountDueCents: 50000,
            },
        ],
        isPending: false,
        isError: false,
        isSuccess: true,
    }),

    // Return different contributions depending on the selected stokvel.
    useContributions: (id: number | null) => ({
        data: {
            pages: [
                {
                    items: [
                        {
                            id: id === 9002 ? "community-payment" : "family-payment",
                            stokvelId: id,
                            cycle: id === 9002 ? 2 : 3,
                            cycleNumber: 1,
                            memberName:
                                id === 9002 ? "Community Member" : "Family Member",
                            amountCents: id === 9002 ? 25000 : 10000,
                            status: "Paid",
                            paidAtUtc: "2026-10-20T12:00:00Z",
                        },
                    ],
                },
            ],
        },
        isPending: false,
        isError: false,
        isSuccess: true,
        hasNextPage: false,
    }),
}));

// Simulate the browser's dark-mode detection for jsdom.
beforeAll(() => {
    Object.defineProperty(window, "matchMedia", {
        writable: true,
        value: vi.fn().mockImplementation((query: string) => ({
            matches: false,
            media: query,
            onchange: null,
            addListener: vi.fn(),
            removeListener: vi.fn(),
            addEventListener: vi.fn(),
            removeEventListener: vi.fn(),
            dispatchEvent: vi.fn(),
        })),
    });
});

// Clean up rendered components after each test.
afterEach(() => {
    cleanup();
});

// Verify that switching stokvels does not display old contribution data.
describe("Stokvel switching", () => {
    it("does not show the previous stokvel's contributions after switching", async () => {
        // Create a fresh React Query cache for this test.
        const queryClient = new QueryClient();

        // Render the application with React Query enabled.
        render(
            <QueryClientProvider client={queryClient}>
                <App />
            </QueryClientProvider>
        );

        // Check that the first stokvel's contribution is displayed.
        expect((await screen.findAllByText("Community Member")).length).toBeGreaterThan(0);

        // Switch to the second stokvel.
        fireEvent.click(
            screen.getByRole("button", { name: "Select stokvel" })
        );

        // Check that the second stokvel's contribution is displayed.
        expect((await screen.findAllByText("Family Member")).length).toBeGreaterThan(0);

        // Ensure the previous stokvel's contribution is no longer visible.
        expect(screen.queryAllByText("Community Member")).toHaveLength(0);
    });
});