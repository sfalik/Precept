const fs = require("fs");
const path = require("path");

const workspaceRoot = path.resolve(__dirname, "..", "..");

function copyTree(sourceRoot, targetRoot, shouldInclude) {
  if (!fs.existsSync(sourceRoot)) {
    return;
  }

  for (const entry of fs.readdirSync(sourceRoot, { withFileTypes: true })) {
    if (!shouldInclude(entry.name, entry.isDirectory())) {
      continue;
    }

    const sourcePath = path.join(sourceRoot, entry.name);
    const targetPath = path.join(targetRoot, entry.name);

    if (entry.isDirectory()) {
      copyTree(sourcePath, targetPath, () => true);
      continue;
    }

    fs.mkdirSync(path.dirname(targetPath), { recursive: true });
    fs.copyFileSync(sourcePath, targetPath);
    process.stdout.write(`Synced ${path.relative(workspaceRoot, targetPath)}\n`);
  }
}

copyTree(
  path.join(workspaceRoot, ".github", "agents"),
  path.join(workspaceRoot, "tools", "Precept.Plugin", "agents"),
  (name, isDirectory) => isDirectory ? name.startsWith("precept-") : name.startsWith("precept-")
);

copyTree(
  path.join(workspaceRoot, ".github", "skills"),
  path.join(workspaceRoot, "tools", "Precept.Plugin", "skills"),
  (name, isDirectory) => isDirectory ? name.startsWith("precept-") : true
);