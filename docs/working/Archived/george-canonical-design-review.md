# George's Canonical Type Checker Design Review

**Reviewer:** George (Runtime Dev)
**Date:** 2026-05-02T16:30:27-04:00
**Source doc:** `docs/compiler/type-checker.md` (817 lines, consolidated canonical)

---

## Executive Summary

The consolidated canonical doc is a significant improvement over Frank's initial analysis. All 6 of my original findings are reflected — nothing was lost in consolidation. The architecture is implementable as-designed for ~85% of the surface. The remaining ~15% has specificity gaps that will require decisions during Slice 2–4 implementation. I've identified 5 concerns and 4 missing items below; none are architectural blockers but all will need resolution before their respective slices begin.

---

## 1. Completeness Check: Original 6 Findings

| # | Original Finding | Status in Canonical Doc | Verdict |
|---|---|---|---|
| 1 | `Operations.FindCandidates`/`FindUnary` already exist; no new `BinaryBySignature` | ✅ Correctly locked (§ Catalog Lookup Strategy, Decision #1) | **Fully resolved** |
| 2 | 7 missing AST node types in Resolve pseudocode | ✅ All 16+ types now listed (§ Sub-pass 2a, lines 67–86) | **Fully resolved** |
| 3 | SemanticIndex record shapes must be pre-Slice 0 | ✅ Explicit "Pre-Slice 0: Shape Commit" section (lines 655–666) | **Fully resolved** |
| 4 | GAP-032 (`pow` ProofRequirement) already closed | ✅ Decision #8 records as closed | **Fully resolved** |
| 5 | `[HandlesCatalogMember]` stub migration protocol | ✅ Dedicated section (lines 637–649), per-slice protocol defined | **Fully resolved** |
| 6a | `ImmutableDictionary` loses declaration order | ✅ Array-primary pattern locked (§ Collection Type Decision, Decision #4) | **Fully resolved** |
| 6b | `SecondaryExpression` role ambiguity | ✅ `ActionSecondaryRole` enum locked (lines 293–300, Decision #5) | **Fully resolved** |

**Verdict: Complete. No findings lost in consolidation.**

---

## 2. CONFIRMED: Design Decisions That Are Correct and Implementable

### C1. 2-Pass / 3-Sub-Pass Architecture

The pass separation (registration → resolution → structural validation) matches the dependency graph of "names first, types second, topology third." Pass 1 completing before Pass 2 starts eliminates the need for forward-declaration fixups. Confirmed implementable.

### C2. Array-Primary + FrozenDictionary Secondary

`ImmutableArray<TypedField>` primary + `FrozenDictionary<string, TypedField>` secondary follows the `Functions.ByName` pattern exactly. This preserves declaration order for "prior fields only" scope while giving O(1) lookup. Correct.

### C3. Catalog Lookup Strategy (No New Indexes)

`Operations.FindCandidates()` at line 1153, `Operations.FindUnary()` at line 1145, `Functions.ByName` at line 298 — all verified present with the exact signatures the design doc claims. Return types match:
- `FindCandidates`: `ReadOnlySpan<BinaryOperationMeta>` ✅
- `FindUnary`: `UnaryOperationMeta?` ✅
- `ByName`: `FrozenDictionary<string, FunctionMeta[]>` ✅

### C4. Qualifier Disambiguation Logic

The ~15-line structural logic after multi-candidate return is correctly scoped. `BinaryOperationMeta.QualifierMatch` exists as `Match = QualifierMatch.Any` on the record (default `Any`, with `Same` and `Different` variants for money/quantity). The design correctly places this in the checker because qualifier *identity* is field-level knowledge.

### C5. ErrorType Propagation + Partial Results

"Any operation with an `ErrorType` operand produces `ErrorType` result" — correct cascade prevention. "No declaration type is ever skipped due to sub-expression errors" — correct for LS/MCP consumers who need the full inventory even when parts fail. The per-declaration error behavior table (lines 625–631) is explicit and implementable.

### C6. TypedAction DU (3-Shape)

`TypedAction` (base/no-operand), `TypedInputAction` (carries expression), `TypedBindingAction` (carries target binding) — correct structural ownership split. `ActionSecondaryRole` with `Index`/`Key` — correct for the current surface. The invariant `SecondaryRole.HasValue == (SecondaryExpression != null)` is a compile-time-checkable constructor contract.

### C7. TypedExpression DU

All typed expression subtypes are correctly shaped. `TypedBinaryOp.QualifierBinding?` for qualifier propagation, `TypedFunctionCall.ProofRequirements` for proof forwarding, `TypedErrorExpression(Expression Syntax)` for error recovery — all correct.

### C8. CheckContext Design

The mutable working state class is practical. `List<TypedField>` + `Dictionary<string, TypedField>` parallel accumulation during Pass 1, then freezing to `ImmutableArray` + `FrozenDictionary` in Slice 10 — clean lifecycle. `CurrentEventArgs` as a set/clear field for event scope — minimal and sufficient for the non-nested scope model.

### C9. HandlesCatalogMember Migration Protocol

Per-slice remove-from-stub + add-to-real-handler is mechanically correct. The 13 current annotations on `TypeChecker.cs` match the 13 `ExpressionFormKind` enum members exactly (verified by grep). The protocol prevents PRECEPT0019 duplicate-coverage failures.

---

## 3. CONCERNS: Issues Needing Clarification

### CON-1. AST Node Name Discrepancies (Slice 2–3 Blocker if Unresolved)

**Severity:** Medium — won't compile if uncaught

The design doc uses idealized names in the Resolve pseudocode. The **actual AST class names** differ:

| Design Doc Name | Actual AST Class | Notes |
|---|---|---|
| `FunctionCallExpression` | `CallExpression` | Different prefix |
| `GroupedExpression` | `ParenthesizedExpression` | Entirely different word |

These are the types the Resolve function will `switch` on. The design doc's pseudocode at line 69–86 uses the idealized names. When I implement, I'll be matching on `CallExpression` and `ParenthesizedExpression`.

**Recommendation:** Either update the design doc pseudocode to use actual class names, or add a mapping table to the doc. Not a blocker for me (I know the real names), but it will confuse any other reader or AI agent.

### CON-2. PostfixOperation → IsSet/IsNotSet Stub Migration Error

**Severity:** Medium — incorrect protocol step in Slice 6

The design doc (line 714) says:
> **Stub migration:** Remove `IsSet`, `IsNotSet` from stub

But `ExpressionFormKind` has **no** `IsSet` or `IsNotSet` members. There is only `ExpressionFormKind.PostfixOperation` (value 11). Both `IsSetExpression` and `IsNotSetExpression` AST nodes are produced under this single catalog form.

The correct protocol: Slice 6 removes `ExpressionFormKind.PostfixOperation` from the stub and adds it to whichever method handles both `IsSetExpression` and `IsNotSetExpression`.

**Recommendation:** Fix the stub migration note in Slice 6 to reference `PostfixOperation`, not `IsSet`/`IsNotSet`.

### CON-3. ExpressionFormKind.Literal Covers Multiple AST Types Across Slices

**Severity:** Medium — protocol ambiguity

`ExpressionFormKind.Literal` (value 1) covers:
- `LiteralExpression` (Slice 2)
- `TypedConstantExpression` (Slice 4)
- `InterpolatedStringExpression` (Slice 3)
- `InterpolatedTypedConstantExpression` (Slice 4)

The `[HandlesCatalogMember(ExpressionFormKind.Literal)]` annotation is a single unit — it can't be split across Slices 2, 3, and 4. When Slice 2 implements `LiteralExpression`, it must take ownership of `ExpressionFormKind.Literal`, meaning the remaining sub-forms (typed constants, interpolated strings) need stub handling *within the real handler*, not on the dead-letter `CheckExpression` stub.

**The design doc's Slice 3 protocol says:** "Remove `InterpolatedString` from stub" — but there is no such enum member. The annotation `ExpressionFormKind.Literal` will already have migrated to the real handler in Slice 2.

**Recommendation:** Clarify that `ExpressionFormKind.Literal` migrates in Slice 2. Slices 3–4 ADD arms within the already-migrated handler. No further stub migration for these sub-forms.

### CON-4. TypeAccessor DU Return Type Resolution Not Specified

**Severity:** Low-Medium — 10 lines of logic, but needs to be correct

The design doc records `TypedMemberAccess.ResolvedAccessor: TypeAccessor` but does not describe the return-type resolution logic based on the accessor DU subtype:

| Accessor Subtype | Return Type Logic |
|---|---|
| Base `TypeAccessor` | Returns the owning collection's **element type** (e.g., `queue.peek → T`) |
| `FixedReturnAccessor` | Returns `accessor.Returns` directly (e.g., `date.year → integer`) |
| `ElementParameterAccessor` | Always returns `integer` (e.g., `bag.countof(x) → integer`) |

For method calls (`MethodCallExpression`), the parameter type also depends on the subtype:
- `ElementParameterAccessor.ParameterType` is **null** — the parameter type must be derived from the owning field's element type
- `FixedReturnAccessor.ParameterType` — if non-null, use directly

This is exactly the DU pattern from GAP-040 (history.md). The design doc should spell out the 3-arm pattern-match that resolves both return type and parameter type from the accessor subtype.

**Recommendation:** Add a 5-line code block to § Sub-pass 2a or § Slice 3 showing the accessor resolution pattern-match.

### CON-5. Source File Paths in § Source Files Table

**Severity:** Low — cosmetic but will confuse tooling/agents

The Source Files table (lines 810–817) references `src/Precept/Catalogs/Operations.cs` etc. The actual path is `src/Precept/Language/Operations.cs`. The `Catalogs/` directory does not exist.

**Recommendation:** Update the path column to use `src/Precept/Language/`.

---

## 4. MISSING: Items the Design Doesn't Cover

### MISS-1. Quantifier Binding Stack in CheckContext

**Impact:** Slice 9 (Quantifiers)

The design mentions "binding variable scoping (push/pop)" in Slice 9 but `CheckContext` (lines 439–455) has no binding stack data structure. For nested quantifiers like:

```precept
each x in items (each y in x.subitems (y > 0))
```

We need a stack of `(string BindingName, TypeKind BindingType)` in CheckContext. The `CurrentEventArgs` pattern (set/clear) doesn't generalize to nesting.

**Required addition to CheckContext:**

```csharp
public Stack<(string Name, TypeKind Type)> QuantifierBindings { get; } = new();
```

Identifier resolution in `Resolve` must check `QuantifierBindings` before `FieldLookup` to handle binding shadowing.

### MISS-2. Type Widening in Binary Operation Fallback

**Impact:** Slice 2 (Binary & Unary Ops)

The `IsAssignable` helper is documented (line 505–510), and its use in "binary operation lookup fallback (try widened variants)" is mentioned, but the **fallback strategy** itself isn't specified:

1. Call `FindCandidates(op, leftType, rightType)` — if non-empty, done.
2. If empty, try `FindCandidates(op, widen(leftType), rightType)` for each `leftType.WidensTo`?
3. Or try `FindCandidates(op, leftType, widen(rightType))` for each `rightType.WidensTo`?
4. Both sides? Which direction takes priority?

For `integer + number` → should we find `(+, number, number)` by widening the left? Or do we just emit "no matching operation"?

The current `Operations.BinaryIndex` is keyed by exact `(OperatorKind, TypeKind, TypeKind)` tuples. `FindCandidates` does NOT perform implicit widening — it's an exact lookup. So the checker must try widened combinations explicitly.

**Recommendation:** Document the widening fallback strategy in § Expression Resolution, ideally with the priority order (left-widen-first? both-widen? shortest-widen-chain?).

### MISS-3. WidensTo Lookup for IsAssignable

**Impact:** Slice 2+

The `IsAssignable` helper (line 505–510) uses `Types.GetMeta(source).WidensTo.Contains(target)`. This is correct for one-step widening. But is **transitive** widening supported? If `integer → number → decimal`, does `IsAssignable(integer, decimal)` return true?

Looking at the actual `WidensTo` data in Types.cs, `integer` widens to `[number]`, and `number` widens to `[decimal]`. If the checker only checks one hop, `IsAssignable(integer, decimal)` returns false. If it's transitive, it returns true.

**Recommendation:** Explicitly state whether widening is transitive or single-hop. If single-hop (my assumption given the flat `Contains` check), document that as a deliberate choice.

### MISS-4. Function Overload Resolution Disambiguation Strategy

**Impact:** Slice 3 (Functions)

`Functions.ByName` returns `FunctionMeta[]` (an array of function definitions with the same name). Each `FunctionMeta` has multiple `FunctionOverload` entries. The design doc says "overload resolution" but doesn't specify:

1. **Match strategy:** Exact-type match first? Then widening? Then error?
2. **Ambiguity resolution:** If two overloads match after widening, which wins?
3. **Arity filtering:** Filter by parameter count first (eliminating arity mismatches), then type-match?

For `min(amount, 100)` where `amount: money` and `100: numeric literal`:
- Is `100` resolved as `integer` first, then widened to `money`?
- Or does `expectedType` from context propagate into the literal resolution?
- `min` has overloads for `(integer, integer)`, `(decimal, decimal)`, `(money, money)`, etc.

The design's mention of `expectedType` context propagation suggests bidirectional resolution, but the concrete algorithm isn't specified.

**Recommendation:** Add a 10-line pseudocode block for the overload matching algorithm in § Slice 3 or § Expression Resolution.

---

## 5. Slice Dependency Verification

| Slice | Declared Deps | Actual Deps Hold? | Notes |
|---|---|---|---|
| Pre-Slice 0 | None | ✅ | Shape-only, compiles immediately |
| Slice 1 | Pre-Slice 0 | ✅ | Only needs type definitions to compile symbol table logic |
| Slice 2 | Slice 1 | ✅ | Needs symbol tables for `IdentifierExpression` resolution |
| Slice 3 | Slice 2 | ✅ | Extends Resolve with new arms; depends on base function existing |
| Slice 4 | Slice 2 | ✅ | Context propagation uses Resolve infrastructure |
| Slice 5 | Slices 2–4 | ✅ | Guard/action resolution uses full expression engine |
| Slice 6 | Slices 2–4 | ⚠️ | `IsSet`/`IsNotSet` are expression forms, so depends on Slice 2 for Resolve infrastructure. The cycle detection (structural) only depends on Slice 1. Consider splitting Slice 6 into 6a (structural: cycles, choice, forward-ref) and 6b (IsSet/IsNotSet expression forms). |
| Slice 7 | Slice 1 only | ✅ | Correctly independent — modifiers are Pass 1 metadata, no expression resolution |
| Slice 8 | Slices 2–3 | ✅ | CI enforcement needs function resolution from Slice 3 |
| Slice 9 | Slices 2–3 | ✅ | Quantifier needs Resolve + binding scoping |
| Slice 10 | All | ✅ | Final assembly — correct terminal position |

**Parallelism claim verified:** Slices 5, 6, 7 can run in parallel after Slice 4. Slice 7 only needs Slice 1. Slices 8, 9 can run after Slice 3. All correct.

---

## 6. Red Flags: Will Be Painful in Practice

### RF-1. Numeric Literal Type Ambiguity

`LiteralExpression` with `TokenKind.NumberLiteral` can resolve to `integer`, `decimal`, `number`, `money`, or `quantity` depending on context. The `expectedType` parameter is the resolution mechanism, but what happens when there's no context?

Example: `amount > 100` — binary op, `amount: money`, so `100` should resolve as money? Or as integer (with widening to money happening at the binary op level)? The design is silent on whether literal resolution is "bottom-up then widen" or "top-down from expected context."

**My working assumption:** Bottom-up resolution produces `integer` for bare numeric literals. Binary op resolution then finds `(>, money, integer)` or falls back to widening. This is the simpler implementation. If top-down is intended (binary op pushes `money` expectation into the literal), the implementation is more complex because binary ops have TWO operands with independent types.

### RF-2. Computed Field Expression Resolution Scope

Computed field expressions (`computed total = price * quantity`) can only reference fields declared before the current field (forward-reference prohibition). But `CurrentFieldIndex` in CheckContext controls this scope. If computed field resolution happens in Pass 2 (after ALL fields are registered in Pass 1), the field lookup will find ALL fields in the dictionary — including those declared after. The `CurrentFieldIndex` gate must be applied during name resolution for computed expressions specifically.

The design mentions this but doesn't show where the `CurrentFieldIndex < targetField.Index` guard goes. It needs to be in the `IdentifierExpression` resolution arm, conditioned on "currently resolving a computed or default expression."

### RF-3. EventHandler vs TransitionRow Scope Distinction

`TypedEventHandler` (line 250) resolves actions but has **no guard expression** and presumably **no event args scope**. But `EventHandlerNode` might reference event args in action value expressions (e.g., `set amount = args.value`). Does `CurrentEventArgs` get set for EventHandler resolution?

Looking at the design: Slice 5 says "Scope: set `CurrentEventArgs` when entering transition row" — but EventHandlers are not transition rows. They're separate constructs. Either EventHandlers DO have event arg access (in which case scope management must set `CurrentEventArgs` for them too) or they DON'T (in which case `args.value` inside an EventHandler should emit a "not in scope" diagnostic).

**Needs clarification before Slice 5.**

---

## 7. Implementation Readiness Assessment

| Slice | Ready to Implement? | Blocking Questions |
|---|---|---|
| Pre-Slice 0 | ✅ Yes | None — pure type definitions |
| Slice 1 | ✅ Yes | None |
| Slice 2 | ⚠️ Almost | Widening fallback strategy (MISS-2), numeric literal context (RF-1) |
| Slice 3 | ⚠️ Almost | Overload disambiguation (MISS-4), accessor return-type resolution (CON-4) |
| Slice 4 | ✅ Yes (with hardcoded dispatch) | ContentValidation DU timing (acknowledged in doc) |
| Slice 5 | ⚠️ Almost | EventHandler scope clarification (RF-3) |
| Slice 6 | ✅ Yes | PostfixOperation naming fix is cosmetic (CON-2) |
| Slice 7 | ✅ Yes | None — clean, independent |
| Slice 8 | ✅ Yes | None |
| Slice 9 | ⚠️ Almost | Binding stack needed in CheckContext (MISS-1) |
| Slice 10 | ✅ Yes | None |

---

## 8. Summary Table

| Category | Count | Status |
|---|---|---|
| CONFIRMED (correct, implementable) | 9 items | Proceed as designed |
| CONCERNS (need clarification) | 5 items | Fixable with doc updates; none are architectural |
| MISSING (design gaps) | 4 items | Must resolve before their slice begins |
| RED FLAGS (will be painful) | 3 items | Need working assumptions documented |

**Overall verdict:** The canonical doc is implementable. Pre-Slice 0 and Slice 1 can start immediately. Slices 2–3 need the MISS-2 and MISS-4 clarifications before I'm confident the implementation won't need mid-slice redesign. All concerns are within the "10 lines of pseudocode" resolution range — no architectural rework needed.
