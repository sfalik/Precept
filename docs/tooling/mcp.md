# MCP Server

## 1. Status

| Property | Value |
|---|---|
| Doc maturity | Full |
| Implementation state | Partial (`precept_ping` implemented; other tools stubbed awaiting pipeline/evaluator) |
| Source | `tools/Precept.Mcp/` |
| Upstream | Compiler (`Compilation`), Runtime (`Precept` + `Version`), Catalogs |
| Downstream | AI agents, Copilot, developer workflow |

---

## 2. Overview

The MCP server exposes five tools that project Precept's compiler and runtime to AI agents. It is a **primary distribution surface** — designed alongside the core API, not retrofitted.

| Tool | Purpose | Core API |
|------|---------|----------|
| `precept_ping` | Connectivity check | — |
| `precept_language` | DSL vocabulary from catalogs | Reads 11 language-definition catalogs |
| `precept_compile(text)` | Parse/type-check/analyze | `Compiler.Compile` → `Compilation` |
| `precept_inspect(text, state, data, args?)` | Read-only transition preview | `Version.InspectFire` + `Version.InspectUpdate` |
| `precept_fire(text, state, event, data?, args?)` | Execute single event | `Precept.From` + `Version.Fire` |
| `precept_update(text, state, data, patch)` | Apply field patch | `Precept.From` + `Version.Update` |

All tools are **thin wrappers**: deserialize arguments → call one core API → serialize result. Domain logic lives in `src/Precept/`, never in the MCP layer. If a tool method exceeds ~30 lines of non-serialization code, the excess belongs in core as a new API surface.

### Stateless Operation Model

Each tool call is self-contained. The caller supplies the full context — precept source, current state, current field values — and receives a complete result. The MCP server maintains no session state between calls.

This stateless design supports AI agent workloads: agents call `precept_inspect` to preview transitions, decide which event to fire, call `precept_fire` with the chosen event, and receive the new state+fields. No session ID, no server-side entity tracking. The agent owns the entity's lifecycle.

---

## 3. Responsibilities and Boundaries

**OWNS:**

- MCP protocol handling (stdio transport, tool dispatch)
- Tool argument deserialization
- Structured result serialization (JSON)
- Error-to-structured-response translation

**Does NOT OWN:**

- Compiler logic (lexer, parser, type checker, graph analyzer, proof engine) — `src/Precept/`
- Runtime logic (evaluator, constraint checking, state machine) — `src/Precept/`
- Catalog content (language vocabulary) — `src/Precept/Language/`
- DSL vocabulary knowledge — derived from catalogs, never hardcoded

The MCP layer is a **projection surface**, not a domain owner.

---

## 4. Right-Sizing

**Thin-wrapper discipline:** Each MCP tool method follows a strict three-step pattern:

1. Deserialize JSON arguments into typed parameters
2. Call exactly one core API method
3. Serialize the core API result to JSON

If a tool needs to orchestrate multiple core calls, filter results, or apply domain logic, that logic belongs in `src/Precept/` as a new method that the tool can call atomically. The MCP layer never accumulates intelligence.

**30-line budget:** If a tool method body exceeds ~30 lines of non-serialization code, refactor. The code either (a) belongs in core, or (b) is over-engineering serialization.

**No vocabulary duplication:** `precept_language` reads catalogs directly — it does not maintain a parallel vocabulary list. When a catalog member is added, the MCP output includes it automatically. No MCP-side propagation required.

---

## 5. Inputs and Outputs

### Tool Signatures

| Tool | Arguments | Returns |
|------|-----------|---------|
| `precept_ping` | (none) | `"ok"` |
| `precept_language` | (none) | Complete DSL vocabulary JSON |
| `precept_compile` | `text: string` | `CompileResult` |
| `precept_inspect` | `text: string`, `currentState: string?`, `data: object`, `eventArgs?: object` | `InspectResult` |
| `precept_fire` | `text: string`, `currentState: string?`, `event: string`, `data?: object`, `args?: object` | `FireResult` |
| `precept_update` | `text: string`, `currentState: string?`, `data: object`, `patch: object` | `UpdateResult` |

### Internal Flow

Each runtime tool (`precept_inspect`, `precept_fire`, `precept_update`):

1. Compiles `text` via `Compiler.Compile`
2. If compilation errors: returns `{ "error": "CompilationErrors", "diagnostics": [...] }`
3. Builds `Precept` from `Compilation` via `Precept.From`
4. Restores `Version` from `currentState` + `data` via `precept.Restore`
5. Calls the appropriate operation (`InspectFire`/`Fire`/`Update`)
6. Serializes the outcome to JSON

> **Open Question:** Initial event with null data
> The MCP flow still does not define what `Restore` receives when an initial event is fired with `data = null`. Tooling and runtime parity need one deterministic bootstrap contract so null startup data is not interpreted differently across surfaces.
> *Flagged: 2026-05-04*

---

## 6. Architecture

### Hosting Model

The MCP server uses Microsoft.Extensions.Hosting with stdio transport:

