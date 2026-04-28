import { spawnSync } from "child_process";
import * as fs from "fs";
import * as path from "path";
import * as vscode from "vscode";
import {
  LanguageClient,
  LanguageClientOptions,
  RevealOutputChannelOn,
  ServerOptions
} from "vscode-languageclient/node";

let client: LanguageClient | undefined;
let currentPreviewPanel: vscode.WebviewPanel | undefined;
let currentPreviewTarget: vscode.Uri | undefined;
let previewFollowActiveEditor = true;
let languageServerRestartTimer: NodeJS.Timeout | undefined;
let languageServerRestartInProgress = false;
let languageServerRestartRequested = false;
let languageServerRuntimeSequence = 0;
let languageServerStatusItem: vscode.StatusBarItem | undefined;
let currentLanguageServerLaunchInfo: LanguageServerLaunchInfo | undefined;

const languageServerDllName = "Precept.LanguageServer.dll";
const bundledServerRelativePath = path.join("server", languageServerDllName);
const devLanguageServerRootRelativePath = path.join("temp", "dev-language-server");
const devLanguageServerBuildDllRelativePath = path.join(
  devLanguageServerRootRelativePath,
  "bin",
  "Precept.LanguageServer",
  "debug",
  languageServerDllName
);
const devLanguageServerRuntimeRelativePath = path.join(devLanguageServerRootRelativePath, "runtime");

type LanguageServerLaunchMode = "dev-build-shadow-copy" | "bundled";

interface LanguageServerLaunchInfo {
  mode: LanguageServerLaunchMode;
  command: string;
  args: string[];
  cwd: string;
  projectPath?: string;
  runtimeDllPath?: string;
}


export async function activate(context: vscode.ExtensionContext): Promise<void> {
  const outputChannel = vscode.window.createOutputChannel("Precept");
  context.subscriptions.push(outputChannel);

  languageServerStatusItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left, 100);
  languageServerStatusItem.name = "Precept Language Server";
  languageServerStatusItem.command = "precept.showLanguageServerMode";
  context.subscriptions.push(languageServerStatusItem);
  updateLanguageServerStatusItem("starting", undefined, undefined);
  languageServerStatusItem.show();

  const openPreviewDisposable = vscode.commands.registerCommand("precept.openPreview", () => {
    void openPreviewPanel();
  });
  context.subscriptions.push(openPreviewDisposable);

  const togglePreviewLockingDisposable = vscode.commands.registerCommand("precept.togglePreviewLocking", () => {
    togglePreviewLocking();
  });
  context.subscriptions.push(togglePreviewLockingDisposable);

  const activeEditorSubscription = vscode.window.onDidChangeActiveTextEditor((editor) => {
    if (!previewFollowActiveEditor || !currentPreviewPanel) {
      return;
    }
    if (!editor || editor.document.languageId !== "precept") {
      return;
    }
    retargetPreviewPanel(currentPreviewPanel, editor.document.uri, editor.document.fileName);
  });
  context.subscriptions.push(activeEditorSubscription);

  const showLanguageServerModeDisposable = vscode.commands.registerCommand("precept.showLanguageServerMode", () => {
    showLanguageServerMode(outputChannel);
  });
  context.subscriptions.push(showLanguageServerModeDisposable);

  const launchInfo = resolveLanguageServerLaunchInfo(context, outputChannel);

  if (!launchInfo) {
    updateLanguageServerStatusItem("error", "server not found", undefined);
    outputChannel.appendLine("Language server not found. Checked dev build and bundled server paths.");
    void vscode.window.showErrorMessage(
      "Precept: could not locate the language server. See the Precept output channel for details."
    );
    outputChannel.show(true);
    return;
  }

  const serverWorkingDirectory = launchInfo.cwd;

  const clientOptions: LanguageClientOptions = {
    outputChannel,
    revealOutputChannelOn: RevealOutputChannelOn.Error,
    documentSelector: [
      {
        scheme: "file",
        language: "precept"
      }
    ],
    synchronize: {
      fileEvents: vscode.workspace.createFileSystemWatcher("**/*.precept")
    }
  };

  try {
    await startLanguageClient(launchInfo, clientOptions, outputChannel);
  } catch (error) {
    updateLanguageServerStatusItem("error", "startup failed", undefined);
    outputChannel.appendLine(`Language client failed to start: ${String(error)}`);
    void vscode.window.showErrorMessage("Precept: failed to start the language server. See the Precept output channel for details.");
    outputChannel.show(true);
    return;
  }

  if (launchInfo.mode === "dev-build-shadow-copy") {
    const devLanguageServerWatcher = createDevLanguageServerWatcher(
      serverWorkingDirectory,
      () => {
        scheduleLanguageClientRestart(launchInfo, clientOptions, outputChannel);
      },
      outputChannel
    );
    context.subscriptions.push(devLanguageServerWatcher);
    context.subscriptions.push(
      new vscode.Disposable(() => {
        if (!languageServerRestartTimer) {
          return;
        }

        clearTimeout(languageServerRestartTimer);
        languageServerRestartTimer = undefined;
      })
    );
  }

}

