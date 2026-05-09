# PE-G4 through PE-G18 — Full Resolution (No Deferrals)

**Date:** 2026-05-08
**Author:** Frank (Lead/Architect)
**Status:** All gaps resolved — zero open questions, zero deferrals
**Directive:** Shane's explicit mandate — no phases, no "the implementer decides," no "future stage." Define everything now.

---

## PE-G4: `SemanticIndex.AllTypedExpressions` Missing → RESOLVED

**Gap:** The spec's Pass 1 pseudocode references `semantics.AllTypedExpressions` — a property that does not exist on `SemanticIndex`.

**Resolution:** Do NOT add an `AllTypedExpressions` property to `SemanticIndex`. Replace the pseudocode with an explicit walk-target enumeration. The proof engine walks these specific `SemanticIndex` members to collect all proof-relevant expressions:

| Walk Target | Expression Source | ProofRequirements Location |
|---|---|---|
| `TransitionRows[].Actions[]` | `TypedAction.ProofRequirements` | On the action record itself |
| `TransitionRows[].Actions[]` (input) | `TypedInputAction.InputExpression` recursively | Nested `TypedBinaryOp`, `TypedFunctionCall`, `TypedMemberAccess` |
| `TransitionRows[].Guard` | Guard expression tree | Nested expressions (these are read by strategies, not walked for obligations) |
| `EventHandlers[].Actions[]` | Same as transition row actions | On the action record itself + nested expressions |
| `StateHooks[].Actions[]` | Same pattern | On the action record itself + nested expressions |
| `Rules[].Condition` | Rule condition expression tree | Nested `TypedBinaryOp`, `TypedFunctionCall`, `TypedMemberAccess` |
| `Ensures[].Condition` | Ensure condition expression tree | Same nested pattern |
| `Fields[].DefaultExpression` | Field default expressions | Nested expressions (relevant for initial-state satisfiability, not obligation walk) |
| `Fields[].ComputedExpression` | Computed field expressions | Nested expressions |

**Walk algorithm:** Recursive depth-first traversal of each expression tree. For each node, check whether it is a `TypedBinaryOp`, `TypedFunctionCall`, `TypedMemberAccess`, or `TypedAction` with non-empty `ProofRequirements`. If so, instantiate one `ProofObligation` per requirement.

**Expression types that carry `ProofRequirements`:**
- `TypedBinaryOp` — `ProofRequirements` property (from `BinaryOperationMeta`)
- `TypedFunctionCall` — `ProofRequirements` property (from `FunctionOverload`)
- `TypedMemberAccess` — `ProofRequirements` property (from `TypeAccessor`)
- `TypedAction` (and subtypes) — `ProofRequirements` property (from `ActionMeta`)

**Expression types that do NOT carry `ProofRequirements`:**
- `TypedLiteral`, `TypedFieldRef`, `TypedArgRef`, `TypedConditional`, `TypedQuantifier`, `TypedInterpolatedString`, `TypedTypedConstant`, `TypedListLiteral`, `TypedPostfixOp`, `TypedUnaryOp`, `TypedErrorExpression` — none of these carry `ProofRequirements`. They may appear as children of obligation-bearing nodes.

**Rationale:** Adding a helper property would create a coupling surface that must be maintained when new typed expression kinds are added. The explicit walk ensures the implementer knows exactly where to look and why. The walk is finite and deterministic.

**Spec correction required:** Yes — replace `semantics.AllTypedExpressions` in §9 pseudocode with the explicit walk. The walk should be a private helper method in `ProofEngine.cs`, not a public API.

**Implementation note:** Implement as a private static method `CollectObligations(SemanticIndex semantics)` that returns `ImmutableArray<ProofObligation>`. Walk each of the targets listed above. Use a recursive visitor or pattern-match descent for nested expressions.

---

## PE-G5: `ConstraintIdentity` Shapes (Spec/Source Mismatch) → RESOLVED

**Gap:** The spec defines `ConstraintIdentity` subtypes with different parameter names/shapes than the existing source implementation.

**Resolution:** The source shapes are canonical. The spec must be updated to match. This was already identified in PE-G3 analysis and is consolidated here as the definitive ruling.

| Type | Spec (WRONG) | Source (CANONICAL) |
|---|---|---|
| `RuleIdentity` | `(string RuleName, int Index)` | `(int RuleIndex)` |
| `EnsureIdentity` | `(ConstraintKind Kind, string? AnchorState, string? AnchorEvent, int Index)` | `(ConstraintKind Kind, string? AnchorName, int EnsureIndex)` |

**Why source wins:**
1. The source shapes were created during TypeChecker implementation — they are tested and consumed by `ConstraintFieldRefs`.
2. `RuleIdentity` does not need `RuleName` — rules are positionally identified by index in the `Rules` array. Name would be redundant and error-prone.
3. `EnsureIdentity` uses a single `AnchorName` because an ensure is anchored to either a state OR an event, never both simultaneously. Two separate nullable fields (`AnchorState`, `AnchorEvent`) would create an invalid representation (both non-null). The `ConstraintKind` discriminates which kind of anchor `AnchorName` refers to.

**Rationale:** Source is implementation truth. The spec hypothesized shapes; the implementation refined them. No implementer should waste time reconciling.

**Spec correction required:** Yes — update `proof-engine.md` §5 `ConstraintInfluenceEntry` subsection: replace the spec's `ConstraintIdentity` shapes with the source shapes. Update any pseudocode that references `RuleName`, `AnchorState`, or `AnchorEvent`.

**Implementation note:** No code changes needed — source already has the correct shapes. Only the spec document changes.

---

## PE-G6: `FindEnclosingTransitionRow` Undefined → RESOLVED

**Gap:** Strategies 3 and 4 call `FindEnclosingTransitionRow(obligation.Site, semantics)` but this function is never defined.

**Resolution:** The proof engine builds an expression-to-context index in Pass 1. Each `ProofObligation` is tagged with its `ObligationContext` at instantiation time — the context is not discovered later.

**ObligationContext DU:**

```csharp
public abstract record ObligationContext;

public sealed record TransitionRowContext(TypedTransitionRow Row) : ObligationContext;
public sealed record ConstraintContext(ConstraintIdentity Constraint) : ObligationContext;
public sealed record StateHookContext(TypedStateHook Hook) : ObligationContext;
public sealed record EventHandlerContext(TypedEventHandler Handler) : ObligationContext;
public sealed record FieldExpressionContext(TypedField Field) : ObligationContext;
```

**How it works:** During Pass 1, the walk knows which top-level context it's iterating. When walking `TransitionRows[i].Actions`, the context is `TransitionRowContext(row)`. When walking `Rules[i].Condition`, the context is `ConstraintContext(RuleIdentity(i))`. This context is stored on `ProofObligation`:

```csharp
public sealed record ProofObligation(
    ProofRequirement Requirement,
    TypedExpression Site,
    ObligationContext Context,       // ← NEW: set at instantiation
    ProofDisposition Disposition,
    ProofStrategy? Strategy,
    DiagnosticCode? EmittedDiagnostic
);
```

