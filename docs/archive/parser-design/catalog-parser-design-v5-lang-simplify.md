# Language Surface Simplifications for Parser Complexity Reduction

**By:** George (Runtime Dev)
**Date:** 2026-04-27
**Status:** Analysis for Shane review — no implementation changes proposed without sign-off
**References:**
- `docs/working/catalog-parser-design-v4.md` — Round 4 design (current parser design)
- `docs/language/precept-language-spec.md` — DSL spec (law)
- `docs/archive/language-design/precept-language-vision.md` — language vision (target surface, archived)
- All 28 sample files in `samples/`

---

## Preamble: Where the Parser Complexity Actually Lives

Before proposing changes, I need to be precise about what's hard. The catalog-driven parser has one overwhelmingly dominant complexity source: **the generic disambiguator's pre-disambiguation `when` guard consumption.**

Here's the disambiguator flow (from v4 § 6):

1. Consume leading token (preposition: `in`, `to`, `from`, `on`)
2. Parse anchor target (state or event name)
3. **If `Current() == TokenKind.When`: consume the entire guard expression, stash it**
4. Check `Current()` against each candidate's `DisambiguationTokens`
5. Route to the matched construct
6. Inject stashed anchor and guard into the construct's slot array at specific indices

Steps 1, 2, 4, 5 are mechanical. Step 6 is a simple index assignment. **Step 3 is where the complexity concentrates.** It forces the disambiguator to:

- Invoke the full expression parser before knowing which construct it's building
- Stash the result in a temporary variable
- Track whether a guard was consumed (to inject it at the right slot index)
- Handle the "no guard" fallback path
- Align injection indices with the specific construct's slot sequence

Without step 3, the disambiguator is a trivial token-match router. With it, it's a stateful pre-parser with injection logic.

The secondary complexity sources (G5 anomaly, `write all` LeadingTokenSlot, three-way `from` disambiguation) are all small and mechanically solvable in implementation. They don't warrant language changes.

---

## Empirical Ground Truth: How `when` Is Used in Practice

I grepped all 28 sample files for every `when` occurrence. The results are decisive:

| Pattern | Occurrences | Position relative to verb |
|---------|-------------|---------------------------|
| `from State on Event when Guard -> ...` | ~45 | **Post-verb** (after `on Event`) |
| `rule Expr when Guard because "..."` | 2 | **Post-expression** (after rule body) |
| `ensure Expr when Guard because "..."` | 3 | **Post-expression** (after ensure body) |
| `on Event ensure Expr when Guard because "..."` | 1 | **Post-expression** (after ensure body) |
| `in State when Guard write Field` | 2 | **Pre-verb** (before `write`) |
| `to/from State when Guard -> ...` (state action) | **0** | N/A — never used |
| `in/to/from State when Guard ensure ...` (pre-ensure) | **0** | N/A — never used |

**Key finding:** The pre-verb `when` position is used by exactly ONE construct family: access modes. Two occurrences across all 28 samples. Every other `when` usage is post-verb or post-expression.

**Second key finding:** The spec grammar shows ensures with pre-`ensure` guards: `StateTarget ("when" BoolExpr)? ensure BoolExpr because StringExpr`. But **zero samples use this position.** All three guarded ensures use the post-expression form (`ensure Expr when Guard because "..."`), which is the same pattern as rules. The spec grammar and sample usage are misaligned — the samples represent the intended syntax.

---

## Language Changes That Actually Reduce Complexity

### Proposal A: Move Access Mode Guards to Post-Field Position

**The change:**

```precept
# Before (current):
in UnderReview when not FraudFlag write AdjusterName
in UnderReview when DocumentsVerified write DecisionNote

# After (proposed):
in UnderReview write AdjusterName when not FraudFlag
in UnderReview write DecisionNote when DocumentsVerified
```

**Which complexity it eliminates:**

This is the only pre-verb `when` position used in practice. Moving it to post-field position means:

1. **The disambiguator's step 3 disappears entirely.** After parsing the anchor target, the next token is ALWAYS a disambiguation token — never `when`. The disambiguator becomes:
   - Consume leading token
   - Parse anchor target
   - Match current token against disambiguation tokens
   - Route

   No guard pre-parsing. No guard stashing. No guard injection. No slot index alignment for guards.

2. **The `DisambiguationEntry` record simplifies.** No code path needs to track "did we stash a guard?" because guards are always parsed inside the construct body, not by the disambiguator.

3. **Pre-parsed injection complexity halves.** Only the anchor target needs injection at slot 0. Guard injection at arbitrary slot indices is eliminated.

4. **The SlotOrderingDriftTests for guard position (v4 Task 1) become unnecessary.** There's no guard injection path to test for drift.

**What the user sees:**

The guard moves from before the verb to after the field target. "In UnderReview, write AdjusterName when not FraudFlag" reads naturally in English — arguably MORE naturally than the current form, since the permission (write AdjusterName) is stated first, then scoped (when not FraudFlag).

**The cost:**

- 2 lines change across all 28 sample files
- The spec grammar updates from `in StateTarget ("when" BoolExpr)? (write|read|omit) FieldTarget` to `in StateTarget (write|read|omit) FieldTarget ("when" BoolExpr)?`
- Users who learned the pre-verb form must adjust — but with zero shipped parser, there are no existing users

**Consistency gain:**

After this change, all `when` guards in the language follow the same positional pattern:

| Construct | Guard position |
|-----------|---------------|
| Rules | `rule Expr **when Guard** because "..."` |
| Ensures (state) | `in State ensure Expr **when Guard** because "..."` |
| Ensures (event) | `on Event ensure Expr **when Guard** because "..."` |
| Access modes | `in State write Field **when Guard**` |
| Transition rows | `from State on Event **when Guard** -> ...` |

The guard always appears AFTER the primary content and BEFORE the terminal clause. One pattern, universally applied.

**Verdict: Worth it.** This is the highest-ratio change in this analysis: 2 sample lines changed, core disambiguator complexity eliminated, language consistency improved.

---

### Proposal B: Align Spec Grammar to Actual Ensure Usage (Post-Expression Guards Only)

**The change:**

The spec currently shows:
```
(in|to|from) StateTarget ("when" BoolExpr)? ensure BoolExpr because StringExpr
```

No sample uses the pre-`ensure` guard position. All guarded ensures use:
```
(in|to|from) StateTarget ensure BoolExpr ("when" BoolExpr)? because StringExpr
```

This proposal formalizes the actual usage as the only valid form.

**Which complexity it eliminates:**

Strictly, this is a spec alignment rather than a language change — no sample code changes. But it eliminates a parser obligation: the parser would no longer need to handle `when` between state target and `ensure`. Combined with Proposal A, this means the disambiguator NEVER sees `when` after the anchor target.

Without this alignment, a parser that faithfully implements the spec grammar must handle pre-`ensure` guards even though no sample uses them. That keeps the disambiguator's step 3 alive for a form nobody writes.

**The cost:**

- Zero sample files change (nobody uses the pre-`ensure` form)
- The spec grammar updates
- Any future user who wanted `in State when Guard ensure ...` would need to use `in State ensure Expr when Guard because ...` instead

**Verdict: Worth it.** This is free — it aligns spec to reality and removes a dead code path from the parser obligation.

---

### Proposal C: Treat `when` as a StateAction Disambiguation Token

**The change:**

If state actions ever use `when` guards (currently zero samples), the `when` would come before `->`:
```precept
to State when Guard -> set Field = Value
```

With Proposals A and B applied, `when` after a state target can ONLY mean StateAction (ensures no longer use pre-verb `when`, access modes no longer use pre-verb `when`). So `when` becomes a natural disambiguation token:

```csharp
ConstructKind.StateAction => new(
    ...
    [
        new DisambiguationEntry(TokenKind.To,   [TokenKind.Arrow, TokenKind.When]),
        new DisambiguationEntry(TokenKind.From, [TokenKind.Arrow, TokenKind.When]),
    ]),
```

The disambiguator sees `when` after state target → routes to StateAction. StateAction's body parser handles `when Guard -> ActionChain` internally. No pre-parsing. No stashing.

