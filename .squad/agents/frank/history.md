## Core Context

- Owns language research, spec wording, and cross-surface documentation for Precept's DSL and architecture.
- Converts owner/design decisions into implementable language-surface guidance for parser, catalog, and tooling work.
- Historical summary: corrected the expression-form catalog boundary, locked the separate `ExpressionForms` catalog shape and MCP propagation, authored the parser-gap implementation plan/spec audit, and established parser coverage as a metadata-backed validation problem rather than a runtime routing problem.

- G1 resolved: `DiagnosticId_MultiLeadCollision` / `MultiLeadCollisionRule` renamed to `DiagnosticId_MultiSequenceCollision` / `MultiSequenceCollisionRule` in `Precept0023OperatorsDUShapeInvariants.cs`. The diagnostic checks the full `(TokenKind, TokenKind?, TokenKind?)` sequence, not just the lead token — the old "MultiLead" name falsely implied a lead-token-only check. Sibling naming pattern (`SingleMultiCollision`, `MultiSequenceCollision`) is now consistent.

## Learnings

- **Dapr-as-runtime research (2026-05-02):** Conducted deep analysis mapping Precept's model onto Dapr building blocks. Key findings: (1) Dapr Actors are the strongest fit for hosting Precept entity instances — single-threaded turn-based execution maps cleanly to Precept's single-event-at-a-time semantics. (2) Dapr Workflows are a poor fit — they model orchestration steps, not constrained state transitions; guards/constraints would be awkward as activity return values. (3) Dapr State Store is the persistence layer regardless of hosting model — the state blob is `{state, fields, etag}`. (4) Pub/Sub is at-least-once and async — fundamentally at odds with Precept's synchronous constraint validation; requires idempotency and dehydrate/rehydrate patterns. (5) The recommended architecture is Actor-hosted Precept instances with state store persistence, service invocation as the `Fire` endpoint, and pub/sub only for external event ingestion (not constraint enforcement). (6) Key tension: Dapr adds distributed infrastructure overhead to what is currently a pure in-memory constraint engine — the value proposition only materializes when you need distributed entity hosting, not just constraint evaluation.

- §0.4.1 "No loops" prohibits general iteration, recursion, and fixpoint-requiring constructs. Bounded quantifier predicates (`each`/`any`/`no`) are not iteration constructs — they unfold to finite conjunctions/disjunctions, require no fixpoint reasoning, introduce no mutable loop variable, and terminate in bounded time over statically-declared finite collections. The spec amendment must draw this line explicitly; leaving it as a design-doc footnote is insufficient.
- The carve-out for bounded quantifiers depends on Expression Purity (§0.4.6) remaining non-negotiable. If predicates could mutate state or observe external context, the distinction between quantifier and loop collapses. The two principles are coupled.
- Philosophy compatibility: every core philosophical commitment (prevention, determinism, inspectability, compile-time-first checking) is satisfied by bounded quantifiers. No tension exists.
- Timing discipline: the spec amendment and the feature implementation belong in the same PR. Amending the spec preemptively introduces aspirational text, which is the exact problem the spec has been cleaned up to avoid.
- Q1 in collection-types.md §Open Questions is resolved by the amendment. That resolution should be recorded in the same PR that lands the amendment and the feature.

