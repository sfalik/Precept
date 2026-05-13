# Field-State Guarantees

> **Status:** Design — Pending Shane Sign-off  
> **Author:** Frank (Lead/Architect)  
> **Date:** 2026-05-12

---

## 1. Executive Summary

Precept's governance model guarantees that invalid entity configurations are structurally impossible (§0.1.1 Prevention). Field-state guarantees make that rule concrete at state boundaries: a field declared `omit` in a state is structurally absent there, and the compiler must reject definitions that treat it as meaningfully present.

The compiler enforces three rules:

1. **D130 — OmittedFieldReadInState.** Expressions in state-anchored contexts must not read fields that are `omit` in the anchored state.
2. **D131 — OmittedFieldSetInTargetState.** A `set` action in a transition must not target a field that is `omit` in the to-state. This is explicitly grounded in §2.2 rule #6.
3. **D132 — RequiredFieldUnassignedOnEntry.** When a transition moves a required field from `omit` (from-state) to non-omit (to-state), the transition action must `set` that field. This follows from §0.1 Prevention, Totality, and Static completeness.

Field-state enforcement spans two distinct surfaces. On the Fire/set surface, `readonly` and `editable` do not restrict `set` actions; §2.2 rule #6 restricts `set` only by target-state `omit`. Those access modes govern only the Update (external patch) surface.

---

## 2. Current Pipeline Context

### What the pipeline currently does

1. **`PopulateAccessModes`** (TypeChecker) iterates `ConstructKind.AccessMode` constructs. For each, it resolves the state target, the field target (single field name via `FieldTargetSlot`), the access mode keyword (`readonly`/`editable`), and the optional guard expression. Produces `TypedAccessMode` records into `ctx.AccessModes`.

2. **`PopulateEditDeclarations`** (TypeChecker) iterates `ConstructKind.OmitDeclaration` constructs. Extracts a single field name into `TypedEditDeclaration`. **Omit declarations are disconnected from the access mode table** — they populate `ctx.EditDeclarations`, not `ctx.AccessModes`. This means omit status is invisible to any validation that reads `AccessModes`.

3. **`NormalizeTransitionRow`** (TypeChecker) resolves `fromState`, event, guard, action chain, and outcome. It knows `fromState` — every `TypedTransitionRow` carries `FromState: string?`. But it **does not consult access modes** when resolving actions or guards.

4. **`PopulateStateHooks`** (TypeChecker) resolves on-entry/on-exit hooks with their action chains. The hook's `StateName` is known, but actions are resolved without access mode awareness.

5. **No validation pass exists** that cross-references field access modes against transition actions, guards, or expression trees.

### What the pipeline does NOT do (and must)

- No diagnostic fires when a guard reads an omit field in the from-state.
- No diagnostic fires when a `set` targets a field omit in the to-state (D131 is defined but not emitted).
- No diagnostic fires when a transition leaves a required field unset during an omit→non-omit crossing.
- Existing diagnostic codes `ConflictingAccessModes` (42) and `RedundantAccessMode` (43) are defined but never emitted.

### Parser limitation (must be fixed first)

`ParseFieldTarget` parses comma-separated field names but stores only the first. Given `in Draft omit ApprovedAmount, AdjusterName`, the parser creates a `FieldTargetSlot` with `FieldName = "ApprovedAmount"` only — silently discarding `AdjusterName`. This must be fixed as a prerequisite to any field-state enforcement.

---

## 3. Diagnostic Table

| Diagnostic | Code | Status | Trigger | Spec Grounding |
|---|---|---|---|---|
| `OmittedFieldReadInState` | D130 | Defined | Reading a field that is `omit` in the state-anchored expression context (transition row guard, `in`-state ensure, `from`-state ensure, state action guard) | §2.2 "field absent from state entirely"; §3.5 name resolution ≠ semantic validity; evaluator.md line ~499: guard evaluates against from-state slots before working copy |
| `OmittedFieldSetInTargetState` | D131 | Defined | `set F` in a transition action where `F` is `omit` in the to-state | §2.2 rule #6: "`set` targeting an `omit` field in the target state is a compile error" |
| `RequiredFieldUnassignedOnEntry` | D132 | Defined | Field is non-optional, has no default, is `omit` in from-state, non-omit in to-state, and the transition action does not `set` it | §0.1.1 Prevention, §0.1.10 Totality, §0.1.11 Static completeness; spec gap — structural dual of `InitialEventMissingAssignments` (§3A.5) |

**Boundary of this design:** No Fire/set diagnostic is assigned for `readonly`, `editable`, or conditional editability. §2.2 rule #6 restricts `set` only when the target field is `omit` in the target state, and the evaluator enforces access modes on the Update surface.

---

## 4. Two Enforcement Surfaces (Critical Architecture)

This distinction is foundational. It must be prominent in any implementation.

### Update Surface — External Patches

| Property | Value |
|---|---|
| **What** | Direct field writes from API callers (the `Update(patch)` operation) |
| **Governed by** | `readonly`, `editable`, `modify when {condition}`, `omit` |
| **Failure mode** | Runtime outcome #8: "Access mode failure — Patch targets a field not editable in the current state" (§3A.2 line ~1746) |
| **Enforcement location** | Evaluator `Update` path (evaluator.md line 559–597): `GetAccessMode(field, version.CurrentState)` checks before any mutation |
| **Spec grounding** | §2.2 composition rules #1–#4c (baseline + state override model); §3A.2 outcome type #8 |

### Fire/Set Surface — Internal Event Actions

