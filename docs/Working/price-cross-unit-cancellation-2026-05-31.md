# Price × Quantity Cross-Unit Cancellation — Investigation

**Status**: Draft — **decision made 2026-05-31: auto-convert within dimension** (see § Decision below); `/lifecycle-2-design` formalizes the lock. **The hole is currently LIVE** as of the Slice 1 fix: Slice 1 fixed same-unit `price × quantity` cancellation and, by owner decision (option C), left the cross-unit case open — it currently cancels silently, dropping the conversion factor; the auto-convert decision closes it. A skipped test (`PriceTimesQuantityTests.CrossUnit_SameDimension_MustNotSilentlyCancel`) marks the gap in the suite.
**Context**: Phase 7 (total language conformance sweep), Slice 2. Surfaced while doing the Slice 1 spec-understanding pass on `price × quantity → money`. This doc preserves the detailed evidence so the decision can be made later without re-deriving it.
**Scope**: compile-time semantics only — Precept's runtime evaluator is still a stub, so no magnitude arithmetic exists to test against.
**Was the neutral evidence base; the decision is now made.** The 3-option framing below is retained as the record of how it was reached.

---

## Decision & scope correction (2026-05-31)

**Decision: auto-convert within dimension** (the original "Option B" / "Option 2"). When a price denominator unit and a quantity unit are the same dimension but different scale, the language converts (target-directed to the price's denominator unit, per D8's resolution rule) and applies the exact UCUM factor, producing correctly-scaled `money`. Grounded by [`research/language/expressiveness/cross-unit-conversion-arithmetic-survey.md`](../../research/language/expressiveness/cross-unit-conversion-arithmetic-survey.md): every runtime-capable unit system auto-converts; F#'s explicit-conversion requirement is forced by compile-time erasure, not a design choice.

**Scope correction — the exclusion is much narrower than first thought.** The survey's "exclude affine and log units (°C, °F, dB, pH)" conclusion was **too broad**. The real distinction is **amount vs. absolute position**, not linear-vs-affine-vs-log:

- `quantity` is *always an amount* (a magnitude), never a position — the measurement analog of `duration`, not `instant`. Multiplying an operand only makes sense for an amount.
- Converting an *amount* across units uses the **scale factor only**, never the offset: a 20 °C *change* → °F is `× 1.8` (= 36 °F of change), not `× 1.8 + 32`. The offset cancels in any difference. So °C/°F amounts convert and cancel **cleanly**.
- **Log units as amounts — position-rule-fine, but cross-scale is exactness-gated.** A dB *gain* is an amount (not an absolute position), so the position rule does not exclude it, and priced **same-unit** (`$5/dB × 10 dB`, factor 1) it cancels fine. But dB *cross-scale* conversion (dB↔Np) has an **irrational** factor (8.686 = 20/ln 10) — *not* an exact `decimal` — so the **exactness gate rejects it**, like angle→radian. *(An earlier draft here said dB "converts to nepers cleanly and prices consistently" via ÷8.686; the math is consistent but the factor is inexact, so the gate rejects the cross-scale conversion — corrected.)*
- **What the position rule excludes: nothing for `quantity` amounts.** kg, °C-of-change, and dB-gain are all amounts. Cross-scale admissibility is then the *exactness gate's* separate call: °C↔°F (×1.8, exact) admitted; dB↔Np (irrational) rejected. The two gates compose — a cross-unit cancellation is admitted iff (amount/ratio-scale) **and** (exact-`decimal` factor).

**What is genuinely carved out: absolute positions.** An absolute *reading* — a thermostat temperature, an absolute level (`dBm`, relative to a reference), absolute pH — is a *point* relative to a reference/origin: the measurement analog of `instant`. A point can't be multiplied (combining a level with a gain has offset/log structure), so it needs separate offset/reference-aware arithmetic. Precept's `quantity` does not model absolute positions and does not need to for pricing. **Tracked as Phase 7 Slice 4** (a new point-type construct) — out of scope for this cancellation decision.

