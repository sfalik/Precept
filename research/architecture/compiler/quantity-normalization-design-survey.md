# External Research: Quantity Normalization Design Evaluation

**Date:** 2026-05-14T21:25:18-04:00  
**Author:** Frank (Lead/Architect)  
**Status:** Research complete — findings for team review  
**Scope:** `docs/Working/quantity-normalization-design.md`

---

## Research Summary

The quantity normalization design is **well-grounded and architecturally sound**. The "normalize to base units at compile time, store both forms, compute in normalized form, display in declared form" pattern aligns with established practice across F#, Rust/uom, JSR-385, and production FHIR/UCUM systems. The choice of `decimal` over `double` is correct for a business-domain integrity engine. No fundamental design flaws were found. Two medium-priority improvements are recommended.

---

## Per-Question Findings

### Q1: Compile-time unit checking — how do other languages do it?

**Key findings:**

| System | Approach | Runtime unit info? | Normalization |
|--------|----------|-------------------|---------------|
| F# Units of Measure | Type-level phantom types; units erased at runtime | No (type erasure) | Dimensional algebra at compile time; no base-unit normalization of magnitudes |
| Haskell `dimensional` | Phantom type-level dimension vectors (DataKinds) | No | Same as F# — type arithmetic, zero runtime cost |
| Rust `uom` | Type-level exponent vectors via generics + macros | No | Converts to SI base unit internally; zero-cost abstraction |
| JSR-385 (Java) | Runtime generic `Quantity<Length>` objects | Yes (immutable value objects) | `unit.getSystemUnit()` normalizes to base; values carry unit at runtime |
| SPARK Ada | Distinct derived numeric types per dimension | No (type system) | Type checker prevents mismatched operations |

**Implications for our design:** Precept's approach — "normalize magnitude to base unit, carry unit label separately" — is closest to JSR-385/FHIR's runtime pattern and Rust/uom's internal storage model. This is the **correct** choice for a DSL that must:
1. Compare quantities with different declared units (interval arithmetic)
2. Store values persistently (PreceptValue)
3. Display values to humans in their authored unit

The F#/Haskell "erase units entirely" approach won't work because Precept needs runtime unit identity for display and ingress validation. **No change needed.**

**References:**
- Don Syme, "The F# Units of Measure Design" (Microsoft Research, 2008)
- Rust `uom` crate: https://docs.rs/uom
- JSR-385: https://jcp.org/en/jsr/detail?id=385
- Haskell `dimensional`: https://hackage.haskell.org/package/dimensional

---

### Q2: Interval arithmetic with quantities — known approaches and pitfalls

**Key findings:**

1. **`decimal` is correct for Precept's domain.** `decimal` (128-bit, base-10, 28-29 significant digits) avoids the classic IEEE 754 binary floating-point representation errors that plague `double` for exact decimal fractions. Since UCUM conversion factors are defined as exact decimal ratios (e.g., `[lb_av]` = exactly 0.45359237 kg), `decimal` multiplication preserves exactness.

2. **Interval arithmetic over decimal is safe when conversion factors are exact.** The UCUM system defines most mass/length/volume conversions as exact decimal values. Multiplication of `decimal` by an exact conversion factor introduces no rounding for factors within 28 digits.

3. **Potential pitfall: compound unit chains.** If a future unit requires chained conversions (A→B→C), each multiplication could accumulate rounding in the least-significant digits. For Precept's current scope (single-factor UCUM conversions), this is not a risk.

4. **No need for interval widening.** Some scientific interval arithmetic implementations widen bounds by an epsilon to account for rounding. This is necessary for `double` but NOT for `decimal` with exact conversion factors.

**Implications:** The design's use of `decimal` throughout is **confirmed correct**. No interval-widening or precision-loss mitigation is needed for the current scope. **No change needed**, but document the assumption that conversion factors are exact decimals (they are, per UCUM spec).

