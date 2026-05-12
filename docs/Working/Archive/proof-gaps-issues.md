# Proof Engine Gap Issues

## Issue: Money arithmetic missing currency compatibility enforcement

**Gap ID:** G1, G2  
**Type:** Silent Gap (CRITICAL)  
**Scenario:** `money in 'USD' + money in 'EUR'` compiles clean. Same for subtraction.  
**Expected:** `UnprovedQualifierCompatibility` diagnostic — operands must have matching currency qualifiers.  
**Actual:** No obligation generated. No diagnostic. Invalid currency mixing is structurally possible.  
**Fix location:** Operations.cs catalog entries `MoneyPlusMoney` (L422) and `MoneyMinusMoney` (L426) — add `QualifierCompatibilityProofRequirement` on `QualifierAxis.Currency`.  
**LOC estimate:** 8  
**Priority:** Critical  
**Philosophy violation:** "Invalid configurations structurally impossible" — violated. Cross-currency arithmetic produces meaningless results.

---

## Issue: Money comparisons missing currency compatibility enforcement

**Gap ID:** G3  
**Type:** Silent Gap (CRITICAL)  
**Scenario:** `money in 'USD' > money in 'EUR'` compiles clean. All 6 comparison operators affected.  
**Expected:** `UnprovedQualifierCompatibility` diagnostic — comparing currencies without conversion is semantically meaningless.  
**Actual:** `Match: QualifierMatch.Same` is set on the catalog entries but this only affects operation disambiguation (selecting among multiple candidates). Since money comparisons have exactly ONE candidate per operator, `Match` has no enforcement effect. No proof requirement exists.  
**Fix location:** Operations.cs catalog entries `MoneyEqualsMoney` through `MoneyGreaterThanOrEqualMoney` (L914-937) — add `QualifierCompatibilityProofRequirement` on `QualifierAxis.Currency`.  
**LOC estimate:** 12  
**Priority:** Critical  
**Philosophy violation:** Same as G1/G2. Comparing USD to EUR amounts without conversion is nonsensical.

---

## Issue: Exchange rate × money has no from-currency chain validation

**Gap ID:** G4  
**Type:** Silent Gap (CRITICAL)  
**Scenario:** `exchangerate from 'USD' to 'EUR' * money in 'GBP'` compiles clean. The rate's `from` currency (USD) should match the money's currency (GBP), but no validation exists.  
**Expected:** Diagnostic preventing application of a USD→EUR rate to a GBP amount.  
**Actual:** No obligation generated. The conversion produces a meaningless result.  
**Fix location:** New `QualifierChainProofRequirement` DU subtype + Strategy 5 extension + catalog entry on `ExchangeRateTimesMoney`.  
**LOC estimate:** 50  
**Priority:** Critical  
**Notes:** Requires new proof requirement type because existing `QualifierCompatibilityProofRequirement` checks same-axis equality. This needs cross-type, cross-axis validation (rate's FromCurrency vs money's Currency).

---

## Issue: Price × quantity has no dimension chain validation

**Gap ID:** G5  
**Type:** Silent Gap (CRITICAL)  
**Scenario:** `price in 'USD' per 'kg' * quantity of 'length'` compiles clean. The price's per-unit dimension (mass) should match the quantity's dimension (length), but no validation exists.  
**Expected:** Diagnostic preventing multiplication of incompatible price/quantity dimensions.  
**Actual:** No obligation. Dimensional cancellation produces a meaningless money amount.  
**Fix location:** Same `QualifierChainProofRequirement` infrastructure as G4 + catalog entry on `PriceTimesQuantity`.  
**LOC estimate:** 4 (after G4 infrastructure)  
**Priority:** Critical  
**Notes:** The entire point of `price * quantity → money` is dimensional cancellation. If the dimensions don't match, the cancellation is invalid.

---

## Issue: Dimension-only quantity fields produce false positive on Unit-axis operations

