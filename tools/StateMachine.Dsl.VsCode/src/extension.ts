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
const previewPanels = new Map<string, vscode.WebviewPanel>();
const snapshotSequenceByPanel = new WeakMap<vscode.WebviewPanel, number>();

function getPreviewPanelKey(uri: vscode.Uri): string {
  return uri.toString().toLowerCase();
}

function nextSnapshotSequence(panel: vscode.WebviewPanel): number {
  const next = (snapshotSequenceByPanel.get(panel) ?? 0) + 1;
  snapshotSequenceByPanel.set(panel, next);
  return next;
}

type PreviewAction = "snapshot" | "fire" | "reset" | "replay";

interface PreviewRequest {
  action: PreviewAction;
  uri: string;
  text?: string;
  eventName?: string;
  args?: Record<string, unknown>;
  steps?: Array<{ eventName: string; args?: Record<string, unknown> }>;
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
}

type PreviewLayoutMode = "spacious" | "balanced" | "compact" | "top-down";

export async function activate(context: vscode.ExtensionContext): Promise<void> {
  const outputChannel = vscode.window.createOutputChannel("StateMachine DSL");
  context.subscriptions.push(outputChannel);

  const openPreviewDisposable = vscode.commands.registerCommand("stateMachineDsl.openInspectorPreview", () => {
    void openInspectorPreviewPanel(context, outputChannel);
  });
  context.subscriptions.push(openPreviewDisposable);

  const projectPath = resolveLanguageServerProjectPath(context, outputChannel);

  if (!projectPath) {
    outputChannel.appendLine("Language server project not found.");
    void vscode.window.showErrorMessage(
      "StateMachine DSL: could not locate tools/StateMachine.Dsl.LanguageServer/StateMachine.Dsl.LanguageServer.csproj from the current workspace."
    );
    outputChannel.show(true);
    return;
  }

  const projectDirectory = path.dirname(projectPath);
  const serverWorkingDirectory = path.resolve(projectDirectory, "..", "..");

  outputChannel.appendLine(`Language server project: ${projectPath}`);
  outputChannel.appendLine(`Language server cwd: ${serverWorkingDirectory}`);

  const serverOptions: ServerOptions = {
    run: {
      command: "dotnet",
      args: ["run", "--project", projectPath],
      options: {
        cwd: serverWorkingDirectory
      }
    },
    debug: {
      command: "dotnet",
      args: ["run", "--project", projectPath],
      options: {
        cwd: serverWorkingDirectory
      }
    }
  };

  const clientOptions: LanguageClientOptions = {
    outputChannel,
    revealOutputChannelOn: RevealOutputChannelOn.Error,
    documentSelector: [
      {
        scheme: "file",
        language: "state-machine-dsl"
      }
    ],
    synchronize: {
      fileEvents: vscode.workspace.createFileSystemWatcher("**/*.sm")
    }
  };

  client = new LanguageClient(
    "stateMachineDslLanguageServer",
    "StateMachine DSL Language Server",
    serverOptions,
    clientOptions
  );

  client.onDidChangeState((state) => {
    outputChannel.appendLine(`Language client state: ${state.oldState} -> ${state.newState}`);
  });

  context.subscriptions.push(client);
  await client.start();

  const changeSubscription = vscode.workspace.onDidChangeTextDocument((event) => {
    const panel = previewPanels.get(getPreviewPanelKey(event.document.uri));
    if (!panel) {
      return;
    }

    void sendSnapshotToPanel(panel, event.document, outputChannel);
  });
  context.subscriptions.push(changeSubscription);

  const saveSubscription = vscode.workspace.onDidSaveTextDocument((document) => {
    const panel = previewPanels.get(getPreviewPanelKey(document.uri));
    if (!panel) {
      return;
    }

    void sendSnapshotToPanel(panel, document, outputChannel);
  });
  context.subscriptions.push(saveSubscription);
}

export async function deactivate(): Promise<void> {
  for (const panel of previewPanels.values()) {
    panel.dispose();
  }

  previewPanels.clear();

  if (!client) {
    return;
  }

  await client.stop();
  client = undefined;
}

function resolveLanguageServerProjectPath(context: vscode.ExtensionContext, output: vscode.OutputChannel): string | undefined {
  const relativeProjectPath = path.join(
    "tools",
    "StateMachine.Dsl.LanguageServer",
    "StateMachine.Dsl.LanguageServer.csproj"
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
        "StateMachine.Dsl.LanguageServer",
        "StateMachine.Dsl.LanguageServer.csproj"
      )
    );
  }

  candidates.push(
    path.resolve(
      context.extensionPath,
      "..",
      "StateMachine.Dsl.LanguageServer",
      "StateMachine.Dsl.LanguageServer.csproj"
    )
  );

  for (const candidate of candidates) {
    output.appendLine(`Server project candidate: ${candidate}`);
  }

  return candidates.find((candidate) => fs.existsSync(candidate));
}

