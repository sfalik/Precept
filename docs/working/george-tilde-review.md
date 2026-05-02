# George's Review: ~string Composition Analysis

**Reviewer:** George (Senior Language Designer, skeptic)
**Branch:** `spike/Precept-V2`
**Date:** 2026-06-10
**Reviewing:** `research/language/collection-types/tilde-string-analysis.md` (original + Composition Analysis appendix)

---

## Verdict

Frank's composition analysis is thorough on the surfaces he chose to examine, and his central architectural conclusions are sound: `~string` is not a new `TypeKind`, the enforcement model must consist of both rules together or neither, and the `contains` mismatch is the genuinely dangerous gap. However, the analysis has a critical factual error about the current implementation that invalidates his description of the parser change, and it has three composition surface gaps that are real enough to require answers before a lock-in decision: the built-in string function argument surface (a third enforcement case Frank missed entirely), the event argument declaration surface, and `if/then/else` branch type unification. The verdict: **not ready to lock in.** The factual error and the function argument gap both need resolution, and the event arg / conditional type questions need explicit design decisions even if the answers turn out to be simple.

---

## What Frank got right

**1. The two-rule minimum is the right frame.**
Frank's conclusion that rule 1 (equality enforcement) and rule 2 (contains cross-collection mismatch) must ship together or not at all is correct. Shipping rule 1 without rule 2 creates a feature that signals safety without providing it, which is worse than no feature. This is the clearest, most actionable conclusion in the document and I agree with it without reservation.

**2. Assignment compatibility analysis.**
`string → ~string`, `~string → string`, `~string → ~string` — all fine, all for the right reasons. Storage is case-preserving; the CI obligation is a comparison-site constraint, not a storage modifier. The `notempty`/`optional` interaction analysis flows correctly from this model.

**3. Ordering operators are a non-issue.**
`string` doesn't support `<`/`>` at all. `~string` inherits that restriction. No gap, no `~<` operator needed. Confirmed against the spec. Frank is right to dispatch this in one paragraph.

**4. Both-operand enforcement on `==`/`!=`.**
Checking only the left operand would miss `"admin@example.com" == Email`. The enforcement rule must fire when either operand is `~string`-typed. This is a subtle correctness point that Frank got right.

**5. The type model: flag not a new TypeKind.**
`CaseInsensitive = true` on the type reference node, not a new `TypeKind.CaseInsensitiveString`, is the correct call. The proof engine doesn't reason over string value domains; the only consumer that needs the flag is the type checker's comparison-site enforcement. A new `TypeKind` member would cascade across the entire type system for no benefit. Flag is correct.

**6. `lookup of ~string to V` semantics.**
`put "MEDICAL" = 100` then `put "medical" = 200` is an overwrite, not an error. This is consistent with `set of ~string` deduplication and with `put` being an explicit upsert operation by design. The compile-time case for an error doesn't exist — the key values are typically dynamic, so no compile-time collision is detectable. Correct.

**7. The `contains` mismatch diagnostic is well-specified.**
`CaseInsensitiveValueInCaseSensitiveContains` covers `set of string contains ~string value`, `queue`, `stack`, `log`, `bag`, `list`, and `lookup of string to V contains ~string key`. The inverse direction (CI collection, plain string value) requires no diagnostic because the collection's comparer is authoritative. The teaching message template is exactly right.

**8. Empty-string special-case correctly rejected.**
`Email == ""` where Email is `~string` should be an error, not an exception. `Email ~= ""` works; `notempty` handles the structural case. Adding a carve-out for the empty string literal creates a leaky abstraction with no composable generalization. Frank is right to refuse it.

**9. The `~string` field as `rule` and `ensures` argument.**
The enforcement applies everywhere `==` appears, not only in guards. The `no a in Addresses (a == Email)` quantifier case (binding type is `string` from `list of string`, Email is `~string`) correctly triggers `CaseInsensitiveFieldRequiresTildeEquals`. Frank handles this correctly.

**10. Concatenation result type.**
`Email + "@domain.com"` returns `string`, not `~string`. Correct. The CI obligation cannot and should not propagate through string-producing operations — the result's comparison intent is the author's responsibility.

---

## Gaps and concerns

### 1. Critical factual error: code 66 is never emitted and the parser change is additive, not reductive

**What Frank said:** "The parser currently sets `caseInsensitive = true` only inside the collection inner type path... The `CaseInsensitiveStringOnNonCollection` diagnostic is emitted when `~string` appears outside that path. The parser change is: remove the out-of-collection-context error."

