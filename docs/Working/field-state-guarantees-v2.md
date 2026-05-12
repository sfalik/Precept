# Field-State Guarantees — v2 Design

> **Status:** Proposal  
> **Author:** Frank (Lead/Architect)  
> **Date:** 2026-05-12  

---

## Executive Summary

Precept guarantees no runtime errors. That guarantee does not extend to field-state access today: a transition originating in a state where a field is `omit`ted can still read or write that field; a transition can write to a `readonly` field; and a conditionally-editable field (`modify when {condition}`) can be written without the transition guard implying the access condition. None of these violations produce compile-time diagnostics. The runtime evaluator defers to compile-time prevention (its remarks say so explicitly), but the compile-time enforcement does not exist.

**The fix has two parts.** Part 1 is a **structural enforcement pass** in the TypeChecker that validates actions, guards, and expressions against per-state access mode tables — catching writes to omitted fields, reads of omitted fields, writes to readonly fields, and writes to fields omitted in the target state. Part 2 is a **conditional enforcement obligation** in the ProofEngine that verifies transition guards logically imply the access condition for `modify when {condition}` fields — using the existing `GuardInPath` decomposition infrastructure. A prerequisite parser fix captures all comma-separated field names in access mode declarations, where the current parser silently discards fields 2–N.

---

## Root Cause Analysis

### What the pipeline currently does

1. **`PopulateAccessModes`** (TypeChecker.cs:938–1005) iterates `ConstructKind.AccessMode` constructs. For each, it resolves the state target, the field target (single field name via `FieldTargetSlot`), the access mode keyword (`readonly`/`editable`), and the optional guard expression. It produces `TypedAccessMode` records into `ctx.AccessModes`.

2. **`PopulateEditDeclarations`** (TypeChecker.cs:1059–1093) iterates `ConstructKind.OmitDeclaration` constructs. It extracts a single field name into `TypedEditDeclaration`. **The edit declarations are not connected to the access mode table** — they populate `ctx.EditDeclarations`, not `ctx.AccessModes`. Omit declarations are therefore invisible to any validation that reads `AccessModes`.

3. **`NormalizeTransitionRow`** (TypeChecker.cs:1096–1221) resolves `fromState`, event, guard, action chain, and outcome. It knows `fromState` — every `TypedTransitionRow` carries `FromState: string?`. But it **does not consult access modes** when resolving actions or guards. `ResolveAction` (TypeChecker.Expressions.Callables.cs:102) receives only `CheckContext` — no `fromState`, no access mode table.

4. **`PopulateStateHooks`** (TypeChecker.cs:1011) resolves on-entry/on-exit hooks with their action chains. The hook's `StateName` is known, but actions are resolved without access mode awareness.

5. **`ValidateStructural`** (TypeChecker.Validation.cs:264) does computed-field cycle detection. It does not validate field-state access.

6. **Diagnostic codes `ConflictingAccessModes` (42) and `RedundantAccessMode` (43)** exist in `DiagnosticCode.cs` with full `DiagnosticMeta` entries in `Diagnostics.cs`. They are **never emitted** — no code in the pipeline references them.

### What the parser currently does wrong

**`ParseFieldTarget`** (Parser.cs:1005–1048) parses comma-separated field names but stores only the first:

```csharp
var first = Advance();
var lastSpan = first.Span;
while (Peek().Kind == TokenKind.Comma)
{
    Advance();                    // consume comma
    if (Peek().Kind == TokenKind.Identifier)
    {
        lastSpan = Advance().Span; // consume identifier — DISCARDED
        continue;
    }
    // ...
}
return new FieldTargetSlot(first.Text, SourceSpan.Covering(first.Span, lastSpan))
{
    NameSpan = first.Span,        // only first field's span
};
```

Given `in Draft omit ApprovedAmount, AdjusterName, DecisionNote, MissingDocuments`, the parser:
- Correctly lexes and consumes all four identifiers and three commas
- Creates a `FieldTargetSlot` with `FieldName = "ApprovedAmount"` only
- Sets the span to cover all four names (cosmetically correct, semantically wrong)
- **Silently discards `AdjusterName`, `DecisionNote`, `MissingDocuments`**

This means `insurance-claim.precept` line 26 produces ONE `TypedAccessMode` for `ApprovedAmount` in `Draft`. The other three fields have no omit record. Line 27 produces ONE `TypedAccessMode` for `ClaimantName` — `ClaimAmount` and `PoliceReportRequired` are silently dropped.

### What the ProofEngine currently knows

The ProofEngine (ProofEngine.cs, ~121 KB) handles 6 obligation kinds: `Numeric`, `Presence`, `Dimension`, `Modifier`, `QualifierCompatibility`, `QualifierChain`. None relate to field-state access.

The engine has mature guard decomposition infrastructure:
- `ExtractGuardConstraints` (line 766) decomposes `and`-conjoined guards into `GuardConstraint` facts
- `GuardSubsumes` (line 847) checks whether a guard constraint satisfies a numeric requirement
- Presence checks are handled by matching `field is set` postfix operations
- OR disjuncts are correctly rejected (line 783)
- `not (X op Y)` negation is handled (line 821)

This infrastructure can be extended to check whether a transition guard implies an access condition.

