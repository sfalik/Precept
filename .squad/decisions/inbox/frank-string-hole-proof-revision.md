# Revised String-Hole Proof Analysis (frank-25 follow-up)

**Author:** Frank  
**Date:** 2026-05-11T15:22:20.155-04:00  
**Context:** Shane's corrections and follow-up question on the frank-25 string-hole exclusion analysis.

---

## Correction Acknowledged: Samples Don't Reflect Real Usage

My frank-25 analysis stated: "Zero samples use `currency`/`unitofmeasure` field types" and classified the `fee-schedule.precept` string-carrying-currency pattern as a "design smell." That reasoning was wrong on the frequency evidence, and I'm correcting it explicitly.

**The samples were written before interpolated typed constant support existed.** The feature was (and remains) an unimplemented crash stub — `ParseInterpolatedTypedConstant()` skips hole tokens, and the type checker maps `TypedConstantStart` to `TypedErrorExpression`. No author could have written `currency`/`unitofmeasure` field types in interpolation holes because interpolation holes in typed constants don't work at all. The absence of these patterns in samples is evidence of feature non-existence, not evidence of low usage frequency.

**The primary use case IS the compositional typed-literal construction pattern.** Shane's canonical example — `event SetCurrency(Code as currency)` → `set Balance = '{Amount} {Code}'` — is not a niche edge case. It is the central motivation for interpolated typed constants: assembling a typed value from independently-typed components received at runtime boundaries. When this feature ships, this will be the dominant pattern.

My frank-25 "frequency verdict: rare in practice" is withdrawn. The correct assessment: **high frequency, primary use case, currently blocked by unimplemented feature.**

The architectural verdict — exclude `string` entirely — remains correct, but for the right reason: type safety and proof power, not rarity of use.

---

## Revised Strategy-by-Strategy Analysis

Shane's question: if `string` is excluded and ALL holes must be typed, can the proof engine prove ALL 5 strategies for interpolated typed constants?

The answer requires separating two conditions:
- **Typed holes, values statically unknown** — e.g., `Code` comes from a `currency`-typed event argument fired at runtime
- **Typed holes, values statically known** — e.g., `Code` has a foldable default like `default 'USD'` and no mutations

### S1 — Literal Proof

**frank-25 assessment:** "None" delta.  
**Revised assessment:** Conditional gain — **foldable when all hole values are statically known.**

`TryLiteralProof` (ProofEngine.cs:347) requires the subject to be a `TypedLiteral` with a numeric `Value` (decimal/int/long). Typed constant fields fall into `GetTypeDefault`'s `_ => MarkUnfoldable` branch (line 1200), which returns `null` and marks the field as unfoldable. The constant folder then returns `UnknownSentinel` for any expression involving that field.

**With typed holes and statically known values:** If ALL hole expressions in an interpolated typed constant resolve to compile-time-known values (e.g., `Amount` has `default 5` and `Code` has `default 'USD'`, both with no mutations), the type checker can substitute and produce the complete literal `'5 USD'`. At that point, the proof engine *could* treat the resulting money field as having a known numeric magnitude — but only if the folding pipeline is extended to decompose typed constant literals into their numeric component.

**What's required for S1 to fire:**
1. All hole values must be statically known (foldable defaults, no mutations).
2. `GetTypeDefault` must be extended to recognize interpolated typed constants with all-foldable holes and extract the numeric magnitude.
3. The constant folder must produce a `decimal` value for the magnitude component, not `UnknownSentinel`.

**Verdict:** S1 is **achievable under the all-foldable condition** but requires proof engine enhancement — it doesn't come for free from string exclusion alone. String exclusion is a *prerequisite* (you can't fold a `string` hole into a validated magnitude), but not sufficient by itself.

**When hole values are NOT statically known:** S1 remains unavailable. The field stays unfoldable. This is correct behavior — you can't prove a numeric bound on a value you don't know.

### S2 — Declaration Attribute Proof

