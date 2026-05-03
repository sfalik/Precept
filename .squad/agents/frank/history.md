## Core Context

- Owns language research, spec wording, and cross-surface documentation for Precept's DSL and architecture.
- Converts owner/design decisions into implementable guidance for parser, catalog, type-checker, runtime, and tooling work.
- Durable active baseline: catalogs remain the language truth; generic consumer flow dispatches by metadata/shape instead of construct identity; CC#1 keeps a sealed typed-expression DU while the broader parser/runtime surface stays metadata-driven.
- CC#25 runtime baseline is now fixed for current planning: production Fire uses Option A + G (`PreceptValue` tagged value storage plus catalog-owned delegate arrays), while LS/MCP interactive tooling keeps traced tree-walk evaluation.
- TypeBuilder/source-generation paths remain recorded as analyzed alternatives, not the active SaaS architecture, unless deployment or inspectability constraints change.

## Learnings

- **CC#25 Fire data lifecycle walkthrough (2026-05-03):** Peak live footprint for one Fire under A+G is ~44-48 `PreceptValue` slots, total stack traffic is ~4,480 bytes, the working copy is the donated next-version slot array, and pooled arrays cut GC-visible allocation to the boundary objects. The remaining implementation questions are slot-array ownership transfer, eval-stack allocation strategy, JSON ingress/egress ownership, event-args representation, trace-path data structures, and multi-row working-copy pooling.
- **CC#25 final runtime recommendation (2026-05-03):** The real performance lever is representation, not dispatch. Replace boxed `object?` hot-path values with a 32-byte `PreceptValue` tagged struct and keep execution semantics on catalog-owned delegate arrays. `System.Linq.Expressions` stays an upgrade seam, not a v1 dual-path commitment.
- **CC#25 SaaS constraint resolution (2026-05-03):** TypeBuilder/source-generated CLR types only win under a different product shape. In the current SaaS, per-definition cold-start and loss of fine-grained inspectability outweigh warm-path throughput gains.
- Catalog schema diagram work (2026-05-03) produced a three-level visual section in `docs/language/catalog-system.md` with 13 catalogs in scope, `ConstructSlotKind` treated as support schema rather than a catalog, and Elaine owning the rendering while Frank owns the architectural message.
- LS enrichment features (did you mean? / code actions) require three catalog structure changes before LS implementation: `Diagnostic.Args`, `DiagnosticMeta.SuggestionSources`, and `ConstructMeta.ModifierDomain`; classification axes like `SuggestionSource` and `ModifierDomain` stay bare enums.
- The `tree` variable/type-name sweep confirmed the durable naming boundary: use `ConstructManifest` / `manifest` for the flat Precept parser artifact, while legitimate Roslyn `SyntaxTree`, parse-tree prose, and graph-theory tree language remain untouched.
- `docs/compiler-and-runtime-design.md` is the narrative overview layer over the canonical stage docs; it inherits open questions rather than silently resolving them, and `SemanticIndex` must stay framed as a flat semantic inventory rather than an annotated syntax tree.
- Gap-register deprecation (2026-05-03) is final: discovery registers were archived, unresolved gaps moved into canonical docs as Open Questions, and `docs/working/cross-cutting-decisions.md` is now the sequencing/ownership driver.
- **CC#1 resolved (2026-05-03):** `ParsedExpression` and `TypedExpression` are sealed DUs, the expression tree is the only strongly typed parser output layer, and exhaustiveness relies on sealed-hierarchy switches plus the annotation-bridge pattern for distributed dispatch.

## Recent Updates

### 2026-05-03T22:22:27Z — CC#25 corpus canonicalized
- Scribe merged 19 CC#25 inbox files into 7 durable ledger entries, deleted the processed inbox notes, and recorded the active runtime baseline as `PreceptValue` + catalog-owned delegate dispatch with TypeBuilder and lane-split alternatives explicitly closed.
- The Fire-call lifecycle walkthrough is now part of Frank's active context as the quantitative implementation baseline for A+G memory/ownership work.

### 2026-05-03T16:44:09Z — Gap-register deprecation and wave driver recorded
- Frank-38 restructured `docs/working/cross-cutting-decisions.md` into the wave-ordered execution driver (Waves 0-5, 26 decisions, ownership labels), archived the two working gap registers, and migrated their unresolved content into canonical docs as inline Open Questions.
- Durable baseline: separate gap registers are retired; new gaps belong directly in the relevant canonical doc, while sequencing and ownership routing now live in `docs/working/cross-cutting-decisions.md`.
- Specific closeout: missing gap #55 (`GraphEvent.IsInitial` derivation) was added to `docs/compiler/graph-analyzer.md`, and the deprecation rationale is now captured in the decision ledger.

### 2026-05-03T15:18:05Z — Catalog diagram baseline and ownership routing recorded
- Frank-34's research memo is now the durable baseline for schema-diagram work: the live catalog system is 13 catalogs because `ExpressionForms` is in scope, and `ConstructSlotKind` is supporting schema rather than a catalog.
- User routing directive updated: Elaine owns both Mermaid and ASCII diagram rendering. Frank remains the architectural analyst/decision source for what the diagrams should communicate.
- The because-clause ledger closeout is also recorded: grammar docs already match the separate `EnsureClause` + `BecauseClause` slot anatomy, and George's optional-slot follow-up closed the last catalog-red defect.

