# Proof Engine × Qualifier Audit

**Author:** Frank  
**Date:** 2026-05-11  
**Scope:** Exhaustive audit of all qualifier-proof interactions across the Precept proof engine, type checker, and operations catalog

## Executive Summary

This audit examines every interaction between qualifier declarations and the proof engine. The finding is severe: **the currency axis has near-total enforcement failure for arithmetic and comparison operations.** Money addition, subtraction, and all six comparison operators declare "same currency required" in their catalog descriptions but carry NO `QualifierCompatibilityProofRequirement` and NO other enforcement mechanism. This means `money in 'USD' + money in 'EUR'` compiles clean — a direct violation of the "invalid configurations structurally impossible" guarantee from `docs/philosophy.md`.

In contrast, the **quantity unit axis** and **price compound axis** are correctly enforced via `QualifierCompatibilityProofRequirement` on all arithmetic and comparison operations. The **temporal dimension axis** is correctly enforced via `DimensionProofRequirement` (Strategy 2). The **numeric modifier subsumption** system (positive ⊇ nonzero, nonnegative for >=0) works correctly with comprehensive test coverage.

The audit identifies **7 critical gaps**, **4 significant gaps**, and **3 minor gaps**. The most urgent: money currency enforcement requires adding `QualifierCompatibilityProofRequirement` to 8 operation catalog entries — a surgical fix of ~16 LOC in `Operations.cs`.

## Audit Matrix

### Dimension Qualifiers (`quantity of 'X'`, `price per 'X'`)

| Scenario | Status | Evidence |
|----------|--------|----------|
| `quantity of 'mass' + quantity of 'mass'` (same dimension, no explicit unit) | ⚠️ False Positive | Operations.cs L496: requires `QualifierAxis.Unit`. Fields with only `DeclaredQualifierMeta.Dimension` have no `QualifierAxis.Unit` entry. `ResolveQualifierOnAxis` (ProofEngine.cs L898-911) returns null → obligation UNRESOLVED → spurious diagnostic. |
| `quantity in 'kg' + quantity in 'kg'` (same unit) | ✅ Proved | Operations.cs L496: `QualifierCompatibilityProofRequirement` on `QualifierAxis.Unit`. Both fields have `DeclaredQualifierMeta.Unit("kg", "Mass")`. Strategy 5 (L876-896) resolves both, compares via record equality → proved. |
| `quantity in 'kg' + quantity in 'lb_av'` (different unit, same dimension) | ✅ Caught | Same requirement fires; `Unit("kg", "Mass") != Unit("lb_av", "Mass")` → UNRESOLVED → `UnprovedQualifierCompatibility` diagnostic. |
| `quantity in 'kg' + quantity in 'm'` (different dimension) | ✅ Caught | Same mechanism: `Unit("kg", "Mass") != Unit("m", "Length")` → caught. |
| Cross-field assignment `set f2 = f1` (different dimensions) | ✅ Caught | TypeChecker.Expressions.cs L1281-1304: `ValidateAssignmentQualifiers` checks `DimensionCategoryMismatch` for `DeclaredQualifierMeta.Dimension` source vs target. |
| Cross-field assignment via expression `set f2 = f1 + f3` | ⚠️ Silent Gap | TypeChecker.Expressions.cs L1262-1274: `ValidateAssignmentQualifiers` only extracts qualifiers from `TypedFieldRef`, `TypedArgRef`, or `TypedTypedConstant`. Binary expression results carry no qualifier provenance → no check performed on the assignment target. |
| Static typed constant `field f as quantity of 'mass' default '1 [ft_i]'` | ✅ Caught | QuantityValidator.cs L30-53: `UnitDimensionHelper.DeriveUnitDimensionName` extracts dimension from literal unit, compares to `DeclaredQualifiers` dimension → `DimensionCategoryMismatch` on mismatch. |
| Interpolated typed constant `field f as quantity of 'mass' default '1 {g.unit}'` where g is `quantity of 'length'` | ⚠️ Silent Gap | Known gap from earlier analysis. `TypedMemberAccess` for `.unit` resolves to bare `TypeKind.UnitOfMeasure` with no dimension provenance. Documented in interpolation plan Slice 2 extension (decision frank-dimension-proof-propagation). |
| Guard `when f1 > '0 kg'` where f1 is `quantity of 'length'` | ✅ Caught | The typed constant `'0 kg'` is validated by QuantityValidator at type-check time. If f1 is `quantity of 'length'`, the comparison operation would require `QualifierAxis.Unit` matching, and the literal's resolved qualifiers would mismatch → diagnostic. |
| Rule `rule f1 > '0 kg' because "msg"` where f1 is `quantity of 'length'` | ✅ Caught | Same mechanism as guards — operation catalog requirements fire at rule condition resolution. |
| `price in 'USD' per 'kg' + price in 'USD' per 'kg'` | ✅ Proved | Operations.cs L576-582: BOTH `QualifierAxis.Unit` AND `QualifierAxis.Currency` requirements → Strategy 5 discharges both. |
| `price in 'USD' per 'kg' + price in 'EUR' per 'kg'` | ✅ Caught | Currency axis mismatch → obligation unresolved. |
| `price in 'USD' per 'kg' + price in 'USD' per 'lb_av'` | ✅ Caught | Unit axis mismatch → obligation unresolved. |

