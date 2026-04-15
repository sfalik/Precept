# Comprehensive Grammar Consistency Analysis — Precept DSL

**Date:** 2026-04-14
**Author:** Frank (Lead/Architect & Language Designer)
**Context:** Audit triggered during null-reduction proposal refinement. Shane asked "what other grammar inconsistencies exist?" after identifying that `set`/`not set` would introduce the first postfix operator and shift `not` from always-prefix to dual-position.

---

## Part 1: Consistency Wins

Before cataloging problems, the grammar has substantial areas of principled, clean design. These establish the baseline against which inconsistencies are measured.

**1. Preposition convention is airtight.** `in`/`to`/`from`/`on` carry the same temporal meaning everywhere they appear. Disambiguation is always by the token *after* the identifier (`assert`, `edit`, `->`, `on`). This is LL(1)-clean and never requires backtracking beyond one token. Principle #10 is comprehensively satisfied.

**2. `->` as universal "results in" operator.** Transitions (`-> set X = 1 -> transition Y`), state entry/exit actions (`to X -> set Z = 0`), and computed fields (`field X as number -> Expr`) all use `->` to mean "produces / results in." The arrow uniformly separates *context* from *action/value*. Principle #11 is consistently respected.

**3. Keyword-anchored flat statements.** Every statement starts with a recognizable keyword. No indentation dependency, no section headers, no end markers. The parser, language server, and TextMate grammar all leverage this — completions can fire from the first token. Principles #3 and #9 are deeply served.

**4. `because` as sentinel.** The `because "reason"` pattern is 100% consistent across all constraint-carrying statements (invariants, all three state assert forms, event asserts). It always occupies the same final position. No constraint-carrying statement omits it; no non-constraint statement uses it.

**5. Modifier any-order syntax on field/arg declarations.** `nullable`, `default`, constraints, `ordered` can appear in any order after the type. The parser's discriminated-union `FieldModifier` approach with `ExtractModifiers()` is clean. C70 catches duplicates. This is user-friendly without creating ambiguity.

**6. Collection verb symmetry.** The write verbs that supply a value (`add`, `remove`, `enqueue`, `push`) all use the same `<verb> <collection> <expr>` form. The consume-and-capture verbs (`dequeue`, `pop`) share a form: `<verb> <collection> [into <field>]`. `clear` stands alone as `<verb> <collection>`. The three groups are internally consistent and the distinctions map to semantic reality.

**7. Expression grammar hierarchy.** Atom → Unary → Factor → Term → Comparison → And → Or is a clean, standard precedence ladder. `contains` at the Comparison level alongside `==`/`!=` is appropriate — it's a binary test producing boolean. No precedence anomalies.

**8. Collect-all vs first-match separation.** Validation (invariants, asserts) collects *all* failures. Transition rows use first-match. These are never confused in the grammar — the statement form tells you which strategy applies. Principle #6 is clearly honored.

**9. `with` for argument introduction.** `event E with Arg as string` is consistent across all event declarations, including multi-name forms. No alternative syntax competes.

**10. Scope restriction transparency.** Event asserts = arg-only scope. Transition rows = entity fields + dotted event args. Invariants = entity fields only. Computed fields = entity fields + `.count` only. The narrowing from broadest to most restricted is principled, well-documented, and enforced by distinct diagnostics (C14/C15/C16 for event assert scope, C69 for cross-scope guards, C83/C84/C85 for computed field scope).

---

## Part 2: Inconsistency Catalog

### I-1. `set` — Dual-loaded keyword (Action + Type)

**What it is:** `set` is simultaneously an action keyword (`-> set Field = value`) and a type keyword (`field Tags as set of string`). It's the *only* token in the enum carrying two `[TokenCategory]` attributes (`Action` and `Type`). No other keyword has dual classification.

**Examples:**
```precept
field Tags as set of string         # type keyword
from Draft on Submit -> set Tags = Submit.items -> transition Submitted  # action keyword
```

**Severity:** Structural — grammatical category is formally violated (one token, two categories).

**Principles touched:** #9 (tooling drives syntax — dual classification creates a special case for grammar highlighting and completions), #13 (keywords for domain — the same keyword serves two unrelated domain concepts: "unordered collection" and "assign a value").

**Real problems:** *Minimal in practice.* Disambiguation is LL(1) — after `as`/`of` it's a type, after `->` it's an action. The parser handles it cleanly. IntelliSense offers both forms in context. The TextMate grammar handles it. But every new tooling feature must special-case `set`: the MCP `precept_language` tool lists it in both `actionKeywords` and `typeKeywords`, semantic tokens must classify it differently based on context, and documentation must explain the dual nature.

