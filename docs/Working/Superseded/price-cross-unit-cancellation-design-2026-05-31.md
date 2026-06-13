---
status: Locked 2026-05-31
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

> **SUPERSEDED 2026-06-11** — replaced by [`compiler-readiness-plan-2026-06-11.md`](../compiler-readiness-plan-2026-06-11.md). Retained for history; all still-valid obligations were mined into that plan (see its `-appendices/working-docs-triage.md`). Do **not** treat as current strategy.


# Price × Quantity Cross-Unit Cancellation — Design (Auto-Convert Within Dimension)

> **Review trail (2026-05-31):** reviewed by `precept-reviewer` (0 blockers; 2 concerns — dB position-vs-exactness conflation + a paraphrased philosophy excerpt — both applied) and by Frank/Squad (`phase7-slice2-design-review-FRANK-2026-05-31.md`, **conditionally approved**; conditions applied — `:168` doc-update disambiguation, `IsRatioScale` timing, `ScaleToBaseFactor` rename, forward-reference tracking home). Frank's prior Option-1 leaning formally withdrawn. Ready to lock pending owner sign-off. Decision 2 was subsequently revised (owner ruling 2026-05-31) from a rejection gate to allow-all-with-surfacing; the prior precept-reviewer + Frank approvals of the rejection gate are superseded. Re-reviewed: precept-reviewer (2026-05-31) returned **READY TO LOCK** (0 blockers, 0 concerns, 1 NIT on P8's "type system" clause). **Owner affirmed (2026-05-31)** that inspection-surfacing — backed by the `maxplaces`-exactness type guarantee — satisfies P8's "visible in the type system" clause (no type-level approximate-`money` marker is built now; the second Falsifier watches the only future case where that would flip). **Locked 2026-05-31.**

## Goal

When done, `price in 'USD/kg' × quantity in 'g' → money in 'USD'` compiles **and** evaluates to the correctly-scaled amount (the quantity is converted g→kg by the exact UCUM factor before cancelling), while cross-*dimension* (`kg × m`) and no-universal-factor count pairs (`each × box`) stay rejected — demonstrated by a scenario-test matrix and the runtime evaluator applying the factor.

## Scope

- **In scope**: the cancellation semantics for `price × quantity → money` (and its commutative `quantity × price`) when operands share a UCUM dimension but differ in unit/scale; the compile-time proof obligation; the runtime conversion obligation (documented as a requirement — runtime evaluator is still a stub); a catalog metadata fact (per-unit exact-rational scale-to-base factor) + inspection-surfacing of the conversion's exact/approximate status; all commensurable conversions allowed; the narrowed exclusion (absolute positions only).
- **Out of scope**: absolute-position measurement values (thermostat readings, `dBm` levels, absolute pH) — carved to **Slice 4** (a new point-type construct); the `money / price → quantity` and `price × period/duration` paths (separate operations, governed by D15); any change to cross-*dimension* rejection (`kg × m` stays an error).
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
| 7. Compile-time-first static checking | Y | The proof engine discharges the dimension-match + ratio-scale obligation at compile time; rejects cross-dimension and count (`spec §0.1 #7`). | N/A | N/A |
| 8. Approximation honesty | Y | The conversion's exact/approximate status is **surfaced** (visible, per P8 "the line must be visible") — not forbidden; the `money` lane stays exact (values correct to `maxplaces`); exact-rational conversions are exact, irrational ones are surfaced as approximate (`philosophy.md:25`, `spec §0.1 #8`). | A converted result may still need `maxplaces` rounding. | The money result rides the *existing* `maxplaces` rounding (already honest); this design adds no new approximation. |
| 9. Mandatory rationale | N | No rule/ensure surface change. | N/A | N/A |
| 10. Totality | Y | Runtime conversion is total over all commensurable pairs; `decimal` rounding at `maxplaces` is total (`spec §0.1 #10`). | N/A | N/A |
| 11. Static completeness | Y | All commensurable conversions are total (defined for every same-dimension pair); a well-typed cross-unit cancellation won't fault once the runtime ships (`spec §0.1 #11`). | The runtime piece is unbuilt (PoC). | The compile-time contract is stated now; the evaluator obligation is documented so static-completeness holds when the runtime lands. |