**Which complexity it eliminates:**

This closes the last possible "what if `when` appears at the disambiguation point?" question. Even if state actions with guards are eventually used, the disambiguator doesn't pre-parse them — it routes to StateAction, which owns the guard parsing.

**The cost:**

- Zero sample files change
- Slightly more entries in the `DisambiguationTokens` arrays
- If Shane later authorizes pre-event guards (`from State when Guard on Event`), this approach makes them naturally invalid because `when` routes to StateAction, not TransitionRow — which is arguably the correct behavior anyway (the spec explicitly excludes pre-event guards)

**Verdict: Worth it.** Zero cost, completes the elimination of pre-disambiguation guard consumption.

---

### Proposal D: Add Intro Token to Rule Body (Fix G5)

**The change:**

```precept
# Before:
rule amount > 0 when condition because "reason"

# After (option 1 — keyword):
rule that amount > 0 when condition because "reason"

# After (option 2 — colon):
rule: amount > 0 when condition because "reason"
```

**Which complexity it eliminates:**

G5 anomaly: `RuleDeclaration`'s first slot has no introduction token. Every other slot parser expects an intro token to signal "this slot is present." The rule body expression follows directly after `rule` with no separator.

Adding an intro token would make rules consistent with the generic slot parsing model. Every slot parser checks for its intro token, finds it (or returns null), done.

**The cost:**

- Every `rule` line in every sample file changes (adds `that` or `:`)
- Adds a reserved keyword (`that`) or repurposes punctuation (`:`)
- `that` reads oddly: "rule that amount > 0" — rule declarations are already assertive statements, `that` adds subordinate-clause tone
- `:` is inconsistent with the rest of the language (no other construct uses colon as separator)

**Why it's not worth the language change:**

The G5 anomaly is cleanly solvable in implementation:

1. Rename the slot kind from `GuardClause` to `RuleBody` (or keep `GuardClause` but have the rule-specific slot parser skip the `when` intro check)
2. The rule body's slot parser calls `ParseExpression(0)` directly — no intro token needed
3. The `BuildNode` switch arm for `RuleDeclaration` reads the slot as an expression regardless

This is 5–10 lines of implementation code. No language change needed.

**Verdict: Not worth it.** Implementation fix is cleaner than a language surface change.

---

## Language Changes That Look Helpful But Don't Help

### Anti-Proposal E: Different Keywords for `from`-Led Constructs

**The idea:** Replace `to/from` state actions with `entering/exiting/leaving` to give each construct a unique leading token, eliminating `from`-three-way disambiguation.

```precept
# Before:
from Submitted -> set SubmittedAt = now()
to Approved -> set ApprovedAt = now()

# After:
exiting Submitted -> set SubmittedAt = now()
entering Approved -> set ApprovedAt = now()
```

**Why it doesn't help:**

1. **The three-way `from` disambiguation is trivial.** After parsing the state target, the disambiguator checks: `On` → TransitionRow, `Ensure` → StateEnsure, `Arrow` → StateAction. Three candidates, three distinct tokens, linear scan. This is O(1) logic, not a complexity driver.

2. **It breaks the preposition discipline.** The language vision is explicit: `in` = residing, `to` = entering, `from` = leaving, `on` = event fires. `entering` and `exiting` are gerunds, not prepositions. They break the consistent grammatical model.

3. **It adds keywords.** Two new reserved words for no real parser benefit.

4. **The real complexity (pre-disambiguation `when`) is unaffected.** Even with unique leading tokens, if access modes still use `in State when Guard write Field`, the `in`-led disambiguator still needs guard pre-parsing.

**Verdict: Not worth it.** Solves a non-problem while breaking a design principle.

### Anti-Proposal F: Punctuation Separators Between Scope and Body

**The idea:** Add colons or semicolons between the state target and the verb to make the boundary explicit.

```precept
# Before:
in Approved ensure Amount > 0 because "..."
from Draft on Submit -> set Name = Submit.Name -> transition Submitted

# After:
in Approved: ensure Amount > 0 because "..."
from Draft: on Submit -> set Name = Submit.Name -> transition Submitted
```

