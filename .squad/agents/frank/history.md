## Core Context

- Owns language research, spec wording, and cross-surface documentation for Precept's DSL and architecture.
- Converts owner/design decisions into implementable language-surface guidance for parser, catalog, and tooling work.
- Historical summary: corrected the expression-form catalog boundary, locked the separate `ExpressionForms` catalog shape and MCP propagation, authored the parser-gap implementation plan/spec audit, and established parser coverage as a metadata-backed validation problem rather than a runtime routing problem.

- G1 resolved: `DiagnosticId_MultiLeadCollision` / `MultiLeadCollisionRule` renamed to `DiagnosticId_MultiSequenceCollision` / `MultiSequenceCollisionRule` in `Precept0023OperatorsDUShapeInvariants.cs`. The diagnostic checks the full `(TokenKind, TokenKind?, TokenKind?)` sequence, not just the lead token — the old "MultiLead" name falsely implied a lead-token-only check. Sibling naming pattern (`SingleMultiCollision`, `MultiSequenceCollision`) is now consistent.

## Learnings

- **Type Checker Design Analysis (2025-07-14):** Completed full 5-section design analysis. Key architectural insight: the type checker is ~70-75% metadata interpreter (catalog lookups for Operations, Functions, Types, Modifiers, Actions) and ~25-30% structural logic (symbol tables, scope, cycle detection, choice validation). Expression typing is almost entirely catalog-driven — the Operations catalog's ~200 entries encode the full type algebra; the checker just queries it. Identified 5 catalog gaps: (1) TypedConstant ContentValidation on TypeMeta (HIGH — prevents per-type switch), (2) Scope rules (SKIP — tiny/structural), (3) TypedActionShape on ActionMeta (MEDIUM — explicit > derived), (4) CI enforcement diagnostics (LOW — stable 5-rule surface), (5) pow ProofRequirement (existing GAP-032). Proposed SemanticIndex uses ImmutableDictionary for symbols (O(1) lookup) and ImmutableArray for normalized declarations (ordered iteration). Resolution architecture: two-pass (registration → checking), where checking has 3 sub-passes: expression resolution engine (the metadata interpreter core), declaration normalization (thin per-construct wiring), structural validation (cycles, choices, cross-validation). Key missing catalog infrastructure: `Operations.BinaryBySignature` and `Operations.UnaryBySignature` frozen indexes for efficient (op, type, type) → OperationMeta lookup. 10-slice vertical decomposition proposed with clear dependency graph.

- **GAP-033 fixed (2026-05-02, Frank):** Stale XML doc comment on `ModifierKind.Notempty` updated: "Flag: string is non-empty" → "Flag: string or collection is non-empty (string + 8 collection types; Lookup excluded)." `StringAndCollectionTypes` in `Modifiers.cs` = String + Set/Queue/Stack/Log/LogBy/Bag/List/QueueBy (9 types). Lookup deliberately excluded — lookup entries are design-time defined. One-line doc-only fix. Same pattern as GAP-027 (Tokens.cs). Build clean.
- **GAP-019 (InvalidCallTarget/UnexpectedKeyword, 2026-05-02):**The "unreachable" comment at the infix LeftParen branch is misleading — it's only true for bare `identifier(` (consumed in ParseAtom), not for `42(args)` or `(A+B)(args)` which reach the branch with non-MemberAccess left operands and silently break. Fix is one line (`EmitDiagnostic(InvalidCallTarget, ...)`) before the break — this is a parser-phase fix, not TypeChecker. `UnexpectedKeyword` has no identified emit site; spec §2.7 should explicitly mark it reserved rather than listing it as active.
- **Diagnostic activation (Codes 11 & 12, 2026-05-02):** Elaine's UX review confirmed DISTINCT failure modes. Updated spec §2.7, catalog (`Diagnostics.cs`), and `DiagnosticCode.cs` doc comments. Key architectural decision: `InvalidCallTarget` category moved from `Naming` to `Structure` — it's a structural parse error (wrong expression kind as callee), not a naming problem. `UnexpectedKeyword` reduced from 2 params to 1 — the old `{1}` context param was vague. Both codes now have FixHints. George owns emission sites only.
- **GAP-024 (bag/list/log TypeQualifier, 2026-05-02):** The parser's catalog-driven qualifier acceptance (element type decides, not collection kind) is architecturally correct per the metadata-driven principle. Qualifier semantics are orthogonal to container behavior — `bag of money in 'USD'` and `set of money in 'USD'` have identical constraint semantics. The spec §2.3 restriction to only set/queue/stack was incomplete notation, not deliberate design. Spec should be updated to match the parser, not the reverse. **RESOLVED 2026-05-02:** Spec §2.3 updated — `TypeQualifier?` added to all four collection forms (`bag`, `list`, `log`, `log by`). No C# changes needed. Owner (Shane) signed off.

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

