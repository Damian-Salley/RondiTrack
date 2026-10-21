import { useEffect, useState } from "react";
import { Clock } from "lucide-react";
import {
    getCountdownText,
    msUntilNextSAMidnight,
} from "@/lib/dueCountdown";


interface DueCountdownProps {
    dueDate: string;
    status: string;
}

// Display countdowns only for open cycles.
export function DueCountdown({ dueDate, status }: DueCountdownProps) {
    const isOpen = status === "Active" || status === "Open";

    const [now, setNow] = useState(() => new Date());

    useEffect(() => {
        if (!isOpen) return;

        // Update precisely when the Johannesburg calendar day changes.
        const timeout = window.setTimeout(() => {
            setNow(new Date());
        }, msUntilNextSAMidnight(now));

        // Clean up the timer on unmount or when dependencies change.
        return () => window.clearTimeout(timeout);
    }, [isOpen, now]);

    if (!isOpen) return null;

    return (
        <p className="flex items-center gap-2 text-sm font-medium">
            <Clock aria-hidden="true" className="size-4" />
            {getCountdownText(dueDate, now)}
        </p>
    );
}