**Why it doesn't help:**

1. **The state target boundary is already unambiguous.** State targets are single identifiers (or `any`). The parser knows when the target ends — it sees a keyword, not another identifier.

2. **Disambiguation still happens after the colon.** The parser still needs to check what follows `in State:` — `ensure`, `write`, etc. The colon adds no routing information.

3. **It adds visual noise to every scoped construct.** Every transition row, every ensure, every access mode gets a colon. For the most-written construct (transition rows), this is pure overhead.

4. **It's inconsistent with the line-oriented, keyword-anchored design principle.** Precept avoids punctuation-heavy syntax deliberately.

**Verdict: Not worth it.** Adds noise, doesn't help routing.

### Anti-Proposal G: Different Arrow for Actions vs. Outcomes

**The idea:** Use `->` for actions and `=>` for outcomes to make the action chain terminator explicit.

```precept
# Before:
from Draft on Submit
    -> set Name = Submit.Name
    -> transition Submitted

# After:
from Draft on Submit
    -> set Name = Submit.Name
    => transition Submitted
```

**Why it doesn't help:**

1. **The action/outcome boundary is already unambiguous.** After `->`, the parser checks: action keyword (`set`, `add`, `remove`, etc.) → action; outcome keyword (`transition`, `no`, `reject`) → outcome. One token peek.

2. **The action chain loop's complexity is in multi-line continuation and error recovery, not in distinguishing actions from outcomes.** Whether the arrow is `->` or `=>`, the loop structure is identical.

3. **It adds a second arrow operator.** The language's "everything flows through `->` visual pipeline" is a deliberate design choice per the spec: "The `->` arrow is deliberately overloaded to create a visual pipeline that reads top-to-bottom."

**Verdict: Not worth it.** Solves a non-problem, breaks a deliberate design choice.

### Anti-Proposal H: Mandatory Parentheses Around Guards

**The idea:** `when (Guard)` instead of `when Guard` to make guard boundaries explicit.

**Why it doesn't help:**

1. **Guard boundaries are already unambiguous.** The Pratt expression parser stops at `when`, `because`, `->`, `ensure`, `on`, and other keywords that have no left-binding power. The guard expression always terminates cleanly.

2. **The complexity is about WHEN the guard is consumed (pre- vs post-disambiguation), not about WHERE it ends.** Parentheses don't change the disambiguation flow.

3. **Adds visual noise to the most common guarded construct (transition rows).** `from State on Event when (long guard expression)` is harder to read than `from State on Event when long guard expression`.

**Verdict: Not worth it.** Addresses the wrong dimension of the problem.

---

## The One Change That Would Help Most

**Move access mode guards from pre-verb to post-field position** (Proposal A), combined with the spec alignment for ensures (Proposal B) and the `when`-as-disambiguation-token for state actions (Proposal C). These three are a package — A is the language change, B and C are spec/catalog adjustments that complete it.

The combined effect:

| Before | After |
|--------|-------|
| Disambiguator has 6-step flow with guard pre-parsing and injection | Disambiguator has 4-step flow: consume, parse anchor, match token, route |
| Pre-parsed injection for both anchor and guard at specific slot indices | Pre-parsed injection for anchor only, always at slot 0 |
| `SlotOrderingDriftTests` needs guard-position tests for multiple constructs | Guard drift tests eliminated (guards parsed inside construct body) |
| `when` position varies by construct (pre-verb for access modes, post-expression for rules/ensures, post-event for transition rows) | `when` position is uniform: always after the primary content |

**Sample file impact:** 2 lines across 28 files:
```precept
# loan-application.precept line 28:
in UnderReview write DecisionNote when DocumentsVerified

# insurance-claim.precept line 26:
in UnderReview write AdjusterName when not FraudFlag
```

**Parser complexity reduction:** The generic disambiguator drops from the hardest piece of the parser to a trivial dispatch function. The pre-parsed injection path loses its guard-handling half. Three test categories become unnecessary.

**Language consistency gain:** Every `when` guard in the language follows one pattern: guard after primary content, before terminal clause. No exceptions.

