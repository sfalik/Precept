# Field State Guarantees — Compile-Time Enforcement Analysis

**Author:** Frank (Lead/Architect & Language Designer)
**Date:** 2026-05-12T14:57:13.598-04:00
**Status:** Second revision — post design review

---

## Executive Summary

The Precept compiler does not enforce field-state access mode constraints during transition action resolution, guard expression resolution, or state-hook action resolution. `ResolveAction` and `Resolve` are completely stateless with respect to the source state's omit/readonly declarations — they validate types and qualifiers but never consult the `TypedAccessMode` records. This means a transition `from Draft on Submit -> set ApprovedAmount = '0.00 USD'` compiles silently even when `in Draft omit ApprovedAmount` declares the field structurally absent. The fix requires threading `fromState` context into action and expression resolution during `NormalizeTransitionRow`, then consulting the already-populated `AccessModes` list (or a precomputed lookup) to emit new diagnostic errors.

## Confirmed Trigger

**File:** `samples/insurance-claim.precept`
**Line 26:** `in Draft omit ApprovedAmount, AdjusterName, DecisionNote, MissingDocuments`
**Line 42–44:**
```
from Draft on Submit when ClaimantName is set and ClaimAmount > '0.00 USD' and (not PoliceReportRequired or ClaimAmount <= '100000.00 USD')
    -> set ApprovedAmount = '0.00 USD'
    -> transition Submitted
```

`ApprovedAmount` is declared structurally absent in the `Draft` state via the `omit` access mode on line 26. The transition originating from `Draft` on line 42 writes to `ApprovedAmount` on line 43. The compiler emits no diagnostic. This is a structural soundness violation — the field does not exist in the source state, so writing to it during a transition from that state is semantically invalid.

**Semantic note:** The `set ApprovedAmount = '0.00 USD'` on line 43 is also logically unnecessary — `ApprovedAmount` already has `default '0.00 USD'` on line 10, and the field will materialize with that default when the entity transitions to `Submitted` (where it is not omitted). The action is both illegal (writes to an absent field) and redundant (the default achieves the same result). The correct fix for the sample is to remove line 43 entirely.

---

## Implementation Tracker

### Design Review

| Review | Status |
|--------|--------|
| Frank review | ✅ RESOLVED |
| Soup Nazi test audit | ✅ RESOLVED |
| Shane sign-off | ⏳ Pending |

| Step | Description | Status | Notes |
|------|-------------|--------|-------|
| Step 0a | Spec §2.2 update (source-state omit rule + broaden rule 6 wording) | ⏳ Pending | Required doc/spec alignment before implementation starts. |
| Step 0b | Route `OmitDeclaration` into `AccessModes` (`PopulateAccessModes`) | ⏳ Pending | Needed so omit declarations participate in enforcement. |
| Step 0c | Fix `ParseFieldTarget` multi-field parser bug | ⏳ Pending | Unblocks multi-field omit enforcement correctness. |
| Step 1 | Build the access mode lookup (`BuildFieldAccessLookup`) | ⏳ Pending | Depends on Step 0b. |
| Step 2 | Add `ValidateFieldStateAccess` pass | ⏳ Pending | Core enforcement slice; sample update in Step 4 must land in the same commit. |
| Step 3 | Multi-field omit handling verification | ⏳ Pending | Expected to be satisfied by Step 0c; verify rather than re-implement. |
| Step 4 | Update `samples/insurance-claim.precept` (remove line 43) | ⏳ Pending | Must land in the same commit as Step 2. |
| Step 5 | Add `DiagnosticMeta` entries (`D128`–`D134`) | ⏳ Pending | Add metadata once enforcement diagnostics are wired. |
| Step 6 | Test plan (31 test cases) | ⏳ Pending | Includes prior audit gaps now captured in plan. |

## Findings Matrix

| Category | TypeChecker | GraphAnalyzer | ProofEngine | Status |
|----------|-------------|---------------|-------------|--------|
| Omit field in transition action (fromState) | NOT ENFORCED | NOT ENFORCED | NOT ENFORCED | **GAP** |
| Omit field in transition guard (fromState) | NOT ENFORCED | NOT ENFORCED | NOT ENFORCED | **GAP** |
| Omit field in action RHS expression (fromState) | NOT ENFORCED | NOT ENFORCED | NOT ENFORCED | **GAP** |
| ReadOnly field in transition action | NOT ENFORCED | NOT ENFORCED | NOT ENFORCED | **GAP** |
| ReadOnly field in event handler action | N/A (stateless) | N/A | N/A | N/A — event handlers are stateless |
| Omit field in state-hook action (to/from scope) | NOT ENFORCED | NOT ENFORCED | NOT ENFORCED | **GAP** |
| ReadOnly field in state-hook action (in scope) | NOT ENFORCED | NOT ENFORCED | NOT ENFORCED | **GAP** |
| Wildcard access mode expansion in enforcement | NOT ENFORCED | NOT ENFORCED | NOT ENFORCED | **GAP** |
| Set targeting omit field in target state | NOT ENFORCED | NOT ENFORCED | NOT ENFORCED | **GAP** |

**Legend:**
- **NOT ENFORCED:** No code path in this pipeline stage checks this constraint. Confirmed by code reading.
- **N/A:** The construct is not state-scoped, so field-state constraints do not apply.

### Category Details

**Category 1 — Omit field in transition action (fromState):**
`ResolveAction` (TypeChecker.Expressions.Callables.cs:102) calls `ResolveActionTarget` (line 314) which looks up the field in `ctx.FieldLookup` — a global, state-agnostic dictionary. Neither method receives `fromState` or consults `ctx.AccessModes`. All action variants (`set`, `add`, `remove`, `clear`, `insert`, `put`, `removeAt`) pass through the same state-blind path.

