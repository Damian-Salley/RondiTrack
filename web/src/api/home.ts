// Import the latest API contracts generated from ASP.NET Core.
import type { paths } from "./schema";

// Obtain the home endpoint's successful JSON response type.
export type HomeResponse =
    paths["/api/v1/me/home"]["get"]["responses"][200]["content"]["application/json"];

// Retrieve the authenticated user's home-screen information.
export async function getHome(
    baseUrl: string,
    accessToken: string
): Promise<HomeResponse> {
    // Send an authenticated request to the API.
    const response = await fetch(`${baseUrl}/api/v1/me/home`, {
        method: "GET",
        headers: {
            Authorization: `Bearer ${accessToken}`
        },
        credentials: "include"
    });

    // Reject unsuccessful HTTP responses.
    if (!response.ok) {
        throw new Error(`Home request failed: ${response.status}`);
    }

    // Parse the response using the generated TypeScript contract.
    return (await response.json()) as HomeResponse;
}

// Verify that the generated contract contains the property
// expected by the frontend.
export function getStokvelNames(home: HomeResponse): string[] {
    // Use an empty string if the API property is missing.
    return home.map(stokvel => stokvel.stokvelName ?? "");
}