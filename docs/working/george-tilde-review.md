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

---

## George's Final Review
**Date:** 2026-05-01

The design docs have incorporated the previous round's feedback. The core model is sound. But I found twelve issues a developer will hit when they sit down to implement. Four of them are blocking — the implementation cannot proceed correctly without resolution. The rest will cause confusion, incorrect code, or subtle bugs.

---

### Blocking

**1. `each` is absent from the spec's token vocabulary (§1.1) and reserved keyword list (§1.2)**

*Location:* `precept-language-spec.md` §1.1 and §1.2; `collection-types.md` §Quantifier Predicates

`collection-types.md` explicitly states: "`each` and `no` require new lexer entries." The spec's token vocabulary (§1.1) has no `Each` entry. The reserved keyword list (§1.2) has `no` (already there for `no transition`) but no `each`. An implementer extending the lexer looks at §1.1 as the ground truth for `TokenKind` entries and reserved keyword registration. The absence of `Each` means they'll add it without any spec guidance on its token category or the keyword string mapping. If `each` is accidentally left unreserved, authors can name fields `each`, which breaks any precept that uses quantifier predicates — silently, after the fact.

*Recommended resolution:* Add `Each | each | Context: quantifier predicate` to §1.1 token vocabulary (Quantifiers/Modifiers category) and add `each` to the §1.2 reserved keyword list, with a note marking it as a quantifier addition alongside `any`/`no`.

---

**2. Quantifier predicate grammar and type-checking are entirely absent from the spec**

*Location:* `precept-language-spec.md` §2.1, §2.2, §3.5, §3.8; `collection-types.md` §Quantifier Predicates

The `QuantifierExpr` grammar exists only in `collection-types.md`. The spec — which is the ground truth for the parser and type checker — has none of it. Concretely:

- **§2.1 null-denotation table** has no entries for `Each`, `Any` (in quantifier/expression role), or `No` (in quantifier role). The parser that reads this table will not know to emit a `QuantifierExpression` node when it sees `each item in Tags (...)`.
- **§2.2 grammar** has no `QuantifierExpr` production. The `in` keyword used inside `each item in Field (...)` is currently spec'd only as a declaration-level preposition. The parser dispatches `in` at the declaration level (transition rows, ensures, access mode) — it has no handling for `in` appearing mid-expression as a quantifier separator.
- **§3.5 scope rules** say nothing about the binding variable. An implementer adding quantifier predicate type-checking has no spec guidance on: (a) what scope the binding variable is visible in, (b) whether it shadows field names, (c) whether its type is always the collection's inner type.
- **§3.8 semantic checks** have no section on quantifier predicate validation — no type rule saying "the predicate must produce `boolean`", no mention of binding variable resolution.

This is not a matter of a few missing sentences. The spec omits an entire grammar production, its null-denotation dispatch, its scope model, and its type-checking rules. An implementer working from the spec alone cannot implement quantifier predicates at all.

*Recommended resolution:* Add a `QuantifierExpr` null-denotation entry to §2.1. Add the grammar production to §2.2 (or cross-reference `collection-types.md` §Quantifier Predicates explicitly as normative). Add binding variable scope rules to §3.5. Add quantifier predicate type-checking rules to §3.8.

---

**3. `notempty` on collections: direct contradiction between spec §3.8 and `collection-types.md`**

*Location:* `precept-language-spec.md` §3.8 modifier validation table; `collection-types.md` §Constraint Catalog

The spec §3.8 modifier validation table says:

| `notempty` | Applicable to: `string` | Error when applied to: collections |

`collection-types.md` §Constraint Catalog says `notempty` is applicable to `set`, `queue`, `stack`, `log`, `bag`, `list`, and `queue of T by P` — with specific semantics ("statically discharges `.min`/`.max`/`.peek`/`.first`/`.last` access obligations").

These are directly contradictory. An implementer writing the modifier-type validation check reads §3.8 (the ground truth for the type checker) and concludes that `field Tags as set of string notempty` is a compile error. That would be wrong.

*Recommended resolution:* Correct §3.8's `notempty` row to say: applicable to `string` and all collection kinds; error when applied to numeric types, `boolean`, `choice`.

---

**4. Diagnostic message templates for all five `~string` diagnostics use hardcoded examples, not parameterized templates**

*Location:* `precept-language-spec.md` §3.10 `~string` enforcement diagnostics table

Every existing diagnostic in §3.10 uses `{0}`, `{1}`, etc. placeholder notation:
- `"Field '{0}' is not declared"` — `{0}` = field name
- `"Expected a {0} value here, but got '{1}'"` — `{0}` = type name, `{1}` = expression

The five new `~string` diagnostics use hardcoded example field names:
- `CaseInsensitiveFieldRequiresTildeEquals`: `'Email' is declared ~string (case-insensitive). Use ~= instead of ==...`
- `CaseInsensitiveFieldRequiresTildeNotEquals`: same pattern, `Email` hardcoded
- `CaseInsensitiveValueInCaseSensitiveContains`: `'Email' is ~string but 'Roles' is set of string...`
- `CaseInsensitiveFieldRequiresTildeStartsWith`: `'Email' is declared ~string...`
- `CaseInsensitiveFieldRequiresTildeEndsWith`: same

