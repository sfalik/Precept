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

Enable the agent plugins preview in your VS Code user settings:

```json
{
  "chat.plugins.enabled": true
}
```

Then register the plugin locally using the workspace task:

- **`plugin: enable`** — adds `tools/Precept.Plugin/` to `chat.pluginLocations`
- **`plugin: disable`** — removes it

After enabling, reload the window (`Developer: Reload Window`). Copilot will discover the plugin's MCP server, agent, and skills automatically.

Once loaded, the MCP tools are available to Copilot without requiring an explicit agent switch. The agent and skills are additional layers on top of the same tool surface.

The dev `.mcp.json` uses a launcher script that builds the MCP server from source and shadow-copies the output to prevent file locking during rebuilds. At publish time, CI rewrites `.mcp.json` to use `dotnet tool run precept-mcp`.
