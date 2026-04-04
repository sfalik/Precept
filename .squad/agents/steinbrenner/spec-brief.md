# Precept Language Spec Brief

**Date:** 2026-03-27  
**Reviewed:** 9 core design docs + implementation plan  
**Current Status:** Language feature-complete; core runtime implemented; type-checker expanded (Phases D–H complete); graph analysis added (Phase I complete); MCP server redesigned to 5 tools; Copilot plugin and skills authored.

---

## 1. Language Surface — Full DSL Feature Inventory

### Declarations (Keyword-Anchored, No Indentation)

| Construct | Syntax | Purpose |
|---|---|---|
| **Precept header** | `precept <Name>` | Top-level workflow name |
| **Field (scalar)** | `field <Name>[, <Name>] as <Type> [nullable] [default <Value>]` | Data field: string, number, boolean (scalar or nullable) |
| **Field (collection)** | `field <Name>[, <Name>] as set\|queue\|stack of <ScalarType> [default [items]]` | Unordered set, FIFO queue, LIFO stack; empty by default |
| **State** | `state <Name>[, <Name>] [initial]` | Workflow state; exactly one marked `initial` |
| **Event** | `event <Name>[, <Name>] [with <Arg> as <Type> [nullable] [default <Val>], ...]` | Triggerable event with optional typed arguments |
| **Invariant** | `invariant <BoolExpr> because "<Reason>"` | Data constraint always holds post-commit; scope: fields only |
| **Edit declaration** | `in <any\|State[,State]> edit <Field>[, <Field>]` | Which fields are directly editable in which states |

### Asserts — Movement-Scoped Constraints

| Form | Scope | When Checked | Preposition Meaning |
|---|---|---|---|
| `in <State> assert` | Fields only | Post-mutation, when resulting state is named state (entry + no-transition) | While residing in this state |
| `to <State> assert` | Fields only | Post-mutation, only when crossing **into** this state from different state | Upon entering this state |
| `from <State> assert` | Fields only | Post-mutation, only when crossing **out of** this state to different state | Before leaving this state |
| `on <Event> assert` | Event args only | Pre-transition, before row selection | When this event fires |

All asserts require `because "<Reason>"`. Multi-state targets supported for `in/to/from` (e.g., `in Open, InProgress assert`); `in any` expands to all states.

### State Actions — Automatic Mutations on State Change

| Form | When Executed |
|---|---|
| `to <State> -> <ActionChain>` | Upon entering target state (during transition or self-transition) |
| `from <State> -> <ActionChain>` | Upon leaving source state (during cross-state transition) |

No `in` actions (staying in-place mutations would be surprising). Actions use same pipeline as transition rows: `set`, collection operations, no outcomes. Multi-state support (`from Open, InProgress ->`).

### Transitions — Event Routing with First-Match Evaluation

```
from <StateTarget> on <EventName> [when <BoolExpr>] [-> <ActionChain>]* -> <Outcome>
```

- **Routing:** `from` + `on` select state(s) and event
- **Guard:** `when <BoolExpr>` optional; if false, skip this row (try next)
- **Actions:** `->` chains of mutations: `set`, `add`, `remove`, `enqueue`, `dequeue`, `push`, `pop`, `clear`
- **Outcomes:** 
  - `transition <State>` — move to named state
  - `no transition` — stay in current state (but data may change)
  - `reject "<reason>"` — explicit rejection

Multi-state `from any on Event -> outcome` supported. First matching row wins; no catch-all required.

### Types

| Scalar | Collection |
|---|---|
| `string` (unicode) | `set of string` (unordered, no dups) |
| `number` (double-precision float) | `queue of number` (FIFO ordered) |
| `boolean` | `stack of string` (LIFO ordered) |

All types support `nullable` modifier. Scalars have required `default <Literal>` unless nullable (nullable defaults to null). Collections default to empty.

### Expression Language

**Operators (left-to-right evaluation):**
- Logical: `&&` (AND), `||` (OR), `!` (NOT)
- Comparison: `==`, `!=`, `>`, `>=`, `<`, `<=`
- Arithmetic: `+`, `-`, `*`, `/`, `%`
- Membership: `contains` (infix, e.g., `Tags contains "urgent"`)
- Grouping: `( … )`

