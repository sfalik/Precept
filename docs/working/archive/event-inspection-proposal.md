# EventInspection Design Proposal

**Authors:** Frank (Architect) + George (Implementation Feasibility)  
**Date:** 2026-05-07  
**Blocks:** CC#8 (EventInspection Shape)  
**Unblocks:** CC#12, CC#23, CC#24, MCP N+1 issue, LS preview contract

---

## 1. Problem Statement

CC#8 is blocked on the canonical `EventInspection` shape. Three options (A/B/C) were proposed in the CC#8 entry, but they addressed the wrong question — they were shape variants when the real issue is a **contract question**: what does `InspectFire` owe to its consumers?

The root problem has three components:

**1a. Two incompatible shapes exist in the docs.**  
`result-types.md` and `evaluator.md` (the evaluator-authoritative docs) describe:
```csharp
public sealed record EventInspection(
    Prospect OverallProspect,
    ImmutableArray<TransitionInspection> Transitions,
    ImmutableArray<ConstraintResult> EventEnsures,
    ImmutableArray<FieldSnapshot> FieldSnapshots);   // ← semantics undefined + bug
```

`language-server.md §7.6` describes:
```csharp
public sealed record EventInspection(
    string EventName,
    Prospect Outcome,
    string? Explanation,
    ImmutableArray<TransitionRowInspection> Rows,
    ImmutableArray<FieldSnapshot> BeforeFields,
    ImmutableArray<FieldSnapshot> AfterFields);
```

These are not reconcilable variations — they reflect different views of what a consumer needs.

**1b. A latent implementation bug exists.**  
The `InspectFire` code in `evaluator.md §7.2` overwrites `fieldSnapshots` on every row iteration. The final `EventInspection(... fieldSnapshots)` argument receives only the *last* row's projected post-mutation state — not the pre-mutation current state. Fixing this bug requires deciding what "current field snapshots" semantically means.

**1c. Partial-arg evaluation has no defined contract.**  
The current code uses `args ?? FiredArgs.Empty`, which treats all arg-dependent guard expressions as evaluating against absent values. The Kleene three-value logic documented in `result-types.md` is not wired into the guard evaluation path — guards return `bool`, not `Prospect`. The inspection surface for the "no args supplied" scenario is unspecified.

---

## 2. Shane's Design Direction

> "My preference is to have very thin LS and MCP layers. The evaluator/runtime should produce a useable result, and this will also be used by future runtime consumers to build full user-facing UX, likely fired on user input changes (debounced) to give the user feedback on their input — are the args supplied valid, and if they are, what would be the outcome. If args are not supplied, or are invalid, then the runtime should supply best answer disambiguated by the prospect field."

This reframes CC#8 from a naming decision into a **runtime contract** decision. The key constraints:

- The runtime result is the primary artifact — not an intermediate representation for LS/MCP to enrich
- Thin wrappers: LS and MCP serialize; they do not compute
- The result is built for interactive UX: debounced on input change, renders arg validity + outcome
- `Prospect` is the consumer's semantic discriminator — it tells the consumer how to render each row/event
- All four arg-lifecycle scenarios must produce usable, self-contained output from `InspectFire` alone

---

## 3. Consumer Use Cases

These four scenarios define what `InspectFire` must produce for a consumer to render without any additional computation.

### Scenario 1: Args fully supplied, no violations

The user has provided all required args. The consumer should show: will it transition? to what state? which fields change?

**What the consumer needs:**
- `OverallProspect = Certain` → render as "will fire"
- Exactly one `TransitionInspection` with `Prospect.Certain` → identify the winning row
- `TransitionInspection.PostFields` → show every field value after the event, including arg-driven assignments
- `TransitionInspection.Effect` is `RowEffect.TransitionTo(TargetState)` → show the state the entity will enter
- `TransitionInspection.GuardSummary = null` when the winning guard passed without ambiguity
- `TransitionInspection.Constraints` all `Satisfied` → no warnings needed

### Scenario 2: Args partially supplied

The user is mid-input. Some required args are present; others are not yet filled in.

