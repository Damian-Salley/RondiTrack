import type { components } from "../api/schema";
import { formatCents, formatDate } from "../utils/formatters";
import { StatusBadge } from "./StatusBadge";

type Contribution = components["schemas"]["ContributionResponse"];

interface ContributionCardProps {
    contribution: Contribution;
}

// Displays one contribution and its payment status.
export function ContributionCard({ contribution }: ContributionCardProps) {
    return (
        <article className="contribution-card">
            <header>
                <h3>Contribution</h3>
                <StatusBadge kind="contribution" status={contribution.status} />
            </header>

            <p>
                <strong>Member:</strong>{" "}
                {contribution.memberName || "Not available"}
            </p>

            <p>
                <strong>Amount:</strong>{" "}
                {formatCents(contribution.amountCents ?? 0)}
            </p>

            <p>
                <strong>Cycle:</strong>{" "}
                {contribution.cycleNumber ?? "Not available"}
            </p>

            <p>
                <strong>Payment date:</strong>{" "}
                {formatDate(contribution.paidAtUtc)}
            </p>
        </article>
    );
}