import {
    useQuery,
    useInfiniteQuery,
    useMutation,
    useQueryClient,
} from "@tanstack/react-query";
import { api } from "./client";

// Retrieve the authenticated user's stokvel dashboard.
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

// Retrieve all stokvels.
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

// Retrieve a specific stokvel.
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

// Retrieve contribution cycles for the selected stokvel.
export function useCycles(stokvelId: number | null) {
    return useQuery({
        queryKey: ["cycles", stokvelId],
        enabled: stokvelId !== null,
        queryFn: async () => {
            const { data, error } = await api.GET("/api/v1/contributioncycles");

            if (error) throw error;

            return (data ?? []).filter(
                (cycle) => Number(cycle.stokvelId) === stokvelId
            );
        },
        retry: false,
    });
}

// Retrieve paginated contributions for the selected stokvel.
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

// Retrieve the authenticated user's details.
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

// Retrieve members of the selected stokvel.
export function useMembers(stokvelId: number | null) {
    return useQuery({
        queryKey: ["members", stokvelId],
        enabled: stokvelId !== null,
        queryFn: async () => {
            const { data, error } = await api.GET(
                "/api/v1/stokvels/{stokvelId}/members",
                {
                    params: {
                        path: { stokvelId: stokvelId! },
                    },
                }
            );

            if (error) throw error;

            return data ?? [];
        },
        retry: false,
    });
}

// Information required to record a contribution.
interface RecordContributionInput {
    stokvelId: number;
    cycleId: number;
    userId: number;
    amountInCents: number;
    idempotencyKey: string;
}

// Submit a contribution and refresh the dashboard on success.
export function useRecordContribution() {
    const queryClient = useQueryClient();

    return useMutation({
        mutationFn: async ({
            stokvelId,
            cycleId,
            userId,
            amountInCents,
            idempotencyKey,
        }: RecordContributionInput) => {
            const { data, error } = await api.POST(
                "/api/v1/stokvels/{stokvelId}/cycles/{cycleId}/contributions",
                {
                    params: {
                        path: {
                            stokvelId,
                            cycleId,
                        },
                        header: {
                            "Idempotency-Key": idempotencyKey,
                        },
                    },
                    body: {
                        userId,
                        amountInCents,
                    },
                }
            );

            if (error) throw error;

            return data;
        },

        onSuccess: async (_data, variables) => {
            await Promise.all([
                queryClient.invalidateQueries({ queryKey: ["home"] }),
                queryClient.invalidateQueries({
                    queryKey: ["cycles", variables.stokvelId],
                }),
                queryClient.invalidateQueries({
                    queryKey: ["contributions", variables.stokvelId],
                }),
            ]);
        },

        retry: false,
    });
}