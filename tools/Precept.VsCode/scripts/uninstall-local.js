#!/usr/bin/env node
const fs = require("fs");
const path = require("path");
const { spawnSync } = require("child_process");

const extensionRoot = path.resolve(__dirname, "..");

function quoteArg(arg) {
  return /\s/.test(arg) ? `"${arg}"` : arg;
}

const manifest = JSON.parse(
  fs.readFileSync(path.join(extensionRoot, "package.json"), "utf8")
);
const extensionId = `${manifest.publisher}.${manifest.name}`;

const isInsiders =
  !!process.env.VSCODE_GIT_ASKPASS_NODE &&
  process.env.VSCODE_GIT_ASKPASS_NODE.includes("Insiders");
const codeCli = isInsiders ? "code-insiders" : "code";

const cmdline = `${codeCli} --uninstall-extension ${quoteArg(extensionId)}`;
const result = spawnSync(cmdline, {
  cwd: extensionRoot,
  stdio: "inherit",
  shell: true,
});
if (result.status !== 0) {
  throw new Error(`${cmdline} failed with exit code ${result.status}`);
}

const hostLabel = isInsiders ? "VS Code Insiders" : "VS Code";
console.log("");
console.log(`Uninstalled ${extensionId} from your local ${hostLabel} profile.`);
console.log(
  "Run 'Developer: Reload Window' in VS Code if this window currently has the packaged extension loaded."
);