export async function deactivate(): Promise<void> {
  if (currentPreviewPanel) {
    currentPreviewPanel.dispose();
    currentPreviewPanel = undefined;
  }
  currentPreviewTarget = undefined;
  previewFollowActiveEditor = true;

  if (languageServerRestartTimer) {
    clearTimeout(languageServerRestartTimer);
    languageServerRestartTimer = undefined;
  }

  languageServerStatusItem?.dispose();
  languageServerStatusItem = undefined;

  if (!client) {
    return;
  }

  await client.stop();
  client = undefined;
}

function resolveLanguageServerProjectPath(context: vscode.ExtensionContext, output: vscode.OutputChannel): string | undefined {
  const relativeProjectPath = path.join(
    "tools",
    "Precept.LanguageServer",
    "Precept.LanguageServer.csproj"
  );

  const workspaceFolders = vscode.workspace.workspaceFolders ?? [];
  const candidates: string[] = [];

  for (const folder of workspaceFolders) {
    const workspaceRoot = folder.uri.fsPath;

    candidates.push(path.join(workspaceRoot, relativeProjectPath));
    candidates.push(
      path.resolve(
        workspaceRoot,
        "..",
        "Precept.LanguageServer",
        "Precept.LanguageServer.csproj"
      )
    );
  }

  candidates.push(
    path.resolve(
      context.extensionPath,
      "..",
      "Precept.LanguageServer",
      "Precept.LanguageServer.csproj"
    )
  );

  for (const candidate of candidates) {
    output.appendLine(`Server project candidate: ${candidate}`);
  }

  return candidates.find((candidate) => fs.existsSync(candidate));
}

function resolveLanguageServerLaunchInfo(
  context: vscode.ExtensionContext,
  output: vscode.OutputChannel
): LanguageServerLaunchInfo | undefined {
  // Dev mode: try to find the .csproj and build from source (shadow-copy with hot restart).
  const projectPath = resolveLanguageServerProjectPath(context, output);
  if (projectPath) {
    const projectDirectory = path.dirname(projectPath);
    const serverWorkingDirectory = path.resolve(projectDirectory, "..", "..");
    output.appendLine(`Language server project: ${projectPath}`);
    output.appendLine(`Language server cwd: ${serverWorkingDirectory}`);
    try {
      return resolveDevLaunchConfiguration(projectPath, serverWorkingDirectory, output);
    } catch (error) {
      output.appendLine(`Dev launch configuration failed: ${String(error)}`);
      output.appendLine("Falling back to bundled server.");
    }
  }

  // Bundled mode: look for a pre-built DLL shipped inside the extension.
  const bundledDllPath = path.join(context.extensionPath, bundledServerRelativePath);
  if (fs.existsSync(bundledDllPath)) {
    output.appendLine(`Language server launch mode: bundled`);
    output.appendLine(`Language server DLL: ${bundledDllPath}`);
    return {
      mode: "bundled",
      command: "dotnet",
      args: [bundledDllPath],
      cwd: context.extensionPath
    };
  }

  output.appendLine(`Bundled server not found at ${bundledDllPath}`);
  return undefined;
}

