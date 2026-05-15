# Research: Business Units (`each`, `box`, `package`) in Quantity Normalization

**Author:** Frank (Lead Architect)  
**Date:** 2026-05-14  
**Trigger:** Shane's request to analyze how the normalization design handles non-UCUM business/inventory units  
**Predecessor:** frank-7 (external normalization research — APPROVED)

---

## Research Summary

**Verdict: The normalization design is correct by construction, but the investigation exposed one deferred comparison-proof gap.**

Business units (`each`, `box`, `case`, `pallet`, `roll`, etc.) are registered in `UcumAtomCatalog` with `DimensionVector.None` and `UcumExactFactor.One`. This means they parse successfully as valid UCUM atoms, carry a scale factor of 1.0, and share the `count` dimension. The normalization pipeline handles them identically to any other unit with factor 1 — the `Scale(1.0m)` operation is a no-op, so raw magnitudes pass through unchanged. This is architecturally correct for normalization: business units are dimensionless counting units with no universal inter-unit conversion factor (1 case ≠ N each at the language level — that's a product-level property modeled as a field value like `StockingUnitsPerPurchaseUnit`).

However, that same shared `DimensionVector.None` / `count` dimension currently makes the binary-op qualifier proof path too permissive for explicit counting-unit comparisons. A comparison such as `Qty > BoxCount` (`each` vs `box`) can currently pass by same-dimension fallback even though no type-level conversion factor exists. This does not invalidate the normalization slices, but it is a known architectural gap: dimensional compatibility is not value convertibility, and the comparison checker needs a follow-up tightening.

The design's treatment of dynamic business-unit fields (`'{StockingUnit}'`) correctly falls through to `Unbounded` via `TryGetStaticScalingFactor → null`, which is the conservative-safe path. The `positive` constraint on ratio quantities like `StockingUnitsPerPurchaseUnit` is enforced at runtime (evaluator constraint plan), not proved at compile time — this is by-design for dynamic-unit forms.

---

## Part A: External Research Findings

### A1. F# Units of Measure

F# treats custom/counting units identically to SI units at the type level. You declare `[<Measure>] type each` and annotate values as `int<each>`. Key insight: **F# does NOT encode conversion factors in the unit system** — conversion (`1 box = 12 each`) is a value-level relationship, not a type-level one. The type system only prevents mixing `int<each>` with `int<box>` without an explicit conversion expression. This is exactly Precept's approach: `StockingUnitsPerPurchaseUnit` is a field carrying the conversion ratio, not a type-system-level relationship.

### A2. Rust `uom` / `dimensioned`

Both crates model `each`-style units as custom dimensions or dimensionless quantities. `uom` lets you define a custom `count` quantity with unit `each` having factor 1.0. Critically, **cross-unit conversion (box → each) is NOT handled by the unit system** — it's application logic. The crates provide type safety (can't add `each` to `meters`) but no automatic packaging-unit conversion. Precept's approach aligns.

### A3. JSR-385 / Units of Measurement API

JSR-385 treats `each` as an alias for `Units.ONE` (the dimensionless unit). All dimensionless units are mathematically compatible by default. If you need semantic non-convertibility (bag ≠ box), you enforce it in application logic, not the unit API. JSR-385 **does not** distinguish "convertible" from "non-convertible" at the API level — all dimensionless units are compatible. This validates Precept's choice to give all counting units `DimensionVector.None` — they're the same dimension, and inter-unit conversion is a business property.

### A4. ERP/Inventory Systems (SAP)

SAP ERP handles this with a **two-layer architecture**: (1) a global unit system (EA, CS, PL as unit codes) and (2) per-material conversion factors (1 CS = 12 EA for material X, 1 CS = 24 EA for material Y). The conversion is **always product-specific** and lives in master data (table MARM), not in the unit system itself. Inventory is stored in the base UoM; transactions convert using the material-specific factor. Precept's `StockingUnitsPerPurchaseUnit` field is exactly this SAP pattern — conversion as product-level data, not universal unit semantics.

### A5. PLT / Algebraic Dimensional Type Systems

