import { readFileSync } from "node:fs";
import process from "node:process";

// Reads the installed version of a dependency straight from node_modules.
function readInstalledVersion(packageName) {
    try {
        const packageJsonUrl = new URL(
            `../node_modules/${packageName}/package.json`,
            import.meta.url
        );
        const packageJson = JSON.parse(readFileSync(packageJsonUrl, "utf8"));
        return packageJson.version;
    } catch {
        return "not installed (run `npm install`)";
    }
}

// npm sets npm_config_user_agent (for example "npm/11.6.2 node/v24.1.0 ...")
// when a script is started with `npm run`.
function readNpmVersion() {
    const userAgent = process.env.npm_config_user_agent ?? "";
    const match = /^npm\/(\S+)/.exec(userAgent);
    return match ? match[1] : "unknown (start this script with `npm run runtime:info`)";
}

console.log("Runtime information");
console.log("-------------------");
console.log(`Node.js    : ${process.version}`);
console.log(`npm        : ${readNpmVersion()}`);
console.log(`Vite       : ${readInstalledVersion("vite")}`);
console.log(`TypeScript : ${readInstalledVersion("typescript")}`);
console.log(`React      : ${readInstalledVersion("react")}`);
console.log(`Platform   : ${process.platform} (${process.arch})`);