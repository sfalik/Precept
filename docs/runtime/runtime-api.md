# Runtime API

## Status

| Property | Value |
|---|---|
| Doc maturity | Design — public surface locked; entity internals (R3) pending |
| Implementation state | Partial stub (`Precept.cs` and `Version.cs` exist; all operation bodies throw `NotImplementedException`) |
| Source | `src/Precept/Runtime/Precept.cs`, `src/Precept/Runtime/Version.cs` |
| Upstream | Compiler pipeline (`Compilation`) |
| Downstream | Host applications, language server, MCP tools |

---

## Overview

The runtime API is the boundary between Precept and host applications. It has four concerns:

1. **Construction** — compile source → build executable model → create initial entity via `Create` / `InspectCreate`
2. **Restoration** — reconstitute an entity from persisted data via `Restore` (validated, not trusted)
3. **Operations** — commit changes via `Fire` and `Update`
4. **Inspection** — progressive evaluation via `InspectFire` and `InspectUpdate`

The API surface is deliberately small. Two types carry the entire public contract: `Precept` (the executable model, definition-level queries, construction) and `Version` (entity snapshot, instance-level operations and inspection). `Precept` mirrors `Version`'s commit/inspect pattern: `Create` / `InspectCreate` parallel `Fire` / `InspectFire`.

### Two-Lane Ingress Principle

**The runtime API accepts two and only two ingress shapes for all mutable operations — no more, no less.**

- **JSON lane** (`JsonElement?`) — for wire callers: MCP tools, HTTP APIs, deserialization pipelines. Callers that have a JSON string use `JsonDocument.Parse(str).RootElement` inline. No string-input convenience overloads exist.
- **Typed lane** (`Action<IArgBuilder>?` for event args; `Action<IFieldBuilder>?` for field patches) — for in-process callers that work with typed CLR values. Zero-boxing via `TypeRuntime<T>`.

**There are no `IReadOnlyDictionary<string, object?>` overloads anywhere.** That provisional convenience lane is fully obsolete and superseded by these two lanes.

**Restore is JSON-only.** `Restore(string? state, JsonElement fields)` has no typed overload. Restore is a hydration path from persisted storage — storage always provides serialized data, so in-process callers never need a typed Restore lane.

**Typed output via `PreceptValue`.** Field values read back from `Version` (via the indexer or `FieldAccess`) are `PreceptValue`. Convert via `ToClr<T>()` or `ToJson()`. `FiredArgs` carries submitted arg values for egress using the same `Get<T>()` pattern.

---

## Responsibilities and Boundaries

**OWNS:** Public API surface (`Precept` and `Version`); executable model lifecycle (construction from `Compilation`, validation); entity construction (`Create`, `InspectCreate`); entity restoration (`Restore`); operation dispatch (`Fire`, `Update`); inspection surface (`InspectFire`, `InspectUpdate`); definition-level structural queries (`States`, `Fields`, `Events`, `Constraints`); constraint exposure model (all three tiers); field access surface (`FieldAccess`, `AvailableEvents`, `RequiredArgs`, `ApplicableConstraints`).

**Does NOT OWN:** Compilation (owned by the compiler pipeline); evaluation logic (owned by the `Evaluator`); executable model internals / dispatch table layouts (pending D8/R4); result type definitions (`EventOutcome.cs`, `UpdateOutcome.cs`, `RestoreOutcome.cs`); descriptor type definitions (`Descriptors.cs`); fault system (`FaultException` and fault site taxonomy).

---

## Right-Sizing

The runtime API surface is deliberately minimal: two types (`Precept` and `Version`), four commit operations (`Create`, `Restore`, `Fire`, `Update`), and parallel inspection variants (`InspectCreate`, `InspectFire`, `InspectUpdate`). Definition-level queries are precomputed structural reads — zero evaluation cost. The boundary between `Precept` and `Version` is clean: `Precept` carries everything definition-scoped; `Version` carries everything instance-scoped. Internal evaluation complexity lives entirely in the `Evaluator` behind `Version`'s thin facade.

---

## Inputs and Outputs

**Inputs:**
- `Compilation` → `Precept.From()` — the compiled artifact; must be error-free
- `state`, `fields` → `Precept.Restore()` — persisted entity state for validated reconstruction (`JsonElement` only)
- `args` → `Precept.Create()` / `Version.Fire()` — event arguments; `JsonElement?` (JSON lane) or `Action<IArgBuilder>?` (typed lane)
- `fields` → `Version.Update()` — field patch; `JsonElement?` (JSON lane) or `Action<IFieldBuilder>?` (typed lane), applied atomically
- `eventName`, `args?` → `Version.InspectFire()` — event name with optional arg refinement; both lanes
- `fields?` → `Version.InspectUpdate()` — optional hypothetical field patch; both lanes

**Outputs:**
- `Precept` — the executable model; returned from `Precept.From()`
- `EventOutcome` (7 variants) — returned from `Create`, `Fire`; see `result-types.md`
- `RestoreOutcome` (3 variants) — returned from `Restore`; see `result-types.md`
- `UpdateOutcome` (4 variants) — returned from `Update`; see `result-types.md`
- `EventInspection` — returned from `InspectCreate`, `InspectFire`; full annotated landscape
- `UpdateInspection` — returned from `InspectUpdate`; annotated landscape with event prospects

