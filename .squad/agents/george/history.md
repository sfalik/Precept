## Core Context

- Owns code-level feasibility, parser/runtime implementation detail, and architecture-to-code translation across checker, analyzer, and tooling surfaces.
- Parser and checker work stay catalog-derived, array-primary where order matters, and hostile to mirrored duplicate state.
- Shared-environment build discipline matters: targeted build/test commands are safer than full-solution runs when the workspace may have external file locks.

## Learnings

- Self-edges (`ToState == FromState`) from `Reject` and `NoTransition` outcomes must be filtered out of terminal-violation analysis — a terminal state that only rejects events is honoring its contract, not violating it.
- When the spec says "Emit Diagnostic(X)" in pseudocode, verify the actual `Diagnostics.Create()` call exists in the code — violation data structures can be populated correctly while diagnostic emission is entirely missing.
- Doc appendix code numbers can drift from the actual `DiagnosticCode` enum when codes are added out-of-sequence (e.g., `DeadEndState = 108` was added after the Proof block at 82–84, but the appendix assumed contiguous Graph-stage numbering).

- `Types.GetMeta(...).ImpliedModifiers` is the durable source of truth for implied field modifiers.
- Error propagation in the TypeChecker should return `TypedErrorExpression` without layering extra diagnostics on parent nodes — but the error SOURCE must emit a TC-level diagnostic to satisfy D26 self-containment.
- `ParsedConstruct.LeadingTokenKind` is the minimal surface for recovering the anchor scope keyword (in/to/from/on) consumed by the parser but not previously stored.
- Context-sensitive literal retry is valuable when catalogs make typed constants or overload resolution depend on expected type.
- Parser metadata should expose the lookup axes the parser actually queries (`BindingPower`, termination tokens, closed vocabularies) instead of duplicating parser-local tables.
- Multi-location diagnostics can grow additively through payload fields like `RelatedSpans` without destabilizing constructor-heavy call sites.
- When PRECEPT0024 blocks downstream `.Syntax` access, hoist declaration `SourceSpan` data (for example `TypedState.NameSpan` / `TypedEvent.NameSpan`) into the typed artifact instead of reaching back to parse constructs.
- Graph wildcard expansion should stay structural: expand `FromState == null` rows per declared state, but suppress the expanded edge when that state already has an explicit row for the same event.

## Recent Updates

### 2026-05-08T03:29:02Z — Wave 2 closeout recorded
- George's Wave 2 slices are durably closed: compiler-stage doc corrections, runtime stub alignment, the `TypeChecker` partial split, MCP compile DTO shapes, and the `FieldSnapshot.ClrType` + outcome-hierarchy updates.
- All six Wave 2 design gates D1–D6 were closed through the merged decision inbox, giving downstream graph/proof/tooling work a settled runtime and compiler baseline.




### 2026-05-08T03:08:18Z — R3 blockers B1/B2/B3 closed
- George-16 resolved the TypeChecker consumer blockers: MissingExpression now emits a TC-level diagnostic, field default/computed expressions and computed deps are populated, and ensures/access modes/state hooks/edit declarations are normalized.
- `docs/compiler/type-checker.md` was synchronized in the same batch, and validation closed green at 3342 `Precept.Tests` + 263 `Precept.Analyzers.Tests`.

### 2026-05-08T07:00:00Z — R3 blockers B1/B2/B3 fixed
- Fixed B3 (MissingExpression D26 gap), B1 (field default/computed expression resolution + ComputedFieldDep extraction), B2 (ensures, access modes, state hooks, edit declaration normalization).
- Added `ParsedConstruct.LeadingTokenKind` for anchor scope determination.
- Updated docs §1/§4/§13. Updated 17 tests to match new behavior.
- Validation closed green at 3342/3342 + 263/263 tests.

### 2026-05-08T05:30:00Z — Slice 8: CI Enforcement shipped
- Commit: `00ef822`.
- Added CI enforcement passes over resolved expression trees with catalog-backed tracking for CI fields and element collections.
- Validation closed green at 3242/3242 tests.