---

## Architecture: Two-Category Model

### Category 1: Structural Violations (TypeChecker)

These are **definite** violations — no guard condition can make them safe. A field that is `omit`ted in a state does not exist in that state, period. A field that is `readonly` cannot be written, period (unless the readonly itself is conditional, which crosses into Category 2).

| Violation | Surface | Detection |
|-----------|---------|-----------|
| Action writes to omitted field in `fromState` | Transition action | fromState + access modes |
| Guard reads omitted field in `fromState` | Transition guard | fromState + access modes |
| Action RHS reads omitted field in `fromState` | Transition action expression | fromState + access modes |
| Action writes to readonly field in `fromState` | Transition action | fromState + access modes |
| State hook reads/writes omitted field | on-entry/on-exit action | hook state + access modes |
| State hook writes readonly field | on-entry/on-exit action | hook state + access modes |
| Action writes field omitted in `targetState` | Transition action | targetState + access modes |

For unconditional access modes (no guard), this is a pure TypeChecker concern. The TypeChecker already has all the data:
- `TypedTransitionRow.FromState` — known after `NormalizeTransitionRow`
- `TypedTransitionRow.TargetState` — known after outcome resolution  
- `TypedAccessMode` records — known after `PopulateAccessModes`
- `TypedStateHook.StateName` — known after `PopulateStateHooks`

### Category 2: Conditional Violations (ProofEngine)

These apply only to **guarded access modes**: `in State when {condition} modify Field editable`. The field is editable only when the condition holds. If a transition writes to such a field, the transition's guard must logically imply the access condition.

Example from `insurance-claim.precept`:
```
in UnderReview when not FraudFlag modify AdjusterName editable
```