**Category 2 — Omit field in transition guard (fromState):**
`NormalizeTransitionRow` (TypeChecker.cs:1084) resolves guards at line 1144 via `Resolve(guardSlot.Expression, ctx)`. The `ctx.CurrentScope` is set to `FieldScopeMode.AllFields` (line 1143), which gates only declaration-order restrictions. There is no `CurrentFromState` property on `CheckContext` — the `fromState` is resolved (line 1097) and stored as a local variable, but never propagated into the expression resolution context. An omitted field referenced in a guard from that state compiles without error.

**Category 3 — Omit field in action RHS expression (fromState):**
The RHS of `set Field = <expression>` is resolved via `Resolve(assign.Value, ctx, ...)` at TypeChecker.Expressions.Callables.cs:122. Same problem as Category 2 — no state context flows into expression resolution. If the RHS references an omitted field, no diagnostic fires.

**Category 4 — ReadOnly field in transition action:**
`ModifierKind.Read` (value 25) is the read-only access mode. `TypedAccessMode` records are populated in `PopulateAccessModes` (TypeChecker.cs:896) and stored in `ctx.AccessModes`. But `ResolveAction` never consults this list. A `set` action targeting a field with `in State modify Field readonly` from that state compiles silently.

**Category 5 — Event handler actions (stateless):**
Event handlers (`on Event -> action`) are stateless — they fire regardless of current state (TypeChecker.cs:1221). Field-state constraints do not apply here. This is correct behavior, not a gap.

**Category 6 — Omit field in state-hook action (to/from scope):**
State hooks (`to State -> set Field = ...` / `from State -> set Field = ...`) execute during state entry/exit. `PopulateStateHooks` (TypeChecker.cs:984) resolves actions at line 1029 via the same state-blind `ResolveAction`. For `to` hooks, the target state's omit constraints should apply. For `from` hooks, the source state's constraints should apply.

**Category 7 — ReadOnly field in state-hook action (in scope):**
`in State` hooks (if they exist or when the scope model expands) should enforce the state's read-only constraints on any actions. Currently not enforced.

**Category 8 — Wildcard access mode expansion in enforcement:**
`in any omit Field` or `in any modify Field readonly` uses the `any` state wildcard (confirmed: `Tokens.GetMeta(TokenKind.Any).IsStateWildcard == true`). `PopulateAccessModes` (TypeChecker.cs:908) stores the wildcard text as `StateName`. The proposed `BuildFieldAccessLookup` builds keys as `(StateName, FieldName)` — but transition rows use concrete state names in `FromState`, so a wildcard key `("any", "Foo")` never matches a transition key `("Draft", "Foo")`. Wildcard entries must be expanded to all declared states during lookup construction.

**Category 9 — Set targeting omit field in target state:**
The language spec (§2.2 composition rule 6) states: "`set` targeting an `omit` field in the target state is a compile error." This is a mandatory compile error, not a semantic ambiguity. If a transition `from A on E -> set F = v -> transition B` and `in B omit F`, the value written to `F` is immediately discarded on state entry (spec rule 5: "omit clears on state entry"). The spec declares this illegal. Currently not enforced.

---

## Root Cause Analysis

The root cause is architectural: **`CheckContext` has no concept of "current state for field accessibility."**

### Evidence — `CheckContext` (CheckContext.cs)

The `CheckContext` class (CheckContext.cs:28) carries scope state for:
- `CurrentEventArgs` (line 56) — which event's args are in scope
- `CurrentFieldIndex` (line 63) — for default-expression forward-reference gating
- `CurrentScope` (line 70) — `FieldScopeMode.AllFields` vs `PriorFieldsOnly`
- `QuantifierBindings` (line 79) — quantifier variable bindings

**Missing:** There is no `CurrentFromState`, `CurrentTargetState`, or any field-accessibility-restriction property. The type checker knows which state a transition comes from (it resolves it at TypeChecker.cs:1097), but this information is a local variable in `NormalizeTransitionRow` — it never enters `CheckContext`.

### Evidence — `ResolveAction` (TypeChecker.Expressions.Callables.cs:102)

`ResolveAction` signature: `private static TypedAction ResolveAction(ParsedAction parsedAction, CheckContext ctx)`

It receives only the parsed action and the context. The context has no state information. `ResolveActionTarget` (line 314) does a flat `ctx.FieldLookup.TryGetValue` — the global field dictionary, not filtered by state.

### Evidence — `AccessModes` population timing

`PopulateAccessModes` runs at TypeChecker.cs:47 — **after** `PopulateTransitionRows` (line 41). This means even if `NormalizeTransitionRow` wanted to consult access modes, they haven't been populated yet.

**This is the critical ordering issue.** The pipeline processes transitions before it processes access mode declarations. To enforce field-state constraints during transition resolution, either:
1. Move `PopulateAccessModes` before `PopulateTransitionRows` (risky — may have other dependencies), or
2. Add a separate validation pass that runs after both are complete (safer — consistent with `ValidateModifiers` and `ValidateStructural` patterns).

### Evidence — `TypedAccessMode` records are inert

`TypedAccessMode` (SemanticIndex.cs:401) records are populated and stored in the `SemanticIndex.AccessModes` array. The `GraphAnalyzer` (confirmed by grep — no hits for `AccessMode`, `Omit`, or `omit`) does not read them. The `ProofEngine` (confirmed — no hits for `AccessMode`, `Omit`, or `omit` except unrelated record struct names) does not read them. The records exist for the language server (hover, semantic tokens) and runtime — but no pipeline stage validates that transitions respect them.

### Summary of Causal Chain

1. `CheckContext` lacks state-scoped field accessibility.
2. `ResolveAction` and `Resolve` are state-blind.
3. `PopulateAccessModes` runs after transition resolution.
4. No validation pass checks transitions against access modes.
5. `GraphAnalyzer` and `ProofEngine` do not check access modes.
6. Result: field-state violations compile silently.

---

## Precondition Infrastructure Gaps

Design review identified two infrastructure bugs that must be fixed before the enforcement pass can function correctly. These are preconditions — without them, `ValidateFieldStateAccess` would silently miss violations even if implemented perfectly.

