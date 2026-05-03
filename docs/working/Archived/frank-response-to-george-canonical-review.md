# Architectural Response to George's Canonical Type Checker Review

**Author:** Frank (Lead/Architect)
**Date:** 2026-05-02T17:50:16-04:00
**Responding to:** `docs/working/george-canonical-design-review.md`

---

## Executive Response

George's canonical review is thorough and well-reasoned. The verification that all 6 original findings survived consolidation confirms the canonical doc process worked correctly. I'm accepting all 5 concerns with concrete resolutions, accepting 3 of 4 missing items (rejecting transitive widening), and resolving all 3 red flags with explicit architectural decisions.

After applying these resolutions, the implementation readiness assessment changes: **all slices are ready to start** in their dependency order — no blocking questions remain.

---

## 1. CONCERNS — Resolutions

### CON-1: AST Node Name Discrepancies — ACCEPTED

**Verdict:** George is correct. The actual AST classes are:
- `CallExpression` (not `FunctionCallExpression`)
- `ParenthesizedExpression` (not `GroupedExpression`)
- `CIFunctionCallExpression` exists as expected (in `QuantifierExpression.cs`)

**Resolution:** Update the canonical doc pseudocode at line 73 and line 78 to use actual class names. Add a note that the `ExpressionFormKind` enum member names (`FunctionCall`, `Grouped`) are catalog classification names, not AST class names — the catalog names what the *form* is; the AST names what the *syntax node* is.

**Mapping table (added to doc):**

| ExpressionFormKind | AST Class |
|---|---|
| `Literal` | `LiteralExpression` |
| `Identifier` | `IdentifierExpression` |
| `Grouped` | `ParenthesizedExpression` |
| `BinaryOperation` | `BinaryExpression` |
| `UnaryOperation` | `UnaryExpression` |
| `MemberAccess` | `MemberAccessExpression` |
| `Conditional` | `ConditionalExpression` |
| `FunctionCall` | `CallExpression` |
| `MethodCall` | `MethodCallExpression` |
| `ListLiteral` | `ListLiteralExpression` |
| `PostfixOperation` | `IsSetExpression` / `IsNotSetExpression` |
| `Quantifier` | `QuantifierExpression` |
| `CIFunctionCall` | `CIFunctionCallExpression` |

**Slice affected:** None (documentation-only). No implementation readiness change.

---

### CON-2: PostfixOperation Stub Migration — ACCEPTED

**Verdict:** George is correct. There is no `IsSet` or `IsNotSet` in `ExpressionFormKind`. The only member is `PostfixOperation` (value 11). Both `IsSetExpression` and `IsNotSetExpression` AST nodes fall under this single form.

**Resolution:** Fix the Slice 6 stub migration note:

> **Before:** Remove `IsSet`, `IsNotSet` from stub
> **After:** Remove `PostfixOperation` from stub. The real handler pattern-matches on `IsSetExpression | IsNotSetExpression` within the single `PostfixOperation` form handler.

**Slice affected:** Slice 6 — cosmetic doc fix only. Readiness unchanged (already ✅).

---

### CON-3: ExpressionFormKind.Literal Across Slices — ACCEPTED

**Verdict:** George correctly identifies that `ExpressionFormKind.Literal` is a single annotation unit covering multiple AST types: `LiteralExpression`, `TypedConstantExpression`, `InterpolatedStringExpression`, `InterpolatedTypedConstantExpression`. The `[HandlesCatalogMember]` annotation cannot be split.

**Resolution — locked protocol:**

1. **Slice 2** takes ownership of `ExpressionFormKind.Literal`. Migrates the annotation from stub to the real Resolve handler. The handler has a switch on the actual AST node subtype within the `Literal` form:
   - `LiteralExpression` → resolve (Slice 2 implements)
   - All others → `TypedErrorExpression` (stub within the real handler)

2. **Slice 3** adds the `InterpolatedStringExpression` arm inside the already-migrated handler. No stub migration annotation change.

3. **Slice 4** adds `TypedConstantExpression` and `InterpolatedTypedConstantExpression` arms. No stub migration annotation change.

