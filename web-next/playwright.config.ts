import { defineConfig, devices } from "@playwright/test";

export default defineConfig({
    testDir: "./e2e",
    fullyParallel: false,
    workers: 1,
    retries: 0,
    reporter: [["list"], ["html", { open: "never" }]],
    use: { baseURL: "http://localhost:3000", trace: "on", ...devices["Desktop Chrome"] },
    webServer: { command: "npm run start", url: "http://localhost:3000/login", reuseExistingServer: false },
    projects: [
        { name: "setup", testMatch: /auth.setup.ts/ },
        { name: "chromium", testIgnore: /auth.setup.ts/, dependencies: ["setup"], use: { storageState: "e2e/.auth/sipho.json" } },
    ],
});
