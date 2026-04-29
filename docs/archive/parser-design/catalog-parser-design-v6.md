# Catalog-Driven Parser: George Round 6 Review

**By:** George (Runtime Dev)
**Date:** 2026-04-27
**Status:** Design review Round 6 — v5 decisions confirmed, language change pending Shane input
**References:**
- `docs/working/catalog-parser-design-v5.md` — Frank's Round 5 (validation layer, extensibility contracts, G5 resolution)
- `docs/working/catalog-parser-design-v5-lang-simplify.md` — George's language simplification analysis
- `docs/language/precept-language-spec.md` — DSL spec (law)
- `docs/language/catalog-system.md` — metadata-driven catalog architecture

---

## §1: v5 Review — George's Verdict

All of Frank's Round 5 decisions confirmed:

**F7 (switch over dictionary for CS8509):** AGREE. Enum is closed. CS8509 at build time beats `KeyNotFoundException` at runtime. Shane's directive settles it.

**F8 (RuleExpression slot kind, no intro token):** AGREE. Slot sequence `[RuleExpression, GuardClause(optional), BecauseClause]` is correct. Pratt parser terminates naturally at `when`/`because` keywords (no left-binding power). Generic slot iterator doesn't need special handling — `ParseRuleExpression()` always returns a node (even `IsMissing`), so `IsRequired` check is satisfied trivially.

**F9 (pre-event guard withdrawn):** AGREE. Semantic reading is the clincher: `from Submitted on Approve when Verified` — the guard is on the approval, not the state. Correct. Diagnostic approach (consume unconditionally, error on From+On+stashed guard, place guard in post-event slot) is the right error recovery. Same `GuardClause` slot regardless of which position it was written in.

**F10 (EnsureClause + BecauseClause separate):** AGREE. Clean slot composability. `IsRequired` enforcement path is straightforward.

**Validation layer 4-tier design:** CONFIRMED closes the `_slotParsers` gap. `InvokeSlotParser` switch gives CS8509 enforcement for new `ConstructSlotKind` members. Startup tests catch orphan slot kinds and drift.

**One test nit for Frank R7:** Test 4 (`BuildNodeHandlesEveryConstructKind`) — calling `BuildNode` with null-filled slots may hit `NullReferenceException`/`InvalidCastException` in the cast before reaching any "unhandled kind" error. Assertion needs to distinguish `ArgumentOutOfRangeException` (actual gap) from null-propagation exceptions (expected with null slots). Minor, but the test won't reliably catch the intended gap without this fix.

**PR 1 scope with F8:** Still clean. `RuleExpression` enum member and `SlotRuleExpression` instance are pure catalog additions — no parser implementation needed. Land naturally with the `GetMeta()` rewrite.

---

## §2: Language Change Discussion — Pending Shane Input

George's language simplification analysis (`docs/working/catalog-parser-design-v5-lang-simplify.md`) identified one change worth discussing with Shane:

**Proposal: Move access mode guards from pre-verb to post-field position.**
```
# Current:
in UnderReview when DocumentsVerified write DecisionNote

# Proposed:
in UnderReview write DecisionNote when DocumentsVerified
```

**Parser impact:** Eliminates the pre-disambiguation `when` consumption from the disambiguator entirely. Drops from a 6-step stateful pre-parser to a clean 4-step token-match router. The single highest-leverage change available.

**User-visible cost:** 2 lines change across all 28 sample files. New form is more consistent with every other `when` guard in the language (transition rows, rules, ensures — all place the guard after primary content).

**Status: PENDING SHANE INPUT.** Shane was asked whether the proposed syntax reads naturally or whether he prefers the current form. No answer yet. This decision gates whether the disambiguator pre-consumption path stays in the design or can be removed.

**Note:** Shane's stated framing — "open to discussion, not necessarily making changes." The parser handles either form; this is a language ergonomics question, not a blocker.

---

## §3: Open Items for Frank R7

1. **Language change decision (L1):** Shane's input on guard position (pre-verb vs post-field) pending. Frank R7 should incorporate Shane's answer and update the disambiguator design accordingly if the change is accepted.
2. **Test nit (T1):** `BuildNodeHandlesEveryConstructKind` — tighten assertion to distinguish `ArgumentOutOfRangeException` from null-propagation.
3. **Design is otherwise stable.** Frank may begin authoring the implementation plan (PR sequence) once L1 is resolved.