**Risk:** Near zero. No shipped parser. No existing users. Two sample lines. The new form reads better in English.

---

## Spec Tension Resolved

This analysis surfaces a pre-existing spec tension: the spec grammar for ensures shows `("when" BoolExpr)? ensure` (pre-`ensure` guard), but all sample files use `ensure Expr ("when" BoolExpr)? because` (post-expression guard). Proposal B formalizes what the samples already demonstrate: the ensure guard follows the ensure expression, matching rule guard syntax.

If Shane prefers to keep BOTH guard positions for ensures (pre-`ensure` and post-expression), the disambiguator's pre-disambiguation `when` consumption cannot be fully eliminated — it stays for the pre-`ensure` case. In that scenario, Proposal A still helps (removes access mode pre-verb guards), but the disambiguator keeps step 3 for ensures.

**My recommendation:** Kill the pre-`ensure` guard position. Nobody uses it. The post-expression form matches rules. Consistency wins.

---

## Implementation Implications (If Proposals A+B+C Are Approved)

1. **Spec update:** Ensure grammar changes from `StateTarget ("when" BoolExpr)? ensure BoolExpr because StringExpr` to `StateTarget ensure BoolExpr ("when" BoolExpr)? because StringExpr`.

2. **Spec update:** Access mode grammar changes from `in StateTarget ("when" BoolExpr)? (write|read|omit) FieldTarget` to `in StateTarget (write|read|omit) FieldTarget ("when" BoolExpr)?`.

3. **Catalog update:** `StateAction` `DisambiguationTokens` gain `TokenKind.When` as an additional token.

4. **Sample update:** 2 lines in 2 files (loan-application, insurance-claim).

5. **Parser design update:** Disambiguator step 3 removed. Guard stash variable removed. Guard injection logic removed. Guard drift tests removed.

6. **v4 design doc update:** Task 1 (SlotOrderingDriftTests) loses the guard-position subtests. Task 3 (when-guard sample) is simplified — only the post-event position matters; the pre-event position is naturally excluded.

7. **Language vision update:** Core declaration inventory adjusts the guard position for ensures and access modes.

---

## What I'm NOT Recommending

1. **No G5 language change.** The rule body intro token is an implementation problem, not a language problem. Fix it with a `RuleBody` slot kind or by having the generic slot iterator skip intro-token checks for constructs with `NoIntroToken` metadata.

2. **No new keywords.** All proposals reposition existing syntax — no additions to the keyword set.

3. **No punctuation additions.** No colons, semicolons, or new bracket forms.

4. **No construct merges or splits.** The construct inventory stays at 11. No "merge StateEnsure and EventEnsure" or "split TransitionRow into guarded and unguarded."

5. **No preposition discipline changes.** `in/to/from/on` keep their current meanings and lead the same constructs they do today.

---

## Addendum: When-Post-Field Complexity — Re-evaluation Under New Grammar Proposals

**By:** George (Runtime Dev)
**Date:** 2026-04-28
**Status:** Analysis — no implementation changes proposed without Shane sign-off
**Trigger:** Shane requested re-evaluation of the `when`-post-field complexity finding against two new grammar proposals: (1) `->` operator grammar with adjective-after-field, and (2) a semantic split between `omit` (structural exclusion) and access modes (access-level constraint).

---

### Context Snapshot

The original analysis established one finding that drove everything else:

> **The pre-verb `when` position was the sole source of disambiguation complexity in the access mode grammar.** Moving it to post-field eliminated the disambiguator's guard pre-parsing, stash variable, and injection logic, reducing a 6-step stateful pre-parser to a 4-step trivial token-match router.

That finding was evaluated against the verb-before-field grammar:

```
in StateTarget VERB FieldTarget ("when" BoolExpr)?   ← settled after proposal A
```

Shane has since proposed two structural changes that alter the grammar shape:

1. **`->` operator grammar with adjective-after-field:**
   ```
   in State -> Field ADJECTIVE ("when" BoolExpr)?
   ```
   Example: `in Draft -> Amount editable when not Finalized`

