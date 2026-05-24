#!/usr/bin/env node
const fs = require("fs");
const path = require("path");
const { spawnSync } = require("child_process");

const extensionRoot = path.resolve(__dirname, "..");
const repoRoot = path.resolve(extensionRoot, "..", "..");

function quoteArg(arg) {
  return /\s/.test(arg) ? `"${arg}"` : arg;
}

function run(command, args, options = {}) {
  const cmdline = [command, ...args.map(quoteArg)].join(" ");
  const result = spawnSync(cmdline, {
    cwd: options.cwd || extensionRoot,
    stdio: "inherit",
    shell: true,
  });
  if (result.status !== 0) {
    throw new Error(`${cmdline} failed with exit code ${result.status}`);
  }
}

const typescriptMarker = path.join(
  extensionRoot,
  "node_modules",
  "typescript",
  "package.json"
);
if (!fs.existsSync(typescriptMarker)) {
  console.log("Extension dependencies missing; running npm install...");
  run("npm", ["install"]);
}

// Rebuild dev language server so the installed extension picks up the latest source.
// The bundled server is rebuilt separately by vscode:prepublish (dotnet publish -o ./server).
// Without this step, the running extension would use a potentially stale dev DLL from
// temp/dev-language-server/ since dev mode bypasses the bundled server entirely.
const lsProject = path.join(
  repoRoot,
  "tools",
  "Precept.LanguageServer",
  "Precept.LanguageServer.csproj"
);
const devArtifactsPath = path.join(repoRoot, "temp", "dev-language-server");
console.log("Rebuilding dev language server...");
run("dotnet", [
  "build",
  lsProject,
  "--artifacts-path",
  devArtifactsPath,
  "-c",
  "Release",
]);

run("npm", ["run", "package:local"]);

const manifest = JSON.parse(
  fs.readFileSync(path.join(extensionRoot, "package.json"), "utf8")
);
const vsixName = `${manifest.name}-${manifest.version}.vsix`;
const vsixPath = path.join(extensionRoot, vsixName);
if (!fs.existsSync(vsixPath)) {
  throw new Error(`VSIX not found: ${vsixPath}`);
}

const isInsiders =
  !!process.env.VSCODE_GIT_ASKPASS_NODE &&
  process.env.VSCODE_GIT_ASKPASS_NODE.includes("Insiders");
const codeCli = isInsiders ? "code-insiders" : "code";

run(codeCli, ["--install-extension", vsixPath, "--force"]);

const hostLabel = isInsiders ? "VS Code Insiders" : "VS Code";
console.log("");
console.log(`Installed ${vsixName} into your local ${hostLabel} profile.`);
console.log(
  "Run 'Developer: Reload Window' in VS Code to load the updated extension in this window."
);
