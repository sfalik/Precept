---
status: Externally-Grounded
phase-target: Phase 7 (Slice 2)
comparable-systems-research-status: strong — cites the Stage-1 survey `research/language/expressiveness/cross-unit-conversion-arithmetic-survey.md`
sources-consulted:
  - docs/language/business-domain-types.md § D8 (:1733-1739): locked auto-convert-within-dimension rule for quantity arithmetic + resolution order (:449-450)
  - docs/language/business-domain-types.md :220: cross-dimension price×quantity is a compile error (the only price-cancellation case the spec locks)
  - docs/language/business-domain-types.md :1866: price comparison requires same currency AND same denominator unit
  - docs/language/business-domain-types.md :397: count units share the count dimension but have no universal factor (PRE0137)
  - docs/language/precept-language-spec.md § 0.1 (:91-111): the 11 design principles
  - docs/philosophy.md (:22,:23,:25,:49): determinism, approximation honesty, prevention
  - research/language/expressiveness/cross-unit-conversion-arithmetic-survey.md (37971a4b): comparator survey grounding auto-convert + the affine/exactness edges
  - docs/Working/price-cross-unit-cancellation-2026-05-31.md: investigation + decision & scope correction
  - src/Precept/Pipeline/ProofEngine.Qualifiers.cs / src/Precept/Language/Operations.cs:644-653: the PriceTimesQuantity QualifierChain proof path
---

# Price × Quantity Cross-Unit Cancellation — Design (Auto-Convert Within Dimension)

## Goal

When done, `price in 'USD/kg' × quantity in 'g' → money in 'USD'` compiles **and** evaluates to the correctly-scaled amount (the quantity is converted g→kg by the exact UCUM factor before cancelling), while cross-*dimension* (`kg × m`), no-universal-factor count pairs (`each × box`), and non-exact-factor units stay rejected — demonstrated by a scenario-test matrix and the runtime evaluator applying the factor.

## Scope

- **In scope**: the cancellation semantics for `price × quantity → money` (and its commutative `quantity × price`) when operands share a UCUM dimension but differ in unit/scale; the compile-time proof obligation; the runtime conversion obligation (documented as a requirement — runtime evaluator is still a stub); two catalog metadata facts (per-unit amount-conversion scale factor + exact-`decimal` flag); the exactness gate; the narrowed exclusion (absolute positions only).
- **Out of scope**: absolute-position measurement values (thermostat readings, `dBm` levels, absolute pH) — carved to **Slice 4** (a new point-type construct); transcendental/log/multiply-defined conversion factors (rejected by the exactness gate, not supported); the `money / price → quantity` and `price × period/duration` paths (separate operations, governed by D15); any change to cross-*dimension* rejection (`kg × m` stays an error).
- **Deferred to future**: an explicit temperature/log *increment* spelling sugar (`delta °C`) — "per kelvin" already expresses per-degree-of-change; sugar is its own future language-surface question (survey Open Question).

## Philosophy Alignment