If a transition `from UnderReview on AssignAdjuster -> set AdjusterName = ...` has no guard (or a guard that doesn't imply `not FraudFlag`), then the write could execute when `FraudFlag` is true — when `AdjusterName` is readonly. This is a conditional violation.

The ProofEngine already has the machinery to check this: `ExtractGuardConstraints` can decompose both the transition guard and the access condition. The question is whether the transition guard's constraints subsume the access condition's constraints.

---

## Phase 1: Structural Enforcement (TypeChecker Pass)

### 1.1 Parser Fix — Multi-Field FieldTargetSlot

**File:** `src/Precept/Pipeline/SlotValue.cs`

Change `FieldTargetSlot` to carry all field names:

```csharp
/// <summary>Field name(s) or "all".</summary>
public sealed record FieldTargetSlot(
    ImmutableArray<string> FieldNames,
    SourceSpan Span
) : SlotValue(ConstructSlotKind.FieldTarget, Span)
{
    /// <summary>Backward-compat: first field name, or null if empty/broadcast.</summary>
    public string? FieldName => FieldNames.IsDefaultOrEmpty ? null
        : FieldNames[0];

    /// <summary>Per-field name spans for diagnostics and LS references.</summary>
    public ImmutableArray<SourceSpan> NameSpans { get; init; } =
        ImmutableArray<SourceSpan>.Empty;
}
```

**File:** `src/Precept/Pipeline/Parser.cs`, method `ParseFieldTarget` (line 1005)

Replace the single-capture loop with multi-capture:

```csharp
private SlotValue ParseFieldTarget(ConstructSlot slot)
{
    var current = Peek();
    var meta = Tokens.GetMeta(current.Kind);
    if (meta.IsFieldBroadcast)
    {
        var tok = Advance();
        return new FieldTargetSlot(
            ImmutableArray<string>.Empty,
            tok.Span)
        {
            NameSpans = ImmutableArray<SourceSpan>.Empty,
        };
    }

    if (current.Kind == TokenKind.Identifier)
    {
        var names = ImmutableArray.CreateBuilder<string>();
        var spans = ImmutableArray.CreateBuilder<SourceSpan>();
        var first = Advance();
        names.Add(first.Text);
        spans.Add(first.Span);
        var lastSpan = first.Span;

        while (Peek().Kind == TokenKind.Comma)
        {
            Advance(); // consume comma
            if (Peek().Kind == TokenKind.Identifier)
            {
                var fieldTok = Advance();
                names.Add(fieldTok.Text);
                spans.Add(fieldTok.Span);
                lastSpan = fieldTok.Span;
                continue;
            }
            _diagnostics.Add(DiagnosticsCatalog.Create(
                DiagnosticCode.ExpectedToken, Peek().Span, "identifier", Peek().Text));
            break;
        }

        return new FieldTargetSlot(
            names.ToImmutable(),
            SourceSpan.Covering(first.Span, lastSpan))
        {
            NameSpans = spans.ToImmutable(),
        };
    }

    if (!slot.IsRequired)
        return MakeSentinel(slot);
    _diagnostics.Add(DiagnosticsCatalog.Create(
        DiagnosticCode.ExpectedToken, Peek().Span, "field name", Peek().Text));
    return new FieldTargetSlot(ImmutableArray<string>.Empty, Peek().Span);
}
```

**Downstream changes:** Every consumer of `FieldTargetSlot.FieldName` must iterate `FieldNames` instead. Two call sites:
- `PopulateAccessModes` (TypeChecker.cs:950–969) — loop over `FieldNames` and `NameSpans`, emitting one `TypedAccessMode` per field per resolved state.
- `PopulateEditDeclarations` (TypeChecker.cs:1069–1086) — loop over `FieldNames`, emitting one `TypedAccessMode` with `Mode = ModifierKind.Omit` per field per resolved state.

### 1.2 Unify Omit Into AccessModes

`PopulateEditDeclarations` currently produces `TypedEditDeclaration` — a separate accumulator from `AccessModes`. This is architecturally wrong. Omit is an access mode (`ModifierKind.Omit = 26`). It belongs in `ctx.AccessModes`.

**Change:** In `PopulateEditDeclarations`, instead of pushing to `ctx.EditDeclarations`, push `TypedAccessMode` records with `Mode = ModifierKind.Omit` into `ctx.AccessModes`:

```csharp
// For each field in the omit declaration, for each resolved state:
ctx.AccessModes.Add(new TypedAccessMode(
    StateName: resolvedState.StateName,
    FieldName: fieldName,
    Mode: ModifierKind.Omit,
    Guard: null,          // omit never has guards (enforced by parser D13)
    Syntax: construct));
```

**Keep `EditDeclarations` for backward compat** if anything reads it (stateless-precept design D24). But the field-state enforcement reads `AccessModes` only.

### 1.3 Build Field Access Lookup Table

After `PopulateAccessModes` and `PopulateEditDeclarations` complete, build a lookup table:

**New method in `TypeChecker.cs`:**

```csharp
/// <summary>
/// Build a per-(state, field) access mode lookup from the accumulated
/// <see cref="CheckContext.AccessModes"/>. Emits <see cref="DiagnosticCode.ConflictingAccessModes"/>
/// and <see cref="DiagnosticCode.RedundantAccessMode"/> diagnostics.
/// </summary>
private static FrozenDictionary<(string State, string Field), FieldAccess>
    BuildFieldAccessLookup(CheckContext ctx)
{
    // Group unconditional access modes by (state, field)
    // Detect conflicts (two different unconditional modes for same state+field)
    // Detect redundancy (unconditional mode matches field baseline)
    // Return effective access for structural enforcement
}
```

**New record:**
```csharp
/// <summary>Effective access mode for a field in a state.</summary>
internal readonly record struct FieldAccess(
    ModifierKind EffectiveMode,
    TypedExpression? Condition,   // null for unconditional
    SourceSpan DeclarationSpan);
```

This method also emits the currently-dead `ConflictingAccessModes` and `RedundantAccessMode` diagnostics.

### 1.4 Field-State Validation Pass

**New method in `TypeChecker.Validation.cs`:**

```csharp
/// <summary>
/// Validate field-state access guarantees: no read/write to omitted fields,
/// no write to readonly fields, no write to fields omitted in target state.
/// </summary>
private static void ValidateFieldStateAccess(CheckContext ctx)
```

**What it validates:**

#### 1.4a — Transition Row Structural Violations

For each `TypedTransitionRow` in `ctx.TransitionRows`:

1. Look up `row.FromState` in the field access lookup.
2. For each `TypedAction` in `row.Actions`:
   - If action writes a field and that field is `Omit` in `fromState` → emit `WriteToOmittedField`
   - If action writes a field and that field is `Read` in `fromState` → emit `WriteToReadOnlyField`
   - If action writes a field and that field is `Omit` in `row.TargetState` → emit `WriteToTargetOmittedField`
3. Walk `row.Guard` expression tree:
   - If any `TypedFieldRef` references a field that is `Omit` in `fromState` → emit `ReadOfOmittedField`
4. Walk each action's RHS expression tree:
   - If any `TypedFieldRef` references a field that is `Omit` in `fromState` → emit `ReadOfOmittedField`
5. **Wildcard rows** (`FromState == null`): validate against ALL states. If the access mode lookup shows the field is omitted in ANY reachable state where the wildcard applies, emit the diagnostic. Conservative but correct.

#### 1.4b — State Hook Structural Violations

For each `TypedStateHook` in `ctx.StateHooks`:

1. Look up `hook.StateName` in the field access lookup. For on-entry hooks, the relevant state is the target; for on-exit hooks, it's the source.
2. For each `TypedAction` in `hook.Actions`:
   - Write to omitted field → `WriteToOmittedField`
   - Write to readonly field → `WriteToReadOnlyField`
3. Walk guard and action expressions for reads of omitted fields.

#### 1.4c — Conditional Access Modes

When a field has a guarded access mode (`modify when {condition} Field editable`):
- The field's unconditional effective mode is `Read` (the field exists but is not writable unless the condition holds).
- If a transition writes to this field and the access mode is conditional, **do not emit the structural diagnostic**. Instead, mark it for Phase 2 proof obligation.

### 1.5 Pipeline Ordering

Current pipeline order (TypeChecker.cs:28–62):

```
Pass 1:  PopulateFields → PopulateStates → PopulateEvents
Pass 1b: ResolveFieldExpressions
Pass 2:  PopulateTransitionRows → PopulateEventHandlers → PopulateRules
Pass 2b: PopulateEnsures → PopulateAccessModes → PopulateStateHooks → PopulateEditDeclarations
         ValidateModifiers → ValidateStructural → ValidateCIEnforcement
         BuildSemanticIndex
```

**Required new order:**

```
Pass 1:  PopulateFields → PopulateStates → PopulateEvents
Pass 1b: ResolveFieldExpressions
Pass 2:  PopulateTransitionRows → PopulateEventHandlers → PopulateRules
Pass 2b: PopulateEnsures → PopulateAccessModes → PopulateEditDeclarations
         → BuildFieldAccessLookup                    ← NEW (must follow access modes + omits)
         → PopulateStateHooks
         ValidateModifiers → ValidateStructural
         → ValidateFieldStateAccess                  ← NEW (must follow rows + hooks + lookup)
         → ValidateCIEnforcement
         BuildSemanticIndex
```

Key dependencies:
- `BuildFieldAccessLookup` depends on `PopulateAccessModes` and `PopulateEditDeclarations` (both feed `ctx.AccessModes`).
- `ValidateFieldStateAccess` depends on `BuildFieldAccessLookup` (the lookup table), `PopulateTransitionRows` (the rows), and `PopulateStateHooks` (the hooks).
- `PopulateStateHooks` can run before or after the lookup — it just normalizes constructs.

The lookup table is stored on `CheckContext`:

```csharp
// Add to CheckContext.cs
public FrozenDictionary<(string State, string Field), FieldAccess>?
    FieldAccessLookup { get; set; }
```

### 1.6 Expression Tree Walking

To check whether a guard or action expression reads an omitted field, walk the typed expression tree:

```csharp
/// <summary>
/// Collect all field names referenced (read) in a typed expression tree.
/// </summary>
private static void CollectFieldReads(
    TypedExpression? expr,
    HashSet<string> fieldNames)
{
    if (expr is null) return;
    switch (expr)
    {
        case TypedFieldRef fr:
            fieldNames.Add(fr.FieldName);
            break;
        case TypedBinaryOp bin:
            CollectFieldReads(bin.Left, fieldNames);
            CollectFieldReads(bin.Right, fieldNames);
            break;
        case TypedUnaryOp un:
            CollectFieldReads(un.Operand, fieldNames);
            break;
        case TypedPostfixOp post:
            CollectFieldReads(post.Operand, fieldNames);
            break;
        case TypedFunctionCall func:
            foreach (var arg in func.Arguments)
                CollectFieldReads(arg, fieldNames);
            break;
        case TypedMemberAccess ma:
            CollectFieldReads(ma.Object, fieldNames);
            break;
        case TypedConditional cond:
            CollectFieldReads(cond.Condition, fieldNames);
            CollectFieldReads(cond.TrueExpr, fieldNames);
            CollectFieldReads(cond.FalseExpr, fieldNames);
            break;
        // Literals, errors, etc. — no field refs
    }
}
```

---

## Phase 2: Conditional Enforcement (ProofEngine)

### 2.1 The Problem

```
in UnderReview when not FraudFlag modify AdjusterName editable
```

A transition `from UnderReview on AssignAdjuster -> set AdjusterName = trim(AssignAdjuster.Name) -> transition UnderReview` writes to `AdjusterName`. The field is editable **only when `not FraudFlag`**. The transition has no guard.

This is not a structural violation (the field is not unconditionally readonly — it's conditionally editable). But it IS a violation: the write can execute when `FraudFlag` is true, at which point `AdjusterName` is readonly.

### 2.2 Solution: Access Condition Proof Obligation

Introduce a new `ProofRequirementKind`:

```csharp
// In ProofRequirementKind.cs
/// <summary>
/// Access condition implication — a transition guard must imply the access
/// mode condition for a conditionally-editable field being written.
/// </summary>
AccessCondition = 7,
```

**New `ProofRequirement` subtype:**

```csharp
// In ProofRequirement.cs
/// <summary>
/// Access condition proof: the transition guard must logically imply
/// the access condition for a conditionally-editable field.
/// </summary>
public sealed record AccessConditionProofRequirement(
    string FieldName,
    string StateName,
    TypedExpression AccessCondition,
    string Description
) : ProofRequirement(ProofRequirementKind.AccessCondition, Description);
```

**New `ProofRequirementMeta` subtype:**

```csharp
// In ProofRequirement.cs, inside ProofRequirementMeta
public sealed record AccessCondition()
    : ProofRequirementMeta(ProofRequirementKind.AccessCondition,
        "Access condition — transition guard must imply the field's conditional editability guard");
```

### 2.3 Obligation Collection

In Phase 1's `ValidateFieldStateAccess`, when a transition writes to a field that has a guarded access mode:

1. Do NOT emit a structural diagnostic.
2. Instead, record the obligation for later ProofEngine collection.

**New accumulator on `CheckContext`:**

```csharp
// In CheckContext.cs
public List<PendingAccessConditionObligation> AccessConditionObligations { get; } = [];
```

```csharp
internal sealed record PendingAccessConditionObligation(
    TypedTransitionRow Row,
    TypedAction Action,
    TypedAccessMode AccessMode);
```

**In the ProofEngine's `CollectObligations`:**

Add a new section after existing obligation collection:

```csharp
// Access condition obligations
foreach (var pending in semantics.AccessConditionObligations)
{
    obligations.Add(new ProofObligation(
        Requirement: new AccessConditionProofRequirement(
            pending.AccessMode.FieldName,
            pending.AccessMode.StateName,
            pending.AccessMode.Guard!,
            $"Write to '{pending.AccessMode.FieldName}' requires guard to imply: {FormatCondition(pending.AccessMode.Guard!)}"),
        Site: pending.Action.Span,  // squiggle on the write action
        Context: new TransitionRowContext(pending.Row),
        Disposition: ProofDisposition.Unresolved,
        Strategy: null,
        EmittedDiagnostic: null));
}
```

Wait — `SemanticIndex` is immutable. The pending obligations need to flow from TypeChecker to ProofEngine. Two options:

**Option A:** Add `AccessConditionObligations` to `SemanticIndex`.  
**Option B:** Have the ProofEngine derive them from `SemanticIndex.AccessModes` + `SemanticIndex.TransitionRows`.

Option B is architecturally cleaner — the ProofEngine already walks transition rows. It can cross-reference guarded access modes when it encounters a write action:

```csharp
// In CollectObligations, inside the TransitionRows loop:
foreach (var row in semantics.TransitionRows)
{
    var ctx = new TransitionRowContext(row);
    WalkActions(row.Actions, ctx, obligations);
    
    // Access condition obligations
    if (row.FromState is not null)
    {
        foreach (var action in row.Actions)
        {
            if (IsWriteAction(action.Kind))
            {
                var guardedModes = semantics.AccessModes
                    .Where(am => am.StateName == row.FromState
                              && am.FieldName == action.FieldName
                              && am.Guard is not null
                              && am.Mode == ModifierKind.Write);
                foreach (var am in guardedModes)
                {
                    obligations.Add(new ProofObligation(
                        Requirement: new AccessConditionProofRequirement(
                            am.FieldName, am.StateName, am.Guard!,
                            $"Write to '{am.FieldName}' requires the transition guard to imply the access condition"),
                        Site: action is TypedInputAction input ? input.InputExpression : new TypedLiteral(TypeKind.Error, null, action.Span),
                        Context: ctx,
                        Disposition: ProofDisposition.Unresolved,
                        Strategy: null,
                        EmittedDiagnostic: null));
                }
            }
        }
    }
}
```

### 2.4 Discharge: Guard Implies Access Condition

**New strategy in `ProofStrategy`:**

```csharp
// In ProofLedger.cs
AccessConditionImplication = 7,
```

**New discharge method in `ProofEngine.cs`:**

```csharp
private static bool TryAccessConditionProof(ProofObligation obligation, SemanticIndex semantics)
{
    if (obligation.Requirement is not AccessConditionProofRequirement access)
        return false;

    // Get the transition guard
    var guard = obligation.Context switch
    {
        TransitionRowContext t => t.Row.Guard,
        StateHookContext s => s.Hook.Guard,
        _ => null
    };
    if (guard is null) return false; // no guard → cannot imply anything

    // Extract constraints from the transition guard
    var guardConstraints = ExtractGuardConstraints(guard);

    // Extract constraints from the access condition
    var accessConstraints = ExtractGuardConstraints(access.AccessCondition);

    // The guard must imply the access condition.
    // For conjunction of access constraints, every access constraint
    // must be subsumed by some guard constraint.
    foreach (var ac in accessConstraints)
    {
        bool subsumed = false;
        foreach (var gc in guardConstraints)
        {
            if (gc.Field == ac.Field)
            {
                if (ac.IsPresenceCheck && gc.IsPresenceCheck)
                {
                    subsumed = true;
                    break;
                }
                if (!ac.IsPresenceCheck && !gc.IsPresenceCheck
                    && ac.Value is { } av && gc.Value is { } gv
                    && NumericConstraintSubsumes(gc.Comparison, gv,
                        new NumericProofRequirement(
                            new SelfSubject(), ac.Comparison, av, "")))
                {
                    subsumed = true;
                    break;
                }
            }
        }
        if (!subsumed) return false;
    }

    return true;
}
```

This reuses the existing `ExtractGuardConstraints` infrastructure. The check is: every conjunct in the access condition is subsumed by some conjunct in the transition guard. This is sound (no false negatives for the `and`-conjunct decomposition) and conservative (may reject valid programs where the implication holds but requires reasoning beyond conjunct matching).

**Special case: boolean field guards.** The access condition `when not FraudFlag` decomposes to a guard constraint on `FraudFlag`. The transition guard must contain `not FraudFlag` (or `FraudFlag == false`) to subsume it. The existing `ExtractGuardConstraintsCore` handles `not X` as a negated comparison (line 821), and boolean field checks as `field == true/false` via the postfix `is set` path. We may need to extend constraint extraction to recognize `not {boolField}` as `{boolField} == false`:

```csharp
// In ExtractGuardConstraintsCore, add case:
case TypedUnaryOp { ResolvedOp: var uop } un
    when Operations.GetMeta(uop).Op == OperatorKind.Not
    && un.Operand is TypedFieldRef negField
    && negField.ResultType == TypeKind.Boolean:
    // "not X" where X is boolean → X == false → X <= 0
    builder.Add(new GuardConstraint(negField.FieldName, OperatorKind.Equals, 0, false));
    break;
```

And for bare boolean field references:
```csharp
case TypedFieldRef { ResultType: TypeKind.Boolean } boolRef:
    // bare boolean field in guard context → field == true → field >= 1
    builder.Add(new GuardConstraint(boolRef.FieldName, OperatorKind.Equals, 1, false));
    break;
```

### 2.5 Add to TryDischarge Cascade

In `TryDischarge` (ProofEngine.cs:456), add `TryAccessConditionProof` to the strategy cascade:

```csharp
private static (ProofDisposition, ProofStrategy?) TryDischarge(ProofObligation obligation, SemanticIndex semantics)
{
    if (TryLiteralProof(obligation))
        return (ProofDisposition.Proved, ProofStrategy.Literal);
    if (TryDeclarationAttributeProof(obligation, semantics))
        return (ProofDisposition.Proved, ProofStrategy.DeclarationAttribute);
    if (TryGuardInPathProof(obligation, semantics))
        return (ProofDisposition.Proved, ProofStrategy.GuardInPath);
    if (TryAccessConditionProof(obligation, semantics))                    // ← NEW
        return (ProofDisposition.Proved, ProofStrategy.AccessConditionImplication);
    if (TryFlowNarrowingProof(obligation, semantics))
        return (ProofDisposition.Proved, ProofStrategy.FlowNarrowing);
    // ...
}
```

---

## Diagnostic Codes

### New Codes

| Code | Name | Severity | Message | Category |
|------|------|----------|---------|----------|
| 130 | `WriteToOmittedField` | Error | `Field '{0}' is omitted in state '{1}' — it cannot be written` | Structure |
| 131 | `ReadOfOmittedField` | Error | `Field '{0}' is omitted in state '{1}' — it cannot be read` | Structure |
| 132 | `WriteToReadOnlyField` | Error | `Field '{0}' is read-only in state '{1}' — it cannot be written` | Structure |
| 133 | `WriteToTargetOmittedField` | Error | `Field '{0}' is omitted in target state '{1}' — setting it in this transition has no effect` | Structure |
| 134 | `UnprovedAccessCondition` | Error | `Write to conditionally-editable field '{0}' in state '{1}': the transition guard does not imply the access condition` | Proof |

### Existing Codes (Now Emitted)

| Code | Name | Notes |
|------|------|-------|
| 42 | `ConflictingAccessModes` | Now emitted by `BuildFieldAccessLookup` |
| 43 | `RedundantAccessMode` | Now emitted by `BuildFieldAccessLookup` |

### Files to Modify

- `src/Precept/Language/DiagnosticCode.cs` — add codes 130–134
- `src/Precept/Language/Diagnostics.cs` — add `DiagnosticMeta` entries for 130–134
- `src/Precept/Language/FaultCode.cs` — add `WriteToOmittedField`, `WriteToReadOnlyField` fault codes with `[StaticallyPreventable]` attributes pointing to the new diagnostic codes

---

## Implementation Plan

### Slice 1: Parser Fix (FieldTargetSlot Multi-Field)

**Goal:** Comma-separated field targets produce all field names, not just the first.

1. **`src/Precept/Pipeline/SlotValue.cs`** — Change `FieldTargetSlot`:
   - Replace `string? FieldName` with `ImmutableArray<string> FieldNames`
   - Add `ImmutableArray<SourceSpan> NameSpans`
   - Add backward-compat `FieldName` property (returns first or null)

2. **`src/Precept/Pipeline/Parser.cs`** — Rewrite `ParseFieldTarget` (line 1005):
   - Accumulate all identifiers and spans into builders
   - Return multi-field slot

3. **`src/Precept/Pipeline/TypeChecker.cs`** — Update `PopulateAccessModes` (line 938):
   - Loop over `fieldSlot.FieldNames` with matching `fieldSlot.NameSpans`
   - Emit one `TypedAccessMode` per field per resolved state

4. **`src/Precept/Pipeline/TypeChecker.cs`** — Update `PopulateEditDeclarations` (line 1059):
   - Loop over `fieldSlot.FieldNames`
   - Emit `TypedAccessMode` with `Mode = ModifierKind.Omit` (unification, see Slice 2)

5. **Tests:**
   - Parser test: `in Draft omit A, B, C` → three `TypedAccessMode` records
   - Parser test: `in Draft modify X, Y editable` → two `TypedAccessMode` records
   - Parser test: single field still works
   - Parser test: `all` keyword still works
   - Regression: all existing parser tests pass

### Slice 2: Omit Unification + Field Access Lookup

**Goal:** Omit declarations produce `TypedAccessMode` records; build the lookup table; emit D42/D43.

1. **`src/Precept/Pipeline/TypeChecker.cs`** — In `PopulateEditDeclarations`:
   - Replace `ctx.EditDeclarations.Add(...)` with `ctx.AccessModes.Add(new TypedAccessMode(Mode: ModifierKind.Omit, ...))` for each field

2. **`src/Precept/Pipeline/CheckContext.cs`** — Add:
   - `public FrozenDictionary<(string State, string Field), FieldAccess>? FieldAccessLookup { get; set; }`

3. **`src/Precept/Pipeline/SemanticIndex.cs`** — Add:
   - `internal readonly record struct FieldAccess(ModifierKind EffectiveMode, TypedExpression? Condition, SourceSpan DeclarationSpan)`

4. **`src/Precept/Pipeline/TypeChecker.cs`** — New method `BuildFieldAccessLookup`:
   - Group `ctx.AccessModes` by `(StateName, FieldName)`
   - Unconditional modes: check for conflicts (D42), redundancy with field baseline (D43)
   - Build `FrozenDictionary<(string, string), FieldAccess>`
   - Store on `ctx.FieldAccessLookup`

5. **`src/Precept/Pipeline/TypeChecker.cs`** — Update pipeline order:
   - Move `BuildFieldAccessLookup(ctx)` call after `PopulateEditDeclarations` and before `PopulateStateHooks`

6. **Tests:**
   - D42: `in Draft modify Name editable` + `in Draft modify Name readonly` → ConflictingAccessModes
   - D43: `field Name as string writable` + `in Draft modify Name editable` → RedundantAccessMode
   - Lookup correctness: omit produces `ModifierKind.Omit` entries
   - Lookup correctness: unmentioned field+state → default access

### Slice 3: Structural Enforcement

**Goal:** Emit D130–D133 for structural field-state violations.

1. **`src/Precept/Language/DiagnosticCode.cs`** — Add codes 130–134

2. **`src/Precept/Language/Diagnostics.cs`** — Add `DiagnosticMeta` for 130–134

3. **`src/Precept/Pipeline/TypeChecker.Validation.cs`** — New method `ValidateFieldStateAccess(CheckContext ctx)`:
   - Iterate `ctx.TransitionRows`
   - For each row with non-null `FromState`, look up `ctx.FieldAccessLookup[(fromState, field)]` for each action target field
   - Emit D130 (write to omitted), D131 (read of omitted), D132 (write to readonly), D133 (write to target-omitted)
   - For guarded access modes, skip structural diagnostic — record for Phase 2
   - Iterate `ctx.StateHooks` similarly
   - Walk guard and action expression trees with `CollectFieldReads`

4. **`src/Precept/Pipeline/TypeChecker.Validation.cs`** — New helper `CollectFieldReads`:
   - Recursive expression tree walker, collecting `TypedFieldRef.FieldName` values

5. **`src/Precept/Pipeline/TypeChecker.cs`** — Call `ValidateFieldStateAccess(ctx)` after `ValidateStructural`

6. **Tests (organized by diagnostic):**

   **D130 — WriteToOmittedField:**
   - Transition `from Draft on Submit -> set ApprovedAmount = ...` where Draft omits ApprovedAmount → Error
   - State hook `on entering Draft -> set ApprovedAmount = ...` → Error
   - Wildcard row `from * on X -> set OmittedField = ...` where field is omitted in any state → Error

   **D131 — ReadOfOmittedField:**
   - Guard `from Draft on Submit when ApprovedAmount > 0` where Draft omits ApprovedAmount → Error
   - Action RHS `from Draft on X -> set Y = ApprovedAmount + 1` where Draft omits ApprovedAmount → Error
   - Computed expression referencing omitted field (not state-scoped — this is a different concern)

   **D132 — WriteToReadOnlyField:**
   - Transition writes to field that is `readonly` in fromState → Error
   - State hook writes to readonly field → Error
   - Field with no `writable` modifier and no `modify editable` in state → read-only by default → Error on write

   **D133 — WriteToTargetOmittedField:**
   - `from Submitted on X -> set InternalNotes = "..." -> transition Draft` where Draft omits InternalNotes → Error

   **Negative (no diagnostic):**
   - Write to field with no access mode restrictions → clean
   - Write to field in state where it's `editable` → clean
   - Read of field that exists in fromState → clean
   - Omitted field that is not touched → clean

### Slice 4: Conditional Enforcement (ProofEngine)

**Goal:** Emit D134 for unproved access condition implications.

1. **`src/Precept/Language/ProofRequirementKind.cs`** — Add `AccessCondition = 7`

2. **`src/Precept/Language/ProofRequirement.cs`** — Add:
   - `AccessConditionProofRequirement` record
   - `ProofRequirementMeta.AccessCondition` subtype

3. **`src/Precept/Pipeline/ProofLedger.cs`** — Add `AccessConditionImplication = 7` to `ProofStrategy`

4. **`src/Precept/Pipeline/ProofEngine.cs`** — In `CollectObligations`:
   - After walking transition row actions, cross-reference guarded access modes
   - Emit `AccessConditionProofRequirement` obligations for writes to conditionally-editable fields

5. **`src/Precept/Pipeline/ProofEngine.cs`** — New method `TryAccessConditionProof`:
   - Extract constraints from transition guard and access condition
   - Check conjunct-wise subsumption

6. **`src/Precept/Pipeline/ProofEngine.cs`** — Extend `ExtractGuardConstraintsCore`:
   - Handle bare boolean field refs (`FraudFlag` → `FraudFlag == true`)
   - Handle `not BoolField` → `BoolField == false`

7. **`src/Precept/Pipeline/ProofEngine.cs`** — Add to `TryDischarge` cascade

8. **`src/Precept/Pipeline/ProofEngine.cs`** — Add to `CreateDiagnostic` for D134

9. **Tests:**
   - `in S when Cond modify F editable` + transition `from S -> set F = ...` with no guard → D134
   - Same + transition with `when Cond` guard → clean (proved by AccessConditionImplication)
   - `when X > 5` condition + `when X > 10` guard → clean (guard is stronger)
   - `when X > 5` condition + `when X > 3` guard → D134 (guard is weaker)
   - `when not FraudFlag` condition + `when not FraudFlag` guard → clean
   - `when not FraudFlag` condition + no guard → D134
   - `when A and B` condition + `when A` guard → D134 (missing B)
   - `when A and B` condition + `when A and B and C` guard → clean

---

## What the ProofEngine Already Knows (Summary)

| Capability | Status | Relevance |
|------------|--------|-----------|
| 6 obligation kinds (Numeric, Presence, Dimension, Modifier, QualifierCompatibility, QualifierChain) | Shipped | Not directly applicable, but the framework is extensible |
| `ExtractGuardConstraints` — AND-conjunct decomposition | Shipped | **Directly reusable** for access condition comparison |
| `GuardSubsumes` — numeric constraint implication | Shipped | **Directly reusable** for numeric access conditions |
| Presence check matching | Shipped | Reusable for `field is set` access conditions |
| OR rejection | Shipped | Correct — OR disjuncts cannot guarantee access conditions |
| `not (X op Y)` negation | Shipped | Partially reusable — extend for bare boolean negation |
| `TransitionRowContext` — links obligation to transition | Shipped | **Directly reusable** for access condition obligations |
| `StateHookContext` — links obligation to state hook | Shipped | Reusable for hook access conditions |
| Forwarding facts from `StateGraph` | Shipped | Not needed for access conditions |
| `CreateDiagnostic` dispatch | Shipped | Extend for new obligation kind |
| `CreateFaultSiteLink` dispatch | Shipped | Extend for new fault code |

### What Needs to Be Added

1. `ProofRequirementKind.AccessCondition` — new enum member
2. `AccessConditionProofRequirement` — new DU subtype
3. `ProofStrategy.AccessConditionImplication` — new strategy enum member
4. `TryAccessConditionProof` — new discharge method (~40 lines)
5. Boolean guard constraint extraction — extend `ExtractGuardConstraintsCore` (~10 lines)
6. Obligation collection for guarded access modes — ~20 lines in `CollectObligations`
7. Diagnostic creation for D134 — ~5 lines in `CreateDiagnostic`

**Total ProofEngine delta: ~75 lines of new code.** The infrastructure does the heavy lifting.

---

## Test Plan Summary

| Category | Test Count | Key Scenarios |
|----------|-----------|---------------|
| Parser multi-field | 5 | Single field, two fields, three fields, all keyword, trailing comma error |
| Omit unification | 3 | Omit → AccessMode, omit + modify same field = conflict, omit all |
| D42 ConflictingAccessModes | 3 | Same field, two modes; different fields clean; guarded + unguarded |
| D43 RedundantAccessMode | 3 | Writable field + editable mode; readonly field + readonly mode; guarded mode (no redundancy) |
| D130 WriteToOmittedField | 5 | Transition action, state hook, wildcard row, target-state action (→ D133), clean case |
| D131 ReadOfOmittedField | 4 | Guard read, action RHS read, conditional expression read, clean case |
| D132 WriteToReadOnlyField | 4 | Explicit readonly, default readonly (no writable), state hook, clean case |
| D133 WriteToTargetOmittedField | 3 | Set + transition to omit state, set + no transition (clean), set + transition to non-omit (clean) |
| D134 UnprovedAccessCondition | 8 | No guard, matching guard, stronger guard, weaker guard, boolean guard, negated boolean, compound condition, clean case |
| Regression | 5 | insurance-claim.precept compiles clean (after parser fix captures all fields), existing samples maintain diagnostic count |
| **Total** | **~43** | |

---

## Open Design Questions

1. **Wildcard row handling for conditional access modes.** A `from * on X -> set F = ...` row touches all states. If `F` is conditionally editable in one state, the obligation would need to consider all states' access conditions. The conservative answer: emit one obligation per state that has a guarded access mode for the field. This is correct but may produce many diagnostics.

2. **`if` expressions inside actions.** `set F = if Cond then Expr1 else Expr2` — if `Expr1` reads an omitted field but `Cond` guarantees we only reach `Expr1` when the field exists, should we allow it? For v2, the conservative answer is NO — the structural check walks the full expression tree. Conditional field reads under `if` guards require Phase 2 proof integration.

3. **Event handlers.** `TypedEventHandler` has actions but no `fromState`. Event handlers are stateless — they fire in any state. For field-state validation, they must be clean in ALL states. If any state omits a field that the handler writes, that's an error.

4. **On-entry hooks and target state.** An on-entry hook (`to State`) fires when entering the state. The access modes of the target state apply. An on-exit hook (`from State`) fires when leaving — the access modes of the source state apply. The hook's `Scope` field (`AnchorScope.To` vs `AnchorScope.From`) determines which direction.
