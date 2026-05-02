## Core Context

- Owns language research, spec wording, and cross-surface documentation for Precept's DSL and architecture.
- Converts owner/design decisions into implementable language-surface guidance for parser, catalog, type-checker, and tooling work.
- Active durable baseline: catalogs remain the language truth; parser/tooling behavior should derive from them rather than hardcoded parallel tables.

## Learnings

- Research validation now has a durable pattern: validation artifacts live in research/language/, design docs cite them via `## Research Validation`, working drafts are marked superseded in place, and research/language/README.md indexes the promoted validations.
- The canonical checker design is implementation-ready only with its locked shape constraints intact: pre-Slice 0 typed records first, array-primary field ordering, single-hop widening, deterministic overload resolution, and slice-by-slice `[HandlesCatalogMember]` migration from stub to real handlers.
- Kramer's R3 objection is durable architecture: do not mirror event-arg data into TypedTransitionRow.ResolvedArgs; derive from canonical typed models instead.
- GAP-046 proved the language-surface rule again: if the spec presents ~startsWith / ~endsWith as real functions, they belong in FunctionKind and FunctionMeta, not only in parser-side CI routing.
- Iteration 10 audits added two lasting parser rules: variant-action arms inside shape-specific parser methods must throw when unreachable, and every parser-facing catalog should expose an O(1) metadata index keyed to the lookup axis the parser actually uses.
- Open doc/catalog follow-through remains GAP-047: money/quantity overload coverage in spec §3.7 still needs explicit treatment.

## Recent Updates

### 2026-05-02T21:58:21Z — Canonical type checker batch closed
- Frank's canonical response is now durable end-to-end: George's 5 concerns were accepted, 3 of 4 missing items were accepted, transitive widening stayed rejected, and the checker plan now marks all 11 slices implementation-ready.
- Cross-agent follow-through is part of the active baseline: Kramer's tooling review remains non-blocking but derivation-first, and Soup-Nazi's 450-550 test estimate plus 3 non-negotiable gates define the expected checker validation bar.

### 2026-05-02 — Active focus snapshot
- Immediate open design work is narrow: GAP-047 still needs spec coverage for domain-type overloads, while the rest of the checker shape questions are now locked in docs/compiler/type-checker.md.
- Use docs/working/type-checker-research-crossref.md, docs/working/kramer-tooling-review.md, and docs/working/soup-nazi-test-strategy-review.md as the supporting context set behind the canonical checker doc.

### 2026-05-02 — Historical Summary (fully compacted)
- Older active-history detail was moved to history-archive.md during Scribe closeout to keep Frank under the 15 KB gate.
- Use the archive for the earlier Dapr research notes, gap-by-gap audit trail, and prior batch closeout sequence.