For ratio types like `m/s` where one component is non-normalizable, the standard pattern is **partial normalization**: normalize what you can, leave the opaque component unchanged. For `kg/each`, normalize `kg → g` (factor 1000) but leave `each` untouched (factor 1). The resulting ratio factor is `1000/1 = 1000`. Precept's design handles this correctly by construction: `TryGetStaticScalingFactor` computes the composite factor from the parsed unit expression, which for `kg/each` would be `Scale(kg) / Scale(each) = 1000 / 1 = 1000`. The partial-normalization pattern falls out naturally from the UCUM factor arithmetic.

---

## Part B: Internal Analysis — Scenario Verdicts

| Scenario | Verdict | Explanation |
|----------|---------|-------------|
| **1: Same-unit comparison with `each`** | ✅ CORRECT | `'each'` parses successfully → `UcumParsedUnit` with `Scale = One`. `TryGetStaticScalingFactor` returns `ApplyFactor(1m, One) = 1.0m`. `NumericInterval.Scale(1.0m)` is a no-op. Raw magnitude comparison (`0 ≤ x ≤ 100`) works correctly. |
| **2: Cross-unit comparison (`each` vs `box`)** | ⚠️ KNOWN GAP | The current binary-op qualifier proof path can accept explicit counting-unit comparisons because both qualifiers reduce to `DimensionVector.None` / `count`. That is architecturally wrong: `1 box` is not comparable to `1 each` without a product-level conversion factor. Intended behavior: reject static qualifier mismatches the way direct assignment already does, or return `Unbounded` / not proved when the comparison depends on runtime conversion data. |
| **3: Dynamic business unit with static bounds** | ✅ CORRECT (conservative) | Field qualifier is dynamic (`'{StockingUnit}'`) → `TryGetStaticUnit` for the field value returns null → interval is `Unbounded`. The bounds `'0 each'` and `'100 each'` are stored as `NormalizedDeclaredMin/Max = 0/100` on `TypedField`. The proof engine cannot prove containment statically (which is correct — if `StockingUnit = 'box'` at runtime, `'5 box'` might exceed `'100 each'`). Runtime enforcement handles it. |
| **4: Ratio quantity with two dynamic components** | ✅ CORRECT (conservative) | Both numerator/denominator are dynamic → `TryGetStaticUnit` returns null → `Unbounded`. The `positive` constraint is still enforced at **runtime** via the Builder's constraint plan (which reads `NormalizedDeclaredMin = 0+ε` from `TypedField` and emits a `LoadLit` + `Compare` opcode sequence). Compile-time proof is not attempted for dynamic-unit forms — this is by design per §5.4's "Explicitly safe exclusion." |
| **5: Price with one physical and one business unit** | ✅ CORRECT | For `AverageCost as price in '{CatalogCurrency}' of '{StockingUnit.dimension}'`: the qualifier is dynamic → interval proof defers to runtime. If `StockingUnit = 'each'`, dimension = `count` (DimensionVector.None). If `StockingUnit = 'roll'` (and roll is a length alias), dimension = `length`. The type checker's runtime dimension-compatibility check handles this — normalization is not involved for dynamic qualifiers. |
| **6: `each.dimension` concept** | ✅ CORRECT | `each` has `DimensionVector.None`, which maps to the `"count"` dimension alias in `DimensionCatalog`. So `StockingUnit.dimension` when `StockingUnit = 'each'` resolves to `count`. This is an opaque dimensionless identifier. It does NOT participate in UCUM normalization (factor = 1). It provides dimensional type safety: a `quantity of 'count'` cannot be assigned a `quantity of 'length'`. |
| **7: Compile-time normalization correctness** | ✅ CORRECT | For static `each` bounds: `TryGetStaticScalingFactor` returns 1.0 → `NormalizedDeclaredMin/Max` stores the same value as `DeclaredMin/Max`. No wrong result. For dynamic `'{StockingUnit}'` expressions: returns null → no normalization attempted → `Unbounded`. No wrong result. The "store both" pattern stores identical values when factor = 1 (minor redundancy, not a bug). |

---

## Part C: Recommendations

### HIGH Priority

**None.** The design handles business units correctly by construction.

### MEDIUM Priority

