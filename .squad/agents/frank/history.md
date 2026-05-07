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

- SymbolTable stage design principle: name binding is a separate concern from type inference. The SymbolTable collects declarations (field/state/event/arg) and resolves identifier references — pure name resolution requiring no type information. The type checker then receives pre-resolved names and performs semantic analysis. This follows the pipeline principle that each stage owns exactly one category of resolution.

- LS artifact consumption is layered: lexical features → TokenStream; syntax features → ConstructManifest; name-level features (completions for field/state/event targets, "did you mean?", identifier semantic tokens) → SymbolTable; semantic features (hover with type info, typed expression tokens, operation resolution) → SemanticIndex. The SymbolTable gives the LS useful data even when the TC has errors.

- Current `SemanticIndex.cs` stub is minimal (FieldReferences, StateReferences, EventReferences + diagnostics). The canonical design (`compiler-and-runtime-design.md` §6) specifies a much richer inventory: Symbols, Bindings, Normalized Declarations, Typed Expressions, Dependency Facts. The stub needs major expansion during TC implementation.

- `language-server.md` is fully designed with complete per-feature mechanics (§7.1–7.10), cursor context determination via `SlotContext`, catalog-driven completions, two-pass semantic tokens, "did you mean?" fuzzy matching with Levenshtein ≤3. It reads `SemanticIndex` properties like `FieldsByName`, `StatesByName`, `EventsByName`, `UserFields`, `UserStates`, `UserEvents` that don't yet exist on the stub.

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

### Historical summary through 2026-05-07T09:36:17Z — Parser and NameBinder design baseline

- Closed the parser-prerequisite and computed-field waves: `<-` is the computed-field delimiter, parser exhaustiveness stays scoped to expression handlers, and the parser remains catalog-driven without new architectural blockers.
- Completed the independent and creative parser reviews, surfacing one medium-priority follow-up (action operand structure) plus ergonomics ideas, while keeping the current parser/type-checker boundary intact.
- Produced the SymbolTable/NameBinder stage sketch that formalized the new pipeline slot between Parser and TypeChecker and established the declaration/reference-only contract for `SymbolTable`.

### 2026-05-07T15:18:42Z — NameBinder implementation closed

- Frank-11 completed the exhaustive NameBinder doc sync, created `docs/compiler/name-binder.md`, and updated 18 documentation files with zero stale references remaining.
- Soup-Nazi-2 added `test/Precept.Tests/NameBinder/NameBinderTests.cs` with 40 tests across 9 groups, closing the batch with 2929 total passing tests.
- No new architectural decisions were required; the remaining durable record now lives in the canonical docs, the decision ledger, and the orchestration/session logs.
