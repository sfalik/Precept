# Normal Form — Draft for the Obligation-Discharge Matrix

**Status**: Draft — 2026-07-19. Already adversarially reviewed once; that review produced the `number` exclusion in the exactness caveat (a soundness fix, not an original design choice), the explicit N13 coverage rule, and the interpolated-constant keying. Awaiting owner review.
**Destination**: the matrix's Vocabulary § Normalization, after owner review. Not canon yet. The rules are implemented in `tools/Precept.MatrixTools/Canon.cs` and `Canonicalizer.cs`; the code doesn't cite this draft (working docs are transient — code references only canonical docs), so this document describes the rules rather than owning them.
**Tests**: every rule below has positive and negative cases in `test/Precept.MatrixTools.Tests/NormalFormTests.cs` (plus the establishment and witness suites for N11–N13).

The matrix already pins two normalization rules — flipping a comparison's direction, and reordering the operands of commutative operators — and leaves the rest of "the full normal form" open. This document is the full proposal: the complete set of rewriting rules the mechanized equality check applies, each with a one-sentence explanation a business-domain author could follow, plus the rewrites deliberately **not** adopted and the judgment calls flagged for the owner.

A few terms, defined once:

- The **weakest precondition (WP)** of an obligation is the condition the compiler needs to hold before an operation so that the rule holds after it. Proving an obligation means showing this condition is met.
- Two expressions are **normal-form-equal** exactly when the rewriting rules below turn both into the same standard form — same form means equal, different form means not equal. Each expression is rewritten into a standard tree and rendered to a text key; identical keys mean the expressions match, and different keys mean they don't. This is how the checker decides that `a <= b` and `b >= a` are the same statement even though they're spelled differently.
- A **conjunct** is one part of an `and` chain: `A and B and C` has three conjuncts.
- A guard is **licensed by substitution match** exactly when its facts cover the obligation's WP, in the sense rule N13 defines — cover it and the match holds; fail to cover it and it doesn't.

## Adopted rules

| # | Rule | Author explanation |
|---|------|--------------------|
| N1 | **Resolved-name identity.** References compare by what they resolve to — a field by its declared name, an event argument by its (event, argument) pair — never by how the reference was spelled. | Two ways of naming the same thing are the same thing. |
| N2 | **Comparison-direction flip** *(one of the two rules the matrix already pins)*: `a <= b` ≡ `b >= a`, `a < b` ≡ `b > a`. | "a is at most b" and "b is at least a" are the same statement. |
| N3 | **Operand reordering** (commutation) *(the other matrix-pinned rule)*: operands of `+`, `*`, `==`, `!=`, `and`, `or` are put in a fixed order. For arithmetic, this applies only to value families with exact arithmetic (see the exactness caveat below). | a + b and b + a are the same sum; "x and y" and "y and x" are the same requirement. |
| N4 | **Chains flatten**: a chain of the same commutative operator becomes one flat list — `(a + b) + c` ≡ `a + (b + c)`, and likewise `and`/`or` chains. Exact value families only (exactness caveat below). | It doesn't matter which two you add first; a three-part sum is one sum. |
| N5 | **Numeric literals compare by value**: `500` and `500.0` are the same number. | Five hundred is five hundred, however you write it. |
| N6 | **Subtraction is addition of the negation**: `a - b` ≡ `a + (-b)`, and `-(-a)` ≡ `a`. Exact value families only. | Subtracting b is the same as adding negative b; undoing a minus twice gets you back where you started. |
| N7 | **Logical double negation**: `not (not P)` ≡ `P`. | Saying "it is not the case that it is not so" is just saying "it is so." |
| N8 | **Duplicates collapse**: `P and P` ≡ `P`, and likewise for `or`. | Requiring the same thing twice requires it once. |
| N9 | **Constant folding** ⚠ *flagged, see Open items*: arithmetic between literal numbers is evaluated (`500 + 500` ≡ `1000`); identity elements drop (`a + 0` ≡ `a`, `a * 1` ≡ `a`); comparisons between literals evaluate to true/false; `and`/`or` absorb literal true/false. Division and modulo are **never** folded. | The compiler does the arithmetic the author could do on paper: 500 + 500 simply is 1000. |
| N10 | **Quantifier placeholder names don't matter**: the bound variable is renamed to a standard name (alpha-renaming, in the code's terms), so `no x in S (x > 100)` ≡ `no y in S (y > 100)`. | The name of the placeholder variable doesn't matter — only what is required of each element. |
| N11 | **Presence evaluates over substituted values** (applies only when checking establishment — whether a rule holds after construction, i.e. over defaults plus initial-event writes): after substituting the defaults, `F is set` becomes false when `F` has neither a write nor a default, and true when a concrete value was substituted. Operands that stay symbolic stay symbolic. | If nothing ever gave the field a value, "the field has a value" is simply false. |
| N12 | **Implications are never folded**: the WP of a conditional rule (`when C`) is kept as "C' implies R'" even when C' folds down to a literal (e.g. false). Whether a false activation condition discharges the obligation (the vacuous case) is a discharge-contract question, decided in the matrix cell — not by normalization. | The calculator reports "if the rule is active, then …" verbatim; deciding what an inactive rule demands is the definition's job, not the simplifier's. |
| N13 | **WP-conjunct decomposition (guard-fact coverage)** ⚠ *flagged, see Open items*: a guard's top-level `and`-list is its fact set; a WP is guard-licensed when **every** top-level conjunct of the WP appears (normal-form-equal) among the guard's facts. The guard may carry additional facts beyond the WP. | A requirement made of several parts is met when the guard states each part; extra guard conditions don't take anything away. |

