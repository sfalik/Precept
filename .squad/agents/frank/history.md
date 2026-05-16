## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for the Precept DSL and runtime.
- Catalog metadata remains the language truth; pipeline, runtime, tooling, and docs should derive from durable metadata rather than enum-identity switches or parallel lists.
- Proof, qualifier, field-state, and normalization design work must stay grounded in shipped surfaces and verified implementation seams.

## Live Guidance

- Quantity normalization still has two durable lanes: compile-time normalization for declarations/literals and runtime normalization for ingress values; both should stay on shared normalizer logic.
- `TypedField` remains the normalization handshake between analysis and execution: authored bounds stay available for display, normalized bounds feed proof/comparison surfaces.
- Comparison/equality checking must stay as strict about explicit counting-unit identity as assignment is about constrained qualifier axes.
- Completion-provider defects should be treated as context-routing mistakes first; prefer catalog-sourced vocabularies plus explicit slot lanes over local keyword lists.
- Reject-bearing surfaces should be split structurally: success/mutation rows and refusal rows are separate constructs, not a shared slot plus cleanup diagnostics.

## Durable Learnings

- Any claim that work happens "only at compile time" must be stress-tested against Fire/Update/Restore ingress paths.
- Dynamic-unit interpolated forms MUST produce `Unbounded` / not-proved — never fall back to raw `StaticMagnitude` against normalized bounds.
- Counting-unit comparisons need unit-code identity, not just dimension-family compatibility.
- `ResolveSlotSourceQualifierAxis` must distinguish `Unknown` from `Absent`; `IsAssignmentQualifierAxisApplicable` is the discriminator.
- Function-call qualifier preservation belongs in the same enforcement story as operator qualifier compatibility.
- When the grammar can make an invalid form impossible, do that instead of inventing a later semantic ban.
- Hollow-entity validation should be shared across all pre-materialization expression lanes, not re-added slot by slot.
- Formal grammar production rules must reflect structural exclusion decisions immediately — the grammar doc is a design deliverable, not an afterthought that waits for implementation.

- Completed the planned runtime-tool section in `docs/tooling/mcp.md` so all four planned tools now have purpose, inputs, outputs, and explicit runtime-implementation dependencies: `precept_create`, `precept_update`, `precept_inspect`, and `precept_fire`.
- Locked OQ7 in `docs/working/constructor-semantics.md`: construction inspection will be implemented as `InspectCreate()` in core and exposed through `precept_inspect`.
- Wrote the confirmation note to `.squad/decisions/inbox/frank-planned-runtime-tools.md` so the canonical MCP doc reflects Shane's confirmed surface before runtime work begins.

### 2026-05-16T02:47:48Z — Frank-24 decision merged and inbox cleared

- Merged the `frank-oq4-unreachable-row.md` inbox note into `.squad/decisions.md`.
- The durable row-shadowing diagnostic is `UnreachableRow`; the construction-row family now uses `EventRow` / `EventRowReject`.
- The inbox note was deleted after merge.
## Historical Summary

- 2026-05-12 through 2026-05-16 concentrated Frank's work around hover contract reviews, field-state guarantees, constructor semantics, reject-surface structure, interval-proof design, quantity normalization, diagnostic-enforcement architecture, and counting-unit comparison gaps.
- The constructor/reject track settled three durable ideas: `on <Event>` is the honest construction surface, fallback `reject` is valid authored refusal rather than misuse, and grammar-level structural exclusion is preferred whenever the language already knows a path is impossible.
- The 2026-05-15/16 constructor spike also locked OQ4/OQ5/OQ6, applied the `Resolution/Reject` naming sweep, and cleared Slice 3/4 review gates; the detailed batch-by-batch notes now live in `.squad/decisions.md`.
- Older slice-by-slice review detail now lives in `history-archive.md` and `.squad/decisions.md`; this file keeps only the guidance and outcomes other agents need immediately.

## Recent Updates

### 2026-05-16T09:08:43-04:00 — Graph Analyzer: Complete Row Taxonomy Analysis (Broadened)

