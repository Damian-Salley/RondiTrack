import { test, expect } from "@playwright/test";
import AxeBuilder from "@axe-core/playwright";
import { parseRandToCents } from "../lib/formatters";

test.afterEach(async ({ context }) => {
    await context.storageState({ path: "e2e/.auth/sipho.json" });
});

async function axe(page: import("@playwright/test").Page) {
    const result = await new AxeBuilder({ page }).withTags(["wcag2a", "wcag2aa", "wcag21aa", "wcag22aa"]).analyze();
    expect(result.violations.filter(item => item.impact === "serious" || item.impact === "critical")).toEqual([]);
}

test("member gets real API 403 and stays signed in", async ({ page }) => {
    await page.goto("/stokvels/2");
    const response = await page.request.get("/bff/cycles/212/close-preview");
    expect(response.status()).toBe(403);
    const result = await response.json();
    expect(result.message).toContain("treasurers");
    await expect(page.getByRole("button", { name: "Record Contribution" })).toBeVisible();
    const navigation = await page.goto("/bff/cycles/212/close-preview");
    expect(navigation?.status()).toBe(403);
    await expect(page.locator("body")).toContainText(result.message);
    expect((await page.request.get("/bff/me/activity")).status()).toBe(200);
});

test("signed-out navigation has a safe return path and BFF returns JSON 401", async ({ browser }) => {
    const context = await browser.newContext({ storageState: { cookies: [], origins: [] } });
    const page = await context.newPage();
    await page.goto("/stokvels/2");
    await expect(page).toHaveURL(/\/login\?/);
    const url = new URL(page.url());
    expect(url.pathname).toBe("/login");
    expect(url.searchParams.get("next")).toBe("/stokvels/2");
    const response = await context.request.get("http://localhost:3000/bff/me/activity");
    expect(response.status()).toBe(401);
    expect(response.headers()["content-type"]).toContain("application/json");
    expect(response.headers()["x-robots-tag"]).toContain("noindex");
    await context.close();
});

test("R150,50 updates the displayed total by exactly 15050 cents", async ({ page }) => {
    await page.goto("/stokvels/3");
    const total = page.getByLabel("Total collected this cycle");
    await expect(total).toBeVisible();
    const before = parseRandToCents((await total.textContent())!);
    expect(before).not.toBeNull();
    await page.getByLabel("Member", { exact: true }).selectOption("1");
    await page.getByLabel("Amount (R)").fill("R 150,50");
    await page.getByRole("button", { name: "Record Contribution" }).click();
    await expect(page.getByRole("status").filter({ hasText: "Contribution recorded successfully" })).toBeVisible();
    await expect.poll(async () => parseRandToCents((await total.textContent())!)).toBe(before! + 15050);
});

test("home, private stokvel, invalid contribution and close dialog pass axe", async ({ page }) => {
    await page.goto("/stokvels");
    await expect(page.getByRole("heading", { name: /Private test stokvel 4 / })).toBeVisible();
    await axe(page);
    await page.goto("/stokvels/4");
    await expect(page.getByRole("button", { name: "Record Contribution" })).toBeVisible();
    await expect(page.locator('meta[name="robots"]')).toHaveAttribute("content", /noindex/);
    await axe(page);
    await page.getByLabel("Member", { exact: true }).selectOption("1");
    await page.getByLabel("Amount (R)").fill("bad");
    await page.getByRole("button", { name: "Record Contribution" }).click();
    await expect(page.getByLabel("Amount (R)")).toHaveAttribute("aria-invalid", "true");
    await axe(page);
    await page.getByRole("button", { name: "Close cycle", exact: true }).click();
    await expect(page.getByRole("alertdialog")).toBeVisible();
    await expect(page.getByRole("button", { name: /^Close .+ and record/ })).toBeEnabled();
    await expect(page.getByRole("button", { name: "Cancel", exact: true })).toBeFocused();
    await axe(page);
    await page.keyboard.press("Escape");
    await expect(page.getByRole("alertdialog")).not.toBeVisible();
    await expect(page.getByRole("button", { name: "Close cycle", exact: true })).toBeFocused();
});

test("real invite signed-out metadata and WhatsApp head reveal no private data", async ({ page, browser }) => {
    await page.goto("/stokvels/5");
    await page.getByText("Create a seven-day invite", { exact: true }).click();
    await page.getByLabel("Rules", { exact: true }).fill("Private test rules");
    await page.getByLabel("Contribution schedule").fill("Monthly");
    await page.getByRole("button", { name: /Create invite/i }).click();
    const invite = page.getByRole("link", { name: /^\/join\?code=/i });
    await expect(invite).toBeVisible();
    const href = await invite.getAttribute("href");
    expect(href).toContain("code=");
    const context = await browser.newContext({ storageState: { cookies: [], origins: [] } });
    const preview = await context.newPage();
    await preview.goto(href!);
    await expect(preview.getByRole("heading", { name: "Stokvel invitation" })).toBeVisible();
    await expect(preview.locator('meta[name="robots"]')).toHaveAttribute("content", /noindex/);
    const tags = await preview.locator('title, meta[name="description"], meta[property^="og:"]').evaluateAll(elements => elements.map(element => element.textContent + " " + element.getAttribute("content")).join(" "));
    expect(tags).not.toMatch(/Private test stokvel|150[,.]50|15050|Sipho/);
    const bot = await context.request.get(new URL(href!, "http://localhost:3000").toString(), { headers: { "User-Agent": "WhatsApp/2" } });
    const head = (await bot.text()).split("</head>")[0];
    expect(head).toContain("Stokvel invitation");
    expect(head).not.toMatch(/Private test stokvel|150[,.]50|15050|Sipho/);
    await context.close();
});