### B1 — OmitDeclaration Routing (`PopulateAccessModes` ignores `ConstructKind.OmitDeclaration`)

**Finding:** `PopulateAccessModes` (TypeChecker.cs:896) only processes `ConstructKind.AccessMode`. `OmitDeclaration` constructs are routed to `ctx.EditDeclarations` via `PopulateEditDeclarations`. `BuildFieldAccessLookup` reads `ctx.AccessModes` — zero omit entries exist there → D128–D130, D132, D134 never fire.

**Root cause:** `OmitDeclaration` is a separate `ConstructKind` from `AccessMode`. The type checker dispatches them to different populations. But omit IS an access mode — it restricts field accessibility per state. The routing is wrong.

**Fix specification:** Expand `PopulateAccessModes` to also process `ConstructKind.OmitDeclaration`. For each omit construct, emit a `TypedAccessMode` record with `Mode = ModifierKind.Omit` into `ctx.AccessModes`. The omit declaration's state name and field name(s) map directly to `TypedAccessMode.StateName` and `TypedAccessMode.FieldName`. This ensures `BuildFieldAccessLookup` sees omit entries and enforcement diagnostics fire correctly.

**Implementation slice:** Precondition Step 0b (see § Fix Architecture).

### B2 — Multi-Field Parser Bug (`ParseFieldTarget` Discards Fields 2–N)

**Finding:** `ParseFieldTarget` (Parser.cs) discards field names 2–N in comma-separated field lists. `in Draft omit ApprovedAmount, AdjusterName, DecisionNote, MissingDocuments` produces only one record for `ApprovedAmount`. The remaining three fields are silently dropped.

**Root cause:** `ParseFieldTarget` reads the first identifier, then does not loop over subsequent comma-separated identifiers. The comma is consumed but the following identifier is not collected.

**Fix specification:** Fix `ParseFieldTarget` to loop over all comma-separated identifiers and emit one `OmitDeclaration`/`AccessMode` construct per field name. Spec §2.2 documents comma-separated field lists as shorthand for multiple single-field declarations — the parser must expand the shorthand at parse time. This also fixes multi-field `modify` declarations (same code path, same root cause).

**Implementation slice:** Precondition Step 0c (see § Fix Architecture).

---

## Spec Update Required

### B4 — Missing Spec Rule for Source-State Omit Enforcement

**Finding:** Spec §2.2 rule 6 covers target-state omit enforcement (`set` targeting an omit field in the target state is a compile error). No rule covers source-state access: writing to or reading a field omitted in the from-state. Diagnostics D128–D130 lack a spec citation.

**Required spec change:** Add a new composition rule to `docs/language/precept-language-spec.md` §2.2:

> **Rule N:** Write or read of a field omitted in the source state is a compile error. The field is structurally absent in that state and cannot be referenced in actions, guards, or action expressions for transitions originating from that state.

This rule provides the spec foundation for D128, D129, and D130. The spec is the contract — it must say it before the compiler enforces it.

**Implementation slice:** Precondition Step 0a (see § Fix Architecture) — this is the very first step, before parser and routing fixes.

> **Spec Hygiene (rule 6 wording):** §2.2 rule 6 says "`set` targeting an `omit` field in the target state is a compile error." D134 as designed covers ALL action kinds (`add`, `remove`, `clear`, `insert`, `put`, etc.) — not just `set`. Update §2.2 rule 6 to read "action targeting" rather than "`set` targeting" in the spec update slice. This is not a blocker but a hygiene item — the spec should match the enforcement breadth.

---

## Proposed Diagnostic Codes

All proposed codes are **Error** severity — these are structural soundness violations, not warnings.

### D128: `WriteToOmittedField`

```
Ordinal:          128
Name:             WriteToOmittedField
Stage:            Type
Severity:         Error
Category:         Structure
MessageTemplate:  "Field '{0}' is omitted in state '{1}' and cannot be written by a '{2}' action in a transition from that state"
TriggerCondition: A transition row's fromState has an omit access mode for the
                  action's target field. Applies to: set, add, remove, clear, insert,
                  put, removeAt, insertAt, and all collection mutation actions.
FixHint:          "Remove the action, or remove the omit declaration for this field in this state"
RecoverySteps:    ["Remove the action from this transition — the field does not exist in the source state",
                   "If the field should exist in this state, remove it from the omit declaration"]
```

### D129: `ReadOmittedFieldInGuard`

```
Ordinal:          129
Name:             ReadOmittedFieldInGuard
Stage:            Type
Severity:         Error
Category:         Structure
MessageTemplate:  "Field '{0}' is omitted in state '{1}' and cannot be referenced in a guard for transitions from that state"
TriggerCondition: A transition row's guard expression references (via TypedFieldRef)
                  a field that is omitted in the fromState.
FixHint:          "Remove the field reference from the guard, or remove the omit declaration"
```

### D130: `ReadOmittedFieldInActionExpression`

```
Ordinal:          130
Name:             ReadOmittedFieldInActionExpression
Stage:            Type
Severity:         Error
Category:         Structure
MessageTemplate:  "Field '{0}' is omitted in state '{1}' and cannot be read in an action expression for transitions from that state"
TriggerCondition: An action's RHS expression references a field omitted in the fromState.
FixHint:          "Use a literal value or event argument instead of the omitted field"
```

### D131: `WriteToReadOnlyField`

```
Ordinal:          131
Name:             WriteToReadOnlyField
Stage:            Type
Severity:         Error
Category:         Structure
MessageTemplate:  "Field '{0}' is read-only in state '{1}' and cannot be written by a '{2}' action in a transition from that state"
TriggerCondition: A transition row's fromState has a read (readonly) access mode for
                  the action's target field.
FixHint:          "Remove the action, or change the access mode to editable for this field in this state"
```

### D132: `WriteToOmittedFieldInStateHook`

