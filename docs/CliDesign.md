# CLI Design Notes

> **Authority boundary:** This file lives in `docs/`, the repository's legacy/current reference set. Use it for the implemented v1 surface, current product reference, or historical context. If you are designing or implementing `src/Precept.Next` / the v2 clean-room pipeline, start in [docs.next/README.md](../docs.next/README.md) instead.

Date: 2026-03-04

Status: **Design phase — not yet implemented.**

## Overview

`smcli` is a thin command-line host for the DSL runtime. It supports two execution modes: an interactive REPL for exploration and one-shot commands for scripting. The CLI itself contains **zero domain logic** — all workflow semantics live in `DslWorkflowEngine` methods, and the CLI is pure I/O, formatting, and argument parsing.

## Motivation

The previous CLI host was removed. The only remaining runtime surface is the VS Code language server preview panel (`PreceptPreviewHandler`). This leaves two gaps:

1. **Automated testing** — no way to drive event sequences against sample `.precept` files from a terminal or CI pipeline.
2. **Shell-level exploration** — no way to inspect/fire/query a machine without the editor open.

A dedicated CLI closes both gaps while keeping the runtime library (`Precept`) free of presentation concerns.

## Design Principles

### Thin-host architecture

The CLI must not duplicate domain logic that belongs in the runtime. During the API audit, three chunks of logic were identified as living in `PreceptPreviewHandler` that would need to be duplicated in the CLI:

| Logic | Where it lives today | Problem |
|-------|---------------------|---------|
| Available-event filtering | `PreceptPreviewHandler.BuildSnapshot` — now uses `engine.Inspect(instance)` aggregate | Both CLI and LSP need this; now exposed via the aggregate |
| Event argument coercion | `engine.CoerceEventArguments(eventName, args)` — converts JSON/untyped values to runtime types | Now a public engine method |
| Instance data serialization | `instance.InstanceData` — clean `IReadOnlyDictionary<string, object?>` with `List<object>` for collections; no internal keys exposed | Clean public format is now standard |

