# Precept MCP Server Design

**Status:** Original 6-tool surface implemented (2026-03-06); packaging and Copilot workflow design updated (2026-03-22); redesigned to 4 tools with text input and structured feedback (2026-03-26); design decisions finalized (2026-03-27); expanded to 5 tools with `precept_update` + `precept_fire` rename (2026-03-27)

## Purpose

An MCP (Model Context Protocol) server that exposes DSL parsing, validation, structural analysis, and runtime execution as tools callable by Copilot (and any other MCP host). This enables semantic understanding of `.precept` files beyond what plain text reading provides.

This design also defines the distribution strategy for the MCP server and companion Copilot customizations (agent + skills). The delivery is split across two vehicles:

- **VS Code extension** — editor features only (language server, syntax highlighting, preview panel, commands)
- **Agent plugin** — Copilot features only (MCP server, custom agent, skills)

This split reflects the principle that the MCP server and Copilot customizations exist solely for AI consumption and have no reason to live in the editor extension.

## Project Location

```
tools/Precept.Mcp/
    Program.cs
    Tools/
        LanguageTool.cs
        CompileTool.cs
        InspectTool.cs
        FireTool.cs
        UpdateTool.cs
        ViolationDto.cs
        JsonConvert.cs
    Precept.Mcp.csproj
```

References `src/Precept/Precept.csproj` directly — all parsing, compilation, and runtime execution reuse the existing implementation unchanged.

## SDK