### Currency Qualifiers (`money in 'USD'`)

| Scenario | Status | Evidence |
|----------|--------|----------|
| `money in 'USD' + money in 'USD'` | ⚠️ **Silent Gap** | Operations.cs L422-424: `MoneyPlusMoney` has NO `ProofRequirements` array, NO `Match` setting. No enforcement mechanism exists. |
| `money in 'USD' + money in 'EUR'` | ⚠️ **CRITICAL Silent Gap** | Same as above. This compiles clean. Philosophy violation: invalid configuration is NOT structurally impossible. |
| `money in 'USD' - money in 'EUR'` | ⚠️ **CRITICAL Silent Gap** | Operations.cs L426-428: `MoneyMinusMoney` — same issue as addition. |
| `money in 'USD' == money in 'EUR'` | ⚠️ **CRITICAL Silent Gap** | Operations.cs L914-917: `MoneyEqualsMoney` has `Match: QualifierMatch.Same` but NO `ProofRequirements`. `Match` is a disambiguation selector for multi-candidate lookup — it has NO proof enforcement. Since money comparisons have only one candidate per operator, `Match` does nothing. |
| `money in 'USD' > money in 'EUR'` | ⚠️ **CRITICAL Silent Gap** | Same analysis for all 6 comparison operators (L914-937). |
| `set usdField = eurField` (direct assignment) | ✅ Caught | TypeChecker.Expressions.cs L1328-1346: `ValidateAssignmentQualifiers` compares `DeclaredQualifierMeta.Currency` codes → `QualifierMismatch` diagnostic. |
| `set usdField = usdField + eurField` (expression assignment) | ⚠️ Silent Gap | Binary expression `usdField + eurField` produces `TypeKind.Money` with no qualifier. Assignment check sees no source qualifiers → passes silently. |
| `money in 'USD' / money in 'USD'` (same currency ratio) | ✅ Proved | Operations.cs L443-451: `MoneyDivideMoneySameCurrency` has `Match: QualifierMatch.Same`. Since there are TWO candidates (Same/Different), the type checker disambiguates → selects Same variant → compiles as decimal ratio. No proof requirement because the type checker's disambiguation IS the enforcement. |
| `money in 'USD' / money in 'EUR'` (cross currency) | ✅ Caught | Type checker selects `MoneyDivideMoneyCrossCurrency` (`Match: QualifierMatch.Different`) → result is `exchangerate`. The disambiguator picks the correct entry. |
| Static `field f as money in 'USD' default '100.00 EUR'` | ✅ Caught | TypeChecker.Expressions.cs L1266-1270: `TypedTypedConstant` for money extracts `CurrencyEntry` → builds `DeclaredQualifierMeta.Currency` → `ValidateAssignmentQualifiers` catches mismatch. |