```csharp
var builder = Host.CreateApplicationBuilder(args);
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();
await builder.Build().RunAsync();
```

Tools are static methods decorated with `[McpServerToolType]` on the class and `[McpServerTool(Name = "...")]` on each method. The MCP SDK discovers and registers tools via assembly scanning.

### Tool Implementation Pattern

Canonical example (`PingTool.cs`):

```csharp
[McpServerToolType]
public static class PingTool
{
    [McpServerTool(Name = "precept_ping")]
    [Description("Connectivity check — returns ok.")]
    public static string Ping() => "ok";
}
```

This is the ceiling for MCP tool complexity: single expression, no logic. Other tools follow the same pattern with argument deserialization and core API calls.

### Pipeline Position

The MCP server is **not a compiler pipeline stage**. It is a hosting layer that runs the complete pipeline on demand:

```
AI agent tool call
    ↓
┌─────────────────────────────────────────────────────────┐
│  MCP Server                                             │
│  ┌─────────────────────────────────────────────────┐   │
│  │  Deserialize args                                │   │
│  └─────────────────────────────────────────────────┘   │
│                        ↓                                │
│  ┌─────────────────────────────────────────────────┐   │
│  │  Call core API (Compiler.Compile / Precept.From / │  │
│  │  Version.Fire / Version.Update / etc.)          │   │
│  └─────────────────────────────────────────────────┘   │
│                        ↓                                │
│  ┌─────────────────────────────────────────────────┐   │
│  │  Serialize result to JSON                        │   │
│  └─────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────┘
    ↓
JSON response to agent
```

---

## 7. Component Mechanics

### `precept_ping`

**Arguments:** None

**Core API:** None — this is a health check

**Returns:**

```json
"ok"
```

**Behavior:** Immediate return. Used by AI agents to verify MCP connectivity before issuing domain tool calls.

---

### `precept_language`

**Arguments:** None

**Core API:** Direct read of 11 language-definition catalogs

**Returns:** Complete DSL vocabulary in structured JSON:

```json
{
  "tokens": [
    {
      "kind": "Precept",
      "text": "precept",
      "categories": ["Declaration"],
      "description": "Precept header declaration",
      "textMateScope": "keyword.declaration.precept",
      "semanticTokenType": "keyword"
    },
    // ... all 90+ tokens
  ],
  "types": [
    {
      "kind": "String",
      "keyword": "string",
      "description": "UTF-8 text",
      "accessors": [
        { "name": "length", "returnType": "Integer", "description": "Character count" }
      ],
      "widensTo": []
    },
    {
      "kind": "Money",
      "keyword": "money",
      "description": "Monetary amount with currency",
      "accessors": [
        { "name": "amount", "returnType": "Decimal", "description": "Numeric amount" },
        { "name": "currency", "returnType": "Currency", "description": "Currency code" }
      ],
      "qualifierShape": {
        "axes": [{ "preposition": "in", "axis": "Currency" }]
      },
      "widensTo": []
    },
    // ... all types
  ],
  "modifiers": {
    "field": [
      {
        "kind": "Optional",
        "keyword": "optional",
        "description": "Field may be absent",
        "isPresence": true,
        "isAccessMode": false
      },
      {
        "kind": "Nonnegative",
        "keyword": "nonnegative",
        "description": "Value >= 0",
        "isPresence": false,
        "isAccessMode": false,
        "applicableTo": ["Integer", "Decimal", "Number", "Money", "Quantity", "Duration"]
      },
      // ... all field modifiers
    ],
    "state": [
      {
        "kind": "Initial",
        "keyword": "initial",
        "description": "Entry state for new entities"
      },
      {
        "kind": "Terminal",
        "keyword": "terminal",
        "description": "No outgoing transitions allowed"
      },
      // ... all state modifiers
    ],
    "access": [
      {
        "kind": "Writable",
        "keyword": "writable",
        "description": "Field can be modified via Update",
        "isPresent": true,
        "isWritable": true
      },
      // ... all access modes
    ],
    "anchor": [
      {
        "kind": "Entry",
        "keyword": "entry",
        "description": "Constraint checked on state entry",
        "appliesTo": "ensures"
      },
      // ... all anchors
    ]
  },

> **✅ Resolved in Source — ModifierMeta.ModifierCategory:** `Modifier.cs` already carries a `Category` property of type `ModifierCategory`. The grouping keys here should be derived from that catalog field rather than hardcoded. Update the MCP serialization to read `ModifierMeta.Category` and use its string representation as the grouping key. *(Was: catalog-gap-register.md #24)*

  "actions": [
    {
      "kind": "TransitionTo",
      "keyword": "transition",
      "description": "State machine transition",
      "applicableTo": ["Event"],
      "syntaxShape": "transition to <state>"
    },
    {
      "kind": "Set",
      "keyword": "set",
      "description": "Field assignment",
      "applicableTo": ["Event", "Edit"],
      "syntaxShape": "set <field> to <expr>"
    },
    // ... all actions
  ],

> **✅ Resolved in Source — ActionMeta.SyntaxShape:** `Action.cs` already carries a `SyntaxShape` property of type `ActionSyntaxShape`. The MCP output should read this field from the catalog rather than hardcoding it. *(Was: catalog-gap-register.md #17)*

  "constructs": [
    {
      "kind": "PreceptDecl",
      "leaderToken": "precept",
      "description": "Top-level precept definition",
      "slots": ["name", "body"]
    },
    {
      "kind": "FieldDecl",
      "leaderToken": "field",
      "description": "Field declaration",
      "slots": ["name", "type", "modifiers", "defaultValue"]
    },
    // ... all constructs
  ],
  "constraints": [
    {
      "kind": "Invariant",
      "keyword": "rule",
      "description": "Always-true assertion",
      "scope": "definition"
    },
    {
      "kind": "StateResident",
      "keyword": "ensures",
      "description": "State-anchored constraint",
      "scope": "state",
      "anchors": ["entry", "exit", "resident"]
    },
    // ... all constraint kinds
  ],
  "operators": [
    {
      "kind": "Plus",
      "text": "+",
      "arity": "Binary",
      "precedence": 6,
      "associativity": "Left",
      "description": "Addition"
    },
    {
      "kind": "And",
      "text": "and",
      "arity": "Binary",
      "precedence": 3,
      "associativity": "Left",
      "description": "Logical conjunction"
    },
    // ... all operators
  ],
  "functions": [
    {
      "kind": "Abs",
      "name": "abs",
      "category": "Math",
      "overloads": [
        { "parameters": [{ "name": "value", "type": "Integer" }], "returnType": "Integer" },
        { "parameters": [{ "name": "value", "type": "Decimal" }], "returnType": "Decimal" },
        { "parameters": [{ "name": "value", "type": "Number" }], "returnType": "Number" }
      ],
      "description": "Absolute value"
    },
    // ... all functions
  ],
  "firePipeline": [
    "RowMatching",
    "GuardEvaluation",
    "PreconditionCheck",
    "MutationApplication",
    "InvariantCheck",
    "StateEnsuresCheck",
    "EventEnsuresCheck"
  ]
}
```