**Could/should it be fixed?** Renaming the collection type to `hashset`, `bag`, or `unordered` would eliminate the dual use. Breaking cost: every sample file with `set of ...`, the grammar, completions, all tests. Medium cost for a small gain — the current disambiguation works. **Verdict: tolerable, monitor for proposal interactions.**

---

### I-2. `min`/`max` — Triple-loaded keywords (Constraint + Function + Member accessor)

**What it is:** `min` and `max` each serve *three* grammatical roles:

1. **Constraint keyword:** `field Score as number min 0 max 100`
2. **Built-in function name:** `min(a, b)`, `max(a, b)`
3. **Dotted member accessor on sets:** `Tags.min`, `Tags.max`

Yet the `PreceptToken` enum gives them only *one* `[TokenCategory]` attribute: `Constraint`. The parser explicitly works around this by adding `Min`/`Max` tokens to `AnyMemberToken` and `AnyFunctionName` parsers.

**Severity:** Structural — more loaded than `set` (3 roles vs 2), but only one category declared. The vocabulary report from `precept_language` lists them only as `constraintKeywords`, not as functions or accessors.

**Principles touched:** #9 (tooling-driven syntax — the MCP vocabulary is incomplete for these tokens), #2 (English-ish — `min`/`max` are natural English words for all three uses, making the overloading feel natural to humans).

**Real problems:** *Parser ambiguity is real but resolved.* `min 5` after a type reference = constraint. `min(a, b)` with `(` = function call. `Tags.min` after `.` = member accessor. Context suffices for LL(1). But the vocabulary gap means AI consumers relying on `precept_language` don't know `min`/`max` are valid function names or member accessors — they'd have to discover this from the `functions` and `constructs` arrays instead of the keyword classification.

**Could/should it be fixed?** Adding multiple `[TokenCategory]` attributes (like `set` has) would fix the vocabulary gap at no breaking cost. Alternatively, separate function-name tokens would eliminate the triple-loading but add parser complexity. **Verdict: fix the classification gap; the overloading itself is tolerable.**

---

### I-3. `no` — Single-purpose negation keyword for `no transition`

**What it is:** The keyword `no` exists *solely* to form the two-word outcome `no transition`. It appears nowhere else in the grammar. This is the only place two keywords combine into a single semantic unit without an operand between them:

```
transition State     # keyword + identifier
no transition        # keyword + keyword  ← unique
reject "reason"      # keyword + string
```

**Severity:** Cosmetic — the grammar is unambiguous and the form is natural English.

**Principles touched:** #2 (English-ish — "no transition" is perfectly fluent), #3 (minimal ceremony — the alternative `notransition` is less readable, and a symbol like `->!` would violate #13).

**Real problems:** None in practice. The parser handles it trivially. But from a grammar-design purity standpoint, `no` is a keyword that cannot appear independently — it's syntactic sugar for a compound keyword. Every other keyword either starts a statement, follows a preposition, or is an operator.

**Could/should it be fixed?** Could use `stay` or `remain` as a single keyword. Cost: breaking change in all sample files and transition rows that use `no transition`. **Verdict: leave it — "no transition" is maximally readable and the parser cost is zero.**

---

### I-4. `no` vs `not` — Split negation vocabulary

**What it is:** Two different negation words serve different roles:
- `no` — determiner in `no transition` (outcome context)
- `not` — unary logical operator in expressions (`not IsVip`)

**Severity:** Cosmetic — these are linguistically correct English forms (determiner vs adverb) applied to different grammatical contexts.

**Principles touched:** #2 (English-ish — `no transition` and `not IsVip` are both natural English), #13 (keywords for domain — both are keywords, consistent with the framework).

**Real problems:** Potential author confusion: "why `no` here but `not` there?" The answer is grammatical English (you say "no transition" not "not transition", and "not true" not "no true"), but domain experts don't think in POS categories. The constraint keyword `notempty` adds a third negation pattern (see I-5).

**Could/should it be fixed?** Not without violence to English readability. **Verdict: inherent to the English-ish approach. Document the pattern, don't change it.**

---

### I-5. `notempty` vs `nonnegative` — Inconsistent negation prefix in constraint names

**What it is:** The constraint keywords use different English negation prefixes:
- `nonnegative` — uses `non-` prefix (standard English adjective negation)
- `notempty` — uses `not-` prefix (reads as "note-mpty" to fresh eyes)

