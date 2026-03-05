# CLI Design Notes

Date: 2026-03-04

Status: **Design phase — not yet implemented.**

## Overview

`smcli` is a thin command-line host for the DSL runtime. It supports two execution modes: an interactive REPL for exploration and one-shot commands for scripting. The CLI itself contains **zero domain logic** — all workflow semantics live in `DslWorkflowDefinition` methods, and the CLI is pure I/O, formatting, and argument parsing.

## Motivation

The previous CLI host was removed. The only remaining runtime surface is the VS Code language server preview panel (`SmPreviewHandler`). This leaves two gaps:

1. **Automated testing** — no way to drive event sequences against sample `.sm` files from a terminal or CI pipeline.
2. **Shell-level exploration** — no way to inspect/fire/query a machine without the editor open.

A dedicated CLI closes both gaps while keeping the runtime library (`StateMachine.Dsl`) free of presentation concerns.

## Design Principles

### Thin-host architecture

The CLI must not duplicate domain logic that belongs in the runtime. During the API audit, three chunks of logic were identified as living in `SmPreviewHandler` that would need to be duplicated in the CLI:

| Logic | Where it lives today | Problem |
|-------|---------------------|---------|
| Available-event filtering | `SmPreviewHandler.BuildSnapshot` — filters `machine.Transitions` + `machine.TerminalRules` by current state | Both CLI and LSP need this; transition map is private |
| Event argument coercion | `SmPreviewHandler.CoerceEventArgs` — converts JSON strings/JsonElement to runtime types | Both CLI and LSP need this; contract map is private |
| Instance data serialization | Scattered — `__collection__` key convention, `CollectionValue.ToSerializableList()` | Internal storage detail leaks to consumers |

