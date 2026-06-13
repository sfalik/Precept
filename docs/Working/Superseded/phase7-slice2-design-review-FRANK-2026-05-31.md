> **SUPERSEDED 2026-06-11** — replaced by [`compiler-readiness-plan-2026-06-11.md`](../compiler-readiness-plan-2026-06-11.md). Retained for history; all still-valid obligations were mined into that plan (see its `-appendices/working-docs-triage.md`). Do **not** treat as current strategy.

# Phase 7 Slice 2 — Design Review (Frank)

**Status**: Review — 2026-05-31
**Design under review**: `docs/Working/price-cross-unit-cancellation-design-2026-05-31.md`
**Reviewer**: Frank (Lead/Architect & Language Designer)
**Verdict**: **CONDITIONALLY APPROVED**

---

## 1. Executive Summary

This design proposes extending D8's auto-convert-within-dimension rule from `quantity ± quantity` to `price × quantity → money` cancellation. When a price denominator and a quantity share a UCUM dimension but differ in scale, the language auto-converts the quantity to the price's denominator unit using an exact `decimal` factor, then cancels — producing correctly-scaled `money`. Non-exact factors are compile-rejected. Cross-dimension stays rejected. Count units stay rejected.

**Verdict: CONDITIONALLY APPROVED.** The design has earned the auto-convert path. It addressed every substantive concern I would have raised against Option 2, grounded the choice in both the locked D8 precedent and a thorough cross-system survey, and erected the exactness gate that makes the path philosophy-honest. The conditions are minor and addressable without design rework.

---

## 2. Against My Prior Recommendation

My prior investigation (`price-cross-unit-cancellation-2026-05-31.md`) framed Option 1 (exact-unit-only) as the conservative choice, leaning on `:1866` (price comparison requires same denominator unit) and `:168` ("Unit conversion is explicit"). The investigation explicitly noted the tension: the price path says "same denominator unit" while the quantity path (D8) says "auto-convert within dimension."

### Has the design addressed the objections?

**Yes — decisively on every front:**

1. **The `:1866` objection.** I would have argued that `:1866`'s "same currency AND same denominator unit" for price comparison implies price denominators should be exact-unit across the board. The design dispatches this cleanly: comparison and cancellation are fundamentally different operations. Comparison asks "are these the same magnitude" (unit identity is semantically load-bearing for bound reasoning). Cancellation *consumes* the unit — it disappears into the result. D8 already auto-converts for `quantity` division (which also consumes units), and no runtime system treats rate-cancellation as exact-unit-only. This distinction is sound. `:1866` governs ordering semantics, not cancellation semantics.

2. **The `:168` objection ("conversion is explicit").** The design correctly identifies that `:168` states a property of the `quantity` type system at the surface level, but D8 already establishes that the *arithmetic* auto-converts within a dimension. The "explicit" claim and D8's "auto-convert" are not contradictory — D8 is the specific rule for arithmetic, `:168` is the general stance that the type system makes units visible (not silently erased). The design's inspectability requirement (hover surfaces the conversion) actually honors `:168`'s spirit — the conversion is explicit *to inspection*, just not required as manual boilerplate in the expression.

3. **The approximation risk.** My strongest implicit concern with Option 2 was silent precision loss in the money lane. The exactness gate (Decision 2) closes this completely. Only exact-`decimal` factors auto-convert; anything non-exact is a compile error. This is strictly more honest than any surveyed system (Indriya uses exact rationals, Pint silently rounds; Precept rejects).

### Is the case strong enough to override my prior leaning?

