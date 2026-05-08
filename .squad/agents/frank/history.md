# Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for Precept's DSL and runtime.
- Catalogs remain the language truth; runtime, tooling, and docs must derive behavior from metadata and durable shape rather than enum-identity switch logic or parallel lists.
- Public API surfaces expose stable CLR/JSON interchange contracts; evaluator internals stay internal.
- Investigation docs can be archived once their outcomes are captured in canonical docs, proposals, or the squad decision ledger.

## Learnings

### 2026-05-08T04:30:00Z — Exhaustive GraphAnalyzer post-implementation audit completed
- Three-dimensional review (requirements, catalog compliance, code quality) across 70 findings total: 39+7+14 pass, 4+3+2 advisory, 1 non-blocking gap.
- Verdict: APPROVED. No blocking defects. All 7 Graph-stage diagnostics (80, 81, 108, 109, 110, 111 + defensive 32) are registered, correctly staged/severed, and emitted. Catalog-driven modifier dispatch in `GetStateFlags()` is exemplary. `IsInitial` direct enum check is acceptable (topological entry point, not structural constraint). `TransitionRowOutcome` switch is DU dispatch, not catalog violation.
- Single ❌: `EventModifierMeta.RequiredAnalysis` is declared in the catalog but not consumed by the analyzer. Zero risk today (only `initial` event modifier exists and is handled equivalently). Becomes a must-fix when future event modifiers ship.
- Three should-fix items for future polish: event-per-state index for O(1) lookups, `RelatedSpans` on structural violation diagnostics, code comment for planned `EventModifierMeta` consumption path.
- Diagnostic count verified: 111 codes in enum, 111 claimed in `diagnostic-system.md`, 6 Graph codes — all consistent.

### 2026-05-08T00:36:25Z — Full advisory list reconstructed from re-examination
- Scribe merged and deleted the exhaustive review file before the full advisory list was extracted. Re-examined all GraphAnalyzer sources to reconstruct the complete 9+1 list.
- Requirements (4): A1 RelatedSpans on 109/110/111, A2 TerminalCompletenessFact zero-terminal semantics, A3 Graph diagnostics lack RelatedCodes, A4 Doc §4 lists Fields/StateHooks as inputs but they're not consumed.
- Catalog (3): A5 missing planned-consumption comment for EventModifierMeta, A6 IsInitial enum check lacks explanatory comment, A7 NoInitialState dedup uses fragile string comparison.
- Quality (2): A8 event-per-state index for O(1) coverage, A9 GraphEvent.IsInitial duplicates linear scan.
- Gap1: EventModifierMeta.RequiredAnalysis not consumed (future).
- 8 of 9 advisories are addressable now. Written to `.squad/decisions/inbox/frank-advisory-reconstruction.md`.

- Closed-vocabulary syntax (types, modifiers, access modes) should be resolved by the parser at recognition time; only open-ended expressions stay deferred as parsed expression trees.
- Symbol binding is a separate concern from type inference: the binder owns declarations/references, then the TypeChecker owns semantic normalization.
- Tooling consumption is layered: TokenStream for lexical work, ConstructManifest for syntax, SymbolTable for name-aware features, SemanticIndex for semantic features.
- Event coverage remains a structural graph concern; guard-conditioned reachability belongs to the proof engine.
- When a working proposal becomes canonical, every downstream contract should be updated in one pass and stale references retired immediately.

## Recent Updates

### 2026-05-08T05:15:57Z — Shane locked PE decisions 1 and 2; evaluator architecture deep dive requested
- Shane approved renaming Strategy 2 to `Declaration Attribute Proof` and locked unqualified periods to the permissive `PeriodDimension.Any` behavior.
- Decision 3 remains open: Frank must deep-dive whether initial-state satisfiability should reuse evaluator architecture or a compile-time mini-evaluator, including SemanticIndex/runtime dependencies and shared-logic boundaries.
- Durable record merged from `.squad/decisions/inbox/frank-proof-engine-design-decisions.md` and `.squad/decisions/inbox/shane-pe-signoffs.md` into `.squad/decisions/decisions.md`.

### 2026-05-08T01:00:51Z — ProofEngine PE-G1/G2/G3 design decisions delivered
- Resolved all three BLOCKING gaps from the gap analysis. Strategy 2 expanded to "Declaration Attribute Proof" covering modifiers, qualifier bindings, and dimension qualifiers. `ProofDischarge` catalog type designed with 10 modifier entries. `ProofLedger` output type confirmed sound with `ConstraintIdentity` spec corrections. 8 spec updates and 4 catalog changes ordered for implementation readiness. Decision document at `.squad/decisions/inbox/frank-proof-engine-design-decisions.md`.

