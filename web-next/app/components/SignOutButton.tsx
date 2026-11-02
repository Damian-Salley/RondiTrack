"use client";

import { usePinnedStokvels } from "./PinnedStokvelProvider";

export default function SignOutButton() {
    const clear = usePinnedStokvels((state) => state.clear);

    return (
        <button
            type="submit"
            onClick={() => clear()}
            className="rounded-lg border border-gray-600 px-4 py-2 text-sm font-medium hover:bg-gray-800"
        >
            Sign out
        </button>
    );
}