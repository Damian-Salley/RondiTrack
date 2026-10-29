"use server";

import { redirect } from "next/navigation";
import { createSession, deleteSession, getSession } from "@/lib/session";

const baseUrl = process.env.API_BASE_URL;

export async function login(formData: FormData) {
    const email = String(formData.get("email") ?? "");
    const password = String(formData.get("password") ?? "");

    const response = await fetch(`${baseUrl}/api/v1/auth/login`, {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({ email, password }),
        cache: "no-store",
    });

    if (!response.ok) {
        redirect("/login?error=1");
    }

    const data = (await response.json()) as {
        accessToken: string;
    };

    const refreshCookie = response.headers
        .getSetCookie()
        .find((cookie) => cookie.startsWith("refreshToken="));

    const refreshToken = refreshCookie
        ?.split(";")[0]
        .substring("refreshToken=".length);

    if (!refreshToken) {
        throw new Error("Backend did not return a refresh token.");
    }

    await createSession({
        accessToken: data.accessToken,
        refreshToken,
    });

    redirect("/stokvels");
}

export async function logout() {
    const session = await getSession();

    if (session) {
        await fetch(`${baseUrl}/api/v1/auth/logout`, {
            method: "POST",
            headers: {
                Cookie: `refreshToken=${session.refreshToken}`,
            },
            cache: "no-store",
        });
    }

    await deleteSession();

    redirect("/login");
}