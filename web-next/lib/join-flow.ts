export type JoinStep = "code" | "rules" | "confirm";
export type JoinEvent = "next" | "back";
export function transition(step: JoinStep, event: JoinEvent): JoinStep {
    const steps: JoinStep[] = ["code", "rules", "confirm"];
    const index = steps.indexOf(step) + (event === "next" ? 1 : -1);
    return steps[Math.max(0, Math.min(2, index))];
}
export function joinUrl(code: string, step: JoinStep): string {
    return `/join?${new URLSearchParams({ code, step })}`;
}
export type InvitePreview = {
    stokvelId: number; name: string; contributionAmountCents: number;
    schedule: string; rules: string; memberCount: number; alreadyMember: boolean;
};