**Strategy dispatch rules:**
- **Strategy 3 (Guard-in-Path):** Fires ONLY for `TransitionRowContext`. Reads `Row.Guard`. For `ConstraintContext`, `StateHookContext`, `EventHandlerContext`, `FieldExpressionContext` — returns `false` immediately. State hooks have guards, so `StateHookContext` is extended: `StateHookContext(TypedStateHook Hook)` and Strategy 3 reads `Hook.Guard` when non-null.
- **Strategy 4 (Flow Narrowing):** Same scope as Strategy 3 — `TransitionRowContext` only (plus `StateHookContext` when guard is present). The guard must be in the same row.

**Rationale:** The context-at-instantiation pattern avoids a post-hoc search (which would be O(N²) — walking all transition rows for each obligation to find which one contains the expression). Since Pass 1 already knows the context, attaching it is O(1).

**Spec correction required:** Yes — add `ObligationContext` DU definition to §5 (output types). Add `Context` field to `ProofObligation`. Update Strategy 3/4 pseudocode to read `obligation.Context` instead of calling `FindEnclosingTransitionRow`.

**Implementation note:** `ObligationContext` goes in `ProofLedger.cs`. `ProofObligation` gains a `Context` field. Pass 1's walk method passes the current context as a parameter.

---

## PE-G7: `ResolveSubject` and `GetFieldName` Undefined → RESOLVED

**Gap:** Strategies 1 and 2 call `ResolveSubject` and `GetFieldName` but neither is defined.

**Resolution:** Define both as private helper methods in `ProofEngine.cs`. These are subject resolution utilities, not public API.

### `ResolveSubject(ProofSubject subject, TypedExpression site) → TypedExpression?`

Resolves a `ProofSubject` to the concrete `TypedExpression` node within the obligation's expression site.

```csharp
static TypedExpression? ResolveSubject(ProofSubject subject, TypedExpression site)
{
    return subject switch
    {
        ParamSubject param => site switch
        {
            // Binary op: match parameter identity against the operation's parameter list
            // BinaryOperationMeta has exactly 2 parameters (Left, Right).
            // ParamSubject.Parameter is reference-equal to one of them.
            TypedBinaryOp bin => ResolveParamInBinaryOp(param.Parameter, bin),

            // Function call: match parameter against the resolved overload's Parameters list
            // to find positional index, then return Arguments[index].
            TypedFunctionCall call => ResolveParamInFunctionCall(param.Parameter, call),

            // Member access: the parameter refers to the object being accessed
            TypedMemberAccess access => ResolveParamInMemberAccess(param.Parameter, access),

            _ => null  // no resolution possible
        },

        SelfSubject self => site switch
        {
            // For TypedMemberAccess, "self" is the Object expression
            TypedMemberAccess access => access.Object,

            // For TypedAction, "self" is the field being acted on.
            // Resolve to a synthetic TypedFieldRef from the action's FieldName.
            TypedAction action => ResolveSelfInAction(self, action),

            _ => null
        },

        _ => null
    };
}
```

**Binary op parameter resolution:**

The `BinaryOperationMeta` in `Operations.cs` declares parameters with names like `PInteger`, `PDecimal`. The `ProofRequirement` on a `TypedBinaryOp` carries a `ParamSubject` whose `Parameter` is reference-equal to one of the operation's declared parameters. The proof engine resolves this by checking reference equality:

```csharp
static TypedExpression? ResolveParamInBinaryOp(ParameterMeta param, TypedBinaryOp bin)
{
    // Look up the BinaryOperationMeta for bin.ResolvedOp
    var opMeta = Operations.GetMeta(bin.ResolvedOp);

    // Binary ops have exactly 2 parameters: opMeta.Left, opMeta.Right
    // ParamSubject.Parameter is reference-equal to one of them
    if (ReferenceEquals(param, opMeta.Left)) return bin.Left;
    if (ReferenceEquals(param, opMeta.Right)) return bin.Right;
    return null;
}
```

**Function call parameter resolution:**

```csharp
static TypedExpression? ResolveParamInFunctionCall(ParameterMeta param, TypedFunctionCall call)
{
    var overloads = Functions.GetOverloads(call.ResolvedFunction);
    foreach (var overload in overloads)
    {
        for (int i = 0; i < overload.Parameters.Length; i++)
        {
            if (ReferenceEquals(param, overload.Parameters[i]))
                return i < call.Arguments.Length ? call.Arguments[i] : null;
        }
    }
    return null;
}
```

### `GetFieldName(ProofSubject subject, TypedExpression? resolvedExpr = null) → string?`

Extracts the field name from a resolved subject expression. Used by Strategies 3/4 to match guard fields against obligation subjects.

```csharp
static string? GetFieldName(ProofSubject subject, TypedExpression site)
{
    var resolved = ResolveSubject(subject, site);
    return resolved switch
    {
        TypedFieldRef fieldRef => fieldRef.FieldName,
        TypedMemberAccess { Object: TypedFieldRef fieldRef } => fieldRef.FieldName,
        _ => null  // subject is not a simple field reference — cannot match guard
    };
}
```

**Rationale:** Subject resolution is the fundamental operation connecting abstract `ProofSubject` identities to concrete expression nodes. It must be defined here because the spec uses it as a black box everywhere. The reference-equality model is already established in `ProofRequirement.cs` line 16 — this resolution is the mechanical consequence of that design.

**Spec correction required:** Yes — add a `ResolveSubject` pseudocode section to §7 before Strategy 1. Add `GetFieldName` as a companion utility.

**Implementation note:** Both methods are `private static` in `ProofEngine.cs`. The `Operations.GetMeta()` and `Functions.GetOverloads()` calls are already available through the catalog API.

---

## PE-G8: Initial-State Satisfiability Algorithm Underspecified → RESOLVED

**Gap:** The spec says "check whether default field values satisfy constraints" without defining what "check" means, what "default values" are, which fields and constraints are in scope, or what happens with initial-event arguments.

**Resolution:** Full algorithm definition. This is bounded constant folding — no evaluator dependency, no runtime coupling.

### Algorithm: `CheckInitialStateSatisfiability`

**Input:** `SemanticIndex semantics`

**Output:** `ImmutableArray<InitialStateSatisfiabilityResult>`

**Step 1 — Find the initial state:**
```csharp
var initialState = semantics.States.FirstOrDefault(s =>
    s.Modifiers.Contains(ModifierKind.Initial));
if (initialState is null)
    return ImmutableArray<InitialStateSatisfiabilityResult>.Empty;
    // Stateless precepts have no initial state — nothing to check.
```

**Step 2 — Collect applicable constraints:**

Collect all `TypedEnsure` entries anchored to the initial state with `ConstraintKind.StateResident` (the `in` anchor). These are the constraints that must hold while the entity is in the initial state.

```csharp
var initialEnsures = semantics.EnsuresByState.TryGetValue(initialState.Name, out var ensures)
    ? ensures.Where(e => e.Kind == ConstraintKind.StateResident)
    : Enumerable.Empty<TypedEnsure>();
```

**Why only `StateResident` ("`in`")?** `StateEntry` constraints (`to`) fire on transition into the state — but the initial state is entered at creation time, not via a transition. `StateExit` constraints (`from`) are irrelevant at creation. `StateResident` constraints must hold for the entire duration of residency in a state — including the initial moment.