### 2026-05-08T00:56:00Z — Grammar spec and ProofEngine readiness captured
- The authoritative TextMate grammar spec review is now durably recorded with 35 missing constructs, 10 stale patterns, 16 scope misassignments, 28 repository patterns, a 49-scope vocabulary, and 24 generator-completion requirements.
- The ProofEngine pre-implementation review concluded the stage is not ready: blocking gaps `PE-G1`, `PE-G2`, and `PE-G3` must close before implementation begins.

### 2026-05-08T04:55:35Z — ProofEngine gap analysis and grammar spec draft merged
- Frank marked ProofEngine implementation NOT READY until the discharge-strategy, `FieldModifierMeta.ProofDischarges`, and `ProofLedger` contract gaps are closed.
- Frank also drafted the authoritative TextMate grammar spec, locking catalog scopes and parity-or-better generator output as the replacement bar for `precept.tmLanguage.json`.


### 2026-05-08T00:49:00Z — Advisory reconstruction durably merged and implementation confirmed
- The reconstructed 9+1 GraphAnalyzer advisory inventory is now durably captured in `decisions.md`, preserving A1-A9 plus Gap1 after the earlier exhaustive-review merge dropped the detailed list.
- George subsequently closed all 8 addressable items in commit `79c3403`; only Gap1 (future `EventModifierMeta.RequiredAnalysis` dispatch) remains open by design.
- Validation at George handoff closed green at 3385/3385 `Precept.Tests`.


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

### 2026-05-07 — Comprehensive sprint audit completed
- Audited all Wave 2 code (`053406b`), Wave 2.5 artifacts, 6 doc overhauls, LS test port, grammar generator, and cross-surface sync.
- Code quality confirmed good: TypeChecker 3-way split compiles clean, EventOutcome/UpdateOutcome renames correct, ConstraintViolation 5-field expansion correct, DiagnosticCode.DeadEndState wired properly. 3,345 tests green.
- Found one doc error: `diagnostic-system.md` still says "107 total" — must be 108 after DeadEndState addition.
- R4 review gate was bypassed — George-22 started GraphAnalyzer without explicit R4 clearance. This audit serves as the R4 equivalent. No blocking defects found.
- Three test coverage gaps flagged: no shape tests for EventOutcome.Faulted, FieldSnapshot.ClrType, or ConstraintViolation 5-field construction.
- Grammar generator works but output would regress hand-authored tmLanguage.json — generator needs completion before replacing the hand-authored file.
- LS→core dependency direction approved as architecturally correct. LocationOrDocumentSymbol is a tactical implementation detail, not an architectural concern.
- Findings written to `.squad/decisions/inbox/frank-comprehensive-audit.md`.

### 2026-05-07 — R4 Architectural Review completed for GraphAnalyzer (67d42b2)
- Reviewed GraphAnalyzer.cs (587L), StateGraph.cs (98L), SemanticIndex/TypeChecker span additions, and doc updates.
- Verdict: R4 CONDITIONAL — architecturally sound but one blocking defect: three structural-violation diagnostics (TerminalStateHasOutgoingEdges, IrreversibleStateHasBackEdge, RequiredStateDoesNotDominateTerminal) are specified in the design doc but neither registered in DiagnosticCode nor emitted. Violations are detected and recorded in data structures but produce zero user-visible feedback.
- Design doc appendix has stale code-number collision: claims 82–84 for graph diagnostics but those slots are Proof-stage codes.
- Catalog-driven modifier dispatch (GetStateFlags via StateModifierMeta) is correct. Proof-forwarding DU complete. Error recovery correct. Pipeline isolation clean.
- Reject-outcome self-edges flagged as an architectural concern for terminal-violation analysis — no user impact today but should be filtered when violation diagnostics are implemented.
- Findings written to `.squad/decisions/inbox/frank-r4-review.md`.

### 2026-05-08T03:35:00Z — GraphAnalyzer OQ1/OQ2 resolved
- Landed the GraphAnalyzer OQ1/OQ2 implementation: dead-end states now produce a separate `DeadEndStateFact` with `DiagnosticCode.DeadEndState = 108` detected via reverse-reachability BFS from terminal states in Phase 2.
- Locked the event-handler coverage question as a structural impossibility: `EventHandlerInStatefulPrecept` blocks handlers in stateful precepts, the graph analyzer only runs on stateful precepts, and `docs/compiler/graph-analyzer.md` §4 plus `docs/compiler/proof-engine.md` were corrected accordingly.
- Manifest validation closed green at 3345 tests.
