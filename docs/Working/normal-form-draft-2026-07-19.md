# Normal Form — Draft for the Obligation-Discharge Matrix

**Status**: Draft — 2026-07-19, produced by the WP-calculator build (matrix-population Slice 1 item 1); revised same day per adversarial review (number-lane soundness, conjunct-decomposition rule made explicit, interpolated-constant keying).
**Destination**: the matrix's Vocabulary § Normalization, after owner review. Not canon; the calculator's code carries no references to this draft (transient-vs-canonical rule) — the rules live in `tools/Precept.MatrixTools/Canon.cs`/`Canonicalizer.cs`, and this document describes them.
**Tests**: every rule below has positive and negative cases in `test/Precept.MatrixTools.Tests/NormalFormTests.cs` (plus the establishment/witness suites for N11–N13).

The matrix pins two normalization rules (comparison-direction flip; operand commutation for commutative operators) and leaves "the full normal form" open. This draft is the full proposal: the complete set of rules the mechanized equality check applies, each with a one-sentence explanation a business-domain author could understand, plus the rules deliberately **not** adopted and the judgment calls flagged for the owner.

Two expressions are **normal-form-equal** iff their canonical trees render to the same key. A guard is **licensed by substitution match** iff its guard-fact set covers the obligation's weakest precondition per N13 below.

## Adopted rules

| # | Rule | Author explanation |
|---|------|--------------------|
| N1 | **Resolved-name identity.** References compare by what they resolve to — a field by its declared name, an event argument by its (event, argument) pair — never by surface spelling. | Two ways of naming the same thing are the same thing. |
| N2 | **Comparison-direction flip** *(matrix-pinned)*: `a <= b` ≡ `b >= a`, `a < b` ≡ `b > a`. | "a is at most b" and "b is at least a" are the same statement. |
| N3 | **Operand commutation** *(matrix-pinned)*: operands of `+`, `*`, `==`, `!=`, `and`, `or` are put in a fixed order. Applies to arithmetic only on the exact lanes (see the lane caveat below). | a + b and b + a are the same sum; "x and y" and "y and x" are the same requirement. |
| N4 | **Associativity flattening**: chains of the same commutative operator are one flat list — `(a + b) + c` ≡ `a + (b + c)`, likewise `and`/`or` chains. Exact lanes only (lane caveat below). | It doesn't matter which two you add first; a three-part sum is one sum. |
| N5 | **Numeric literals compare by value**: `500`, `500.0` are the same number. | Five hundred is five hundred, however you write it. |
| N6 | **Subtraction is addition of the negation**: `a - b` ≡ `a + (-b)`; and `-(-a)` ≡ `a`. Exact lanes only. | Subtracting b is the same as adding negative b; undoing a minus twice gets you back where you started. |
| N7 | **Logical double negation**: `not (not P)` ≡ `P`. | Saying "it is not the case that it is not so" is just saying "it is so." |
| N8 | **Duplicate facts collapse**: `P and P` ≡ `P` (likewise `or`). | Requiring the same thing twice requires it once. |
| N9 | **Constant folding** ⚠ *flagged, see Open items*: arithmetic between literal numbers evaluates (`500 + 500` ≡ `1000`); identity elements drop (`a + 0` ≡ `a`, `a * 1` ≡ `a`); comparisons between literals evaluate to true/false; `and`/`or` absorb literal true/false. Division and modulo are **never** folded. | The compiler does the arithmetic the author could do on paper: 500 + 500 simply is 1000. |
| N10 | **Quantifier bindings are alpha-renamed**: `no x in S (x > 100)` ≡ `no y in S (y > 100)`. | The name of the placeholder variable doesn't matter — only what is required of each element. |
| N11 | **Presence evaluates over substituted values** (establishment only): after substituting the default configuration, `F is set` becomes false when `F` has neither write nor default, and true when a concrete value was substituted. Symbolic operands stay symbolic. | If nothing ever gave the field a value, "the field has a value" is simply false. |
| N12 | **Implications are never folded**: the WP of a conditional rule (`when C`) is kept as "C' implies R'" even when C' folds to a literal (e.g. false). Whether a false activation discharges the obligation (vacuity) is a discharge-contract question, decided in the matrix cell — not by normalization. | The calculator reports "if the rule is active, then …" verbatim; deciding what an inactive rule demands is the definition's job, not the simplifier's. |
| N13 | **WP-conjunct decomposition (guard-fact coverage)** ⚠ *flagged, see Open items*: a guard's top-level `and`-list is its fact set; a WP is guard-licensed when **every** top-level conjunct of the WP appears (normal-form-equal) among the guard's facts. A guard may carry additional facts beyond the WP. | A requirement made of several parts is met when the guard states each part; extra guard conditions don't take anything away. |

**Lane caveat (exactness boundary).** The group treatment behind N3/N4/N6 — commutation, flattening, subtraction-as-negated-addition — applies only to value families with exact group arithmetic: integer, decimal, money, quantity, price, duration, period, exchange rate (per the operand kinds the Operations catalog declares). **`number` (IEEE double) is excluded**: doubles commute under `+`/`*` but do not reassociate — `(1e16 + -1e16) + 1` is `1` while `1e16 + (-1e16 + 1)` is `0` — so reordering/flattening would equate spellings with different runtime values. Number arithmetic keeps its written shape (only spelling-identical number expressions are equal), as do string concatenation and mixed temporal arithmetic (`date + period`).