> **Open Question (unresolved):** The `firePipeline` array is a hardcoded list that does not match evaluator.md's actual execution model. Should a `FirePipeline` catalog be created, or should this output be removed until it can be derived from evaluator metadata?

**Catalog derivation:** The MCP tool reads `Tokens.All`, `Types.All`, `Modifiers.All`, `Actions.All`, `Constructs.All`, `Constraints.All`, `Operators.All`, `Functions.All`, etc. No parallel vocabulary is maintained — the tool iterates catalog metadata directly (except for `firePipeline`, noted above).

---

### `precept_compile`

**Arguments:**

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `text` | string | Yes | Precept source code |

**Core API:** `Compiler.Compile(text)` → `Compilation`

**Returns on success (no errors):**

```json
{
  "hasErrors": false,
  "diagnostics": [],
  "definition": {
    "name": "LoanApplication",
    "isStateless": false,
    "fields": [
      {
        "name": "Amount",
        "type": "money",
        "qualifier": "in 'USD'",
        "modifiers": ["nonnegative"],
        "isOptional": false,
        "isWritable": false,
        "defaultValue": null
      },
      {
        "name": "Status",
        "type": "choice",
        "options": ["Pending", "Approved", "Rejected"],
        "modifiers": [],
        "isOptional": false,
        "isWritable": false,
        "defaultValue": null
      }
    ],
    "states": [
      {
        "name": "Pending",
        "modifiers": ["initial"],
        "constraints": []
      },
      {
        "name": "Approved",
        "modifiers": ["terminal"],
        "constraints": [
          {
            "kind": "StateResident",
            "anchor": "entry",
            "expression": "Amount <= 100000",
            "because": "Approval limit"
          }
        ]
      }
    ],

> **Open Question:** `SemanticIndex.EnsuresByState` index
> MCP currently has to re-correlate flat `TypedEnsure` arrays just to nest constraints under their anchor state. The compile-time surface needs to decide whether that grouping becomes first-class on `SemanticIndex` or remains a reconstruction step in tooling.
> *Flagged: 2026-05-04*

    "events": [
      {
        "name": "Approve",
        "args": [],
        "rows": [
          {
            "fromStates": ["Pending"],
            "guard": "Amount <= 100000",
            "actions": ["transition to Approved"],
            "toState": "Approved"
          }
        ]
      }
    ],
    "rules": [
      {
        "expression": "Amount > 0",
        "because": "Loan amount must be positive"
      }
    ]
  }
}
```

**Returns on compilation errors:**

```json
{
  "hasErrors": true,
  "diagnostics": [
    {
      "code": "PRE0042",
      "severity": "Error",
      "message": "Unknown type 'moneys'",
      "location": {
        "line": 3,
        "column": 14,
        "length": 6
      }
    }
  ],
  "definition": null
}
```

**Behavior:** Compiles precept source through the full pipeline (lexer → parser → type checker → graph analyzer → proof engine). Returns diagnostics (errors, warnings, hints) and a structured definition on success. AI agents use this to validate precept source and understand its structure before runtime operations.

