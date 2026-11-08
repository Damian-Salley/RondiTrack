import { test as setup, expect } from "@playwright/test";
import { mkdir } from "node:fs/promises";

setup("sign in once", async ({ page }) => {
    setup.setTimeout(90000);
    await expect.poll(() => page.request.get("http://localhost:8080/health/ready")
        .then(response => response.ok()).catch(() => false), { timeout: 60000 }).toBe(true);
    if (!process.env.E2E_PASSWORD) throw new Error("Set the throwaway E2E_PASSWORD used by Compose.");
    await page.goto("/login");
    await page.getByLabel("Email").fill("sipho@assignment92.example");
    await page.getByLabel("Password", { exact: true }).fill(process.env.E2E_PASSWORD);
    await page.getByRole("button", { name: "Sign in", exact: true }).click();
    await expect(page).toHaveURL(/\/stokvels$/);
    await mkdir("e2e/.auth", { recursive: true });
    await page.context().storageState({ path: "e2e/.auth/sipho.json" });
});