**Principle-1/11 tradeoff (stated, per the matrix):** The design accepts that, in the current pre-runtime PoC, the compile-time `"Proved"` for cross-unit cancellation is a *forward reference* to an unbuilt runtime conversion. This is honest **only** because (a) nothing ships — there is no external author who can observe a wrong result, and (b) the runtime requirement is explicitly documented (§ Semantic Rules → runtime obligation) and tracked (skipped test + this doc), rather than left as hidden certainty. When the runtime is built, applying the factor makes the `"Proved"` literally true. This is the philosophy-honest interpretation of "document the runtime requirements well." **The forward-reference is tracked in two durable places — the `evaluator.md` reduction-rule obligation (canonical) and the skipped `CrossUnit_SameDimension_MustNotSilentlyCancel` test — *not* a code comment, which the no-transient-refs-in-code rule would forbid for a reference to a design/slice; canonical-doc + test keeps the obligation visible without rotting refs.**

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
  ────────────────────────────────────────────────────────────────────────
                       Γ ⊢ p * q : money⟨currency c⟩
```

`dim(u_p)=dim(u_q)` is the existing dimension-match (discharged today). The remaining premise — `ratioScale(...)` (both operands are ratio-scale, i.e. not absolute-position units) — is the position gate. The conversion is always admitted for same-dimension ratio-scale operands; its exact/approximate status is computed (from whether the operand scales are rational) for surfacing in inspection, not for gating. `u_q = u_p` (same unit) is the degenerate case with `factor = 1` (Slice 1).

**Evaluation / reduction rule** (the runtime obligation — currently unbuilt; documented as the requirement):

```
  p ⇓ price(m_p in c per u_p)     q ⇓ quantity(m_q in u_q)     k = factor(u_q → u_p)
  ──────────────────────────────────────────────────────────────────────────────────
            p * q  ⇓  money( m_p · (m_q · k)  in  c )