**Error handling:** When compilation fails, `definition` is null and only `diagnostics` are returned. The `Compilation.SemanticIndex` may contain partial information but is not exposed through this tool.

---

### `precept_inspect`

**Arguments:**

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `text` | string | Yes | Precept source code |
| `currentState` | string? | No | Current state name (null for stateless or initial) |
| `data` | object | Yes | Current field values keyed by field name |
| `eventArgs` | object? | No | Optional event argument values (for event-specific inspection) |

**Core API:** `Version.InspectFire(event, args)` for each available event; `Version.InspectUpdate(patch)` for field editability

> **Open Question:** `precept_inspect` N+1 API calls
> `precept_inspect` is currently a composite view that fans out into one inspection call per event plus update inspection, which violates the thin-wrapper story described earlier in the doc. The tooling surface needs to decide whether this tool is explicitly exempt or whether the core runtime gains a batched inspection API.
> *Flagged: 2026-05-04*

```json
{
  "currentState": "Pending",
  "fields": {
    "Amount": { "value": 50000, "type": "money", "qualifier": "in 'USD'" },
    "ApplicantName": { "value": "Jane Doe", "type": "string" }
  },
  "availableEvents": [
    {
      "name": "Approve",
      "prospect": "Certain",
      "rows": [
        {
          "prospect": "Certain",
          "effect": { "kind": "TransitionTo", "state": "Approved" },
          "resultingFields": {
            "Amount": 50000,
            "ApplicantName": "Jane Doe"
          },
          "constraints": [
            {
              "expression": "Amount <= 100000",
              "status": "Satisfied",
              "evaluatedValue": true
            }
          ]
        }
      ],
      "eventEnsures": []
    },
    {
      "name": "Reject",
      "prospect": "Certain",
      "rows": [
        {
          "prospect": "Certain",
          "effect": { "kind": "TransitionTo", "state": "Rejected" },
          "resultingFields": {
            "Amount": 50000,
            "ApplicantName": "Jane Doe"
          },
          "constraints": []
        }
      ],
      "eventEnsures": []
    }
  ],
  "unavailableEvents": [
    {
      "name": "Cancel",
      "reason": "No matching transition from state 'Pending'"
    }
  ],
  "writableFields": [
    {
      "name": "ApplicantName",
      "currentValue": "Jane Doe",
      "constraints": []
    }
  ]
}
```

**Prospect values:**

| Prospect | Meaning |
|----------|---------|
| `Certain` | The row/event will definitely fire given current field values |
| `Possible` | The row/event may fire depending on unknown values (args, externals) |
| `Impossible` | The row/event cannot fire — guard evaluates to false |

**Behavior:** Read-only preview of what transitions and updates are available from the current state+data. For each declared event, returns whether it can fire and what the result would be. For writable fields, returns what constraints would apply to an update. AI agents use this to understand options before committing to an action.

**Error response (compilation errors):**

```json
{
  "error": "CompilationErrors",
  "diagnostics": [
    { "code": "PRE0042", "severity": "Error", "message": "..." }
  ]
}
```

---

### `precept_fire`

**Arguments:**

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `text` | string | Yes | Precept source code |
| `currentState` | string? | No | Current state name (null for initial event / stateless) |
| `event` | string | Yes | Event name to fire |
| `data` | object? | No | Current field values (null for initial event with no prior state) |
| `args` | object? | No | Event argument values keyed by arg name |

**Core API:** `Precept.From(compilation)` → `precept.Restore(state, fields)` → `version.Fire(event, args)` → `EventOutcome`

**Returns on success (`Transitioned` or `Applied`):**

```json
{
  "outcome": "Transitioned",
  "previousState": "Pending",
  "newState": "Approved",
  "fields": {
    "Amount": 50000,
    "ApplicantName": "Jane Doe",
    "ApprovedAt": "2024-01-15T10:30:00Z"
  },
  "mutations": [
    { "field": "ApprovedAt", "action": "set", "value": "2024-01-15T10:30:00Z" }
  ]
}
```

> **Open Question:** `EventOutcome.mutations` payload
> MCP can only populate its `mutations` array by diffing old and new slot values after a fire operation. The evaluator and tooling surface need to decide whether mutation deltas belong in `EventOutcome` itself or remain a post-processing responsibility in the MCP layer.
> *Flagged: 2026-05-04*

```json
{
  "outcome": "Rejected",
  "reason": "Amount exceeds approval limit",
  "matchedRow": {
    "fromStates": ["Pending"],
    "guard": "Amount > 100000",
    "effect": "reject"
  }
}
```

**Returns on constraint violation (`EventConstraintsFailed`):**

```json
{
  "outcome": "EventConstraintsFailed",
  "violations": [
    {
      "constraint": {
        "kind": "StateEntry",
        "anchor": "entry",
        "state": "Approved",
        "expression": "Amount <= 100000",
        "because": "Approval limit"
      },
      "evaluatedExpression": "50001 <= 100000",
      "fieldValues": {
        "Amount": 50001
      },
      "result": false
    }
  ]
}
```