- Broadened the frank-25 construction-row analysis to cover ALL row types in Precept.
- Complete taxonomy: 5 row kinds — `TransitionRow`, `TransitionRowReject` (generate edges, live in `TransitionRows`), `EventRow`, `ConstructionRow`, `ConstructionRowReject` (no edges, live in `EventHandlers`).
- Validated user's intuition: ALL EventRow-family rows are structurally incapable of state transitions — no `FromState`, no `TargetState`, no `Outcome`. The graph analyzer should NOT generate edges for them. Correct.
- Confirmed `BuildEdges` is complete — it correctly processes only `TransitionRows` and correctly handles both success and reject subtypes.
- Confirmed PRE0081 is the ONLY diagnostic with a false-positive gap. All 8 other graph-analyzer diagnostics reason about state-to-state topology and are immune.
- `AlwaysRejecting` (PRE0082) already correctly scans BOTH collections — no gap there.
- Plain `EventRow` (stateless handler) cannot cause false positives in stateful precepts because: (a) the `States.Length > 0` gate on PRE0081 protects stateless precepts, and (b) `PRE0092` blocks plain EventRows in stateful precepts at type-check time.
- Net recommendation: the ~6-line Slice 8b fix is surgical and complete. No structural refactoring needed.
- Decision written to `.squad/decisions/inbox/frank-graph-analyzer-all-event-rows.md`.

### 2026-05-16T09:06:00-04:00 — Graph Analyzer × Constructor Semantics Deep Dive

- Deep analysis of `GraphAnalyzer.cs` confirmed construction rows should NOT generate graph edges — edges model state-to-state transitions, construction is pre-existence.
- Confirmed PRE0081 (`UnhandledEvent`) is a **false positive** on construction-only events: `BuildEdges` only processes `TransitionRows`, construction rows live in `EventHandlers`, so construction events produce zero edges and trip the "unhandled" check.
- Confirmed `AlwaysRejecting` severity promotion (Slice 6) is correct — it already scans `EventHandlers`.
- Identified 3 gaps: (1) PRE0081 false positive [MUST FIX in 8b], (2) `GraphEvent.IsInitial` flag wrong for construction events [fix in 8b], (3) `EventCoverageFact` cosmetic gap [deferred].
- Fix for Gap 1: before emitting PRE0081, check `semantics.EventHandlers.Any(row => row.IsConstruction && row.EventName == evt.Name)` and skip.
- Fix for Gap 2: OR construction-row existence into `GraphEvent.IsInitial` computation.
- Decision written to `.squad/decisions/inbox/frank-graph-analyzer-construction-deep-dive.md`.

### 2026-05-16T09:00:00-04:00 — Slice 1: Completion vocabulary metadata shipped

- Implemented Option (b) from the catalog-driven completions architecture analysis.
- Added `SlotVocabulary` enum (13 values) to `src/Precept/Language/ConstructSlot.cs`.
- Extended `ConstructSlot` record with `IsList`, `IsChainable`, `ItemIntroducerToken`, and `Vocabulary` fields.
- Populated all 24 slot instances in `Constructs.cs` with correct metadata.
- Key design decision: `ModifierList.IsList=false` because modifiers are whitespace-separated (no explicit introducer token), unlike comma-separated lists.
- Added 16 tests in `SlotVocabularyMetadataTests.cs` covering individual slot assertions and structural invariants.
- Full 3-slice implementation plan written to `.squad/decisions/inbox/frank-completions-plan.md`.
- All 6,157 tests pass (5780 + 333 + 44).

### 2026-05-16T09:00:00-04:00 — Test.precept line 8 diagnostic analysis

- Analyzed 4 diagnostics on `samples/Test.precept` after premature syntax update (removed `initial` from construction row before Slice 8b implementation).
- All 4 diagnostics are logically consistent cascade from single root cause: parser classifies `on create` (without `initial`) as plain EventHandler, not ConstructionRow.
- Identified stale diagnostic messages in PRE0146 and PRE0147 that reference old `on {0} initial` syntax — must be updated in Slice 8b scope.
- Flagged latent question: does `UnhandledEvent` (PRE0081) correctly exempt initial events with construction rows? Recommended regression test for 8b.
- Verdict: revert Test.precept to `on create initial` until 8b ships; update the sample in the same PR as the parser change.
- Decision written to `.squad/decisions/inbox/frank-test-precept-line8-analysis.md`.

