import { createStore } from "zustand/vanilla";
import { persist, createJSONStorage } from "zustand/middleware";

// Only save the information needed for pinned navigation.
export type PinnedStokvel = {
    id: number;
    name: string;
};

export type PinnedStokvelState = {
    pins: PinnedStokvel[];
    pin: (stokvel: PinnedStokvel) => void;
    unpin: (id: number) => void;
    clear: () => void;
};

// Create a separate Zustand store for each provider.
export function createPinnedStokvelStore() {
    return createStore<PinnedStokvelState>()(
        persist(
            (set) => ({
                pins: [],

                // Add a stokvel only if it is not already pinned.
                pin: (stokvel) =>
                    set((state) => ({
                        pins: state.pins.some(
                            (pin) => pin.id === stokvel.id
                        )
                            ? state.pins
                            : [...state.pins, stokvel],
                    })),

                // Remove a stokvel using its ID.
                unpin: (id) =>
                    set((state) => ({
                        pins: state.pins.filter(
                            (pin) => pin.id !== id
                        ),
                    })),

                // Remove every pinned stokvel.
                clear: () => set({ pins: [] }),
            }),
            {
                name: "ronditrack-pinned-stokvels",

                // Store pins in the browser's local storage.
                storage: createJSONStorage(() => localStorage),

                // Wait until the browser mounts before reading storage.
                skipHydration: true,

                // Persist only the pins, not the store's functions.
                partialize: (state) => ({
                    pins: state.pins,
                }),
            }
        )
    );
}