- **GAP-029 fixed (2026-05-02, Frank):** `IsOutcomeAhead()` in `Parser.cs` was hardcoding `TokenKind.Transition or TokenKind.No or TokenKind.Reject` instead of deriving from the catalog. Added `OutcomeKeywords` static `FrozenSet<TokenKind>` field derived from `Tokens.All.Where(m => m.Categories.Contains(TokenCategory.Outcome))`, following the same pattern as `ActionKeywords`, `ModifierKeywords`, etc. Updated `IsOutcomeAhead()` to use `OutcomeKeywords.Contains(next.Kind)`. Pure drift-prevention fix — no behavior change since the three tokens are identical to what the catalog holds today. 2690 tests green.
- **GAP-031 fixed (2026-05-02):** Replaced three hardcoded binding power literals in `Parser.Expressions.cs` with direct catalog lookups: `not` → `Operators.ByToken[(TokenKind.Not, Arity.Unary)].Precedence` (25), unary negate → `Operators.ByToken[(TokenKind.Minus, Arity.Unary)].Precedence` (65), `is set` guard → `Operators.ByTokenSequence(TokenKind.Is, TokenKind.Set)!.Precedence` (60). Values are identical to what was hardcoded — this is a drift-prevention fix, not a behavior change. `ByToken` keying on `(TokenKind, Arity)` is the correct disambiguation for the `Minus`/`Negate` ambiguity. The `is not set` branch shares the same guard and needs no separate lookup. 2690 tests green. Completed 6-dimension pre-TypeChecker consistency audit. Dimensions 1, 2, 5, 6 clean. Two new gaps: (1) GAP-032 — `Functions.cs` `pow(integer, integer)` overload missing `ProofRequirement` for `exp >= 0`; spec §0.6 item 4 lists this alongside `sqrt` as an explicit non-negative proof obligation; `sqrt` has the correct `NumericProofRequirement`; `pow` has none. Fix: add `NumericProofRequirement(PPowExp, GreaterThanOrEqual, 0m, ...)` to the Integer^Integer overload. (2) GAP-033 — `ModifierKind.cs` line 22 XML comment "Flag: string is non-empty" is stale after GAP-025 expanded applicability; analogous to GAP-027 (Tokens.cs) but in the enum file; one-line fix. Prior gaps GAP-025/026/028 all confirmed fixed. Parser vocabulary sets confirmed fully catalog-derived. `contains`/`is set`/`for` absent from Operations.cs is intentional design. `Dot`/`LeftParen` binding powers 80/90 are legitimate hardcodes (structural grammar constructs, not cataloged operators). Audit closed at 33 gaps total: 28 Fixed, 5 Unresolved.

- **GAP-025/026/028 resolved (2026-05-02, Frank):** Three catalog/implementation mismatches fixed in a single pass. Pattern: doc fixes (GAP-003, GAP-004) from prior iterations updated specs but left the actual C# catalogs (`Modifiers.cs`, `Functions.cs`) stale — the classic "doc-fixed-but-catalog-not-updated" failure mode. Key specifics: (1) `Notempty.ApplicableTo`: added `StringAndCollectionTypes` array (9 types: String + 8 collection kinds; Lookup excluded because lookup entries are defined at design time). `StringOnly` array was NOT removed — still used by `Minlength`/`Maxlength`. (2) `CollectionTypes`: extended from 3 to 9 members by adding Log/LogBy/Bag/List/QueueBy/Lookup — Lookup IS included here (contrast with Notempty) because constraining how many lookup entries exist is meaningful for mincount/maxcount. (3) `sqrt`: Integer→Number and Decimal→Number overloads removed per spec §3.7 "Number-lane only; use approximate() to convert first." Dead `PSqrtInteger`/`PSqrtDecimal` fields cleaned up. All 6 test failures cascaded from 3 catalog changes; test count updates: overload total 49→47, sqrt test assertions trimmed. Pre-TypeChecker audit closed: 28/28 gaps resolved.
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

### 2026-05-02 — Iteration 7 language consistency audit complete
- **GAP-019 closed:** Verified George's implementation. `InvalidCallTarget` (12) is emitted at the infix `LeftParen` branch when `left` is not a `MemberAccessExpression`; `UnexpectedKeyword` (11) is emitted in `ParseAtom` default fallback via catalog-derived `AllKeywordKinds.Contains()`. `AllKeywordKinds = Tokens.Keywords.Values.ToFrozenSet()` is correct. Message templates in `Diagnostics.cs` match spec §2.7 exactly. `DiagnosticCode.cs` "reserved" comments removed.
- **GAP-017 false-Fixed:** Summary table said Fixed but C# changes were never applied (iteration 5 completion comment: "no C# edits"). Applied in iteration 7: Arrow `Cat_Str` → `Cat_Op`; `TwoCharOperators` filter `or Structural` clause removed; doc comment updated. Zero behavior change — Arrow was already scanned correctly via the workaround.
- **GAP-003 incomplete (GAP-025):** Docs fixed (spec + 3 supporting docs) but `Modifiers.cs` not updated. `Notempty.ApplicableTo` still `StringOnly`; spec §3.8 requires string + 8 collection types. Filed as GAP-025 (Unresolved) — TypeChecker implications, needs owner sign-off.
- **GAP-026 new gap:** `Modifiers.cs` `CollectionTypes = [Set, Queue, Stack]` is stale — 6 new TypeKind members (Log, LogBy, Bag, List, QueueBy, Lookup) missing, affecting `mincount`/`maxcount` applicability per spec §3.8. Pure omission with no ambiguity. Deferred pending TypeChecker work.
- **GAP-027 fixed:** `Tokens.cs` `Notempty` description "String constraint: non-empty" → "String or collection constraint: non-empty" (one-word doc fix; catalog description field was not updated during GAP-003 resolution).
- **GAP-028 new gap:** `Functions.cs` `sqrt` has `Integer` and `Decimal` overloads but spec §3.7 says integer/decimal inputs are type errors. Likely a doc-fix-only gap from GAP-004 that missed the catalog. Owner judgment required.
- **Audit status after iteration 7:** 28 gaps total (24 original + 4 new). Fixed: 23. Unresolved: 5 (GAP-024, GAP-025, GAP-026, GAP-028, and... actually: GAP-024, GAP-025, GAP-026, GAP-028). All remaining Unresolved gaps are Doc-Catalog with TypeChecker implications — no further language surface investigation needed before TypeChecker work begins.

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