**Returns on no matching row (`Unmatched`):**

```json
{
  "outcome": "Unmatched",
  "event": "Approve",
  "currentState": "Pending",
  "evaluatedGuards": [
    {
      "guard": "Amount <= 100000",
      "result": false,
      "fieldValues": { "Amount": 150000 }
    }
  ]
}
```

> **Open Question:** `Unmatched` guard trace enrichment
> MCP can only produce `evaluatedGuards` by re-inspecting transition rows after `version.Fire` reports `Unmatched`. The runtime contract needs to decide whether guard traces are first-class on `Unmatched` or whether tooling is expected to reconstruct them out of band.
> *Flagged: 2026-05-04*

```json
{
  "outcome": "UndefinedEvent",
  "event": "Approve",
  "currentState": "Rejected",
  "availableEvents": []
}
```

**Returns on invalid args (`InvalidArgs`):**

```json
{
  "outcome": "InvalidArgs",
  "reason": "Missing required argument 'reason'",
  "expectedArgs": [
    { "name": "reason", "type": "string", "required": true }
  ],
  "providedArgs": {}
}
```

**Error response (compilation errors):**

```json
{
  "error": "CompilationErrors",
  "diagnostics": [...]
}
```

**Behavior:** Executes a single event against the current state+data. Returns the complete outcome including new state, new field values, and any mutations applied. On failure, returns structured information about why the event could not fire — constraint violations include the failing expression, evaluated field values, and `because` rationale.

---

### `precept_update`

**Arguments:**

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `text` | string | Yes | Precept source code |
| `currentState` | string? | No | Current state name |
| `data` | object | Yes | Current field values |
| `patch` | object | Yes | Field values to update, keyed by field name |

**Core API:** `Version.Update(patch)` → `UpdateOutcome`

**Returns on success (`FieldWriteCommitted`):**

```json
{
  "outcome": "FieldWriteCommitted",
  "state": "Pending",
  "fields": {
    "Amount": 75000,
    "ApplicantName": "Jane Doe Updated"
  },
  "appliedChanges": [
    { "field": "ApplicantName", "oldValue": "Jane Doe", "newValue": "Jane Doe Updated" }
  ]
}
```

**Returns on constraint violation (`UpdateConstraintsFailed`):**

```json
{
  "outcome": "UpdateConstraintsFailed",
  "violations": [
    {
      "constraint": {
        "kind": "Invariant",
        "expression": "Amount > 0",
        "because": "Loan amount must be positive"
      },
      "evaluatedExpression": "-100 > 0",
      "fieldValues": { "Amount": -100 },
      "result": false
    }
  ]
}
```

**Returns on access denied (`AccessDenied`):**

```json
{
  "outcome": "AccessDenied",
  "field": "Amount",
  "actualMode": "readonly",
  "requiredMode": "writable",
  "reason": "Field 'Amount' is not writable in state 'Approved'"
}
```

**Returns on invalid input (`InvalidInput`):**

```json
{
  "outcome": "InvalidInput",
  "reason": "Type mismatch: field 'Amount' expects money, got string",
  "field": "Amount",
  "expectedType": "money",
  "providedValue": "not a number"
}
```

**Error response (compilation errors):**

```json
{
  "error": "CompilationErrors",
  "diagnostics": [...]
}
```

**Behavior:** Applies a field patch to the current state. Checks access mode (field must be writable), validates types, and evaluates all applicable constraints (rules, state ensures). Returns the complete outcome including new field values on success, or structured violation information on failure.

---

## 8. Dependencies and Integration Points

### Upstream Dependencies

| Dependency | Used By | Purpose |
|------------|---------|---------|
| `Compiler.Compile` | `precept_compile`, `precept_inspect`, `precept_fire`, `precept_update` | Parse/type-check/analyze source |
| `Precept.From` | `precept_inspect`, `precept_fire`, `precept_update` | Build executable model from Compilation |
| `Precept.Restore` | `precept_inspect`, `precept_fire`, `precept_update` | Reconstruct Version from state+fields |
| `Version.Fire` | `precept_fire` | Execute event, return EventOutcome |
| `Version.Update` | `precept_update` | Apply patch, return UpdateOutcome |
| `Version.InspectFire` | `precept_inspect` | Preview event outcomes |
| `Version.InspectUpdate` | `precept_inspect` | Preview update outcomes |
| 11 Language catalogs | `precept_language` | DSL vocabulary metadata |

### Downstream Consumers

| Consumer | Primary Tools | Usage Pattern |
|----------|---------------|---------------|
| AI agents (Copilot) | All 5 | `precept_language` → `precept_compile` → `precept_inspect` → `precept_fire` |
| Skills (precept-authoring, precept-debugging) | `precept_compile`, `precept_language` | Validation during authoring |
| Developer workflow | `precept_compile` | Manual validation via MCP client |

### Launch Configuration

The MCP server is launched by VS Code (via `tools/scripts/start-precept-mcp.js`) or by the Copilot CLI (via `.mcp.json`). It runs as a child process communicating over stdio.

