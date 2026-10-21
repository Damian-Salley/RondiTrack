import type { components } from "../api/schema";
import { formatCents, formatDate } from "../utils/formatters";
import { StatusBadge } from "./StatusBadge";

type Stokvel = components["schemas"]["HomeStokvelResponse"];

interface StokvelCardProps {
  stokvel: Stokvel;
}

// Displays a stokvel's information for the authenticated member.
export function StokvelCard({ stokvel }: StokvelCardProps) {
  return (
    <article className="stokvel-card">
      <header>
        <h2>{stokvel.stokvelName ?? "Unnamed stokvel"}</h2>
        <span className="role-badge">{stokvel.role ?? "Member"}</span>
      </header>

      <p>
        <strong>Current cycle:</strong>{" "}
        {stokvel.currentCycle ?? "No active cycle"}
      </p>

      <p>
        <strong>Cycle status:</strong>{" "}
        <StatusBadge kind="cycle" status={stokvel.cycleStatus} />
      </p>

      <p>
        <strong>Paid this cycle:</strong>{" "}
        {formatCents(stokvel.paidThisCycle ?? 0)}
      </p>

      <p>
        <strong>Still owed:</strong>{" "}
        {formatCents(stokvel.stillOwes ?? 0)}
      </p>

      <StatusBadge
        kind="arrears"
        amountCents={Number(stokvel.arrearsCents ?? 0)}
      />

      <p>
        <strong>Next payout:</strong>{" "}
        {formatDate(stokvel.nextPayoutDate)}
      </p>
    </article>
  );
}