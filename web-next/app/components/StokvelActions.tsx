import ContributionForm from "./ContributionForm";
import CreateInvite from "./CreateInvite";
import CloseCycle from "./CloseCycle";

export default function StokvelActions({ stokvelId, cycleId, role, members }: {
    stokvelId: number; cycleId?: number; role: string;
    members: { id: number; firstName: string; lastName: string }[];
}) {
    return <>
        {role === "Treasurer" && <section className="mt-6">
            <CreateInvite stokvelId={stokvelId} />
            {cycleId && <CloseCycle cycleId={cycleId} />}
        </section>}
        {cycleId && <section className="mt-6"><ContributionForm stokvelId={stokvelId} cycleId={cycleId} role={role} members={members} /></section>}
    </>;
}