```json
// .mcp.json (Copilot CLI)
{
  "mcpServers": {
    "precept": {
      "command": "node",
      "args": ["tools/scripts/start-precept-mcp.js"]
    }
  }
}
```

```json
// .vscode/mcp.json (VS Code)
{
  "servers": {
    "precept": {
      "command": "node",
      "args": ["tools/scripts/start-precept-mcp.js"]
    }
  }
}
```

---

## 9. Failure Modes and Recovery

### Error Handling Policy

All MCP tools return structured responses — never raw exceptions. The caller always receives valid JSON, even on catastrophic errors.

| Failure Type | Tool Behavior | Response Shape |
|--------------|---------------|----------------|
| Compilation errors | Return error response with diagnostics | `{ "error": "CompilationErrors", "diagnostics": [...] }` |
| Runtime constraint violation | Return structured outcome | `{ "outcome": "EventConstraintsFailed", "violations": [...] }` |
| Invalid arguments | Return structured outcome | `{ "outcome": "InvalidArgs", "reason": "..." }` |
| Internal error (bug) | Return error response | `{ "error": "InternalError", "message": "..." }` |
| Unknown event | Return structured outcome | `{ "outcome": "UndefinedEvent", "event": "...", "availableEvents": [...] }` |

### Compilation Error Handling

Runtime tools (`precept_inspect`, `precept_fire`, `precept_update`) compile the source before operating. If compilation fails, they return early with the diagnostics:

```json
{
  "error": "CompilationErrors",
  "diagnostics": [
    {
      "code": "PRE0042",
      "severity": "Error",
      "message": "Unknown type 'moneys'",
      "location": { "line": 3, "column": 14, "length": 6 }
    }
  ]
}
```

The caller can then call `precept_compile` directly to get detailed error information before retrying.

### Internal Error Handling

If an unexpected exception occurs (a bug), the MCP tool catches it and returns:

```json
{
  "error": "InternalError",
  "message": "Unhandled exception in Version.Fire: NullReferenceException at ..."
}
```

This ensures the MCP protocol remains intact even when the underlying runtime has bugs. The message includes enough detail for debugging without exposing internal state.

### Recovery Patterns

| Scenario | Recovery |
|----------|----------|
| Compilation error | Fix source and retry |
| Constraint violation | Adjust field values or choose different event |
| Access denied | Field not writable in current state — transition first |
| Unmatched event | Check guards with `precept_inspect` |
| Internal error | Report bug, work around with alternative approach |

---

## 10. Contracts and Guarantees

### Tool Contracts

| Tool | Contract |
|------|----------|
| `precept_ping` | Always returns `"ok"` immediately |
| `precept_language` | Returns complete vocabulary — every catalog member included |
| `precept_compile` | Always returns a response (never throws); `hasErrors` indicates success |
| `precept_inspect` | Read-only — never modifies state; compilation errors → error response |
| `precept_fire` | Returns `EventOutcome` or error response; outcome indicates success/failure |
| `precept_update` | Returns `UpdateOutcome` or error response; outcome indicates success/failure |

### Outcome Discriminants

**EventOutcome** (from `precept_fire`):

| Outcome | Meaning | Success? |
|---------|---------|----------|
| `Transitioned` | State changed, fields updated | ✅ |
| `Applied` | No state change, fields updated (stateless or no-transition row) | ✅ |
| `Rejected` | Authored `reject` row matched | ❌ |
| `InvalidArgs` | Event arguments invalid | ❌ |
| `EventConstraintsFailed` | Constraints violated | ❌ |
| `Unmatched` | No row's guard matched | ❌ |
| `UndefinedEvent` | Event not available in current state | ❌ |

**UpdateOutcome** (from `precept_update`):

| Outcome | Meaning | Success? |
|---------|---------|----------|
| `FieldWriteCommitted` | Patch applied, constraints passed | ✅ |
| `UpdateConstraintsFailed` | Constraints violated | ❌ |
| `AccessDenied` | Field not writable | ❌ |
| `InvalidInput` | Type mismatch or structural error | ❌ |

### Vocabulary Guarantee

`precept_language` output exactly matches the 11 language-definition catalogs. Every member of `Tokens.All`, `Types.All`, `Modifiers.All`, `Actions.All`, `Constructs.All`, `Constraints.All`, `Operators.All`, `Functions.All`, `Operations.All`, `ExpressionForms.All`, and `ProofRequirements.All` appears in the output. No filtering, no omission.

### JSON Stability

Output JSON shapes are stable across minor versions. Field additions are backward-compatible; field removals are breaking changes requiring major version bump.

---

## 11. Design Rationale and Decisions

### Co-Designed, Not Retrofitted

The MCP surface was designed alongside the core API, not bolted on afterward. This means:

- Tool signatures reflect what the compiler and runtime naturally produce
- No adapter layers to reshape core output for MCP consumption
- Core types (`EventOutcome`, `UpdateOutcome`, `Compilation`, etc.) serialize cleanly to JSON
- The core API exposes inspection methods (`InspectFire`, `InspectUpdate`) that MCP tools call directly

