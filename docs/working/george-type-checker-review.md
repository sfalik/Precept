# George's Review: Type Checker Design Analysis

**Reviewer:** George (Runtime Dev)
**Date:** 2026-05-02T15:48:45.383-04:00
**Source doc:** `docs/working/type-checker-design-analysis.md` (891 lines, by Frank)

---

## Summary Verdict

Frank's design is architecturally sound and well-grounded in catalog-first principles. The 2-pass / 3-sub-pass model is implementable, the SemanticIndex shape is close to right, and the catalog-gap analysis is mostly correct. However, there are **six concrete problems** that would bite me mid-implementation if I didn't catch them now: the Operations catalog lookup API already exists under different names (and returns arrays, not single entries), five AST node types are missing from the Resolve pseudocode, the SemanticIndex record shape needs to be committed *before* Slice 2 (not at Slice 10), Gap 5 is already closed (GAP-032 landed 2026-05-02), and Frank doesn't address the `[HandlesCatalogMember]` stub migration that PRECEPT0019 enforces.

---

## 1. Implementability ✅ / ⚠️ Concerns

### Overall: implementable, but the architecture description is slightly off

The 2-pass, 3-sub-pass model is the right structure. Pass 1 building the symbol tables before Pass 2 resolves expressions is a hard requirement — you can't resolve `amount + fee` without first knowing that `amount` and `fee` are registered fields. The sub-pass breakdown (2a: expression resolution, 2b: declaration normalization, 2c: structural validation) matches the dependency graph correctly.

**What Frank gets right:**
- The recursive `Resolve(expr, expectedType)` function pattern is the correct core design. The `expectedType` parameter for top-down context propagation is essential for numeric literal resolution.
- ErrorType propagation ("any op with ErrorType → ErrorType") is explicitly designed in and will prevent diagnostic cascades.
- The pass architecture avoids requiring forward references — fields are registered before transition rows check them.

**Concern: "single-pass semantic resolution" in the existing `type-checker.md` vs "two-pass" in Frank's doc**

`docs/compiler/type-checker.md § Processing Model` currently describes "single-pass semantic resolution ... fields first, states second, events and args third, transition rows fourth, constraints fifth." Frank's two-pass architecture is *correct* — but it contradicts the existing doc. The single-pass description in the stub doc was written before the design was fully analyzed. Frank is right; the stub doc needs updating when this ships.

**Concern: implicit mutable working state not designed**

Frank's architecture describes what passes produce but not what the *mutable working context* looks like during the check. Something like:

```csharp
private sealed class CheckContext
{
    public Dictionary<string, TypedField> FieldTable;
    public Dictionary<string, TypedState> StateTable;
    public Dictionary<string, TypedEvent> EventTable;
    // current event args (for transition row scope):
    public IReadOnlyDictionary<string, TypedArg>? CurrentEventArgs;
    public List<Diagnostic> Diagnostics;
}
```

This needs to be designed explicitly before Slice 1, because every slice adds to or reads from it. Frank leaves this implicit. It's not complicated but it needs to be written down before the first line of code.

---

## 2. SemanticIndex Shape ⚠️ Concerns

### TypedField, TypedState, TypedEvent, TypedArg — ✅ correct

These shapes align with `docs/compiler-and-runtime-design.md §6` exactly. The `ImmutableDictionary<string, TypedField>` for symbol tables, `ImmutableArray<...>` for ordered inventories — correct. GraphAnalyzer needs to key transitions by (state, event) at its own layer; it doesn't need the SemanticIndex to pre-key them.

### TypedTransitionRow — ⚠️ string sentinel problem

```csharp
// Frank proposes:
string FromState,   // "any" encoded as a sentinel or null
```

The actual AST node is `TransitionRowNode.FromState: StateTargetNode`. "Any" state in the syntax is a distinct node. Using a `string?` with a null-means-any sentinel is an anti-pattern that forces every consumer to remember the convention. This should be:

