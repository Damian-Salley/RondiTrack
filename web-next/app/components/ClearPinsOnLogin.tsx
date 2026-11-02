"use client";

import { useEffect } from "react";

import {
    usePinnedStokvels,
    usePinsHydrated,
} from "./PinnedStokvelProvider";

export default function ClearPinsOnLogin() {
    const hydrated = usePinsHydrated();
    const clear = usePinnedStokvels((state) => state.clear);

    useEffect(() => {
        if (hydrated) {
            clear();
        }
    }, [hydrated, clear]);

    return null;
}