### Exchange Rate Qualifiers (`exchangerate from 'USD' to 'EUR'`)

| Scenario | Status | Evidence |
|----------|--------|----------|
| `exchangerate from 'USD' to 'EUR' * money in 'USD'` | ⚠️ **CRITICAL Silent Gap** | Operations.cs L621-623: `ExchangeRateTimesMoney` has NO `ProofRequirements`. No validation that the rate's `from` currency matches the money's currency. `exchangerate from 'USD' to 'EUR' * money in 'GBP'` compiles clean. |
| `exchangerate from 'USD' to 'EUR' * money in 'EUR'` | ⚠️ **CRITICAL Silent Gap** | Same issue — the money's currency should match the rate's `from` (USD), not `to` (EUR). No enforcement exists. |
| `set rateField = rate1` where qualifiers differ | ✅ Caught | `ValidateAssignmentQualifiers` would catch `FromCurrency`/`ToCurrency` mismatches IF the source is a field ref. However, this needs verification — the current implementation (L1277-1349) only handles `Dimension`, `Unit`, and `Currency` cases. `FromCurrency` and `ToCurrency` are NOT in the switch statement. |
| Exchange rate comparison (==, !=) | ❓ Unclear | No exchange rate comparison operations found in Operations.cs. Exchange rates may not support direct comparison — needs verification against language spec. |

### Numeric Modifiers (`nonzero`, `nonnegative`, `positive`)

| Scenario | Status | Evidence |
|----------|--------|----------|
| `a / b` where b has no modifier | ✅ Caught | Operations.cs division entries all carry `NumericProofRequirement(PDecimal/PMoney/etc, NotEquals, 0m)`. Obligation generated, no strategy can discharge → `DivisionByZero` diagnostic. |
| `a / b` where b is `nonzero` | ✅ Proved | Modifiers.cs L101-109: `nonzero` has `ProofSatisfaction.Numeric(SelfValue, NotEquals, 0m)`. Strategy 2 (ProofEngine.cs L430-440) walks field modifiers → `SatisfactionCovers` matches (L584: NotEquals/NotEquals at same threshold) → proved. |
| `a / b` where b is `positive` | ✅ Proved (Subsumption) | Modifiers.cs L86-94: `positive` has `ProofSatisfaction.Numeric(SelfValue, GreaterThan, 0m)`. `SatisfactionCovers` L571-572: `(GreaterThan, NotEquals) when bound==0 && threshold==0 → true`. Positive subsumes nonzero. |
| `a / b` where b is `nonnegative` | ✅ Caught (correct behavior) | Modifiers.cs L70-78: `nonnegative` has `ProofSatisfaction.Numeric(SelfValue, GreaterThanOrEqual, 0m)`. `SatisfactionCovers`: no rule maps `(GreaterThanOrEqual, NotEquals)` → NOT proved. `nonnegative` does NOT subsume `nonzero` because zero is allowed. Test: ProofEngineTests.cs L591-609. |
| `sqrt(x)` where x is `nonnegative` | ✅ Proved | Sqrt requires `>= 0`. `SatisfactionCovers` L579-580: `(GreaterThanOrEqual, GreaterThanOrEqual) when bound >= threshold` → proved. Test: ProofEngineTests.cs L613. |
| `sqrt(x)` where x is `positive` | ✅ Proved (Subsumption) | `(GreaterThan, GreaterThanOrEqual) when bound >= threshold` (L573-574) → proved. Test: ProofEngineTests.cs L633. |
| Guard `when b > 0` then `a / b` | ✅ Proved | Strategy 3 (ProofEngine.cs L608-636): `ExtractGuardConstraints` extracts field/op/literal → `GuardSubsumes` L725-726: `(GT, NE) when guard==0 && threshold==0` → proved. |
| Guard `when b >= 0` then `a / b` | ✅ Caught (correct) | `GuardSubsumes`: no rule for `(GTE, NE)` → not proved. Correct: `>= 0` allows zero. |
| Implied modifier from type (e.g., currency implying notempty) | ✅ Proved | ProofEngine.cs L430: `attributeField.Modifiers.Concat(attributeField.ImpliedModifiers)` — both declared and implied modifiers are walked. |
| `-a` where a is `nonnegative` | ❓ Unclear | Negation result tracking is out of scope for the current proof engine. The engine proves obligations at USE sites, not propagation through expressions. The nonnegative modifier stays on the field declaration — the negated expression is a new value without modifier attribution. This is architecturally correct — no gap. |
| `a + b` where both are `nonnegative` — is result nonnegative? | ❓ Unclear (by design) | The proof engine does NOT propagate modifiers through expressions. It proves obligations from field declarations. If `set c = a + b` and `c` is declared `nonnegative`, there's no mechanism to prove `c`'s modifier is satisfied by the addition of two nonnegative values. This is a limitation, not a bug — no proof strategy exists for arithmetic result modifier inference. |
| `ensure a > 0` where a is `positive` | ❓ Unclear | Initial-state satisfiability (S11, ProofEngine.cs L1132-1186) does constant folding. But ensure obligations outside initial state are NOT actively proven by the proof engine — they are runtime constraints. The proof engine does NOT discharge ensure conditions against field modifiers. This is architecturally intentional — ensures are RUNTIME assertions. |