```csharp
string? FromState  // null = "any" wildcard
```

...or better, a purpose-built type:

```csharp
// Option A: nullable string (null = any). Simple, correct.
string? FromState

// Option B: small DU (more explicit):
public abstract record StateTarget;
public sealed record AnyState : StateTarget;
public sealed record NamedState(string Name) : StateTarget;
```

Option A (nullable string) is fine given the anti-mirroring rule — I don't want to introduce a new DU just to carry a null. But the doc should commit to the convention explicitly so GraphAnalyzer doesn't accidentally treat `null` as "no from-state" rather than "any state."

### TypedAction DU — ⚠️ `SecondaryExpression` underspecified

Frank's `TypedInputAction`:

```csharp
TypedExpression? SecondaryExpression,  // for insert (index), put (key), appendBy (key), enqueueBy (priority)
```

This is a single nullable slot for what are actually *four different semantic roles* (index, key, key again, priority). When the Evaluator consumes this, it can't know which role the secondary expression plays without looking back at `ActionKind`. That defeats the purpose of the DU.

The actual `ActionSyntaxShape` catalog has 9 shapes. The proper approach: the secondary expression's role should be encoded in a sub-DU or at minimum a labeled field:

```csharp
// Better: named fields per semantic role
TypedExpression? IndexExpression,      // for InsertAt only
TypedExpression? KeyExpression,        // for PutKeyValue, AppendBy, EnqueueBy
TypedExpression? PriorityExpression,   // for EnqueueBy only (if different from key)
```

Or alternatively, preserve a single `SecondaryExpression` but add `SecondaryRole: ActionSecondaryRole` (Index, Key, Priority). Either way, the single nullable with a comment is not sufficient for the Evaluator.

### TypedEnsure — ✅ correct

The `ConstraintKind` discriminator + anchor fields is exactly what GraphAnalyzer and ProofEngine need.

### ImmutableDictionary loses insertion order — ⚠️

`ImmutableDictionary<string, TypedField>` for the field symbol table loses the declaration order. Declaration order matters for:
1. "Prior fields only" scope in default value expressions (§3.5)
2. Field listing in LS hover and MCP compile output

The existing `type-checker.md` open question #3 asks exactly this: "keyed by name for lookup, or ordered for iteration? Both consumers exist; consider supporting both via a wrapper."

Frank doesn't resolve this. Before Slice 1 I need a concrete answer. My recommendation: use `ImmutableArray<TypedField>` as the primary (preserves order) + derive a `FrozenDictionary<string, TypedField>` as a secondary lookup index. Same pattern as `Functions.ByName`.

---

## 3. Catalog Gap Assessment

### Gap 1: TypedConstant content validation ⚠️ HIGH — needs more precision

Frank identifies this correctly but the proposed shape needs one amendment. Frank proposes:

```csharp
public sealed record ContentValidation(
    string Pattern,
    string[] Examples,
    string FormatDescription
);
```

The `Pattern` field is under-typed. For date/time types, the validator delegates to NodaTime's parser by format string — a regex won't work. For currency/unit types, validation is membership in a closed set (ISO 4217, UCUM). For freeform typed constants, a regex may suffice.

The shape should discriminate between validation strategies:

```csharp
public abstract record ContentValidation;
public sealed record RegexContentValidation(string Pattern, string FormatDescription, string[] Examples) : ContentValidation;
public sealed record ParseDelegateContentValidation(string DelegateName, string FormatDescription, string[] Examples) : ContentValidation;
// e.g., DelegateName = "NodaTime.LocalDate" tells the checker to use NodaTime's parser
```

Without this, the checker still has a hidden per-type switch — "if delegate-validated, call NodaTime; if regex-validated, match regex" — which is an improvement over the pure hardcode but not fully catalog-driven.

**Implementation blocker:** This is a non-trivial catalog shape change. Until it's resolved, Slice 4 (Typed Constants) must hardcode the per-TypeKind dispatch table with a TODO referencing this gap. That's acceptable as a temporary scaffold — but it's a known debt item that must be tracked.

