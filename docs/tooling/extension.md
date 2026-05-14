# VS Code Extension

## Status

| Property | Value |
|---|---|
| Doc maturity | Stub |
| Implementation state | Substantially implemented — LS client lifecycle (dev-build-shadow-copy and bundled modes), status bar item, three commands (`precept.openPreview`, `precept.togglePreviewLocking`, `precept.showLanguageServerMode`), dev file watcher with auto-restart, and preview panel scaffold (placeholder UI — full inspector content pending LS v2) |
| Source | `tools/Precept.VsCode/` |
| Upstream | Language server, MCP server, TextMate grammar (generated) |
| Downstream | VS Code UI (editor, problems panel, commands) |

---

## Contents

- [Overview](#overview)
- [Responsibilities and Boundaries](#responsibilities-and-boundaries)
- [Right-Sizing](#right-sizing)
- [Inputs and Outputs](#inputs-and-outputs)
- [Orchestration Architecture](#orchestration-architecture)
- [VS Code Integration Points](#vs-code-integration-points)
  - [Grammar Registration](#grammar-registration)
  - [LS Client Lifecycle](#ls-client-lifecycle)
  - [Language Server Status Bar](#language-server-status-bar)
  - [MCP Server Launch](#mcp-server-launch)
  - [Commands](#commands)
  - [Preview Webview](#preview-webview)
- [Dependencies and Integration Points](#dependencies-and-integration-points)
- [Failure Modes and Recovery](#failure-modes-and-recovery)
- [Contracts and Guarantees](#contracts-and-guarantees)
- [Design Rationale and Decisions](#design-rationale-and-decisions)
- [Innovation](#innovation)
- [Open Questions / Implementation Notes](#open-questions-implementation-notes)
- [Deliberate Exclusions](#deliberate-exclusions)
- [Cross-References](#cross-references)
- [Source Files](#source-files)

## Overview

The VS Code extension hosts the language server, launches the MCP server, provides the TextMate grammar for syntax highlighting, and surfaces Precept-specific commands and the preview webview panel. It is the thin host shell — all intelligence comes from the language server and compiler.

The extension activates when a workspace already contains `.precept` files and when you open a `.precept` document directly. That keeps the language-server status item available in single-file and no-workspace editor sessions instead of only in repo-style workspaces.

---

## Responsibilities and Boundaries

**OWNS:** Extension activation/deactivation, LS client lifecycle, MCP server launch, TextMate grammar registration, command registration, webview hosting.

**Does NOT OWN:** LSP protocol handling (LS server), grammar generation (build tool), MCP logic (MCP server), compiler/runtime logic.

---

## Right-Sizing

The extension is a host shell. It wires surfaces together and handles VS Code lifecycle events. It adds no language intelligence. If extension code begins to reason about Precept syntax or semantics, that logic has drifted out of place and belongs in the compiler or LS.

---

## Inputs and Outputs

**Input:** VS Code extension API events (activate, deactivate, commands)

**Output:** Language client (LSP), MCP server process, registered commands, webview panels, status bar items

---

## Orchestration Architecture

Three activation responsibilities:

1. **Grammar registration:** The TextMate grammar (`precept.tmLanguage.json`) is declared in `package.json` and registered by VS Code automatically at activation — no extension code needed.
2. **LS client:** At activation, start the LS process and create an LSP client that forwards editor events to the server.
3. **MCP server launch:** Launch `tools/scripts/start-precept-mcp.js` as a child process via the MCP configuration in `.vscode/mcp.json`.

---

## VS Code Integration Points

### Grammar Registration

Declared in `package.json` as a `grammars` contribution. VS Code reads `precept.tmLanguage.json` directly. Since the grammar is a generated file (from catalog metadata), it cannot drift from the language.

### LS Client Lifecycle

The extension creates a `LanguageClient` pointing at the LS executable. The client handles reconnection on server crashes. All LSP requests/responses are forwarded transparently — the extension does not intercept or modify protocol messages.

### Language Server Status Bar

The extension shows a left-aligned `Precept` status bar item as soon as activation begins. The item stays visible while the language server is starting, ready, restarting, stopped, or in an error state, and clicking it runs `precept.showLanguageServerMode` so the user can inspect the current launch mode in the Precept output channel.

### MCP Server Launch

The MCP server is launched as a separate process via the workspace-local `.vscode/mcp.json` configuration. This configuration points to `tools/scripts/start-precept-mcp.js`, which builds and starts the MCP server from source — enabling inner-loop development without a published package.

### Commands

Three commands are registered:

- `precept.openPreview` (`Precept: Open Preview`) — opens the preview webview panel beside the active `.precept` editor; re-reveals and retargets if already open
- `precept.togglePreviewLocking` (`Precept: Toggle Preview Locking`) — locks the preview to the current file or resumes following the active editor; updates the panel title with `[Locked]` indicator
- `precept.showLanguageServerMode` (`Precept: Show Language Server Mode`) — logs and shows the current launch mode (dev-build-shadow-copy or bundled) in the Precept output channel

> **Note:** `Precept: Compile` is not implemented. Diagnostics are pushed automatically on every document change by the language server — no manual compile command is needed.

### Preview Webview

The preview panel is implemented as a scaffold: the `precept.openPreview` command opens a webview beside the active `.precept` editor. Currently it displays a placeholder UI ("Coming in v2 — the interactive state inspector is being rebuilt") while the LS v2 inspector is under development.

**Implemented behaviors:**
- Opens `vscode.ViewColumn.Beside` preserving focus
- Follows active `.precept` editor when unlocked (`onDidChangeActiveTextEditor` subscription)
- Lock/unlock via `precept.togglePreviewLocking`; title shows `[Locked]` when locked
- Panel title shows the current file name
- Panel state cleaned up on dispose

**Pending:** actual state diagram + inspection content driven by LS `precept/inspect`.

---

## Dependencies and Integration Points

- **Language server** (`tools/Precept.LanguageServer/`): the extension launches this as an LSP server process
- **MCP server** (`tools/Precept.Mcp/`): launched via `.vscode/mcp.json`
- **TextMate grammar** (`precept.tmLanguage.json`): registered at activation from `tools/Precept.VsCode/syntaxes/`
- **VS Code extension API** (upstream): activation events, command registration, webview API

---

## Failure Modes and Recovery

If the LS crashes, the `LanguageClient` retries with exponential backoff. If the MCP server fails to start, VS Code shows a notification — the extension continues running without MCP features. Grammar activation failure (malformed JSON) surfaces as a VS Code error; the extension still activates.

---

## Contracts and Guarantees

- Extension activation never throws an unhandled exception to VS Code.
- The LS client is restarted automatically on crash.
- Grammar registration is declarative — no runtime grammar loading.

---

## Design Rationale and Decisions

The extension is a thin host shell by design. All value comes from the LS, compiler, and generated grammar. Keeping extension code minimal means language intelligence is portable — any LSP-capable editor can consume the LS directly.

---

## Innovation

- **Grammar from catalog:** The TextMate grammar the extension registers is a generated artifact — not hand-authored — so it cannot drift from the language.
- **Source-first MCP config:** Workspace-local `.vscode/mcp.json` points to the source-built MCP server, enabling inner-loop development without a published package.

---

## Open Questions / Implementation Notes

1. Preview webview full content (state diagram + inspection) pending LS v2 `precept/inspect` integration.
2. Confirm MCP server launch is correctly configured in `.vscode/mcp.json` and works with inner-loop development.

---

## Deliberate Exclusions

- **No language intelligence:** The extension is a shell. Language knowledge lives in the LS and compiler.
- **No grammar hand-editing:** `tmLanguage.json` is always regenerated from catalogs.

---

## Cross-References

| Topic | Document |
|---|---|
| Language server the extension hosts | `docs/tooling/language-server.md` |
| MCP server the extension launches | `docs/tooling/mcp.md` |
| Grammar generation and semantic tokens | `docs/compiler/tooling-surface.md` |
| Grammar generation design | `docs/compiler-and-runtime-design.md §13` |

---

## Source Files

| File | Purpose |
|---|---|
| `tools/Precept.VsCode/` | All VS Code extension source files |
| `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` | Generated grammar artifact — do not hand-edit |