The design doc's Slice 3 and Slice 4 stub migration notes are updated to say "Add arm within `Literal` handler" instead of "Remove X from stub."

**Slice affected:** Slices 2, 3, 4 — protocol clarification only. All remain ready.

---

### CON-4: TypeAccessor Return Type Resolution — ACCEPTED

**Verdict:** George correctly identifies that the accessor DU resolution logic isn't spelled out. This is a 3-arm pattern match that the implementer needs.

**Resolution — added pseudocode:**

```csharp
// Accessor return-type + parameter-type resolution (§ Sub-pass 2a, MemberAccess arm)
(TypeKind returnType, TypeKind? paramType) = resolvedAccessor switch
{
    // Base TypeAccessor (peek, dequeue, pop): returns element type of owning collection
    TypeAccessor a when a is not FixedReturnAccessor and a is not ElementParameterAccessor
        => (owningField.ElementType!.Value, null),

    // FixedReturnAccessor (date.year → integer, date.month → integer):
    // returns accessor.Returns directly
    FixedReturnAccessor f
        => (f.Returns, f.ParameterType),

    // ElementParameterAccessor (bag.countof(x) → integer):
    // return type = integer (always), parameter type = owning field's element type
    ElementParameterAccessor e
        => (TypeKind.Integer, owningField.ElementType!.Value),
};
```

For `MethodCallExpression` (accessor with parameters), validate the argument type against `paramType`. If `paramType` is null but the accessor was invoked with `()`, that's a structural error (accessor is property-style, not method-style).

**Slice affected:** Slice 3. This adds ~10 lines to the implementation but doesn't change readiness — was already "almost" due to MISS-4, now fully specified.

---

### CON-5: Source File Paths — ACCEPTED

**Verdict:** George is correct. The actual path is `src/Precept/Language/`, not `src/Precept/Catalogs/`. The `Catalogs/` directory does not exist.

**Resolution:** Update the Source Files table:

| File | Purpose |
|---|---|
| `src/Precept/Pipeline/TypeChecker.cs` | Type checker implementation |
| `src/Precept/Pipeline/SemanticIndex.cs` | SemanticIndex artifact |
| `src/Precept/Language/Operations.cs` | `FindCandidates()` at line 1153, `FindUnary()` at line 1145 |
| `src/Precept/Language/Functions.cs` | `ByName` frozen dictionary at line 298 |
| `src/Precept/Language/Types.cs` | `GetMeta()` → `TypeMeta` with `.Accessors`, `.WidensTo` |
| `src/Precept/Language/Modifiers.cs` | Modifier applicability, mutual exclusivity, subsumption |

**Slice affected:** None — doc cosmetic. No readiness change.

---

## 2. MISSING ITEMS — Resolutions

### MISS-1: Quantifier Binding Stack — ACCEPTED

**Verdict:** George is correct. `CheckContext` needs a binding stack for nested quantifier scoping. The `CurrentEventArgs` set/clear pattern doesn't generalize to nesting because quantifiers CAN nest.

**Resolution — locked addition to CheckContext:**

```csharp
// In CheckContext:
public Stack<(string Name, TypeKind Type)> QuantifierBindings { get; } = new();
```

**Identifier resolution priority in Resolve:**
1. `QuantifierBindings` (top of stack first — innermost binding wins)
2. `CurrentEventArgs` (if set)
3. `FieldLookup` (field symbol table)
4. Error: "unresolved identifier"

Quantifier binding shadows event args which shadow fields. This is correct because `each x in items (...)` introduces `x` as a local name that MUST be usable even if a field named `x` exists.

**Push/pop protocol:** Push binding at quantifier entry, pop at quantifier exit. If `Resolve` throws (unlikely given error-recovery), the stack must still pop — wrap in try/finally or structured scope.

**Slice affected:** Slice 9. Readiness changes from ⚠️ to ✅.

---

### MISS-2: Widening Fallback Strategy — ACCEPTED

**Verdict:** George correctly identifies that `FindCandidates` is exact-match only and the fallback strategy is unspecified.

**Resolution — locked widening fallback algorithm:**

