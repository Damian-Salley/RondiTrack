import { Suspense } from "react";
import { redirect } from "next/navigation";
import { workflowRequest } from "@/lib/workflow-api";
import { joinUrl, type InvitePreview, type JoinStep } from "@/lib/join-flow";
import type { ActionResult } from "@/lib/workflow-types";
import JoinFlow from "./JoinFlow";

type Props = { searchParams: Promise<{ code?: string; step?: string; issue?: string }> };
export default function JoinPage(props: Props) {
    return <main className="mx-auto max-w-xl p-6"><Suspense fallback={<p>Loading invite...</p>}><JoinContent {...props} /></Suspense></main>;
}
async function JoinContent({ searchParams }: Props) {
    const params = await searchParams;
    const code = typeof params.code === "string" ? params.code.trim().toUpperCase() : "";
    const requested = params.step;
    const step: JoinStep = requested === "rules" || requested === "confirm" ? requested : "code";
    if (requested !== step || (!requested && code)) redirect(joinUrl(code, code ? "rules" : "code"));
    if (step !== "code" && !code) redirect(joinUrl(code, "code"));
    let preview: InvitePreview | undefined;
    let failure: ActionResult | undefined;
    const error = params.issue === "expired" ? "Ask for a new invite." : params.issue === "rate" ? "Too many requests. Please wait and try again." : params.issue === "unknown" ? "Check the code and try again." : "";
    if (step !== "code") {
        const { response, result } = await workflowRequest(`/invites/${encodeURIComponent(code)}`);
        if (result.status === 401) redirect(`/login?next=${encodeURIComponent(joinUrl(code, step))}`);
        if (!response) {
            if ([404, 410, 429].includes(result.status)) redirect(`${joinUrl(code, "code")}&issue=${result.status === 410 ? "expired" : result.status === 429 ? "rate" : "unknown"}`);
            failure = result;
        } else preview = await response.json() as InvitePreview;
    }
    return <JoinFlow code={code} step={step} preview={preview} error={error} failure={failure} />;
}
