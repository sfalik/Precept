# Frank-23 Response — Interpolation Plan Redesign Complete

**From:** Frank  
**To:** Shane (via Copilot coordinator)  
**Date:** 2026-05-11  
**Re:** Revised `docs/Working/interpolation-plan.md` — type-grammar-driven slot classification

---

## Core Architectural Decision

**Type-grammar matching replaces position-text heuristics.** Each type that supports interpolation defines a closed, finite set of valid `(TextSegment | HoleSegment)*` patterns. The type checker matches the parsed segment sequence against the target type's pattern table, assigns slot identities to holes on match, and emits a structural error on no match.

This is the right design because:
- The type checker already knows the target type from context-sensitive resolution (§3.3).
- The valid interpolated forms per type are small and finite (at most 8 for `price`/`exchangerate`).
- Slot identity is inherently type-dependent — `'{x} {y}/{z}'` means magnitude/currency/unit for `price` but magnitude/from-currency/to-currency for `exchangerate`. No amount of text examination can distinguish these without the target type.
- The canonical docs (`business-domain-types.md` §1289) already enumerate exactly these patterns. The implementation mirrors the spec.

---

## What Changed from the Previous Plan

| Area | Previous (frank-16/18) | Revised (frank-23) |
|------|------------------------|---------------------|
| **Slot classification** | Position-text heuristics ("hole precedes unit → magnitude") | Type-grammar matching against per-type valid-form tables |
| **Structural validation** | None — invalid forms fell through as type mismatches | Explicit `InvalidInterpolatedTypedConstantForm` diagnostic (code 120) |
| **Type coverage** | `money`, `quantity`, `price`, `currency`, `unitofmeasure` | All 19 typed constant types exhaustively addressed |
| **Temporal types** | Not covered | `duration`/`period` with compound forms; `date`/`time`/`instant`/`datetime`/`zoneddatetime`/`timezone` explicitly prohibited with rationale |
| **Multi-hole forms** | Mentioned but not specified | Full grammar patterns for 2-hole (`'{Amt} {Curr}'`) and 3-hole (`'{Rate} {Curr}/{Unit}'`) forms |
| **Compound period** | Not covered | `'{n} years + {m} months'` — compound forms with magnitude holes and literal ` + ` bridges |
| **Diagnostic codes** | 1 code (120) | 3 codes: 120 (structural form error), 121 (interpolation unsupported for type), 122 (hole type mismatch) |
| **Test matrix** | 4 test categories | Comprehensive matrix: structural errors, unsupported types, hole type mismatches, string exception, valid combinations, whole-value forms |
| **Slice 1 (Parser)** | Unchanged | Unchanged (confirmed correct) |
| **Slice 2 (TypeChecker)** | Walk-and-classify per hole | Match-then-check: match full segment sequence against type grammar first, then check hole types |

---

## Open Questions Requiring Shane's Input Before Slice 1

1. **Temporal magnitude teachable messages:** Should `decimal`/`number` in temporal magnitude slots get a specialized teachable message about Decision #28, or use the generic mismatch message?

2. **Compound temporal unit holes:** `'{n} {u1} + {m} {u2}'` is prohibited because `+` semantics depend on knowing unit names. Permanent prohibition, or Phase 2 candidate?

3. **Diagnostic code numbering:** Three codes allocated at 120/121/122. Confirm or redirect.

---

## Status

Plan is complete and written to `docs/Working/interpolation-plan.md`. Slice 1 (Parser) remains unchanged and correct. Slice 2 (TypeChecker) is fully redesigned. Awaiting Shane review before any implementation begins.
