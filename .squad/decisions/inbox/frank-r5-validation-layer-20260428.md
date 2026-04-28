# Frank R5 — Validation Layer Decisions

**Date:** 2026-04-28
**By:** Frank (Language Designer / Compiler Architect)
**Source:** `docs/working/catalog-parser-design-v5.md`

---

### F7: `_slotParsers` is an exhaustive switch, not a dictionary

**Decision:** Replace the `_slotParsers` `FrozenDictionary<ConstructSlotKind, Func<SyntaxNode?>>` with an exhaustive switch method `InvokeSlotParser(ConstructSlotKind)`. CS8509 enforces completeness at build time.

**Rationale:** Shane's directive (`.squad/decisions/inbox/copilot-directive-extensibility-validation-20260427.md`) requires that adding a new `ConstructSlotKind` without a parser fails loudly — at compile time if possible. A dictionary lookup fails at runtime with `KeyNotFoundException`; a switch fails at build time with CS8509. The `ConstructSlotKind` enum is closed (no runtime registration, no plugin extensibility), so a switch is the correct pattern for a closed exhaustive mapping.

**Supersedes:** F2 (dictionary for `_slotParsers`).

**Alternatives rejected:** (A) Dictionary + startup-time assertion — catches the gap at test time, not build time. (B) Dictionary + Roslyn analyzer — adds custom analyzer infrastructure for a problem the language already solves (CS8509). Both violate Shane's "fail at compile time if possible" directive when a zero-cost compile-time solution exists.

**Tradeoff:** The switch dispatches on call (not at construction time). In practice, tests exercise every slot kind, so both approaches catch gaps at test time. But only the switch catches gaps at build time.

---

### F8: Introduce `ConstructSlotKind.RuleExpression` — G5 resolution

**Decision:** Add `ConstructSlotKind.RuleExpression` for the rule body expression. Update `RuleDeclaration` slot sequence from `[GuardClause, BecauseClause]` to `[RuleExpression, GuardClause(optional), BecauseClause]`.

**Rationale:** George's G5 identified a real bug: `RuleDeclaration` maps its primary boolean expression to `GuardClause`, but `ParseGuardClause()` expects `When` as its introduction token. The rule body has no introduction token — it starts immediately after `rule`. Additionally, the optional `when Guard` part of the rule was missing from the slot sequence entirely.

**`ParseRuleExpression()` contract:** No introduction token. Calls `ParseExpression(0)` directly. The Pratt parser naturally stops at `when` or `because` (neither has binding power in the expression grammar). This is the one slot parser that does NOT check for an introduction token before parsing.

**Alternatives rejected:** (A) Make `ParseGuardClause()` context-aware (check for `When` OR parse directly) — violates the principle that each slot parser has exactly one introduction-token contract. (B) Accept the naming inconsistency — `GuardClause` meaning "primary boolean expression" for rules would confuse every future implementer.

---

### F9: Reject pre-event guard in `from ... on` with diagnostic

**Decision:** Withdraw F4 (two-position `when` guard for TransitionRow). The disambiguator consumes `when` unconditionally before disambiguation (required by G1 for `In`-led constructs). When a pre-consumed guard appears in a `From`-led construct that routes to `TransitionRow`, the parser emits a diagnostic: "Guard must follow the event name in transition rows."

**Rationale:**
1. The spec explicitly excludes pre-event guard: "except `from ... on`, where the guard is inside the transition row after the event name" (spec § 2.2).
2. Zero out of 28 sample files use pre-event guard position.
3. Post-event guard is semantically more precise: `from State on Event when Guard` conditions the guard on the event, not on the state.
4. The disambiguator's unconditional `when` consumption is still mandatory for `In`-led constructs (`in State when Guard write Field`). The diagnostic fires only for the `From` + `On` specific case.

**Recommendation to Shane:** The spec is correct as written. No language expansion needed. The parser provides a helpful diagnostic and error-recovers by placing the guard in the correct slot anyway.

**Alternatives rejected:** (A) Accept both positions (language expansion) — adds surface area with no user demand and ambiguous semantics. (B) Don't consume `when` for `From`-led constructs — breaks the generic disambiguator's uniformity and requires per-preposition special-casing.

---

### F10: `EnsureClause` and `BecauseClause` remain separate slots

**Decision:** Keep `EnsureClause` and `BecauseClause` as separate `ConstructSlotKind` values with independent slot parsers. The mandatory `because` coupling is enforced by `IsRequired: true` on the `BecauseClause` slot and by the type checker, not by merging the parsers.

**Rationale:** Slot parsers are minimal grammar producers. Each slot is independently parseable. Merging `ensure + because` into one compound slot would couple expression parsing with string parsing and lose the composability of the slot system. The `BecauseClause` slot's `IsRequired: true` flag means the generic slot iterator emits a diagnostic when it's missing — this is equivalent to a grammar-level enforcement but stays within the metadata-driven architecture.

**Alternatives rejected:** Merged `ParseEnsureWithReason()` — tighter coupling, loses slot independence, no correctness benefit.
