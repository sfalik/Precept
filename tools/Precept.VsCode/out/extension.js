"use strict";
var __createBinding = (this && this.__createBinding) || (Object.create ? (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    var desc = Object.getOwnPropertyDescriptor(m, k);
    if (!desc || ("get" in desc ? !m.__esModule : desc.writable || desc.configurable)) {
      desc = { enumerable: true, get: function() { return m[k]; } };
    }
    Object.defineProperty(o, k2, desc);
}) : (function(o, m, k, k2) {
    if (k2 === undefined) k2 = k;
    o[k2] = m[k];
}));
var __setModuleDefault = (this && this.__setModuleDefault) || (Object.create ? (function(o, v) {
    Object.defineProperty(o, "default", { enumerable: true, value: v });
}) : function(o, v) {
    o["default"] = v;
});
var __importStar = (this && this.__importStar) || (function () {
    var ownKeys = function(o) {
        ownKeys = Object.getOwnPropertyNames || function (o) {
            var ar = [];
            for (var k in o) if (Object.prototype.hasOwnProperty.call(o, k)) ar[ar.length] = k;
            return ar;
        };
        return ownKeys(o);
    };
    return function (mod) {
        if (mod && mod.__esModule) return mod;
        var result = {};
        if (mod != null) for (var k = ownKeys(mod), i = 0; i < k.length; i++) if (k[i] !== "default") __createBinding(result, mod, k[i]);
        __setModuleDefault(result, mod);
        return result;
    };
})();
Object.defineProperty(exports, "__esModule", { value: true });
exports.activate = activate;
exports.deactivate = deactivate;
const fs = __importStar(require("fs"));
const path = __importStar(require("path"));
const vscode = __importStar(require("vscode"));
const node_1 = require("vscode-languageclient/node");
let client;
const previewPanels = new Map();
const snapshotSequenceByPanel = new WeakMap();
let elkLayoutEngine;
function getPreviewPanelKey(uri) {
    return uri.toString().toLowerCase();
}
function nextSnapshotSequence(panel) {
    const next = (snapshotSequenceByPanel.get(panel) ?? 0) + 1;
    snapshotSequenceByPanel.set(panel, next);
    return next;
}
async function activate(context) {
    const outputChannel = vscode.window.createOutputChannel("StateMachine DSL");
    context.subscriptions.push(outputChannel);
    const openPreviewDisposable = vscode.commands.registerCommand("stateMachineDsl.openInspectorPreview", () => {
        void openInspectorPreviewPanel(context, outputChannel);
    });
    context.subscriptions.push(openPreviewDisposable);
    const projectPath = resolveLanguageServerProjectPath(context, outputChannel);
    if (!projectPath) {
        outputChannel.appendLine("Language server project not found.");
        void vscode.window.showErrorMessage("StateMachine DSL: could not locate tools/StateMachine.Dsl.LanguageServer/StateMachine.Dsl.LanguageServer.csproj from the current workspace.");
        outputChannel.show(true);
        return;
    }
    const projectDirectory = path.dirname(projectPath);
    const serverWorkingDirectory = path.resolve(projectDirectory, "..", "..");
    outputChannel.appendLine(`Language server project: ${projectPath}`);
    outputChannel.appendLine(`Language server cwd: ${serverWorkingDirectory}`);
    const serverOptions = {
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
    const clientOptions = {
        outputChannel,
        revealOutputChannelOn: node_1.RevealOutputChannelOn.Error,
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
    client = new node_1.LanguageClient("stateMachineDslLanguageServer", "StateMachine DSL Language Server", serverOptions, clientOptions);
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
async function deactivate() {
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
function resolveLanguageServerProjectPath(context, output) {
    const relativeProjectPath = path.join("tools", "StateMachine.Dsl.LanguageServer", "StateMachine.Dsl.LanguageServer.csproj");
    const workspaceFolders = vscode.workspace.workspaceFolders ?? [];
    const candidates = [];
    for (const folder of workspaceFolders) {
        const workspaceRoot = folder.uri.fsPath;
        candidates.push(path.join(workspaceRoot, relativeProjectPath));
        candidates.push(path.resolve(workspaceRoot, "..", "StateMachine.Dsl.LanguageServer", "StateMachine.Dsl.LanguageServer.csproj"));
    }
    candidates.push(path.resolve(context.extensionPath, "..", "StateMachine.Dsl.LanguageServer", "StateMachine.Dsl.LanguageServer.csproj"));
    for (const candidate of candidates) {
        output.appendLine(`Server project candidate: ${candidate}`);
    }
    return candidates.find((candidate) => fs.existsSync(candidate));
}
async function openInspectorPreviewPanel(context, output) {
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
    const panel = vscode.window.createWebviewPanel("stateMachineDslInspectorPreview", `Preview ${path.basename(document.fileName)}`, vscode.ViewColumn.Beside, {
        enableScripts: true,
        retainContextWhenHidden: true
    });
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
        const action = message.action;
        if (typeof requestId === "undefined" || !action) {
            return;
        }
        const liveDocument = await getDocumentByUri(document.uri);
        const request = {
            action,
            uri: document.uri.toString(),
            text: liveDocument.getText(),
            eventName: typeof message.eventName === "string" ? message.eventName : undefined,
            args: typeof message.args === "object" && message.args !== null ? message.args : undefined,
            steps: Array.isArray(message.steps) ? message.steps : undefined
        };
        const response = await sendPreviewRequest(request, output);
        const responseWithLayout = await withLayout(response, output);
        void panel.webview.postMessage({
            type: "previewResponse",
            requestId,
            ...responseWithLayout
        });
    });
    panel.webview.html = await getInspectorPreviewHtml(context, output, document.fileName);
}
async function sendSnapshotToPanel(panel, document, output) {
    const snapshotSequence = nextSnapshotSequence(panel);
    const response = await sendPreviewRequest({
        action: "snapshot",
        uri: document.uri.toString(),
        text: document.getText()
    }, output);
    const responseWithLayout = await withLayout(response, output);
    void panel.webview.postMessage({
        type: "snapshot",
        snapshotSequence,
        ...responseWithLayout
    });
}
function getElkEngine() {
    if (elkLayoutEngine) {
        return elkLayoutEngine;
    }
    const Elk = require("elkjs/lib/elk.bundled.js");
    elkLayoutEngine = new Elk();
    return elkLayoutEngine;
}
function extractSnapshotStates(snapshot) {
    const rawStates = (snapshot.states ?? snapshot.States);
    if (!Array.isArray(rawStates)) {
        return [];
    }
    return rawStates
        .map((item) => String(item ?? ""))
        .filter((item) => item.length > 0);
}
function extractSnapshotTransitions(snapshot) {
    const rawTransitions = (snapshot.transitions ?? snapshot.Transitions);
    if (!Array.isArray(rawTransitions)) {
        return [];
    }
    return rawTransitions
        .map((transition) => {
        if (!transition || typeof transition !== "object") {
            return null;
        }
        const value = transition;
        const from = String(value.from ?? value.From ?? "");
        const to = String(value.to ?? value.To ?? "");
        const event = String(value.event ?? value.Event ?? "");
        const kind = String(value.kind ?? value.Kind ?? "transition");
        if (!from || !to) {
            return null;
        }
        return { from, to, event, kind };
    })
        .filter((value) => value !== null);
}
function getPreviewLayoutMode() {
    const configured = vscode.workspace
        .getConfiguration("stateMachineDsl.preview")
        .get("layoutMode", "balanced");
    if (configured === "spacious" || configured === "compact" || configured === "orthogonal" || configured === "top-down" || configured === "balanced") {
        return configured;
    }
    return "balanced";
}
function getElkLayoutOptions(mode) {
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
function computeNodeSize(stateName) {
    const charWidth = 8.5;
    const horizontalPadding = 36;
    const width = Math.max(80, Math.round(stateName.length * charWidth + horizontalPadding));
    const height = 44;
    return { width, height };
}
async function computeLayoutForSnapshot(snapshot) {
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
    const nodes = {};
    for (const child of resultChildren) {
        if (!child?.id) {
            continue;
        }
        const size = nodeSizes.get(String(child.id)) ?? { width: 80, height: 44 };
        const x = Number(child.x ?? 0) + (size.width / 2) + padding;
        const y = Number(child.y ?? 0) + (size.height / 2) + padding;
        nodes[String(child.id)] = { x, y, width: size.width, height: size.height };
    }
    const edges = [];
    for (const edge of resultEdges) {
        const id = String(edge?.id ?? "");
        const match = /^transition-(\d+)$/.exec(id);
        if (!match) {
            continue;
        }
        const transitionIndex = Number(match[1]);
        const sections = Array.isArray(edge.sections) ? edge.sections : [];
        const points = [];
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
async function withLayout(response, output) {
    if (!response.success || !response.snapshot || typeof response.snapshot !== "object") {
        return response;
    }
    try {
        const snapshot = response.snapshot;
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
    }
    catch (error) {
        output.appendLine(`Preview layout failed: ${String(error)}`);
        return response;
    }
}
async function sendPreviewRequest(request, output) {
    if (!client) {
        return {
            success: false,
            error: "Language client is not started."
        };
    }
    try {
        const response = await client.sendRequest("stateMachine/preview/request", request);
        return response;
    }
    catch (error) {
        output.appendLine(`Preview request failed (${request.action}): ${String(error)}`);
        return {
            success: false,
            error: String(error)
        };
    }
}
async function getDocumentByUri(uri) {
    const open = vscode.workspace.textDocuments.find((doc) => doc.uri.toString() === uri.toString());
    if (open) {
        return open;
    }
    return vscode.workspace.openTextDocument(uri);
}
async function getInspectorPreviewHtml(context, output, filePath) {
    const previewTemplatePath = path.join(context.extensionPath, "webview", "inspector-preview.html");
    try {
        const rawHtml = await fs.promises.readFile(previewTemplatePath, "utf8");
        const displayFileName = escapeHtml(path.basename(filePath));
        return rawHtml.replace(/__FILE_NAME__/g, displayFileName);
    }
    catch (error) {
        output.appendLine(`Failed loading inspector preview HTML: ${String(error)}`);
        return `<!doctype html><html><body style=\"background:#1e1e1e;color:#fff;font-family:Segoe UI,Arial,sans-serif;padding:16px\"><h2>Inspector Preview</h2><p>Failed to load preview template at:<br>${escapeHtml(previewTemplatePath)}</p></body></html>`;
    }
}
function escapeHtml(value) {
    return value
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/\"/g, "&quot;")
        .replace(/'/g, "&#39;");
}
//# sourceMappingURL=extension.js.map