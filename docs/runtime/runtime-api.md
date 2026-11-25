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

## Contents

- [Overview](#overview)
  - [Two-Lane Ingress Principle](#two-lane-ingress-principle)
- [Responsibilities and Boundaries](#responsibilities-and-boundaries)
- [Right-Sizing](#right-sizing)
- [Inputs and Outputs](#inputs-and-outputs)
- [Construction](#construction)
  - [Compilation → Executable Model](#compilation-executable-model)
  - [Executable Model → Initial Entity](#executable-model-initial-entity)
  - [Precept — Definition-Level Queries](#precept-definition-level-queries)
- [Restoration](#restoration)
- [Operations](#operations)
  - [Version — Entity Snapshot](#version-entity-snapshot)
  - [Fire](#fire)
  - [Update](#update)
  - [Constraint Exposure Model](#constraint-exposure-model)
  - [Shared Types](#shared-types)
  - [Ingress Types](#ingress-types)
  - [Value Types](#value-types)
  - [Correctness Invariant](#correctness-invariant)
  - [Thread Safety](#thread-safety)
- [Inspection](#inspection)
  - [InspectFire](#inspectfire)
  - [InspectUpdate](#inspectupdate)
- [Typed Lane](#typed-lane)
  - [Get\<T\>() Mechanics](#gett-mechanics)
  - [Valid CLR Types](#valid-clr-types)
- [CLR Type Discovery](#clr-type-discovery)
- [Design Rationale and Decisions](#design-rationale-and-decisions)
- [Open Questions / Implementation Notes](#open-questions-implementation-notes)
  - [R3 — Entity Representation Internals](#r3-entity-representation-internals)
  - [D8/R4 — Executable Model Contract](#d8r4-executable-model-contract)
  - [Stateless Precepts — CreateInitialVersion](#stateless-precepts-createinitialversion)
- [Deliberate Exclusions](#deliberate-exclusions)
- [Cross-References](#cross-references)
- [Source Files](#source-files)

## Overview

The runtime API is the boundary between Precept and host applications. It has four concerns:

1. **Construction** — compile source → build executable model → create initial entity via `Create` / `InspectCreate`
2. **Restoration** — reconstitute an entity from persisted data via `Restore` (hydration from storage, not validated business input)
3. **Operations** — commit changes via `Fire` and `Update`
4. **Inspection** — progressive evaluation via `InspectFire` and `InspectUpdate`

The API surface is deliberately small. Two types carry the entire public contract: `Precept` (the executable model, definition-level queries, construction) and `Version` (entity snapshot, instance-level operations and inspection). `Precept` mirrors `Version`'s commit/inspect pattern: `Create` / `InspectCreate` parallel `Fire` / `InspectFire`.

### Two-Lane Ingress Principle

**The runtime API accepts two and only two ingress shapes for all mutable operations — no more, no less.**

- **JSON lane** (`JsonElement?`) — for wire callers: MCP tools, HTTP APIs, deserialization pipelines. Callers that have a JSON string use `JsonDocument.Parse(str).RootElement` inline. No string-input convenience overloads exist.
- **Typed lane** (`Action<IArgBuilder>?` for event args; `Action<IFieldBuilder>?` for field patches) — for in-process callers that work with typed CLR values. Zero-boxing via `TypeRuntime<T>`.

**There are no `IReadOnlyDictionary<string, object?>` overloads anywhere.** That provisional convenience lane is fully obsolete and superseded by these two lanes.

**Restoration is JSON-only.** `Restore(string? state, JsonElement fields)` has no typed overload. Restoration is a hydration path from persisted storage — storage always provides serialized data. Invalid inputs are reported through `RestoreInvalidInput` / `ArgumentException`-style boundary failures, not a typed-lane overload.

**Dual-lane output.** The raw-lane indexer (`version["fieldName"]`) returns `JsonElement` — no Precept type dependency required. The typed lane uses `Get<T>()`, which materializes a CLR value via the registered `TypeRuntime<T>.ToClr`. `FiredArgs` carries submitted arg values for egress using the same `Get<T>()` pattern.

---

## Responsibilities and Boundaries

**OWNS:** Public API surface (`Precept` and `Version`); executable model lifecycle (construction from `Compilation`, validation); entity construction (`Create`, `InspectCreate`); entity restoration (`Restore`); operation dispatch (`Fire`, `Update`); inspection surface (`InspectFire`, `InspectUpdate`); definition-level structural queries (`States`, `Fields`, `Events`, `Constraints`); constraint exposure model (all three tiers); field access surface (`FieldAccess`, `AvailableEvents`, `RequiredArgs`, `ApplicableConstraints`).

**Does NOT OWN:** Compilation (owned by the compiler pipeline); evaluation logic (owned by the `Evaluator`); executable model internals / dispatch table layouts (pending D8/R4); result type definitions (`EventOutcome.cs`, `UpdateOutcome.cs`); descriptor type definitions (`Descriptors.cs`); fault system (`Fault` and the fault classes).

---

## Right-Sizing

The runtime API surface is deliberately minimal: two types (`Precept` and `Version`), four operations (`Create`, `Restore`, `Fire`, `Update`), and parallel inspection variants (`InspectCreate`, `InspectFire`, `InspectUpdate`). Definition-level queries are precomputed structural reads — zero evaluation cost. The boundary between `Precept` and `Version` is clean: `Precept` carries everything definition-scoped; `Version` carries everything instance-scoped. Internal evaluation complexity lives entirely in the `Evaluator` behind `Version`'s thin facade.

---

## Inputs and Outputs

**Inputs:**
- `Compilation` → `Precept.From()` — the compiled artifact; must be error-free
- `state`, `fields` → `Precept.Restore()` — persisted state name plus stored field values (`JsonElement` only)
- `args` → `Precept.Create()` / `Version.Fire()` — event arguments; `JsonElement?` (JSON lane) or `Action<IArgBuilder>?` (typed lane)
- `fields` → `Version.Update()` — field patch; `JsonElement?` (JSON lane) or `Action<IFieldBuilder>?` (typed lane), applied atomically
- `eventName`, `args?` → `Version.InspectFire()` — event name with optional arg refinement; both lanes
- `fields?` → `Version.InspectUpdate()` — optional hypothetical field patch; both lanes

**Outputs:**
- `Precept` — the executable model; returned from `Precept.From()`
- `Version` — the hydrated entity snapshot; returned from `Precept.Restore()` on `Restored`
- `EventOutcome` (8 variants) — returned from `Create`, `Fire`; see `result-types.md`
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
    EventOutcome.Created c                => c.Result,     // construction succeeded — entity now exists in initial state
    EventOutcome.Rejected r               => throw new BusinessException(r.Reason),
    EventOutcome.InvalidArgs e            => throw new ArgumentException(e.Reason),
    EventOutcome.Faulted fault            => throw new InvalidOperationException(fault.Fault.Message),
    // Transitioned — structurally impossible (construction is terminal in initial state)
    // Applied — not a Create() outcome (Create() produces Created, not Applied)
    // Unmatched — structurally impossible (no-match at construction → Rejected)
    // UndefinedEvent — structurally impossible (compiler ensures initial event rows exist)
    // ConstraintsFailed — structurally impossible (no post-mutation constraint evaluation exists)
};

// Precepts WITHOUT an initial event — all fields have defaults
EventOutcome simple = precept.Create();  // always Created
```

`Create` is construction. It always returns `EventOutcome.Created` on success — regardless of whether an initial event is declared. `Created` means "an entity was brought into existence." Whether that creation involved firing an initial event or just applying defaults is an implementation detail — the outcome category is the same.

If the precept declares an `initial` event, `Create` runs it once, atomically, through the same phases `Fire` runs — without calling `Fire`, which refuses the initial event's name (spec §3A.4 phase 1): build hollow version (defaults + initial state + omitted fields) → validate the supplied arguments at ingress → evaluate construction rows (first-match) → execute matched row's action chain → run the initial state's entry actions → computed-field recomputation → commit → return outcome. The steps, their order and the reason the hollow version is built before the arguments are checked are stated in `precept-language-spec.md` § 3A.4, *Operation execution order*, and § 3A.5; this sentence is a summary of them. There is no constraint-evaluation step between the action chain and the commit: the compiler proved that construction — default materialization, the construction row's actions and the initial state's entry actions, or on a precept with no initial event the defaults and the entry actions — establishes every rule (spec §0.6 item 11, §0.7, § 3A.5), so a construction that ran to completion is valid by construction. If no initial event is declared, `Create` constructs from defaults, runs the initial state's entry actions against them, and returns `Created` (always succeeds by compile-time guarantee C59/C86). Construction enters the initial state; spec § 3A.4 states when entry and exit actions run.

**Construction outcome space:** `Create()` returns exactly one of:

| Outcome | Meaning | When produced |
|---------|---------|---------------|
| `Created` | Construction succeeded. Entity now exists in the initial state (stateful) or with `State = null` (stateless). | A mutation row matched and its action chain ran to completion. |
| `Rejected` | Intake refused. Entity was NOT created. | An authored `reject` row matched, OR all `when` guards failed with no unconditional fallback (synthesized reason: `"No construction row matched arguments"`). |
| `InvalidArgs` | Ingress validation failed, before row matching — collect-all as `precept-language-spec.md` § 3A.4 phase 1 defines it, so every failure the check finds is reported. | Wrong type, unknown key, missing required argument, an argument value that violates a constraint declared on it, or an `on <Event> ensure` that does not hold (spec § 3A.2 item 4). |
| `Faulted` | A breach of one of the engine's own invariants — a defect in the compiler, the builder or the evaluator. | Nothing constructs one today; whether anything ever will is undecided (`fault-system.md` Q3). |

**Structurally impossible outcomes from `Create()`:**
- `Transitioned` — the grammar excludes `transition` from construction rows. Construction always terminates in the initial state.
- `Applied` — exclusively a `Fire()` outcome. `Create()` produces `Created`, never `Applied`.
- `Unmatched` — the compiler guarantees at least one construction row exists (`ZeroConstructionRows` diagnostic). If all guards fail at runtime, the result is `Rejected`, not `Unmatched`.
- `UndefinedEvent` — `Create()` targets the declared initial event; the compiler ensures it has rows.
- `ConstraintsFailed` — there is no post-mutation constraint evaluation to fail. The compiler proved that construction establishes every rule (spec §0.7, § 3A.5).

**Why `Create` returns `EventOutcome`:** Construction goes through the full event pipeline — same ingress validation of the supplied arguments, same guards, same action chain. The caller uses the same exhaustive pattern matching they use for every `Fire`, minus the impossible variants.

**Semantic guarantee:** After successful construction (`EventOutcome.Created`), `version.State == precept.InitialState.Name` for stateful precepts, or `version.State == null` for stateless precepts. Always. No exceptions.

**Discovery:** `precept.InitialEvent` names the initial event (or null). `precept.RequiredArgs(initialEvent)` returns typed arg descriptors — the construction contract.

**Preview:** `precept.InspectCreate(args?)` returns `EventInspection` with the same progressive resolution model as `InspectFire` — Certain/Possible/Impossible per row, constraint results, field snapshots. It runs the steps `Create` runs, in the same order, commits nothing, and evaluates nothing `Create` would not (`precept-language-spec.md` § 3A.6, *What a preview evaluates*).

**Fire-once enforcement:** The initial event fires exactly once — at construction time. After construction succeeds, the initial event is excluded from the entity's post-construction event space. Calling `version.Fire(initialEventName, args)` on an existing entity returns `EventOutcome.UndefinedEvent`, whatever the arguments, before they are checked (spec § 3A.4 phase 1). No new mechanism is required — the initial event simply does not appear in the post-construction dispatch table. `version.AvailableEvents` (when exposed) excludes the initial event.

**Hollow context (construction guard evaluation):** During construction, the entity does not yet exist. Construction row guards (`when` clauses) are evaluated against the *hollow version* — a working copy with defaults applied and initial state set, but no field values established by prior actions. Guard expressions may read:
- **Event args (`args.*`):** Always valid. The caller-supplied arguments are bound before row matching.
- **Defaulted fields:** Valid — fields with declared `default` values are pre-populated on the hollow version.
- **Fields with no value yet:** Invalid reads. A required field (non-optional, no default) has no value until a construction action establishes it, and reading it in a construction guard — or in a construction `reject` row's reason, an initial-event argument constraint or an `on <InitialEvent> ensure` — is a compile-time error. An `optional` field without a default is not set and a non-optional collection without a default starts empty; both have a value and may be read. The full statement is `precept-language-spec.md` § 3A.4 phase 0.

**`AlwaysRejecting` for initial events:** If every construction row for the initial event unconditionally rejects (zero mutation rows, or all mutation row guards are provably unsatisfiable), the compiler emits `AlwaysRejecting` at **Error** severity (promoted from Warning for non-initial events). An unconstructible precept is a definition-level defect — the entity type can never be instantiated.

**Compiler enforcement:**
- **`RequiredFieldsNeedInitialEvent`:** Precept has required fields (non-optional, no default) but does not declare an initial event — construction cannot produce a valid initial version.
- **`InitialEventMissingAssignments`:** Initial event does not assign all required fields that lack defaults — post-construction state may violate constraints.
- **`UninitializedFieldReadInInitialAssignment`:** A required field with no default is read inside its own first initial-event assignment — construction would consume an undefined value.
- **`ZeroConstructionRows`:** Initial event is declared but has zero `on <InitialEvent>` construction rows — the event cannot fire.
- **`MultipleInitialEvents`:** More than one event carries the `initial` modifier — only one initial event is permitted per precept.
- **`InitialEventInTransitionRow`:** The initial event appears in a `from <State> on <InitialEvent>` transition row — the initial event is reserved for construction rows only.

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

    // Entity restoration — JSON only (no typed overload)
    public RestoreOutcome Restore(string? state, JsonElement fields);

    // Definition-level queries (structural — precomputed from graph analysis)
    public IReadOnlyList<StateDescriptor> States { get; }
    public IReadOnlyList<FieldDescriptor> Fields { get; }
    public IReadOnlyList<EventDescriptor> Events { get; }
    public StateDescriptor? InitialState { get; }
    public EventDescriptor? InitialEvent { get; }        // null = no initial event, args optional
    public bool IsStateless { get; }

    // Constraint catalog (Tier 1 — all declared constraints)
    public IReadOnlyList<ConstraintDescriptor> Constraints { get; }
}
```

**Design decisions:**
- **Sealed class, not record.** `Precept` owns internal mutable-during-construction state (dispatch tables, slot arrays) that is frozen after `From()` completes. Records imply value semantics; `Precept` has reference identity.
- **Definition-level queries are structural.** They are precomputed from graph analysis during construction. No evaluator involved — zero runtime cost.
- **Single `Precept` per definition.** Thread-safe, shareable, cacheable. Mirrors CEL's `Program` (one compiled program evaluated against many activations) and OPA's `PreparedEvalQuery`.
- **Construction mirrors operations.** `Create` / `InspectCreate` parallel `Version.Fire` / `Version.InspectFire`. Same pipeline, same outcome types, same pattern matching. No special construction path. `Create()` returns `Created` (not `Applied`) — semantically distinct from mutation on an existing entity.
- **`InitialEvent` nullable.** `null` means no initial event is declared — `Create()` builds from defaults (always `Created`). Non-null (`EventDescriptor`) means the initial event fires atomically during construction. The compiler ensures (`RequiredFieldsNeedInitialEvent`/`InitialEventMissingAssignments`) that this is coherent.
- **`InitialState` nullable.** `null` for stateless precepts; otherwise a `StateDescriptor`.
- **Constraint catalog is definition-level.** `Precept.Constraints` exposes every declared constraint (rules, state ensures, event ensures) with full metadata: kind, scope, anchor, `because` rationale, referenced fields, guard presence. This is the full catalog — unfiltered by state. Callers use it for definition-level introspection (e.g., "what rules does this precept declare?").

### Definition Load and the Certificate Gate

The runtime does not re-evaluate rules against a finished working copy, so what stands behind the guarantee at runtime is not a second evaluation of the rules — it is a check on the definition itself, run once when the definition is loaded.

Alongside the compiled definition the compiler emits a **certificate**: a checkable record of the proof, carrying for each obligation the theorem, the premises the proof used (which guard, which argument constraint, which pre-state rule), and the derivation. `Precept.From(Compilation)` runs the certificate checker over that record before it will return a usable `Precept`, and **refuses to govern under a definition whose certificate does not verify**. A definition that was never proven, that was tampered with, or that is stale with respect to the checker never governs anything. The confidence is rooted in verification the runtime performed itself rather than in provenance it assumed.

The checker walks the recorded derivation and confirms each step; it never repeats the search for a proof. That is why it stays small and fast enough to run on every load.

The certificate also tells the runtime which checks it may not skip. The premise list *is* the set of runtime validations the proofs depend on — which argument constraints and which guards must run — derived from the proof rather than maintained as a parallel list alongside it.

> **Not yet built.** No certificate is emitted, no checker exists, and `Precept.From` performs no such gate today. This section states the contract the runtime is being built to; the shape of the certificate record and the checker's API are not settled here.

---

## Restoration

```csharp
// Restore from persisted state — returns structured RestoreOutcome
RestoreOutcome outcome = precept.Restore(state: "Pending", fields: storedJsonElement);

Version version = outcome switch
{
    Restored r                      => r.Result,
    RestoreInvalidInput e           => throw new ArgumentException(e.Reason),
};
```

`Restore` reconstitutes a `Version` from persisted data. It accepts state name and field values separately and recomputes computed fields. It does **not** re-validate constraints: restored state is trusted as valid at the time it was persisted — like a direct database read — and re-checking would block data written under an earlier definition (spec §0.7; evaluator.md Decision 5). Handling data written under an older definition is the post-MVP job of the restore path itself, through migration. Nothing re-governs a stale value later either: there is no pass over the finished working copy that a subsequent operation could catch it in (spec §0.7, §3A.4). What restore trusts is the data; what the runtime verifies for itself is the *definition* — see § Definition Load and the Certificate Gate.

> ⚠️ **Design note:** The implementation uses `Restore(string? state, JsonElement fields)` — taking state and fields as separate parameters — and returns a structured `RestoreOutcome` rather than returning `Version` directly and throwing on errors. See inbox item `newman-comprehensive-doc-review.md` for the full design divergence assessment.
>
> **There is no constraint-failure outcome on restore.** Restore does not re-validate — restored data is trusted as valid at the time it was persisted (spec §0.7) — so the only ways restore can fail are structural: an undefined state name, unknown fields, a type mismatch. `RestoreOutcome` carries those and nothing else.

**Access modes are bypassed.** Fields that are `readonly` in the restored state were `editable` when previously written. `Restore` accepts all stored fields regardless of the current state's access declarations.

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
    public JsonElement this[string fieldName] { get; }              // throws on omitted field
    public T Get<T>(string fieldName);                              // typed access via TypeRuntime<T>
    public IReadOnlyList<FieldAccessInfo> FieldAccess { get; }      // omit = absent from list

    // Structural queries (precomputed — zero evaluation cost)
    public IReadOnlyList<EventDescriptor> AvailableEvents { get; }           // events with rows in current state
    public IReadOnlyList<ArgDescriptor> RequiredArgs(EventDescriptor @event); // typed arg descriptors per arg

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
| **Structural** | `AvailableEvents`, `FieldAccess`, `RequiredArgs` | Precomputed, except that `FieldAccess` evaluates the guard of each guarded access mode against the version — the same evaluation `Update` makes at phase 1 (spec §3A.4, *The operations*) | Graph analysis baked into executable model |
| **Inspection** | `InspectFire`, `InspectUpdate` | Evaluator runs pipeline | Same path as commit, working copy discarded |
| **Commit** | `Fire`, `Update` | Evaluator runs pipeline | Working copy promoted on success |

#### Design Decisions

- **Immutable snapshot.** Operations return new `Version` instances. Survey evidence overwhelmingly favors this: XState `MachineSnapshot`, CEL activations, CUE `Value`, Dhall `Val`. At Precept's scale (10–50 fields), snapshot creation is sub-microsecond.
- **`string? State` for stateless precepts.** Stateless precepts have events, hooks, rules, and fields — but no states. `State` is `null`. This is the degenerate case, not a separate type. XState's pattern (state machines are one `ActorLogic` implementation among several) suggests a unified interface. All `EventOutcome` variants except `Transitioned` are reachable for stateless precepts.
- **Field indexer throws on omitted fields.** An `omit`ted field is structurally absent — it doesn't exist in the current state. Accessing it is a programming error, not a null. `FieldAccess` lists only non-omitted fields, so callers can enumerate what's accessible.
- **Two ingress lanes for all commit and inspect operations.** The **JSON lane** (`JsonElement?`) serves wire callers — MCP, HTTP APIs, deserialization pipelines — that already have JSON on hand. The **typed lane** (`Action<IArgBuilder>?` for Create, Fire and their inspections, `Action<IFieldBuilder>?` for Update and InspectUpdate) serves in-process callers that work with typed values. Both lanes cover all callers. There are no `IReadOnlyDictionary<string, object?>` overloads anywhere. Callers with a JSON string use `JsonDocument.Parse(str).RootElement` inline — no string-input convenience overloads.
- **Typed Restore is deliberately absent.** `Restore` takes `JsonElement` only — no `Action<IFieldBuilder>` overload. Restore is a hydration path from persisted storage, and storage always returns serialized data. In-process code never constructs a Restore call from scratch; callers use Create or Fire instead.
- **Instance methods delegate to static evaluator.** `Version.Fire(...)` delegates to `Evaluator.Fire(this.Precept, this, ...)`. The Version is a thin facade over the stateless evaluation function (R1). This keeps entity representation separate from evaluation logic.
- **Applicable constraints are state-scoped.** `Version.ApplicableConstraints` exposes the subset of `Precept.Constraints` that are active for the entity's current state: global rules (always), `in <CurrentState>` residency ensures, `from <CurrentState>` exit ensures, and event ensures for available events. Precomputed from the executable model's scope index — zero evaluation cost. It answers "what applies here?", and not every constraint it lists is true of the current version. Which of them are known of every committed version in the state is stated once, in `precept-language-spec.md` § 3A.4, *Between operations nothing is carried except what every reachable configuration satisfies*; it excludes, among others, a rule that mentions `now()` or a field the current state omits. A `from <CurrentState>` ensure is what a transition out of the state must make true, and an event ensure constrains that event's arguments.

### Fire

```csharp
// JSON lane (wire callers)
EventOutcome outcome = version.Fire("submit",
    JsonDocument.Parse("""{"approver":"Jane","timestamp":"2026-05-03T23:45:15Z"}""").RootElement);

// Typed lane (in-process callers)
EventOutcome outcome = version.Fire("submit",
    args => args.Set<string>("approver", "Jane")
               .Set<Instant>("timestamp", SystemClock.Instance.GetCurrentInstant()));

Version next = outcome switch
{
    EventOutcome.Transitioned t           => t.Result,
    EventOutcome.Applied a                => a.Result,
    EventOutcome.Rejected r               => throw new BusinessException(r.Reason),
    EventOutcome.InvalidArgs e            => throw new ArgumentException(e.Reason),
    EventOutcome.Unmatched                => HandleNoMatch(),
    EventOutcome.UndefinedEvent           => HandleUnknownEvent(),
};
```

Fire runs the event pipeline in § 3A.4 order (`precept-language-spec.md`, § 3A.4, *Operation execution order*): ingress governance (arg validation) → dispatch (row matching, first-match with guard evaluation; a winning reject row ends the operation here) → exit actions → the state change → the row's action chain on the working copy → the `omit` reset and entry actions → computed-field recomputation → commit. That section is the full statement of each step; this sentence is a summary of it. Returns one `EventOutcome` variant. See `result-types.md` for the full hierarchy.

There is no constraint-evaluation stage between recomputation and commit, and no pass/fail decision the commit waits on. What the operation tests — the arguments at ingress, the row guards at dispatch, and the guards of guarded state actions — are the premises the compiler's proofs rest on (spec §0.7); everything else was proven at compile time, and a working copy the operation completed is promoted unconditionally. When the working copy is discarded instead is stated in spec § 3A.4, phase 8: an operation that refused at phase 1 or ended at phase 2 never reaches commit, which for `Fire` covers `InvalidArgs`, `UndefinedEvent`, `Unmatched` and `Rejected`.

> Exit actions, the state change, the `omit` reset and entry actions run for a `transition` row, including one back to the same state, and not for a `no transition` row. Where a state carries several state actions, all of them run in declaration order. Spec § 3A.4 states both rules.

**Row dispatch model:** Transition rows come in two shapes — mutation rows (`TransitionRowMutation`: carry action chain + success outcome) and reject rows (`TransitionRowReject`: carry reject clause only). Row matching evaluates guards in declaration order; the first matching row's shape determines the outcome, and the remaining rows are not evaluated (spec §3A.4 phase 2). A mutation row executes its action chain and produces `Transitioned` or `Applied`. A reject row produces `Rejected` with the authored reason string. The two shapes are grammar-level constructs — a single row cannot mix mutations with rejection.

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
    UpdateOutcome.Updated c             => c.Result,
    UpdateOutcome.ConstraintsFailed f   => HandleViolations(f.Violations),
    UpdateOutcome.FieldNotEditable d    => HandleDenied(d.FieldName, d.ActualMode),
    UpdateOutcome.InvalidFields e       => throw new ArgumentException(e.Reason),
};
```

Update runs the field-write pipeline: access mode check → type validation → ingress validation of each patched value (collect-all) → patch application to working copy → computed field recomputation → commit. Returns one `UpdateOutcome` variant. These are phases 1, 5, 7 and 8 of `precept-language-spec.md` § 3A.4, *Operation execution order*, which is the full statement.

Ingress validation happens **before** the value is applied, and it holds the incoming value to the constraints declared on the field it enters (spec §0.7, *Governance*). It is the same operation-blind check every externally-supplied value gets. There is no second pass over the finished working copy: an editable field is an ordinary write site, and the compiler discharged its obligations at compile time like any other. A patched value that fails its check is refused before any mutation runs, so nothing is rolled back. Whether this check also covers every rule that mentions the edited field is an outstanding owner decision — see `precept-language-spec.md` § Open Questions.

**Why Update, not Edit:** Renamed from `Edit` to avoid confusion with `edit` declarations in the DSL (per-state field access blocks). `Update` describes what the caller does to field values; `edit` describes what the precept author declares.

### Constraint Exposure Model

Constraints are exposed at three tiers, each with a distinct purpose:

| Tier | Surface | Contents | Cost | Purpose |
|------|---------|----------|------|---------|
| **1. Catalog** | `Precept.Constraints` | Every declared rule, ensure, rejection | Zero — precomputed | Definition introspection: "what rules does this precept have?" |
| **2. Applicable** | `Version.ApplicableConstraints` | Constraints active for current state | Zero — precomputed | State introspection: "what applies here?" (see *Tier 2 Scope Rules* for which of them are true of the current version) |
| **3. Evaluated** | `ConstraintResult` / `ConstraintViolation` | Per-constraint evaluation status | Evaluator cost | Operation results: "what passed, failed, or couldn't be resolved?" |

All three tiers use `ConstraintDescriptor` as the canonical identity. Tier 3 results reference back to the descriptor, so callers can always trace a violation to its declaration, kind, scope, and rationale.

#### ConstraintDescriptor

```csharp
public enum ConstraintKind { Invariant = 1, StateResident = 2, StateEntry = 3, StateExit = 4, EventPrecondition = 5 }

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

Appears in `EventInspection.EventEnsures`, `TransitionInspection.Constraints`, and `UpdateInspection.Constraints`. References the descriptor and carries evaluation status: `Satisfied`, `Violated`, or `Unresolvable`.

> **Provisional (G1/G9):** `FieldNames` is a flat string list. The prototype carries a typed target hierarchy (field, event-arg, event, state, definition) for rich UI attribution. When metadata descriptors (D8/R4) are defined, this will become a typed target list that distinguishes WHERE a violation lands.

#### ConstraintViolation (Tier 3 — Commit)

```csharp
public sealed record ConstraintViolation(
    ConstraintDescriptor Constraint,
    string? Because,                              // From the constraint's because clause
    ImmutableArray<FieldSnapshot> RelevantFields, // Field values at evaluation time
    string? FailingSubexpression,                 // Innermost expression that evaluated false
    JsonElement? FailingValue                     // Value at the failure site
);
```

- `Constraint` — the descriptor of the rule that was violated
- `Because` — the `because "..."` text from the constraint declaration, if present
- `RelevantFields` — snapshots of field values at the time of evaluation (for diagnostic context)
- `FailingSubexpression` — the innermost subexpression text that evaluated to false
- `FailingValue` — the `JsonElement` at the failure site (e.g., the computed value that violated the bound)

Appears in `UpdateOutcome.ConstraintsFailed.Violations` — the ingress failures on an update patch, one per constraint the incoming values did not satisfy. Only produced for constraints that are definitively violated (not unresolvable — commit operations have complete data). Ingress failures on event arguments surface through `EventOutcome.InvalidArgs`, which carries a reason string rather than this type; see `result-types.md` for the open question that leaves.

> **Provisional (Shane ruling 2026-05-04):** Shape promoted from minimal 2-field form to the canonical 5-field design. Field population (especially `FailingSubexpression` and `FailingValue`) depends on evaluator instrumentation; all fields nullable/defaultable for fields not yet wired. `FieldNames` dropped — superseded by typed `ImmutableArray<FieldSnapshot>`.

#### Tier 2 Scope Rules

`Version.ApplicableConstraints` includes:

| Current state | Included constraints |
|--------------|---------------------|
| Any state S | All global `rule` declarations |
| State S | `in S ensure` (residency truth) |
| State S | `from S ensure` (exit truth — what a transition out of S must make true; proven at compile time, not evaluated) |
| State S | `on E ensure` for every event E with rows in state S |

`to <State> ensure` constraints are NOT included in `ApplicableConstraints` for the target state — they are transitional, only surfacing in Tier 3 during inspect/fire when the target state is known from row matching. They appear in `TransitionInspection.Constraints` for rows that transition into that state. This is the correct boundary: Tier 2 is "what applies in my current state" — of which the ones true of the current version are those `precept-language-spec.md` § 3A.4, *Between operations nothing is carried except what every reachable configuration satisfies*, names, a `from` ensure is an obligation on a transition out of the state, which configuration of that transition it must hold of being not yet stated (`precept-language-spec.md` § 3A.4, *Known exceptions*, item 12), and an event ensure is a check on that event's arguments; Tier 3 is "what applies to this specific operation" (which includes to-ensures once a target is resolved). Listing a constraint in either tier does not mean an operation evaluates it: rules and state ensures are proven at compile time and no operation evaluates them; what an operation does evaluate is its ingress check (spec §3A.4 phase 1, and phase 8 for why nothing is checked before commit).

For stateless precepts, `ApplicableConstraints` includes all global rules and all event ensures.

### Shared Types

#### FieldAccessInfo

```csharp
public sealed record FieldAccessInfo(
    FieldDescriptor Field,
    FieldAccessMode Mode,
    object? CurrentValue);

public enum FieldAccessMode { Read, Write }
```

Returned by `Version.FieldAccess`. Lists every non-omitted field in the current state with its access mode and current value. `Omit`ted fields are structurally absent — they don't appear in this list.

#### ArgDescriptor

```csharp
public sealed record ArgDescriptor(string Name, TypeKind Type, bool IsOptional, string? DefaultExpression, int SourceLine);
```

Returned by `Version.RequiredArgs(EventDescriptor @event)`. Enables the UI to render typed input controls for event arguments.

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

Each `Set<T>` call is resolved through the registered `TypeRuntime<T>` for zero-boxing conversion to `PreceptValue`. The builder internally produces a `PreceptValue[]` arg slot array populated via the **presence mask** — a `bool[]` of the same length, where `presence[i] == true` means arg slot `i` was explicitly set by the caller and `presence[i] == false` means it was not provided. What an argument the caller did not set holds is stated in `precept-language-spec.md` § 3A.4, phase 1: an argument with a `default` takes its default, whether or not it is also `optional`; an `optional` argument without a `default` stays absent (the corresponding `PreceptValue` slot is the absent sentinel); and only a required argument — neither `optional` nor carrying a `default` — causes `InvalidArgs` at the Fire boundary before the opcode loop begins. The presence mask and slot array are both built during the `Action<IArgBuilder>` invocation and discarded after the call completes — they are not observable from outside the builder.

#### IFieldBuilder

The fluent builder for field patches, used in the typed lane for `Update` and `InspectUpdate`. `Create` takes no field patch: its only input is the initial event's arguments (`precept-language-spec.md` § 3A.4, *The operations*).

```csharp
public interface IFieldBuilder
{
    IFieldBuilder Set<T>(string name, T value);
    IFieldBuilder Clear(string name);   // patch an optional field to not set
}
```

Usage: `fields => fields.Set<string>("ApplicantName", "Jane Doe").Set<decimal>("Amount", 500m)`

Each `Set<T>` call is resolved through the registered `TypeRuntime<T>`. `Clear` names a field with the value not set, which is valid only for an `optional` field; a field neither call names is not patched. On the JSON lane a member whose value is `null` means not set and a member left out is not patched. What a patch value may be — not set for an `optional` field, and for a collection a value with every property its kind keeps — is stated in `precept-language-spec.md` § 3A.4, *What a patch value may be*. The patch is applied atomically — either all fields commit or none do.

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

Catalog-owned metadata record that holds the hot-path serialization delegates for each Precept type. This is the target registration shape for the runtime boundary; it is documented here even though `TypeRuntimeMeta` is not yet carried directly on the `TypeMeta` record in code.

```csharp
public sealed record TypeRuntimeMeta(
    ReadJsonDelegate         ReadJson,          // ref Utf8JsonReader, ref PreceptValue — Phase 1 ingress
    WriteJsonDelegate        WriteJson,         // Utf8JsonWriter, PreceptValue — Phase 8 egress
    ParseStringDelegate      ParseString,       // string → PreceptValue (LS/authoring path)
    FormatStringDelegate     FormatString,      // PreceptValue → string (LS/authoring path)
    BinaryExecutorDelegate[] BinaryExecutors,   // indexed by OperationKind — builder embeds delegates in opcodes at compile time
    UnaryExecutorDelegate[]  UnaryExecutors);   // indexed by OperationKind — builder embeds delegates in opcodes at compile time
```

Active surface for Fire/Inspect/Update/Restore hot paths: `ReadJson`, `WriteJson`. `BinaryExecutors` and `UnaryExecutors` are consumed at build time — the builder embeds executor delegates directly in `BinaryOp`/`UnaryOp` opcodes; the evaluator never indexes these arrays at evaluation time. `ParseString` and `FormatString` are used by the language server and authoring tools. `ParseString` shares the same underlying domain parsers as compile-time `ContentValidation`; `FormatString` produces the canonical string form for that type.

Ownership rules (locked per CC#25): the call site advances to the value token and handles `null`; collection runtimes own structural array/object loops; scalar fields read and write the inline value region directly without boxing intermediaries.

##### JSON wire formats

| Precept type | JSON wire format | Example |
|---|---|---|
| `date` | ISO 8601 string | `"2026-04-15"` |
| `time` | ISO 8601 string | `"14:30:00"` |
| `datetime` | ISO 8601 string | `"2026-04-15T14:30:00"` |
| `instant` | ISO 8601 UTC string | `"2026-04-15T14:30:00Z"` |
| `zoneddatetime` | Object: `{datetime, timezone}` | `{"datetime":"2026-04-15T14:30:00","timezone":"America/New_York"}` |
| `timezone` | IANA string | `"America/New_York"` |
| `duration` | ISO 8601 duration string | `"PT72H"` |
| `period` | ISO 8601 period string | `"P30D"` |
| `money` | Object: `{amount, currency}` | `{"amount":100.00,"currency":"USD"}` |
| `quantity` | Object: `{magnitude, unit}` | `{"magnitude":5.0,"unit":"kg"}` |
| `price` | Object: `{amount, currency, unit}` | `{"amount":4.17,"currency":"USD","unit":"each"}` |
| `exchangerate` | Object: `{rate, from, to}` | `{"rate":1.08,"from":"USD","to":"EUR"}` |
| `currency` | ISO 4217 string | `"USD"` |
| `unitofmeasure` | UCUM string | `"kg"` |
| `dimension` | Dimension name string | `"mass"` |

##### Delegate roles by ingress/egress path

| Delegate | Role |
|---|---|
| `ReadJson` | Shared JSON ingress delegate for Fire, Update, and Restore. Reads the wire format above into the runtime value lane. |
| `WriteJson` | Shared JSON egress delegate for version snapshots, outcomes, and inspection surfaces. |
| `ParseString` | Authoring/LS parse path. Uses the same underlying domain parsers as compile-time `ContentValidation`, but returns runtime values instead of `TypedConstantParseResult`. |
| `FormatString` | Canonical string formatter for authoring and display. Produces the same canonical text shape the compile-time validator reports. |

#### FiredArgs

Event arg egress — appears on `EventOutcome` variants that carry submission context (`Transitioned.Args`, `Applied.Args`, `Rejected.Args`). Allows callers to read back what was submitted as strongly-typed values. `Rejected` carries `FiredArgs` so callers can log or display what was submitted alongside the rejection reason — the submitted args are part of the rejection context, not discarded because the row matched.

```csharp
public sealed class FiredArgs
{
    public static FiredArgs Empty { get; }         // canonical empty instance for no-arg events
    public JsonElement this[string name] { get; }
    public T Get<T>(string name);
    public bool TryGet<T>(string name, out T value); // returns false when an optional arg without a default was not provided
}
```

`Get<T>` uses the registered `TypeRuntime<T>.ToClr` for the conversion. The `JsonElement` indexer returns the raw value for callers that want to inspect or serialize it directly.

`TryGet<T>` is available on `FiredArgs` only (not on `Version`). It returns `false` when an `optional` arg without a `default` was not provided by the caller; an arg with a `default` that was not provided holds its default and `TryGet` returns it (`precept-language-spec.md` § 3A.4, phase 1). It does NOT swallow type errors. Mismatched `T` still throws `InvalidOperationException`. Fields are either resolved or omitted; `TryGet` is only meaningful for optional args that may or may not have been included.

`FiredArgs.Empty` is the canonical instance for events with no arguments.

`Get<string>()` is a universal overload that works for every type Precept supports — analogous to `.ToString()` in .NET. This includes all primitive and scalar types (`int`, `decimal`, `bool`, `string`, `DateTime`, and their Precept-mapped counterparts), all business-domain value types, and any other type registered with the runtime. No separate registered runtime entry is required; `string` is always a valid target. The canonical form for business-domain value types: `Quantity` → UCUM literal (e.g., `"5 kg"`); `Money` → ISO 4217 amount string (e.g., `"100.00 USD"`); `Price` → ratio string (e.g., `"2.50 USD/each"`); `ExchangeRate` → ratio string (e.g., `"1.08 USD/EUR"`); `Currency` → alpha code (e.g., `"USD"`). This applies equally on `Version.Get<string>()` and `FiredArgs.Get<string>()`.

### Correctness Invariant

**Inspection and commit share the same evaluation path.** `InspectFire` runs the same pipeline as `Fire` — same ingress validation of the supplied arguments, same guard evaluation, same exit actions, action chain, `omit` reset and entry actions, same computed-field recomputation, in the same order. It also evaluates nothing `Fire` would not: once a row has certainly won, no later row's guard or actions are evaluated, and values that fail ingress are never computed with (`precept-language-spec.md` § 3A.6, *What a preview evaluates*). The only difference is disposition: inspection discards the working copy; commit promotes it. This is a structural guarantee, not a convention.

Because execution has no constraint-evaluation phase, inspection has none to preview either (spec §3A.6). What inspection can answer is what the operation would actually decide: whether the supplied arguments pass ingress, which row's guard would win, and what the resulting field values would be. Where a rule holds for a reason other than a check — because the compiler proved this handler preserves it — inspection reports the proof and the premises it used rather than a verdict it did not compute.

**Survey grounding:** XState's `transition()` / `getNextSnapshot()` pair — same code path, different output. CEL's `ExhaustiveEval` — same `Interpretable` tree, different evaluation mode. K8s dry-run — same controller logic, different commit behavior.

### Two-Layer Enforcement Model

Precept enforces field constraints across two layers:

1. **Compile-time diagnostics** — 132 diagnostic codes catch invalid configurations statically. The proof engine resolves interval-containment obligations and emits `NumericOverflow` for values that demonstrably exceed declared bounds. Unit-aware normalization (UCUM scale factors) prevents false positives on cross-unit assignments.

2. **Ingress governance** — `TypeRuntimeMeta` / `TypeRuntime` enforce a field's *declared* constraints (qualifiers, bounds) on every externally-sourced value at runtime ingress (event firing, field assignment). This is governance, not deferral: it upholds the declared contract on values that enter at runtime — values the compiler never saw — and is *not* a fallback for fault-safety the compiler couldn't prove. Fault-safety is always proven or rejected at compile time (spec §0.7).

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

Returns the full annotated landscape for an event: every row with `Prospect` (Certain/Possible/Impossible), field snapshots per row, constraint results, and hypothetical result when fully resolvable. `OverallProspect` provides the reduced boolean answer. Every row is reported, but a row is evaluated only as far as `Fire` could reach it: a row after a certain winner is reported `Impossible` without its guard or actions being evaluated, and field snapshots exist only for rows that were simulated (`precept-language-spec.md` § 3A.6).

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

Returns all non-omitted fields with post-patch values, constraint results, and full `EventInspection` for every event defined in the current state — evaluated against the hypothetical field state. This is the "what if I change this field?" query. When the patch would be refused — a field not editable, or a value that fails its ingress check — `Update` applies nothing, so the preview applies nothing either: it reports the refusal, the fields as they are, and no event prospects, because there is no resulting configuration to evaluate them against (`precept-language-spec.md` § 3A.6).

**Field patches re-evaluate event prospects.** Changing a field value shifts which event guards pass. `InspectUpdate` returns the full event landscape so the UI can enable/disable buttons based on the hypothetical state.

**Why InspectFire/InspectUpdate are separate:** Fire and Update are mutually exclusive commit operations — field patches and event args produce a new Version through fundamentally different pipelines (row matching + transition vs. access-mode check + ingress validation). A unified `Inspect(fields?, event?, args?)` would force callers to disentangle which combination was evaluated and would conflate two non-overlapping input/output shapes.

---

## Typed Lane

The typed lane — `Version.Get<T>(string fieldName)` and `FiredArgs.Get<T>(string name)` — provides strongly-typed field and arg access without boxing. It is the preferred access path for in-process callers.

### Get\<T\>() Mechanics

```csharp
// Version field access
decimal amount = version.Get<decimal>("Amount");
string state = version.Get<string>("Status");   // universal overload — works for every type

// FiredArgs access on outcome variants
EventOutcome.Transitioned t = ...;
string reviewer = t.Args.Get<string>("Reviewer");
bool wasApproved = t.Args.Get<bool>("Approved");

// Optional arg with TryGet (FiredArgs only)
if (t.Args.TryGet<string>("Comment", out var comment))
    Console.WriteLine($"Reviewer comment: {comment}");
```

Resolution uses the registered `TypeRuntime<T>` for the field's type — zero boxing for primitives. `Get<string>()` is a universal overload that works for every Precept-supported type; it formats to the canonical string representation.

`TryGet<T>` is available on `FiredArgs` only. It returns `false` when an `optional` arg without a `default` was not provided; a defaulted arg left out holds its default. It does NOT swallow type errors — mismatched `T` still throws.

### Valid CLR Types

`T` must be a type from this closed set:

| Precept type | Valid `T` |
|---|---|
| `integer` | `long` |
| `decimal` | `decimal` |
| `number` | `double` |
| `string`, `text` | `string` |
| `boolean` | `bool` |
| `date` | `LocalDate` |
| `time` | `LocalTime` |
| `datetime` | `LocalDateTime` |
| `instant` | `Instant` |
| `zoneddatetime` | `ZonedDateTime` |
| `timezone` | `DateTimeZone` |
| `duration` | `Duration` (NodaTime) |
| `period` | `Period` (NodaTime) |
| `choice` | `string` |
| `money` | `Money` |
| `currency` | `Currency` |
| `quantity` | `Quantity` |
| `unitofmeasure` | `UnitOfMeasure` |
| `dimension` | `MeasureDimension` |
| `price` | `Price` |
| `exchangerate` | `ExchangeRate` |
| `stateref` | `string` |
| `list of T` (single-type collection) | `IReadOnlyList<TElement>` |
| `list of T keyed by K` (keyed collection) | `IReadOnlyList<KeyedElement<TValue, TKey>>` |
| `map K to T` (lookup collection) | `IReadOnlyDictionary<TKey, TValue>` |

**`Get<string>()`** is universally valid for every type. No separate registration required.

**`KeyedElement<TValue, TKey>`** is a public `readonly record struct` in `namespace Precept.Types`:

```csharp
namespace Precept.Types;
public readonly record struct KeyedElement<TValue, TKey>(TKey Key, TValue Value);
```

**Throws on invalid `T`:** Requesting a `T` not in the valid set for the field's declared type throws `InvalidOperationException` with a descriptive message identifying the field name, declared type, and requested `T`. The runtime uses `FieldDescriptor.ClrType` and `ArgDescriptor.ClrType` (precomputed at Precept Builder time) to validate at the call boundary.

---

## CLR Type Discovery

To discover the valid `T` for `Get<T>()` without calling the method, inspect `ClrType` on the descriptor:

```csharp
// From the Precept definition
foreach (FieldDescriptor fd in precept.Fields)
    Console.WriteLine($"{fd.Name}: {fd.ClrType.Name}");

// From a specific arg
IReadOnlyList<ArgDescriptor> args = version.RequiredArgs("Submit");
foreach (ArgDescriptor ad in args)
    Console.WriteLine($"{ad.Name}: {ad.ClrType.Name}");
```

`FieldDescriptor.ClrType` and `ArgDescriptor.ClrType` are `System.Type` instances computed at Precept Builder time. For collection-typed fields, `ClrType` is the full constructed generic type (e.g., `typeof(IReadOnlyList<long>)` for `list of integer`).

| Discovery surface | What it tells you |
|---|---|
| `FieldDescriptor.ClrType` | Valid `T` for `version.Get<T>(fieldName)` |
| `ArgDescriptor.ClrType` | Valid `T` for `firedArgs.Get<T>(argName)` |
| `FieldSnapshot.ClrType` | Valid `T` when iterating inspection results |

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

**Locked by CC#26 (2026-05-06) — Option 1: Null-state initial version.**

For stateless precepts (no `state` declarations), `CreateInitialVersion` returns a `Version` with `State = null`. This is the contract — not an error, not a degenerate edge case, the honest representation of "no state machine."

**Pipeline steps.** The steps of `Create` and their order are stated once, in `precept-language-spec.md` § 3A.4, *Operation execution order*, and § 3A.5; this table only says how each differs between a stateful and a stateless precept. The state is written once, by default materialization (phase 0); nothing writes it again during construction. `Create` runs the initial event's handling itself, through the phases `Fire` runs, and does not call `Fire`, which refuses the initial event's name (spec § 3A.4 phase 1).

| Step | Stateful precept | Stateless precept |
|------|-----------------|-------------------|
| Build hollow version (phase 0) | Defaults applied, `State = InitialState` | Defaults applied, `State = null` |
| Initial event, if declared: ingress, then the first construction row whose guard is true, then its action chain (phases 1, 2, 5) | Runs | Runs — identical |
| `to <State> ensure` entry constraints | In scope for the initial state — proven at compile time, not evaluated here | Out of scope — no state to enter |
| `in <State> ensure` residency constraints | In scope for the initial state — proven at compile time, not evaluated here | Out of scope — no state to enter |
| Fields the initial state omits | Absent from the configuration construction commits; their storage holds their reset values from default materialization, and no separate `omit` reset runs (spec § 3A.4, phase 0) | Not applicable — no state |
| Entry actions (`to <State> -> …`, phase 6) | Run for the initial state, after the initial event's action chain (or against the defaults if none is declared) | Out of scope — no state to enter |
| Arg ensures (`on <Event> ensure`) | Evaluated at ingress if initial event declared | Evaluated at ingress if initial event declared |
| Field constraints on the supplied arguments | Evaluated at ingress | Evaluated at ingress |
| Global rules | Proven at compile time to be established by construction — not evaluated here | Proven at compile time to be established by construction — not evaluated here |
| Computed field recomputation | Always performed | Always performed |
| Working copy promotion | Unconditional once construction completes | Unconditional once construction completes |

**Contract:**
- `Version.State` is `null` for stateless precepts in all outcomes that include a version (`Created`, `Applied`, `Updated`, `Restored`). A stateless precept never produces `Transitioned` (spec § 3A.4, *The operations*).
- `CreateInitialVersion` returns `EventOutcome.Created(version)` where `version.State == null` when construction succeeds.
- `EventOutcome.Transitioned` is never produced during stateless construction — there are no transitions.
- The `Rejected` and `InvalidArgs` outcomes remain possible; stateless construction does not suppress an authored refusal or an argument that fails its ingress check.

**Compiler guarantee:** The compiler enforces `RequiredFieldsNeedInitialEvent` / `InitialEventMissingAssignments` for stateless precepts exactly as for stateful precepts — same rules, same diagnostics. Stateless is not a weaker contract.

---

## Deliberate Exclusions

- **No `IReadOnlyDictionary<string, object?>` overloads.** Fully obsolete. The two-lane ingress (JSON + typed) covers all callers. No dictionary-based convenience extensions exist or will be added.
- **No fault type definitions.** `Fault` and the fault classes are in `fault-system.md`. The runtime API surface produces structured outcomes only; should a breach of one of the engine's own invariants ever be reported as a structured value, it surfaces as the `EventOutcome.Faulted(Fault)` variant and is never thrown. Whether it is reported that way at all, or stays an unrecoverable exception, is undecided (`fault-system.md` Q3).
- **No result type hierarchy.** Full `EventOutcome`, `UpdateOutcome`, and inspection type shapes are in `result-types.md`. This document covers when and why each operation produces outcomes; `result-types.md` covers the shape of each type.
- **No evaluation logic.** The runtime API delegates all evaluation to the `Evaluator`. No pipeline mechanics live in `Precept.cs` or `Version.cs`.
- **No build-time analysis.** Graph analysis, type checking, and compilation are owned by the compiler pipeline. `Precept.From()` accepts only an error-free `Compilation` — it does not re-analyze.
- **No migration logic.** If the definition changed since data was stored, nothing in the current restore path transforms it; handling that is the post-MVP job of the restore path itself. Restore does **not** re-validate — restored state is trusted as valid at persistence time (spec §0.7), so schema drift is not detected on load. It is not detected later either: no subsequent operation re-governs a value that did not enter through the contract (spec §0.7). A host that restores data written under an older definition has stepped outside the guarantee, and nothing inside the engine will notice.

---

## Cross-References

| Topic | Document |
|---|---|
| Compiler and runtime architectural decisions (R1–R5, D8) | `docs/compiler-and-runtime-design.md` |
| Full result type taxonomy (`EventOutcome`, `UpdateOutcome`, inspection types) | `docs/runtime/result-types.md` |
| Fault system and `Fault` taxonomy | `docs/runtime/fault-system.md` |
| Evaluator — plan execution and pipeline mechanics | `docs/runtime/evaluator.md` |
| Descriptor types used in outcomes and inspections | `docs/runtime/descriptor-types.md` |
| Precept Builder — how the executable model is constructed from `Compilation` | `docs/runtime/precept-builder.md` |

---

## Source Files

| File | Purpose |
|---|---|
| `src/Precept/Runtime/Precept.cs` | Executable model — `Precept.From()`, `Create`, `InspectCreate`, `Restore`, definition-level queries |
| `src/Precept/Runtime/Version.cs` | Entity snapshot — `Fire`, `Update`, `InspectFire`, `InspectUpdate`, field access, structural queries |