**References:**
- IEEE 754-2019 §3.3 (decimal floating-point)
- Microsoft .NET `decimal` documentation (28-29 significant digits, no binary representation error)
- UCUM specification: all base-unit conversion factors are defined as exact rational numbers

---

### Q3: UCUM in software systems — lessons learned

**Key findings:**

1. **`[lb_av]` → kg is exact:** 1 [lb_av] = 0.45359237 kg exactly (by international definition). No precision concern with this specific conversion.

2. **Known edge cases in UCUM:**
   - Nonlinear units (Celsius, decibels) cannot be handled by simple multiplication — require affine transforms
   - Ambiguous parsing: `m` (meter) vs potential custom codes
   - Composite units like `mm[Hg]` need correct bracket parsing
   - SI prefix stacking

3. **Production usage (HL7 FHIR):** FHIR's quantity comparison uses exactly the pattern Precept implements — normalize both quantities to canonical UCUM base units via the UCUM service, then compare numeric values. This is the **standard approach** in healthcare interoperability.

4. **.NET UCUM libraries:** Firely's Fhir .NET API includes a UCUM service. Worth evaluating if Precept ever needs broader UCUM coverage beyond the catalog's built-in conversions.

**Implications:** The design aligns with FHIR production practice. **Minor addition recommended:** Document that nonlinear units (°C, dB, pH) are out of scope for the current `TryGetStaticScalingFactor` approach (they require affine transforms, not simple multiplication). This is already implicitly true but should be explicit in the design doc.

**References:**
- UCUM specification: https://ucum.org/ucum.html
- HL7 FHIR Quantity comparison: https://www.hl7.org/fhir/datatypes.html#Quantity
- Firely .NET UCUM service: https://github.com/FirelyTeam/Fhir-net-api

---

### Q4: DSL design for quantity constraints — prior art

**Key findings:**

1. **Modelica's `unit`/`displayUnit` split** is directly analogous to Precept's design:
   - `unit` = the normalized SI unit used for all computation
   - `displayUnit` = the human-facing unit for authoring and display
   - Assertions validate in normalized form; display shows declared form

2. **SPARK Ada** separates dimensional correctness (type system, compile-time) from value-range correctness (proof obligations via GNATprove). Precept's ProofEngine obligations for interval containment are analogous to SPARK's value-range proof obligations.

3. **The "author in natural units, validate in normalized units" pattern is well-established** across Modelica, SPARK Ada, and FHIR. Precept is not inventing a new pattern — it's applying a proven one.

**Implications:** **No change needed.** The design doc could optionally reference Modelica's `unit`/`displayUnit` as prior art for the "store both" / "display in declared, compute in normalized" pattern.

---

### Q5: Diagnostic display — any established conventions?

**Key findings:**

1. **Modelica/OpenModelica diagnostic format:**
   ```
   [UnitCheck] Error: Type mismatch in assignment:
     left type: Real (unit = "kg.m2/s2")
     right type: Real (unit = "N.m")
   ```
   Shows expected vs. actual units clearly.

2. **Our format** `[−∞ .. 5 kg] (computed: [6 [lb_av] .. 6 [lb_av]])` is **more informative** than typical Modelica diagnostics because it shows both the constraint interval AND the computed value interval in their respective natural units. This is superior for a business-domain audience.

3. **No standard "interval display" convention exists** across tools — each tool uses its own format. Our bracketed interval notation with parenthetical computed values is clear and self-documenting.

**Implications:** **No change needed.** The diagnostic format is good — arguably better than industry standard.

---

### Q6: The "store both" pattern — is it standard or unusual?

**Key findings:**

1. **The pattern is well-recognized** in software architecture under names like "dual storage," "canonical + original," or "materialized view + source of truth." It's standard in:
   - Data warehousing (raw events + denormalized tables)
   - Search systems (source documents + indexed forms)
   - FHIR (stored quantity + canonical comparison value)
   - Event sourcing (commands + projections)