An implementer cannot write `string.Format(template, ...)` calls without knowing: how many parameters each diagnostic takes, which parameter is the field name, which parameter is the collection name. As written, these look like static strings — but they must be parameterized or the error message will always say "Email" regardless of the actual field. The `CaseInsensitiveValueInCaseSensitiveContains` case has *two* names (`Email` and `Roles`) that need to be parameters.

*Recommended resolution:* Rewrite all five message templates using `{0}`, `{1}` placeholder notation and document what each placeholder represents, consistent with the existing-codes format.

---

### Important

**5. Tilde token responsibility: lexer spec and parser spec contradict each other**

*Location:* `precept-language-spec.md` §1.1 (Operators section) and §2.1 (null-denotation table)

§1.1 says: "A standalone `~` is valid immediately before `string` in a collection inner type position and as a scalar field type qualifier — **elsewhere it is a lexer error**."

§2.1 null-denotation table says: "`Tilde` (when next token is `startsWith` or `endsWith` identifier) → `CIFunctionCallExpression`."

The parser null-denotation table only fires when the parser *receives* a `Tilde` token. If the lexer rejects `~` in expression position as a lexer error, the parser never gets that token and the `~startsWith`/`~endsWith` production is unreachable. Either the lexer does *not* emit a lexer error for expression-position `~` (which the scan-priority list at §1.5 implicitly confirms — `~` is listed as producing `Tilde` with no contextual restriction), or the parser's `CIFunctionCallExpression` production is dead code. A developer implementing the lexer who reads §1.1 will write a context-check and break `~startsWith`/`~endsWith`.

*Recommended resolution:* Correct §1.1 to say the `Tilde` token is always emitted by the lexer wherever `~` appears (consistent with §1.5 scan order). "Elsewhere it is a lexer error" should be removed. Invalid uses of `Tilde` are caught by the parser (in expression position, wrong identifier follows) and the type checker (in type position, scalar `~` before non-`string`).

---

**6. `Tilde` before wrong identifier in expression position: diagnostic not specified**

*Location:* `precept-language-spec.md` §2.1 null-denotation table

The null-denotation entry for `Tilde` ends with: "any other identifier after a lone `Tilde` in expression position is a **parse error**." "Parse error" is not sufficient specification. The §2.7 parser diagnostics table has several codes. Which one fires? `ExpectedToken`? If so, what are the `{0}` and `{1}` substitutions — what was expected and what was found? An implementer writing this error path needs to know the exact diagnostic code and message. There is no `_other_` catch-all row in the null-denotation table for the Tilde case specifically.

*Recommended resolution:* Specify the diagnostic explicitly: "emits `ExpectedToken` with `{0}` = `startsWith or endsWith`, `{1}` = the actual identifier text, and highlights the `Tilde` + identifier span."

---

**7. `~string + ~string` concatenation result not specified in `primitive-types.md`**

*Location:* `primitive-types.md` §`~string` (Type unification / Concatenation note); `precept-language-spec.md` §3.6 binary operators table

`primitive-types.md`'s type unification table shows:
- `~string` + `~string` → `~string` (for `if/then/else` selection)
- `~string` + `string` → `~string` (for `if/then/else` selection)

The concatenation note immediately below says: "The `+` concatenation operator follows a distinct rule: `~string + string → string`." It does **not** state `~string + ~string → string`.

A developer reading `primitive-types.md` in isolation (which they will — the file is titled the canonical reference for these types) will see the `~string + ~string → ~string` entry in the unification table, not read the concatenation note carefully enough to realize it doesn't address both cases, and implement `~string + ~string → ~string` for the `+` operator. This is wrong. `spec §3.6` has the correct entry (`~string + ~string → string`), but the mismatch between the docs guarantees at least one developer will implement this incorrectly.

*Recommended resolution:* Extend the concatenation note in `primitive-types.md` to explicitly cover both cases: "`~string + string → string` and `~string + ~string → string`."

---

**8. Code 66 reassignment: "reassigned" appears in three places but the concrete enum operation is never specified**

*Location:* `primitive-types.md` §Implementation model; `collection-types.md` §`~string` inner type section; `precept-language-spec.md` §3.10 code-66 note

All three docs say code 66 is "reassigned" to `CaseInsensitiveFieldRequiresTildeEquals`. None say what this means for the C# enum. Three possible interpretations:

1. Rename `CaseInsensitiveStringOnNonCollection` → `CaseInsensitiveFieldRequiresTildeEquals` (keep value 66, change member name)
2. Delete `CaseInsensitiveStringOnNonCollection` and add a new member `CaseInsensitiveFieldRequiresTildeEquals = 66`
3. Add `CaseInsensitiveFieldRequiresTildeEquals` at the end of the enum and update `CaseInsensitiveStringOnNonCollection`'s value to something else

