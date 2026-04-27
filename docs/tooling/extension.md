# VS Code Extension

## Status

| Property | Value |
|---|---|
| Doc maturity | Stub |
| Implementation state | Bootstrap only (extension loads, grammar activates, MCP server launches; no custom commands or webview implemented) |
| Source | `tools/Precept.VsCode/` |
| Upstream | Language server, MCP server, TextMate grammar (generated) |
| Downstream | VS Code UI (editor, problems panel, commands) |

---

## Overview

The VS Code extension hosts the language server, launches the MCP server, provides the TextMate grammar for syntax highlighting, and will surface Precept-specific commands and the preview webview. It is the thin host shell — all intelligence comes from the language server and compiler.

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

## Architecture

Three activation responsibilities:

1. **Grammar registration:** The TextMate grammar (`precept.tmLanguage.json`) is declared in `package.json` and registered by VS Code automatically at activation — no extension code needed.
2. **LS client:** At activation, start the LS process and create an LSP client that forwards editor events to the server.
3. **MCP server launch:** Launch `tools/scripts/start-precept-mcp.js` as a child process via the MCP configuration in `.vscode/mcp.json`.

---

## Component Mechanics

### Grammar Registration

Declared in `package.json` as a `grammars` contribution. VS Code reads `precept.tmLanguage.json` directly. Since the grammar is a generated file (from catalog metadata), it cannot drift from the language.

### LS Client Lifecycle

The extension creates a `LanguageClient` pointing at the LS executable. The client handles reconnection on server crashes. All LSP requests/responses are forwarded transparently — the extension does not intercept or modify protocol messages.

### MCP Server Launch

The MCP server is launched as a separate process via the workspace-local `.vscode/mcp.json` configuration. This configuration points to `tools/scripts/start-precept-mcp.js`, which builds and starts the MCP server from source — enabling inner-loop development without a published package.

### Commands

Planned commands:
- `Precept: Compile` — force recompile and surface diagnostics
- `Precept: Preview state machine` — open the preview webview for the active `.precept` file

### Preview Webview

Planned: a webview panel that renders the state diagram and shows inspection results for the current document. Layout: state machine diagram on the left, field/constraint details on the right. Inspection driven by the LS preview/inspect API.

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

1. Extension bootstrap only — LS client, commands, and webview not yet implemented.
2. Implement LS client activation first (connects extension to language server, surfaces diagnostics).
3. Preview webview: define the state-diagram + inspection visualization before implementing.
4. Commands: confirm the command list — at minimum "Precept: Compile" and "Precept: Preview state machine".
5. Confirm MCP server launch is correctly configured in `.vscode/mcp.json` and works with inner-loop development.

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