---

## Construction

### Compilation → Executable Model

```csharp
Compilation compilation = Compiler.Compile(source);
Precept precept = Precept.From(compilation);
```

`Precept.From(Compilation)` is the Precept Builder stage: it transforms the analysis-oriented compilation into a runtime-optimized executable form. Construction only succeeds when the compilation has no errors — on broken input, the LS and MCP consume the `Compilation` directly.

If construction fails despite an error-free compilation result, that is a compiler bug — not a user-facing condition.

**Survey grounding:** CEL's `env.Program(ast)` — lowering is the Program constructor. OPA's `rego.PrepareForEval(ctx)` — lowering is inside preparation. XState's `createMachine()` — machine creation IS the lowering.

### Executable Model → Initial Entity

```csharp
// Precepts WITH an initial event — args required

// JSON lane (wire callers: MCP, HTTP APIs, deserialization)
EventOutcome outcome = precept.Create(
    JsonDocument.Parse("""{"Amount":500.00,"ApplicantName":"Jane Doe","CreditScore":720}""").RootElement);

// Typed lane (in-process callers)
EventOutcome outcome = precept.Create(
    args => args.Set<decimal>("Amount", 500.00m)
               .Set<string>("ApplicantName", "Jane Doe")
               .Set<int>("CreditScore", 720));

Version version = outcome switch
{
    Transitioned t            => t.Result,     // initial event transitioned to another state
    Applied a                 => a.Result,     // stayed in initial state
    Rejected r                => throw new BusinessException(r.Reason),
    InvalidArgs e             => throw new ArgumentException(e.Reason),
    EventConstraintsFailed f  => HandleViolations(f.Violations),
    Unmatched                 => HandleNoMatch(),
    UndefinedEvent            => throw new InvalidOperationException(),  // compiler prevents this
};

// Precepts WITHOUT an initial event — all fields have defaults
EventOutcome simple = precept.Create();  // always Applied
```

`Create` is construction. If the precept declares an `initial` event, `Create` fires it atomically: build hollow version (defaults + initial state + omitted fields) → fire initial event with args → return `EventOutcome`. If no initial event is declared, `Create` constructs from defaults and returns `Applied` (always succeeds by compile-time guarantee C59/C86).

**Why `Create` returns `EventOutcome`:** Construction goes through the full event pipeline — same guards, same ensures, same constraint checking, same outcomes. No special construction result type. The caller uses the same pattern matching they use for every `Fire`.

**Discovery:** `precept.InitialEvent` names the initial event (or null). `precept.RequiredArgs(initialEvent)` returns typed arg descriptors — the construction contract.

**Preview:** `precept.InspectCreate(args?)` returns `EventInspection` with the same progressive resolution model as `InspectFire` — Certain/Possible/Impossible per row, constraint results, field snapshots.

**Compiler enforcement:**
- **`RequiredFieldsNeedInitialEvent`:** Precept has required fields (non-optional, no default) but does not declare an initial event — construction cannot produce a valid initial version.
- **`InitialEventMissingAssignments`:** Initial event does not assign all required fields that lack defaults — post-construction state may violate constraints.

**Open (R3):** The internal representation (slot array vs. dictionary, immutable record shape) is not yet decided. The public contract is unaffected — `Version` exposes named field access regardless of internal storage.

### Precept — Definition-Level Queries

`Precept` is the executable model. A single instance serves all entity versions of the same precept definition. It exposes the full definition-level query surface — not filtered by any entity's current state.

```csharp
public sealed class Precept
{
    // Construction
    public static Precept From(Compilation compilation);

    // Entity creation — JSON lane (wire callers)
    public EventOutcome Create(JsonElement? args = null);
    public EventInspection InspectCreate(JsonElement? args = null);

    // Entity creation — typed lane (in-process callers)
    public EventOutcome Create(Action<IArgBuilder>? args = null);
    public EventInspection InspectCreate(Action<IArgBuilder>? args = null);

    // Entity restoration — JSON only (no typed overload for Restore)
    public RestoreOutcome Restore(string? state, JsonElement fields);

    // Definition-level queries (structural — precomputed from graph analysis)
    public IReadOnlyList<string> States { get; }
    public IReadOnlyList<string> Fields { get; }
    public IReadOnlyList<string> Events { get; }
    public string? InitialState { get; }
    public string? InitialEvent { get; }             // null = no initial event, args optional
    public bool IsStateless { get; }

    // Constraint catalog (Tier 1 — all declared constraints)
    public IReadOnlyList<ConstraintDescriptor> Constraints { get; }
}
```