- GAP-7/GAP-8/GAP-11 resolved in the language spec: `TypeQualifier` includes `to` for `ExchangeRate`, `contains` documents `BinaryExpression(Contains, ...)`, and number/boolean literals document the unified `LiteralExpression` node.
- GAP-6 resolved: period negation added to spec §3.6. Catalog entry NegatePeriod = 8 in OperationKind.cs. NodaTime Period.Negate() is the backing implementation.
-The two exhaustiveness enforcement strategies (CS8509 for centralized switches, `[HandlesCatalogExhaustively]` + `[HandlesCatalogMember]` for distributed dispatch) are topology-dependent — the decision is made at the commit introducing the dispatcher, not retrofitted.
- Annotation naming must be catalog-agnostic from day one: `[HandlesForm]` was ExpressionForm-specific naming on a system designed to be universal. Renamed to `[HandlesCatalogMember]` for symmetry with `[HandlesCatalogExhaustively]` and C#'s standard "member" terminology for enum values.
- Catalog inclusion is decided by **language surface**, not by whether a current consumer already needs the data.
- Multi-token operators such as `is set` still belong in the catalog; parser structure and catalog completeness answer different questions.
- Implementation plans must name exact insertion points inside methods, not just the method names.
- GAP-2 does **not** require removing `When` from parser boundary tokens; the boundary is already correct.
- Philosophy/spec wording must match actual runtime guarantees; if they drift, flag the gap rather than silently rewriting either side.
- Durable research should preserve rationale, rejected alternatives, and concrete examples.
- Pratt parsing discovers expression form by reading tokens, so `ExpressionFormKind` is a coverage/validation axis, not a runtime parser routing key.
- Coverage enforcement should consume stable metadata and annotations, not parser implementation internals.

## Recent Updates

### 2026-05-01T20:36:28Z — spike/Precept-V2 full review complete
- Frank's full architecture review is now durably recorded as APPROVED with 0 blockers and 3 guidance items for `spike/Precept-V2`.
- George-8 closed G3 by removing the RS1030 `Compilation.GetSemanticModel()` pattern and captured the Phase 3 G2 prerequisite as a TODO; the branch follow-on also includes coordinator commit `4d988d8` with commented-out `ConstraintKind` / `ProofRequirementKind` entries in `CatalogEnumNames` for future activation.
- Soup-Nazi-4 closed all 6 missing-test gaps from the review and the branch finished green at 2687 passing tests, so Frank's approval now stands with implementation follow-through complete.

### 2026-05-02 — Full architecture review of spike/Precept-V2: APPROVED
- Reviewed entire branch (36ccec4..4831cb3): annotation bridge (PRECEPT0019), 4 catalog integrity analyzers (PRECEPT0020–0023), parser fixes (GAP-A/B/C, is-set, method call, list literal, typed constant), ExpressionFormKind catalog (11 members), OperatorMeta DU shape, TokenMeta.IsValidAsMemberName, parser 3-file split, and docs.
- Build clean (1 pre-existing RS1030 warning). 2678 tests passing, 0 failures.
- Verdict: APPROVED with 0 blockers, 3 guidance items (G1: PRECEPT0023c naming, G2: CatalogEnumNames missing ConstraintKind/ProofRequirementKind for Phase 3, G3: pre-existing RS1030).
- Key finding: The `CatalogEnumNames` set in `CatalogAnalysisHelpers` does not yet include `ConstraintKind` or `ProofRequirementKind` — their GetMeta switches still use discard arms. When Phase 3 drops the discard arms, those enums must be added to enable PRECEPT0007 enforcement.
- The annotation bridge pattern is production-ready and catalog-agnostic. Future catalog enums opt in without analyzer changes.
- OperatorMeta DU (SingleTokenOp/MultiTokenOp) with dual indexing (ByToken + ByTokenSequence) is architecturally sound.

### 2026-05-01T20:10:18Z — Rename implementation landed
- George-7 completed the mechanical propagation of Frank's catalog-agnostic annotation rename: active code, analyzer, tests, and docs now use `[HandlesCatalogMember]`.
- Future architecture guidance should treat `[HandlesForm]` as retired terminology preserved only in historical notes and archived design context.

### 2026-05-01 — Annotation rename and scope audit closed out
- Scribe merged `frank-handlesform-rename.md` into `decisions.md`; the durable per-member annotation name is `[HandlesCatalogMember]`, paired with `[HandlesCatalogExhaustively]`.
- Frank-9's full catalog-enum sweep found no currently-missing distributed-dispatch annotations anywhere in the codebase; centralized switch sites remain correctly covered by CS8509 instead.
- Legacy `[HandlesForm]` mentions in older notes should now be read as historical rename context, not active API guidance.