async function startLanguageClient(
  launchInfo: LanguageServerLaunchInfo,
  clientOptions: LanguageClientOptions,
  output: vscode.OutputChannel
): Promise<void> {
  currentLanguageServerLaunchInfo = launchInfo;
  updateLanguageServerStatusItem("starting", undefined, undefined);
  const serverOptions = createServerOptions(launchInfo);
  const nextClient = new LanguageClient(
    "preceptLanguageServer",
    "Precept Language Server",
    serverOptions,
    clientOptions
  );

  nextClient.onDidChangeState((state) => {
    output.appendLine(`Language client state: ${state.oldState} -> ${state.newState}`);
    // State 1=Starting, 2=Stopped, 3=Running
    if (state.newState === 2) {
      updateLanguageServerStatusItem("stopped", undefined, undefined);
    }
  });

  client = nextClient;

  try {
    await nextClient.start();
    const caps = nextClient.initializeResult?.capabilities ?? {};
    updateLanguageServerStatusItem("ready", undefined, caps);
  } catch (error) {
    client = undefined;
    updateLanguageServerStatusItem("error", "start failed", undefined);
    throw error;
  }
}

function createServerOptions(launchConfiguration: LanguageServerLaunchInfo): ServerOptions {
  return {
    run: {
      command: launchConfiguration.command,
      args: launchConfiguration.args,
      options: {
        cwd: launchConfiguration.cwd
      }
    },
    debug: {
      command: launchConfiguration.command,
      args: launchConfiguration.args,
      options: {
        cwd: launchConfiguration.cwd
      }
    }
  };
}

function createDevLanguageServerWatcher(
  serverWorkingDirectory: string,
  onBuildArtifactChanged: () => void,
  output: vscode.OutputChannel
): vscode.FileSystemWatcher {
  const dllPattern = new vscode.RelativePattern(
    serverWorkingDirectory,
    toGlobPath(devLanguageServerBuildDllRelativePath)
  );
  const watcher = vscode.workspace.createFileSystemWatcher(dllPattern);
  const handleBuildArtifactChanged = (uri: vscode.Uri) => {
    output.appendLine(`Dev language server build changed: ${uri.fsPath}`);
    onBuildArtifactChanged();
  };

  watcher.onDidCreate(handleBuildArtifactChanged);
  watcher.onDidChange(handleBuildArtifactChanged);
  watcher.onDidDelete(handleBuildArtifactChanged);
  return watcher;
}

function scheduleLanguageClientRestart(
  launchInfo: LanguageServerLaunchInfo,
  clientOptions: LanguageClientOptions,
  output: vscode.OutputChannel
): void {
  if (languageServerRestartTimer) {
    clearTimeout(languageServerRestartTimer);
  }

  languageServerRestartTimer = setTimeout(() => {
    languageServerRestartTimer = undefined;
    void restartLanguageClient(launchInfo, clientOptions, output);
  }, 500);
}

async function restartLanguageClient(
  launchInfo: LanguageServerLaunchInfo,
  clientOptions: LanguageClientOptions,
  output: vscode.OutputChannel
): Promise<void> {
  if (languageServerRestartInProgress) {
    languageServerRestartRequested = true;
    return;
  }

  languageServerRestartInProgress = true;
  updateLanguageServerStatusItem("restarting", undefined, undefined);

  try {
    do {
      languageServerRestartRequested = false;
      output.appendLine("Restarting language client to pick up the latest language server build.");

      const currentClient = client;
      client = undefined;

      if (currentClient) {
        await currentClient.stop();
      }

      const freshLaunchInfo = resolveDevLaunchConfiguration(
        launchInfo.projectPath!,
        launchInfo.cwd,
        output
      );

      const startPromise = startLanguageClient(freshLaunchInfo, clientOptions, output);
      try {
        await startPromise;
      } finally {
      }
    } while (languageServerRestartRequested);

    void vscode.window.setStatusBarMessage("Precept language server restarted", 3000);
  } catch (error) {
    output.appendLine(`Language client restart failed: ${String(error)}`);
    updateLanguageServerStatusItem("error", "restart failed", undefined);
  } finally {
    languageServerRestartInProgress = false;
    if (client) {
      updateLanguageServerStatusItem("ready", undefined, client?.initializeResult?.capabilities ?? {});
    }
  }
}

