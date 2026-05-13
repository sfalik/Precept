# Precept Interval Hover Design

**By:** Elaine · **V1** · **2026-05-13T18:17:15.685-04:00** · VS Code markdown hover  
**Extends:** `docs/working/hover-design.md` (V7) — does not replace  
**Reference:** `docs/working/overflow-prevention-design-analysis.md` (Frank, interval arithmetic recommendation)

---

## 1. Design Intent

Interval hover answers one question: **is this value's range safe?** It extends the existing proof hover contract by surfacing bound claims — declared on fields, inferred through arithmetic — using the same three-line compact budget and the same badge vocabulary already established in V7.

**Intervals are a proof concern, not a data-display concern.** They appear on hovers only when they carry proof-relevant information: a declared bound, an arithmetic result, or a gap. They do not appear as ambient metadata on every numeric field.

---

## 2. Interval Notation

All interval displays use bracket notation with two-dot separator:

```
[lo .. hi]
```

| Pattern | Meaning |
|---------|---------|
| `[0 .. 1 000]` | Fully bounded: min 0, max 1 000 |
| `[0 .. +∞]` | Lower-bounded only |
| `[−∞ .. 0]` | Upper-bounded only |
| `[−∞ .. +∞]` | Unbounded — no declared or inferred limit |

**Compactness rule:** Suppress trailing zeros but keep enough precision to show the bound is meaningful. Use thin space as thousands separator in intervals longer than four digits (`999 999.99` not `999999.99`).

**Declared vs. inferred:** Label origin when it's the proof-relevant question.
- `declared: min 0 max 1 000` — from field annotation
- `inferred from arithmetic` — derived by the interval solver

---

## 3. Badge Usage for Intervals

No new icons are introduced. The existing vocabulary extends naturally:

| Badge | Interval role |
|-------|--------------|
| ✅ | Result interval fits the target bound — proof is complete |
| ⚠️ | Result interval escapes the target bound, or operand is unbounded |
| 🔬 | Arithmetic chain, propagation step, or interval reasoning |
| ⚖️ | Declared bounds on a field (bounds are a comparison contract: `x ≥ min AND x ≤ max`) |

`⚖️` already means "currency, unit, or comparison contract." Declared numeric bounds are a comparison contract and belong to the same badge class.

---

## 4. Template Variations

### 4.1 Field with Declared Bounds (Proven Safe)

Field has declared `min`/`max`; all assignments proven to stay within the bound.

```md
✅ Proven · `balance` stays within `[0 .. 999 999 999.99]`
⚖️ Declared: `min 0 max 999 999 999.99` · `CatalogCurrency`
Governed by: 2 rules · 1 ensure
```

**Reading:** Line 1 delivers the verdict. Line 2 cites the bound source — declared, not inferred — and links the qualifier. Line 3 provides governance context (unchanged from standard field card).

---

### 4.2 Field with Declared Bounds (Gap — Assignment Overflows)

Field has declared bounds but a computed assignment produces a result interval that escapes them.

```md
⚠️ Gap · `balance` assignment may leave `[0 .. 999 999 999.99]`
🔬 `balance − amount` → `[−50 000 .. 999 999 999.99]` · lower bound unsafe
`amount` has no lower bound · add guard `amount ≤ balance` or bound `amount`
```

**Reading:** Line 1 names the field and the violated range. Line 2 shows the arithmetic: left operand interval minus right operand interval yields the result, flagging which side is unsafe. Line 3 is the actionable repair hint — one concrete path forward.