```
Ordinal:          132
Name:             WriteToOmittedFieldInStateHook
Stage:            Type
Severity:         Error
Category:         Structure
MessageTemplate:  "Field '{0}' is omitted in state '{1}' and cannot be written in a state hook for that state"
TriggerCondition: A state hook (to/from/in) targets a state where the action's target
                  field is omitted.
FixHint:          "Remove the action from the state hook, or remove the omit declaration"
```

### D133: `WriteToReadOnlyFieldInStateHook`

```
Ordinal:          133
Name:             WriteToReadOnlyFieldInStateHook
Stage:            Type
Severity:         Error
Category:         Structure
MessageTemplate:  "Field '{0}' is read-only in state '{1}' and cannot be written in a state hook for that state"
TriggerCondition: A state hook's action writes to a field that is read-only in the
                  hook's target state.
FixHint:          "Remove the action, or change the access mode to editable"
```

### D134: `SetTargetsOmittedFieldInTargetState`

```
Ordinal:          134
Name:             SetTargetsOmittedFieldInTargetState
Stage:            Type
Severity:         Error
Category:         Structure
MessageTemplate:  "Field '{0}' is omitted in target state '{1}' — the value set here will be discarded on state entry"
TriggerCondition: A transition row's action writes to a field that is omitted in
                  the transition's target state. Per spec §2.2 rule 6: 'set targeting
                  an omit field in the target state is a compile error.' Per rule 5,
                  omit clears the field to default on state entry, making the write
                  dead code.
FixHint:          "Remove the action — the field does not exist in the target state"
RecoverySteps:    ["Remove the action from this transition — the value will be discarded on entry to the target state",
                   "If the field should exist in the target state, remove it from the omit declaration"]
```

### Ordinal Range Justification

The current highest `DiagnosticCode` ordinal is 127 (`AssignmentInExpressionContext`). Ordinals 128–134 are unoccupied. This block of 7 codes covers the full field-state enforcement surface.

---

## Fix Architecture

### Approach: Post-Resolution Validation Pass

**Do NOT modify `ResolveAction` or `Resolve` to be state-aware.** These methods are generic expression resolution machinery used across all contexts (fields, rules, ensures, transitions, state hooks). Adding state context to them would pollute their signatures across dozens of call sites.

Instead, add a new validation pass — `ValidateFieldStateAccess` — that runs after both `PopulateTransitionRows` and `PopulateAccessModes` are complete. But first, three precondition slices must land to make the pass functional.

### Step 0a: Update Spec §2.2 — Source-State Omit Rule

Add a new composition rule to `docs/language/precept-language-spec.md` §2.2:

> **Rule N:** Write or read of a field omitted in the source state is a compile error. The field is structurally absent in that state and cannot be referenced in actions, guards, or action expressions for transitions originating from that state.

Also broaden rule 6 wording from "`set` targeting" to "action targeting" to match D134's enforcement breadth (all action kinds, not just `set`).

**Rationale:** The spec is the contract. No enforcement code ships without a spec citation. D128–D130 cite the new rule; D134 cites the broadened rule 6.

### Step 0b: Route `OmitDeclaration` into `AccessModes`

Expand `PopulateAccessModes` (TypeChecker.cs:896) to also process `ConstructKind.OmitDeclaration`:

```csharp
// In PopulateAccessModes — add processing for OmitDeclaration constructs
foreach (var construct in ctx.Constructs.Where(c => c.Kind == ConstructKind.OmitDeclaration))
{
    // Emit TypedAccessMode with Mode = ModifierKind.Omit for each field
    // StateName and FieldName from the omit declaration's slots
    ctx.AccessModes.Add(new TypedAccessMode(
        StateName: /* omit construct's state slot */,
        FieldName: /* omit construct's field slot */,
        Mode: ModifierKind.Omit,
        Guard: null,
        Syntax: construct));
}
```

This ensures `BuildFieldAccessLookup` (Step 1) sees omit entries in `ctx.AccessModes`. Without this fix, all omit-related diagnostics (D128–D130, D132, D134) are structurally impossible.

### Step 0c: Fix Multi-Field `ParseFieldTarget`

Fix `ParseFieldTarget` (Parser.cs) to loop over all comma-separated identifiers and emit one construct per field name:

```csharp
// Current (broken): reads first identifier, discards rest
// Fixed: loop over comma-separated identifiers
// in Draft omit F1, F2, F3 → emit 3 OmitDeclaration constructs (one per field)
// in Draft modify F1, F2, F3 editable → emit 3 AccessMode constructs (one per field)
```

This also fixes multi-field `modify` declarations (same code path, same root cause). Per spec §2.2, comma-separated field lists are shorthand for multiple single-field declarations — the parser must expand the shorthand.

### Step 1: Build the Access Mode Lookup

Add a helper method to `CheckContext` or as a static utility:

```csharp
/// <summary>
/// Build a lookup: (stateName, fieldName) → effective ModifierKind (Omit, Read, Write).
/// Expands wildcard access modes to all declared states. Skips guarded access modes
/// (conservative — the guard might not hold, so the base mode applies).
/// </summary>
private static FrozenDictionary<(string State, string Field), ModifierKind>
    BuildFieldAccessLookup(List<TypedAccessMode> accessModes, IReadOnlyCollection<string> allStateNames)
{
    var dict = new Dictionary<(string, string), ModifierKind>();

    // Pass 1: wildcard (any-state) entries — expand to all declared states
    foreach (var am in accessModes)
    {
        if (am.Guard is not null) continue; // Skip guarded — conservative
        if (!HasKeywordTokenMeta(am.StateName, meta => meta.IsStateWildcard)) continue;
        foreach (var state in allStateNames)
            dict[(state, am.FieldName)] = am.Mode;
    }

    // Pass 2: specific state entries — override wildcard entries
    foreach (var am in accessModes)
    {
        if (am.Guard is not null) continue; // Skip guarded — conservative
        if (HasKeywordTokenMeta(am.StateName, meta => meta.IsStateWildcard)) continue;
        dict[(am.StateName, am.FieldName)] = am.Mode;
    }

    return dict.ToFrozenDictionary();
}
```

