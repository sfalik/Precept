# Core Context



- Owns language research, spec wording, and cross-surface architecture documentation for Precept's DSL and runtime.

- Catalogs remain the language truth; runtime, tooling, and docs derive behavior from metadata and shape rather than hardcoded enum identity or parallel lists.

- Public API surfaces must expose stable CLR/JSON interchange types; evaluator internals stay internal and never leak into the durable surface.

- Operation legality lives in `Language.Operations`; computation lives in `TypeRuntime` plus the runtime dispatch registry. The evaluator stays zero-knowledge.

- Identity-type work follows the dual-shape rule: enriched internal entities when metadata/lifetime demands it, lightweight API-boundary code/value shapes when callers need stable interchange.

- Collection internals are settled around universal `PreceptValue[]` backing, stride-2 pair storage, static `CollectionActions` helpers, and evaluator-owned copy-on-write.

- Collection CLR adapters are lazy at the `Version` level and eager on first materialization, not per-index lazy.

- Working docs drift quickly during heavy deliberation; canonical docs and squad records must be synchronized as soon as a decision locks.

- Investigation docs may be archived once their outcomes are captured in canonical docs, proposals, or the squad decision ledger.



## Learnings



- SlotValue shape principle: closed-vocabulary tokens (types, modifiers, access modes) are resolved by the parser at recognition time — never deferred as spans. String literals (`because` clause) are extracted at parse time. Only expressions (open-ended, precedence-sensitive) are deferred as `ParsedExpression`.

- Disambiguation offset for StateScoped/EventScoped constructs is structurally invariant at 2: `peek(2)` always hits the disambiguation token because the anchor name is always a single identifier at position 1.

- `AccessModeSlot` in `SlotValue.cs` needs a code fix: must carry `TokenKind AccessMode` (currently stores only base `SourceSpan`).

- `type-checker.md` § SlotValue Subtypes table is stale (carries pre-resolution shapes for 4 slots); must be updated as a follow-up.



- Diagnostic policy follows the philosophy's "proven violations only" rule: per-state event coverage gaps are design choices, but zero-handler events across all states justify `UnhandledEvent`.

- When a working proposal becomes canonical, update every downstream contract in one pass and repoint CC references to canonical homes before archiving the proposal.

- `GraphState` is a derived-facts output record, never a source-model mirror; booleans are the right shape when the question is structural.

- `SlotContext` and `ConstructSlotKind` are different concepts; mapping between them is legitimate, aliasing them is not.

- Default-valued field additions on `readonly record struct` contracts are acceptable when they preserve existing call sites and the new data is structurally optional.

- `EventCoverageEntry` stays at event-level; guard-conditioned reachability belongs to the proof engine via `ProofForwardingFact`. Graph analysis is structural, not evaluative.

- Back-edges in the graph analyzer are BFS-ancestor edges, not DFS back-edges. BFS ancestor is canonical because reachability and irreversibility use the same traversal order.

- Graph structural diagnostic codes (82-85) precede proof engine codes (86+) by pipeline stage order.



## Recent Updates



### 2026-05-07T05:09:00Z — Independent parser audit: catalog vs. implementation



- Audited all 12 constructs in `Constructs.cs` against `Parser.cs` slot dispatch.

- **Result: All constructs verified. No discrepancies found.**

- Parser is catalog-driven by construction: `ParseSlots` iterates `meta.Slots` in order, guaranteeing slot count, order, and kind alignment structurally.

- The known concern (EventHandler slots) confirmed correct: catalog declares `[EventTarget, ActionChain]` (2 slots) and parser produces exactly that. The erroneous coordinator spec `[EventTarget, ActionChain, Outcome]` was never implemented.

- `ParseOutcome` correctly uses `Outcomes.ByToken` catalog lookup and dispatches by `OutcomeSyntaxShape`.

- `EventTarget` mid-construct handling (TransitionRow) is correct: parser conditionally consumes `on` only if present, which handles both the leading-token-already-consumed case and the mid-construct case.

- Test coverage gap: `RuleDeclaration` has no parser test in either test file.



---



### 2026-05-07T04:02:01Z — Parser prerequisite decisions approved



- Shane approved Frank's B2 + B3 parser-prerequisite decisions and Scribe merged the paired inbox notes into one canonical ledger entry.

- Durable parser rule: keep `peek(2)` as the scoped-construct disambiguation invariant because anchor names are single-token grammar productions and the offset never varies by construct kind.

- George is unblocked on `ParsedExpression.cs`; the remaining paired code fix is `AccessModeSlot(TokenKind AccessMode, SourceSpan Span)` in `SlotValue.cs`.



---



### 2026-05-06 — Disambiguation offset: structural invariant confirmed



- Assessed whether `peek(2)` in the disambiguation protocol should be a `DisambiguationEntry` catalog field. Verdict: KEEP as structural invariant.

- Key reasoning: the offset does not vary by construct kind — it is universally 2 for all StateScoped/EventScoped entries. A field with a single constant value for all members is not metadata; it is grammar geometry.