```
ResolveOp(op, leftType, rightType):
  1. candidates = FindCandidates(op, leftType, rightType)
  2. if candidates.Length >= 1 → disambiguate (qualifier or single), done
  3. Try LEFT widening only:
     for each wt in Types.GetMeta(leftType).WidensTo:
       candidates = FindCandidates(op, wt, rightType)
       if candidates.Length >= 1 → disambiguate, done (result type from catalog entry)
  4. Try RIGHT widening only:
     for each wt in Types.GetMeta(rightType).WidensTo:
       candidates = FindCandidates(op, leftType, wt)
       if candidates.Length >= 1 → disambiguate, done
  5. Try BOTH widening:
     for each lwt in Types.GetMeta(leftType).WidensTo:
       for each rwt in Types.GetMeta(rightType).WidensTo:
         candidates = FindCandidates(op, lwt, rwt)
         if candidates.Length >= 1 → disambiguate, done
  6. Emit "NoMatchingOperation" diagnostic, return TypedErrorExpression
```

**Priority order:** Left-first, then right-first, then both. This is deterministic and predictable. For `integer + number`: step 3 tries `FindCandidates(+, number, number)` → match found. For `integer + decimal`: step 3 tries `(+, decimal, decimal)` → match, AND `(+, number, decimal)` → take first found in `WidensTo` order. Since `IntegerWidens = [Decimal, Number]`, step 3 tries `decimal` first → `(+, decimal, decimal)` exists → done.

**No ambiguity resolution needed:** If the widening loop finds a match on the first `WidensTo` entry, it returns. The ordering in `WidensTo` IS the priority. This is a deliberate design choice — `WidensTo` arrays are ordered narrowest-first.

**Slice affected:** Slice 2. Readiness changes from ⚠️ to ✅.

---

### MISS-3: Transitive Widening — REJECTED (single-hop only)

**Verdict:** George's question is valid. Looking at the actual data: `IntegerWidens = [Decimal, Number]`. There is NO separate `NumberWidens` or `DecimalWidens` array — I searched. This means **the widening graph is already flat** in practice. Integer widens to both decimal AND number directly (not transitively through number→decimal).

**Decision:** Widening is **single-hop only**. `IsAssignable(source, target)` checks `Types.GetMeta(source).WidensTo.Contains(target)` — one lookup, no recursion. The `WidensTo` arrays are designed to be complete for each type — if integer can reach decimal, `Decimal` is IN the array directly.

**Rationale:** Transitive widening would introduce non-obvious implicit conversions, make type error messages confusing ("why did my integer become a decimal via number?"), and complicate the widening fallback algorithm. The catalog encodes direct widening relationships only.

**Slice affected:** Slice 2 — confirms the simple `Contains` check is correct. No code complexity added.

---

### MISS-4: Function Overload Resolution — ACCEPTED

**Verdict:** George correctly identifies that the overload matching algorithm is unspecified. The `FunctionMeta[]` array (same name, different overloads) needs a concrete dispatch strategy.

**Resolution — locked overload resolution algorithm:**

```
ResolveFunctionCall(name, resolvedArgs[]):
  1. allOverloads = Functions.ByName[name].SelectMany(fm => fm.Overloads)
  2. Filter by arity: keep only overloads where Parameters.Length == resolvedArgs.Length
  3. For each remaining overload, score:
     a. EXACT match:  all arg types == parameter types → score 0 (best)
     b. WIDENED match: all args IsAssignable to params → score = count of widened args
     c. NO match:     skip
  4. Sort by score ascending. If exactly one score-0 entry → select it.
  5. If multiple score-0 entries → ambiguity error (shouldn't happen with current catalog)
  6. If no score-0 but one or more widened → select lowest score
  7. If no match at all → emit "NoMatchingOverload" diagnostic, return TypedErrorExpression
```

**Numeric literal context propagation:** When an argument is a `LiteralExpression` (numeric), resolve it **without** expectedType first (→ `integer`). If no exact match found, retry the literal with `expectedType` = each candidate's parameter type. This is the bidirectional resolution that makes `min(amount, 100)` work: first try `min(money, integer)` → no match → retry `100` with expectedType=money → `min(money, money)` → match.