### Compound Qualifiers (`price in 'USD' per 'kg'`)

| Scenario | Status | Evidence |
|----------|--------|----------|
| `price + price` with matching qualifiers on both axes | ✅ Proved | Operations.cs L576-582: TWO `QualifierCompatibilityProofRequirement` entries — one for `QualifierAxis.Unit`, one for `QualifierAxis.Currency`. Strategy 5 discharges each independently. |
| `price + price` mismatched on one axis only | ✅ Caught | Each axis is checked independently. Mismatch on either → unresolved obligation → diagnostic. |
| `price * quantity` — dimension chain validation | ⚠️ **Silent Gap** | Operations.cs L595-597: `PriceTimesQuantity` has NO `ProofRequirements`. `price in 'USD' per 'kg' * quantity of 'length'` compiles clean. The per-unit dimension of the price should match the quantity's dimension, but no validation exists. |
| `price * period` — temporal chain validation | ⚠️ Silent Gap | Operations.cs L599-601: `PriceTimesPeriod` has NO `ProofRequirements`. `price in 'USD' per 'month' * period of 'time'` — no dimension compatibility check. |
| `price * decimal` scaling | ✅ Correct | No qualifier interaction needed — scaling preserves qualifiers by identity. |

### String Escape Hatch

| Scenario | Status | Evidence |
|----------|--------|----------|
| Field with no qualifier declared (bare `money`, `quantity`) | ✅ Correct | `DeclaredQualifiers` is `IsDefaultOrEmpty`. `ResolveQualifierOnAxis` returns null → Strategy 5 returns false → obligation stays unresolved. The obligation is only generated by operations between TWO qualified fields. If either field is unqualified, the obligation fires but cannot be discharged — correct behavior: you MUST declare qualifiers to prove compatibility. |
| Unqualified field in qualifier slot | ✅ Correct | No false obligation generated. Operations generate obligations with `ParamSubject` pointing to the operation's parameter metadata, not the field's qualifier. The subject resolution (ProofEngine.cs L254-272) finds the field, then `ResolveQualifierOnAxis` (L898-911) looks up its qualifiers. If empty → returns null → strategy 5 fails → unresolved. This is correct: the proof engine says "I can't prove these are compatible because one side has no declared qualifier." |

### Temporal Qualifiers (`period of 'date'`, `period in 'months'`)

