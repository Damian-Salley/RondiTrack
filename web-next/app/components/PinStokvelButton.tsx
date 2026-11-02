"use client";

import {
    usePinnedStokvels,
    usePinsHydrated,
} from "./PinnedStokvelProvider";

type Props = {
    id: number;
    name: string;
};

export default function PinStokvelButton({ id, name }: Props) {
    const hydrated = usePinsHydrated();

    const isPinned = usePinnedStokvels((state) =>
        state.pins.some((pin) => pin.id === id)
    );

    const pin = usePinnedStokvels((state) => state.pin);
    const unpin = usePinnedStokvels((state) => state.unpin);

    return (
        <button
            type="button"
            disabled={!hydrated}
            aria-pressed={hydrated && isPinned}
            onClick={() => {
                if (isPinned) {
                    unpin(id);
                } else {
                    pin({ id, name });
                }
            }}
            className="rounded-lg border border-gray-600 px-3 py-2 text-sm hover:bg-gray-800 disabled:opacity-50"
        >
            {!hydrated
                ? "Loading..."
                : isPinned
                  ? "★ Unpin"
                  : "☆ Pin"}
        </button>
    );
}