2. **Semantic split:** `omit` (structural exclusion, field absent from state) is categorically distinct from access-mode constraints (field present, mutability modified). The two may warrant separate grammar families.

---

### Q1: Does the `when`-post-field finding still hold under the `->` grammar?

**Yes. It holds and is strengthened.**

The original finding was about the *position* of `when` relative to the disambiguation point. Moving `when` to after the field means the disambiguator never sees a guard before it has committed to a construct — the entire complexity source disappears.

Under `in State -> Field ADJECTIVE ("when" BoolExpr)?`, the `when` guard is even further from the disambiguation point than in the settled verb-before-field grammar. The outer disambiguator sees `->` and routes immediately. The `when` guard is the last token sequence in the production — it's deep inside the construct's body parser by the time the guard token is encountered.

The 4-step disambiguator is preserved exactly:

1. Consume leading token (`in`)
2. Parse anchor target (state name)
3. Match current token against disambiguation tokens (`->` for access mode, `omit` for omit, `ensure` for state ensure)
4. Route

No guard pre-parsing. No stash. No injection. The `when`-post-field finding remains fully operative.

---

### Q2: Does the `->` operator change the parse complexity picture?

**It changes the picture favorably, by a measurable margin.**

Under the settled verb-before-field grammar, step 3 of the disambiguator matched a vocabulary keyword (`write`, `read`, `omit`, or the B2 adjectives `editable`, `fixed`, `omit`) against the candidate set. These are identifier-space tokens — the lexer classifies them as keywords, but they live in the same token-class family as identifiers, and any schema evolution that adds a new access-mode keyword adds to the candidate list.

Under the `->` grammar, step 3 matches a single punctuation operator token. `->` is structurally distinct from all identifier-space tokens. The match is one token kind against one token kind. It cannot collide with field names, adjective keywords, or future vocabulary additions. The disambiguation token set for AccessMode shrinks from `{editable, fixed}` (or whatever final B2 vocabulary is adopted) to `{->}`.

**New concern flagged: field/adjective boundary inside the AccessMode body parser.**

In `in State -> Field ADJECTIVE [when Guard]`, after routing to the AccessMode production, the body parser sees:

```
-> Identifier(Amount) Keyword(editable) ...
```

The `->` is consumed first. Then the body parser reads `Identifier(Amount)` as the field target. Then it reads `Keyword(editable)` as the adjective. The boundary between field and adjective is resolved by the lexer's token classification: if `editable` and `fixed` are registered catalog keywords (TokenKind entries), the lexer emits them as distinct token kinds, not as identifiers. The body parser never faces an identifier-vs-identifier ambiguity.

This is **not a new disambiguation problem.** It is the same reserved-word contract that governs all catalog keywords. The cost is that field names cannot be `editable` or `fixed` — a minor reserved-word footprint expansion. This is not a parser complexity issue; it is a language design constraint that exists for every keyword Precept uses.

**Net result for Q2:** The `->` grammar eliminates the vocabulary-keyword discrimination at the disambiguation point and replaces it with a single-punctuation-token match. Body-parser field/adjective boundary is handled lexically with no lookahead. Parse complexity is **marginally lower** than the settled verb-before-field grammar.

---

### Q3: Does the `omit`/access-mode semantic split affect parser complexity?

**It simplifies the parser by separating two structurally different constructs at the token level.**

Under the current unified model, one production handles all three modes:

```
in StateTarget (write|read|omit) FieldTarget ("when" BoolExpr)?
```

The internal semantics differ significantly: `omit` is unconditional (guarded omit is prohibited), while `write`/`read` (or `editable`/`fixed`) are guarded. The parser's slot array must accommodate this difference internally — a conditional optional slot that is structurally disallowed for `omit` but allowed for the access-mode keywords. This is either an internal branch or a per-mode slot-schema that pretends to be uniform while handling a real distinction.

Under the split model:

- **OmitDeclaration:** `in State omit Field` — no guard slot, full stop. Simpler production.
- **AccessModeDeclaration:** `in State -> Field ADJECTIVE ("when" BoolExpr)?` — guard slot present, always optional.

