import type { components } from "../api/schema";
import { formatCents, formatDate } from "../utils/formatters";

// Use the contribution response generated from our actual API.
type Contribution = components["schemas"]["ContributionResponse"];

interface ContributionCardProps {
    contribution: Contribution;
}

// Displays one contribution and its payment status.
export function ContributionCard({
    contribution,
}: ContributionCardProps) {
    // Use the status returned by the API, not a calculated status.
    const status = contribution.status ?? "Unknown";

    return (
        <article className="contribution-card">
            <header>
                <h3>Contribution</h3>

                <span className={`status-badge status-${status.toLowerCase()}`}>
                    {status}
                </span>
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