**Design decisions:**
- **Sealed class, not record.** `Precept` owns internal mutable-during-construction state (dispatch tables, slot arrays) that is frozen after `From()` completes. Records imply value semantics; `Precept` has reference identity.
- **Definition-level queries are structural.** They are precomputed from graph analysis during construction. No evaluator involved — zero runtime cost.
- **Single `Precept` per definition.** Thread-safe, shareable, cacheable. Mirrors CEL's `Program` (one compiled program evaluated against many activations) and OPA's `PreparedEvalQuery`.
- **Construction mirrors operations.** `Create` / `InspectCreate` parallel `Version.Fire` / `Version.InspectFire`. Same pipeline, same outcome types, same pattern matching. No special construction path.
- **`InitialEvent` nullable.** `null` means no initial event is declared — `Create()` builds from defaults (always `Applied`). Non-null means the initial event fires atomically during construction. The compiler ensures (`RequiredFieldsNeedInitialEvent`/`InitialEventMissingAssignments`) that this is coherent.
- **`InitialState` nullable.** `null` for stateless precepts.
- **Constraint catalog is definition-level.** `Precept.Constraints` exposes every declared constraint (rules, state ensures, event ensures) with full metadata: kind, scope, anchor, `because` rationale, referenced fields, guard presence. This is the full catalog — unfiltered by state. Callers use it for definition-level introspection (e.g., "what rules does this precept declare?").

---

## Restoration

```csharp
RestoreOutcome outcome = precept.Restore(
    state: "Approved",
    fields: JsonDocument.Parse("""{"Amount":500.00,"ApplicantName":"Jane Doe","CreditScore":720}""").RootElement);

Version version = outcome switch
{
    Restored r                    => r.Result,
    RestoreConstraintsFailed f    => HandleMigrationNeeded(f.Violations),
    RestoreInvalidInput e         => HandleSchemaMismatch(e.Reason),
};
```

`Restore` is validated reconstruction from persisted data. It is a distinct pipeline entry — not Fire-without-event, not Update-without-access-checks.

**Pipeline stages:**

| Stage | Fire | Update | Restore |
|-------|------|--------|---------|
| Row matching / guards | ✅ | — | — |
| Apply mutations / patches | ✅ | ✅ | ✅ (all fields from storage) |
| Set state | via `transition` | — | from caller |
| Access mode checks | N/A | ✅ | — (bypassed) |
| Recompute computed fields | ✅ | ✅ | ✅ |
| Evaluate constraints | ✅ | ✅ | ✅ |

Access modes are bypassed because persisted fields were written when the entity was in prior states — a field that is `readonly` in `Approved` was `editable` when the entity was in `Draft`. Restore accepts all stored fields regardless of the restored state's access declarations.

**Return type:** `RestoreOutcome` — three variants:
- `Restored(Version)` — data valid, constraints passed.
- `RestoreConstraintsFailed(Violations)` — constraints violated against current definition.
- `RestoreInvalidInput(Reason)` — structural mismatch (undefined state, unknown fields, type mismatch).

**Why validated, not trusted:** Restoring an entity in an invalid state is not allowed. The data must satisfy the current definition's constraints. If the definition changed since the data was stored, the correct response is migration (future design), not silent acceptance of invalid data.

**Future:** Migration logic will run before the validation pipeline — transforming persisted data to conform to the current definition before constraints are evaluated.

---

## Operations

### Version — Entity Snapshot

`Version` is an immutable snapshot of an entity at a point in time. Every operation returns a new `Version` — the input is never mutated.

```csharp
public sealed record Version
{
    // Identity
    public Precept Precept { get; }
    public string? State { get; }                                    // null for stateless precepts

    // Field access
    public PreceptValue this[string fieldName] { get; }             // throws on omitted field
    public T Get<T>(string fieldName);                              // typed access via TypeRuntime<T>
    public IReadOnlyList<FieldAccessInfo> FieldAccess { get; }      // omit = absent from list

    // Structural queries (precomputed — zero evaluation cost)
    public IReadOnlyList<string> AvailableEvents { get; }           // events with rows in current state
    public IReadOnlyList<ArgDescriptor> RequiredArgs(string eventName); // typed arg descriptors per arg

    // Applicable constraints (Tier 2 — filtered for current state)
    public IReadOnlyList<ConstraintDescriptor> ApplicableConstraints { get; }

    // Commit — JSON lane (wire callers)
    public EventOutcome  Fire(string eventName, JsonElement? args = null);
    public UpdateOutcome Update(JsonElement? fields = null);

    // Commit — typed lane (in-process callers)
    public EventOutcome  Fire(string eventName, Action<IArgBuilder>? args = null);
    public UpdateOutcome Update(Action<IFieldBuilder>? fields = null);

    // Inspect — JSON lane
    public EventInspection  InspectFire(string eventName, JsonElement? args = null);
    public UpdateInspection InspectUpdate(JsonElement? fields = null);

    // Inspect — typed lane
    public EventInspection  InspectFire(string eventName, Action<IArgBuilder>? args = null);
    public UpdateInspection InspectUpdate(Action<IFieldBuilder>? fields = null);
}
```

#### Three Access Tiers

| Tier | Methods | Cost | Source |
|------|---------|------|--------|
| **Structural** | `AvailableEvents`, `FieldAccess`, `RequiredArgs` | Zero — precomputed | Graph analysis baked into executable model |
| **Inspection** | `InspectFire`, `InspectUpdate` | Evaluator runs pipeline | Same path as commit, working copy discarded |
| **Commit** | `Fire`, `Update` | Evaluator runs pipeline | Working copy promoted on success |

