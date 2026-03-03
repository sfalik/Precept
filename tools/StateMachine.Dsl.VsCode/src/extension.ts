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
let elkLayoutEngine: any | undefined;
const layoutStabilityByUri = new Map<string, SnapshotLayout>();

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

type PreviewLayoutMode = "spacious" | "balanced" | "compact" | "orthogonal" | "top-down";

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
    layoutStabilityByUri.delete(document.uri.toString());
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

  if (configured === "spacious" || configured === "compact" || configured === "orthogonal" || configured === "top-down" || configured === "balanced") {
    return configured;
  }

  return "balanced";
}

function getElkLayoutOptions(mode: PreviewLayoutMode): Record<string, string> {
  if (mode === "spacious") {
    return {
      "elk.algorithm": "layered",
      "elk.direction": "RIGHT",
      "elk.spacing.nodeNode": "96",
      "elk.layered.spacing.nodeNodeBetweenLayers": "184",
      "elk.layered.spacing.edgeNodeBetweenLayers": "56",
      "elk.layered.crossingMinimization.strategy": "LAYER_SWEEP",
      "elk.layered.nodePlacement.strategy": "BRANDES_KOEPF",
      "elk.edgeRouting": "ORTHOGONAL"
    };
  }

  if (mode === "compact") {
    return {
      "elk.algorithm": "layered",
      "elk.direction": "RIGHT",
      "elk.spacing.nodeNode": "42",
      "elk.layered.spacing.nodeNodeBetweenLayers": "78",
      "elk.layered.crossingMinimization.strategy": "LAYER_SWEEP",
      "elk.layered.nodePlacement.strategy": "NETWORK_SIMPLEX",
      "elk.edgeRouting": "SPLINES"
    };
  }

  if (mode === "orthogonal") {
    return {
      "elk.algorithm": "layered",
      "elk.direction": "RIGHT",
      "elk.spacing.nodeNode": "62",
      "elk.layered.spacing.nodeNodeBetweenLayers": "112",
      "elk.layered.crossingMinimization.strategy": "LAYER_SWEEP",
      "elk.layered.nodePlacement.strategy": "BRANDES_KOEPF",
      "elk.edgeRouting": "ORTHOGONAL"
    };
  }

  if (mode === "top-down") {
    return {
      "elk.algorithm": "layered",
      "elk.direction": "DOWN",
      "elk.spacing.nodeNode": "70",
      "elk.layered.spacing.nodeNodeBetweenLayers": "116",
      "elk.layered.crossingMinimization.strategy": "LAYER_SWEEP",
      "elk.layered.nodePlacement.strategy": "BRANDES_KOEPF",
      "elk.edgeRouting": "SPLINES"
    };
  }

  return {
    "elk.algorithm": "layered",
    "elk.direction": "DOWN",
    "elk.spacing.nodeNode": "64",
    "elk.layered.spacing.nodeNodeBetweenLayers": "108",
    "elk.layered.crossingMinimization.strategy": "LAYER_SWEEP",
    "elk.layered.nodePlacement.strategy": "BRANDES_KOEPF",
    "elk.edgeRouting": "ORTHOGONAL"
  };
}

function offsetPolyline(points: Array<{ x: number; y: number }>, offset: number): Array<{ x: number; y: number }> {
  if (Math.abs(offset) < 0.001 || points.length < 2) {
    return points;
  }

  const start = points[0];
  const end = points[points.length - 1];
  const dx = end.x - start.x;
  const dy = end.y - start.y;
  const length = Math.hypot(dx, dy);
  if (length < 0.001) {
    return points;
  }

  const normalX = -dy / length;
  const normalY = dx / length;

  if (points.length === 2) {
    const midX = (start.x + end.x) / 2 + (normalX * offset);
    const midY = (start.y + end.y) / 2 + (normalY * offset);
    return [start, { x: midX, y: midY }, end];
  }

  return points.map((point, index) => {
    if (index === 0 || index === points.length - 1) {
      return point;
    }

    return {
      x: point.x + (normalX * offset),
      y: point.y + (normalY * offset)
    };
  });
}

