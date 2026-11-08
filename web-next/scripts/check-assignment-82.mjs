// Import Node.js utilities for reading files and measuring performance.
import { readFile } from "node:fs/promises";
import { performance } from "node:perf_hooks";
import assert from "node:assert/strict";

// Read source files from the current Next.js project.
async function readSource(path) {
    return readFile(new URL(`../${path}`, import.meta.url), "utf8");
}

// Check that the contribution implementation uses the required API contract.
async function checkContributionContract() {
    const action = await readSource("app/actions/contributions.ts");
    const controller = await readFile(
        new URL("../../Controllers/StokvelsController.cs", import.meta.url),
        "utf8"
    );

    assert.match(action, /\/cycles\/\$\{cycleId\}\/contributions/);
    assert.match(
        controller,
        /HttpPost\("\{stokvelId:int\}\/cycles\/\{cycleId:int\}\/contributions"\)/
    );

    assert.match(action, /"Idempotency-Key"/);
    assert.match(action, /amountInCents/);
    assert.match(controller, /Idempotency-Replayed/);

    console.log("PASS: Contribution API contract");
}

// Check that private API requests explicitly avoid shared caching.
async function checkPrivateCaching() {
    const api = await readSource("lib/api.ts");

    assert.match(api, /cache:\s*"no-store"/);

    console.log("PASS: Private API requests use no-store");
}

// Check that the stokvel page uses concurrent fetching and Suspense.
async function checkParallelFetching() {
    const page = await readSource("app/(signed-in)/stokvels/[id]/page.tsx");

    assert.match(page, /Promise\.all/);
    assert.match(page, /contributionsPromise/);
    assert.match(page, /<Suspense/);

    console.log("PASS: Parallel fetching and Suspense are configured");
}

// Read configuration from environment variables.
// Never store credentials or tokens directly in source code.
const apiBaseUrl = (
    process.env.BENCHMARK_API_BASE_URL ?? "http://localhost:5274"
).replace(/\/$/, "");

const stokvelId = process.env.BENCHMARK_STOKVEL_ID ?? "9002";

const accessToken = process.env.BENCHMARK_ACCESS_TOKEN;

// Use real, read-only endpoints exposed by the RondiTrack backend.
// These requests do not create or modify database records.
const endpoints = [
    `/api/v1/stokvels/${stokvelId}`,
    `/api/v1/stokvels/${stokvelId}/members`,
    `/api/v1/stokvels/${stokvelId}/contributions`,
    `/api/v1/me/home`
];

// Make one authenticated request to the actual backend.
async function requestEndpoint(endpoint) {
    const response = await fetch(`${apiBaseUrl}${endpoint}`, {
        method: "GET",
        headers: {
            Authorization: `Bearer ${accessToken}`
        },
        cache: "no-store"
    });

    // A benchmark is invalid if it measures failed API requests.
    if (!response.ok) {
        throw new Error(
            `Benchmark request failed: ${endpoint} returned HTTP ${response.status}`
        );
    }

    // Consume the response body so both approaches perform equivalent work.
    await response.arrayBuffer();
}

// Measure the time required to execute a function.
async function measure(operation) {
    const start = performance.now();

    await operation();

    return performance.now() - start;
}

// Benchmark real backend requests.
async function benchmarkFetching() {
    if (!accessToken) {
        console.log(
            "SKIP: Real API benchmark requires BENCHMARK_ACCESS_TOKEN."
        );
        return;
    }

    // Check every endpoint before measuring.
    // This prevents failed authorization requests from skewing the results.
    for (const endpoint of endpoints) {
        await requestEndpoint(endpoint);
    }

    const iterations = 5;
    const sequentialResults = [];
    const parallelResults = [];

    // Alternate execution order to reduce order-related bias.
    for (let iteration = 0; iteration < iterations; iteration++) {
        const sequential = async () => {
            for (const endpoint of endpoints) {
                await requestEndpoint(endpoint);
            }
        };

        const parallel = async () => {
            await Promise.all(endpoints.map(requestEndpoint));
        };

        if (iteration % 2 === 0) {
            sequentialResults.push(await measure(sequential));
            parallelResults.push(await measure(parallel));
        } else {
            parallelResults.push(await measure(parallel));
            sequentialResults.push(await measure(sequential));
        }
    }

    // Use the median to reduce the effect of occasional slow requests.
    const median = (values) => {
        const sorted = [...values].sort((a, b) => a - b);
        return sorted[Math.floor(sorted.length / 2)];
    };

    const sequentialMs = median(sequentialResults);
    const parallelMs = median(parallelResults);

    console.log("\nREAL BACKEND BENCHMARK");
    console.log(`Backend: ${apiBaseUrl}`);
    console.log(`Stokvel: ${stokvelId}`);
    console.log(`Iterations: ${iterations}`);
    console.log(`Sequential median: ${sequentialMs.toFixed(1)} ms`);
    console.log(`Parallel median:   ${parallelMs.toFixed(1)} ms`);
    console.log(
        `Measured speedup: ${(sequentialMs / parallelMs).toFixed(2)}x`
    );
}

// Run all checks.
async function main() {
    await checkContributionContract();
    await checkPrivateCaching();
    await checkParallelFetching();
    await benchmarkFetching();

    console.log("\nAssignment 8.2 checks completed.");
}

main().catch((error) => {
    console.error("Assignment check failed:", error.message);
    process.exitCode = 1;
});