**What the consumer needs:**
- `OverallProspect = Possible` → render as "might fire"
- `TransitionInspection.Prospect = Possible` for rows whose guard references missing args
- `TransitionInspection.GuardSummary` populated for rows whose guard evaluation is ambiguous → render the runtime-authored guard explanation directly
- `TransitionInspection.PostFields` with `IsResolved = false` on fields that depend on missing args → render those fields as "pending" or "?"
- `TransitionInspection.Constraints` with `Status = Unresolvable` on constraints touching unresolved fields → do not show as violations

### Scenario 3: Args supplied but invalid

The user has filled in all args but with wrong types, unknown keys, or structurally bad values.

**What the consumer needs:**
- `OverallProspect = Impossible` → render event as "cannot fire"
- A structured collection of arg errors → show per-arg validation feedback (e.g., "OrderQuantity must be a positive integer")
- Guard/constraint evaluation is not meaningful here — arg errors precede evaluation

### Scenario 4: No args supplied for an event that requires them

The user has not yet interacted with the arg inputs. The consumer should show the "best guess" outcome: what would happen if appropriate args were supplied.

**What the consumer needs:**
- `OverallProspect = Possible` → render as "might fire; awaiting args"
- `TransitionInspection.PostFields` with `IsResolved = false` on arg-dependent fields → show the stable (non-arg-driven) field values and indicate which are pending
- `DeclaredArgs` on `EventInspection` → render the arg input form with correct types and labels
- Guards that reference no args evaluate normally → filter to rows that are viable from current field state alone

---

## 4. `Prospect` Semantics Analysis

### Current three-value design holds

`result-types.md §Deliberate Exclusions (E6)` explicitly considered and rejected a fourth `ArgDependent` variant. The rationale was: the UI already knows which args are missing via `RequiredArgs`, so a fourth value is redundant with information already on the `Version` surface.

Shane's direction is consistent with this. He names `Prospect` as the discriminator and describes it as a three-way signal: valid args → definitive outcome, absent/invalid args → best-guess outcome. Three values cover this:

| Prospect | Consumer renders |
|----------|-----------------|
| `Certain` | "This will fire. Here's what happens." |
| `Possible` | "This might fire. Here's the expected outcome if your args work out." |
| `Impossible` | "This cannot fire. Here's why." |

### What `Prospect` must NOT carry

`Prospect` should not carry payload explaining *why* a row is `Impossible`. That information lives in `TransitionInspection.Constraints` (constraint violations), `EventInspection.ArgErrors` (invalid args), or `TransitionInspection.GuardSummary` (guard failures / ambiguity).

A consumer distinguishes "impossible because constraints fail" from "impossible because args are missing" from "impossible because no matching row" through the combination of `ArgErrors`, `TransitionInspection.Constraints`, and whether any `TransitionInspection` entries exist — not through `Prospect` variants.

### The `Possible` propagation edge case

The current Kleene propagation rules in `result-types.md` say: "A `Possible` row leaves subsequent rows at most `Possible`." This is correct for first-match routing. Under Shane's "best guess" scenario, when no args are provided, multiple rows may evaluate as `Possible` (all arg-dependent guards return `Unknown → Possible`). `OverallProspect = Possible` is the right aggregate — the consumer gets the landscape and picks the first non-`Impossible` row's `PostFields` as the best-guess outcome. The runtime does not need to pick a canonical winner.

**Frank's position:** Three `Prospect` values are sufficient. No fourth variant needed. The contract is complete if `EventInspection` carries the additional structured fields proposed below.

---

## 5. Proposed `EventInspection` Shape

### The proposed types

```csharp
public sealed record EventInspection(
    string EventName,                                   // event name — essential for self-describing collection embedding
    Prospect OverallProspect,                          // Certain | Possible | Impossible
    ImmutableArray<ArgDescriptor> DeclaredArgs,        // arg contract for this event — drives UX input form
    ImmutableArray<ArgError> ArgErrors,                // non-empty when provided args are structurally invalid
    ImmutableArray<FieldSnapshot> CurrentFields,       // pre-mutation field state — captured once, before row loop
    ImmutableArray<TransitionInspection> Transitions,  // per-row inspection detail; empty = UndefinedEvent
    ImmutableArray<ConstraintResult> EventEnsures);    // on<event> constraint results

public sealed record TransitionInspection(
    Prospect Prospect,
    RowEffect Effect,                                  // TransitionTo | NoTransition | Rejection
    string? GuardSummary,                              // Contract rule: all rule failure surfaces carry a human-readable description. GuardSummary fulfills this contract for guard failures.
    ImmutableArray<ConstraintResult> Constraints,
    ImmutableArray<FieldSnapshot> PostFields);         // projected post-state for this specific row

public abstract record RowEffect
{
    public sealed record TransitionTo(string TargetState) : RowEffect;
    public sealed record NoTransition() : RowEffect;
    public sealed record Rejection(string Reason) : RowEffect;
}

public sealed record ArgError(
    string ArgName,
    string Reason);                                    // e.g. "expected integer, got string"
```

