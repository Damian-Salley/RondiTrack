import { spawnSync } from "node:child_process";

function askPassword() {
    return new Promise((resolve, reject) => {
        const result = spawnSync(
            "bash",
            ["-c", 'read -r -s -p "Enter Treasurer password: " password; printf "\\n%s" "$password"'],
            { encoding: "utf8", stdio: ["inherit", "pipe", "inherit"] }
        );

        if (result.error) {
            reject(result.error);
            return;
        }

        if (result.status !== 0) {
            reject(new Error("Password input failed."));
            return;
        }

        resolve(result.stdout.trim());
    });
}

try {
    const password = await askPassword();

    const response = await fetch(
        "http://localhost:5274/api/v1/auth/login",
        {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                email: "assignment71-demo@ronditrack.example",
                password
            })
        }
    );

    if (!response.ok) {
        throw new Error(`Login failed: HTTP ${response.status}`);
    }

    const data = await response.json();

    if (!data.accessToken) {
        throw new Error("Login response did not contain an access token.");
    }

    console.log("\nLogin successful. Running benchmark...\n");

    const result = spawnSync(
        process.execPath,
        ["scripts/check-assignment-82.mjs"],
        {
            stdio: "inherit",
            env: {
                ...process.env,
                BENCHMARK_ACCESS_TOKEN: data.accessToken
            }
        }
    );

    process.exitCode = result.status ?? 1;
} catch (error) {
    console.error(error.message);
    process.exitCode = 1;
}