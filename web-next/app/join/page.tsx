import { Suspense } from "react";
import Link from "next/link";
import type { Metadata } from "next";
import { redirect } from "next/navigation";

import { getSession } from "@/lib/session";
import { workflowRequest } from "@/lib/workflow-api";
import {
  joinUrl,
  type InvitePreview,
  type JoinStep,
} from "@/lib/join-flow";
import type { ActionResult } from "@/lib/workflow-types";

import JoinFlow from "./JoinFlow";

// Define safe metadata for invitation links.
//
// Invitation URLs contain private invitation codes, so search engines
// must not index them. Social media previews must also avoid exposing
// stokvel names, contribution amounts, or member information.
export async function generateMetadata(): Promise<Metadata> {
  return {
    title: "Stokvel invitation | RondiTrack",
    description:
      "You have an invitation to join a stokvel on RondiTrack. Sign in to check it.",

    robots: {
      index: false,
      follow: false,
    },

    openGraph: {
      title: "Stokvel invitation | RondiTrack",
      description: "Sign in to check your invitation.",
      images: [],
    },
  };
}

// Query parameters supported by the invitation page.
type Props = {
  searchParams: Promise<{
    code?: string;
    step?: string;
    issue?: string;
  }>;
};

// Render the invitation page while its content is being loaded.
export default function JoinPage(props: Props) {
  return (
    <main className="mx-auto max-w-xl p-6">
      <Suspense fallback={<p>Loading invite...</p>}>
        <JoinContent {...props} />
      </Suspense>
    </main>
  );
}

// Load the invitation and determine which join step to display.
async function JoinContent({ searchParams }: Props) {
  const params = await searchParams;

  // Signed-out visitors must sign in before viewing invitation details.
  // Preserve the invitation code and step so they can continue afterward.
  if (!(await getSession())) {
    const query = new URLSearchParams();

    if (typeof params.code === "string") {
      query.set("code", params.code);
    }

    if (typeof params.step === "string") {
      query.set("step", params.step);
    }

    return (
      <>
        <h1>Stokvel invitation</h1>

        <p>Sign in to check your invitation.</p>

        <Link
          href={`/login?next=${encodeURIComponent(`/join?${query}`)}`}
        >
          Sign in
        </Link>
      </>
    );
  }

  // Normalize the invitation code before using it.
  const code =
    typeof params.code === "string"
      ? params.code.trim().toUpperCase()
      : "";

  // Only allow the three supported join steps.
  const requested = params.step;

  const step: JoinStep =
    requested === "rules" || requested === "confirm"
      ? requested
      : "code";

  // Keep the URL synchronized with the current join step.
  if (requested !== step || (!requested && code)) {
    redirect(joinUrl(code, code ? "rules" : "code"));
  }

  // A valid invitation code is required after the first step.
  if (step !== "code" && !code) {
    redirect(joinUrl(code, "code"));
  }

  let preview: InvitePreview | undefined;
  let failure: ActionResult | undefined;

  // Show a user-friendly message for known invitation errors.
  const error =
    params.issue === "expired"
      ? "Ask for a new invite."
      : params.issue === "rate"
        ? "Too many requests. Please wait and try again."
        : params.issue === "unknown"
          ? "Check the code and try again."
          : "";

  // Retrieve invitation details only when the user is signed in
  // and has progressed beyond the code-entry step.
  if (step !== "code") {
    const { response, result } = await workflowRequest(
      `/invites/${encodeURIComponent(code)}`
    );

    // If the session has expired, redirect to login while preserving
    // the invitation code and current step.
    if (result.status === 401) {
      redirect(
        `/login?next=${encodeURIComponent(joinUrl(code, step))}`
      );
    }

    if (!response) {
      // Return to code entry when the invitation is invalid,
      // expired, or temporarily rate-limited.
      if ([404, 410, 429].includes(result.status)) {
        const issue =
          result.status === 410
            ? "expired"
            : result.status === 429
              ? "rate"
              : "unknown";

        redirect(`${joinUrl(code, "code")}&issue=${issue}`);
      }

      // Preserve other API failures so the UI can display feedback.
      failure = result;
    } else {
      // Load the invitation preview returned by the backend.
      preview = (await response.json()) as InvitePreview;
    }
  }

  // Display the appropriate step of the existing join workflow.
  return (
    <JoinFlow
      code={code}
      step={step}
      preview={preview}
      error={error}
      failure={failure}
    />
  );
}