"use client";

import { useEffect } from "react";

import {
    usePinnedStokvels,
    usePinsHydrated,
} from "./PinnedStokvelProvider";

type Stokvel = {
    id: number;
    name: string;
};

export default function SyncPinnedStokvels({
    stokvels,
}: {
    stokvels: Stokvel[];
}) {
    const hydrated = usePinsHydrated();

    const pins = usePinnedStokvels((state) => state.pins);
    const unpin = usePinnedStokvels((state) => state.unpin);

    useEffect(() => {
        if (!hydrated) {
            return;
        }

        const validIds = new Set(
            stokvels.map((stokvel) => stokvel.id)
        );

        for (const pin of pins) {
            if (!validIds.has(pin.id)) {
                unpin(pin.id);
            }
        }
    }, [hydrated, stokvels, pins, unpin]);

    return null;
}