**Step 3 — Build the default value environment:**

For each field in `semantics.Fields`, determine its default value:

| Field Condition | Default Value |
|---|---|
| Has `DefaultExpression` that is `TypedLiteral` | The literal's `Value` |
| Has `DefaultExpression` that is NOT `TypedLiteral` | **Unfoldable** — mark as unknown |
| Has `ComputedExpression` (`IsComputed = true`) | **Unfoldable** — computed fields depend on other fields |
| Is `IsOptional = true` with no default | `null` (not set) |
| Is numeric type with no default | `0` (the CLR default for numeric types) |
| Is `string` type with no default | `""` (empty string) |
| Is `boolean` type with no default | `false` |
| Is collection type with no default | Empty collection (count = 0) |
| Is `date`/`datetime`/`period`/`duration`/`money`/`price`/`quantity` with no default | **Unfoldable** — no meaningful zero value |

```csharp
// The default value environment
Dictionary<string, object?> defaults = new();
HashSet<string> unfoldable = new();

foreach (var field in semantics.Fields)
{
    if (field.DefaultExpression is TypedLiteral lit)
        defaults[field.Name] = lit.Value;
    else if (field.DefaultExpression is not null || field.IsComputed)
        unfoldable.Add(field.Name);
    else if (field.IsOptional)
        defaults[field.Name] = null; // "not set"
    else
        defaults[field.Name] = GetTypeDefault(field.ResolvedType);
        // Returns 0m for numeric, "" for string, false for boolean,
        // empty for collections, or marks unfoldable for complex types.
}
```

**Step 4 — Fold each constraint condition:**

For each initial-state ensure, substitute `TypedFieldRef` nodes in the condition with their default values, then constant-fold.

```csharp
foreach (var ensure in initialEnsures)
{
    var foldResult = ConstantFold(ensure.Condition, defaults, unfoldable);

    if (foldResult == FoldResult.False)
    {
        // The constraint is definitely unsatisfiable with defaults.
        violations.Add(new UnsatisfiedConstraint(
            EnsureIdentity(ensure.Kind, ensure.AnchorState ?? ensure.AnchorEvent, ensureIndex),
            FormatViolationReason(ensure, defaults)));
    }
    else if (foldResult == FoldResult.Unknown)
    {
        // Cannot determine — expression references unfoldable fields or uses
        // non-foldable operations. Conservative: treat as satisfiable (no false negatives).
    }
    // FoldResult.True → constraint is satisfied by defaults, no violation.
}
```

**Step 5 — `ConstantFold` specification:**

Bounded constant folding over `TypedExpression` trees. Supported operations:

| Expression Type | Fold Behavior |
|---|---|
| `TypedLiteral` | Return the literal value |
| `TypedFieldRef` | Substitute from `defaults` map; if field is in `unfoldable`, return `Unknown` |
| `TypedBinaryOp` | Fold both operands; if both are known values, evaluate the operation; otherwise `Unknown` |
| `TypedUnaryOp` | Fold operand; if known, evaluate; otherwise `Unknown` |
| `TypedFunctionCall` | **Not folded** — return `Unknown`. Function calls involve catalog-defined semantics too complex for constant folding. |
| `TypedConditional` | Fold condition; if `true`, fold `ThenBranch`; if `false`, fold `ElseBranch`; otherwise `Unknown` |
| `TypedMemberAccess` | **Not folded** — return `Unknown` |
| `TypedPostfixOp` | Fold operand; if operand is `null`, `is set` = `false`, `is not set` = `true`; otherwise `is set` = `true` |
| Any other | `Unknown` |

**Fold result type:**
```csharp
enum FoldResult { True, False, Unknown }
```

The constant folder operates on `object?` values and uses the same comparison semantics as the Precept expression language (decimal arithmetic, string equality, boolean logic).

**Step 6 — Initial event arguments are NOT considered.**

Initial event arguments are runtime values — they vary per instantiation. The satisfiability check verifies structural satisfiability: "given only declared defaults, can the entity be created in a valid state?" If the answer is "no," the precept has a structural defect regardless of what arguments are provided.

If the initial event's `set` actions assign fields that satisfy constraints, that's a runtime guarantee, not a compile-time one. The proof engine checks compile-time defaults only.

**Step 7 — Guarded ensures are skipped.**

If `ensure.Guard` is not null, the ensure is conditionally applied. The satisfiability check does not evaluate guards — they're runtime conditions. Guarded ensures are treated as vacuously satisfiable for initial-state checking purposes.

**Rationale:** This algorithm is deliberately conservative — it only reports violations it can prove statically. Unfoldable fields, non-literal defaults, function calls, and guarded ensures all resolve to "Unknown" (conservative pass). This ensures zero false positives at the cost of potentially missing some true violations that would require deeper analysis.

**Spec correction required:** Yes — replace the hand-wavy description in §7 "Initial-State Satisfiability" with this full algorithm. Add the `ConstantFold` specification and the default value table.

**Implementation note:** Implement as `private static ImmutableArray<InitialStateSatisfiabilityResult> CheckInitialStateSatisfiability(SemanticIndex semantics)` in `ProofEngine.cs`. The `ConstantFold` method is a recursive `TypedExpression` → `(object? value, bool isKnown)` reducer. Estimated ~80–100 LOC for fold + ~30 LOC for the orchestration.

---

## PE-G9: Collection Safety Ownership → RESOLVED

**Gap:** The spec describes collection non-empty obligations in the proof engine, but the type checker already emits `UnguardedCollectionAccess` (63) and `UnguardedCollectionMutation` (64). `FaultCode.CollectionEmptyOnAccess` and `FaultCode.CollectionEmptyOnMutation` link to the type checker diagnostic codes via `[StaticallyPreventable]`. Ownership is unclear.

**Resolution:** **The type checker owns collection safety diagnostics. The proof engine does NOT create separate collection-empty obligations or emit separate diagnostic codes for them.**

Here is the precise ownership split:

| Concern | Owner | Mechanism |
|---|---|---|
| "Is this collection access guarded?" | **Type Checker** | Emits `UnguardedCollectionAccess` (63) or `UnguardedCollectionMutation` (64) |
| "Can the guard prove the collection is non-empty?" | **Proof Engine** | The proof engine's obligation for `NumericProofRequirement(count > 0)` on `first()`, `last()`, `peek`, `dequeue`, `pop` covers this. The requirement is already stamped by the type checker from catalog metadata. |

**The critical insight:** Collection non-empty requirements are already encoded as `NumericProofRequirement(SelfSubject(CollectionCount), GreaterThan, 0)` in the catalog — see `Types.cs` and `Actions.cs`. These requirements are stamped onto `TypedMemberAccess` and `TypedAction` nodes by the type checker. The proof engine processes them as ordinary numeric obligations through Strategies 1/2/3.

**What the type checker does:** Emits `UnguardedCollectionAccess` when a `.first`, `.last`, or `.peek` access appears without a `count > 0` guard or `notempty` modifier. This is a Warning-level diagnostic.