### Gap 2: Scope visibility rules — SKIP ✅

Frank's recommendation is correct. The scope model has 7 fixed contexts (§3.5), no nesting, and won't grow. Keep as checker logic.

### Gap 3: Action typed-shape classification — LOW (not MEDIUM)

Frank marks this MEDIUM. I disagree. The `ActionSyntaxShape` catalog (now 9 values: `AssignValue`, `CollectionValue`, `CollectionInto`, `FieldOnly`, `CollectionValueBy`, `InsertAt`, `RemoveAtIndex`, `PutKeyValue`, `CollectionIntoBy`) maps to the 3-shape DU with a stable 3-arm switch:

```csharp
// Full mapping, all 9 shapes covered:
FieldOnly                         → TypedAction (Base)
CollectionInto, CollectionIntoBy  → TypedBindingAction (Binding)
all others (6 shapes)             → TypedInputAction (Input)
```

This is 3 arms. It's stable because the DU's semantic meaning (Base/Input/Binding) reflects structural ownership (no operand / carries expression / carries binding target), not surface syntax. New `ActionSyntaxShape` values would fall into one of these three ownership categories by their nature.

I'll add a `TypedActionShape` field to `ActionMeta` if Shane/Frank want explicit catalog coverage, but from an implementation standpoint I don't need it. The checker can derive it safely.

The real concern Frank should have flagged is the `SecondaryExpression` role ambiguity (see §2 above) — that's the actual shape correctness issue for the Evaluator.

### Gap 4: `~string` CI enforcement — LOW ✅

Frank's assessment is correct. Five stable rules, `FunctionMeta.HasCIVariant` already exists. The checker can derive the enforcement diagnostic from operand type + `HasCIVariant`/`CaseInsensitiveEquals` operator presence. The 5-rule switch is fine.

### Gap 5: `pow` ProofRequirement — **CLOSED**

Frank marks this as "existing GAP-032." **GAP-032 was fixed on 2026-05-02** (see `history.md`: `PPowIntExp` named constant, `NumericProofRequirement(PPowIntExp, GreaterThanOrEqual, 0m, ...)` applied to Integer^Integer overload). The `Functions.cs` already carries this. Gap 5 is not a blocker.

---

## 4. Expression Resolution Engine ⚠️ Concerns

### ~100 lines claim is optimistic

Frank's Resolve pseudocode covers:
`LiteralExpression, IdentifierExpression, BinaryExpression, UnaryExpression, FunctionCallExpr, MemberAccessExpr, ConditionalExpr, QuantifierExpr, GroupedExpr, ListLiteralExpr`

**Missing from the pseudocode** (all present in `src/Precept/Pipeline/SyntaxNodes/Expressions/`):

| Missing node | Notes |
|---|---|
| `IsSetExpression` | Separate AST type — not a `UnaryExpression`. Resolves via `Operations.FindUnary(IsSet, fieldType)`. Must also validate operand is optional. |
| `IsNotSetExpression` | Same as above for `IsNotSet`. |
| `CIFunctionCallExpression` | Dedicated AST type for `~startsWith(str, prefix)`. Different from `CallExpression`. Deferred to Slice 8, but it needs an arm in the core Resolve function. |
| `MethodCallExpression` | `obj.Method(args)` — receiver type resolution + method lookup. Frank lists it in `ExpressionFormKind` but it's absent from the Resolve pseudocode. |
| `InterpolatedStringExpression` | Each `{expr}` hole must be scalar type; result is `string`. Not complex but not zero work. |
| `InterpolatedTypedConstantExpression` | Same structure as interpolated string but context-typed. |
| `TypedConstantExpression` | Frank defers to Slice 4, but this is a first-class AST node with context-sensitive resolution logic. |

