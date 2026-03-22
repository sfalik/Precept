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
let clientStartPromise: Promise<void> | undefined;
const snapshotSequenceByPanel = new WeakMap<vscode.WebviewPanel, number>();
let currentPreviewPanel: vscode.WebviewPanel | undefined;
let currentPreviewDocumentUri: vscode.Uri | undefined;
let previewFollowActiveEditor = true;
let extensionContext: vscode.ExtensionContext | undefined;
let elkLayoutEngine: any | undefined;
let languageServerRestartTimer: NodeJS.Timeout | undefined;
let languageServerRestartInProgress = false;
let languageServerRestartRequested = false;
let languageServerRuntimeSequence = 0;
let languageServerStatusItem: vscode.StatusBarItem | undefined;
let currentLanguageServerLaunchInfo: LanguageServerLaunchInfo | undefined;

const languageServerDllName = "Precept.LanguageServer.dll";
const devLanguageServerRootRelativePath = path.join("temp", "dev-language-server");
const devLanguageServerBuildDllRelativePath = path.join(
  devLanguageServerRootRelativePath,
  "bin",
  "Precept.LanguageServer",
  "debug",
  languageServerDllName
);
const devLanguageServerRuntimeRelativePath = path.join(devLanguageServerRootRelativePath, "runtime");

type LanguageServerLaunchMode = "dev-build-shadow-copy";

interface LanguageServerLaunchInfo {
  mode: LanguageServerLaunchMode;
  command: string;
  args: string[];
  cwd: string;
  buildDllPath?: string;
  runtimeDllPath?: string;
}

function nextSnapshotSequence(panel: vscode.WebviewPanel): number {
  const next = (snapshotSequenceByPanel.get(panel) ?? 0) + 1;
  snapshotSequenceByPanel.set(panel, next);
  return next;
}

type PreviewAction = "snapshot" | "fire" | "reset" | "replay" | "inspect" | "inspectUpdate" | "update";

interface PreviewRequest {
  action: PreviewAction;
  uri: string;
  text?: string;
  eventName?: string;
  args?: Record<string, unknown>;
  steps?: Array<{ eventName: string; args?: Record<string, unknown> }>;
  fieldUpdates?: Record<string, unknown>;
}

interface PreviewResponse {
  success: boolean;
  error?: string;
  snapshot?: unknown;
  replayMessages?: string[];
}

interface LayoutNode {
  x: number;
  y: number;
  width: number;
  height: number;
}

interface LayoutEdge {
  transitionIndex: number;
  points: Array<{ x: number; y: number }>;
}

interface SnapshotLayout {
  width: number;
  height: number;
  nodes: Record<string, LayoutNode>;
  edges: LayoutEdge[];
}

interface TransitionLike {
  from: string;
  to: string;
  event: string;
  kind: string;
}

type PreviewLayoutMode = "spacious" | "balanced" | "compact" | "orthogonal" | "top-down";

interface PreviewTarget {
  uri: vscode.Uri;
  fileName: string;
}