function deconflictParallelEdges(edges: LayoutEdge[], transitions: TransitionLike[]): LayoutEdge[] {
  const grouped = new Map<string, LayoutEdge[]>();
  for (const edge of edges) {
    const transition = transitions[edge.transitionIndex];
    if (!transition) {
      continue;
    }

    const key = `${transition.from}->${transition.to}`;
    const group = grouped.get(key) ?? [];
    group.push(edge);
    grouped.set(key, group);
  }

  const deconflicted = new Map<number, LayoutEdge>();
  for (const group of grouped.values()) {
    if (group.length === 1) {
      deconflicted.set(group[0].transitionIndex, group[0]);
      continue;
    }

    const sorted = [...group].sort((left, right) => left.transitionIndex - right.transitionIndex);
    sorted.forEach((edge, index) => {
      const centered = index - ((sorted.length - 1) / 2);
      const laneOffset = centered * 14;
      deconflicted.set(edge.transitionIndex, {
        transitionIndex: edge.transitionIndex,
        points: offsetPolyline(edge.points, laneOffset)
      });
    });
  }

  return edges.map((edge) => deconflicted.get(edge.transitionIndex) ?? edge);
}

function deconflictFanEdges(edges: LayoutEdge[], transitions: TransitionLike[]): LayoutEdge[] {
  const byTarget = new Map<string, LayoutEdge[]>();
  const bySource = new Map<string, LayoutEdge[]>();

  for (const edge of edges) {
    const transition = transitions[edge.transitionIndex];
    if (!transition) {
      continue;
    }

    const targetGroup = byTarget.get(transition.to) ?? [];
    targetGroup.push(edge);
    byTarget.set(transition.to, targetGroup);

    const sourceGroup = bySource.get(transition.from) ?? [];
    sourceGroup.push(edge);
    bySource.set(transition.from, sourceGroup);
  }

  const adjustments = new Map<number, number>();
  const applySpread = (group: LayoutEdge[], magnitude: number, weight: number) => {
    if (group.length <= 2) {
      return;
    }

    const sorted = [...group].sort((left, right) => left.transitionIndex - right.transitionIndex);
    sorted.forEach((edge, index) => {
      const centered = index - ((sorted.length - 1) / 2);
      const existing = adjustments.get(edge.transitionIndex) ?? 0;
      adjustments.set(edge.transitionIndex, existing + (centered * magnitude * weight));
    });
  };

  for (const group of byTarget.values()) {
    applySpread(group, 11, 1);
  }

  for (const group of bySource.values()) {
    applySpread(group, 8, 0.7);
  }

  return edges.map((edge) => {
    const offset = adjustments.get(edge.transitionIndex) ?? 0;
    if (Math.abs(offset) < 0.001) {
      return edge;
    }

    return {
      transitionIndex: edge.transitionIndex,
      points: offsetPolyline(edge.points, offset)
    };
  });
}

function routeThroughTargetIngressBand(points: Array<{ x: number; y: number }>, bandOffset: number): Array<{ x: number; y: number }> {
  if (Math.abs(bandOffset) < 0.001 || points.length < 2) {
    return points;
  }

  const end = points[points.length - 1];
  const previous = points[points.length - 2];
  const dx = end.x - previous.x;
  const dy = end.y - previous.y;
  const length = Math.hypot(dx, dy);
  if (length < 0.001) {
    return points;
  }

  const ux = dx / length;
  const uy = dy / length;
  const normalX = -uy;
  const normalY = ux;
  const ingressDistance = Math.max(28, Math.min(54, length * 0.62));
  const ingressPoint = {
    x: end.x - (ux * ingressDistance) + (normalX * bandOffset),
    y: end.y - (uy * ingressDistance) + (normalY * bandOffset)
  };

  return [
    ...points.slice(0, -1),
    ingressPoint,
    end
  ];
}