#### Design Decisions

- **Immutable snapshot.** Operations return new `Version` instances. Survey evidence overwhelmingly favors this: XState `MachineSnapshot`, CEL activations, CUE `Value`, Dhall `Val`. At Precept's scale (10–50 fields), snapshot creation is sub-microsecond.
- **`string? State` for stateless precepts.** Stateless precepts have events, hooks, rules, and fields — but no states. `State` is `null`. This is the degenerate case, not a separate type. XState's pattern (state machines are one `ActorLogic` implementation among several) suggests a unified interface. All `EventOutcome` variants except `Transitioned` are reachable for stateless precepts.
- **Field indexer throws on omitted fields.** An `omit`ted field is structurally absent — it doesn't exist in the current state. Accessing it is a programming error, not a null. `FieldAccess` lists only non-omitted fields, so callers can enumerate what's accessible.
- **Two ingress lanes for all commit and inspect operations.** The **JSON lane** (`JsonElement?`) serves wire callers — MCP, HTTP APIs, deserialization pipelines — that already have JSON on hand. The **typed lane** (`Action<IArgBuilder>?` for Fire/Inspect, `Action<IFieldBuilder>?` for Update/Create) serves in-process callers that work with typed values. Both lanes cover all callers. There are no `IReadOnlyDictionary<string, object?>` overloads anywhere. Callers with a JSON string use `JsonDocument.Parse(str).RootElement` inline — no string-input convenience overloads.
- **Typed Restore is deliberately absent.** `Restore` takes `JsonElement` only — no `Action<IFieldBuilder>` overload. Restore is a hydration path from persisted storage, and storage always returns serialized data. In-process code never constructs a Restore call from scratch; callers use Create or Fire instead.
- **Instance methods delegate to static evaluator.** `Version.Fire(...)` delegates to `Evaluator.Fire(this.Precept, this, ...)`. The Version is a thin facade over the stateless evaluation function (R1). This keeps entity representation separate from evaluation logic.
- **Applicable constraints are state-scoped.** `Version.ApplicableConstraints` exposes the subset of `Precept.Constraints` that are active for the entity's current state: global rules (always), `in <CurrentState>` residency ensures, `from <CurrentState>` exit ensures, and event ensures for available events. Precomputed from the executable model's scope index — zero evaluation cost. This is the "what must be true here?" surface.

### Fire

```csharp
// JSON lane (wire callers)
EventOutcome outcome = version.Fire("submit",
    JsonDocument.Parse("""{"approver":"Jane","timestamp":"2026-05-03T23:45:15Z"}""").RootElement);

// Typed lane (in-process callers)
EventOutcome outcome = version.Fire("submit",
    args => args.Set<string>("approver", "Jane")
               .Set<DateTimeOffset>("timestamp", DateTimeOffset.UtcNow));

Version next = outcome switch
{
    Transitioned t => t.Result,     // state changed
    Applied a      => a.Result,     // stateless or no-transition success
    Rejected r     => throw new BusinessException(r.Reason),
    InvalidArgs e  => throw new ArgumentException(e.Reason),
    EventConstraintsFailed f => HandleViolations(f.Violations),
    Unmatched      => HandleNoMatch(),
    UndefinedEvent => HandleUnknownEvent(),
};
```

Fire runs the full event pipeline: arg validation → row matching (first-match with guard evaluation) → action chain execution on working copy → computed field recomputation → constraint evaluation (collect-all) → commit or discard. Returns one `EventOutcome` variant. See `result-types.md` for the full hierarchy.

### Update

```csharp
// JSON lane (wire callers)
UpdateOutcome outcome = version.Update(
    JsonDocument.Parse("""{"email":"jane@example.com","phone":"555-0100"}""").RootElement);

// Typed lane (in-process callers)
UpdateOutcome outcome = version.Update(
    fields => fields.Set<string>("email", "jane@example.com")
                   .Set<string>("phone", "555-0100"));

Version next = outcome switch
{
    FieldWriteCommitted c       => c.Result,
    UpdateConstraintsFailed f   => HandleViolations(f.Violations),
    AccessDenied d              => HandleDenied(d.FieldName, d.ActualMode),
    InvalidInput e              => throw new ArgumentException(e.Reason),
};
```

Update runs the field-write pipeline: access mode check → type validation → patch application to working copy → computed field recomputation → constraint evaluation (collect-all) → commit or discard. Returns one `UpdateOutcome` variant.

**Why Update, not Edit:** Renamed from `Edit` to avoid confusion with `edit` declarations in the DSL (per-state field access blocks). `Update` describes what the caller does to field values; `edit` describes what the precept author declares.

### Constraint Exposure Model

Constraints are exposed at three tiers, each with a distinct purpose:

| Tier | Surface | Contents | Cost | Purpose |
|------|---------|----------|------|---------|
| **1. Catalog** | `Precept.Constraints` | Every declared rule, ensure, rejection | Zero — precomputed | Definition introspection: "what rules does this precept have?" |
| **2. Applicable** | `Version.ApplicableConstraints` | Constraints active for current state | Zero — precomputed | State introspection: "what must be true here?" |
| **3. Evaluated** | `ConstraintResult` / `ConstraintViolation` | Per-constraint evaluation status | Evaluator cost | Operation results: "what passed, failed, or couldn't be resolved?" |