**Design note on wildcard expansion:** `in any omit Field` expands to an omit entry for every declared state. A specific `in State modify Field editable` overrides the wildcard for that (state, field) pair. Pass 2 entries win because they run second, matching the spec rule "State-level override always wins."

**Design note on guarded access modes:** `in UnderReview when not FraudFlag modify AdjusterName editable` is conditional — the field is only editable when `FraudFlag` is false. The validator skips guarded modes entirely — enforcement uses only unconditional modes plus the D3 baseline. This is conservative (no false positives). Closing this gap requires guard-implication analysis: proving that a transition's guard implies the access mode's guard. That infrastructure does not exist and is deferred (see § Deferred — Blocked By).

**Design note on multi-field omit:** `in Draft omit ApprovedAmount, AdjusterName, DecisionNote, MissingDocuments` declares multiple fields. **Confirmed bug (B2):** `ParseFieldTarget` discards fields 2–N — only `ApprovedAmount` produces a construct. Step 0c fixes the parser to emit one construct per field. After Step 0c, each field produces a separate `TypedAccessMode` record and the lookup handles multi-field omit correctly.

### Step 2: Add `ValidateFieldStateAccess` Pass

Insert after `PopulateAccessModes` in the pipeline (TypeChecker.cs, after line 47):

```csharp
// Field-state access enforcement (after access modes and transitions are both populated)
ValidateFieldStateAccess(ctx, allStateNames);
```

Implementation in `TypeChecker.Validation.cs`:

```csharp
/// <summary>
/// Validate that transition actions and guards respect field-state access modes.
/// Runs after PopulateTransitionRows and PopulateAccessModes.
/// </summary>
private static void ValidateFieldStateAccess(CheckContext ctx, IReadOnlyCollection<string> allStateNames)
{
    if (ctx.AccessModes.Count == 0) return;

    var lookup = BuildFieldAccessLookup(ctx.AccessModes, allStateNames);

    // ── Transition rows ──────────────────────────────────────────
    foreach (var row in ctx.TransitionRows)
    {
        if (row.FromState is null) continue; // Wildcard — no state to check

        // Check actions
        foreach (var action in row.Actions)
        {
            if (string.IsNullOrEmpty(action.FieldName)) continue;
            if (lookup.TryGetValue((row.FromState, action.FieldName), out var mode))
            {
                if (mode == ModifierKind.Omit)
                    ctx.Diagnostics.Add(Diagnostics.Create(
                        DiagnosticCode.WriteToOmittedField, action.Span,
                        action.FieldName, row.FromState, Actions.GetMeta(action.Kind).Token.Text));
                else if (mode == ModifierKind.Read)
                    ctx.Diagnostics.Add(Diagnostics.Create(
                        DiagnosticCode.WriteToReadOnlyField, action.Span,
                        action.FieldName, row.FromState, Actions.GetMeta(action.Kind).Token.Text));
            }
        }

        // Check guard field references (read of omitted field)
        if (row.Guard is not null)
        {
            foreach (var fieldRef in CollectFieldRefsFromExpression(row.Guard))
            {
                if (lookup.TryGetValue((row.FromState, fieldRef), out var mode)
                    && mode == ModifierKind.Omit)
                {
                    ctx.Diagnostics.Add(Diagnostics.Create(
                        DiagnosticCode.ReadOmittedFieldInGuard, row.Guard.Span,
                        fieldRef, row.FromState));
                }
            }
        }

        // Check action RHS expression field references (read of omitted field)
        foreach (var action in row.Actions)
        {
            // Extract both InputExpression and SecondaryExpression (index/key in insert…at / put…key)
            IEnumerable<TypedExpression> rhsExpressions = action switch
            {
                TypedInputAction input => input.SecondaryExpression is not null
                    ? [input.InputExpression, input.SecondaryExpression]
                    : [input.InputExpression],
                _ => []
            };

            foreach (var rhsExpression in rhsExpressions)
            {
                foreach (var fieldRef in CollectFieldRefsFromExpression(rhsExpression))
                {
                    if (lookup.TryGetValue((row.FromState, fieldRef), out var mode)
                        && mode == ModifierKind.Omit)
                    {
                        ctx.Diagnostics.Add(Diagnostics.Create(
                            DiagnosticCode.ReadOmittedFieldInActionExpression, rhsExpression.Span,
                            fieldRef, row.FromState));
                    }
                }
            }
        }
    }

    // ── Target-state validation (spec §2.2 rule 6) ──────────────────
    foreach (var row in ctx.TransitionRows)
    {
        if (row.TargetState is null) continue; // no transition / reject — no target

        foreach (var action in row.Actions)
        {
            if (string.IsNullOrEmpty(action.FieldName)) continue;
            if (lookup.TryGetValue((row.TargetState, action.FieldName), out var targetMode)
                && targetMode == ModifierKind.Omit)
            {
                ctx.Diagnostics.Add(Diagnostics.Create(
                    DiagnosticCode.SetTargetsOmittedFieldInTargetState, action.Span,
                    action.FieldName, row.TargetState));
            }
        }
    }

    // ── State hooks ──────────────────────────────────────────────
    foreach (var hook in ctx.StateHooks)
    {
        if (string.IsNullOrEmpty(hook.StateName)) continue;

        foreach (var action in hook.Actions)
        {
            if (string.IsNullOrEmpty(action.FieldName)) continue;
            if (lookup.TryGetValue((hook.StateName, action.FieldName), out var mode))
            {
                if (mode == ModifierKind.Omit)
                    ctx.Diagnostics.Add(Diagnostics.Create(
                        DiagnosticCode.WriteToOmittedFieldInStateHook, action.Span,
                        action.FieldName, hook.StateName));
                else if (mode == ModifierKind.Read)
                    ctx.Diagnostics.Add(Diagnostics.Create(
                        DiagnosticCode.WriteToReadOnlyFieldInStateHook, action.Span,
                        action.FieldName, hook.StateName));
            }
        }
    }
}

/// <summary>
/// Recursively collect field names referenced in a typed expression tree.
/// </summary>
private static IEnumerable<string> CollectFieldRefsFromExpression(TypedExpression expr)
{
    switch (expr)
    {
        case TypedFieldRef fieldRef:
            yield return fieldRef.FieldName;
            break;
        case TypedBinaryOp bin:
            foreach (var f in CollectFieldRefsFromExpression(bin.Left)) yield return f;
            foreach (var f in CollectFieldRefsFromExpression(bin.Right)) yield return f;
            break;
        case TypedUnaryOp un:
            foreach (var f in CollectFieldRefsFromExpression(un.Operand)) yield return f;
            break;
        case TypedConditional cond:
            foreach (var f in CollectFieldRefsFromExpression(cond.Condition)) yield return f;
            foreach (var f in CollectFieldRefsFromExpression(cond.ThenBranch)) yield return f;
            foreach (var f in CollectFieldRefsFromExpression(cond.ElseBranch)) yield return f;
            break;
        case TypedFunctionCall func:
            foreach (var arg in func.Arguments)
                foreach (var f in CollectFieldRefsFromExpression(arg)) yield return f;
            break;
        case TypedMemberAccess member:
            foreach (var f in CollectFieldRefsFromExpression(member.Object)) yield return f;
            break;
        case TypedQuantifier quant:
            foreach (var f in CollectFieldRefsFromExpression(quant.Collection)) yield return f;
            foreach (var f in CollectFieldRefsFromExpression(quant.Predicate)) yield return f;
            break;
        case TypedInterpolatedString interp:
            foreach (var seg in interp.Segments)
                if (seg is TypedHoleSegment hole)
                    foreach (var f in CollectFieldRefsFromExpression(hole.Expression)) yield return f;
            break;
        case TypedInterpolatedTypedConstant itc:
            foreach (var slot in itc.Slots)
                foreach (var f in CollectFieldRefsFromExpression(slot.Expression)) yield return f;
            break;
        case TypedListLiteral list:
            foreach (var elem in list.Elements)
                foreach (var f in CollectFieldRefsFromExpression(elem)) yield return f;
            break;
        case TypedPostfixOp postfix:
            foreach (var f in CollectFieldRefsFromExpression(postfix.Operand)) yield return f;
            break;
    }
}
```

