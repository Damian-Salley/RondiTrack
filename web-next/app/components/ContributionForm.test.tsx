// @vitest-environment jsdom

import {
    act,
    render,
    screen,
    waitFor,
} from "@testing-library/react";

import userEvent from "@testing-library/user-event";

import {
    beforeEach,
    expect,
    it,
    vi,
} from "vitest";

import ContributionForm from "./ContributionForm";

import {
    recordContribution,
    type ContributionActionState,
} from "@/app/actions/contributions";

// Prevent Next.js's server-only restriction from blocking unit tests.
vi.mock("server-only", () => ({}));

// Mock the Server Action so tests do not call the real backend.
vi.mock("@/app/actions/contributions", () => ({
    recordContribution: vi.fn(),
}));

// Access the mocked Server Action.
const action = vi.mocked(recordContribution);

// Test data representing a treasurer and one stokvel member.
const props = {
    stokvelId: 1,
    cycleId: 2,
    members: [
        {
            id: 7,
            firstName: "Sipho",
            lastName: "Nkosi",
        },
    ],
    role: "Treasurer",
};

// Reset the mock before each test.
beforeEach(() => {
    action.mockReset();
});

// Fill in the contribution form with valid values.
async function fill() {
    const user = userEvent.setup();

    await user.selectOptions(
        screen.getByRole("combobox", { name: "Member" }),
        "7"
    );

    await user.type(
        screen.getByRole("textbox", { name: "Amount (R)" }),
        "R 150,50"
    );

    return user;
}

// Rejected contributions must preserve the entered values.
it.each([409, 422])(
    "announces a %i refusal and reference, retaining input",
    async (status) => {
        action.mockResolvedValue({
            success: false,
            message: `Refused (${status})`,
            correlationId: "ref-123",
        });

        render(<ContributionForm {...props} />);

        const user = await fill();

        await user.click(
            screen.getByRole("button", {
                name: "Record Contribution",
            })
        );

        // The rejection message must be visible.
        expect(
            await screen.findByText(`Refused (${status})`)
        ).toBeVisible();

        // The reference must be announced.
        expect(screen.getByRole("alert")).toHaveTextContent(
            "ref-123"
        );

        // Both fields must retain their values.
        expect(
            screen.getByLabelText("Amount (R)")
        ).toHaveValue("R 150,50");

        expect(
            screen.getByLabelText("Member")
        ).toHaveValue("7");
    }
);

// Validation errors must be associated with their fields.
it("associates each field error with its field", async () => {
    action.mockResolvedValue({
        success: false,
        message: "Invalid request",
        fieldErrors: {
            amount: "Too large",
            userId: "Unknown member",
        },
    });

    render(<ContributionForm {...props} />);

    const user = await fill();

    await user.click(
        screen.getByRole("button", {
            name: "Record Contribution",
        })
    );

    await screen.findByText("Too large");

    // Screen readers must be able to identify each field error.
    expect(
        screen.getByLabelText("Amount (R)")
    ).toHaveAccessibleDescription("Too large");

    expect(
        screen.getByLabelText("Member")
    ).toHaveAccessibleDescription("Unknown member");

    expect(
        screen.getByLabelText("Amount (R)")
    ).toHaveAttribute("aria-invalid", "true");
});

// Users must not submit duplicate requests while one is pending.
it("blocks duplicate clicks while the real action state is pending", async () => {
    let finish!: (state: ContributionActionState) => void;

    // Keep the action pending until the test resolves it.
    action.mockImplementation(
        () =>
            new Promise((resolve) => {
                finish = resolve;
            })
    );

    render(<ContributionForm {...props} />);

    const user = await fill();

    await user.click(
        screen.getByRole("button", {
            name: "Record Contribution",
        })
    );

    // The submit button must be disabled while pending.
    const button = screen.getByRole("button", {
        name: "Recording...",
    });

    expect(button).toBeDisabled();

    // Clicking again must not create another request.
    await user.click(button);

    expect(action).toHaveBeenCalledTimes(1);

    // Complete the pending action.
    await act(async () => {
        finish({
            success: true,
            message: "Recorded",
        });
    });

    expect(
        await screen.findByRole("status")
    ).toHaveTextContent("Recorded");
});

// Retrying unchanged details must reuse the idempotency key.
it("reuses the key on network failure and replaces it after success", async () => {
    const keys: FormDataEntryValue[] = [];

    // First request: uncertain result.
    // Second request: confirmed success.
    // Third request: another successful contribution.
    action.mockImplementation(async (_, data) => {
        keys.push(data.get("idempotencyKey")!);

        return keys.length === 1
            ? {
                success: false,
                uncertain: true,
                message: "Retry safely",
            }
            : {
                success: true,
                message: `Recorded ${keys.length}`,
            };
    });

    render(<ContributionForm {...props} />);

    let user = await fill();

    // Submit the first contribution.
    await user.click(
        screen.getByRole("button", {
            name: "Record Contribution",
        })
    );

    await screen.findByText("Retry safely");

    // Retry without changing the contribution details.
    await user.click(
        screen.getByRole("button", {
            name: "Record Contribution",
        })
    );

    await screen.findByText("Recorded 2");

    // Confirm the form clears after success.
    await waitFor(() => {
        expect(
            screen.getByLabelText("Amount (R)")
        ).toHaveValue("");
    });

    // Fill in another contribution.
    user = await fill();

    await user.click(
        screen.getByRole("button", {
            name: "Record Contribution",
        })
    );

    await screen.findByText("Recorded 3");

    // The retry must reuse the original key.
    expect(keys[0]).toBeTruthy();
    expect(keys[1]).toBe(keys[0]);

    // A new contribution must use a different key.
    expect(keys[2]).not.toBe(keys[0]);
});