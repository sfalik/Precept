## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for the Precept DSL and runtime.
- Catalog metadata remains the language truth; runtime, tooling, and docs must derive behavior from durable catalog shape rather than enum-identity switch logic or parallel lists.
- Public API surfaces expose stable CLR/JSON interchange contracts; evaluator internals stay internal.
- Investigation docs can be archived once their outcomes are captured in canonical docs, proposals, or the squad decision ledger.

## Learnings
- 2026-05-10T15:38:30-04:00 — Comprehensive grammar doc review found 17 issues (8 Error, 6 Warning, 3 Minor). All doc-only. The systematic pattern: pre-verb `when` guard slots are missing from 6 anatomy/family-detail locations. Additionally: computed-field anatomy has wrong slot order annotation (modifiers shown trailing when they precede `<-`), ExpressionForms count says 13 but is 14, CIFunctionCall example syntax is inverted (`startswith~` vs `~startsWith`), BecauseClause incorrectly called "mandatory" in StateEnsure context, quick-reference invariant 2 stale. Full report at `docs/working/frank-grammar-comprehensive-review-2026-05-10.md`.

- 2026-05-10T15:32:08-04:00 — Exhaustive grammar/parser audit confirmed `SupportsPostActionEnsure` is the SOLE out-of-band parser injection. No other `Supports*` flags or post-slot-walk injection blocks exist. `IsOutlineNode` is LS-only, never read by parser. Grammar doc lags on `when` guard slots for EventEnsure, StateEnsure, and StateAction — all doc-only fixes. Language spec line 861–869 documents the bad form and must be corrected on removal. Audit at `docs/working/frank-grammar-spec-audit-2026-05-10.md`.
- Track 2 now runs from a 15-slice master plan:the durable root gaps remain operator result typing, parser/catalog drift, orphaned MCP DTO projections, and action-shape parser rewires. Fix order stays catalog fields → pipeline rewires → MCP/docs → tests.
- `SemanticTokenTypes`, outline tags, and authoring-reference metadata are settled catalog surfaces; runtime/tooling consumers must project them instead of keeping parallel token, outline, or doc-specific lists.
- Typed literals stay inside the current split: compile-time literal validation goes through `TypedConstantValidation.Validate(...)`, runtime JSON lanes go through `TypeRuntime<T>` / `TypeRuntimeMeta`, and ISO/UCUM remain embedded external datasets with Precept-owned augmentation only in source metadata.
- Highest-leverage prevention remains real-catalog contract tests for parser routing/disambiguation, MCP definition matrices, keyword-collision accessors, proof paths, hook branches, and tracker hygiene at the same boundary as execution status.
- 2026-05-10T16:15:12Z — Guard-position audit locked the live language surface: rule remains the one deliberate post-expression exception, transition rows keep post-event guards, state/event ensures and state actions are pre-verb, access mode is still the lone post-adjective inconsistency, and the current breakage is now narrowed to spec/sample drift plus missing zero-diagnostic assertions on integration samples.
- 2026-05-10T17:10:00Z — When-guard design review rejected the coordinator's slot[1]-is-GuardClause proposal as positional magic. Recommended `GuardPolicy` enum metadata (`None/SlotWalk/PreVerb/PostVerb`) on `ConstructMeta`, moving AccessMode to `in S when G modify F editable`, and treating the parallel `SupportsPostActionEnsure` smell as separate follow-up scope rather than hiding parse protocol in slot indexes.
- 2026-05-10T17:15:46Z — PostVerb constraint forced `GuardPolicy` re-evaluation. 4-member enum collapsed to 2 (`SlotDriven`/`PreVerb`): `None` is structurally redundant (absence of guard slot suffices), `SlotWalk` is operationally identical to default (parser walks slots normally either way). The surviving distinction is: does the parser inject a guard between anchor and disambiguation token (`PreVerb`), or not (`SlotDriven`)? AccessMode moves from post-verb slot to `PreVerb` injection. Decision record at `.squad/decisions/inbox/frank-when-guard-revised.md`.
- 2026-05-10T17:16:47Z — GuardPolicy enum eliminated entirely. Deep-reading the actual parser code revealed the prior objection ("slot-list-only approach requires rearchitecting ParseScopedConstruct") was wrong. Disambiguation happens before ParseScopedConstruct is called, so a guard at slot[1] is a regular optional slot. The 3-phase protocol (anchor → flag-gated injection → disambig + remaining slots) collapses to a single unified loop that walks all slots in order, consuming the disambiguation keyword at the natural boundary. Per-construct guard slot instances with appropriate TerminationTokens make the slot list fully self-describing. No metadata flags, no enums — the slot list IS the metadata. Prior recommendation reversed after code-level verification. Decision record at `.squad/decisions/inbox/frank-when-guard-final.md`.
- 2026-05-10T13:35:47-04:00 — Synced the live language docs to the final when-guard model: `precept-language-spec.md` now shows pre-verb guards for state/event ensures and access modes, `precept-grammar.md` now diagrams guarded access mode in pre-verb order, `catalog-system.md` removes the obsolete construct-level guard boolean and states that slot order carries guard position, and `docs/Working/precept-toolchain-plan.md` no longer preserves the obsolete boolean as the planned design. Notable gap found: `.squad/decisions.md` still surfaces superseded GuardPolicy/post-adjective AccessMode guidance, so a follow-up note belongs in the decisions inbox.
- 2026-05-10T15:07:23.325-04:00 — Collision audit across `precept-language-spec.md`, `precept-grammar.md`, and `catalog-system.md` confirmed the final pre-verb `when` model survived intact (`SupportsPreVerbWhenGuard` absent; no live post-verb access/ensure grammar remained). The one surviving coherence break was in `catalog-system.md`: the Constructs inventory still claimed 11 members and omitted `OmitDeclaration`, so I restored the 12-member count/list and recorded the fix in the decisions inbox.
- 2026-05-10T15:43:48.339-04:00 — Synced the remaining `SupportsPostActionEnsure` removal fallout in the canonical docs: `precept-language-spec.md` now ends EventHandler grammar at the action chain and explicitly states handlers reject trailing `ensure`, its parser diagnostic now lists the full live set of guard-supporting constructs, and `catalog-system.md` no longer advertises the deleted `ConstructMeta.SupportsPostActionEnsure` field.