function applyTargetIngressBands(edges: LayoutEdge[], transitions: TransitionLike[]): LayoutEdge[] {
  const grouped = new Map<string, LayoutEdge[]>();

  for (const edge of edges) {
    const transition = transitions[edge.transitionIndex];
    if (!transition || transition.from === transition.to) {
      continue;
    }

    const key = `${transition.to}::${transition.event}`;
    const group = grouped.get(key) ?? [];
    group.push(edge);
    grouped.set(key, group);
  }

  const rerouted = new Map<number, LayoutEdge>();
  for (const group of grouped.values()) {
    if (group.length < 3) {
      continue;
    }

    const sorted = [...group].sort((left, right) => {
      const leftTransition = transitions[left.transitionIndex];
      const rightTransition = transitions[right.transitionIndex];
      const byFrom = leftTransition.from.localeCompare(rightTransition.from);
      if (byFrom !== 0) {
        return byFrom;
      }

      return left.transitionIndex - right.transitionIndex;
    });

    sorted.forEach((edge, index) => {
      const centered = index - ((sorted.length - 1) / 2);
      const bandOffset = centered * 18;
      rerouted.set(edge.transitionIndex, {
        transitionIndex: edge.transitionIndex,
        points: routeThroughTargetIngressBand(edge.points, bandOffset)
      });
    });
  }

  return edges.map((edge) => rerouted.get(edge.transitionIndex) ?? edge);
}

function stabilizeLayout(uri: string, layout: SnapshotLayout): SnapshotLayout {
  const previous = layoutStabilityByUri.get(uri);
  if (!previous) {
    layoutStabilityByUri.set(uri, layout);
    return layout;
  }

  const nodes: Record<string, LayoutNode> = {};
  for (const [name, node] of Object.entries(layout.nodes)) {
    const prior = previous.nodes[name];
    if (!prior) {
      nodes[name] = node;
      continue;
    }

    nodes[name] = {
      x: (node.x * 0.78) + (prior.x * 0.22),
      y: (node.y * 0.78) + (prior.y * 0.22)
    };
  }

  const priorEdgeMap = new Map(previous.edges.map((edge) => [edge.transitionIndex, edge]));
  const edges = layout.edges.map((edge) => {
    const prior = priorEdgeMap.get(edge.transitionIndex);
    if (!prior || prior.points.length !== edge.points.length) {
      return edge;
    }

    return {
      transitionIndex: edge.transitionIndex,
      points: edge.points.map((point, index) => ({
        x: (point.x * 0.82) + (prior.points[index].x * 0.18),
        y: (point.y * 0.82) + (prior.points[index].y * 0.18)
      }))
    };
  });

  const stabilized: SnapshotLayout = {
    width: (layout.width * 0.88) + (previous.width * 0.12),
    height: (layout.height * 0.88) + (previous.height * 0.12),
    nodes,
    edges
  };

  layoutStabilityByUri.set(uri, stabilized);
  return stabilized;
}

function normalizeLayoutBounds(layout: SnapshotLayout): SnapshotLayout {
  const points: Array<{ x: number; y: number }> = [];
  for (const node of Object.values(layout.nodes)) {
    points.push({ x: node.x, y: node.y });
  }

  for (const edge of layout.edges) {
    for (const point of edge.points) {
      points.push({ x: point.x, y: point.y });
    }
  }

  if (points.length === 0) {
    return layout;
  }

  const minX = points.reduce((value, point) => Math.min(value, point.x), Number.POSITIVE_INFINITY);
  const maxX = points.reduce((value, point) => Math.max(value, point.x), Number.NEGATIVE_INFINITY);
  const minY = points.reduce((value, point) => Math.min(value, point.y), Number.POSITIVE_INFINITY);
  const maxY = points.reduce((value, point) => Math.max(value, point.y), Number.NEGATIVE_INFINITY);

  const baseSpanX = Math.max(260, (maxX - minX) + 140);
  const baseSpanY = Math.max(180, (maxY - minY) + 90);
  const targetWidth = 900;
  const targetHeight = 740;
  const fitScale = Math.min(targetWidth / baseSpanX, targetHeight / baseSpanY);
  const scale = Math.max(0.78, Math.min(1.12, fitScale));
  const paddingX = 56;
  const paddingY = 64;

  const transformPoint = (point: { x: number; y: number }) => ({
    x: ((point.x - minX) * scale) + paddingX,
    y: ((point.y - minY) * scale) + paddingY
  });

  const nodes: Record<string, LayoutNode> = {};
  for (const [name, node] of Object.entries(layout.nodes)) {
    nodes[name] = transformPoint(node);
  }

  const edges: LayoutEdge[] = layout.edges.map((edge) => ({
    transitionIndex: edge.transitionIndex,
    points: edge.points.map(transformPoint)
  }));

  const width = Math.max(700, (baseSpanX * scale) + (paddingX * 2));
  const height = Math.max(420, (baseSpanY * scale) + (paddingY * 2));

  return {
    width,
    height,
    nodes,
    edges
  };
}