**Yes.** The argument that swings it: D8 is already locked. D8 says "arithmetic between `quantity` values of the same UCUM dimension is allowed even when units differ" with target-directed resolution. Price cancellation is the same commensurable-arithmetic family. Requiring explicit conversion for `price × quantity` while auto-converting for `quantity ÷ quantity` would be an inconsistency in the language — a special case where D8 doesn't apply, without a principled reason. The design correctly identifies that F#'s explicit-conversion requirement is forced by compile-time erasure (F# units are erased — they *cannot* convert at runtime), not by design preference. Precept has a runtime.

### New concerns not anticipated?

One: the design introduces a "forward-reference" tradeoff — compile-time `"Proved"` for a conversion the runtime can't yet execute. The design is honest about this (Philosophy Alignment row, Principle 1/11 tradeoff paragraph) and mitigates it correctly (no external authors, runtime obligation documented and tracked, no silent wrong answers because the runtime is a stub for *all* arithmetic). This is acceptable for a pre-release PoC. It would not be acceptable post-release.

---

## 3. Spec Analysis

### D8 (commensurable arithmetic) — `:1733-1739`

The design correctly reads D8 as establishing the precedent. D8's text: "Arithmetic between `quantity` values of the same UCUM dimension is allowed even when units differ. Resolution: (1) target-directed, (2) left-operand wins." The design extends this to price cancellation with the same resolution order (target-directed to the price's denominator unit). This is a natural extension, not a contradiction.

The design's application of "target-directed" to mean "convert the quantity to the price's denominator unit" is correct — the target (the `money` result) has no unit, so the next deterministic anchor is the price denominator. This parallels D8's "target-directed" for `quantity ± quantity` where the assignment target's unit is the target.

### `:1866` (price comparison exact-unit rule)

Correctly bounded. The design cites `:1866` in Decision 1's "Strongest counter-evidence" and dispatches it. The distinction (comparison preserves unit identity for bound semantics; cancellation consumes the unit) is principled. `:1866` remains untouched — price comparison still requires same denominator unit.

### `:220` (cross-dimension error)

Correctly bounded. `:220` states "`UnitPrice * Weight` where Weight is `quantity in 'kg'` is a compile-time error — the price's denominator is `each`, not `kg`." This is a cross-*dimension* rejection (count ≠ mass). The design preserves this exactly — cross-dimension stays a compile error. The design only opens the cross-*unit-within-same-dimension* path.

**Minor concern:** `:220`'s example demonstrates a `USD/each` price × `kg` quantity rejection. The design should ensure the spec update makes clear that `:220` addresses dimension mismatch specifically, not unit mismatch generally. Currently the text reads ambiguously ("the price's denominator is `each`, not `kg`") — it could be read as "denominators must match exactly." The spec update in the doc-update enumeration must disambiguate this.

### `:397` (count units / PRE0137)

Correctly preserved. Count units share `DimensionVector.None` with no universal factor, so `factor(box → each)` does not exist → the proof obligation cannot discharge → the diagnostic fires. The exactness gate also independently catches this: there is no exact-`decimal` factor for count cross-conversions. Two independent rejection paths — sound.

### Tensions / omissions

1. The spec says at `:168`: "Unit conversion is explicit." The design's position is that D8 supersedes this for arithmetic, and inspection preserves explicitness. This is defensible but the spec text should be updated to clarify that "explicit" means "visible to inspection and traceable," not "requires manual conversion expression." The doc-update enumeration should note this — **it currently does not.** See Condition 1.

---

## 4. Philosophy Analysis

Walking the alignment table row by row:

| Row | My assessment |
|---|---|
| Prevention (P1) | **Agree.** The live hole (silent factor-dropping) is a prevention violation. Closing it with correct-conversion-or-rejection serves P1. The "forward reference" tradeoff is honest. |
| One file (P2) | **Agree.** Trivially satisfied — no external logic. |
| Determinism (P3) | **Agree.** UCUM factors are fixed constants, `decimal` arithmetic is deterministic. No issue. |
| Inspectability (P4) | **Agree, with emphasis.** This is the load-bearing mitigation for "implicit in syntax." The design correctly notes the conversion must be surfaced in hover/trace. I want to underscore: this is not optional polish — it is a **structural requirement** for philosophy compliance. If the hover doesn't ship, P4 is violated. |
| Keyword readability (P5) | **Agree.** No new syntax. |
| Domain meaning (P6) | **Agree.** Reinforces domain identity. |
| Compile-time-first (P7) | **Agree.** The proof engine gates everything at compile time. |
| Approximation honesty (P8) | **Agree.** The exactness gate is precisely the P8 mechanism. Only exact factors admitted; non-exact rejected. No silent rounding. This is the strongest row in the table. |
| Mandatory rationale (P9) | **Agree.** N/A. |
| Totality (P10) | **Agree.** `decimal` × `decimal` is total; the gate admits only factors where this holds. |
| Static completeness (P11) | **Agree, qualified.** The qualification is correct: currently the runtime is a stub. The design is honest about this. When the runtime ships, P11 is fully satisfied. The pre-release PoC context makes the forward-reference acceptable. |