**What he missed:** This is factually wrong. `CaseInsensitiveStringOnNonCollection` (code 66) exists in `DiagnosticCode.cs` and `Diagnostics.cs` but is **never emitted anywhere in the codebase**. The parser's `ParseTypeRef()` only handles the `Tilde` token inside the `set|queue|stack` collection branch. In a scalar type position (`field Email as ~string`), the parser encounters `~` (a `Tilde` token that is not in `TypeKeywords`, not a collection keyword, not `choice`), falls into the error path at line 911 of `Parser.Declarations.cs`, and emits `ExpectedToken` — not code 66. Code 66 is an aspirational catalog entry that was defined in anticipation of this feature but never implemented.

**What the correct answer is:** The parser change is not "remove a guard" — it is **add a new Tilde handling path** to `ParseTypeRef()`'s scalar branch. Furthermore, `ScalarTypeRefNode` currently has no `CaseInsensitive` property (confirmed in `TypeRefNode.cs`). Only `CollectionTypeRefNode` has `CaseInsensitive = true/false`. Extending `ScalarTypeRefNode` is required — this is a structural AST change. The implementation scope described in Frank's document is wrong about what changes: it says "one guard removed, one path extended to produce a CI-flagged `ScalarTypeRefNode`" but the reality is that a new token-handling path must be added AND the AST node must be structurally modified.

**What this means for test impact:** Frank says "anything checking for code 66 in tests or tooling must be updated." But there are NO tests that assert code 66 fires on scalar `~string` attempts. The only test reference to code 66 is in `DiagnosticsTests.cs`'s `AllDiagnosticCodes` list, which only tests metadata roundtrip — it does not assert that the diagnostic is actually emitted. The real test gap is the opposite: **there are no parser tests for `field Email as ~string`** that assert the current `ExpectedToken` behavior. When the parser change is made, new tests must be added for the new scalar `~string` parse path. This is a real testing obligation, not just retiring an existing test.

---

### 2. Built-in string functions applied to `~string` fields — a third enforcement case Frank missed entirely

**What Frank said:** Frank covers `.length`, `+` concatenation, and string interpolation. He does not address the built-in string function catalog.

**What he missed:** The spec defines 8 string-operating functions: `startsWith(s, prefix)`, `endsWith(s, suffix)`, `trim(value)`, `toLower(s)`, `toUpper(s)`, `left(s, n)`, `right(s, n)`, `mid(s, start, length)`. All 8 accept `(string, ...) → ...` — they accept `~string` inputs (since `~string` is assignable to `string`) but operate case-sensitively.

Consider:
```precept
field Email as ~string

# This compiles under Frank's two-rule model — but is it semantically correct?
when startsWith(Email, "admin@")   # case-sensitive prefix test on a CI-declared field
```

`startsWith(Email, "admin@")` will miss `"Admin@example.com"`. This is the same class of silent semantic mismatch as `set of string contains ~string value`. The two-rule enforcement model doesn't cover it.

**What the correct answer is:** There are two defensible positions, and Frank needs to take one:

- **Position A (extend enforcement):** Add a third rule: `CaseInsensitiveValueInCaseSensitiveFunction`. When a `~string` field is passed as the first argument to `startsWith` or `endsWith` (the two functions where CI semantics meaningfully differ), emit an error. `trim`, `left`, `right`, `mid` are structural operations where CI semantics don't apply, so they don't need the diagnostic. `toLower`/`toUpper` on a `~string` field is a useful operation (normalizing for storage) so those are also fine.

- **Position B (exclude from enforcement, document the gap):** String function arguments are outside the enforcement scope — the same logic that excludes concatenation excludes function argument positions. Authors who use `startsWith` on a `~string` field understand they're doing a case-sensitive prefix test. Document this in the teaching notes.

Position B is defensible given that Frank explicitly excludes event arg type boundaries from enforcement ("accepted tradeoff"). But Position A is more consistent with the stated goal of the enforcement model: making the dangerous mismatch structurally impossible. This needs a decision, not silence. Frank's two-rule model as stated is incomplete if `startsWith(Email, ...)` compiles without complaint.

---

### 3. Event argument declarations — can `event Foo(Email as ~string)` be declared?

**What Frank said:** Frank covers `~string` fields being passed as event args where the event arg is declared `string`. He does not address whether event arguments can themselves be declared `~string`.