| Principle | Affected? | How served (cite) | Tension | Tradeoff |
|---|---|---|---|---|
| 1. Prevention, not detection | Y | Closes the live Slice-1 hole: cross-unit either cancels *correctly* (factor applied) or is rejected — no silently-wrong money (`philosophy.md:49`). | The compile-time "Proved" presently outruns the unbuilt runtime conversion. | Until the runtime applies the factor, "Proved" is honest *only because* the runtime obligation is documented and tracked (this doc); see Decision 1 tradeoff. |
| 2. One file, complete rules | N | Conversion is a language semantic; no scattered logic. | N/A | N/A |
| 3. Deterministic semantics | Y | UCUM factors are fixed constants; `decimal` arithmetic → same data, same outcome (`philosophy.md:22`). | N/A | N/A |
| 4. Full inspectability | Y | The conversion is surfaced in hover/trace/proof attribution ("converted g→kg ×0.001"), not hidden (`spec §0.1 #4`). | Conversion is implicit in *syntax*. | Implicit-in-syntax is acceptable (D8 already is) *iff* surfaced in inspection — required, not optional. |
| 5. Keyword-anchored readability | N | No new syntax; existing `price`/`quantity`/`*`. | N/A | N/A |
| 6. Explicit domain meaning over primitive convenience | Y | Reinforces it — `price`/`quantity` carry dimensional identity that bare decimals can't (`spec §0.1 #6`, `business-domain-types.md:314`). | N/A | N/A |
| 7. Compile-time-first static checking | Y | The proof engine discharges the dimension-match + exact-factor obligation at compile time; rejects non-exact/cross-dimension (`spec §0.1 #7`). | N/A | N/A |
| 8. Approximation honesty | Y | The **exactness gate** keeps the exact `money` lane exact — only exact-`decimal` factors auto-convert; inexact factors are rejected, not silently rounded (`philosophy.md:25`, `spec §0.1 #8`). | A converted result may still need `maxplaces` rounding. | That rounding is the *existing* money-precision mechanism (already honest); this design adds no new approximation. |
| 9. Mandatory rationale | N | No rule/ensure surface change. | N/A | N/A |
| 10. Totality | Y | Runtime conversion is total over commensurable exact-factor pairs; non-total (transcendental) factors are compile-rejected (`spec §0.1 #10`). | N/A | N/A |
| 11. Static completeness | Y | A well-typed cross-unit cancellation will not fault at runtime once the conversion ships; the exactness gate makes the unsupported cases compile errors, not runtime faults (`spec §0.1 #11`). | The runtime piece is unbuilt (PoC). | The compile-time contract is stated now; the evaluator obligation is documented so static-completeness holds when the runtime lands (this is the gate's purpose). |

**Principle-1/11 tradeoff (stated, per the matrix):** The design accepts that, in the current pre-runtime PoC, the compile-time `"Proved"` for cross-unit cancellation is a *forward reference* to an unbuilt runtime conversion. This is honest **only** because (a) nothing ships — there is no external author who can observe a wrong result, and (b) the runtime requirement is explicitly documented (§ Semantic Rules → runtime obligation) and tracked (skipped test + this doc), rather than left as hidden certainty. When the runtime is built, applying the factor makes the `"Proved"` literally true. This is the philosophy-honest interpretation of "document the runtime requirements well."

**Companion commitments.** *Stateless-first-class*: unaffected — cancellation is data-arithmetic, works in stateless precepts identically. *Domain-expert-primary-author*: directly served — the author writes the natural `price in 'USD/kg' × quantity in 'g'` and gets the right answer without hand-converting units (see Audience).

## Language Design Grounding

**General language design.** The construct is *dimensional cancellation with cross-scale unit conversion* in a typed expression: multiplying a rate (currency÷unit) by a magnitude (unit) and cancelling the shared unit dimension to a scalar-currency result. The evaluation semantics: the quantity operand is converted to the price's denominator unit by a fixed scale factor, then the units cancel by exponent subtraction, leaving the currency. The survey (`cross-unit-conversion-arithmetic-survey.md`, 37971a4b) establishes the field position with verbatim primary/secondary excerpts:

- **Every runtime-capable unit system auto-converts** same-dimension/different-scale operands before cancelling — Frink, Pint, GNU `units`, Indriya/JSR-385, Rust `uom`. Indriya represents factors as *exact rationals* (`RationalConverter` = "quotient of two `BigInteger`"); Pint uses floats and silently loses precision (issue #201) — Precept's `decimal` + exact factors lands at the honest Indriya end.
- **Compile-time-only systems require explicit conversion** *because they cannot convert at runtime* — F# units of measure are erased ("checking the units at run time is not possible"), so the programmer must supply a conversion constant. This is **not** counter-evidence against auto-conversion; it is a forced consequence of erasure. Precept has a runtime → auto-convert camp (D8's UnitsNet precedent).
- **The affine/log edge:** four systems (Pint `OffsetUnitCalculusError`, mp-units, GNU `units`, Boost.Units `absolute<>`) forbid multiplying *absolute/point* units, but all admit the *amount/increment* form. Per the scope correction, that maps to "exclude absolute positions" — not "exclude °C/dB amounts."

This is the *dimensional-analysis / units-of-measure* domain; the companion studies are `research/architecture/compiler/units-of-measure-dimensional-analysis-survey.md` (Kennedy's free-abelian-group theory, F#/Boost/Frink representation) and the new cross-unit survey. No gap in `research/language/` for this domain — it is well-covered.

**Precept-specific application.** Extends `business-domain-types.md § D8` (commensurable arithmetic with deterministic unit resolution) from `quantity ± quantity` to `price × quantity` cancellation, using D8's own resolution order (target-directed, then left-operand). Touches the `price` and `quantity` types and the `*` operator's result semantics. Does **not** conflict with `:220` (cross-*dimension* stays an error) or `:1866` (price *comparison* still requires exact same unit — comparison ≠ cancellation; see Decision 1 counter-evidence). Respects `:397` (count units keep rejecting — no universal factor).

## Audience and Teachability

**Worked example** (retail produce pricing — a domain expert authoring an invoice precept):

```precept
field UnitPrice    as price in 'USD/kg'
field NetWeight    as quantity in 'g'          # scale reads grams
field LineTotal    as money in 'USD' <- UnitPrice * NetWeight
                       because "line total is unit price times weight sold"
```

The author prices per kilo but the scale reports grams — they write the natural expression and the engine converts (g→kg) and bills correctly. No manual `/1000`.

**Error message** (a plausible misuse — multiplying a price by a *different-dimension* quantity):

```
PRE0114: 'UnitPrice' is priced per kilogram (mass) but 'Distance' is measured in
miles (length) — these don't cancel. A price's denominator and the quantity must
measure the same thing. Use a quantity in a mass unit (kg, g, lb), or a price
per length unit (USD/mi).
```

Domain-targeted: it names the *real-world* mismatch ("priced per kilogram" / "measured in miles" / "measure the same thing"), not compiler vocabulary like "dimension vector" or "qualifier axis," and offers the two concrete fixes a domain author would reach for.

**10-minute teaching path:**
1. `business-domain-types.md` § price (the `price × quantity → money` headline, ~2 min).
2. `business-domain-types.md § D8` (commensurable arithmetic auto-converts, ~1 min).
3. One sample using a cross-unit line total (added in execution, ~2 min).
4. The hover/inspection showing the conversion (~1 min).

## Semantic Rules

**Typing rule** (cross-unit price cancellation):

```
  Γ ⊢ p : price⟨currency c, denom unit u_p⟩    Γ ⊢ q : quantity⟨unit u_q⟩
  dim(u_p) = dim(u_q)        ratioScale(u_p) ∧ ratioScale(u_q)
  exactDecimal( factor(u_q → u_p) )
  ────────────────────────────────────────────────────────────────────────
                       Γ ⊢ p * q : money⟨currency c⟩
```

`dim(u_p)=dim(u_q)` is the existing dimension-match (discharged today). The two new premises — `ratioScale(...)` (both operands are ratio-scale, i.e. not absolute-position units) and `exactDecimal(factor(...))` (the cross-scale factor is an exact `decimal`) — are the exactness/position gates. `u_q = u_p` (same unit) is the degenerate case with `factor = 1` (Slice 1).

**Evaluation / reduction rule** (the runtime obligation — currently unbuilt; documented as the requirement):

```
  p ⇓ price(m_p in c per u_p)     q ⇓ quantity(m_q in u_q)     k = factor(u_q → u_p)
  ──────────────────────────────────────────────────────────────────────────────────
            p * q  ⇓  money( m_p · (m_q · k)  in  c )
```

The evaluator must (1) look up the exact `decimal` factor `k = factor(u_q → u_p)` from the catalog (target-directed: convert the quantity to the price's denominator unit, per D8 resolution rule 1), (2) compute `m_q · k` in `decimal`, (3) multiply by the price magnitude, (4) tag the result `money in c`. For `u_q = u_p`, `k = 1` (Slice-1 behavior, already correct). **This reduction rule is the load-bearing runtime requirement** the owner asked to be documented well.

**Proof obligations.** The existing `QualifierChainProofRequirement` (Dimension↔Dimension, `Operations.cs:650`, discharged via `ProofEngine.Qualifiers.cs`) is **extended** with two checks read from catalog metadata:
- *Ratio-scale*: both operand units carry the `ratioScale` flag (reject absolute-position units → routes to Slice 4 when that exists; until then, no absolute-position unit is admissible as a `quantity`, so this is vacuously satisfied for current units).
- *Exact factor*: `factor(u_q → u_p)` is flagged `exactDecimal` in the catalog. If not, the obligation does not discharge → diagnostic (non-exact factor not supported).

No new `ProofRequirementKind` is required — the existing `QualifierChain` requirement gains catalog-driven side-conditions. (Inventory notes the metadata additions.)

**Soundness preservation claim.**
- **Principle 8 (approximation honesty):** holds — the `exactDecimal` gate guarantees the conversion injects no rounding into the exact `money` lane; non-exact factors are compile-rejected, never silently approximated.
- **Principle 10 (totality):** holds — `factor(u_q → u_p)` is total over the commensurable exact-`decimal` pairs the gate admits; `decimal` multiplication of finite exact factors is total (no `NaN`/`Infinity`).
- **Principle 11 (static completeness):** holds *once the runtime ships* — every admitted cancellation has a defined evaluation (the reduction rule), and every rejected case is a compile diagnostic, so a well-typed program has no cross-unit runtime fault. In the current pre-runtime PoC the evaluator is a stub for *all* arithmetic; this design adds the obligation that the cross-unit path apply `k` when that stub is implemented (tracked, not hidden — see Philosophy tradeoff).

## Architecture Grounding

### Precept-internal placement

**Layer placement.** The two new facts — per-unit **amount-conversion scale factor** and the **`exactDecimal` flag** — belong in the **UCUM atom catalog** (catalog metadata), not in pipeline dispatch. They are intrinsic unit properties (a unit's scale relative to its dimension's base; whether that scale is an exact `decimal`), so per Precept's catalog-driven architecture they are declared once in the catalog and the proof engine + evaluator *derive* from them. Putting them in proof/eval code would duplicate unit knowledge the catalog should own.

**Cross-component propagation.**
- **Runtime (parser, type checker, evaluator, diagnostics):** type checker / proof engine read the new catalog flags to gate the `QualifierChain` obligation; the **evaluator gains the conversion reduction rule** (the documented runtime obligation); diagnostics: the existing PRE0114 message is broadened to name the real-world mismatch, and a distinct diagnostic (or PRE0114 variant) covers "non-exact factor not supported." No parser change (no new syntax).
- **Tooling (highlighting, completions, hover, semantic tokens):** **hover** must surface the conversion (which unit→which, the factor) per the inspectability requirement; no highlighting/completion/semantic-token change.
- **MCP (vocabulary, DTOs, tool output):** None — no new catalog member *names* reach MCP vocabulary; the proof-obligation output already carries the QualifierChain disposition.

**Breaking changes.** None to public contract: no catalog member renames, no diagnostic-code removal. The *behavior* of `price × quantity` changes (cross-unit now cancels-with-conversion instead of erroring/silently-mis-cancelling), which is the intended semantic change; no external authors exist (PoC).

### External architectural precedent

The architectural problem — *where does per-unit conversion metadata live* — is solved by every runtime unit library by attaching conversion data to the unit registry, not to the arithmetic code. Indriya (JSR-385 RI) attaches a `UnitConverter` (an exact `RationalConverter`, "quotient of two `BigInteger`") to each unit; Pint attaches factors to its unit registry. Precept takes the same registry-owns-conversion approach but **diverges** by storing factors as exact `decimal` (not `BigInteger` rationals or floats) to match its `decimal` magnitude backing (D12), and by adding the `exactDecimal` *flag* so the type system can *reject* (rather than silently round) the non-exact cases the survey found other systems handle silently — consistent with Precept's catalog-driven, prevention-first stance (`compiler-and-runtime-design.md § 2` grounds the catalog choice the same way).

## Inventory of what will be built

- **Catalog (UCUM atom metadata):** add per-unit `AmountConversionFactor` (exact `decimal`, scale to the dimension's base unit) and `IsExactDecimalFactor` (bool); add `IsRatioScale` (bool; false for absolute-position units — none currently admissible as `quantity`, set the stage for Slice 4). Files: the UCUM/unit catalog source (`src/Precept/Language/Ucum/` — `DimensionCatalog.cs` and the atom table) + `UnitDimensionHelper.cs`.
- **Proof engine:** extend the `QualifierChain` discharge in `ProofEngine.Qualifiers.cs` to read `IsRatioScale` (both operands) and `IsExactDecimalFactor(factor(u_q→u_p))`; emit a diagnostic when the factor is non-exact. No new `ProofRequirementKind`.
- **Evaluator (documented obligation, built when runtime lands):** the reduction rule — look up `factor(u_q→u_p)`, apply in `decimal`, cancel, tag `money`.
- **Diagnostics:** broaden PRE0114 wording (domain-targeted); add (or variant) "non-exact conversion factor not supported."
- **Tooling:** hover surfaces the conversion.
- **Tests:** `test/Precept.Tests/Operations/PriceTimesQuantityTests.cs` — un-skip/extend the cross-unit marker to assert correct cancellation; add exactness-gate rejection (a non-exact-factor unit), affine-as-amount acceptance (°C-of-change cancels), cross-dimension still-errors, count still-errors.
- **Docs:** see Doc-update enumeration.

## Decisions

### Decision 1: Extend D8's auto-convert-within-dimension to `price × quantity → money` cancellation (target-directed to the price denominator unit)

**Stakes**: high

- **Rationale**: `price × quantity` cancellation is the same *commensurable-arithmetic* family D8 already governs for `quantity ± quantity`; cancelling a per-unit rate against a same-dimension magnitude is dimensionally identical to combining two same-dimension magnitudes. Every runtime-capable unit system auto-converts; Precept has a runtime and already normalizes to base units, so extending D8's rule (not inventing a new one) is the consistent, lowest-surprise choice.
- **Tradeoff accepted**: the conversion happens implicitly (the author doesn't see the `/1000`), and in the current PoC the compile-time `"Proved"` forward-references an unbuilt runtime conversion. Mitigated by the inspectability requirement (hover surfaces it) and by documenting the runtime obligation (Semantic Rules).
- **Alternatives considered**: **(A) Exact-unit-only** (reject `kg × g`, author converts): rejected — D8 already rejected "require explicit conversion always — verbose" for the parallel `quantity` case; the systems that require explicit conversion do so only because they lack a runtime (F# erasure), and Precept doesn't. **(B) Auto-convert but emit a precision warning every time**: rejected — for exact factors there is no inexactness to warn about; unconditional warnings are false noise (the exactness gate, Decision 2, handles the real inexact cases by rejection). **(C) Leave the Slice-1 silent cancellation as-is**: rejected — it drops the factor → wrong money (violates Prevention/Honesty).
- **Precedent**: `business-domain-types.md § D8` (locked: auto-convert within dimension, target-directed then left-operand, UnitsNet precedent); the cross-unit survey (Frink, Pint, Indriya, Rust uom, UnitsNet all auto-convert).
- **Sources consulted for this decision**:
  - `business-domain-types.md:1733-1739` — "Arithmetic between `quantity` values of the same UCUM dimension is allowed even when units differ. Resolution: (1) target-directed, (2) left-operand wins. … Alternatives rejected … (C) Require explicit conversion always — verbose. … Precedent: UnitsNet supports cross-unit arithmetic with target-directed conversion."
  - `business-domain-types.md:220` — "`UnitPrice * Weight` where Weight is `quantity in 'kg'` is a compile-time error — the price's denominator is `each`, not `kg`." (confirms only the cross-*dimension* case is locked; cross-unit-same-dimension is open.)
  - `cross-unit-conversion-arithmetic-survey.md` § Findings/Conclusion 1 — "Every system with a runtime conversion engine auto-converts … F#'s 'explicit conversion' is … a consequence of erasure, not an argument against auto-conversion."
- **Strongest counter-evidence**: `business-domain-types.md:1866` — price *comparison* requires "same currency AND same denominator unit" (exact unit, no conversion). One could argue price denominators should be exact-unit for cancellation too. Response: comparison and cancellation are different operations — comparison asks "are these the same magnitude" (where unit identity matters for a meaningful bound), while cancellation *consumes* the unit; D8 already auto-converts for `quantity` cancellation/division, and the survey shows no runtime system treats rate-cancellation as exact-unit-only. The comparison rule is about bound semantics, not a general "price denominators never convert" principle.
- **Reversibility**: `Easy` in this project's context — pre-release PoC, no external authors, runtime not yet built; reversal = revert the catalog flags + proof extension + re-probe. (Would be `Hard`/irreversible only post-public-release; classified `high` not `irreversible` for that reason — see `project_solo_dev_pre_release`.)
- **Blast radius**: catalogs — UCUM atom metadata (+2-3 fields). Docs — `business-domain-types.md` (extend D8 / price section), `proof-engine.md` (QualifierChain side-conditions), `catalog-system.md` (new metadata). Samples — one cross-unit line-total example. External consumers — none.

### Decision 2: Exactness gate — auto-convert only across exact-`decimal`, ratio-scale factors

**Stakes**: high

- **Rationale**: auto-conversion is honest only when the factor is an exact `decimal` (kg↔g ×0.001, lb↔kg ×0.45359237). Transcendental (angle/π), log-scale, and multiply-defined (calorie/BTU) factors cannot be represented exactly in `decimal`; auto-converting them would inject silent approximation into the exact `money` lane — exactly what Principle 8 forbids. Rejecting them (rather than rounding) keeps the lane honest.
- **Tradeoff accepted**: units whose cross-scale factor is not an exact `decimal` cannot participate in cross-unit cancellation; the author must use a same-unit pair or handle them explicitly. (Same-*unit* still works for any unit — only cross-*scale* requires the exact factor.)
- **Alternatives considered**: **(A) Assume all UCUM factors exact**: rejected — the normalization survey flags this holds only for currently-cataloged units; transcendental/log factors break it. **(B) Float factors (Pint's choice)**: rejected — Pint #201 shows float factors silently inject error even for exact conversions; incompatible with `decimal` backing (D12) and Principle 8.
- **Precedent**: Indriya `RationalConverter` ("exact scaling factor … quotient of two `BigInteger`") — the exactness-preserving design Precept's `decimal`-factor approach parallels; Pint #201 — the float counter-example; `quantity-normalization-design-survey.md` names the exact-factor assumption as a bounded MEDIUM.
- **Sources consulted for this decision**:
  - `cross-unit-conversion-arithmetic-survey.md` § Conclusion 2 — "auto-convert-and-cancel is sound where the cross-scale factor is an exact `decimal` … Transcendental/irrational/log-scale factors are out of scope."
  - `philosophy.md:25` — "Silent approximation inside an exact-looking path weakens the user's ability to reason about outcomes … Precept therefore draws a hard line between exact and approximate behavior and requires that line to be visible in the type system and public surface."
- **Strongest counter-evidence**: no surveyed system *rejects* inexact factors — they all either use exact rationals (Indriya) or silently round (Pint); none refuse. Response: that none refuse is *because none make Precept's exact/approximate-must-be-visible commitment*; rejecting (or, later, surfacing approximation explicitly) is the Precept-specific honesty stance, conservative relative to the field. "No counter-evidence found that rejection is *wrong*; found only that others don't bother."
- **Reversibility**: `Easy` — loosening the gate (admit a flagged-approximate factor with an explicit approximation marker) is an additive future change; tightening is the conservative default.
- **Blast radius**: catalog `IsExactDecimalFactor` flag; one diagnostic; docs as Decision 1. External consumers — none.

### Decision 3: The exclusion is *absolute positions* only — `quantity` amounts (incl. °C/°F/dB) all cancel; absolute-reading math → Slice 4

**Stakes**: medium

- **Rationale**: `quantity` is by definition an *amount* (a magnitude), the `duration`-not-`instant` analog, so the **position rule** excludes nothing — an amount has no offset to trip on (20 °C of change → °F is ×1.8, the +32 cancels in a difference). **Cross-scale admissibility is then governed independently by the exactness gate (Decision 2):** °C↔°F (×1.8 — an exact `decimal`) is admitted; dB↔Np (÷8.686 = 20/ln 10 — *irrational*) is **rejected by the exactness gate**, exactly like angle→radian. So dB is *not excluded by the position rule* — a dB *gain* is an amount, and priced **same-unit** (`$5/dB × 10 dB`, factor 1) it cancels fine — but its **cross-scale** conversion is exactness-gated, not "clean." Only *absolute positions* (a thermostat reading, a `dBm` level) are excluded by the position rule, and `quantity` does not model those — they are a separate point-type (Slice 4). The two gates compose: a cross-unit cancellation is admitted iff (amount/ratio-scale) **and** (exact-`decimal` factor).
- **Tradeoff accepted**: authors who conflate an absolute reading with an amount (model a thermostat reading as `quantity in 'Cel'`) get a *quantity*-semantics conversion (scale-only), which is wrong for an absolute reading — a teachability risk (the `instant`-vs-`duration` discipline) that the design mitigates with docs, not a soundness hole.
- **Alternatives considered**: **(A) Exclude all affine/log units** (the survey's first-pass conclusion): rejected — too broad; it conflates the absolute *position* (genuinely non-multiplicable) with the *amount* (fine), excluding valid pricing like "per kelvin" or same-unit "per dB-of-gain" (the latter still subject to the exactness gate for cross-scale). **(B) Special-case affine units with delta semantics inside cancellation as a default**: rejected as a *default* — for `quantity` it is already the amount, so no special-case is needed; the only place delta-vs-absolute matters is absolute positions, which `quantity` excludes by being an amount.
- **Precedent**: the four-system convergence (Pint `delta_degC`, GNU `degC` increment, mp-units point-vs-vector, Boost `absolute<>`) — all forbid the *point* and admit the *amount/increment*; Precept's own `instant`/`duration` split is the same shape.
- **Sources consulted for this decision**:
  - `cross-unit-conversion-arithmetic-survey.md` § Conclusion 3 (refined note) — "`quantity` amounts are **not excluded by the position rule** … Cross-scale admissibility is then the exactness gate's call … °C↔°F (×1.8, an exact decimal) is admitted; dB↔Np (÷8.686 = 20/ln 10, *irrational*) is **rejected** like angle→radian."
  - `price-cross-unit-cancellation-2026-05-31.md` § Decision & scope correction — the amount-vs-position analysis.
- **Tradeoff** (restated): see above. (medium stakes — counter-evidence/reversibility/blast-radius legs optional, but: reversible `Easy`; blast radius = the Slice-4 carve-out boundary + docs.)

### Decision 4: The conversion metadata lives in the UCUM atom catalog (not pipeline code)

**Stakes**: medium

- **Rationale**: a unit's scale-to-base and whether that scale is an exact `decimal` are intrinsic unit properties; per Precept's catalog-driven architecture they are declared once in the catalog and derived everywhere (proof engine, evaluator, hover). Encoding them in proof/eval code would duplicate unit knowledge the catalog must own (a catalog-discipline violation).
- **Tradeoff accepted**: the UCUM atom table grows by 2-3 fields per unit; the catalog becomes the single point that must stay correct against the UCUM standard (Evolvability addresses versioning).
- **Alternatives considered**: **(A) Compute factors in the proof engine / evaluator on demand** (parse UCUM at proof time): rejected — duplicates unit semantics outside the catalog, the exact anti-pattern CLAUDE.md's catalog rules forbid. **(B) A separate conversion-table doc**: rejected — splits unit metadata from the unit catalog.
- **Precedent**: Indriya/Pint attach conversion data to the unit registry, not the arithmetic; `compiler-and-runtime-design.md § 2` grounds Precept's catalog-owns-domain-knowledge stance.
- **Sources consulted for this decision**:
  - `cross-unit-conversion-arithmetic-survey.md` § Implications — "Whether a unit is ratio-scale … and whether its factor is an exact `decimal`, are unit-metadata facts — they belong in the UCUM atom catalog … not in per-operator dispatch logic."
  - `CLAUDE.md` § Catalog System — "domain knowledge is declared as structured metadata, and pipeline stages … derive from it. They never maintain parallel copies."
- **Tradeoff** (restated): catalog growth + version-maintenance burden (see Evolvability).

## Falsifiers

- If, after the runtime ships, **three or more `samples/` need a manual unit-conversion workaround** to get a correct line total, the auto-conversion is not actually firing where authors expect — re-examine the target-unit resolution.
- If the **exactness gate rejects a common business unit pair** (not just exotic angle/log units) — e.g. a frequently-used energy or volume conversion turns out non-exact-`decimal` — the gate is too strict and must widen (or surface-approximation must replace rejection for that class).
- If a domain expert in review **cannot tell from inspection that a conversion happened** (hover doesn't surface it), the inspectability claim is falsified and Principle 4 is violated — the conversion is then "hidden."
- If **absolute-position pricing turns out to be a real, recurring business need** before Slice 4 ships (authors repeatedly want `price per absolute °C`), the "edge case, defer to Slice 4" scoping was wrong and Slice 4 must be pulled forward.

## Acceptance criteria

- `price in 'USD/kg' × quantity in 'g'` compiles; the `QualifierChain` obligation discharges; (when runtime ships) `4.00 USD/kg × 500 g` evaluates to `2.00 USD`. **Test**: scenario test asserts no PRE0114 + (runtime) correct magnitude.
- `price in 'USD/kg' × quantity in 'm'` (cross-dimension) still emits PRE0114. **Test**: present (`DifferentDimension_UnitForm_StillErrors`).
- `price in 'USD/each' × quantity in 'box'` (count, no factor) still rejects (PRE0137). **Test**: scenario test asserts the count diagnostic.
- A non-exact-`decimal`-factor cross-unit pair is rejected with the "non-exact factor not supported" diagnostic. **Test**: scenario test on a flagged-inexact unit pair.
- A temperature *amount* cross-unit case (`price in 'USD/Cel' × quantity in 'Cel'`, or °C↔K amount) compiles and cancels (NOT excluded). **Test**: scenario test asserts clean.
- The skipped `CrossUnit_SameDimension_MustNotSilentlyCancel` test is un-skipped and rewritten to assert *correct* cancellation (factor applied), closing the live hole.
- `business-domain-types.md § D8` / price section documents the extension; `proof-engine.md` documents the side-conditions; `catalog-system.md` documents the new metadata. **Test**: doc-update enumeration satisfied at promote.

## Dependencies

- **Upstream**: D8 (locked); Slice 1 (the `Dimension ← Unit` projection — shipped `b7c17482`); the cross-unit survey (`37971a4b`). The **runtime evaluator** must exist for the conversion to actually execute — until then the compile-time contract + documented obligation stand.
- **Downstream**: enables correct cross-unit line totals in samples; sets the `IsRatioScale` flag that **Slice 4** (absolute positions) will consume; informs the eventual runtime arithmetic for all D8 conversions (this is the first place the runtime conversion obligation is pinned).

## Doc-update enumeration

- `docs/language/business-domain-types.md` § price / § D8 — extend D8's stated scope to price cancellation; state the exactness gate and the absolute-position exclusion.
- `docs/compiler/proof-engine.md` § QualifierChain — the ratio-scale + exact-factor side-conditions on the cross-unit discharge.
- `docs/language/catalog-system.md` § (UCUM/unit catalog) — the new `AmountConversionFactor` / `IsExactDecimalFactor` / `IsRatioScale` metadata.
- `docs/compiler/diagnostic-system.md` — the broadened PRE0114 wording + the non-exact-factor diagnostic.
- `docs/tooling/language-server.md` — hover surfaces the conversion.
- `docs/runtime/evaluator.md` — the conversion reduction rule (the runtime obligation), flagged as pending runtime implementation.

## Operational dimensions

- **Observability** (touches evaluation + diagnostics): when a cross-unit cancellation surfaces a diagnostic (cross-dimension, count, or non-exact factor), the message names the real-world mismatch and the fix (see Audience). When it *succeeds*, the conversion (units + factor) is surfaced in hover/proof-attribution so the author can see the `/1000` happened — required by the inspectability principle. Runtime conversion, once built, should be traceable in the structured outcome.
- **Evolvability** (depends on UCUM): the `AmountConversionFactor` / `IsExactDecimalFactor` values are pinned to a specific UCUM version (the catalog's UCUM atom table). Migration story: a UCUM revision that changes a factor or reclassifies a unit's exactness requires a catalog update + a re-run of the conversion scenario tests; the catalog is the single point of change (Decision 4). If UCUM removes/renames an atom Precept catalogs, that surfaces as a catalog-vs-UCUM diff at update time, not a silent drift. Pinning the UCUM version in the catalog source is required.

## Open questions

None blocking. (The temperature/log *increment-spelling* sugar — `delta °C` — is explicitly **deferred** to a future language-surface question, not open for this design: "per kelvin" already expresses per-degree-of-change. Absolute-position math is **Slice 4**, not this design.)