**Design consequences for `/lifecycle-2-design`:**
- Auto-convert across exact-`decimal`, ratio-of-scale factors for `quantity` amounts of any dimension — °C↔°F amounts (×1.8, exact) included; dB-gain prices same-unit but dB cross-scale (irrational factor) is excluded by the **exactness gate**, not the position rule.
- The catalog must carry, per temperature/log unit, the **amount-conversion scale factor** (distinct from any absolute-conversion function).
- Exactness gate still applies: transcendental / non-terminating / multiply-defined factors are out of scope (state the exact-`decimal` boundary).
- **Teachability, not exclusion:** log/affine units need the level-vs-amount distinction taught (the `instant`-vs-`duration` discipline). The pricing-an-amount math is sound.

---

## The question

When a `price` and a `quantity` are multiplied across **the same dimension but different units**, what should happen?

```precept
field PricePerKg as price in 'USD/kg'
field Weight     as quantity in 'g'
field Cost       as money in 'USD' <- PricePerKg * Weight
```

`USD/kg × g` is dimensionally sound (mass cancels) but carries a **1000× scale factor** (`g → kg`). Three behaviors are possible: reject it, auto-convert it (apply the factor, produce correctly-scaled money), or cancel the dimension and silently drop the factor (a wrong-by-1000× result).

This is distinct from the exact-unit case (`kg × kg`, `each × each`), which is unambiguous and is what Slice 1 fixes. Cross-unit only becomes observable **after** Slice 1, because today every `price × quantity` errors `PRE0114` (quantity-side dimension unresolved) before any cross-unit policy is reached.

---

## Evidence — what the spec binds

The spec has a clear, locked unit model for **quantity** arithmetic, and a stricter rule for **price**, but never states the price × quantity *cross-unit* case explicitly.

### Quantity arithmetic auto-converts within a dimension (D8)