**Exactness caveat (which value families the algebra rules apply to).** The reorder/flatten/negate treatment behind N3/N4/N6 applies only to value families with exact group arithmetic: integer, decimal, money, quantity, price, duration, period, exchange rate (per the operand kinds the Operations catalog declares). **`number` (IEEE double) is excluded**: doubles commute under `+`/`*` but do not reassociate — `(1e16 + -1e16) + 1` is `1` while `1e16 + (-1e16 + 1)` is `0` — so reordering or flattening would equate spellings with different runtime values. Number arithmetic keeps its written shape (only spelling-identical number expressions are equal). So do string concatenation and mixed temporal arithmetic (`date + period`).

**Modifiers on optional fields** (this feeds N11 and N12): a modifier on an `optional` field constrains the value only when one is present, so the rule it desugars to carries the activation condition `Field is set`. When establishment is checked over a default configuration in which the field is unset, that yields `false implies …` — and per N12, whether that vacuous case counts as discharged stays with the discharge contract.

## The licensing boundary this normal form draws

- **Per-term bound spellings are licensed premises wherever they appear.** A guard conjunct of the shape *term compared to constant* (`Amount1 <= 500`) is a licensed premise feeding the interval-arithmetic derivation — the same derivation that discharges arg-modifier bounds. The calculator exposes these via its bound-fact extractor (`ExtractBoundFacts`); it does **not** run the interval arithmetic itself (that is the prover's derivation, not normalization). Such a guard is still **not normal-form-equal** to the WP: `Amount1 <= 500 and Amount2 <= 500` and `Amount1 + Amount2 <= 1000` are genuinely different statements — they are connected by a derivation, not by spelling.
- **Algebraic rearrangement is not spelling-equivalence.** Moving a term across an inequality (`Amount1 <= 1000 - Amount2`), scaling both sides, distributing, factoring, or cancelling (`a + b - b`) all stay **outside** the normal form. The tests pin the negative cases.
- **CLI verdicts.** The calculator's CLI reports three guard verdicts per row obligation: *guard normal-form-equal to WP* (the whole condition matches), *guard facts cover WP* (N13 coverage), and — for conditional-rule WPs, which are implications — *guard facts cover WP consequent; activation side not decided here* (informational: the "then" part is covered, and whether the "if" part discharges, e.g. vacuously, is cell content).

### Non-rules (deliberately not adopted, tested as non-equal)

- Inequality implication (`a <= 500 and b <= 500` vs `a + b <= 1000`) — a derivation, not a spelling.
- Term movement across a comparison (`a - b <= 10` vs `a <= 10 + b`).
- Distribution/factoring (`a * (b + c)` vs `a*b + a*c`).
- De Morgan (`not (P and Q)` vs `not P or not Q`).
- Algebraic cancellation (`a + b - b` vs `a`).
- Negation distribution over sums (`-(a + b)` vs `-a - b`).
- Pushing `not` through comparisons (`not (a <= b)` vs `b < a`) — see Open items.
- Reordering or flattening on the `number` lane (see the exactness caveat).

## Open items flagged for the owner

1. **Constant folding (N9) widens the licensed guard set.** With folding, `when X <= 500 + 500` counts as normal-form-equal to WP `X <= 1000`. This is implemented as the default on the judgment that an author doing literal arithmetic is restating, not deriving — but it genuinely widens what counts as a "licensed spelling," and the owner should ratify or strike it. Sub-decisions bundled under this flag: dropping identity elements (`a + 0` ≡ `a`), evaluating literal comparisons (`500 <= 1000` ≡ `true` — this is what makes establishment-over-defaults WPs resolve to true/false), and `and`/`or` absorbing literal true/false. Folding of division and modulo was **excluded**: integer-vs-decimal division semantics would leak evaluator behavior into spelling equality. Note: folding currently has **no off switch** — striking or scoping this rule is a code change to the calculator, not a data toggle.
2. **WP-conjunct decomposition (N13) is itself a licensing decision.** Coverage licenses a guard that states the WP's parts as separate conjuncts, and ignores extra guard facts. The narrower alternative — accept only a whole-condition match — would reject any guard that carries an additional fact (e.g. a daily-limit conjunct alongside the overdraft restatement), which the worked bank example relies on. Implemented as coverage; the owner should ratify the decomposition rule or narrow it.
3. **`not` through comparisons** (`not (a <= b)` ≡ `b < a`): sound for totally ordered values, but it interacts with how comparisons treat optional/unset values. Not adopted; a candidate for a later power-widening amendment if authors hit it.
4. **Case-insensitive equality folding**: literal `=~` comparisons fold using ordinal-ignore-case. If the language pins a different culture rule, this must follow it.
5. **Typed constants** (`'100 USD'`, dates) compare by the pipeline's parsed value when available (formatted with the invariant culture), else by raw text — two spellings of the same value are equal only if the pipeline parses them identically. Interpolated typed constants key by result type + static text/magnitude/qualifier + per-slot semantic kind. Not exercised by the current witness set beyond unit tests.
6. **`optional` fields with a default, and computed fields, in establishment**: computed fields are currently left symbolic (outside the single-write scope, pending the mention-set/computed-transitivity decisions); a default expression that references another field is put in normal form without substitution. Both are edge shapes the population pass should either exercise or explicitly exclude.
7. **Multi-write plans** return NotSupported by design (the write-plan-decomposition decision is still open); establishment through the two-write `OpenAccount` plan in the worked bank example is therefore not yet computable by this tool.
8. **Out-of-scope desugars surface as skip records**, never as silence: cross-field bounds (`min OtherField`), accessor-projected bounds (length/count), and satisfaction-less desugars (`maxplaces`) each yield an explicit skipped-obligation record naming the reason.