### Thin-Wrapper Discipline

The decision to keep MCP tools as thin wrappers is deliberate:

1. **Single source of truth:** Domain logic lives in `src/Precept/` — not duplicated in MCP
2. **Stable projection:** Core API changes automatically flow to MCP output
3. **Testable core:** The core API is tested independently; MCP tests focus on serialization
4. **Maintainable surface:** ~30-line budget prevents tool methods from becoming mini-applications

**Alternative rejected:** Rich MCP tools that orchestrate multiple core calls. This would create a second API surface that drifts from core, requires separate testing, and hides complexity in the wrong layer.

### Stateless Design

Each MCP call is self-contained — the caller provides full context, the server returns a complete result. No session state.

**Rationale:**
- AI agents naturally work stateless — they can checkpoint and resume at any point
- Simpler server implementation — no session management, no cleanup, no timeouts
- Easier testing — each call is independent
- Natural fit for serverless/lambda deployment if needed

**Alternative rejected:** Session-based API where the server holds entity state between calls. This adds complexity (session IDs, timeouts, state synchronization) without benefit — the caller (AI agent) already tracks entity state as part of its reasoning.

### Structured Violation Output

When constraints fail, the output includes:

- The failing constraint (expression, kind, anchor, `because` rationale)
- The evaluated expression with actual values substituted
- The field values that led to the failure

**Rationale:** AI agents can reason about failures — they don't just see "constraint failed" but understand *which* constraint, *why* it failed, and *what values* caused it. This transforms MCP from a status reporter to a causal reasoning surface.

**Example:**

```json
{
  "constraint": {
    "kind": "Invariant",
    "expression": "Amount > 0",
    "because": "Loan amount must be positive"
  },
  "evaluatedExpression": "-100 > 0",
  "fieldValues": { "Amount": -100 },
  "result": false
}
```

An AI agent can read this and say: "The constraint `Amount > 0` failed because Amount was -100. The rationale is 'Loan amount must be positive.' I need to provide a positive Amount."

### Catalog-Derived Vocabulary

`precept_language` reads catalogs directly rather than maintaining a parallel vocabulary list.

**Rationale:**
- No drift between catalog and MCP output
- Adding a language feature to a catalog automatically includes it in MCP output
- The catalog is the language specification; MCP just serializes it

**Alternative rejected:** Hand-maintained JSON schema for MCP vocabulary. This creates a second source of truth that must be manually synchronized with catalogs.

---

## 12. Innovation

### Causal Reasoning Surface

Traditional APIs return success/failure. Precept's MCP tools return *why* — structured violation information that enables AI agents to reason about failures:

- Failing expression with actual values substituted
- Field values at time of evaluation
- Constraint rationale (`because` clause)
- Guard evaluation trace for unmatched events

This is not just debugging output — it's the information an AI agent needs to fix the problem without human intervention.

### Inspection as First-Class Operation

`precept_inspect` previews every available action without committing. The AI agent sees:

- Which events can fire, with what probability (Certain/Possible/Impossible)
- What state and field changes each event would cause
- Which constraints would be evaluated and their current status
- Which fields are writable and what constraints would apply

This look-before-you-leap capability is essential for AI planning — the agent can reason about consequences before taking action.

### Complete Language Knowledge via MCP

`precept_language` exposes the entire DSL vocabulary — every keyword, type, operator, function, constraint kind, and grammar construct. AI agents don't need documentation or training data to understand Precept; they can query the language definition directly.

The vocabulary includes:

- **Tokens:** 90+ keywords, operators, punctuation with categories and descriptions
- **Types:** 25+ types with accessors, qualifiers, widening rules
- **Modifiers:** Field, state, access, and anchor modifiers with applicability
- **Actions:** Mutation verbs with syntax shapes and applicability
- **Constructs:** Grammar forms with slots and leader tokens
- **Constraints:** Invariants, state-anchored, event preconditions with scopes
- **Operators:** Precedence, associativity, arity for expression parsing
- **Functions:** Built-in library with overloads and parameter types

### Fire Pipeline Transparency

`precept_language` includes the `firePipeline` array — the ordered list of stages that `precept_fire` executes. AI agents can understand the execution model:

1. Row matching — find candidate transition rows
2. Guard evaluation — filter by guard expressions
3. Precondition check — event `requires` clauses
4. Mutation application — execute `set`, `add`, `remove`, etc.
5. Invariant check — global `rule` constraints
6. State ensures check — target state's `ensures` constraints
7. Event ensures check — event's `ensures` clauses

This transparency helps agents predict and debug event execution.

---

## 13. Open Questions / Implementation Notes

**[OQ-1] Runtime tools blocked on Evaluator:**
`precept_inspect`, `precept_fire`, `precept_update` are stubbed. Implementation requires:
- Evaluator implementation (expression evaluation, constraint checking)
- `Version.Fire`, `Version.Update`, `Version.InspectFire`, `Version.InspectUpdate` implementations
- `Precept.From`, `Precept.Restore` implementations

