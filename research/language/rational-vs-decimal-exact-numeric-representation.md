---
status: Cited — amended 2026-07-20 (see "Amendments")
authored: 2026-07-19
amended: 2026-07-20
author: research sub-agent (commissioned by owner Shane via /research)
topic: native representation of exact non-integer numbers (p/q rationals vs arbitrary-precision decimal) and what it implies for collapsing Precept's three numeric lanes toward a single exact type
external-engagement: strong
---

# Exact Rational (p/q) vs Arbitrary-Precision Decimal: Prior Art for a Single Exact Numeric Type

> A neutral survey of how programming languages natively represent exact non-integer numbers,
> written to give a future `/design` pass an honest, balanced evidence base — including the
> strongest reasons **not** to collapse Precept's three numeric lanes (`integer` / `decimal` /
> `number`) toward a single exact type. It proposes **no decision**; it lays out tradeoff clusters.

## Background

Precept currently ships three numeric lanes (`docs/language/primitive-types.md` § Numeric Lane Rules):
`integer` (exact whole), `decimal` (exact base-10 fractional), `number` (IEEE 754 double, approximate).
The lanes never mix implicitly; crossing into `number` requires the explicit `approximate()` bridge.
The separation is grounded in `docs/philosophy.md` "honesty about approximation."

The owner is exploring a simplification: collapse toward a **single exact numeric type** — with
`integer` as sugar and floating-point dropped — and is leaning toward **exact rationals (p/q)** as
that single type. This survey tests that idea against the prior art. It builds forward from three
existing internal surveys rather than re-deriving them:

- `research/architecture/compiler/exact-decimal-arithmetic-survey.md` — System.Decimal / BigDecimal / IEEE 754 decimal / SQL DECIMAL mechanics (the decimal-lane grounding).
- `research/architecture/compiler/interval-vs-value-evaluation-prior-art-2026-06-05.md` — proof-engine bounds-reasoning prior art (Cousot/Miné interval domain, SPARK, Liquid Haskell, CUE).
- `research/architecture/compiler/currency-precision-coupling-survey.md` — money/precision comparators (Joda-Money, JSR-354).

**Verified Precept-internal facts** (this session, against committed source):
- The proof engine already represents every static numeric magnitude as .NET `decimal`:
  `NumericRuleFact(string FieldName, OperatorKind Comparison, decimal Value)` and
  `TryGetStaticNumericValue(out decimal value)` in `src/Precept/Pipeline/ProofEngine.*.cs`.
- The `.precept` corpus (78 files) uses `as number` **0** times, `as integer` **223** times,
  `as decimal` **76** times. (The commissioning brief cited 218/73; re-count gives 223/76 — same
  direction: `number` is unused; exact lanes carry the whole corpus.)
- `primitive-types.md` § Open Questions **explicitly defers** the integer-overflow model
  (fixed-width 64-bit prove-or-reject *vs* arbitrary-precision) — so arbitrary precision is already
  an open candidate, not a settled rejection.

**Precept already ships an exact rational type (added 2026-07-20 — this survey originally missed it).**
This materially changes the framing: the question is not whether to *introduce* rational arithmetic, but
how far to carry the rational representation that already exists.

- **`UcumExactFactor`** (`src/Precept/Language/Ucum/UcumExactFactor.cs`) is an exact rational:
  `BigInteger Numerator` / `BigInteger Denominator` plus an `int Base10Exponent`. The constructor
  reduces by GCD and factors powers of ten out into the exponent, keeping the pair small.
  `Multiply`, `Divide` and `Pow` are exact. There is no `Add`/`Sub` — unit scale factors only ever
  compose multiplicatively.
- **It is the representation of every unit's scale**, not an incidental helper: `UcumAtom`,
  `UcumPrefix` and `UcumParsedUnit` all carry it, and derived-unit reduction composes it exactly
  (`UcumAtomCatalog.UnitEvaluation.Multiply/Divide/Pow`). This is the same integer-pair representation
  the ERP survey found in SAP and Dynamics, plus a base-10 exponent.
- **The irrational-scale problem is already handled structurally.** `UcumAtom.ScaleIsRational` records
  whether a unit's scale-to-base is an exact rational; it is `false` for the plane-angle family (whose
  reduction passes through a stored rational *approximation* of `[pi]`) and for logarithmic units, and
  it propagates transitively — a derived unit is exact-rational only if every atom in it is. Documented
  at `docs/language/catalog-system.md` § UCUM atom metadata. Area 4's transcendental gap is therefore
  already tracked at the catalog level for units, rather than being wholly open.
- **Exactness stops at application, not at composition.**
  `TypedConstantNormalizer.ApplyFactor(decimal magnitude, UcumExactFactor factor)` is
  `magnitude * FactorToDecimal(factor)`, and `NormalizeResult` rounds to 24 places
  (`MidpointRounding.ToEven`). So the factor chain is exact right up to the point it meets a value, then
  lands in `decimal`. Concretely: inch→mm (× 25.4) is exact; mm→inch (× 5/127) has no finite decimal and
  rounds here.
- **The performance measurement this survey listed as an open question already exists.**
  `tools/Precept.Bench` benchmarks `decimal` against `UcumExactFactor` and against a `BigRational`
  prototype (same shape, *with* `Add`/`Sub`) explicitly to inform the fixed-decimal vs
  arbitrary-precision decision, and prints a denominator blow-up demonstration.