**Gap ID:** G6  
**Type:** False Positive  
**Scenario:** `quantity of 'mass' + quantity of 'mass'` → `UnprovedQualifierCompatibility` diagnostic. Two fields with the same dimension qualifier should be compatible for addition.  
**Expected:** Proved — same dimension means same measurement family, addition is valid.  
**Actual:** Operations require `QualifierAxis.Unit` matching. Fields with only `DeclaredQualifierMeta.Dimension` (axis = `QualifierAxis.Dimension`) have no Unit-axis qualifier. `ResolveQualifierOnAxis` returns null → cannot discharge.  
**Fix location:** ProofEngine.cs `ResolveQualifierOnAxis` (L898-911) — add fallback: when requested axis is `Unit` and no Unit qualifier exists, fall back to `Dimension` axis for compatibility check.  
**LOC estimate:** 15  
**Priority:** High  
**Notes:** The `InOfExclusive: true` shape means a field has EITHER `in 'kg'` (Unit) OR `of 'mass'` (Dimension), never both. The proof engine must handle both cases.

---

## Issue: Expression results carry no qualifier provenance for assignment validation

**Gap ID:** G7  
**Type:** Silent Gap  
**Scenario:** `set usdField = eurField + eurField` — the binary expression result is bare `TypeKind.Money` with no qualifier tracking. `ValidateAssignmentQualifiers` only checks `TypedFieldRef`, `TypedArgRef`, and `TypedTypedConstant` sources.  
**Expected:** The assignment should verify that the expression result's effective qualifier matches the target field's qualifier.  
**Actual:** Binary/unary expression results are invisible to the assignment qualifier checker. Any expression involving arithmetic bypasses qualifier validation on the target field.  
**Fix location:** TypeChecker.Expressions.cs `ValidateAssignmentQualifiers` or new proof obligation on `set` actions.  
**LOC estimate:** 40-60  
**Priority:** Medium  
**Notes:** This is a deeper architectural question. Option C (proof obligation on assignment) is preferred because it keeps the proof engine as the qualifier authority. The type checker would emit a qualifier compatibility obligation when assigning an expression (not a direct ref) to a qualified field.

---

## Issue: Price × period/duration missing temporal dimension chain validation

**Gap ID:** G8, G13  
**Type:** Silent Gap  
**Scenario:** `price in 'USD' per 'month' * period of 'time'` or `price in 'USD' per 'hour' * duration` — no validation that the price's temporal denominator matches the multiplier's temporal dimension.  
**Expected:** Diagnostic when the temporal dimension axes are incompatible.  
**Actual:** No proof requirement on `PriceTimesPeriod` or `PriceTimesDuration`.  
**Fix location:** Operations.cs catalog entries + potentially `QualifierChainProofRequirement` for temporal axis.  
**LOC estimate:** 8-16  
**Priority:** Medium  
**Notes:** Less common scenario than price × quantity, but same philosophical issue: dimensional cancellation without dimension compatibility is meaningless.

---

## Issue: ValidateAssignmentQualifiers missing FromCurrency/ToCurrency handling

**Gap ID:** G9  
**Type:** Silent Gap  
**Scenario:** `set rateField = otherRateField` where the two exchange rate fields have different from/to currencies. The switch statement in `ValidateAssignmentQualifiers` handles Dimension, Unit, and Currency cases but NOT FromCurrency or ToCurrency.  
**Expected:** `QualifierMismatch` diagnostic on from/to currency mismatch.  
**Actual:** The switch falls through silently — no case matches `DeclaredQualifierMeta.FromCurrency` or `DeclaredQualifierMeta.ToCurrency`.  
**Fix location:** TypeChecker.Expressions.cs L1277-1349 — add `case DeclaredQualifierMeta.FromCurrency` and `case DeclaredQualifierMeta.ToCurrency`.  
**LOC estimate:** 20  
**Priority:** Medium