**Presence-conditioned modifier rules** (enumeration semantics, feeding N11/N12): a modifier on an `optional` field constrains the value only when one is present, so its desugared rule carries the activation condition `Field is set`. Establishment over a default configuration in which the field is unset therefore yields `false implies …` — the vacuous-discharge decision stays with the discharge contract per N12.

## The licensing boundary this normal form draws

- **Per-term bound spellings are licensed premises wherever they appear.** A guard conjunct of the shape *term ⋈ constant* (`Amount1 <= 500`) is a licensed premise feeding the interval-arithmetic derivation — the same derivation that discharges arg-modifier bounds. The calculator exposes these via its bound-fact extractor (`ExtractBoundFacts`); it does **not** run the interval arithmetic (that is the prover's derivation, not normalization). Such a guard is still **not normal-form-equal** to the WP: `Amount1 <= 500 and Amount2 <= 500` and `Amount1 + Amount2 <= 1000` are genuinely different statements — they are connected by a derivation, not by spelling.
- **Algebraic rearrangement is not spelling-equivalence.** Moving a term across an inequality (`Amount1 <= 1000 - Amount2`), scaling both sides, distributing, factoring, or cancelling (`a + b - b`) all stay **outside** the normal form. The tests pin the negative cases.
- **CLI verdicts.** The calculator's CLI reports three guard verdicts per row obligation: *guard normal-form-equal to WP* (whole-condition match), *guard facts cover WP* (N13 coverage), and — for conditional-rule WPs, which are implications — *guard facts cover WP consequent; activation side not decided here* (informational: the consequent is covered, and whether the activation side discharges, e.g. by vacuity, is cell content).

### Non-rules (deliberately not adopted, tested as non-equal)

- Inequality implication (`a <= 500 and b <= 500` vs `a + b <= 1000`) — a derivation, not a spelling.
- Term movement across a comparison (`a - b <= 10` vs `a <= 10 + b`).
- Distribution/factoring (`a * (b + c)` vs `a*b + a*c`).
- De Morgan (`not (P and Q)` vs `not P or not Q`).
- Algebraic cancellation (`a + b - b` vs `a`).
- Negation distribution over sums (`-(a + b)` vs `-a - b`).
- Pushing `not` through comparisons (`not (a <= b)` vs `b < a`) — see Open items.
- Reassociation/commutation on the `number` lane (see the lane caveat).

## Open items flagged for the owner

1. **Constant folding (N9) widens the licensed guard set.** With folding, `when X <= 500 + 500` counts as normal-form-equal to WP `X <= 1000`. Judgment call implemented as the default because an author doing literal arithmetic is restating, not deriving — but it is a genuine widening of "licensed spelling" and the owner should ratify or strike it. Sub-decisions bundled under this flag: identity-element dropping (`a + 0` ≡ `a`), literal-comparison evaluation (`500 <= 1000` ≡ `true` — this is what makes establishment-over-defaults WPs resolve to true/false), and `and`/`or` literal absorption. Division/modulo folding was **excluded** (integer-vs-decimal division semantics would leak evaluator behavior into spelling equality). Note: folding currently has **no off switch** — striking or scoping this rule is a code change to the calculator, not a data toggle.
2. **WP-conjunct decomposition (N13) is itself a licensing decision.** Subset coverage licenses a guard that states the WP's parts as separate conjuncts (and ignores extra guard facts). The narrower alternative — whole-condition match only — would reject a guard that carries any additional fact (e.g. a daily-limit conjunct alongside the overdraft restatement), which the worked bank example relies on. Implemented as coverage; the owner should ratify the decomposition rule or narrow it.
3. **`not` through comparisons** (`not (a <= b)` ≡ `b < a`): sound for totally ordered values, but interacts with optional/unset comparison semantics. Not adopted; candidate for a later power-widening amendment if authors hit it.
4. **Case-insensitive equality folding**: literal `=~` comparisons fold using ordinal-ignore-case. If the language pins a different culture rule, this must follow it.
5. **Typed constants** (`'100 USD'`, dates) compare by the pipeline's parsed value when available (invariant-culture formatted), else raw text — two spellings of the same value are only equal if the pipeline parses them identically. Interpolated typed constants key by result type + static text/magnitude/qualifier + per-slot semantic kind. Not exercised by the current witness set beyond unit tests.
6. **Fields that are `optional` with a default** and **computed fields** in establishment: computed fields are currently left symbolic (out of the single-write scope, pending the mention-set/computed-transitivity decisions); a default expression referencing another field is canonicalized without substitution. Both are edge shapes the population pass should either exercise or explicitly exclude.
7. **Multi-write plans** return NotSupported by design (open write-plan-decomposition decision); establishment through the two-write `OpenAccount` plan in the worked bank example is therefore not yet computable by this tool.
8. **Out-of-scope desugars surface as skip records**, never silences: cross-field bounds (`min OtherField`), accessor-projected bounds (length/count), and satisfaction-less desugars (`maxplaces`) each yield an explicit skipped-obligation record naming the reason.