| Scenario | Status | Evidence |
|----------|--------|----------|
| `date + period` where period is `period of 'date'` | ✅ Proved | Operations.cs L265-271: `DimensionProofRequirement(PPeriod, PeriodDimension.Date)`. Strategy 2 (ProofEngine.cs L381-386): `ResolvePeriodDimension` extracts `TemporalDimension.Value` from field → matches `Date` → proved. |
| `date + period` where period is `period of 'time'` | ✅ Caught | `ResolvePeriodDimension` returns `Time` ≠ `Date` → unresolved → `UnprovedDimensionRequirement` diagnostic. |
| `time + period` where period is `period of 'time'` | ✅ Proved | Operations.cs L293-298: requires `PeriodDimension.Time` → matches. |
| `period of 'date'` — `PeriodDimension.Any` in compatibility | ✅ Caught | ProofEngine.cs L888-893: Explicit check rejects `Any` in qualifier compatibility. Locked decision. |
| `period in 'months'` dimension inference | ✅ Proved | `DeclaredQualifierMeta.TemporalUnit` carries `DerivedDimension`. `ResolvePeriodDimension` (L529-530) reads `tu.DerivedDimension` → works. |

## Gap Inventory

| ID | Category | Scenario | Status | Fix Location | Est. LOC |
|----|----------|----------|--------|--------------|----------|
| G1 | Currency | `money + money` — no currency enforcement | ⚠️ CRITICAL Silent Gap | Operations.cs catalog | 4 |
| G2 | Currency | `money - money` — no currency enforcement | ⚠️ CRITICAL Silent Gap | Operations.cs catalog | 4 |
| G3 | Currency | `money == != < > <= >= money` — no currency enforcement | ⚠️ CRITICAL Silent Gap | Operations.cs catalog | 12 |
| G4 | Currency Chain | `exchangerate * money` — no from-currency validation | ⚠️ CRITICAL Silent Gap | New proof requirement type or catalog entry | 30-50 |
| G5 | Dimension Chain | `price * quantity` — no dimension chain validation | ⚠️ CRITICAL Silent Gap | New proof requirement type or catalog entry | 30-50 |
| G6 | Dimension | `quantity of 'mass' + quantity of 'mass'` — false positive (Unit axis lookup fails on Dimension-only fields) | ⚠️ False Positive | ProofEngine.cs Strategy 5 — axis fallback logic | 15-25 |
| G7 | Assignment | Expression results carry no qualifier provenance for assignment checks | ⚠️ Silent Gap | TypeChecker.Expressions.cs or new proof path | 40-60 |
| G8 | Dimension Chain | `price * period` — no temporal dimension chain validation | ⚠️ Silent Gap | Operations.cs catalog | 4-8 |
| G9 | ExchangeRate | `ValidateAssignmentQualifiers` missing `FromCurrency`/`ToCurrency` cases | ⚠️ Silent Gap | TypeChecker.Expressions.cs L1277-1349 | 20 |
| G10 | Interpolation | `.unit` accessor drops dimension provenance (known, tracked in interpolation plan) | ⚠️ Silent Gap | Interpolation plan Slice 2 extension | 25 |
| G11 | Modifier Propagation | No proof that `nonneg + nonneg = nonneg` or `pos * pos = pos` | ❓ By Design | Would require new strategy (S6+) | 80-120 |
| G12 | Price Dimension | `price of 'mass'` (dimension without unit) — same false-positive risk as G6 | ⚠️ False Positive | Same fix as G6 | 0 (subsumed) |
| G13 | Temporal Chain | `price * duration` — no temporal unit chain validation | ⚠️ Silent Gap | Operations.cs catalog | 4-8 |
| G14 | Division Qualifier | `money / money` disambiguation relies on type checker only — works correctly but no proof requirement validates it | ✅ By Design | N/A | 0 |

## Prioritized Fix Recommendations

### Priority 1 — CRITICAL (philosophy violations)

**G1+G2+G3: Add `QualifierCompatibilityProofRequirement` to money arithmetic and comparison operations**