2. **Known risks and mitigations:**
   - **Staleness:** Normalized form diverges from original after updates → Mitigate with atomic computation (compute normalized at write time, never independently)
   - **Consistency:** Two values claiming to represent the same thing → Mitigate by making normalized form a computed derivation, not independently settable

3. **Precept's implementation is safe** because:
   - Normalized values are computed once at compile time (or ingress) from the declared values
   - They are never independently mutated
   - The Builder is the single computation boundary
   - `NormalizedDeclaredMin ?? DeclaredMin` fallback handles the null case

**Implications:** **No change needed.** The design correctly implements this as a computed derivation rather than independent state. The null-fallback pattern (`NormalizedDeclaredMin ?? DeclaredMin`) is a good safety net. One minor addition: document the invariant that normalized values are NEVER set independently of declared values — they are always computed derivations.

**References:**
- Martin Fowler, "Canonical Data Model" pattern
- Event Sourcing pattern (Fowler, 2005)
- Materialized View pattern (Microsoft Cloud Design Patterns)

---

## Recommended Design Changes

### Priority: MEDIUM

1. **Explicitly document the nonlinear-unit exclusion.**
   - **What:** Add a note to the design doc that `TryGetStaticScalingFactor` only handles linear unit conversions (multiplication by a constant factor). Nonlinear units (°C, dB, pH, log-scale units) are explicitly out of scope and would require affine or nonlinear transform support.
   - **Why:** UCUM includes nonlinear units. If the catalog ever adds temperature or logarithmic units, someone might assume the normalization pipeline handles them. An explicit exclusion prevents that mistake.
   - **Priority:** MEDIUM — no current catalog units are nonlinear, but the exclusion should be documented before it becomes a trap.

2. **Document the "exact conversion factor" assumption.**
   - **What:** Add a brief note that the interval arithmetic correctness guarantee depends on UCUM conversion factors being exact decimal rationals (which they are for all currently-cataloged units). If a future unit has an irrational or transcendental conversion factor, precision analysis would be needed.
   - **Why:** The proof that "no interval widening is needed" rests on decimal * exact_decimal = exact_decimal. This assumption is true but implicit.
   - **Priority:** MEDIUM — defensive documentation.

### Priority: LOW

3. **Optional: Reference Modelica's `unit`/`displayUnit` as prior art.**
   - **What:** Add a brief note in the design rationale section referencing Modelica's established pattern as validation that "store in normalized, display in declared" is a proven approach.
   - **Why:** Strengthens the design justification for reviewers unfamiliar with the pattern.
   - **Priority:** LOW — nice to have for credibility.

---

## Confirmed Sound

The following design decisions are **confirmed correct by external research:**

1. ✅ **Normalize to base units as bare `decimal`** — matches Rust/uom internal model, JSR-385 `getSystemUnit()`, and FHIR canonical comparison
2. ✅ **Use `decimal` not `double`** — correct for business-domain quantities with exact conversion factors; avoids binary floating-point representation errors
3. ✅ **"Store both" declared + normalized bounds** — well-established pattern (dual storage / materialized view); safe when normalized is a computed derivation
4. ✅ **UCUM as the unit system** — industry standard in healthcare (HL7 FHIR), exact conversion factors for physical units
5. ✅ **Diagnostic de-normalization** — showing constraints in declared units while computing in normalized units matches Modelica's `displayUnit` pattern
6. ✅ **Expression-type-dispatched scaling** — the `TryGetStaticScalingFactor` dispatch pattern prevents the "false proof" problem that would arise from blindly scaling dynamic-unit expressions
7. ✅ **`[lb_av]` = 0.45359237 kg is exact** — no precision concern with this specific conversion factor
8. ✅ **Interval containment without epsilon widening** — correct for `decimal` arithmetic with exact factors (would NOT be correct with `double`)

---

## HIGH-Priority Gaps Found

**None.** The design is well-grounded. No HIGH-priority changes are needed before implementation.