**frank-25 assessment:** "Gains qualifier resolution."  
**Revised assessment:** Confirmed — **fully provable regardless of whether hole values are statically known.**

`TryDeclarationAttributeProof` (ProofEngine.cs:378) operates on field declarations — modifiers and dimension metadata — not on runtime values. When a field is declared `field Balance as money nonnegative`, the proof engine proves the `nonnegative` obligation from the modifier on the declaration, regardless of what value the field holds at runtime.

Qualifier resolution (for `DeclaredQualifiers`) also operates at the declaration level. When `Balance` is assigned via `'{Amount} {Code}'` where `Code as currency`, the type checker can resolve `Balance`'s currency qualifier from `Code`'s declared type — it's a `currency`, so it carries valid ISO 4217 semantics. If `Code` were `string`, the qualifier would be unresolvable.

**Verdict:** S2 is **fully provable with typed holes, regardless of static-knowability.** String exclusion is both necessary and sufficient for this strategy.

### S3 — Guard in Path Proof

**frank-25 assessment:** "Gains guard decomposition for qualifier slots."  
**Revised assessment:** Nuanced — **fully provable for numeric guards; qualifier guards gain type safety but still require qualifier-axis matching.**

Guard decomposition (ProofEngine.cs:663-710) pattern-matches `TypedFieldRef op TypedLiteral` to extract `GuardConstraint` records with decimal threshold values. This works on the *numeric magnitude* of typed constant comparisons (e.g., `when Balance > '0 USD'` extracts threshold `0`).

With typed holes: guard decomposition of the *numeric* component works regardless of hole value knowability — the guard literal `'0 USD'` is always static. The proof engine extracts the numeric threshold and proves the obligation.

The *qualifier* component of guard comparisons (is `Balance` in USD when the guard says `> '0 USD'`?) is handled by S5 (Qualifier Compatibility), not S3 directly. S3's guard decomposition focuses on the numeric bound.

**Verdict:** S3 is **fully provable for numeric guard obligations regardless of static-knowability.** The qualifier dimension of guard comparisons is delegated to S5.

### S5 — Qualifier Compatibility Proof

**frank-25 assessment:** "Gains qualifier compatibility proofs."  
**Revised assessment:** Confirmed — **fully provable regardless of whether hole values are statically known.**

Qualifier compatibility resolves `DeclaredQualifiers` from field declarations and compares them for axis-match equality. This is a declaration-level check. When `Balance` is constructed from `'{Amount} {Code}'` where `Code as currency`, the type checker knows `Balance`'s qualifier comes from a `currency`-typed source. If `Code` is declared as `currency in 'USD'`, the qualifier is `USD`. If `Code` has no `in` qualifier, the proof engine can still determine that both operands in `Balance + OtherMoney` have their currencies sourced from the same `currency`-typed field (or compatible `currency`-typed fields).

If `Code` were `string`, qualifier resolution would fail — `string` carries no qualifier metadata. The proof would fall to `Unresolved`.

**Verdict:** S5 is **fully provable with typed holes, regardless of static-knowability.** This is the single biggest proof-power gain from string exclusion. String exclusion is both necessary and sufficient.

### S10 — Initial State Satisfiability

**frank-25 assessment:** "None" delta.  
**Revised assessment:** Conditional gain — **foldable when all hole values are statically known.**

Initial state satisfiability (ProofEngine.cs:1140-1186) builds a default-value environment by folding field defaults. Typed constant fields hit `GetTypeDefault`'s `_ => MarkUnfoldable` branch and become `UnknownSentinel`. Any `ensure` constraint involving an unfoldable field produces an unknown fold result, and the constraint is conservatively treated as satisfiable (not a violation).

**With typed holes and statically known values:** The same enhancement described for S1 applies. If `GetTypeDefault` can decompose an interpolated typed constant with all-foldable holes into a numeric magnitude, then `ensure` constraints like `ensure Balance >= '0 USD'` can be evaluated against the known default. The constant folder would substitute the magnitude, compare against the threshold, and prove or disprove satisfiability.

