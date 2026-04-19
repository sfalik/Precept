# Engine Design

Date: 2026-04-19

Status: **Implemented** — `PreceptEngine` fully operational. All four instance operations (CreateInstance, Inspect, Fire, Update) shipped. Atomic execution model, outcome taxonomy, stateless and stateful modes, computed field pipeline, and guarded edit evaluation all active.

> **Research grounding:** Architecture overview: [ArchitectureDesign.md](ArchitectureDesign.md). Public API surface: [RuntimeApiDesign.md](RuntimeApiDesign.md). Evaluator component: [EvaluatorDesign.md](EvaluatorDesign.md) *(draft — issue #115)*. Implementation: `src/Precept/Dsl/PreceptRuntime.cs` (`PreceptEngine`, ~L1–2820).

> **Documented assumptions.** Several behavioral details are implemented but not explicitly commented in the source. This document states them as design facts; they are flagged with *(assumption)* for Shane's review. See [§ Documented Assumptions](#documented-assumptions) at the end of this document.

---

## Overview

`PreceptEngine` is the run-time host for a compiled Precept definition. It is the boundary object between the Compile-Time phase and the Run-Time phase. The compiler produces it; everything the runtime does flows through it.

The engine holds the compiled definition state: the field map, transition table, collection field map, computed field evaluation order, static and guarded edit permissions, and state action map. These are all baked in at construction time from the `PreceptDefinition` model. The engine is immutable after construction — its internal state never changes. Multiple entity instances can run concurrently against the same engine.

The engine exposes four operations on `PreceptInstance` objects: `CreateInstance`, `Inspect`, `Fire`, and `Update`. All four operations that evaluate expressions route through the same expression evaluator — the same component, with the same isolation and determinism guarantees, at every invocation. The engine does not evaluate expressions itself; it orchestrates evaluation by building the correct evaluation context and handing it to `PreceptExpressionRuntimeEvaluator`.

---

## Architecture Position

```
PreceptCompiler.CompileFromText / Compile
    │
    ├─ Parse → PreceptDefinition
    ├─ TypeChecker.Check → TypeCheckResult
    │       (injects ComputedFieldOrder into engine at construction)
    │
    └─ new PreceptEngine(definition, typeCheckResult)
            │
            └─ Instance lifecycle (CreateInstance / Inspect / Fire / Update)
                    │
                    └─ PreceptExpressionRuntimeEvaluator (per-expression invocations)
```

The engine is downstream of the compiler and upstream of everything that works with entity instances. Callers who have a `PreceptEngine` have a validated, immutable contract. There is no other path to an engine.

---

## Engine Construction

Construction is performed by `PreceptCompiler`, not by callers directly. The compiler invokes `TypeChecker.Check` first and injects the resulting `TypeCheckResult` — specifically `ComputedFieldOrder` and the global `ProofContext` — into the engine. The engine does not independently determine computed field evaluation order; the type checker owns topological sort.

At construction, the engine precomputes the following internal structures:

| Structure | Type | What it holds |
|---|---|---|
| `_transitionRowMap` | `Dictionary<(State, Event), List<TransitionRow>>` | All transition rows, keyed by (state, event). First-match wins at runtime. Keys are ordinal case-sensitive. |
| `_editableFieldsByState` | `Dictionary<string, HashSet<string>>` | Statically declared editable fields per state (unguarded `edit` blocks only). Precomputed; `["all"]` expands to all non-computed scalar + collection fields. |
| `_guardedEditBlocks` | `List<GuardedEditBlock>` | Guarded `edit` blocks (those with `when` guards). NOT precomputed — evaluated per-call. Fail-closed: guard evaluation exception → field not granted. |
| `_stateActionMap` | `Dictionary<(EnsureAnchor, State), StateAction>` | Entry (`to`) and exit (`from`) automatic mutations per state. |
| `_computedFieldOrder` | `List<string>?` | Topological evaluation order for computed fields. Null if no computed fields. Injected from `TypeCheckResult`. |
| `_computedFieldDependencies` | `Dictionary<string, HashSet<string>>` | Transitive stored-field dependency map for computed fields. Used when a constraint failure must name the user-visible field, not an intermediate computed field. |

The `IsStateless` property is derived at every call from `States.Count == 0` — not a flag set at construction. This means stateless detection is always consistent with the actual definition model.

---

## PreceptInstance

`PreceptInstance` is an immutable record produced by all successful operations:

```csharp
public sealed record PreceptInstance(
    string WorkflowName,
    string? CurrentState,
    string? LastEvent,
    DateTimeOffset UpdatedAt,
    IReadOnlyDictionary<string, object?> InstanceData);
```

`CurrentState` is `null` for stateless instances. `LastEvent` is `null` at creation. `InstanceData` is in **clean/public format**: scalar fields as their runtime types, collection fields as plain `List<object>`. No constraint state, no evaluation metadata, no internal representation is embedded.

The engine maintains a separate **internal data format** during operations: collections are stored under `__collection__fieldName` keys as `CollectionValue` objects (carrying typed element metadata). The `HydrateInstanceData` method converts clean → internal before any evaluator call; `DehydrateData` converts internal → clean before producing `InstanceData` in results. Callers never see the internal format. Every evaluator invocation operates against the internal format.

Instance immutability is not just a record property — it is an operational guarantee. Every operation that succeeds produces a **new** `PreceptInstance` via `instance with { ... }`. The caller's original instance is never modified, never invalidated. Callers can hold references to multiple historical `PreceptInstance` values from the same engine.

---

## Atomic Execution Model

`Fire` and `Update` share the same atomicity model:

```
1. Build working copy:
      updatedData = new Dictionary<>(HydrateInstanceData(instance.InstanceData))
      workingCollections = CloneCollections(updatedData)

2. Execute mutations on working copy
      (row mutations / field patch operations / state actions)

3. CommitCollections(updatedData, workingCollections)
4. RecomputeDerivedFields(updatedData)     ← evaluator invoked
5. Evaluate constraints against working copy
      (EvaluateRules + EvaluateStateEnsures)  ← evaluator invoked

6a. If all constraints pass:
        return new PreceptInstance(... InstanceData: DehydrateData(updatedData))

6b. If any constraint fails:
        discard working copy, return failure outcome
        caller's PreceptInstance is unchanged
```

The input `PreceptInstance` is never mutated at any step. If step 5 fails, the working copy is simply not used — the allocations are discarded. There is no rollback; there is nothing to roll back because the committed instance was never written.

`Inspect` uses the same working copy pattern (steps 1–5) but skips step 6a. It returns the predicted outcome without producing an updated `PreceptInstance`.

---

## CreateInstance

Two overloads:

| Overload | Use |
|---|---|
| `CreateInstance(instanceData?)` | Stateless precepts, or stateful precepts using `initial state` |
| `CreateInstance(string initialState, instanceData?)` | Stateful precepts with explicit initial state |

**Validation performed:**
- C33: `initialState` must be non-empty
- C34: `initialState` must be a declared state
- C35: `instanceData` fields must satisfy the type/nullability contract (`TryValidateDataContract`)

**Validation NOT performed:**
- Rules and state ensures are NOT evaluated at CreateInstance. Rule satisfaction at creation time is guaranteed at compile time via C29/C30 (the type checker validates that the initial state's constraints are satisfiable). CreateInstance is a structural operation, not a constraint-evaluation operation.

**Computed fields:** `RecomputeDerivedFields` is called during `BuildInitialInstanceData`. This invokes the evaluator for any computed fields. Caller-provided values for computed fields are rejected with an exception — the "Terraform model": explicit rejection, not silent override.

**Collections:** Collection fields are initialized as empty `List<object>` by default. Callers can override via the optional `instanceData` parameter.

---

## Inspect

`PreceptEngine` has three `Inspect` overloads with different shapes:

### `Inspect(instance, eventName, eventArguments?)` — single-event simulation

Simulates what a named event would do. This is a **full simulation** of the Fire pipeline on a working copy — not a transition table lookup.

**Pre-flight:** `CheckCompatibility(instance)` runs first. If the instance is not compatible (e.g., it was produced by a different engine version), returns `Undefined`.

**Stateless hard wall:** Stateless precepts have no transition surface. Returns `Undefined` with a stateless message.

**Fast-path guard pre-check:** If ALL transition rows for `(currentState, eventName)` have `when` guards, the engine evaluates those guards against instance data only (no event arguments). If no guard passes, returns `Unmatched` immediately. This is a discovery optimization — callers can determine that an event is not applicable in the current data state without providing event arguments.

**Full simulation pipeline (when a match is possible):**
1. Validate event arguments (if provided)
2. Evaluate event ensures (if event arguments provided; args-only context — instance fields NOT available in event ensures)
3. Resolve transition via `ResolveTransition` (first-match guard evaluation)
4. On match: exit state actions → row mutations → entry state actions → `CommitCollections` → `RecomputeDerivedFields` → `CollectConstraintViolations`
5. Return predicted `EventInspectionResult`

All simulation operates on a working copy (`simulatedData`, `simulatedCollections`). The input instance is never touched.

**Possible results:** `Undefined`, `Unmatched`, `Rejected`, `NoTransition`, `ConstraintFailure`, `Transitioned`.

### `Inspect(instance)` — bulk event discovery

Returns an `InspectionResult` covering all outgoing events from the current state. **Only events that have at least one transition row for the current state are inspected** — events with no rows for the current state are absent from the result. For stateless precepts, all declared events are returned as `Undefined`, and editable field information is always built and attached.

The result includes `EditableFields` — the union of statically declared and currently-guarded editable fields — for all cases.

### `Inspect(instance, patch)` — update simulation

Simulates an `Update` patch without committing. Applies the patch to a working copy, evaluates rules and in-state ensures, and returns an `InspectionResult` with any violations surfaced in `PreceptEditableFieldInfo.Violation`. This is the mechanism behind real-time field editing feedback.

---

## Fire

Fire is the mutating event execution operation.

### Pre-flight

1. `CheckCompatibility(instance)` — if not compatible, return `Undefined`
2. Stateless hard wall — stateless precepts return `Undefined`
3. Event argument validation (`TryValidateEventArguments`)

### Event ensures (Stage 1)

Event ensures are evaluated against an **args-only context** built via `BuildDirectEvaluationData(eventName, eventArguments)`. This context contains event arguments but NOT instance field values. Event ensures cannot reference instance fields. This is by design: event ensures validate input arguments as a pre-condition, before any instance state is examined.

### Transition resolution (Stage 2)

`ResolveTransition` performs first-match routing through the transition table:

- Rows are evaluated in declaration order
- The first row whose `when` guard passes (or that has no guard) is selected
- All subsequent rows are ignored

Resolution outcomes:
- `Undefined` — no rows defined for `(currentState, eventName)`
- `Accepted` — a transition row matched
- `NoTransition` — a no-transition row matched
- `RejectedByRow` — an explicit `reject` row matched
- `NotApplicable` — rows exist but all guards failed (maps to `Unmatched` in results)

### No-transition branch

When a no-transition row matches:

1. Row mutations execute on working copy
2. `CommitCollections` + `RecomputeDerivedFields`
3. Rules + `in`-state ensures evaluated

**Exit and entry state actions do NOT run** — there is no state change, so no anchor transitions.

### Transition branch

When a transition row matches:

1. Exit state actions (`EnsureAnchor.From`) on working copy
2. Row mutations on working copy
3. Entry state actions (`EnsureAnchor.To`) on working copy
4. `CommitCollections` + `RecomputeDerivedFields`
5. `CollectConstraintViolations` — rules first, then `from` ensures, then `to` ensures, then `in` ensures

### Sequential mutation semantics

Set assignments within a row execute sequentially against the shared working copy. Each assignment calls `BuildEvaluationDataWithCollections`, which reads from the current state of the working copy — including values written by earlier assignments in the same row. This "read-your-writes" semantics is intentional: later assignments in a row can reference values set by earlier assignments.

### Commit or discard

If all constraints pass: promote working copy → new `PreceptInstance` with updated `CurrentState`, `LastEvent`, `UpdatedAt`, and `InstanceData`. Return `FireResult` with outcome `Transition` or `NoTransition`.

If any constraint fails: discard working copy. Return `FireResult` with outcome `ConstraintFailure` (if rules/ensures failed) or `Rejected` (if a `reject` row matched). Caller's instance unchanged.

---

## Update

Update applies a direct field edit to an instance without firing an event.

### Pipeline stages

**Stage 1 — Conflict detection + computed field rejection.** The patch is validated for conflicting operations (e.g., `Set` and `Replace` on the same field). Computed fields are explicitly rejected: "Cannot update computed field — computed fields are derived automatically."

**Stage 2 — Editability check.** All patch fields must be editable in the current state. Editability is the union of:
- Static editable fields from `_editableFieldsByState` (precomputed, unguarded `edit` blocks)
- Dynamic editable fields from guarded `edit` blocks that pass their guards (evaluated per-call)

Guarded edit evaluation is fail-closed: if a guard expression throws, the field is treated as not editable. This prevents a broken guard from silently granting broader access than the author intended.

**Stage 3 — Type check.** Each patch operation is validated against the field's declared type. Unknown fields return an explicit error.

**Stage 4 — Atomic mutation.** All operations apply to a working copy. Same pattern as Fire: `updatedData` + `workingCollections`, then `CommitCollections` + `RecomputeDerivedFields`.

**Stage 5 — Constraint evaluation.** Rules + `in`-state ensures only. `from`/`to` ensures do not run — there is no state transition. Event ensures do not run — there is no event.

**Stage 6 — Commit or discard.** On success: new `PreceptInstance` with updated `InstanceData` and `UpdatedAt`. On failure (`ConstraintFailure` or `UneditableField`): discard working copy, return failure with per-field violation messages.

---

## Outcome Taxonomy

All outcomes derive from `TransitionOutcome` (or the Update-specific `UpdateOutcome`).

| Outcome | Operations | `IsSuccess` | `UpdatedInstance` | Meaning |
|---|---|---|---|---|
| `Transition` | Fire | ✓ | non-null | State change committed |
| `NoTransition` | Fire | ✓ | non-null | In-place mutations committed, no state change |
| `Rejected` | Fire, Inspect | ✗ | null | Explicit `reject` row — designed prohibition |
| `ConstraintFailure` | Fire, Inspect, Update | ✗ | null | Post-mutation data would violate rule or ensure |
| `Unmatched` | Fire, Inspect | ✗ | null | Rows exist but all guards failed for current data |
| `Undefined` | Fire, Inspect | ✗ | null | No rows defined for this event in this state |
| `UneditableField` | Update | ✗ | null | Patch targets a field not editable in current state |
| `Update` (outcome) | Update | ✓ | non-null | Direct field edit committed |

**`Rejected` vs `ConstraintFailure`:** These serve different diagnostic purposes. `Rejected` means the event is explicitly prohibited by the definition — a `reject` row is a designed prohibition, not a data validation failure. `ConstraintFailure` means the event pipeline would succeed (a row matched, mutations executed) but the result would violate a rule or ensure. Callers should diagnose these differently: a `Rejected` outcome may prompt a user message about why the action is not permitted; a `ConstraintFailure` outcome should surface the specific constraint violation to guide the user toward a valid state.

**`Undefined` vs `Unmatched`:** `Undefined` is a routing gap — no transition surface is defined for this event in this state. `Unmatched` is a data condition — routing exists but the current field values do not satisfy any guard. Callers should diagnose these differently: `Undefined` suggests a definition or usage error; `Unmatched` means the entity needs different field values before this event can fire.

**`Transition` and `NoTransition` are both successes.** `NoTransition` is not a failure and not a no-op. It represents an event that executes mutations without moving to a new state. Exit and entry state actions do not run for `NoTransition`, but the mutations and constraint evaluation run in full.

**Violation ordering** *(assumption)*: Within a `CollectConstraintViolations` call, rules are evaluated first, then ensures in anchor order (`from` → `to` → `in`), each in declaration order within the model. This ordering is treated as a contract — callers who receive `ConstraintFailure` results can rely on violations being returned in this order. *(Flagged for Shane's review.)*

---

## Engine↔Evaluator Boundary

The engine does not evaluate expressions directly. It delegates all expression evaluation to `PreceptExpressionRuntimeEvaluator.Evaluate`. The engine's responsibility is building the correct evaluation context for each invocation.

The engine uses two context builders:

| Builder | When used | What it includes |
|---|---|---|
| `BuildEvaluationData(internalData, event, args)` | Guard evaluation during `ResolveTransition`; initial Inspect pass | Static snapshot of internal data + event args |
| `BuildEvaluationDataWithCollections(data, collections, event, args)` | Set-assignment expressions during mutations | Working collection copy — reads the current state of mutations in progress |

The second builder gives "read-your-writes" semantics within a mutation block: later expressions in the same row can observe values written by earlier expressions in the same row. This is intentional — it allows multi-step computations to be expressed as sequential assignments without requiring an intermediate event.

The evaluator is expression-isolated: it cannot mutate entity state, trigger side effects, or observe anything outside the provided context. The engine relies on this isolation — specifically, `Inspect` depends on the evaluator being safe to call against a working copy with no observable effect on the original instance or any shared state.

---

## CheckCompatibility

Every public operation calls `CheckCompatibility(instance)` as a pre-flight. `CheckCompatibility` verifies that the instance was produced by an engine whose rules and structure are compatible with the current engine.

`CheckCompatibility` is not a lightweight structural check. It evaluates rules and `in`-state ensures against the current instance data. An instance produced by an older engine version may become incompatible with a newer engine if new rules or ensures were added.

**This is by design** *(assumption)*: governed integrity means the current definition governs the entity at every moment — including instances produced against an older version of the definition. Precept does not grandfather old instances against new rules. When a definition is updated, all instances should be checked for compatibility and any incompatible instances surfaced for remediation. *(Flagged for Shane's review.)*

---

## Stateless vs Stateful Execution

`IsStateless` is derived from `States.Count == 0` at every call.

| Behavior | Stateful | Stateless |
|---|---|---|
| `CreateInstance` overload | Requires `initialState` (or uses `InitialState`) | No state argument |
| `CurrentState` on `PreceptInstance` | Non-null string | `null` |
| `Inspect(instance, eventName)` | Full simulation | Hard wall — returns `Undefined` |
| `Inspect(instance)` | Events from current state only | All declared events returned as `Undefined` + editable field info |
| `Fire` | Full event pipeline | Hard wall — returns `Undefined` |
| `Update` | Editability by state | Editability from root `edit` blocks |
| Constraint evaluation | Rules + `in`/`from`/`to` ensures | Rules + unconditional ensures only |

Stateless precepts expose full field editing (via `Update`) and full constraint enforcement (rules + ensures) with no lifecycle dimension. The same engine, evaluator, and operation surface apply — only the state-routing paths are bypassed.

---

## Philosophy-Rooted Design Principles

The following principles govern the engine's design, traced to Precept's core philosophy commitments.

1. **The engine is the runtime realization of prevention.** An engine can only exist if its definition compiled without errors. The engine's construction gate — enforced by `PreceptCompiler`, not the engine constructor — is the architectural mechanism that makes "invalid configurations cannot exist" true before any instance is created. A caller who has a `PreceptEngine` has already passed the compile-time gate. *(Philosophy: "Prevention, not detection.")*

2. **Immutability is a correctness guarantee, not a convenience.** The engine is immutable after construction. `PreceptInstance` records are immutable after creation. Operations produce new instances rather than modifying existing ones. This is not defensive programming — it is the mechanism that makes concurrent usage safe, makes Inspect non-mutating provable (not disciplinary), and makes historical instance retention natural. *(Philosophy: "The engine is deterministic — same definition, same data, same outcome.")*

3. **Atomic execution with rollback-free discard is the only safe mutation model.** `Fire` and `Update` operate on working copies. If constraints fail, the working copy is discarded. There is no committed-then-rolled-back path, no write-then-check pattern, no window where an invalid configuration exists in memory. The invalid configuration never existed — it was only ever a working copy. *(Philosophy: "Invalid configurations structurally impossible.")*

4. **Inspect is a full simulation, not a lookup.** Inspect executes the complete event pipeline — guards, state actions, mutations, derived field recomputation, constraint evaluation — on a working copy. Making Inspect a table lookup would be faster but would make it dishonest: it would not account for the evaluated guards, the mutations that change derived field values, or the constraint violations that would block the transition. An honest Inspect requires full simulation. The cost is the cost of knowing the truth. *(Philosophy: "Full inspectability — preview every possible action without executing anything.")*

5. **The outcome taxonomy carries diagnostic meaning.** A 2-value pass/fail outcome loses the information callers need to respond correctly. `Rejected` and `ConstraintFailure` have different causes and require different responses. `Undefined` and `Unmatched` represent different conditions and require different diagnostic messages. `Transition` and `NoTransition` are both successes but have different implications for entry/exit actions and UI state. The 6-value taxonomy is the minimum resolution that preserves diagnostic fidelity. *(Philosophy: "Nothing is hidden.")*

6. **Guarded edit blocks are fail-closed.** If a guarded `edit` block's guard expression throws during evaluation, the field is treated as not editable. This is the conservative choice: a broken guard should not silently grant edit access. Fail-open would mean a guard exception is indistinguishable from a passing guard — producing surprising data mutations with no visible failure signal. Fail-closed surfaces the problem without unauthorized mutation. *(Philosophy: "Prevention, not detection.")*

7. **Event ensures are argument-scoped, not instance-scoped.** Event ensures validate input arguments as a pre-condition before any instance state is examined. They use an args-only evaluation context. This is a deliberate boundary: event ensures are a contract on what the caller provides, not on what the instance currently looks like. Making them instance-aware would couple input validation to entity state, breaking the conceptual separation between "did the caller provide valid inputs" and "is the entity in a valid state for this transition." *(Philosophy: "One file, complete rules" — the rules for each concern are where they belong.)*

8. **Same evaluator, same guarantees, every invocation.** All three mutating/simulating operations (`Inspect`, `Fire`, `Update`) route through the same `PreceptExpressionRuntimeEvaluator`. The determinism, totality, and isolation guarantees of the evaluator hold equally for guard evaluation, mutation expressions, derived field recomputation, and constraint evaluation — because they all go through the same component. There are no special-cased evaluation paths. *(Philosophy: "The engine is deterministic — same definition, same data, same outcome.")*

---

## Documented Assumptions

The following behavioral facts are implemented in the engine but not explicitly commented in the source. They are stated as design facts in this document and flagged for Shane's review.

| # | Assumption | Location |
|---|---|---|
| A1 | Violation ordering is a contract: rules first, then ensures in anchor order (`from` → `to` → `in`), each in declaration order within the model. | `CollectConstraintViolations` |
| A2 | Instance invalidation after engine recompile is intentional. `CheckCompatibility` is designed to detect incompatible instances, not to grandfather them. When a definition changes, all instances should be re-validated. | `CheckCompatibility` |
| A3 | Sequential mutations within a row ("read-your-writes") are by design. Later assignments can reference values written by earlier assignments in the same row. | `ExecuteRowMutations`, `BuildEvaluationDataWithCollections` |
| A4 | The hydrate/dehydrate dual-format design exists because collections require typed metadata (`CollectionValue`) during evaluation but must serialize as plain `List<object>` in public results. The format boundary is enforced architecturally. | `HydrateInstanceData`, `DehydrateData` |
| A5 | Guarded edit block fail-closed behavior (guard exception → field not editable) is a deliberate security decision, not error suppression. | `EvaluateGuardedEditFields` |

---

## Future Considerations

The following items emerged from external research during the architecture documentation phase and are tracked here for consideration in future design work.

**1. Fire outcome taxonomy — `Rejected` covers three causally distinct cases (issue #129).** Currently, `Fire` returns `Rejected` for three situations that require different caller responses: (a) an authored `reject` row matched — a designed prohibition; (b) event argument validation failed (pre-flight stage) — a bad caller input; and (c) an event ensure was violated — an input pre-condition failure. `Update` correctly splits these by having `UneditableField` for structural prohibition and `ConstraintFailure` for constraint violations. The asymmetry means callers of `Fire` cannot distinguish a designed prohibition from an argument error from a pre-condition failure. Possible future resolution: introduce `ArgumentValidationFailure` and `EnsureViolation` as distinct `Fire` outcomes, matching `Update`'s precision. Tracked as [issue #129](https://github.com/sfalik/Precept/issues/129). See `research/architecture/outcome-taxonomy-result-types.md`.

**2. `NumericInterval` `double` bounds precision.** `NumericInterval` stores its lower and upper bounds as `double` (IEEE 754 binary64). All current endpoint values originate from parsed source literals — a context where the `decimal`→`double` cast is exact or a known slight widening, which is sound for all current proof use cases. It is not formally proved for *accumulated* or *computed* bounds (e.g., interval arithmetic chains that derive bounds from multiple source values). Before substantially expanding the proof surface — adding new interval operations, cross-event interval carryover, or derived-field interval chaining — the formal precision boundary should be documented. See the `## Numeric Precision and IEEE 754` section of `ProofEngineDesign.md` and `research/architecture/proof-engine-abstract-interpretation.md`.

**3. Incremental compilation.** The current engine rebuilds the full `TypeCheckResult` and `PreceptEngine` on every compile invocation. Two cheap wins are available in the language server layer without changing the core: `TextDocumentSyncKind.Incremental` (narrows parse work to changed ranges) and a debounce on the compile trigger (avoids redundant full checks mid-edit). When `.precept` files routinely exceed ~1,500 lines, a K2/FIR-style phase formalization — separating topological sort, interval injection, and type check passes explicitly — would support partial re-check without full recompilation. See `research/architecture/incremental-compilation-patterns.md`.

**4. Documented Assumptions promotion.** The assumptions in A1–A5 above are flagged for Shane's review. Once confirmed, the `*(assumption)*` markers should be retired — these become ordinary design facts, stated inline in the relevant section prose. The `## Documented Assumptions` section is scaffolding for review, not a permanent home for behavioral contracts.