**Resolution:** These are now available on `DslWorkflowEngine` as public methods (see [Runtime API Extensions](#runtime-api-extensions)). The CLI then calls `engine.Method()` for every operation and only handles formatting.

### Mode separation

| Aspect | REPL | One-shot |
|--------|------|----------|
| Invocation | `precept --file path.precept` | `precept --file path.precept <command> [args]` |
| Strictness | Non-strict (blocked/undefined print warning, continue) | Strict (non-zero exit code on blocked/undefined) |
| Output style | Human-readable, colorized | JSON on stdout |
| Prompting | Interactive prompts for missing args | No prompting; missing args → error |
| Data flow | Persistent instance across commands | stdin JSON → hydrate → execute → stdout → exit |

---

## CLI Contract

### Executable

`smcli`

### Global Options

| Option | Description |
|--------|-------------|
| `--file <path.precept>` | Path to `.precept` definition file (required) |
| `--no-color` | Disable ANSI color output |
| `--verbose` | Show additional diagnostic information |

### Commands

#### `state`

Print the current state name.

- **REPL:** Plain text to console.
- **One-shot:** `{ "state": "Red" }`

#### `events`

Print events available from the current state with their outcome status.

- **REPL:** Formatted list with outcome indicators (enabled/blocked/undefined/noTransition).
- **One-shot:** JSON array of event objects.
- **Runtime API:** `engine.Inspect(instance)` — use `.Events` list for outcomes per event.

#### `inspect <event> [json]`

Non-mutating evaluation of an event against the current instance.

- **REPL (no json):** Prompt for each required event argument using field metadata from `engine.Events[n].Args`.
- **REPL (with json):** Parse inline JSON as event arguments.
- **One-shot:** JSON argument object required (or empty `{}` for no-arg events).
- **Runtime API:** `engine.Inspect(instance, eventName, coercedArgs)`
- **Output:** Outcome, target state (if accepted), reasons (if blocked/rejected), required argument keys.

#### `fire <event> [json]`

Mutating event execution. Same argument handling as `inspect`.

- **Runtime API:** `engine.Fire(instance, eventName, coercedArgs)` — update `instance = result.UpdatedInstance` on success.
- **Output:** Outcome, previous state → new state (if accepted), reasons (if blocked/rejected).
- **REPL prompt update:** `MachineName[NewState]>` after successful fire.

#### `data`

Print the current instance data.

- **REPL:** Pretty-printed, colorized JSON.
- **One-shot:** JSON to stdout.
- **Runtime API:** `instance.InstanceData` — clean `IReadOnlyDictionary<string, object?>` with `List<object>` for collections, no `__collection__` prefix.

#### `data load [@path | json]`

Reset and hydrate the instance with provided data.

- **`@path`:** Read JSON from file.
- **Inline json:** Parse directly.
- **REPL (no argument):** Prompt field-by-field using `engine.Fields` and `engine.CollectionFields` metadata.
- **Runtime API:** `engine.CreateInstance(parsed)` — accepts a clean dictionary with `List<object>` for collections.

#### `data save [@path]`

Write current instance data as JSON.

- **`@path`:** Write to file.
- **No argument:** Write to stdout.
- **Runtime API:** JSON serialize `instance.InstanceData` directly.

#### `rules`

Evaluate all current rules against the instance.

- **REPL:** List of violated rules with reasons, or "All rules satisfied."
- **One-shot:** JSON array of violation strings.
- **Runtime API:** `engine.CheckCompatibility(instance)` (runs rule evaluation)

#### `help`

Show available commands. REPL only.

#### `exit`

Quit the REPL. REPL only.

### REPL Prompt

```
MachineName[CurrentState]>
```

Example: `TrafficLight[Red]>`

### Exit Codes (One-shot)

| Code | Meaning |
|------|---------|
| 0 | Success (event accepted or query completed) |
| 1 | Blocked / rejected |
| 2 | Undefined (unknown state or event) |
| 3 | Parse / compile error in `.precept` file |
| 4 | Runtime or input error (bad JSON, file not found) |

### One-shot Data Flow

```
stdin JSON ──► hydrate instance ──► execute command ──► stdout result ──► exit with code
```

When stdin is not a TTY and no explicit `data load` is given, stdin is read as JSON and used to hydrate the instance before executing the command.

---

## Runtime API Extensions

These methods are available on `PreceptEngine` in `src/Precept/Dsl/PreceptRuntime.cs` to keep the CLI (and `PreceptPreviewHandler`) free of domain logic.

### `Inspect(instance)` aggregate

Returns `DslInspectionResult` with `CurrentState`, clean `InstanceData`, and `Events : IReadOnlyList<DslEventInspectionResult>` — all events evaluated in one call. This replaces the former `GetAvailableEvents` proposal.

### `CoerceEventArguments`

```csharp
/// <summary>
/// Coerces raw event argument values (e.g. JsonElement from JSON deserialization)
/// to the declared scalar types for the given event.
/// Returns null if args is null.
/// </summary>
public IReadOnlyDictionary<string, object?>? CoerceEventArguments(string eventName, IReadOnlyDictionary<string, object?>? args)
```

Uses `_eventArgContractMap` internally to look up declared argument types per event.

**Consumers:** CLI `inspect`/`fire` argument handling, `PreceptPreviewHandler.HandleFire`/`HandleInspect`.

### `CheckCompatibility`

```csharp
/// <summary>
/// Validates schema compatibility and evaluates all current rules against the instance.
/// Returns DslCompatibilityResult(IsCompatible, Reason?).
/// </summary>
public DslCompatibilityResult CheckCompatibility(DslWorkflowInstance instance)
```

**Consumers:** CLI `rules` command.

### `CreateInstance`

```csharp
/// <summary>
/// Creates a new DslWorkflowInstance from a clean data dictionary.
/// Collections should be List&lt;object&gt; under their plain field names (no __collection__ prefix).
/// </summary>
public DslWorkflowInstance CreateInstance(IReadOnlyDictionary&lt;string, object?&gt;? data = null)
```

`instance.InstanceData` always returns a clean dictionary — no internal keys exposed.

**Consumers:** CLI `data load`, REPL startup.

### Impact on PreceptPreviewHandler (already applied)

---

## Libraries

| Package | Purpose |
|---------|---------|
| `System.CommandLine` | Argument parsing, command routing, help generation |
| `Spectre.Console` | Colorized output, tables, prompting |
| `Spectre.Console.Json` | Pretty-printed colorized JSON |
| `RadLine` | REPL line editing (history, key bindings) |

### Project Location

```
tools/Precept.Cli/
├── Precept.Cli.csproj
├── Program.cs
└── ...
```

References `src/Precept/Precept.csproj` as a project reference.

---

## Script File Support

The existing `samples/traffic.script.txt` demonstrates the command format. In the future, `smcli` will accept `--script <path>` to replay a sequence of commands from a file, enabling automated integration testing of all sample machines.

### Script format

```text
# Comments start with #
data
inspect

fire Advance
fire Emergency '{"AuthorizedBy":"Dispatcher","Reason":"Accident"}'
state
data
exit
```

Each line is interpreted as if typed at the REPL prompt. Blank lines and `#` comment lines are skipped.

---

## CLI-to-Runtime Mapping (Full Reference)

| CLI surface | Runtime entry point | Return type |
|-------------|-------------------|-------------|
| Load `.precept` file | `PreceptParser.Parse(text)` | `DslWorkflowModel` |
| Compile | `DslWorkflowCompiler.Compile(model)` | `DslWorkflowEngine` |
| Create instance | `engine.CreateInstance(data?)` | `DslWorkflowInstance` |
| `state` | `instance.CurrentState` | `string` |
| `events` + outcomes | `engine.Inspect(instance)` | `DslInspectionResult` (`.Events` list) |
| `inspect` | `engine.Inspect(instance, event, args?)` | `DslEventInspectionResult` |
| `fire` | `engine.Fire(instance, event, args?)` | `DslFireResult` (`.UpdatedInstance`) |
| `data` | `instance.InstanceData` | `IReadOnlyDictionary<string, object?>` |
| `data load` | `engine.CreateInstance(data)` | `DslWorkflowInstance` |
| `rules` | `engine.CheckCompatibility(instance)` (includes rules) | `DslCompatibilityResult` |
| Compatibility | `engine.CheckCompatibility(instance)` | `DslCompatibilityResult` |
| Coerce args | `engine.CoerceEventArguments(event, args)` | `IReadOnlyDictionary<string, object?>?` |

---

## Open Questions

1. **Tab completion in REPL** — Should `RadLine` autocomplete event names and field names? Deferred to implementation phase.
2. **`--script` mode** — Execute script files non-interactively. Accept `--script` alongside `--file`. Planned but not in initial scope.
3. **`--state` override** — Allow one-shot mode to start from a non-initial state. Low priority.

---

## Implementation Prompt

> Use this prompt verbatim to start a new implementation session.

---

Implement the `precept` CLI tool for the Precept runtime. Work through the tasks in order. Do not skip ahead. After each phase, run `dotnet build` and fix any errors before proceeding.

**Repository:** `c:\Users\Shane.Falik\source\repos\Precept`
**Design doc:** `docs/CliDesign.md` — read this in full before starting.

### Phase 1 — Runtime API verification

The following runtime methods are already implemented on `PreceptEngine` in `src/Precept/Dsl/PreceptRuntime.cs`. Verify they behave as documented before building the CLI layer.

**`Inspect(DslWorkflowInstance instance) → DslInspectionResult`**
Evaluates all events for the current instance state in one call. Use `.Events` (list of `DslEventInspectionResult`) to drive the REPL events display and `events` command output.

**`CoerceEventArguments(string eventName, IReadOnlyDictionary<string, object?>? args) → IReadOnlyDictionary<string, object?>?`**
Return `null` if `args` is null. Coerces raw values (including `JsonElement`) to the declared scalar types for the event's arguments. Unknown keys pass through unchanged. Does not throw.

**`CheckCompatibility(DslWorkflowInstance instance) → DslCompatibilityResult`**
Validates schema compatibility and evaluates all current rules. Use for the `rules` command.

**`CreateInstance(IReadOnlyDictionary<string, object?>? data) → DslWorkflowInstance`**
Creates a new instance with clean field data. Collections are `List<object>` in `instance.InstanceData` — no `__collection__` prefix keys.

After verifying these methods, run the full test suite (`dotnet test`) and confirm all tests pass before proceeding.

### Phase 2 — PreceptPreviewHandler refactor

Simplify `tools/Precept.LanguageServer/PreceptPreviewHandler.cs` to use the new runtime methods. Do not change any behavior — this is a pure refactor.

- Replace the `outgoingEventNames` filtering block in `BuildSnapshot` with `session.Engine.Inspect(session.Instance)` aggregate (already done — `PreceptPreviewHandler` is updated).
- Replace all calls to the private `CoerceEventArgs` method with `session.Engine.CoerceEventArguments(eventName, args)` (already done).
- Instance data is already clean in `session.Instance.InstanceData` — no conversion needed.

Build and run all existing tests after this phase.

### Phase 3 — CLI project scaffold

Create `tools/Precept.Cli/Precept.Cli.csproj`:
- Target `net10.0`, `Exe` output type.
- Project reference to `../../src/Precept/Precept.csproj`.
- NuGet references: `System.CommandLine` (prerelease), `Spectre.Console`, `Spectre.Console.Json`, `RadLine`.
- Add to `Precept.slnx`.

### Phase 4 — CLI implementation

Implement `tools/Precept.Cli/Program.cs` and supporting files. Follow the thin-host principle: **no domain logic in CLI files** — only argument parsing, `definition.Method()` calls, and output formatting.

**Global setup:**
- Parse `--file <path>` (required), `--no-color`, `--verbose` using `System.CommandLine`.
- On startup: read `.precept` file, call `PreceptParser.Parse`, `PreceptCompiler.Compile`, `engine.CreateInstance()`. On parse/compile error, write to stderr and exit with code 3.
- Detect mode: if a subcommand is present → one-shot; otherwise → REPL.

**REPL mode:**
- Use `RadLine` for line input with history.
- Prompt format: `MachineName[CurrentState]> ` (use Spectre.Console markup for color).
- Parse each line as `command [arg] [json]`. Blank lines and `#`-prefixed lines are ignored.
- On blocked/undefined outcome: print warning, do not exit.
- Commands: `state`, `events`, `inspect <event> [json]`, `fire <event> [json]`, `data`, `data load [@path|json]`, `data save [@path]`, `rules`, `help`, `exit`.
- When `inspect` or `fire` is given without a JSON argument, prompt for each required event argument interactively using `Spectre.Console`'s `Ask<T>` (use `engine.Events` to find arg names/types, skip nullable args the user leaves blank).

**One-shot mode:**
- If stdin is not a TTY, read it as JSON and call `engine.CreateInstance(data)` to hydrate the instance before running the command.
- Serialize output as JSON to stdout using `System.Text.Json.JsonSerializer` with `WriteIndented = true`.
- Exit with the appropriate code (0–4) based on the outcome.

**Output formatting (REPL):**
- `state` → plain text line.
- `events` → formatted list; prefix each event with a colored indicator from `engine.Inspect(instance).Events`: green `●` for `Transition`, yellow `●` for `NoTransition`, red `●` for `Rejected`/`ConstraintFailure`, dim `○` for `Undefined`/`Unmatched`.
- `inspect`/`fire` → outcome line (colored), target/previous→new state, reasons list if non-empty.
- `data` → use `Spectre.Console.Json` for pretty-printed colorized JSON.
- `rules` → violations as a red list, or green "All rules satisfied."

**Exit code mapping:**
- `TransitionOutcome.Transition` or `NoTransition` → 0
- `TransitionOutcome.Rejected` or `ConstraintFailure` → 1
- `TransitionOutcome.Undefined` → 2
- Parse/compile error → 3
- Runtime or input error → 4

### Phase 5 — Integration test

Run the CLI against every `.precept` file in `samples/` using the script format from `samples/traffic.script.txt`. For each sample, at minimum: load the workflow, print `state`, print `events`, inspect each available event, fire at least one enabled event, print the new state. Report any failures.

Replay `samples/traffic.script.txt` against `samples/trafficlight.precept` and verify it completes with exit code 0.
