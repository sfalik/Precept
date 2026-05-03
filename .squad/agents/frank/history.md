## Core Context

- Owns language research, spec wording, and cross-surface documentation for Precept's DSL and architecture.
- Converts owner/design decisions into implementable language-surface guidance for parser, catalog, type-checker, and tooling work.
- Active durable baseline: catalogs remain the language truth; parser/tooling behavior should derive from them rather than hardcoded parallel tables.

## Learnings

- **Superpower lineage (2026-05-02):** The original Precept runtime was built on Superpower (Nicholas Blumhardt's .NET parser combinator library). The grammar's flat, keyword-anchored, slot-sequential shape was designed for a combinator model from the start — not discovered after the fact. The radical catalog-driven parser design is a return to that architecture, not a departure; the key innovation is moving the combinator tree from C# source code into `ConstructMeta.Grammar` so the grammar becomes an inspectable catalog artifact rather than a buried implementation detail.

- **Grammar Primer (2026-05-02):** Added §0 "The Grammar of Precept" to both `parser-radical.md` (full, with examples) and `type-checker-radical.md` (summary + cross-reference). Sample files drawn from: `trafficlight.precept` (simple state/field/transition), `insurance-claim.precept` (decimal types, set collections, family disambiguation with `in`), `loan-application.precept` (complex guards, event args, rules). Key patterns illustrated: Leader→Slot→Slot shape, five slot types (identifier, type-ref, expression, modifiers, action-chain), family disambiguation via secondary token peek, combinator mapping for `state` (trivial) and `event-ensure` (moderate). No novel grammar patterns discovered — all examples confirmed the flat/keyword-anchored/slot-sequential thesis holds universally across 28 sample files.

- The `ImmutableArray<ConstructSlot> Slots` field on `ConstructMeta` is vestigial in the radical parser design. In the current parser, the slot list IS the grammar (the parser iterates it). In the radical design, `Tag("name", rule)` nodes in the `Grammar` combinator tree ARE the named captures — they are the slots, inline and executable. Keeping a separate `Slots` field creates a parallel representation of the same truth that can diverge. Drop the field; derive named-capture lists from the grammar tree via `ExtractNamedCaptures(grammar)` at startup. The concept of "slot" survives; the separate catalog field does not.
- **LOCKED (2026-05-02):** Separate `Slots` field removed from `ConstructMeta` in the radical parser design. `ExtractNamedCaptures(ParseRule grammar)` is the runtime extraction path — walks `Tag` nodes, returns `ImmutableArray<string>`. Shane confirmed; design doc updated. No parallel field, no divergence risk.

- Time-cost arguments must be calibrated to actual team velocity. When the team builds with AI and the original parser took 2 days, citing "1-2 weeks" for a rebuild is indefensible. Distinguish time cost (AI collapses it) from design risk (AI doesn't solve open design questions) and regression risk (AI helps iterate but can't eliminate implicit knowledge loss). The surviving argument for incremental over rebuild is risk profile, not schedule.
- Research validation now has a durable pattern: validation artifacts live in research/language/, design docs cite them via `## Research Validation`, working drafts are marked superseded in place, and research/language/README.md indexes the promoted validations.
- The canonical checker design is implementation-ready only with its locked shape constraints intact: pre-Slice 0 typed records first, array-primary field ordering, single-hop widening, deterministic overload resolution, and slice-by-slice `[HandlesCatalogMember]` migration from stub to real handlers.
- Kramer's R3 objection is durable architecture: do not mirror event-arg data into TypedTransitionRow.ResolvedArgs; derive from canonical typed models instead.
- GAP-046 proved the language-surface rule again: if the spec presents ~startsWith / ~endsWith as real functions, they belong in FunctionKind and FunctionMeta, not only in parser-side CI routing.
- Iteration 10 audits added two lasting parser rules: variant-action arms inside shape-specific parser methods must throw when unreachable, and every parser-facing catalog should expose an O(1) metadata index keyed to the lookup axis the parser actually uses.
- GAP-047 is the durable reminder that spec shorthand like "numeric" must stay scoped to primitive numeric lanes when domain types such as money and quantity have qualifier-preserving overloads.

## Recent Updates

### 2026-05-03T00:15:16Z — Radical parser slot field removed
- The radical parser doc now removes `ImmutableArray<ConstructSlot> Slots` from `ConstructMeta`; named parse positions live only as `Tag` nodes inside `Grammar`.
- Tooling/documentation should derive ordered capture names via `ExtractNamedCaptures(ParseRule)` at startup rather than maintain a parallel slot list.
- The parser rebuild recommendation remains Path C only on risk grounds; AI velocity collapses the schedule argument, so unresolved stashed-guard, split-modifier, and variant-action gaps are the only surviving case.

### 2026-05-02T19:11:32-04:00 — Spec challenge response: implicit-knowledge argument withdrawn
- Shane challenged the `implicit grammar knowledge'' regression argument: if the parser was built from spec with AI, a rebuild from the same spec reproduces the same result; any divergence is a spec gap, not a reason to preserve code.
- Conceded fully. The regression argument is dead. Path C recommendation now rests solely on the three unsolved design gaps (stashed-guard, split-modifier, variant-action). If those are resolved on paper or shown to be spec-covered, Path C has no remaining case.
- Response written to docs/working/frank-spec-challenge-response.md.

### 2026-05-02T22:22:24Z — Iteration 11 audit session recorded
- Scribe merged Frank's iteration-11 findings into the canonical ledger, cleared all current decision inbox files, and wrote the audit closeout logs.
- Cross-agent context to retain: the durable batch now bundles the spike type-checker directive, both catalog-driven type-checker reviews, GAP-047 closure, Frank's GAP-048–056 doc/catalog gaps, and George's GAP-062–067 catalog-impl gaps.
- Health gate result: decisions archive ran under the 7-day rule before merge; no history summarization was required after propagation.

### 2026-05-02T22:22:24Z — Iteration 11 doc/catalog audit pass
- Filed GAP-048 through GAP-056 in the language-consistency ledger; Frank's pass added 9 unresolved doc/catalog gaps. Combined ledger state now stands at 64 total gaps, 49 fixed / 15 unresolved after the parallel iteration 11 catalog-impl pass.
- The dominant pattern is catalog lag behind the spec on declaration-shape metadata: guarded ensures, guarded state actions, and stateless event-hook trailing `ensure` all exist in the spec without matching `Constructs`/`Constraints` metadata.
- Queue-by semantics now need owner clarification in two places: whether `ascending` / `descending` belongs in the type catalog, and whether `dequeue ... by H` means keyed selection or something else.

### 2026-05-02T21:58:21Z — Canonical type checker batch closed
- Frank's canonical response is now durable end-to-end: George's 5 concerns were accepted, 3 of 4 missing items were accepted, transitive widening stayed rejected, and the checker plan now marks all 11 slices implementation-ready.
- Cross-agent follow-through is part of the active baseline: Kramer's tooling review remains non-blocking but derivation-first, and Soup-Nazi's 450-550 test estimate plus 3 non-negotiable gates define the expected checker validation bar.

### 2026-05-02 — Active focus snapshot
- Immediate open design work has shifted back to checker implementation: GAP-047 is now closed, while the rest of the checker shape questions are locked in docs/compiler/type-checker.md.
- Use docs/working/type-checker-research-crossref.md, docs/working/kramer-tooling-review.md, and docs/working/soup-nazi-test-strategy-review.md as the supporting context set behind the canonical checker doc.

### 2026-05-02 — Historical Summary (fully compacted)
- Older active-history detail was moved to history-archive.md during Scribe closeout to keep Frank under the 15 KB gate.
- Use the archive for the earlier Dapr research notes, gap-by-gap audit trail, and prior batch closeout sequence.

### 2026-05-02T22:14:44Z — GAP-047 closed
- Spec §3.7 now explicitly documents the money/quantity overloads for `min`, `max`, `abs`, `clamp`, and `round(value, places)`, including same-qualifier requirements and qualifier-preserving results.
- The working gap ledger is fully closed for this audit pass: GAP-047 is Fixed, and the primitive numeric-lane shorthand is now explicitly separated from domain-type overload semantics.