- **There is prior locked design on this exact territory.** The exact-rational scale representation, the
  `ScaleIsRational` flag, and the rejection of float factors (citing Pint's silent precision loss) were
  decided in a price-cross-unit-cancellation design that also cites Indriya's `RationalConverter`
  ("quotient of two `BigInteger`") as precedent. Any design pass here is extending a locked decision,
  not opening a new question.

## Methodology

- **Research question.** How do languages natively represent exact non-integer numbers (p/q rationals
  vs arbitrary-precision decimal), and what does that prior art imply for collapsing Precept's three
  numeric lanes toward one exact type (rational), with `integer` as sugar and float dropped?
- **Search strategy.** Primary language/standard docs fetched live: Python `fractions`, Ruby
  `Rational`, Julia manual, Haskell `Data.Ratio`, Raku `Rat`, Racket reference, Scheme R5RS §6.2,
  Common Lisp HyperSpec, Clojure reference, Wolfram docs, SMT-LIB theory of Reals, IBM/Cowlishaw
  General Decimal Arithmetic FAQ. Internal Precept research + committed source for the Precept legs.
- **Inclusion / exclusion.** Included: languages with a *native or stdlib* exact-rational or
  exact-decimal type, and the standards that finance/verification actually standardize on. Excluded:
  third-party-only rational libraries in languages without native support (e.g. C++ Boost.Rational,
  Java `BigFraction`) — the question is about *native* representation; excluded full interactive
  provers (Coq/Isabelle) — out of Precept's no-opaque-solver scope.
- **Source-grade mix.** Predominantly Primary (language specs, stdlib docs, SMT-LIB standard,
  Cowlishaw decimal FAQ, Precept committed source + internal all-Primary surveys). Wolfram is
  Secondary (vendor docs). A small number of performance/blowup claims are Tertiary (field knowledge)
  and flagged in Threats to Validity.
- **Time bounds.** All external fetches 2026-07-19. Load-bearing excerpts mirrored to
  `research/references/rational-numeric-representation/source-excerpts.md`.

## Findings

### Area 1 — Native / stdlib rational support (comparator table)

Every cell is grounded by the verbatim excerpts in the mirror file (all fetched 2026-07-19; Primary
unless noted). "Default?" asks whether a *decimal-looking* literal (`0.1`, `3.14`) is a rational by
default, or whether rational is opt-in via an explicit operator / constructor / import.

| Language | Rational type & literal | Exactness guarantee | Position in numeric tower | Rational the default for `0.1`-style literals? |
|---|---|---|---|---|
| **Scheme (R5–R7RS)** | exact rational, literal `1/3` | "+ should always produce exact results when given exact arguments" | rational level of the exact/inexact tower | **No** — `.`-literals are inexact unless prefixed `#e`; rational arises from exact `/` |
| **Racket** | `1/3` exact rational | "adding, multiplying, subtracting, and dividing exact numbers always produces an exact result … limited only by available memory" | full numeric tower, exact by default for integer/ratio | **No** — a literal with a decimal point is inexact (float) |
| **Common Lisp** | `ratio`, literal `3/4` | "greatest common divisor is one … denominator is positive and greater than one" (canonical) | `rational = integer ∪ ratio`, under `real` | **No** — `0.1` is a float; ratio arises from `/` on integers |
| **Clojure** | `Ratio`, literal `22/7` | "Division of integers that can't be reduced to an integer yields a ratio … rather than a floating point or truncated value" | JVM tower; `Ratio` + `BigInt` + `BigDecimal` | **No** — `0.1` is a double; ratio only from integer `/` |
| **Haskell** | `Data.Ratio`, `n % d`, `type Rational = Ratio Integer` | reduced form, "no common factor and the denominator is positive" | `Ratio a` over `Integral a`; `Fractional` class | **No** — opt-in (`import Data.Ratio`); decimal literals are `Fractional` (usually `Double`) |
| **Python** | `fractions.Fraction`, `Fraction(1,3)` | "immutable"; "in lowest terms … denominator … Guaranteed to be positive" | stdlib `numbers.Rational` | **No** — opt-in stdlib import; `0.1` is a `float` |
| **Ruby** | `Rational`, literal `2/3r` | "an exact number, which helps you to write programs without any rounding errors"; auto-"Reduced" | core `Numeric`; `Integer`⊂`Rational` | **No** — bare `0.1` is a `Float`; rational needs the `r` suffix |
| **Julia** | `Rational`, `//` operator, `3//5` | "exact ratios of integers"; "reduced to lowest terms" | core type; promotes to float on mixing | **No** — `0.1` is `Float64`; rational needs `//` |
| **Raku (Perl 6)** | `Rat`; **decimal literals `0.1` are `Rat`**; also fraction forms `<1/3>`, `2/3`, `⅔` | exact, until denominator overflow | `Rat` ⊂ `Real`; `FatRat` for unbounded | **YES** — "Number literals with a dot but without exponent produce Rats." |
| **Smalltalk** | `Fraction` | exact; result of non-integer integer division; reduced | `Fraction` ⊂ `Number` | **No** — literal `0.1` is a `Float`/`ScaledDecimal` |
| **Wolfram/Mathematica** (Secondary) | exact rational `452/62` reduced; symbolic `Sqrt[2]` | exact/symbolic by default | full symbolic tower | **No** — "Whenever you give a number with an explicit decimal point, the Wolfram Language produces an approximate numerical result." |

**The single most design-relevant pattern:** across eleven native-rational systems, **only Raku makes
a rational the default for a decimal-looking literal.** Everyone else keeps float (or an inexact
decimal) as the default for `0.1`, and treats rational as *opt-in* — via an explicit operator (`//`,
`%`, `r`-suffix), a constructor (`Fraction(...)`), an import, or as the *result of exact integer
division only*. This is the first substantive counter-signal to "make one exact type the single
default": the field overwhelmingly does **not** do that with rationals.

### Area 2 — The exact/inexact distinction, and whether Precept rediscovers the Scheme tower

Scheme formalized exactness (R5RS §6.2.2, Primary): *"A number is exact if it was written as an exact
constant or was derived from exact numbers using only exact operations,"* and inexact "if it was
derived using inexact ingredients … or inexact operations." The bridge is explicit named procedures
(§6.2.5): `exact->inexact` "returns an inexact representation … numerically closest to the argument,"
and `inexact->exact` its inverse.

**Precept is a partial rediscovery of this tower — already.** Precept's `decimal` (exact) vs `number`
(inexact IEEE 754), with the `approximate()` function as the one-way `decimal → number` bridge, is
structurally the Scheme exact/inexact split with a named coercion. `primitive-types.md` even mirrors
Scheme's *asymmetry*: crossing to inexact is a deliberate, named act; the exact lane stays exact.

The difference is **which value lives at the exact level.** Scheme's exact level is *rational* (p/q).
Precept's exact level is *base-10 decimal*. So the owner's proposal is not "invent a tower" — the
tower exists — it is: **move the exact level's representation from base-10 decimal to p/q rational,
and delete the inexact level.** Framed that way, Areas 4–6 are exactly the tests of whether that move
holds up.

### Area 3 — Known failure modes and mitigations

- **Denominator blowup.** Repeated addition of unlike fractions grows the denominator toward the LCM
  of all denominators involved; unbounded rational arithmetic can produce very large numerator/
  denominator bignums even when the *value* is small. Every surveyed system mitigates the constant
  factor by **reducing to lowest terms** (Python "lowest terms," Ruby "Reduced," Julia "reduced to
  lowest terms," Haskell/CL "no common factor"), but reduction does not bound growth in the general
  case. **Direct evidence that the blowup is real enough to design around: Raku.** Its default `Rat`
  is *not* unbounded — "On overflow of the denominator during an arithmetic operation a `Num`
  (floating-point number) is returned instead," with unbounded precision available only by opting in
  to `FatRat`. A language that adopted rational-by-default judged the blowup severe enough to bake in
  a silent float fallback plus a denominator ceiling (Primary; docs.raku.org, 2026-07-19).
- **Performance of bignum rational math.** Each op is a pair of bignum ops plus a GCD reduction;
  this is materially slower than fixed-width `decimal`/`double` for hot arithmetic. *(Tertiary —
  field knowledge; not separately excerpt-grounded. The Raku ceiling is the corroborating primary
  signal that designers treat unbounded rational cost as a real constraint.)*
- **Display / rendering.** The rendering policy is where rational-native systems diverge sharpest from
  business notation. Python prints `Fraction(1, 3)`; Julia prints `1//3`; a computed one-third is
  shown as the *fraction*, not `0.333…`. Raku is the counter-model: a `Rat` *displays* as a decimal —
  `say 1/3` outputs `0.333333`, with the exact fraction available only on request via `.raku`
  (`say (1/3).raku; # OUTPUT: «<1/3>»`, docs.raku.org/type/Rat, verified 2026-07-20). So the
  "1/3 vs 0.333" surface is a *policy choice independent of the backing*: you can have rational backing
  with decimal display (Raku) or rational backing with fraction display (Python/Julia).
- **Decimal display of an exact rational is itself lossy (added 2026-07-20).** Raku stores one-third
  exactly and renders `0.333333`. That is an approximation reaching the reader **with no marker**. For
  Precept, whose stated commitment is that exact and approximate behavior be visible in the type system
  and public surface, "exactly stored, silently truncated when rendered" is a cost the Raku-hybrid shape
  carries and this survey originally missed. *(The exact digit count came via the search summarizer and
  is unconfirmed; the decimal-not-fraction display behavior is corroborated by the primary `.raku`
  example above.)*

### Area 4 — The transcendental gap (the strongest technical obstacle to "drop float")

`sqrt`, `log`, `sin`, `cos`, `exp` produce irrational results with **no** finite p/q (or finite
decimal) form. Rational-native languages resolve this in one of two ways, and neither is "stay
rational":

1. **Fall back to inexact/float.** Scheme's `sqrt` on an exact argument is only "*desirable (but not
   required)* … to produce exact answers whenever possible (for example the square root of an exact 4
   ought to be an exact 2)" (R5RS, Primary) — i.e. a perfect square may stay exact, but `sqrt 2`
   yields an inexact number. Racket, Julia, Python `math.sqrt`, Ruby all return a float for
   non-perfect-square roots. The inexact type is *required to exist* to receive the result.
2. **Keep it symbolic (full CAS).** Wolfram keeps `Sqrt[2]` unevaluated as an exact symbolic object,
   collapsing to a decimal only under `N[]` (Secondary; Wolfram docs). This needs a computer-algebra
   engine — far outside Precept's compile-time-proof / no-opaque-solver scope.

**Implication for Precept:** supporting any transcendental function **forces an inexact type back into
existence** in a rational world, unless Precept either (a) refuses transcendentals entirely, or (b)
becomes a symbolic algebra system. Precept already quarantines `sqrt` in the `number` lane
(`primitive-types.md`: "sqrt() lives exclusively in the number lane … Future functions (log, sin,
cos, exp) will also be number-lane-only"). So "drop floating-point" and "keep sqrt/log/trig" are in
direct tension: the closure test at `primitive-types.md` § "A function keeps its decimal overload iff
the operation is closed over finite decimals" is exactly the same test that would keep them out of a
rational lane — irrationals are not closed over rationals either. Dropping `number` does not delete
the need for an inexact result home; it only removes the place to put it.

### Area 5 — Proof-engine feasibility: rationals vs decimals for bound/interval reasoning

The pivotal algebraic fact: **rationals are closed under +, −, ×, ÷ (except ÷0); base-10 decimals are
not closed under ÷.** `1/3` has no finite base-10 form. The internal exact-decimal survey documents
both real-world consequences of that non-closure:

> System.Decimal "does not throw on non-terminating decimal division (e.g., 1m / 3m). Instead, the
> result is rounded to 28–29 significant digits …" — while Java BigDecimal `divide()` on a
> non-terminating result throws `ArithmeticException("Non-terminating decimal expansion; no exact
> representable decimal result.")`
> — `research/architecture/compiler/exact-decimal-arithmetic-survey.md` (Primary, internal)

So for a prover, decimal division forces a choice between *silent rounding* (System.Decimal — a
`honesty about approximation` hazard) and *throwing* (BigDecimal). Rationals sidestep it: `1/3` is
represented exactly, and any linear combination of rationals stays exact. **This is why the
verification field reasons over exact rationals, not decimals:** the SMT-LIB theory of Reals
interprets the sort as "the set of all real numbers," defines `/` as the real-division function, and
its model values are exact rationals — Real values "include all terms of the form (/ m n)" with m, n
coprime (Primary; smt-lib.org, 2026-07-19). Z3 and other solvers implement `Real` with exact rational
arithmetic (rational simplex / Fourier–Motzkin) precisely because closure under the field operations
makes the arithmetic sound without rounding.

**Bearing on Precept's proof engine.** Precept's prover today stores magnitudes as .NET `decimal`
(`NumericRuleFact(..., decimal Value)`) — the *non-closed* representation. Every division-involving
bound it reasons about inherits System.Decimal's silent-round-at-28-digits behavior. Moving the
prover's internal numeric representation to exact rationals (`BigInteger` numerator/denominator) would
make division-derived bounds **exactly** representable, aligning Precept with the SMT-Real semantics
its interval/relational reasoning already resembles (per the interval-vs-value prior-art survey). The
solver-free static-analysis survey already contemplates a **rational** coefficient path (TVPI-rational)
as "strongly-polynomial," so rational coefficients in the prover are neither novel nor off-limits.

Net for Area 5: **rationals simplify the proof engine's exactness story for division; decimals
complicate it.** This is the one area where the evidence points *toward* the owner's lean — but note
it argues for a *rational internal prover representation*, which is a narrower and more clearly-good
change than "rational as the language's single author-facing type."

### Area 6 — Counter-evidence (weighted heavily): finance converged on decimal, not rational

No surveyed business-rule engine, financial platform, or data-interchange standard stores or exchanges
rationals. The entire commercial stack standardized on **decimal**: .NET `System.Decimal`, Java
`BigDecimal`, IEEE 754-2008 **decimal** floating point (decimal64/128), SQL `NUMERIC`/`DECIMAL`, COBOL
`COMP-3` packed decimal, REXX, IBM GDAS — all base-10 scaled integers, none rational (grounded in the
internal exact-decimal survey + the currency-precision survey). The reasons, from the primary source
that drove the IEEE 754 decimal standard (Cowlishaw / IBM General Decimal Arithmetic FAQ, Primary,
2026-07-19):

- **Regulation mandates decimal digits and decimal rounding.** "There are legal and other
  requirements (for example, in Euro regulations) which dictate the working precision (in decimal
  digits) and rounding method (to decimal digits) to be used for calculations," which "can only be met
  by working in base 10, using an arithmetic which preserves precision." A rational `1/3` dollar has no
  legal meaning; the law requires it be *rounded to cents at a defined point* — a decimal operation.
- **Real business data is overwhelmingly decimal.** "almost all (98.6%) of the numbers in commercial
  databases have a decimal or integer representation, and the majority are decimal (scaled by a power
  of ten)." Money has a natural smallest unit (the minor unit); it is inherently scaled-decimal, not
  arbitrary p/q. Precept already encodes this: currency `MinorUnit` drives an implicit `maxplaces`
  (`precept-value-types-investigation.md` D10).
- **Interchange.** "0.1" is a decimal string across JSON, CSV, SQL, XBRL, EDI; "1/10" is not a wire
  format anyone uses for money.

**The deepest counter-point — rational closure can be a *mismatch*, not a win.** Rationals' headline
advantage (division stays exact: `1 ÷ 3 = 1/3`) is precisely what regulated finance does *not* want.
Business rules require that a division result be **rounded to a defined decimal precision at a defined
moment** (tax to the cent, interest to the working precision). The *non-closure of decimal under
division is a feature*: it forces the author to confront the rounding decision the domain mandates.
Exact `1/3` silently **defers** a decision the regulation says must be made — the opposite of Precept's
"honesty about approximation," under which a lossy step must be *visible*, not postponed indefinitely
inside an ever-growing denominator.

**Audience fit — WITHDRAWN 2026-07-20 as unsupported. Do not cite.**

> The original paragraph claimed: Precept's primary author is a domain expert, not a programmer;
> rational literal syntax (`1//3`, `2/3r`, `n % d`) and fraction display (`Fraction(1, 3)`) are
> Lisp/Haskell/Julia programmer-culture artifacts while decimal notation is "universal business
> notation"; therefore rational-native risks an audience mismatch answerable only by adopting Raku's
> whole hybrid.

Four defects, found in owner review:

1. **No citation.** It was the only paragraph in Area 6 carrying no source. Every other claim here is
   grounded in Cowlishaw/IBM. This was an assertion the survey made, not a finding it produced.
2. **Notation/storage conflation.** The cited Area 6 evidence — 98.6% of commercial database numbers
   are decimal, Euro regulations dictate decimal working precision, `0.1` is the JSON/XBRL/EDI wire
   format — concerns **storage, interchange and regulated calculation**. None of it is evidence about
   what a human prefers to *write*. The paragraph borrowed the authority of the storage evidence for a
   claim about authoring notation. Those are separate questions.
3. **Literal syntax is not a real constraint on Precept.** Fraction notation is opt-in in every
   surveyed language, never forced, and Precept controls its own grammar outright: it can offer decimal
   literals only, whatever the backing. Nothing about rational *backing* obliges fraction *syntax*.
   The paragraph also bundled Raku's *arithmetic* policy (float fallback, denominator ceiling) with its
   *notation* policy, as though decimal-in/decimal-out could only be had by importing both. It cannot
   be — those are independent choices.
4. **Non-terminating values invert the argument.** For a value with no finite decimal form, there is no
   decimal to write. The author's choice is not `1/3` versus `0.3333333333` — it is `1/3` versus a
   *different number*. Any finite decimal typed there is wrong, and silently so. Under
   "honesty about approximation," forcing a truncated decimal into the source text is the *dishonest*
   option and the fraction is the honest one. Additionally, fraction notation is demonstrably present in
   commerce and law rather than confined to programming culture — undivided fractional interests in
   property, 30/360 day-count conventions, fractional share allocations, fractional-point rate moves,
   US customary measure in the building trades (including fraction-native calculators sold to
   tradespeople) — so "programmer-culture artifact" does not survive contact with the domain.

**Now grounded by evidence.** The replacement survey is
[`business-fraction-vs-decimal-notation.md`](./business-fraction-vs-decimal-notation.md)
(2026-07-20), commissioned specifically to establish what the sources actually show about
**notation** as distinct from storage. Its results, in brief:

- Defect 4's claim that fraction notation is present in commerce and law **is confirmed** against
  primary sources — deeds and mineral conveyances ("one sixteenth (1/16)", still litigated verbatim
  in 2026), the Uniform Probate Code, Del. Code tit. 8 § 203 ("66 2/3%"), FOMC rate language,
  16 CFR § 500.17 (which authorizes common fractions and names the permitted denominators), Treasury
  quoting in 32nds, and NIST Handbook 44 on binary-fraction subdivision in the building trades.
- But the **inverse claim is equally unsupported**. Currency, prices, posted ledger amounts, equity
  percentages, and conversion ratios are decimal; and every business-rule formula grammar inspected
  — including OMG DMN/FEEL, the standard aimed squarely at business analysts — has **no fraction
  literal**, treating `/` solely as the division operator, over decimal128 backing.
- The dividing line found is **by kind of quantity**, not by audience: fractions for
  definitionally-exact shares of a whole, decimals for measured, priced, or computed magnitudes.
- On defect 4's "truncated decimal is the dishonest option" reasoning specifically: no authority was
  found calling a truncated decimal an error. The regulatory pattern instead (Council Regulation (EC)
  No 1103/97 is the clearest case) is to **fix a finite working precision, carry it without further
  rounding, round only the final payable amount, and disclose the rounding**. That is a third option
  the original argument did not consider.
- The comprehension literature does **not** transfer in either direction, and should not be cited
  here — it concerns schoolchildren, probabilistic risk communication, or small lab samples.

**What survives:** the *display* leg only, and it now points the other way. Raku demonstrates decimal
display of rational values is achievable (Area 3), but also that it is **lossy and unmarked** — which is
a cost to the hybrid, not a rescue of it.

**Status of the underlying question:** genuinely open, pending a dedicated pass on how exact
non-integer quantities are notated in business, legal and financial practice (commissioned 2026-07-20 —
see "Amendments"). Until that lands, this survey supports **no** conclusion about authoring notation in
either direction.

## Implications for Precept

1. **The proposal decomposes into two independent questions the design pass must not conflate.**
   - **(A) Exact representation: base-10 decimal vs p/q rational.** This is where rational genuinely
     wins *for the prover* (Area 5: closure under division; SMT-Real precedent) and genuinely *loses
     for the domain surface* (Area 6: regulation wants decimal digits + decimal rounding; the corpus
     and finance are all-decimal).
   - **(B) Drop floating-point entirely.** This is **blocked by the transcendental gap** (Area 4):
     `sqrt`/`log`/`trig` yield irrationals with no exact home, and Precept intends to support them
     (number-lane-only today). Dropping `number` requires either refusing transcendentals or a symbolic
     CAS — neither on the table. So "float dropped" is the weakest leg of the proposal as stated.

2. **Precept already owns the Scheme exact/inexact tower** (Area 2). The `decimal`/`number`/
   `approximate()` structure *is* the tower with a named bridge. The live question is representation of
   the exact level and whether the inexact level can be deleted — not whether to have a tower.

3. **A narrower, cleaner change is latent in Area 5** — and it is **reuse, not new construction**
   *(revised 2026-07-20)*: move the *proof engine's internal* numeric representation from `decimal` to
   exact rational, independent of any author-facing type change. `UcumExactFactor` already provides the
   representation and exact multiply/divide/pow, and `tools/Precept.Bench/BigRational.cs` already
   prototypes the `Add`/`Sub` the prover would additionally need. A design pass should extend those
   rather than introduce a parallel rational type. That aligns the prover with SMT-Real semantics, removes the System.Decimal
   silent-round-at-28-digits behavior from division-derived bounds, and touches no DSL surface, no
   corpus, no audience-facing notation. It is separable from — and far less risky than — collapsing the
   language's numeric lanes.

4. **If an author-facing single exact type is still wanted, notation is a free choice, not a
   constraint** *(revised 2026-07-20)*. Precept controls its own grammar: decimal literals, fraction
   literals, or both are available whatever the backing, and Raku shows decimal *display* of rational
   values is achievable too. Raku's float fallback and denominator ceiling come from its **arithmetic**
   policy, not its notation policy, and need not be adopted alongside it. What a design pass must
   actually settle is (a) whether an inexact fallback exists at all and how its boundary is surfaced,
   (b) whether decimal rendering of a non-terminating exact value is permissible given
   honesty-about-approximation, since it is lossy and unmarked in Raku, and (c) whether authors should
   be *able* to write exact fractions — open, pending the notation research.

## Conclusions (tradeoff clusters — survey proposes no decision)

Per the brief, this section presents tradeoff clusters rather than an adopt-this recommendation.

**Cluster 1 — Rational as the single author-facing exact type (the owner's lean).**
- *For:* closure under ÷ makes exact arithmetic total and matches SMT-Real semantics (Area 5); one
  exact type is conceptually simpler than two lanes; `integer` genuinely is sugar for `q=1`.
- *For (added 2026-07-20):* for a non-terminating value there is no decimal to write, so exact fraction
  notation is the only way an author can state the value they mean; a truncated decimal is a different
  number, entered silently.
- *Against:* denominator blowup + bignum cost (Area 3, Raku ceiling as evidence); the transcendental gap
  still forces an inexact escape hatch (Area 4) so float is not actually dropped; diverges from the
  finance/decimal **storage and interchange** corpus (Area 6); rational's exact-division *defers* a
  decimal-rounding decision that regulation requires be *made* — but see the caveat below.
- *Withdrawn 2026-07-20:* the "fraction display is an audience mismatch" objection — unsupported, see
  Area 6.
- *Caveat on the deferred-rounding objection (2026-07-20, reasoning not survey evidence):* it is an
  argument about *when* rounding happens, not against exact representation. Precept already forces the
  decision at a defined point via `maxplaces` and explicit `round()`. Exact intermediates plus an
  enforced rounding boundary at store is regulation-compatible and loses less than truncating early and
  carrying the error forward. The objection has force against a system with **no** mandatory rounding
  boundary; that is not Precept. Needs a design pass to confirm, not a survey.

**Cluster 2 — Keep decimal as the single exact type; drop `number` only.**
- *For:* stays aligned with finance/regulation and the all-decimal corpus; smallest surface change.
- *Against:* the transcendental gap is unresolved (`sqrt` has no decimal home) — so `number` cannot
  actually be dropped while transcendentals are supported; decimal remains non-closed under division
  (the prover keeps living with System.Decimal rounding).

**Cluster 3 — Raku hybrid: decimal syntax + rational backing + decimal display + explicit inexact fallback.**
- *For:* gains rational exactness for the common case with a shipped, real precedent.
- *Against:* most machinery of the three; reintroduces a floating-point fallback and a denominator
  ceiling; the fallback is a silent exactness loss unless the boundary is surfaced — which
  honesty-about-approximation demands, adding yet more surface. **Its decimal display of a
  non-terminating exact value is itself lossy and unmarked** (Area 3, added 2026-07-20).
- *Weakened 2026-07-20:* this cluster existed largely to answer an audience objection now withdrawn.
  With notation established as a free choice independent of backing, the hybrid's distinguishing
  feature is its *arithmetic* policy (bounded rationals + float fallback), which should be evaluated on
  cost and honesty grounds rather than as an audience remedy.

**Cluster 4 — Internal-only: rational proof-engine representation, no author-facing change.**
- *For:* captures Area 5's real win (exact division in the prover, SMT-Real alignment) with zero DSL/
  audience/corpus risk; separable and independently shippable.
- *Against:* does nothing for the "one type is simpler" ergonomic goal; leaves the three author-facing
  lanes intact.

The evidence base is asymmetric: rational's advantage is concentrated in the **prover's internal
arithmetic** (Cluster 4 / question A), while its disadvantages concentrate in **cost (denominator
blowup) and fit with decimal storage, interchange and regulated rounding** (Area 6), and the "drop
float" leg is **blocked by the transcendental gap** (Area 4) regardless of which representation is
chosen.

*Revised 2026-07-20:* this asymmetry originally read "disadvantages concentrated in the author-facing
surface." That is no longer supported. With the audience objection withdrawn, the author-facing case
against rationals rests on the deferred-rounding argument alone — itself caveated above — and the
author-facing case *for* them (a non-terminating value has no writable decimal) is unrebutted. The
remaining solid objections are cost and the transcendental gap, neither of which is about the author.

## What would change these conclusions

- **If a real regulated-finance or business-rule system is found that natively stores/exchanges
  rationals** (not decimal), Area 6's "finance converged on decimal, universally" weakens and Cluster 1
  gains real-world cover.
- **If the set of transcendental functions Precept commits to supporting turns out to be empty**
  (owner rules out `sqrt`/`log`/`trig` at the language surface), Area 4's block dissolves and "drop
  float" becomes genuinely feasible.
- **If a regulation or standard is found that mandates *exact fractional* (not decimal-rounded)
  results** for some in-scope domain, the "decimal-rounding-is-a-required-decision" counter-point in
  Area 6 flips for that domain.
- **If denominator-blowup benchmarks on realistic Precept rule shapes show growth stays bounded**
  (reduction + the shallow arithmetic depth of business rules keeps denominators small), Area 3's
  cost objection weakens for Precept specifically.

## Open Questions

- Does Precept's actual arithmetic depth (rule/ensure/derived-field expressions in the corpus) produce
  denominators large enough to matter, or does business arithmetic stay shallow enough that blowup is
  moot? **Partly answered by tooling that already exists** (added 2026-07-20): `tools/Precept.Bench`
  benchmarks `decimal` vs `UcumExactFactor` vs a `BigRational` prototype and prints a blow-up
  demonstration. Running it and recording the numbers would close most of this; what it does *not*
  cover is corpus-realistic expression shapes.
- **How far should the existing exact rational be carried?** (added 2026-07-20) The representation,
  exact composition, and irrational-scale tracking are built; the factor collapses to `decimal` at
  application. The live questions are whether the exact factor should reach the value domain, whether
  `Add`/`Sub` belong on it (`BigRational` already prototypes them), and what bounds the denominator once
  addition is in play. This is extension of a locked decision, not a new direction.
- What exactly is the honest surfacing of a Raku-style Rat→Num fallback under
  "honesty about approximation" — a compile rejection (prove denominator stays bounded), a runtime
  fault, or a visible inexact-typed result? This is a `/design` question, not a survey question.
- Would a rational *internal* prover representation (Cluster 4) change any currently-emitted diagnostic
  (e.g. a bound that System.Decimal rounds today but rational would keep exact)? Needs a probe against
  `ProofEngine.*.cs`.
- Decimal-arithmetic behavior on non-terminating division is itself unresolved in Precept's own docs
  (System.Decimal rounds vs BigDecimal throws) — which does `decimal ÷ decimal` do today? A separate
  question the exact-decimal survey raises and this survey inherits.

## Threats to Validity

- **An uncited assertion sat in a findings section (found 2026-07-20, owner review).** The Area 6
  "Audience fit" paragraph carried no source and drew on storage/interchange evidence to support a claim
  about authoring notation. It is now withdrawn. The generalizable lesson for reading the rest of this
  survey: **check that each claim's citation actually covers the claim being made**, not merely the
  neighbourhood. The load-bearing legs in Areas 4, 5 and 6 are each anchored on separately excerpted
  primary sources (R5RS, SMT-LIB, Cowlishaw/IBM); Area 1's per-language table is assembled row-by-row
  and is the most likely place for further thin entries — the Raku row needed correcting on 2026-07-20
  for the same reason. Spot-check Area 1 rows individually before citing them.
- **WebFetch summarization.** Several excerpts were returned by the fetch tool, which runs a small
  model over the page and can lightly reword. The load-bearing quotes (Raku denominator fallback,
  R5RS exactness + sqrt clause, SMT-Real division, Cowlishaw regulation + 98.6% figures, Haskell
  `type Rational = Ratio Integer`, HyperSpec canonical-ratio) read as verbatim standard/spec phrasing
  and are mirrored, but a reviewer should spot-check the two *fetch-summarized* lines flagged in the
  mirror (SMT-LIB "values" and Wolfram "principle") against the live pages before either becomes
  load-bearing in a locked decision.
- **R7RS PDF unfetchable (binary).** The R7RS PDF could not be text-extracted; the exact/inexact and
  sqrt claims are carried by **R5RS §6.2** instead, whose exactness language R7RS inherits materially
  unchanged. Low risk, flagged.
- **Performance/blowup claims are partly Tertiary.** The general "bignum rational math is slower / can
  blow up" claim is field knowledge, not separately benchmarked here; the Raku denominator-ceiling is
  the corroborating Primary signal, and the open-question above proposes the missing measurement.
- **Corpus counts are this-session grep.** 223 `as integer` / 76 `as decimal` / 0 `as number` differ
  slightly from the brief's 218/73/0 (grep methodology / corpus drift); the *direction* — `number`
  unused, exact lanes dominant — is robust either way.
- **Selection bias toward rational-native languages.** Area 1 enumerates the languages that *have*
  native rationals (the brief's list). This over-represents Lisp/functional/scientific culture and
  under-represents the (far larger) population of languages with **no** native rational and only
  decimal/float — which is itself the Area 6 counter-signal, so the bias is disclosed rather than
  hidden, and arguably strengthens the counter-evidence.
- **Wolfram is Secondary** (vendor docs, not a standard); its symbolic-CAS behavior is illustrative of
  the "keep symbolic" pole, not a comparator Precept could adopt.

## Sources

| # | Title / doc | Author / org | Stable identifier + URL | Grade | Access |
|---|---|---|---|---|---|
| 1 | `fractions` — Rational numbers | Python Software Foundation | Python 3 stdlib docs, docs.python.org/3/library/fractions.html | Primary | 2026-07-19 |
| 2 | `Rational` class | Ruby core | ruby-doc.org/core/Rational.html | Primary | 2026-07-19 |
| 3 | Complex and Rational Numbers | Julia project | docs.julialang.org/en/v1/manual/complex-and-rational-numbers/ | Primary | 2026-07-19 |
| 4 | `Data.Ratio` | Haskell `base` 4.22.0.0 | hackage-content.haskell.org/package/base-4.22.0.0/docs/Data-Ratio.html | Primary | 2026-07-19 |
| 5 | `Rat` type | Raku documentation | docs.raku.org/type/Rat | Primary | 2026-07-19 |
| 6 | Numbers (numeric tower) | Racket | docs.racket-lang.org/reference/numbers.html | Primary | 2026-07-19 |
| 7 | R5RS §6.2 Numbers | Scheme (RnRS) | conservatory.scheme.org/…/R5RS/HTML/r5rs-Z-H-9.html | Primary | 2026-07-19 |
| 8 | `ratio` system class | Common Lisp HyperSpec (ANSI X3.226) | lispworks.com/documentation/HyperSpec/Body/t_ratio.htm | Primary | 2026-07-19 |
| 9 | Data Structures (Ratio, BigInt) | Clojure | clojure.org/reference/data_structures | Primary | 2026-07-19 |
| 10 | Exact and Approximate Results | Wolfram Research | reference.wolfram.com/language/tutorial/ExactAndApproximateResults.html | Secondary | 2026-07-19 |
| 11 | Theory of Reals | SMT-LIB | smt-lib.org/theories-Reals.shtml | Primary | 2026-07-19 |
| 12 | General Decimal Arithmetic FAQ | M. Cowlishaw / IBM | speleotrove.com/decimal/decifaq1.html | Primary | 2026-07-19 |
| 13 | Exact Decimal Arithmetic Survey | Precept research (internal) | `research/architecture/compiler/exact-decimal-arithmetic-survey.md` | Primary | in-repo |
| 14 | Interval-vs-Value Evaluation Prior Art | Precept research (internal) | `research/architecture/compiler/interval-vs-value-evaluation-prior-art-2026-06-05.md` | Primary | in-repo |
| 15 | Solver-free Static Analysis Techniques Survey | Precept research (internal) | `research/architecture/compiler/solver-free-static-analysis-techniques-survey.md` | Primary | in-repo |
| 16 | Precept committed source | Precept | `src/Precept/Pipeline/ProofEngine.*.cs`; `docs/language/primitive-types.md`; `docs/philosophy.md` | Primary | in-repo |

All load-bearing external excerpts mirrored to
`research/references/rational-numeric-representation/source-excerpts.md` (2026-07-19).

## Amendments

**2026-07-20 — owner review (Shane), in conversation.** Corrections applied in place, each marked at
its site:

1. **Area 6 "Audience fit" withdrawn as unsupported.** Uncited; conflated storage/interchange evidence
   with authoring notation; treated fraction syntax as forced when it is a free grammar choice; and
   ignored that a non-terminating value has no writable decimal, which inverts the honesty argument.
2. **Area 1 Raku row corrected.** Raku has fraction literal forms (`<1/3>`, `2/3`, `⅔`) alongside its
   dotted-decimal literals; the row recorded only the latter. (An earlier conversational summary also
   misattributed Ruby's `2/3r` literal to Raku — that error was in the relay, not this file.)
3. **Area 3 display finding added and verified.** `say 1/3` in Raku prints `0.333333`; the exact
   fraction surfaces only via `.raku`. Decimal display of rational backing is therefore real — and also
   lossy and unmarked, a cost this survey originally missed.
4. **Clusters 1 and 3, Implications item 4, and the closing asymmetry re-weighted** to remove reliance
   on the withdrawn paragraph. Cluster 1 gains an unrebutted author-facing argument *for* rationals;
   Cluster 3 weakens, since it existed largely to answer the withdrawn objection.
5. **Deferred-rounding objection caveated** — it constrains *when* rounding happens, and Precept already
   forces that at `maxplaces`/`round()`. Marked as reasoning, not survey evidence; a design pass should
   confirm or reject it.

6. **Precept-internal facts corrected — an exact rational type already exists.** The survey's
   Precept-internal legs recorded only that the proof engine reasons in `decimal`, and drew conclusions
   ("no rational representation," "introducing rationals") on that basis. It missed
   `UcumExactFactor` — a shipped, load-bearing exact rational carrying every UCUM unit scale — along
   with the `ScaleIsRational` catalog flag that already tracks irrational scales, the
   `tools/Precept.Bench` harness built to measure exactly the decimal-vs-arbitrary-precision question,
   and the prior locked design that chose exact-rational scales over float factors. Recorded in
   Background. **Effect:** this is an extension question, not an introduction question, and Area 4's
   transcendental gap is already handled at the catalog level for units.

   *Search-methodology lesson:* the type is named `UcumExactFactor` and lives under
   `src/Precept/Language/Ucum/`. Greps for "rational," "conversion factor," and "numerator/denominator"
   in pipeline and runtime code all miss it. Enumerate the relevant directory before concluding a
   capability is absent.

**Doc drift found and fixed 2026-07-20:** `business-domain-types.md` D12's tradeoff line read "UCUM
conversion factors become `decimal` constants," which contradicts both the code and
`catalog-system.md` § UCUM atom metadata. It predates the exact-rational decision and was never
updated. Corrected in place to state that factors are exact rationals composed exactly, collapsing to
`decimal` only at application. `catalog-system.md` was checked and is accurate.

**Commissioned as a result:** a dedicated research pass on how exact non-integer quantities are notated
in business, legal and financial practice — including fraction-native tools aimed at non-programmers
(trades and woodworking calculators, CAD architectural units) and a matched counter-sweep for
decimalization mandates and metric practice. Until it lands, this survey supports no conclusion about
authoring notation in either direction. Cross-link it here on arrival.
</content>