- 2026-05-10T15:47:35.085-04:00 — Applied the `precept-grammar.md` comprehensive doc-alignment pass from `docs/working/frank-grammar-comprehensive-review-2026-05-10.md`: restored missing pre-verb `[when Guard]` coverage across anatomy/family summaries, corrected computed-field slot order (`ModifierList` before `<-`), fixed optional `BecauseClause` wording for state/event ensures, and synced the expression/catalog/appendix quick references.

## Historical Summary

- Early May work locked the typed-literal boundary, the external-data posture for ISO/UCUM, and the requirement that durable rationale live in decisions/research rather than scattered implementation switches.
- Recent batches settled the language-server baseline, the Phase 2 gap-closure plan, the parser/proof metadata audit, and tracker-status hygiene.
- Use `.squad/decisions.md` for exact chronology and `docs/` / `research/` for the surviving canonical rationale.

## Recent Updates

### 2026-05-11T01:38:51Z — Diagnostic-gating analysis and parser follow-through recorded
- Frank-2's terminal-state analysis is now durable: path-to-terminal warnings only make sense once at least one terminal state is declared, and lifecycle messaging should make the declared-terminal contract explicit.
- The team ultimately adopted Elaine's two-diagnostic UX split rather than the one-code refinement, so George shipped C119 `StructuralSinkState` alongside the gated C108 path warning.
- Frank's separate parser/type-checker batch is also now in the ledger: non-associative operators use `meta.Precedence + 1` on the RHS, and typed constants inherit peer operand type context before the D13 bailout.

### 2026-05-10T19:47:35Z — Grammar doc-fix batch durably recorded
- Frank-10/11/12 are now recorded together: the comprehensive precept-grammar.md audit, the spec/catalog cleanup for the illegal EventHandler trailing-ensure form, and the final doc-alignment pass all landed in the squad ledger.
- Durable guidance now locked: pre-verb when coverage must appear anywhere StateEnsure, StateAction, EventEnsure, or AccessMode are documented; computed-field modifiers precede <-; and ConstructMeta no longer carries ad-hoc handler-ensure support metadata.
- George-5 committed the fixes in 9b8e8384 and b8e7df94; validation closed green at 4,388 tests.

### 2026-05-10T16:02:38Z — Slice 8 review approved with one durable partial gap
- Slice 8 parser/catalog rewires were approved as architecturally clean and closed green at 3869/3869.
- BUG-019 remains partial: typed constants still fail in binary comparison context until `ResolveBinaryOp` retries context before the D13 bailout.

### 2026-05-10T15:52:58Z — Track 2 Phase A doc sync closed the audit gap
- D1-D8 doc drift in `catalog-system.md` plus the named modifier-test anchors are now closed; Phase B can proceed on aligned source/docs.

### 2026-05-10T15:34:08Z — BUG-049a follow-through completed
- `FixedReturnAccessor.ReturnNonnegative`, shared `Types.CollectionCountAccessor`, and Strategy 2 proof docs are the approved closeout for BUG-049a.

### 2026-05-10T13:46:52Z — BUG-006 / BUG-051 stayed operational, not architectural
- PRE0009 on `min(A,B)` was a stale build symptom; George's parser fix remained correct and the required action was rebuild-only.
