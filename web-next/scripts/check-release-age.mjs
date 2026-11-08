import { readFile } from "node:fs/promises";

const pkg = JSON.parse(await readFile(new URL("../package.json", import.meta.url), "utf8"));
const cutoff = new Date(process.argv[2] ?? "2026-11-08T00:00:00Z").getTime() - 14 * 86400000;
for (const [name, version] of Object.entries({ ...pkg.dependencies, ...pkg.devDependencies, ...pkg.overrides })) {
    if (!/^\d+\.\d+\.\d+$/.test(version)) throw new Error(`${name} must have an exact version`);
    const response = await fetch(`https://registry.npmjs.org/${encodeURIComponent(name)}`);
    if (!response.ok) throw new Error(`${name}: registry returned ${response.status}`);
    const metadata = await response.json();
    const published = metadata.time[version];
    const oldEnough = published && new Date(published).getTime() <= cutoff;
    console.log(`${name}@${version}\t${published}\t${oldEnough ? "PASS" : "TOO NEW"}`);
    if (!oldEnough) process.exitCode = 1;
}
