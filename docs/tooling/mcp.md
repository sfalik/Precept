# MCP Server

> **Canonical MCP contract document.** `docs/tooling/mcp.md` is the single source of truth for the live Precept MCP surface. `docs/McpServerDesign.md` is archived as a redirect.

## 1. Status

| Property | Value |
|---|---|
| Canonical doc | Yes |
| Last consolidated | 2026-05-15 |
| Source | `tools/Precept.Mcp/` |
| Live discoverable tools | 10 |
| Runtime execution tools | Planned only — not registered in `tools/Precept.Mcp/Tools/` |
| Legacy broad language tool | Removed — `precept_language` has no compatibility shim |

## 2. Scope and document boundaries

- This document defines the current discoverable MCP contract exposed by the live tool classes under `tools/Precept.Mcp/Tools/`.
- `tools/Precept.Plugin/README.md` is a distribution-surface/plugin README, not the canonical source-first MCP contract document.
- When a live MCP tool is added, removed, or its payload changes, update this file in the same pass.

## 3. Live tool surface

Verified against the registered tool classes currently present in `tools/Precept.Mcp/Tools/`.

| Tool | Source file | Parameters | Returns |
|---|---|---|---|
| `precept_ping` | `PingTool.cs` | none | plain text `ok` |
| `precept_quickstart` | `QuickstartTool.cs` | none | compact markdown quickstart |
| `precept_syntax` | `SyntaxTool.cs` | none | compact markdown syntax reference |
| `precept_types` | `TypesTool.cs` | `scope?` | compact markdown type-system reference |
| `precept_operations` | `OperationsTool.cs` | `category?` | compact markdown operations catalog |
| `precept_proofs` | `ProofsTool.cs` | none | compact markdown proof/fault catalog |
| `precept_patterns` | `PatternsTool.cs` | none | compact markdown patterns and anti-patterns |
| `precept_diagnostic` | `DiagnosticTool.cs` | `code` | compact markdown diagnostic explanation |
| `precept_domains` | `DomainsTool.cs` | `scope?` | compact markdown domain catalog |
| `precept_compile` | `CompileTool.cs` | `text` | compact JSON diagnostics + proof obligations + summary |

## 4. Planned or absent tools

These names are **not** live unless and until a matching registered tool class exists under `tools/Precept.Mcp/Tools/`.

| Tool | Status | Notes |
|---|---|---|
| `precept_language` | removed | No `LanguageTool.cs`, no discoverable tool, no compatibility shim. Focused catalog tools replace it. |
| `precept_inspect` | planned | Not present in `tools/Precept.Mcp/Tools/`; older contract text is design intent only. |
| `precept_fire` | planned | Not present in `tools/Precept.Mcp/Tools/`; do not document it as live behavior. |
| `precept_update` | planned | Not present in `tools/Precept.Mcp/Tools/`; do not document it as live behavior. |
| `precept_create` | planned | Planned construction tool; not present in `tools/Precept.Mcp/Tools/`; do not document it as live behavior. |

### Planned runtime tools

#### `precept_create`

- **Purpose:** Constructs a new precept entity. Accepts a precept definition (text or reference) and construction arguments (the fields required by the matching construction row). Returns the construction outcome: `Created` with the initial state, `Rejected` with a reason, or `NoMatchingRow` when no construction row matches.
- **Inputs:** precept definition text (or the name of a loaded definition), construction event name, construction arguments as key-value pairs.
- **Output:** outcome type (`Created` / `Rejected` / `NoMatchingRow`), resulting state (if `Created`), reject reason (if `Rejected`), and which construction row matched.
- **Status:** Planned — requires runtime implementation (`Create()`).

#### `precept_update`

- **Purpose:** Updates one or more fields on an existing entity outside of an event transition. Accepts a precept definition, an entity state snapshot, and field key-value pairs to apply.
- **Inputs:** precept definition text (or the name of a loaded definition), entity state (restored snapshot), and field updates as key-value pairs.
- **Output:** updated entity state when validation succeeds, or a validation error when the requested updates violate constraints.
- **Status:** Planned — requires runtime implementation (`Update()`).