async function openInspectorPreviewPanel(context: vscode.ExtensionContext, output: vscode.OutputChannel): Promise<void> {
  const editor = vscode.window.activeTextEditor;
  if (!editor || editor.document.languageId !== "state-machine-dsl") {
    void vscode.window.showInformationMessage("Open a .sm file first to launch Inspector Preview.");
    return;
  }

  const document = editor.document;
  const panelKey = getPreviewPanelKey(document.uri);
  const existingPanel = previewPanels.get(panelKey);
  if (existingPanel) {
    existingPanel.reveal(vscode.ViewColumn.Beside, true);
    await sendSnapshotToPanel(existingPanel, document, output);
    return;
  }

  const panel = vscode.window.createWebviewPanel(
    "stateMachineDslInspectorPreview",
    `Inspector Preview: ${path.basename(document.fileName)}`,
    vscode.ViewColumn.Beside,
    {
      enableScripts: true,
      retainContextWhenHidden: true
    }
  );

  previewPanels.set(panelKey, panel);

  panel.onDidDispose(() => {
    previewPanels.delete(panelKey);
  });

  panel.onDidChangeViewState((event) => {
    if (!event.webviewPanel.visible) {
      return;
    }

    void sendSnapshotToPanel(event.webviewPanel, document, output);
  });

  panel.webview.onDidReceiveMessage(async (message) => {
    if (!message || typeof message !== "object") {
      return;
    }

    if (message.type === "ready") {
      await sendSnapshotToPanel(panel, document, output);
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

    const liveDocument = await getDocumentByUri(document.uri);
    const request: PreviewRequest = {
      action,
      uri: document.uri.toString(),
      text: liveDocument.getText(),
      eventName: typeof message.eventName === "string" ? message.eventName : undefined,
      args: typeof message.args === "object" && message.args !== null ? message.args as Record<string, unknown> : undefined,
      steps: Array.isArray(message.steps) ? message.steps : undefined
    };

    const response = await sendPreviewRequest(request, output);
    const responseWithLayout = await withLayout(response, output, request.uri);
    void panel.webview.postMessage({
      type: "previewResponse",
      requestId,
      ...responseWithLayout
    });
  });

  panel.webview.html = await getInspectorPreviewHtml(context, output, document.fileName);
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
  const responseWithLayout = await withLayout(response, output, document.uri.toString());

  void panel.webview.postMessage({
    type: "snapshot",
    snapshotSequence,
    ...responseWithLayout
  });
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
      if (!from || !to) {
        return null;
      }

      return { from, to, event };
    })
    .filter((value): value is TransitionLike => value !== null);
}

function getPreviewLayoutMode(): PreviewLayoutMode {
  const configured = vscode.workspace
    .getConfiguration("stateMachineDsl.preview")
    .get<string>("layoutMode", "balanced");

  if (configured === "spacious" || configured === "compact" || configured === "top-down" || configured === "balanced") {
    return configured;
  }

  return "balanced";
}

function getLayoutDirection(mode: PreviewLayoutMode): string {
  return mode === "top-down" ? "TB" : "LR";
}

function getLayoutSpacing(mode: PreviewLayoutMode): { ranksep: number; nodesep: number; edgesep: number } {
  switch (mode) {
    case "compact":
      return { ranksep: 60, nodesep: 35, edgesep: 15 };
    case "spacious":
      return { ranksep: 120, nodesep: 70, edgesep: 30 };
    default:
      return { ranksep: 80, nodesep: 50, edgesep: 20 };
  }
}

function computeNodeSize(stateName: string): { width: number; height: number } {
  const charWidth = 8.5;
  const padding = 36;
  return {
    width: Math.max(80, Math.round(stateName.length * charWidth + padding)),
    height: 40
  };
}