**Identifier Forms:**
- Bare field/arg names in scoped contexts (event asserts)
- **Dotted form** `EventName.ArgName` in transition rows (required to avoid shadowing fields)
- **Accessors:** `.count` (all collections), `.min`/`.max` (sets), `.peek` (queue/stack)

### Comments
- `# comment` — full-line or inline; stripped before parsing

### Keyword Inventory (Case-Sensitive, Lowercase)

**Control:** `precept`, `state`, `initial`, `from`, `on`, `in`, `to`, `when`, `any`, `edit`

**Declaration:** `field`, `as`, `nullable`, `default`, `invariant`, `because`, `event`, `with`, `assert`, `edit`

**Actions:** `set`, `add`, `remove`, `enqueue`, `dequeue`, `push`, `pop`, `clear`, `into`

**Outcomes:** `transition`, `no`, `reject`

**Types:** `string`, `number`, `boolean`, `set`, `queue`, `stack`, `of`

**Literals:** `true`, `false`, `null`

---

## 2. What's Implemented vs. Planned

| Feature | Status | Notes |
|---|---|---|
| **Core DSL parsing** | ✅ Done | Superpower-based tokenizer + Superpower combinators; zero indentation; flat keyword-anchored statements |
| **Field declarations** | ✅ Done | Scalar + collection; multi-name syntax; nullable; defaults |
| **States & initial marking** | ✅ Done | Multi-name syntax; exactly one initial |
| **Events & arguments** | ✅ Done | Multi-name syntax; `with` keyword for args; typed args with defaults |
| **Invariants** | ✅ Done | Always-hold data constraints; field-scoped; checked post-commit |
| **State asserts** | ✅ Done | `in`/`to`/`from` preposition-scoped; checked post-mutation, pre-commit |
| **Event asserts** | ✅ Done | Arg-only scope; checked pre-transition |
| **State actions** | ✅ Done | `to`/`from` entry/exit actions with mutation chains |
| **Transition rows** | ✅ Done | First-match evaluation; `when` guards; action chains; three outcome types |
| **Edit declarations** | ✅ Done | Parser + model; `in <State> edit <Fields>` syntax; runtime `Update` API fully implemented |
| **Type checking** | ✅ Done | Compile-time expression validation; null-flow narrowing; scalar type checking; collection inner-type enforcement |
| **Equality rules** | ✅ Done | Same-family only; no cross-type coercion; `== null` valid only for nullable operands (Phase D) |
| **Event-arg scope hardening** | ✅ Done | Transition rows require dotted form; bare args rejected; cross-event refs rejected (Phase E) |
| **Non-boolean rule strictness** | ✅ Done | Invariants, asserts, guards must produce boolean (Phase F) |
| **Identical-guard duplicate detection** | ✅ Done | C47 error when two rows for same (state, event) have identical guards (Phase G) |
| **Constraint violation structuring** | ✅ Done | `ConstraintViolation(Message, Source, Targets[])`; source kinds: Invariant / StateAssertion / EventAssertion / TransitionRejection |
| **Graph analysis warnings** | ✅ Done | C48–C53: unreachable states, orphaned events, dead-end states, reject-only pairs, events that never succeed, empty precepts (Phase I) |
| **MCP server** | ✅ Done | 5 tools: `precept_language` (reference), `precept_compile` (validation), `precept_inspect` (read-only possibility map), `precept_fire` (single-event execution), `precept_update` (direct field editing) |
| **VS Code extension** | ✅ Done | Syntax highlighting, language server, preview webview, commands |
| **Copilot plugin** | ✅ Done | Agent definition, 2 skills (authoring + debugging), MCP launcher |
| **Sample files** | ✅ Done | 20 example `.precept` files covering major language features |
| **CLI** | 📋 Planned | Design exists; implementation deferred pending other priorities |
| **Fluent interface** | 📋 Planned | Runtime API could expose fluent-style builder (low priority) |
| **Same-preposition contradiction detection** | 📋 Planned | Two asserts on same state with provably empty per-field domains (interval/set analysis) |
| **Cross-preposition deadlock detection** | 📋 Planned | `in`/`to` vs `from` on same state making it unexitable |

---

## 3. Key Design Decisions — The "Why"

### 3.1 Flat, Keyword-Anchored Syntax (Not Indentation-Based)