**What the proof engine does:** Processes the `NumericProofRequirement(count > 0)` obligation through its strategies:
- Strategy 1 (Literal): Collection literal `[1, 2, 3]` — count is known > 0 → discharged
- Strategy 2 (Declaration Attribute): `notempty` modifier → discharged (via `ProofSatisfaction`)
- Strategy 3 (Guard-in-Path): `when count(Items) > 0` → discharged

If the obligation is unresolved, the proof engine emits the corresponding proof-stage diagnostic for the specific requirement violation (e.g., `DivisionByZero` for division, `SqrtOfNegative` for sqrt). For collection non-empty obligations, the unresolved case maps to the existing `FaultCode.CollectionEmptyOnAccess` / `CollectionEmptyOnMutation` fault codes, which are already linked to the type checker's diagnostics via `[StaticallyPreventable]`.

**No new diagnostic code is needed.** The type checker's `UnguardedCollectionAccess` (63) and `UnguardedCollectionMutation` (64) are the prevention diagnostics for `FaultCode.CollectionEmptyOnAccess` (9) and `FaultCode.CollectionEmptyOnMutation` (10). The proof engine discharges the numeric obligation — if it cannot, the type checker's diagnostic already covers the authoring-time warning.

**Spec correction required:** Yes — update §7 "Collection Non-Empty Proof" to clarify that collection non-empty obligations are ordinary `NumericProofRequirement` obligations processed through the standard strategies. The type checker owns the diagnostic emission. The proof engine discharges the obligation. Remove any implication that the proof engine needs its own collection-specific diagnostic code.

**Implementation note:** No additional diagnostic codes needed. The proof engine processes collection obligations as `NumericProofRequirement` — no special-casing. If the obligation is unresolved after all strategies, the `FaultSiteLink` is created with `FaultCode.CollectionEmptyOnAccess` or `CollectionEmptyOnMutation` (determined by looking up the `[StaticallyPreventable]` chain from the diagnostic code already emitted by the type checker).

---

## PE-G10: Guard Decomposition Scope (ExtractGuardConstraints) → RESOLVED

**Gap:** Strategy 3 calls `ExtractGuardConstraints(row.Guard)` but the decomposition rules for compound, negated, and complex guards are unspecified.

**Resolution:** Full decomposition specification:

### `ExtractGuardConstraints(TypedExpression guard) → ImmutableArray<GuardConstraint>`

**GuardConstraint record:**

```csharp
// Internal to ProofEngine — not part of public API
record GuardConstraint(
    string Field,                    // field name
    OperatorKind Comparison,         // comparison operator
    decimal? Value,                  // literal threshold (null for presence checks)
    bool IsPresenceCheck             // true for "field is set" patterns
);
```

**Decomposition rules:**

| Guard Pattern | Decomposition |
|---|---|
| `TypedBinaryOp(And, left, right)` | Recurse into both `left` and `right` — each produces independent constraints. All AND-conjuncts contribute constraints because ALL conjuncts must be true when the guard passes. |
| `TypedBinaryOp(Or, left, right)` | **Do NOT decompose.** Neither disjunct is guaranteed true when the guard passes — the guard only guarantees one of them. Return empty for this branch. |
| `TypedBinaryOp(comparison, TypedFieldRef, TypedLiteral)` | Yield one `GuardConstraint(field.FieldName, comparison, literal.Value)` |
| `TypedBinaryOp(comparison, TypedLiteral, TypedFieldRef)` | Yield one `GuardConstraint(field.FieldName, InvertOp(comparison), literal.Value)`. Example: `0 < X` → `GuardConstraint(X, GreaterThan, 0)` |
| `TypedBinaryOp(NotEquals, TypedFieldRef, TypedLiteral)` | Yield `GuardConstraint(field, NotEquals, literal.Value)` |
| `TypedPostfixOp(TypedFieldRef, IsNegated: false)` | Yield `GuardConstraint(field, _, null, IsPresenceCheck: true)` — "`field is set`" |
| `TypedPostfixOp(TypedFieldRef, IsNegated: true)` | **Do NOT yield.** "`field is not set`" does not establish a positive constraint usable for proof. |
| `TypedFunctionCall(Count, [TypedFieldRef])` embedded in comparison | Recognize `count(collection) > 0` pattern: yield `GuardConstraint(field, GreaterThan, 0)` |
| `TypedMemberAccess(TypedFieldRef, CountAccessor)` embedded in comparison | Recognize `collection.count > 0` pattern: same as above |
| `TypedUnaryOp(Not, inner)` | Attempt to invert simple comparisons: `not (X == 0)` → `GuardConstraint(X, NotEquals, 0)`. For complex inner expressions, return empty — do not decompose. |
| `TypedConditional` | **Do NOT decompose.** Conditional expressions in guards are too complex for pattern matching. |
| `TypedQuantifier` | **Do NOT decompose.** Quantifiers are beyond the bounded strategy scope. |
| Any other expression | Return empty — not a recognized constraint form. |

**Operator inversion table** (for `InvertOp`, used when the literal is on the left):

| Original | Inverted |
|---|---|
| `GreaterThan` | `LessThan` |
| `LessThan` | `GreaterThan` |
| `GreaterThanOrEqual` | `LessThanOrEqual` |
| `LessThanOrEqual` | `GreaterThanOrEqual` |
| `Equals` | `Equals` (symmetric) |
| `NotEquals` | `NotEquals` (symmetric) |

**Negation inversion table** (for `not (X op Y)` → `X inverted_op Y`):

| Original | Negated |
|---|---|
| `Equals` | `NotEquals` |
| `NotEquals` | `Equals` |
| `GreaterThan` | `LessThanOrEqual` |
| `LessThanOrEqual` | `GreaterThan` |
| `LessThan` | `GreaterThanOrEqual` |
| `GreaterThanOrEqual` | `LessThan` |

**Rationale:** AND-decomposition is safe because all conjuncts are true when the guard passes. OR-decomposition is unsafe because only one disjunct is guaranteed. Negation inversion is safe for simple comparisons but not for complex expressions. This keeps the decomposer bounded and predictable — Strategy 3 may miss some provable cases (where a disjunct or complex expression would suffice), but it never produces false proofs.

**Spec correction required:** Yes — add the decomposition rules table and the operator inversion tables to §7 Strategy 3. Replace the hand-wavy "parse the guard expression into simple constraint forms" with this explicit specification.

**Implementation note:** Implement as `private static ImmutableArray<GuardConstraint> ExtractGuardConstraints(TypedExpression guard)` in `ProofEngine.cs`. Recursive, returns empty array for unrecognized patterns. Estimated ~60 LOC.

---

## PE-G11: Builder Contract (No Builder Spec Exists) → RESOLVED

**Gap:** The spec references "Precept Builder" and `precept-builder.md` three times, but no builder spec exists. Original analysis said "accept this gap for now — the builder is a future stage." Shane's directive overrides this.

**Resolution:** Define the Precept Builder's proof-consumption contract now. The builder itself doesn't exist yet, but the proof engine's output shapes must be designed for a concrete consumer, not a hypothetical one.

### Builder Proof-Consumption Contract

The Precept Builder (pipeline stage 7, after ProofEngine) consumes `ProofLedger` to produce two runtime artifacts:

**1. FaultSiteDescriptor Backstops (from `ProofLedger.FaultSiteLinks`)**

For each `FaultSiteLink` (one per unresolved obligation):

```csharp
// Builder transforms:
//   FaultSiteLink(obligation, faultCode, diagnosticCode, SourceSpan site)
// Into:
//   FaultSiteDescriptor(faultCode, diagnosticCode, site.StartLine)
//
// The FaultSiteDescriptor is stamped onto the runtime opcode at the expression site.
// At runtime, the evaluator checks the backstop before executing the operation:
//   if (backstop.ShouldCheck && !condition) → throw FaultException(backstop.Code)
```

**Builder rule:** Every `Unresolved` obligation MUST produce a `FaultSiteDescriptor`. No exceptions. If the builder encounters an `Unresolved` obligation without a valid `FaultCode` mapping, that is a Roslyn-analyzer-enforced compile error in the Precept source itself (PRECEPT0001/PRECEPT0002 guarantee the mapping exists).

**2. ConstraintInfluenceMap (from `ProofLedger.ConstraintInfluence`)**

```csharp
// Builder transforms:
//   ImmutableArray<ConstraintInfluenceEntry> → ConstraintInfluenceMap
//
// ConstraintInfluenceMap shape (runtime artifact):
public sealed record ConstraintInfluenceMap(
    FrozenDictionary<string, ImmutableArray<ConstraintIdentity>> FieldToConstraints,
    FrozenDictionary<(string EventName, string ArgName), ImmutableArray<ConstraintIdentity>> ArgToConstraints
);
//
// Inverted index: given a field or arg that changed, which constraints might be affected?
// Used by the evaluator to determine which constraints to re-check after a field mutation.
// Used by AI agents for causal reasoning: "changing field X affects constraints Y and Z."
```

**3. InitialStateSatisfiabilityResults (from `ProofLedger.InitialStateResults`)**

The builder reads `InitialStateResults` and, if any `IsSatisfiable == false`, prevents the production of a runtime `Precept` model. An unsatisfiable initial state is a structural defect — the precept cannot be instantiated.

```csharp
// Builder rule:
if (proof.InitialStateResults.Any(r => !r.IsSatisfiable))
{
    // Do NOT produce runtime Precept model.
    // The diagnostics are already in ProofLedger.Diagnostics.
    // Compilation.HasErrors will be true.
}
```

**4. ProofLedger.Obligations (diagnostic-only, not consumed by builder)**

The builder does not consume the full obligations list. Obligations are diagnostic artifacts — they exist for the language server, MCP output, and developer tooling. The builder only needs `FaultSiteLinks` (unresolved obligations with fault mappings) and `ConstraintInfluence`.

### Builder Contract Summary

| ProofLedger Field | Builder Consumption | Runtime Artifact |
|---|---|---|
| `Obligations` | Not consumed by builder | Diagnostic-only (LS, MCP) |
| `FaultSiteLinks` | Transforms to `FaultSiteDescriptor` per opcode | Runtime backstops |
| `ConstraintInfluence` | Inverts to `ConstraintInfluenceMap` | Runtime constraint re-check index |
| `InitialStateResults` | Gate: blocks runtime model if unsatisfiable | No runtime artifact — compile-time gate |
| `Diagnostics` | Merged into `Compilation.Diagnostics` | Authoring-time feedback |

**Rationale:** Defining this contract now — even without a builder implementation — ensures the proof engine's output shapes are designed for real consumption, not speculation. The three consumption patterns (backstop planting, influence inversion, satisfiability gating) are architecturally stable — they won't change when the builder is implemented. The builder is a mechanical transform, not a design decision.

**Spec correction required:** Yes — update §8 "Downstream Consumers" to include this contract. Replace "See `precept-builder.md §Pass 4`" references with the inline contract above. Note that `precept-builder.md` does not yet exist and will reference this contract when authored.

**Implementation note:** No code changes for the proof engine — the output shapes are already defined in PE-G3. The builder will be implemented as a separate pipeline stage. The `ConstraintInfluenceMap` type should be defined in `src/Precept/Runtime/` when the builder is built — it is a runtime artifact, not a pipeline artifact.

---

## PE-G12: Diagnostic Message Formatting → RESOLVED

**Gap:** The pseudocode calls `CreateDiagnostic(obligation)` without specifying how template parameters `{0}`, `{1}`, etc. are populated.

**Resolution:** This gap was originally ADVISORY and the existing diagnostic message templates already provide the answer through their registered templates in `Diagnostics.cs`. This is a documentation gap, not a design gap. Here is the formatting table:

| DiagnosticCode | Template | `{0}` | `{1}` | `{2}` |
|---|---|---|---|---|
| `DivisionByZero` (83) | `"Division by zero: '{0}' can be zero when {1}"` | Field name of the divisor (from `GetFieldName(requirement.Subject, obligation.Site)`) | Context description: transition row identity (e.g., "event 'Calculate' in state 'Active'") | — |
| `SqrtOfNegative` (84) | `"sqrt() requires a non-negative value, but '{0}' can be negative when {1}"` | Field name of the operand | Context description (same format) | — |
| `UnsatisfiableGuard` (82) | `"The condition '{0}' on event '{1}' can never be true when {2} — this transition will never fire"` | Guard expression text (from `obligation.Context` → `TransitionRowContext.Row.Guard.Span` → source text) | Event name (from `Row.EventName`) | State name or "any state" (from `Row.FromState ?? "any state"`) |

**Context description format:** `"event '{EventName}' in state '{FromState}'"` for transition rows. `"state hook '{Scope}' on '{StateName}'"` for state hooks. `"event handler '{EventName}'"` for event handlers. `"rule at index {RuleIndex}"` for rules. `"ensure in {AnchorName}"` for ensures.

**Field name resolution for diagnostics:** Use `GetFieldName(requirement.Subject, obligation.Site)`. If the subject resolves to a field, use the field name. If it resolves to an expression, use the expression span's source text. If resolution fails, use `"<unknown>"`.

**Spec correction required:** Yes — add the formatting table to §9 after the error accumulation section. This makes diagnostic output testable.

**Implementation note:** The `CreateDiagnostic` method resolves template parameters from `obligation.Requirement`, `obligation.Context`, and `obligation.Site`. No new diagnostic codes needed — the three proof-stage codes (82, 83, 84) cover the numeric obligation failure cases. For future requirement kinds (dimension, modifier, qualifier-compatibility), new diagnostic codes will be allocated when those strategies emit diagnostics.

---

## PE-G13: Error Propagation from Upstream Stages → RESOLVED

**Gap:** The spec doesn't say what happens when the proof engine encounters `TypedErrorExpression` nodes or upstream-error conditions.

**Resolution:** The spec already partially addresses this in §9 "Upstream Error Handling" (line 1196–1198):

> "If the `SemanticIndex` contains `TypedErrorExpression` nodes (from type checker error recovery), the proof engine skips them — they have no `ProofRequirements` to process."

This is correct but incomplete. Full rule set:

