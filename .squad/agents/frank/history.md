## Core Context

- Owns language research, spec wording, and cross-surface documentation for Precept's DSL and architecture.
- Converts owner/design decisions into implementable guidance for parser, catalog, type-checker, and tooling work.
- Durable active baseline: catalogs remain the language truth; generic consumer flow should dispatch by `SlotValue` shape instead of construct identity; Option F-style generic parse output is acceptable for consumers, but any accessor layer stays deferred until a concrete need exists.

## Learnings

- Keep catalog metadata as the single language source of truth; parser/tooling sets and per-member consumer switches are mirrored truth unless the catalog cannot express the distinction.
- Grammar `Tag(...)` captures are the slot system in the radical parser design; do not reintroduce a parallel `ConstructMeta.Slots` field once the grammar tree carries named captures.
- The surviving argument for incremental parser change over rebuild is design-risk sequencing, not schedule; AI-assisted throughput weakens time-cost arguments but does not erase unresolved architecture gaps.
- Outcomes require metadata when outcome-level meaning cannot be recovered from token categories alone; `no transition` is the durable example of composition that must live in catalog data.
- The catalog-driven thesis now reaches the upstream pipeline too: lexer tables are already catalog-fed, the radical parser is mostly catalog-driven above the Pratt kernel, and the builder is the cleanest proof-of-concept stage if `ModelContribution` metadata is added.

## Recent Updates

### 2026-05-03T02:52:51Z — Catalog-driven pipeline follow-through recorded
- Scribe merged Frank's consumer-architecture note plus Shane's accessor-layer ruling into the canonical ledger: keep consumers generic, keep MCP above raw parse output, and treat any accessor layer as YAGNI until a real caller proves otherwise.
- Scribe also recorded Frank's upstream coverage pass: lexer/parser/builder now sit inside the same catalog-driven pipeline thesis, with the builder identified as the strongest candidate for a first generic proof-of-concept stage.
- Detailed prior active-history entries were compacted into `history-archive.md` during this pass to bring Frank back under the 15 KB gate.

### 2026-05-03T01:34:25Z — Radical AST options note recorded
- The pending-owner-ruling record now keeps Option F (generic `ParsedConstruct` internals + thin typed accessors at boundaries) as the preferred radical AST path, with source generation as the explicit fallback if ergonomics win.

### 2026-05-03T01:07:30Z — Outcomes catalog reversal recorded
- The durable parser/type-checker rule remains: outcomes use the two-level catalog pattern while retaining `OutcomeNode` as the syntax-layer DU because `no transition` is an outcome-level abstraction that token categories cannot enumerate by themselves.

### 2026-05-02T22:22:24Z — Iteration 11 audit session recorded
- Keep the audit baseline in mind: the doc/catalog gap set now centers on declaration-shape metadata lag, queue-by clarification, and the canonical checker implementation gate already locked in `docs/compiler/type-checker.md`.
