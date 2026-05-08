# ProofEngine Spec — Pre-Implementation Gap Analysis

**Date:** 2026-05-08
**Author:** Frank
**Commit reviewed:** `79c340357aee4e54520a539dca8208bc734e3606`
**Verdict:** NOT READY

**Spec files reviewed:**
- `docs/compiler/proof-engine.md` (983 lines — primary spec)
- `docs/compiler/graph-analyzer.md`
- `docs/compiler/type-checker.md`
- `docs/compiler/diagnostic-system.md`

**Source files reviewed:**
- `src/Precept/Pipeline/ProofEngine.cs` (stub)
- `src/Precept/Pipeline/ProofLedger.cs` (stub)
- `src/Precept/Pipeline/StateGraph.cs`
- `src/Precept/Pipeline/GraphAnalyzer.cs`
- `src/Precept/Pipeline/SemanticIndex.cs`
- `src/Precept/Pipeline/Compilation.cs`
- `src/Precept/Compiler.cs`
- `src/Precept/Language/ProofRequirement.cs`
- `src/Precept/Language/ProofRequirementKind.cs`
- `src/Precept/Language/ProofRequirements.cs`
- `src/Precept/Language/DiagnosticCode.cs`
- `src/Precept/Language/Diagnostics.cs`
- `src/Precept/Language/FaultCode.cs`
- `src/Precept/Language/Faults.cs`
- `src/Precept/Language/Modifier.cs`
- `src/Precept/Language/Modifiers.cs`
- `src/Precept/Runtime/Descriptors.cs`

---

## Executive Summary