function toGlobPath(filePath: string): string {
  return filePath.replace(/\\/g, "/");
}

function updateLanguageServerStatusItem(
  state: "starting" | "ready" | "restarting" | "error" | "stopped",
  detail: string | undefined,
  caps: object | undefined
): void {
  if (!languageServerStatusItem) {
    return;
  }

  const modeIcon = getLanguageServerModeIcon(currentLanguageServerLaunchInfo?.mode);
  const launchLabel = getLanguageServerLaunchModeLabel(currentLanguageServerLaunchInfo?.mode);
  const obj = caps ? caps as Record<string, unknown> : undefined;
  const activeCount = obj ? Object.keys(obj).filter(k => {
    if (nonCapabilityKeys.has(k)) { return false; }
    const v = obj[k]; return v !== undefined && v !== null && v !== false;
  }).length : undefined;
  const capCountLabel = activeCount !== undefined ? ` · $(list-unordered) ${activeCount}` : "";

  switch (state) {
    case "starting":
      languageServerStatusItem.text = `$(sync~spin) Precept${modeIcon}`;
      break;
    case "restarting":
      languageServerStatusItem.text = `$(sync~spin) Precept${modeIcon}`;
      break;
    case "error":
      languageServerStatusItem.text = `$(error) Precept${modeIcon}`;
      break;
    case "stopped":
      languageServerStatusItem.text = `$(circle-slash) Precept${modeIcon}`;
      break;
    default:
      languageServerStatusItem.text = `$(pulse) Precept${modeIcon}${capCountLabel}`;
      break;
  }

  let tooltipText: string;
  switch (state) {
    case "starting":
      tooltipText = `**Precept** · Starting\u2026`;
      break;
    case "restarting":
      tooltipText = `**Precept** · Restarting\u2026`;
      break;
    case "error":
      tooltipText = `**Precept** · Error${detail ? `\n\n${detail}` : "\n\nServer did not start. Check the **Precept** output channel."}`;
      break;
    case "stopped":
      tooltipText = `**Precept** · Stopped`;
      break;
    default: {
      const capsLine = caps ? buildCapabilityTooltipLines(caps) : "";
      tooltipText = `**Precept** · \`${launchLabel.toLowerCase()}\`${capsLine ? `\n\n${capsLine}` : ""}`;
      break;
    }
  }
  languageServerStatusItem.tooltip = new vscode.MarkdownString(tooltipText);
  languageServerStatusItem.show();
}

const nonCapabilityKeys = new Set(["experimental", "workspace", "positionEncoding"]);

function buildCapabilityTooltipLines(caps: object): string {
  const obj = caps as Record<string, unknown>;
  const active: string[] = [];
  for (const key of Object.keys(obj)) {
    if (nonCapabilityKeys.has(key)) { continue; }
    const val = obj[key];
    if (val !== undefined && val !== null && val !== false) {
      active.push(key);
    }
  }
  if (active.length === 0) {
    return "No capabilities reported.";
  }
  return active.map(k => `✓ ${k}`).join(" · ");
}

function getLanguageServerLaunchModeLabel(mode: LanguageServerLaunchMode | undefined): string {
  switch (mode) {
    case "dev-build-shadow-copy":
      return "dev";
    case "bundled":
      return "bundled";
    default:
      return "?";
  }
}

function getLanguageServerModeIcon(mode: LanguageServerLaunchMode | undefined): string {
  switch (mode) {
    case "dev-build-shadow-copy":
      return " $(beaker)";
    default:
      return "";
  }
}

