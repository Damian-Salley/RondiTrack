"use client";

import { useState } from "react";
import {
    QueryClient,
    QueryClientProvider,
} from "@tanstack/react-query";

// Give each application instance its own QueryClient.
export default function QueryProvider({
    children,
}: {
    children: React.ReactNode;
}) {
    // Keep the same QueryClient across component re-renders.
    const [queryClient] = useState(
        () =>
            new QueryClient({
                defaultOptions: {
                    queries: {
                        // Avoid immediately refetching already loaded pages.
                        staleTime: 30_000,
                    },
                },
            })
    );

    return (
        <QueryClientProvider client={queryClient}>
            {children}
        </QueryClientProvider>
    );
}