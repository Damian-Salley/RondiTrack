import { spawnSync } from "node:child_process";

for (let run = 1; run <= 10; run++) {
    const timezone = run % 2 ? "UTC" : "Africa/Johannesburg";
    console.log(`\nRun ${run}/10 TZ=${timezone}`);
    const result = spawnSync(process.execPath, ["node_modules/vitest/vitest.mjs", "run"], {
        stdio: "inherit", env: { ...process.env, TZ: timezone },
    });
    if (result.status !== 0) process.exit(result.status ?? 1);
}
