# Precept Artifact Operating Model

Date: 2026-04-06
Status: Current standard

---

## Purpose

This document defines the operating model for how Precept artifacts are developed locally versus how they are validated in their shipped or distribution form.

The standard is intentionally **source-first** and **workspace-owned**:

- local development runs from source in the current workspace
- the current workspace owns the dev-time configuration that wires those source artifacts together
- packaged, installed, or distribution-shaped artifacts are used only for explicit validation and release checks

This is not a proposal. It describes the model already implied by the repo's current structure, tasks, launchers, and packaging scripts.

---

## Policy

1. Daily development is source-first. Edit and run from `src/`, `tools/`, `.vscode/`, and `temp/` in the current workspace rather than from packaged outputs.
2. Workspace-owned dev config stays in workspace-owned files. Local registration, overrides, and launch indirection live in `.vscode/`, not in shipped plugin or extension payloads.
3. Packaged or installed artifacts are for explicit validation only. They are used to verify the shipped shape, not as the normal inner loop.
4. Worktrees are first-class. Each worktree owns its own `temp/` outputs and source checkout. `.vscode/` and `temp/` must be worktree-local directories, not shared folders and not symlinks or junctions into another worktree. Do not share dev runtime directories across worktrees.
5. No artifact may require build-output file locking in the local loop. Running processes must execute from shadow-copied runtime directories or packaged install locations, never from the directories being rebuilt.
6. Local changes must become visible immediately after the documented refresh step for that artifact class. If a path requires extra manual copying, path rewriting, or stale-file cleanup, it is the wrong local-development path.

---

## Terminology

- **Source-first**: the executable or loaded artifact comes from the current repo contents, not from a packaged or published distribution.
- **Workspace-owned**: the active config lives under the current checkout, typically in `.vscode/`, and applies only to that workspace or worktree.
- **Distribution validation**: an explicit check that uses the packaged or installed shape that end users receive.
- **Shadow-copy runtime**: a launch model that copies build output into a separate runtime directory before starting the process so rebuilds do not fight file locks.
- **Artifact class**: one of the major deliverable groups in this repo: MCP server, plugin metadata/content, VS Code extension, language server, grammar/preview assets, and generated build outputs.

---

## Operating Model

### Default Mode: Local Development

Local development always starts from source in the current workspace.

For the MCP path, the workspace override in `.vscode/mcp.json` uses the VS Code `servers` schema and points the `precept` server at `tools/scripts/start-precept-mcp.js`, not at the plugin's shipped `.mcp.json`. The launcher builds `tools/Precept.Mcp/Precept.Mcp.csproj` into `temp/dev-mcp/`, copies the chosen build output into `temp/dev-mcp/runtime/run-*`, and runs the copy. That keeps the local loop source-first and rebuild-safe.

For the editor path, the default build task in `.vscode/tasks.json` builds `tools/Precept.LanguageServer/Precept.LanguageServer.csproj` into `temp/dev-language-server/`. The extension code in `tools/Precept.VsCode/src/extension.ts` resolves the language-server project from the workspace, watches the dev build DLL, copies it into `temp/dev-language-server/runtime/run-*`, and restarts from the copied runtime. If workspace project resolution or dev launch preparation fails, the extension can fall back to the bundled server inside the installed extension. That fallback is visible through the `Precept LS` status item, the `Precept: Show Language Server Mode` command, or the `Precept` output channel; developers should verify that the mode is `dev-build-shadow-copy` when they expect source-first behavior. That is the standard for C# changes in `src/Precept/` and `tools/Precept.LanguageServer/`.

For plugin-like Copilot content, local activation is workspace-native, not plugin registration. Agent markdown and skill markdown live under `.github/agents/` and `.github/skills/` so every worktree loads the current checkout's customizations with no per-worktree registration step. The refresh step is window reload, not repackaging. `tools/Precept.Plugin/` remains the shipped payload and is refreshed from the workspace-native sources only for explicit validation.

