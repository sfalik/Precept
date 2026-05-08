# Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for Precept's DSL and runtime.
- Catalogs remain the language truth; runtime, tooling, and docs must derive behavior from metadata and durable shape rather than enum-identity switch logic or parallel lists.
- Public API surfaces expose stable CLR/JSON interchange contracts; evaluator internals stay internal.
- Investigation docs can be archived once their outcomes are captured in canonical docs, proposals, or the squad decision ledger.

## Learnings

- Closed-vocabulary syntax (types, modifiers, access modes) should be resolved by the parser at recognition time; only open-ended expressions stay deferred as parsed expression trees.
- Symbol binding is a separate concern from type inference: the binder owns declarations/references, then the TypeChecker owns semantic normalization.
- Tooling consumption is layered: TokenStream for lexical work, ConstructManifest for syntax, SymbolTable for name-aware features, SemanticIndex for semantic features.
- Event coverage remains a structural graph concern; guard-conditioned reachability belongs to the proof engine.
- When a working proposal becomes canonical, every downstream contract should be updated in one pass and stale references retired immediately.

## Recent Updates

### 2026-05-08T03:29:02Z — Wave 2 design batch recorded
- Frank-17 and Frank-18 closed the graph-analyzer and proof-engine design-doc corrections; all six D1–D6 decision gates are now durably closed in `.squad/decisions.md`.
- Frank-19 remains in flight on `TerminalCompletenessFact` and `EventCoverage`, so future analyzer/proof work should treat those contracts as the active continuation thread.


### 2026-05-08T03:08:18Z — Comprehensive language/compiler review closed
- Completed the comprehensive language/compiler doc review: `catalog-system.md` and `precept-grammar.md` were synchronized directly, while philosophy/runtime-claim drift plus graph-analyzer/proof-engine design gaps were recorded for owner review in the decision ledger.
- The durable follow-up is now in `.squad/decisions.md`, the orchestration log, and the session log rather than the doc inbox.

### 2026-05-08T03:08:18Z — TypeChecker consumer gate cleared
- George-16 resolved B1/B2/B3, updated `docs/compiler/type-checker.md`, and validated green at 3342 `Precept.Tests` + 263 `Precept.Analyzers.Tests`.
- GraphAnalyzer and downstream consumer-stage work are now unblocked.

### Historical summary through 2026-05-08T01:00:00Z
- The R0/R1/R2/R3 review line established the canonical TypeChecker baseline: the `TransitionRowOutcome` naming blocker was closed, slices 1–7 were approved, R3 surfaced B1/B2/B3 as the final consumer blockers, and the resolution path stayed catalog-driven.
- Earlier branch work locked the parser/checker boundary, the slot-value policy for parsed expressions vs. closed vocabularies, the NameBinder contract, and the rule that doc sync must ship in the same batch as language or runtime changes.
- Durable catalog learnings preserved from the fuller history: `ActionMeta.SyntaxShape`, `FunctionMeta.HasCIVariant`, and `FunctionMeta.CIVariantOf` now live in the canonical docs and should be treated as the downstream contract for parser/tooling consumers.