This is the highest-impact fix with the lowest cost. Add `QualifierAxis.Currency` proof requirements to:
- `MoneyPlusMoney` (Operations.cs L422)
- `MoneyMinusMoney` (Operations.cs L426)
- `MoneyEqualsMoney` (L914)
- `MoneyNotEqualsMoney` (L918)
- `MoneyLessThanMoney` (L922)
- `MoneyGreaterThanMoney` (L926)
- `MoneyLessThanOrEqualMoney` (L930)
- `MoneyGreaterThanOrEqualMoney` (L934)

**Fix pattern** (identical to existing quantity operations):
```csharp
ProofRequirements:
[
    new QualifierCompatibilityProofRequirement(
        new ParamSubject(PMoney), new ParamSubject(PMoney),
        QualifierAxis.Currency,
        "Operands must have matching currency qualifiers"),
]
```

**LOC:** ~20 (2 lines per operation × 8 operations + import if needed)  
**Risk:** Low — identical pattern to quantity operations that already work.  
**Tests needed:** ~8 (one per operation, plus integration tests for discharge).

### Priority 2 — CRITICAL (cross-type chain validation)

**G4: Exchange rate × money currency chain validation**

This requires a NEW proof requirement type: `QualifierChainProofRequirement` that validates the from-currency of an exchange rate matches the currency of the money operand. The current `QualifierCompatibilityProofRequirement` only checks same-axis equality — it cannot express cross-axis or cross-type constraints.

**Options:**
- A) New DU subtype: `QualifierChainProofRequirement(LeftSubject, LeftAxis, RightSubject, RightAxis)` — validates that the qualifier value on LeftAxis of Left == qualifier value on RightAxis of Right.
- B) Extend `QualifierCompatibilityProofRequirement` with optional axis override per subject.
- C) Defer to type checker with a specialized validation pass (not catalog-driven — architectural smell).

**Recommendation:** Option A. Fits the DU pattern, keeps the catalog clean. New Strategy or extension to Strategy 5.

**LOC:** ~50 (new record + Strategy 5 extension + catalog entries)  
**Tests needed:** ~6

**G5: Price × quantity dimension chain validation**

Same pattern as G4. The price's `per 'X'` dimension should match the quantity's dimension. Requires the same `QualifierChainProofRequirement` infrastructure.

**LOC:** ~4 (after G4 infrastructure exists)  
**Tests needed:** ~4

### Priority 3 — HIGH (false positive prevention)

**G6: Dimension-only fields causing false positives on Unit-axis operations**

When a field is declared as `quantity of 'mass'` (Dimension axis) but the operation requires `QualifierAxis.Unit` matching, Strategy 5 returns null and the obligation stays unresolved. This produces a false diagnostic for a VALID operation (two mass quantities should be addable).

**Fix:** Extend `ResolveQualifierOnAxis` to fall back: when the requested axis is `QualifierAxis.Unit` and no Unit qualifier exists, check for `QualifierAxis.Dimension` and return it for compatibility comparison. Two `quantity of 'mass'` fields would then resolve to equal `DeclaredQualifierMeta.Dimension("Mass")` records → proved.

**LOC:** ~15  
**Tests needed:** ~4

### Priority 4 — MEDIUM (assignment qualifier propagation)

**G7: Expression results carry no qualifier provenance**

`ValidateAssignmentQualifiers` only checks direct sources (field refs, arg refs, typed constants). Binary expressions like `balance + payment` resolve to bare `TypeKind.Money` with no qualifier tracking. This means `set usdField = eurField + eurField` passes silently.

**Options:**
- A) Track qualifiers on `TypedBinaryOp` result expressions (new field on the typed expression)
- B) Post-assignment qualifier inference: if both operands of a same-type binary have matching qualifiers, the result inherits them.
- C) Defer to the proof engine: generate a qualifier compatibility obligation on the assignment action itself.

**Recommendation:** Option C is architecturally cleanest — it keeps the proof engine as the qualifier authority and doesn't pollute the type expression model. The type checker would generate a `QualifierCompatibilityProofRequirement` on `set` actions when the target field has qualifiers and the source is not a simple ref.

**LOC:** ~40-60  
**Tests needed:** ~8

### Priority 5 — MEDIUM

