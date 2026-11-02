"use client";

import {
    createContext,
    useContext,
    useEffect,
    useState,
    useSyncExternalStore,
    type ReactNode,
} from "react";

import { useStore } from "zustand";

import {
    createPinnedStokvelStore,
    type PinnedStokvelState,
} from "@/lib/pinned-stokvels";

type PinnedStore = ReturnType<typeof createPinnedStokvelStore>;

const PinnedStokvelContext = createContext<PinnedStore | null>(null);

export default function PinnedStokvelProvider({
    children,
}: {
    children: ReactNode;
}) {
    const [store] = useState(createPinnedStokvelStore);

    useEffect(() => {
        void store.persist.rehydrate();
    }, [store]);

    return (
        <PinnedStokvelContext.Provider value={store}>
            {children}
        </PinnedStokvelContext.Provider>
    );
}

export function usePinnedStokvels<T>(
    selector: (state: PinnedStokvelState) => T
): T {
    const store = useContext(PinnedStokvelContext);

    if (!store) {
        throw new Error(
            "usePinnedStokvels must be used inside PinnedStokvelProvider."
        );
    }

    return useStore(store, selector);
}

export function usePinsHydrated(): boolean {
    const store = useContext(PinnedStokvelContext);

    if (!store) {
        throw new Error("PinnedStokvelProvider is missing.");
    }

    const hydrated = useSyncExternalStore(
        (callback) => {
            const unsubscribe = store.persist.onFinishHydration(callback);
            return unsubscribe;
        },
        () => store.persist.hasHydrated(),
        () => false
    );

    return hydrated;
}