Standard English and mathematical convention: "nonnegative", "nonempty" (both `non-`). The `not-` prefix breaks the pattern established by `nonnegative`.

**Severity:** Cosmetic — naming convention violated within the constraint keyword family.

**Principles touched:** #2 (English-ish — `nonempty` is more standard than `notempty`), #9 (tooling — IntelliSense could suggest either, but having consistent prefixes aids discoverability).

**Real problems:** An author who sees `nonnegative` and infers the pattern might try `nonempty` and get a parse error. The asymmetry is learnable but gratuitous — there's no semantic reason for different prefixes.

**Could/should it be fixed?** Rename `notempty` to `nonempty`. Breaking cost: every sample file using `notempty`, grammar, completions, all tests. Medium cost but the fix is purely mechanical (find-replace). **Verdict: worth fixing if a breaking-change window opens. It's the simplest inconsistency to resolve.**

---

### I-6. `when` placement varies across statement forms

**What it is:** The `when` optional guard clause appears in *three* different positions relative to the main body of a statement:

| Statement form | `when` position | Reading |
|---|---|---|
| Transition row | `from X on E **when** Guard -> ...` | Before body |
| Invariant / Assert | `invariant Expr **when** Guard because "..."` | After expression, before `because` |
| State edit | `in X **when** Guard edit Fields` | Before `edit` keyword |
| Root edit | `edit Fields **when** Guard` | After field list |

Two specific asymmetries:
1. **State edit vs root edit:** State-scoped puts `when` *before* `edit`; root-level puts `when` *after* the field list.
2. **Transition rows vs constraint declarations:** In transition rows, `when` precedes the action pipeline. In constraints, `when` follows the expression being guarded.

**Severity:** Structural — the same keyword occupies different grammatical slots across statement forms, violating positional consistency.

**Principles touched:** #10 (consistent prepositions — `when` isn't a preposition but carries a fixed meaning "conditional applicability", yet its positional signature varies), #3 (minimal ceremony — each placement reads naturally in its context), #9 (tooling — completions must offer `when` at different cursor positions depending on context).

**Real problems:** Author confusion when learning the language. "Where does `when` go?" has at least three answers. The state edit / root edit divergence (I-7) is the most jarring because they're the same concept (edit declarations) with different syntax due to parsing constraints.

**Could/should it be fixed?** The transition-row position is load-bearing (it determines row applicability before actions execute). The constraint position is also load-bearing (guard conditions the entire constraint). Unifying would require grammar restructuring. The state edit / root edit asymmetry is the actionable target. **Verdict: fix the edit asymmetry if possible; the transition-vs-constraint split is justified.**

---

### I-7. `edit` declaration form differs between state-scoped and root-level

**What it is:** Sub-case of I-6, isolated for clarity:

```precept
# State-scoped: in <State> [when <Guard>] edit <Fields>
in Open when IsVip edit SensitiveField

# Root-level: edit <Fields> [when <Guard>]
edit Priority when Active
```

The `when` clause position is reversed relative to `edit`. For state-scoped, `when` precedes `edit`. For root-level, `when` follows the field list.

**Severity:** Structural — the same declaration concept has different syntax depending on whether states are present.

**Principles touched:** #9 (tooling — completions must handle two different patterns for the same concept), #2 (English-ish — both read naturally but differently: "in Open when VIP, edit Fields" vs "edit Fields when VIP").

**Real problems:** The parser requires this split because `in State` + `when` must be consumed before the `assert`/`edit` disambiguation point. For root-level, there's no leading `in State`, so `when` shifts to trailing position. This is a parser-driven compromise.

**Could/should it be fixed?** Root-level could also accept `when Guard edit Fields` (without the `in` prefix), making both forms `[when Guard] edit Fields`. But this puts `when` at statement-start, which currently isn't a valid statement lead. Alternatively, root-level could accept both orderings. **Verdict: worth prototyping the unified syntax in a future proposal.**

---

### I-8. `ordered` — Classified as constraint but doesn't desugar like constraints

**What it is:** All constraint keywords except `ordered` desugar into invariant or assert expressions at parse time:
- `nonnegative` → `Field >= 0`
- `positive` → `Field > 0`
- `notempty` → `Field != ""` / `Field.count > 0`
- `min N` → `Field >= N`
- etc.

But `ordered` doesn't desugar — it's a type-level flag that *enables* ordinal comparison operators (`<`, `<=`, `>`, `>=`) on choice fields. The parser implements it as a separate `OrderedModifier` record, distinct from `ConstraintModifier`:

```csharp
private sealed record OrderedModifier : FieldModifier;        // ← separate type
private sealed record ConstraintModifier(FieldConstraint Constraint) : FieldModifier;
```