**[OQ-2] `precept_language` catalog coverage:**
Verify all 11 language-definition catalogs are serialized. Current expected list:
- Tokens, Types, Functions, Operators, Operations
- Modifiers (4 subtypes: Field, State, Access, Anchor)
- Actions, Constructs, ExpressionForms, Constraints, ProofRequirements

**[OQ-3] FirePipeline array maintenance:**
The `firePipeline` array in `precept_language` output must stay synchronized with actual pipeline stages as they are implemented. Consider deriving this from a catalog or pipeline metadata rather than a static array.

**[OQ-4] Partial compilation results:**
When some pipeline stages are stubbed, `precept_compile` should return useful partial structure. Define what "partial" means — AST without semantic info? Semantic info without graph analysis?

**[OQ-5] MCP DTO evolution:**
When core types change (`EventOutcome` variants, `Compilation` fields, descriptor types), MCP serialization must be updated. Consider:
- Automated DTO generation from core types
- Integration tests that fail when shapes drift

**[OQ-6] Restore validation:**
`precept_fire` and `precept_update` must call `Precept.Restore` to reconstruct Version from caller-provided state+fields. Restore validates constraints — should this validation be strict (fail on violation) or lenient (warn but proceed)?

---

## 14. Deliberate Exclusions

### No Domain Logic in MCP Layer

The MCP layer is a projection surface — it does not contain domain logic:

- **No expression evaluation:** Done by Evaluator in `src/Precept/`
- **No constraint checking:** Done by runtime in `src/Precept/`
- **No state machine logic:** Done by runtime in `src/Precept/`
- **No vocabulary knowledge:** Derived from catalogs in `src/Precept/Language/`

If a tool method needs domain knowledge, that knowledge belongs in core.

### No Custom Serialization

Core types (`EventOutcome`, `UpdateOutcome`, `Compilation`, `Compilation.Definition`, etc.) serialize naturally to JSON via System.Text.Json. No hand-crafted JSON shapes, no custom converters, no DTOs that differ from core types.

**Exception:** If a core type contains internal fields that should not be exposed via MCP, a projection record may be needed. This is not custom serialization — it's API surface control.

### No Session State

The MCP server maintains no state between calls:

- No entity tracking
- No compilation caching (each call compiles fresh)
- No session IDs
- No timeouts or cleanup

The caller owns state. The server is a pure function: `(request) → response`.

### No Batch Operations

Each MCP call operates on one precept source, one event, one patch. Batching (compile multiple sources, fire multiple events) is not supported.

**Rationale:** Simplicity. AI agents can call tools in sequence. The MCP protocol handles request/response pairs naturally. Batching adds complexity without clear benefit for the AI-agent use case.

### No Streaming

Tool responses are complete JSON objects, not streamed. Even large responses (e.g., `precept_language` vocabulary) are returned atomically.

**Rationale:** MCP protocol and typical AI agent integrations handle complete responses. Streaming adds complexity for marginal benefit given response sizes.

### No Authentication/Authorization

The MCP server trusts its caller. No API keys, no user identity, no permission checks.

**Rationale:** The MCP server runs as a local child process, launched by VS Code or Copilot CLI. The trust boundary is the process launch, not the MCP protocol.

---

## 15. Cross-References

| Topic | Document |
|-------|----------|
| Catalog system (vocabulary source) | `docs/language/catalog-system.md` |
| Runtime API (`Precept`, `Version`, outcomes) | `docs/runtime/runtime-api.md` |
| Evaluator (expression evaluation, constraint checking) | `docs/runtime/evaluator.md` |
| Compiler pipeline (lexer → parser → type checker → etc.) | `docs/compiler/` |
| VS Code extension (MCP server launch) | `docs/tooling/extension.md` |
| Plugin (agent + skills that call MCP) | `docs/tooling/plugin.md` |
| Type system (types, accessors, qualifiers) | `docs/language/type-system.md` |
| Constraint system (rules, ensures, anchors) | `docs/language/constraint-system.md` |

---

## 16. Source Files

| File | Purpose |
|------|---------|
| `tools/Precept.Mcp/Program.cs` | MCP server bootstrap — hosting configuration |
| `tools/Precept.Mcp/Tools/PingTool.cs` | `precept_ping` implementation |
| `tools/Precept.Mcp/Tools/LanguageTool.cs` | `precept_language` implementation (planned) |
| `tools/Precept.Mcp/Tools/CompileTool.cs` | `precept_compile` implementation (planned) |
| `tools/Precept.Mcp/Tools/InspectTool.cs` | `precept_inspect` implementation (planned) |
| `tools/Precept.Mcp/Tools/FireTool.cs` | `precept_fire` implementation (planned) |
| `tools/Precept.Mcp/Tools/UpdateTool.cs` | `precept_update` implementation (planned) |
| `tools/scripts/start-precept-mcp.js` | Launch script for VS Code / Copilot CLI |
| `.mcp.json` | Copilot CLI MCP server configuration |
| `.vscode/mcp.json` | VS Code MCP server configuration |