### 2026-05-01 — Analyzer audit and Phase 2e planning tracked
- Frank-9-1 completed the analyzer recommendations audit, wrote `docs/working/analyzer-recommendations.md`, and identified the PRECEPT0020-PRECEPT0023 gap set.
- Frank-10 is appending Phase 2e Slices 28-32 to `parser-gap-fixes-plan.md`; that plan update remained in flight at closeout.


### 2026-05-01 — Generic annotation bridge merged to ledger
- Canonical decision merged: the class marker is now `[HandlesCatalogExhaustively(typeof(T))]`, replacing the earlier parameterless `[HandlesExpressionForms]` direction.
- Durable enforcement shape locked as catalog-agnostic: `HandlesFormAttribute(object kind)` pairs with PRECEPT0019 so future catalog enums opt in without analyzer rewrites.
- Session closeout recorded the bridge under the full-vision annotation workstream and cleared the processed inbox artifacts.

### 2026-05-01 — Annotation-bridge coverage design recorded
- Recommended the annotation-bridge pattern for expression-form coverage: handler methods claim responsibility with `[HandlesForm(...)]`, while pipeline classes opt in with parameterless `[HandlesExpressionForms]`.
- Locked the preferred enforcement stack as PRECEPT0007 for catalog completeness, PRECEPT0019 for handler-claim completeness, and xUnit for end-to-end parser coverage.
- Rejected parser-structure-coupled analysis as the wrong maintenance tradeoff; durable enforcement should analyze metadata and attributes.

### 2026-05-01 — Parser coverage assertion follow-on locked
- Decision merged to canonical squad ledgers: parser coverage against `ExpressionFormKind` is worth doing, but as a follow-on slice rather than an expansion of the current parser-gap slice.
- Durable recommendation: use catalog-side explicit lead-token metadata plus xUnit assertions that parser dispatch handles every declared form.

### 2026-05-01 — Full implementation review of 13-slice parser gap fixes
- Reviewed all 13 slices against plan, spec, and design intent. 9 slices CLEAN, 2 NOT IMPLEMENTED (Slice 2/GAP-A deferred, Slice 13 deferred), 2 CLEAN with acceptable design deviations.
- Key finding: `is set` implementation uses two separate nodes (IsSetExpression/IsNotSetExpression) instead of one with boolean — acceptable improvement. Precedence 60 vs plan's 40 — needs resolution.
- OperatorKind.IsSet/Arity.Postfix never added to catalog — the multi-token operator gap. Shane chose Full DU (Option B) for Phase 2.
- PRECEPT0019 correctly implemented as generic catalog-agnostic analyzer but stays at Warning with suppression because TypeChecker/GraphAnalyzer lack annotations.
- Authored Phase 2 extended plan: 7 work items (A–G), 3 phases, strict dependency ordering, 13-point acceptance gate. No deferrals, no holes.
- Learning: when a plan specifies "add to catalog" but the implementation hardcodes a handler instead, the result works but violates the Completeness Principle — always verify catalog additions were actually made.

### 2026-05-01 — Analyzer gap review and Slices 28–32 added to plan
- Authored `docs/working/analyzer-recommendations.md`: full coverage map of 19 existing analyzers, 4 identified gaps (A–D), 2 hardcoded-pattern targets, and prioritized candidate analyzer specs (PRECEPT0020–0023).
- Shane approved all items — no gaps, everything goes in the plan.
- Appended Phase 2e to `parser-gap-fixes-plan.md`: new phase header, Slices 29–32 full specs, process note for `CatalogAnalysisHelpers.CatalogEnumNames`.
- Acceptance gate expanded from 13 to 14 points to include Phase 2e completion.
- Decision inbox entry filed: `.squad/decisions/inbox/frank-analyzer-slices-added.md`.
- Key sequencing: Slice 32 (PRECEPT0023) is deferred pending Phase 2b completion; Slices 29–31 are independent and ready for George.


