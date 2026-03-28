const fs = require("fs");
const path = require("path");
const { spawn, spawnSync } = require("child_process");

const workspaceRoot = path.resolve(__dirname, "..", "..");
const projectPath = path.join(workspaceRoot, "tools", "Precept.Mcp", "Precept.Mcp.csproj");
const buildRoot = path.join(workspaceRoot, "temp", "dev-mcp");
const buildBinRoot = path.join(buildRoot, "bin");
const runtimeRoot = path.join(buildRoot, "runtime");
const runtimeSequenceFilePath = path.join(runtimeRoot, ".sequence");
const mcpDllName = "Precept.Mcp.dll";

main();

function main() {
  try {
    ensureBuild();
    const buildDllPath = resolveBuildDllPath();
    const runtimeDllPath = prepareRuntime(buildDllPath);
    launchRuntime(runtimeDllPath);
  } catch (error) {
    writeError(String(error instanceof Error ? error.message : error));
    process.exit(1);
  }
}

function ensureBuild() {
  fs.mkdirSync(buildRoot, { recursive: true });

  const build = spawnSync(
    "dotnet",
    ["build", projectPath, "--artifacts-path", buildRoot],
    {
      cwd: workspaceRoot,
      encoding: "utf8"
    }
  );

  if (build.status === 0) {
    return;
  }

  const output = [build.stdout, build.stderr]
    .filter((value) => typeof value === "string" && value.trim().length > 0)
    .join("\n")
    .trim();

  throw new Error(output.length > 0 ? output : "Precept MCP build failed.");
}

function resolveBuildDllPath() {
  const candidate = findFile(buildBinRoot, mcpDllName, (fullPath) => {
    const normalized = normalizePath(fullPath);
    return normalized.includes("/Precept.Mcp/") && normalized.includes("/debug/");
  });

  if (!candidate) {
    throw new Error(`Unable to locate ${mcpDllName} under ${buildBinRoot}.`);
  }

  return candidate;
}

function prepareRuntime(buildDllPath) {
  const buildDirectory = path.dirname(buildDllPath);
  fs.mkdirSync(runtimeRoot, { recursive: true });

  const runtimeDirectory = path.join(runtimeRoot, `run-${Date.now()}-${nextRuntimeSequence()}`);
  copyDirectory(buildDirectory, runtimeDirectory);
  pruneRuntimeDirectories(runtimeDirectory);

  return path.join(runtimeDirectory, mcpDllName);
}

function nextRuntimeSequence() {
  let nextValue = 1;

  try {
    if (fs.existsSync(runtimeSequenceFilePath)) {
      const previous = Number.parseInt(fs.readFileSync(runtimeSequenceFilePath, "utf8"), 10);
      if (Number.isFinite(previous) && previous >= 0) {
        nextValue = previous + 1;
      }
    }
  } catch {
    nextValue = 1;
  }

  fs.writeFileSync(runtimeSequenceFilePath, String(nextValue), "utf8");
  return nextValue;
}

function copyDirectory(sourceDirectory, targetDirectory) {
  fs.mkdirSync(targetDirectory, { recursive: true });

  for (const entry of fs.readdirSync(sourceDirectory, { withFileTypes: true })) {
    const sourcePath = path.join(sourceDirectory, entry.name);
    const targetPath = path.join(targetDirectory, entry.name);

    if (entry.isDirectory()) {
      copyDirectory(sourcePath, targetPath);
      continue;
    }

    fs.copyFileSync(sourcePath, targetPath);
  }
}

function pruneRuntimeDirectories(activeRuntimeDirectory) {
  if (!fs.existsSync(runtimeRoot)) {
    return;
  }

  for (const entry of fs.readdirSync(runtimeRoot, { withFileTypes: true })) {
    if (!entry.isDirectory()) {
      continue;
    }

    const candidate = path.join(runtimeRoot, entry.name);
    if (candidate === activeRuntimeDirectory) {
      continue;
    }

    try {
      fs.rmSync(candidate, { recursive: true, force: true });
    } catch {
      // Ignore cleanup failures; locked runtimes can be pruned on a later launch.
    }
  }
}

function launchRuntime(runtimeDllPath) {
  const child = spawn("dotnet", [runtimeDllPath], {
    cwd: workspaceRoot,
    stdio: "inherit"
  });

  child.on("exit", (code, signal) => {
    if (signal) {
      process.kill(process.pid, signal);
      return;
    }

    process.exit(code ?? 0);
  });

  child.on("error", (error) => {
    writeError(`Failed to launch Precept MCP runtime: ${String(error)}`);
    process.exit(1);
  });
}

function findFile(directory, fileName, predicate) {
  if (!fs.existsSync(directory)) {
    return undefined;
  }

  const stack = [directory];
  while (stack.length > 0) {
    const current = stack.pop();
    if (!current) {
      continue;
    }

    for (const entry of fs.readdirSync(current, { withFileTypes: true })) {
      const entryPath = path.join(current, entry.name);

      if (entry.isDirectory()) {
        stack.push(entryPath);
        continue;
      }

      if (entry.name !== fileName) {
        continue;
      }

      if (!predicate || predicate(entryPath)) {
        return entryPath;
      }
    }
  }

  return undefined;
}

function normalizePath(filePath) {
  return filePath.replace(/\\/g, "/");
}

function writeError(message) {
  process.stderr.write(`${message}\n`);
}