All three tiers use `ConstraintDescriptor` as the canonical identity. Tier 3 results reference back to the descriptor, so callers can always trace a violation to its declaration, kind, scope, and rationale.

#### ConstraintDescriptor

```csharp
public enum ConstraintKind { Rule, StateEnsureIn, StateEnsureTo, StateEnsureFrom, EventEnsure }

public sealed record ConstraintDescriptor(
    ConstraintKind Kind,
    string? ScopeTarget,                // state or event name; null for rules
    string ExpressionText,              // the source expression text
    string Because,                     // the mandatory `because` clause
    IReadOnlyList<string> ReferencedFields,
    bool HasGuard,
    int SourceLine);                    // 1-based line in .precept source
```

- **`Kind`** distinguishes the five constraint categories from the vision: global rules, three state-ensure anchors (`in`/`to`/`from`), and event ensures. Transition rejections (`reject`) are NOT a constraint kind — they are author-intentional routing outcomes, expressed as `EventOutcome.Rejected`.
- **`ScopeTarget`** is the state name for state ensures, the event name for event ensures, and `null` for global rules.
- **`ExpressionText`** is the source expression as written by the author. Enables side-by-side display with the precept source for debugging and tooling UX.
- **`Because`** is the author's mandatory rationale — the `because` clause text. Always present.
- **`ReferencedFields`** are the semantic subjects — the fields the constraint expression references. Computed field references are transitively expanded to stored fields. (Provisional — will become typed descriptors under D8/R4.)
- **`HasGuard`** indicates whether the constraint has a `when` guard (conditional constraint scoping).
- **`SourceLine`** is the 1-based line number in the `.precept` source. Enables go-to-definition in the language server and "click to jump" in preview.

Descriptors are created during `Precept.From()` and are immutable. They are reference-equal — the same descriptor instance appears in `Precept.Constraints`, `Version.ApplicableConstraints`, and any `ConstraintResult` or `ConstraintViolation` that evaluates it.

#### ConstraintResult (Tier 3 — Inspection)

```csharp
public sealed record ConstraintResult(
    ConstraintDescriptor Constraint,
    IReadOnlyList<string> FieldNames,
    ConstraintStatus Status);
```

Appears in `EventInspection.EventEnsures`, `RowInspection.Constraints`, and `UpdateInspection.Constraints`. References the descriptor and carries evaluation status: `Satisfied`, `Violated`, or `Unresolvable`.

> **Provisional (G1/G9):** `FieldNames` is a flat string list. The prototype carries a typed target hierarchy (field, event-arg, event, state, definition) for rich UI attribution. When metadata descriptors (D8/R4) are defined, this will become a typed target list that distinguishes WHERE a violation lands.

#### ConstraintViolation (Tier 3 — Commit)

```csharp
public sealed record ConstraintViolation(
    ConstraintDescriptor Constraint,
    string? BecauseClause,                        // From the constraint's because clause
    ImmutableArray<FieldSnapshot> RelevantFields,  // Field values at evaluation time
    string? FailingSubexpression,                  // Innermost expression that evaluated false
    PreceptValue? FailingValue                     // Value at the failure site
);
```

- `Constraint` — the descriptor of the rule that was violated
- `BecauseClause` — the `because "..."` text from the constraint declaration, if present
- `RelevantFields` — snapshots of field values at the time of evaluation (for diagnostic context)
- `FailingSubexpression` — the innermost subexpression text that evaluated to false
- `FailingValue` — the `PreceptValue` at the failure site (e.g., the computed value that violated the bound)

Appears in `EventConstraintsFailed.Violations` and `UpdateConstraintsFailed.Violations`. Only produced for constraints that are definitively violated (not unresolvable — commit operations have complete data).

> **Provisional (Shane ruling 2026-05-04):** Shape promoted from minimal 2-field form to the canonical 5-field design. Field population (especially `FailingSubexpression` and `FailingValue`) depends on evaluator instrumentation; all fields nullable/defaultable for fields not yet wired. `FieldNames` dropped — superseded by typed `ImmutableArray<FieldSnapshot>`.

#### Tier 2 Scope Rules

`Version.ApplicableConstraints` includes:

| Current state | Included constraints |
|--------------|---------------------|
| Any state S | All global `rule` declarations |
| State S | `in S ensure` (residency truth) |
| State S | `from S ensure` (exit truth — will be checked on transition out) |
| State S | `on E ensure` for every event E with rows in state S |

`to <State> ensure` constraints are NOT included in `ApplicableConstraints` for the target state — they are transitional, only surfacing in Tier 3 during inspect/fire when the target state is known from row matching. They appear in `RowInspection.Constraints` for rows that transition into that state. This is the correct boundary: Tier 2 is "what must hold in my current state"; Tier 3 is "what was actually evaluated for this specific operation" (which includes to-ensures once a target is resolved).

For stateless precepts, `ApplicableConstraints` includes all global rules and all event ensures.