[`ModelContextProtocol`](https://www.nuget.org/packages/ModelContextProtocol) — the official Microsoft C# MCP SDK. Exposes tools as attributed methods on a class; the SDK handles JSON-RPC transport over stdio.

```xml
<PackageReference Include="ModelContextProtocol" Version="0.1.*" />
<PackageReference Include="Microsoft.Extensions.Hosting" Version="9.*" />
```

Transport: **stdio** (default for local MCP servers launched by VS Code).

---

## Tools

> **Redesign status (2026-03-27):** The original 6-tool surface (validate, schema, audit, run, language, inspect) was implemented on 2026-03-06. The redesign to 5 tools is now implemented — `precept_compile` (merging validate+schema+audit), `precept_inspect` (delegating to engine.Inspect), `precept_fire` (single-event execution, named to match `engine.Fire()`), `precept_update` (direct field editing), and `precept_language` (unchanged). All tools accept inline text input and return structured feedback.

### Tool Philosophy

Each tool owns exactly one concern. There is no overlap in what the tools report, and their failure modes reinforce the intended workflow:

- **`precept_compile`** is the sole source of structured diagnostics (parse errors, type errors, graph warnings). It answers: *"Is this definition correct and well-structured?"*
- **`precept_inspect`** is a read-only possibility map. It answers: *"From this state and data, what can happen for each event, and which fields are editable?"*
- **`precept_fire`** is single-event execution. It answers: *"What actually happens when I fire this event from this state and data?"*
- **`precept_update`** is direct field editing. It answers: *"Can I change these fields from this state, and what constraints fire?"*

Inspect and run compile internally (stateless), but treat compilation as a pass/fail gate — not a diagnostic surface. On compile failure they return a short error directing Copilot to use `precept_compile`. Only `precept_compile` returns structured diagnostics. This gives Copilot a clear signal chain: compile → fix → inspect/fire/update.

### Tool Tiers

| Tier | Tools | Input | Requires Runtime |
|---|---|---|---|
| Language | `precept_language` | *(none)* | No |
| Definition | `precept_compile` | `text` | No (parse + type-check + graph analysis) |
| Runtime | `precept_inspect`, `precept_fire`, `precept_update` | `text` + state + data | Yes |

### 1. `precept_language`

**Purpose:** Return a complete, structured reference for the Precept DSL — vocabulary, construct forms, semantic constraints, expression scoping rules, fire pipeline stages, and outcome kinds. Enables Copilot to write semantically correct `.precept` definitions without relying on trial-and-error against `precept_compile`.

Unlike the other tools, this tool takes no input — it describes the language itself.

**Input:** `{}` (no parameters)

**Output:** Full language reference JSON (vocabulary, constructs, constraints, expressionScopes, firePipeline, outcomeKinds). See the `precept_language` output format section below — unchanged from the original design.

The `vocabulary` object contains the following keyword lists, each reflecting `PreceptToken` metadata:

| Property | `TokenCategory` | Keywords |
|---|---|---|
| `ControlKeywords` | `Control` | `when` |
| `DeclarationKeywords` | `Declaration` | `precept`, `field`, `rule`, `state`, `event`, `ensure`, `edit`, `in`, `to`, `from`, `on` |
| `GrammarKeywords` | `Grammar` | `as`, `with`, `nullable`, `default`, `because`, `any`, `all`, `of`, `into`, `initial` |
| `ActionKeywords` | `Action` | `set`, `add`, `remove`, `enqueue`, `dequeue`, `push`, `pop`, `clear` |
| `OutcomeKeywords` | `Outcome` | `transition`, `no`, `reject` |
| `TypeKeywords` | `Type` | `set`, `string`, `number`, `boolean`, `integer`, `decimal`, `choice`, `queue`, `stack` |
| `ConstraintKeywords` | `Constraint` | `nonnegative`, `positive`, `min`, `max`, `notempty`, `minlength`, `maxlength`, `mincount`, `maxcount`, `maxplaces`, `ordered` |
| `LiteralKeywords` | `Literal` | `true`, `false`, `null` |

`GrammarKeywords` contains connective and modifier keywords that serve a structural grammar role — they join, qualify, or introduce parts of declarations — rather than performing computation or control flow.

`ControlKeywords` is intentionally narrow: it is reserved for actual guard/control-flow tokens. Statement anchors such as `state`, `in`, `to`, `from`, and `on` are emitted under `DeclarationKeywords` so the vocabulary mirrors the runtime token metadata used by syntax highlighting and semantic tokens.

**Scalar type reference** — the `typeKeywords` list includes:

| Type | Description |
|---|---|
| `string` | UTF-16 string value |
| `number` | 64-bit floating-point (IEEE 754) |
| `boolean` | `true` or `false` |
| `integer` | Whole number, no decimal component. Supports arithmetic and numeric range constraints (`nonnegative`, `positive`, `min`, `max`). |
| `decimal` | Exact base-10 decimal. Supports `maxplaces` constraint and the `round()` built-in function. |
| `choice("A","B","C")` | Constrained string value set. Use `ordered` to enable ordinal comparison operators (`<`, `<=`, `>`, `>=`). |

**Constraint keyword reference** — the `constraintKeywords` list includes:

| Constraint | Applies to | Description |
|---|---|---|
| `nonnegative` | `number`, `integer`, `decimal` | Value must be ≥ 0 |
| `positive` | `number`, `integer`, `decimal` | Value must be > 0 |
| `min N` | `number`, `integer`, `decimal` | Value must be ≥ N |
| `max N` | `number`, `integer`, `decimal` | Value must be ≤ N |
| `notempty` | `string`, collections | Value must not be empty |
| `minlength N` | `string` | String length must be ≥ N |
| `maxlength N` | `string` | String length must be ≤ N |
| `mincount N` | collections | Collection element count must be ≥ N |
| `maxcount N` | collections | Collection element count must be ≤ N |
| `maxplaces N` | `decimal` | Caps decimal places to N (e.g. `maxplaces 2` ensures at most 2 decimal digits) |
| `ordered` | `choice` | Enables ordinal comparison operators on choice fields; values compare in declaration order |

**Built-in function reference** — built-in functions are exposed in the `functions` section of the `precept_language` output, with full signature, parameter, and description metadata:

| Function | Category | Signatures | Description |
|---|---|---|---|
| `abs(value)` | numeric | `int→int`, `dec→dec`, `num→num` | Absolute value (type-preserving) |
| `floor(value)` | numeric | `dec→int`, `num→int` | Round toward negative infinity |
| `ceil(value)` | numeric | `dec→int`, `num→int` | Round toward positive infinity |
| `round(value)` | numeric | `int→int`, `dec→int`, `num→int` | Banker's rounding to nearest integer |
| `round(value, places)` | numeric | `(num, int-literal)→dec` | Precision rounding |
| `truncate(value)` | numeric | `dec→int`, `num→int` | Truncate toward zero |
| `min(a, b, ...)` | numeric | `int*→int`, `dec*→dec`, `num*→num` | Smallest of 2+ values (variadic) |
| `max(a, b, ...)` | numeric | `int*→int`, `dec*→dec`, `num*→num` | Largest of 2+ values (variadic) |
| `clamp(value, min, max)` | numeric | `(int×3)→int`, `(dec×3)→dec`, `(num×3)→num` | Constrain to range |
| `pow(base, exp)` | numeric | `(int, int)→int`, `(dec, int)→dec`, `(num, int)→num` | Integer exponent power |
| `sqrt(value)` | numeric | `dec→dec`, `num→num` | Square root (requires non-negative proof) |
| `toLower(value)` | string | `str→str` | Lowercase (invariant culture) |
| `toUpper(value)` | string | `str→str` | Uppercase (invariant culture) |
| `trim(value)` | string | `str→str` | Remove leading/trailing whitespace |
| `startsWith(value, prefix)` | string | `(str, str)→bool` | Case-sensitive prefix test |
| `endsWith(value, suffix)` | string | `(str, str)→bool` | Case-sensitive suffix test |
| `left(value, count)` | string | `(str, num)→str` | Leftmost N chars (clamping) |
| `right(value, count)` | string | `(str, num)→str` | Rightmost N chars (clamping) |
| `mid(value, start, length)` | string | `(str, num, num)→str` | Substring, 1-indexed (clamping) |

The `functions` section in the JSON output provides structured `FunctionDto` objects with `name`, `description`, and `signatures` (each containing `parameters` with name/type/constraint, and `returnType`).

**Implementation:** Serializes `ConstructCatalog.Constructs` + `DiagnosticCatalog.Diagnostics` + reflected `PreceptToken` vocabulary. No MCP-specific data — everything comes from core infrastructure.

---

### 2. `precept_compile`

**Purpose:** Parse, type-check, analyze, and compile a precept definition. Returns the full typed structure (states, fields, events, transitions) alongside any diagnostics — errors that block compilation and warnings/hints that flag structural quality issues. This is the single correctness and structure tool, replacing the former `precept_validate`, `precept_schema`, and `precept_audit`.

**Input:**
```json
{
  "text": "precept BugTracker\nfield Assignee as string nullable\n..."
}
```

**Output (valid definition):**
```json
{
  "valid": true,
  "isStateless": false,
  "name": "BugTracker",
  "initialState": "Triage",
  "stateCount": 7,
  "eventCount": 9,
  "states": [
    { "name": "Triage", "rules": [] },
    { "name": "Blocked", "rules": ["Must have an assignee while blocked"] }
  ],
  "fields": [
    { "name": "Assignee", "type": "string", "nullable": true, "default": null },
    { "name": "Priority", "type": "number", "nullable": false, "default": 3 }
  ],
  "collectionFields": [
    { "name": "PendingSignatories", "kind": "set", "innerType": "string" }
  ],
  "events": [
    {
      "name": "Block",
      "args": [{ "name": "Reason", "type": "string", "nullable": false, "required": true }]
    }
  ],
  "transitions": [
    { "from": "InProgress", "on": "Block", "branches": ["if → Blocked", "else → reject"] }
  ],
  "diagnostics": [
    { "line": 0, "message": "State 'Archived' is unreachable from the initial state.", "code": "PRECEPT048", "severity": "warning" }
  ]
}
```

**Output (type errors — partial structure with diagnostics):**
```json
{
  "valid": false,
  "isStateless": false,
  "name": "BugTracker",
  "initialState": "Triage",
  "stateCount": 7,
  "eventCount": 9,
  "states": [...],
  "fields": [...],
  "collectionFields": [...],
  "events": [...],
  "transitions": [...],
  "diagnostics": [
    { "line": 12, "message": "set target 'Value' type mismatch: expected number but expression produces number|null.", "code": "PRECEPT042", "severity": "error" },
    { "line": 12, "message": "unknown identifier 'Missing'.", "code": "PRECEPT038", "severity": "error" }
  ]
}
```

**Output (parse failure — diagnostics only):**
```json
{
  "valid": false,
  "diagnostics": [
    { "line": 3, "message": "Expected state declaration.", "severity": "error" }
  ]
}
```

**Implementation:** Calls `PreceptCompiler.CompileFromText(text)` — a composed pipeline that runs parse → structured validation → compile. Returns the full model projection when parsing succeeds (even with type errors), diagnostics only when parsing fails. Graph analysis findings (C48–C53) appear as warning/hint-severity diagnostics alongside any type errors. The tool is a thin projection of the core result into JSON.

**Declaration arrays:** The compile output includes four arrays surfacing invariants, state asserts, event asserts, and edit blocks from the parsed definition:

| Array | Item shape |
|-------|------------|
| `rules` | `{ expression, when?, reason, line, isSynthetic }` |
| `stateEnsures` | `{ anchor, state, expression, when?, reason, line }` |
| `eventEnsures` | `{ event, expression, when?, reason, line }` |
| `editBlocks` | `{ state?, when?, fields[], line }` |

The `when` property is present only when the declaration includes a `when <Guard>` clause. It contains the guard expression text.

**`isStateless` field:** `true` when the precept has no `state` declarations. When `isStateless: true`, `initialState` is `null`, `states` is `[]`, and `stateCount` is `0`.

**Stateless example output:**
```json
{
  "valid": true,
  "isStateless": true,
  "name": "CustomerProfile",
  "initialState": null,
  "stateCount": 0,
  "eventCount": 0,
  "states": [],
  "fields": [
    { "name": "Name", "type": "string", "nullable": false, "default": "" },
    { "name": "Email", "type": "string", "nullable": false, "default": "" }
  ],
  "collectionFields": [],
  "events": [],
  "transitions": [],
  "diagnostics": []
}
```

---

### 3. `precept_inspect`

**Purpose:** From a given state and data snapshot, evaluate all declared events and report what each would do — without mutating anything. Lets Copilot explore the precept interactively ("what can happen from here?") instead of guessing event sequences for `precept_fire`.

**Input:**
```json
{
  "text": "precept BugTracker\n...",
  "currentState": "InProgress",
  "data": {
    "Assignee": "alice",
    "Priority": 3,
    "BlockReason": null,
    "Resolution": null
  },
  "eventArgs": {
    "Block": { "Reason": "Waiting on infra" },
    "Reassign": { "User": "bob" }
  }
}
```

The `currentState` parameter is `string?` — pass `null` for stateless precepts. When `currentState` is `null`, all events return `Undefined` outcome (no transition surface). The `data` and `eventArgs` fields behave identically for stateless and stateful precepts.

The `eventArgs` field is optional. When provided, the specified args are used for the named events during evaluation — the tool re-inspects those events individually with the supplied args. Events not listed in `eventArgs` are inspected without args, and the engine still reports its actual `TransitionOutcome`. When the engine surfaces required event arguments for an inspected transition, they appear in the optional `requiredArgs` array.

**Output:**
```json
{
  "currentState": "InProgress",
  "data": { "Assignee": "alice", "Priority": 3, "BlockReason": null, "Resolution": null },
  "events": [
    {
      "event": "Block",
      "outcome": "Transition",
      "resultState": "Blocked",
      "violations": []
    },
    {
      "event": "SubmitReview",
      "outcome": "ConstraintFailure",
      "resultState": null,
      "violations": [
        {
          "message": "Cannot leave InProgress without completion note",
          "source": {
            "kind": "state-ensure",
            "stateName": "InProgress",
            "anchor": "from",
            "expressionText": "CompletionNote != null",
            "reason": "Cannot leave InProgress without completion note",
            "sourceLine": 14
          },
          "targets": [
            { "kind": "field", "fieldName": "CompletionNote" }
          ]
        }
      ]
    },
  ],
  "editableFields": [
    { "name": "Priority", "type": "number", "nullable": false, "currentValue": 3 },
    { "name": "BlockReason", "type": "string", "nullable": true, "currentValue": null }
  ],
  "error": null
}
```

The response echoes the resolved instance snapshot (`currentState` + `data` with defaults applied), so Copilot can see what defaults were filled in and confirm the starting point matches intent. For stateless precepts, `currentState` is `null` in the response. Events appear in declaration order (no sorting). The `editableFields` array lists the effective editable field set for the current data snapshot: stateful `in <State> edit` declarations that match the current state plus any passing guarded edit blocks, or stateless root-level `edit` declarations plus any passing guarded root-level edit blocks.

**Stateless precept behavior summary:**

| Operation | Stateless result |
|-----------|------------------|
| `precept_compile` | `isStateless: true`, `initialState: null`, `states: []` |
| `precept_inspect` with `currentState: null` | All events return `Undefined`; `currentState: null` in response; `editableFields` from root `edit` declarations |
| `precept_inspect` (event) with `currentState: null` | Returns `Undefined` |
| `precept_fire` with `currentState: null` | Returns `Undefined` |
| `precept_update` with `currentState: null` | Works on root-editable fields; `currentState: null` in response |

Each event reports:
- `outcome` — the engine's actual `TransitionOutcome` string (e.g. `Transition`, `NoTransition`, `ConstraintFailure`, `Rejected`, `Undefined`, `Unmatched`)
- `resultState` — the target state on success, `null` otherwise
- `violations` — structured `ViolationDto` array (empty unless `ConstraintFailure`)
- `requiredArgs` — list of required argument names (present only when the engine populates `RequiredEventArgumentKeys`, typically on successful transition inspection)

**Implementation:** Calls `PreceptCompiler.CompileFromText(text)`, then `engine.Inspect(instance)` for the full state-level inspection (declaration order preserved). When `eventArgs` are supplied, re-inspects those specific events individually with `engine.Inspect(instance, eventName, args)`. Projects `EditableFields` from the core `InspectionResult`. No reimplementation of the inspection loop.

**On compile failure:** Returns a short error: `"Compilation failed. Use precept_compile to diagnose and fix errors first."` — no diagnostics, no runtime results. Only `precept_compile` surfaces structured diagnostics.

---

### 4. `precept_fire`

**Purpose:** Fire a single event against a precept from a given state and data snapshot. Returns the execution outcome — the new state, updated data, and any constraint violations. Lets Copilot verify that a specific action actually works at runtime. Named to match the core API (`engine.Fire()`).

Unlike `precept_inspect` (which previews all events read-only), `precept_fire` executes one event and returns its concrete result. Copilot chains sequential calls to trace multi-step scenarios, feeding each result’s state+data into the next call.
The `currentState` parameter is `string?` — pass `null` for stateless precepts. When `currentState` is `null`, fire always returns `Undefined` outcome.
**Input:**
```json
{
  "text": "precept BugTracker\n...",
  "currentState": "InProgress",
  "data": {
    "Assignee": "alice",
    "Priority": 3,
    "BlockReason": null,
    "Resolution": null
  },
  "event": "Block",
  "args": { "Reason": "Waiting on infra" }
}
```

The `currentState` and `data` inputs match the same shape as `precept_inspect`. The `args` field is optional — only needed for events that declare arguments.

**Output (success):**
```json
{
  "event": "Block",
  "outcome": "Transition",
  "fromState": "InProgress",
  "toState": "Blocked",
  "data": { "Assignee": "alice", "Priority": 3, "BlockReason": "Waiting on infra", "Resolution": null },
  "violations": [],
  "error": null
}
```

**Output (constraint failure):**
```json
{
  "event": "SubmitReview",
  "outcome": "ConstraintFailure",
  "fromState": "InProgress",
  "toState": null,
  "data": { "Assignee": "alice", "Priority": 3, "BlockReason": null, "Resolution": null },
  "violations": [
    {
      "message": "Cannot leave InProgress without completion note",
      "source": {
        "kind": "state-ensure",
        "stateName": "InProgress",
        "anchor": "from",
        "expressionText": "CompletionNote != null",
        "reason": "Cannot leave InProgress without completion note",
        "sourceLine": 14
      },
      "targets": [
        { "kind": "field", "fieldName": "CompletionNote" }
      ]
    }
  ],
  "error": null
}
```

The response echoes the resolved data snapshot (with defaults applied), matching the inspect tool's behavior.

**Implementation:** Calls `PreceptCompiler.CompileFromText(text)`, creates an instance at the given state+data, then calls `engine.Fire(instance, event, args)`. Projects `FireResult.Violations` as full `ViolationDto` arrays — no string joining.

**On compile failure:** Returns a short error: `"Compilation failed. Use precept_compile to diagnose and fix errors first."` — no execution results. Only `precept_compile` surfaces structured diagnostics.

---

### 5. `precept_update`

**Purpose:** Apply a direct field edit to a precept instance from a given state and data snapshot. Returns the update outcome — whether the edit succeeded, was rejected (uneditable field, constraint failure, invalid input), and the resulting data. Lets Copilot test stateful `in <State> edit` declarations or stateless root-level `edit` declarations, including guarded forms, without firing events.

The `currentState` parameter is `string?` — pass `null` for stateless precepts. When `currentState` is `null`, `Update` applies to the effective root-editable field set declared with `edit all`, `edit Field1, Field2`, or guarded root-level forms such as `edit all when Guard` and `edit Field1 when Guard`.

**Input:**
```json
{
  "text": "precept BugTracker\n...",
  "currentState": "InProgress",
  "data": {
    "Assignee": "alice",
    "Priority": 3,
    "BlockReason": null,
    "Resolution": null
  },
  "fields": {
    "Priority": 1
  }
}
```

The `fields` object contains the field names and new values to apply. At least one field must be provided.

**Output (success):**
```json
{
  "outcome": "Update",
  "data": { "Assignee": "alice", "Priority": 1, "BlockReason": null, "Resolution": null },
  "violations": [],
  "error": null
}
```

**Output (uneditable field):**
```json
{
  "outcome": "UneditableField",
  "data": { "Assignee": "alice", "Priority": 3, "BlockReason": null, "Resolution": null },
  "violations": [],
  "error": null
}
```

**Output (constraint failure):**
```json
{
  "outcome": "ConstraintFailure",
  "data": { "Assignee": "alice", "Priority": 3, "BlockReason": null, "Resolution": null },
  "violations": [
    {
      "message": "Priority must be between 1 and 5",
      "source": { "kind": "rule", "expressionText": "Priority >= 1 and Priority <= 5", "reason": "Priority must be between 1 and 5", "sourceLine": 8 },
      "targets": [{ "kind": "field", "fieldName": "Priority" }]
    }
  ],
  "error": null
}
```

**Implementation:** Calls `PreceptCompiler.CompileFromText(text)`, creates an instance at the given state+data, then calls `engine.Update(instance, patch => { foreach field: patch.Set(key, value) })`. Projects `UpdateResult.Violations` as `ViolationDto` arrays.

**On compile failure:** Returns a short error: `"Compilation failed. Use precept_compile to diagnose and fix errors first."` — no update results. Only `precept_compile` surfaces structured diagnostics.

---

### DTOs

Tools use two DTO types for structured feedback — diagnostics (compile-time) and violations (runtime). These are thin projections of core types — no domain logic.

**`DiagnosticDto`** (inline in `CompileTool.cs` — sole consumer) — parse, type-check, and graph analysis findings:

```json
{ "line": 12, "column": 18, "message": "unknown identifier 'Missing'.", "code": "PRECEPT038", "severity": "error" }
```

Fields: `line` (1-based), `column` (0-based, optional), `message`, `code` (optional — present for all registered constraints), `severity` (`"error"`, `"warning"`, or `"hint"`).

**`ViolationDto`** (shared `Tools/ViolationDto.cs` — used by inspect, fire, and update) — runtime constraint violations:

```json
{
  "message": "Approved total cannot exceed requested total",
  "source": {
    "kind": "rule",
    "stateName": null,
    "anchor": null,
    "expressionText": "ApprovedTotal <= RequestedTotal",
    "reason": "Approved total cannot exceed requested total",
    "sourceLine": 18
  },
  "targets": [
    { "kind": "field", "fieldName": "ApprovedTotal" },
    { "kind": "field", "fieldName": "RequestedTotal" },
    { "kind": "field", "fieldName": "LodgingTotal" },
    { "kind": "field", "fieldName": "MealsTotal" },
    { "kind": "field", "fieldName": "MileageTotal" },
    { "kind": "field", "fieldName": "Miles" },
    { "kind": "field", "fieldName": "Rate" },
    { "kind": "definition" }
  ]
}
```

`ViolationDto` is a full projection of core `ConstraintViolation`:

- **`source`** — projects `ConstraintSource` (4 subtypes: `rule`, `state-ensure`, `event-ensure`, `transition-rejection`). Each subtype carries its relevant fields (expression text, reason, state name, anchor, event name, source line).
- **`targets`** — projects `ConstraintTarget[]` (5 subtypes: `field`, `event-arg`, `event`, `state`, `definition`). Each subtype carries its relevant identifiers. For field-based rule violations, `targets` represents the full semantic dependency set, not just the field names written literally in the rule text: if a violated rule or state ensure reads a computed field, the violation includes that computed field and every transitive field input beneath it, while still carrying the normal scope target.

This means `precept_inspect`, `precept_fire`, and `precept_update` report the entity data the violated rule actually depends on, explicitly and implicitly, so AI and UI consumers can attribute the failure to the real editable surface without reconstructing the computed-field graph themselves.

This preserves the full structured violation model from core without information loss.

### Error Handling by Tier

| Tool | Compile-time issues | Runtime violations |
|---|---|---|
| `precept_compile` | `IReadOnlyList<DiagnosticDto>` | n/a |
| `precept_inspect` | Short error string (gate) | `IReadOnlyList<ViolationDto>` per event |
| `precept_fire` | Short error string (gate) | `IReadOnlyList<ViolationDto>` |
| `precept_update` | Short error string (gate) | `IReadOnlyList<ViolationDto>` |
| `precept_language` | n/a | n/a |

Only `precept_compile` surfaces structured diagnostics. Inspect, fire, and update treat compilation as a pass/fail gate — on failure they return `"Compilation failed. Use precept_compile to diagnose and fix errors first."` with no runtime results. This prevents diagnostic duplication and gives Copilot a clear signal about which tool to call.

---

## Agent Plugin Distribution

The MCP server, custom agent, and skills are distributed as a **Copilot agent plugin** — a self-contained bundle that users install once and that works across any workspace. Agent plugins are a Preview feature (`chat.plugins.enabled`) that bundle any combination of agents, skills, MCP servers, hooks, and slash commands into a single installable unit.

This replaces the earlier design where the VS Code extension bundled the MCP server via `registerMcpServerDefinitionProvider()` and scaffolded skills into `.github/skills/`. The plugin model eliminates the scaffolding problem entirely — Copilot discovers plugin-provided agents, skills, and MCP servers automatically without workspace file creation.

### Why Plugin Instead of Extension

| Concern | Extension | Plugin |
|---|---|---|
| MCP server registration | `registerMcpServerDefinitionProvider()` | `.mcp.json` at plugin root |
| Skills | No native API; must scaffold into workspace | Discovered automatically from plugin `skills/` |
| Agents | No native API; must scaffold into workspace | Discovered automatically from plugin `agents/` |
| Trust model | Implicit (extension is trusted) | Implicit on install (plugin MCP servers are implicitly trusted) |
| Update mechanism | VS Code extension marketplace updates | Plugin marketplace updates (auto-checked every 24h) |
| User setup | Zero (extension activation) | One-time install from marketplace or Git URL |
| Works in any workspace | Yes (extension is global) | Yes (plugin is global once installed) |

The VS Code extension continues to provide all editor features: language server (diagnostics, completions, semantic tokens, code actions), syntax highlighting (TextMate grammar), preview panel, and commands. It no longer carries MCP server binaries or Copilot customization content.

### Plugin Directory Structure

```
tools/Precept.Plugin/
├── .claude-plugin/
│   └── plugin.json                # Plugin metadata (Claude format)
├── agents/
│   └── precept-author.agent.md    # Precept Author custom agent
├── skills/
│   ├── precept-authoring/
│   │   └── SKILL.md               # Authoring workflow skill
│   └── precept-debugging/
│       └── SKILL.md               # Debugging/diagnosis skill
├── .mcp.json                      # MCP server definition (dotnet tool run precept-mcp)
└── README.md
```

The plugin uses the **Claude format** (`.claude-plugin/plugin.json`). The `.mcp.json` uses `dotnet tool run precept-mcp` — a globally resolvable command that works across both VS Code and Copilot CLI consumers. During development, `.vscode/mcp.json` overrides the plugin's MCP config with a launcher script that builds from source.

### .claude-plugin/plugin.json

```json
{
  "name": "precept",
  "description": "Precept DSL authoring, validation, and debugging tools for GitHub Copilot.",
  "version": "1.0.0",
  "author": { "name": "Precept" },
  "license": "MIT",
  "keywords": ["precept", "dsl", "state-machine", "workflow", "domain-integrity"],
  "agents": ["./agents"],
  "skills": [
    "./skills/precept-authoring",
    "./skills/precept-debugging"
  ]
}
```

### .mcp.json

```json
{
  "mcpServers": {
    "precept": {
      "command": "dotnet",
      "args": ["tool", "run", "precept-mcp"],
      "env": {}
    }
  }
}
```

The plugin uses `dotnet tool run precept-mcp` to launch the MCP server. This is a globally resolvable command that works across both VS Code (via the plugin system) and Copilot CLI (via `/plugin install`). End users must have the .NET SDK installed (same prerequisite as using the Precept NuGet package).

This is the shipped/distribution shape. It is kept in the plugin payload for end-user installation and explicit distribution validation, not as the default local development path for this repo.

### .vscode/mcp.json (Development Override)

During development in this repo, the workspace-owned `.vscode/mcp.json` defines the local source-first MCP launch path with a launcher script:

```json
{
  "servers": {
    "precept": {
      "command": "node",
      "args": ["tools/scripts/start-precept-mcp.js"]
    }
  }
}
```

The launcher (`tools/scripts/start-precept-mcp.js`) builds from source, shadow-copies the output, and runs the copy — preventing file locking during rebuilds. This workspace file is the dev-time override owned by the current checkout and uses the VS Code `servers` schema. The plugin's `.mcp.json` remains in shipped `dotnet tool run precept-mcp` form for plugin payloads and explicit distribution validation, and that plugin file uses its own `mcpServers` payload schema. The path is relative so it resolves correctly from the workspace root.

### Distribution Channels

The plugin can be distributed through multiple channels:

1. **Marketplace listing** — submit to `github/awesome-copilot` or a dedicated marketplace repo. Users discover and install via the Extensions view (`@agentPlugins`).
2. **Direct Git install** — users run `Chat: Install Plugin From Source` with the plugin repo URL. No marketplace needed.
3. **Workspace recommendation** — repos using Precept can recommend the plugin via workspace settings:

```json
{
  "enabledPlugins": {
    "precept@awesome-copilot": true
  }
}
```

4. **Local development** — use the committed workspace MCP override in `.vscode/mcp.json` and workspace-native customizations in `.github/agents/` and `.github/skills/`. Do not require editor-level plugin registration for the default inner loop.
```

### Developer Inner Loop

Cross-artifact policy now lives in `docs/ArtifactOperatingModelDesign.md`. This section is the MCP-specific view of that broader operating model.

This repo produces two VS Code artifacts: the **extension** (editor features) and the **agent plugin** (Copilot features). Both are developed locally from `tools/` and follow the same edit → build → reload cycle.

#### Prerequisites

Open the repo in VS Code. The committed workspace files provide the local MCP path and workspace-native customizations.

#### Tasks

All dev tasks are in `.vscode/tasks.json`, runnable via **Tasks: Run Task**:

| Task | What it does |
|------|-------------|
| `build` | Builds the language server to `temp/dev-language-server/` |
| `extension: install` | Builds + installs the extension from `tools/Precept.VsCode/` |
| `extension: uninstall` | Uninstalls the local extension |
| `plugin: sync payload` | Copies workspace-native agent and skill sources into `tools/Precept.Plugin/` for explicit plugin validation |

#### Edit → Test cycle

**Extension changes** (language server, syntax, preview, commands):

1. For C# changes (`src/Precept/`, `tools/Precept.LanguageServer/`): run `Build Task` / `Ctrl+Shift+B`. The extension detects the new DLL, shadow-copies it, and restarts automatically — no window reload needed.
2. For TypeScript, webview, preview, or syntax grammar changes (`tools/Precept.VsCode/`): run task `extension: install`, then `Developer: Reload Window`. The install task auto-detects VS Code Stable vs Insiders.

**Plugin changes** (agent, skills, MCP tools):

1. For agent/skill markdown: edit in `.github/agents/` and `.github/skills/`, then `Developer: Reload Window`.
2. For MCP server C#: edit in `tools/Precept.Mcp/`, then `Developer: Reload Window` and invoke any MCP tool. The launcher rebuilds and shadow-copies lazily on the next tool invocation after reload.
3. For shipped plugin payload validation: run task `plugin: sync payload`, then validate `tools/Precept.Plugin/` as the distribution-shaped artifact.

#### File locking safety

Both the language server and MCP server use shadow-copy launchers to avoid file locking:

| Server | Build output | Running process locks | Rebuild safe? |
|--------|-------------|----------------------|---------------|
| Language server | `temp/dev-language-server/bin/` | `temp/dev-language-server/runtime/` | Yes |
| MCP server | `temp/dev-mcp/bin/` | `temp/dev-mcp/runtime/run-*/` | Yes |

The running process never locks the build output directory. Old runtime copies are pruned on the next launch; locked directories are silently skipped. The MCP tools accept precept text directly (no file reads), and the language server reads exclusively from LSP in-memory buffers (never from disk).

#### Development vs Distribution

| Concern | Development (this repo) | Distribution (end users) |
|---|---|---|
| Language server | Dev build + shadow copy (auto-restart on `Ctrl+Shift+B`) | Bundled in VSIX under `server/` (framework-dependent) |
| MCP server launch | `.vscode/mcp.json` override with launcher script (build + shadow copy) | Plugin `.mcp.json` with `dotnet tool run precept-mcp` |
| Workspace customizations | `.github/agents/` and `.github/skills/` | Marketplace install or Git URL |
| Extension registration | `extension: install` task | VS Code Marketplace |
| Plugin payload sync | `plugin: sync payload` task | Packaging/validation only |
| .NET prerequisite | .NET SDK (builds from source) | .NET runtime (runs pre-built binaries) |

**Extension packaging for marketplace:** Run `npm run package:marketplace` in `tools/Precept.VsCode/`. This publishes the language server into `server/` (framework-dependent, no self-contained runtime) and packages the VSIX. The `vscode:prepublish` hook runs the same `dotnet publish` step automatically.

**Plugin packaging for distribution:** The plugin's `.mcp.json` already uses the distribution format (`dotnet tool run precept-mcp`). No rewriting is needed at publish time. The launcher script (`tools/scripts/start-precept-mcp.js`) and `.vscode/mcp.json` are workspace-level dev infrastructure, not part of the published plugin. Workspace-native agent and skill sources under `.github/` should be synced into `tools/Precept.Plugin/` before validation or packaging.

### Precept Author Agent

The plugin ships a custom agent (`precept-author.agent.md`) that establishes a lightweight persona with strict tool restrictions. The agent:

- Restricts tools to `read`, `edit`, `search`, `fetch`, and all `precept/*` MCP tools (no terminal, no destructive operations)
- Defines the specialist role, routes to the relevant companion skill, and keeps a small set of cross-cutting guardrails
- Is available from the agents dropdown; some hosts may also support subagent invocation, but the plugin design should not rely on implicit delegation

The agent body is intentionally thin — it owns the **persona, routing, and tool restrictions**. Detailed workflows live in the companion skills, which VS Code auto-discovers and loads based on the user's request. This separation keeps the agent focused on identity and boundaries ("what am I doing, and when should I hand off to a skill?") while skills handle procedure ("how do I do it?").

### Companion Skills

Two skills provide targeted capabilities accessible as slash commands and via automatic model invocation. These skills are the primary procedural layer of the plugin:

**`precept-authoring`** — standardizes the creation and editing workflow:
- If the workspace already contains `.precept` files, read one representative file first to match local conventions
- If no `.precept` files exist, rely on `precept_language` plus task requirements
- Call `precept_compile` to validate and inspect the structure of any file being edited
- Use `precept_inspect` and `precept_fire` in gated sequence only when they add evidence beyond compile output
- Optionally include a Mermaid `stateDiagram-v2` diagram when it helps the user understand the resulting state machine; do not require a diagram for every authoring task
- The skill must be repo-agnostic — it cannot assume `samples/` exists

**`precept-debugging`** — standardizes diagnosis and behavior tracing:
- Diagnose correctness and review structural quality with `precept_compile` (errors + warnings/hints)
- Explore runtime behavior with `precept_inspect` and `precept_fire` only when the prior step succeeded and runtime tracing is still needed
- Optionally include a focused Mermaid `stateDiagram-v2` diagram when it clarifies structure or transition behavior

Both skills include Mermaid guidance for generating full or partial state diagrams from `precept_compile` output data. Diagrams are optional aids, not mandatory output. When the host supports Mermaid rendering, the skill can present a rendered diagram in chat; otherwise the instructions must distinguish clearly between a rendered artifact and raw Mermaid source text. The extension's interactive preview panel (ELK + custom SVG) remains independent; skill-generated Mermaid diagrams serve a different purpose (conversation-embedded, focused, partial).

Both skills follow the [Agent Skills specification](https://agentskills.io/specification):
- `name` must be lowercase kebab-case, match the parent directory name, max 64 chars
- `description` must be explicit and trigger-oriented for reliable discovery
- Keep `SKILL.md` body under 500 lines; move detailed reference to separate files
- Progressive disclosure: metadata (~100 tokens) → instructions (<5000 tokens) → resources (on demand)

---

## Build Order

`Precept.Mcp.csproj` sits alongside the language server in `tools/` and is included in `Precept.slnx`. It depends only on `Precept.csproj` — not on the language server project.

The plugin assembly work depends on the MCP project existing first. The agent and skill content can be drafted in parallel with MCP tool development since they are plain markdown files. The plugin packaging step combines MCP server (via `dotnet tool run`) + agent + skills into the final plugin directory structure.

The VS Code extension has no dependency on the plugin — they are separate distribution artifacts. The extension's `registerMcpServerDefinitionProvider()` is removed once the plugin is the shipping path for MCP. During development, `.vscode/mcp.json` overrides the plugin's MCP config with the launcher script (`tools/scripts/start-precept-mcp.js`), which provides lazy build + shadow-copy for file-locking safety.

---

## Not In Scope (First Version)

- Hot-reload / file watching (Copilot calls tools on demand)
- Multi-file / import resolution (not a DSL feature)
- Authentication / remote transport (stdio is sufficient for local VS Code use)
- A `precept_fix` tool (auto-correction is out of scope; compile + inspect + run provide enough signal for Copilot to self-correct)