Yet the MCP vocabulary reports `ordered` under `constraintKeywords` alongside `nonnegative`, `positive`, etc.

**Severity:** Structural — `ordered` is categorically different from other constraints (it's a type modifier, not a desugared invariant), but it's classified identically in the vocabulary.

**Principles touched:** #9 (tooling — AI consumers see `ordered` as a constraint and may expect it to desugar), #5 (data truth vs movement truth — constraints desugar to invariants/asserts, but `ordered` modifies comparison behavior).

**Real problems:** Minimal in practice — authors use `ordered` correctly because it only applies to `choice` fields and C66 catches misuse. But conceptually, it's more like `nullable` (a type modifier) than `nonnegative` (a desugared constraint).

**Could/should it be fixed?** Reclassify `ordered` as a Grammar keyword alongside `nullable` and `default`. Or introduce a new "type modifier" category. Breaking cost: vocabulary output changes, grammar highlighting might shift. **Verdict: reclassify when the vocabulary categories are next revised.**

---

### I-9. `contains` — Keyword operator at comparison precedence level

**What it is:** `contains` is the only keyword that appears in infix binary operator position at the comparison precedence level (level 4), alongside symbols `==`, `!=`, `>`, `<`, `>=`, `<=`. The logical keywords `and`/`or`/`not` are also keyword operators, but at lower precedence levels (1, 2, 7).

```precept
# 'contains' parsed identically to '==' in the expression grammar
Tags contains "urgent" and Status != "Closed"
```

In the parser: `contains` is handled through `Superpower.Parse.Chain` at the `Comparison` level, identical to how `==` is handled.

**Severity:** Cosmetic — the design doc explicitly addresses this in the Keyword vs Symbol Framework, and the decision is justified. `contains` is a domain concept (Principle #13) and a keyword is correct.

**Principles touched:** #13 (keywords for domain — `contains` is a domain concept, correctly a keyword), #2 (English-ish — `Tags contains "x"` reads naturally).

**Real problems:** None. The precedence is correct (membership test at comparison level), the keyword form is readable, and the parser handles it identically to symbolic operators. The only theoretical issue is that `contains` has asymmetric typing (left must be collection, right must be scalar), unlike `==` which is symmetric — but this is a type-checker concern, not a grammar concern.

**Could/should it be fixed?** No. This is a correct application of Principle #13. **Verdict: not an inconsistency — it's a principled decision.**

---

### I-10. `any` vs `all` — Different quantifier words for the same idea ("all of them")

**What it is:** Two different words express universal quantification in different contexts:
- `any` for state targets: `from any`, `in any assert`, `in any edit` — means "all declared states"
- `all` for field targets: `edit all` — means "all declared fields"

Both mean "every member of the set." Neither means existential quantification.

**Severity:** Cosmetic — the words are context-appropriate English (`from any state` vs `edit all fields`) and don't overlap in position.

**Principles touched:** #2 (English-ish — both are natural), #10 (consistent prepositions — the quantifiers are quasi-prepositions serving parallel roles).

**Real problems:** An author might try `in any edit any` or `edit all when true` thinking `any`/`all` are interchangeable. They're not — `any` is reserved for state targets, `all` for field targets. The parser rejects `from all` and `edit any`.

**Could/should it be fixed?** Accepting both words in both positions (as synonyms) would eliminate the learning curve. But `from all on Event` reads less naturally than `from any on Event`, and `edit any` reads less naturally than `edit all`. The English idiom difference is real. **Verdict: leave it — the English-ish phrasing is more readable with context-specific quantifiers.**

---

### I-11. Collection action verbs lack glue words where `set` uses `=`

**What it is:** Scalar assignment uses an explicit `=` operator:
```precept
set Field = expr
```

Collection mutations use juxtaposition — no glue word or operator:
```precept
add Tags "urgent"       # add <where> <what>
remove Tags "urgent"    # remove <where> <what>
enqueue Queue "item"    # enqueue <where> <what>
push Stack "item"       # push <where> <what>
```

The "target + value" ordering is consistent within collection verbs, but the lack of any separator between target and value differs from `set`'s `=`.

**Severity:** Cosmetic — the distinction maps to semantic reality: `set` is assignment (requires `=`), collection verbs are mutations (no assignment operator makes sense for "add to a set").

**Principles touched:** #3 (minimal ceremony — removing a glue word saves a token), #2 (English-ish — `add Tags "urgent"` reads like "add-to-Tags urgent", which is terse but workable).

**Real problems:** Minor author confusion: is it `add "urgent" Tags` or `add Tags "urgent"`? The answer (target-first) is consistent across all collection verbs, but the lack of `to`/`from` glue means you have to learn the convention.

**Could/should it be fixed?** Adding `to`/`from` glue (`add "urgent" to Tags`, `remove "urgent" from Tags`) would be more readable but conflicts with Principle #10 (prepositions carry spatial state-machine meaning). Reusing `to`/`from` for collection operations would dilute their precise meaning. **Verdict: leave it — the convention is learnable and the preposition conflict is worse.**

---

### I-12. `dequeue`/`pop` + `into` — Target-last assignment vs `set`'s target-first

**What it is:** Two different patterns for "store a value in a field":
```precept
set Field = expr           # target FIRST, then =, then source
dequeue Queue into Field   # source FIRST, then into, then target
pop Stack into Field       # source FIRST, then into, then target
```

The assignment direction is reversed. `set` writes left-to-right (target = source). `dequeue/pop into` writes left-to-right (source → target).

**Severity:** Cosmetic — both read naturally in English. "Set Field to value" vs "dequeue Queue into Field."

**Principles touched:** #2 (English-ish — both phrasings are natural English), #7 (self-contained rows — both are independently readable).

**Real problems:** Minor cognitive load: "which direction does assignment flow?" differs between `set` and `dequeue/pop into`. But the verbs themselves signal the difference — `set` targets a field, `dequeue` operates on a queue.

**Could/should it be fixed?** The `dequeue/pop into` form is load-bearing: `dequeue Queue` is already a complete statement (discard the front element), and `into Field` is optional. You can't put the optional target first without restructuring. **Verdict: inherent to the optional-capture design. Leave it.**

---

### I-13. `assert` never starts a statement — dependent keyword classified as Declaration

**What it is:** `assert` is classified as a Declaration keyword in the token enum and vocabulary. But it *never* starts a statement — it always follows a preposition + state/event target:
- `in Open assert ...`
- `to Closed assert ...`
- `from Draft assert ...`
- `on Submit assert ...`

Contrast with real top-level declaration keywords: `field`, `state`, `event`, `invariant`, `precept`, `edit` — all of these *start* statements. `assert` is syntactically dependent, more like `because` or `initial` (which are both classified as Grammar, not Declaration).

**Severity:** Structural — keyword classification doesn't match grammatical behavior. `assert` occupies a dependent position but is classified as Declaration.

**Principles touched:** #9 (tooling — completions currently offer `assert` in the keyword list, but it's never valid at statement-start position without a preposition prefix).

**Real problems:** Minimal — the parser tries `assert` statements only after a preposition prefix, so it works correctly. But an author typing `assert` at line beginning gets a parse error without a helpful message pointing to `in/to/from/on`.

**Could/should it be fixed?** Reclassify `assert` as Grammar (dependent keyword). Or introduce a "dependent declaration" sub-category. The completion engine already handles this contextually by offering `in/to/from/on` + `assert` as patterns rather than `assert` alone. **Verdict: reclassification is clean and costs nothing at the grammar level.**

---

### I-14. `initial` — Position-specific modifier in multi-name declarations

**What it is:** In multi-name state declarations, `initial` applies to the *preceding* name only:

```precept
state Draft initial, UnderReview, Approved
#      ^^^^^initial   ^^^^^^^^^^^not initial
```

But in multi-name field declarations, modifiers apply to *all* names:

```precept
field MinAmount, MaxAmount as number default 0
#     ^^^^^^^^^^^^^^^^^^^^^^all get number default 0
```

The granularity of modifier attachment differs: per-name for `initial` on states, per-declaration for modifiers on fields/events.

**Severity:** Cosmetic — the asymmetry maps to semantic reality (only one state can be initial; all fields in a multi-name declaration share a type).

**Principles touched:** #2 (English-ish — "state Draft initial, UnderReview" reads as "Draft is initial, UnderReview is not"), #7 (self-contained — the initial marker is local to the name it follows).

**Real problems:** None in practice — C8 enforces single-initial, so misunderstanding the scope produces a clear error. The convention is learnable.

**Could/should it be fixed?** No — the semantic constraint (only one initial state) drives the per-name attachment. **Verdict: justified asymmetry. No fix needed.**

---

### I-15. `choice(...)` parenthesized syntax vs `set of T` keyword syntax

**What it is:** Types use two different syntactic patterns:

| Type | Pattern | Example |
|---|---|---|
| Scalar types | Bare keyword | `string`, `number`, `boolean`, `integer`, `decimal` |
| Collections | `keyword of keyword` | `set of string`, `queue of number` |
| Choice | `keyword ( literals )` | `choice("A", "B", "C")` |

`choice` is the only type that uses parenthesized syntax. All others use keywords and/or `of`.

**Severity:** Cosmetic — the difference maps to a real distinction: choice members are compile-time string literals, not type references. Parentheses signal "inline literal list," which is different from `of` signaling "inner type."

**Principles touched:** #2 (English-ish — `choice("A","B")` reads like a function call, not like "a choice of A, B"), #3 (minimal ceremony — parentheses are the most compact way to inline a literal set).

**Real problems:** None — the parser handles it cleanly. The pattern is immediately recognizable.

**Could/should it be fixed?** An alternative like `choice of "A", "B"` would unify with the `of` pattern but creates ambiguity (when does the member list end?). Parentheses provide clear boundaries. **Verdict: correct design decision. The asymmetry is justified.**

---

### I-16. Reconstitution bug: `not` unary operator missing space

**What it is:** The `ReconstituteExpr` method in the parser produces `notX` instead of `not X` for unary `not`:

```csharp
PreceptUnaryExpression un => $"{un.Operator}{ReconstituteExpr(un.Operand)}",
```

For unary `-`, this produces `-X` (correct). For unary `not`, it produces `notX` (syntactically invalid — would tokenize as an identifier, not `not` + identifier).

**Severity:** This is an implementation bug rather than a grammar inconsistency, but it reveals that the grammar's keyword operators (`not`) and symbol operators (`-`) aren't handled uniformly in reconstitution. Included here because it indicates the keyword-in-operator-position pattern creates edge cases.

**Principles touched:** #13 (keywords for domain, symbols for math — the reconstitution code treats both uniformly but they require different spacing).

**Real problems:** Reconstructed expression text (used in `ExpressionText` fields and MCP output) would roundtrip incorrectly for `not` expressions. This could mislead AI consumers and break re-parsing.

**Could/should it be fixed?** Yes — add a space after keyword operators in reconstitution. Zero breaking cost since it fixes a bug. **Verdict: fix immediately.**

---

### I-17. `ordered` and `maxplaces` — Constraints that aren't invariant desugars

**What it is:** Most constraint keywords desugar into invariant/assert expressions at parse time. Two don't:
- `ordered` — enables ordinal comparison operators on choice fields (type modifier)
- `maxplaces N` — caps decimal places (runtime enforcement, not a boolean expression)

Both are classified under `constraintKeywords` in the vocabulary and `[TokenCategory(Constraint)]` in the token enum.

**Severity:** Structural — same as I-8 for `ordered`. `maxplaces` is similarly non-desugarable (you can't express "at most N decimal places" as a field expression in the current expression language).

**Principles touched:** #5 (data truth — constraints that don't desugar to invariants occupy an ambiguous space between "data rule" and "type shape").

**Real problems:** Minimal — authors don't need to care about desugaring. But AI consumers and tool developers may expect that all constraints produce visible invariant nodes in the compiled model. They don't for `ordered` and `maxplaces`.

**Could/should it be fixed?** Either introduce a "type modifier" classification for these two, or acknowledge that constraint keywords span two sub-categories: "desugarable" and "non-desugarable." **Verdict: classify more precisely when the category system is revised.**

---

### I-18. Expression scope naming inconsistency for event args

**What it is:** Event arguments are referenced differently depending on context:
- In event asserts: bare name `Amount` OR dotted `Submit.Amount` — both valid
- In transition rows: ONLY dotted `Submit.Amount` — bare names resolve as field references

This is documented and justified (field name collision prevention in mixed-scope contexts). But it means the same value (`Submit.Amount`) has two valid syntactic forms in one context and only one in another.

**Severity:** Structural — not a grammar defect, but a scope-dependent naming asymmetry that must be learned.

**Principles touched:** #4 (locality of reference — event asserts are local to the event, so bare names are unambiguous), #8 (sound analysis — requiring dotted form in mixed scope prevents silent collision bugs).

**Real problems:** An author writes `on Submit assert Amount > 0` in an event assert, then tries `when Amount > 0` in a transition row expecting the same meaning. Instead, `Amount` resolves as a field. The error message (C38 "unknown identifier" if no field named Amount exists, or silent field reference if one does) may not clearly redirect to the dotted form.

**Could/should it be fixed?** The constraint is load-bearing. The alternative (allowing bare arg names in transition rows) creates silent collision bugs. Better error messages could help. **Verdict: keep the rule, improve diagnostic messages for arg-looks-like-field cases.**

---

### I-19. `string` `.length` vs collection `.count` — Different size accessor names

**What it is:** String size: `.length`. Collection size: `.count`. Different names for "how big is this thing?"

**Severity:** Cosmetic — follows .NET conventions (`string.Length` vs `ICollection<T>.Count`).

**Principles touched:** #2 (English-ish — both are natural; "the length of a string" vs "the count of items"), #9 (tooling — completions offer the correct accessor per type, so discoverability isn't an issue).

**Real problems:** An author who knows `.count` on collections might try `.count` on strings (or vice versa). The type checker catches this — the only cost is a failed attempt.

**Could/should it be fixed?** Unifying to one name (e.g., both `.length` or both `.count`) would reduce learning surface. But `.length` on collections or `.count` on strings feels wrong in English. **Verdict: acceptable — .NET conventions are familiar to the target audience.**

---

### I-20. `reject` reason string vs `because` reason string — Parallel syntax, different semantics

**What it is:** Both `reject` and `because` take a string literal:
```precept
reject "Only fraud cancellation allowed"
invariant Balance >= 0 because "Balance cannot go negative"
```

The syntax is identical (`keyword "string"`), but the semantics differ:
- `because "..."` — why the rule exists (author documentation + runtime message on violation)
- `reject "..."` — what to tell the caller (imperative action)

**Severity:** Cosmetic — the parallel syntax is convenient and the different keywords make the distinction clear.

**Real problems:** None. The keywords (`reject` vs `because`) carry the semantic distinction. No author confuses them.

**Verdict: not an inconsistency — it's consistent syntax for different purposes.**

---

### I-21. `into` — Ultra-narrow keyword used only with `dequeue`/`pop`

**What it is:** `into` exists solely for `dequeue Collection into Field` and `pop Stack into Field`. It's a Grammar keyword that appears in exactly two statement forms. Alongside `no` (used only for `no transition`), it's one of the narrowest-use keywords in the language.

**Severity:** Cosmetic — narrow-use keywords increase the reserved word count without proportional utility. But every use of `into` is natural English.

**Principles touched:** #3 (minimal ceremony — `into` reads naturally and avoids needing a new syntax), #2 (English-ish — "dequeue the queue into the field" is perfect English).

**Real problems:** The reserved keyword blocks `into` as an identifier. If a domain needs a field named `Into`, they can use `InTo` (case-sensitive identifiers), but it's a surprise.

**Could/should it be fixed?** Alternatives: `-> Field` (but `->` already means something else), `as Field` (collision with type annotation). `into` is the right word. **Verdict: correct design. The narrow use is inherent to the feature.**

---

## Part 3: Interaction with the Null-Reduction Proposal

The null-reduction proposal involves three changes: (a) `optional` replacing `nullable`, (b) `set`/`not set` or `is set`/`is not set` for presence testing, and (c) `clear` for setting a field to absent. Here's how each interacts with the cataloged inconsistencies:

### `set`/`not set` for presence testing → **Dramatically worsens I-1**

Adding presence testing to `set` creates a *third* role:
1. Type keyword: `field Tags as set of string`
2. Action keyword: `-> set Field = value`
3. Presence operator: `Field is set` / `Field is not set`

This would make `set` the only token with *three* `[TokenCategory]` attributes. The disambiguation remains LL(1) (postfix position vs start-of-action vs type context), but the cognitive load on authors and the special-casing burden on tooling multiplies. Every new tool feature must now handle three cases.

**`is set`/`is not set` variant:** If the syntax uses `is set` (as an infix compound), then `set` appears after `is`, not at statement-start or after `as`. This is disambiguable but adds `is` as a new keyword — introducing a narrow-use copula.

**`not set`:** This introduces `not` in a new role — postfix modifier on `set` rather than unary prefix operator. Currently `not` is ALWAYS a prefix: `not IsVip`, `not (A and B)`. Making `not` also postfix after `set` creates a position inconsistency for the `not` keyword, analogous to I-4.

### `optional` replacing `nullable` → **Neutral to inconsistencies**

`optional` and `nullable` both occupy the same grammatical slot (post-type modifier). Renaming doesn't create or fix any inconsistency — it's a pure vocabulary change.

### `clear Field` for null-assignment → **Worsens I-11 slightly, creates new overloading**

Currently `clear` is a collection-only verb: `clear Tags` empties a collection. Making `clear Field` mean "set to absent" on scalar fields creates dual use:
- Collection: `clear Tags` → empties the collection (items removed)
- Scalar: `clear Name` → sets to absent (value cleared)

Both mean "remove the value," so the semantic is somewhat consistent. But the distinction between "empty a collection" and "null-ify a scalar" maps to different runtime behaviors.

### Overall proposal impact

| Inconsistency | `set`/`not set` | `optional` | `clear Field` |
|---|---|---|---|
| I-1 (`set` loading) | **Worse** (3 roles) | Neutral | Neutral |
| I-4 (`no`/`not` split) | **Worse** (`not` gains postfix role) | Neutral | Neutral |
| I-5 (`notempty` naming) | Neutral | Neutral | Neutral |
| I-6 (`when` placement) | Neutral | Neutral | Neutral |
| I-11 (collection verb glue) | Neutral | Neutral | **Slightly worse** (dual-use `clear`) |
| I-13 (`assert` classification) | Neutral | Neutral | Neutral |
| New issue: `is` keyword | **Worse** (new keyword, new copula) | Neutral | Neutral |

**Net assessment:** The `set`/`not set` form is the most damaging to grammatical consistency. The `is set`/`is not set` form introduces `is` as a new keyword with no other use — a narrow-use keyword like I-21. `clear Field` for null assignment is a modest worsening.

**The cleanest null-reduction syntax from a consistency standpoint** would avoid overloading `set` entirely. If null-state testing used a dedicated operator or keyword (e.g., `Field == null` is already valid), and null assignment used `set Field = null` (already valid for nullable fields), the proposal's consistency cost drops to zero. The question is whether the proposal provides enough readability gain to justify the consistency cost.

---

## Part 4: Ranked Summary

| Rank | ID | Inconsistency | Severity | Verdict |
|---|---|---|---|---|
| 1 | I-6 | `when` placement varies across statement forms | Structural | Fix the edit asymmetry (I-7); transition-vs-constraint split is justified |
| 2 | I-2 | `min`/`max` triple-loaded (Constraint + Function + Accessor) | Structural | Fix classification gap; overloading is tolerable |
| 3 | I-1 | `set` dual-loaded (Action + Type) | Structural | Tolerable — monitor for proposal interactions |
| 4 | I-13 | `assert` classified Declaration but never starts a statement | Structural | Reclassify as Grammar |
| 5 | I-8 | `ordered` classified as Constraint but doesn't desugar | Structural | Reclassify as Grammar/type modifier |
| 6 | I-17 | `maxplaces` also doesn't desugar (same category as I-8) | Structural | Same fix as I-8 |
| 7 | I-18 | Event arg naming (bare vs dotted) varies by scope | Structural | Keep rule, improve diagnostics |
| 8 | I-16 | `not` reconstitution bug (missing space) | Implementation | Fix immediately — zero cost |
| 9 | I-5 | `notempty` vs `nonnegative` — inconsistent negation prefix | Cosmetic | Fix to `nonempty` when breaking-change window opens |
| 10 | I-7 | State edit vs root edit `when` position | Structural | Prototype unified syntax in future proposal |
| 11 | I-10 | `any` vs `all` — different quantifiers for universal | Cosmetic | Leave — English idiom is correct |
| 12 | I-4 | `no` vs `not` — split negation vocabulary | Cosmetic | Leave — inherent to English-ish design |
| 13 | I-3 | `no` — single-purpose keyword for `no transition` | Cosmetic | Leave — maximally readable |
| 14 | I-14 | `initial` — per-name vs per-declaration modifier scope | Cosmetic | Leave — semantically justified |
| 15 | I-15 | `choice(...)` parenthesized vs `set of` keyword syntax | Cosmetic | Leave — justified by literal vs type distinction |
| 16 | I-19 | `.length` vs `.count` — different size accessor names | Cosmetic | Leave — follows .NET conventions |
| 17 | I-12 | `dequeue/pop into` target-last vs `set` target-first | Cosmetic | Leave — inherent to optional-capture design |
| 18 | I-11 | Collection verbs lack glue words | Cosmetic | Leave — preposition conflict is worse |
| 19 | I-21 | `into` — ultra-narrow keyword | Cosmetic | Leave — correct word |
| 20 | I-9 | `contains` as keyword infix operator | Cosmetic | Not an inconsistency — principled P#13 decision |

**Top-priority fix targets:** I-16 (reconstitution bug — free fix), I-5 (`notempty` → `nonempty` — mechanical rename), I-2 (add missing `[TokenCategory]` attributes to `min`/`max`), I-13 (reclassify `assert`), I-8/I-17 (reclassify `ordered` and `maxplaces`).