| Property | Value |
|---|---|
| **What** | `set`, `add`, `remove`, `enqueue`, `dequeue`, `push`, `pop`, `clear`, `append`, `insert`, `put` actions within transition rows, state hooks, and event handlers |
| **Governed by** | Only `omit` in the **target** state (rule #6). `readonly`/`editable` are irrelevant. |
| **Failure mode** | Compile error — invalid definitions are rejected before any engine exists |
| **Enforcement location** | TypeChecker validation pass |
| **Spec grounding** | §2.2 rule #6: "`set` targeting an `omit` field in the target state is a compile error; `readonly`/`editable` do not restrict `set`" |

**Why the spec draws this line:** The access mode system (`readonly`/`editable`/`modify when`) exists to control what **external callers** can directly change. Event actions are the **definition author's** declared mutations — the author is the authority on what fields change during a business event. Restricting the definition author's own `set` actions by `readonly` would make it impossible to update a field that external callers shouldn't touch directly. This is by design.

---

## 5. D130 — OmittedFieldReadInState

### Rationale

§2.2 defines `omit` as "field absent from state entirely." §3.5 puts "All field names" in expression scope without qualifying by omit status — this is a spec gap. However, reading a field that is structurally absent is semantically vacuous: the slot holds its cleared default (per rule #5, "field value resets to default on any transition into an `omit` state"), and any expression depending on that value is either dead (always evaluates to a known constant) or misleading (appears to depend on meaningful data that doesn't exist).

The evaluator confirms this at the operational level: `EvaluateGuard(row.Guard, version.Slots, args)` at evaluator.md line ~499 evaluates the guard against from-state slots. A field that is `omit` in the from-state holds its cleared default. The guard technically runs, but the result is deterministic on that field — it reads a constant.

### Scope: All State-Anchored Expression Contexts

D130 fires in **every** expression context where a state anchor is known and the field is `omit` in that state:

| Context | State Anchor | Spec Basis |
|---|---|---|
| Transition row guard | From-state (`TypedTransitionRow.FromState`) | Guard evaluates against from-state slots (evaluator.md line ~499) |
| `in <State> ensure` expression | Residency state | §3A.1: "constraint holds for every operation while the entity is in this state" — reading an absent field is vacuous |
| `from <State> ensure` expression | Exit state | §3A.1: "constraint is checked on any transition out of this state" — field absent in that state |
| State action guard (on-entry/on-exit) | Hook state | §3.5: state action expressions run in the hook's state context |

**Not in scope:** `to <State> ensure` expressions. The entity is transitioning INTO the state — the field may not yet be omit (it exists in the from-state). The to-state ensure evaluates after mutations, when the field may have been cleared by omit-on-entry (rule #5). However, the ensure's purpose is to validate the entering configuration, and an omit field has been cleared — reading the cleared value in a to-state ensure is a valid (if unusual) pattern. This is a judgment call; the conservative position is to not flag it.

**Wildcard rows** (`FromState == null`): Validate against ALL states the wildcard covers. If the field is `omit` in any reachable state, emit D130 listing all affected states.

---

## 6. D132 — RequiredFieldUnassignedOnEntry

### The Problem

Consider:

```precept
field ApprovedAmount as money in USD
in Draft omit ApprovedAmount

from Draft on Approve -> transition Review
```

`ApprovedAmount` has no default and is not `optional`. In `Draft`, it is omit — structurally absent, slot cleared. The transition to `Review` does not `set ApprovedAmount`. After the transition, the entity is in `Review` with `ApprovedAmount` holding its cleared default — but `ApprovedAmount` is a required field with no default. The entity is in an invalid configuration.

This is the omit→non-omit direction: a field goes from absent to present without being populated.

### Trigger Conditions

D132 fires when ALL of the following are true:

1. The field does NOT carry the `optional` modifier.
2. The field has no `default` value expression.
3. The field is `omit` in the transition's from-state.
4. The field is NOT `omit` in the transition's to-state (it becomes present).
5. The transition's action chain does NOT contain a `set` targeting this field.

**Exemptions:**
- Fields with `optional` — the empty/unset state IS a valid value.
- Fields with `default` — the default provides a valid value when the field becomes present.
- Computed fields (`field X as T <- Expr`) — their value is derived, not stored; recomputation produces a value automatically.
- Collection-typed fields (`set`, `list`, `queue`, `bag`, `log`) — collection types have an intrinsic empty value (empty set, empty list, etc.) that is a valid initial state when the field becomes present. An empty collection is semantically meaningful, unlike an unset scalar. No explicit `set` is required on an omit→non-omit crossing for a collection field.

### Spec Grounding

The spec is silent on the omit→non-omit direction. Composition rule #5 says "field value resets to default on any transition into an `omit` state" — it covers entering-omit (clear to default) but says nothing about leaving-omit (must populate). This is a spec gap.

D132 follows from first principles:

- **§0.1.1 Prevention:** Invalid configurations are structurally prevented. A required field with no value is invalid.
- **§0.1.10 Totality:** Every expression evaluates to a result. A required field with no value violates totality for any expression that reads it.
- **§0.1.11 Static completeness:** If a precept compiles without diagnostics, it does not fault at runtime. An unpopulated required field in a non-omit state could cause runtime faults.

D132 is the **structural dual of `InitialEventMissingAssignments`** (§3A.5). At construction, the compiler ensures all required fields are populated. D132 extends this guarantee to state transitions that bring fields from absent to present.

---

## 7. Field Initialization — Three Precept Forms

§3A.5 defines entity construction semantics across three precept forms. D132's applicability varies by form.

### Form 1: Stateful Precept With Initial Event

```precept
event Create(Name as string, Amount as money in USD) initial
```

- **`InitialEventMissingAssignments`** ensures all required fields are set at construction time. The initial event fires through the standard pipeline — guards, mutations, ensures, constraints.
- **D132 is needed** for subsequent state transitions. The initial event populates fields at construction; D132 ensures fields remain populated across omit→non-omit crossings during the entity's lifecycle.

### Form 2: Stateful Precept Without Initial Event

```precept
precept SimpleTracker
  field Status as choice of string("Open", "Closed")
  state Open initial
  state Closed terminal
```

- **`RequiredFieldsNeedInitialEvent`** rejects the definition if any field is non-optional and has no default. Construction is parameterless — there is no way to provide initial values.
- **D132 cannot fire** in this form. Since all fields must have defaults or be optional (enforced by `RequiredFieldsNeedInitialEvent`), every field satisfies D132's exemption criteria. The diagnostic's trigger conditions are structurally unsatisfiable.

### Form 3: Stateless Precept

Per §3A.5: "All other steps apply unchanged."

- **`RequiredFieldsNeedInitialEvent`** and **`InitialEventMissingAssignments`** apply per §3A.5 — the construction semantics are the same.
- **D132 applies** to any state transition in a stateless precept that has states. However, stateless precepts by definition declare no states — so omit declarations (which require a state target) cannot exist. D132 is structurally inapplicable to truly stateless precepts.

> **Clarification:** A "stateless precept" in Precept means one with no `state` declarations. Since `omit` requires `in <State>`, a stateless precept cannot have omit fields, and D132's trigger conditions (field is omit in from-state) cannot be met.

---

## 8. Spec Gaps

This section identifies where the design relies on first-principles reasoning beyond explicit specification text.

### Gap 1: §3.5 Scope — "All field names" without omit qualification

**What the spec says:** §3.5 Expression Scope lists "All field names" in scope for rule conditions, ensure conditions, transition row guards, and state action guards. No qualification by omit status.

**What D130 asserts:** Reading a field that is `omit` in the anchored state is a compile error.

**Reconciliation:** §3.5 describes name resolution scope — what identifiers the compiler can resolve. §2.2 describes structural semantics — `omit` means "field absent from state entirely." D130 fills the gap between resolution (the name resolves) and semantic validity (the field is absent). The name is resolvable; using it is an error because the value is semantically vacuous.

**Spec annotation needed:** When D130 ships, §3.5 should add a note: "Field names that resolve to fields `omit` in the anchoring state are in scope for resolution but produce `OmittedFieldReadInState` (D130) — reading a structurally absent field is a compile error."

### Gap 2: §2.2 Rule #5 — Silent on leaving-omit direction

**What the spec says:** Rule #5: "field value resets to default on any transition into an `omit` state (including self-transitions); does NOT apply to `no transition`." This covers entering-omit.

**What D132 asserts:** Leaving-omit (omit→non-omit) for a required field without a default requires the transition to `set` the field.

**Reconciliation:** The spec covers entering-omit (clear) but is silent on leaving-omit (populate). D132 follows from §0.1 principles — Prevention, Totality, and Static completeness demand that a required field cannot exist in a non-omit state without a valid value. This is the structural dual of `InitialEventMissingAssignments`.

**Spec addition needed:** A new composition rule (or annotation on rule #5) should state: "A transition from a state where a field is `omit` to a state where it is not must `set` the field if it is non-optional and has no default — `RequiredFieldUnassignedOnEntry` (D132)."

### Gap 3: State hook access mode direction

The spec at §2.2 describes state hooks with `(to|from) StateTarget ("when" BoolExpr)? ("->" ActionStatement)*` but does not explicitly state which state's access modes govern hook action expressions. The natural reading is:
- `to <State>` (on-entry) hooks operate in the context of the entered state.
- `from <State>` (on-exit) hooks operate in the context of the exiting state.

D130 follows this reading: an on-entry hook guard reading a field omit in the entered state triggers D130; an on-exit hook guard reading a field omit in the exiting state triggers D130. This is not explicitly grounded in the spec but follows from the contextual semantics.

---

## 9. Design Motivation — Omit vs Sentinel Defaults

`omit` is the correct modeling tool when a field has no business meaning in a state. Using `default 0`, `default false`, `default ""`, or similar sentinel defaults in those states keeps the field structurally present while smuggling "not meaningful yet" through a value that looks legitimate. That weakens the language contract: readers, tooling, and AI agents must remember a convention instead of learning the truth from the state shape. By contrast, `omit` says the field is absent by construction until the lifecycle reaches a state where the field actually exists.

`default` remains correct when it expresses a real business starting value rather than a placeholder sentinel. An account can genuinely start with `Balance = 0`; a review workflow does not genuinely start with `ApprovedAmount = 0` if nothing has been approved yet. This design therefore treats `omit` as the preferred surface for not-yet-meaningful fields and relies on D132 `RequiredFieldUnassignedOnEntry` to make that choice safe: when a transition makes the field present again, the compiler forces an explicit assignment. The guarantee is structural, not a runtime null or sentinel convention.

---

## 10. Implementation Plan

> **Author:** Frank (Lead/Architect)
> **Date:** 2026-05-12
> **Quality bar exemplar:** PR #108 (9 vertical slices, method-level specificity, ~56 edge-case tests, 16 regression anchors)

---

### Architectural Approach

**Post-resolution validation, not resolution-time threading.** The new field-state enforcement uses a post-resolution validation pass (`ValidateFieldStateGuarantees`) that walks already-resolved `TypedExpression` trees and `TypedAction` chains with knowledge of the state anchor. This follows the established pattern of `ValidateCIEnforcement` (TypeChecker.Validation.cs:350) — a recursive expression walker that runs after all Pass 2 resolution is complete.

**Why not thread `CurrentFromState`/`CurrentTargetState` into `CheckContext`?** Threading state context into resolution would require modifying `ResolveIdentifier` (TypeChecker.Expressions.cs:522) to emit diagnostics during name resolution, mixing two concerns (resolution and semantic validation) and requiring careful state management across every resolution call site. The post-resolution approach is lower risk, follows existing patterns, and keeps all field-state validation in one coherent pass.

**Omit lookup as a `HashSet<(string State, string Field)>`.** Built from `OmitDeclaration` constructs (with wildcard expansion) during a new `BuildOmitLookup` step. Stored on `CheckContext` as a lookup property. The existing `TypedEditDeclaration` record is NOT modified — it is a placeholder for future stateless-precept edit declarations (D24) and has no `StateName` property.

---

### Ordering Constraints

```
Slice 0 (parser fix) ──→ Slice 2 (omit lookup) ──→ Slice 3 (D130)
                                                ──→ Slice 4 (D131)
                                                ──→ Slice 5 (D132)
Slice 1 (diagnostic infra) ──→ Slice 3, 4, 5
Slice 3, 4, 5 ──→ Slice 6 (sample corrections)
Slice 6 ──→ Slice 7 (spec updates)
Slice 8 (MCP + LS sync) is independent — can be authored at any point
Slice 9 (OR / ProofEngine bugfix) is independent of Slices 0–8
Slice 10 (D93 enforcement) is independent of Slices 0–9
Slice 10 ──→ Slice 11 (D94 enforcement)
```

- **Slice 0 must be first.** The parser fix is a prerequisite for all other slices because multi-field omit declarations silently discard fields 2–N. Without this fix, the omit lookup will be incomplete.
- **Slice 1 must precede Slices 3, 4, 5.** Diagnostic codes and metadata must exist before enforcement code can emit them.
- **Slice 2 must precede Slices 3, 4, 5.** The omit lookup is the data structure all enforcement rules query.
- **Slices 3, 4, 5 are independent of each other.** D130, D131, and D132 enforce different rules and can be implemented in any order. D130 is recommended first because it establishes the expression-walking infrastructure reused by D131 and D132.
- **Slice 6 must follow Slices 3, 4, 5.** Sample corrections depend on the diagnostics being emitted so the fixes can be validated.
- **Slice 7 follows Slice 6.** Spec annotations reference the shipped diagnostic behavior.
- **Slice 8 is a verification slice** confirming automatic propagation.
- **Slice 9 is a standalone correctness bugfix.** It is appended after the current slices only to avoid renumbering the approved v3 plan; it does **not** depend on D130/D131/D132 enforcement.
- **Slice 10 is independent** of Slices 0–9 — it adds new construction-time validation that uses only Pass 1 symbols (`ctx.Events`, `ctx.Fields`). D93 diagnostic metadata already exists (declared in Slice 1 era).
- **Slice 11 depends on Slice 10.** It extends `ValidateConstructionGuarantees` with the D94 initial-event-per-row analysis.

---

### Slice 0 — Parser Fix: `FieldTargetSlot` Multi-Field Broadcast

The parser's `ParseFieldTarget` method (Parser.cs:1005–1048) correctly lexes and consumes all comma-separated field names but creates a `FieldTargetSlot` with only the first field's name. Given `in Draft omit ApprovedAmount, AdjusterName`, the parser stores `FieldName = "ApprovedAmount"` and silently discards `AdjusterName`. This must be fixed before any field-state enforcement can be correct.

**Modify:**

- **`FieldTargetSlot`** (SlotValue.cs:100–105) — add property:
  ```csharp
  public ImmutableArray<(string Name, SourceSpan Span)> AdditionalFields { get; init; }
      = ImmutableArray<(string, SourceSpan)>.Empty;
  ```
  `FieldName` and `NameSpan` continue to hold the first field (backward compatibility). `AdditionalFields` holds fields 2–N with their spans. Consumers iterate `[FieldName + AdditionalFields]` for the full set.

- **`ParseFieldTarget`** (Parser.cs:1005–1048) — in the comma-separated loop (lines 1025–1036), collect each consumed identifier's text and span into a builder:
  ```csharp
  var additionalBuilder = ImmutableArray.CreateBuilder<(string, SourceSpan)>();
  while (Peek().Kind == TokenKind.Comma)
  {
      Advance(); // consume comma
      if (Peek().Kind == TokenKind.Identifier)
      {
          var next = Advance();
          additionalBuilder.Add((next.Text, next.Span));
          lastSpan = next.Span;
          continue;
      }
      // ... existing error handling
  }
  return new FieldTargetSlot(first.Text, SourceSpan.Covering(first.Span, lastSpan))
  {
      NameSpan = first.Span,
      AdditionalFields = additionalBuilder.ToImmutable(),
  };
  ```

- **`PopulateAccessModes`** (TypeChecker.cs:950–970) — after resolving the primary `fieldSlot.FieldName`, iterate `fieldSlot.AdditionalFields` and produce additional `TypedAccessMode` records per resolved state. Each additional field name goes through the same `ctx.FieldLookup.TryGetValue` + `FieldReference` registration path.

- **`PopulateEditDeclarations`** (TypeChecker.cs:1069–1092) — after extracting the primary field name, iterate `fieldSlot.AdditionalFields` and add each to the `fields` builder. Each additional field goes through `ctx.FieldLookup.TryGetValue` + `FieldReference` registration.

- **`NameBinder`** (NameBinder.cs:422–425) — after resolving the primary `fieldTargetSlot.FieldName`, iterate `fieldTargetSlot.AdditionalFields` and call `ResolveFieldReference` for each additional name/span pair. This is the path that emits `UndeclaredField` diagnostics for unknown field names — it applies to additional fields exactly as it does to the primary field. `PopulateAccessModes` and `PopulateEditDeclarations` use `FieldLookup.TryGetValue` for reference registration with silent skip on miss, consistent with the primary-field path.

**Tests (in `test/Precept.Tests/Parser/`):**

- `ParseFieldTarget_MultiField_AllFieldsCaptured` — `[Fact]`: parse `in Draft omit A, B, C`; assert `FieldName == "A"`, `AdditionalFields` has `("B", _)` and `("C", _)` with correct spans.
- `ParseFieldTarget_SingleField_AdditionalFieldsEmpty` — `[Fact]`: parse `in Draft omit A`; assert `AdditionalFields.IsEmpty`.
- `ParseFieldTarget_MultiField_SpanCoversAll` — `[Fact]`: assert the containing `Span` covers from first field to last field.
- `ParseFieldTarget_MultiField_TrailingComma_Diagnostic` — `[Fact]`: parse `in Draft omit A, B,`; assert `AdditionalFields` has `("B", _)` and an `ExpectedToken` diagnostic is emitted.

**Tests (in `test/Precept.Tests/TypeChecker/`):**

- `PopulateAccessModes_MultiFieldOmit_AllFieldsRecorded` — `[Fact]`: compile `in Draft omit A, B`; assert both `A` and `B` appear as `TypedAccessMode` entries with `Mode = Omit` for state `Draft`. (Note: this test validates the TypeChecker consumer, not just the parser.)
- `ParseFieldTarget_MultiFieldAccessMode_AllFieldsRecorded` — `[Fact]`: compile `in Draft readonly A, B`; assert both `A` and `B` appear as `TypedAccessMode` entries with `Mode = Readonly` for state `Draft`. Validates the access-mode consumer path (distinct from the omit path tested above).
- `PopulateEditDeclarations_MultiFieldOmit_AllFieldsRecorded` — `[Fact]`: compile `in Draft omit A, B`; assert `TypedEditDeclaration.EditableFields` contains both `A` and `B`.
- `NameBinder_MultiFieldOmit_AllFieldsGetReferences` — `[Fact]`: compile `in Draft omit A, B`; assert field references are recorded for both `A` and `B`.

**Regression anchors:**

- `ParserScopedConstructTests.OmitDeclaration_HappyPath_FieldTargetSlot_ContainsFieldName` (test/Precept.Tests/Parser/ParserScopedConstructTests.cs:854) — must still pass for single-field case.
- `ParserScopedConstructTests.OmitDeclaration_HappyPath_FieldTargetSlot_ContainsFieldName` (line 879) — `omit all` broadcast must still have `FieldName == "all"`.
- `ParserCoverageGapTests` omit-related tests (lines 413–436) — omit all and modify all broadcast assertions.
- `StateTargetTests.Parser_OmitAll_OmitDeclaration_FieldTargetSlot_CarriesAll` (line 139) — broadcast keyword path.
- `AccessMode_HappyPath_FieldTargetSlot_ContainsFieldName` (line 690) — single-field access mode.

**Files:** `src/Precept/Pipeline/SlotValue.cs`, `src/Precept/Pipeline/Parser.cs`, `src/Precept/Pipeline/TypeChecker.cs`, `src/Precept/Pipeline/NameBinder.cs`

- [ ] Add `AdditionalFields` property to `FieldTargetSlot`
- [ ] Modify `ParseFieldTarget` to collect fields 2–N
- [ ] Update `PopulateAccessModes` to iterate all field names
- [ ] Update `PopulateEditDeclarations` to iterate all field names
- [ ] Update `NameBinder` to resolve all field references
- [ ] Parser tests (4 tests)
- [ ] TypeChecker consumer tests (4 tests)
- [ ] Verify regression anchors pass (5 anchors)

---

### Slice 1 — Diagnostic Infrastructure: D130, D131, D132

Add the three diagnostic codes and their `DiagnosticMeta` entries. No enforcement logic — codes and metadata only.

**Modify:**

- **`DiagnosticCode.cs`** (src/Precept/Language/DiagnosticCode.cs) — add after `DuplicateStateInList = 129` (line 52):
  ```csharp
  /// <summary>
  /// Reading a field that is <c>omit</c> in the state-anchored expression context
  /// (transition row guard, in-state ensure, from-state ensure, state action guard/RHS).
  /// </summary>
  OmittedFieldReadInState            = 130,

  /// <summary>
  /// A <c>set</c> action in a transition or state hook targets a field that is
  /// <c>omit</c> in the target state.
  /// Grounded in §2.2 rule #6.
  /// </summary>
  OmittedFieldSetInTargetState       = 131,

  /// <summary>
  /// A transition moves a required field from <c>omit</c> (from-state) to non-omit
  /// (to-state) without a <c>set</c> action assigning it.
  /// Structural dual of <see cref="InitialEventMissingAssignments"/> (D94).
  /// </summary>
  RequiredFieldUnassignedOnEntry     = 132,
  ```

- **`Diagnostics.cs`** (src/Precept/Language/Diagnostics.cs) — add `DiagnosticMeta` entries in the `GetMeta` switch expression, after the `DuplicateStateInList` (D129) entry:

  **D130 message text:**
  ```
  "Field '{0}' is omitted in state '{1}' and cannot be read in this expression"
  ```
  - `{0}` = field name, `{1}` = state name (or comma-joined state list for wildcards)
  - FixHint: `"Remove the reference to '{0}', or remove the omit declaration for '{0}' in state '{1}'"`
  - TriggerCondition: `"An expression in a state-anchored context (guard, ensure, state action) reads a field that is omit in the anchored state."`
  - RecoverySteps: `["Remove the field reference from this expression", "Remove 'in {State} omit {Field}' if the field should be accessible in this state"]`
  - Category: `DiagnosticCategory.Structure`
  - Stage: `DiagnosticStage.Type`
  - Severity: `Severity.Error`

  **D131 message text:**
  ```
  "Field '{0}' is omitted in target state '{1}'; this transition cannot set it"
  ```
  - `{0}` = field name, `{1}` = target state name
  - FixHint: `"Remove the 'set {0}' action, or remove the omit declaration for '{0}' in state '{1}'"`
  - TriggerCondition: `"A set action in a transition or on-entry hook targets a field that is omit in the target state."`
  - RecoverySteps: `["Remove the set action — the field is structurally absent in the target state", "Remove 'in {State} omit {Field}' if the field should be settable in this state"]`
  - Category: `DiagnosticCategory.Structure`
  - Stage: `DiagnosticStage.Type`
  - Severity: `Severity.Error`

  **D132 message text:**
  ```
  "Required field '{0}' is omitted in '{1}' but present in '{2}'; add `set {0} = ...` to this transition"
  ```
  - `{0}` = field name, `{1}` = from-state, `{2}` = to-state
  - FixHint: `"Add 'set {0} = <value>' to the transition action chain, or add 'in {2} omit {0}' to keep it absent, or add 'default <value>' to the field declaration"`
  - TriggerCondition: `"A transition moves a required field (non-optional, no default, not computed) from omit (from-state) to non-omit (to-state) without a set action assigning it."`
  - RecoverySteps: `["Add 'set {Field} = <value>' to the transition action chain", "Add 'in {TargetState} omit {Field}' if the field should remain absent", "Add 'default <value>' to the field declaration", "Mark the field 'optional' if absence is valid"]`
  - RelatedCodes: `[DiagnosticCode.InitialEventMissingAssignments]`
  - Category: `DiagnosticCategory.Structure`
  - Stage: `DiagnosticStage.Type`
  - Severity: `Severity.Error`

**Scope decision — ConflictingAccessModes (D42) / RedundantAccessMode (D43):**

> **Out of scope for this work.** D42 and D43 are access-mode declaration validation rules (§2.2 rules #4c and #7). They are defined with full `DiagnosticMeta` entries (Diagnostics.cs:335–347) but never emitted. They validate the *declaration surface* (conflicting/redundant `modify` statements) — not the *enforcement surface* (field reads/writes in state-anchored contexts). Activating them is independent work that requires its own validation pass and test coverage. Tracked separately.

**Tests (in `test/Precept.Tests/`):**

- `DiagnosticsTests.DiagnosticMeta_D130_HasRequiredFields` — `[Fact]`: verify D130 meta has non-null FixHint, TriggerCondition, RecoverySteps, and correct Stage/Severity/Category.
- `DiagnosticsTests.DiagnosticMeta_D131_HasRequiredFields` — `[Fact]`: same for D131.
- `DiagnosticsTests.DiagnosticMeta_D132_HasRequiredFields` — `[Fact]`: same for D132, plus `RelatedCodes` contains `InitialEventMissingAssignments`.
- `DiagnosticsTests.DiagnosticMeta_AllCodesHaveEntries` — existing exhaustiveness test must pass with D130–D132 added.

**Regression anchors:**

- `DiagnosticsTests` — existing tests that verify all codes have metadata entries (exhaustiveness check).
- `DiagnosticsCatalog` tests — any test that enumerates all diagnostic codes.

**Files:** `src/Precept/Language/DiagnosticCode.cs`, `src/Precept/Language/Diagnostics.cs`

- [ ] Add D130, D131, D132 enum values
- [ ] Add D130 `DiagnosticMeta` entry with exact message text
- [ ] Add D131 `DiagnosticMeta` entry with exact message text
- [ ] Add D132 `DiagnosticMeta` entry with exact message text
- [ ] Diagnostic metadata tests (3 + existing exhaustiveness)

---

### Slice 2 — Omit Lookup Construction

Build the omit lookup data structure from `OmitDeclaration` constructs, including wildcard expansion for `omit all` and multi-state targets. This is the foundation queried by Slices 3, 4, and 5.

**Create:**

- **`BuildOmitLookup`** method in `TypeChecker.Validation.cs` (~30 lines):
  ```csharp
  private static HashSet<(string State, string Field)> BuildOmitLookup(
      ConstructManifest manifest, CheckContext ctx)
  ```
  Algorithm:
  1. If `manifest.ByKind` does not contain `ConstructKind.OmitDeclaration`, return empty set.
  2. For each `OmitDeclaration` construct:
     a. Resolve state targets via `ResolveStateTargets(stateSlot, ctx)` — handles single states, comma-separated states, and `any` wildcard.
     b. Extract the primary field name from `FieldTargetSlot.FieldName`.
     c. If `FieldName` is a broadcast keyword (checked via `HasKeywordTokenMeta(name, meta => meta.IsFieldBroadcast)`), expand to ALL declared fields from `ctx.Fields`.
     d. Extract additional field names from `FieldTargetSlot.AdditionalFields` (Slice 0 property).
     e. For each `(resolvedState, fieldName)` pair, add to the `HashSet`.
  3. For wildcard state targets (`resolvedState.IsWildcard`), expand to ALL declared states from `ctx.States`. Add `(state.Name, fieldName)` for every state.
  4. Return the populated `HashSet<(string State, string Field)>`.

**Modify:**

- **`CheckContext.cs`** (src/Precept/Pipeline/CheckContext.cs) — add after `EditDeclarations` (line 102):
  ```csharp
  /// <summary>
  /// Omit lookup: (state, field) pairs where the field is declared <c>omit</c>.
  /// Built by <see cref="TypeChecker.BuildOmitLookup"/> from OmitDeclaration constructs.
  /// Consumed by <see cref="TypeChecker.ValidateFieldStateGuarantees"/>.
  /// </summary>
  public HashSet<(string State, string Field)> OmitLookup { get; } = new();
  ```

- **`TypeChecker.Check`** (TypeChecker.cs:28–62) — add the omit lookup build step after `PopulateEditDeclarations` and before `ValidateModifiers`:
  ```csharp
  PopulateEditDeclarations(manifest, ctx);

  // Field-state omit lookup (prerequisite for ValidateFieldStateGuarantees)
  BuildOmitLookup(manifest, ctx);

  ValidateModifiers(ctx);
  ```

**Tests (in `test/Precept.Tests/TypeChecker/`):**

- `OmitLookup_SingleFieldSingleState_Present` — `[Fact]`: `in Draft omit Amount` → lookup contains `("Draft", "Amount")`.
- `OmitLookup_MultiFieldSingleState_AllPresent` — `[Fact]`: `in Draft omit A, B, C` → lookup contains all three pairs.
- `OmitLookup_SingleFieldMultiState_AllPresent` — `[Fact]`: `in Draft, Pending omit A` → lookup contains `("Draft", "A")` and `("Pending", "A")`.
- `OmitLookup_WildcardState_ExpandsToAllDeclaredStates` — `[Fact]`: `in any omit A` with states Draft, Submitted, Done → lookup contains `("Draft", "A")`, `("Submitted", "A")`, `("Done", "A")`.
- `OmitLookup_BroadcastField_ExpandsToAllDeclaredFields` — `[Fact]`: `in Draft omit all` with fields A, B → lookup contains `("Draft", "A")` and `("Draft", "B")`.
- `OmitLookup_NoOmitDeclarations_EmptyLookup` — `[Fact]`: precept with no omit declarations → lookup is empty.
- `OmitLookup_UndeclaredField_DiagnosticEmitted_NotInLookup` — `[Fact]`: `in Draft omit NonExistent` → `UndeclaredField` diagnostic, field not in lookup.

**Regression anchors:**

- All existing TypeChecker tests — `BuildOmitLookup` runs before `ValidateModifiers`, so any regression in state/field resolution would surface as test failures.
- `TypeCheckerTransitionTests` — transition row resolution must be unaffected.

**Files:** `src/Precept/Pipeline/CheckContext.cs`, `src/Precept/Pipeline/TypeChecker.cs`, `src/Precept/Pipeline/TypeChecker.Validation.cs`

- [ ] Add `OmitLookup` property to `CheckContext`
- [ ] Create `BuildOmitLookup` method in `TypeChecker.Validation.cs`
- [ ] Wire into `TypeChecker.Check` pipeline after `PopulateEditDeclarations`
- [ ] Omit lookup tests (7 tests)
- [ ] Verify regression anchors pass

---

### Slice 3 — D130: OmittedFieldReadInState

Enforce that expressions in state-anchored contexts do not read fields that are `omit` in the anchored state. This is the core expression-walking infrastructure that Slice 4 and 5 also use.

**Create:**

- **`ValidateFieldStateGuarantees`** method in `TypeChecker.Validation.cs` (~80 lines):
  ```csharp
  private static void ValidateFieldStateGuarantees(CheckContext ctx)
  ```
  This is the top-level validation pass. It walks four context types for D130:

  1. **Transition row guards** — for each `TypedTransitionRow` in `ctx.TransitionRows`:
     - If `FromState != null`: walk `row.Guard` for `TypedFieldRef` nodes; for each, check `ctx.OmitLookup.Contains((row.FromState, fieldRef.FieldName))` → emit D130.
     - If `FromState == null` (wildcard): collect all states where the field is omit; if any match, emit D130 listing all affected states.

  2. **Transition row action RHS expressions** — for each action in `row.Actions`:
     - If action is `TypedInputAction`, walk `InputExpression` and `SecondaryExpression` for `TypedFieldRef` nodes against the from-state omit lookup.

  3. **Ensures** — for each `TypedEnsure` in `ctx.Ensures`:
     - If `Kind == ConstraintKind.StateResidency` (`in State ensure`): walk `Condition` for field refs omit in `AnchorState`.
     - If `Kind == ConstraintKind.StateExit` (`from State ensure`): walk `Condition` for field refs omit in `AnchorState`.

  4. **State hooks** — for each `TypedStateHook` in `ctx.StateHooks`:
     - Walk `Guard` (if present) for field refs omit in `StateName`.
     - Walk action RHS expressions for field refs omit in `StateName`.

- **`CollectFieldRefsFromExpression`** helper method (~40 lines):
  ```csharp
  private static void CollectFieldRefsFromExpression(
      TypedExpression? expr, List<TypedFieldRef> refs)
  ```
  Recursive walker following the `EnforceCIInExpression` pattern (TypeChecker.Validation.cs:407). Walks all expression subtypes (`TypedBinaryOp`, `TypedUnaryOp`, `TypedFunctionCall`, `TypedConditional`, `TypedQuantifier`, `TypedListLiteral`, `TypedMethodCall`, `TypedInterpolatedString`, `TypedPostfixOp`, `TypedMemberAccess`). When it encounters a `TypedFieldRef`, adds it to the output list.

- **`EmitD130ForWildcard`** helper (~15 lines):
  ```csharp
  private static void EmitD130ForWildcard(
      string fieldName, SourceSpan span, CheckContext ctx)
  ```
  Collects all states where field is omit: `ctx.States.Where(s => ctx.OmitLookup.Contains((s.Name, fieldName)))`. If any, emits D130 with comma-joined state names as `{1}`.

**Modify:**

- **`TypeChecker.Check`** (TypeChecker.cs:56–60) — add after `ValidateStructural`:
  ```csharp
  ValidateStructural(ctx);

  // Field-state guarantees (D130, D131, D132)
  ValidateFieldStateGuarantees(ctx);

  ValidateCIEnforcement(ctx);
  ```

**Tests (in `test/Precept.Tests/TypeChecker/`):**

New test class: `TypeCheckerFieldStateTests.cs`

*Transition row guard — D130:*
- `D130_TransitionGuard_ReadsOmitField_Fires` — `[Fact]`: `in Draft omit F` + `from Draft on E when F > 0 -> ...` → D130 on `F`.
- `D130_TransitionGuard_ReadsNonOmitField_NoDiagnostic` — `[Fact]`: field not omit in from-state → no D130.
- `D130_TransitionGuard_OmitInDifferentState_NoDiagnostic` — `[Fact]`: `in Pending omit F` + `from Draft on E when F > 0 -> ...` → no D130 (F is not omit in Draft).
- `D130_TransitionGuard_WildcardFromState_ReadsOmitField_ListsAffectedStates` — `[Fact]`: `in Draft omit F` + `from any on E when F > 0 -> ...` → D130 message includes "Draft".
- `D130_TransitionGuard_WildcardFromState_OmitInMultipleStates_ListsAll` — `[Fact]`: `in Draft omit F` + `in Pending omit F` + wildcard row → D130 lists "Draft, Pending".
- `D130_TransitionGuard_NoGuard_NoDiagnostic` — `[Fact]`: transition with no guard → no D130.

*Action RHS — D130:*
- `D130_ActionRHS_ReadsOmitField_Fires` — `[Fact]`: `in Draft omit G` + `from Draft on E -> set F = G -> ...` → D130 on `G`.
- `D130_ActionRHS_ReadsEventArg_NoDiagnostic` — `[Fact]`: `from Draft on E -> set F = E.Arg -> ...` → no D130 (event args are not fields).
- `D130_ActionRHS_ComplexExpression_ReadsOmitField_Fires` — `[Fact]`: `from Draft on E -> set F = G + 1 -> ...` where G is omit → D130.

*Ensure — D130:*
- `D130_InStateEnsure_ReadsOmitField_Fires` — `[Fact]`: `in Draft omit F` + `in Draft ensure F > 0 because "..."` → D130.
- `D130_FromStateEnsure_ReadsOmitField_Fires` — `[Fact]`: `in Draft omit F` + `from Draft ensure F > 0 because "..."` → D130.
- `D130_ToStateEnsure_ReadsOmitField_NoDiagnostic` — `[Fact]`: `in Draft omit F` + `to Draft ensure F > 0 because "..."` → no D130 (to-state ensures are out of scope per §5 design).

*State hooks — D130:*
- `D130_ToStateHook_GuardReadsOmitField_Fires` — `[Fact]`: `in Draft omit F` + `to Draft when F > 0 -> set G = 1` → D130 on guard.
- `D130_FromStateHook_ActionRHSReadsOmitField_Fires` — `[Fact]`: `in Draft omit F` + `from Draft -> set G = F` → D130 on RHS.

*Self-loop — D130:*
- `D130_SelfLoop_OmitInState_Fires` — `[Fact]`: `in S omit F` + `from S on E when F > 0 -> transition S` → D130 fires (from-state is S, F is omit in S).

**Regression anchors:**

- `TypeCheckerTransitionTests` — all existing transition resolution tests must pass unchanged.
- `TypeCheckerExpressionTests` — expression resolution is unchanged; only validation is added.
- `TypeCheckerStructuralTests` — computed field cycle detection is unaffected.
- `WritableSurfaceTests` — existing writable surface tests must pass.

**Files:** `src/Precept/Pipeline/TypeChecker.cs`, `src/Precept/Pipeline/TypeChecker.Validation.cs`

- [ ] Create `ValidateFieldStateGuarantees` method
- [ ] Create `CollectFieldRefsFromExpression` helper
- [ ] Create `EmitD130ForWildcard` helper
- [ ] Wire into `TypeChecker.Check` pipeline
- [ ] Transition row guard tests (6 tests)
- [ ] Action RHS tests (3 tests)
- [ ] Ensure tests (3 tests)
- [ ] State hook tests (2 tests)
- [ ] Self-loop test (1 test)
- [ ] Verify regression anchors (4 anchor families)

---

### Slice 4 — D131: OmittedFieldSetInTargetState

Enforce that `set` actions in transitions and on-entry state hooks do not target fields that are `omit` in the target state. This is grounded in §2.2 rule #6.

**Modify:**

- **`ValidateFieldStateGuarantees`** (TypeChecker.Validation.cs) — add D131 checks within the same method:

  1. **Transition rows** — for each `TypedTransitionRow` where `Outcome == Transition` and `TargetState != null`:
     - For each action in `row.Actions` where `action.Kind` is a set-family action (`set`, or any action that writes to `action.FieldName`):
       - Check `ctx.OmitLookup.Contains((row.TargetState, action.FieldName))` → emit D131 with `{0} = action.FieldName`, `{1} = row.TargetState`.
     - For wildcard from-state rows: the target state is explicit (not wildcarded), so D131 checks the same way.

  2. **State hooks** — for each `TypedStateHook` where `Scope == AnchorScope.OnEntry` (`to State`):
     - The entered state IS the target state. For each action:
       - Check `ctx.OmitLookup.Contains((hook.StateName, action.FieldName))` → emit D131.
     - `from State` (on-exit) hooks: D131 does NOT apply — the target state is unknown at the hook level.

**Tests (in `test/Precept.Tests/TypeChecker/TypeCheckerFieldStateTests.cs`):**

- `D131_SetAction_TargetFieldOmitInTargetState_Fires` — `[Fact]`: `in Review omit F` + `from Draft on E -> set F = 1 -> transition Review` → D131.
- `D131_SetAction_TargetFieldNotOmitInTargetState_NoDiagnostic` — `[Fact]`: field not omit in target state → no D131.
- `D131_SetAction_NoTransitionOutcome_NoDiagnostic` — `[Fact]`: `-> set F = 1 -> no transition` → no D131 (no target state to check).
- `D131_SetAction_RejectOutcome_NoDiagnostic` — `[Fact]`: `-> reject "..."` → no D131.
- `D131_SetAction_OmitInFromStateNotTargetState_NoDiagnostic` — `[Fact]`: `in Draft omit F` + `from Draft on E -> set F = 1 -> transition Review` where F is NOT omit in Review → no D131 (D131 checks target state only; from-state reads are D130's domain).
- `D131_OnEntryHook_SetOmitField_Fires` — `[Fact]`: `in Draft omit F` + `to Draft -> set F = 1` → D131.
- `D131_OnExitHook_SetOmitField_NoDiagnostic` — `[Fact]`: `in Draft omit F` + `from Draft -> set F = 1` → no D131 (target state unknown for from-hooks).
- `D131_SelfLoop_SetOmitField_Fires` — `[Fact]`: `in S omit F` + `from S on E -> set F = 1 -> transition S` → D131 (target is S, F is omit in S).
- `D131_WildcardFromState_SetOmitInTarget_Fires` — `[Fact]`: `in Review omit F` + `from any on E -> set F = 1 -> transition Review` → D131.

**Regression anchors:**

- `TypeCheckerTransitionTests` — action resolution is unchanged.
- All Slice 3 tests — D130 must still pass.

**Files:** `src/Precept/Pipeline/TypeChecker.Validation.cs`

- [ ] Add D131 transition row checks
- [ ] Add D131 state hook (on-entry) checks
- [ ] D131 tests (9 tests)
- [ ] Verify regression anchors

---

### Slice 5 — D132: RequiredFieldUnassignedOnEntry

Enforce that when a transition moves a required field from `omit` (from-state) to non-omit (to-state), the transition action chain must include a `set` for that field. This is the structural dual of `InitialEventMissingAssignments` (D94).

**Modify:**

- **`ValidateFieldStateGuarantees`** (TypeChecker.Validation.cs) — add D132 checks:

  For each `TypedTransitionRow` where `Outcome == Transition` and `TargetState != null`:

  1. Determine the effective from-states:
     - If `FromState != null`: single from-state.
     - If `FromState == null` (wildcard): iterate all declared states.

  2. For each effective from-state, for each field in `ctx.Fields`:
     - **Skip if exempt:** `field.IsOptional`, `field.DefaultExpression != null`, `field.IsComputed`, or `field.IsCollection` (collection types — `set`, `list`, `queue`, `bag`, `log` — have an intrinsic empty value) — any of these exempts the field.
     - **Check omit crossing:** `ctx.OmitLookup.Contains((fromState, field.Name))` AND NOT `ctx.OmitLookup.Contains((row.TargetState, field.Name))` — field goes from omit to non-omit.
     - **Check action chain:** `row.Actions.Any(a => string.Equals(a.FieldName, field.Name, StringComparison.Ordinal) && IsSetAction(a.Kind))` — if no set action targets this field, emit D132.
     - **Emit:** D132 with `{0} = field.Name`, `{1} = fromState`, `{2} = row.TargetState`.

  3. Helper: `IsSetAction(ActionKind kind)` — returns true for `ActionKind.Set` and any other action kind that assigns a value to the field (`Add` through the `TypedInputAction` path does not count — it adds to a collection, not replaces the field value). Only `Set` qualifies.

  For state hooks: D132 does NOT apply to state hooks. State hooks fire within a single state's context; they do not represent a state crossing. The from→to crossing is a property of transition rows.

**Tests (in `test/Precept.Tests/TypeChecker/TypeCheckerFieldStateTests.cs`):**

*Core D132:*
- `D132_OmitToNonOmit_RequiredField_NoSet_Fires` — `[Fact]`: `field F as integer` + `in Draft omit F` + `from Draft on E -> transition Review` → D132 (F is required, no default, not optional, omit in Draft, not omit in Review, no set).
- `D132_OmitToNonOmit_RequiredField_WithSet_NoDiagnostic` — `[Fact]`: same as above but transition has `-> set F = 1` → no D132.
- `D132_OmitToNonOmit_OptionalField_NoDiagnostic` — `[Fact]`: `field F as integer optional` + `in Draft omit F` + transition → no D132 (optional exemption).
- `D132_OmitToNonOmit_DefaultField_NoDiagnostic` — `[Fact]`: `field F as integer default 0` + `in Draft omit F` + transition → no D132 (default exemption).
- `D132_OmitToNonOmit_ComputedField_NoDiagnostic` — `[Fact]`: `field F as integer <- G + 1` + `in Draft omit F` + transition → no D132 (computed exemption).
- `D132_BothStatesOmit_NoDiagnostic` — `[Fact]`: `in Draft omit F` + `in Review omit F` + `from Draft on E -> transition Review` → no D132 (field stays omit).
- `D132_NeitherStateOmit_NoDiagnostic` — `[Fact]`: field not omit in either state → no D132.

*Wildcard from-state — D132:*
- `D132_WildcardFromState_OmitInOneState_Fires` — `[Fact]`: `in Draft omit F` + `from any on E -> transition Review` → D132 for the Draft→Review crossing.
- `D132_WildcardFromState_OmitInAllStates_NoDiagnostic` — `[Fact]`: `in any omit F` → no D132 (field is omit everywhere, so every crossing stays omit).

*Self-loop — D132:*
- `D132_SelfLoop_OmitInState_NoDiagnostic` — `[Fact]`: `in S omit F` + `from S on E -> transition S` → no D132 (field is omit in both from and to states).

*Form 2 inapplicability — D132:*
- `D132_NoPreconditionForFormTwoPrecept` — `[Fact]`: precept with no initial event and all fields having defaults → verify D132 is structurally unsatisfiable (no required fields without defaults exist).

*IsSetAction soundness — D132:*
- `D132_NonSetAction_DoesNotSatisfyAssignment_Fires` — `[Fact]`: `field F as integer` + `in Draft omit F` + `from Draft on E -> add F 1 -> transition Review` → D132 fires. An `add` action does not satisfy the D132 set-requirement; only a `set` action does.

*Form 3 inapplicability — D132:*
- `D132_StatelessPrecept_Inapplicable_NoDiagnostic` — `[Fact]`: stateless precept (Form 3, no declared states) with a required field and a transition → verify D132 does not fire. The omit lookup has no state entries, so no omit→non-omit crossing can exist.

*Collection exemption — D132:*
- `D132_CollectionField_OmitToNonOmit_NoDiagnostic` — `[Fact]`: `field Items as set of string` + `in Draft omit Items` + `from Draft on E -> transition Review` → no D132. Collection types have an intrinsic empty value and are exempt.
- `D132_ListField_OmitToNonOmit_NoDiagnostic` — `[Fact]`: `field Entries as list of string` + `in Draft omit Entries` + `from Draft on E -> transition Review` → no D132. Validates the exemption covers all collection container types.

**Regression anchors:**

- All Slice 3 tests (D130) and Slice 4 tests (D131) — must pass unchanged.
- `TypeCheckerTransitionTests` — transition resolution unaffected.
- `WritableSurfaceTests` — writable surface tests unaffected.

**Files:** `src/Precept/Pipeline/TypeChecker.Validation.cs`

- [ ] Add D132 transition row checks
- [ ] Add `IsSetAction` helper
- [ ] D132 core tests (7 tests)
- [ ] D132 wildcard tests (2 tests)
- [ ] D132 self-loop test (1 test)
- [ ] D132 Form 2 inapplicability test (1 test)
- [ ] D132 IsSetAction soundness test (1 test)
- [ ] D132 Form 3 inapplicability test (1 test)
- [ ] D132 collection exemption tests (2 tests)
- [ ] Verify regression anchors

---

### Slice 6 — Sample Corrections

With D130, D131, and D132 now enforced, existing sample files may emit new diagnostics. Fix violations that represent modeling anti-patterns; leave intentional violations that demonstrate correct D132 behavior.

**Modify:**

- **`insurance-claim.precept`** (samples/insurance-claim.precept):
  - **Remove line 43:** `-> set ApprovedAmount = '0.00 USD'`. This is a D131 violation (ApprovedAmount is omit in Draft, and the transition targets Submitted where it's NOT omit — wait, this is actually a D130 issue: the set happens from Draft where ApprovedAmount is omit. But actually, D131 checks the TARGET state. ApprovedAmount is NOT omit in Submitted, so D131 doesn't fire here. However, `set ApprovedAmount = '0.00 USD'` is writing a value to a field that already has `default '0.00 USD'` — it's semantically redundant. The real issue is D130: the RHS `'0.00 USD'` is a literal, so no field ref is read. And D131: the target is Submitted, where ApprovedAmount is NOT omit. So actually neither D130 nor D131 fires here as written!)

    Wait — let me re-analyze. `from Draft on Submit -> set ApprovedAmount = '0.00 USD' -> transition Submitted`:
    - D130: the RHS is a literal `'0.00 USD'`, not a field reference. No field read. D130 doesn't fire.
    - D131: ApprovedAmount is omit in Draft, but D131 checks the TARGET state (Submitted). ApprovedAmount is NOT omit in Submitted. D131 doesn't fire.
    - So this line survives D130/D131. It's semantically redundant (field has default '0.00 USD') but not a compile error.
    - Remove it anyway because it's a modeling anti-pattern (sentinel default), and the v1 analysis identified it as the trigger case.

    Actually, re-reading the v3 design §9 and v1 findings: the line IS a modeling anti-pattern. The field has `default '0.00 USD'`, and this set is redundant. But it's not a D130/D131/D132 violation with the current omit declarations (omit only in Draft, not in Submitted).

    The correct action: remove line 43 because it's a redundant sentinel set (the default achieves the same result). This is a code quality improvement, not a diagnostic fix. Include a comment noting the removal rationale.

  - **Review for D132 violations:** Check whether any transition from a state where a field IS omit to a state where it is NOT omit violates D132. With the current `insurance-claim.precept`:
    - `in Draft omit ApprovedAmount, AdjusterName, DecisionNote, MissingDocuments` (line 26)
    - `from Draft on Submit -> transition Submitted` (lines 42-44, 45-46, 47-48): ApprovedAmount has `default '0.00 USD'` (line 10) → D132 exempt (default). AdjusterName is `optional` (line 11) → exempt. DecisionNote is `optional` (line 12) → exempt. MissingDocuments is `set of string` → exempt (collection type with intrinsic empty value).
    - **No D132 violations in insurance-claim.precept** — all omit→non-omit fields are either optional or have defaults.

- **Survey other samples for sentinel-default patterns.** Scan `samples/` for `default 0`, `default false`, `default ""` on fields that could use `omit` instead. Document findings but do NOT convert — sentinel-to-omit migration is a separate design decision. The survey informs future guidance.

**Tests:**

- `Samples_InsuranceClaim_CompilesClean_AfterSentinelRemoval` — `[Fact]`: compile `samples/insurance-claim.precept` after line 43 removal; assert zero D130/D131/D132 diagnostics.
- `Samples_AllSampleFiles_NoUnexpected_D130_D131_D132` — `[Theory, MemberData(nameof(AllSampleFilePaths))]`: compile each `.precept` file under `samples/`; assert no D130, D131, or D132 diagnostics fire. This is the named regression sweep harness Soup Nazi identified — documents sample compliance as a named, executable test rather than an ad hoc verification bullet.

**Files:** `samples/insurance-claim.precept`

- [ ] Remove line 43 from insurance-claim.precept
- [ ] `Samples_InsuranceClaim_CompilesClean_AfterSentinelRemoval` test (1 test)
- [ ] `Samples_AllSampleFiles_NoUnexpected_D130_D131_D132` theory (1 theory)
- [ ] Document sentinel-default survey results (in commit message or PR body, not in a new file)

---

### Slice 7 — Spec and Documentation Updates

Annotate the language spec with the field-state enforcement rules and close the identified spec gaps.

**Modify:**

- **`precept-language-spec.md`** (docs/language/precept-language-spec.md):

  - **§2.2 Rule #5 annotation** — after the existing rule #5 text ("field value resets to default on any transition into an `omit` state"), add:
    > When a transition moves a required field (non-optional, no default, not computed) from `omit` in the from-state to non-omit in the to-state, the transition action must include a `set` for that field — `RequiredFieldUnassignedOnEntry` (D132). This is the structural dual of `InitialEventMissingAssignments` (D94) applied to state transitions.

  - **§2.2 Rule #6 annotation** — after the existing rule #6 text ("set targeting an omit field in the target state is a compile error"), add:
    > Enforced as `OmittedFieldSetInTargetState` (D131). Additionally, reading a field that is `omit` in a state-anchored expression context (transition row guard, `in`-state ensure, `from`-state ensure, state action guard, or action RHS) is a compile error — `OmittedFieldReadInState` (D130).

  - **§3.5 scope annotation** — in the "All field names" scope description, add:
    > Field names that resolve to fields `omit` in the anchoring state are in scope for resolution but produce `OmittedFieldReadInState` (D130) — reading a structurally absent field is a compile error.

- **`docs/tooling/mcp.md`** — verify whether diagnostic codes are listed. If so, add D130/D131/D132 to the list. If not (MCP output auto-formats all diagnostics), no changes needed.

- **`README.md`** — scan for enforcement claims that should reference field-state guarantees. If the README mentions "invalid configurations are structurally impossible," a brief note about D130/D131/D132 may be appropriate in the same section.

**Files:** `docs/language/precept-language-spec.md`, `docs/tooling/mcp.md` (if needed), `README.md` (if needed)

- [ ] Annotate §2.2 rule #5 with D132 leaving-omit rule
- [ ] Annotate §2.2 rule #6 with D130/D131 enforcement
- [ ] Annotate §3.5 with omit-qualified scope note
- [ ] Review mcp.md for diagnostic code listings
- [ ] Review README.md for enforcement claims

---

### Slice 8 — MCP + Language Server Sync Assessment

**MCP — No changes needed.**

New `DiagnosticCode` enum values are automatically surfaced in MCP output:
- `CompileTool.FormatDiagnosticCode` (tools/Precept.Mcp/Tools/CompileTool.cs:59–64) uses `Enum.TryParse<DiagnosticCode>` and formats as `PRE{code:D4}`. D130/D131/D132 → `PRE0130`, `PRE0131`, `PRE0132` automatically.
- `CompileTool.MapDiagnostic` (line 26–32) passes `diagnostic.Message` through unchanged — the message text from `Diagnostics.GetMeta` is used directly.
- `LanguageTool.cs` (the `precept_language` vocabulary tool) does not maintain a static list of diagnostic codes — it reads from the `DiagnosticsCatalog` dynamically. D130/D131/D132 will appear in vocabulary output automatically.
- No DTO changes needed. `CompileDiagnosticDto` fields (line, column, severity, code, message) are sufficient.

**Language Server — No changes needed.**

New `DiagnosticCode` values are automatically surfaced as LSP diagnostics:
- `DiagnosticProjector.cs` (tools/Precept.LanguageServer/DiagnosticProjector.cs:17–24) maps all `compilation.Diagnostics` to LSP `Diagnostic` objects without filtering by code.
- `DiagnosticEnricher.cs` (tools/Precept.LanguageServer/DiagnosticEnricher.cs) uses `Enum.TryParse(diagnostic.Code, out code)` followed by `Diagnostics.GetMeta(code)` to enrich diagnostics with suggestions. New codes are automatically parsed and enriched.
- No severity override table exists — severity comes from `DiagnosticMeta.Severity` in the centralized registry.
- `SuggestionSources` in `DiagnosticMeta` can provide "did you mean?" suggestions if configured; for D130/D131/D132, no `SuggestionSources` are needed initially (the `FixHint` and `RecoverySteps` are sufficient).

**Verification:**

- Compile a `.precept` file with a D130 violation via `precept_compile` MCP tool → verify output includes `PRE0130` with correct message.
- Open a `.precept` file with a D131 violation in VS Code → verify the Problems panel shows the diagnostic.

- [ ] Verify D130/D131/D132 appear in MCP `precept_compile` output
- [ ] Verify D130/D131/D132 appear in VS Code Problems panel
- [ ] Confirm no DTO or registration changes needed

---

### Slice 9 — OR / ProofEngine Disjunction Support

This is a standalone correctness bugfix slice. It is appended after the field-state slices only to avoid renumbering the approved v3 plan; it does **not** depend on D130/D131/D132 enforcement. Parser and TypeChecker already accept `or`; the live gap is branch-aware proof obligation discharge plus ensure pre-guard normalization.

**OR-Splitting Soundness Semantics (Formal Statement):**

When a guard expression contains `A or B`, proof of obligation X succeeds **only if X holds under each OR branch independently**. The algorithm: extract constraints from branch A; attempt to prove X against A's constraints alone; extract constraints from branch B; attempt to prove X against B's constraints alone. Only if **both** branches independently prove X is the obligation discharged. This is sound because at runtime either branch could be the one that holds — the proof must cover all possibilities. A naive "prove X under ANY branch" approach would be unsound: `D > 0 or Z != 0` would incorrectly discharge a `D != 0` obligation by succeeding on branch A while ignoring branch B.

For three-way or deeper disjunctions (`A or B or C`), the same rule applies recursively: ALL branches must independently prove X.

**Modify:**

- **`TryGuardInPathProof` / `ExtractGuardConstraints`** (`src/Precept/Pipeline/ProofEngine.Strategies.cs:271–380`) — replace the current flat guard-constraint extraction with branch-aware extraction.
  - `and` keeps facts in the same branch fact set.
  - `or` splits into independent branch fact sets and evaluates the obligation against each branch independently per the soundness semantics above, instead of dropping the OR node.
  - Regression target: `D > 0 or D < 0` must discharge a `D != 0` obligation; `D > 0 or Z != 0` must remain unresolved.

- **`TryFlowNarrowingProof` / `ExtractFieldToFieldConstraints`** (`src/Precept/Pipeline/ProofEngine.Strategies.cs:442–504`) — apply the same branch-aware extraction to field-vs-field narrowing facts. Recursive branch splitting must handle three-way disjunctions without regressing the existing non-disjunctive narrowing path. The same ALL-branches-must-prove rule applies.

- **`TryGetNumericEnsureFact` / `TryGetNumericConstraintFact`** (`src/Precept/Pipeline/ProofEngine.Composition.cs:163–203`) — guarded ensures must not become unconditional numeric facts once `TypedEnsure.Guard` is preserved. Either thread the guard into branch-aware fact extraction or refuse to emit unconditional facts from guarded ensures in this slice. The current `Guard: null` normalization masks this soundness hole.

- **`PopulateEnsures`** (`src/Precept/Pipeline/TypeChecker.cs:767–871`) — stop discarding the parsed `GuardClauseSlot` on `ConstructKind.StateEnsure` and `ConstructKind.EventEnsure`. For each ensure construct, extract the `GuardClauseSlot`, resolve the guard expression via `Resolve(guardSlot.Expression, ctx)`, and type-validate that the result is `TypeKind.Boolean` (emitting `TypeMismatch` and replacing with `TypedErrorExpression` on failure) — following the exact pattern established by `PopulateAccessModes` (TypeChecker.cs:979–992). Preserve the resolved guard in `TypedEnsure.Guard`. Set `ctx.CurrentScope = FieldScopeMode.AllFields` before resolution. If later proof consumers need per-branch lowering, do it explicitly; do not silently null it out.

- **`ProofEngine.Analysis.cs` is not the change site for this bug.** `CheckInitialStateSatisfiability` already observes `ensure.Guard` and skips guarded ensures, and boolean OR constant-folding already exists there. The gap is branch-aware fact extraction plus ensure normalization, not boolean folding.

**Tests (in `test/Precept.Tests/ProofEngineTests.cs`):**

- `ProofEngine_DischargesObligation_WhenDisjunctiveGuardCoversAllCases` — verifies `D > 0 or D < 0` suppresses `DivisionByZero`.
- `ProofEngine_DoesNotDischarge_WhenDisjunctiveGuardIsPartial` — verifies a partial disjunction does not false-suppress.
- `ProofEngine_DischargesObligation_WhenThreeWayDisjunction` — verifies recursive OR splitting handles three branches.
- `ProofEngine_FlowNarrowing_Discharges_WhenDisjunctiveGuardCoversAllBranches` — `[Fact]`: field-vs-field constraint where `X > 0 or X < 0` is the transition guard; verifies `TryFlowNarrowingProof` extracts branch-aware field-to-field constraints and discharges the obligation. (Distinct from GuardInPath: this exercises the field-narrowing path, not the literal-path.)
- `ProofEngine_FlowNarrowing_DoesNotDischarge_WhenDisjunctiveGuardIsPartial` — `[Fact]`: partial disjunction in a field-vs-field narrowing context does not false-suppress. Ensures the branch-split logic in `TryFlowNarrowingProof` correctly rejects incomplete coverage.
- `ProofEngine_GuardedEnsure_DoesNotBecomeUnconditionalFact` — `[Fact]`: `when D > 0 ensure result >= 0` with guard preserved in `TypedEnsure.Guard`; verify that `TryGetNumericEnsureFact` does NOT emit this as an unconditional numeric fact (i.e., the obligation is only dischargeable on the guarded branch, not globally).

**Tests (in `test/Precept.Tests/TypeChecker/TypeCheckerAssemblyTests.cs`):**

- `EnsureNormalizer_PreservesOrGuard_WhenUsedWithEnsure` — verifies `when D > 0 or D < 0 ensure ...` preserves a non-null `TypedEnsure.Guard` instead of silently dropping it.
- `EnsureNormalizer_PreservesGuard_ForEventEnsure` — `[Fact]`: `when D > 0 ensure result >= 0` on an event ensure (not state ensure); verify that `PopulateEnsures` preserves the `GuardClauseSlot` on `ConstructKind.EventEnsure` as well as `ConstructKind.StateEnsure`. Both paths are being changed — both paths need coverage.
- `EnsureNormalizer_NonBooleanGuard_EmitsTypeMismatch` — `[Fact]`: `when Amount ensure result >= 0` where `Amount` is `money`/`integer` (non-boolean guard); verify `PopulateEnsures` emits `TypeMismatch` diagnostic and sets `TypedEnsure.Guard` to `TypedErrorExpression`. Validates the type-check step follows the `PopulateAccessModes` pattern.

**Additional Finding — Ensure when Guards Are Dropped Entirely:**

When a `when {condition} ensure {constraint}` block has its guard removed entirely (leaving `ensure {constraint}` with no `when` clause, i.e. `Guard: null`), the proof engine and ensure normalizer must handle the null-guard case gracefully — no crash, no false discharge. This is the structural dual of the guard-preservation bug: if preserving a guard incorrectly makes a guarded fact unconditional, then an absent guard should produce an unconditional fact correctly. The risk is that code paths added for guard-preservation silently break the null-guard baseline.

**Tests (null-guard / guard-dropped baseline):**

- `EnsureNormalizer_NoGuard_ProducesUnconditionalFact` — `[Fact]`: `ensure D >= 0` (no `when` clause) → `TypedEnsure.Guard == null` → fact treated as unconditional; ProofEngine discharges a `D >= 0` obligation.
- `ProofEngine_DoesNotCrash_WhenEnsureHasNullGuard` — `[Fact]`: definition with a null-guard ensure processed through the full pipeline → no exception thrown.
- `ProofEngine_DoesNotCrash_WhenTransitionGuardAbsent` — `[Fact]`: transition row with no guard expression (unconditional transition) processed through branch-aware extraction → no exception from null dereference in the new OR-splitting path.

These tests must remain green throughout Slice 9 implementation. Any null-guard crash introduced by the branch-awareness refactor is a regression.

**Regression anchors:**

- `ProofEngineTests.Slice5_GuardInPathProof.Strategy3_OrGuard_DoesNotDischarge` — rewrite this obsolete expectation; it currently documents the live bug.
- `ProofEngineTests.Slice5_GuardInPathProof.Strategy3_AndGuard_DecomposesConjuncts` — conjunction behavior must remain unchanged.
- `ProofEngineTests.Slice6_FlowNarrowing.Strategy4_GuardImpliesSubtractionNonNegative_FlowNarrowingProves` — the existing narrowing happy path must remain unchanged.
- `TypeCheckerAssemblyTests.StateEnsure_MultiStateList_ExpandsIntoIndependentEnsures` — ensure expansion must still work once guards are preserved.

**MCP + Language Server sync assessment:** No changes needed. Proof discharge changes do not alter MCP DTO shapes, and diagnostic surfacing remains automatic through the existing enum/catalog-based projection path.

**Files:** `src/Precept/Pipeline/ProofEngine.Strategies.cs`, `src/Precept/Pipeline/ProofEngine.Composition.cs`, `src/Precept/Pipeline/TypeChecker.cs`, `test/Precept.Tests/ProofEngineTests.cs`, `test/Precept.Tests/TypeChecker/TypeCheckerAssemblyTests.cs`

- [ ] Make `TryGuardInPathProof` branch-aware for OR
- [ ] Make `TryFlowNarrowingProof` branch-aware for OR
- [ ] Prevent guarded ensures from becoming unconditional numeric facts
- [ ] Preserve `GuardClauseSlot` in `PopulateEnsures` with full expression resolution + boolean type validation
- [ ] ProofEngine GuardInPath OR tests (3 tests)
- [ ] ProofEngine FlowNarrowing OR tests (2 tests)
- [ ] ProofEngine guarded-ensure unconditional-fact test (1 test)
- [ ] Ensure normalizer state-ensure guard test (1 test)
- [ ] Ensure normalizer event-ensure guard test (1 test)
- [ ] Ensure normalizer non-boolean guard type-mismatch test (1 test)
- [ ] Null-guard baseline tests (3 tests — guards-dropped-entirely finding)
- [ ] Verify regression anchors

---

### Slice 10 — D93: RequiredFieldsNeedInitialEvent Enforcement

**Gap identified by:** v3 gap audit (2026-05-12). D93 was declared in `DiagnosticCode.cs` and `Diagnostics.cs` but never emitted by any pipeline stage. The v3 design's §7 Form 2 analysis assumes D93 is enforced — without it, Form 2 precepts with required fields compile clean when they should fail.

**Spec grounding:** §3A.5: "If the precept does not declare an initial event, `Create()` is parameterless and always succeeds (the compiler guarantees all fields have defaults or are optional — enforced by `RequiredFieldsNeedInitialEvent`)."

**Scope:** Stateful precepts (at least one `state` declaration) that do NOT declare an initial event. If any field is non-optional, non-computed, has no default value, and is not a collection type, the definition must be rejected.

Stateless precepts (no `state` declarations) with no initial event follow the same rule per §3A.5: "All other steps apply unchanged." D93 applies to stateless precepts that have required fields and no initial event.

**Trigger conditions:**

D93 fires when ALL of the following are true:
1. The precept has no event with `IsInitial == true`.
2. At least one field exists that is:
   - NOT `optional`
   - NOT computed (`ComputedExpression == null`)
   - Has no `DefaultExpression`
   - Is NOT a collection type (`set`, `list`, `queue`, `bag`, `log`, `stack`, `lookup`, `queueby`, `logby`)

**Exemptions:**
- Precepts WITH an initial event — D94 handles those, not D93.
- Fields with `optional` — unset is a valid state.
- Fields with `default` — a value is available at construction.
- Computed fields — value is derived.
- Collection-typed fields — empty collection is a valid initial value.

**Modify:**

- **`TypeChecker.Validation.cs`** — add `ValidateConstructionGuarantees` method (~35 lines):
  ```csharp
  private static void ValidateConstructionGuarantees(CheckContext ctx)
  ```
  Algorithm:
  1. Check if any event in `ctx.Events` has `IsInitial == true`. If yes, defer to D94 logic (Slice 11). If no, continue.
  2. Collect all required fields: iterate `ctx.Fields`, filter to non-optional, non-computed, no default, non-collection.
  3. If the collection is non-empty, emit D93 with `{0}` = comma-joined field names.
  4. Span: use the first field's `Span` (or the precept-level span if available).

- **`TypeChecker.cs`** (line ~63) — wire `ValidateConstructionGuarantees` after `ValidateFieldStateGuarantees`:
  ```csharp
  ValidateFieldStateGuarantees(ctx);

  // Construction-time field guarantees (D93, D94) — Slice 10-11.
  ValidateConstructionGuarantees(ctx);
  ```

**Tests (in `test/Precept.Tests/TypeChecker/`):**

New test class or section: `TypeCheckerConstructionTests.cs`

- `D93_StatefulPrecept_NoInitialEvent_RequiredField_Fires` — `[Fact]`: precept with states, no initial event, and `field Name as string` (required, no default) → D93 fires listing "Name".
- `D93_StatefulPrecept_NoInitialEvent_AllFieldsHaveDefaults_NoDiagnostic` — `[Fact]`: all fields have defaults → no D93.
- `D93_StatefulPrecept_NoInitialEvent_AllFieldsOptional_NoDiagnostic` — `[Fact]`: all fields optional → no D93.
- `D93_StatefulPrecept_NoInitialEvent_ComputedField_NoDiagnostic` — `[Fact]`: only computed fields → no D93.
- `D93_StatefulPrecept_NoInitialEvent_CollectionField_NoDiagnostic` — `[Fact]`: `field Items as set of string` (collection) → no D93.
- `D93_StatefulPrecept_WithInitialEvent_RequiredField_NoDiagnostic` — `[Fact]`: initial event declared → no D93 (D94's domain).
- `D93_StatefulPrecept_NoInitialEvent_MultipleRequiredFields_ListsAll` — `[Fact]`: two required fields → D93 message lists both.
- `D93_StatelessPrecept_NoInitialEvent_RequiredField_Fires` — `[Fact]`: stateless precept (no states) with required field and no initial event → D93 fires.
- `D93_StatelessPrecept_NoInitialEvent_AllDefaults_NoDiagnostic` — `[Fact]`: stateless precept, all fields defaulted → no D93.
- `D93_MixedFields_OnlyRequiredFieldsListed` — `[Fact]`: mix of optional, defaulted, computed, collection, and required fields → D93 lists only the required ones.

**Regression anchors:**

- All Slices 0–9 tests — D93 enforcement is additive, touches no existing validation logic.
- `TypeCheckerFieldStateTests` — D130/D131/D132 must pass unchanged.
- `DiagnosticsTests.DiagnosticMeta_AllCodesHaveEntries` — D93 already has metadata, so no change needed.

**Files:** `src/Precept/Pipeline/TypeChecker.cs`, `src/Precept/Pipeline/TypeChecker.Validation.cs`

- [x] Create `ValidateConstructionGuarantees` method
- [x] Wire into `TypeChecker.Check` pipeline
- [x] D93 tests (10 tests)
- [x] Verify regression anchors

---

### Slice 11 — D94: InitialEventMissingAssignments Enforcement

**Gap identified by:** v3 gap audit (2026-05-12). D94 was declared in `DiagnosticCode.cs` and `Diagnostics.cs` but never emitted by any pipeline stage. A Form 1 precept where the initial event doesn't assign all required fields compiles clean.

**Spec grounding:** §3A.5: "InitialEventMissingAssignments: Initial event does not assign all required fields that lack defaults — post-construction state may violate constraints."

**Scope:** Precepts that declare an initial event (Form 1). The initial event must, across the transition rows whose `from` state is initial, assign all required fields. This is the construction-time counterpart of D132 (which handles mid-lifecycle omit→non-omit crossings).

**Trigger conditions:**

D94 fires when ALL of the following are true:
1. The precept declares an initial event (`IsInitial == true`).
2. A required field exists (non-optional, non-computed, no default, non-collection).
3. Either (a) the initial event has no transition rows whose `from` state is initial, or (b) at least one such initial-state row does NOT include a `set` action for the required field.

**Semantic complexity — per-row vs. per-event analysis:**

The initial event may have multiple transition rows (guarded). D94 must fire per-row, not per-event, but only for rows that can participate in construction. If row A starts from an initial state and sets the required field but row B starts from a non-initial state and does not, D94 does not fire for row B — that row is a lifecycle path, not a construction path.

If the initial event has NO transition rows whose `from` state is initial, D94 fires for the event as a whole — there is no construction path through which required fields could be set.

**Initial event action chain analysis:**

For each transition row associated with the initial event whose `from` state is initial:
1. Identify rows by matching `row.EventName` to the initial event's name and requiring `row.FromState` to be one of the initial state names.
2. For each required field, check whether `row.Actions` contains a `set` action targeting that field (using the same `IsSetAction` helper from D132).
3. If not → emit D94 with `{0}` = event name, `{1}` = comma-joined missing field names.

**What about initial event args vs. set actions?**

The initial event may declare args that are intended to populate fields (e.g., `event Create(Name as string) initial`). However, having an arg is not the same as having a `set Name = Name` action. The transition row must explicitly set the field — the compiler does not infer field assignment from arg names. This matches D132's semantics: only explicit `set` actions count.

**Modify:**

- **`ValidateConstructionGuarantees`** (TypeChecker.Validation.cs) — extend the method from Slice 10:
  After the D93 check (no initial event), add the D94 check (initial event exists):
  1. Find the initial event: `ctx.Events.FirstOrDefault(e => e.IsInitial)`.
  2. If no initial event → D93 path (Slice 10). If initial event exists → D94 path.
  3. Collect required fields (same filter as D93).
  4. If no required fields → return (no D94 needed).
  5. Find all initial-state transition rows for the initial event: `ctx.TransitionRows.Where(r => string.Equals(r.EventName, initialEvent.Name, StringComparison.Ordinal) && r.FromState is { } fromState && initialStateNames.Contains(fromState))`.
  6. If no initial-state rows exist → emit D94 for the event as a whole.
  7. For each initial-state transition row, for each required field:
     - Check `row.Actions.Any(a => IsSetAction(a.Kind) && string.Equals(a.FieldName, field.Name, StringComparison.Ordinal))`.
     - If not → emit D94 with event name and missing field name(s).

**Tests (in `test/Precept.Tests/TypeChecker/TypeCheckerConstructionTests.cs`):**

- `D94_InitialEvent_AssignsAllRequiredFields_NoDiagnostic` — `[Fact]`: initial event with `set` for every required field → no D94.
- `D94_InitialEvent_MissesRequiredField_Fires` — `[Fact]`: initial event doesn't set a required field → D94 fires.
- `D94_InitialEvent_MissesMultipleFields_ListsAll` — `[Fact]`: two required fields unset → D94 message lists both.
- `D94_InitialEvent_OptionalField_NoDiagnostic` — `[Fact]`: optional field not set → no D94.
- `D94_InitialEvent_DefaultField_NoDiagnostic` — `[Fact]`: field with default not set → no D94.
- `D94_InitialEvent_ComputedField_NoDiagnostic` — `[Fact]`: computed field not set → no D94.
- `D94_InitialEvent_CollectionField_NoDiagnostic` — `[Fact]`: collection field not set → no D94.
- `D94_InitialEvent_MultipleRows_OneRowMissesField_Fires` — `[Fact]`: row A sets the field, row B doesn't → D94 fires for row B.
- `D94_InitialEvent_AllRowsSetField_NoDiagnostic` — `[Fact]`: all initial-state rows set the field → no D94.
- `D94_NonInitialStateRow_NotChecked` — `[Fact]`: a non-initial-state row for the initial event misses the field, but the initial-state construction row sets it → no D94.
- `D94_NoTransitionRows_InitialEvent_RequiredField_Fires` — `[Fact]`: initial event defined but no initial-state transition rows reference it → D94 fires.

**Regression anchors:**

- All Slice 10 tests — D93 must still pass.
- All Slices 0–9 tests — D94 enforcement is additive.
- `TypeCheckerFieldStateTests` — D130/D131/D132 must pass unchanged.

**Dependencies:** Slice 10 (D93) must be implemented first — Slice 11 extends `ValidateConstructionGuarantees`.

**Files:** `src/Precept/Pipeline/TypeChecker.Validation.cs`

- [x] Extend `ValidateConstructionGuarantees` with D94 logic
- [x] D94 tests (11 tests)
- [x] Verify regression anchors
- Bug fix: commit `4c567cdc` scoped D94 row analysis to initial-state `from` rows only, fixing the false positive on `samples/Test.precept`.

---

### File Inventory

| File | Change | Slices | Description |
|------|--------|--------|-------------|
| `src/Precept/Pipeline/SlotValue.cs` | Modify | 0 | Add `AdditionalFields` property to `FieldTargetSlot` |
| `src/Precept/Pipeline/Parser.cs` | Modify | 0 | Collect fields 2–N in `ParseFieldTarget` |
| `src/Precept/Pipeline/TypeChecker.cs` | Modify | 0, 2, 9 | Update `PopulateAccessModes`, `PopulateEditDeclarations`, and `PopulateEnsures`; wire `BuildOmitLookup` and `ValidateFieldStateGuarantees` into pipeline |
| `src/Precept/Pipeline/NameBinder.cs` | Modify | 0 | Iterate `AdditionalFields` for reference resolution |
| `src/Precept/Language/DiagnosticCode.cs` | Modify | 1 | Add D130, D131, D132 enum values |
| `src/Precept/Language/Diagnostics.cs` | Modify | 1 | Add `DiagnosticMeta` entries for D130, D131, D132 |
| `src/Precept/Pipeline/CheckContext.cs` | Modify | 2 | Add `OmitLookup` property |
| `src/Precept/Pipeline/TypeChecker.Validation.cs` | Modify | 2, 3, 4, 5 | `BuildOmitLookup`, `ValidateFieldStateGuarantees`, `CollectFieldRefsFromExpression`, D130/D131/D132 enforcement |
| `src/Precept/Pipeline/ProofEngine.Strategies.cs` | Modify | 9 | Branch-aware OR handling for `TryGuardInPathProof` and `TryFlowNarrowingProof` |
| `src/Precept/Pipeline/ProofEngine.Composition.cs` | Modify | 9 | Keep preserved ensure guards from becoming unconditional numeric facts |
| `samples/insurance-claim.precept` | Modify | 6 | Remove redundant sentinel set (line 43) |
| `docs/language/precept-language-spec.md` | Modify | 7 | §2.2 rule #5/#6 annotations, §3.5 scope annotation |
| `docs/tooling/mcp.md` | Review | 7 | Verify if diagnostic code listing needs update |
| `README.md` | Review | 7 | Verify if enforcement claims need update |
| `test/Precept.Tests/TypeChecker/TypeCheckerFieldStateTests.cs` | New | 3, 4, 5 | All D130/D131/D132 enforcement tests |
| `test/Precept.Tests/ProofEngineTests.cs` | Modify | 9 | Disjunctive guard / narrowing proof regressions |
| `test/Precept.Tests/TypeChecker/TypeCheckerAssemblyTests.cs` | Modify | 9 | Ensure normalizer guard-preservation regression |
| `test/Precept.Tests/Parser/` (existing files) | Modify | 0 | Multi-field parser tests |
| `test/Precept.Tests/TypeChecker/` (existing files) | Modify | 0, 2 | Omit lookup and multi-field consumer tests |
| `test/Precept.Tests/DiagnosticsTests.cs` | Modify | 1 | D130/D131/D132 metadata tests |
| `test/Precept.Tests/TypeChecker/TypeCheckerConstructionTests.cs` | New | 10, 11 | D93/D94 construction enforcement tests |

**Total estimated tests:** ~93 new tests across all slices (73 original + 20 from Slices 10–11).
**Regression anchors:** ~21 named existing test families.

---

## 11. Status

**Design Approved — Implementation Complete**

Gap audit (2026-05-12) identified two blocking gaps: D93 and D94 were declared but never enforced. Slices 10–11 closed that remediation work. Slices 0–11 are complete.

---

## 12. Implementation Tracker

**Progress:** 12 / 12 slices complete

| Slice | Name | Status | Depends On | Commit |
|---|---|---|---|---|
| Slice 0 | Parser Fix: `FieldTargetSlot` Multi-Field Broadcast | ✅ Done | — | `b7020917` |
| Slice 1 | Diagnostic Infrastructure: D130, D131, D132 | ✅ Done | — | `99f7a693` |
| Slice 2 | Omit Lookup Construction | ✅ Done | Slice 0 | `bedb9dd6` |
| Slice 3 | D130: `OmittedFieldReadInState` | ✅ Done | Slices 1, 2 | `18415226` |
| Slice 4 | D131: `OmittedFieldSetInTargetState` | ✅ Done | Slices 1, 2 | `eaa3b45e` |
| Slice 5 | D132: `RequiredFieldUnassignedOnEntry` | ✅ Done | Slices 1, 2 | `dea339d8` |
| Slice 6 | Sample Corrections | ✅ Done | Slices 3, 4, 5 | `6d5d464b` |
| Slice 7 | Spec and Documentation Updates | ✅ Done | Slice 6 | `40bcd746` |
| Slice 8 | MCP + Language Server Sync Assessment | ✅ Done | Independent | `12449503` |
| Slice 9 | OR / ProofEngine Disjunction Support + guards-dropped-entirely | ✅ Done | Standalone | `c2d5b8fb` |
| Slice 10 | D93: `RequiredFieldsNeedInitialEvent` enforcement | ✅ Done | Independent (additive) | `597a0479` |
| Slice 11 | D94: `InitialEventMissingAssignments` enforcement | ✅ Done | Slice 10 | `0b42fd1a` |