- Confirmed `StateTarget` (Identifier | Any) and `EventTarget` (Identifier) are formally single-token productions in the grammar spec. This is a language-level guarantee, not an unstated assumption.

- The catalog-driven principle governs per-member domain knowledge that varies by kind. The disambiguation offset is applied uniformly before any member is identified — it is machinery topology, not a per-construct fact.

- Decision written to `.squad/decisions/inbox/frank-disambiguation-catalog.md`.



---



### 2026-05-06 — Wave 5: Archive & Cleanup



- Deleted `docs/working/` entirely (67 files): cross-cutting-decisions.md, both migrated gap registers, all Archived and inbox working artifacts.

- Deleted `docs/compiler/parser-radical.md` and `docs/compiler/type-checker-radical.md` (radical proposals superseded by canonical stage docs).

- Pre-deletion scan confirmed zero genuine unresolved items in working docs. All open questions not covered by CC decisions already live in canonical docs.

- Fixed broken cross-references in 8 canonical docs: README.md (superseded section removed), parser.md (6 refs), proof-engine.md (5 refs), type-checker.md (4 refs), tooling-surface.md (2 refs), catalog-system.md (ActionMeta OQ converted to settled note + 1 ref), precept-grammar.md (parser-radical.md ref), mcp.md (2 refs).

- `catalog-system.md` ActionMeta "Open Question (unresolved)" converted to "✅ Settled (Wave 4 Gap 6)" — Description is canonical, SyntaxShape is internal, SnippetTemplate is deferred.

- Build validation: 3 pre-existing SemanticIndex.cs errors, 0 new. Baseline unchanged.

- Commit: `421605afc9ec32ff0c28468b5927656bc725441c`



---



### 2026-05-07 — Wave 4: Final consistency pass + 6 genuine gap triage



- All 6 Wave 3 follow-up gaps resolved as team-autonomous; no owner-required items.

- **Gap 1 (TokenMeta.SemanticTokenModifiers #41):** No field added. All Precept tokens carry zero modifier bits — LSP modifier flags have no analog in Precept's token taxonomy. `language-server.md` code comment updated; `catalog-system.md` OQ closed.

- **Gap 2 (EventCoverageEntry granularity):** Event-level granularity confirmed. Guard-split is the proof engine's domain. `graph-analyzer.md` OQ closed.

- **Gap 3 (back-edge definition):** BFS-ancestor is canonical; DFS not used. `graph-analyzer.md` OQ closed.

- **Gap 4 (GraphEvent.IsInitial):** Derived structurally from edges originating from the initial-state vertex. `graph-analyzer.md` OQ closed.

- **Gap 5 (TBD diagnostic codes):** Assigned 82=TerminalStateHasOutgoingEdges, 83=IrreversibleStateHasBackEdge, 84=RequiredStateDoesNotDominateTerminal, 85=NoInitialState; proof codes start at 86. `graph-analyzer.md` appendix updated; `proof-engine.md` source-file note corrected.

- **Gap 6 (ActionMeta LS/MCP alignment #43):** Description surfaces in LS hover and MCP; SyntaxShape is internal; SnippetTemplate is a deferred milestone. Pattern settled. `language-server.md` OQ converted to settled note.

- Terminology sweep: 6 `precept/preview` occurrences corrected to `precept/inspect` in `tooling-surface.md`; no other mismatches found (OrphanedEvent, FieldChange, MutationRecord, SlotKind-as-cursor all clean).

- `ConstructSlotKind` count fixed: 15 → 17 in `catalog-system.md §Constructs`; stale OQ about parser.md discrepancy removed (canonical count is 17).

- `docs/compiler/README.md` updated: superseded-doc section added for parser-radical.md and type-checker-radical.md.

- `docs/working/cross-cutting-decisions.md`: Wave 3 marked ✅ COMPLETE; Wave 4 marked ✅ COMPLETE with full outcome block.

- Validation unchanged: `dotnet build src/Precept/Precept.csproj` reports only the 3 pre-existing `SemanticIndex.cs` errors.



---



### Historical summary through 2026-05-05



- 2026-05-04 established the execution/runtime baseline: catalogs describe legality, `TypeRuntime` plus runtime registries own computation, and the evaluator remains type-agnostic.

- Collection API design converged on CLR-friendly adapters and declared-direction storage while keeping internal storage on `PreceptValue[]`.

- Currency, unit, and dimension work converged on catalog-backed identity types with clear public/internal shape boundaries.

- Use `.squad/decisions.md` for full per-decision provenance; keep `history.md` focused on durable operating context and the newest closures.



### 2026-05-07T08:40:33Z — BackArrow decision and parser exhaustiveness closeout

- Frank's backarrow analysis and plan are now the durable basis for computed fields: `<-` is adopted, `=` is rejected because it collides with `set X = expr`, and `->` stays reserved for outcomes and action chains.

- The PRECEPT0019 follow-through also locked the narrow parser scope: `ExpressionFormKind` exhaustiveness belongs on `ParserState` and its expression handlers, not as a broader parser-wide attribute sweep.

- George's two commits (`266ee5a`, `5212c9d`) and Soup-Nazi's test pass closed the batch at 2810/2810 green.



