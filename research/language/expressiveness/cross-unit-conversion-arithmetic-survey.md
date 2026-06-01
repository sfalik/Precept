---
status: Cited
authored: 2026-05-31
author: research (external-engagement leg)
topic: Cross-unit (same-dimension, different-scale) auto-conversion in multiplicative/cancellation arithmetic — why runtime-capable unit systems auto-convert while compile-time-only ones require explicit constants; the inexact-factor and affine-unit edges that bound the auto-conversion choice
external-engagement: strong
---

# Cross-Unit Conversion in Multiplicative Arithmetic — Survey

> When a rate (`price in 'USD/kg'`) multiplies a same-dimension but different-scale quantity
> (`quantity in 'g'`), Precept will **auto-convert within the dimension and apply the UCUM factor** —
> extending the locked `quantity`-arithmetic rule (D8) to `price × quantity` cancellation. This survey
> grounds the four-leg rationale for that choice: it confirms auto-conversion is the dominant
> runtime-system behavior, isolates *why* compile-time-only systems (F#) require explicit conversion
> instead (so their "explicit" is not genuine counter-evidence against the choice), and surfaces the two
> real constraints — **inexact conversion factors** and **affine units** — that bound where
> auto-conversion is honest.

## Background

Precept's `business-domain-types.md` **already locks** auto-convert-within-dimension for `quantity`
arithmetic:

> **D8. Commensurable arithmetic with deterministic unit resolution** — "Arithmetic between `quantity`
> values of the same UCUM dimension is allowed even when units differ. Resolution: (1) target-directed,
> (2) left-operand wins." … Alternatives rejected include "(C) Require explicit conversion always —
> verbose." … Precedent: "UnitsNet supports cross-unit arithmetic with target-directed conversion."
> Tradeoff accepted: "UCUM conversion factors become a runtime dependency. Physical unit conversions are
> fixed constants — safe for auto-conversion."
> — `docs/language/business-domain-types.md` § D8 (lines 1733–1739, accessed 2026-05-31)

The owner has chosen "Option B": extend the same auto-conversion to `price × quantity → money`
cancellation when the price's denominator unit and the quantity's unit are the same dimension but
different scale (`'USD/kg' × 'g'`). This is a consistency extension of an already-locked rule, not a new
construct — so the consultation gate is satisfied by the existing D8 lock. This survey supplies the
*precedent*, *alternatives-considered*, and *counter-evidence* legs the design pass (Phase 7 Slice 2)
will cite, and flags the affine and exactness edges so they are weighed honestly rather than discovered
late.

The dimensional-cancellation **core** (that `price × quantity → money` is sound dimensionally; that F#,
Boost.Units, Frink, Kennedy's theory, Rust uom all compute cross-cancellation) is already grounded in
prior research and is **not re-surveyed here**:

- `research/language/expressiveness/currency-quantity-uom-research.md` — money/quantity/uom domain
  modeling; the `price × quantity → money` algebra and the F# / UnitsNet / SAP precedent table.
- `research/architecture/compiler/units-of-measure-dimensional-analysis-survey.md` — F#, Boost.Units,
  Frink, Haskell, Rust uom, JSR-354, Kennedy theory: representation, cancellation, erasure.
- `research/architecture/compiler/quantity-normalization-design-survey.md` — normalize-to-base pattern;
  `decimal` exactness; already names the nonlinear-unit (°C/dB/pH) and exact-factor edges as MEDIUM
  follow-ups.
- `research/architecture/compiler/business-units-quantity-normalization-survey.md` — count/business
  units (`each`/`box`) and why they have no universal factor.
- `research/language/division-by-temporal-span-prior-art.md` — the divisor side of the same algebra.

## Methodology

- **Research question** — Three specific gaps the prior research left open:
  1. When same-dimension/different-scale operands meet in multiplicative arithmetic, do comparable
     systems auto-convert or require explicit conversion — and *why* do they differ? Is F#'s
     "explicit-conversion-constant" requirement genuine counter-evidence to auto-conversion-as-a-choice,
     or a forced consequence of being a compile-time-only tag system with no runtime conversion engine?
  2. How do runtime unit libraries represent conversion factors that are not exact terminating
     decimals (irrational, multiply-defined, non-terminating)? Exact rationals, floats, decimal+rounding?
     Do any surface inexactness?
  3. How do systems handle **affine** (offset+scale) units — temperature, dB, pH — in multiplicative /
     cancellation contexts? Reject, special-case, or forbid?
  Plus a brief (4) confirm how count/dimensionless units with no universal factor are treated.
- **Search strategy** — Primary-source-first: official library/manual docs (Pint, GNU `units`, Boost.Units,
  mp-units, Indriya/JSR-385 source + Javadoc, F# language reference, Rust uom). GitHub source and issue
  threads where the docs were thin (Pint #201; Indriya `RationalConverter.java`). Web search to locate
  the exact doc nodes. Venues: readthedocs, gnu.org/software/units manual, boost.org, mpusz.github.io,
  github.com, learn.microsoft.com. Existing Precept research re-read first to avoid duplication.
- **Inclusion / exclusion criteria** — In scope: behavior of cross-scale same-dimension conversion in
  `×`/`÷`; factor representation/exactness; affine handling. Out of scope: the dimensional-cancellation
  mechanics already surveyed; diagnostics formatting; the divisor (`÷ period/duration`) question
  (covered in `division-by-temporal-span-prior-art.md`).
- **Source-grade declaration** — Mixed, honestly graded. **Primary**: Indriya `RationalConverter` source
  Javadoc, GNU `units` manual, F# language reference (re-cited). **Secondary**: Pint docs, Boost.Units
  docs, mp-units docs, Rust uom docs. **Tertiary**: Pint issue #201 thread (corroborated by a Secondary
  docs option `non_int_type=Decimal`). No load-bearing conclusion rests on a Tertiary-only source.
- **Time bounds** — Investigation ran 2026-05-31. gnu.org returned intermittent HTTP 429 (rate limit);
  the Temperature-Conversions node was captured successfully on retry. javadoc.io returned 403 for
  RationalConverter; the identical class Javadoc was captured from the GitHub source file instead.
  Load-bearing external excerpts mirrored to `research/references/cross-unit-conversion/source-excerpts.md`.

## Findings

### Comparator table — cross-scale conversion, factor exactness, affine handling

| System | Runtime conversion engine? | Same-dim diff-scale in `×`/`÷` | Factor representation | Affine units (°C/dB) in `×`/`÷` | Grade |
|---|---|---|---|---|---|
| **Frink** | Yes (units carried at runtime) | **Auto-converts** to base then cancels | Exact rationals where possible | Special-cased (temperature scale functions, not multiplicative units) | Primary/Secondary (existing survey) |
| **Pint** (Python) | Yes | **Auto-converts** (delta units used in multiplicative context) | **`float`** internally → precision loss; `non_int_type=Decimal` opt-in | **Rejects** by default (`OffsetUnitCalculusError`); opt-in `autoconvert_offset_to_baseunit` | Secondary |
| **GNU units** | Yes (calculator) | **Auto-converts** to base then cancels | Floating point (calculator) | **Functional notation required** (`tempF(x)`); not a plain multiplicative unit | Primary |
| **Indriya / JSR-385** (Java) | Yes (runtime `Quantity` objects) | **Auto-converts** via `UnitConverter` chain | **Exact rational** (`RationalConverter` = quotient of two `BigInteger`) | `AddConverter` models offset; affine units are non-multiplicative converters | Primary |
| **Rust uom** | Compile-time types, **base-unit storage at construction** | **Auto-converts** (normalizes to base; `autoconvert` flag) | Storage-type dependent (`f64`/`Decimal`); factor applied once at construction | Not a built-in concern; offset units not first-class | Secondary |
| **Boost.Units** (C++) | Compile-time | Cross-system conversion is **explicit** (different *systems*); same-system auto | Compile-time constants | `absolute<>` wrapper; point ± vector only — **no point `×`** | Secondary |
| **F# units of measure** | **No — compile-time tag, erased at runtime** | **Explicit conversion constant required** | n/a (no runtime factor at all) | n/a (no runtime conversion; user supplies constants) | Primary |
| **Haskell `dimensional`/`units`** | **No — type-level, erased** | Explicit (`redim`/conversion in code) | n/a | n/a | Primary (existing survey) |
| **mp-units** (C++, P3045) | Compile-time | Auto within compatible units | Compile-time | **Multiply syntax disabled for point-origin (temperature) units**; "multiply nor divide *points* with anything else" | Secondary |

### Gap 1 — Auto vs explicit conversion tracks runtime-capability, not a design disagreement

The split in the table is **not** a disagreement about whether auto-conversion is desirable. It tracks
**whether the system has a runtime conversion engine at all.**

**F# cannot auto-convert because it has no runtime to convert in.** F# units of measure are phantom
type tags, erased before IL generation [Primary; access 2026-05-31; mirrored]:

> "any attempt to implement functionality that depends on checking the units at run time is not
> possible. For example, implementing a `ToString` function to print out the units is not possible."
> — Microsoft Learn, "Units of Measure" (re-cited from
> `research/architecture/compiler/units-of-measure-dimensional-analysis-survey.md`)

Because the tag is gone at runtime, a `g → kg` conversion *has* to be a programmer-supplied constant
whose units cancel — `convertGramsToKilograms x = x / gramsPerKilogram` where
`gramsPerKilogram : float<g/kg> = 1000.0<g/kg>` (same survey). F#'s "explicit conversion" is therefore
**a consequence of erasure, not an argument against auto-conversion**. The same holds for Haskell
`dimensional`/`units` (type-level, erased) and Boost.Units across *unit systems* (the explicitness is
between SI and CGS *systems*, a compile-time concern).

**Every system with a runtime conversion engine auto-converts.** Frink carries the dimension vector at
runtime and converts to base before cancelling (`week/day → 7`); GNU `units` is a runtime calculator and
converts; Pint converts (and uses delta units in multiplicative context); Indriya chains
`UnitConverter`s at runtime; Rust uom normalizes to base units **at construction** so by the time `×`
runs the operands are already in base units (the `autoconvert` flag gates this for integer storage).

**Bearing on Precept:** Precept **has** a runtime and already normalizes quantities to base units
(`quantity-normalization-design-survey.md`). It is squarely in the runtime-capable camp (UnitsNet / Pint
/ Frink / Indriya / uom), not the F#/Haskell erasure camp. The F#/Boost-across-systems "explicit"
requirement is therefore **not genuine counter-evidence** against Option B — it is what a
*compile-time-only* system is forced into, and Precept is not one. The honest residual counter-evidence
is narrower (see Gaps 2–3), not "F# makes you do it by hand."

### Gap 2 — Inexact conversion factors: the representation spectrum, and who surfaces inexactness

Runtime systems split on factor representation, and the split has correctness consequences.

**Exact rational (the honest end).** Indriya, the JSR-385 reference implementation, uses exact rational
scaling [Primary; access 2026-05-31; mirrored]:

> "This class represents a converter multiplying numeric values by an exact scaling factor (represented
> as the quotient of two `BigInteger` numbers)."
> — `RationalConverter` Javadoc, unitsofmeasurement/indriya (master)

An exact-rational factor (`p/q` over `BigInteger`) reproduces any *terminating-or-repeating* conversion
exactly and only loses precision at the final decimal materialization (a `BigDecimal` divide under a
`MathContext`). This is the representation that does **not** silently inject error for the common
business factors (`1 lb = 0.45359237 kg` exactly; `1 kg = 1000 g` exactly).

**Float (the silent-error end).** Pint computes conversion factors as `float` internally, which bakes
in binary rounding *before* any decimal materialization [Tertiary, corroborated by Secondary docs;
access 2026-05-31; mirrored]:

> Issue #201: "Conversions for quantities with Decimal units yield imprecise results." Reported:
> `Decimal(1835008) * ureg.bytes` → mebibytes yields `Quantity(1.749999999999541248, 'mebibyte')`
> instead of `Decimal('1.75')`.
> — hgrecco/pint#201

Pint offers `non_int_type=Decimal` to mitigate, but the factor-computation path remains the precision
source. **No surveyed system *flags* inexactness to the user** — Pint silently returns the imprecise
value; GNU `units` prints a truncated decimal. Inexactness is mitigated by representation choice
(exact rationals), not surfaced as a type/diagnostic signal.

**Genuinely-inexact factors do exist** and are the honest boundary of "physical unit conversions are
fixed constants — safe for auto-conversion" (D8's tradeoff):

- **Multiply-defined** factors: the calorie (thermochemical 4.184 J vs IT 4.1868 J) and the BTU have
  several incompatible definitions; "the calorie" has no single factor. Any system must *pick a
  definition*; the pick is exact once made, but the ambiguity is real (Frink's data file and GNU `units`
  both carry multiple named calorie/BTU atoms for exactly this reason — existing-survey + manual).
- **Transcendental / non-terminating** factors: angle units (degree → radian carries π); log-scale units
  (dB, pH, the bel) are *not even multiplicative* (see Gap 3). These cannot be represented as an exact
  rational at all.

`quantity-normalization-design-survey.md` already names this as a MEDIUM follow-up: the
"no-interval-widening-needed" correctness proof "depends on UCUM conversion factors being exact decimal
rationals (which they are for all currently-cataloged units). If a future unit has an irrational or
transcendental conversion factor, precision analysis would be needed."

**Bearing on Precept:** Precept's `decimal` backing + exact UCUM factors put it at the **exact-rational
end** (Indriya-like), not the Pint float end — the better camp. Option B is honest **for the
terminating/repeating-factor units that dominate business measurement** (mass/volume/length: kg↔g,
L↔mL, m↔cm — all exact). The exactness condition is a **scoping constraint, not a blocker**: auto-
conversion is sound where the factor is an exact `decimal`; the design should state that boundary
explicitly (as the normalization survey recommends) rather than imply all UCUM conversions are exact.

### Gap 3 — Affine units (°C/°F, dB, pH): every careful system forbids them in `×`/`÷`

This is the **sharpest** finding and the one Option B must exclude explicitly. Affine units (offset +
scale: `°C = K − 273.15`) are **points in an affine space**, not displacement vectors, and a point has
no meaningful product. Four independent systems converge on forbidding affine units in multiplicative
context:

**Pint — raises by default** [Secondary; access 2026-05-31; mirrored]:

> "multiplication, division and exponentiation of quantities with offset units is problematic just like
> addition." … "Pint (since version 0.6) will by default raise an error when a quantity with offset unit
> is used in these operations." → `OffsetUnitCalculusError: Ambiguous operation with offset unit (degC).`

**mp-units — multiply syntax disabled for temperature** [Secondary; access 2026-05-31; mirrored]:

> "The multiply syntax support is disabled for units that provide a point origin in their definition
> (i.e., units of temperature like `K`, `deg_C`, and `deg_F`)." … "It is not possible to: add two
> *points*, subtract a *point* from a *vector*, multiply nor divide *points* with anything else."
> — mp-units, "The Affine Space"

**GNU units — temperatures require functional notation, not multiplicative units** [Primary; access
2026-05-31; mirrored]:

> "Conversions between temperatures are different from linear conversions between temperature
> *increments*. The absolute temperature conversions are handled by units starting with 'temp', and you
> must use functional notation." … "Think of 'tempF(x)' not as a function but as a notation that
> indicates that x should have units of 'tempF' attached to it."
> — GNU `units` manual, "Temperature Conversions"

Critically, GNU `units` separates the **point** (`tempC`, absolute, functional) from the **increment**
(`degC`, a plain linear unit). The *increment* IS multiplicative; the *absolute point* is not.

**Boost.Units — `absolute<>` wrapper isolates points** [Secondary; access 2026-05-31; mirrored]:

> "it is important to be able to differentiate between an absolute temperature measurement and a
> measurement of temperature difference."

`quantity<absolute<fahrenheit::temperature>>` (a point, displayed `{ 32 } F`) is a distinct type from
the relative `quantity<fahrenheit::temperature>` (a vector, `[ 32 ] F`); the wrapper admits point ±
vector but not point × scalar.

**Bearing on Precept:** The unanimous answer is **exclude absolute/affine units from multiplicative
cancellation**. A `price in 'USD/°C' × quantity in '°C'` (or any affine denominator/operand) must **not**
auto-convert-and-cancel — the cancellation is meaningless because `°C` is a point, not a ratio-scale
magnitude. This matches `quantity-normalization-design-survey.md`'s existing MEDIUM note that
`TryGetStaticScalingFactor` "only handles linear unit conversions … Nonlinear units (°C, dB, pH …) are
explicitly out of scope." For Option B the rule is: **auto-convert-and-cancel only across ratio-scale
(linear, zero-origin) units of the same dimension; reject affine/offset and log-scale units.** UCUM's
own grammar marks "special units" (non-ratio) distinctly, so the catalog can detect them. This is a
real constraint on Option B's scope — *not* a reason to reject auto-conversion for the linear majority.

### Gap 4 — Count/dimensionless units with no universal factor (brief)

Already settled in Precept (reject; PRE0137) and grounded in
`business-units-quantity-normalization-survey.md`. Confirming the external pattern in one line: surveyed
systems treat `each`/`box`-style counts as the dimensionless unit (JSR-385: `each` aliases `Units.ONE`;
all dimensionless units are mathematically "compatible") and provide **no** cross-count factor — `1 box
= N each` is application/product data, not a unit relationship (SAP's per-material MARM factors; F# and
uom keep it value-level). So a same-"dimension" pair with **no defined factor** (count ↔ count) is the
one same-dimension case where auto-conversion correctly does *not* apply — consistent with Precept
rejecting it.

## Threats to Validity

- **Pint precision claim is Tertiary at root.** The float-factor precision-loss claim rests on issue
  #201 (a thread), graded Tertiary. It is corroborated by Pint's Secondary docs offering
  `non_int_type=Decimal` precisely to mitigate float factors, so the *direction* of the claim is sound
  even if a specific reproduction value drifts across versions. It is also **not load-bearing** for the
  conclusion — Precept is at the exact-rational end regardless of Pint's choice.
- **gnu.org rate-limiting (recovered).** The GNU `units` manual returned HTTP 429 on several attempts;
  the Temperature-Conversions node was captured on retry and the Defining-Nonlinear-Units shape was
  captured via search snippet (Secondary) rather than a full-page fetch. The load-bearing temperature
  excerpts are from the successfully-fetched Primary node.
- **javadoc.io 403 for Indriya.** The `RationalConverter` Javadoc was captured from the GitHub source
  file (the canonical comment), not the rendered Javadoc — same text, arguably more authoritative.
- **Selection bias.** Comparators were chosen to span the runtime-capable / compile-time-only axis
  deliberately (to test the Gap-1 framing), not enumerated exhaustively. UnitsNet — D8's named precedent —
  was *not* re-fetched here; it is already grounded in `currency-quantity-uom-research.md` as
  "cross-unit arithmetic with target-directed conversion." If UnitsNet's actual behavior diverged from
  that prior claim, the D8 precedent leg (not this survey's new contribution) would weaken.
- **Affine = strong convergence, low risk.** Four independent systems (Pint, mp-units, GNU units,
  Boost.Units) plus the affine-space mathematics all forbid point multiplication. This is the
  best-supported finding; the risk it is wrong is low.
- **"No system surfaces inexactness" is a negative claim.** It is bounded to the surveyed set; a system
  that *does* flag inexact conversions (none found) would strengthen, not weaken, Precept's honesty
  case — so the negative is conservative.

## Implications for Precept

1. **Option B is consistent with the runtime-system mainstream.** Precept has a runtime, normalizes to
   base units, and uses exact `decimal` factors — it belongs with UnitsNet/Pint/Frink/Indriya/uom, all
   of which auto-convert same-dimension/different-scale operands in multiplicative context. Extending D8
   to `price × quantity` is the *consistent* choice, not a novel one.
2. **F#'s explicit-conversion requirement is correctly *not* counter-evidence.** It is forced by
   compile-time erasure. Precept is not compile-time-only, so the design pass should not weigh "but F#
   makes you convert by hand" as a reason to reject Option B. (It *can* cite F# as precedent for the
   *cancellation algebra* — which D8 already does — without inheriting F#'s erasure constraint.)
3. **Two honest constraints bound Option B's scope, and both should be stated in the design, not
   discovered later:**
   - **Exactness:** auto-convert-and-cancel is sound where the cross-scale factor is an exact `decimal`
     (the business-measurement majority). Transcendental/irrational/log-scale factors are out of scope;
     multiply-defined factors (calorie/BTU) require the catalog to name a definite atom. State the
     exact-`decimal` boundary explicitly (the normalization survey already recommends this).
   - **Affine:** **exclude absolute/offset and log-scale units** (`°C`, `°F`, `dB`, `pH`) from
     multiplicative cancellation. Four systems forbid point multiplication; Precept should too. Detect
     via the UCUM special-unit marker / a catalog non-ratio flag.
4. **The catalog is the right place for both gates.** Whether a unit is ratio-scale (linear, zero-origin)
   vs affine, and whether its factor is an exact `decimal`, are unit-metadata facts — they belong in the
   UCUM atom catalog (consistent with Precept's catalog-driven architecture), not in per-operator
   dispatch logic.

## Conclusions

### Conclusion 1 — Auto-convert-within-dimension for `price × quantity` cancellation (Option B) is the well-precedented choice for a runtime-capable engine

- **Rationale** — Every surveyed system that *has a runtime conversion engine* auto-converts
  same-dimension/different-scale operands before cancelling (Frink, Pint, GNU units, Indriya, Rust uom);
  only systems with **no runtime** (F# units of measure, Haskell type-level, Boost across *systems*)
  require an explicit conversion constant, and that requirement is a forced consequence of unit erasure,
  not a design preference. Precept has a runtime and already normalizes quantities to base units, so the
  auto-conversion that D8 locks for `quantity` arithmetic carries over to `price × quantity` cancellation
  without introducing any new mechanism.
- **Alternatives considered and rejected** — **(A) Require explicit conversion always** (the F#/Boost
  shape): rejected — it is verbose (D8 already rejected it for `quantity`), and the systems that impose
  it do so only because they cannot convert at runtime; Precept can. **(B) Strict same-unit-only
  cancellation** (`'USD/kg' × 'g'` is an error unless the author writes `g → kg`): rejected — it makes
  the dimension-level `of` constraint cancellation-useless and contradicts D8's resolution for the
  parallel `quantity` case (D8 alternative (A), already rejected). **(C) Auto-convert *and* surface a
  precision warning on every cross-scale cancellation**: rejected — no surveyed system does this, and
  for exact-`decimal` factors there is no inexactness to warn about; warning unconditionally would be
  false noise. (Inexactness is handled by the exactness *scope gate* in Conclusion 2, not a runtime
  warning.)
- **Precedent** — UnitsNet (D8's named precedent: target-directed cross-unit conversion); Frink
  (runtime base-conversion then cancel); Pint (delta units in multiplicative context); Indriya
  (`UnitConverter` chains, exact-rational); Rust uom (base-unit normalization at construction, with
  `autoconvert`). The cross-cancellation algebra itself is grounded across F#, Boost.Units, Kennedy
  theory in the existing dimensional-analysis survey.
- **Tradeoff accepted** — A `decimal` conversion factor becomes part of the cancellation result, so the
  result's exactness depends on the factor being an exact `decimal` (true for the business-measurement
  majority; bounded by Conclusion 2). The author no longer sees the conversion happen — the language
  performs it — which is the same "fixed-constant conversion is invisible" tradeoff D8 already accepted
  for `quantity`.

### Conclusion 2 — Auto-conversion is sound only across exact-`decimal`, ratio-scale factors; this is a scope condition, not a blocker

> **Superseded by owner ruling (2026-05-31) — see design Decision 2 (`docs/Working/price-cross-unit-cancellation-design-2026-05-31.md`).** This conclusion recommended *rejecting* non-exact-`decimal` factors. The owner ruled **allow all commensurable conversions and *surface* the exact-vs-approximate status** instead — on the basis that (a) `in→ft = 1/12` is an *exact rational* (non-terminating in base-10 is the same rounding as any `money / 3`, absorbed at `maxplaces`), (b) the author's explicit cross-unit expression makes the conversion non-silent, and (c) P8 requires the line be *visible*, not *forbidden*. The **findings below stand** (exact-rational vs float representation, the exactness spectrum); only the "reject inexact" *recommendation* is overridden. The same supersession applies to the "Exactness" bullet in § Implications.

- **Rationale** — Auto-conversion injects the conversion factor into the result. Where the factor is an
  exact `decimal` (kg↔g, L↔mL, lb↔kg = 0.45359237 exactly), `decimal` arithmetic preserves exactness and
  the result is honest. Where the factor is transcendental/irrational (angle↔radian via π), log-scale
  (dB, the bel), or multiply-defined (calorie, BTU), exactness is not guaranteed and "fixed constant" no
  longer holds. The business-measurement units that dominate `price × quantity` are exact; the inexact
  cases are out-of-scope edges, so the condition constrains scope without blocking the feature.
- **Alternatives considered and rejected** — **(A) Assume all UCUM factors are exact** (implicit in a
  naive reading of D8): rejected — the normalization survey already flags that this is true only for
  currently-cataloged units; transcendental/log factors break it. **(B) Float-based factors** (Pint's
  internal choice): rejected — Pint #201 shows float factors silently inject error even for exact
  conversions like 1835008 B → 1.75 MiB; incompatible with `decimal` backing (D12) and with the
  "honesty about approximation" commitment.
- **Precedent** — Indriya `RationalConverter` ("exact scaling factor … quotient of two `BigInteger`")
  is the exactness-preserving design Precept's `decimal`-factor approach parallels. Pint #201 is the
  counter-example showing what float factors cost. UCUM defines physical conversions as exact rationals
  for the linear majority (`quantity-normalization-design-survey.md`).
- **Tradeoff accepted** — Units whose cross-scale factor is *not* an exact `decimal` cannot participate
  in auto-cancellation; the author must handle them explicitly (or the catalog must exclude them). This
  is the price of not presenting an approximate conversion as exact — Precept's standing honesty
  tradeoff.

### Conclusion 3 — Exclude affine/offset and log-scale units from multiplicative cancellation

> **Refined post-survey (2026-05-31, owner discussion).** This conclusion's scope is **narrower** than
> first stated. The surveyed libraries forbid multiplying the **absolute/point** form of these units
> (offset/`absolute<>`/point-origin), but they *allow* the **amount/increment** form (Pint `delta_degC`,
> GNU `degC`). Since Precept's `quantity` is *always an amount* (never an absolute position — the
> `duration`-not-`instant` analog), `quantity` amounts are **not excluded by the position rule** (an
> amount has no offset to trip on). The **libraries' finding** — they forbid affine/log *point*
> operations — is unchanged and stands. But **Precept's own decision diverges**: per the owner's
> allow-all-with-surfacing ruling (2026-05-31), Precept admits **all** commensurable conversions and
> *surfaces* exact-vs-approximate rather than rejecting. So for Precept, dB *amounts* cancel: same-unit
> (factor 1) exactly, and cross-scale (dB↔Np, ÷8.686 = 20/ln 10, *irrational*) **allowed and surfaced as
> approximate** — like angle→radian, also allowed and surfaced. Precept is thus **more permissive** than
> the libraries (which reject the point form) while keeping the exact/approximate status visible. The
> "dB converts cleanly to nepers" framing first stated here was the conflation: the cross-scale factor is
> irrational, so the conversion is *labelled approximate*, not silently "clean." What the position rule
> excludes is only *absolute positions* (thermostat readings, `dBm` levels), which `quantity` does not
> model — tracked as Phase 7 Slice 4. See `docs/Working/price-cross-unit-cancellation-2026-05-31.md` § Decision & scope
> correction. The excerpts below remain accurate (they describe the libraries forbidding **point**
> multiplication); it is the "exclude all affine/log" framing that narrowed, and Precept's own rejection
> framing that the allow-all ruling replaced.

- **Rationale** — Affine units (°C, °F, dB, pH) are points in an affine space, not ratio-scale
  magnitudes; a point has no meaningful product or quotient. Auto-converting-then-cancelling a `°C`
  operand would produce a number with no real-world meaning. Excluding them keeps cancellation honest.
- **Alternatives considered and rejected** — **(A) Auto-convert affine units like any other** (treat °C
  as `K` with an offset and cancel): rejected — produces meaningless results; Pint specifically calls
  this "ambiguous" and raises `OffsetUnitCalculusError`. **(B) Special-case affine units with delta/
  increment semantics inside cancellation** (Pint's `autoconvert_offset_to_baseunit` / GNU `degC`
  increment): rejected as a *default* — it silently reinterprets the author's `°C` as a temperature
  *difference*, which is a guess about intent; better to reject and require the author to express the
  increment explicitly. (Precept may later choose to support an explicit increment unit, but that is a
  separate decision, not part of Option B's auto-cancellation.)
- **Precedent** — Convergent across four systems: Pint (`OffsetUnitCalculusError` by default), mp-units
  ("multiply nor divide *points* with anything else"; multiply syntax disabled for temperature),
  GNU `units` (temperatures require functional notation; `tempC` point vs `degC` increment),
  Boost.Units (`absolute<>` wrapper: point ± vector only). Affine-space mathematics underpins all four.
- **Tradeoff accepted** — Authors cannot write `price in 'USD/°C' × quantity in '°C'` and have it cancel;
  affine quantities are excluded from the price-cancellation surface. This is correct — the operation has
  no honest meaning — but it is a real restriction the design must document so the exclusion is not read
  as a bug.

## What would change this conclusion

- **Gap-1 framing falls** if a *compile-time-only, erasure-based* unit system is found that nonetheless
  auto-converts same-dimension/different-scale operands at the value level without a runtime — that would
  show "explicit conversion" is a genuine design choice, not a forced consequence of erasure, and F#
  would become real counter-evidence.
- **Conclusion 2 tightens or loosens** if UCUM/the catalog turns out to carry *common business* units
  with non-exact-`decimal` cross-scale factors (not just exotic angle/log units) — then the exact-factor
  majority assumption is weaker and the scope gate must be larger.
- **Conclusion 3 falls** if two or more careful unit systems are found that *do* permit absolute/affine
  units in multiplicative cancellation by default (not via an explicit opt-in like
  `autoconvert_offset_to_baseunit`) and treat the result as meaningful — that would undercut the
  "unanimous exclusion" precedent.

## Open Questions

- **Explicit increment/delta units in Precept.** Pint and GNU `units` both expose a *difference* unit
  (`delta_degC` / `degC`) that IS multiplicative, distinct from the absolute point. Does Precept want an
  explicit temperature-*increment* unit so `price per °C-of-difference` is expressible? Out of scope for
  Option B; a separate language-surface question for the owner if temperature pricing ever arises.
- **Multiply-defined factor atoms (calorie/BTU).** If Precept's catalog ever admits energy units with
  multiple historical definitions, it must name definite atoms (as Frink and GNU `units` do) rather than
  a single ambiguous "calorie." Catalog-curation question, not an Option-B blocker.
- **UnitsNet behavior re-confirmation.** D8 names UnitsNet as precedent; this survey did not re-fetch it
  (it is grounded in `currency-quantity-uom-research.md`). A direct UnitsNet excerpt for target-directed
  cross-unit conversion would upgrade D8's precedent leg from Secondary-internal to Primary-external.

## Sources

- **Pint — non-multiplicative / offset units** — hgrecco/Pint docs — readthedocs (stable) —
  **Secondary** — accessed 2026-05-31 — mirrored to
  `research/references/cross-unit-conversion/source-excerpts.md` —
  https://pint.readthedocs.io/en/stable/user/nonmult.html
- **Pint #201 — Decimal conversion imprecision** — hgrecco/pint GitHub issue — **Tertiary** (thread;
  corroborated by Secondary `non_int_type` docs) — accessed 2026-05-31 — mirrored —
  https://github.com/hgrecco/pint/issues/201
- **Indriya `RationalConverter`** (exact rational = quotient of two `BigInteger`) — unitsofmeasurement/
  indriya, JSR-385 RI, master — **Primary** (source Javadoc) — accessed 2026-05-31 — mirrored —
  https://github.com/unitsofmeasurement/indriya/blob/master/src/main/java/tech/units/indriya/function/RationalConverter.java
- **GNU `units` — Temperature Conversions** (`temp` functional vs `deg` increment) — FSF, GNU `units`
  manual — **Primary** — accessed 2026-05-31 — mirrored —
  https://www.gnu.org/software/units/manual/html_node/Temperature-Conversions.html
- **GNU `units` — Defining Nonlinear Units** (function + inverse, not a factor) — FSF — **Primary**
  (search-snippet capture) — accessed 2026-05-31 — mirrored —
  https://www.gnu.org/software/units/manual/html_node/Defining-Nonlinear-Units.html
- **Boost.Units — Absolute and Relative Temperature** (`absolute<>` wrapper) — Schabel & Watanabe,
  Boost 1.84 docs — **Secondary** — accessed 2026-05-31 — mirrored —
  https://www.boost.org/doc/libs/1_84_0/doc/html/boost_units/Examples.html
- **mp-units — The Affine Space** ("multiply syntax disabled for … temperature"; "multiply nor divide
  *points*") — Mateusz Pusz, mp-units docs (P3045 reference) — **Secondary** — accessed 2026-05-31 —
  mirrored — https://mpusz.github.io/mp-units/latest/users_guide/framework_basics/the_affine_space/
- **F# units of measure** (runtime erasure; explicit conversion constant) — Microsoft Learn —
  **Primary** — re-cited from
  `research/architecture/compiler/units-of-measure-dimensional-analysis-survey.md` —
  https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/units-of-measure
- **Rust uom** (`autoconvert`; base-unit normalization) — docs.rs/uom 0.38.x — **Secondary** —
  re-cited from `units-of-measure-dimensional-analysis-survey.md` — https://docs.rs/uom/latest/uom/
- **Existing Precept research** (re-cited, not re-surveyed) —
  `research/language/expressiveness/currency-quantity-uom-research.md`;
  `research/architecture/compiler/units-of-measure-dimensional-analysis-survey.md`;
  `research/architecture/compiler/quantity-normalization-design-survey.md`;
  `research/architecture/compiler/business-units-quantity-normalization-survey.md`;
  `research/language/division-by-temporal-span-prior-art.md`.
- **Spec lock** — `docs/language/business-domain-types.md` § D8 (lines 1733–1739) — **Primary**
  (canonical) — accessed 2026-05-31.
