// @vitest-environment jsdom
import { render, screen } from "@testing-library/react";
import { expect, it, vi } from "vitest";
import type { components } from "../../../clients/typescript/src/api";
import StokvelActions from "./StokvelActions";
import PaymentProofLink from "./PaymentProofLink";

vi.mock("@/app/actions/contributions", () => ({ recordContribution: vi.fn() }));
vi.mock("@/app/actions/closing", () => ({ closeCycle: vi.fn(), loadClosePreview: vi.fn() }));
vi.mock("@/app/actions/join", () => ({ createInvite: vi.fn() }));
vi.mock("next/navigation", () => ({ usePathname: () => "/stokvels/1", useSearchParams: () => new URLSearchParams() }));

const memberships = [
    { stokvelId: 1, stokvelName: "Family", role: "Treasurer" },
    { stokvelId: 2, stokvelName: "Work", role: "Member" },
] satisfies components["schemas"]["HomeStokvelResponse"][];
const sipho = { id: 7, firstName: "Sipho", lastName: "Nkosi" } satisfies components["schemas"]["UserResponse"];

it.each(memberships)("Sipho's controls follow the $stokvelName membership", membership => {
    render(<><StokvelActions stokvelId={membership.stokvelId} cycleId={1} role={membership.role} members={[sipho]} /><PaymentProofLink stokvelId={membership.stokvelId} cycleId={1} memberId={8} userId={sipho.id} role={membership.role} /></>);
    if (membership.role === "Treasurer") {
        expect(screen.getByRole("combobox", { name: "Member" })).toBeVisible();
        expect(screen.getByRole("button", { name: "Close cycle" })).toBeVisible();
        expect(screen.getByRole("link", { name: "Proof of payment" })).toBeVisible();
    } else {
        expect(screen.queryByRole("combobox", { name: "Member" })).not.toBeInTheDocument();
        expect(screen.queryByRole("button", { name: "Close cycle" })).not.toBeInTheDocument();
        expect(screen.queryByText("Create a seven-day invite")).not.toBeInTheDocument();
        expect(screen.queryByRole("link", { name: "Proof of payment" })).not.toBeInTheDocument();
    }
});
