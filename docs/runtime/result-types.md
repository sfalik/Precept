# Result Type Taxonomy

## Status

| Property | Value |
|----------|-------|
| Doc maturity | Design |
| Implementation state | Pending — not yet implemented |
| Source | `src/Precept/Runtime/` |
| Upstream | `compiler-and-runtime-design.md`; `executable-model.md` (D8/R4) |
| Downstream | `fault-system.md`; `runtime-api.md` |

## Contents

- [Overview](#overview)
- [Responsibilities and Boundaries](#responsibilities-and-boundaries)
- [Right-Sizing](#right-sizing)
- [Inputs and Outputs](#inputs-and-outputs)
- [EventOutcome](#eventoutcome)
- [UpdateOutcome](#updateoutcome)
- [Inspection Types](#inspection-types)
  - [Progressive Evaluation Model](#progressive-evaluation-model)
  - [Enums](#enums)
  - [EventInspection](#eventinspection)
  - [UpdateInspection](#updateinspection)
  - [TransitionInspection](#transitioninspection)
  - [RowEffect](#roweffect)
  - [ArgError](#argerror)
  - [Shared Primitives](#shared-primitives)
- [Version API Surface](#version-api-surface)
- [Design Rationale and Decisions](#design-rationale-and-decisions)
  - [Survey Grounding](#survey-grounding)
  - [Review Feedback Incorporated](#review-feedback-incorporated)
- [Innovation](#innovation)
- [Open Questions / Implementation Notes](#open-questions-implementation-notes)
  - [Arg-dependency sets (D8/R4)](#arg-dependency-sets-d8r4)
  - [Transitive unresolvability propagation](#transitive-unresolvability-propagation)
  - [Kleene three-value guard evaluation](#kleene-three-value-guard-evaluation)
- [Deliberate Exclusions](#deliberate-exclusions)
- [Cross-References](#cross-references)
- [Source Files](#source-files)

## Overview

The runtime uses three type families to represent operation results:

1. **EventOutcome** (8 variants) — returned by `Precept.Create` and `Version.Fire`. One committed result.
2. **UpdateOutcome** (4 variants) — returned by `Version.Update`. One committed result.
3. **Inspection types** (EventInspection, UpdateInspection) — returned by `Precept.InspectCreate`, `Version.InspectFire` and `Version.InspectUpdate`. Progressive annotated landscape.

A `Fault` — a breach of one of the engine's own invariants — has its shape settled: should anything ever construct one, it is returned as the `EventOutcome.Faulted(Fault)` variant, a structured value inside the outcome hierarchy, never thrown. Nothing constructs one today and whether anything ever will is undecided (`fault-system.md` Q3). See `fault-system.md`.

## Responsibilities and Boundaries

**OWNS**
- The sealed result type hierarchies for commit operations: `EventOutcome` (8 variants) and `UpdateOutcome` (4 variants)
- The inspection type family: `EventInspection`, `UpdateInspection`, `TransitionInspection`
- Shared primitives: `FieldSnapshot`, `ConstraintResult`
- The `Prospect` and `ConstraintStatus` enums and their propagation rules
- The `Version` API surface (`Fire`, `Update`, `InspectFire`, `InspectUpdate`)

**Does NOT OWN**
- Fault classification — `Fault` and `FaultCode` are defined in `fault-system.md` (surfaced here as the `EventOutcome.Faulted` variant)
- The executable model or evaluation pipeline implementation — see `executable-model.md`
- Constraint descriptors and catalog metadata — declared in the DSL catalog
- DSL parsing and type checking

## Right-Sizing

The result type families are scoped to exactly the operations the `Version` exposes: two commit operations (`Fire`, `Update`) and two inspection counterparts (`InspectFire`, `InspectUpdate`). Each commit operation returns its own sealed hierarchy so callers pattern-match exactly the variants their operation can produce. Inspection operations return an annotated landscape without committing state.

A breach of one of the engine's own invariants — a defect in the compiler, the builder or the evaluator — has one place to go in this hierarchy should it ever arrive as a structured value: the single `EventOutcome.Faulted(Fault)` variant, rather than a thrown exception. Confining it to one variant keeps the rest of the hierarchy business-meaningful while still letting a caller pattern-match the engine-defect case explicitly, with no hidden control flow. Whether an engine defect reaches the caller that way at all, or stays an unrecoverable exception, is undecided (`fault-system.md` Q3).

## Inputs and Outputs

| Direction | Type | Description |
|-----------|------|-------------|
| In | `string eventName` | Event to fire |
| In | `JsonElement? args` / `Action<IArgBuilder>?` | Event arguments — JSON lane or typed lane for `Fire` / `InspectFire` |
| In | `JsonElement? fields` / `Action<IFieldBuilder>?` | Field patch — JSON lane or typed lane for `Update` / `InspectUpdate` |
| Out | `EventOutcome` | Sealed committed result of `Version.Fire` |
| Out | `UpdateOutcome` | Sealed committed result of `Version.Update` |
| Out | `EventInspection` | Annotated landscape from `Version.InspectFire` |
| Out | `UpdateInspection` | Annotated landscape from `Version.InspectUpdate` |

See `runtime-api.md` for the full two-lane ingress API surface (`Version.Fire`, `Version.Update`, `Version.InspectFire`, `Version.InspectUpdate`) and their JSON-lane / typed-lane overloads.

---

## EventOutcome

Sealed hierarchy. Callers pattern-match; any unhandled variant produces a compiler warning.

```csharp
public abstract record EventOutcome
{
    public sealed record Created(Version Result, FiredArgs Args) : EventOutcome;
    public sealed record Transitioned(Version Result, FiredArgs Args, ImmutableArray<FieldMutation> Mutations) : EventOutcome;
    public sealed record Applied(Version Result, FiredArgs Args, ImmutableArray<FieldMutation> Mutations) : EventOutcome;
    public sealed record Rejected(string Reason, FiredArgs Args) : EventOutcome;
    public sealed record InvalidArgs(string Reason) : EventOutcome;
    public sealed record Unmatched(ImmutableArray<TransitionInspection> EvaluatedRows) : EventOutcome;
    public sealed record UndefinedEvent() : EventOutcome;
    public sealed record Faulted(Fault Fault) : EventOutcome;
}
```

| Variant | Meaning | Pipeline stage | Stateless reachable? |
|---------|---------|----------------|---------------------|
| `Created` | Construction succeeded — `Create` only, with or without an initial event; the entity exists in the initial state, or with `State = null` on a stateless precept (spec §3A.2, *Construction outcomes*) | Commit (§3A.4) | Yes |
| `Transitioned` | State change succeeded, new Version in target state; `Args` carries what was submitted; `Mutations` carries per-field before/after diff | Commit (§3A.4) | No (no states) |
| `Applied` | No-transition row or stateless event succeeded, mutations committed; `Args` carries what was submitted; `Mutations` carries per-field before/after diff | Commit (§3A.4) | Yes |
| `Rejected` | Authored `reject` row matched — business prohibition; `Args` carries what was submitted | Dispatch — reject row matched (§3A.4) | Yes |
| `InvalidArgs` | Ingress validation failure — wrong type, unknown key, or an argument value that violates a constraint declared on it | Ingress governance (§3A.4) | Yes |
| `Unmatched` | All guards failed (including `when` precondition) — no row matched; `EvaluatedRows` carries per-candidate guard trace | Dispatch — no row matched (§3A.4) | Yes |
| `UndefinedEvent` | `Fire` only. The event name is not a declared event, or names the initial event, which cannot be fired after construction; or the event is declared but has no rows in the current state | The first two at the start of ingress governance, before any argument is checked (§3A.4 phase 1); the third at dispatch, after the arguments passed ingress (§3A.4 phase 2) | Yes |
| `Faulted` | A breach of one of the engine's own invariants — a defect in the compiler, the builder or the evaluator, not a business outcome | — | Yes |

**`Rejected` vs `InvalidArgs`:** `Rejected` is a business decision authored in the precept (`-> reject "reason"`). `InvalidArgs` is a caller error — the args don't match the event's declared contract. Parallel with `UpdateOutcome.InvalidFields`. DDD's "business rejection vs. invalid input" distinction.

**There is no `EventOutcome.ConstraintsFailed`.** The variant used to mean "the working copy was built and then failed a constraint check." No such check runs: the compiler proved every handler preserves every rule (spec §0.7), and §3A.4's phase order has no constraint-evaluation phase between recomputation and commit. Global rules, state ensures (`in`/`to`/`from`) and event ensures are all proven rather than re-evaluated, so no outcome reports them failing after a mutation.

What does fail at runtime is ingress: an argument value that violates a constraint declared on it. That is `InvalidArgs`, and it is collect-all as spec §3A.4 phase 1 defines it — every failure the check finds is reported, not just the first; a constraint over a value that failed an earlier check, or whose proof relied on a phase-1 constraint that failed, is not evaluated.

> **Open — the shape `InvalidArgs` needs.** `InvalidArgs(string Reason)` carries one string. Collect-all reporting of constraint violations on arguments wants the structured per-violation detail `ConstraintViolation` already carries (`Because`, `RelevantFields`, `FailingSubexpression`), the same way `UpdateOutcome.ConstraintsFailed` carries it for a patch. Whether `InvalidArgs` gains a `Violations` array, splits into two variants (structural arg errors vs. constraint failures on args), or stays a string is an API-shape decision neither the want document nor the specification settles. Left as-is here rather than guessed at.

**`Transitioned` and `Applied` mutations:** `Mutations` is always populated on success — callers never need a second `InspectFire` call to get before/after field diffs. The evaluator computes the diff against the working copy it already maintains during execution.

**`Unmatched` trace:** `EvaluatedRows` uses the same `TransitionInspection` type as `EventInspection.Transitions`, making inspect and commit paths type-consistent. Callers who want to understand why no row matched get the full per-candidate guard trace at no extra cost — guard evaluation was already running during the commit pass.

**`Faulted` semantics:** A `Fault` names a breach of one of the engine's own invariants — a defect in the compiler, the builder or the evaluator. `Faulted` is where it would surface if it were reported as a structured outcome variant rather than as a raw exception at the runtime boundary. Nothing constructs one today; whether anything ever will is undecided (`fault-system.md` Q3). See `fault-system.md` for the `Fault` type definition. MCP `precept_fire` serializes `Faulted` as `{ "outcome": "Faulted", "fault": { ... } }`.

---

## UpdateOutcome

```csharp
public abstract record UpdateOutcome
{
    public sealed record Updated(Version Result) : UpdateOutcome;
    public sealed record ConstraintsFailed(ImmutableArray<ConstraintViolation> Violations) : UpdateOutcome;
    public sealed record FieldNotEditable(string FieldName, FieldAccessMode ActualMode) : UpdateOutcome;
    public sealed record InvalidFields(string Reason) : UpdateOutcome;
}
```

| Variant | Meaning | Pipeline stage |
|---------|---------|----------------|
| `Updated` | Patch passed its ingress check and was applied; new Version committed | Commit |
| `ConstraintsFailed` | A patched value violates a constraint declared on the field it was entering — refused before the patch was applied, collect-all | Ingress governance (§3A.4) |
| `FieldNotEditable` | Field's access mode in the current state (`Readonly`, or `Omit`) prevents direct editing; for a guarded access mode, decided by its guard over the starting configuration | Ingress governance — access mode check (§3A.4 phase 1) |
| `InvalidFields` | Type mismatch or structurally invalid patch | Type check |

> **Open — the shape of a refused `Update`.** Phase 1 of `Update` is collect-all across the whole patch (spec §3A.4 phase 1, §3A.1): every non-editable field, every mistyped value and every failed constraint that phase 1 evaluates is found — a constraint over a value that passed its earlier checks, and whose proof relied on no phase-1 constraint that failed. `FieldNotEditable` names one field, `InvalidFields` carries one string and `ConstraintsFailed` carries constraint violations only, so no single variant can report everything one refused patch can fail on. Whether the variants gain arrays, merge, or are reported in a fixed precedence is an API-shape decision neither the want document nor the specification settles — the same question as the shape of `InvalidArgs` above. What is evaluated does not depend on it.

**`UpdateOutcome.ConstraintsFailed` is an ingress failure, not a post-mutation one.** The constraints it reports are the ones declared on the fields the patch writes, checked as the values enter and before anything derives from them (spec §0.7, *Governance*). The variant name reads as though the check were a sweep over the finished working copy; it is not, and the name should be revisited when the `Update` API is next opened.

---

## Inspection Types

### Progressive Evaluation Model

The evaluator runs the full fire or update pipeline as far as available data allows, annotating each decision point with certainty:

- **Guards** with arg-dependent terms that cannot be resolved remain `Possible`.
- **Set assignments** referencing missing args produce `IsResolved = false` on the resulting FieldSnapshot.
- **Constraints** evaluated against an unresolvable working copy slot produce `ConstraintStatus.Unresolvable`.
- **Derived fields** depending on unresolvable inputs are transitively marked unresolvable.

### Enums

```csharp
// Row-level certainty in first-match routing
public enum Prospect { Certain, Possible, Impossible }

// Per-constraint evaluation result under partial information
public enum ConstraintStatus { Satisfied, Violated, Unresolvable }
```

**Prospect propagation rules (first-match):**
- A `Certain` row makes all subsequent rows `Impossible`. They are reported so without being evaluated — neither guard nor actions — because `Fire` never reaches them (`precept-language-spec.md` § 3A.6, *What a preview evaluates*).
- An `Impossible` row opens up the next row (may become Certain or Possible).
- A `Possible` row leaves subsequent rows at most `Possible`.

**Guard evaluation under partial args uses Kleene three-value logic:**

| Left | Op | Right | Result |
|------|----|-------|--------|
| false | and | Unknown | Impossible (short-circuit) |
| true | and | Unknown | Possible |
| true | or | Unknown | Certain (short-circuit) |
| false | or | Unknown | Possible |
| Unknown | not | — | Possible |

Where "Unknown" = the expression reads a required argument not yet provided, or it was not evaluated because a premise its compile-time proof relied on is itself `Unknown` — an earlier row's guard, the row's own guard, a guarded state action's guard, or an earlier operand of the same `and`/`or` or the condition of an `if` (spec §3A.6, *When inputs are missing*; `evaluator.md` § `EvaluateGuardProspect`). So in `Unknown and X`, where the proof of `X` relied on the first operand, `X` is not evaluated and the result is `Unknown`, reported as `Possible`.

### EventInspection

```csharp
public sealed record EventInspection(
    string EventName,                                   // self-describing — essential when embedded in UpdateInspection.Events
    Prospect OverallProspect,                          // Certain | Possible | Impossible
    ImmutableArray<ArgDescriptor> DeclaredArgs,        // arg contract for this event — drives UX input form rendering
    ImmutableArray<ArgError> ArgErrors,                // non-empty when provided args are structurally invalid
    ImmutableArray<FieldSnapshot> CurrentFields,       // pre-mutation field state — captured once before row loop
    ImmutableArray<TransitionInspection> Transitions,  // per-row inspection detail; empty = UndefinedEvent
    ImmutableArray<ConstraintResult> EventEnsures);    // on<event> constraint results
```

- `EventName` — makes each `EventInspection` self-describing regardless of access path. When embedded inside `UpdateInspection.Events`, consumers correlate results to events without a separate lookup.
- `OverallProspect` — reduced from Transitions: if any transition is `Certain` or `Possible`, the event is enabled. Forced to `Impossible` when `ArgErrors` is non-empty.
- `DeclaredArgs` — the full arg contract from `EventDescriptor.ArgDescriptors`, including both required and optional args. Consumers rendering an input form need all declared args, not just required ones.
- `ArgErrors` — populated when provided args fail structural validation (type mismatch, unknown keys, etc.). When non-empty, guard/constraint evaluation is not invoked; the evaluator returns directly with `OverallProspect = Impossible`.
- `CurrentFields` — pre-mutation snapshot of all fields, captured *once* before the row evaluation loop. This is the entity's current state at the time of inspection. Consumers use it alongside per-row `PostFields` to show before/after comparisons. (Renamed from `FieldSnapshots` to fix a latent bug where the value was overwritten per-row iteration.)
- `Transitions` — empty list = undefined event (no rows/hooks for this event in current state).
- `EventEnsures` — event-scoped `on<event>` constraint results. Currently passes empty array until the constraint plan index is wired for event-keyed inspection. It stays **event-level**. OQ-4 used to ask whether it should move inside `TransitionInspection`, evaluated per row against that row's post-mutation state; that option describes a pipeline the language no longer has. An `on <Event> ensure` validates the caller's arguments at ingress, once, before any row is matched (spec §3A.1), so there is no per-row post-mutation state to evaluate it against and nothing to attribute to a row. What the array should *carry* now that execution has no constraint-evaluation phase is the open question recorded below under *what the `ConstraintResult` arrays mean now* — not where it lives.

Returned by `Version.InspectFire(eventName, args?)`. Also nested inside `UpdateInspection.Events`.

### UpdateInspection

```csharp
public sealed record UpdateInspection(
    ImmutableArray<FieldSnapshot> Fields,
    ImmutableArray<ConstraintResult> Constraints,
    ImmutableArray<EventInspection> Events);
```

- `Fields` — ALL non-omitted fields with post-patch values and recomputed derived fields.
- `Constraints` — rules evaluated against the hypothetical post-patch state.
- `Events` — full EventInspection for every event defined in the current state, evaluated against the hypothetical field state. Undefined events for other states are not included.
- `HypotheticalResult` — non-null when the patch is complete and passes its ingress check.

Returned by `Version.InspectUpdate(fields?)`. When called with no patch, returns the landscape against current field values.

### TransitionInspection

```csharp
public sealed record TransitionInspection(
    Prospect Prospect,
    RowEffect Effect,                                  // TransitionTo | NoTransition | Rejection
    string? GuardSummary,                              // null = guard passed or no guard; populated = guard failed or ambiguous
    ImmutableArray<ConstraintResult> Constraints,
    ImmutableArray<FieldSnapshot> PostFields);
```

- `Prospect` — would this row fire? `Certain` when its guard is certainly true and every earlier row's guard is certainly false (spec §3A.6); `Possible` when its guard is undetermined because an input is missing, or when its guard is true but an earlier row's guard is undetermined (see *Prospect propagation* above); `Impossible` when the guard failed, or when an earlier row certainly wins, in which case this row's guard was not evaluated. The guard alone decides: dispatch is first-matching-guard-wins (spec §3A.4 phase 2), so no constraint result can turn a guard-passing row into a non-firing one.
- `Effect` — a discriminated union encoding the row's outcome kind. Replaces the previous `string? TargetState` nullable encoding. Consumers pattern-match on `Effect` to determine transition target, no-transition, or rejection. See `RowEffect` below.
- `GuardSummary` — human-readable description of the guard condition. All rule failure surfaces carry a human-readable description (contract rule). `null` when no guard applies or when the guard passed without ambiguity. Populated by the runtime when the guard fails or evaluates to `Possible` (ambiguous due to missing args).
- `PostFields` — for a row that was simulated, every field the target state does not omit, as `Fire` would leave it: after exit actions, the row's actions, the `omit` reset, entry actions and recomputation. Fields whose access mode changes upon transition reflect the target state's mode. Empty for a row that was not simulated — a row reported as unable to fire because an earlier row certainly wins, or a row whose guard is certainly false — because a preview evaluates nothing the operation would not (spec §3A.6, *What a preview evaluates*).
- `Constraints` — constraint evaluation results for this specific row.

> **Open — what the `ConstraintResult` arrays mean now.** `TransitionInspection.Constraints`, `UpdateInspection.Constraints` and `EventInspection.EventEnsures` were shaped around a pipeline that evaluated rules against the finished working copy. Execution no longer does that, and spec §3A.6 says inspection has no constraint-evaluation phase to preview because there is none to preview. What the want document asks inspection to report instead is proof provenance — "no check needed here, and here are the premises it was proven from" (`what-i-want-2026-07-16.md:211`) — but neither that document nor the specification says which type carries it. The three arrays are left exactly as they are, and their descriptions above still describe the old model. Deciding whether they become ingress results, proof-provenance records, or a plain report of which declared constraints happen to hold in the previewed configuration is a design question, not a correction.

### RowEffect

```csharp
public abstract record RowEffect
{
    public sealed record TransitionTo(string TargetState) : RowEffect;
    public sealed record NoTransition() : RowEffect;
    public sealed record Rejection(string Reason) : RowEffect;
}
```

A discriminated union encoding what a transition row does upon firing. Constructed from `ExecutionRow.Outcome`:
- `Transition` → `RowEffect.TransitionTo(targetStateName)` — the row transitions the entity to a new state.
- `NoTransition` → `RowEffect.NoTransition()` — the row fires actions without changing state.
- `Reject` → `RowEffect.Rejection(row.RejectReason ?? "")` — the row rejects with a human-readable reason from the `because` clause (CC#11).

### ArgError

```csharp
public sealed record ArgError(
    string ArgName,
    string Reason);
```

Reported when provided event args fail structural validation before the evaluator is invoked. `Reason` is a plain string (e.g., "expected integer, got string") — matches the field edit error pattern (`ConstraintViolation.Because`, `InvalidFields.Reason`). No structured error code; one can be added when a concrete need arises.

### Shared Primitives

```csharp
public sealed record FieldMutation(
    string FieldName,
    JsonElement? Before,
    JsonElement? After);

public sealed record FieldSnapshot(
    string FieldName,
    FieldAccessMode Mode,
    string FieldType,
    bool IsResolved,
    JsonElement? Value,
    Type ClrType);

public sealed record ConstraintResult(
    ConstraintDescriptor Constraint,
    IReadOnlyList<string> FieldNames,
    ConstraintStatus Status);
```

**`FieldMutation`:** Carries a single field's before/after diff for successful `Transitioned` and `Applied` outcomes. `Before` is `null` for fields that were unset prior to the event; `After` is `null` for fields cleared by the event. Both are `JsonElement?` to match the existing `FieldSnapshot.Value` JSON representation. Only fields whose value actually changed appear in the `Mutations` array — unchanged fields are omitted.

**`FieldSnapshot.IsResolved`:** `false` when the post-mutation value could not be computed because a required arg dependency is missing. `Value` is `null` when `IsResolved == false` (unresolved computed field or structurally absent). Prevents ambiguity between genuinely-null optional fields (`IsResolved = true, Value` is a JSON null) and stuck assignments.

**`FieldSnapshot.ClrType`:** The valid CLR type for `Get<T>()` on this field, precomputed from `FieldDescriptor.ClrType`. Carries the full constructed generic type for collection fields (e.g., `typeof(IReadOnlyList<long>)` for `list of integer`).

**`ConstraintResult.Constraint`:** References the `ConstraintDescriptor` that was evaluated — callers can access kind, scope, anchor, guard, and `because` rationale through the descriptor. This is Tier 3 of the three-tier constraint exposure model (see `runtime-api.md` § Constraint Exposure Model).

**`ConstraintResult.FieldNames`:** Identifies which fields a constraint relates to. Enables per-field inline validation in the UI. Empty for entity-level constraints with no specific field attribution. Computed field references are transitively expanded to the stored fields the user can actually change.

---

## Version API Surface

The `Version` API surface is documented in `runtime-api.md` § Operations — Version Entity Snapshot. That section is authoritative for the `Fire`, `Update`, `InspectFire`, and `InspectUpdate` overloads (both JSON lane and typed lane), the field indexer, `FieldAccess`, `AvailableEvents`, `RequiredArgs`, and `ApplicableConstraints`.

For reference, the two-lane commit and inspect overloads:

```csharp
public sealed record Version
{
    public Precept Precept { get; }
    public string? State { get; }
    public JsonElement this[string fieldName] { get; }
    public T Get<T>(string fieldName);
    public IReadOnlyList<FieldAccessInfo> FieldAccess { get; }
    public IReadOnlyList<EventDescriptor> AvailableEvents { get; }
    public IReadOnlyList<ArgDescriptor> RequiredArgs(EventDescriptor @event);
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

public sealed record FieldAccessInfo(FieldDescriptor Field, FieldAccessMode Mode, object? CurrentValue);
public enum FieldAccessMode { Read, Write }
```

**Two lanes for commit and inspect:** Every operation has a JSON lane (`JsonElement?`) for wire callers and a typed lane (`Action<IArgBuilder>?` / `Action<IFieldBuilder>?`) for in-process callers. There are no `IReadOnlyDictionary<string, object?>` overloads. **`Restore` is JSON-only** — no typed overload; restoration is a hydration path from persisted storage.

**Four commit/inspect pairs, consistent naming:** Every commit verb (`Fire`, `Update`) has an `Inspect___` counterpart. Fire/Update require complete input and return one committed outcome. InspectFire/InspectUpdate accept optional partial input and return an annotated landscape.

**Why InspectFire/InspectUpdate are separate:** Fire and Update are mutually exclusive commit operations — field patches and event args produce a new Version through fundamentally different pipelines (row matching + transition vs. access-mode check + ingress validation of the patched values). A unified `Inspect(fields?, event?, args?)` would force callers to disentangle which combination was evaluated and would conflate two non-overlapping input/output shapes.

**Why Update, not Edit:** Renamed from `Edit` to avoid confusion with `edit` declarations in the DSL (per-state field access blocks). `Update` describes what the caller does to field values; `edit` describes what the precept author declares.

**Always full landscape:** Both inspection methods return the full annotated result. At DSL scale (10–50 fields, 5–15 events) the full pipeline is sub-millisecond. A lightweight mode can be added later without changing existing signatures.

---

## Design Rationale and Decisions

### Survey Grounding

| System | Relevant finding | How it informs R2 |
|--------|-----------------|-------------------|
| Temporal Workflow | `WorkflowResult<T>`, `NonDeterminismException` for system errors | Faults throw (not return). Business outcomes are typed results. |
| XState v5 | 4 snapshot statuses, `transition()` returns same `State` type | Preview/commit return type parity. Inspection returns same structural depth. |
| Rust `Result<T,E>` | Exhaustive match required by compiler | Sealed hierarchy with no catch-all — every variant handled. |
| F# discriminated unions | Exhaustive match, per-case payload | Same pattern in C# via sealed records. |
| K8s dry-run | Same response type as real operation, `managedFields` annotation | Commit and inspection share structural vocabulary. |
| Terraform plan | Progressive resolution — known values vs. `(known after apply)` | `FieldSnapshot.IsResolved` and `ConstraintStatus.Unresolvable` follow this pattern. |
| OPA `deny[msg]` | Collect-all violation set | Ingress validation reports every violation it finds, not the first (spec §3A.4 phase 1 states what it leaves unevaluated) — `InvalidArgs` for event and construction arguments, `UpdateOutcome.ConstraintsFailed` for a patch. Collect-all applies at ingress only; there is no post-mutation evaluation left to collect from. |
| DDD per-command results | Each command returns its own result type | Two families (EventOutcome, UpdateOutcome), not unified. |
| gRPC status codes | FAILED_PRECONDITION / INVALID_ARGUMENT / ABORTED | Maps to Rejected / InvalidArgs / Unmatched. |

### Review Feedback Incorporated

This design incorporates findings from three concurrent reviews (2026-04-22):

| Reviewer | Verdict | Key contributions |
|----------|---------|-------------------|
| Frank (Architect) | CHANGES_NEEDED → resolved | InvalidArgs variant, Rejected scope documentation, survey citation corrections |
| George (Runtime) | FEASIBLE_WITH_CAVEATS | FieldSnapshot null ambiguity (→ IsResolved), arg-dependency sets, transitive unresolvability, Kleene logic |
| Elaine (UX) | UX_CONCERNS | ConstraintResult field attribution, OverallProspect on EventInspection, ArgInfo, IsResolved |

---

## Innovation

No surveyed system has progressive annotation with per-constraint certainty, per-row prospect, and field-level attribution. The inspection model is purpose-built for Precept's progressive evaluation requirement — evaluating pipelines as far as available data allows, annotating each decision point with certainty, and surfacing a complete landscape without requiring complete inputs.

---

## Open Questions / Implementation Notes

These are deferred to the executable model contract (D8/R4) and evaluator design doc, but recorded here as binding requirements from R2:

### Arg-dependency sets (D8/R4)

Each expression node in the executable model must carry a dependency annotation indicating which event arguments appear in its subtree. This is a lowering-time tree walk during `Precept.From()`, not a runtime check. The type checker already resolves all identifier references — computing arg-dependency sets is a straightforward extension.

Without this, the evaluator cannot distinguish field-only guards (fully resolvable) from arg-dependent guards (possibly resolvable) without attempting evaluation and catching failures.

### Transitive unresolvability propagation

When a `set` assignment references a missing arg, the working copy slot is marked unresolvable (not null). Derived fields computed from unresolvable inputs are transitively unresolvable. The recomputation loop must check for unresolvable inputs and propagate accordingly — otherwise expressions evaluated against the working copy will receive wrong values and report spurious results.

The working copy data structure must carry an `IsResolved` flag per slot, mirroring the output `FieldSnapshot.IsResolved`.

### Kleene three-value guard evaluation

Guard evaluation under partial args must use Kleene three-value logic. Comparisons referencing a missing arg — a required argument that was not supplied; an `optional` argument left out is not set, not missing (spec §3A.6, *When inputs are missing*) — produce Unknown. Boolean operators propagate Unknown according to the truth table above. The evaluator must not throw or fault when encountering an unresolvable subexpression — it must propagate the Unknown signal through the expression tree and map the final result to a `Prospect` value.

---

## Deliberate Exclusions

| Concern | Disposition |
|---------|-------------|
| Lightweight inspection mode (E5) | Always return full landscape. At DSL scale (10–50 fields, 5–15 events) the full pipeline is sub-millisecond — premature optimization would add API surface for no measurable gain. A lightweight mode can be added later without changing existing signatures. |
| `Possible` conflating data-insufficient and logically-uncertain (E6) | Three-value Prospect is sufficient. A fourth value (`ArgDependent`) was considered and rejected: the UI already knows which args are missing via `RequiredArgs` and can infer arg-absence from context. A fourth value would complicate pattern matching at every consumer for a signal that's redundant with information already on the Version surface. |
| `Version.Precept` contract (E7) | Separate design decision. Not part of R2. |
| Migration distance from prototype (G8) | Full API break acknowledged. Implementation planning concern. |

---

## Cross-References

- `compiler-and-runtime-design.md` — compiler/runtime architecture overview
- `executable-model.md` (D8/R4) — arg-dependency sets and evaluator contract
- `fault-system.md` — `Fault`/`FaultCode`; should a breach of one of the engine's own invariants ever be reported as a structured value, it is returned as the `EventOutcome.Faulted(Fault)` variant
- `runtime-api.md` — full `Version` API spec; § Constraint Exposure Model (three-tier model, Tier 3 = `ConstraintResult.Constraint`)

---

## Source Files

`src/Precept/Runtime/` — stub; not yet implemented. Expected types:

- `EventOutcome.cs` — sealed record hierarchy (with nested variants)
- `UpdateOutcome.cs` — sealed record hierarchy (with nested variants)
- `EventInspection.cs` — inspection result type
- `UpdateInspection.cs` — inspection result type
- `TransitionInspection.cs` — transition-level inspection (replaces `RowInspection.cs`)
- `RowEffect.cs` — discriminated union for transition row outcome kind
- `ArgError.cs` — arg validation error record
- `FieldSnapshot.cs` — shared primitive
- `ConstraintResult.cs` — shared primitive
- `FiredArgs.cs` — event arg egress; appears on Transitioned, Applied, Rejected
- `Prospect.cs` — enum
- `ConstraintStatus.cs` — enum
- `Version.cs` — runtime Version record