**What he missed:** The event argument grammar is `ArgDecl := Identifier as TypeRef FieldModifier*`. Under the scalar `~string` model, `TypeRef` would now accept `~string` in scalar position. This means `event SubmitForm(Email as ~string)` becomes syntactically valid. What happens?

- `SubmitForm.Email` in a guard or action has type `~string`.
- `SubmitForm.Email == someString` would be a `CaseInsensitiveFieldRequiresTildeEquals` error — correct under the enforcement model.
- But is this semantically meaningful? Event arg values originate from the caller (the application code), which has no knowledge of the `~string` constraint when constructing the event. The CI obligation at the event arg level forces the precept author to write `SubmitForm.Email ~= ...` everywhere they compare the arg — but the arg's value came in as a plain .NET `string` with no comparer attached. There's no enforcement at the caller side.

**What needs resolution:** Is `event Foo(Email as ~string)` intended to be valid under scalar `~string`? If yes, the enforcement model applies as stated. If not, a separate diagnostic (`CaseInsensitiveStringOnEventArg`) is needed. Frank doesn't say which it is. This is a small but real design gap that needs an explicit decision.

---

### 4. `if/then/else` branch type unification is not addressed

**What Frank said:** Frank does not address `if/then/else` expressions involving `~string` branches.

**What he missed:** The spec says: "The `then` and `else` branches must have compatible types (same type, or one widens to the other). The result type is the common type."

Consider:
```precept
field Email as ~string
field Alias as ~string
field Target as string

# What is the result type of this conditional?
set Target = if UseAlias then Alias else Email
```

Both branches are `~string`. What is the result type? Under Frank's model, both are `TypeKind.String` at the storage level. But do they unify to `string` (dropping the CI flag) or to `~string` (preserving it)?