### 2026-05-16T03:25:00Z — OQ8 diagram entry arrow locked

- Frank's OQ8 decision was merged into `.squad/decisions.md`.
- Construction now uses the `●` pseudo-node entry arrow and keeps construction rows in the inspector section above transition rows.
- The inbox note was deleted after merge.
### 2026-05-16T03:20:00Z — Scribe batch closed

- The planned runtime-tool note was already present in `.squad/decisions.md`, so the inbox copy was cleared without duplication.
- No decision entries were older than 30 days, so the archive gate made no changes.
- Frank history was condensed to stay below the 15 KB threshold.

### 2026-05-16T03:08:40Z — Precept-create OQ6 batch recorded

- Pre-check found `.squad/decisions.md` at 20999 bytes and 0 inbox notes; the archive gate found no entries older than 30 days.
- No decision merge was needed, no history file crossed the 15 KB summarization gate, and the batch ended with orchestration/session logs only.

### 2026-05-16T03:07:39Z — MCP consolidation batch recorded

- Merged the `frank-precept-create-tool.md` inbox note into `.squad/decisions.md`; the earlier OQ5 canonical note was already present.
- No decision entries older than 30 days were archived; no agent history crossed the 15 KB gate on this pass.

### Batch summary

- 2026-05-16 02:40 through 02:12: TransitionRow/EventHandler naming was finalized, the reject-only split was confirmed, and constructor/runtime doc sync closed.
- 2026-05-15 23:59 through 22:12: qualifier follow-ups, completion-position validation, and runtime API sync were completed.

## Learnings

### 2026-05-16T04:16:00Z — Slice 2 Parser Routing implemented

- Secondary disambiguation pattern: `ResolveRejectVariant()` does lookahead for `-> reject` AFTER primary disamb resolves the base kind. Keeps catalog-driven disamb clean while adding post-arrow split logic.
- `ParseRejectClause` and `ParseSuccessOutcome` live in `Parser.Expressions.cs` alongside `ParseOutcome`.
- `RejectClauseSlot` and `SuccessOutcomeSlot` added to `SlotValue.cs` with sentinels wired in `MakeSentinel()`.
- PRE0014 is structurally bypassed: construction rows have `SlotPreVerbGuardArrow` which consumes `when` during slot parsing, so the post-slot guard gate never sees it. No code change needed in the guard gate itself.
- The `on` leading-token candidate array orders `ConstructionRow` (19) before `ConstructionRowReject` (20), so primary disamb on `Initial` always picks the base kind; reject variant is resolved by secondary lookahead.
- `TransitionRow` outcome tests that previously used `OutcomeSlot`+`RejectOutcome` must now use `RejectClauseSlot` on `TransitionRowReject`.

### 2026-05-16T04:16:00Z — Slice 1 Grammar Foundations implemented

- `ConstructMeta` record lives in `src/Precept/Language/Construct.cs` (not a separate ConstructMeta.cs file).
- `Constructs.GetMeta()` is the exhaustive switch in `src/Precept/Language/Constructs.cs` — this is where new kind entries go.
- `ConstructSlotKind` values 14-17 were already taken (`AccessModeKeyword`, `FieldTarget`, `RuleExpression`, `InitialMarker`); used 18/19 for `RejectClause`/`SuccessOutcome`.
- The `SlotOrderingDriftTests` asserts an exact set of scoped constructs — new kinds need adding there.
- `ConstructsTests` has count invariants (`Total_Count`, `TopLevel_Count`, `SharedLeadingTokens_HaveCorrectCandidateCount`) that must be updated when constructs are added.
- The EventRow slots are left unchanged for Slice 1 (no guard slot yet); Slice 2 parser routing will add it when PRE0014 is removed.