| # | Item | Action |
|---|------|--------|
| M1 | **Explicit documentation of business-unit normalization behavior** | Add a paragraph to §6 (Risks and Tradeoffs) or §7 (Q&A) explaining: business units (each, box, case, etc.) are registered as UCUM atoms with `DimensionVector.None` and `UcumExactFactor.One`. They parse successfully, normalize with a no-op factor (1.0), and share the `count` dimension. This is by design — cross-unit conversion (1 case = N each) is a product-level property, not a universal unit relationship. |
| M2 | **Document the `NormalizedDeclaredMin/Max` value for business-unit fields** | Clarify that for `field Qty as quantity in 'each' max '100 each'`, `NormalizedDeclaredMax = 100` (same as `DeclaredMax`). The "store both" pattern stores duplicate values for factor-1 units. This is correct and expected — no special case needed. |
| M3 | **Document that `dozen`/`gross` intentionally do NOT encode 12/144** | The atom catalog registers `dozen` and `gross` with `UcumExactFactor.One`, not 12 or 144. This is deliberate: in inventory systems, "1 dozen" is not universally 12 of the base unit — it depends on what you're counting. The conversion is a product-level field value, matching the SAP/ERP pattern. This decision should be stated explicitly. |
| M4 | **Tighten static counting-unit comparison proof** | Binary-op qualifier proof currently accepts `each` vs `box` via the shared `count` dimension. Follow-up fix: require matching static qualifiers for explicit counting-unit comparisons, or return `Unbounded` when a runtime conversion field is needed. Document this as a known deferred gap in the design doc. |

### LOW Priority

| # | Item | Action |
|---|------|--------|
| L1 | **Consider documenting `count` dimension alias usage** | The `DimensionCatalog` entry `("count", DimensionVector.None)` is the dimension alias for all business units. Document that `of 'count'` and `of '{StockingUnit.dimension}'` (when `StockingUnit = each/box/etc.`) resolve to the same **dimension alias**. This means dimension-only declarations and proofs stay in the same dimensional family; it does **not** define inter-unit conversion or semantic equivalence between explicit counting units (`1 box` is not `1 each`, and `1 box = N each` remains product-level data). Avoid describing this as compatibility between different counting units, because that reads as value convertibility. |

---

## Implementation Impact

**The current normalization slices do not need reordering or redesign.** The normalization pipeline still handles business units correctly through existing mechanisms:

1. **Slice 14** (`TypedConstantNormalizer`): `Normalize(100m, ucumParsedUnit_each)` returns `100m` because `Scale = One`. No code change needed.
2. **Slice 15** (`TypedField` bounds): `NormalizedDeclaredMax` stores the same value as `DeclaredMax` for factor-1 units. No code change needed.
3. **Slice 16** (`TryGetStaticScalingFactor`): Returns `1.0m` for static `each` expressions, `null` for dynamic `'{StockingUnit}'`. Both paths are correct.
4. **Slices 19–21** (Interpolated): Dynamic unit holes → `Unbounded`. Static `each` in interpolated form → factor 1.0 no-op. Both correct.

**Separate follow-up required:**
- Tighten binary-op qualifier proof for explicit counting-unit comparisons (`each` vs `box`). This is outside the current normalization slices; it belongs as a dedicated TypeChecker / ProofEngine follow-up.

**Documentation changes now:**
- Add a new known-gap subsection to the design doc covering M1–M4 above.
- No slice renumbering is required; the comparison gap is tracked as deferred follow-up work rather than a blocker for the current implementation plan.

---

## Key Architectural Insight

The reason business units "just work" is a consequence of a good architectural decision made in `UcumAtomCatalog`: registering `each`, `box`, `case`, etc. as first-class atoms with `Scale = One` rather than treating them as "unknown/unparseable" strings. This means:

- They parse → `UcumParsedUnit` is non-null → `TryGetStaticScalingFactor` returns a value (1.0) rather than null
- Static bounds in business units (`max '100 each'`) get real `NormalizedDeclaredMin/Max` values → proof engine can prove containment
- Only **dynamic** business-unit references (`'{StockingUnit}'`) fall to Unbounded — which is correct because you can't know the unit identity at compile time

If business units were NOT in the atom catalog, `TryGetStaticScalingFactor('5 each')` would fail to parse and return null, causing `NormalizedDeclaredMax` to be null and the proof engine to report false-positive `PRE0078` on every `each`-typed field. The atom catalog registration is what makes the normalization pipeline work for business units without special-case code.