- `quantity ± quantity`, comparisons, `quantity / quantity`: "same dimension required; **auto-converts if commensurable**" — `business-domain-types.md:659, :663, :674`. Example: `'10 kg' / '5 kg' → 2`.
- **D8** decision block (`:445–452, :1733`): commensurable arithmetic converts via "UCUM standard conversion factors"; target-directed (convert to the result field's unit) else left-operand-wins.
- **Count units are the explicit exception** (`:397`): `each`/`box`/`case`/`dozen` share the `count` dimension (`DimensionVector.None`, factor-one) but have **no universal conversion factor**, so cross-count `×`/`÷` is **rejected** with `PRE0137 CrossCountingUnitOperation`. "Conversion between counting units still requires explicit multiplication by a typed business conversion factor."
- **The codebase already does UCUM base-unit normalization** for bounds: interval containment "normalizes both sides to UCUM base units before comparison" — `max '5 kg'` with `set '6 [lb_av]'` is proved safe because `6 lb ≈ 2.72 kg < 5 kg` (`:428`). So a base-unit-normalization mechanism exists somewhere in the proof engine (§5).

### Price is treated more strictly than quantity

- Price **comparison** requires "same currency **AND same denominator unit**" — *exact unit*, not just dimension (`:1866`).
- Price × quantity **cancellation** (`:888, :926`) is described as "dimensional cancellation," with the quantity unit said to "**match**" the denominator. **Only same-unit examples are shown** (`each × each` at `:217`). The cross-unit case is **never stated**.
- "Unit conversion is explicit" appears as a headline property of `quantity` (`:168`).

**The tension:** the price path says "same denominator unit" (exact) and "conversion is explicit," while the quantity path says "auto-convert within dimension." Whether `price × quantity` cancellation follows the price rule or the quantity rule is the unresolved point.

---

## Evidence — what the implementation does

- `Operations.cs:644–653` — `PriceTimesQuantity` discharges a `QualifierChainProofRequirement` on the **Dimension** axis (`QualifierAxis.Dimension` ↔ `QualifierAxis.Dimension`, "Price dimension must match quantity dimension"), with `ResultQualifierPolicy.CompoundUnitCancellation`. It checks **dimension match, not unit**. So `USD/kg × g` would cancel (`mass == mass`) once the quantity side resolves.
- Live probe (2026-05-31, after MCP rebuild) confirms the asymmetry that masks this today:
  `PRE0114: ... 'UnitPrice' [Dimension↔Dimension: 'count'] and 'OrderQty' [Dimension↔Dimension: unresolved]`. The price side resolves its denominator dimension; the quantity side is unresolved → cancellation never reaches the cross-unit question. (This is the Slice 1 bug.)
- **No conversion-factor / scale step found in the cancellation path.** Scoped grep of `ProofEngine.Qualifiers.cs` and `Runtime/Measures/` for `ConversionFactor`/`ConvertTo`/base-unit-normalize/`scale` returned nothing on the price-cancellation path. The cancellation resolves the result **currency** but tracks **no magnitude factor**. (Scoped grep, not exhaustive — the bounds-path normalization at `:428` lives elsewhere and is not obviously wired into cancellation.)
- Runtime magnitude arithmetic is a **stub** — no conversion is applied at runtime either.

**Net:** the impl is in a middle state — it cancels on *dimension* and tracks *no factor*. That is sound only if exact-unit is enforced separately (it is not, on this path). So cross-unit `price × quantity` is a **latent scale-factor hole**, currently masked by the Slice 1 resolution bug.

---

## Interaction with Slice 1 — RESOLVED (hole now live)

Before Slice 1, both the exact-unit case (the spec's headline `each × each`) and the cross-unit case (`kg × g`) failed with the **same** `PRE0114`, because the quantity operand's dimension never resolved. **Slice 1 fixed the quantity-side resolution** (Unit→Dimension projection on the Dimension axis). The dimension-axis check is now live, so `kg × g` and `each × box` **now cancel silently** — exactly the cross-unit policy this investigation is about. The adversarial review of Slice 1 confirmed this empirically (was `PRE0114`, now clean). Because Precept is a pre-release PoC with no external consumers, the owner chose to land the sound same-unit fix and leave the cross-unit hole open (option C), tracked here and by the skipped suite test, rather than pre-decide the policy. **This decision must be made before the hole can close.**

---

## Decision space (parked)

| Option | Leans on | Notes |
|---|---|---|
| **1. Exact-unit-only** for price cancellation — reject `kg × g`; author converts explicitly | price comparison rule `:1866` + "conversion is explicit" `:168` | Simplest, most conservative. Diverges from quantity-arithmetic D8. Aligns with the existing impl *if* a unit-equality check is added on top of the dimension check. |
| **2. Auto-convert within dimension** — apply the UCUM factor, produce correctly-scaled money | quantity-arithmetic D8 + the base-unit normalization already used for bounds `:428` | Uniform with quantity arithmetic. Requires the conversion factor to be actually applied (compile-time normalization and/or the future runtime), not just dimension-cancelled. |
| **3. Split by unit kind** — physical dimensions convert (D8), count units reject (`PRE0137`) | the assembled spec as-is | Most internally consistent with the current text; most moving parts. |

**Philosophy stakes:** the choice pits "exact within UCUM / honesty about approximation" (`:168`) and determinism against uniformity-with-quantity-arithmetic (D8). Option 1 leans on "conversion is explicit"; options 2/3 lean on D8's auto-conversion precedent. This is why it is a genuine semantics decision, not a bug with an obvious answer — the spec supports more than one reading for the price path, and the impl picked "dimension match" without a recorded decision.

---

## Honest gaps in this investigation (to close during the decision pass)

- The "no conversion factor in the cancellation path" claim is from a **scoped** grep, not an exhaustive trace. Confirm by reading the `CompoundUnitCancellation` result-qualifier resolution end-to-end.
- Whether the bounds-path UCUM base-unit normalization (`:428`, proof-engine §5) is **reusable** for cancellation (relevant to Option 2's feasibility) is unconfirmed.
- **No comparable-systems survey done** — deliberately deferred to the decision pass. A `/lifecycle-2-design` on this will need the broader field (how unit libraries — F#/UoM, Frink, boost::units, Haskell `dimensional`, UCUM tooling — handle multiplying a per-kg rate by a gram quantity; most normalize to base units automatically).

## Next step

When we return to decide: run `/lifecycle-2-design` on "price × quantity cross-unit cancellation policy," using this doc as the evidence base, with the three options above as the decision's alternatives and the comparable-systems survey filled in.