function computeLayoutForSnapshot(snapshot: Record<string, unknown>): SnapshotLayout | undefined {
  const states = extractSnapshotStates(snapshot);
  if (states.length === 0) {
    return undefined;
  }

  const transitions = extractSnapshotTransitions(snapshot);
  const layoutMode = getPreviewLayoutMode();
  const spacing = getLayoutSpacing(layoutMode);
  const direction = getLayoutDirection(layoutMode);

  const dagre = require("@dagrejs/dagre");
  const g = new dagre.graphlib.Graph({ multigraph: true });
  g.setGraph({
    rankdir: direction,
    ranksep: spacing.ranksep,
    nodesep: spacing.nodesep,
    edgesep: spacing.edgesep,
    marginx: 50,
    marginy: 50
  });
  g.setDefaultEdgeLabel(() => ({}));

  // Add nodes with measured sizes
  const nodeSizes = new Map<string, { width: number; height: number }>();
  for (const state of states) {
    const size = computeNodeSize(state);
    nodeSizes.set(state, size);
    g.setNode(state, { width: size.width, height: size.height, label: state });
  }

  // Separate self-loop transitions from regular transitions
  const regularTransitions: Array<{ transition: TransitionLike; index: number }> = [];
  const selfLoopTransitions: Array<{ transition: TransitionLike; index: number }> = [];
  for (let i = 0; i < transitions.length; i++) {
    if (transitions[i].from === transitions[i].to) {
      selfLoopTransitions.push({ transition: transitions[i], index: i });
    } else {
      regularTransitions.push({ transition: transitions[i], index: i });
    }
  }

  // Add regular edges to Dagre (it cannot handle self-loops)
  for (const { transition, index } of regularTransitions) {
    const labelWidth = Math.max(32, transition.event.length * 7 + 16);
    g.setEdge(transition.from, transition.to, {
      label: transition.event,
      width: labelWidth,
      height: 16,
      transitionIndex: index
    }, `transition-${index}`);
  }

  dagre.layout(g);

  // Extract node positions (Dagre gives center coordinates)
  const padding = 50;
  const nodes: Record<string, LayoutNode> = {};
  for (const state of states) {
    const node = g.node(state);
    nodes[state] = { x: node.x, y: node.y };
  }

  // Extract edge routing points
  const edges: LayoutEdge[] = [];
  for (const edgeObj of g.edges()) {
    const edgeData = g.edge(edgeObj);
    if (typeof edgeData.transitionIndex !== "number") {
      continue;
    }

    const points = Array.isArray(edgeData.points)
      ? edgeData.points.map((p: { x: number; y: number }) => ({ x: p.x, y: p.y }))
      : [];

    if (points.length >= 2) {
      edges.push({
        transitionIndex: edgeData.transitionIndex,
        points
      });
    }
  }

  // Generate self-loop paths
  for (const { transition, index } of selfLoopTransitions) {
    const node = g.node(transition.from);
    if (!node) {
      continue;
    }

    const halfW = node.width / 2;
    const halfH = node.height / 2;
    const loopReach = 55;
    const isHorizontal = direction === "LR";

    if (isHorizontal) {
      // Loop on the right side of the node
      edges.push({
        transitionIndex: index,
        points: [
          { x: node.x + halfW, y: node.y - halfH * 0.3 },
          { x: node.x + halfW + loopReach * 0.6, y: node.y - halfH - loopReach * 0.7 },
          { x: node.x + halfW + loopReach, y: node.y },
          { x: node.x + halfW + loopReach * 0.6, y: node.y + halfH + loopReach * 0.7 },
          { x: node.x + halfW, y: node.y + halfH * 0.3 }
        ]
      });
    } else {
      // Loop on the bottom of the node
      edges.push({
        transitionIndex: index,
        points: [
          { x: node.x + halfW * 0.3, y: node.y + halfH },
          { x: node.x + halfW + loopReach * 0.7, y: node.y + halfH + loopReach * 0.6 },
          { x: node.x, y: node.y + halfH + loopReach },
          { x: node.x - halfW - loopReach * 0.7, y: node.y + halfH + loopReach * 0.6 },
          { x: node.x - halfW * 0.3, y: node.y + halfH }
        ]
      });
    }
  }

  const graphInfo = g.graph();
  const width = Math.max(600, Number(graphInfo.width ?? 0) + padding * 2);
  const height = Math.max(300, Number(graphInfo.height ?? 0) + padding * 2);

  return { width, height, nodes, edges };
}

function withLayout(response: PreviewResponse, output: vscode.OutputChannel, _uri: string): PreviewResponse {
  if (!response.success || !response.snapshot || typeof response.snapshot !== "object") {
    return response;
  }

  try {
    const snapshot = response.snapshot as Record<string, unknown>;
    const layout = computeLayoutForSnapshot(snapshot);
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
  if (!client) {
    return {
      success: false,
      error: "Language client is not started."
    };
  }

  try {
    const response = await client.sendRequest<PreviewResponse>("stateMachine/preview/request", request);
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
  filePath: string
): Promise<string> {
  const previewTemplatePath = path.join(context.extensionPath, "webview", "inspector-preview.html");

  try {
    const rawHtml = await fs.promises.readFile(previewTemplatePath, "utf8");
    const displayFileName = escapeHtml(path.basename(filePath));

    return rawHtml.replace(/__FILE_NAME__/g, displayFileName);
  } catch (error) {
    output.appendLine(`Failed loading inspector preview HTML: ${String(error)}`);
    return `<!doctype html><html><body style=\"background:#1e1e1e;color:#fff;font-family:Segoe UI,Arial,sans-serif;padding:16px\"><h2>Inspector Preview</h2><p>Failed to load preview template at:<br>${escapeHtml(previewTemplatePath)}</p></body></html>`;
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
