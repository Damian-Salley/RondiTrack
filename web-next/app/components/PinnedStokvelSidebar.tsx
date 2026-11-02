"use client";

import Link from "next/link";
import { usePinnedStokvels } from "./PinnedStokvelProvider";

export default function PinnedStokvelSidebar() {
    const pins = usePinnedStokvels((state) => state.pins);

    return (
        <aside className="mb-6 rounded-xl border border-gray-700 p-4">
            <h2 className="mb-3 font-semibold">
                ★ Pinned stokvels
            </h2>

            {pins.length === 0 ? (
                <p className="text-sm text-gray-400">
                    No pinned stokvels yet.
                </p>
            ) : (
                <nav aria-label="Pinned stokvels">
                    <ul className="space-y-2">
                        {pins.map((pin) => (
                            <li key={pin.id}>
                                <Link
                                    href={`/stokvels/${pin.id}`}
                                    className="text-blue-400 hover:underline"
                                >
                                    {pin.name}
                                </Link>
                            </li>
                        ))}
                    </ul>
                </nav>
            )}
        </aside>
    );
}