interface SnapshotLayoutPair {
  raw: SnapshotLayout;
  optimized: SnapshotLayout;
}

function hashName(value: string): number {
  let hash = 0;
  for (let index = 0; index < value.length; index += 1) {
    hash = ((hash << 5) - hash) + value.charCodeAt(index);
    hash |= 0;
  }

  return Math.abs(hash);
}

function createOrganicOptimizedLayout(rawLayout: SnapshotLayout, transitions: TransitionLike[]): SnapshotLayout {
  const nodeNames = Object.keys(rawLayout.nodes);
  if (nodeNames.length <= 1) {
    return rawLayout;
  }

  const positions: Record<string, { x: number; y: number }> = {};
  for (const name of nodeNames) {
    const base = rawLayout.nodes[name];
    const hash = hashName(name);
    positions[name] = {
      x: base.x + (((hash % 15) - 7) * 3),
      y: base.y + ((((Math.floor(hash / 15)) % 15) - 7) * 3)
    };
  }

  const rawXs = nodeNames.map((name) => rawLayout.nodes[name].x);
  const rawYs = nodeNames.map((name) => rawLayout.nodes[name].y);
  const spanX = Math.max(160, Math.max(...rawXs) - Math.min(...rawXs));
  const spanY = Math.max(160, Math.max(...rawYs) - Math.min(...rawYs));
  const area = spanX * spanY;
  const k = Math.sqrt(area / Math.max(1, nodeNames.length));
  const iterations = 92;

  for (let iteration = 0; iteration < iterations; iteration += 1) {
    const cooling = 1 - (iteration / iterations);
    const displacements = new Map(nodeNames.map((name) => [name, { x: 0, y: 0 }]));

    for (let leftIndex = 0; leftIndex < nodeNames.length; leftIndex += 1) {
      const leftName = nodeNames[leftIndex];
      const left = positions[leftName];

      for (let rightIndex = leftIndex + 1; rightIndex < nodeNames.length; rightIndex += 1) {
        const rightName = nodeNames[rightIndex];
        const right = positions[rightName];
        let dx = left.x - right.x;
        let dy = left.y - right.y;
        let distance = Math.hypot(dx, dy);
        if (distance < 1) {
          distance = 1;
          dx = 1;
          dy = 0;
        }

        const force = (k * k) / distance;
        const fx = (dx / distance) * force;
        const fy = (dy / distance) * force;

        const leftDisp = displacements.get(leftName)!;
        const rightDisp = displacements.get(rightName)!;
        leftDisp.x += fx;
        leftDisp.y += fy;
        rightDisp.x -= fx;
        rightDisp.y -= fy;
      }
    }

    for (const transition of transitions) {
      const source = positions[transition.from];
      const target = positions[transition.to];
      if (!source || !target) {
        continue;
      }

      let dx = source.x - target.x;
      let dy = source.y - target.y;
      let distance = Math.hypot(dx, dy);
      if (distance < 1) {
        distance = 1;
      }

      const force = (distance * distance) / Math.max(1, k * 2.4);
      const fx = (dx / distance) * force;
      const fy = (dy / distance) * force;

      const sourceDisp = displacements.get(transition.from)!;
      const targetDisp = displacements.get(transition.to)!;
      sourceDisp.x -= fx;
      sourceDisp.y -= fy;
      targetDisp.x += fx;
      targetDisp.y += fy;
    }

    for (const name of nodeNames) {
      const displacement = displacements.get(name)!;
      const magnitude = Math.hypot(displacement.x, displacement.y);
      const stepLimit = Math.max(3, k * 0.24 * cooling);
      if (magnitude > 0) {
        const scale = Math.min(stepLimit, magnitude) / magnitude;
        positions[name].x += displacement.x * scale;
        positions[name].y += displacement.y * scale;
      }

      const anchor = rawLayout.nodes[name];
      positions[name].x += (anchor.x - positions[name].x) * 0.055;
      positions[name].y += (anchor.y - positions[name].y) * 0.055;
    }
  }

  return normalizeLayoutBounds({
    width: rawLayout.width,
    height: rawLayout.height,
    nodes: positions,
    edges: []
  });
}