function buildLanguageServerLaunchInfoText(launchInfo: LanguageServerLaunchInfo): string {
  const lines = [
    `Mode: ${launchInfo.mode}`
  ];

  if (launchInfo.runtimeDllPath) {
    lines.push(`Runtime: ${launchInfo.runtimeDllPath}`);
  }

  return lines.join("\n");
}

function showLanguageServerMode(output: vscode.OutputChannel): void {
  if (!currentLanguageServerLaunchInfo) {
    void vscode.window.showInformationMessage("Precept language server launch mode has not been resolved yet.");
    return;
  }

  const message = buildLanguageServerLaunchInfoText(currentLanguageServerLaunchInfo);
  output.appendLine(`Language server details:\n${message}`);
  output.show(true);
  void vscode.window.showInformationMessage(
    `Precept language server mode: ${currentLanguageServerLaunchInfo.mode}`,
    "Open Output"
  ).then((selection) => {
    if (selection === "Open Output") {
      output.show(true);
    }
  });
}

function resolveDevLaunchConfiguration(
  projectPath: string,
  serverWorkingDirectory: string,
  output: vscode.OutputChannel
): LanguageServerLaunchInfo {
  ensureDevLanguageServerBuild(projectPath, serverWorkingDirectory, output);
  const devBuildDllPath = path.join(serverWorkingDirectory, devLanguageServerBuildDllRelativePath);

  if (!fs.existsSync(devBuildDllPath)) {
    throw new Error(`Dev language server build not found at ${devBuildDllPath}.`);
  }

  try {
    const runtimeDllPath = prepareDevLanguageServerRuntime(serverWorkingDirectory, devBuildDllPath);
    output.appendLine("Language server launch mode: dev build shadow copy");
    output.appendLine(`Language server build dll: ${devBuildDllPath}`);
    output.appendLine(`Language server runtime dll: ${runtimeDllPath}`);

    return {
      mode: "dev-build-shadow-copy",
      command: "dotnet",
      args: [runtimeDllPath],
      cwd: serverWorkingDirectory,
      projectPath,
      runtimeDllPath
    };
  } catch (error) {
    output.appendLine(`Failed to prepare dev language server runtime copy: ${String(error)}`);
    throw error;
  }
}

function ensureDevLanguageServerBuild(
  projectPath: string,
  serverWorkingDirectory: string,
  output: vscode.OutputChannel
): void {
  const devBuildRoot = path.join(serverWorkingDirectory, devLanguageServerRootRelativePath);
  const devBuildDllPath = path.join(serverWorkingDirectory, devLanguageServerBuildDllRelativePath);

  if (fs.existsSync(devBuildDllPath)) {
    return;
  }

  fs.mkdirSync(devBuildRoot, { recursive: true });
  output.appendLine("Bootstrapping dev language server build into temp/dev-language-server.");

  const build = spawnSync(
    "dotnet",
    [
      "build",
      projectPath,
      "--artifacts-path",
      devBuildRoot
    ],
    {
      cwd: serverWorkingDirectory,
      encoding: "utf8"
    }
  );

  if (build.stdout) {
    output.appendLine(build.stdout.trimEnd());
  }

  if (build.stderr) {
    output.appendLine(build.stderr.trimEnd());
  }

  if (build.status === 0 && fs.existsSync(devBuildDllPath)) {
    output.appendLine(`Bootstrapped dev language server build: ${devBuildDllPath}`);
    return;
  }

  throw new Error(`Bootstrapping dev language server build failed. Expected ${devBuildDllPath}.`);
}

function prepareDevLanguageServerRuntime(serverWorkingDirectory: string, buildDllPath: string): string {
  const buildDirectory = path.dirname(buildDllPath);
  const runtimeRoot = path.join(serverWorkingDirectory, devLanguageServerRuntimeRelativePath);
  const runtimeDirectory = path.join(runtimeRoot, `run-${Date.now()}-${++languageServerRuntimeSequence}`);

  copyDirectory(buildDirectory, runtimeDirectory);
  pruneLanguageServerRuntimeDirectories(runtimeRoot, runtimeDirectory);

  return path.join(runtimeDirectory, languageServerDllName);
}