The ProofEngine spec is architecturally strong — the two-pass design, four-strategy discharge model, proof/fault chain, and catalog-driven obligation instantiation are well-conceived. However, the spec has **three blocking gaps** and **seven significant gaps** that prevent implementation from starting cleanly. The most critical issue: the spec defines five `ProofRequirementKind` values but only describes discharge strategies for two of them (Numeric and Presence). `DimensionProofRequirement`, `ModifierRequirement`, and `QualifierCompatibilityProofRequirement` are defined in the DU but have zero strategy coverage — an implementer would have to invent discharge logic from scratch. Additionally, the `FieldModifierMeta.ProofDischarges` property the spec declares as "canonical" (CC#5 resolved) does not exist in the source code, and the output type `ProofLedger` described in the spec is materially different from the stub in source.

---

## Gap Inventory

### [BLOCKING] Gaps

---

**PE-G1: Three of five ProofRequirementKind values have no discharge strategy**
- **Severity:** BLOCKING
- **Location:** `proof-engine.md` §7 "Four Proof Strategies" (lines 412–766)
- **Description:** The spec defines five `ProofRequirementKind` subtypes in §6 (lines 348–389): `Numeric`, `Presence`, `Dimension`, `Modifier`, and `QualifierCompatibility`. The four strategies (Literal, Modifier, GuardInPath, FlowNarrowing) only describe discharge predicates for `NumericProofRequirement` and `PresenceProofRequirement`. The remaining three kinds are completely absent:
  - **`DimensionProofRequirement`** — "period operand must have required time dimension." No strategy says how this is proven. Is it a static type check (always provable by the type checker)? Does it need a new strategy?
  - **`ModifierRequirement`** — "field must declare required modifier (e.g. `ordered`)." No strategy covers this. Logically Strategy 2 (Modifier Proof) should handle it, but the Strategy 2 pseudocode (lines 536–569) only reads `FieldModifierMeta.ProofDischarges`, not `ModifierRequirement.Required` directly. The mapping is unspecified.
  - **`QualifierCompatibilityProofRequirement`** — "two operands must share a qualifier value on the specified axis." This is a dual-subject requirement. None of the four strategies handle dual-subject obligations. The spec provides no guidance on how to discharge this — is it always resolvable from type-checker qualifier propagation? Does it require a fifth strategy?
- **Why it matters:** An implementer would have to guess how to handle 3 of 5 obligation kinds. Two implementers would write different code. This is the definition of a blocking ambiguity.
- **Suggested resolution:** For each of the three unhandled kinds, the spec must state:
  1. Which strategy discharges it (existing or new), OR
  2. That it is always discharged by the type checker and never reaches the proof engine as an unresolved obligation, OR
  3. That it is always `Unresolved` and produces a diagnostic (defensive backstop).

  Likely answers based on code analysis:
  - `DimensionProofRequirement`: Likely always resolvable by type-checker period-dimension inference. If so, state that it reaches the proof engine pre-discharged, or that it is a type error (not a proof obligation) and should never appear.
  - `ModifierRequirement`: Likely checked by seeing if the field has `ModifierKind.Required` in its `Modifiers` array. Add this to Strategy 2 pseudocode.
  - `QualifierCompatibilityProofRequirement`: Likely checked by the type checker's `QualifierBinding` propagation. If so, state the handoff.

---

**PE-G2: `FieldModifierMeta.ProofDischarges` does not exist in source code**
- **Severity:** BLOCKING
- **Location:** `proof-engine.md` §7 Strategy 2, lines 505–572, especially the CC#5 resolution box at line 571
- **Description:** The spec declares at line 571: "✅ Resolved (CC#5) — `FieldModifierMeta.ProofDischarges` is now canonical" and references `ProofDischarge[]` on `FieldModifierMeta`. The actual `FieldModifierMeta` record in `src/Precept/Language/Modifier.cs` (lines 105–121) has **no** `ProofDischarges` property. The `ProofDischarge` record type does not exist anywhere in the source code. `grep` for `ProofDischarge` across all of `src/Precept/Language/` returns zero matches.

  The Strategy 2 pseudocode depends entirely on `meta.ProofDischarges` for its discharge logic (line 551: `foreach (var discharge in meta.ProofDischarges)`). Without this property, Strategy 2 cannot be implemented as specified.
- **Why it matters:** Strategy 2 is the second most common discharge strategy. It covers `positive`, `nonnegative`, `nonzero`, `notempty`, `min(N)`, `max(N)`. Without the catalog property, the implementer must either:
  (a) Add the property to the catalog first (design + implementation work), or
  (b) Hardcode per-modifier logic in the proof engine (violating catalog-driven architecture).
  Both are design decisions that must be made before coding starts.
- **Suggested resolution:** Add the `ProofDischarges` property to `FieldModifierMeta` and the `ProofDischarge` record type before implementation begins. This is a catalog prerequisite, not part of the proof engine implementation itself.

---

**PE-G3: Output type `ProofLedger` in spec diverges materially from source stub**
- **Severity:** BLOCKING
- **Location:** `proof-engine.md` §5 "Output", lines 172–287
- **Description:** The spec defines `ProofLedger` with five fields:
  ```csharp
  ProofLedger(
      ImmutableArray<ProofObligation> Obligations,
      ImmutableArray<FaultSiteLink> FaultSiteLinks,
      ImmutableArray<ConstraintInfluenceEntry> ConstraintInfluence,
      ImmutableArray<InitialStateSatisfiabilityResult> InitialStateResults,
      ImmutableArray<Diagnostic> Diagnostics
  )
  ```
  The source stub at `src/Precept/Pipeline/ProofLedger.cs` defines:
  ```csharp
  ProofLedger(ImmutableArray<Diagnostic> Diagnostics)
  ```
  The following types referenced by the spec's `ProofLedger` do **not exist** anywhere in the source:
  - `ProofObligation`
  - `ProofDisposition` (enum)
  - `ProofStrategy` (enum)
  - `FaultSiteLink`
  - `ConstraintInfluenceEntry`
  - `EventArgReference`
  - `InitialStateSatisfiabilityResult`
  - `UnsatisfiedConstraint`
  - `FaultSiteAnnotation`

  The `Compilation` record in `Compilation.cs` consumes `ProofLedger` but only reads `Diagnostics` — it has no field for `FaultSiteLinks` or `ConstraintInfluence`.
- **Why it matters:** The implementer must create ~10 new record types and expand the ProofLedger shape before any meaningful work begins. The spec needs to be explicit about whether these types are created as part of the ProofEngine implementation or as a prerequisite.
- **Suggested resolution:** State that Slice 0 of the implementation plan is "shape declarations" — creating all the output types in `ProofLedger.cs` and `SemanticIndex.cs` with empty-default construction, updating the `Compilation` record, and verifying the build stays green. This matches the pattern from TypeChecker (Slice 0 shape) and GraphAnalyzer.

---

### [SIGNIFICANT] Gaps

---

**PE-G4: `SemanticIndex.AllTypedExpressions` does not exist**
- **Severity:** SIGNIFICANT
- **Location:** `proof-engine.md` §9 "Failure Modes", line 968
- **Description:** The spec's Pass 1 pseudocode (line 968) references `semantics.AllTypedExpressions`:
  ```csharp
  foreach (var expr in semantics.AllTypedExpressions)
  ```
  No such property exists on `SemanticIndex`. The `SemanticIndex` record exposes `TransitionRows`, `Rules`, `Ensures`, `StateHooks`, `EventHandlers` — but no aggregated expression enumeration surface.
- **Why it matters:** The implementer needs to know exactly which `SemanticIndex` members to walk to collect all proof-relevant expressions. Walking `TransitionRows[].Actions[].ProofRequirements` is obvious, but what about guard expressions? Constraint conditions? Computed field expressions? State hook actions? The spec doesn't enumerate the walk targets.
- **Suggested resolution:** Replace `AllTypedExpressions` with an explicit list of walk targets:
  - `TransitionRows` → `Actions[].ProofRequirements` and `Guard` expressions
  - `Rules` → `Condition` expressions
  - `Ensures` → `Condition` expressions
  - `StateHooks` → `Actions[].ProofRequirements`
  - `EventHandlers` → `Actions[].ProofRequirements`
  - Computed fields → `ComputedExpression` (if proof-relevant)

  Or add the `AllTypedExpressions` helper to `SemanticIndex` as a prerequisite.

---

**PE-G5: `ConstraintIdentity` shapes in spec differ from implementation**
- **Severity:** SIGNIFICANT
- **Location:** `proof-engine.md` §5, lines 263–267
- **Description:** The spec defines:
  ```csharp
  public sealed record RuleIdentity(string RuleName, int Index) : ConstraintIdentity;
  public sealed record EnsureIdentity(ConstraintKind Kind, string? AnchorState, string? AnchorEvent, int Index) : ConstraintIdentity;
  ```
  The actual implementation in `SemanticIndex.cs` (lines 401–404) defines:
  ```csharp
  public sealed record RuleIdentity(int RuleIndex) : ConstraintIdentity;
  public sealed record EnsureIdentity(ConstraintKind Kind, string? AnchorName, int EnsureIndex) : ConstraintIdentity;
  ```
  Differences:
  1. `RuleIdentity`: spec has `(string RuleName, int Index)`, source has `(int RuleIndex)` — no `RuleName` field.
  2. `EnsureIdentity`: spec has `(ConstraintKind, string? AnchorState, string? AnchorEvent, int Index)`, source has `(ConstraintKind, string? AnchorName, int EnsureIndex)` — spec separates state/event anchors into two nullable fields; source uses a single `AnchorName`.
- **Why it matters:** The `ConstraintInfluenceEntry` output uses `ConstraintIdentity`. If the implementer follows the spec shapes, they'll create types that conflict with existing ones. If they follow the source shapes, the spec's `EventArgReference` resolution logic may not work as described.
- **Suggested resolution:** Update the spec to match the existing source shapes. The implementation is canonical — it was created during TypeChecker implementation and has tests.

---

**PE-G6: `FindEnclosingTransitionRow` is not specified**
- **Severity:** SIGNIFICANT
- **Location:** `proof-engine.md` §7 Strategy 3, line 625; Strategy 4, line 722
- **Description:** Both Strategy 3 and Strategy 4 call `FindEnclosingTransitionRow(obligation.Site, semantics)` to find the transition row that encloses the proof obligation's expression site. The spec never defines this function. The proof engine must know: given a `TypedExpression`, how do you find which `TypedTransitionRow` contains it?
  
  This is non-trivial because:
  1. `TypedExpression` nodes don't carry parent pointers or transition-row back-references.
  2. The proof engine would need to either build an expression→row index in Pass 1, or walk `TransitionRows[].Actions` looking for expression identity matches.
  3. Obligations on expressions in `TypedRule`, `TypedEnsure`, `TypedStateHook`, or `TypedEventHandler` have no enclosing transition row — what do Strategies 3/4 return for those?
- **Why it matters:** This is critical path logic for the two guard-based strategies. The spec's pseudocode uses it as a black box, but its implementation drives the data structure design of Pass 1.
- **Suggested resolution:** Specify that Pass 1 builds an `obligation → enclosing context` index. Define the context as a discriminated union: `TransitionRowContext(TypedTransitionRow)`, `ConstraintContext(TypedRule | TypedEnsure)`, `HookContext(TypedStateHook)`, `HandlerContext(TypedEventHandler)`. Strategies 3/4 only fire for `TransitionRowContext`. All other contexts return `false`.

---

**PE-G7: `ResolveSubject` is not specified**
- **Severity:** SIGNIFICANT
- **Location:** `proof-engine.md` §7 Strategy 1, line 445; Strategy 2, line 539
- **Description:** Strategy 1 calls `ResolveSubject(numeric.Subject, obligation.Site)` and Strategy 2 calls `GetFieldName(obligation.Requirement.Subject, obligation.Site)`. Neither is defined. Given the `ProofSubject` DU:
  - `ParamSubject(ParameterMeta Parameter)` — how do you resolve a parameter to a concrete expression node from the obligation site? The `ParameterMeta` has object identity, but how does one locate the corresponding argument expression in a `TypedFunctionCall` or operand in a `TypedBinaryOp`?
  - `SelfSubject(TypeAccessor? Accessor)` — how does one resolve "self" to the receiver expression in a `TypedMemberAccess`?
  
  The spec says `ParamSubject` "must be reference-equal to one of the `ParameterMeta` instances in the containing overload's `Parameters` list" (ProofRequirement.cs, line 16), which gives identity, but the resolution logic from identity to expression is missing.
- **Why it matters:** Subject resolution is the first step in every strategy. Without it being specified, the implementer must infer the mapping from `ParameterMeta` identity to `TypedExpression` arguments — a non-trivial piece of logic.
- **Suggested resolution:** Add a `ResolveSubject` pseudocode section that handles both `ParamSubject` and `SelfSubject`:
  - `ParamSubject`: For `TypedFunctionCall`, match `Parameter` identity against `ResolvedFunction`'s overload `Parameters` list to find the positional index, then return `Arguments[index]`. For `TypedBinaryOp`, match against `ResolvedOp`'s operation metadata parameters.
  - `SelfSubject`: For `TypedMemberAccess`, return `Object`. For `TypedAction`, return the field reference expression (requires knowing the field from `FieldName`).

---

**PE-G8: Initial-state satisfiability check is underspecified**
- **Severity:** SIGNIFICANT
- **Location:** `proof-engine.md` §7 "Initial-State Satisfiability", lines 863–883
- **Description:** The spec says: "For each constraint condition, check whether default field values satisfy it." This is vague. Specifically:
  1. What does "check" mean? Evaluate the constraint expression with default values? Symbolically analyze it? The spec doesn't say.
  2. How are "default field values" determined? Fields with `default` expressions have typed defaults in `TypedField.DefaultExpression`. Fields without defaults — what is their default? `0` for numeric? `""` for string? `null` for optional? The spec doesn't define the default value model.
  3. What about fields that are set by the initial event? The initial event's `set` actions provide values at instantiation. Does satisfiability account for initial event args, or only declared defaults?
  4. The spec says to check `ensure in Draft: ...`. But the `ConstraintKind.StateResident` anchor means "while in state", not "at entry". Is entry a special case of residency? Does entry use `ConstraintKind.StateEntry` anchors instead?
  5. Computed fields (`IsComputed = true`) have `ComputedExpression` not `DefaultExpression`. Are computed field values available for satisfiability?
- **Why it matters:** This check is one of the three output surfaces of the proof engine (alongside obligation discharge and constraint influence). Without clear semantics, the implementer must make design decisions that should be in the spec.
- **Suggested resolution:** Define the satisfiability algorithm explicitly:
  - State which fields are relevant (all fields? only fields referenced by initial-scope constraints?)
  - Define the "default value" for each type kind when no `default` is declared
  - State whether initial event arguments are considered (probably not — they're runtime values)
  - Define which constraint scopes are checked (`in`, `to`, both?)

---

**PE-G9: No diagnostic code for collection-empty proof failures**
- **Severity:** SIGNIFICANT
- **Location:** `proof-engine.md` §7 "Collection Non-Empty Proof", lines 885–899; `DiagnosticCode.cs`
- **Description:** The spec describes collection non-empty obligations (first, last, peek, dequeue, pop) but the only proof-stage diagnostic codes are:
  - 82: `UnsatisfiableGuard` (Warning)
  - 83: `DivisionByZero` (Error)
  - 84: `SqrtOfNegative` (Error)
  
  There is no proof-stage diagnostic for "collection may be empty when `first()` is called." The type-checker stage has `UnguardedCollectionAccess` (63) and `UnguardedCollectionMutation` (64), but these are `DiagnosticStage.Type` — they fire during type checking, not proof. If the proof engine is supposed to handle collection non-empty proof discharge, it needs its own diagnostic code for the "unresolved" case. Or alternatively, collection safety is fully handled by the type checker and the proof engine should NOT create obligations for them.
  
  The `FaultCode` enum has `CollectionEmptyOnAccess = 9` with `[StaticallyPreventable(DiagnosticCode.UnguardedCollectionAccess)]` — linking to the type-checker code, not a proof code.
- **Why it matters:** The spec says the proof engine handles collection non-empty obligations, but there's no diagnostic to emit if the obligation is unresolved. Either the spec is wrong (collection safety is the type checker's job entirely) or diagnostic codes are missing.
- **Suggested resolution:** Clarify which pipeline stage owns collection non-empty safety:
  - If the type checker already emits `UnguardedCollectionAccess`/`UnguardedCollectionMutation` for all cases, the proof engine should NOT create duplicate obligations. Remove collection non-empty from the proof engine spec.
  - If the proof engine handles the richer case (modifier proof + guard proof), add a proof-stage diagnostic code for unresolved collection obligations.

---

**PE-G10: `ExtractGuardConstraints` is not specified**
- **Severity:** SIGNIFICANT
- **Location:** `proof-engine.md` §7 Strategy 3, line 631
- **Description:** Strategy 3 calls `ExtractGuardConstraints(row.Guard)` to decompose a `TypedExpression` guard into simple constraint forms. The spec lists supported patterns (line 599–608) but doesn't specify:
  1. What happens with compound guards? `when A > 0 and B > 0` — are both constraints extracted? What about `or`?
  2. What happens with negation? `when not (A == 0)` — is this recognized as `A != 0`?
  3. What about nested function calls in guards? `when count(Items) > 0 and len(Name) > 3` — is `len(Name) > 3` a valid constraint form?
  4. Does the proof engine look inside `TypedConditional` (if/then/else) for guard constraints?
- **Why it matters:** The guard pattern language directly determines Strategy 3's power. Without clarity on compound/negated guards, the implementer must choose a scope that may be too narrow or too broad.
- **Suggested resolution:** Specify that `ExtractGuardConstraints`:
  - Decomposes `and` conjunctions recursively — each leaf becomes a separate constraint
  - Does NOT decompose `or` disjunctions — the proof engine cannot use a disjunct because either branch might be false
  - Handles simple negation by inverting the comparison operator
  - Ignores complex expressions (nested conditionals, quantifiers) — they are not constraint forms

---

### [ADVISORY] Gaps

---

**PE-G11: Spec references `Compilation` but doesn't address the Precept Builder gap**
- **Severity:** ADVISORY
- **Location:** `proof-engine.md` §8 "Downstream Consumers", lines 930–937
- **Description:** The spec references "Precept Builder" as a consumer of `FaultSiteLinks` and `ConstraintInfluence`, and references `precept-builder.md §Pass 4` (line 218, 236, 250). No `precept-builder.md` file exists in `docs/compiler/`. The consumer contract for `ProofLedger` is described in the proof engine spec but has no counterpart in any builder spec.
- **Why it matters:** The proof engine's output shape is driven by what the builder consumes. Without a builder spec, the output shape is hypothetical — it could change when the builder is designed. Implementation risk is moderate: the proof engine can be built to the spec, but the builder may require changes.
- **Suggested resolution:** Accept this gap for now — the builder is a future stage. Add a note in the proof engine spec: "Builder contract is forward-looking; output shape may evolve when `precept-builder.md` is authored."

---

**PE-G12: No specification of diagnostic message formatting for proof obligations**
- **Severity:** ADVISORY
- **Location:** `proof-engine.md` §9 "Failure Modes", line 981
- **Description:** The pseudocode calls `CreateDiagnostic(obligation)` but doesn't specify how the diagnostic message template parameters `{0}`, `{1}` are populated. The existing diagnostic entries in `Diagnostics.cs` have:
  - `DivisionByZero`: `"Division by zero: '{0}' can be zero when {1}"` — what is `{0}` (field name? expression text?) and `{1}` (state name? guard absence?)?
  - `SqrtOfNegative`: `"sqrt() requires a non-negative value, but '{0}' can be negative when {1}"` — same question.
  - `UnsatisfiableGuard`: `"The condition '{0}' on event '{1}' can never be true when {2}"` — three params.
- **Why it matters:** Without knowing what fills the template parameters, test authors can't assert diagnostic messages. This is a testability gap.
- **Suggested resolution:** Add a message-formatting table: for each diagnostic code, specify what each `{N}` parameter is (field name, expression text, state name, constraint description).

---

**PE-G13: Error propagation from upstream stages is unspecified**
- **Severity:** ADVISORY
- **Location:** `proof-engine.md` §3 "Responsibilities and Boundaries"
- **Description:** The spec doesn't say whether the proof engine should short-circuit if the `SemanticIndex` or `StateGraph` already contain errors. Looking at the existing pipeline in `Compiler.cs`, every stage runs unconditionally — the proof engine receives its inputs regardless of upstream errors. But:
  1. If the `SemanticIndex` contains `TypedErrorExpression` nodes, can the proof engine encounter them during obligation instantiation? If so, what does it do?
  2. If the `StateGraph` has structural violation diagnostics (unreachable states), does the proof engine suppress obligations for those states? (The spec addresses this via `ReachabilityFact`, but doesn't address the case where the _graph analyzer itself_ emitted errors.)
- **Why it matters:** Without clarity, the implementer might crash on `TypedErrorExpression` nodes.
- **Suggested resolution:** Add: "Proof obligations are not instantiated for expression trees containing `TypedErrorExpression` — those trees already have type-checker diagnostics and no valid proof subject."

---

**PE-G14: `GuardRelationImpliesObligation` in Strategy 4 is a pattern-match black box**
- **Severity:** ADVISORY
- **Location:** `proof-engine.md` §7 Strategy 4, lines 758–766
- **Description:** The function `GuardRelationImpliesObligation` is described as "a simple pattern match on (guard.Op, expression.Op, requirement.Comparison) triples — not a solver" and provides three example triples. But the complete triple set is not enumerated. The spec gives examples but not an exhaustive table.
- **Why it matters:** An implementer would need to enumerate all valid triples. Given the bounded operator set, this is a finite list — but it's work the spec should contain.
- **Suggested resolution:** Add an exhaustive table of (guard.Op, expr.Op, requirement) → discharge triples. Given Precept's bounded operator set, this is likely ~10-15 entries.

---

**PE-G15: No specification of whether proof engine runs for stateless precepts**
- **Severity:** ADVISORY
- **Location:** `proof-engine.md` — absent from §3 and §9
- **Description:** The graph analyzer has explicit stateless-precept handling (emitting vacuous `TerminalCompletenessFact` and `DeadEndStateFact`). The proof engine spec doesn't address stateless precepts. Stateless precepts have `EventHandlers` instead of `TransitionRows` and no state machine. Questions:
  1. Do event handlers in stateless precepts carry proof requirements? (Yes — their `TypedAction` nodes can have `ProofRequirements`.)
  2. Do Strategies 3/4 (guard-based) apply to event handlers? (Event handlers don't have guards — `TypedEventHandler` has no `Guard` field.)
  3. Are there any proof obligations specific to stateless precepts?
- **Why it matters:** If the implementer ignores stateless precepts, proof obligations on event handler actions would be silently missed.
- **Suggested resolution:** Add a subsection: "For stateless precepts, the proof engine walks `EventHandlers[].Actions[]` for obligations. Strategies 1 (Literal) and 2 (Modifier) apply. Strategies 3/4 do not apply (event handlers have no guards). All unresolved obligations produce diagnostics as normal."

---

**PE-G16: Spec's `ProofObligation.Site` identity matching is underspecified**
- **Severity:** ADVISORY
- **Location:** `proof-engine.md` §5, line 217 (CC#6 resolved box)
- **Description:** CC#6 says the builder "matches against `ProofLedger.FaultSiteLinks` by `ProofObligation.Site` identity." But `TypedExpression` is a record — C# record equality is structural, not referential. The spec doesn't say whether `Site` matching uses reference equality or structural equality. For records, structural equality means two independently-created `TypedBinaryOp` nodes with identical fields would match — which could cause false positives.
- **Why it matters:** If the builder or proof engine relies on reference identity, the implementer must ensure the same `TypedExpression` object instance is used in both the `ProofObligation` and the builder's walk. If structural equality is fine, no action needed.
- **Suggested resolution:** Clarify that `ProofObligation.Site` uses the same object reference passed through from `SemanticIndex` — no copies. Reference identity is preserved because the proof engine reads the same `TypedExpression` nodes the builder later visits.

---

### [DOC-ONLY] Gaps

---

**PE-G17: Spec shows `OperatorKind` in code samples but source uses different names**
- **Severity:** DOC-ONLY
- **Location:** `proof-engine.md` §7, line 454
- **Description:** The Strategy 1 pseudocode uses `OperatorKind.NotEquals`, `OperatorKind.GreaterThan`, etc. Need to verify these match the actual `OperatorKind` enum values in source. Minor naming discrepancies between spec pseudocode and source enum members would cause confusion during implementation.
- **Suggested resolution:** Cross-reference with `src/Precept/Language/OperatorKind.cs` and update spec pseudocode to use actual enum member names.

---

**PE-G18: Spec says "accumulate diagnostics without abandoning" but doesn't cite the principle by name**
- **Severity:** DOC-ONLY
- **Location:** `proof-engine.md` §9, line 945
- **Description:** The spec references Precept's error accumulation principle but doesn't cite the canonical name or doc location. Other pipeline stage docs reference `diagnostic-system.md §Error Accumulation`.
- **Suggested resolution:** Add cross-reference to `diagnostic-system.md`.

---

## Cross-Stage Seam Issues

### GraphAnalyzer → ProofEngine

1. **ReachabilityFact emission is per-state.** The GraphAnalyzer emits one `ReachabilityFact` per state (line 186 of GraphAnalyzer.cs). The spec's consumption table (line 907) says "suppress proof obligations on transitions originating from unreachable states." This is correct — the proof engine can look up `ReachabilityFact.IsReachable` for a transition's `FromState`. **No gap.**

2. **EventCoverageFact consumption is vague.** The spec says the proof engine "uses coverage gaps to reason about guard completeness: in states where an event is handled, are the guards sufficient?" (line 909). This is hand-wavy. What does "guard completeness" mean for the proof engine? Is the proof engine checking that guards on transition rows cover all possible field value ranges? That's a significantly harder problem than the spec's other strategies suggest. **Overlaps with PE-G1 (underspecified algorithm).** The EventCoverageFact consumption should be clarified — likely it's just a structural record, not an active proof check.

3. **DominancePathFact:** The spec says "if `DominatedTerminals` is empty, records a structural violation in the proof ledger." But the GraphAnalyzer already emits `RequiredStateDoesNotDominateTerminal` (111) for this case. The proof engine recording it again is redundant. **Clarify whether the proof engine adds to the structural record or merely records the fact for downstream consumption without additional diagnostics.**

### ProofEngine → Runtime (via Precept Builder)

4. **No `precept-builder.md` exists.** The spec references it in three CC#6 resolution boxes (lines 218, 236, 250). The downstream contract is hypothetical. **Covered by PE-G11.**

5. **`FaultSiteAnnotation` is described in the spec but does not exist in source.** The source has `FaultSiteDescriptor` in `Runtime/Descriptors.cs` with a different shape: `FaultSiteDescriptor(FaultCode, DiagnosticCode PreventedBy, int SourceLine)`. The spec's `FaultSiteAnnotation` has `(FaultCode Code, DiagnosticCode PreventedBy, SourceSpan Site)` — `SourceSpan` vs `int SourceLine`. These may be different types (builder-time vs runtime), but the relationship is unspecified.

---

## Catalog Compliance Issues

1. **PE-G2 is the primary catalog violation.** `FieldModifierMeta.ProofDischarges` is described as catalog metadata but doesn't exist. The spec correctly identifies this as catalog-driven (Strategy 2 reads `meta.ProofDischarges` from the catalog), but the catalog hasn't been updated. **BLOCKING.**

2. **The four strategies themselves are generic machinery, not catalog-driven.** The strategies are predicate functions that pattern-match on requirement types and expression types. This is correct — strategies are algorithms, not per-member metadata. The obligation _source_ is catalog-driven (ProofRequirements on catalog entries), the _discharge_ is algorithmic. **No violation.**

3. **`ProofRequirementMeta` catalog is correctly implemented.** The `ProofRequirements.cs` catalog with `GetMeta()` switch and `All` enumeration matches the catalog pattern. **No issue.**

---

## Diagnostic Catalog Status

| Code | Name | Stage | Severity | Registered in `DiagnosticCode.cs` | Registered in `Diagnostics.cs` | `PreventsFault` | Status |
|------|------|-------|----------|------------------------------------|-------------------------------|-----------------|--------|
| 82 | `UnsatisfiableGuard` | Proof | Warning | ✅ | ✅ | — | Complete |
| 83 | `DivisionByZero` | Proof | Error | ✅ | ✅ | `FaultCode.DivisionByZero` | Complete |
| 84 | `SqrtOfNegative` | Proof | Error | ✅ | ✅ | `FaultCode.SqrtOfNegative` | Complete |

**Three proof-stage diagnostics exist and are fully registered.** `RelatedCodes` cross-link all three. `FixHint` values are present.

**Missing diagnostic gap:** Collection non-empty proof failures have no proof-stage diagnostic code (PE-G9). Depending on resolution of PE-G9, additional codes may be needed.

**Missing diagnostic gap:** `DimensionProofRequirement`, `ModifierRequirement`, and `QualifierCompatibilityProofRequirement` failures have no diagnostic codes. Depending on resolution of PE-G1, additional codes may be needed.

---

## Spec Readiness Verdict

**NOT READY** — three BLOCKING gaps prevent implementation from starting.

### Blockers (must resolve before any implementation work):

1. **PE-G1:** Three of five `ProofRequirementKind` values have no discharge strategy. The implementer cannot write discharge logic for `Dimension`, `Modifier`, or `QualifierCompatibility` obligations without spec guidance.
2. **PE-G2:** `FieldModifierMeta.ProofDischarges` does not exist in source. Strategy 2 cannot be implemented as specified.
3. **PE-G3:** Output type `ProofLedger` and ~10 supporting record types don't exist. Shape declarations must be created before coding begins.

### Conditions (must resolve before implementation is complete, but won't block starting if blockers are cleared):

4. **PE-G4:** `AllTypedExpressions` doesn't exist — Pass 1 walk targets must be enumerated.
5. **PE-G5:** `ConstraintIdentity` shapes must match source, not spec.
6. **PE-G6:** `FindEnclosingTransitionRow` must be specified.
7. **PE-G7:** `ResolveSubject` must be specified.
8. **PE-G8:** Initial-state satisfiability needs a concrete algorithm.
9. **PE-G9:** Collection non-empty proof ownership must be decided (type checker vs proof engine).
10. **PE-G10:** Guard decomposition rules must be specified.

---

## Recommended Pre-Implementation Actions

1. **Resolve PE-G1** — For each of `Dimension`, `Modifier`, `QualifierCompatibility`: state which strategy handles it, or state that the type checker resolves it before proof. This is a design decision, not an implementation detail.

2. **Implement PE-G2** — Add `ProofDischarge` record and `ProofDischarges` property to `FieldModifierMeta`. Populate entries for `positive`, `nonnegative`, `nonzero`, `notempty`, `min(N)`, `max(N)`. This is a catalog prerequisite.

3. **Update spec for PE-G3** — Add a "Slice 0: Shape declarations" section listing all new types to create. The implementer should create these in a build-green commit before any logic.

4. **Resolve PE-G9** — Decide collection-empty ownership. This affects diagnostic code allocation and obligation walk scope.

5. **Update spec for PE-G5** — Align `ConstraintIdentity` shapes with source implementation.

6. **Add `FindEnclosingTransitionRow` spec (PE-G6)** and **`ResolveSubject` spec (PE-G7)** — These are the two most complex helper functions. Providing pseudocode prevents design divergence during implementation.

7. **Specify initial-state satisfiability algorithm (PE-G8)** — Define the default value model and which constraint scopes are checked.

8. **Add compound guard decomposition rules (PE-G10)** — Specify `and`/`or`/`not` handling.

9. **Add stateless precept handling section (PE-G15)** — Small but prevents a class of missed-obligation bugs.