**Why:** Every statement begins with a keyword, not indentation level. Enables:
- Deterministic, stateless parsing (no indentation context stack)
- LSP-friendly tokenization and error recovery
- First-class AI authoring (AI doesn't track indentation depth)
- Consistent structure across precept declarations, asserts, transitions, editable fields

### 3.2 Four Assert Kinds, Not One

**Why:** Different temporal scopes (entry vs. exit vs. residing vs. event-args) require distinct semantics:
- `in` → checked on entry **and** self-transition (state invariant)
- `to` → checked **only** on entry from different state (entry gate)
- `from` → checked **only** on exit to different state (exit gate)
- `on` → checked **before** transition selection (input validation)

Collapsed into one would lose expressivity and confuse authorship.

### 3.3 Prepositions Over Keywords

**Why:** `in`/`to`/`from` carry consistent meaning everywhere (state references). Uniform across asserts, actions, transitions. Reduces keyword bloat.

### 3.4 State Actions (Entry/Exit Automatic Mutations)

**Why:** Some state entry/exit logic should fire automatically (audit logging, timestamp recording, derived field updates). Including them as side effects during state transitions makes them visible and controllable — no hidden behavior.

### 3.5 First-Match Transitions (Not Exclusive Clauses)

**Why:** Multiple rows for same (state, event) with different guards is a common pattern:
- Guarded row with special logic
- Unguarded fallback

First-match is simpler than "exclusive" semantics and requires no guard coverage proof. Clear evaluation order: top-to-bottom, first match wins.

### 3.6 `when` Guards Are Routing Logic, Not Constraints

**Why:** Distinguish:
- **Routing** (`when`) — which row fires for this input? (decision tree)
- **Constraint** (`assert`/`invariant`) — what must always be true? (safety net)

Mixing them conflates "does this row apply?" with "is the state valid?" Both are needed, distinct purposes.

### 3.7 Editable Fields Decouple Data Editing from Events

**Why:** "Add a note" and "complete the task" are different kinds of changes:
- Events = lifecycle actions (routing, audit, state transitions)
- Edits = data corrections (state-scoped, invariant-enforced, no routing)

Forcing both through the event pipeline adds ceremony without value.

### 3.8 Strict Null Model

**Why:** Nullable fields without explicit null handling are a class of bugs. Enforced compile-time:
- Nullable fields must declare `nullable` modifier
- `null` comparisons valid only with nullable operands
- Non-null assignments require prior narrowing in guards

Forces authors to be explicit about which fields can be missing.

### 3.9 Expression Type Checking at Compile Time

**Why:** Workflow bugs should be caught before execution, not discovered via preview. Type checking is product feature, not implementation detail.

### 3.10 Constraint Violation Structure (Source + Targets)

**Why:** Violations include what failed (Source: invariant / assert / rejection) and what it implicates (Targets: fields / args / event / state / definition). Enables:
- Preview UI to highlight inline for field targets, banner for others
- CLI to attribute errors precisely
- API callers to route feedback semantically

---

## 4. API Surface — The C# Contract

### Parsing

```csharp
public static class PreceptParser
{
    // Throws InvalidOperationException on syntax errors
    public static PreceptDefinition Parse(string text);

    // Returns model + diagnostics; never throws
    public static (PreceptDefinition? Model, IReadOnlyList<ParseDiagnostic> Diagnostics) 
        ParseWithDiagnostics(string text);
        
    // For expression analysis (language server internal use)
    public static PreceptExpression ParseExpression(string expression);
}
```

### Compilation

```csharp
public static class PreceptCompiler
{
    // Throws if any error-severity diagnostics
    public static PreceptEngine Compile(PreceptDefinition model);

    // Returns all diagnostics (errors block compilation, warnings/hints don't)
    public static CompileResult Compile(string text);  // or CompileFromText
}

public sealed record CompileResult(
    bool IsValid,  // HasErrors == false
    PreceptDefinition? Definition,
    PreceptEngine? Engine,
    IReadOnlyList<ValidationDiagnostic> Diagnostics);
```

### Engine (Immutable, Thread-Safe)

```csharp
public sealed class PreceptEngine
{
    // Properties (read-only)
    public string Name { get; }
    public IReadOnlyList<string> States { get; }
    public string InitialState { get; }
    public IReadOnlyList<PreceptEvent> Events { get; }
    public IReadOnlyList<PreceptField> Fields { get; }
    public IReadOnlyList<PreceptCollectionField> CollectionFields { get; }
    public IReadOnlyList<PreceptEditableFieldInfo>? EditableFields { get; }

    // Instance Creation
    public PreceptInstance CreateInstance(
        IReadOnlyDictionary<string, object?>? data = null);

    public PreceptInstance CreateInstance(
        string initialState,
        IReadOnlyDictionary<string, object?>? data = null);

    // Compatibility
    public PreceptCompatibilityResult CheckCompatibility(PreceptInstance instance);

    // Discovery (Read-Only Simulation)
    public EventInspectionResult Inspect(
        PreceptInstance instance,
        string eventName,
        IReadOnlyDictionary<string, object?>? eventArgs = null);

    public InspectionResult Inspect(PreceptInstance instance);

    public InspectionResult Inspect(
        PreceptInstance instance,
        Action<IUpdatePatchBuilder> patch);  // Hypothetical patch, no commit

    // Execution
    public FireResult Fire(
        PreceptInstance instance,
        string eventName,
        IReadOnlyDictionary<string, object?>? eventArgs = null);

    // Direct Field Editing
    public UpdateResult Update(
        PreceptInstance instance,
        Action<IUpdatePatchBuilder> patch);

    // Coercion (JSON → Typed)
    public IReadOnlyDictionary<string, object?>? CoerceEventArguments(
        string eventName,
        IReadOnlyDictionary<string, object?>? args);
}
```

### Instance (Immutable Value)

```csharp
public sealed record PreceptInstance(
    string WorkflowName,
    string CurrentState,
    string? LastEvent,
    DateTimeOffset UpdatedAt,
    IReadOnlyDictionary<string, object?> InstanceData);
```

### Fire Results

```csharp
public enum TransitionOutcome
{
    Transition,         // State changed
    NoTransition,       // State unchanged, data may change
    Rejected,           // Explicit `reject` outcome or event assert failed
    ConstraintFailure,  // Invariant/assert violation (rolled back)
    Unmatched,          // No row matched (all `when` guards failed)
    Undefined           // Event or state unknown
}

public sealed record FireResult(
    TransitionOutcome Outcome,
    string PreviousState,
    string EventName,
    string? NewState,
    IReadOnlyList<ConstraintViolation> Violations,
    PreceptInstance? UpdatedInstance);
```

### Update Results

```csharp
public enum UpdateOutcome
{
    Update,              // Fields modified, all constraints passed
    ConstraintFailure,   // Constraints violated (rolled back)
    UneditableField,     // One or more fields not editable in current state
    InvalidInput         // Type mismatch, unknown field, or patch conflict
}

public sealed record UpdateResult(
    UpdateOutcome Outcome,
    IReadOnlyList<ConstraintViolation> Violations,
    PreceptInstance? UpdatedInstance);

public interface IUpdatePatchBuilder
{
    IUpdatePatchBuilder Set(string fieldName, object? value);
    IUpdatePatchBuilder Add(string fieldName, object value);
    IUpdatePatchBuilder Remove(string fieldName, object value);
    IUpdatePatchBuilder Enqueue(string fieldName, object value);
    IUpdatePatchBuilder Dequeue(string fieldName);
    IUpdatePatchBuilder Push(string fieldName, object value);
    IUpdatePatchBuilder Pop(string fieldName);
    IUpdatePatchBuilder Clear(string fieldName);
    IUpdatePatchBuilder Replace(string fieldName, IEnumerable<object> values);
}
```

### Inspection Results

```csharp
public sealed record EventInspectionResult(
    TransitionOutcome Outcome,
    string CurrentState,
    string EventName,
    string? TargetState,
    IReadOnlyList<string> RequiredEventArgumentKeys,
    IReadOnlyList<ConstraintViolation> Violations);

public sealed record InspectionResult(
    string CurrentState,
    IReadOnlyDictionary<string, object?> InstanceData,
    IReadOnlyList<EventInspectionResult> Events,
    IReadOnlyList<PreceptEditableFieldInfo>? EditableFields = null);

public sealed record PreceptEditableFieldInfo(
    string FieldName,
    string FieldType,  // Composite: "string", "set<string>", "queue<number>", etc.
    bool IsNullable,
    object? CurrentValue,
    ConstraintViolation? Violation = null);
```

### Constraint Violations

```csharp
public sealed record ConstraintViolation(
    string Message,
    ConstraintSource Source,
    IReadOnlyList<ConstraintTarget> Targets);

public abstract record ConstraintSource(ConstraintSourceKind Kind, int? SourceLine = null)
{
    public sealed record InvariantSource(
        string ExpressionText, string Reason, int? SourceLine = null)
        : ConstraintSource(ConstraintSourceKind.Invariant, SourceLine);

    public sealed record StateAssertionSource(
        string ExpressionText, string Reason,
        string StateName, AssertAnchor Anchor, int? SourceLine = null)
        : ConstraintSource(ConstraintSourceKind.StateAssertion, SourceLine);

    public sealed record EventAssertionSource(
        string ExpressionText, string Reason,
        string EventName, int? SourceLine = null)
        : ConstraintSource(ConstraintSourceKind.EventAssertion, SourceLine);

    public sealed record TransitionRejectionSource(
        string Reason, string EventName, int? SourceLine = null)
        : ConstraintSource(ConstraintSourceKind.TransitionRejection, SourceLine);
}

public enum ConstraintSourceKind
{
    Invariant,
    StateAssertion,
    EventAssertion,
    TransitionRejection
}

public abstract record ConstraintTarget(ConstraintTargetKind Kind)
{
    public sealed record FieldTarget(string FieldName)
        : ConstraintTarget(ConstraintTargetKind.Field);

    public sealed record EventArgTarget(string EventName, string ArgName)
        : ConstraintTarget(ConstraintTargetKind.EventArg);

    public sealed record EventTarget(string EventName)
        : ConstraintTarget(ConstraintTargetKind.Event);

    public sealed record StateTarget(string StateName, AssertAnchor? Anchor = null)
        : ConstraintTarget(ConstraintTargetKind.State);

    public sealed record DefinitionTarget()
        : ConstraintTarget(ConstraintTargetKind.Definition);
}

public enum ConstraintTargetKind { Field, EventArg, Event, State, Definition }
public enum AssertAnchor { In, To, From }
```

---

## 5. Constraint System — End-to-End

### Constraint Kinds

| Kind | Where | Scope | When Checked | Example |
|---|---|---|---|---|
| **Invariant** | Top-level | Fields | Always, post-commit | `invariant Balance >= 0 because "Cannot go negative"` |
| **State assert** | State-scoped | Fields | Post-mutation, before commit; timing depends on preposition | `in Open assert Assignee != null because "Must have owner"` |
| **Event assert** | Event-scoped | Event args only | Pre-transition, before row selection | `on CreateOrder assert Items.count > 0 because "Must include items"` |
| **Transition reject** | Transition row | (n/a — explicit) | When matching row's outcome is `reject` | `from Draft on Submit -> reject "Not ready yet"` |

### Fire Pipeline Order (Complete)

1. **Event asserts** (`on <Event> assert`) — arg-only, pre-transition. Violation → `Rejected`.
2. **Compatibility check** — instance state/data must match engine schema.
3. **Event arg validation** — types match event contract.
4. **First-match row selection** — evaluate `when` guards top-to-bottom. First matching row fires.
5. **Exit actions** (`from <SourceState> ->`) — automatic mutations on leaving source state.
6. **Row mutations** (`-> set ...`, `-> add ...`, etc.) — the matched row's action pipeline.
7. **Entry actions** (`to <TargetState> ->`) — automatic mutations on entering target state.
8. **Validation** — invariants + state asserts (with preposition-scoped timing) + field-level rules. Collect all violations. Any violation → rollback, `ConstraintFailure`.

For `no transition`: steps 5 & 7 skipped (not leaving/entering). `in <State>` asserts still run.

### Compile-Time Checks

| ID | Check | Severity | When |
|---|---|---|---|
| C7–C32 | Parse-time + compile-time validation (field defaults, initial state, etc.) | Error | Parse → Compile |
| C38 | Unknown identifier | Error | Type check |
| C39 | Expression type mismatch | Error | Type check |
| C40 | Unary operator type error | Error | Type check |
| C41 | Binary operator type error (includes equality rules) | Error | Type check |
| C42 | Null-flow violation | Error | Type check |
| C43 | Collection pop/dequeue target mismatch | Error | Type check |
| C44 | Non-boolean rule position | Error | Type check |
| C45–C47 | Duplicate/impossible guards, identical-guard duplicates | Error | Validation |
| C48–C53 | Graph analysis: unreachable states, orphaned events, dead-end, reject-only pairs, event never succeeds, empty precept | Warning/Hint | Validation |

---

## 6. Open Design Questions

1. **Same-preposition contradiction detection** — Two asserts on same state with provably empty per-field domains (interval/set analysis). Marked as planned but not yet implemented. Requires sophisticated domain reasoning.

2. **Cross-preposition deadlock detection** — `in`/`to` vs `from` on same state making it unexitable. Same interval/set analysis as contradiction detection.

3. **CLI design implementation** — Design exists in `docs/CliDesign.md`; execution pending. Would add command-line access to parse/compile/fire/inspect without embedding in .NET project.

4. **Fluent interface for runtime** — Builder pattern for `CreateInstance()`, `Update()`, etc. Low priority, optional ergonomic improvement.

5. **Cross-preposition same-field shadowing** — Can `in State` and `from State` on same field legally coexist, or do they conflict? (Currently no restriction.)

---

## 7. For Hero Work Specifically

### ✅ Stable & Showable

These features are fully implemented, well-tested, and appropriate for hero examples:

- **Invariants** — central feature, cleanly expressed (`invariant Balance >= 0`)
- **State transitions** — core workflow (`from Draft on Submit -> transition Submitted`)
- **Guards** (`when` conditions) — decision logic in rows
- **State asserts** (`in`, `to`, `from`) — state-scoped constraints showing "invalid states impossible"
- **Event arguments** — event-scoped data (`event UpdatePriority with Level as number`)
- **Collection mutations** — `add`, `remove`, `enqueue`, `dequeue` on sets/queues/stacks
- **Editable fields** — direct data editing in states (`in Open edit Notes, Priority`)
- **Multi-state targets** — concise state grouping (`in Open, InProgress assert`, `from any on Event`)

### ⚠️ Mature But Specialized

Implemented and working, but narrow use cases:

- **State actions** (entry/exit automatic mutations) — less common pattern; good for audit logging, timestamp updating
- **Event asserts** (arg-only pre-transition validation) — important for API validation but less demonstrative than guards
- **No-transition outcome** — data-only changes without state movement; useful for editable fields, less intuitive than transitions

### 🔴 Not Yet Stable (Avoid in Hero)

Designed but not yet implemented; would require substantial work before inclusion in hero:

- **Same-preposition contradiction detection** — intended as compile-time safety feature but interval/set analysis not yet built
- **Cross-preposition deadlock detection** — similar status
- **CLI** — design complete; execution pending
- **Fluent interface** — ergonomic improvement, not core semantic

### Recommended Hero Example Structure

A hero should demonstrate:
1. **Fields + invariants** — data model with constraints (`field Balance as number default 0`, `invariant Balance >= 0`)
2. **States + events** — workflow progression (`state Draft initial, Submitted, Closed` + `event Submit`)
3. **Transition with guards** — conditional routing (`from Draft on Submit when Amount > 0 -> transition Submitted`)
4. **Asserts showing "invalid states impossible"** — either `in`, `to`, `from`, or event asserts proving a state can never be violated

Domain: something concrete and relatable (loan application, work ticket, shipment tracking) — not fantasy. Domain should let viewers project themselves into the scenario.

**Hero length:** 10–15 lines. Demonstrate 4–5 major features. One or two clean examples of constraint enforcement.

---

## 8. PM Assessment

### Three Things That Matter Most Right Now

1. **The language is feature-complete for core workflows.** Parsing, typing, runtime, inspector, editing — all done. The DSL is production-ready for adoption. What matters now is distribution: the Copilot plugin works, sample files exist, but the stories that make adoption frictionless (CLI, marketing, integrations) are pending.

2. **Type safety is a strategic advantage.** Phase D–H took the compile-time checking from "parse succeeds" to "type-correct and semantically sound." The equality rules, null-flow narrowing, and identical-guard detection prevent entire classes of bugs at author time. This differentiates Precept from hand-written state machines or simpler DSLs. Keep leaning into this.

3. **Constraint violations are now structured, not flat.** The shift from `IReadOnlyList<string> Reasons` to `ConstraintViolation(Source, Targets[])` is complete in the runtime but not yet surfaced in all consumers (preview UI, CLI). This is the bridge to rich error attribution — showing developers *which field* is the problem, not just "something failed." Completing this work (preview UI update, CLI implementation) will dramatically improve the authoring experience.

**Shipping implication:** The language is ready. Pick a hero example (loan application? shipment workflow?), land it, and start talking about when/where teams should use Precept. The technical foundation is sound.

