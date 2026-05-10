## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for the Precept DSL and runtime.
- Catalog metadata remains the language truth; runtime, tooling, and docs must derive behavior from durable catalog shape rather than enum-identity switch logic or parallel lists.
- Public API surfaces expose stable CLR/JSON interchange contracts; evaluator internals stay internal.
- Investigation docs can be archived once their outcomes are captured in canonical docs, proposals, or the squad decision ledger.

## Learnings

- Track 2 now runs from a 15-slice master plan: the durable root gaps remain operator result typing, parser/catalog drift, orphaned MCP DTO projections, and action-shape parser rewires. Fix order stays catalog fields → pipeline rewires → MCP/docs → tests.
- `SemanticTokenTypes`, outline tags, and authoring-reference metadata are settled catalog surfaces; runtime/tooling consumers must project them instead of keeping parallel token, outline, or doc-specific lists.
- Typed literals stay inside the current split: compile-time literal validation goes through `TypedConstantValidation.Validate(...)`, runtime JSON lanes go through `TypeRuntime<T>` / `TypeRuntimeMeta`, and ISO/UCUM remain embedded external datasets with Precept-owned augmentation only in source metadata.
- Highest-leverage prevention remains real-catalog contract tests for parser routing/disambiguation, MCP definition matrices, keyword-collision accessors, proof paths, hook branches, and tracker hygiene at the same boundary as execution status.
- 2026-05-10T16:15:12Z — Guard-position audit locked the live language surface: rule remains the one deliberate post-expression exception, transition rows keep post-event guards, state/event ensures and state actions are pre-verb, access mode is still the lone post-adjective inconsistency, and the current breakage is now narrowed to spec/sample drift plus missing zero-diagnostic assertions on integration samples.
- 2026-05-10T17:10:00Z — When-guard design review rejected the coordinator's slot[1]-is-GuardClause proposal as positional magic. Recommended `GuardPolicy` enum metadata (`None/SlotWalk/PreVerb/PostVerb`) on `ConstructMeta`, moving AccessMode to `in S when G modify F editable`, and treating the parallel `SupportsPostActionEnsure` smell as separate follow-up scope rather than hiding parse protocol in slot indexes.

## Historical Summary

- Early May work locked the typed-literal boundary, the external-data posture for ISO/UCUM, and the requirement that durable rationale live in decisions/research rather than scattered implementation switches.
- Recent batches settled the language-server baseline, the Phase 2 gap-closure plan, the parser/proof metadata audit, and tracker-status hygiene.
- Use `.squad/decisions.md` for exact chronology and `docs/` / `research/` for the surviving canonical rationale.

## Recent Updates

### 2026-05-10T16:02:38Z — Slice 8 review approved with one durable partial gap
- Slice 8 parser/catalog rewires were approved as architecturally clean and closed green at 3869/3869.
- BUG-019 remains partial: typed constants still fail in binary comparison context until `ResolveBinaryOp` retries context before the D13 bailout.

### 2026-05-10T15:52:58Z — Track 2 Phase A doc sync closed the audit gap
- D1-D8 doc drift in `catalog-system.md` plus the named modifier-test anchors are now closed; Phase B can proceed on aligned source/docs.

### 2026-05-10T15:34:08Z — BUG-049a follow-through completed
- `FixedReturnAccessor.ReturnNonnegative`, shared `Types.CollectionCountAccessor`, and Strategy 2 proof docs are the approved closeout for BUG-049a.

### 2026-05-10T13:46:52Z — BUG-006 / BUG-051 stayed operational, not architectural
- PRE0009 on `min(A,B)` was a stale build symptom; George's parser fix remained correct and the required action was rebuild-only.