For extension TypeScript, grammar, and preview assets under `tools/Precept.VsCode/`, the local loop is still source-first but the artifact boundary is the VSIX install. `npm run loop:local` packages the current source and force-installs it into the local VS Code profile. That install is the local refresh mechanism for extension-host assets; it is not the distribution validation path. The inverse loop, `npm run loop:local:uninstall`, removes the installed VSIX from the local profile, but any already-open window that loaded that extension still needs `Developer: Reload Window` before the uninstall is reflected in that window's running extension host.

### Explicit Mode: Distribution Validation

Distribution validation is a separate, explicit activity used to confirm what external users get.

For the VS Code extension, distribution validation means validating the packaged VSIX shape produced by `npm run package:marketplace` in `tools/Precept.VsCode/`, including the bundled language server published into `tools/Precept.VsCode/server/`.

For the MCP server, distribution validation means validating the .NET tool shape defined in `tools/Precept.Mcp/Precept.Mcp.csproj` via `PackAsTool` and `ToolCommandName` `precept-mcp`, then exercising the shipped `dotnet tool run precept-mcp` command.

For the plugin, distribution validation means validating `tools/Precept.Plugin/.claude-plugin/plugin.json`, `tools/Precept.Plugin/.mcp.json`, and the shipped `agents/` and `skills/` contents as a plugin payload. The plugin's `.mcp.json` is already in distribution form, uses the plugin payload `mcpServers` schema, and must stay that way. Development overrides belong in `.vscode/mcp.json`, which uses the VS Code `servers` schema, not in the plugin payload.

Distribution validation is never the daily authoring loop. If a developer needs the shipped form for every edit, the boundary between local mode and distribution mode has been violated.

---

## Artifact Classes

| Artifact class | Source of truth | Local-development path | Shipped/distribution path |
|---|---|---|---|
| MCP server | `tools/Precept.Mcp/` + `src/Precept/` | `.vscode/mcp.json` -> `tools/scripts/start-precept-mcp.js` -> `temp/dev-mcp/` shadow copy | `.NET tool` command `dotnet tool run precept-mcp` from plugin `.mcp.json` |
| Plugin skills, agents, manifests | Workspace-native sources in `.github/agents/` and `.github/skills/`; shipped payload in `tools/Precept.Plugin/` | Auto-discovered from the current checkout's `.github/` customizations | Plugin payload rooted at `tools/Precept.Plugin/`, especially `.claude-plugin/plugin.json` and `.mcp.json` |
| VS Code extension | `tools/Precept.VsCode/` | `npm run loop:local` / task `extension: install`, then reload | Packaged VSIX from `npm run package:marketplace` |
| Language server | `tools/Precept.LanguageServer/` + `src/Precept/` | Build to `temp/dev-language-server/`, shadow-copy runtime, auto-restart from source build | Bundled under `tools/Precept.VsCode/server/` inside the shipped VSIX |
| Grammar and preview assets | `tools/Precept.VsCode/syntaxes/`, `tools/Precept.VsCode/src/`, webview assets in the extension project | Extension local install from current source, then reload | Included in the packaged VSIX |
| Runtime and build outputs | `temp/dev-language-server/`, `temp/dev-mcp/`, generated VSIX files, published `server/` output | Ephemeral, worktree-local, rebuildable, never authoritative | Only the packaging outputs that are intentionally shipped |

---

## Config Ownership Boundaries

### Workspace-Owned Dev Config

These files exist to make the current checkout runnable from source:

- `.vscode/tasks.json`
- `.vscode/mcp.json`
- `tools/scripts/start-precept-mcp.js`

These files are allowed to assume the current workspace root, the current worktree's `temp/` folder, and source-relative paths.

They must not become required inputs for shipped extension or plugin users.

### Plugin-Owned Distribution Config

These files define the shipped Copilot-facing payload:

- `tools/Precept.Plugin/.claude-plugin/plugin.json`
- `tools/Precept.Plugin/.mcp.json`
- `tools/Precept.Plugin/agents/`
- `tools/Precept.Plugin/skills/`

The workspace-native agent and skill sources under `.github/` are the local source of truth. `tools/Precept.Plugin/agents/` and `tools/Precept.Plugin/skills/` are synchronized copies used for plugin payload validation and distribution.

They must describe the distribution surface, not the repo-local dev override. In particular, `tools/Precept.Plugin/.mcp.json` must keep the shipped `dotnet tool run precept-mcp` command and must not be rewritten to point at workspace scripts or `temp/` outputs.

### Extension-Owned Packaging Config

`tools/Precept.VsCode/package.json` owns the extension packaging shape. The scripts `package:marketplace` and `vscode:prepublish` are the extension's boundary between local source editing and packaged validation. The `server/` directory is a packaging output for the shipped extension, not the normal daily dev runtime.

### Generated Output Boundary

Everything under `temp/dev-language-server/` and `temp/dev-mcp/` is generated local infrastructure. These directories are disposable. They must remain outside the shipped plugin and outside the shipped VSIX contract.

---

## Mode Boundaries

### What Local Mode Is Allowed To Do

- resolve projects and assets from the current checkout
- use workspace-relative launch scripts and workspace-native customization files
- build into `temp/`
- use shadow-copy runtimes to avoid locks
- reload or restart the running host to pick up the latest source build

### What Local Mode Must Not Do

- depend on packaged marketplace installs as the primary source of truth
- mutate shipped plugin manifests to make local development work
- run from build output directories that are also rebuild targets
- share mutable runtime directories across worktrees

### What Distribution Validation Is Allowed To Do

- package the extension and validate the packaged VSIX
- validate the plugin payload in its shipped layout
- validate the MCP server through the shipped `.NET tool` command

### What Distribution Validation Must Not Become

- the default edit-test-refresh loop
- the place where source edits are first observed
- a substitute for workspace-owned dev config

---

## Worktrees

Worktrees are explicitly supported by this operating model.

Each worktree is its own local-development environment. That means:

- each worktree uses its own checkout of `src/`, `tools/`, `.vscode/`, and `temp/`
- each worktree automatically sees the agent and skill customizations checked into that worktree under `.github/`
- each worktree builds its own `temp/dev-language-server/` and `temp/dev-mcp/` outputs

This isolation is required for two reasons.

First, it keeps configuration ownership clear. The current worktree's `.github/` customizations and `.vscode/mcp.json` determine the local Copilot and MCP behavior without relying on user-global plugin registration.

Second, it preserves rebuild safety. A worktree must never launch the MCP server or language server from another worktree's build output or runtime directory. Doing so reintroduces stale binary risk, shared-state confusion, and cross-worktree locking behavior.

The installed VSIX in the editor profile may be shared across worktrees, but once a worktree is open the active runtime resolution must still come from that worktree's source and `temp/` outputs.

The prerequisite is strict: do not point one worktree's `.vscode/` or `temp/` at another worktree through a symlink, junction, shared directory, or external sync step. If those directories are shared, the workspace-owned model stops being workspace-owned and the shadow-copy runtime isolation becomes unreliable.

---

## No-File-Locking And Immediate Refresh Requirements

The local-development path must satisfy both of these requirements at all times:

1. **No file locking on rebuild targets.** The running language server and MCP server must execute from shadow-copied runtime directories, not from the directories that `dotnet build` writes into.
2. **Immediate refresh after the documented step.** The new source must be observable immediately after the required action for that artifact class.

Current implementation:

- Language server: `Ctrl+Shift+B` or task `build` writes to `temp/dev-language-server/`; the extension watches the dev DLL, waits for an approximately 500 ms restart debounce, prepares a runtime copy, and restarts automatically. No window reload is required for C# runtime or language-server changes. The language server can reuse the latest successful dev build already in `temp/dev-language-server/`; it does not run a fresh `dotnet build` on every restart.
- MCP server: the launcher in `tools/scripts/start-precept-mcp.js` runs `dotnet build` on every MCP server spawn, copies the resulting build into `temp/dev-mcp/runtime/run-*`, and launches the copy. Reload the window so Copilot can reconnect, then wait for the next actual MCP spawn triggered by a tool invocation. That next spawn rebuilds and runs the fresh copy; the fresh process is not guaranteed to exist at the exact instant the window reload finishes.
- Workspace agent and skill customizations: reload the window after editing `.github/agents/` or `.github/skills/` so Copilot rediscovers the current worktree's customization files. When validating the shipped plugin payload, sync those sources into `tools/Precept.Plugin/` first.
- Extension TypeScript, grammar, and preview assets: run `extension: install`, then reload the window so the new local VSIX is active. If you undo that install with `extension: uninstall` or `npm run loop:local:uninstall`, reload the window again before assuming the packaged extension is gone from the current session.

### Troubleshooting And Verification

- Language server mode check: if C# edits do not appear to take effect, click the `Precept LS` status item or run `Precept: Show Language Server Mode`, then inspect the `Precept` output channel. Source-first mode should report `Mode: dev-build-shadow-copy`. If it reports `bundled`, the extension resolved or fell back to the packaged server instead of the workspace project.
- Language server timing: after a successful language-server build, the extension restart is intentionally debounced by about 500 ms so multiple file changes can settle before restart.
- MCP timing: a window reload only resets the host and allows Copilot to reconnect. The dev MCP launcher rebuilds on spawn, so the fresh build appears on the next MCP tool invocation that causes a new spawn.
- Extension uninstall loop: uninstall removes the VSIX from the local profile, but it does not retroactively tear down an already-running extension host in the current window. Reload the window to observe the uninstall cleanly.
- Shadow-copy cleanup: runtime directory pruning is best-effort and lazy. If a previous runtime directory is locked, cleanup is skipped and an older `temp/dev-language-server/runtime/run-*` or `temp/dev-mcp/runtime/run-*` directory can remain until a later restart or spawn succeeds in pruning it.

If any future artifact path cannot meet those two conditions, it does not meet the project standard and must be redesigned.

---

## Practical Standard By Artifact

### MCP Server

Develop against `tools/Precept.Mcp/` through `.vscode/mcp.json` and `tools/scripts/start-precept-mcp.js`. Validate the `.NET tool` shape only when intentionally checking the shipped path.

### Plugin Skills, Agents, And Manifests

Edit `.github/agents/` and `.github/skills/` directly for local work. Sync those sources into `tools/Precept.Plugin/` only when validating or packaging the shipped plugin payload. Keep `.mcp.json` and `.claude-plugin/plugin.json` in shipped form; do not bend them into dev-only launchers.

### VS Code Extension

Use the local install loop for extension-host assets and the dev language-server build for C# behavior. Use marketplace packaging only when intentionally validating the shipped VSIX.

### Language Server

The language server is source-driven in local mode and bundled only for distribution. The bundled copy under `server/` is a packaging artifact, not the normal development runtime.

### Grammar And Preview Assets

Treat grammar, preview webview code, and extension commands as extension assets. They ride the extension local-install loop, not the MCP or plugin path.

### Runtime And Build Outputs

Treat `temp/` outputs and generated packages as disposable products of the current worktree. They are evidence of a build, not configuration inputs or hand-maintained assets.

---

## Decision Summary

The chosen model is:

- source-first for all routine local work
- workspace-owned for all dev-time wiring and overrides
- worktree-local for settings, temp outputs, and runtime copies
- shadow-copy based for rebuild safety
- packaged or distribution-shaped only for explicit validation

That split is the project standard across the MCP server, plugin payload, VS Code extension, language server, grammar/preview assets, and generated build outputs.