**Resolution:** Push these into `DslWorkflowDefinition` as public methods (see [Runtime API Extensions](#runtime-api-extensions)). The CLI then calls `definition.Method()` for every operation and only handles formatting.

### Mode separation

| Aspect | REPL | One-shot |
|--------|------|----------|
| Invocation | `smcli --file path.sm` | `smcli --file path.sm <command> [args]` |
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
| `--file <path.sm>` | Path to `.sm` definition file (required) |
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
- **Runtime API:** `definition.GetAvailableEvents(instance.CurrentState)` + `definition.Inspect(instance, eventName)` for each.

#### `inspect <event> [json]`

Non-mutating evaluation of an event against the current instance.

- **REPL (no json):** Prompt for each required event argument using field metadata from `definition.Events[n].Args`.
- **REPL (with json):** Parse inline JSON as event arguments.
- **One-shot:** JSON argument object required (or empty `{}` for no-arg events).
- **Runtime API:** `definition.Inspect(instance, eventName, coercedArgs)`
- **Output:** Outcome, target state (if accepted), reasons (if blocked/rejected), required argument keys.

#### `fire <event> [json]`

Mutating event execution. Same argument handling as `inspect`.

- **Runtime API:** `definition.Fire(instance, eventName, coercedArgs)` — update `instance = result.UpdatedInstance` on success.
- **Output:** Outcome, previous state → new state (if accepted), reasons (if blocked/rejected).
- **REPL prompt update:** `MachineName[NewState]>` after successful fire.

#### `data`

Print the current instance data.

- **REPL:** Pretty-printed, colorized JSON.
- **One-shot:** JSON to stdout.
- **Runtime API:** `definition.SerializeInstanceData(instance)` — returns clean dictionary with collection fields as arrays, no `__collection__` prefix.

#### `data load [@path | json]`

Reset and hydrate the instance with provided data.

- **`@path`:** Read JSON from file.
- **Inline json:** Parse directly.
- **REPL (no argument):** Prompt field-by-field using `definition.DataFields` and `definition.CollectionFields` metadata.
- **Runtime API:** `definition.DeserializeInstanceData(parsed)` → `definition.CreateInstance(deserialized)`.

#### `data save [@path]`

Write current instance data as JSON.

- **`@path`:** Write to file.
- **No argument:** Write to stdout.
- **Runtime API:** `definition.SerializeInstanceData(instance)` → JSON serialize.

#### `rules`

Evaluate all current rules against the instance.

- **REPL:** List of violated rules with reasons, or "All rules satisfied."
- **One-shot:** JSON array of violation strings.
- **Runtime API:** `definition.EvaluateCurrentRules(instance)`

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
| 3 | Parse / compile error in `.sm` file |
| 4 | Runtime or input error (bad JSON, file not found) |

### One-shot Data Flow

```
stdin JSON ──► hydrate instance ──► execute command ──► stdout result ──► exit with code
```

When stdin is not a TTY and no explicit `data load` is given, stdin is read as JSON and used to hydrate the instance before executing the command.

---

## Runtime API Extensions

These methods will be added to `DslWorkflowDefinition` in `src/StateMachine/Dsl/StateMachineDslRuntime.cs` to keep the CLI (and `SmPreviewHandler`) free of domain logic.

### `GetAvailableEvents`

```csharp
/// <summary>
/// Returns the distinct event names that have at least one transition or terminal rule
/// from the specified state, ordered by event declaration order.
/// </summary>
public IReadOnlyList<string> GetAvailableEvents(string state)
```

Uses the existing private `_transitionMap` and `_terminalRuleMap` to find events reachable from the given state. Orders by declaration position in `Events`.

**Consumers:** CLI `events` command, `SmPreviewHandler.BuildSnapshot`.

### `CoerceEventArguments`

```csharp
/// <summary>
/// Coerces raw event argument values (strings from CLI input, JsonElement from JSON parsing)
/// to the runtime types declared in the event's argument contract.
/// Returns null if input is null.
/// </summary>
public IReadOnlyDictionary<string, object?>? CoerceEventArguments(
    string eventName,
    IReadOnlyDictionary<string, object?>? args)
```

Walks each argument, looks up the `DslFieldContract` in `_eventArgContractMap`, and coerces:
- String → `double` for `DslScalarType.Number`
- String → `bool` for `DslScalarType.Boolean`
- `JsonElement` → unwrapped primitive
- Passthrough for already-correct types

**Consumers:** CLI `inspect`/`fire` argument handling, `SmPreviewHandler.HandleFire`/`HandleInspect`.

### `SerializeInstanceData`

```csharp
/// <summary>
/// Returns a clean dictionary suitable for JSON serialization. Collection fields stored
/// internally under __collection__ keys are remapped to their declared field names with
/// values converted to List&lt;object&gt; via ToSerializableList().
/// </summary>
public Dictionary<string, object?> SerializeInstanceData(DslWorkflowInstance instance)
```

Encapsulates the `__collection__` internal keying convention so consumers never see it.

**Consumers:** CLI `data`/`data save`, `SmPreviewHandler.BuildSnapshot` data payload.

### `DeserializeInstanceData`

```csharp
/// <summary>
/// Takes a flat dictionary (as from JSON deserialization) where collection fields are
/// represented as arrays, and returns a dictionary with CollectionValue objects under
/// __collection__ keys, suitable for passing to CreateInstance().
/// </summary>
public IReadOnlyDictionary<string, object?> DeserializeInstanceData(
    IDictionary<string, object?> data)
```

The inverse of `SerializeInstanceData`. Recognizes collection field names, creates `CollectionValue` instances, and loads items via `LoadFrom()`.

**Consumers:** CLI `data load`, any future host that needs to hydrate from JSON.

### Impact on SmPreviewHandler

Once these four methods exist, `SmPreviewHandler` can be simplified:

| Current code | Replacement |
|-------------|-------------|
| `outgoingEventNames` filtering block (~15 lines) | `definition.GetAvailableEvents(instance.CurrentState)` |
| `CoerceEventArgs` method (~40 lines) | `definition.CoerceEventArguments(eventName, args)` |
| `BuildSnapshot` data copy line | `definition.SerializeInstanceData(instance)` |

Total reduction: ~60–80 lines of duplicated logic removed from `SmPreviewHandler`.

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
tools/StateMachine.Dsl.Cli/
├── StateMachine.Dsl.Cli.csproj
├── Program.cs
└── ...
```

References `src/StateMachine/StateMachine.csproj` as a project reference.

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
| Load `.sm` file | `StateMachineDslParser.Parse(text)` | `DslMachine` |
| Compile | `DslWorkflowCompiler.Compile(machine)` | `DslWorkflowDefinition` |
| Create instance | `definition.CreateInstance(data?)` | `DslWorkflowInstance` |
| `state` | `instance.CurrentState` | `string` |
| `events` | `definition.GetAvailableEvents(state)` | `IReadOnlyList<string>` |
| `inspect` | `definition.Inspect(instance, event, args?)` | `DslInspectionResult` |
| `fire` | `definition.Fire(instance, event, args?)` | `DslInstanceFireResult` |
| `data` | `definition.SerializeInstanceData(instance)` | `Dictionary<string, object?>` |
| `data load` | `definition.DeserializeInstanceData(json)` → `definition.CreateInstance(data)` | `DslWorkflowInstance` |
| `data save` | `definition.SerializeInstanceData(instance)` | `Dictionary<string, object?>` |
| `rules` | `definition.EvaluateCurrentRules(instance)` | `IReadOnlyList<string>` |
| Compatibility | `definition.CheckCompatibility(instance)` | `DslInstanceCompatibilityResult` |
| Coerce args | `definition.CoerceEventArguments(event, args)` | `IReadOnlyDictionary<string, object?>?` |

---

## Open Questions

1. **Tab completion in REPL** — Should `RadLine` autocomplete event names and field names? Deferred to implementation phase.
2. **`--script` mode** — Execute script files non-interactively. Accept `--script` alongside `--file`. Planned but not in initial scope.
3. **`--state` override** — Allow one-shot mode to start from a non-initial state. Low priority.

---

## Implementation Prompt

> Use this prompt verbatim to start a new implementation session.

---

Implement the `smcli` CLI tool for the StateMachine DSL runtime. Work through the tasks in order. Do not skip ahead. After each phase, run `dotnet build` and fix any errors before proceeding.

**Repository:** `c:\Users\Shane.Falik\source\repos\StateMachine`
**Design doc:** `docs/CliDesign.md` — read this in full before starting.

### Phase 1 — Runtime API extensions

Add four public methods to `DslWorkflowDefinition` in `src/StateMachine/Dsl/StateMachineDslRuntime.cs`. Do not change any existing method signatures or behavior.

**`GetAvailableEvents(string state) → IReadOnlyList<string>`**
Return distinct event names that have at least one entry in `_transitionMap` or `_terminalRuleMap` for the given state. Order results by declaration position in the `Events` list (use the list index as sort key), then alphabetically as a tiebreak. If the state is unknown, return an empty list.

**`CoerceEventArguments(string eventName, IReadOnlyDictionary<string, object?>? args) → IReadOnlyDictionary<string, object?>?`**
Return `null` if `args` is null. Look up each argument key in `_eventArgContractMap[eventName]` to get its `DslFieldContract`. Coerce values as follows:
- If the value is `System.Text.Json.JsonElement`, unwrap it: String→string, Number→double, True/False→bool, Null→null.
- After unwrapping, coerce to the declared `DslScalarType`: Number→`Convert.ToDouble`, Boolean→`bool` (handle "true"/"false" strings), String→`ToString()`, Null→`null`.
- Unknown argument keys pass through unchanged.
- This method must not throw; return the input value unchanged if coercion is impossible.

**`SerializeInstanceData(DslWorkflowInstance instance) → Dictionary<string, object?>`**
Return a clean dictionary for JSON serialization. Walk `instance.InstanceData`:
- Keys matching `__collection__<name>` → remap to `<name>`, call `(CollectionValue)value).ToSerializableList()`.
- All other keys → pass through as-is.
The result contains no `__collection__` keys.

**`DeserializeInstanceData(IDictionary<string, object?> data) → IReadOnlyDictionary<string, object?>`**
Inverse of `SerializeInstanceData`. For each key in `data`:
- If the key matches a name in `CollectionFields` and the value is `IEnumerable` (but not string) → create a `CollectionValue(kind, innerType)`, call `LoadFrom(items)`, store under `__collection__<name>`.
- Otherwise → store the key/value directly.
Returns a new `Dictionary<string, object?>` with `StringComparer.Ordinal`.

After adding these methods, add corresponding tests in `test/StateMachine.Tests/` — verify `GetAvailableEvents` returns correct events per state, `CoerceEventArguments` handles string-to-number and JsonElement unwrapping, `SerializeInstanceData` round-trips through `DeserializeInstanceData` for a machine with both scalar and collection fields.

### Phase 2 — SmPreviewHandler refactor

Simplify `tools/StateMachine.Dsl.LanguageServer/SmPreviewHandler.cs` to use the new runtime methods. Do not change any behavior — this is a pure refactor.

- Replace the `outgoingEventNames` filtering block in `BuildSnapshot` with `session.Definition.GetAvailableEvents(session.Instance.CurrentState)`.
- Replace all calls to the private `CoerceEventArgs` method with `session.Definition.CoerceEventArguments(eventName, args)`. Delete the private `CoerceEventArgs`, `CoerceValue`, `CoerceToNumber`, `CoerceToBoolean`, and `UnwrapJsonElement` methods from `SmPreviewHandler`.
- Replace the `new Dictionary<string, object?>(session.Instance.InstanceData, ...)` line in `BuildSnapshot` with `session.Definition.SerializeInstanceData(session.Instance)`.

Build and run all existing tests after this phase.

### Phase 3 — CLI project scaffold

Create `tools/StateMachine.Dsl.Cli/StateMachine.Dsl.Cli.csproj`:
- Target `net10.0`, `Exe` output type.
- Project reference to `../../src/StateMachine/StateMachine.csproj`.
- NuGet references: `System.CommandLine` (prerelease), `Spectre.Console`, `Spectre.Console.Json`, `RadLine`.
- Add to `StateMachine.slnx`.

### Phase 4 — CLI implementation

Implement `tools/StateMachine.Dsl.Cli/Program.cs` and supporting files. Follow the thin-host principle: **no domain logic in CLI files** — only argument parsing, `definition.Method()` calls, and output formatting.

**Global setup:**
- Parse `--file <path>` (required), `--no-color`, `--verbose` using `System.CommandLine`.
- On startup: read `.sm` file, call `StateMachineDslParser.Parse`, `DslWorkflowCompiler.Compile`, `definition.CreateInstance()`. On parse/compile error, write to stderr and exit with code 3.
- Detect mode: if a subcommand is present → one-shot; otherwise → REPL.

**REPL mode:**
- Use `RadLine` for line input with history.
- Prompt format: `MachineName[CurrentState]> ` (use Spectre.Console markup for color).
- Parse each line as `command [arg] [json]`. Blank lines and `#`-prefixed lines are ignored.
- On blocked/undefined outcome: print warning, do not exit.
- Commands: `state`, `events`, `inspect <event> [json]`, `fire <event> [json]`, `data`, `data load [@path|json]`, `data save [@path]`, `rules`, `help`, `exit`.
- When `inspect` or `fire` is given without a JSON argument, prompt for each required event argument interactively using `Spectre.Console`'s `Ask<T>` (use `definition.Events` to find arg names/types, skip nullable args the user leaves blank).

**One-shot mode:**
- If stdin is not a TTY, read it as JSON and call `definition.DeserializeInstanceData` + `definition.CreateInstance` to hydrate the instance before running the command.
- Serialize output as JSON to stdout using `System.Text.Json.JsonSerializer` with `WriteIndented = true`.
- Exit with the appropriate code (0–4) based on the outcome.

**Output formatting (REPL):**
- `state` → plain text line.
- `events` → formatted list; prefix each event with a colored indicator: green `●` for enabled, yellow `●` for noTransition, red `●` for blocked, dim `○` for undefined.
- `inspect`/`fire` → outcome line (colored), target/previous→new state, reasons list if non-empty.
- `data` → use `Spectre.Console.Json` for pretty-printed colorized JSON.
- `rules` → violations as a red list, or green "All rules satisfied."

**Exit code mapping:**
- `DslOutcomeKind.Enabled` or `NoTransition` → 0
- `DslOutcomeKind.Blocked` → 1
- `DslOutcomeKind.Undefined` → 2
- Parse/compile error → 3
- Runtime or input error → 4

### Phase 5 — Integration test

Run the CLI against every `.sm` file in `samples/` using the script format from `samples/traffic.script.txt`. For each sample, at minimum: load the machine, print `state`, print `events`, inspect each available event, fire at least one enabled event, print the new state. Report any failures.

Replay `samples/traffic.script.txt` against `samples/trafficlight.sm` and verify it completes with exit code 0.