### Field-by-field rationale

**`EventName`**  
Absent from the evaluator shape, present in the LS shape — and the LS shape is right about this one. When `EventInspection` items appear inside `UpdateInspection.Events`, each entry is opaque without a name. The consumer cannot correlate results to events without a string lookup on its side. Adding `EventName` makes every `EventInspection` self-describing regardless of how it reaches the consumer. Cost: one string field.

**`DeclaredArgs`**  
`ArgDescriptor` is already on the `Version` surface via `RequiredArgs(eventName)`. Embedding it in `EventInspection` removes the need for a second lookup when the consumer is rendering "what do I need to fill in to fire this event?" — exactly Shane's scenario 4. The consumer should not have to correlate an inspection result back to a catalog query. `ArgDescriptor` already carries name, type, and required/optional status — no new type needed.

**`ArgErrors`**  
Required for Scenario 3. The current design has no mechanism for `InspectFire` to report invalid-arg feedback — `Fire` uses `EventOutcome.InvalidArgs(reason)`, but inspection has no equivalent. A flat collection of `ArgError(ArgName, Reason)` is sufficient. When `ArgErrors.Length > 0`, `OverallProspect` is forced to `Impossible` — there is nothing useful to evaluate.

**`CurrentFields` (renamed from `FieldSnapshots`)**  
Fixes the latent bug and clarifies semantics. This is the pre-mutation snapshot of all fields at the time `InspectFire` is called. It is captured *once* before the row evaluation loop, not inside it. The old name `FieldSnapshots` was ambiguous (snapshot of what? before? after? which row?). `CurrentFields` is unambiguous. Consumers use this to show the entity's current state alongside projected post-states.

**`Transitions`**  
Unchanged from the evaluator shape. Per-row detail. Empty when `OverallProspect = Impossible` and no dispatch entries exist (i.e., `UndefinedEvent` path).

**`EventEnsures`**  
Unchanged. Event-scoped constraint results for `on<event>` constraints.