**What's required for S10 to fire:**
1. All hole values must be statically known.
2. `GetTypeDefault` must extract the numeric magnitude from all-foldable interpolated typed constants.
3. The `ConstantFold` function must handle typed constant comparison by extracting the numeric component.

**Verdict:** S10 is **achievable under the all-foldable condition** but requires proof engine enhancement. String exclusion is a prerequisite but not sufficient alone. When hole values are not statically known, S10 remains conservative (fields are unfoldable), which is the correct behavior.

---

## Summary Table

| Strategy | Typed holes, values unknown | Typed holes, values known | Requires engine enhancement? |
|----------|---------------------------|--------------------------|------------------------------|
| S1 Literal Proof | ❌ Unavailable (correct) | ✅ Achievable | Yes — `GetTypeDefault` must decompose typed constants |
| S2 Declaration Attribute | ✅ Fully provable | ✅ Fully provable | No — works today once string is excluded |
| S3 Guard in Path | ✅ Fully provable (numeric) | ✅ Fully provable | No — guard literals are always static |
| S5 Qualifier Compatibility | ✅ Fully provable | ✅ Fully provable | No — works today once string is excluded |
| S10 Initial State Satisfiability | ❌ Conservative/unknown (correct) | ✅ Achievable | Yes — constant folder must handle typed constants |

---

## Answer to Shane's Question

**"Would that allow us to prove all 5 strategies at compile time for interpolated typed literals?"**

**Three of five (S2, S3, S5) — yes, unconditionally.** These strategies operate on field declarations, modifiers, and qualifier metadata. They never need to know the runtime value of a hole. String exclusion is the only gate — once all holes are typed, these strategies have full proof power regardless of whether `Code` is `'USD'` or a runtime-supplied currency.

**Two of five (S1, S10) — yes, conditionally.** These strategies require *value-level* knowledge — they need to know what number `Amount` is, what currency `Code` resolves to. They are achievable when all hole values are statically known (foldable defaults, no mutations), but they require proof engine enhancement to decompose typed constant values into their numeric components. When hole values come from runtime event arguments (the primary use case), S1 and S10 remain correctly conservative — you can't prove a numeric bound on a value you haven't received yet.

**Is "all 5" achievable?** Yes — under the condition that all holes resolve to compile-time-known values. This is a real scenario: `field Balance as money default '{DefaultAmount} {DefaultCurrency}'` where both `DefaultAmount` and `DefaultCurrency` have foldable defaults. But this is the *secondary* use case. The *primary* use case — constructing typed literals from event arguments at runtime — gives you 3 of 5, which is the correct theoretical maximum. You cannot prove value-dependent properties about values you don't have.

**The 3-of-5 for the primary use case is not a weakness.** S2, S3, and S5 cover the proof families that matter most for typed constant correctness: modifier satisfaction, guard decomposition, and qualifier compatibility. S1 and S10 being conservative for runtime-valued fields is mathematically correct — the proof engine should not claim to know what it cannot know.

---

## Revised Verdict

**Exclude `string` entirely.** The original verdict stands, with corrected reasoning:

1. **The primary use case is compositional typed-literal construction** — assembling money/quantity/price values from typed event arguments. This is high-frequency, not rare.
2. **Three strategies (S2, S3, S5) gain unconditional proof power** — no enhancement needed, just string exclusion.
3. **Two strategies (S1, S10) gain conditional proof power** — achievable with proof engine enhancement when values are statically known.
4. **The 3-of-5 unconditional result is the correct theoretical maximum** for the primary use case (runtime event arguments). Demanding 5-of-5 for runtime-valued holes would require the proof engine to claim knowledge it doesn't have.
5. **String exclusion is the prerequisite for ALL of this.** With string holes, S2/S5 lose qualifier resolution, S3 loses qualifier-aware guard decomposition, and S1/S10 can never fold because string content is unvalidatable at compile time. String exclusion doesn't just help — it's the gate that unlocks the entire proof-power gain.