function copyDirectory(sourceDirectory: string, targetDirectory: string): void {
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

function pruneLanguageServerRuntimeDirectories(runtimeRoot: string, activeRuntimeDirectory: string): void {
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
      // Ignore cleanup failures; the next restart can try again.
    }
  }

}

// ---------------------------------------------------------------------------
// Preview panel — scaffold for v2. Shows a placeholder until the v2 runtime
// and language server are operational.
// ---------------------------------------------------------------------------

function getPreviewPanelTitle(fileName: string): string {
  const suffix = previewFollowActiveEditor ? "" : " [Locked]";
  return `Preview ${path.basename(fileName)}${suffix}`;
}

function buildPreviewPlaceholderHtml(fileName: string): string {
  const displayName = path.basename(fileName).replace(/&/g, "&amp;").replace(/</g, "&lt;");
  return `<!doctype html>
<html>
<head><meta charset="UTF-8"></head>
<body style="background:#1e1e1e;color:#ccc;font-family:Segoe UI,Arial,sans-serif;margin:0;
             display:flex;align-items:center;justify-content:center;height:100vh;box-sizing:border-box">
  <div style="text-align:center;padding:32px">
    <div style="font-size:48px;margin-bottom:16px">&#x2697;&#xFE0F;</div>
    <h2 style="color:#A5B4FC;margin:0 0 8px">Precept Preview</h2>
    <p style="color:#9096A6;margin:0">Coming in v2 &mdash; the interactive state inspector is being rebuilt.</p>
    <p style="color:#6B7280;font-size:0.85em;margin:12px 0 0">${displayName}</p>
  </div>
</body>
</html>`;
}

function retargetPreviewPanel(panel: vscode.WebviewPanel, uri: vscode.Uri, fileName: string): void {
  currentPreviewTarget = uri;
  panel.title = getPreviewPanelTitle(fileName);
  panel.webview.html = buildPreviewPlaceholderHtml(fileName);
}

async function openPreviewPanel(): Promise<void> {
  const editor = vscode.window.activeTextEditor;
  if (!editor || editor.document.languageId !== "precept") {
    void vscode.window.showInformationMessage("Open a .precept file first to launch Preview.");
    return;
  }

  const { uri, fileName } = editor.document;

  if (currentPreviewPanel) {
    previewFollowActiveEditor = true;
    retargetPreviewPanel(currentPreviewPanel, uri, fileName);
    currentPreviewPanel.reveal(vscode.ViewColumn.Beside, true);
    return;
  }

  const panel = vscode.window.createWebviewPanel(
    "preceptPreview",
    getPreviewPanelTitle(fileName),
    vscode.ViewColumn.Beside,
    { enableScripts: false, retainContextWhenHidden: true }
  );

  currentPreviewPanel = panel;
  previewFollowActiveEditor = true;

  panel.onDidDispose(() => {
    if (currentPreviewPanel === panel) {
      currentPreviewPanel = undefined;
      currentPreviewTarget = undefined;
      previewFollowActiveEditor = true;
    }
  });

  retargetPreviewPanel(panel, uri, fileName);
}

function togglePreviewLocking(): void {
  if (!currentPreviewPanel) {
    void vscode.window.showInformationMessage("Open Preview first to toggle preview locking.");
    return;
  }

  previewFollowActiveEditor = !previewFollowActiveEditor;

  const editor = vscode.window.activeTextEditor;
  if (previewFollowActiveEditor && editor && editor.document.languageId === "precept") {
    retargetPreviewPanel(currentPreviewPanel, editor.document.uri, editor.document.fileName);
  } else if (currentPreviewTarget) {
    currentPreviewPanel.title = getPreviewPanelTitle(currentPreviewTarget.fsPath);
  }

  void vscode.window.showInformationMessage(
    previewFollowActiveEditor
      ? "Precept Preview now follows the active .precept editor."
      : "Precept Preview is now locked to the current .precept file."
  );
}