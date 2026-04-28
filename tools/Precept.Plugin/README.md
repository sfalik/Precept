# Precept Agent Plugin

Copilot agent plugin for the Precept DSL — provides MCP tools, a custom agent, and companion skills for authoring and debugging precept definitions.

## What The Plugin Installs

The plugin has three pieces:

- **MCP tools** — the core semantic integration for the Precept DSL and runtime.
- **Skills** — `precept-authoring` and `precept-debugging`, which add task-specific workflows to an ordinary Copilot chat.
- **Agent** — **Precept Author**, an optional specialist mode you can select from the agents picker.

In normal use, the tools and skills are the primary integration surface. Do not assume Copilot will always auto-delegate into the custom agent; treat the agent as the explicit specialist entry point.

## When To Use What

- Use the **MCP tools** when Copilot needs authoritative DSL or runtime answers.
- Use the **skills** when you want the current chat session to follow a Precept-specific workflow.
- Use the **agent** when you want the whole conversation to stay in a Precept-focused specialist mode.

## Tools

The plugin's MCP server exposes five tools:

| Tool | Purpose |
|---|---|
| `precept_language` | DSL vocabulary — keywords, operators, expression scopes, constraints |
| `precept_compile` | Parse, type-check, analyze, and compile a precept definition |
| `precept_inspect` | Read-only inspection from any state + data snapshot |
| `precept_fire` | Single-event execution through the fire pipeline |
| `precept_update` | Direct field editing with constraint evaluation |

## MCP Configuration Surfaces

Three distinct MCP config files exist in this repo:

| File | Surface | Schema | Purpose |
|---|---|---|---|
| `.vscode/mcp.json` | VS Code/workspace-local | VS Code `servers` | Source-first development in VS Code — default inner loop |
| `.mcp.json` (repo root) | Copilot CLI repo-local | CLI `mcpServers` | Source-first development via Copilot CLI |
| `tools/Precept.Plugin/.mcp.json` | Shipped/distribution | CLI `mcpServers` | Plugin payload validation — not for local development |

Both `.vscode/mcp.json` and repo-root `.mcp.json` point `precept` at `tools/scripts/start-precept-mcp.js` — the same source-first wrapper. The `github` server appears only in `.vscode/mcp.json`; Copilot CLI provides GitHub MCP natively and does not need it mirrored.

`tools/Precept.Plugin/.mcp.json` stays in shipped `dotnet tool run precept-mcp` form. Update it only via the `plugin: sync payload` task.

## Local Development

Daily local development is worktree-native:

- **VS Code:** MCP runs from `.vscode/mcp.json` (`servers.precept` → `tools/scripts/start-precept-mcp.js`).
- **Copilot CLI:** MCP runs from repo-root `.mcp.json` (`mcpServers.precept` → same script).
- The Precept Author agent and companion skills load from `.github/agents/` and `.github/skills/` in the current checkout.
- Reload the window after editing those workspace-native customization files.

If you previously used a `chat.pluginLocations`-based local setup, remove any stale registration that points at `tools/Precept.Plugin/`. The workspace-native `.github/` customizations are now the default local authoring path.

`tools/Precept.Plugin/` is the shipped plugin payload, not the default local authoring surface. When you want to refresh that payload from the workspace-native sources for explicit validation, run task `plugin: sync payload`.