**`RowEffect` (replaces `TransitionKind` enum + nullable strings)**  
The original proposal used a `TransitionKind` enum with `string? TargetState` and `string? RejectReason` on `TransitionInspection`. Shane closed OQ-3 as the DU. `RowEffect` is adopted: `TransitionTo(TargetState)` encodes the target state directly, `NoTransition()` signals a non-transitioning row, and `Rejection(Reason)` carries the authored rejection message from `ExecutionRow.RejectReason` (CC#11). Consumers pattern-match on `Effect`. The nullable-string encoding is eliminated; the DU is self-describing and matches the source prototype shape.

**`GuardSummary`**  
A human-readable summary of the guard condition that was evaluated. Provided by the runtime when a guard fails or evaluates ambiguously. The UI renders this string directly — no DSL parsing required. Null when no guard applies or when the guard passed without ambiguity.

### What was removed from the LS shape (and why)

**`Explanation` (LS shape)** — Prose generation is a display concern, not a runtime contract. The LS webview or MCP tool synthesizes prose from the structured result. Adding prose to the runtime result conflates the data contract with the display contract and creates a prose-generation dependency in a pure-computation layer.

**`BeforeFields` / `AfterFields` (LS shape)** — Superseded by `CurrentFields` (pre-mutation, top-level) + `TransitionInspection.PostFields` (per-row projected post-state). The LS shape collapsed per-row post-state into a single `AfterFields` — this loses the distinction between rows and doesn't compose with the `Possible` multi-row scenario. The evaluator shape is correct and more expressive.

---

## 6. Partial-Arg Evaluation Contract

### The core fix: Kleene guard evaluation

The current `InspectFire` code in `evaluator.md §7.2` calls `EvaluateGuard(...)` which returns `bool`. Under the partial-arg contract, guard evaluation must return `Prospect` (not `bool`). The Kleene three-value truth table is already documented in `result-types.md §Inspection Types` — it is not new — but it is not wired into the guard evaluation path.

The fix: add a separate inspection-mode guard evaluation function:

```csharp
// Commit path — unchanged
internal static bool EvaluateGuard(ExecutionPlan plan, PreceptValue[] slots, FiredArgs args);

// Inspect path — new; returns Certain/Possible/Impossible via Kleene logic
internal static Prospect EvaluateGuardProspect(ExecutionPlan plan, PreceptValue[] slots, FiredArgs? args);
```

`EvaluateGuardProspect` treats any `LOAD_ARG` opcode for a missing arg as producing `Unknown` at the Kleene level. The stack machine propagates `Unknown` through boolean operators per the documented truth table. The final result maps to `Prospect`.

This is the prerequisite for correct partial-arg behavior. Without it, `InspectFire` with `args = null` silently evaluates all guards with missing args as `false` (absent values in comparisons) or causes runtime failures — neither is the intended semantics.

### Behavior by arg state

| Args state | `EvaluateGuardProspect` behavior | `PostFields.IsResolved` | `OverallProspect` |
|-----------|----------------------------------|-------------------------|-------------------|
| `null` (none provided) | Missing args → `Unknown`; Kleene propagation | `false` for arg-dependent fields | `Possible` if any row could fire; `Impossible` if all guards fail on non-arg terms |
| Partial `JsonElement` | Present args resolve normally; absent args → `Unknown` | `false` for fields depending on missing args | Mix of `Certain`, `Possible`, `Impossible` per row |
| Complete | Standard binary evaluation; no `Unknown` | `true` for all reachable fields | `Certain` or `Impossible` per row |
| Invalid (type/structural errors) | Not reached — arg parsing fails first | N/A | `Impossible`; `ArgErrors` populated |

### Option assessment (George)

Three options were considered for the partial-arg path:

**Option 1 — Null args: evaluate all rows ignoring arg-dependent guards.** Simple but semantically wrong. A guard of `Amount > threshold` where `threshold` is an arg would be evaluated as "guard passes" (vacuously), which misleads the consumer into showing `Certain` where `Possible` is correct.

**Option 2 — Return a specialized `InsufficientArgs` meta-variant from `InspectFire`.** Adds complexity at the API boundary without adding useful information. The consumer already gets `DeclaredArgs` to know what to ask for; a meta-variant that says "args missing" is redundant.

**Option 3 — Kleene ternary evaluation on the existing opcode engine.** Semantically correct. Requires that `LOAD_ARG` for a missing arg produces `Unknown` at the Kleene level. Implementation: add `Prospect EvaluateGuardProspect(...)` alongside the existing `bool EvaluateGuard(...)`. Same opcode walk, different result type propagation.

**Recommendation: Option 3.** It is the only approach consistent with the three-value `Prospect` design and the Kleene documentation in `result-types.md`. The precondition (arg-dependency sets per expression node, D8/R4) is referenced in `result-types.md §Open Questions` as a lowering-time tree walk — it is a known prerequisite, not a new discovery.

**Bootstrap path before D8/R4 ships:** Without per-node arg-dependency sets, the evaluator can implement a conservative approximation: any expression referencing any arg (detected by attempting evaluation and catching the absent-arg condition) → mark that guard expression as `Unknown → Possible`. This is conservative (may over-report `Possible`) but never wrong. When D8/R4 ships, the precise Kleene evaluation replaces the approximation automatically.

---

## 7. Thin Wrapper Contract

### What "thin" means precisely

A wrapper is thin when its entire body is: deserialize inputs → call one runtime method → serialize output. A wrapper that computes, filters, zips, diffs, or generates prose is not thin.

**Current violation in LS `§7.6`:** `MapToResult(inspection)` adds `EventName`, `BeforeFields`, `AfterFields`, and `Explanation` that the runtime does not provide. This is not thin — it's reconstruction work.

**Current violation in MCP `precept_inspect`:** The tool fans out into N+1 `InspectFire` calls (one per available event) to produce the event landscape. This is not thin — it's orchestration work.

### The thin LS wrapper

```csharp
// LS handler — after the proposed changes
InspectResult? HandleInspect(InspectParams p)
{
    var state = _documents[p.Uri];
    if (state.Precept is not { } precept) return null;
    
    var version = BuildVersion(precept, p.CurrentState, p.Data);
    
    if (p.Event is not null)
    {
        // Single event inspection — args optional
        var args = BuildArgs(p.EventArgs);  // JSON lane; null if not provided
        return Serialize(version.InspectFire(p.Event, args));
    }
    
    // Full landscape — one call
    return Serialize(version.InspectUpdate(null));  // Events field contains all EventInspection
}
```

No prose, no field diff, no event-name patching. `EventName` is on every `EventInspection` because the runtime puts it there.

### The thin MCP wrapper

The N+1 problem in `mcp.md §precept_inspect` is resolved by `InspectUpdate(null)`: when called with no patch, `UpdateInspection.Events` already contains a full `EventInspection` for every event available in the current state — each one self-describing via `EventName`. One runtime call; one result.

```csharp
// MCP precept_inspect — after the proposed changes
public object HandleInspect(InspectArgs args)
{
    var version = CompileAndRestore(args.Text, args.CurrentState, args.Data);
    
    if (args.EventArgs is not null)
    {
        // Caller is inspecting a specific event with args — single fire inspection
        return Serialize(version.InspectFire(args.Event!, args.EventArgs));
    }
    
    // Full landscape
    return Serialize(version.InspectUpdate(null));
}
```

Still one core call. The wrapper does not decide which events to inspect.

### Irreducible differences

The only irreducible differences between the runtime result and what LS/MCP serializes:

| Layer | Irreducible addition | Rationale |
|-------|---------------------|-----------|
| LS | `Uri`, request routing | LSP protocol requirement, not semantic |
| MCP | Compilation step | MCP is source-first; LS keeps a compiled `Precept` in memory |
| MCP | `Restore` step | MCP carries entity state in the call args |

Neither layer should add semantic fields to the result. The runtime result is complete.

---

## 8. Implementation Notes (George)

### Evaluator changes required

**1. Capture `CurrentFields` before the row loop** (fixes the `FieldSnapshots` bug)  
One line change. `version.Slots` is available before the loop; `BuildFieldSnapshots(version.Slots, precept.Fields, version.CurrentState)` runs once and is stored. Cost: one array allocation per `InspectFire` call. Already effectively free at DSL scale.

**2. Add `Prospect EvaluateGuardProspect(plan, slots, args?)`**  
New method alongside the existing `bool EvaluateGuard(...)`. Same opcode walk, different result type. Requires that the opcode stack can carry a Kleene-Unknown signal — the simplest approach is to add a sentinel `PreceptValue` representing `Unknown` at the Kleene level, or use an out-of-band `bool guardIsPartial` flag alongside the binary result for the bootstrap approximation. Full Kleene-accurate implementation requires D8/R4 arg-dependency annotations; the conservative bootstrap is safe and reversible.

**3. Construct `RowEffect` for `TransitionInspection`**  
`ExecutionRow.Outcome` (the `TransitionOutcome` enum: Transition/NoTransition/Reject) is already in scope at the row evaluation site. Map to `RowEffect`: `Transition` → `new RowEffect.TransitionTo(targetStateName)`, `NoTransition` → `new RowEffect.NoTransition()`, `Reject` → `new RowEffect.Rejection(row.RejectReason ?? "")`. `RejectReason` is already on `ExecutionRow` (CC#11 resolved).

**4. Populate `EventEnsures`**  
Currently passes an empty array (noted in `evaluator.md §7.2`). Evaluating `on<event>` constraints requires the `ConstraintPlanIndex` to carry event-keyed buckets. This is a planned gap, not introduced by CC#8.

**5. Add `ArgError` collection to `InspectFire` entry point**  
At the `Version.InspectFire` API boundary (before the evaluator is called), arg validation runs. If `IArgBuilder.Build()` produces validation errors, the evaluator is not invoked. Return `EventInspection(EventName, Impossible, DeclaredArgs, argErrors, [], [], [])` directly from the API boundary. Cost: zero evaluator change; validation already exists for the commit path.

**6. Populate `DeclaredArgs` on `EventInspection`**  
`EventDescriptor` already carries `ArgDescriptors`. Reading it into the result is one array copy.

### Per-operation allocation delta

The additional fields per `InspectFire` call (worst-case, 15 events, 40 fields):

| New field | Allocation |
|-----------|-----------|
| `EventName` (string) | Reference to existing interned string — zero |
| `DeclaredArgs` | Reference to existing `ImmutableArray` on `EventDescriptor` — zero |
| `ArgErrors` | Empty `ImmutableArray<ArgError>.Empty` on the happy path — zero |
| `CurrentFields` | One `FieldSnapshot[]` → `ImmutableArray<FieldSnapshot>` — same size as the current (broken) `FieldSnapshots` |

The proposed changes do not increase the per-call allocation budget. The existing `PostFields` per `TransitionInspection` row is unchanged. `RowEffect` adds one small sealed record allocation per row — negligible at DSL scale.

### Performance

At DSL scale (10–50 fields, 5–15 events per state), `InspectFire` for the full landscape via `InspectUpdate(null)` is still sub-millisecond. The guard evaluation path change (`bool` → `Prospect`) adds no branching complexity; the Kleene truth table is the same switch with an extra `Unknown` case.

---

## 9. Open Questions

**OQ-1: `DeclaredArgs` or `RequiredArgs`?**  
`ArgDescriptor` carries whether an arg is required or optional. The field name `DeclaredArgs` was chosen over `RequiredArgs` to include optional args (a consumer building an input form needs to know all declared args, not just required ones). If the team prefers `RequiredArgs` as a name for consistency with `Version.RequiredArgs(eventName)`, the content is the same.

**OQ-2: `ArgError` granularity — CLOSED**  
~~The proposed `ArgError(ArgName, Reason)` is a simple string reason. Should it carry a structured error code (parallel to `DiagnosticCode` in the compiler)? A structured code would allow LS/MCP to localize or stylize the error message. Recommendation: start with a string reason and add a code field when there's a concrete need.~~

> **Resolution (2026-05-06):** Closed. String `Reason` only — matches the field edit error pattern (`ConstraintViolation.Because`, `InvalidFields.Reason`). No `ArgErrorKind`.

**OQ-3: `TransitionKind` vs sealed record DU — CLOSED**  
~~`TransitionKind` is proposed as an enum because the fields `TargetState` and `RejectReason` are already on `TransitionInspection` as nullable strings. An alternative is a sealed DU `RowEffect`:~~
```csharp
public abstract record RowEffect
{
    public sealed record Transition(string TargetState) : RowEffect;
    public sealed record Apply() : RowEffect;
    public sealed record Reject(string Reason) : RowEffect;
}
```
~~The DU is more type-safe (it eliminates the nullable-string ambiguity for `TargetState` and `RejectReason`), but requires pattern matching at every consumer. The enum approach keeps pattern matching optional. Frank's slight preference: DU, because the nullable-string encoding is a footgun. George's preference: enum, because the consumer surface is simpler. **Pending Shane's call.**~~

> **Resolution (2026-05-06):** Closed. DU adopted — `RowEffect { TransitionTo(TargetState), NoTransition, Rejection(Reason) }`. Matches source prototype. Nullable string fields (`string? TargetState`, `string? RejectReason`) removed from `TransitionInspection`; consumers pattern-match on `Effect`.

**OQ-4: `EventEnsures` timing**  
`on<event>` constraint evaluation requires evaluating against the post-mutation working copy. Which row's working copy? For a `Certain` outcome, it is the winning row's. For a `Possible` outcome with multiple potentially-matching rows, there is no canonical working copy. Frank's view: `EventEnsures` should move inside `TransitionInspection` (evaluated per-row against that row's post-mutation state). George's view: this changes the public API shape in a way that may surprise consumers who expect `EventEnsures` to be event-level. **Pending Shane's call on scope level.**

**OQ-5: Interaction with `UpdateInspection.Events` evaluation**  
When `InspectUpdate(null)` calls `InspectFire` per event (for `UpdateInspection.Events`), each call now populates `CurrentFields` — which is the same snapshot for every event. This is mildly redundant. It could be omitted from the embedded copies (set to empty) since the caller has `UpdateInspection.Fields` for the same information. However, keeping it consistent (always populated) makes each `EventInspection` self-describing regardless of how it is accessed. Recommendation: keep it populated; redundancy is preferable to context-dependent omission.

---

## 10. What Gets Unblocked

### CC#8 (EventInspection Shape) — the decision itself

The proposed shape above is the recommendation for Shane's approval. Once approved, update `result-types.md` and `evaluator.md` to the canonical shape, and supersede `language-server.md §7.6`'s conflicting definition.

### CC#12 (Faulted as EventOutcome variant)

CC#12 is orthogonal to the inspection shape — it is about the commit path (`Fire`). It was blocked on CC#8 only because both were in the "evaluator output cluster." With CC#8 resolved, CC#12 can proceed: add `Faulted(Fault)` to `EventOutcome`.

### CC#23 (EventOutcome.mutations payload)

The proposed `CurrentFields` (pre-mutation) + `TransitionInspection.PostFields` (per-row post-mutation) gives the inspection path a natural before/after diff surface. For the commit path, CC#23 asks whether `EventOutcome.Transitioned` / `EventOutcome.Applied` should carry a `mutations` diff. The inspection shape informs this: the runtime already computes the working copy state; attaching a diff to commit outcomes requires the same per-field comparison. The decision of whether to put it on the outcome DU or leave it to consumers is still Shane's call, but the inspection shape does not block it.

### CC#24 (Unmatched guard trace)

`TransitionInspection` per row is exactly the guard trace that `Unmatched` in the commit path lacks. After CC#8 is resolved, the inspection path gives consumers a complete row-by-row guard evaluation picture. For the commit `Unmatched` variant, the question becomes: should `Unmatched()` carry the same row-level detail? Frank's view: yes — `Unmatched` should carry `ImmutableArray<TransitionInspection> EvaluatedRows` (the rows that were considered and why each was rejected). This collapses CC#24 into the same `TransitionInspection` type defined by CC#8, making the types consistent between inspect and commit paths.

### MCP N+1 issue (mcp.md open question)

Resolved by the thin-wrapper contract: `precept_inspect` with no `event` argument calls `version.InspectUpdate(null)`, which returns `UpdateInspection.Events` — a complete `EventInspection` for every event available in the current state. One call. Each `EventInspection` is self-describing via `EventName`. The N+1 fan-out is eliminated from the MCP layer.

### LS preview contract (language-server.md §7.6)

The LS can serialize `EventInspection` directly from the runtime result with no added fields. `EventName` on the runtime type eliminates the need for the LS to patch names. The conflicting shape in §7.6 is superseded.

---

## Appendix: Current Shape vs. Proposed Shape

| Field | Current (`result-types.md`) | Proposed | Δ |
|-------|-----------------------------|----------|---|
| `OverallProspect` | `Prospect OverallProspect` | `Prospect OverallProspect` | — |
| Event name | absent | `string EventName` | **+** |
| Arg contract | absent | `ImmutableArray<ArgDescriptor> DeclaredArgs` | **+** |
| Arg validity | absent | `ImmutableArray<ArgError> ArgErrors` | **+** |
| Pre-mutation fields | `ImmutableArray<FieldSnapshot> FieldSnapshots` (semantics broken) | `ImmutableArray<FieldSnapshot> CurrentFields` (pre-mutation, fixed) | **Δ name + fix** |
| Per-row detail | `ImmutableArray<TransitionInspection> Transitions` | `ImmutableArray<TransitionInspection> Transitions` | — |
| Event-level constraints | `ImmutableArray<ConstraintResult> EventEnsures` | `ImmutableArray<ConstraintResult> EventEnsures` | — |

| Field | Current `TransitionInspection` | Proposed | Δ |
|-------|---------------------------------|----------|---|
| `Prospect` | `Prospect Prospect` | `Prospect Prospect` | — |
| Target state | `string? TargetState` | ~~removed~~ (now inside `RowEffect.TransitionTo`) | **−** |
| Row effect kind | absent | `RowEffect Effect` | **+** |
| Reject reason | absent | ~~removed as standalone field~~ (now inside `RowEffect.Rejection`) | **−/+** |
| Guard summary | absent | `string? GuardSummary` (`null` when guard passed / no guard; populated when guard failed or was ambiguous) | **+** |
| Constraints | `ImmutableArray<ConstraintResult> Constraints` | `ImmutableArray<ConstraintResult> Constraints` | — |
| Post-state fields | `ImmutableArray<FieldSnapshot> PostFields` | `ImmutableArray<FieldSnapshot> PostFields` | — |
