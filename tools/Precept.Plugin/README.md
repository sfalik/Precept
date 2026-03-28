# Precept Agent Plugin

Copilot agent plugin for the Precept DSL — provides MCP tools, a custom agent, and companion skills for authoring and debugging precept definitions.

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

The dev `.mcp.json` uses a launcher script that builds the MCP server from source and shadow-copies the output to prevent file locking during rebuilds. At publish time, CI rewrites `.mcp.json` to use `dotnet tool run precept-mcp`.