That's 7 additional arms. The function is probably 250-350 lines, not ~100. Still a single recursive function — the principle holds — but the line count estimate will mislead whoever writes the implementation plan.

### The BinaryIndex returns `BinaryOperationMeta[]`, not a single entry — ❌ Critical miss

Frank's proposed APIs:

```csharp
// Frank proposes:
public static FrozenDictionary<(OperatorKind, TypeKind, TypeKind), BinaryOperationMeta> BinaryBySignature { get; }
```

**These already exist**, but under different names and with a critical semantic difference:

```csharp
// What's already in Operations.cs (lines 1111-1136):
public static FrozenDictionary<(OperatorKind, TypeKind, TypeKind), BinaryOperationMeta[]> BinaryIndex { get; }
public static FrozenDictionary<(OperatorKind, TypeKind), UnaryOperationMeta> UnaryIndex { get; }

// Plus convenience wrappers:
public static UnaryOperationMeta? FindUnary(OperatorKind op, TypeKind operand)
public static ReadOnlySpan<BinaryOperationMeta> FindCandidates(OperatorKind op, TypeKind lhs, TypeKind rhs)
```

The checker should use `Operations.FindCandidates` and `Operations.FindUnary` — not build new indexes.

The critical semantic difference: `BinaryIndex` returns `BinaryOperationMeta[]` (array), not `BinaryOperationMeta?` (single). For `(Times, Money, Money)` and `(Divide, Money, Money)`, there are *two* catalog entries — one with `QualifierMatch.Same` and one without. The checker must disambiguate between them based on qualifier match. Frank's design assumes a 1:1 lookup which silently breaks for money/money and quantity/quantity division.

**The checker needs a qualifier-disambiguation layer:**

```csharp
// After FindCandidates returns > 1 entry:
// - If all entries have QualifierMatch.Same, check that operand qualifiers match → emit QualifierMismatch if not
// - Select the matching entry
// This is ~15 lines of logic after the catalog call, not zero
```

This is the one place where the "pure catalog lookup" framing breaks down slightly. The lookup is still catalog-driven; the disambiguation is tiny structural logic that the catalog can't do for us because qualifier identity requires knowing the actual field qualifiers, not just the types.

---

## 5. Missing Catalog Query APIs ✅ (mostly already exist)

Frank's proposals:

| Proposed | Actual in Operations.cs |
|---|---|
| `BinaryBySignature: FrozenDictionary<(OperatorKind, TypeKind, TypeKind), BinaryOperationMeta>` | `BinaryIndex: FrozenDictionary<(OperatorKind, TypeKind, TypeKind), BinaryOperationMeta[]>` + `FindCandidates()` |
| `UnaryBySignature: FrozenDictionary<(OperatorKind, TypeKind), UnaryOperationMeta>` | `UnaryIndex: FrozenDictionary<(OperatorKind, TypeKind), UnaryOperationMeta>` + `FindUnary()` |
| Widening-aware lookup | `IsAssignable(source, target)` using `TypeMeta.WidensTo` — correct as described |

The lookup APIs exist. The type checker should import `Operations.FindCandidates`, `Operations.FindUnary`, and `Types.GetMeta(TypeKind).WidensTo` directly. No new catalog additions needed for these.

**One API that IS missing:** `Functions.FindByName` as an efficient lookup. `Functions.ByName` is a `FrozenDictionary<string, FunctionMeta>` — let me verify this exists. I'll assume it does given the established pattern (same as `Actions.ByTokenKind`). If it doesn't, it needs to be added before Slice 3.

---

## 6. Vertical Slices — Ordering and Dependencies ⚠️ Concerns

### SemanticIndex record shape must be Slice 0 or pre-Slice

Frank's Slice 10 is "Final SemanticIndex construction and immutable sealing." But I can't write passing Slice 2-9 tests without the `SemanticIndex` record types (`TypedField`, `TypedState`, `TypedEvent`, `TypedExpression` DU, etc.) being defined first.