**Expanded view (lines 4–5, when proof is the user's question):**

```md
⚠️ Gap · `balance` assignment may leave `[0 .. 999 999 999.99]`
🔬 `balance − amount` → `[−50 000 .. 999 999 999.99]` · lower bound unsafe
`amount` has no lower bound · add guard `amount ≤ balance` or bound `amount`
🔬 `balance ∈ [0 .. 999 999 999.99]` (declared)
🔬 `amount ∈ [−∞ .. +∞]` (no bounds declared) · subtraction expands lower to `−∞`
```

---

### 4.3 Computed Expression with Interval Result (Proven Safe)

Arithmetic expression with fully bounded operands; result fits target.

```md
✅ Proven · `principal + interest` result `[0 .. 50 000]` fits `loanBalance`
🔬 `[0 .. 45 000]` + `[0 .. 5 000]` → `[0 .. 50 000]`
Target declared: `max 50 000` · proven safe
```

**Reading:** Line 1 gives the verdict, names the expression, shows the result interval, and names the target field. Line 2 shows the one-step propagation in full. Line 3 confirms what the target declared and why the proof succeeded.

---

### 4.4 Computed Expression with Interval (Gap — Overflow Risk)

Arithmetic expression where the result interval exceeds the target field's bound.

```md
⚠️ Gap · `invoice + surcharge` may exceed `[0 .. 999 999.99]`
🔬 `[0 .. 999 990.00]` + `[0 .. 100.00]` → `[0 .. 1 000 090.00]` · upper bound unsafe
`surcharge` max exceeds available headroom in `invoice` · tighten `surcharge` bounds
```

**Reading:** Line 1 names the expression and the violated bound. Line 2 shows the full arithmetic chain with the computed result, flagging which end of the interval is unsafe. Line 3 gives the repair direction — it names the operand causing the overflow and the specific fix.

**Expanded view (when detailed arithmetic is needed):**

```md
⚠️ Gap · `invoice + surcharge` may exceed `[0 .. 999 999.99]`
🔬 `[0 .. 999 990.00]` + `[0 .. 100.00]` → `[0 .. 1 000 090.00]` · upper bound unsafe
`surcharge` max exceeds available headroom in `invoice` · tighten `surcharge` bounds
🔬 `invoice ∈ [0 .. 999 990.00]` (declared) · headroom: `9.99`
🔬 `surcharge ∈ [0 .. 100.00]` (declared) · exceeds headroom by `90.01`
```

---

### 4.5 Unbounded Field (Implicit `[−∞ .. +∞]`)

A numeric field with no declared bounds. The interval solver cannot make any safe claims about expressions that include it.

```md
⚠️ Gap · `adjustment` has no declared bounds
🔬 Interval: `[−∞ .. +∞]` · arithmetic with this field can't be proven safe
Declare `min` / `max` to enable interval proof · fallback: runtime overflow check
```

**Reading:** Line 1 states the gap plainly — no bounds, no proof. Line 2 shows what the solver sees: unbounded input propagates unbounded output. Line 3 offers the fix path and names the fallback behavior.

**Routing note:** An unbounded field hover always shows `⚠️ Gap`, not `⚡ Enforced`, even though a runtime check exists. The hover reflects the static proof status, not the runtime safety net. The distinction matters: the runtime check is a fallback, not a guarantee.

---

### 4.6 Optional Field (Interval Carries Presence Uncertainty)

An optional field where the interval applies only when the field is present. Expressions that use the field without a presence guard have an undefined interval path when absent.

```md
⚠️ Gap · `discount` is optional · interval applies only when present
🔬 When present: `[0 .. 50.00]` · when absent: no value, no interval
`totalPrice − discount` requires presence guard before arithmetic
```

**Reading:** Line 1 states both the gap type (presence uncertainty) and its interval implication. Line 2 shows the dual-path interval: meaningful range when present, nothing when absent. Line 3 names the specific expression at risk and what's needed.

**Relationship to presence proof:** This card variant combines an interval gap with a presence gap. If both `PRE0116` (presence not confirmed) and an interval gap fire on the same expression, the diagnostic squiggle card wins (routing rule 1). This template applies when no diagnostic has fired but the hover is on the field declaration itself, showing its latent risk.

---

## 5. Interval Propagation Chains

For multi-operator expressions, show the chain compactly. Expand only when proof is the user's question.

### Compact (default — 3-line budget):

```md
✅ Proven · `(basePrice − discount) × taxRate` result `[−400 .. 1 600]` fits `netFee`
🔬 `[0 .. 8 000]` − `[0 .. 2 000]` → `[−2 000 .. 8 000]` × `[0.05 .. 0.20]` → `[−400 .. 1 600]`
Target declared: `min −500 max 2 000` · proven safe
```

### Expanded (lines 4–5, when the proof chain is the question):

```md
✅ Proven · `(basePrice − discount) × taxRate` result `[−400 .. 1 600]` fits `netFee`
🔬 Step 1: `basePrice − discount` → `[0 .. 8 000]` − `[0 .. 2 000]` = `[−2 000 .. 8 000]`
🔬 Step 2: `(intermediate) × taxRate` → `[−2 000 .. 8 000]` × `[0.05 .. 0.20]` = `[−400 .. 1 600]`
Target `netFee` declared: `min −500 max 2 000`
`[−400 .. 1 600]` ⊆ `[−500 .. 2 000]` · ✅
```

**Propagation display rules:**
- Compact: show all steps on one `🔬` line using `→` as step separator
- Expanded: one `🔬` line per step, named Step N
- Always show the final result interval and the containment verdict (`⊆`) in expanded view
- Cap expansion at 5 lines total (lines 1–5 of the hover)

---

## 6. Routing Rules Extension

These additions layer on top of the existing routing rules in V7 § 4. Priority ordering is unchanged.

1. **Proof diagnostic span wins** — interval-overflow diagnostics (`NumericOverflowOnAssignment`) are proof diagnostics and win at rule 1.
2. **Smallest proof-bearing `TypedBinaryOp` wins** — if the cursor is on an arithmetic expression with an interval result, the interval proof-expression hover fires at rule 2.
3. **Field declaration hovers** — when the cursor is on a field with declared bounds and no expression is in scope, the standard field card shows bound information inline (Template 4.1 or 4.5).
4. **Unbounded field vs. bounded field** — both show on the field declaration hover; the unbounded template (4.5) fires whenever `Interval == null` or `Interval.IsUnbounded`.
5. **Optional field interval** — fires only on the field declaration itself when no expression hover is active and presence is uncertain. Does not replace the diagnostic squiggle hover when `PRE0116` is active.

---

## 7. Compactness Rules for Intervals

The V7 compactness contract applies unchanged: **design for 3 lines, use lines 4–5 only when proof is the user's actual question.**

| Scenario | Default lines | Expanded lines |
|----------|--------------|----------------|
| Declared bounds, proven | 3 | Not applicable |
| Declared bounds, gap | 3 | 5 (when expression detail matters) |
| Computed expression, proven | 3 | Not applicable |
| Computed expression, gap | 3 | 5 (when overflow arithmetic matters) |
| Unbounded field | 3 | Not applicable |
| Optional field | 3 | Not applicable |

**Line 1 always carries the verdict.** `✅ Proven` or `⚠️ Gap` — never buried.

**The repair hint belongs on line 3** for gap cases, not in an expanded-only view. Users need to know what to do without expanding. Expanded view adds the mathematical detail, not the instruction.

**Numbers in intervals:** Use actual numeric values, not code variable names. `[0 .. 50 000]` not `[minBalance .. maxBalance]`. The hover is showing the evaluated fact, not the declaration.

---

## 8. Gap Handling

Gaps in interval proofs fall into three categories. Each has a distinct repair suggestion:

| Gap type | Cause | Line-3 repair hint |
|----------|-------|--------------------|
| **Unbounded operand** | Field has no declared bounds | Declare `min` / `max` on the unbounded field |
| **Result escapes bound** | Arithmetic result interval exceeds target | Tighten operand bounds OR add guard that narrows the input |
| **Presence uncertainty** | Optional field used in arithmetic without guard | Add presence guard before arithmetic |

**No new diagnostic shown on gap hovers.** Gap hovers in this design are for the field declaration and expression spans. When a diagnostic is active (`NumericOverflowOnAssignment`), the diagnostic squiggle hover fires instead (routing rule 1) and carries the `PRE` code inline, consistent with existing rule/ensure gap cards.

---

## 9. V1 Boundary

**Available in V1 (when interval proof ships):**
- `PreceptField.Bounds` — declared `Interval` on the field
- Proof result: whether the interval obligation was satisfied or not
- The specific gap type (unbounded operand vs. result overflow vs. presence uncertainty)
- Operand intervals for the failing expression

**Not available in V1:**
- Step-by-step chain rendering (expanded propagation view) — requires the solver to expose its intermediate steps, not just the final result
- Cross-field bounds expressions (Tier 3 inference)
- Guard-narrowed interval display (guard narrows `x`'s lower bound mid-path)
- Per-operand gap breakdown when more than two operands contribute to overflow

**V1 hover behavior:** Cards show Template 4.1–4.5 (compact, 3-line). Expanded propagation chains (§ 5 Expanded view) are a V2 surface, contingent on solver exposing intermediate intervals.

---

## 10. Design Decisions

| # | Decision | Rationale |
|---|----------|-----------|
| D1 | `[lo .. hi]` two-dot notation | Distinct from Precept's range syntax; readable without ambiguity in markdown |
| D2 | No new badges | `🔬` covers interval arithmetic; `⚖️` covers declared bounds as a comparison contract — the existing vocabulary is sufficient |
| D3 | Repair hint on line 3 (not expanded-only) | Users need to know what to do on first glance; expanding should reveal math, not instructions |
| D4 | Unbounded field shows `⚠️ Gap`, not `⚡ Enforced` | Runtime check is a fallback, not a static guarantee; the hover reflects proof status, not safety net existence |
| D5 | Optional+interval uses combined gap template | Presence and interval are distinct concerns but co-occur naturally; one card handles both when no diagnostic is active |
| D6 | V1 shows compact only; expanded is V2 | Step-by-step chain requires solver to expose intermediate values; V1 ships proof results, not solver internals |
