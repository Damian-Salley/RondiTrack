// @vitest-environment jsdom

import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
    cleanup,
    fireEvent,
    render,
    screen,
    waitFor,
} from "@testing-library/react";
import { RecordContributionForm } from "./RecordContributionForm";

const mocks = vi.hoisted(() => ({
    mutateAsync: vi.fn(),
}));

vi.mock("../api/queries", () => ({
    useMe: () => ({
        data: {
            id: 5,
            firstName: "Test",
            lastName: "Member",
        },
        isSuccess: true,
        isError: false,
    }),

    useMembers: () => ({
        data: [
            { id: 5, firstName: "Test", lastName: "Member" },
            { id: 8, firstName: "Another", lastName: "Member" },
        ],
        isSuccess: true,
        isError: false,
    }),

    useRecordContribution: () => ({
        mutateAsync: mocks.mutateAsync,
        isPending: false,
    }),
}));

const defaultProps = {
    stokvelId: 9002,
    cycleId: 12,
    stokvelName: "Community Savings",
    cycleNumber: 3,
    cycleStatus: "Active",
    role: "Member",
};

beforeEach(() => {
    mocks.mutateAsync.mockReset();
    mocks.mutateAsync.mockResolvedValue({});
});

afterEach(() => {
    cleanup();
});

describe("RecordContributionForm", () => {
    it("submits the correct contribution details", async () => {
        render(<RecordContributionForm {...defaultProps} />);

        fireEvent.change(screen.getByLabelText("Amount (ZAR)"), {
            target: { value: "150.50" },
        });

        fireEvent.click(
            screen.getByRole("button", { name: "Record Contribution" })
        );

        await waitFor(() => {
            expect(mocks.mutateAsync).toHaveBeenCalledWith({
                stokvelId: 9002,
                cycleId: 12,
                userId: 5,
                amountInCents: 15050,
                idempotencyKey: expect.any(String),
            });
        });

        expect(
            await screen.findByText("Contribution recorded successfully.")
        ).toBeTruthy();
    });

    it("rejects invalid amounts", async () => {
        render(<RecordContributionForm {...defaultProps} />);

        fireEvent.change(screen.getByLabelText("Amount (ZAR)"), {
            target: { value: "0" },
        });

        fireEvent.click(
            screen.getByRole("button", { name: "Record Contribution" })
        );

        expect(
            await screen.findByText("Enter a valid amount greater than R0.00.")
        ).toBeTruthy();

        expect(mocks.mutateAsync).not.toHaveBeenCalled();
    });

    it("allows Treasurers to select a member", async () => {
        render(
            <RecordContributionForm {...defaultProps} role="Treasurer" />
        );

        fireEvent.change(screen.getByLabelText("Member"), {
            target: { value: "8" },
        });

        fireEvent.change(screen.getByLabelText("Amount (ZAR)"), {
            target: { value: "200.00" },
        });

        fireEvent.click(
            screen.getByRole("button", { name: "Record Contribution" })
        );

        await waitFor(() => {
            expect(mocks.mutateAsync).toHaveBeenCalledWith(
                expect.objectContaining({
                    userId: 8,
                    amountInCents: 20000,
                })
            );
        });
    });

    it("prevents submissions for inactive cycles", () => {
        render(
            <RecordContributionForm {...defaultProps} cycleStatus="Closed" />
        );

        expect(
            (
                screen.getByRole("button", {
                    name: "Record Contribution",
                }) as HTMLButtonElement
            ).disabled
        ).toBe(true);

        expect(mocks.mutateAsync).not.toHaveBeenCalled();
    });

    it("displays an error when submission fails", async () => {
        mocks.mutateAsync.mockRejectedValueOnce(new Error("Network error"));

        render(<RecordContributionForm {...defaultProps} />);

        fireEvent.change(screen.getByLabelText("Amount (ZAR)"), {
            target: { value: "100.00" },
        });

        fireEvent.click(
            screen.getByRole("button", { name: "Record Contribution" })
        );

        expect(
            await screen.findByText(/The contribution could not be confirmed/)
        ).toBeTruthy();

        expect(
            screen.getByRole("button", { name: "Retry Submission" })
        ).toBeTruthy();
    });

    it("reuses the same idempotency key when retrying", async () => {
        mocks.mutateAsync
            .mockRejectedValueOnce(new Error("Network error"))
            .mockResolvedValueOnce({});

        render(<RecordContributionForm {...defaultProps} />);

        fireEvent.change(screen.getByLabelText("Amount (ZAR)"), {
            target: { value: "75.25" },
        });

        fireEvent.click(
            screen.getByRole("button", { name: "Record Contribution" })
        );

        const retryButton = await screen.findByRole("button", {
            name: "Retry Submission",
        });

        fireEvent.click(retryButton);

        await waitFor(() => {
            expect(mocks.mutateAsync).toHaveBeenCalledTimes(2);
        });

        const firstRequest = mocks.mutateAsync.mock.calls[0][0];
        const secondRequest = mocks.mutateAsync.mock.calls[1][0];

        expect(secondRequest).toEqual(firstRequest);
        expect(firstRequest.idempotencyKey).toBeTruthy();
    });
});