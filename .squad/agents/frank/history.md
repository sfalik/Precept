## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for the Precept DSL and runtime.
- Catalog metadata remains the language truth; parser, analyzer, evaluator, tooling, and docs should derive from durable metadata rather than enum-identity switches or parallel keyword lists.
- Constructor-semantics work stays complete only when docs, diagnostics, samples, and downstream tooling surfaces match shipped behavior.

## Live Guidance

- Quantity normalization still has two durable lanes: compile-time normalization for declarations/literals and runtime normalization for ingress values; both should stay on shared normalizer logic.
- `TypedField` remains the normalization handshake between analysis and execution: authored bounds stay available for display, normalized bounds feed proof/comparison surfaces.
- Comparison/equality checking must stay as strict about explicit counting-unit identity as assignment is about constrained qualifier axes.
- When the grammar can make an invalid form impossible, do that instead of inventing a later semantic ban.
- Documentation updates for a shipped feature must verify against the actual source and validation run; stale tooling builds are ops drift, not spec truth.

## Durable Learnings

- Any claim that work happens "only at compile time" must be stress-tested against Fire/Update/Restore ingress paths.
- Construction row syntax is now declaration-driven: `initial` lives only on event declarations, while authored rows are bare `on <Event>` and the type checker classifies construction from event metadata.
- Graph analysis for construction must stay semantic, not topological: construction handlers do not generate graph edges, PRE0081 must consult construction handlers, and `GraphEvent.IsInitial` must come from event metadata.
- Hollow-entity validation should be shared across all pre-materialization expression lanes, not re-added slot by slot.
- Formal grammar production rules must reflect structural exclusion decisions immediately; the grammar doc is a design deliverable, not follow-up cleanup.

## Historical Summary

- 2026-05-12 through 2026-05-16 concentrated Frank's work around hover contract reviews, field-state guarantees, constructor semantics, reject-surface structure, interval-proof design, quantity normalization, diagnostic-enforcement architecture, and counting-unit comparison gaps.
- The constructor/reject track settled three durable ideas: `on <Event>` is the honest construction surface, fallback `reject` is valid authored refusal rather than misuse, and grammar-level structural exclusion is preferred whenever the language already knows a path is impossible.
- Detailed batch-by-batch chronology now lives in `.squad/decisions.md` and `history-archive.md`; this file keeps only the guidance and latest durable closeout.

## Recent Updates

### 2026-05-16T13:08:43Z — Constructor semantics batch closed end-to-end

- Frank's graph-analyzer passes locked the durable analyzer model: construction handlers live in `EventHandlers`, do not create topology edges, and require semantic handling for PRE0081 and `GraphEvent.IsInitial`.
- George completed Slice 8b at commit `c72db9b0`, removing row-level `initial` and making construction classification metadata/type-check driven.
- Kramer completed Slices 9+10 at commits `ec5525d2` and `e19736f6`, aligning hover and grammar generation with declaration-level `initial` semantics.
- Newman completed Slice 11, adding `isConstruction` to the MCP compile event-handler DTO surface without duplicating core logic.
- Frank completed Slice 12 docs/sample closeout: the language spec and constructor-semantics tracker are current, `CHANGELOG.md` records the shipped surface, and `samples/Test.precept` was locally verified while the stale MCP result was correctly treated as deployment drift.

