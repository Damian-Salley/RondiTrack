import createClient from "openapi-fetch";
import type { paths } from "./schema";
import { authFetch } from "../auth/session";

export const api = createClient<paths>({
    baseUrl: import.meta.env.VITE_API_BASE_URL,
    fetch: async (request) => authFetch(
        new URL(request.url).pathname + new URL(request.url).search,
        {
            method: request.method,
            headers: request.headers,
            body: request.body ? await request.text() : undefined,
        }
    ),
});