export async function activate(context: vscode.ExtensionContext): Promise<void> {
  extensionContext = context;
  const outputChannel = vscode.window.createOutputChannel("Precept");
  context.subscriptions.push(outputChannel);

  languageServerStatusItem = vscode.window.createStatusBarItem(vscode.StatusBarAlignment.Left, 100);
  languageServerStatusItem.name = "Precept Language Server";
  languageServerStatusItem.command = "precept.showLanguageServerMode";
  context.subscriptions.push(languageServerStatusItem);
  updateLanguageServerStatusItem("starting");
  languageServerStatusItem.show();

  const openPreviewDisposable = vscode.commands.registerCommand("precept.openPreview", () => {
    void openInspectorPreviewPanel(context, outputChannel);
  });
  context.subscriptions.push(openPreviewDisposable);

  const togglePreviewLockingDisposable = vscode.commands.registerCommand("precept.togglePreviewLocking", () => {
    void togglePreviewLocking(outputChannel);
  });
  context.subscriptions.push(togglePreviewLockingDisposable);

  const showLanguageServerModeDisposable = vscode.commands.registerCommand("precept.showLanguageServerMode", () => {
    showLanguageServerMode(outputChannel);
  });
  context.subscriptions.push(showLanguageServerModeDisposable);

  const projectPath = resolveLanguageServerProjectPath(context, outputChannel);

  if (!projectPath) {
    updateLanguageServerStatusItem("error", "project not found");
    outputChannel.appendLine("Language server project not found.");
    void vscode.window.showErrorMessage(
      "Precept: could not locate tools/Precept.LanguageServer/Precept.LanguageServer.csproj from the current workspace."
    );
    outputChannel.show(true);
    return;
  }

  const projectDirectory = path.dirname(projectPath);
  const serverWorkingDirectory = path.resolve(projectDirectory, "..", "..");

  outputChannel.appendLine(`Language server project: ${projectPath}`);
  outputChannel.appendLine(`Language server cwd: ${serverWorkingDirectory}`);

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
    await ensureLanguageClientStarted(projectPath, serverWorkingDirectory, clientOptions, outputChannel);
  } catch (error) {
    updateLanguageServerStatusItem("error", "startup failed");
    outputChannel.appendLine(`Language client failed to start: ${String(error)}`);
    void vscode.window.showErrorMessage("Precept: failed to start the language server. See the Precept output channel for details.");
    outputChannel.show(true);
    return;
  }

  const devLanguageServerWatcher = createDevLanguageServerWatcher(
    serverWorkingDirectory,
    () => {
      scheduleLanguageClientRestart(projectPath, serverWorkingDirectory, clientOptions, outputChannel);
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

  const changeSubscription = vscode.workspace.onDidChangeTextDocument((event) => {
    if (!currentPreviewPanel || !currentPreviewDocumentUri) {
      return;
    }

    if (event.document.uri.toString() !== currentPreviewDocumentUri.toString()) {
      return;
    }

    void sendSnapshotToPanel(currentPreviewPanel, event.document, outputChannel);
  });
  context.subscriptions.push(changeSubscription);

  const saveSubscription = vscode.workspace.onDidSaveTextDocument((document) => {
    if (!currentPreviewPanel || !currentPreviewDocumentUri) {
      return;
    }

    if (document.uri.toString() !== currentPreviewDocumentUri.toString()) {
      return;
    }

    void sendSnapshotToPanel(currentPreviewPanel, document, outputChannel);
  });
  context.subscriptions.push(saveSubscription);

  const activeEditorSubscription = vscode.window.onDidChangeActiveTextEditor((editor) => {
    if (!previewFollowActiveEditor || !currentPreviewPanel) {
      return;
    }

    const target = getPreviewTargetFromEditor(editor);
    if (!target) {
      return;
    }

    void retargetPreviewPanel(currentPreviewPanel, target, context, outputChannel, false);
  });
  context.subscriptions.push(activeEditorSubscription);

  registerMcpServerProvider(context);
}

export async function deactivate(): Promise<void> {
  if (currentPreviewPanel) {
    currentPreviewPanel.dispose();
  }

  currentPreviewPanel = undefined;
  currentPreviewDocumentUri = undefined;
  previewFollowActiveEditor = true;
  extensionContext = undefined;

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

async function ensureLanguageClientStarted(
  projectPath: string,
  serverWorkingDirectory: string,
  clientOptions: LanguageClientOptions,
  output: vscode.OutputChannel
): Promise<void> {
  if (client) {
    return;
  }

  if (clientStartPromise) {
    await clientStartPromise;
    return;
  }

  clientStartPromise = startLanguageClient(projectPath, serverWorkingDirectory, clientOptions, output);
  try {
    await clientStartPromise;
  } finally {
    clientStartPromise = undefined;
  }
}

async function startLanguageClient(
  projectPath: string,
  serverWorkingDirectory: string,
  clientOptions: LanguageClientOptions,
  output: vscode.OutputChannel
): Promise<void> {
  const launchConfiguration = resolveLanguageServerLaunchConfiguration(projectPath, serverWorkingDirectory, output);
  currentLanguageServerLaunchInfo = launchConfiguration;
  updateLanguageServerStatusItem("starting");
  const serverOptions = createServerOptions(launchConfiguration);
  const nextClient = new LanguageClient(
    "preceptLanguageServer",
    "Precept Language Server",
    serverOptions,
    clientOptions
  );

  nextClient.onDidChangeState((state) => {
    output.appendLine(`Language client state: ${state.oldState} -> ${state.newState}`);
  });

  client = nextClient;

  try {
    await nextClient.start();
    updateLanguageServerStatusItem("ready");
  } catch (error) {
    client = undefined;
    updateLanguageServerStatusItem("error", "start failed");
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
  projectPath: string,
  serverWorkingDirectory: string,
  clientOptions: LanguageClientOptions,
  output: vscode.OutputChannel
): void {
  if (languageServerRestartTimer) {
    clearTimeout(languageServerRestartTimer);
  }

  languageServerRestartTimer = setTimeout(() => {
    languageServerRestartTimer = undefined;
    void restartLanguageClient(projectPath, serverWorkingDirectory, clientOptions, output);
  }, 500);
}

async function restartLanguageClient(
  projectPath: string,
  serverWorkingDirectory: string,
  clientOptions: LanguageClientOptions,
  output: vscode.OutputChannel
): Promise<void> {
  if (languageServerRestartInProgress) {
    languageServerRestartRequested = true;
    return;
  }

  languageServerRestartInProgress = true;
  updateLanguageServerStatusItem("restarting");

  try {
    do {
      languageServerRestartRequested = false;
      output.appendLine("Restarting language client to pick up the latest language server build.");

      const currentClient = client;
      client = undefined;

      if (currentClient) {
        await currentClient.stop();
      }

      clientStartPromise = startLanguageClient(projectPath, serverWorkingDirectory, clientOptions, output);
      try {
        await clientStartPromise;
      } finally {
        clientStartPromise = undefined;
      }
    } while (languageServerRestartRequested);

    void vscode.window.setStatusBarMessage("Precept language server restarted", 3000);
  } catch (error) {
    output.appendLine(`Language client restart failed: ${String(error)}`);
    updateLanguageServerStatusItem("error", "restart failed");
  } finally {
    languageServerRestartInProgress = false;
    if (client) {
      updateLanguageServerStatusItem("ready");
    }
  }
}

function toGlobPath(filePath: string): string {
  return filePath.replace(/\\/g, "/");
}

function updateLanguageServerStatusItem(
  state: "starting" | "ready" | "restarting" | "error",
  detail?: string
): void {
  if (!languageServerStatusItem) {
    return;
  }

  const launchLabel = getLanguageServerLaunchModeLabel(currentLanguageServerLaunchInfo?.mode);
  switch (state) {
    case "starting":
      languageServerStatusItem.text = `$(sync~spin) Precept LS: ${launchLabel}`;
      break;
    case "restarting":
      languageServerStatusItem.text = `$(sync~spin) Precept LS: ${launchLabel}`;
      break;
    case "error":
      languageServerStatusItem.text = `$(error) Precept LS: ${launchLabel}`;
      break;
    default:
      languageServerStatusItem.text = `$(server-process) Precept LS: ${launchLabel}`;
      break;
  }

  const detailLine = detail ? `Status: ${detail}` : `Status: ${state}`;
  const launchInfo = currentLanguageServerLaunchInfo
    ? buildLanguageServerLaunchInfoText(currentLanguageServerLaunchInfo)
    : "Launch mode not resolved yet.";
  languageServerStatusItem.tooltip = `Precept language server\n${detailLine}\n${launchInfo}\nClick for full launch details.`;
  languageServerStatusItem.show();
}

function getLanguageServerLaunchModeLabel(mode: LanguageServerLaunchMode | undefined): string {
  switch (mode) {
    case "dev-build-shadow-copy":
      return "Dev";
    default:
      return "?";
  }
}

function buildLanguageServerLaunchInfoText(launchInfo: LanguageServerLaunchInfo): string {
  const lines = [
    `Mode: ${launchInfo.mode}`,
    `Command: ${launchInfo.command} ${launchInfo.args.join(" ")}`,
    `Working directory: ${launchInfo.cwd}`
  ];

  if (launchInfo.buildDllPath) {
    lines.push(`Dev build DLL: ${launchInfo.buildDllPath}`);
  }

  if (launchInfo.runtimeDllPath) {
    lines.push(`Runtime DLL: ${launchInfo.runtimeDllPath}`);
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

function resolveLanguageServerLaunchConfiguration(
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
      buildDllPath: devBuildDllPath,
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

async function openInspectorPreviewPanel(context: vscode.ExtensionContext, output: vscode.OutputChannel): Promise<void> {
  const target = getPreviewTargetFromEditor(vscode.window.activeTextEditor);
  if (!target) {
    void vscode.window.showInformationMessage("Open a .precept file first to launch Preview.");
    return;
  }

  if (currentPreviewPanel) {
    previewFollowActiveEditor = true;
    currentPreviewPanel.reveal(vscode.ViewColumn.Beside, true);
    await retargetPreviewPanel(currentPreviewPanel, target, context, output, true);
    return;
  }

  const panel = vscode.window.createWebviewPanel(
    "preceptPreview",
    getPreviewPanelTitle(target.fileName),
    vscode.ViewColumn.Beside,
    {
      enableScripts: true,
      retainContextWhenHidden: true
    }
  );

  currentPreviewPanel = panel;
  previewFollowActiveEditor = true;

  panel.onDidDispose(() => {
    if (currentPreviewPanel === panel) {
      currentPreviewPanel = undefined;
      currentPreviewDocumentUri = undefined;
      previewFollowActiveEditor = true;
    }
  });

  panel.onDidChangeViewState((event) => {
    if (!event.webviewPanel.visible) {
      return;
    }

    const previewUri = currentPreviewDocumentUri;
    if (!previewUri) {
      return;
    }

    void getDocumentByUri(previewUri)
      .then((document) => sendSnapshotToPanel(event.webviewPanel, document, output))
      .catch((error) => {
        output.appendLine(`Preview refresh failed: ${String(error)}`);
      });
  });

  panel.webview.onDidReceiveMessage(async (message) => {
    if (!message || typeof message !== "object") {
      return;
    }

    if (message.type === "ready") {
      if (!currentPreviewDocumentUri) {
        return;
      }

      const readyDocument = await getDocumentByUri(currentPreviewDocumentUri);
      await sendSnapshotToPanel(panel, readyDocument, output);
      return;
    }

    if (message.type !== "previewRequest") {
      return;
    }

    const requestId = typeof message.requestId === "number" ? message.requestId : undefined;
    const action = message.action as PreviewAction | undefined;
    if (typeof requestId === "undefined" || !action) {
      return;
    }

    const previewUri = currentPreviewDocumentUri;
    if (!previewUri) {
      return;
    }

    const liveDocument = await getDocumentByUri(previewUri);
    const request: PreviewRequest = {
      action,
      uri: liveDocument.uri.toString(),
      text: liveDocument.getText(),
      eventName: typeof message.eventName === "string" ? message.eventName : undefined,
      args: typeof message.args === "object" && message.args !== null ? message.args as Record<string, unknown> : undefined,
      steps: Array.isArray(message.steps) ? message.steps : undefined,
      fieldUpdates: typeof message.fieldUpdates === "object" && message.fieldUpdates !== null ? message.fieldUpdates as Record<string, unknown> : undefined
    };

    const response = await sendPreviewRequest(request, output);
    const responseWithLayout = await withLayout(response, output);
    void panel.webview.postMessage({
      type: "previewResponse",
      requestId,
      ...responseWithLayout
    });
  });

  await retargetPreviewPanel(panel, target, context, output, true);
}

function getPreviewTargetFromEditor(editor: vscode.TextEditor | undefined): PreviewTarget | undefined {
  if (!editor || editor.document.languageId !== "precept") {
    return undefined;
  }

  return {
    uri: editor.document.uri,
    fileName: editor.document.fileName
  };
}

function isSamePreviewTarget(left: vscode.Uri | undefined, right: vscode.Uri): boolean {
  return !!left && left.toString() === right.toString();
}

function getPreviewPanelTitle(fileName: string): string {
  const suffix = previewFollowActiveEditor ? "" : " [Locked]";
  return `Preview ${path.basename(fileName)}${suffix}`;
}

async function retargetPreviewPanel(
  panel: vscode.WebviewPanel,
  target: PreviewTarget,
  context: vscode.ExtensionContext,
  output: vscode.OutputChannel,
  resetWebviewState: boolean
): Promise<void> {
  const targetChanged = !isSamePreviewTarget(currentPreviewDocumentUri, target.uri);
  currentPreviewDocumentUri = target.uri;
  panel.title = getPreviewPanelTitle(target.fileName);

  if (targetChanged || resetWebviewState) {
    panel.webview.html = await getInspectorPreviewHtml(context, output, target.fileName, previewFollowActiveEditor);
    return;
  }

  const document = await getDocumentByUri(target.uri);
  await sendSnapshotToPanel(panel, document, output);
  void postSourceInfoToPanel(panel, target.fileName);
}

function postSourceInfoToPanel(panel: vscode.WebviewPanel, fileName: string): Thenable<boolean> {
  return panel.webview.postMessage({
    type: "sourceInfo",
    fileName: path.basename(fileName),
    locked: !previewFollowActiveEditor
  });
}

async function togglePreviewLocking(output: vscode.OutputChannel): Promise<void> {
  if (!currentPreviewPanel || !currentPreviewDocumentUri) {
    void vscode.window.showInformationMessage("Open Preview first to toggle preview locking.");
    return;
  }

  previewFollowActiveEditor = !previewFollowActiveEditor;

  if (previewFollowActiveEditor) {
    const activeTarget = getPreviewTargetFromEditor(vscode.window.activeTextEditor);
    if (activeTarget && !isSamePreviewTarget(currentPreviewDocumentUri, activeTarget.uri) && extensionContext) {
      await retargetPreviewPanel(currentPreviewPanel, activeTarget, extensionContext, output, true);
      return;
    }
  }

  try {
    const document = await getDocumentByUri(currentPreviewDocumentUri);
    currentPreviewPanel.title = getPreviewPanelTitle(document.fileName);
    void postSourceInfoToPanel(currentPreviewPanel, document.fileName);
  } catch (error) {
    output.appendLine(`Preview lock toggle failed: ${String(error)}`);
  }

  const message = previewFollowActiveEditor
    ? "Precept Preview now follows the active .precept editor."
    : "Precept Preview is now locked to the current .precept file.";
  void vscode.window.showInformationMessage(message);
}

async function sendSnapshotToPanel(
  panel: vscode.WebviewPanel,
  document: vscode.TextDocument,
  output: vscode.OutputChannel
): Promise<void> {
  const snapshotSequence = nextSnapshotSequence(panel);
  const response = await sendPreviewRequest(
    {
      action: "snapshot",
      uri: document.uri.toString(),
      text: document.getText()
    },
    output
  );
  const responseWithLayout = await withLayout(response, output);

  void panel.webview.postMessage({
    type: "snapshot",
    snapshotSequence,
    ...responseWithLayout
  });
}

function getElkEngine(): any {
  if (elkLayoutEngine) {
    return elkLayoutEngine;
  }

  const Elk = require("elkjs/lib/elk.bundled.js");
  elkLayoutEngine = new Elk();
  return elkLayoutEngine;
}

function extractSnapshotStates(snapshot: Record<string, unknown>): string[] {
  const rawStates = (snapshot.states ?? snapshot.States) as unknown;
  if (!Array.isArray(rawStates)) {
    return [];
  }

  return rawStates
    .map((item) => String(item ?? ""))
    .filter((item) => item.length > 0);
}

function extractSnapshotTransitions(snapshot: Record<string, unknown>): TransitionLike[] {
  const rawTransitions = (snapshot.transitions ?? snapshot.Transitions) as unknown;
  if (!Array.isArray(rawTransitions)) {
    return [];
  }

  return rawTransitions
    .map((transition) => {
      if (!transition || typeof transition !== "object") {
        return null;
      }

      const value = transition as Record<string, unknown>;
      const from = String(value.from ?? value.From ?? "");
      const to = String(value.to ?? value.To ?? "");
      const event = String(value.event ?? value.Event ?? "");
      const kind = String(value.kind ?? value.Kind ?? "transition");
      if (!from || !to) {
        return null;
      }

      return { from, to, event, kind };
    })
    .filter((value): value is TransitionLike => value !== null);
}

function getPreviewLayoutMode(): PreviewLayoutMode {
  const configured = vscode.workspace
    .getConfiguration("precept.preview")
    .get<string>("layoutMode", "balanced");

  if (configured === "spacious" || configured === "compact" || configured === "orthogonal" || configured === "top-down" || configured === "balanced") {
    return configured;
  }

  return "balanced";
}

function getElkLayoutOptions(mode: PreviewLayoutMode): Record<string, string> {
  const direction = mode === "top-down" ? "DOWN" : "DOWN";
  const nodeSpacing = mode === "compact" ? "6" : mode === "spacious" ? "12" : "7";
  const layerSpacing = mode === "compact" ? "10" : mode === "spacious" ? "20" : "15";

  return {
    "elk.algorithm": "layered",
    "elk.direction": direction,
    "elk.spacing.nodeNode": nodeSpacing,
    "elk.layered.spacing.nodeNodeBetweenLayers": layerSpacing,
    "elk.layered.spacing.edgeNodeBetweenLayers": "21",
    "elk.layered.crossingMinimization.strategy": "LAYER_SWEEP",
    "elk.layered.nodePlacement.strategy": "NETWORK_SIMPLEX",
    "elk.edgeRouting": "ORTHOGONAL",
    "elk.layered.mergeEdges": "false",
    "elk.layered.feedbackEdges": "true",
    "elk.separateConnectedComponents": "false",
    "elk.layered.cycleBreaking.strategy": "MODEL_ORDER",
    "elk.insideSelfLoops.activate": "true",
    "elk.edgeLabels.inline": "true",
    "elk.layered.considerModelOrder.strategy": "NODES_AND_EDGES"
  };
}

function computeNodeSize(stateName: string): { width: number; height: number } {
  const charWidth = 8.5;
  const horizontalPadding = 36;
  const width = Math.max(80, Math.round(stateName.length * charWidth + horizontalPadding));
  const height = 44;
  return { width, height };
}

async function computeLayoutForSnapshot(snapshot: Record<string, unknown>): Promise<SnapshotLayout | undefined> {
  const states = extractSnapshotStates(snapshot);
  if (states.length === 0) {
    return undefined;
  }

  const transitions = extractSnapshotTransitions(snapshot);
  const nodeSizes = new Map(states.map((state) => [state, computeNodeSize(state)]));

  const elk = getElkEngine();
  const layoutMode = getPreviewLayoutMode();
  const layoutInput = {
    id: "root",
    layoutOptions: getElkLayoutOptions(layoutMode),
    children: states.map((state) => ({
      id: state,
      width: nodeSizes.get(state)?.width ?? 80,
      height: nodeSizes.get(state)?.height ?? 44
    })),
    edges: transitions
      .map((transition, originalIndex) => ({ transition, originalIndex }))
      .filter(({ transition }) => transition.kind === "transition")
      .map(({ transition, originalIndex }) => ({
      id: `transition-${originalIndex}`,
      sources: [transition.from],
      targets: [transition.to],
      labels: transition.event
        ? [{
          id: `label-${originalIndex}`,
          text: transition.event,
          width: Math.max(32, transition.event.length * 7),
          height: 12
        }]
        : []
    }))
  };

  const layoutResult = await elk.layout(layoutInput);
  const resultChildren = Array.isArray(layoutResult.children) ? layoutResult.children : [];
  const resultEdges = Array.isArray(layoutResult.edges) ? layoutResult.edges : [];

  const padding = 14;
  const nodes: Record<string, LayoutNode> = {};
  for (const child of resultChildren) {
    if (!child?.id) {
      continue;
    }

    const size = nodeSizes.get(String(child.id)) ?? { width: 80, height: 44 };
    const x = Number(child.x ?? 0) + (size.width / 2) + padding;
    const y = Number(child.y ?? 0) + (size.height / 2) + padding;
    nodes[String(child.id)] = { x, y, width: size.width, height: size.height };
  }

  const edges: LayoutEdge[] = [];
  for (const edge of resultEdges) {
    const id = String(edge?.id ?? "");
    const match = /^transition-(\d+)$/.exec(id);
    if (!match) {
      continue;
    }

    const transitionIndex = Number(match[1]);
    const sections = Array.isArray(edge.sections) ? edge.sections : [];
    const points: Array<{ x: number; y: number }> = [];

    if (sections.length > 0) {
      for (const section of sections) {
        if (section.startPoint) {
          points.push({ x: Number(section.startPoint.x) + padding, y: Number(section.startPoint.y) + padding });
        }

        for (const bendPoint of section.bendPoints ?? []) {
          points.push({ x: Number(bendPoint.x) + padding, y: Number(bendPoint.y) + padding });
        }

        if (section.endPoint) {
          points.push({ x: Number(section.endPoint.x) + padding, y: Number(section.endPoint.y) + padding });
        }
      }
    }

    if (points.length >= 2) {
      edges.push({ transitionIndex, points });
    }
  }

  const width = Number(layoutResult.width ?? 0) + padding * 2;
  const height = Number(layoutResult.height ?? 0) + padding * 2;

  return { width, height, nodes, edges };
}

async function withLayout(response: PreviewResponse, output: vscode.OutputChannel): Promise<PreviewResponse> {
  if (!response.success || !response.snapshot || typeof response.snapshot !== "object") {
    return response;
  }

  try {
    const snapshot = response.snapshot as Record<string, unknown>;
    const layout = await computeLayoutForSnapshot(snapshot);
    if (!layout) {
      return response;
    }

    return {
      ...response,
      snapshot: {
        ...snapshot,
        layout
      }
    };
  } catch (error) {
    output.appendLine(`Preview layout failed: ${String(error)}`);
    return response;
  }
}

async function sendPreviewRequest(request: PreviewRequest, output: vscode.OutputChannel): Promise<PreviewResponse> {
  if (clientStartPromise) {
    try {
      await clientStartPromise;
    } catch {
      // The response below will surface the restart failure.
    }
  }

  if (!client) {
    return {
      success: false,
      error: "Language client is not started."
    };
  }

  try {
    const response = await client.sendRequest<PreviewResponse>("precept/preview/request", request);
    return response;
  } catch (error) {
    output.appendLine(`Preview request failed (${request.action}): ${String(error)}`);
    return {
      success: false,
      error: String(error)
    };
  }
}

async function getDocumentByUri(uri: vscode.Uri): Promise<vscode.TextDocument> {
  const open = vscode.workspace.textDocuments.find((doc) => doc.uri.toString() === uri.toString());
  if (open) {
    return open;
  }

  return vscode.workspace.openTextDocument(uri);
}

async function getInspectorPreviewHtml(
  context: vscode.ExtensionContext,
  output: vscode.OutputChannel,
  filePath: string,
  isDynamicPreview: boolean
): Promise<string> {
  const previewTemplatePath = path.join(context.extensionPath, "webview", "inspector-preview.html");

  try {
    const rawHtml = await fs.promises.readFile(previewTemplatePath, "utf8");
    const displayFileName = escapeHtml(path.basename(filePath));
    const previewModeLabel = escapeHtml(isDynamicPreview ? "Following active editor" : "Locked to current file");

    return rawHtml
      .replace(/__FILE_NAME__/g, displayFileName)
      .replace(/__PREVIEW_MODE__/g, previewModeLabel);
  } catch (error) {
    output.appendLine(`Failed loading Preview HTML: ${String(error)}`);
    return `<!doctype html><html><body style=\"background:#1e1e1e;color:#fff;font-family:Segoe UI,Arial,sans-serif;padding:16px\"><h2>Preview</h2><p>Failed to load preview template at:<br>${escapeHtml(previewTemplatePath)}</p></body></html>`;
  }
}

function escapeHtml(value: string): string {
  return value
    .replace(/&/g, "&amp;")
    .replace(/</g, "&lt;")
    .replace(/>/g, "&gt;")
    .replace(/\"/g, "&quot;")
    .replace(/'/g, "&#39;");
}

function registerMcpServerProvider(context: vscode.ExtensionContext): void {
  if (typeof vscode.lm?.registerMcpServerDefinitionProvider !== "function") {
    return;
  }

  const provider = vscode.lm.registerMcpServerDefinitionProvider(
    "precept.mcpServer",
    {
      provideMcpServerDefinitions: async () => {
        const serverPath = resolveBundledMcpServerPath(context);
        if (!serverPath) {
          return [];
        }

        return [
          new vscode.McpStdioServerDefinition(
            "Precept",
            serverPath,
            [],
            {},
            context.extension.packageJSON.version
          )
        ];
      }
    }
  );

  context.subscriptions.push(provider);
}

function resolveBundledMcpServerPath(context: vscode.ExtensionContext): string | undefined {
  const rid = getPlatformRid();
  if (!rid) {
    return undefined;
  }

  const executable = process.platform === "win32" ? "Precept.Mcp.exe" : "Precept.Mcp";
  const serverPath = path.join(context.extensionPath, "mcp-server", rid, executable);

  if (fs.existsSync(serverPath)) {
    return serverPath;
  }

  return undefined;
}

function getPlatformRid(): string | undefined {
  const platform = process.platform;
  const arch = process.arch;

  if (platform === "win32" && arch === "x64") { return "win-x64"; }
  if (platform === "linux" && arch === "x64") { return "linux-x64"; }
  if (platform === "darwin" && arch === "arm64") { return "osx-arm64"; }
  if (platform === "darwin" && arch === "x64") { return "osx-x64"; }

  return undefined;
}