**Required pre-slice:** Define the full record type hierarchy in `SemanticIndex.cs` (and a companion file like `TypedExpressions.cs`). All the types from §3 of Frank's doc. No logic — just type definitions. This is the structural contract every subsequent slice builds toward. Without it, Slice 2 has no return type to compile against.

### Slice 2 + Slice 3 artificial split

Slice 2 (scalars: binary/unary ops) and Slice 3 (functions + accessors) are both parts of the same `Resolve()` function. A test for `min(amount, 100)` exercises both. The split is workable — you stub out function resolution in Slice 2 (returning `TypedError` for any `CallExpression`) and implement it in Slice 3 — but the "dependency arrow" makes it sound like you can't touch function resolution at all until Slice 3. In practice the implementations interleave.

I'd merge Slices 2 and 3 or at minimum document the stub strategy explicitly.

### Slice 7 (Modifiers) correctly independent — ✅

Modifier validation depends only on Slice 1 (symbol tables) because it operates on `FieldDeclarationNode.Modifiers`, not on expressions. The ordering here is correct.

### Missing: `[HandlesCatalogMember]` stub migration

The current `TypeChecker.cs` has 13 `[HandlesCatalogMember]` annotations on a stub `private static void CheckExpression()` method. These annotations satisfy PRECEPT0019. When real per-form handlers are implemented, these stubs must be *removed from the stub method* and *re-applied to the actual handling methods*. If Slice 2 adds a real binary operation handler but doesn't remove `[HandlesCatalogMember(BinaryOperation)]` from the stub, PRECEPT0019 will fire a duplicate-coverage diagnostic — or worse, the stub will silently shadow the real coverage check.

Frank doesn't mention this migration at all. It needs to be the first step of each slice that implements a real expression form.

### Dependency graph annotation for Slice 8

Slice 8 covers `~string` CI enforcement. But `CIFunctionCallExpression` is a core AST node that the Resolve function needs to handle, not just a feature layered on top of Slices 2-3. Its Resolve arm should be in the core Resolve function from the start, stubbed to return `TypedError` until Slice 8. Deferring the arm entirely to Slice 8 means Slice 5 and 6 tests will crash if any test expression contains `~startsWith(...)`.

---

## 7. What Frank Didn't Address ❌ Problems / Gaps

### 1. `InterpolatedStringExpression` and `InterpolatedTypedConstantExpression` — no slice ownership

These AST nodes exist (`InterpolatedStringExpression`, `InterpolatedTypedConstantExpression` in `SyntaxNodes/`). Neither appears in any of Frank's 10 slices. The interpolated string checker needs to:
- Decompose into `InterpolationPart[]`
- Type-check each `ExpressionInterpolationPart` — expression must be scalar (TypeCategory != Collection)
- Result type is `TypeKind.String` for `InterpolatedStringExpression`, context-typed for `InterpolatedTypedConstantExpression`

This isn't complex but it needs a home. **My recommendation:** Add to Slice 8 alongside string operations, or add as Slice 8a.

### 2. `MethodCallExpression` resolution strategy

`MethodCallExpression(Receiver, MethodName, Arguments)` exists as an AST node and `ExpressionFormKind.MethodCall` is in the catalog. Frank's Resolve pseudocode omits it entirely. The checker needs a strategy: is a method call resolved as a TypeMeta accessor lookup (same as `.count` member access) or as a separate path? For the current surface, method calls appear to be collection operations. This needs explicit design before Slice 3.

### 3. Qualifier propagation through expressions

`TypedFieldRef.IsCaseInsensitive` is defined for `~string`. But qualifier propagation for money/quantity — "the result of `usd_amount + fee` is `Money` with currency USD" — isn't addressed. The Evaluator needs qualifier identity on expressions to validate runtime safety. Either:
- Typed expressions carry qualifier binding (`TypedBinaryOp.ResultQualifier?`)
- Or qualifiers are always re-derived from field references at evaluation time

