import { useQuery, useInfiniteQuery } from "@tanstack/react-query";
import { api } from "./client";

export function useHome() {
    return useQuery({
        queryKey: ["home"],
        queryFn: async () => {
            const { data, error } = await api.GET("/api/v1/me/home");

            if (error) throw error;

            return data;
        },
        retry: false,
    });
}

export function useStokvels() {
    return useQuery({
        queryKey: ["stokvels"],
        queryFn: async () => {
            const { data, error } = await api.GET("/api/v1/stokvels");

            if (error) throw error;

            return data;
        },
        retry: false,
    });
}

export function useStokvel(id: number | null) {
    return useQuery({
        queryKey: ["stokvel", id],
        enabled: id !== null,
        queryFn: async () => {
            const { data, error } = await api.GET("/api/v1/stokvels/{id}", {
                params: {
                    path: { id: id! },
                },
            });

            if (error) throw error;

            return data;
        },
        retry: false,
    });
}

export function useCycles(stokvelId: number | null) {
    return useQuery({
        queryKey: ["cycles", stokvelId],
        enabled: stokvelId !== null,
        queryFn: async () => {
            const { data, error } = await api.GET(
                "/api/v1/stokvels/{stokvelId}/cycles",
                {
                    params: {
                        path: { stokvelId: stokvelId! },
                    },
                }
            );

            if (error) throw error;

            return data;
        },
        retry: false,
    });
}

export function useContributions(stokvelId: number | null) {
    return useInfiniteQuery({
        queryKey: ["contributions", stokvelId],
        enabled: stokvelId !== null,
        initialPageParam: "",
        queryFn: async ({ pageParam }) => {
            const { data, error } = await api.GET(
                "/api/v1/stokvels/{stokvelId}/contributions",
                {
                    params: {
                        path: { stokvelId: stokvelId! },
                        query: {
                            PageSize: 20,
                            ...(pageParam ? { PageToken: pageParam } : {}),
                        },
                    },
                }
            );

            if (error) throw error;

            return data;
        },
        getNextPageParam: (lastPage) =>
            lastPage?.nextPageToken ?? undefined,
        retry: false,
    });
}

export function useMe() {
    return useQuery({
        queryKey: ["me"],
        queryFn: async () => {
            const { data, error } = await api.GET("/api/v1/me");

            if (error) throw error;

            return data;
        },
        retry: false,
    });
}