```

The evaluator must (1) look up the exact-**rational** factor `k = factor(u_q → u_p)` from the catalog and apply it in `decimal` (rounding only at the standard `decimal`/`maxplaces` boundary; target-directed: convert the quantity to the price's denominator unit, per D8 resolution rule 1), (2) compute `m_q · k` in `decimal`, (3) multiply by the price magnitude, (4) tag the result `money in c`. For `u_q = u_p`, `k = 1` (Slice-1 behavior, already correct). **This reduction rule is the load-bearing runtime requirement** the owner asked to be documented well.

**Proof obligations.** The existing `QualifierChainProofRequirement` (Dimension↔Dimension, `Operations.cs:650`, discharged via `ProofEngine.Qualifiers.cs`) is **extended** with two checks read from catalog metadata:
- *Ratio-scale*: both operand units carry the `IsRatioScale` flag (reject absolute-position units → routes to Slice 4 when that exists). The check is **wired now** but a **no-op** until Slice 4 introduces non-ratio units — every currently-cataloged unit is `IsRatioScale = true`.

The exact-vs-approximate classification is computed (from whether the operand scales are rational) **for inspection surfacing, not for gating**.

No new `ProofRequirementKind` is required — the existing `QualifierChain` requirement gains a catalog-driven side-condition. (Inventory notes the metadata additions.)

**Soundness preservation claim.**
- **Principle 8 (approximation honesty):** holds — the `money` result is correct to its declared `maxplaces` (the standard precision contract) and the conversion's exact/approximate status is surfaced (visible) in inspection; no silent approximation, no rejection needed.
- **Principle 10 (totality):** holds — `factor(u_q → u_p)` is total over all commensurable pairs; `decimal` arithmetic with rounding is total (no `NaN`/`Infinity`).
- **Principle 11 (static completeness):** holds *once the runtime ships* — every admitted cancellation has a defined evaluation (the reduction rule), and every rejected case is a compile diagnostic, so a well-typed program has no cross-unit runtime fault. In the current pre-runtime PoC the evaluator is a stub for *all* arithmetic; this design adds the obligation that the cross-unit path apply `k` when that stub is implemented (tracked, not hidden — see Philosophy tradeoff).

## Architecture Grounding

### Precept-internal placement

**Layer placement.** The new facts — per-unit **exact-rational scale-to-base factor** and an optional **`ScaleIsRational` classification** used for inspection surfacing — belong in the **UCUM atom catalog** (catalog metadata), not in pipeline dispatch. They are intrinsic unit properties (a unit's scale relative to its dimension's base; whether that scale is rational), so per Precept's catalog-driven architecture they are declared once in the catalog and the proof engine + evaluator *derive* from them. Putting them in proof/eval code would duplicate unit knowledge the catalog should own.

**Cross-component propagation.**
- **Runtime (parser, type checker, evaluator, diagnostics):** the proof engine reads `IsRatioScale` only to gate the `QualifierChain` obligation; the exact/approximate classification feeds hover, not a diagnostic; the **evaluator gains the conversion reduction rule** (the documented runtime obligation); diagnostics: the existing PRE0114 message is broadened to name the real-world mismatch. No parser change (no new syntax).
- **Tooling (highlighting, completions, hover, semantic tokens):** **hover** must surface the conversion (which unit→which, the factor) per the inspectability requirement; no highlighting/completion/semantic-token change.
- **MCP (vocabulary, DTOs, tool output):** None — no new catalog member *names* reach MCP vocabulary; the proof-obligation output already carries the QualifierChain disposition.

**Breaking changes.** None to public contract: no catalog member renames, no diagnostic-code removal. The *behavior* of `price × quantity` changes (cross-unit now cancels-with-conversion instead of erroring/silently-mis-cancelling), which is the intended semantic change; no external authors exist (PoC).

### External architectural precedent

The architectural problem — *where does per-unit conversion metadata live* — is solved by every runtime unit library by attaching conversion data to the unit registry, not to the arithmetic code. Indriya (JSR-385 RI) attaches a `UnitConverter` (an exact `RationalConverter`, "quotient of two `BigInteger`") to each unit; Pint attaches factors to its unit registry. Precept takes the same registry-owns-conversion approach, storing factors as **exact rationals** (like Indriya) and **allowing all commensurable conversions, surfacing exact/approximate rather than rejecting**. It diverges from the libraries that *reject* affine/log point operations by being more permissive — admitting every same-dimension cancellation — while keeping the conversion's exact-vs-approximate status visible in inspection, consistent with Precept's catalog-driven, prevention-first stance (`compiler-and-runtime-design.md § 2` grounds the catalog choice the same way).

## Inventory of what will be built

- **Catalog (UCUM atom metadata):** add per-unit `ScaleToBaseFactor` (exact **rational**, scale to the dimension's base unit) and an optional `ScaleIsRational` (bool — for inspection labelling, not gating); add `IsRatioScale` (bool) — **stored now, `true` for every currently-cataloged unit** (no absolute-position unit is admissible as a `quantity` yet); Slice 4 introduces the `false`-flagged point units. The proof-engine `IsRatioScale` check is **wired now and is a no-op** until those units exist (so Slice 4 adds units + flag values, not a proof-engine change). Files: the UCUM/unit catalog source (`src/Precept/Language/Ucum/` — `DimensionCatalog.cs` and the atom table) + `UnitDimensionHelper.cs`.
- **Proof engine:** extend the `QualifierChain` discharge in `ProofEngine.Qualifiers.cs` to read `IsRatioScale` (both operands); classify exact/approximate for surfacing. No new `ProofRequirementKind`.
- **Evaluator (documented obligation, built when runtime lands):** the reduction rule — look up `factor(u_q→u_p)`, apply in `decimal`, cancel, tag `money`.
- **Diagnostics:** broaden PRE0114 wording (domain-targeted).
- **Tooling:** hover surfaces the conversion.
- **Tests:** `test/Precept.Tests/Operations/PriceTimesQuantityTests.cs` — un-skip/extend the cross-unit marker to assert correct cancellation; add `in↔ft` cancels cleanly (exact rational; result correct to `maxplaces`); an irrational-factor conversion cancels and is surfaced approximate (not rejected); affine-as-amount acceptance (°C-of-change cancels), cross-dimension still-errors, count still-errors.
- **Docs:** see Doc-update enumeration.

## Decisions

### Decision 1: Extend D8's auto-convert-within-dimension to `price × quantity → money` cancellation (target-directed to the price denominator unit)

**Stakes**: high

- **Rationale**: `price × quantity` cancellation is the same *commensurable-arithmetic* family D8 already governs for `quantity ± quantity`; cancelling a per-unit rate against a same-dimension magnitude is dimensionally identical to combining two same-dimension magnitudes. Every runtime-capable unit system auto-converts; Precept has a runtime and already normalizes to base units, so extending D8's rule (not inventing a new one) is the consistent, lowest-surprise choice.
- **Tradeoff accepted**: the conversion happens implicitly (the author doesn't see the `/1000`), and in the current PoC the compile-time `"Proved"` forward-references an unbuilt runtime conversion. Mitigated by the inspectability requirement (hover surfaces it) and by documenting the runtime obligation (Semantic Rules).
- **Alternatives considered**: **(A) Exact-unit-only** (reject `kg × g`, author converts): rejected — D8 already rejected "require explicit conversion always — verbose" for the parallel `quantity` case; the systems that require explicit conversion do so only because they lack a runtime (F# erasure), and Precept doesn't. **(B) Auto-convert but emit a precision warning every time**: rejected — for exact factors there is no inexactness to warn about; unconditional warnings are false noise (Decision 2 allows all commensurable conversions and surfaces exact/approximate; no rejection). **(C) Leave the Slice-1 silent cancellation as-is**: rejected — it drops the factor → wrong money (violates Prevention/Honesty).
- **Precedent**: `business-domain-types.md § D8` (locked: auto-convert within dimension, target-directed then left-operand, UnitsNet precedent); the cross-unit survey (Frink, Pint, Indriya, Rust uom, UnitsNet all auto-convert).
- **Sources consulted for this decision**:
  - `business-domain-types.md:1733-1739` — "Arithmetic between `quantity` values of the same UCUM dimension is allowed even when units differ. Resolution: (1) target-directed, (2) left-operand wins. … Alternatives rejected … (C) Require explicit conversion always — verbose. … Precedent: UnitsNet supports cross-unit arithmetic with target-directed conversion."
  - `business-domain-types.md:220` — "`UnitPrice * Weight` where Weight is `quantity in 'kg'` is a compile-time error — the price's denominator is `each`, not `kg`." (confirms only the cross-*dimension* case is locked; cross-unit-same-dimension is open.)
  - `cross-unit-conversion-arithmetic-survey.md` § Findings/Conclusion 1 — "Every system with a runtime conversion engine auto-converts … F#'s 'explicit conversion' is … a consequence of erasure, not an argument against auto-conversion."
- **Strongest counter-evidence**: `business-domain-types.md:1866` — price *comparison* requires "same currency AND same denominator unit" (exact unit, no conversion). One could argue price denominators should be exact-unit for cancellation too. Response: comparison and cancellation are different operations — comparison asks "are these the same magnitude" (where unit identity matters for a meaningful bound), while cancellation *consumes* the unit; D8 already auto-converts for `quantity` cancellation/division, and the survey shows no runtime system treats rate-cancellation as exact-unit-only. The comparison rule is about bound semantics, not a general "price denominators never convert" principle.
- **Reversibility**: `Easy` in this project's context — pre-release PoC, no external authors, runtime not yet built; reversal = revert the catalog flags + proof extension + re-probe. (Would be `Hard`/irreversible only post-public-release; classified `high` not `irreversible` for that reason — see `project_solo_dev_pre_release`.)
- **Blast radius**: catalogs — UCUM atom metadata (+2-3 fields). Docs — `business-domain-types.md` (extend D8 / price section), `proof-engine.md` (QualifierChain side-conditions), `catalog-system.md` (new metadata). Samples — one cross-unit line-total example. External consumers — none.

### Decision 2: Allow all commensurable conversions; surface the conversion's exact-vs-approximate status (no exactness rejection)

**Stakes**: high

- **Rationale**: Principle 8 requires the exact/approximate line be **visible**, not **forbidden** (`spec §0.1 #8`: "the line between exact and approximate behavior must be visible in the type system and the language surface"). Three facts make every commensurable conversion honest without a rejection gate: (1) the author **explicitly** writes the cross-unit expression (`'USD/ft' × quantity in 'in'`), so the conversion is visible in the source — not silent; (2) the result's precision rides the **existing `maxplaces`/rounding discipline** that already governs every money computation (`money / 3`, a tax `× 0.0725`), which the spec already treats as honest; (3) a factor like `in→ft = 1/12` is an **exact rational** — one inch *is exactly* 1/12 foot — and its non-termination in base-10 is the *same* representational rounding any decimal division has, absorbed at `maxplaces` (below which sub-precision is discarded — even an irrational factor's truncation at 28 digits is invisible at a 2–6-place money `maxplaces`). So the conversion is **allowed**; the type checker **surfaces** whether it was exact (rational factor) or approximate (irrational — angle π, log units) in inspection/hover, satisfying P8's visibility requirement.
- **Tradeoff accepted**: the `money` lane carries values produced via conversion whose sub-`maxplaces` precision depends on the factor's representation; accepted because the result is correct to the declared `maxplaces` (the standard money-precision contract) and the conversion is surfaced. Irrational conversions (exotic in business — angle/log pricing) are allowed and *labelled* approximate in inspection rather than blocked.
- **Alternatives considered**: **(A) Exactness rejection gate** (reject non-exact-`decimal` factors — this design's own first draft): rejected — it blocks **table-stakes** business conversions (`in↔ft = 1/12`, `ft↔yd = 1/3`, `yd↔mi`), which are *exact* rationals, not approximations; and it **over-applies P8**, which asks for visibility, not prohibition. **(B) Reject only irrational factors** (admit rationals, block π/log): rejected — still blocks pricing the author explicitly chose, and the irrational truncation is absorbed by `maxplaces` anyway, so there is no observable dishonesty to prevent; surfacing it is the right, lighter mechanism. **(C) Float factors (Pint's choice)**: rejected — Pint #201 shows float factors silently inject error; exact-rational scales (Indriya-style) carry the factor exactly, with rounding only at the standard `decimal`/`maxplaces` boundary.
- **Precedent**: every runtime unit system **allows** these conversions and **none reject** `in↔ft` (Frink, Pint, GNU `units`, Indriya, Rust uom — survey § Findings); Indriya carries factors as exact `BigInteger` rationals (`RationalConverter`), the representation Precept's exact-rational scales parallel. The field universally treats conversion precision as a representation/rounding concern, not a rejection.
- **Sources consulted for this decision**:
  - `precept-language-spec.md:105` (§0.1 #8) — "Exact-value lanes remain exact. … The line between exact and approximate behavior must be visible in the type system and the language surface." (visible, **not** forbidden — the load-bearing reframe.)
  - `cross-unit-conversion-arithmetic-survey.md` § Findings — "Every system with a runtime conversion engine auto-converts … Indriya … exact rational (`RationalConverter` = quotient of two `BigInteger`)."
  - `business-domain-types.md § D8` (:1733-1739) — "auto-convert … using UCUM standard conversion factors" (no terminating-decimal restriction in the locked rule).
- **Strongest counter-evidence**: this design's *own first draft* argued a rejection gate was the P8 mechanism, and both `precept-reviewer` and Frank approved that framing. Response: that framing conflated "non-terminating in decimal" with "approximate" — `in→ft` is *exact* — and read P8 as "forbid" when the text says "make visible." The owner ruled (allow all, surface) as the more faithful reading. **The earlier approvals of the rejection gate are superseded by this revision; re-review is required.**
- **Reversibility**: `Easy` — re-introducing a gate later (if a real, observable dishonesty surfaces) is additive; PoC, no external authors.
- **Blast radius**: catalog — `ScaleToBaseFactor` becomes an exact rational; the `IsExactDecimalFactor` flag is dropped; an optional `ScaleIsRational` surfacing classification is added (for inspection labelling, not gating). Proof engine — no exactness discharge condition. Diagnostics — the "non-exact factor" diagnostic is dropped (the only rejections remain cross-dimension and count). Docs as Decision 1. External consumers — none.

### Decision 3: The exclusion is *absolute positions* only — `quantity` amounts (incl. °C/°F/dB) all cancel; absolute-reading math → Slice 4

**Stakes**: medium

- **Rationale**: `quantity` is by definition an *amount* (a magnitude), the `duration`-not-`instant` analog, so the **position rule** excludes nothing — an amount has no offset to trip on (20 °C of change → °F is ×1.8, the +32 cancels in a difference). **Cross-scale admissibility is then governed by Decision 2 (allow all commensurable conversions, surface exact/approximate):** °C↔°F (×1.8 — an exact rational) is admitted and surfaced exact; dB↔Np (÷8.686 = 20/ln 10 — *irrational*) is **allowed and surfaced as approximate**, not rejected, exactly like angle→radian. So dB is *not excluded by the position rule* — a dB *gain* is an amount, priced **same-unit** (`$5/dB × 10 dB`, factor 1) it cancels exactly, and its **cross-scale** conversion is allowed with its approximate status made visible. Only *absolute positions* (a thermostat reading, a `dBm` level) are excluded by the position rule, and `quantity` does not model those — they are a separate point-type (Slice 4).
- **Tradeoff accepted**: authors who conflate an absolute reading with an amount (model a thermostat reading as `quantity in 'Cel'`) get a *quantity*-semantics conversion (scale-only), which is wrong for an absolute reading — a teachability risk (the `instant`-vs-`duration` discipline) that the design mitigates with docs, not a soundness hole.
- **Alternatives considered**: **(A) Exclude all affine/log units** (the survey's first-pass conclusion): rejected — too broad; it conflates the absolute *position* (genuinely non-multiplicable) with the *amount* (fine), excluding valid pricing like "per kelvin" or same-unit "per dB-of-gain" (per Decision 2, dB cross-scale is allowed and surfaced approximate). **(B) Special-case affine units with delta semantics inside cancellation as a default**: rejected as a *default* — for `quantity` it is already the amount, so no special-case is needed; the only place delta-vs-absolute matters is absolute positions, which `quantity` excludes by being an amount.
- **Precedent**: the four-system convergence (Pint `delta_degC`, GNU `degC` increment, mp-units point-vs-vector, Boost `absolute<>`) — all forbid the *point* and admit the *amount/increment*; Precept's own `instant`/`duration` split is the same shape.
- **Sources consulted for this decision**:
  - `cross-unit-conversion-arithmetic-survey.md` § Conclusion 3 (refined note) — "`quantity` amounts are **not excluded by the position rule** … per the owner's allow-all-with-surfacing ruling … Precept admits **all** commensurable conversions and *surfaces* exact-vs-approximate rather than rejecting. So for Precept, dB *amounts* cancel: same-unit (factor 1) exactly, and cross-scale (dB↔Np, ÷8.686 = 20/ln 10, *irrational*) **allowed and surfaced as approximate** — like angle→radian, also allowed and surfaced."
  - `price-cross-unit-cancellation-2026-05-31.md` § Decision & scope correction — the amount-vs-position analysis.
- **Tradeoff** (restated): see above. (medium stakes — counter-evidence/reversibility/blast-radius legs optional, but: reversible `Easy`; blast radius = the Slice-4 carve-out boundary + docs.)

### Decision 4: The conversion metadata lives in the UCUM atom catalog (not pipeline code)

**Stakes**: medium

- **Rationale**: a unit's exact-rational scale-to-base and whether that scale is rational (the surfacing classification) are intrinsic unit properties; per Precept's catalog-driven architecture they are declared once in the catalog and derived everywhere (proof engine, evaluator, hover). Encoding them in proof/eval code would duplicate unit knowledge the catalog must own (a catalog-discipline violation).
- **Tradeoff accepted**: the UCUM atom table grows by 2-3 fields per unit; the catalog becomes the single point that must stay correct against the UCUM standard (Evolvability addresses versioning).
- **Alternatives considered**: **(A) Compute factors in the proof engine / evaluator on demand** (parse UCUM at proof time): rejected — duplicates unit semantics outside the catalog, the exact anti-pattern CLAUDE.md's catalog rules forbid. **(B) A separate conversion-table doc**: rejected — splits unit metadata from the unit catalog.
- **Precedent**: Indriya/Pint attach conversion data to the unit registry, not the arithmetic; `compiler-and-runtime-design.md § 2` grounds Precept's catalog-owns-domain-knowledge stance.
- **Sources consulted for this decision**:
  - `cross-unit-conversion-arithmetic-survey.md` § Implications — "…are unit-metadata facts — they belong in the UCUM atom catalog … not in per-operator dispatch logic." (Decision 4 relies on the catalog-placement claim, which is unchanged; the survey's "exact `decimal`" framing for the surfaced fact is superseded by Decision 2 — exact-rational scale + an exact/approximate surfacing classification.)
  - `CLAUDE.md` § Catalog System — "domain knowledge is declared as structured metadata, and pipeline stages … derive from it. They never maintain parallel copies."
- **Tradeoff** (restated): catalog growth + version-maintenance burden (see Evolvability).

## Falsifiers

- If, after the runtime ships, **three or more `samples/` need a manual unit-conversion workaround** to get a correct line total, the auto-conversion is not actually firing where authors expect — re-examine the target-unit resolution.
- If inspection/hover does not surface that a conversion happened and whether it was exact or approximate, P8's visibility requirement is violated and the allow-all-surface choice has not actually delivered visibility.
- If a domain expert in review **cannot tell from inspection that a conversion happened** (hover doesn't surface it), the inspectability claim is falsified and Principle 4 is violated — the conversion is then "hidden."
- If **absolute-position pricing turns out to be a real, recurring business need** before Slice 4 ships (authors repeatedly want `price per absolute °C`), the "edge case, defer to Slice 4" scoping was wrong and Slice 4 must be pulled forward.

## Acceptance criteria

- `price in 'USD/kg' × quantity in 'g'` compiles; the `QualifierChain` obligation discharges; (when runtime ships) `4.00 USD/kg × 500 g` evaluates to `2.00 USD`. **Test**: scenario test asserts no PRE0114 + (runtime) correct magnitude.
- `price in 'USD/kg' × quantity in 'm'` (cross-dimension) still emits PRE0114. **Test**: present (`DifferentDimension_UnitForm_StillErrors`).
- `price in 'USD/each' × quantity in 'box'` (count, no factor) still rejects (PRE0137). **Test**: scenario test asserts the count diagnostic.
- `price in 'USD/ft' × quantity in 'in'` cancels (exact rational 1/12; result correct to `maxplaces`). **Test**: scenario test asserts no PRE0114 + (runtime) correct magnitude.
- An irrational-factor conversion (if such a unit pair is cataloged) cancels and is surfaced approximate in inspection — not rejected. **Test**: scenario test asserts no rejection + the approximate label.
- A temperature *amount* cross-unit case (`price in 'USD/Cel' × quantity in 'Cel'`, or °C↔K amount) compiles and cancels (NOT excluded). **Test**: scenario test asserts clean.
- The skipped `CrossUnit_SameDimension_MustNotSilentlyCancel` test is un-skipped and rewritten to assert *correct* cancellation (factor applied), closing the live hole.
- `business-domain-types.md § D8` / price section documents the extension; `proof-engine.md` documents the side-conditions; `catalog-system.md` documents the new metadata. **Test**: doc-update enumeration satisfied at promote.

## Dependencies

- **Upstream**: D8 (locked); Slice 1 (the `Dimension ← Unit` projection — shipped `b7c17482`); the cross-unit survey (`37971a4b`). The **runtime evaluator** must exist for the conversion to actually execute — until then the compile-time contract + documented obligation stand.
- **Downstream**: enables correct cross-unit line totals in samples; sets the `IsRatioScale` flag that **Slice 4** (absolute positions) will consume; informs the eventual runtime arithmetic for all D8 conversions (this is the first place the runtime conversion obligation is pinned).

## Doc-update enumeration

- `docs/language/business-domain-types.md` § price / § D8 — extend D8's stated scope to price cancellation; state the allow-all-with-surfacing rule (all commensurable conversions admitted; exact/approximate status surfaced in inspection) and the absolute-position exclusion; **clarify `:168` ("Unit conversion is explicit") to mean *visible and traceable to inspection*, not *requires a manual conversion expression* — D8 governs the arithmetic auto-conversion, and the hover/trace inspectability requirement preserves the "explicit" spirit (conversion is explicit to inspection, not erased).**
- `docs/compiler/proof-engine.md` § QualifierChain — the ratio-scale side-condition on the cross-unit discharge (exactness is surfaced in inspection, not gated).
- `docs/language/catalog-system.md` § (UCUM/unit catalog) — the new `ScaleToBaseFactor` (rational) / `ScaleIsRational` / `IsRatioScale` metadata.
- `docs/compiler/diagnostic-system.md` — the broadened PRE0114 wording.
- `docs/tooling/language-server.md` — hover surfaces the conversion.
- `docs/runtime/evaluator.md` — the conversion reduction rule (the runtime obligation), flagged as pending runtime implementation.

## Operational dimensions

- **Observability** (touches evaluation + diagnostics): when a cross-unit cancellation surfaces a diagnostic (cross-dimension or count), the message names the real-world mismatch and the fix (see Audience). When it *succeeds*, the conversion (units + factor) — and its exact/approximate status — is surfaced in hover/proof-attribution so the author can see the `/1000` happened and whether it was exact or approximate — required by the inspectability principle. Runtime conversion, once built, should be traceable in the structured outcome.
- **Evolvability** (depends on UCUM): the `ScaleToBaseFactor` (rational) / `ScaleIsRational` / `IsRatioScale` values are pinned to a specific UCUM version (the catalog's UCUM atom table). Migration story: a UCUM revision that changes a factor or reclassifies a unit's exactness requires a catalog update + a re-run of the conversion scenario tests; the catalog is the single point of change (Decision 4). If UCUM removes/renames an atom Precept catalogs, that surfaces as a catalog-vs-UCUM diff at update time, not a silent drift. Pinning the UCUM version in the catalog source is required.

## Open questions

None blocking. (The temperature/log *increment-spelling* sugar — `delta °C` — is explicitly **deferred** to a future language-surface question, not open for this design: "per kelvin" already expresses per-degree-of-change. Absolute-position math is **Slice 4**, not this design.)
