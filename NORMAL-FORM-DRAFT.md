# Normal Form — Draft for the Obligation-Discharge Matrix

**Status**: Draft — 2026-07-19, produced by the WP-calculator build (matrix-population Slice 1 item 1).
**Destination**: the matrix's Vocabulary § Normalization, after owner review. Not canon; not referenced from code.
**Implementation**: `tools/Precept.MatrixTools/` (`Canon.cs` holds every rule below; `Canonicalizer.cs` maps the pipeline's typed AST into it). Every rule has positive and negative tests in `test/Precept.MatrixTools.Tests/NormalFormTests.cs`.

The matrix pins two normalization rules (comparison-direction flip; operand commutation for commutative operators) and leaves "the full normal form" open. This draft is the full proposal: the complete set of rules the mechanized equality check applies, each with a one-sentence explanation a business-domain author could understand, plus the rules deliberately **not** adopted and the judgment calls flagged for the owner.

Two expressions are **normal-form-equal** iff their canonical trees render to the same key. A guard is **licensed by substitution match** iff it (or one of its top-level `and`-facts) is normal-form-equal to the obligation's weakest precondition.

## Adopted rules

| # | Rule | Author explanation |
|---|------|--------------------|
| N1 | **Resolved-name identity.** References compare by what they resolve to — a field by its declared name, an event argument by its (event, argument) pair — never by surface spelling. | Two ways of naming the same thing are the same thing. |
| N2 | **Comparison-direction flip** *(matrix-pinned)*: `a <= b` ≡ `b >= a`, `a < b` ≡ `b > a`. | "a is at most b" and "b is at least a" are the same statement. |
| N3 | **Operand commutation** *(matrix-pinned)*: operands of `+`, `*`, `==`, `!=`, `and`, `or` are put in a fixed order. | a + b and b + a are the same sum; "x and y" and "y and x" are the same requirement. |
| N4 | **Associativity flattening**: chains of the same commutative operator are one flat list — `(a + b) + c` ≡ `a + (b + c)`, likewise `and`/`or` chains. | It doesn't matter which two you add first; a three-part sum is one sum. |
| N5 | **Numeric literals compare by value**: `500`, `500.0` are the same number. | Five hundred is five hundred, however you write it. |
| N6 | **Subtraction is addition of the negation**: `a - b` ≡ `a + (-b)`; and `-(-a)` ≡ `a`. | Subtracting b is the same as adding negative b; undoing a minus twice gets you back where you started. |
| N7 | **Logical double negation**: `not (not P)` ≡ `P`. | Saying "it is not the case that it is not so" is just saying "it is so." |
| N8 | **Duplicate facts collapse**: `P and P` ≡ `P` (likewise `or`). | Requiring the same thing twice requires it once. |
| N9 | **Constant folding** ⚠ *flagged, see Open items*: arithmetic between literal numbers evaluates (`500 + 500` ≡ `1000`); identity elements drop (`a + 0` ≡ `a`, `a * 1` ≡ `a`); comparisons between literals evaluate to true/false; `and`/`or` absorb literal true/false. Division and modulo are **never** folded. | The compiler does the arithmetic the author could do on paper: 500 + 500 simply is 1000. |
| N10 | **Quantifier bindings are alpha-renamed**: `no x in S (x > 100)` ≡ `no y in S (y > 100)`. | The name of the placeholder variable doesn't matter — only what is required of each element. |
| N11 | **Presence evaluates over substituted values** (establishment only): after substituting the default configuration, `F is set` becomes false when `F` has neither write nor default, and true when a concrete value was substituted. Symbolic operands stay symbolic. | If nothing ever gave the field a value, "the field has a value" is simply false. |
| N12 | **Implications are never folded**: the WP of a conditional rule (`when C`) is kept as "C' implies R'" even when C' folds to a literal (e.g. false). Whether a false activation discharges the obligation (vacuity) is a discharge-contract question, decided in the matrix cell — not by normalization. | The calculator reports "if the rule is active, then …" verbatim; deciding what an inactive rule demands is the definition's job, not the simplifier's. |

Scope notes on the group treatment (N3/N6): `+`/`*` commute and `-` rewrites only over value families that form arithmetic groups (integer, decimal, number, money, quantity, price, duration, period, exchange rate — per the operand kinds the Operations catalog declares). String concatenation and mixed temporal arithmetic (`date + period`) keep their operand order.

## The licensing boundary this normal form draws (owner ruling, 2026-07-19)

- **Per-term bound spellings are licensed premises wherever they appear.** A guard conjunct of the shape *term ⋈ constant* (`Amount1 <= 500`) is a licensed premise feeding the interval-arithmetic derivation — the same derivation that discharges arg-modifier bounds. The calculator exposes these via its bound-fact extractor (`ExtractBoundFacts`); it does **not** run the interval arithmetic (that is the prover's derivation, not normalization). Such a guard is still **not normal-form-equal** to the WP: `Amount1 <= 500 and Amount2 <= 500` and `Amount1 + Amount2 <= 1000` are genuinely different statements — they are connected by a derivation, not by spelling.
- **Algebraic rearrangement is not spelling-equivalence.** Moving a term across an inequality (`Amount1 <= 1000 - Amount2`), scaling both sides, distributing, factoring, or cancelling (`a + b - b`) all stay **outside** the normal form. The tests pin the negative cases.

### Non-rules (deliberately not adopted, tested as non-equal)

- Inequality implication (`a <= 500 and b <= 500` vs `a + b <= 1000`) — a derivation, not a spelling.
- Term movement across a comparison (`a - b <= 10` vs `a <= 10 + b`).
- Distribution/factoring (`a * (b + c)` vs `a*b + a*c`).
- De Morgan (`not (P and Q)` vs `not P or not Q`).
- Algebraic cancellation (`a + b - b` vs `a`).
- Negation distribution over sums (`-(a + b)` vs `-a - b`).
- Pushing `not` through comparisons (`not (a <= b)` vs `b < a`) — see Open items.

## Open items flagged for the owner

1. **Constant folding (N9) widens the licensed guard set.** With folding, `when X <= 500 + 500` counts as normal-form-equal to WP `X <= 1000`. Judgment call implemented as the default because an author doing literal arithmetic is restating, not deriving — but it is a genuine widening of "licensed spelling" and the owner should ratify or strike it. Sub-decisions bundled under this flag: identity-element dropping (`a + 0` ≡ `a`), literal-comparison evaluation (`500 <= 1000` ≡ `true` — this is what makes establishment-over-defaults WPs resolve to true/false), and `and`/`or` literal absorption. Division/modulo folding was **excluded** (integer-vs-decimal division semantics would leak evaluator behavior into spelling equality).
2. **`not` through comparisons** (`not (a <= b)` ≡ `b < a`): sound for totally ordered values, but interacts with optional/unset comparison semantics. Not adopted; candidate for a later power-widening amendment if authors hit it.
3. **Case-insensitive equality folding**: literal `=~` comparisons fold using ordinal-ignore-case. If the language pins a different culture rule, this must follow it.
4. **Typed constants** (`'100 USD'`, dates) compare by the pipeline's parsed value when available, else raw text — two spellings of the same money value are only equal if the pipeline parses them identically. Not exercised by the current witness set.
5. **Fields that are `optional` with a default** and **computed fields** in establishment: computed fields are currently left symbolic (out of the single-write scope, pending the mention-set/computed-transitivity decisions); a default expression referencing another field is canonicalized without substitution. Both are edge shapes the population pass should either exercise or explicitly exclude.
6. **Multi-write plans** return NotSupported by design (open write-plan-decomposition decision); establishment through the two-write `OpenAccount` plan in the want-doc example is therefore not yet computable by this tool.