**No rows that paper over genuine tension.** The Principle 1/11 tradeoff paragraph below the table is the most philosophically honest part of the design — it states the tension explicitly, names the mitigation (no external authors + documented obligation), and identifies when the tension resolves (runtime ships). This is how to handle a "not yet fully literal" claim.

---

## 5. Decisions Analysis

### Decision 1: Extend D8 to price cancellation (target-directed)

- **Rationale**: Sound. D8 already governs the same arithmetic family; extending it is consistency.
- **Alternatives**: (A) Exact-unit-only — genuinely rejected with reason (D8 already rejected "require explicit conversion" for quantity; F# is not counter-evidence). (B) Precision warning — rejected correctly (exact factors have no inexactness to warn about). (C) Silent drop — obviously wrong.
- **Four-leg**: Complete. Rationale, alternatives+rejection reasons, precedent (D8, survey, UnitsNet), tradeoff (implicit conversion + forward reference).
- **Counter-evidence handling**: The `:1866` counter-evidence is the strongest challenge and it's addressed directly. The distinction (comparison ≠ cancellation) is principled.
- **Contradictions**: None found.

### Decision 2: Exactness gate

- **Rationale**: Sound. This is the P8 enforcement mechanism.
- **Alternatives**: (A) Assume all exact — rejected correctly (known-false for transcendental). (B) Float factors — rejected correctly (Pint #201 counter-example).
- **Four-leg**: Complete. The "no surveyed system rejects inexact factors" counter-evidence is honestly addressed: "because none make Precept's exact/approximate-must-be-visible commitment."
- **Contradictions**: None.

### Decision 3: Exclusion is absolute positions only

- **Rationale**: Sound. `quantity` is definitionally an amount, not a position. The °C-amount vs °C-reading distinction maps to `duration` vs `instant` — a familiar Precept pattern.
- **Alternatives**: (A) Exclude all affine/log — rejected correctly (too broad, conflates position with amount). (B) Delta semantics default — rejected correctly (unnecessary for amounts).
- **Four-leg**: Complete. The two-gate composition (position-rule + exactness-gate) is elegant — they address orthogonal concerns.
- **Concern**: The "teachability risk" (authors confusing amount with reading) is real but correctly deferred to docs + Slice 4. Not a blocker.

### Decision 4: Metadata lives in UCUM catalog

- **Rationale**: Sound. This is literally what the catalog-driven architecture mandates.
- **Alternatives**: Both rejected correctly (catalog violations).
- **Four-leg**: Complete.
- **Contradictions**: None. In fact, this is the only decision that *couldn't* go another way without violating catalog discipline.

---

## 6. Scope and Boundary Analysis

### In-scope assessment

Everything scoped-in belongs there:
- The cancellation semantics — the core deliverable.
- The compile-time proof obligation — required for P7/P11.
- The runtime conversion obligation *documented as a requirement* — correct for a pre-runtime PoC. Not building it now, but pinning the contract.
- The catalog metadata additions — required by the proof obligation.
- The exactness gate — required by P8.
- The narrowed exclusion (absolute positions only) — required to not over-restrict.

### Out-of-scope assessment

- **Absolute-position math (Slice 4)**: Correctly deferred. No current `quantity` models absolute positions.
- **Transcendental/log factors**: Correctly rejected by the gate, not scoped.
- **`money / price → quantity` and `price × period`**: Correctly deferred — separate operations.
- **`delta °C` sugar**: Correctly deferred — language-surface question.
- **Temperature/log increment spelling**: Correctly noted as unnecessary ("per kelvin" already works).

### Dangerous gaps?

None that I can identify. The scope boundary is drawn at exactly the right line. The one potential gap — "what happens if an author models a thermostat reading as `quantity in 'Cel'`" — is correctly identified as a teachability issue, not a soundness issue (the math is amount-semantics, which is correct for amounts; it's wrong only if the data isn't actually an amount, which is a modeling error the type system can't prevent without a separate point-type).

---

## 7. Implementation Plan Assessment

The Inventory section provides:
- **Catalog additions**: `AmountConversionFactor` (exact `decimal`), `IsExactDecimalFactor` (bool), `IsRatioScale` (bool) — architecturally sound. These are intrinsic unit properties. The catalog is the correct home. Per-unit, not per-operation.
- **Proof engine extension**: Side-conditions on existing `QualifierChain` discharge — correct approach. No new `ProofRequirementKind` means no catalog-member proliferation.
- **Evaluator**: Documented as obligation, not built. Correct for PoC.
- **Diagnostics**: PRE0114 broadened + non-exact-factor variant. Fine.
- **Tooling**: Hover surfaces conversion. Required by P4.
- **Tests**: Correct coverage — un-skip the live-hole test, add exactness-gate rejection, affine-amount acceptance, cross-dimension still-errors, count still-errors.

**Method-level specificity**: The design is at the "what to build and where" level, not "which method signature on which class." For a design doc this is appropriate — the implementation plan in the PR body is where method-level specificity belongs. The design correctly identifies the files (`ProofEngine.Qualifiers.cs`, `Operations.cs`, the UCUM catalog source, `UnitDimensionHelper.cs`).

**One concern**: The `IsRatioScale` flag is described as "false for absolute-position units — none currently admissible as `quantity`." This means it's vacuously true for all current units. The design should state explicitly whether the flag is implemented now (all `true`) or deferred to Slice 4. If implemented now with all-`true` values, the proof-engine check is a no-op until Slice 4 adds non-ratio units — which is fine, but should be stated. See Condition 2.

---

## 8. Specific Concerns / Blockers

### BLOCKED: None.

### CONDITIONS (must address before implementation, not design rework):

1. **Doc-update enumeration must include `:168` disambiguation.** The spec says "Unit conversion is explicit" at `:168`. The design relies on D8 superseding this for arithmetic and inspection preserving explicitness. The spec update must clarify that "explicit" means "visible and traceable," not "requires manual conversion syntax." Add this to the doc-update enumeration as a line under `business-domain-types.md`.

2. **State whether `IsRatioScale` is implemented now (all-`true`) or deferred.** The inventory lists it as "set the stage for Slice 4." Clarify in the implementation plan: is the flag stored now with all-`true` values and a vacuous check in the proof engine, or is the flag + check deferred entirely to Slice 4? Either answer is acceptable — the ambiguity is not.

### WARNED (should address, not blocking):

1. **The `AmountConversionFactor` naming.** "Amount" in Precept could be confused with "amount" in the money sense. Consider `ScaleToBaseFactor` or `DimensionScaleFactor` — the name should describe what the value *is* (a scale factor to the dimension's base unit), not the *use case* (amount conversion). This is a taste call, not a blocker.

2. **The "Proved" forward-reference tradeoff.** Acceptable for PoC, but should be tracked with the same visibility as the skipped test — ideally a comment in the proof engine code referencing the runtime obligation. When the runtime lands, the compile-time claim becomes literally true. Until then, the tradeoff is honest only if visible. Don't let it become hidden certainty.

### QUESTIONS for Shane:

None. The design is clear and complete. The conditions are minor.

---

## 9. Final Verdict

**CONDITIONALLY APPROVED.**

Conditions:
1. Add `:168` disambiguation to the doc-update enumeration.
2. Clarify `IsRatioScale` implementation timing (now-all-true vs deferred-to-Slice-4).

Both are addressable in minutes. Neither requires design rework. Once addressed, proceed to implementation.

---

## Note on the override

This design overturned what would have been my conservative recommendation (exact-unit-only). It earned the override. The argument is simple and devastating: D8 is already locked. D8 already auto-converts within a dimension for quantity arithmetic. Requiring explicit conversion for the *same arithmetic family* applied to price cancellation would be an unprincipled exception — "D8 applies everywhere except here, for no stated reason." The exactness gate makes the auto-convert path strictly safer than what any surveyed system offers. The inspectability requirement preserves the spirit of "explicit." I was wrong to lean conservative on this one; the locked spec already pointed the way.