Frank's design is silent on this. It's not a blocker for type checking (TypeKind is sufficient for type correctness), but the Evaluator/ProofEngine will need it for qualifier compatibility proof obligations.

### 4. `BinaryIndex` multi-candidate qualifier disambiguation (already noted above, §4)

The qualifier disambiguation logic after `FindCandidates()` returns `>1` entry is not designed. This is ~15 lines but it's a correctness requirement for money and quantity operations.

### 5. The `SemanticIndex` collection type decision (open question #3 in `type-checker.md`)

`ImmutableDictionary<string, TypedField>` loses declaration order. This isn't a problem Frank invented — it was an open question in the existing stub doc — but his design makes a concrete choice without acknowledging the tradeoff. I want an explicit decision here before Slice 1: keyed-only (`ImmutableDictionary`), ordered-only (`ImmutableArray`), or both (ordered array + derived frozen dict). My preference: `ImmutableArray<TypedField>` as primary + `FrozenDictionary<string, TypedField>` as secondary index (the `Functions.ByName` pattern). This satisfies both the ordering requirement (default value expression scope, LS hover order) and the lookup requirement (name resolution, GraphAnalyzer keying).

### 6. Error recovery shape — underspecified

Frank covers "ErrorType propagation" in §3.14 but the policy for *partial results* is not fully specified. If `TransitionRowNode` has a resolution error in its guard, does the checker still produce a `TypedTransitionRow` (with a `TypedError` guard) or does it skip the row entirely? The "accumulate all diagnostics without abandoning the pass" principle from `type-checker.md` says produce the partial result, but Frank doesn't explicitly state how each normalized declaration type handles an error in its sub-expressions. This needs to be documented per-type before Slice 5.

---

## Summary Table

| Area | Verdict | Top Issue |
|---|---|---|
| Implementability | ✅ / ⚠️ | Architecture is right; mutable working context (CheckContext) not designed |
| SemanticIndex shape | ⚠️ | `SecondaryExpression` role ambiguity in TypedInputAction; field ordering decision unresolved |
| Catalog gaps | ✅ / ⚠️ | Gap 5 already closed; Gap 1 needs discriminated ContentValidation shape |
| Expression resolution engine | ⚠️ | 7 AST node types missing from Resolve pseudocode; BinaryIndex returns array not single entry |
| Catalog query APIs | ✅ | Already exist as BinaryIndex/UnaryIndex/FindCandidates/FindUnary |
| Vertical slices | ⚠️ | SemanticIndex type definitions must precede Slice 1; HandlesCatalogMember migration not addressed |
| What's missing | ❌ | Interpolated expressions (no slice), MethodCallExpression strategy, qualifier propagation, error recovery shape |

---

## What I Need Before I Can Start Slice 1

1. **Decision: field ordering.** `ImmutableArray<TypedField>` primary + `FrozenDictionary` secondary, or `ImmutableDictionary` with ordering handled elsewhere? I'm proposing the array-primary approach.

2. **Pre-Slice 0: commit the record type definitions.** All `TypedField`, `TypedState`, `TypedEvent`, `TypedArg`, `TypedExpression` DU, `TypedAction` DU, `TypedTransitionRow`, etc. in `SemanticIndex.cs` (or companion files). This is a pure shape commit — no logic. Slices 1-9 compile against it.

3. **`SecondaryExpression` role clarity.** Either sub-DU or named per-role fields in `TypedInputAction`. My vote: keep a single `SecondaryExpression?` field but add `SecondaryRole: ActionSecondaryRole?` where `ActionSecondaryRole` has 3 members: `Index`, `Key`, `Priority`.

4. **Confirmation that `MethodCallExpression` dispatches via TypeMeta accessors.** I'll treat method calls as accessor-style lookups unless told otherwise.

5. **`InterpolatedStringExpression` gets Slice 8a** or is folded into Slice 3. Someone needs to decide before I hit it mid-Slice-5.