1. **`TypedErrorExpression` nodes:** Skip entirely during Pass 1 obligation walk. `TypedErrorExpression` has `ResultType = TypeKind.Error` and no `ProofRequirements`. If encountered as a child of an obligation-bearing node (e.g., `TypedBinaryOp` with one `TypedErrorExpression` operand), the obligation is still instantiated but subject resolution will fail (returning `null`), causing all strategies to return `false`. The obligation remains `Unresolved` — but the type checker already emitted the root-cause diagnostic. **No additional proof diagnostic is emitted for error-tainted obligations.** Suppress diagnostic emission when the obligation's site or resolved subject contains `TypedErrorExpression`.

2. **Upstream GraphAnalyzer errors:** The proof engine runs unconditionally — see `Compiler.cs` line 17, every stage runs regardless of upstream errors. The proof engine should NOT short-circuit. It processes whatever is available and produces a complete `ProofLedger`. If the `StateGraph` has structural violations, those are the graph analyzer's diagnostics. The proof engine reads `ProofForwardingFact` entries from the graph — if those entries indicate structural problems, the proof engine incorporates them (suppressing obligations on unreachable transitions per the existing spec).

3. **Empty `SemanticIndex`:** Produce an empty `ProofLedger` with no obligations, no fault links, no constraint influence entries, no initial-state results, and no diagnostics. This is already specified in §9 error accumulation table.

**Suppression rule for error-tainted obligations:**

```csharp
// After subject resolution, before diagnostic emission:
if (ContainsErrorExpression(obligation.Site))
{
    // Don't emit a proof diagnostic — the type checker already reported the error.
    // Still record the obligation as Unresolved for completeness, but skip diagnostic + fault link.
    obligation = obligation with { Disposition = ProofDisposition.Unresolved };
    // Do NOT call CreateDiagnostic or CreateFaultSiteLink.
    continue;
}
```

**Helper:**
```csharp
static bool ContainsErrorExpression(TypedExpression expr) => expr switch
{
    TypedErrorExpression => true,
    TypedBinaryOp bin => ContainsErrorExpression(bin.Left) || ContainsErrorExpression(bin.Right),
    TypedUnaryOp un => ContainsErrorExpression(un.Operand),
    TypedFunctionCall call => call.Arguments.Any(ContainsErrorExpression),
    TypedMemberAccess ma => ContainsErrorExpression(ma.Object),
    TypedConditional cond => ContainsErrorExpression(cond.Condition) ||
                             ContainsErrorExpression(cond.ThenBranch) ||
                             ContainsErrorExpression(cond.ElseBranch),
    _ => false
};
```

**Rationale:** Error-tainted expression trees will produce proof obligations that can never be discharged (the subject is broken). Emitting proof diagnostics for these would be noise — the author needs to fix the type error first. Suppression prevents cascading diagnostics. This matches the pattern in the type checker, where `TypedErrorExpression` prevents further analysis of child expressions.

**Spec correction required:** Yes — expand §9 "Upstream Error Handling" with the error-tainted obligation suppression rule and the `ContainsErrorExpression` helper.

**Implementation note:** Add the `ContainsErrorExpression` check in the Pass 2 discharge loop, between strategy evaluation and diagnostic emission.

---

## PE-G14: Guard Pattern Table Incomplete (GuardRelationImpliesObligation) → RESOLVED

**Gap:** Strategy 4's `GuardRelationImpliesObligation` is described as a pattern match on triples but only three examples are given. The complete triple set is not enumerated.

**Resolution:** Exhaustive triple table. Strategy 4 handles field-to-field relational guards. The obligation is about an arithmetic expression combining both fields.

### Complete `GuardRelationImpliesObligation` Triple Table

The function takes `(guard.Op, expr.Op, requirement)` and returns whether the guard's established relation implies the obligation.

**Guard form:** `A guard.Op B` (two field references, no literals)
**Expression form:** `A expr.Op B` or `B expr.Op A`
**Requirement form:** `NumericProofRequirement(subject, req.Op, req.Threshold)`

| Guard | Expression | Requirement | Result | Reasoning |
|---|---|---|---|---|
| `A > B` | `A - B` | `result > 0` | ✅ | A > B → A - B > 0 |
| `A > B` | `A - B` | `result >= 0` | ✅ | A > B → A - B > 0 ≥ 0 |
| `A > B` | `A - B` | `result != 0` | ✅ | A > B → A - B > 0 ≠ 0 |
| `A >= B` | `A - B` | `result >= 0` | ✅ | A >= B → A - B >= 0 |
| `A >= B` | `A - B` | `result != 0` | ❌ | A >= B allows A == B → result == 0 |
| `A > B` | `B / A` | `divisor != 0` | ❌ | A > B doesn't prove A != 0 (both could be negative) |
| `A > 0` | — | — | ❌ | **This is a Strategy 3 case** — A > literal, not A > field |
| `A != B` | `A - B` | `result != 0` | ✅ | A != B → A - B != 0 |
| `A < B` | `B - A` | `result > 0` | ✅ | A < B → B - A > 0 |
| `A < B` | `B - A` | `result >= 0` | ✅ | A < B → B - A > 0 ≥ 0 |
| `A < B` | `B - A` | `result != 0` | ✅ | A < B → B - A > 0 ≠ 0 |
| `A <= B` | `B - A` | `result >= 0` | ✅ | A <= B → B - A >= 0 |

**Implementation pattern:**

```csharp
static bool GuardRelationImpliesObligation(
    FieldToFieldConstraint guard,
    TypedBinaryOp expr,
    NumericProofRequirement requirement)
{
    var (gLeft, gOp, gRight) = (guard.LeftField, guard.Comparison, guard.RightField);
    var exprLeft = GetFieldName(expr.Left);
    var exprRight = GetFieldName(expr.Right);

    if (exprLeft is null || exprRight is null) return false;

    // Normalize: check if the expression involves the same two fields as the guard
    bool sameOrder = exprLeft == gLeft && exprRight == gRight;
    bool reversed = exprLeft == gRight && exprRight == gLeft;
    if (!sameOrder && !reversed) return false;

    // Only handle subtraction (the primary use case for flow narrowing)
    if (expr.ResolvedOp is not OperationKind subtraction
        || !IsSubtractionOp(subtraction)) return false;

    // Determine the effective guard relation on (exprLeft - exprRight)
    var effectiveOp = sameOrder ? gOp : InvertOp(gOp);

    // Check if the effective relation implies the requirement on the result
    return (effectiveOp, requirement.Comparison) switch
    {
        // guard: left > right → (left - right) > 0
        (OperatorKind.GreaterThan, OperatorKind.GreaterThan) when requirement.Threshold == 0 => true,
        (OperatorKind.GreaterThan, OperatorKind.GreaterThanOrEqual) when requirement.Threshold <= 0 => true,
        (OperatorKind.GreaterThan, OperatorKind.NotEquals) when requirement.Threshold == 0 => true,

        // guard: left >= right → (left - right) >= 0
        (OperatorKind.GreaterThanOrEqual, OperatorKind.GreaterThanOrEqual) when requirement.Threshold <= 0 => true,

        // guard: left < right → (left - right) < 0, equivalent to (right - left) > 0
        (OperatorKind.LessThan, OperatorKind.LessThan) when requirement.Threshold == 0 => true,
        (OperatorKind.LessThan, OperatorKind.NotEquals) when requirement.Threshold == 0 => true,

        // guard: left <= right → (left - right) <= 0
        (OperatorKind.LessThanOrEqual, OperatorKind.LessThanOrEqual) when requirement.Threshold <= 0 => true,

        // guard: left != right → (left - right) != 0
        (OperatorKind.NotEquals, OperatorKind.NotEquals) when requirement.Threshold == 0 => true,

        _ => false
    };
}
```

