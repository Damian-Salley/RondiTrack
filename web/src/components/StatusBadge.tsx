import { Badge } from "@/components/ui/badge";
import {
    AlertTriangle,
    Check,
    CheckCheck,
    CircleDollarSign,
    Clock,
    Lock,
} from "lucide-react";

type StatusBadgeProps =
    | {
        kind: "cycle" | "contribution";
        status: string | null | undefined;
    }
    | {
        kind: "arrears";
        amountCents: number;
    };

// Display status text and an icon, not colour alone.
export function StatusBadge(props: StatusBadgeProps) {
    if (props.kind === "arrears") {
        // Do not display an arrears badge when nothing is owed.
        if (props.amountCents <= 0) return null;

        return (
            <Badge variant="destructive">
                <AlertTriangle aria-hidden="true" />
                Arrears: R{(props.amountCents / 100).toFixed(2)}
            </Badge>
        );
    }

    const status = props.status ?? "Unknown";

    if (props.kind === "cycle") {
        // The backend currently uses Active and Paid.
        // Map those values to the assignment's display terminology.
        switch (status) {
            case "Active":
            case "Open":
                return (
                    <Badge variant="secondary">
                        <Clock aria-hidden="true" />
                        Open
                    </Badge>
                );

            case "Closed":
                return (
                    <Badge variant="outline">
                        <Lock aria-hidden="true" />
                        Closed
                    </Badge>
                );

            case "Paid":
            case "PaidOut":
                return (
                    <Badge variant="default">
                        <CircleDollarSign aria-hidden="true" />
                        Paid out
                    </Badge>
                );
        }
    }

    if (props.kind === "contribution") {
        switch (status) {
            case "Pending":
                return (
                    <Badge variant="secondary">
                        <Clock aria-hidden="true" />
                        Pending
                    </Badge>
                );

            case "Paid":
                return (
                    <Badge variant="outline">
                        <Check aria-hidden="true" />
                        Paid
                    </Badge>
                );

            case "Confirmed":
                return (
                    <Badge variant="default">
                        <CheckCheck aria-hidden="true" />
                        Confirmed
                    </Badge>
                );
        }
    }

    // Preserve unknown values rather than guessing their meaning.
    return <Badge variant="outline">{status}</Badge>;
}