async function computeLayoutForSnapshot(snapshot: Record<string, unknown>): Promise<SnapshotLayoutPair | undefined> {
  const states = extractSnapshotStates(snapshot);
  if (states.length === 0) {
    return undefined;
  }

  const transitions = extractSnapshotTransitions(snapshot);
  const nodeSize = new Map(states.map((state) => [state, {
    width: Math.max(112, Math.min(200, 68 + (state.length * 8))),
    height: 44
  }]));

  const elk = getElkEngine();
  const layoutMode = getPreviewLayoutMode();
  const layoutInput = {
    id: "root",
    layoutOptions: getElkLayoutOptions(layoutMode),
    children: states.map((state) => ({
      id: state,
      width: nodeSize.get(state)?.width ?? 120,
      height: nodeSize.get(state)?.height ?? 44
    })),
    edges: transitions.map((transition, index) => ({
      id: `transition-${index}`,
      sources: [transition.from],
      targets: [transition.to],
      labels: transition.event
        ? [{
          id: `label-${index}`,
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

  const nodes: Record<string, LayoutNode> = {};
  for (const child of resultChildren) {
    if (!child?.id) {
      continue;
    }

    const width = Number(child.width ?? nodeSize.get(String(child.id))?.width ?? 120);
    const height = Number(child.height ?? nodeSize.get(String(child.id))?.height ?? 44);
    const x = Number(child.x ?? 0) + (width / 2) + 40;
    const y = Number(child.y ?? 0) + (height / 2) + 40;
    nodes[String(child.id)] = { x, y };
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
          points.push({ x: Number(section.startPoint.x) + 40, y: Number(section.startPoint.y) + 40 });
        }

        for (const bendPoint of section.bendPoints ?? []) {
          points.push({ x: Number(bendPoint.x) + 40, y: Number(bendPoint.y) + 40 });
        }

        if (section.endPoint) {
          points.push({ x: Number(section.endPoint.x) + 40, y: Number(section.endPoint.y) + 40 });
        }
      }
    }

    if (points.length >= 2) {
      edges.push({ transitionIndex, points });
    }
  }

  const width = Math.max(920, Number(layoutResult.width ?? 0) + 120);
  const height = Math.max(430, Number(layoutResult.height ?? 0) + 120);
  const rawLayout: SnapshotLayout = {
    width,
    height,
    nodes,
    edges
  };

  const optimizedLayout = createOrganicOptimizedLayout(rawLayout, transitions);

  return {
    raw: rawLayout,
    optimized: optimizedLayout
  };
}

async function withLayout(response: PreviewResponse, output: vscode.OutputChannel, uri: string): Promise<PreviewResponse> {
  if (!response.success || !response.snapshot || typeof response.snapshot !== "object") {
    return response;
  }

  try {
    const snapshot = response.snapshot as Record<string, unknown>;
    const layout = await computeLayoutForSnapshot(snapshot);
    if (!layout) {
      return response;
    }
    const stabilizedLayout = stabilizeLayout(uri, layout.optimized);

    return {
      ...response,
      snapshot: {
        ...snapshot,
        layoutRaw: layout.raw,
        layout: stabilizedLayout
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