**Concretely for `min(amount, 100)` where `amount: money`:**
1. Resolve args: `amount` → `money`, `100` → `integer` (no context)
2. Filter by arity=2: get overloads `(int,int)`, `(dec,dec)`, `(money,money)`, `(qty,qty)`, `(number,number)`
3. Exact match `(money, integer)` → none
4. Widened match: `IsAssignable(money, int)` → no. `IsAssignable(integer, money)` → no (integer doesn't widen to money).
5. No match → retry with context propagation: resolve `100` with expectedType=money → `TypedLiteral(money, 100)` → re-score: `(money, money)` → exact match → done.

**This is the "one-retry-with-context" pattern.** It only triggers when bare resolution fails and unresolved literals exist.

**Slice affected:** Slice 3. Readiness changes from ⚠️ to ✅.

---

## 3. RED FLAGS — Resolutions

### RF-1: Numeric Literal Type Ambiguity — RESOLVED

**Decision:** **Bottom-up first, context retry on failure.** This is the hybrid approach:

1. Bare numeric literals resolve to `integer` by default (bottom-up).
2. Binary operations try exact match first. `(>, money, integer)` → if the catalog has this entry, use it. If not, try widening per MISS-2 algorithm.
3. If no match after widening either: the binary op can push `expectedType` context into the literal operand for retry (same one-retry pattern as MISS-4).

**In practice for `amount > 100`:** The Operations catalog likely has `(>, money, money)` and `(>, money, integer)` would NOT exist. So step 2 widening doesn't find integer→money. Step 3 retries `100` with expectedType=money → `TypedLiteral(money, 100)` → `(>, money, money)` → match.

**Key insight:** This is NOT two different resolution strategies. It's ONE strategy with a fallback step. Bottom-up produces a concrete type for everything except literals-in-ambiguous-context, and the retry only fires when the initial attempt fails. The implementation is:
- Resolve both operands (bottom-up)
- Try FindCandidates + widening
- If failure AND one operand is a bare literal → retry that operand with target type from the other side
- If failure AND both are bare literals → resolve both as integer (the default)

**Slice affected:** Slice 2 (base) + Slice 4 (full context propagation). The retry mechanism is Slice 4's `expectedType` propagation; Slices 2–3 can use the bottom-up-only path and emit "no matching operation" for cases that need context. This means Slice 2 is simpler: no retry logic needed. Context retry lands in Slice 4 and retroactively improves literal resolution in binary ops.

---

### RF-2: Computed Field Scope Gate — RESOLVED

**Decision:** The `CurrentFieldIndex` guard goes in the `IdentifierExpression` resolution arm, conditioned on whether we're currently resolving a scope-limited expression.

**Resolution — add a scope mode to CheckContext:**

```csharp
// In CheckContext:
public enum FieldScopeMode { AllFields, PriorFieldsOnly }
public FieldScopeMode CurrentScope { get; set; } = FieldScopeMode.AllFields;
```

**Protocol:**
- Before resolving a computed expression or default expression: set `CurrentScope = PriorFieldsOnly`, set `CurrentFieldIndex` to the current field's index.
- In `IdentifierExpression` resolution: if `CurrentScope == PriorFieldsOnly` and the resolved field's index >= `CurrentFieldIndex` → emit "ForwardReferenceProhibited" diagnostic, return `TypedErrorExpression`.
- After resolving: reset `CurrentScope = AllFields`.

This is cleaner than checking "am I currently in a computed expression" because it generalizes to any future scope-limited context. Guards, transition actions, and rule conditions see `AllFields`.

**Slice affected:** Slice 2 (identifier resolution checks the scope mode) + Slice 6 (structural validation of forward references is the verification pass, but the gate fires during expression resolution in Slice 2). Implementation note: Slice 2 adds the scope mode and the check in identifier resolution. Slice 1 sets the mode during field registration iteration. The forward-reference prohibition in Slice 6 becomes redundant structural validation (belt-and-suspenders).

---

### RF-3: EventHandler Scope Distinction — RESOLVED

**Decision:** EventHandlers **DO** have event arg scope. Looking at the `EventHandlerNode` AST:

```csharp
public sealed record EventHandlerNode(
    SourceSpan Span,
    Token EventName,         // ← the event IS named
    ImmutableArray<Statement> Actions,
    Expression? PostConditionGuard) : Declaration(Span);
```

The `EventName` token means the handler is associated with a specific event. That event's args are in scope within the handler's actions and post-condition guard. This is semantically identical to a transition row's scope: you're handling an event, so you have access to its arguments.

**Resolution:** Slice 5 scope management sets `CurrentEventArgs` for BOTH transition rows AND event handlers. The protocol is:

```
// In Sub-pass 2b (EventHandlerNode):
ctx.CurrentEventArgs = ctx.EventLookup[eventName].Args.ToDictionary(a => a.Name);
// ... resolve actions and post-condition guard ...
ctx.CurrentEventArgs = null;
```

This is the same 3-line pattern as transition rows. No new mechanism needed.

**Slice affected:** Slice 5. Already documented as the scope-management slice. Readiness unchanged — the RF-3 question was "does the handler have args?" and the answer is "yes, same pattern as transition rows."

---

## 4. SLICE 6 SPLIT SUGGESTION — REJECTED

George suggests splitting Slice 6 into 6a (structural: cycles, choice, forward-ref) and 6b (IsSet/IsNotSet expression forms).

**Decision:** Reject the split. Rationale:
- `IsSet`/`IsNotSet` is a 10-line handler (`operand must be optional field, result = boolean`). It's trivially small.
- The structural validations (cycle detection, choice validation, forward-ref) are thematically cohesive with "structural correctness checks."
- Splitting would add a slice boundary without meaningful independence — 6b is ~10 lines and would need its own test file/class for no meaningful parallelism gain.
- George correctly notes the dependency (6 depends on Slice 2 for IsSet/IsNotSet), but this is already correctly captured in the dependency graph: Slice 6 depends on Slices 2–4.

Keep Slice 6 as a single unit.

---

## 5. KRAMER'S RECOMMENDATIONS

### R3: TypedTransitionRow.ResolvedArgs — REJECTED for canonical design

**Rationale:** Adding `ImmutableArray<TypedArg> ResolvedArgs` to `TypedTransitionRow` violates the anti-mirroring principle. The args already exist at `SemanticIndex.EventsByName[row.EventName].Args`. Pre-resolving them onto the row means:
1. Two sources of truth for the same data
2. Structural coupling (if an event's args change, all rows referencing it have stale cached copies)
3. The "convenience" is a single dictionary lookup — not a computational savings

The LS should build its own scope-derivation helper as a one-liner: `index.EventsByName[row.EventName].Args`. This is Kramer's own fallback recommendation and it's the correct one.

**If the LS team demonstrates profiling evidence that this single dictionary lookup is a hot path**, we can revisit. Until then, no.

### R4: TypedEditDeclaration — ACCEPTED (deferred to post-Slice 10)

**Rationale:** Stateless precepts with `edit all` / `edit Field1, Field2` declarations DO need typed representation. Currently there is no `EditDeclarationNode` in the AST (verified — no edit-related nodes in SyntaxNodes). This means:
1. The parser doesn't produce an edit declaration node yet
2. The type checker can't type what doesn't parse

**Resolution:** Add `TypedEditDeclaration` to the SemanticIndex shape in Pre-Slice 0 as a placeholder record. The parser and type-checker implementation for edit declarations is a **separate feature** that follows the full implementation lifecycle (proposal → design → slices). It is NOT part of this type checker design doc's scope.

```csharp
// Added to Pre-Slice 0 shape commit (placeholder):
public sealed record TypedEditDeclaration(
    ImmutableArray<string> EditableFields,  // empty = "all"
    bool IsEditAll,
    SyntaxNode Syntax  // EditDeclarationNode once parser supports it
);

// On SemanticIndex:
ImmutableArray<TypedEditDeclaration> EditDeclarations  // initially empty
```

---

## 6. SOUP NAZI'S NON-NEGOTIABLE GATES — Design Implications

### Gate 1: Pre-Slice 0 shape commit with build-verification tests

**Already designed.** Pre-Slice 0 in the canonical doc specifies exactly this: all record types, no behavioral tests, build verification only. No design change needed.

### Gate 2: `TypeCheck()` and `TypeCheckExpr()` test helpers

**Design implication:** These helpers belong in the test project infrastructure, not in the canonical design doc. They are test-side convenience wrappers around `TypeChecker.Check()` and `Resolve()` respectively. The design doc specifies the public API (`TypeChecker.Check(SyntaxTree) → SemanticIndex`). Test helpers are an implementation detail of the test project.

**One addition to canonical doc:** Add a note to Pre-Slice 0 that the shape commit ALSO includes test infrastructure helpers:

> Pre-Slice 0 also delivers: test helpers `TypeCheck(string source) → SemanticIndex` and `TypeCheckExpr(string source) → TypedExpression` in the test project, wrapping the parse→check pipeline for ergonomic slice testing.

### Gate 3: Sample file audit (no false-positive regressions)

**Design implication:** The type checker must produce ZERO diagnostics on all 20 `samples/*.precept` files (modulo intentionally-errored samples if any exist). This is a Slice 10 integration test, but the audit should happen BEFORE implementation begins to identify any spec-vs-samples gaps. This is a pre-implementation activity, not a design change.

**No design changes needed** for the three gates. The design already supports all three.

---

## 7. UPDATED IMPLEMENTATION READINESS

| Slice | Previous | Updated | Reason |
|---|---|---|---|
| Pre-Slice 0 | ✅ | ✅ | Unchanged |
| Slice 1 | ✅ | ✅ | Unchanged |
| Slice 2 | ⚠️ | ✅ | MISS-2 resolved (widening algorithm), RF-1 resolved (bottom-up + retry), RF-2 resolved (scope mode) |
| Slice 3 | ⚠️ | ✅ | MISS-4 resolved (overload algorithm), CON-4 resolved (accessor resolution) |
| Slice 4 | ✅ | ✅ | Unchanged |
| Slice 5 | ⚠️ | ✅ | RF-3 resolved (EventHandler has event args scope) |
| Slice 6 | ✅ | ✅ | CON-2 cosmetic fix only |
| Slice 7 | ✅ | ✅ | Unchanged |
| Slice 8 | ✅ | ✅ | Unchanged |
| Slice 9 | ⚠️ | ✅ | MISS-1 resolved (binding stack + resolution priority) |
| Slice 10 | ✅ | ✅ | Unchanged |

**All slices now ready.** No blocking questions remain.

---

## 8. DECISIONS LOG

| # | Decision | Rationale |
|---|---|---|
| D-15 | Widening is single-hop only, not transitive | `WidensTo` arrays are designed complete; transitive adds complexity and confusing error messages |
| D-16 | Widening fallback: left-first, right-first, then both; `WidensTo` array order is priority | Deterministic, predictable, narrowest-widen-first by array ordering |
| D-17 | Numeric literal default = integer, with one-retry context propagation on failure | Hybrid of bottom-up simplicity + context when needed; retry in Slice 4 |
| D-18 | EventHandlers have event arg scope (same pattern as transition rows) | EventHandlerNode names its event; args are naturally in scope |
| D-19 | Computed/default field scope uses `FieldScopeMode` enum in CheckContext | Generalizes "prior fields only" without special-casing computed expressions |
| D-20 | Identifier resolution priority: quantifier bindings > event args > fields | Innermost scope wins; shadowing is predictable |
| D-21 | Function overload resolution: arity filter → exact match → widened match → context retry | Single clear algorithm; no ambiguity with current catalog |
| D-22 | Reject Slice 6 split (6a/6b) | IsSet/IsNotSet is 10 lines; splitting adds boundary overhead with no parallelism gain |
| D-23 | Reject TypedTransitionRow.ResolvedArgs (Kramer R3) | Anti-mirroring: one dictionary lookup doesn't justify cached copies |
| D-24 | Accept TypedEditDeclaration placeholder (Kramer R4) | Correct shape for eventual stateless-precept edit support; deferred implementation |
| D-25 | ExpressionFormKind.Literal migrates in Slice 2; Slices 3–4 add arms within handler | Single annotation ownership; sub-form stubs live inside the real handler |