#### `precept_inspect`

- **Purpose:** Previews what would happen if a given event were fired without committing the change. Accepts a precept definition, an existing entity state, an event name, and event arguments so agents can inspect the predicted path before mutation.
- **Inputs:** precept definition text (or the name of a loaded definition), entity state (restored snapshot), event name, and event arguments as key-value pairs.
- **Output:** predicted outcome, matching transition row, which guards would pass or fail, and which actions would execute; construction inspection also requires `InspectCreate()` support.
- **Status:** Planned — requires runtime implementation (`InspectFire()` / `InspectCreate()`).

#### `precept_fire`

- **Purpose:** Fires an event on an existing entity. Accepts a precept definition, an existing entity state, an event name, and event arguments, then returns the evaluated runtime outcome.
- **Inputs:** precept definition text (or the name of a loaded definition), entity state (restored snapshot), event name, and event arguments as key-value pairs.
- **Output:** `Transitioned` (with the new state), `NoTransition`, `Rejected` (with reason), or `Unhandled`.
- **Status:** Planned — requires runtime implementation (`Version.Fire()`).

## 5. Tool conventions

Catalog/reference tools return compact markdown for AI readers:

- `#` title
- `##` major sections
- tight bullets and short entry lines
- fenced `precept` blocks only when code examples matter

Additional conventions:

- `precept_ping` returns plain text, not JSON.
- `precept_compile` returns JSON with the fields documented below.
- `precept_types` and `precept_domains` return a markdown `Unsupported scope` response when passed an invalid `scope`.
- `precept_operations` treats `category` as the normal path; unmatched categories still return the category list plus an empty `Matching Operations` section and `Count` of `0`.

## 6. Tool contracts

### `precept_ping`

Returns plain text:

```text
ok
```

### `precept_quickstart`

Returns markdown with this shape:

```text
# Precept Quickstart
## What Precept Is
## Core Guarantee
## Core Concepts
## Tool Guide
## Minimal Examples
```

### `precept_syntax`

Returns markdown with these sections:

```text
# Precept Syntax Reference
## Grammar Rules
## Operator Precedence
## Conventional Order
## Constructs
## Actions
## Outcomes
## Operators
```

### `precept_types`

Optional `scope` values:

- `types`
- `modifiers`
- `modifiers:value`
- `modifiers:state`
- `modifiers:event`
- `modifiers:access`
- `modifiers:anchor`
- `functions`
- omitted = full bundle (large; prefer a scope)

Returns markdown with only the requested sections. Full output shape:

```text
# Precept Type System
## Types
## Modifiers
### Value Modifiers
### State Modifiers
### Event Modifiers
### Access Modifiers
### Anchor Modifiers
## Built-in Functions
```

Invalid `scope` response shape:

```text
# Precept Type System
Unsupported scope: `<value>`
Valid scopes: `types`, `modifiers`, `modifiers:value`, `modifiers:state`, `modifiers:event`, `modifiers:access`, `modifiers:anchor`, `functions`.
```

### `precept_operations`

Optional `category` filter is the intended path. Without it, the tool returns the full catalog.

```text
# Precept Operations
Filtered by: `Money`   # only when a category is supplied
## Available Categories
## Matching Operations
## Count
```

### `precept_proofs`

```text
# Precept Proofs and Runtime Faults
## Proof Requirements
## Runtime Faults
```

### `precept_patterns`

```text
# Precept Patterns
## Common Patterns
## Anti-Patterns
```

### `precept_diagnostic`

Accepted `code` formats:

- code name, for example `UndeclaredField`
- PRE number, for example `PRE0017`

Found result:

```text
# Diagnostic UndeclaredField (PRE0017)
## Summary
## Trigger
## Recovery Steps
## Fix Hint
## Related Codes
## Prevents Fault
## Examples
```

Missing result:

```text
# Diagnostic Lookup Failed
Requested: `PRE9999`
Use a diagnostic code name such as `UndeclaredField` or a PRE number such as `PRE0017`.
```

### `precept_domains`

Optional `scope` values:

- `currencies`
- `units`
- `prefixes`
- `dimensions`
- `temporal`
- omitted = full bundle (large; prefer a scope)

Full output shape:

```text
# Precept Domain Catalog
## Currencies
## UCUM Tier-1 Units
## UCUM Prefixes
## Dimensions
## Temporal Units
```

Invalid `scope` response shape:

```text
# Precept Domain Catalog
Unsupported scope: `<value>`
Valid scopes: `currencies`, `units`, `prefixes`, `dimensions`, `temporal`.
```

### `precept_compile`

Returns compact JSON with diagnostics and proof obligations:

```json
{
  "success": true,
  "diagnosticCount": 0,
  "diagnostics": [],
  "summary": "TrafficLight: 4 states, 3 events, 12 transitions, 2 rules, 0 ensures, 0 type errors.",
  "proofObligations": [],
  "eventHandlers": []
}
```

Diagnostic entry shape:

```json
{
  "line": 12,
  "column": 5,
  "severity": "error",
  "code": "PRE0101",
  "message": "Field 'Balance' is not writable in state 'Locked'."
}
```

Proof obligation entry shape:

```json
{
  "kind": "IntervalContainment",
  "disposition": "Unresolved",
  "strategy": "IntervalContainment",
  "emittedDiagnostic": "NumericOverflow",
  "description": "Expression assigned to 'Balance' must stay within declared bounds.",
  "computedInterval": "[0 .. 160]",
  "targetField": "Balance",
  "declaredMin": 0,
  "declaredMax": 100,
  "normalizedDeclaredMin": 0,
  "normalizedDeclaredMax": 100
}
```

Event handler row entry shape:

```json
{
  "eventName": "Create",
  "isConstruction": true
}
```

`isConstruction` is `true` when the event row handles an `initial` event (a construction row that runs when the entity is first created). It is `false` for regular stateless event rows (`on Event -> actions`).

`summary` is a compact prose description, not a projected definition graph.

## 7. Architecture and launch model

The MCP server uses Microsoft.Extensions.Hosting with stdio transport:

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();
await builder.Build().RunAsync();
```

Tool methods stay thin wrappers: deserialize arguments, call one core API or formatter, serialize the result. Domain logic belongs in `src/Precept/`, never in the MCP layer.

### MCP configuration surfaces

| File | Surface | Schema | Purpose |
|---|---|---|---|
| `.vscode/mcp.json` | VS Code/workspace-local | VS Code `servers` | Source-first development in VS Code |
| `.mcp.json` (repo root) | Copilot CLI repo-local | CLI `mcpServers` | Source-first development via Copilot CLI |
| `tools/Precept.Plugin/.mcp.json` | shipped/distribution | CLI `mcpServers` | Plugin payload validation — not the default local authoring surface |

Both `.vscode/mcp.json` and repo-root `.mcp.json` point `precept` at `tools/scripts/start-precept-mcp.js`.
`tools/Precept.Plugin/.mcp.json` remains in shipped `dotnet tool run precept-mcp` form and is refreshed via `plugin: sync payload`.

## 8. Implementation sync targets

When MCP behavior changes, verify these files together:

- `tools/Precept.Mcp/Program.cs`
- `tools/Precept.Mcp/CatalogFormatters.cs`
- `tools/Precept.Mcp/Dtos/CompileToolDtos.cs`
- every registered tool file in `tools/Precept.Mcp/Tools/`

Language-surface changes must be verified against the focused registered tools documented here — especially `precept_quickstart`, `precept_syntax`, `precept_types`, `precept_operations`, `precept_proofs`, `precept_patterns`, `precept_diagnostic`, and `precept_domains`.

There is no live `LanguageTool.cs`. Do not invent a parallel broad-vocabulary contract.
Runtime-tool contracts should not be promoted to the live surface until the corresponding registered tool class exists.

## 9. Cross-references

| Topic | Document |
|---|---|
| Plugin / agent / skills payload | `tools/Precept.Plugin/README.md` |
| VS Code extension launch surface | `docs/tooling/extension.md` |
| Runtime API | `docs/runtime/runtime-api.md` |
| Catalog architecture | `docs/language/catalog-system.md` |
