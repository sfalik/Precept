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

## Local Development

Daily local development is worktree-native:

- MCP uses the committed workspace override in `.vscode/mcp.json`.
- The Precept Author agent and companion skills load from `.github/agents/` and `.github/skills/` in the current checkout.
- Reload the window after editing those workspace-native customization files.

If you previously used a `chat.pluginLocations`-based local setup, remove any stale registration that points at `tools/Precept.Plugin/`. The workspace-native `.github/` customizations are now the default local authoring path.

`tools/Precept.Plugin/` is the shipped plugin payload, not the default local authoring surface. When you want to refresh that payload from the workspace-native sources for explicit validation, run task `plugin: sync payload`.

The plugin's `.mcp.json` stays in shipped/distribution form with `dotnet tool run precept-mcp`. The workspace-owned `.vscode/mcp.json` remains the source-first local MCP path.