**Scope limitation:** Strategy 4 ONLY handles subtraction expressions (`A - B`) with field-to-field guards. Division (`B / A`) is explicitly NOT covered — proving `A != 0` from `A > B` requires knowledge of B's sign, which is beyond bounded flow narrowing. This is a deliberate scope limit. If an author needs `B / A` proven safe, they add a direct guard (`when A != 0`) which Strategy 3 handles.

**Rationale:** The exhaustive table makes Strategy 4 fully implementable and testable. The scope is deliberately narrow — only subtraction with field-to-field guards. This covers the realistic use case (computing differences between two constrained fields) without attempting general relational reasoning.

**Spec correction required:** Yes — replace the three example triples in §7 Strategy 4 with this exhaustive table. Add the scope limitation note.

**Implementation note:** The `FieldToFieldConstraint` type is the Strategy 4 counterpart to `GuardConstraint`. Add `ExtractFieldToFieldConstraints` as a companion to `ExtractGuardConstraints`:

```csharp
record FieldToFieldConstraint(string LeftField, OperatorKind Comparison, string RightField);
```

---

## PE-G15: Stateless Precept Handling → RESOLVED

**Gap:** The spec doesn't address whether the proof engine runs for stateless precepts or how it handles event handlers (which have no guards).

**Resolution:** The proof engine runs for ALL precepts, including stateless ones. Here is the complete stateless precept handling specification:

**Detection:** A stateless precept has `semantics.States.IsEmpty` (no state declarations). It uses `EventHandlers` instead of `TransitionRows`.

**Walk targets for stateless precepts:**

| Walk Target | Present? | Notes |
|---|---|---|
| `TransitionRows` | Empty | No state machine → no transition rows |
| `EventHandlers` | Non-empty | Actions carry `ProofRequirements` |
| `StateHooks` | Empty | No states → no state hooks |
| `Rules` | May be non-empty | Global invariant constraints apply to stateless precepts |
| `Ensures` | Empty | No states → no state-anchored constraints |
| `Fields` | Non-empty | Fields exist; defaults relevant for computed expressions |

**Strategy applicability for stateless precepts:**

| Strategy | Applies? | Reason |
|---|---|---|
| Strategy 1 (Literal) | ✅ | Literal arguments in event handler actions |
| Strategy 2 (Declaration Attribute) | ✅ | Field modifiers apply to fields regardless of state machine |
| Strategy 3 (Guard-in-Path) | ❌ | `TypedEventHandler` has no `Guard` field |
| Strategy 4 (Flow Narrowing) | ❌ | No guards → no relational constraints |
| Strategy 5 (Qualifier Compatibility) | ✅ | Qualifier comparison is field-level, not state-dependent |

**Initial-state satisfiability for stateless precepts:** Skipped — no initial state. `CheckInitialStateSatisfiability` returns empty when `States.IsEmpty`.

**Constraint influence for stateless precepts:** Rules (if any) are processed normally. `ConstraintRefs` from the SemanticIndex are projected to `ConstraintInfluenceEntry` regardless of whether the precept is stateful.

**Rationale:** Stateless precepts are simpler — fewer walk targets, fewer applicable strategies. But proof obligations on event handler actions are real and must not be silently skipped. Division by zero in an event handler action is just as dangerous as in a transition row action.

**Spec correction required:** Yes — add a "Stateless Precept Handling" subsection to §7, after "ProofForwardingFact Consumption Contract." Include the walk-target and strategy-applicability tables above.

**Implementation note:** No special-casing needed in the proof engine's core loop — `EventHandlers` is already a walk target. The strategy dispatch naturally skips Strategies 3/4 for `EventHandlerContext` obligations because there is no guard to read. The only implementation note is to ensure `CheckInitialStateSatisfiability` gracefully handles empty `States`.

---

## PE-G16: Site Identity Matching Semantics → RESOLVED

**Gap:** The spec says the builder matches against `ProofLedger.FaultSiteLinks` by `ProofObligation.Site` identity, but doesn't clarify whether this is reference or structural equality. C# records use structural equality.

**Resolution:** **Reference identity. The proof engine stores the same `TypedExpression` object reference that exists in the `SemanticIndex`.** No copies are made.

**The proof chain:**
1. The type checker creates `TypedBinaryOp`, `TypedFunctionCall`, etc. — these live in the `SemanticIndex`.
2. The proof engine reads these same objects during Pass 1 and stores them in `ProofObligation.Site`.
3. The builder walks the `SemanticIndex` (the same expression trees) and, for each expression, checks whether a `FaultSiteLink` exists for it.

**Matching mechanism:** The builder builds a `HashSet<TypedExpression>` from `ProofLedger.FaultSiteLinks.Select(l => l.Obligation.Site)`, using `ReferenceEqualityComparer.Instance`. When walking expressions during opcode generation, it checks `faultSites.Contains(expr)` using reference equality.

```csharp
// Builder internals:
var faultSites = new HashSet<TypedExpression>(
    proof.FaultSiteLinks.Select(l => l.Obligation.Site),
    ReferenceEqualityComparer.Instance);

// During expression walk:
if (faultSites.TryGetValue(expr, out var matched))
{
    // Stamp FaultSiteDescriptor on this opcode
}
```

**Why reference equality, not structural?** Structural equality on `TypedExpression` records would match any two expressions with identical structure — creating false positives. Consider two identical `field / Divisor` expressions in different transition rows: they are structurally equal but semantically distinct obligations. One might be proved (guarded) while the other is not. Reference identity ensures exact correspondence.