### Shared Types

#### FieldAccessInfo

```csharp
public sealed record FieldAccessInfo(
    string FieldName,
    FieldAccessMode Mode,       // Readonly or Editable
    string FieldType,
    PreceptValue CurrentValue);

public enum FieldAccessMode { Readonly, Editable }
```

Returned by `Version.FieldAccess`. Lists every non-omitted field in the current state with its access mode and current value. `Omit`ted fields are structurally absent — they don't appear in this list.

#### ArgDescriptor

```csharp
public sealed record ArgDescriptor(string Name, string Type, bool IsOptional, int SlotIndex);
```

Returned by `Version.RequiredArgs(eventName)`. Enables the UI to render typed input controls for event arguments.

### Ingress Types

#### IArgBuilder

The fluent builder for event arguments, used in the typed lane for `Fire`, `InspectFire`, `Create`, and `InspectCreate`.

```csharp
public interface IArgBuilder
{
    IArgBuilder Set<T>(string name, T value);
}
```

Usage: `args => args.Set<decimal>("Amount", 500m).Set<string>("ApplicantName", "Jane")`

Each `Set<T>` call is resolved through the registered `TypeRuntime<T>` for zero-boxing conversion to `PreceptValue`. The builder internally produces a `PreceptValue[]` arg slot array populated via the **presence mask** — a `bool[]` of the same length, where `presence[i] == true` means arg slot `i` was explicitly set by the caller and `presence[i] == false` means it was not provided. Unset optional args remain absent (the corresponding `PreceptValue` slot is the absent sentinel); unset required args cause `InvalidArgs` at the Fire boundary before the opcode loop begins. The presence mask and slot array are both built during the `Action<IArgBuilder>` invocation and discarded after the call completes — they are not observable from outside the builder.

#### IFieldBuilder

The fluent builder for field patches, used in the typed lane for `Update`, `InspectUpdate`, `Create`, and `InspectCreate`.

```csharp
public interface IFieldBuilder
{
    IFieldBuilder Set<T>(string name, T value);
}
```

Usage: `fields => fields.Set<string>("ApplicantName", "Jane Doe").Set<decimal>("Amount", 500m)`

Each `Set<T>` call is resolved through the registered `TypeRuntime<T>`. The patch is applied atomically — either all fields commit or none do.

### Value Types

#### PreceptValue

The evaluation currency for the runtime. `PreceptValue` is a **32-byte tagged struct** — not a class hierarchy. All field and arg values are `PreceptValue` when read back from the runtime.

```csharp
[StructLayout(LayoutKind.Explicit, Size = 32)]
public struct PreceptValue
{
    // Opaque tagged union. Callers do not construct PreceptValue directly.
    // Obtain values from Version["FieldName"], FiredArgs["ArgName"], or
    // via IArgBuilder.Set<T> / IFieldBuilder.Set<T> (which go through TypeRuntime<T>).
    // Convert using TypeRuntime<T>.ToClr / TypeRuntimeMeta.WriteJson.
}
```

`PreceptValue` carries no instance methods for conversion. Conversion is owned by the catalog's `TypeRuntime<T>` and `TypeRuntimeMeta` registrations — not by the value itself.

#### TypeRuntime\<T\>

Zero-boxing registration record for mapping between CLR types and `PreceptValue`. Naming is final: `FromClr` / `ToClr` / `FromJson` / `ToJson`.

```csharp
public sealed record TypeRuntime<T>(
    Func<T, PreceptValue>       FromClr,
    Func<PreceptValue, T>       ToClr,
    Func<JsonElement, PreceptValue> FromJson,
    Func<PreceptValue, JsonElement> ToJson);
```

Registration pattern:

```csharp
PreceptRuntime.Register(new TypeRuntime<decimal>(
    FromClr:  v  => PreceptValue.FromScalar(v),
    ToClr:    pv => pv.AsDecimal(),
    FromJson: el => PreceptValue.FromScalar(el.GetDecimal()),
    ToJson:   pv => JsonSerializer.SerializeToElement(pv.AsDecimal())));
```

Registrations are process-global. `IArgBuilder.Set<T>` and `IFieldBuilder.Set<T>` resolve through registered `TypeRuntime<T>` entries for zero-allocation conversion. `Version.Get<T>()` and `FiredArgs.Get<T>()` use `ToClr` on the registered runtime.

#### TypeRuntimeMeta

Catalog-owned metadata record that holds the hot-path serialization delegates for each Precept type. Every `TypeMeta` entry in the Types catalog carries a `TypeRuntimeMeta` instance.

```csharp
public sealed record TypeRuntimeMeta(
    ReadJsonDelegate         ReadJson,          // ref Utf8JsonReader, ref PreceptValue — Phase 1 ingress
    WriteJsonDelegate        WriteJson,         // Utf8JsonWriter, PreceptValue — Phase 8 egress
    ParseStringDelegate      ParseString,       // string → PreceptValue (LS/authoring path)
    FormatStringDelegate     FormatString,      // PreceptValue → string (LS/authoring path)
    BinaryExecutorDelegate[] BinaryExecutors,   // indexed by OperationKind — builder embeds delegates in opcodes at compile time
    UnaryExecutorDelegate[]  UnaryExecutors);   // indexed by OperationKind — builder embeds delegates in opcodes at compile time
```

