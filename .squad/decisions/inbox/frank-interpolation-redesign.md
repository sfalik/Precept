# Frank-23 — Interpolation Plan Redesign Required

**From:** Copilot (routing Shane's review)  
**To:** Frank  
**Date:** 2026-05-11  
**Re:** `docs/Working/interpolation-plan.md` — design incomplete, redesign needed before Slice 1

---

## Decision Needed

The interpolation plan (frank-16, frank-18) is **not ready for implementation**. Shane's review against the canonical docs found structural gaps. A redesign is required before Slice 1 (Parser) begins.

---

## What the Canonical Docs Define

**`docs/language/business-domain-types.md` §1289** is the authoritative interpolation reference. It defines valid interpolated forms per type:

| Type | Static | Magnitude hole | Qualifier hole | Both |
|------|--------|---------------|----------------|------|
| `money` | `'100 USD'` | `'{Amt} USD'` | `'100 {Curr}'` | `'{Amt} {Curr}'` |
| `quantity` | `'5 kg'` | `'{Wt} kg'` | `'5 {Unit}'` | `'{Wt} {Unit}'` |
| `price` | `'4.17 USD/each'` | `'{Rate} USD/each'` | `'4.17 {Curr}/{Unit}'` | `'{Rate} {Curr}/{Unit}'` |
| `exchangerate` | `'1.08 USD/EUR'` | `'{Rate} USD/EUR'` | `'1.08 {From}/{To}'` | `'{Rate} {From}/{To}'` |

**`docs/language/temporal-type-system.md`** adds:
- `duration`/`period`: `'{GraceDays} days'`, `'30 {unit}'`, `'{n} years + {m} months'`

---

## Gap 1 — `'1 {x} kg'` Is Not a Valid Form

The current plan lists `'1 {x} kg'` as a test case. This form does not appear anywhere in the canonical docs and has no semantic slot:
- `'1 kg'` is already complete — magnitude + unit both present as literals
- A hole between them assembles to `1 <x> kg` — structurally invalid for any type
- This is a **structural error**, not a type mismatch

The plan must explicitly define what constitutes a structurally valid interpolated typed constant form for each type, and reject forms that don't match. `'1 {x} kg'` should be a compile error with a clear diagnostic.

---

## Gap 2 — Compound Qualifier Holes (price, exchangerate)

For `price` (`'4.17 {Curr}/{Unit}'`) and `exchangerate` (`'1.08 {From}/{To}'`), the qualifier is compound — `currency/unit` or `from-currency/to-currency`. Two holes appear in the qualifier part, separated by a literal `/`.

Frank's three-slot model (magnitude / unit / whole-value) does not account for:
- Two independent holes in a compound qualifier
- The `/` separator being a literal text segment between two qualifier holes
- The slot identity of each hole within the compound (first = currency, second = unit — not inferrable from text position alone)

For `price`, the compiled form `'{Rate} {Curr}/{Unit}'` has:
- Hole 1: magnitude slot
- TextSegment: ` ` (space)
- Hole 2: currency slot
- TextSegment: `/`
- Hole 3: unit slot

The position-text approach cannot distinguish hole 2 (currency) from hole 3 (unit) without knowing the target type's grammar.

---

## Gap 3 — Temporal Types (duration, period, compound period)

`duration` and `period` are quantity-like (`<integer> <unit-name>`) and need to appear in the per-type compatibility tables. Additionally:

- **Compound period:** `'{n} years + {m} months'` has TWO magnitude holes with a literal ` + ` separator between them. This is a multi-hole, multi-segment case with no analogue in the current plan.
- **Unit hole for temporal:** `'30 {unit}'` where `unit` must be a valid temporal unit name (`days`, `hours`, `months`, etc.) — what types are valid in this slot?

---

## Gap 4 — Single-Component and Formatted Types

Types with no magnitude/qualifier structure:
- `currency`, `unitofmeasure`, `dimension` — only whole-value slot is valid
- `date`, `time`, `instant`, `datetime`, `zoneddatetime`, `timezone` — formatted strings; the spec says typed constants universally support interpolation, but no interpolation examples exist for these types. Is `'{y}-04-15'` valid for `date`? This is undefined.

The plan must either define valid interpolated forms for these types, or explicitly state that interpolation is not supported and emit a diagnostic.

---

## Core Design Problem — Position-Text Detection Is Type-Unaware

Frank's current approach detects slot position by examining text fragments:
- "hole precedes a unit fragment" → magnitude slot
- "hole follows a numeric fragment" → unit slot

This breaks down for:
- `price` / `exchangerate`: the qualifier structure is type-specific (`currency/unit`), not inferrable from surrounding text
- Compound period: multiple magnitude holes separated by ` + `
- Any type where the literal form has no numeric fragment (e.g., `currency`, `timezone`)

**The required approach is type-grammar-driven:**
1. The type checker knows the target type
2. Each type has a defined grammar of valid interpolated forms (a small, closed set per type)
3. The assembled segments (TextSegment + HoleSegment sequence) are matched against the type's grammar
4. Valid matches assign a slot identity to each hole
5. Invalid sequences (including `'1 {x} kg'`) are structural errors — new diagnostic needed

---

## What the Redesigned Plan Must Provide

1. **Per-type valid form grammar** — a closed set of valid segment sequences for each type that supports interpolation. This is the slot-classification specification.

2. **Complete compatibility table** — valid hole types per slot, per type, including:
   - `money`: magnitude slot, currency slot, whole-value slot
   - `quantity`: magnitude slot, unit slot, whole-value slot  
   - `price`: magnitude slot, currency slot, unit slot, whole-value slot
   - `exchangerate`: magnitude slot, from-currency slot, to-currency slot, whole-value slot
   - `duration`/`period`: magnitude slot, unit slot, whole-value slot, compound-magnitude slot (for `+ `)
   - `currency`, `unitofmeasure`, `dimension`: whole-value slot only
   - `date`, `time`, `instant`, `datetime`, `zoneddatetime`, `timezone`: define or explicitly prohibit

3. **Structural validity diagnostic** — a new `DiagnosticCode` for structurally invalid interpolated form (e.g., `'1 {x} kg'` for any type)

4. **`string` exception** — carries forward from frank-16; `string` is valid in any hole position

5. **Multi-hole forms** — explicit treatment of `'{Amt} {Curr}'` (two holes, no literal separator other than space) and `'{Rate} {Curr}/{Unit}'` (two holes with literal `/`) and `'{n} years + {m} months'` (two holes with literal ` + `)

6. **Test matrix** — one test per valid form per type, plus structural error cases

---

## Constraint

The parser layer is structure-blind — it produces TextSegment + HoleSegment sequences without knowing the target type. Type-grammar matching and slot classification belong entirely in the type checker. The parser redesign from frank-16 (Slice 1) is still correct as specified; the redesign work is in Slice 2 (TypeChecker) and the new structural validity diagnostic.

---

## Response Required

Please revise `docs/Working/interpolation-plan.md` with:
- Per-type valid form grammar (closed set)
- Complete slot compatibility tables for all typed constant types
- Treatment of formatted temporal types (define or prohibit)
- Structural validity diagnostic spec
- Updated test matrix

Do not proceed with Slice 1 implementation until this response is filed and Shane approves the revised plan.