**Invariant:** The proof engine MUST NOT use `with { ... }` on `obligation.Site` or create new `TypedExpression` instances — it must preserve the original object reference. The `ProofObligation` itself can use `with { ... }` (it's a separate record), but the `Site` field must point to the original `SemanticIndex` expression.

**Spec correction required:** Yes — update the CC#6 resolution box in §5 to explicitly state "reference equality via `ReferenceEqualityComparer.Instance`" for site matching. Add the invariant about no-copy.

**Implementation note:** This is a constraint on the implementer, not additional code. Document it as a code comment in `ProofEngine.cs` at the obligation instantiation point.

---

## PE-G17: OperatorKind Enum Name Verification → RESOLVED

**Gap:** The spec pseudocode uses `OperatorKind.NotEquals`, `OperatorKind.GreaterThan`, etc. — need to verify these match the actual enum.

**Resolution:** **Verified. All names match exactly.**

Source (`src/Precept/Language/OperatorKind.cs`):
```csharp
NotEquals                 =  5,
GreaterThan               =  9,
GreaterThanOrEqual        = 11,
LessThan                  =  8,
LessThanOrEqual           = 10,
```

The spec pseudocode uses: `OperatorKind.NotEquals`, `OperatorKind.GreaterThan`, `OperatorKind.GreaterThanOrEqual`, `OperatorKind.LessThan`, `OperatorKind.LessThanOrEqual`. All match the source enum member names exactly.

**Spec correction required:** No — the spec is correct.

**Implementation note:** No action needed. This was a verification-only gap.

---

## PE-G18: Diagnostic Cross-Reference → RESOLVED

**Gap:** The spec says "accumulate diagnostics without abandoning" but doesn't cite the canonical principle by name or document location. Other pipeline stage docs reference `diagnostic-system.md §Error Accumulation`.

**Resolution:** The `diagnostic-system.md` does not have a section literally titled "Error Accumulation" — the principle is described in the "Design Rationale and Decisions" section (§ "Prevention applies inward"). However, the principle is well-established across all pipeline stages:

> Every stage runs unconditionally. Diagnostics accumulate. No stage short-circuits on upstream errors.

This is visible in `Compiler.cs` — every stage runs sequentially regardless of prior-stage diagnostics, and all diagnostics are merged into `Compilation.Diagnostics`.

**Cross-reference to add:** The proof engine spec should cite:
- `docs/compiler/diagnostic-system.md` — for the diagnostic infrastructure contract (DiagnosticCode enum, DiagnosticMeta exhaustive switch, Severity levels, FixHint, RelatedCodes, PreventsFault)
- `src/Precept/Compiler.cs` — for the unconditional pipeline execution pattern
- `docs/compiler/diagnostic-system.md § FaultCode → DiagnosticCode Chain` — for the `[StaticallyPreventable]` contract that the proof/fault chain relies on

**Spec correction required:** Yes — add cross-references to §9 "Error Accumulation." Update to: "The proof engine follows Precept's error-accumulation pipeline contract (see `diagnostic-system.md`): every stage runs unconditionally, diagnostics accumulate without abandoning the analysis pass."

**Implementation note:** Doc-only change. No code impact.

---

## Cross-Stage Seam Clarifications (from gap analysis §Cross-Stage Seam Issues)

Three seam items were identified in the original analysis. Resolving them here for completeness:

### Seam 2: EventCoverageFact Consumption

**Clarification:** The EventCoverageFact consumption described in the spec (line 1108) is **structural recording, not an active proof check.** The proof engine records coverage facts in the ledger for downstream consumption (builder, MCP, LS). It does NOT perform guard completeness analysis — that would require SMT-style reasoning about whether guards on competing transition rows partition the value space, which is outside the bounded strategy scope.

**Spec correction:** Update §7 ProofForwardingFact consumption table to clarify: "EventCoverageFact: Records structural coverage facts for downstream consumption. The proof engine does NOT analyze guard completeness (value-space partitioning). Guard completeness would require solver-grade analysis; coverage facts are structural metadata only."

### Seam 3: DominancePathFact Redundancy

**Clarification:** The GraphAnalyzer already emits `RequiredStateDoesNotDominateTerminal` (111). The proof engine should NOT emit duplicate diagnostics. It records the `DominancePathFact` in the ledger for structural completeness, but suppresses diagnostic emission for facts the graph analyzer already reported.

**Spec correction:** Update §7 ProofForwardingFact consumption table: "DominancePathFact: Records in the proof ledger for structural completeness. No additional diagnostic — the graph analyzer emits `RequiredStateDoesNotDominateTerminal` (111). Redundant diagnostics are suppressed."

### Seam 5: FaultSiteAnnotation vs FaultSiteDescriptor

Already resolved in PE-G3 analysis. `FaultSiteAnnotation` is the compile-time artifact (carries `SourceSpan`). `FaultSiteDescriptor` is the runtime artifact (carries `int SourceLine`). The builder transforms the former into the latter.

---

## New Diagnostic Codes Needed

Based on the resolved gaps, the following new diagnostic codes are needed for requirement kinds beyond numeric:

| Code | Name | Stage | Severity | Template | PreventsFault |
|---|---|---|---|---|---|
| 96 | `UnprovedModifierRequirement` | Proof | Error | `"Field '{0}' must have modifier '{1}' but it is not declared{2}"` | — |
| 97 | `UnprovedDimensionRequirement` | Proof | Error | `"Operand '{0}' requires {1} dimension but has {2}{3}"` | — |
| 98 | `UnprovedQualifierCompatibility` | Proof | Error | `"Operands '{0}' and '{1}' have incompatible {2} qualifiers{3}"` | — |
| 99 | `UnsatisfiableInitialState` | Proof | Error | `"Initial state '{0}' cannot be satisfied: constraint '{1}' fails with default values"` | — |

**Template parameters:**
- `UnprovedModifierRequirement`: `{0}` = field name, `{1}` = required modifier name, `{2}` = context description
- `UnprovedDimensionRequirement`: `{0}` = operand field name, `{1}` = required dimension, `{2}` = actual dimension or "unknown", `{3}` = context description
- `UnprovedQualifierCompatibility`: `{0}` = left operand, `{1}` = right operand, `{2}` = qualifier axis name, `{3}` = context description
- `UnsatisfiableInitialState`: `{0}` = initial state name, `{1}` = constraint description

**Note:** Code 96–99 are provisional allocations based on the current `DiagnosticCode.cs` numbering (95 is the highest type-stage code). The implementer should verify no conflicts exist at implementation time and adjust if needed. Codes 85–95 are type-stage codes; 96+ continues the proof-stage range.

**Wait — numbering conflict check:** Codes 82–84 are proof stage. Codes 85–95 are type stage (choice, lifecycle). The proof engine should use codes starting after the current maximum. Since 95 is the highest allocated code, codes 96–99 are correct for new proof-stage diagnostics.

**Spec correction required:** Yes — add these diagnostic codes to the "Diagnostic Catalog Status" section and to the §7 strategy discharge pseudocode where applicable.

---

## Summary of All Spec Corrections Required

| Gap | Correction |
|---|---|
| PE-G4 | Replace `AllTypedExpressions` with explicit walk targets |
| PE-G5 | Align `ConstraintIdentity` shapes with source |
| PE-G6 | Add `ObligationContext` DU; add `Context` to `ProofObligation`; update Strategy 3/4 pseudocode |
| PE-G7 | Add `ResolveSubject` and `GetFieldName` pseudocode |
| PE-G8 | Replace hand-wavy satisfiability with full algorithm |
| PE-G9 | Clarify collection safety ownership (type checker owns diagnostics) |
| PE-G10 | Add decomposition rules table and operator inversion tables |
| PE-G11 | Add builder proof-consumption contract |
| PE-G12 | Add diagnostic message formatting table |
| PE-G13 | Expand upstream error handling with error-tainted suppression |
| PE-G14 | Add exhaustive guard relation triple table |
| PE-G15 | Add stateless precept handling subsection |
| PE-G16 | Add reference equality specification for site matching |
| PE-G17 | No correction needed — verified |
| PE-G18 | Add diagnostic-system.md cross-reference |
| Seam 2 | Clarify EventCoverageFact is structural, not active proof |
| Seam 3 | Clarify DominancePathFact suppresses duplicate diagnostics |
| New | Add 4 new diagnostic codes (96–99) |