**G9: ValidateAssignmentQualifiers missing FromCurrency/ToCurrency**

The switch in `ValidateAssignmentQualifiers` (L1277-1349) handles `Dimension`, `Unit`, and `Currency` but NOT `FromCurrency` or `ToCurrency`. Assigning one exchange rate field to another with different from/to currencies would pass silently.

**LOC:** ~20  
**Tests needed:** ~4

## Proof Engine Architecture Assessment

### Current Strategy Architecture (S1–S5)

| Strategy | Handles | Qualifier Coverage |
|----------|---------|-------------------|
| S1: Literal | Numeric: literal value satisfies threshold | None |
| S2: DeclarationAttribute | Numeric: field modifier satisfies threshold; Dimension: temporal period dimension; Modifier: field has required modifier; Presence: field is guaranteed-present | Temporal dimension only |
| S3: GuardInPath | Numeric: guard comparison covers obligation; Presence: `is set` guard | None |
| S4: FlowNarrowing | Numeric: field-to-field guard implies subtraction result sign | None |
| S5: QualifierCompatibility | QualifierAxis equality between two operands | All qualifier axes (Currency, Unit, Dimension, TemporalDimension, TemporalUnit) |

### Assessment

The 5-strategy architecture is **sufficient for the current scope** with two targeted additions:

1. **Strategy 5 needs axis fallback logic** (G6) — a ~15 LOC extension to handle Dimension↔Unit axis equivalence.

2. **A new Strategy 6 or Strategy 5 extension is needed** for cross-type qualifier chain validation (G4, G5). This is NOT a whole new strategy tier — it's a second arm within the qualifier compatibility family. The `QualifierChainProofRequirement` fires the same Strategy 5 with different resolution logic (LeftAxis on Left, RightAxis on Right).

No fundamental architectural change is needed. The strategy dispatch loop (ProofEngine.cs L329-343) remains the same. The obligation collection machinery (L107-216) correctly walks all expression nodes. The issue is that the **catalog metadata is incomplete** — operations declare requirements in their descriptions but don't carry them in their `ProofRequirements` arrays.

### The Root Cause

This is not a proof engine bug. It is a **catalog metadata gap**. The operations were declared with textual descriptions noting "same currency required" but the corresponding `QualifierCompatibilityProofRequirement` entries were never added. The proof engine's machinery is correct — it faithfully processes whatever obligations the catalog declares. The catalog is simply silent on money operations.

This aligns with the catalog-driven architecture: the fix belongs in the catalog, not in the engine. The engine does not need money-specific logic — it needs the catalog to declare what it already describes in prose.

## Test Coverage Assessment

### Existing Coverage

The proof engine has **173 test cases** across 13 slice classes in `ProofEngineTests.cs`:

- **Well covered:** Numeric obligations (S1-S4), subsumption chains, guard extraction, flow narrowing, error taint suppression, constraint influence, initial state satisfiability, forwarding facts.
- **Partially covered:** Qualifier compatibility (Slice 7) — tests verify record equality semantics and axis values but do NOT test E2E obligation generation → discharge through the full pipeline with real money/quantity expressions.
- **Not covered:** Currency compatibility on arithmetic, dimension-only field compatibility, exchange rate chain validation, assignment qualifier propagation through expressions.

### Tests Needed for Comprehensive Coverage

| Gap | Tests Needed | Type |
|-----|-------------|------|
| G1-G3 | 8 | E2E: money arithmetic/comparison with matching vs mismatching currencies |
| G4 | 6 | E2E: exchange rate × money with correct/incorrect currency chains |
| G5 | 4 | E2E: price × quantity with matching/mismatching dimensions |
| G6 | 4 | E2E: quantity of 'mass' + quantity of 'mass' (dimension-only fields) |
| G7 | 8 | E2E: assignment from expression results with/without qualifier tracking |
| G9 | 4 | E2E: exchange rate field assignment with qualifier validation |
| Regression | 6 | Existing quantity/price operations still work after changes |

**Total estimated new tests: ~40**