### Step 3: Multi-field Omit Handling

**Confirmed bug (B2) — resolved in Step 0c.** `ParseFieldTarget` discards fields 2–N in comma-separated field lists. Step 0c fixes the parser to emit one construct per field. After that fix, `PopulateAccessModes` (with Step 0b's OmitDeclaration routing) correctly produces separate `TypedAccessMode` records per field. No additional work needed here — the validation pass reads the flat `AccessModes` list regardless of how entries were produced.

### Step 4: Update the Sample File

Remove line 43 from `samples/insurance-claim.precept`:
```diff
 from Draft on Submit when ClaimantName is set and ClaimAmount > '0.00 USD' and (not PoliceReportRequired or ClaimAmount <= '100000.00 USD')
-    -> set ApprovedAmount = '0.00 USD'
     -> transition Submitted
```

The `ApprovedAmount` field has `default '0.00 USD'` (line 10), so the field materializes with the correct value when the entity enters `Submitted`.

### Step 5: Add DiagnosticMeta Entries

Add entries to `Diagnostics.cs` (the `GetMeta` switch expression) for all 6 new codes:

```csharp
DiagnosticCode.WriteToOmittedField => new(
    nameof(DiagnosticCode.WriteToOmittedField),
    DiagnosticStage.Type, Severity.Error,
    "Field '{0}' is omitted in state '{1}' and cannot be written by a '{2}' action in a transition from that state",
    DiagnosticCategory.Structure,
    FixHint: "Remove the action — the field does not exist in the source state",
    RecoverySteps: ["Remove the action from this transition",
                    "If the field should exist in this state, remove it from the omit declaration"]),
// ... analogous entries for D129–D133
```

### Step 6: Test Plan

| Test | What it validates |
|------|-------------------|
| `WriteToOmittedField_EmitsDiagnostic` | `in S omit F` + `from S on E -> set F = v` → D128 |
| `WriteToOmittedField_CollectionAction_EmitsDiagnostic` | `in S omit F` + `from S on E -> add F v` → D128 |
| `ReadOmittedFieldInGuard_EmitsDiagnostic` | `in S omit F` + `from S on E when F is set` → D129 |
| `ReadOmittedFieldInActionRHS_EmitsDiagnostic` | `in S omit F` + `from S on E -> set G = F` → D130 |
| `WriteToReadOnlyField_EmitsDiagnostic` | `in S modify F readonly` + `from S on E -> set F = v` → D131 |
| `WriteToOmittedFieldInStateHook_EmitsDiagnostic` | `in S omit F` + `to S -> set F = v` → D132 |
| `WriteToReadOnlyFieldInStateHook_EmitsDiagnostic` | `in S modify F readonly` + `to S -> set F = v` → D133 |
| `NoOmitViolation_DifferentState_NoDiagnostic` | `in S1 omit F` + `from S2 on E -> set F = v` → no diagnostic |
| `WildcardFromState_SkipsCheck_NoDiagnostic` | `from any on E -> set F = v` (wildcard) → no diagnostic |
| `GuardedAccessMode_ConservativeSkip_NoDiagnostic` | `in S when G modify F editable` (guarded) → no diagnostic for writes to F from S |
| `InsuranceClaim_LegacyBug_NowCaught` | Compile `insurance-claim.precept` (pre-fix) → D128 on line 43 |
| `WildcardOmit_AllStatesEnforced` | `in any omit F` + `from S on E -> set F = v` → D128 for every state |
| `WildcardOmit_OverriddenBySpecific_NoDiagnostic` | `in any omit F` + `in S modify F editable` + `from S on E -> set F = v` → no diagnostic for S |
| `SetTargetsOmittedFieldInTargetState_EmitsDiagnostic` | `from S1 on E -> set F = v -> transition S2` + `in S2 omit F` → D134 |
| `SetTargetsNonOmittedFieldInTargetState_NoDiagnostic` | `from S1 on E -> set F = v -> transition S2` + `in S2 modify F editable` → no diagnostic |
| `NoTransition_SkipsTargetCheck` | `from S on E -> set F = v -> no transition` → no D134 even if F is omitted in other states |
| `MultiFieldOmit_ParseFieldTarget_EmitsPerField` | `in S omit F1, F2, F3` → parser emits 3 separate OmitDeclaration constructs (Step 0c verification) |
| `MultiFieldOmit_AllFieldsEnforced` | `in S omit F1, F2, F3` + `from S on E -> set F2 = v` → D128 fires for F2 (verifies parser splits multi-field omit into separate records) |
| `WriteToOmittedField_CollectionRemove_EmitsDiagnostic` | `in S omit F` + `from S on E -> remove F v` → D128 fires with action kind `remove` |
| `ReadNonOmittedFieldInGuard_NoDiagnostic` | `in S omit F` + `from S on E when NotF is set` → no D129 (guard reads a field present in S) |
| `ReadOmittedFieldInGuard_DeepNesting_EmitsDiagnostic` | `from S on E when (A and (B or OmittedField))` → D129 fires (exercises recursive walk on nested TypedBinaryOp) |
| `ReadNonOmittedFieldInActionRHS_NoDiagnostic` | `in S omit F` + `from S on E -> set G = NotF` → no D130 (RHS reads a field present in S) |
| `ReadOmittedFieldInActionRHS_DeepNesting_EmitsDiagnostic` | `from S on E -> set G = (OmittedField + 1)` → D130 fires (exercises recursive walk on nested TypedBinaryOp in RHS) |
| `WriteToEditableField_NoDiagnostic` | `in S modify F editable` + `from S on E -> set F = v` → no D131 (editable is not readonly) |
| `WriteToReadOnlyField_CollectionAction_EmitsDiagnostic` | `in S modify F readonly` + `from S on E -> add F v` → D131 fires with action kind `add` |
| `WriteToNonOmittedFieldInStateHook_NoDiagnostic` | `in S omit F_other` + `to S -> set F_present = v` → no D132 (F_present is not omitted in S) |
| `WriteToOmittedFieldInStateHook_FromDirection_EmitsDiagnostic` | `in S omit F` + `from S -> set F = v` → D132 fires (from-hook writes field omitted in S) |
| `WriteToEditableFieldInStateHook_NoDiagnostic` | `in S modify F editable` + `to S -> set F = v` → no D133 |
| `WriteToReadOnlyFieldInStateHook_FromDirection_EmitsDiagnostic` | `in S modify F readonly` + `from S -> set F = v` → D133 fires (from-hook writes readonly field in S) |
| `SetTargetsOmittedFieldInTargetState_NonSetAction_EmitsDiagnostic` | `from S1 on E -> add F v -> transition S2` + `in S2 omit F` → D134 fires (non-set action targets field omitted in target state) |
| `SetTargetsOmittedFieldInTargetState_MixedActions_OnlyOmittedFieldFires` | `-> set F1 = v1 -> set F2 = v2 -> transition S2` where only F1 is omitted in S2 → exactly one D134 fires (for F1 only; F2 clean; no bleed between actions) |

---

## Sample File Audit

### Violations Found

| File | Line | Description |
|------|------|-------------|
| `insurance-claim.precept` | 43 | `set ApprovedAmount = '0.00 USD'` — writes to `ApprovedAmount` which is declared `omit` in `Draft` (line 26). The transition originates `from Draft`. **Confirmed bug.** |

### Files with `in State omit` Declarations

| File | Line | Declaration |
|------|------|-------------|
| `insurance-claim.precept` | 26 | `in Draft omit ApprovedAmount, AdjusterName, DecisionNote, MissingDocuments` |

### Files with `in State modify ... editable/readonly` Declarations (No Violations)

| File | Line | Declaration |
|------|------|-------------|
| `apartment-rental-application.precept` | 23 | `in Draft modify ApplicantName, MonthlyIncome, RequestedRent, CreditScore, HouseholdSize editable` |
| `building-access-badge-request.precept` | 23 | `in Draft modify EmployeeName, Department, AccessReason, RequestedFloors editable` |
| `clinic-appointment-scheduling.precept` | 22 | `in Scheduled modify ScheduledDay, ScheduledMinute editable` |
| `crosswalk-signal.precept` | 17 | `in DontWalk modify RequestPending editable` |
| `event-registration.precept` | 26, 28 | `in Draft modify ... editable`, `in PendingPayment modify ContactEmail editable` |
| `hiring-pipeline.precept` | 23 | `in Draft modify CandidateName, RoleName, RecruiterName editable` |
| `insurance-claim.precept` | 27–29 | `in Draft modify ... editable`, `in UnderReview modify FraudFlag editable`, guarded AdjusterName |
| `inventory-item.precept` | 114, 116, 121 | `in Unlisted modify ... editable`, `in Listed modify ... editable`, `in LowStock modify ... editable` |
| `it-helpdesk-ticket.precept` | 22 | `in New modify Priority editable` |
| `library-book-checkout.precept` | 23–24 | `in CheckedOut modify DueDay editable`, `in Overdue modify DueDay, FineAmount editable` |
| `loan-application.precept` | 30 | `in UnderReview when DocumentsVerified modify DecisionNote editable` |
| `maintenance-work-order.precept` | 28 | `in Draft modify Location, IssueSummary, Urgent editable` |
| `refund-request.precept` | 21 | `in Submitted modify ReasonText editable` |
| `subscription-cancellation-retention.precept` | 18 | `in RetentionReview modify LastAgentNote editable` |
| `trafficlight.precept` | 18 | `in Red modify VehiclesWaiting, LeftTurnQueued editable` |
| `utility-outage-report.precept` | 22 | `in VerifiedState modify EstimatedCustomers editable` |
| `vehicle-service-appointment.precept` | 21 | `in CheckedIn modify AdvisorName editable` |

**Note:** No `readonly` (read) access modes are used in any sample file. All `modify` declarations use `editable`. This means the `WriteToReadOnlyField` diagnostic (D131) has no sample file trigger — but the enforcement is still required for completeness.

### Guard References to Omitted Fields

No instances found in current samples where a guard in a `from State` transition references a field omitted in that state. The insurance-claim guards from Draft reference `ClaimantName`, `ClaimAmount`, and `PoliceReportRequired` — none of which are omitted in Draft.

---

## Implementation Scope

### Files Changed

| File | Change |
|------|--------|
| `docs/language/precept-language-spec.md` | Add §2.2 composition rule for source-state omit enforcement; broaden rule 6 "set targeting" → "action targeting" |
| `src/Precept/Pipeline/Parser.cs` | Fix `ParseFieldTarget` to loop over comma-separated identifiers, emitting one construct per field (Step 0c) |
| `src/Precept/Pipeline/TypeChecker.cs` | Expand `PopulateAccessModes` to process `ConstructKind.OmitDeclaration` (Step 0b); add `ValidateFieldStateAccess(ctx)` call after `PopulateAccessModes` |
| `src/Precept/Language/DiagnosticCode.cs` | Add ordinals 128–134 |
| `src/Precept/Language/Diagnostics.cs` | Add `DiagnosticMeta` entries for D128–D134 |
| `src/Precept/Pipeline/TypeChecker.Validation.cs` | Add `ValidateFieldStateAccess` method + `CollectFieldRefsFromExpression` helper + `BuildFieldAccessLookup` helper |
| `samples/insurance-claim.precept` | Remove line 43 (`set ApprovedAmount = '0.00 USD'`) |
| `src/Precept/Runtime/Evaluator.cs` | Add design comments specifying Phase 3 access mode enforcement obligations on action application methods |

### Files New

| File | Purpose |
|------|---------|
| `test/Precept.Tests/Pipeline/TypeCheckerFieldStateTests.cs` | ~31 test cases covering all 7 diagnostics + negative cases + wildcard + target-state + multi-field parser + deep nesting + collection actions + hook directions |

### Test Requirements

- All existing tests must pass (currently ~5500 tests across 3 projects)
- New tests must cover all 7 diagnostic codes
- Insurance-claim sample must compile clean after the line 43 fix
- Regression: no false positives on any other sample file

### Deferred — Blocked By

1. **Guarded access mode enforcement** — proving that a transition's guard implies the access mode's guard (e.g., `from S on E when G1 -> set F = v` is safe if `G1 → G2` where `in S when G2 modify F editable`). **Blocked on:** guard-implication analysis infrastructure. No expression-implication engine exists in the pipeline. The current conservative behavior (skip guarded modes → no false positives) is correct; the gap is that writes to fields with only guarded editability are silently allowed even when the transition has no guard. Risk: low — the pattern is rare, and if the guard doesn't hold at runtime, the evaluator will reject the write once Phase 3 evaluator is implemented.

### Implementation Ordering Constraint

> **CI Ordering Constraint (F5TempVerify):** The sample compilation regression test (`F5TempVerify::Sample_CompilesClean`) compiles all sample files and asserts zero diagnostics. When `ValidateFieldStateAccess` goes live, `insurance-claim.precept` line 43 will emit D128. The line 43 fix (remove `-> set ApprovedAmount = '0.00 USD'`) MUST land in the SAME COMMIT as the enforcement pass — not before (it would change sample semantics prematurely) and not after (it would break CI). This is a hard sequencing constraint on the implementation slice.

### Spec Hygiene Note

> **Spec Hygiene (rule 6 wording):** §2.2 rule 6 says "`set` targeting an `omit` field in the target state is a compile error." D134 as designed covers ALL action kinds (`add`, `remove`, `clear`, `insert`, `put`, etc.) — not just `set`. Update §2.2 rule 6 to read "action targeting" rather than "`set` targeting" in the spec update slice (Step 0a). This is not a blocker — it is a hygiene item to keep the spec aligned with enforcement breadth.

---

## Runtime Enforcement Design Obligation

The evaluator (`src/Precept/Runtime/Evaluator.cs`) is not yet implemented — all action methods throw `NotImplementedException` (Phase 3, D8/R4). However, Precept provides no partial guarantees. Access mode constraints enforced at compile time MUST be enforced at runtime as well. The compiler catching a violation at type-check time is defense-in-depth, not a substitute for runtime enforcement.

**Design obligation:** When Phase 3 implements `Evaluator.Fire`, `Evaluator.Update`, `Evaluator.InspectFire`, and `Evaluator.InspectUpdate`, each action application step MUST:

1. Resolve the current state of the entity before applying the action
2. Look up the access mode for the target field in the current state (from `TypeCheckResult.AccessModes`)
3. If the access mode is `Omit`: throw a `Fault` via `Evaluator.Fail(FaultCode.WriteToOmittedField, ...)` — the `FaultCode.WriteToOmittedField` member must be added with `[StaticallyPreventable(DiagnosticCode.WriteToOmittedField)]` (to be added in Phase 3)
4. If the access mode is `Read`: throw a `Fault` via `Evaluator.Fail(FaultCode.WriteToReadOnlyField, ...)` — the `FaultCode.WriteToReadOnlyField` member must be added with `[StaticallyPreventable(DiagnosticCode.WriteToReadOnlyField)]` (to be added in Phase 3)
5. These faults are non-recoverable — they indicate a compiler bug (the compile-time check should have caught this) or API misuse (calling runtime APIs with uncompiled input)

Design comments documenting these obligations are added directly to the evaluator source (see Implementation Scope).