Options 1 and 2 are both "reassignment" of the numeric value. Option 3 would change the numeric code 66 to a different diagnostic, which is not "reassignment" at all. The previous George review (this file, Gap 8) advocated "mark as `Removed`" — but that recommendation was in the context of the original analysis doc and does not appear to have propagated into the design docs. An implementer reading only the current docs faces three plausible interpretations.

*Recommended resolution:* The docs should say: "Rename the enum member `CaseInsensitiveStringOnNonCollection` to `CaseInsensitiveFieldRequiresTildeEquals`. The numeric value 66 is retained. No enum member is deleted, no ordinal shifts. Existing code references to `DiagnosticCode.CaseInsensitiveStringOnNonCollection` will fail to compile — update them to `DiagnosticCode.CaseInsensitiveFieldRequiresTildeEquals`."

---

**9. `CollectionField` in quantifier grammar is an undefined production**

*Location:* `collection-types.md` §Quantifier Predicates, grammar block

The grammar says:

```
QuantifierExpr  :=  QuantifierKind Identifier in CollectionField '(' BoolExpr ')'
```

`CollectionField` is not defined anywhere. The examples show only bare field names (`Items`, `Reviewers`, `Tags`). But an implementer needs to know: is `CollectionField` a bare identifier only? Can it be `EventName.FieldName` where the field is a collection? Can it be a computed field reference? A member access chain? The grammar production needs to be either defined or restricted.

This matters for error recovery: if an author writes `each item in Approve.Tags (...)` and `CollectionField` is restricted to bare identifiers, the error message should say "only field names are allowed here" — not a generic parse failure.

*Recommended resolution:* Define `CollectionField` explicitly. Recommend: restrict to bare field names (`Identifier` only) in v1. State this restriction explicitly and add a diagnostic for member-access-in-quantifier-position.

---

**10. Quantifier binding variable: scope, keyword collision, and type derivation not specified anywhere**

*Location:* `collection-types.md` §Quantifier Predicates; `precept-language-spec.md` §3.5

The spec §3.5 scope rules define six expression contexts. Quantifier predicates are not one of them. As a result:

- **Type derivation:** How does the type checker know the binding variable's type? The docs say "locally scoped to the parenthesized predicate" but never say its type is the collection's inner type. If `Tags` is `set of ~string`, is the binding variable `item` typed `string` or `~string`? This determines whether `item == "admin"` triggers `CaseInsensitiveFieldRequiresTildeEquals`.
- **Shadowing:** If a field named `item` already exists, does `each item in Tags (item > 0)` shadow the field or is it a compile error?
- **Keyword collision:** `each no in Tags (...)` — `no` is a reserved keyword. Is this a parse error or a scoping error?
- **Leakage:** Is the binding variable visible outside the `(...)` parentheses? The docs say "locally scoped to the parenthesized predicate" which implies no, but that's stated in collection-types.md prose, not in the spec's scope model.

*Recommended resolution:* Add a `QuantifierExpr` entry to §3.5 scope rules specifying: binding variable type = collection's inner type; binding variable cannot share a name with a keyword (parse error); binding variable shadows field names with a warning; binding variable scope is strictly the predicate expression between `(` and `)`.

---

### Minor

**11. `contains` type rules in spec §3.6 are incomplete**

*Location:* `precept-language-spec.md` §3.6 `contains` operator section

The `contains` type rule table in §3.6 covers only `set of T`, `queue of T`, `stack of T`, and non-collection (type error). It does not cover `log of T`, `log of T by P` (which has two distinct membership checks — value membership and P-key membership), `bag of T`, `list of T`, `queue of T by P`, or `lookup of K to V`. All of these are documented in `collection-types.md` with their own `contains` semantics.

An implementer implementing the type checker for `contains` from §3.6 alone will emit `TypeMismatch` on valid `log`/`bag`/`list`/`lookup` membership tests.

*Recommended resolution:* Expand the §3.6 `contains` table to cover all nine collection kinds, matching the table already in `collection-types.md` §Membership Operator.

---

**12. `trim`, `toLower`, `toUpper`, `left`, `right`, `mid` with `~string` argument: validity not confirmed**

*Location:* `precept-language-spec.md` §3.7 built-in function catalog; `primitive-types.md` §`~string`

The function catalog lists `trim(s)` as `(string) → string`, `toLower(s)` as `(string) → string`, etc. `primitive-types.md` says "CI semantics do not apply to these functions; no enforcement check." But neither doc confirms that `~string` is a valid argument type for these `(string)` parameter functions.

Since `~string` is bidirectionally assignment-compatible with `string`, `trim(Email)` where `Email` is `~string` should compile — but this is inference, not specification. An implementer might: (a) add an enforcement check for `~string → (string)` function arguments that shouldn't exist, (b) emit `TypeMismatch` incorrectly because the parameter is `(string)` and the argument is `~string`, or (c) assume `~string` is valid and not document the reason.

*Recommended resolution:* Add one sentence to §3.7: "Functions that accept `(string)` parameters also accept `~string` arguments via the bidirectional assignment compatibility rule (§3.8). No enforcement diagnostic is emitted — CI semantics are not applicable to structural operations." This closes the question permanently.