The disambiguation tokens are entirely distinct:

| Token after `in StateTarget` | Route |
|------------------------------|-------|
| `->` | AccessModeDeclaration |
| `omit` | OmitDeclaration |
| `ensure` | StateEnsure |

Three distinct tokens, three constructs, one-token lookahead. The discriminated-union semantics of the `omit`/access-mode split maps directly onto the token level — the structural difference Shane identified semantically is now visible in the grammar, which means the parser can enforce it without internal branches.

**Two-production model vs. unified model — complexity verdict:**

A unified production with per-mode internal branching is nominally simpler to describe (one rule), but it pushes the structural distinction into the body of the production where it creates a conditional slot that violates the uniform slot-parsing contract. The split model externalizes that distinction to the disambiguation point, making each production simpler and purely structural. Two simple productions > one complex production with an internal guard-omission branch.

---

### Q4: Net Complexity Assessment

Three metrics for comparison: disambiguator steps, body-parser complexity, and test surface.

| Dimension | Original grammar (pre-v5 analysis) | Settled grammar (post-v5, verb-before-field) | Proposed grammar (`->` + adjective + split) |
|-----------|-----------------------------------|--------------------------------------------|---------------------------------------------|
| Disambiguator steps | 6 (includes guard pre-parse, stash, injection) | 4 (trivial token-match router) | 4 (same router; `->` is a punctuation token, not vocabulary keyword) |
| Disambiguation token type at step 3 | Vocabulary keyword (or guard expression) | Vocabulary keyword (`write`/`read`/`omit`) | Single punctuation token (`->`) + `omit` token for omit |
| Guard pre-parsing required? | Yes (step 3 of 6) | No | No |
| Guard stash variable required? | Yes | No | No |
| Guard injection into slot array? | Yes (with index alignment) | No | No |
| `omit`/access-mode split in body parser | Internal branch (conditional guard slot) | Internal branch (conditional guard slot) | None — separate productions, each structurally uniform |
| OmitDeclaration guard slot | Present (conditional, must be rejected) | Present (conditional, must be rejected) | Absent — omit production has no guard slot |
| Reserved-word footprint at disambiguation | `write`, `read`, `omit` (3 keywords) | `write`/`editable`, `read`/`fixed`, `omit` (3 keywords) | `->` (1 punctuation), `omit` (1 keyword) |
| SlotOrderingDrift guard-position tests needed? | Yes | No | No |

**Verdict:** The proposed grammar (`->` operator + adjective-after-field + `omit`/access-mode split) is the simplest of the three grammar shapes evaluated. It is marginally simpler than the already-simplified settled grammar, not because the `when` analysis changes (it doesn't — the finding holds fully), but because:

1. The disambiguation token for access modes is a punctuation operator (`->`), not a vocabulary keyword. It cannot collide with identifiers, requires no keyword-set membership check, and is trivially extended.
2. The `omit`/access-mode split eliminates an internal conditional-guard-slot branch from what was previously a unified production. Each production is now structurally flat.
3. The `when` guard position remains post-field — in fact, post-adjective — which is the furthest possible position from the disambiguation point. The 4-step disambiguator is preserved.

**One caveat:** If the final B2 vocabulary is not adopted (i.e., if `write`/`read` survive rather than `editable`/`fixed`), the `->` grammar still applies, but the adjective token is a verb-form keyword rather than a pure adjective. This does not change the parser complexity assessment — the lexer still classifies it, and the body parser still reads it after the field identifier. The complexity picture is grammar-shape-dependent, not vocabulary-dependent.

**One open question for Shane:** If `omit` keeps the `in State omit Field` shape and access modes use `in State -> Field ADJECTIVE`, do both forms live under one `AccessDeclaration` AST node, or do they become `OmitDeclaration` and `AccessModeDeclaration` as separate SyntaxNode subtypes? This is an AST design question with no parser complexity impact, but it affects the type checker and catalog shape. Flag for Shane before AST design is locked.

---

*Addendum written 2026-04-28. Re-evaluation requested by Shane. No implementation changes proposed; findings are analytical input for Shane's grammar decision.*