### 2026-05-08T04:30:00Z — Slice 10: Final assembly + D26 global assert
- Commit: `844f00e`.
- Replaced `BuildPartialSemanticIndex` with full `BuildSemanticIndex`, wired the full checker pipeline order, and added the global `Debug.Assert` walk for error-expression containment.
- Validation closed green at 3294/3294 tests with 118 integration tests passing; TypeChecker implementation marked done.

### 2026-05-08T03:45:00Z — Slice 5+7 restoration + event arg ref fix
- Commit: `4e1efd8`.
- Restored Slice 5 transition/event-handler wiring and Slice 7 modifier validation after a concurrent-write overwrite, and fixed qualified event-arg references to resolve as `TypedArgRef`.
- Validation closed green at 3196/3196 tests.

### 2026-05-08T01:30:00Z — Slice 9: Quantifiers + List Literals shipped
- Commit: `54fa59b`.
- Implemented `ResolveQuantifier` and `ResolveListLiteral`, including boolean-predicate enforcement and list element unification.
- Validation closed green at 3242/3242 tests.

### Historical summary through 2026-05-07T23:22:15Z
- TypeChecker core line shipped across Slice 1 (`e882396`), Slice 2 (`1111da4`), Slice 3 (`fa87df9`), Slice 4 (`ac95de2`), and Slice 6 (`fe358ef`), with the R0 naming blocker closed by renaming `TypedOutcomeKind` to `TransitionRowOutcome` in `350f386`.
- Parser baseline before the checker push locked in `ParsedOutcome` (`94dec3b`), the `<-` computed-field delimiter (`266ee5a`), the bare-`<-` recovery fix plus `PRECEPT0019` exhaustiveness guard, and metadata moves for member-access precedence / expression-slot termination.
- George's housekeeping closeout committed OutcomesCatalog, parsed action/type-reference DU nodes, NameBinder wiring, diagnostic `RelatedSpans`, and related doc/squad sync across the nine-commit batch ending at `2337fd0`.
- Use `.squad/decisions.md` for full per-batch provenance and branch-level decision chronology.
- R3 self-review (2026-05-07): expression resolution core is correct across all 15 ParsedExpression forms. Widening, qualifier disambiguation, ErrorType propagation, QuantifierBindings symmetry, and D26 invariant all verified. Key gaps: field default/computed expression resolution never fires (always null), and 5 of 8 construct kinds (StateEnsure, EventEnsure, AccessMode, OmitDeclaration, StateAction) lack normalization — their CheckContext accumulators are always empty. These are the next implementation frontier. No stubs or TODOs remain in TypeChecker.cs.

### 2026-05-08T03:35:00Z — GraphAnalyzer OQ1/OQ2 decisions recorded
- Frank-19 resolved the active GraphAnalyzer open questions: model dead-end states as a separate `DeadEndStateFact` (not an expansion of `TerminalCompletenessFact`) and emit `DiagnosticCode.DeadEndState = 108` using reverse-reachability BFS from terminal states in Phase 2.
- Event handlers are outside GraphAnalyzer event coverage by construction: PRECEPT0092 (`EventHandlerInStatefulPrecept`) forbids handlers in stateful precepts, and the graph analyzer only runs on stateful precepts.
- Frank also corrected `docs/compiler/graph-analyzer.md` §4 and `docs/compiler/proof-engine.md`; treat these as the durable analyzer/proof-engine contract for downstream GraphAnalyzer work.

### 2026-05-08T04:26:28Z — GraphAnalyzer R4 blocker fix batch closed
- Commit `5398435` closed the real GraphAnalyzer R4 blockers: structural diagnostics 109/110/111 now exist, emit at analysis time, and the graph doc appendix matches the live enum again.
- `Reject` / `NoTransition` self-loops are now filtered out of terminal outgoing-edge violations, preserving the rule that a terminal state may reject without violating its contract.
- Validation at George handoff closed green at 3370 `Precept.Tests` passing before Soup-Nazi-7's follow-on test batch extended the branch baseline.