- If the result is `~string`, then assigning it to `Target as string` is a `string → ~string` drop (fine by Frank's assignability rules). But more importantly, `if UseAlias then Email else "admin@example.com"` where the else branch is a plain string literal — what is the result type? One branch is `~string`, the other is `string`. Which wins?

- If CI is dropped in unification (result is always `string`), then `set CIField = if cond then ~string_expr else ~string_expr` silently drops the CI obligation. The author would need to `~=` against the result even if both branches were CI-typed.

**What needs resolution:** Frank's type unification rule for `~string`/`string` combinations in conditional branches needs to be stated explicitly. The most defensible rule is: `~string` and `~string` unify to `~string`; `~string` and `string` unify to `string` (CI flag dropped, same as `~string → string` assignment). This needs to be in the spec, not assumed.

---

### 5. The `lookup of ~string to V` overwrite alternative — not dismissed with adequate rigor

**What Frank said:** Overwrite is correct. One brief paragraph.

**What he missed:** The argument for an error is stronger than Frank acknowledges. `log of T by P` treats duplicate P keys as an error at the `append` site — the spec explicitly requires `when not (F contains P)` as a guard. Why shouldn't `lookup of ~string to V` with a `put` of a case-variant key be at least a warning?

The answer is: `put` is explicitly an upsert (create-or-overwrite) by design, whereas `append` on `log of T by P` is explicitly a non-overwriting append where duplicate keys are structurally impossible by declaration. These are different semantics. The analogy Frank implicitly relies on (set deduplication) is correct, but he should have directly addressed the `log of T by P` contrast. A reviewer wondering "why isn't this an error like `log of T by P`?" deserves an explicit answer: because `put` is upsert, not append-or-fail. Frank should add this sentence.

The overwrite conclusion is correct but the reasoning leaves an obvious hole unfilled.

---

### 6. The implementation scope assessment is wrong in its description (though right in its conclusion)

**What Frank said:** "Parser change: Small. One guard removed, one path extended to produce a CI-flagged `ScalarTypeRefNode`."

**What he missed:** As established in Gap 1, there is no guard to remove. The parser change is:
1. Add Tilde handling to `ParseTypeRef()` in the scalar branch (new code, not a removal)
2. Extend `ScalarTypeRefNode` with a `CaseInsensitive` property (structural AST change)
3. Add parser tests for the new scalar `~string` parse path

This is still "small" in absolute terms, but the nature of the change is different from what Frank described. If the implementation team reads his spec and goes to remove a guard that doesn't exist, they'll lose time. The description needs to be corrected.

Additionally, Frank doesn't address the interaction between the Tilde token scan priority and the scalar `~string` path. The spec section 1.1 says: "A standalone `~` is only valid immediately before `string` in a collection inner type position — elsewhere it is a lexer error." This sentence is **wrong** if scalar `~string` ships. The lexer spec must be updated to say `~` is valid before `string` in both collection inner type position **and** scalar field type position. Frank does mention updating the language spec but doesn't call out this specific sentence.

---

### 7. `choice of ~string(...)` — deliberately excluded, but Frank doesn't say so

**What Frank said:** Nothing. Frank doesn't address `choice of ~string(...)`.

**What needs resolution:** The collection grammar currently defines `ChoiceElementType := string | integer | decimal | number | boolean`. The `~` prefix is explicitly excluded. Under scalar `~string`, should `choice of ~string("draft", "active", "closed")` become valid? A case-insensitive choice would mean "DRAFT" and "draft" are the same choice value.

This is almost certainly out of scope for scalar `~string` — choice values are a finite enumerated set where the comparison is against declared literals, and you can simply normalize choice values to canonical case at the declaration site. But Frank should say so explicitly. An author who reads about scalar `~string` might reasonably try `choice of ~string(...)` next. The spec should explicitly state that `~string` as a `ChoiceElementType` is not valid and why.

---

### 8. The DiagnosticsTests exhaustiveness check is an implicit change obligation Frank understates

**What Frank said:** "Anything checking for code 66 in tests or tooling must be updated."

**What the full picture shows:** Code 66 is in the `TypeCodes` enumeration in `DiagnosticsTests.cs`. The tests that consume this list (`Create_ProducesCorrectCodeString_ForEveryDiagnosticCode`, `Create_ProducesCorrectSeverity_ForEveryDiagnosticCode`, etc.) will continue to pass even after the diagnostic is retired — as long as `CaseInsensitiveStringOnNonCollection` remains in `DiagnosticCode.cs` with valid metadata. The test impact of retirement is zero.

But the inverse concern is real: if `CaseInsensitiveStringOnNonCollection` is REMOVED from `DiagnosticCode.cs` entirely (not just deprecated), the `DiagnosticsTests` exhaustiveness tests will catch the removal automatically because they enumerate all `DiagnosticCode` enum members. The retirement strategy needs to decide: remove the enum member entirely (breaking any consuming code that references `DiagnosticCode.CaseInsensitiveStringOnNonCollection`), or keep the member and mark it with a `Removed` status in the catalog (preserving the enum ordinal, which is good practice). Frank says "mark as `Removed`" — that's the right call — but understates why it's important: the numeric code (66) must not be reused, and the string `"CaseInsensitiveStringOnNonCollection"` must not be re-emitted. Ordinal stability is what makes diagnostic codes safe to reference in tooling.

---

## Recommendation to owner

**Lock in with amendments, not now.** The composition analysis is substantively sound but has one factual error that will waste implementation time (the non-existent parser guard), one genuine enforcement gap that needs a design decision (string function arguments), and two surfaces that need explicit decisions before locking in (event arg declarations, conditional branch type unification). None of these are blocking in the sense that they make the feature unsound — but a lock-in decision should be made with complete information, and these gaps mean the information is not yet complete.

**Required before lock-in:**

1. **Correct the parser change description.** Explicitly state that (a) code 66 is never currently emitted, (b) the parser change is additive (new Tilde handling path in `ParseTypeRef()`), and (c) `ScalarTypeRefNode` needs a `CaseInsensitive` property added.

2. **Take a position on string functions.** Either extend the enforcement model to cover `startsWith`/`endsWith` applied to `~string` fields (a third rule), or explicitly exclude function argument positions and document the gap. Silence is not acceptable.

3. **Take a position on event arg declarations.** State whether `event Foo(Email as ~string)` is intended to be valid. One sentence. If invalid, add `CaseInsensitiveStringOnEventArg` to the diagnostics inventory.

4. **State the `if/then/else` unification rule.** One sentence. Recommend: `~string` + `~string` → `~string`; `~string` + `string` → `string` (CI dropped, same as assignment).

5. **Explicitly exclude `choice of ~string`.** One sentence in the spec. Prevents author confusion.

With these five amendments in place, the lock-in is defensible. Frank's core analysis on assignment compatibility, the two-rule enforcement model, the type model, the lookup semantics, and the diagnostics inventory is solid. The implementation scope is genuine — medium, not trivial — and the "minimum condition: both rules or neither" constraint is the right guardrail.