Active surface for Fire/Inspect/Update hot paths: `ReadJson`, `WriteJson`. `BinaryExecutors` and `UnaryExecutors` are consumed at build time — the builder embeds executor delegates directly in `BinaryOp`/`UnaryOp` opcodes; the evaluator never indexes these arrays at evaluation time. `ParseString` and `FormatString` are used by the language server and authoring tools. `ExtractValue`, `StoreValue`, and `ParseValue` are excluded from hot paths.

Ownership rules (locked per CC#25): the call site advances to the value token and handles `null`; collection runtimes own structural array/object loops; scalar fields read and write the inline value region directly without boxing intermediaries.

#### FiredArgs

Event arg egress — appears on `EventOutcome` variants that carry submission context (`Transitioned.Args`, `Applied.Args`, `Rejected.Args`). Allows callers to read back what was submitted as strongly-typed values. `Rejected` carries `FiredArgs` so callers can log or display what was submitted alongside the rejection reason — the submitted args are part of the rejection context, not discarded because the row matched.

```csharp
public sealed class FiredArgs
{
    public PreceptValue this[string name] { get; }
    public T Get<T>(string name);
}
```

`Get<T>` uses the registered `TypeRuntime<T>.ToClr` for the conversion. The `PreceptValue` indexer returns the raw value for callers that want to inspect or serialize it directly.

### Correctness Invariant

**Inspection and commit share the same evaluation path.** `InspectFire` runs the same pipeline as `Fire`— same guard evaluation, same action chain, same constraint checking. The only difference is disposition: inspection discards the working copy; commit promotes it on success. This is a structural guarantee, not a convention.

**Survey grounding:** XState's `transition()` / `getNextSnapshot()` pair — same code path, different output. CEL's `ExhaustiveEval` — same `Interpretable` tree, different evaluation mode. K8s dry-run — same controller logic, different commit behavior.

### Thread Safety

- `Precept` is immutable after construction. Safe to share across threads and entity instances.
- `Version` is an immutable snapshot. Safe to read from multiple threads. Operations return new instances.
- The evaluator is stateless (R1). Working copies are per-operation, never shared.
- No locks, no synchronization, no mutable shared state.

---

## Inspection

### InspectFire

```csharp
// No args — all guards with arg-dependent terms are Possible
EventInspection landscape = version.InspectFire("submit");

// JSON lane — with partial args — refine toward Certain
EventInspection refined = version.InspectFire("submit",
    JsonDocument.Parse("""{"approver":"Jane"}""").RootElement);

// Typed lane — with partial args — refine toward Certain
EventInspection refined = version.InspectFire("submit",
    args => args.Set<string>("approver", "Jane"));

// Check the reduced answer
bool canFire = refined.OverallProspect != Prospect.Impossible;
```

Returns the full annotated landscape for an event: every row with `Prospect` (Certain/Possible/Impossible), field snapshots per row, constraint results, and hypothetical result when fully resolvable. `OverallProspect` provides the reduced boolean answer.

**Why InspectFire, not CanFire:** Inspection is not a boolean predicate — it returns a landscape of annotated possibilities. Running the evaluator and discarding everything except a boolean would waste information the caller likely needs (which constraints fail, what the resulting state would be, which args are missing).

### InspectUpdate

```csharp
// No patch — landscape against current field values
UpdateInspection current = version.InspectUpdate();

// JSON lane — see how changes affect constraints and event prospects
UpdateInspection preview = version.InspectUpdate(
    JsonDocument.Parse("""{"amount":150.00}""").RootElement);

// Typed lane — same hypothetical using in-process values
UpdateInspection preview = version.InspectUpdate(
    fields => fields.Set<decimal>("amount", 150.0m));

// Check which events become available after the hypothetical edit
foreach (var evt in preview.Events)
    Console.WriteLine($"{evt.EventName}: {evt.OverallProspect}");
```

Returns all non-omitted fields with post-patch values, constraint results, and full `EventInspection` for every event defined in the current state — evaluated against the hypothetical field state. This is the "what if I change this field?" query.

**Field patches re-evaluate event prospects.** Changing a field value shifts which event guards pass. `InspectUpdate` returns the full event landscape so the UI can enable/disable buttons based on the hypothetical state.

**Why InspectFire/InspectUpdate are separate:** Fire and Update are mutually exclusive commit operations — field patches and event args produce a new Version through fundamentally different pipelines (row matching + transition vs. access-mode check + constraint evaluation). A unified `Inspect(fields?, event?, args?)` would force callers to disentangle which combination was evaluated and would conflate two non-overlapping input/output shapes.

---

## Design Rationale and Decisions

The two-type surface (`Precept` + `Version`) is the foundational architectural decision. `Precept` is a definition-level artifact — immutable, shareable, one-per-definition. `Version` is an instance-level snapshot — immutable, one-per-operation-result. This clean split maps naturally to how host applications manage entity lifecycle: load a precept once, restore or create entity instances as needed, treat every operation result as a new snapshot.

The construction-mirrors-operations pattern (`Create`/`InspectCreate` paralleling `Fire`/`InspectFire`) eliminates a special construction code path. Construction IS an event firing — it goes through the same pipeline, produces the same outcome types, and requires the same pattern matching. This was chosen over a `Version.CreateInitial()` factory to keep the evaluator's surface uniform.

The three-tier constraint exposure model separates three questions that callers ask at different points:
- "What constraints exist in this precept?" (Tier 1 — definition-time, `Precept.Constraints`)
- "What must hold right now?" (Tier 2 — state-time, `Version.ApplicableConstraints`)
- "What actually evaluated in this operation?" (Tier 3 — operation-time, `ConstraintResult`/`ConstraintViolation`)

Conflating these tiers would require callers to filter and re-scope constraint lists themselves — the three-tier model removes that burden.

**Two-lane ingress design.** The JSON lane (`JsonElement?`) and typed lane (`Action<IArgBuilder>?` / `Action<IFieldBuilder>?`) are the complete ingress surface. `IReadOnlyDictionary<string, object?>` convenience overloads were considered provisionally and superseded — they added a third shape without covering a third class of callers. JSON + typed covers all real callers without compromising type safety or allocation characteristics.

---

## Open Questions / Implementation Notes

### R3 — Entity Representation Internals

How is `Version` represented internally? The public API is decided; the internal storage is not formally closed.

| Option | Trade-off |
|--------|-----------|
| Slot array (`PreceptValue[]`) | O(1) field access via precomputed indices. Requires executable model to resolve field names → slot indices during construction. Donated directly as `Version.Slots` on commit (zero-copy promotion). |
| Hybrid (slot array internal, name-based public API) | Best of both — array performance internally, clean `PreceptValue` API externally. |

Zero-copy promotion is locked (`PreceptValue[]` working copy donated as `Version.Slots` on commit). Slot arrays are the direction; the formal R3 close is pending D8/R4.

### D8/R4 — Executable Model Contract

The Version surface is specified. What the executable model provides (dispatch tables, slot layouts, constraint buckets) is not yet. Version delegates to the evaluator; the evaluator consumes the executable model. The contract between them is D8/R4.

**Metadata descriptors.** D8/R4 must define the typed descriptor types (field descriptor, event descriptor, state descriptor, arg descriptor) that replace raw strings throughout the API. Every `string` placeholder in the current stubs is a known gap. The descriptors carry the full compiled metadata — name, type, slot index, access mode, constraints, arg-dependency sets — and are the canonical identity the evaluator operates against. No string-based lookup at runtime.

### Stateless Precepts — CreateInitialVersion

How does `CreateInitialVersion` work for stateless precepts? No initial state, no state entry actions, no omit-on-entry clearing. Fields get defaults, computed fields are evaluated, rules are checked. The `Version.State` is `null`.

---

## Deliberate Exclusions

- **No `IReadOnlyDictionary<string, object?>` overloads.** Fully obsolete. The two-lane ingress (JSON + typed) covers all callers. No dictionary-based convenience extensions exist or will be added.
- **No fault type definitions.** `FaultException` and fault site taxonomy are in `fault-system.md`. The runtime API surface only produces structured outcomes — faults are the exceptional escape hatch for impossible-path bugs, not part of the normal outcome model.
- **No result type hierarchy.** Full `EventOutcome`, `UpdateOutcome`, `RestoreOutcome`, and inspection type shapes are in `result-types.md`. This document covers when and why each operation produces outcomes; `result-types.md` covers the shape of each type.
- **No evaluation logic.** The runtime API delegates all evaluation to the `Evaluator`. No pipeline mechanics live in `Precept.cs` or `Version.cs`.
- **No build-time analysis.** Graph analysis, type checking, and compilation are owned by the compiler pipeline. `Precept.From()` accepts only an error-free `Compilation` — it does not re-analyze.
- **No migration logic.** Future work. `Restore` validates against the current definition; if the definition changed since data was stored, migration is the caller's responsibility until the migration pipeline is designed.

---

## Cross-References

| Topic | Document |
|---|---|
| Compiler and runtime architectural decisions (R1–R5, D8) | `docs/compiler-and-runtime-design.md` |
| Full result type taxonomy (`EventOutcome`, `UpdateOutcome`, `RestoreOutcome`, inspection types) | `docs/runtime/result-types.md` |
| Fault system and `FaultException` taxonomy | `docs/runtime/fault-system.md` |
| Evaluator — plan execution and pipeline mechanics | `docs/runtime/evaluator.md` |
| Descriptor types used in outcomes and inspections | `docs/runtime/descriptor-types.md` |
| Precept Builder — how the executable model is constructed from `Compilation` | `docs/runtime/precept-builder.md` |

---

## Source Files

| File | Purpose |
|---|---|
| `src/Precept/Runtime/Precept.cs` | Executable model — `Precept.From()`, `Create`, `InspectCreate`, `Restore`, definition-level queries |
| `src/Precept/Runtime/Version.cs` | Entity snapshot — `Fire`, `Update`, `InspectFire`, `InspectUpdate`, field access, structural queries |