# MCP Server

## Status

| Property | Value |
|---|---|
| Doc maturity | Stub |
| Implementation state | Partial (all 5 MCP tools implemented as thin wrappers; runtime-dependent tools — inspect, fire, update — are blocked on Evaluator implementation) |
| Source | `tools/Precept.Mcp/` |
| Upstream | Compiler (Compilation), Runtime (Precept + Version) |
| Downstream | AI agents, Copilot, developer workflow |

---

## Overview

The MCP server exposes five tools that project the Precept compiler and runtime to AI agents and developers. It is a primary distribution surface — not a bolt-on. Tools are thin wrappers around core APIs; domain logic lives in `src/Precept/`, not in the MCP layer.

---

## Responsibilities and Boundaries

**OWNS:** MCP protocol handling, tool serialization/deserialization, thin wrapper logic for the 5 tools.

**Does NOT OWN:** Compiler logic, runtime logic, catalog content, DSL vocabulary knowledge.

---

## Right-Sizing

The MCP server is intentionally thin. If a tool method exceeds ~30 lines of non-serialization code, the logic belongs in `src/Precept/`. The MCP layer adds no intelligence — it projects what the compiler and runtime already produce. No MCP tool may encode language knowledge that is not already in the catalogs.

---

## Inputs and Outputs

**Input:** MCP tool calls from AI agents or developers (JSON-structured tool arguments)

**Output:** MCP tool results (JSON-structured tool responses)

**Internal:** Each tool call compiles fresh or reads from a cached `Compilation`/`Precept` depending on the tool.

---

## Tool Architecture

Five tools, each a thin wrapper:

- `precept_language` — reads catalogs directly, no compilation needed
- `precept_compile` — calls `Compiler.Compile(text)`, serializes `Compilation`
- `precept_inspect` — builds `Precept` from text, calls inspection runtime
- `precept_fire` — builds `Precept` + `Version` from text+state+data, calls `Version.Fire`
- `precept_update` — builds `Precept` + `Version` from text+state+data, calls `Version.Update`

---

## Tool Design and Output Format

### Five Tools

| Tool | Core API | Status |
|---|---|---|
| `precept_language` | Catalogs directly | Implemented |
| `precept_compile(text)` | `Compiler.Compile` → `Compilation` | Implemented (blocked on pipeline stages) |
| `precept_inspect(text, currentState, data, eventArgs?)` | `Precept` + inspection runtime | Blocked on Evaluator |
| `precept_fire(text, currentState, event, data?, args?)` | `Version.Fire` | Blocked on Evaluator |
| `precept_update(text, currentState, data, fields)` | `Version.Update` | Blocked on Evaluator |

### Thin Wrapper Discipline

Each tool method must:
1. Deserialize arguments
2. Call one core API method
3. Serialize the result

If a tool method needs more than ~30 lines of non-serialization code, the excess belongs in `src/Precept/` as a new API surface.

### Structured Output for AI Reasoning

Tool responses include structured violation explanations — failing expression, evaluated field values, guard context. This transforms the MCP surface from a status reporter to a causal reasoning surface for AI agents.

---

## Dependencies and Integration Points

- **Compiler** (`src/Precept/`): `Compiler.Compile` — called per `precept_compile` tool invocation
- **Runtime** (`src/Precept/`): `Precept.From`, `Version.Fire`, `Version.Update`, inspection runtime — called by runtime-dependent tools
- **Catalogs** (upstream): `precept_language` reads catalogs directly
- **AI agents / Copilot** (downstream): primary consumers
- **Developer workflow** (downstream): `precept_compile` for authoring-time validation

---

## Failure Modes and Recovery

MCP tool calls that fail (compilation errors, runtime faults) return structured error responses, not unhandled exceptions. `precept_compile` always returns a `Compilation` result — even broken programs produce a partial result with diagnostics.

---

## Contracts and Guarantees

- `precept_language` output format matches the `McpServerDesign.md § precept_language` specification.
- `precept_compile` always returns a response (never throws to the MCP caller).
- Runtime-dependent tools (`precept_inspect`, `precept_fire`, `precept_update`) return a structured error when called with a program that has compilation errors.

---

## Design Rationale and Decisions

The MCP surface was designed alongside the core API, not retrofitted. This means the tool signatures reflect what the compiler and runtime can do, not what was convenient to expose. The thin-wrapper discipline enforces that intelligence stays in `src/Precept/`, making the MCP tools a stable projection layer that survives core API evolution.

---

## Innovation

- **Catalog-derived vocabulary:** `precept_language` derives its output directly from catalog metadata — no separate MCP vocabulary list.
- **Inspection as a first-class MCP operation:** `precept_inspect` previews every possible transition with full constraint evaluation.
- **Causal reasoning in tool output:** Structured violation explanations transform MCP from status reporting to causal reasoning.
- **AI-first design:** The MCP surface was designed alongside the core API, not retrofitted.

---

## Open Questions / Implementation Notes

1. `precept_inspect`, `precept_fire`, `precept_update` are blocked on Evaluator implementation.
2. Update MCP DTOs in `tools/Precept.Mcp/Tools/` when descriptor types (`FieldDescriptor`, `StateDescriptor`, `EventDescriptor`, etc.) are implemented.
3. Confirm `precept_language` output format matches `McpServerDesign.md § precept_language` specification.
4. Confirm the `FirePipeline` static array in `LanguageTool.cs` stays current as pipeline stages are implemented.
5. Confirm `precept_compile` returns useful partial structure even when some pipeline stages are stubbed.

---

## Deliberate Exclusions

- **No domain logic in MCP layer:** All intelligence in `src/Precept/`.
- **No custom serialization:** Structured outcomes serialize naturally; no hand-crafted JSON shapes.

---

## Cross-References

| Topic | Document |
|---|---|
| MCP tool design and AI-first principles | `docs/compiler-and-runtime-design.md §14` |
| Runtime API the MCP tools wrap | `docs/runtime/runtime-api.md` |
| VS Code extension that launches the MCP server | `docs/tooling/extension.md` |

---

## Source Files

| File | Purpose |
|---|---|
| `tools/Precept.Mcp/` | All MCP server source files |
