# Frank History Archive



Archived updates moved from `history.md` during Scribe summarization.



---



## Archive Batch ΓÇö 2026-05-02T19:42:01Z



---



### 2026-05-01 ΓÇö Annotation rename and scope audit closed out

- Scribe merged `frank-handlesform-rename.md` into `decisions.md`; the durable per-member annotation name is `[HandlesCatalogMember]`, paired with `[HandlesCatalogExhaustively]`.

- Frank-9's full catalog-enum sweep found no currently-missing distributed-dispatch annotations anywhere in the codebase; centralized switch sites remain correctly covered by CS8509 instead.

- Legacy `[HandlesForm]` mentions in older notes should now be read as historical rename context, not active API guidance.



---



### 2026-05-01 ΓÇö Analyzer audit and Phase 2e planning tracked

- Frank-9-1 completed the analyzer recommendations audit, wrote `docs/working/analyzer-recommendations.md`, and identified the PRECEPT0020-PRECEPT0023 gap set.

- Frank-10 is appending Phase 2e Slices 28-32 to `parser-gap-fixes-plan.md`; that plan update remained in flight at closeout.



---



### 2026-05-01 ΓÇö Generic annotation bridge merged to ledger

- Canonical decision merged: the class marker is now `[HandlesCatalogExhaustively(typeof(T))]`, replacing the earlier parameterless `[HandlesExpressionForms]` direction.

- Durable enforcement shape locked as catalog-agnostic: `HandlesFormAttribute(object kind)` pairs with PRECEPT0019 so future catalog enums opt in without analyzer rewrites.

- Session closeout recorded the bridge under the full-vision annotation workstream and cleared the processed inbox artifacts.



---



### 2026-05-01 ΓÇö Annotation-bridge coverage design recorded

- Recommended the annotation-bridge pattern for expression-form coverage: handler methods claim responsibility with `[HandlesForm(...)]`, while pipeline classes opt in with parameterless `[HandlesExpressionForms]`.

- Locked the preferred enforcement stack as PRECEPT0007 for catalog completeness, PRECEPT0019 for handler-claim completeness, and xUnit for end-to-end parser coverage.

- Rejected parser-structure-coupled analysis as the wrong maintenance tradeoff; durable enforcement should analyze metadata and attributes.



---



### 2026-05-01 ΓÇö Parser coverage assertion follow-on locked

- Decision merged to canonical squad ledgers: parser coverage against `ExpressionFormKind` is worth doing, but as a follow-on slice rather than an expansion of the current parser-gap slice.

- Durable recommendation: use catalog-side explicit lead-token metadata plus xUnit assertions that parser dispatch handles every declared form.



## Archive Batch ΓÇö 2026-05-02T19:42:01Z (overflow trim)



---



### 2026-05-01 ΓÇö Full implementation review of 13-slice parser gap fixes

- Reviewed all 13 slices against plan, spec, and design intent. 9 slices CLEAN, 2 NOT IMPLEMENTED (Slice 2/GAP-A deferred, Slice 13 deferred), 2 CLEAN with acceptable design deviations.

- Key finding: `is set` implementation uses two separate nodes (IsSetExpression/IsNotSetExpression) instead of one with boolean ΓÇö acceptable improvement. Precedence 60 vs plan's 40 ΓÇö needs resolution.

- OperatorKind.IsSet/Arity.Postfix never added to catalog ΓÇö the multi-token operator gap. Shane chose Full DU (Option B) for Phase 2.

- PRECEPT0019 correctly implemented as generic catalog-agnostic analyzer but stays at Warning with suppression because TypeChecker/GraphAnalyzer lack annotations.

- Authored Phase 2 extended plan: 7 work items (AΓÇôG), 3 phases, strict dependency ordering, 13-point acceptance gate. No deferrals, no holes.

- Learning: when a plan specifies "add to catalog" but the implementation hardcodes a handler instead, the result works but violates the Completeness Principle ΓÇö always verify catalog additions were actually made.



---



### 2026-05-01 ΓÇö Analyzer gap review and Slices 28ΓÇô32 added to plan

- Authored `docs/working/analyzer-recommendations.md`: full coverage map of 19 existing analyzers, 4 identified gaps (AΓÇôD), 2 hardcoded-pattern targets, and prioritized candidate analyzer specs (PRECEPT0020ΓÇô0023).

- Shane approved all items ΓÇö no gaps, everything goes in the plan.

- Appended Phase 2e to `parser-gap-fixes-plan.md`: new phase header, Slices 29ΓÇô32 full specs, process note for `CatalogAnalysisHelpers.CatalogEnumNames`.

- Acceptance gate expanded from 13 to 14 points to include Phase 2e completion.

- Decision inbox entry filed: `.squad/decisions/inbox/frank-analyzer-slices-added.md`.

- Key sequencing: Slice 32 (PRECEPT0023) is deferred pending Phase 2b completion; Slices 29ΓÇô31 are independent and ready for George.



## Archive Batch ΓÇö 2026-05-02T19:42:01Z (overflow trim)



---



### 2026-05-01T20:36:28Z ΓÇö spike/Precept-V2 full review complete

- Frank's full architecture review is now durably recorded as APPROVED with 0 blockers and 3 guidance items for `spike/Precept-V2`.

- George-8 closed G3 by removing the RS1030 `Compilation.GetSemanticModel()` pattern and captured the Phase 3 G2 prerequisite as a TODO; the branch follow-on also includes coordinator commit `4d988d8` with commented-out `ConstraintKind` / `ProofRequirementKind` entries in `CatalogEnumNames` for future activation.

- Soup-Nazi-4 closed all 6 missing-test gaps from the review and the branch finished green at 2687 passing tests, so Frank's approval now stands with implementation follow-through complete.



## Archive Batch ΓÇö 2026-05-02T19:42:01Z (final compact pass)



---



### 2026-05-02 ΓÇö Full architecture review of spike/Precept-V2: APPROVED

- Reviewed entire branch (36ccec4..4831cb3): annotation bridge (PRECEPT0019), 4 catalog integrity analyzers (PRECEPT0020ΓÇô0023), parser fixes (GAP-A/B/C, is-set, method call, list literal, typed constant), ExpressionFormKind catalog (11 members), OperatorMeta DU shape, TokenMeta.IsValidAsMemberName, parser 3-file split, and docs.

- Build clean (1 pre-existing RS1030 warning). 2678 tests passing, 0 failures.

- Verdict: APPROVED with 0 blockers, 3 guidance items (G1: PRECEPT0023c naming, G2: CatalogEnumNames missing ConstraintKind/ProofRequirementKind for Phase 3, G3: pre-existing RS1030).

- Key finding: The `CatalogEnumNames` set in `CatalogAnalysisHelpers` does not yet include `ConstraintKind` or `ProofRequirementKind` ΓÇö their GetMeta switches still use discard arms. When Phase 3 drops the discard arms, those enums must be added to enable PRECEPT0007 enforcement.

- The annotation bridge pattern is production-ready and catalog-agnostic. Future catalog enums opt in without analyzer changes.

- OperatorMeta DU (SingleTokenOp/MultiTokenOp) with dual indexing (ByToken + ByTokenSequence) is architecturally sound.



---



### 2026-05-02 ΓÇö Iteration 7 language consistency audit complete

- **GAP-019 closed:** Verified George's implementation. `InvalidCallTarget` (12) is emitted at the infix `LeftParen` branch when `left` is not a `MemberAccessExpression`; `UnexpectedKeyword` (11) is emitted in `ParseAtom` default fallback via catalog-derived `AllKeywordKinds.Contains()`. `AllKeywordKinds = Tokens.Keywords.Values.ToFrozenSet()` is correct. Message templates in `Diagnostics.cs` match spec ┬º2.7 exactly. `DiagnosticCode.cs` "reserved" comments removed.

- **GAP-017 false-Fixed:** Summary table said Fixed but C# changes were never applied (iteration 5 completion comment: "no C# edits"). Applied in iteration 7: Arrow `Cat_Str` ΓåÆ `Cat_Op`; `TwoCharOperators` filter `or Structural` clause removed; doc comment updated. Zero behavior change ΓÇö Arrow was already scanned correctly via the workaround.

- **GAP-003 incomplete (GAP-025):** Docs fixed (spec + 3 supporting docs) but `Modifiers.cs` not updated. `Notempty.ApplicableTo` still `StringOnly`; spec ┬º3.8 requires string + 8 collection types. Filed as GAP-025 (Unresolved) ΓÇö TypeChecker implications, needs owner sign-off.

- **GAP-026 new gap:** `Modifiers.cs` `CollectionTypes = [Set, Queue, Stack]` is stale ΓÇö 6 new TypeKind members (Log, LogBy, Bag, List, QueueBy, Lookup) missing, affecting `mincount`/`maxcount` applicability per spec ┬º3.8. Pure omission with no ambiguity. Deferred pending TypeChecker work.

- **GAP-027 fixed:** `Tokens.cs` `Notempty` description "String constraint: non-empty" ΓåÆ "String or collection constraint: non-empty" (one-word doc fix; catalog description field was not updated during GAP-003 resolution).

- **GAP-028 new gap:** `Functions.cs` `sqrt` has `Integer` and `Decimal` overloads but spec ┬º3.7 says integer/decimal inputs are type errors. Likely a doc-fix-only gap from GAP-004 that missed the catalog. Owner judgment required.

- **Audit status after iteration 7:** 28 gaps total (24 original + 4 new). Fixed: 23. Unresolved: 5 (GAP-024, GAP-025, GAP-026, GAP-028, and... actually: GAP-024, GAP-025, GAP-026, GAP-028). All remaining Unresolved gaps are Doc-Catalog with TypeChecker implications ΓÇö no further language surface investigation needed before TypeChecker work begins.



- George-7 completed the mechanical propagation of Frank's catalog-agnostic annotation rename: active code, analyzer, tests, and docs now use `[HandlesCatalogMember]`.

- Future architecture guidance should treat `[HandlesForm]` as retired terminology preserved only in historical notes and archived design context.



---



## Archive Batch ΓÇö 2026-05-02T21:58:21Z (scribe compaction)



---



## Recent Updates



### 2026-05-02 ΓÇö George review of type checker design (RESPONDED)

- George reviewed `docs/working/type-checker-design-analysis.md` and flagged 6 concrete implementation issues. Frank accepted 5/6 (Finding 5 non-finding: GAP-032 already closed).

- Key design revisions locked: (1) Use existing `FindCandidates`/`FindUnary` + ~15-line qualifier disambiguation, no new indexes. (2) Pre-Slice 0 shape commit required before numbered slices. (3) Array-primary field storage + FrozenDictionary secondary. (4) `ActionSecondaryRole?` enum on `TypedInputAction`. (5) Per-slice `[HandlesCatalogMember]` stub migration mandatory. (6) ContentValidation becomes DU (Regex/NodaTime/ClosedSet). (7) Error recovery: always emit partial results with `TypedErrorExpression`. (8) `QualifierBinding?` on typed binary expressions for qualifier propagation. (9) MethodCallExpression = accessor lookup (Slice 3). (10) InterpolatedString = Slice 3, InterpolatedTypedConstant = Slice 4.

- Response written: `docs/working/frank-response-to-george-review.md`. Revised 10-slice plan included.

### 2026-05-02 ΓÇö Historical Summary (fully compacted)

- Detailed early-May review and audit entries were moved to `history-archive.md` to keep active context under the size gate.

- Active durable takeaways: the annotation bridge is production-ready, PRECEPT0020ΓÇôPRECEPT0023 remain the analyzer hardening path, and post-review parser/catalog audits should continue to prefer catalog-derived surfaces over hardcoded parser vocabulary.

- Use `history-archive.md` for the full approval/audit narrative when reconstructing the May 1ΓÇô2 branch review trail.



### 2026-05-02 ΓÇö Active focus snapshot

- The branch-level review baseline remains: `spike/Precept-V2` reviewed APPROVED with guidance only, and future PRECEPT0007 activation still depends on adding `ConstraintKind` / `ProofRequirementKind` only after their discard arms are removed.

- The active audit baseline remains: parser gap follow-up should keep `InvalidCallTarget` / `UnexpectedKeyword` behavior, catalog-derived vocabulary sets, and the remaining doc-catalog TypeChecker follow-ons in sync.



### 2026-05-02 ΓÇö Iteration 10 catalog/spec/doc audit (Frank)



**5 new gaps filed (GAP-043ΓÇô047). 3 fixed inline; 2 pending owner decision.**



**Regression checks (iteration 9 fixes):** GAP-034 (SimpleCollectionTypeLeaders catalog-derived Γ£à), GAP-035 (ChoiceLiteralTokens on all 5 ChoiceElement types Γ£à), GAP-036 (ClearApplicable = {Set,Queue,Stack,Bag,List,QueueBy,Optional} Γ£à), GAP-037 (Writable hover updated Γ£à), GAP-039 (AppendByΓåÆLogBy, EnqueueByΓåÆQueueBy consistent Γ£à), GAP-040 (countof ElementParameterAccessor on BagAccessors Γ£à), GAP-041 (QuantifierPredicateNotBoolean code 106 Γ£à), GAP-042 (dead dispatch arms removed Γ£à). All 8 passed.



**GAP-043 (Fixed):** `catalog-system.md` described the system as 12 catalogs throughout (Status, Overview, inventory table, "Twelve catalogs in two groups", Derive/never-duplicate, Architectural Identity). `ExpressionForms.cs` explicitly says "The 13th catalog" in its doc comment ΓÇö it was added after the doc was written and the doc was never updated. Fixed: updated all "twelve"ΓåÆ"thirteen" instances (8 edits), added ExpressionForms as #9 in the Language Definition table (renumbering ConstraintsΓåÆ10, ProofRequirementsΓåÆ11, DiagnosticsΓåÆ12, FaultsΓåÆ13), updated the escape-hatch clause to "fourteenth aspect", updated "8 language definition catalogs"ΓåÆ"9", and added "expression forms" to the enumerated coverage list in Derive/never-duplicate. Pattern: the 13th catalog entry note in ExpressionForms.cs was correct ΓÇö the doc just never caught up. Going forward: when a new catalog file says "The Nth catalog" in its comment, that number is the signal that the catalog-system.md inventory needs updating.



**GAP-044 (Fixed):** Spec ┬º1.2 "complete v2 reserved keyword set" code block was missing `queue` and `stack`. Both `QueueType` (ordinal 70) and `StackType` (ordinal 71) have been in the lexer since v1, are in the Tokens catalog, and are excluded from "v2 additions" / "v3 additions" / all removals lists ΓÇö meaning they were simply omitted from the ┬º1.2 block when it was originally authored. Fixed: added `queue  stack` before `bag list log lookup` in the collection-types keyword line of the ┬º1.2 code block.



**GAP-045 (Fixed):** Spec ┬º2.3 `ChoiceValueExpr := StringLiteral | NumberLiteral | BooleanLiteral` used `BooleanLiteral` as a terminal. `BooleanLiteral` is not a `TokenKind` ΓÇö the actual tokens are `True` and `False` (text: `true`, `false`). All other boolean references in the spec use `True`/`False` or the keyword text. Fixed: changed `BooleanLiteral` ΓåÆ `true | false` in the grammar production.



**GAP-046 (Unresolved):** Spec ┬º3.7 places `~startsWith`/`~endsWith` in the "Functions catalog" table under "Functions are validated against a closed catalog." But `FunctionKind` has no CI members ΓÇö CI variants are handled via `HasCIVariant: true` on `StartsWith`/`EndsWith` FunctionMeta + `ExpressionFormKind.CIFunctionCall` in the ExpressionForms catalog. The spec's framing implies a `FunctionKind` entry that doesn't exist. Two valid resolutions: (1) add spec note clarifying the ExpressionForms/HasCIVariant mechanism, or (2) add `CIStartsWith`/`CIEndsWith` to `FunctionKind`. Owner decision required.



**GAP-047 (Unresolved):** Spec ┬º3.7 documents `min`, `max`, `abs`, `clamp`, `round(value, places)` with numeric-only (integer/decimal/number) signatures. The Functions catalog has money and quantity overloads for all five. For `round(money, places)` the spec's "bridge function" framing is also wrong ΓÇö it's not a lane bridge for money/quantity, it's a rounding-preserving domain operation. Owner must decide: expand ┬º3.7 rows, or add a cross-reference note to business-domain-types.md.



---



## Archive Batch ΓÇö 2026-05-02T21:58:21Z (full active-history compaction)



---



## Core Context



- Owns language research, spec wording, and cross-surface documentation for Precept's DSL and architecture.

- Converts owner/design decisions into implementable language-surface guidance for parser, catalog, and tooling work.

- Historical summary: corrected the expression-form catalog boundary, locked the separate `ExpressionForms` catalog shape and MCP propagation, authored the parser-gap implementation plan/spec audit, and established parser coverage as a metadata-backed validation problem rather than a runtime routing problem.



- G1 resolved: `DiagnosticId_MultiLeadCollision` / `MultiLeadCollisionRule` renamed to `DiagnosticId_MultiSequenceCollision` / `MultiSequenceCollisionRule` in `Precept0023OperatorsDUShapeInvariants.cs`. The diagnostic checks the full `(TokenKind, TokenKind?, TokenKind?)` sequence, not just the lead token ΓÇö the old "MultiLead" name falsely implied a lead-token-only check. Sibling naming pattern (`SingleMultiCollision`, `MultiSequenceCollision`) is now consistent.



## Learnings



- **Research validation pattern established (2026-05-02):** First research validation artifact for a design doc. Pattern: (1) full analysis lives at `research/language/<design-doc-name>-research-validation.md` with front matter citing the validated doc, date, and corpus; (2) the design doc gains a `## Research Validation` section (after Deliberate Exclusions, before Cross-References) summarizing well-grounded decisions, justified divergences, and gaps; (3) working draft in `docs/working/` is marked superseded with a pointer to the promoted location; (4) `research/language/README.md` indexes all validation artifacts in a "Design validation artifacts" table. Key file paths: `research/language/type-checker-research-validation.md`, `docs/compiler/type-checker.md` (┬ºResearch Validation), `research/language/README.md` (┬ºDesign validation artifacts).



- **Canonical type checker review response (2026-05-02):** Responded to George's canonical review of `docs/compiler/type-checker.md`. Accepted all 5 concerns (CON-1ΓÇô5), accepted 3 of 4 missing items (MISS-1, MISS-2, MISS-4; rejected MISS-3 transitive widening), resolved all 3 red flags (RF-1ΓÇô3). Key new decisions locked (D-15 through D-25): single-hop widening, left-first widening fallback, bottom-up + context-retry for literals, EventHandlers have event arg scope, `FieldScopeMode` enum for forward-ref gate, quantifier binding stack with resolution priority (bindings > args > fields), function overload resolution algorithm (arityΓåÆexactΓåÆwidenedΓåÆretry), Slice 6 stays unsplit, Kramer R3 rejected (anti-mirroring), Kramer R4 accepted as placeholder. All 11 slices now implementation-ready. Canonical doc updated in-place. Critical file path correction: catalogs are at `src/Precept/Language/` not `src/Precept/Catalogs/`. AST class names differ from ExpressionFormKind names (e.g., `CallExpression` not `FunctionCallExpression`, `ParenthesizedExpression` not `GroupedExpression`).



- **GAP-046 design complete (2026-05-02):** Shane chose Option B ΓÇö add `FunctionKind.TildeStartsWith = 22` and `FunctionKind.TildeEndsWith = 23` to the Functions catalog. Key design decisions locked: (1) `CIVariantOf: FunctionKind? = null` new field on `FunctionMeta` ΓÇö the catalog-metadata-native way to express the CIΓåÆbase relationship (inverse of `HasCIVariant`); (2) Parser Tilde null-denotation path does NOT change ΓÇö it already derives CI-capable names from `HasCIVariant` on base functions, which is unchanged; (3) Overload signatures are `(string, string) ΓåÆ boolean` ΓÇö `~string` first-arg enforcement is a TypeChecker behavioral rule, not a distinct TypeKind in the overload signature (that would require `TypeKind.CIString` and conflict with the bidirectional assignment-compatibility rule); (4) `ExpressionForms.CIFunctionCall` HoverDocs updated with cross-reference only ΓÇö the form entry stays, it describes structure not semantics; (5) Spec ┬º3.7 gets a footnote after `~endsWith` row confirming CI functions now have `FunctionKind` entries and clarifying the two-catalog structure; (6) Open deferred concern: no `DiagnosticCode` exists for `~startsWith` called with non-~string first arg (spec says "compile error" but no code is defined) ΓÇö TypeChecker-slice decision, not blocked. Brief: `.squad/decisions/inbox/frank-gap046-design.md`.



- **Type Checker canonical doc consolidated (2026-05-02):** Replaced the 157-line stub at `docs/compiler/type-checker.md` with the full design spec (~500 lines) consolidated from three working documents: Frank's original analysis, George's implementer review, and Frank's response. All 13+ design decisions locked, SemanticIndex shape fully specified (array-primary + frozen dict secondary), 2-pass/3-sub-pass architecture documented, Pre-Slice 0 + 10 numbered slices with dependency graph, error recovery policy per-declaration type, HandlesCatalogMember migration protocol, and catalog gap status. Working documents in `docs/working/` preserved as design discussion record.



- **Type Checker Design Analysis (2025-07-14):** Completed full 5-section design analysis. Key architectural insight: the type checker is ~70-75% metadata interpreter (catalog lookups for Operations, Functions, Types, Modifiers, Actions) and ~25-30% structural logic (symbol tables, scope, cycle detection, choice validation). Expression typing is almost entirely catalog-driven ΓÇö the Operations catalog's ~200 entries encode the full type algebra; the checker just queries it. Identified 5 catalog gaps: (1) TypedConstant ContentValidation on TypeMeta (HIGH ΓÇö prevents per-type switch), (2) Scope rules (SKIP ΓÇö tiny/structural), (3) TypedActionShape on ActionMeta (MEDIUM ΓÇö explicit > derived), (4) CI enforcement diagnostics (LOW ΓÇö stable 5-rule surface), (5) pow ProofRequirement (existing GAP-032). Proposed SemanticIndex uses ImmutableDictionary for symbols (O(1) lookup) and ImmutableArray for normalized declarations (ordered iteration). Resolution architecture: two-pass (registration ΓåÆ checking), where checking has 3 sub-passes: expression resolution engine (the metadata interpreter core), declaration normalization (thin per-construct wiring), structural validation (cycles, choices, cross-validation). Key missing catalog infrastructure: `Operations.BinaryBySignature` and `Operations.UnaryBySignature` frozen indexes for efficient (op, type, type) ΓåÆ OperationMeta lookup. 10-slice vertical decomposition proposed with clear dependency graph.



- **GAP-033 fixed (2026-05-02, Frank):** Stale XML doc comment on `ModifierKind.Notempty` updated: "Flag: string is non-empty" ΓåÆ "Flag: string or collection is non-empty (string + 8 collection types; Lookup excluded)." `StringAndCollectionTypes` in `Modifiers.cs` = String + Set/Queue/Stack/Log/LogBy/Bag/List/QueueBy (9 types). Lookup deliberately excluded ΓÇö lookup entries are design-time defined. One-line doc-only fix. Same pattern as GAP-027 (Tokens.cs). Build clean.

- **GAP-019 (InvalidCallTarget/UnexpectedKeyword, 2026-05-02):**The "unreachable" comment at the infix LeftParen branch is misleading ΓÇö it's only true for bare `identifier(` (consumed in ParseAtom), not for `42(args)` or `(A+B)(args)` which reach the branch with non-MemberAccess left operands and silently break. Fix is one line (`EmitDiagnostic(InvalidCallTarget, ...)`) before the break ΓÇö this is a parser-phase fix, not TypeChecker. `UnexpectedKeyword` has no identified emit site; spec ┬º2.7 should explicitly mark it reserved rather than listing it as active.

- **Diagnostic activation (Codes 11 & 12, 2026-05-02):** Elaine's UX review confirmed DISTINCT failure modes. Updated spec ┬º2.7, catalog (`Diagnostics.cs`), and `DiagnosticCode.cs` doc comments. Key architectural decision: `InvalidCallTarget` category moved from `Naming` to `Structure` ΓÇö it's a structural parse error (wrong expression kind as callee), not a naming problem. `UnexpectedKeyword` reduced from 2 params to 1 ΓÇö the old `{1}` context param was vague. Both codes now have FixHints. George owns emission sites only.

- **GAP-024 (bag/list/log TypeQualifier, 2026-05-02):** The parser's catalog-driven qualifier acceptance (element type decides, not collection kind) is architecturally correct per the metadata-driven principle. Qualifier semantics are orthogonal to container behavior ΓÇö `bag of money in 'USD'` and `set of money in 'USD'` have identical constraint semantics. The spec ┬º2.3 restriction to only set/queue/stack was incomplete notation, not deliberate design. Spec should be updated to match the parser, not the reverse. **RESOLVED 2026-05-02:** Spec ┬º2.3 updated ΓÇö `TypeQualifier?` added to all four collection forms (`bag`, `list`, `log`, `log by`). No C# changes needed. Owner (Shane) signed off.



- **Dapr-as-runtime research (2026-05-02):** Conducted deep analysis mapping Precept's model onto Dapr building blocks. Key findings: (1) Dapr Actors are the strongest fit for hosting Precept entity instances ΓÇö single-threaded turn-based execution maps cleanly to Precept's single-event-at-a-time semantics. (2) Dapr Workflows are a poor fit ΓÇö they model orchestration steps, not constrained state transitions; guards/constraints would be awkward as activity return values. (3) Dapr State Store is the persistence layer regardless of hosting model ΓÇö the state blob is `{state, fields, etag}`. (4) Pub/Sub is at-least-once and async ΓÇö fundamentally at odds with Precept's synchronous constraint validation; requires idempotency and dehydrate/rehydrate patterns. (5) The recommended architecture is Actor-hosted Precept instances with state store persistence, service invocation as the `Fire` endpoint, and pub/sub only for external event ingestion (not constraint enforcement). (6) Key tension: Dapr adds distributed infrastructure overhead to what is currently a pure in-memory constraint engine ΓÇö the value proposition only materializes when you need distributed entity hosting, not just constraint evaluation.



- ┬º0.4.1 "No loops" prohibits general iteration, recursion, and fixpoint-requiring constructs. Bounded quantifier predicates (`each`/`any`/`no`) are not iteration constructs ΓÇö they unfold to finite conjunctions/disjunctions, require no fixpoint reasoning, introduce no mutable loop variable, and terminate in bounded time over statically-declared finite collections. The spec amendment must draw this line explicitly; leaving it as a design-doc footnote is insufficient.

- The carve-out for bounded quantifiers depends on Expression Purity (┬º0.4.6) remaining non-negotiable. If predicates could mutate state or observe external context, the distinction between quantifier and loop collapses. The two principles are coupled.

- Philosophy compatibility: every core philosophical commitment (prevention, determinism, inspectability, compile-time-first checking) is satisfied by bounded quantifiers. No tension exists.

- Timing discipline: the spec amendment and the feature implementation belong in the same PR. Amending the spec preemptively introduces aspirational text, which is the exact problem the spec has been cleaned up to avoid.

- Q1 in collection-types.md ┬ºOpen Questions is resolved by the amendment. That resolution should be recorded in the same PR that lands the amendment and the feature.



- GAP-7/GAP-8/GAP-11 resolved in the language spec: `TypeQualifier` includes `to` for `ExchangeRate`, `contains` documents `BinaryExpression(Contains, ...)`, and number/boolean literals document the unified `LiteralExpression` node.

- GAP-6 resolved: period negation added to spec ┬º3.6. Catalog entry NegatePeriod = 8 in OperationKind.cs. NodaTime Period.Negate() is the backing implementation.

-The two exhaustiveness enforcement strategies (CS8509 for centralized switches, `[HandlesCatalogExhaustively]` + `[HandlesCatalogMember]` for distributed dispatch) are topology-dependent ΓÇö the decision is made at the commit introducing the dispatcher, not retrofitted.

- Annotation naming must be catalog-agnostic from day one: `[HandlesForm]` was ExpressionForm-specific naming on a system designed to be universal. Renamed to `[HandlesCatalogMember]` for symmetry with `[HandlesCatalogExhaustively]` and C#'s standard "member" terminology for enum values.

- Catalog inclusion is decided by **language surface**, not by whether a current consumer already needs the data.

- Multi-token operators such as `is set` still belong in the catalog; parser structure and catalog completeness answer different questions.

- Implementation plans must name exact insertion points inside methods, not just the method names.

- GAP-2 does **not** require removing `When` from parser boundary tokens; the boundary is already correct.



- **GAP-029 fixed (2026-05-02, Frank):** `IsOutcomeAhead()` in `Parser.cs` was hardcoding `TokenKind.Transition or TokenKind.No or TokenKind.Reject` instead of deriving from the catalog. Added `OutcomeKeywords` static `FrozenSet<TokenKind>` field derived from `Tokens.All.Where(m => m.Categories.Contains(TokenCategory.Outcome))`, following the same pattern as `ActionKeywords`, `ModifierKeywords`, etc. Updated `IsOutcomeAhead()` to use `OutcomeKeywords.Contains(next.Kind)`. Pure drift-prevention fix ΓÇö no behavior change since the three tokens are identical to what the catalog holds today. 2690 tests green.

- **GAP-031 fixed (2026-05-02):** Replaced three hardcoded binding power literals in `Parser.Expressions.cs` with direct catalog lookups: `not` ΓåÆ `Operators.ByToken[(TokenKind.Not, Arity.Unary)].Precedence` (25), unary negate ΓåÆ `Operators.ByToken[(TokenKind.Minus, Arity.Unary)].Precedence` (65), `is set` guard ΓåÆ `Operators.ByTokenSequence(TokenKind.Is, TokenKind.Set)!.Precedence` (60). Values are identical to what was hardcoded ΓÇö this is a drift-prevention fix, not a behavior change. `ByToken` keying on `(TokenKind, Arity)` is the correct disambiguation for the `Minus`/`Negate` ambiguity. The `is not set` branch shares the same guard and needs no separate lookup. 2690 tests green. Completed 6-dimension pre-TypeChecker consistency audit. Dimensions 1, 2, 5, 6 clean. Two new gaps: (1) GAP-032 ΓÇö `Functions.cs` `pow(integer, integer)` overload missing `ProofRequirement` for `exp >= 0`; spec ┬º0.6 item 4 lists this alongside `sqrt` as an explicit non-negative proof obligation; `sqrt` has the correct `NumericProofRequirement`; `pow` has none. Fix: add `NumericProofRequirement(PPowExp, GreaterThanOrEqual, 0m, ...)` to the Integer^Integer overload. (2) GAP-033 ΓÇö `ModifierKind.cs` line 22 XML comment "Flag: string is non-empty" is stale after GAP-025 expanded applicability; analogous to GAP-027 (Tokens.cs) but in the enum file; one-line fix. Prior gaps GAP-025/026/028 all confirmed fixed. Parser vocabulary sets confirmed fully catalog-derived. `contains`/`is set`/`for` absent from Operations.cs is intentional design. `Dot`/`LeftParen` binding powers 80/90 are legitimate hardcodes (structural grammar constructs, not cataloged operators). Audit closed at 33 gaps total: 28 Fixed, 5 Unresolved.



- **GAP-025/026/028 resolved (2026-05-02, Frank):** Three catalog/implementation mismatches fixed in a single pass. Pattern: doc fixes (GAP-003, GAP-004) from prior iterations updated specs but left the actual C# catalogs (`Modifiers.cs`, `Functions.cs`) stale ΓÇö the classic "doc-fixed-but-catalog-not-updated" failure mode. Key specifics: (1) `Notempty.ApplicableTo`: added `StringAndCollectionTypes` array (9 types: String + 8 collection kinds; Lookup excluded because lookup entries are defined at design time). `StringOnly` array was NOT removed ΓÇö still used by `Minlength`/`Maxlength`. (2) `CollectionTypes`: extended from 3 to 9 members by adding Log/LogBy/Bag/List/QueueBy/Lookup ΓÇö Lookup IS included here (contrast with Notempty) because constraining how many lookup entries exist is meaningful for mincount/maxcount. (3) `sqrt`: IntegerΓåÆNumber and DecimalΓåÆNumber overloads removed per spec ┬º3.7 "Number-lane only; use approximate() to convert first." Dead `PSqrtInteger`/`PSqrtDecimal` fields cleaned up. All 6 test failures cascaded from 3 catalog changes; test count updates: overload total 49ΓåÆ47, sqrt test assertions trimmed. Pre-TypeChecker audit closed: 28/28 gaps resolved.

- Philosophy/spec wording must match actual runtime guarantees; if they drift, flag the gap rather than silently rewriting either side.

- Durable research should preserve rationale, rejected alternatives, and concrete examples.

- Pratt parsing discovers expression form by reading tokens, so `ExpressionFormKind` is a coverage/validation axis, not a runtime parser routing key.

- Coverage enforcement should consume stable metadata and annotations, not parser implementation internals.



## Recent Updates



### 2026-05-02T21:58:21Z ΓÇö Canonical type checker batch closed

- Frank's canonical response is now durable: George's 5 concerns were accepted, 3 of 4 missing items were accepted, transitive widening stayed rejected, and the revised checker plan now marks all 11 slices implementation-ready.

- The active checker baseline now also incorporates cross-agent follow-through: Kramer's tooling review remains non-blocking, TypedTransitionRow.ResolvedArgs stays rejected as anti-mirroring, TypedEditDeclaration is retained only as a placeholder, and Soup-Nazi's 450-550 test estimate plus 3 non-negotiable gates define the implementation test bar.

- Supporting artifacts now live in docs/compiler/type-checker.md, docs/working/frank-response-to-george-canonical-review.md, docs/working/kramer-tooling-review.md, docs/working/soup-nazi-test-strategy-review.md, and the research cross-reference at docs/working/type-checker-research-crossref.md.



### 2026-05-02 ΓÇö Active focus snapshot

- Immediate open design items remain GAP-046 (CI function catalog membership direction) and GAP-047 (spec coverage for money/quantity overloads).

- The canonical checker design is now the implementation baseline; future work must preserve bottom-up plus context-retry literal resolution, array-primary field ordering, single-hop widening, qualifier propagation, and slice-by-slice [HandlesCatalogMember] migration discipline.



### 2026-05-02 ΓÇö Historical Summary (recompacted)

- Older recent-update detail was moved to history-archive.md during Scribe closeout to keep active context under the 15 KB gate.

- Use the archive for the full early-May slice logs, audit notes, and prior branch closeout narrative.



## Archive Batch ΓÇö 2026-05-03T02:52:51Z



---



## Recent Updates



### 2026-05-03T00:51:29Z ΓÇö Outcomes catalog ruling recorded

- Scribe merged Frank's outcomes-catalog batch into `.squad/decisions.md`, cleared both inbox variants, and recorded the manifest outcome as the durable team ruling.

- Durable rule: outcomes stay DU-only; do not add `OutcomeKind` / `Outcomes.cs` unless Shane explicitly reopens the decision. If radical-parser outcome handling needs more structure, keep it at `OutcomeProd()` and token-level metadata.

- The same batch carried forward the construct/action/outcome/slot explanation and noted the paired design-doc updates in `docs/compiler/parser-radical.md` and `docs/compiler/type-checker-radical.md`.



### 2026-05-02T21:03:20-04:00 ΓÇö Outcomes catalog ruling REVERSED

- Reversed section 0.8 of `docs/compiler/parser-radical.md`: outcomes now get a catalog (`OutcomeKind` + `OutcomeMeta` + `Outcomes.cs`), same two-level pattern as Actions.

- The `no transition` composition gap was the decisive argument ΓÇö token-level enumeration cannot reconstruct outcome-level abstractions without hardcoding domain knowledge.

- Updated section 0.7 and section 0.8 in `parser-radical.md`, cross-reference in `type-checker-radical.md`, and wrote decision to `.squad/decisions/inbox/frank-outcomes-catalog-revised.md`.

### 2026-05-03T00:26:00Z ΓÇö Grammar primer docs recorded

- Added ┬º0 "The Grammar of Precept" to docs/compiler/parser-radical.md and a concise cross-reference summary to docs/compiler/type-checker-radical.md.

- Grounded the primer in samples/trafficlight.precept, samples/insurance-claim.precept, and samples/loan-application.precept; the pass confirmed the flat, keyword-anchored grammar thesis rather than changing the design.



### 2026-05-03T00:15:16Z ΓÇö Radical parser slot field removed

- The radical parser doc now removes `ImmutableArray<ConstructSlot> Slots` from `ConstructMeta`; named parse positions live only as `Tag` nodes inside `Grammar`.

- Tooling/documentation should derive ordered capture names via `ExtractNamedCaptures(ParseRule)` at startup rather than maintain a parallel slot list.

- The parser rebuild recommendation remains Path C only on risk grounds; AI velocity collapses the schedule argument, so unresolved stashed-guard, split-modifier, and variant-action gaps are the only surviving case.



### 2026-05-02T19:11:32-04:00 ΓÇö Spec challenge response: implicit-knowledge argument withdrawn

- Shane challenged the `implicit grammar knowledge'' regression argument: if the parser was built from spec with AI, a rebuild from the same spec reproduces the same result; any divergence is a spec gap, not a reason to preserve code.

- Conceded fully. The regression argument is dead. Path C recommendation now rests solely on the three unsolved design gaps (stashed-guard, split-modifier, variant-action). If those are resolved on paper or shown to be spec-covered, Path C has no remaining case.

- Response written to docs/working/frank-spec-challenge-response.md.



### 2026-05-02T22:22:24Z ΓÇö Iteration 11 audit session recorded

- Scribe merged Frank's iteration-11 findings into the canonical ledger, cleared all current decision inbox files, and wrote the audit closeout logs.

- Cross-agent context to retain: the durable batch now bundles the spike type-checker directive, both catalog-driven type-checker reviews, GAP-047 closure, Frank's GAP-048ΓÇô056 doc/catalog gaps, and George's GAP-062ΓÇô067 catalog-impl gaps.

- Health gate result: decisions archive ran under the 7-day rule before merge; no history summarization was required after propagation.



### 2026-05-02T22:22:24Z ΓÇö Iteration 11 doc/catalog audit pass

- Filed GAP-048 through GAP-056 in the language-consistency ledger; Frank's pass added 9 unresolved doc/catalog gaps. Combined ledger state now stands at 64 total gaps, 49 fixed / 15 unresolved after the parallel iteration 11 catalog-impl pass.

- The dominant pattern is catalog lag behind the spec on declaration-shape metadata: guarded ensures, guarded state actions, and stateless event-hook trailing `ensure` all exist in the spec without matching `Constructs`/`Constraints` metadata.

- Queue-by semantics now need owner clarification in two places: whether `ascending` / `descending` belongs in the type catalog, and whether `dequeue ... by H` means keyed selection or something else.



### 2026-05-02T21:58:21Z ΓÇö Canonical type checker batch closed

- Frank's canonical response is now durable end-to-end: George's 5 concerns were accepted, 3 of 4 missing items were accepted, transitive widening stayed rejected, and the checker plan now marks all 11 slices implementation-ready.

- Cross-agent follow-through is part of the active baseline: Kramer's tooling review remains non-blocking but derivation-first, and Soup-Nazi's 450-550 test estimate plus 3 non-negotiable gates define the expected checker validation bar.



### 2026-05-02 ΓÇö Active focus snapshot

- Immediate open design work has shifted back to checker implementation: GAP-047 is now closed, while the rest of the checker shape questions are locked in docs/compiler/type-checker.md.

- Use docs/working/type-checker-research-crossref.md, docs/working/kramer-tooling-review.md, and docs/working/soup-nazi-test-strategy-review.md as the supporting context set behind the canonical checker doc.



### 2026-05-02 ΓÇö Historical Summary (fully compacted)

- Older active-history detail was moved to history-archive.md during Scribe closeout to keep Frank under the 15 KB gate.

- Use the archive for the earlier Dapr research notes, gap-by-gap audit trail, and prior batch closeout sequence.



### 2026-05-02T22:14:44Z ΓÇö GAP-047 closed

- Spec ┬º3.7 now explicitly documents the money/quantity overloads for `min`, `max`, `abs`, `clamp`, and `round(value, places)`, including same-qualifier requirements and qualifier-preserving results.

- The working gap ledger is fully closed for this audit pass: GAP-047 is Fixed, and the primitive numeric-lane shorthand is now explicitly separated from domain-type overload semantics.



### 2026-05-03T01:07:30Z ΓÇö Outcomes catalog reversal recorded

- Scribe corrected the canonical ledger to match Frank-5's reversed ruling: outcomes now use the two-level catalog pattern (`OutcomeKind` + `OutcomeMeta` + `Outcomes.cs`) while retaining `OutcomeNode` as the syntax-layer DU.

- Durable reason to keep front-of-mind: `no transition` is one outcome-level abstraction composed from two tokens, so token-category derivation alone cannot provide complete outcome enumerability without hardcoded composition logic.



- **Upstream pipeline coverage completed (2026-05-02):** Extended `docs/working/catalog-driven-pipeline.md` with ┬º3.0.1 (Lexer), ┬º3.0.2 (Parser), ┬º3.0.3 (Precept Builder) ΓÇö the three stages that sit before the type checker. Key findings: the lexer is already ~95% catalog-driven (keyword/operator/punctuation tables derived from `Tokens.All`); the parser under the radical design achieves ~85% (construct dispatch generic, Pratt loop irreducible); the precept builder is the MOST catalog-drivable stage of all ΓÇö pure structural assembly with a vanishingly small irreducible kernel (cross-construct name resolution only). The thesis update now lists all 8 pipeline stages. The builder is identified as the natural proof-of-concept stage for the catalog-driven inversion.



- **Radical AST options explored (2026-05-02):** Explored 6 options for eliminating per-construct AST node classes (Universal bag, flat array, source-generated, no-AST, CST-only, hybrid generic+typed). The hybrid (Option F: generic `ParsedConstruct` internal + thin typed accessor functions at consumption boundaries) is the most promising ΓÇö it makes the "parser is untouched" claim fully true while preserving type safety via ~5-line accessor functions per construct. The key tradeoff Shane is weighing: loss of C# pattern matching on node types (ergonomic regression) vs. elimination of per-construct AST classes (architectural purity). Option C (source generation) is the fallback if type safety cannot be compromised. CST-only (E) and raw array (B) are rejected as over-engineered or too fragile for Precept's problem size.



### 2026-05-03T01:07:30Z ΓÇö Radical AST options note recorded

- Scribe merged Frank's late-arriving AST design note into the ledger as a pending-owner-ruling record: Option F keeps generic `ParsedConstruct` internally, thin typed accessors at consumer call sites, and typed MCP DTOs at the boundary.

- Durable tradeoff: the hybrid model preserves parser zero-touch growth but replaces node-type pattern matching with `ConstructKind` dispatch plus accessors; Option C remains the explicit fallback if that ergonomics cost is rejected.





## Archive Batch ΓÇö 2026-05-03T14:18:15Z (scribe compaction)



---



## Recent Updates (compacted from active history)



### 2026-05-03T09:44:20Z ΓÇö compiler-and-runtime-design.md sync to catalog-first pipeline



Synced the overview doc to the 11 canonical stage docs written in the prior session. Key changes made:



- **Status header** updated from "Approved working architecture" to "Canonical design ΓÇö catalog-first pipeline"

- **Catalog count** corrected from 12 to 13 throughout; added `ExpressionForms` to the language-definition catalog list in ┬º2

- **┬º5 Parser** fully rewritten: old typed-node inventory (`FieldDeclarationSyntax`, `StateBlockSyntax`, `EventDeclarationSyntax`, etc.) replaced with `ParsedConstruct(ConstructMeta, ImmutableArray<SlotValue>, SourceSpan)` model; `MissingNode`/`SkippedTokens` terminology removed; parser/TypeChecker contract boundary updated to reflect that `TypeKind` is NOT stamped at parse time

- **SemanticIndex back-pointers** in ┬º6 updated: `ΓåÆ FieldDeclarationSyntax` ΓåÆ `ΓåÆ ParsedConstruct (FieldDeclaration)` throughout, symbols table `ΓåÆ syntax` column updated

- **Earliest-knowable kind table** in ┬º6 updated: `TypeKind on TypeRef nodes` moved to type-checker row; parser row now lists `SlotValue` subtype stamps only

- **Open questions inherited**: expression tree design open question from parser.md and type-checker.md surfaced in ┬º5 and ┬º6 with explicit "inherited from canonical doc" markers

- **Cross-references** added to all canonical stage docs (lexer.md, parser.md, type-checker.md, graph-analyzer.md, proof-engine.md, precept-builder.md, tooling-surface.md, mcp.md, language-server.md)

- **Grammar generation note** in ┬º13 cross-reference: flagged that the generator is designed but not yet implemented ΓÇö current `precept.tmLanguage.json` is hand-crafted



Durable rule: the overview doc (`compiler-and-runtime-design.md`) is the narrative layer over the canonical stage docs ΓÇö it summarizes and links, does not re-spec. Stage docs own their design details; the overview inherits open questions rather than resolving them.



### 2026-05-03T09:10:00Z ΓÇö Catalog-Driven Thesis Deviation Audit



Audited all 11 canonical pipeline stage design docs against the catalog-driven thesis. Findings:

- **2 real deviations** (tooling-surface.md hand-crafted grammar, mcp.md hardcoded firePipeline)

- **2 flagged open questions** that acknowledge the deviation (GraphState booleans, firePipeline)

- **1 structural concern** (type-checker switches on ConstructKind for dispatch, which is structural routing not per-member behavior ΓÇö acceptable)

- All 11 docs are architecturally sound. The thesis is thoroughly embedded. Deviations are known gaps with explicit open questions, not silent drift.

- Decision note written to `.squad/decisions/inbox/frank-thesis-deviation-audit.md`.



### 2026-05-03T05:21:49Z ΓÇö HandlesCatalog cleanup recorded

- frank-18 locked the Option F verdict: remove `[HandlesCatalogExhaustively]` / `[HandlesCatalogMember]` from Parser.cs, TypeChecker.cs, and GraphAnalyzer.cs, but retain the attribute type definitions for catalog-side use.

- frank-19 landed the cleanup: removed all 39 consumer annotations, deleted the two stale reflection enforcement tests, and left the repo building clean with 0 errors and 0 warnings.



### 2026-05-03T05:08:28Z ΓÇö AST clean-slate deletion recorded

- Deleted the entire src/Precept/Pipeline/SyntaxNodes/ tree (38 files including Expressions/) plus test/Precept.Tests/AstNodeTests.cs.

- SyntaxTree.cs, Parser.cs, and GraphAnalyzer.cs were trimmed to remove the remaining SyntaxNode references; build result is 0 errors, 0 warnings.

- Supersedes the earlier "preserve SyntaxNodes as the AST contract" note: the AST surface is now intentionally absent until the catalog-driven replacement lands.





### 2026-05-03T05:13:00Z ΓÇö Option F AST stub implemented



**Files created:**

- `src/Precept/Pipeline/SlotValue.cs` ΓÇö discriminated union with abstract `SlotValue` base + 17 sealed subtypes, one per `ConstructSlotKind` catalog member. Naming adjustments: `Language.Type` ΓåÆ `TypeMeta` (no bare `Type` class exists in `Precept.Language`); used `TypeMeta` for both `TypeExpressionSlot.Type` and `ArgumentListSlot.Args` tuple element. Expression-carrying stubs (`ComputeExpressionSlot`, `GuardClauseSlot`, `OutcomeSlot`, `EnsureClauseSlot`, `RuleExpressionSlot`) hold only `SourceSpan` with `// TODO: add typed Expression tree` comments.

- `src/Precept/Pipeline/ParsedConstruct.cs` ΓÇö `sealed record ParsedConstruct(ConstructMeta Meta, ImmutableArray<SlotValue> Slots, SourceSpan Span)`. Uses `ConstructMeta` (actual type name) not the task's placeholder `Construct`.



**Files updated:**

- `src/Precept/Pipeline/SyntaxTree.cs` ΓÇö added `ImmutableArray<ParsedConstruct> Constructs` parameter.

- `src/Precept/Pipeline/Parser.cs` ΓÇö updated stub constructor call to pass `ImmutableArray<ParsedConstruct>.Empty`.

- `src/Precept/Pipeline/GraphAnalyzer.cs` ΓÇö `AnalyzeExpression()` now takes `ParsedConstruct construct` parameter.



**Build result:** 0 errors, 0 warnings.





- Deleted Parser.cs, Parser.Declarations.cs, Parser.Expressions.cs implementation (Γëê28KB of parsing logic).

- Replaced with a 35-line stub matching TypeChecker pattern: returns empty SyntaxTree with no diagnostics.

- Preserved all SyntaxNode type declarations (SyntaxNodes/ folder) ΓÇö they remain the AST contract.

- Deleted 5 test files testing parser internals/behavior (ExpressionParserTests, ParserInfrastructureTests, SlotParserTests, ParserTests, SampleFileIntegrationTests).

- Trimmed 3 tests referencing deleted Parser fields from ConstructsTests, TokenMetaMemberNameTests, ExpressionFormCoverageTests.

- Final state: build clean, 2603 tests pass (2348 + 255).



### 2026-05-03T02:52:51Z ΓÇö Catalog-driven pipeline follow-through recorded

- Scribe merged Frank's consumer-architecture note plus Shane's accessor-layer ruling into the canonical ledger: keep consumers generic, keep MCP above raw parse output, and treat any accessor layer as YAGNI until a real caller proves otherwise.

- Scribe also recorded Frank's upstream coverage pass: lexer/parser/builder now sit inside the same catalog-driven pipeline thesis, with the builder identified as the strongest candidate for a first generic proof-of-concept stage.

- Detailed prior active-history entries were compacted into `history-archive.md` during this pass to bring Frank back under the 15 KB gate.



### 2026-05-03T01:34:25Z ΓÇö Radical AST options note recorded

- The pending-owner-ruling record now keeps Option F (generic `ParsedConstruct` internals + thin typed accessors at boundaries) as the preferred radical AST path, with source generation as the explicit fallback if ergonomics win.



### 2026-05-03T01:07:30Z ΓÇö Outcomes catalog reversal recorded

- The durable parser/type-checker rule remains: outcomes use the two-level catalog pattern while retaining `OutcomeNode` as the syntax-layer DU because `no transition` is an outcome-level abstraction that token categories cannot enumerate by themselves.



### 2026-05-02T22:22:24Z ΓÇö Iteration 11 audit session recorded

- Keep the audit baseline in mind: the doc/catalog gap set now centers on declaration-shape metadata lag, queue-by clarification, and the canonical checker implementation gate already locked in `docs/compiler/type-checker.md`.



### 2026-05-03T05:13:50Z ΓÇö Durable coordination state after Option F stub batch

- The live parser coordination surface is the generic Option F shape: `SyntaxTree.Constructs`, `ParsedConstruct`, and the 17-case `SlotValue` DU. Treat that as the downstream contract unless a later design decision replaces it.

- Keep consumer follow-through aligned with that baseline: generic consumers should not grow fake per-`ExpressionFormKind` exhaustiveness stubs or reflection tests unless real per-member dispatch returns.



### 2026-05-03T14:02:40Z ΓÇö Compiler overview and catalog-first wording batch recorded

- Frank synced `docs/compiler-and-runtime-design.md` to the canonical stage docs: the overview is narrative-only, the live parser contract is generic `ParsedConstruct`/`SlotValue`, `TypeKind` resolves in the checker, SemanticIndex back-pointers target `ParsedConstruct`, and the catalog count is 13 including `ExpressionForms`.

- Frank also corrected the worst stale architecture sentence in the overview: Precept does **not** extend by ΓÇ£add an enum member and fill an exhaustive switchΓÇ¥; the durable rule is ΓÇ£add a catalog entry, keep stages generic, let metadata shape completeness enforce correctness at declaration time.ΓÇ¥

- Thesis-audit baseline stays active: the only real remaining deviations are the hand-authored TextMate grammar and the hardcoded MCP `firePipeline`, and the ΓÇ£Precept InnovationsΓÇ¥ callout box still needs the same wording cleanup in a later pass.



## Archive Batch — 2026-05-03T22:22:27Z (scribe compaction)



---



## Core Context



- Owns language research, spec wording, and cross-surface documentation for Precept's DSL and architecture.

- Converts owner/design decisions into implementable guidance for parser, catalog, type-checker, and tooling work.

- Durable active baseline: catalogs remain the language truth; generic consumer flow should dispatch by `SlotValue` shape instead of construct identity; the generic `ParsedConstruct` direction remains acceptable for consumers, and any accessor layer stays deferred until a concrete need exists.



## Learnings



- Catalog schema diagram work (2026-05-03) produced a three-level visual section in `docs/language/catalog-system.md` at commit `5675b23`. Level 1 is a Mermaid flowchart with 4 subgraph layers (Lexical/Grammar/Semantic/Failure), all 13 catalogs, ConstructSlotKind as a helper node, and separate pipeline/tooling consumer arrow styles. Level 2 covers Constructs (ASCII anatomy), Modifiers (DU classDiagram), Operations (DU classDiagram), ProofRequirements (two classDiagrams separating meta from obligation instances), and Diagnostics/Faults (ASCII bidirectional duality). Level 3 is a reference table for all 13 catalogs with source-verified counts: ActionKind = 15 (8 original + 7 compound/extended), ConstructKind = 12, ConstructSlotKind = 17. The doc's open question about 11 vs 12 ConstructKind members is resolved by source: 12 is correct.



- LS enrichment features (did you mean? / code actions) require three catalog structure changes before LS implementation: (1) `Diagnostic.Args: ImmutableArray<string>` to carry raw template args through the compiler artifact; (2) `DiagnosticMeta.SuggestionSources: SuggestionSource[]?` to bind naming diagnostics to their fuzzy-match sources without per-code switches in the LS; (3) `ConstructMeta.ModifierDomain: ModifierDomain` to bind construct kinds to modifier DU subtypes without per-kind switches in the LS code action provider. Both `SuggestionSource` and `ModifierDomain` stay bare enums ΓÇö no per-member metadata, classification axes only. Naming-error "did you mean?" candidates: `UndeclaredField` ΓåÆ `SemanticIndex.Fields`; `UndeclaredState` ΓåÆ `SemanticIndex.States`; `UndeclaredEvent` ΓåÆ `SemanticIndex.Events`; `UndeclaredFunction` ΓåÆ `Functions.All`. `SemanticIndex` is unavailable for Lex/Parse-stage diagnostics; LS must guard accordingly.



- The `tree` variable name sweep (2026-05-03) found stale references in 7 files: `Compiler.cs`, `CompileRunner/Program.cs`, `ConstructsTests.cs`, `compiler-and-runtime-design.md`, `precept-language-spec.md`, `tooling-surface.md`, and `language-server.md`. All Roslyn `SyntaxTree` usages in analyzer tests/code are legitimate and left alone. Archived docs are not updated. The `docs/compiler/type-checker.md` still has many `SyntaxTree` type-name references (not caught by `\btree\b` word boundary) that will need a separate pass.



- `docs/compiler-and-runtime-design.md` is the narrative overview layer over the 11 canonical stage docs; it inherits open questions and cross-references the stage docs rather than silently resolving them.

- `SemanticIndex` is a flat semantic inventory, not a mirrored tree; any wording that frames it as annotated syntax is architectural drift.

- Catalog-first propagation means ΓÇ£add a catalog entry and keep consumers generic,ΓÇ¥ not ΓÇ£add an enum member and fill an exhaustive switch.ΓÇ¥

- The live generic parser contract is `ParsedConstruct(ConstructMeta, ImmutableArray<SlotValue>, SourceSpan)` plus `SyntaxTree.Diagnostics`; unresolved SlotValue subtype shape mismatches stay explicit until Shane locks them.

- Grammar `Tag(...)` captures are the slot system in the radical parser design; do not reintroduce `ConstructMeta.Slots` as mirrored truth.

- Outcomes need metadata when outcome-level meaning is compositional (`no transition` remains the durable example).

- The remaining explicit catalog-thesis tooling gaps are still the hand-authored TextMate grammar and the hardcoded MCP `firePipeline` array.

- Tree-shaped naming for the flat parser artifact remains suspect; Shane's current preference is `ConstructManifest` if the `SyntaxTree` rename moves forward.



- The 2026-05-03 `SyntaxTree` doc sweep confirmed the missed type-name drift in `docs/compiler/type-checker.md` and `docs/compiler/README.md`, and also cleaned stale internal references in `tooling-surface.md`, `language-server.md`, `compiler-and-runtime-design.md`, `precept-builder.md`, `fault-system.md`, and multiple archived design notes. The only remaining `SyntaxTree` mention under `docs/` is the intentional Roslyn reference in `docs/working/Archived/type-checker-research-crossref.md`; `dotnet build` stayed green after the sweep.

- Grammar anatomy for `StateEnsure` / `EventEnsure` must model `EnsureClause` and `BecauseClause` as separate slots, mirroring `RuleDeclaration`; the `because` reason remains mandatory even though it is no longer described as embedded inside `EnsureClause`.



- Gap register deprecation (2026-05-03): `catalog-gap-register.md` (#1ΓÇô43) and `structural-gap-register.md` (#44ΓÇô85) served as discovery artifacts and are now archived under `docs/working/Archived/`. Their content was migrated to canonical pipeline docs as Open Questions, making each stage doc self-contained. Nearly all gaps were already captured inline during canonical doc writing. The only genuinely missing gap was #55 (GraphEvent.IsInitial derivation) ΓÇö added to graph-analyzer.md. The execution model going forward: `cross-cutting-decisions.md` drives wave-sequenced resolution (Waves 0ΓÇô2 = Shane decisions, Waves 3ΓÇô5 = team-autonomous). Separate gap registers are superseded.



- **CC#1 resolved (2026-05-03):** Shane ruled Option A ΓÇö Roslyn-style typed expression nodes. Key requirements: (1) `ParsedExpression` is a sealed DU (~10 subtypes), parser output; `TypedExpression` is the corresponding sealed DU with resolved types, type checker output. (2) The expression tree is the ONLY strongly-typed layer ΓÇö rest of parser AST stays generic `ParsedConstruct`. (3) The set is closed by design ΓÇö new expression form requires C# code changes (new DU subtype + update all switch arms). (4) **Exhaustiveness enforcement** via sealed class hierarchy (compiler warnings) PLUS a Roslyn analyzer test that verifies all expression-DU switches are exhaustive at build time. This is the pattern: sealed hierarchy + analyzer test = compiler as correctness partner.



## Recent Updates



### 2026-05-03T14:28:59Z ΓÇö ConstructManifest rename shipped

- Frank-26 completed the `SyntaxTree` ΓåÆ `ConstructManifest` rename across 5 source files and 2 docs.

- Build succeeded after the rename and no test changes were needed for the batch.



### 2026-05-03T14:18:15Z ΓÇö Scribe post-batch sync recorded

- Merged the three Frank inbox files into `decisions.md`, deduplicating the overview-confirmation notes into the already-recorded compiler-overview sync while separately capturing Shane's `ConstructManifest` preference as the current rename target over Frank's `ParsedSource` recommendation.

- Wrote orchestration records for frank-23, frank-24, and frank-25; frank-25's `to` classification verification remains in flight with no canonical ruling yet.

- Summarized this history file into `history-archive.md` to bring Frank back under the 15 KB gate.



### 2026-05-03T14:02:40Z ΓÇö Compiler overview and catalog-first wording batch recorded

- Frank's completed doc-sync pass remains the active baseline: the overview is narrative-only, the live parser contract is generic `ParsedConstruct`/`SlotValue`, `TypeKind` resolves in the checker, SemanticIndex back-pointers target `ParsedConstruct`, and the catalog count is 13 including `ExpressionForms`.

- The durable wording correction remains active: Precept extends by adding catalog metadata, not by adding enum members and downstream exhaustive switches; the ΓÇ£Precept InnovationsΓÇ¥ callout box still needs the same cleanup in a later pass.



### 2026-05-03T09:10:00Z ΓÇö Catalog-thesis deviation audit baseline retained

- Frank's full 11-doc sweep still stands: the only real deviations from the catalog-driven thesis are the hand-authored TextMate grammar and the hardcoded MCP `firePipeline`, both already called out as tooling gaps rather than silent architectural drift.



### 2026-05-03T05:21:49Z ΓÇö HandlesCatalog cleanup remains recorded

- The Option F follow-through still stands: consumer-side `[HandlesCatalogExhaustively]` / `[HandlesCatalogMember]` annotations and their reflection tests were removed, while the attribute types themselves remain valid for catalog-side use.



### 2026-05-03T14:37:24Z ΓÇö Grammar doc accuracy confirmed against catalog

- Frank-27 completed a full review of docs/language/precept-grammar.md and corrected 9 material errors across slot-bearing examples, slot-kind totals, and invariant references.

- Durable baseline: the grammar doc now matches catalog reality for StateEntryList, InitialMarker, GuardClause, and the distinct ActionChain + Outcome slot shape in TransitionRow.

- The active grammar reference should now be treated as accurate on the reviewed slot/routing details unless a later catalog change reopens them.







### 2026-05-03T14:59:24Z ΓÇö ConstructManifest doc cleanup and slot rulings recorded

- Frank-29 swept stale `SyntaxTree` type-name references from the requested compiler docs and adjacent surfaces; build stayed clean. Commit `8baca9f`.

- Frank-30 locked `because` as a separate `BecauseClause` slot for ensure syntax; `RuleDeclaration` is the correct reference shape and `StateEnsure` / `EventEnsure` are the defect sites.

- Frank-31 locked the event-modifier shape to an individual `InitialMarker` slot and confirmed `terminal` remains a state modifier, not an event modifier.



### 2026-05-03T15:18:05Z ΓÇö Catalog diagram baseline and ownership routing recorded

- Frank-34's research memo is now the durable baseline for schema-diagram work: the live catalog system is 13 catalogs because `ExpressionForms` is in scope, and `ConstructSlotKind` is supporting schema rather than a catalog.

- User routing directive updated: Elaine owns both Mermaid and ASCII diagram rendering. Frank remains the architectural analyst/decision source for what the diagrams should communicate.

- The because-clause ledger closeout is also recorded: grammar docs already match the separate `EnsureClause` + `BecauseClause` slot anatomy, and George's optional-slot follow-up closed the last catalog-red defect.



### 2026-05-03T16:05:46Z ΓÇö Catalog gap registers recorded

- Frank's latest gap sweep is now the durable squad baseline: 5 gaps were already captured in `catalog-system.md`, and 34 more were identified across the 11 canonical docs.

- Use `docs/working/catalog-gap-register.md` for the catalog triage view and `docs/working/structural-gap-register.md` for the stage/interface structural blockers that still need owner decisions or design closure.

- Elaine-17 also reset the visual baseline for catalog-system Level 1: refer to the split topology + consumer-landscape pair instead of the former single 70-edge overview.



### 2026-05-03T16:20:17Z ΓÇö Structural gap register rename recorded

- Scribe logged Frank's `frank-register-rename` batch: `docs/working/structural-gap-register.md` is now the durable register name, with the old `pipeline-output-gap-register.md` wording retired.

- Durable baseline update: the structural register now extends through gaps #85, and `docs/working/catalog-gap-register.md` also absorbed the companion catalog gap from the same sweep.

- Scribe health pass: pre-check saw 2 inbox files, the merge processed 3 after a late inbox arrival, `decisions.md` was archived under the 7-day gate before merge, and no history file crossed the 15 KB summarization threshold.





## 2026-05-03 ΓÇö Cross-Cutting Coverage Audit



Audited all 12 out-of-scope items in catalog-gap-register.md against the corrected cross-cutting definition. Found 8/12 are cross-cutting (4 already captured, 4 need promotion: #10, #26, #28, #29). Swept 11+ canonical docs and found 5 additional uncaptured cross-cutting items (TokenMeta.SemanticTokenModifiers, TypeAccessor DU hierarchy, execution dispatch delegates, ActionMeta missing properties, stateless precept semantics). Overall coverage verdict: ~92% ΓåÆ ~97% after recommended fixes. Report delivered to `.squad/decisions/inbox/frank-cross-cutting-audit.md`.



## 2026-05-03 ΓÇö Audit recommendations applied



- Added cross-cutting decision entries #21ΓÇô#26 in `docs/working/cross-cutting-decisions.md`, including the new execution-dispatch and stateless-precept decisions plus the four audit promotions.

- Updated `docs/working/catalog-gap-register.md` with new gaps #41ΓÇô#43 and reclassified the eight mis-scoped items so the register now points at the correct cross-cutting entries.

- Deliberately skipped a separate umbrella decision for evaluator-output richness because #22ΓÇô#24 already provide the concrete navigation points without adding another layer of indirection.





## 2026-05-03 ΓÇö Gap Sequencing Strategy



Produced .squad/decisions/inbox/frank-gap-sequencing.md. Key finding: Shane's catalogΓåÆstructuralΓåÆcross-cutting order is backwards ΓÇö cross-cutting decisions (especially CC#1 Expression Trees, CC#2 SlotValue Shapes, CC#25 Execution Dispatch) are the root of the dependency graph and must resolve first. Recommended 5-wave attack sequence with 12 Shane-required decisions and ~50 team-autonomous resolution items.



### 2026-05-03T16:44:09Z ΓÇö Gap-register deprecation and wave driver recorded

- Frank-38 restructured `docs/working/cross-cutting-decisions.md` into the wave-ordered execution driver (Waves 0-5, 26 decisions, ownership labels), archived the two working gap registers, and migrated their unresolved content into canonical docs as inline Open Questions.

- Durable baseline: separate gap registers are retired; new gaps belong directly in the relevant canonical doc, while sequencing and ownership routing now live in `docs/working/cross-cutting-decisions.md`.

- Specific closeout: missing gap #55 (`GraphEvent.IsInitial` derivation) was added to `docs/compiler/graph-analyzer.md`, and the deprecation rationale is now captured in the decision ledger.





---



## Archived 2026-05-04T03:26:10Z



## Core Context



- Owns language research, spec wording, and cross-surface documentation for Precept's DSL and architecture.

- Converts owner/design decisions into implementable guidance for parser, catalog, type-checker, runtime, and tooling work.

- Durable active baseline: catalogs remain the language truth; generic consumer flow dispatches by metadata/shape instead of construct identity; CC#1 keeps a sealed typed-expression DU while the broader parser/runtime surface stays metadata-driven.

- CC#25 runtime baseline is now fixed for current planning: production Fire uses Option A + G (`PreceptValue` tagged value storage plus catalog-owned delegate arrays), while LS/MCP interactive tooling keeps traced tree-walk evaluation.

- TypeBuilder/source-generation paths remain recorded as analyzed alternatives, not the active SaaS architecture, unless deployment or inspectability constraints change.



- **Q2 resolved (2026-05-03):** Opcodes-in-CompilationResult was evaluated and rejected. The current architecture remains correct: opcodes stay in Precept.From() / the Precept executable model, CompilationResult stays analysis-only, and TypedExpression remains available to LS/MCP consumers. Shane accepted the decision on 2026-05-03.



- **Q1 resolved (2026-05-03):** Option B (`IEvaluatorTrace` hook) chosen for guard trace granularity. Shane confirmed 2026-05-03.



- **Opcodes-in-CompilationResult evaluation (2026-05-03):** Recommended NO — opcodes belong in the `Precept` executable model (built by `Precept.From()`), not in `Compilation`/`CompilationResult`. The canonical design already places opcode lowering in the Precept Builder stage. `Compilation` is the analysis snapshot for authoring surfaces; putting execution plans there conflates analysis with execution, wastes work on LS recompiles, and blurs the severance boundary. No architecture change needed — the current design is correct.



- **FromJson/ToJson/FromClr/ToClr naming locked (2026-05-03):** `TypeRuntime` delegate naming convention is From/To (relative to PreceptValue), not Read/Write (runtime POV) or the inverted Read/Write (caller POV). `ReadJson`→`FromJson`, `WriteJson`→`ToJson`, `ReadClr`→`FromClr`, `WriteClr`→`ToClr`, `ReadClrBoxed`→`FromClrBoxed`. The locked `TypeRuntime` surface is now: `FromJson`, `ToJson`, `ParseString`, `FormatString`, `BinaryExecutors`, `UnaryExecutors` on the abstract base; `FromClr`, `ToClr` on `TypeRuntime<T>`. `Get<T>` calls `ToClr`, `Set<T>` calls `FromClr` — both intuitive. Perspective B (caller POV) was rejected because it inverts the already-locked delegate signatures. Perspective A (runtime POV) was rejected because `Get<T>` calling `WriteClr` is indefensible as a public API. See §11 of `frank-cc25-q7-typed-api.md`.



- **ReadJson/WriteJson API design (2026-05-03):** Closed the JSON ingress/egress seam from the Fire-call lifecycle walkthrough. Phase 8 egress: `FormatValue` is replaced by `WriteJson(Utf8JsonWriter, PreceptValue)` — zero boxing for scalars, ref-region types are already-heap references cast and written directly. Phase 1 ingress: `StoreValue`/`ParseValue` replaced by `ReadJson(ref Utf8JsonReader, ref PreceptValue)` — `ref Utf8JsonReader` required because it's a ref struct; `ref PreceptValue` for consistent write-back. Null handling is call-site-only (check `TokenType == Null` before dispatching). Collection runtimes own their structural loop; element-type runtime handles individual elements. Token-advance ownership: call site advances to value token, `ReadJson` calls `GetXxx()`, call site advances at next iteration. `TypeRuntimeMeta` final surface (names superseded — see above): `ParseString`, `FormatString`, `BinaryExecutors`, `UnaryExecutors` unchanged; `ReadJson`/`WriteJson` are now `FromJson`/`ToJson`. `ExtractValue`/`StoreValue`/`ParseValue` eliminated from all hot paths.



- **CC#25 Fire data lifecycle walkthrough (2026-05-03):** Peak live footprint for one Fire under A+G is ~44-48 `PreceptValue` slots, total stack traffic is ~4,480 bytes, the working copy is the donated next-version slot array, and pooled arrays cut GC-visible allocation to the boundary objects. The remaining implementation questions are slot-array ownership transfer, eval-stack allocation strategy, JSON ingress/egress ownership, event-args representation, trace-path data structures, and multi-row working-copy pooling.

- **CC#25 final runtime recommendation (2026-05-03):** The real performance lever is representation, not dispatch. Replace boxed `object?` hot-path values with a 32-byte `PreceptValue` tagged struct and keep execution semantics on catalog-owned delegate arrays. `System.Linq.Expressions` stays an upgrade seam, not a v1 dual-path commitment.

- **CC#25 SaaS constraint resolution (2026-05-03):** TypeBuilder/source-generated CLR types only win under a different product shape. In the current SaaS, per-definition cold-start and loss of fine-grained inspectability outweigh warm-path throughput gains.

- Catalog schema diagram work (2026-05-03) produced a three-level visual section in `docs/language/catalog-system.md` with 13 catalogs in scope, `ConstructSlotKind` treated as support schema rather than a catalog, and Elaine owning the rendering while Frank owns the architectural message.

- LS enrichment features (did you mean? / code actions) require three catalog structure changes before LS implementation: `Diagnostic.Args`, `DiagnosticMeta.SuggestionSources`, and `ConstructMeta.ModifierDomain`; classification axes like `SuggestionSource` and `ModifierDomain` stay bare enums.

- The `tree` variable/type-name sweep confirmed the durable naming boundary: use `ConstructManifest` / `manifest` for the flat Precept parser artifact, while legitimate Roslyn `SyntaxTree`, parse-tree prose, and graph-theory tree language remain untouched.

- `docs/compiler-and-runtime-design.md` is the narrative overview layer over the canonical stage docs; it inherits open questions rather than silently resolving them, and `SemanticIndex` must stay framed as a flat semantic inventory rather than an annotated syntax tree.

- Gap-register deprecation (2026-05-03) is final: discovery registers were archived, unresolved gaps moved into canonical docs as Open Questions, and `docs/working/cross-cutting-decisions.md` is now the sequencing/ownership driver.

- **CC#1 resolved (2026-05-03):** `ParsedExpression` and `TypedExpression` are sealed DUs, the expression tree is the only strongly typed parser output layer, and exhaustiveness relies on sealed-hierarchy switches plus the annotation-bridge pattern for distributed dispatch.



### 2026-05-03T22:52:59-04:00 — CC#25 Q7 Design Challenges evaluated

- Accepted Shane's Challenge A: `IArgBuilder` revised from dictionary output to `PreceptValue[]` + `BitArray` presence mask. The dictionary was pragmatic not principled — args are fixed-schema structures that deserve slot arrays identical to fields. `ArgDescriptor` gains `SlotIndex`.

- Accepted Shane's Challenge B: `Restore(string?, Action<IFieldBuilder>)` removed from §8. Typed Restore promotes the wrong pattern — it creates a backdoor for assembling Versions from arbitrary CLR values, bypassing round-trip-faithful hydration. Canonical Restore takes `JsonElement` only.

- Durable constraint: every `Version` is either a product of the evaluation pipeline (Create/Fire) or round-trip-faithful restore from Precept's own serialized egress. No third path.



### 2026-05-03T22:33:30-04:00 — CC#25 Q7 Typed Ingress design delivered

- Designed the typed CLR ingress API for `Fire()` and `Inspect()`: fluent `Action<IArgBuilder>` builder with `Set<T>(string, T)` that validates name + type immediately at the call site.

- `TypeRuntimeMeta` gains `ReadClr: Func<object, PreceptValue>` — the parallel CLR→PreceptValue path alongside the existing `ReadJson` for JSON callers.

- Inspect partial-args handled naturally: omission = don't call `.Set(...)`. Builder's `BuildPartial()` skips completeness checks.

- Rejected anonymous objects (reflection, no AOT), generated types (complexity for v1), and positional overloads (fragile, unreadable).

- Key architectural property: both ingress paths converge to `IReadOnlyDictionary<ArgDescriptor, PreceptValue>` — the evaluator is unchanged.



### 2026-05-04T00:43:26Z — TypeRuntime-as-catalog analysis delivered

- Answered Shane's challenge: "Why not make TypeRuntime a full catalog?" — Answer: TypeRuntime is NOT a 14th catalog (it's not language surface), but it IS catalog-owned metadata.

- The correct shape: `TypeMeta` gains a `Runtime` property of type `TypeRuntime` (the abstract class with sealed subclasses). The abstract class hierarchy stays as the implementation shape, but it's owned by the catalog entry rather than maintained as a parallel array.

- Key distinction established: catalog DUs (like `ModifierMeta`) are metadata shapes consumers pattern-match on; implementation class hierarchies (like `TypeRuntime`) are behavioral implementations consumers call via virtual dispatch. TypeRuntime is the latter.

- Consumer access: `Types.GetMeta(kind).Runtime.WriteJson(...)` or via a derived `TypeRuntime[]` index for hot paths — derived from catalog, never a parallel copy.

- This aligns with the existing decision: "persistence behavior belongs on catalog metadata."



### 2026-05-04T00:27:39Z — Collections BCL-vs-Custom analysis delivered

- Revised position: BCL `System.Collections.Immutable` for all nine collection types. Seven direct, two via thin composition wrappers. Zero from-scratch persistent data structures at v1.

- Key finding: All four motivations for custom types (immutability, JSON round-trip, DSL accessors, persistent semantics for discard) are satisfied equally by BCL immutable types.

- Sortability solved via per-field `IComparer<PreceptValue>` built during `Precept.From()`, capturing TypeTag and direction modifier. Feed directly into `ImmutableSortedDictionary.Create()`.

- `PreceptValue` needs `IEquatable<PreceptValue>` + `GetHashCode()` for hash-based collections, but NOT `IComparable<PreceptValue>` (use per-field comparers instead).

- Risk reduction: from multi-month high-complexity custom data structures to days of thin wrapper work.

- Surfaced 3 new open questions: `ImmutableQueue` lacks O(1) `.Count` (need cached count wrapper), `~string` case-insensitive equality requires per-field `IEqualityComparer<PreceptValue>`, Bag zero-count cleanup is trivial wrapper logic.

- The existing `collection-types.md` already documents BCL backing for List (`ImmutableList<T>`) and QueueBy (`SortedDictionary<TPriority, Queue<TElement>>`), confirming the project's established BCL-first approach.



### 2026-05-04T00:15:36Z — CC#25 Collections + TypeRuntimeMeta Q&A delivered

- Answered Shane's two questions on collection backing types and TypeRuntimeMeta justification in `frank-collections-and-typemeta.md`.

- Collections: Precept-owned persistent immutable types (e.g., `ImmutableLog<PreceptValue>`) stored as heap refs in `slot.Ref`. Persistent semantics are non-negotiable for working-copy discard.

- TypeRuntimeMeta: design is correct; recommended rename to `TypeRuntime`, shape as abstract class + sealed subclasses. Defended against switch/delegate-struct/interface alternatives. It is behavioral catalog data, not scattered machinery.

- Surfaced 3 new open questions: composite element representation, builder pattern for ReadJson, element-type parameterization ordering.



### 2026-05-04T00:12:46Z — CC#25 Q2 boundary locked

- Frank-51's evaluation is now durable context: do not move opcodes into `CompilationResult`; keep lowering inside `Precept.From()` on the executable-model side of the boundary.

- Shane accepted the recommendation, and the public-analysis boundary stays unchanged: `Compilation` / `CompilationResult` remains an authoring snapshot while `TypedExpression` continues serving LS/MCP consumers.



### 2026-05-03T23:00:32Z — ReadJson / WriteJson API lock recorded

- Frank-48 closed the CC#25 JSON ingress/egress seam: ReadJson now owns typed value extraction, WriteJson owns symmetric egress, null handling stays at the call site, and collection runtimes own structural JSON loops.

- The locked TypeRuntimeMeta surface is ReadJson, WriteJson, ParseString, FormatString, BinaryExecutors, and UnaryExecutors, with ExtractValue and StoreValue / ParseValue kept out of Fire, Inspect, and Update hot paths.

### 2026-05-03T22:22:27Z — CC#25 corpus canonicalized

- Scribe merged 19 CC#25 inbox files into 7 durable ledger entries, deleted the processed inbox notes, and recorded the active runtime baseline as `PreceptValue` + catalog-owned delegate dispatch with TypeBuilder and lane-split alternatives explicitly closed.

- The Fire-call lifecycle walkthrough is now part of Frank's active context as the quantitative implementation baseline for A+G memory/ownership work.



### 2026-05-03T16:44:09Z — Gap-register deprecation and wave driver recorded

- Frank-38 restructured `docs/working/cross-cutting-decisions.md` into the wave-ordered execution driver (Waves 0-5, 26 decisions, ownership labels), archived the two working gap registers, and migrated their unresolved content into canonical docs as inline Open Questions.

- Durable baseline: separate gap registers are retired; new gaps belong directly in the relevant canonical doc, while sequencing and ownership routing now live in `docs/working/cross-cutting-decisions.md`.

- Specific closeout: missing gap #55 (`GraphEvent.IsInitial` derivation) was added to `docs/compiler/graph-analyzer.md`, and the deprecation rationale is now captured in the decision ledger.



### 2026-05-03T15:18:05Z — Catalog diagram baseline and ownership routing recorded

- Frank-34's research memo is now the durable baseline for schema-diagram work: the live catalog system is 13 catalogs because `ExpressionForms` is in scope, and `ConstructSlotKind` is supporting schema rather than a catalog.

- User routing directive updated: Elaine owns both Mermaid and ASCII diagram rendering. Frank remains the architectural analyst/decision source for what the diagrams should communicate.

- The because-clause ledger closeout is also recorded: grammar docs already match the separate `EnsureClause` + `BecauseClause` slot anatomy, and George's optional-slot follow-up closed the last catalog-red defect.





### 2026-05-04T00:52:48Z — TypeRuntime design locked

- CC#25 TypeRuntime architecture is now locked: TypeRuntime is catalog-owned metadata on TypeMeta, exposed as a Runtime property typed as the abstract TypeRuntime class with sealed subclasses.

- The separate TypeRuntimeMeta DU-through-Types variant is rejected. Keep one type catalog lookup, not parallel GetMeta / GetRuntime switches.

- Durable guidance: consumers call Types.GetMeta(kind).Runtime...; any indexed runtime table is derived from Types.All, never maintained as an independent source of truth.



### 2026-05-04T00:56:54Z — CC#25 slot vocabulary boundary locked

- Answered CC#25 Q1: parser-time construct slots and runtime field slots are different vocabularies with different owners and lifecycles.

- Durable boundary: `ParsedConstruct.Slots` / `SlotValue` stay compile-time only; runtime execution uses field slot indices in the `PreceptValue[]` working copy, with `SlotLayout` as the field-name-to-slot-index map built in `Precept.From()`.

- Shane accepted the answer; when discussion crosses parser and runtime layers, say **construct slots** vs **field slots** explicitly.



### 2026-05-03T21:12:30-04:00 — CC#25 Q2 locked; JSON-first API accepted

- Q2 is now fully locked: event args become `PreceptValue` inside the evaluator, and `LOAD_ARG` loads them into the evaluator's `PreceptValue[]` register file. The args-vs-fields asymmetry is lifecycle/ownership only.

- Public API primary ingress is now `JsonElement` for commit/update/create/restore data and args.

- `IReadOnlyDictionary<string, object?>` overloads are demoted to convenience extension methods for tests and in-process callers.

- `docs/runtime/runtime-api.md` stays unchanged until the implementation PR ships the runtime surface.



- **Q3 resolved (2026-05-03):** "Where do execution plans come from?" — confirmed existing architecture answers all 6 sub-questions. `ExecutionPlan` is a named type (opcode array + ResultType), compiled eagerly in Pass 5 of `Precept.From()`, embedded in owning structures (ExecutionRow.Guard, ActionPlan.Value, ConstraintDescriptor.Expression, FieldDescriptor.ComputedPlan), shared across Versions via the Precept reference, accessed by structural traversal not name-based lookup. No design change required.



### 2026-05-04T01:18:00Z — Q3 ledger merge recorded

- Frank-53's CC#25 Q3 recommendation is now merged into `decisions.md`; the durable runtime baseline remains unchanged because the existing architecture already answered the execution-plan origin questions.

- Durable statement: `ExecutionPlan` stays the eager Pass 5 compiled form owned by the immutable `Precept` model and reached structurally from guards, action values, constraints, and computed fields.



### 2026-05-04T01:18:00Z — Q4 ledger merge recorded

- Frank's working-copy recommendation is now durably recorded in `decisions.md`: each candidate row forks from the original `Version.Slots`, guards read only immutable source slots, and only the winning row can donate its working copy into the next `Version`.

- Durable constraint: no shared mutable working copy crosses row boundaries; pooling remains an optimization seam, not a semantic change.



### 2026-05-04T02:14:47Z — Q6 revised stack-depth decision accepted

- Shane accepted the revised CC#25 Q6 answer: stack-depth enforcement moves into the Type Checker as an LS diagnostic, with the builder reduced to a debug-assert trust boundary.



### 2026-05-03T22:42:48-04:00 — CC#25 Q7 Sections 8-9 delivered (instance construction + zero-boxing)

- Designed `IFieldBuilder` for typed instance construction via `Precept.Create()` and `Precept.Restore()`. Separate from `IArgBuilder` — different backing stores (slot array vs. arg dictionary), different validation rules (defaults vs. optional args), different output types.

- Method name stays `Create` (already established). `FieldBuilder.Build()` auto-fills declared defaults for unset fields, rejects missing non-default non-computed fields. Computed fields are never caller-set.

- No `partial: true` on `FieldBuilder` — a Version with missing required fields is structurally invalid. Test fixtures use `Restore()` or JSON with explicit nulls.

- Zero-boxing analysis: Option 1 (generic subtype `TypeRuntime<T>` on the existing abstract `TypeRuntime` class) is the correct approach. `Func<T, PreceptValue>` invoked on value type `T` does not box. The generic cast at the `Set<T>` call site is a reference-type pattern match — no allocation. Rejected `ITypeConverter<T>` (redundant), `Unsafe.As` (fragile for zero marginal gain), and accept-the-boxing (trivially eliminable).

- Key architectural property: `TypeRuntime<T>` is a natural extension of the already-locked sealed subtype hierarchy on `TypeRuntime`. Catalog retrieval is unchanged — `Types.GetMeta(kind).Runtime` still returns abstract `TypeRuntime`. The generic machinery is confined to the `Set<T>` ingress path.



### 2026-05-03T22:28:26Z — CC#25 Q7 typed egress API design note delivered

- Verdict: drop `object?` indexer, replace with `PreceptValue` indexer as escape hatch. `Get<T>(string)` is the primary typed access pattern — first-class, not aspirational.

- Field access on `Version`, arg access on `FiredArgs` (new type on `Transitioned`/`Applied` outcomes). Parallel surfaces with identical patterns — not artificially unified.

- Type-safety enforced via `TypeRuntimeMeta.ExpectedClrType` — single `==` check, `PreceptTypeException` on mismatch. Exception-based because type mismatch is a programming error, not a user-input error.

- Inspect results use the same surface — `Version?.Get<T>()` works identically on hypothetical results.



### 2026-05-04T03:26:10Z — CC#25 Q7 acceptance revision complete

- Frank's Q7 revision pass is fully accepted and merged into the squad ledger.

- All seven locked decisions are now durable context: From/To naming, no string JSON overloads, typed Restore removed, arg slot arrays with presence mask, zero-boxing `TypeRuntime<T>`, `FiredArgs` typed egress, and fluent typed builders for Fire/Inspect/Create.

- `IReadOnlyDictionary<string, object?>` convenience/extension methods are obsolete and removed from scope.



## Archive Batch — 2026-05-07T04:02:01Z (scribe compaction)



---



### 2026-05-07 — Wave 3 Round 2: canonical doc sweep recorded



- Closed 20 Wave 3 Round 2 markers across `evaluator.md`, `language-server.md`, `mcp.md`, `catalog-system.md`, and `graph-analyzer.md`; `diagnostic-system.md` CC#13 / CC#20 were re-verified complete with no doc edits.

- `evaluator.md` locked the `EventOutcome` DU follow-through: `Faulted(Fault)`, `Mutations` on `Transitioned` / `Applied`, enriched `Unmatched(EvaluatedRows)`, `RejectReason` closure, `AmbiguousDispatch`, and fire pseudocode alignment.

- `language-server.md` confirmed `Compilation.Tokens`, `SemanticIndex.References`, `TypeMeta.IsUserFacing`, and `ActionMeta.Description` as the hover source, while converting §13 open questions into decided notes.

- `mcp.md` closed null-data bootstrap, `firePipeline` scope, `EnsuresByState`, mutations payload, and unmatched guard-trace shape; `catalog-system.md` and `graph-analyzer.md` closed the `ConstraintMeta` hierarchy and wildcard ordering markers.

- Six genuine follow-up gaps were preserved for owner attention: `TokenMeta.SemanticTokenModifiers` (#41), `EventCoverageEntry` granularity, back-edge definition, `GraphEvent.IsInitial` derivation, TBD structural diagnostic codes, and `ActionMeta` LS/MCP alignment (#43).

- Validation remains unchanged: `dotnet build src/Precept/Precept.csproj` reports only the 3 pre-existing `SemanticIndex.cs` errors.



---



### 2026-05-07 — Wave 3 Round 1: canonical doc sweep recorded



- Closed 13 Wave 3 Round 1 markers across `docs/compiler/type-checker.md`, `docs/compiler/proof-engine.md`, and `docs/runtime/precept-builder.md`.

- `type-checker.md`: CC#9 `ConstraintIdentity` DU, CC#11 `RejectReason`, and the stale CC#1-era expression-tree note are now closed.

- `proof-engine.md`: catalog-gap #12 and #13 are closed, CC#1 / CC#5 follow-through notes are complete, and the stale initial-state OQ block is gone.

- `precept-builder.md`: CC#4 `Compilation.Tokens`, CC#11 `ExecutionRow.RejectReason`, and CC#7 `ConstraintMeta.StateAnchored` hierarchy documentation are now canonical.

- Validation remains unchanged: `dotnet build src/Precept/Precept.csproj` reports only the 3 pre-existing `SemanticIndex.cs` errors.



---



### 2026-05-06 — Wave 3 Round 2: canonical doc sweep



Swept six docs to close remaining open question markers by propagating locked CC decisions.



**evaluator.md:** `EventOutcome` DU updated with three changes: (1) `ImmutableArray<FieldMutation> Mutations` added to `Transitioned` and `Applied` variants (CC#23); (2) `Unmatched()` → `Unmatched(ImmutableArray<TransitionInspection> EvaluatedRows)` (CC#24); (3) `Faulted(Fault Fault)` added as 8th variant (CC#12, catalog gap #21). `FieldMutation` record defined: `(string FieldName, JsonElement? Before, JsonElement? After)`. Stale pending note for `RejectReason` replaced with factual closure (CC#11). `FaultCode` table updated — `AmbiguousDispatch` confirmed with `[StaticallyPreventable]` (CC#13). Implementation Note 7 closed. In-domain failures table gained `Faulted` row. Fire pseudocode updated with mutations and new Unmatched signature.



**language-server.md:** `Compilation.Tokens` OQ closed (CC#4). `SemanticIndex.References` OQ closed — Pass 2 reconstructs reference sites by walking typed declarations; `EnsuresByState` is the one first-class grouping (CC#22). `TypeMeta.IsUserFacing` OQ closed (CC#16). `ActionMeta.HoverDescription` OQ closed — `Description` field sufficient; `SnippetTemplate` is future catalog addition. §13 Open Questions 1–3 converted from unresolved to decided notes.



**mcp.md:** Null-data bootstrap OQ closed. `firePipeline` OQ closed as out-of-scope (catalog-gap #25). `SemanticIndex.EnsuresByState` OQ closed (CC#22). Mutations payload OQ closed (CC#23) — JSON example updated to `before`/`after` shape. Unmatched guard trace OQ closed (CC#24) — `evaluatedGuards` → `evaluatedRows` with `TransitionInspection` shape.



**catalog-system.md:** `ConstraintMeta` hierarchy OQ closed — full five-subtype hierarchy documented (`Invariant`, `StateResident`, `StateEntry`, `StateExit`, `EventPrecondition`; three state kinds subtypes of `StateAnchored`). CC#5, CC#13, CC#16, CC#19 verified complete.



**graph-analyzer.md:** Wildcard expansion ordering OQ closed — declaration order confirmed; note moved inline to §6.1. CC#10, CC#21, CC#26 verified complete.



**diagnostic-system.md:** CC#13 and CC#20 verified complete — no changes needed.



Pattern: When a pending note in prose contradicts an already-resolved code block in the same section, remove the note. When an OQ has a recommendation already (workspace/symbol, rename), convert to a decided note rather than leaving "(unresolved)".



---



### 2026-05-06 — Wave 2 cross-cutting decisions fully closed



- Closed all 11 Wave 2 team-autonomous items: CC#5, CC#10, CC#13, CC#14, CC#15, CC#16, CC#17, CC#18, CC#19, CC#20, and CC#22.

- Corrected stale Wave 1 checkbox drift for CC#3, CC#4, CC#6, CC#12, CC#23, and CC#24, plus the CC#26 status row, where the status table was already authoritative.

- Propagated the locked rulings through `cross-cutting-decisions.md`, `catalog-system.md`, `graph-analyzer.md`, `evaluator.md`, `diagnostic-system.md`, `language-server.md`, `type-checker.md`, and `proof-engine.md` with a clean build reported.



---



### 2026-05-06 — Wave 3 Round 1: canonical doc sweep



Swept three docs to close all open question markers by propagating the locked CC decisions.



**type-checker.md:** `ConstraintFieldRefs.ConstraintIdentity` changed from `object` to `ConstraintIdentity` DU (CC#9). `string? RejectReason` added to `TypedTransitionRow` (CC#11). Stale §14 "No expression tree parsing" bullet removed (contradicted CC#1 resolution already in the doc).



**proof-engine.md:** Five OQ blocks closed — `TryLiteralProof` scope (intentional, Strategy 1 = numeric only); Strategy 3 vs Strategy 4 boundary (explicitly specified — direct subject guard vs. relational guard); initial-state satisfiability blocking note corrected (CC#1 resolved design, remaining dependency = TC implementation); corresponding stale OQ block replaced with implementation note; `FieldModifierMeta.ProofDischarges` stale OQ removed (CC#5 canonical in catalog-system.md).



**precept-builder.md:** `TokenStream Tokens` added to `Compilation` code block (CC#4). `string? RejectReason` added to `ExecutionRow` code block (CC#11). `ConstraintMeta` DU hierarchy with `StateAnchored` abstract intermediate node documented after 5-way routing switch (CC#7).



Pattern: When a doc has a code block and a prose "pending" note about the same field, fix both in one edit — the code block and the prose must be consistent. When an OQ block is stale relative to an earlier resolved note in the same doc, remove it — don't leave contradictory annotations.



---



### 2026-05-06 — Wave 2 cross-cutting decisions: all 11 closed



- Frank-156's UX accuracy review fed directly into the same-day Elaine correction pass; the dead zero-arg `Possible` state and the undefined-event rendering error are durably closed.

- Frank-157-1's fit assessment became the acceptance bar for CC#8: once OQ-2 and OQ-3 closed, the proposal was fit to adopt.

- Frank-158-1 applied those closures in `event-inspection-proposal.md`, resolved CC#8 in the cross-cutting register, and unblocked CC#12.

- Wave 1 facilitation opened with CC#7 first; keep the hierarchical `ConstraintMeta.StateAnchored` recommendation attached to that handoff until Shane rules.



---



## Archive Batch — 2026-05-11T20:03:33Z



---



## Core Context



- Owns language research, spec wording, and cross-surface architecture documentation for the Precept DSL and runtime.

- Catalog metadata remains the language truth; runtime, tooling, and docs must derive behavior from durable catalog shape rather than enum-identity switch logic or parallel lists.

- Public API surfaces expose stable CLR/JSON interchange contracts; evaluator internals stay internal.

- Investigation docs can be archived once their outcomes are captured in canonical docs, proposals, or the squad decision ledger.



## Learnings



- frank-slice6-scope: Exhaustive analysis of Slice 6 constraint propagation scope. Shane asked whether `in`/`of` qualifier constraints should also be propagated through interpolated typed constants. Answer: NO — qualifier/dimension/presence obligations are resolved from FIELD DECLARATIONS via existing S2/S5, not from assignment provenance. All modifier `ProofSatisfactions` in the catalog are `Numeric` type — there are no qualifier-axis satisfactions on modifiers. The only gap is whole-value slots (`'{x}'` where x is e.g. money nonzero) which should inherit the source's numeric constraints. This is a ~10-line extension to S6, not a new slice. Qualifier inference from assignment flow (e.g., inferring `in 'USD'` on a bare `money` field from its RHS) is a different feature entirely and does not belong in the interpolation plan.

- frank-slice6-plan-patch: Slice 6 extended to cover whole-value slots (+10 LOC: ~80 → ~90). `GetMagnitudeSlotSource` renamed to `GetSlotSource` (now handles both magnitude and whole-value cases). Qualifier/dimension/presence constraints require NO propagation — S2/S5 resolve them from field declarations, not assignment provenance. Adding qualifier propagation in S6 would be architecturally redundant and could introduce conflicting proof paths. Tests increased from ~8 to ~10 (added whole-value positive case + whole-value negative case).



- frank-optional-notempty:`optional notempty` is a BUG — COMPILE ERROR MISSING. The `Notempty` catalog entry has no `MutuallyExclusiveWith: [ModifierKind.Optional]`, so the type checker's catalog-driven mutual exclusivity path never fires. The proof engine's `TryDeclarationAttributeProof` (Strategy 2) will use `notempty`'s ProofSatisfactions to discharge `.length > 0` obligations without a null guard, meaning proof obligations are treated as satisfied on a field that can be null — a logical inconsistency that violates the Totality guarantee. Fix: add `MutuallyExclusiveWith: [ModifierKind.Optional]` to the `Notempty` entry in `Modifiers.cs`. The existing `InvalidModifierForType` code would catch it but its message template is wrong for modifier-modifier conflict; a new `ConflictingModifiers` code (parallel to `ConflictingAccessModes` = 42) is the clean fix. The right DSL pattern for "optional but non-empty if present" is an explicit rule: `rule Field is not set or Field.length > 0`.



- frank-25: String holes in interpolated typed constants should be excluded entirely. The proof engine treats typed constant fields as opaque (unfoldable in satisfiability, no content inspection). Three of five proof strategies (Declaration Attribute, Guard in Path, Qualifier Compatibility) gain unconditional power when string holes are excluded — qualifier compatibility goes from unprovable to fully provable because `DeclaredQualifiers` are resolvable from typed field declarations but not from string values. Excluding string eliminates 26 compatibility-table rows, the runtime-deferred-check philosophy exception, and runtime content validation for interpolated results. The annotation alternative (`'{x:currency}'`) adds unnecessary complexity.

- frank-25 follow-up (revised): Corrected the frequency assessment — samples predate the feature (unimplemented crash stub), so zero sample usage is evidence of non-existence, not low frequency. The compositional typed-literal pattern (`set Balance = '{Amount} {Code}'` with `Code as currency`) is the PRIMARY use case. Revised proof analysis: S2/S3/S5 are fully provable unconditionally with typed holes (declaration-level, no value knowledge needed). S1/S10 are achievable only when ALL hole values are statically known (foldable defaults), requiring `GetTypeDefault` enhancement to decompose typed constants into numeric magnitudes. 3-of-5 unconditional is the correct theoretical maximum for the primary use case (runtime event arguments); 5-of-5 requires all-foldable holes. String exclusion is the prerequisite gate for all five strategies — without it, even S2/S5 lose qualifier resolution.

- frank-25 follow-up (constraint propagation challenge): Shane challenged whether modifier constraints on hole expressions (e.g., `Amount as number nonzero`) could satisfy S1/S10 obligations without literal values. Verdict: NO change to "3 of 5 unconditional." S1 is definitionally literal-value-only (checks `TypedLiteral.Value` as concrete decimal). S2 already uses modifier ProofSatisfactions for DIRECT references today — the gap is TRANSITIVE propagation through interpolated composition (tracing from `Balance` back through `'{Amount} {Code}'` to inherit Amount's `nonzero`). That requires a new proof strategy (Compositional Constraint Propagation) needing slot classification + dataflow provenance + inter-procedural analysis — architecturally sound but a downstream enhancement, not an S1/S2 refinement and not in scope for the interpolation plan.

- frank-25 follow-up (constraint propagation REVISED — scope reversal): Prior verdict was wrong. Framing the work as "inter-procedural abstract interpretation" inflated the complexity. The actual implementation is a one-hop trace: obligation target field → find all `TypedInputAction` assignments where RHS is `TypedInterpolatedTypedConstant` → extract magnitude slot source expression → check source field's modifiers via existing `SatisfactionCovers()`. This is ~80 lines — less than Strategy 4 (FlowNarrowing). Added as Slice 6 in the interpolation plan. The primary use case (`set Balance = '{Amount} {Code}'` with `Amount as number nonzero`) MUST produce a field the proof engine recognizes as nonzero — shipping without this contradicts the philosophy for the most common scenario. Three errors in prior reasoning: conflated general case with actual scope, let framing drive scope, defaulted to "downstream" when the guarantee depended on it.



- frank-24: Reversed the type-grammar-driven slot classification decision from frank-23. Shane's challenge — "accept any hole expression, stitch text, validate the combined result" — is architecturally sound. The per-type grammar tables (~250 lines of pattern matching + slot compatibility matrices) protect against a narrow class of structurally malformed forms (like `'1 {x} kg'`) that are (a) pathological, (b) caught at runtime anyway, and (c) caught at compile time when values are statically known via the existing content-validation pipeline. The simplification eliminates the grammar tables, the slot identity enum, diagnostic codes 120/122, the per-slot compatibility matrices, and the `string` exception as a special case. Code 121 (formatted temporal prohibition) is retained. The guardrail: opportunistic static substitution — when hole values are compile-time-known, substitute and validate immediately using the existing pipeline.

- Produced architectural brieffor skill rewrite (`frank-authoring-skill-architecture.md`). Key decisions: keep authoring/debugging as two separate skills (generative vs diagnostic cognitive mode); authoring tool order is quickstart → patterns → conditional domain tools → compile loop; `precept_syntax`/`precept_types` are on-demand reference not workflow steps; debugging is fully static (compile → `precept_diagnostic` per code → transition-table reasoning); `precept_diagnostic` is reactive in both skills; keep `precept/*` wildcard in agent definition; strike all references to `precept_language`, `precept_inspect`, `precept_fire`, `precept_update`.

- Three shared root causes explain most typed-literal completion bugs: quote-trigger context normalization, typed-constant boundary detection at unterminated end positions, and missing recovery branches for `NumberTyping` / `AfterPlus` slot phases.

- Invoked completion inside a typed constant cannot key solely on `TriggerCharacter == null`; clients may send an empty trigger character, and peer-expression inference must step left past the active typed-constant token.

- For domain-type bounds, qualifier semantics split by qualifier axis: exact unit match for `in 'kg'`, dimension membership for `of 'mass'`; currency remains an exact-match follow-up gap shared with `default`.

- Guard placement should come from slot metadata and parser protocol reality, not helper booleans or enum-identity switches that duplicate the catalog surface.

- Documentation drift often clusters around grammar slot order and guard position; fix the canonical docs the same pass as the source change.

- Typed literals remain on the current architectural boundary: compile-time literal validation through `TypedConstantValidation`, runtime JSON lanes through `TypeRuntime<T>`, and ISO/UCUM as embedded external datasets with Precept-owned metadata.

- Durable rationale belongs in decisions/research, not in ephemeral review comments or ad hoc implementation switches.

- Interpolated typed constants are completely unimplemented: the parser skips hole tokens without creating expression nodes, and the type checker maps `TypedConstantStart` to `TypedErrorExpression` with no validation. The spec (§2.5, §3.6) requires full expression parsing and context-type validation. This is architecturally distinct from B9–B12 (which fix assignment-level post-resolution checks) — I1 means no expressions exist to check in the first place. The `{expr}` syntax also precludes using curly braces for any non-interpolation semantic inside typed constants (killed C2 Approach B).

- The type checker's `expectedType` parameter in expression resolution is advisory only — it hints numeric widening and typed constant context but does NOT enforce assignment compatibility. `ResolveAction` and `ResolveFieldExpressions` both create typed nodes without post-resolution validation, silently accepting type and qualifier mismatches. Three structural gaps exist: (1) no post-resolution type/qualifier check on assignment targets, (2) `QuantityValidator` validates UCUM syntax but is dimension-blind — `TypedConstantContext` exists for qualifier threading but is unused, (3) `TypedArgRef`/`TypedFieldRef` expression nodes strip `DeclaredQualifiers`, making variable-to-field qualifier comparison impossible at the expression tree level. Diagnostic codes `TypeMismatch` (PRE0018), `DimensionCategoryMismatch` (PRE0069), and `QualifierMismatch` (PRE0068) all exist but are never emitted in these paths. The gap also applies to money fields — it is type-agnostic.

- B6 stayed entirely in `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs`: `TryGetBinaryPeerOperandType`, `TryResolveExpressionTypeEndingAtToken`, `TryResolveParenthesizedExpressionType`, `TryResolveIdentifierType`, and `TryResolveMemberExpressionType` now thread `ImmutableArray<DeclaredQualifierMeta>` beside `TypeKind`, and `TryGetTypedConstantContext` assembles binary peer sites with `new TypedConstantContext(peerType, peerQualifiers)`.

- The event-arg path had the same drop: current-event arg resolution in `TryResolveIdentifierType` and event-member lookup in `TryResolveMemberExpressionType` both needed `DeclaredQualifiers` propagation, not just the field path.

- Regression coverage is integration-style in `test/Precept.LanguageServer.Tests/CompletionHandlerTests.cs`: trigger a space after `'100 ` in rule/when binary expressions and assert the returned completion labels hard-filter to `USD` for both field-peer and event-arg-peer cases.

- Interpolation slot classification must be type-grammar-driven, not position-text-driven. Each type that supports interpolation defines a closed set of valid segment-sequence patterns; the type checker matches against these patterns, assigns slot identities on match, and emits a structural error on no match. Position-text heuristics fail for compound qualifier types (price, exchangerate) where two adjacent holes have different semantic identities, and for compound temporal forms where `+` bridges create multi-magnitude patterns.

- Formatted temporal types (date, time, instant, datetime, zoneddatetime, timezone) do not support interpolation — their content is positional character patterns with no independently typed components. This is consistent with the zero-constructor discipline and the canonical docs which show no interpolation examples for these types.



## Historical Summary



- Early May work locked the typed-literal boundary, the external-data posture for ISO/UCUM, and the requirement that durable rationale live in decisions/research instead of scattered implementation branches.

- Recent batches settled the when-guard parser model, grammar/spec doc-sync rules, terminal-state diagnostic gating, and the typed-literal implementation review loop.



### 2026-05-11T08:26:04Z — X1/X2 crash triage: D26 assertion + extension JS TypeError

- Shane reported two crashes triggered by opening/editing a file with `'1 {s}'` (interpolated typed constant assigned to a quantity field).

- **X1 (P0):** `ResolveLiteral` in `TypeChecker.Expressions.cs:141` has a deferred stub for `TypedConstantStart` that returns `TypedErrorExpression` without emitting a diagnostic. D26 assertion fires at `PopulateEventHandlers` (TypeChecker.cs:592), `Debug.Assert` → `Environment.FailFast` → exit code 0x80131163. Crashes on both `DidOpen` and `DidChange`, creating a crash-restart loop.

- **X2 (P2):** Inside `vscode-languageclient` library's debounced document-sync `Delayer` — `this.task` is `undefined` when the LS process dies because the connection teardown clears the callback before the pending promise resolves. Secondary to X1; no independent fix needed.

- Fix for X1: emit an Error-severity diagnostic before returning `TypedErrorExpression` in the `TypedConstantStart` branch, following the `ResolveMissing` pattern.

- X1 is related to frank-12's interpolation triage (C2 design conflict). The deferred stub exists because interpolated typed constants aren't implemented yet, but D26's contract wasn't satisfied.

- Decision filed: `.squad/decisions/inbox/frank-crash-triage.md`. Tracker entries: X1, X2 in `docs/Working/completions-bugs.md`.



### 2026-05-11 — optional+notempty mutual exclusion: spec update (doc-only)

- Updated `docs/language/precept-language-spec.md` with three changes: (1) `notempty` row in the modifier flag table now notes mutual exclusion with `optional`; (2) new blockquote after the `notempty on collections` note in the modifier validation section documents the mutual exclusion, cites `ConflictingModifiers` (C120), and shows the workaround pattern; (3) `ConflictingModifiers` (C120) row added to the Modifier value validation table.

- Diagnostic codes section does not exist as a standalone section in the spec — codes are documented inline in their respective validation tables. C120 is now covered in the modifier value validation table.



## Learnings

- When adding a new mutual-exclusion constraint between modifiers, update three locations in the spec: (a) the flag/modifier reference table row, (b) the inline applicability notes after the validation table, and (c) the modifier value validation table. There is no separate diagnostics section — codes are inline.



## Recent Updates



### 2026-05-11T08:32:57.386-04:00 — I1/X1 safe-stub fix shipped

- `ResolveLiteral` no longer returns a bare `TypedErrorExpression` for `TypedConstantStart`; the stub now emits an Error-severity TC diagnostic first, mirroring the `ResolveMissing` self-containment pattern.

- Reused existing `DiagnosticCode.TypeMismatch` instead of inventing a new code; the message text explicitly names the unsupported interpolated typed constant so users see a clear error and D26 is satisfied.

- Added regression coverage in `TypeCheckerTypedConstantTests` for the event-handler path and a full `Compiler.Compile(...)` non-crash path using `set q = '1 {s}'`; this closes X1 and leaves X2 closed-by-elimination.





### 2026-05-11 — C2 Approach B invalidated + interpolation bugs I1/I2/I3 triaged

- **C2 conflict confirmed:** `{expr}` interpolation syntax inside typed constants (`'...'`) fatally conflicts with UCUM arbitrary-unit curly-brace syntax (`'{widget}'`). The lexer parses `{widget}` as an interpolation hole, not a unit code. Approach B is dead. Replacement candidates noted: Approach E (square-bracket), F (prefix marker), or C-revisited (explicit declaration).

- **I1 (P1):** `ParseInterpolatedTypedConstant()` in `Parser.Expressions.cs` (line 444) skips all hole tokens without parsing expressions — unlike `ParseInterpolatedString()` which properly calls `ParseExpression()` per hole. The type checker (line 141) then maps `TypedConstantStart` to `TypedErrorExpression` with "deferred" comment. Result: no type checking of interpolated typed constant expressions at all. The spec (§2.5, §3.6) requires full expression parsing and validation. This is a type-safety hole.

- **I2 (P3):** No completions inside `{...}` holes of typed constants — the `CompletionHandler` doesn't detect interpolation-hole context inside typed constants.

- **I3 (P3):** No semantic tokens inside `{...}` holes — the `SemanticTokensHandler` has no expression nodes to walk (downstream of I1).

- Fix order: I1 → B9–B12 → I2 → I3. I1 is orthogonal to B9–B12 (different layers).

- Decision filed: `.squad/decisions/inbox/frank-interpolation-triage.md`.

- Tracker updated: `docs/Working/completions-bugs.md` — C2 updated with invalidation + alternative candidates; I1/I2/I3 filed as new entries.



### 2026-05-11T01:58:19Z — B9–B12 triage: type checker is qualifier-blind on assignments and defaults

- All four bugs share a common architectural gap: `ResolveAction` and `ResolveFieldExpressions` pass `expectedType` as an advisory hint but never validate the resolved expression's result type or qualifiers against the target field.

- B9 (bare integer → quantity): `IsAssignable(Integer, Quantity)` correctly returns false, but the result is never checked.

- B10/B11 (dimension mismatch in literals/defaults): `QuantityValidator` validates UCUM syntax only — dimension-blind. `TypedConstantContext` exists for qualifier threading but is unused.

- B12 (arg dimension mismatch): `TypedArgRef` strips `DeclaredQualifiers` from the expression tree — qualifier comparison is structurally impossible.

- Fix is three layers: (1) post-resolution type check on assignments, (2) dimension-aware quantity validation, (3) qualifier metadata on expression nodes.

- All diagnostic codes already exist (PRE0018, PRE0068, PRE0069). The gap also applies to money fields.

- Decision filed: `.squad/decisions/inbox/frank-b9-b12-triage.md`.



### 2026-05-11 — B9-B12 implementation plan authored

- Confirmed all four bugs share the same three structural deficits: (1) no post-resolution type/qualifier check in `ResolveAction` or `ResolveFieldExpressions`, (2) `QuantityValidator` is dimension-blind despite `TypedConstantContext` parameter existing, (3) `TypedArgRef`/`TypedFieldRef` expression nodes strip `DeclaredQualifiers`.

- Key line numbers confirmed: `ResolveAction` AssignAction case at lines 810–822, `ResolveIdentifier` arg-ref creation at line 549, field-ref at lines 571–572, `ResolveMemberAccess` arg-ref at line 1334, `ResolveFieldExpressions` default resolve at line 452.

- `TypedArgRef` and `TypedFieldRef` are positional records in `SemanticIndex.cs` (lines 20–33) — adding `DeclaredQualifiers` changes positional construction at ~15 call sites. Must audit all before committing.

- `DeriveUnitDimensionName` (TypeChecker.cs line 188) is `private static` and depends on two `FrozenSet<string>` constants — extraction required for `QuantityValidator` to use it. Cleanest path: `internal static` or shared utility.

- `TypedConstantContext` (TypedConstantParseResult.cs line 19) has only `PeerType` and `Operator` — no qualifier data. Extension to add `DeclaredQualifiers?` is backward-compatible via optional parameter.

- `IsAssignable` (TypeChecker.Expressions.cs line 1162) correctly returns `false` for `Integer → Quantity` — the check exists, it's just never called post-resolution in the assignment paths.

- Diagnostic codes PRE0018 (`TypeMismatch`), PRE0068 (`QualifierMismatch`), PRE0069 (`DimensionCategoryMismatch`) all exist with full metadata including examples and fault codes — zero new diagnostic infrastructure needed.

- `MoneyQuantityModifierRegressionTests` (line 138) has explicit gap-documenting tests that assert "no diagnostic" — these flip to assert diagnostics after the fix.



### 2026-05-11T05:34:40Z — B4/B5 retriage corrected the prior completion-bug closure

- Kramer's apostrophe-trigger coercion was a legitimate B1 fix for true expression/default sites, but it does not repair declaration-side qualifier literals.

- B4 (`quantity in '`) and B5 (`quantity of '`) still misroute through `TryGetEnclosingField(...)`, recover the outer `Quantity` type, and surface quantity-literal items instead of the active qualifier slot.

- Durable fix direction: qualifier-site resolution must happen before expression fallback, driven by parsed qualifier metadata / qualifier-shape slots, with concrete unit/dimension assertions replacing weak non-empty tests.



### 2026-05-11 — Empty typed-constant invocation diagnosis tightened

- The lexer/span layer was already correct for empty `''`; the real regressions were client-shape variance on invoked completion and token walks that failed to skip the active typed-constant token while recovering surrounding expression context.

- Record both null and empty trigger characters as invoked completion, and make peer-operand inference walk left past the current literal token.



### 2026-05-11T01:38:51Z — Terminal-state gating and parser follow-through are now durable

- Path-to-terminal warnings only make sense once at least one terminal state is declared, and lifecycle wording should name declared terminals explicitly.

- The paired parser/type-checker closeout also landed: non-associative operators use `meta.Precedence + 1` on the RHS, and typed constants inherit peer operand type context before the D13 bailout.



### 2026-05-10T23:31:04-04:00 — Typed-literal UX review approved the architecture and wrote Kramer's plan

- Elaine's typed-literal UX was approved as the behavioral contract: type-owned routing, qualifier-aware hard filtering, compound temporal in V1, and quiet free-form text.

- The implementation plan locked the 5-slice execution order: type branching, slot detection, qualifier threading, compound temporal continuation, and integration coverage.



### 2026-05-10T19:47:35Z — Grammar doc-fix batch durably recorded

- The comprehensive `precept-grammar.md` audit, the EventHandler trailing-ensure cleanup, and the final doc-alignment pass now live in the squad ledger.

- Durable guidance: document pre-verb `when` coverage everywhere StateEnsure / StateAction / EventEnsure / AccessMode appear, keep computed-field modifiers before `<-`, and remove dead construct-metadata claims once the source deletes them.



## Archive Batch — 2026-05-12T00:50:06Z



---



## Recent Updates



### 2026-05-11T20:25:57Z — DTO-free MCP catalog exposure rejected

- Raw catalog serialization is not an MCP-ready public contract today because abstract-base serialization drops subtype data, enums surface as numeric values, and runtime-shaped values leak transport-hostile structure.

- Durable direction: keep the curated MCP projection layer and reduce maintenance only by moving or generating the mapping logic instead of exposing raw core records.



### 2026-05-11T20:03:33Z — Slice 6 closeout recorded

- frank-6 confirmed that Slice 6 stays numeric-only and that qualifier/dimension/presence propagation should not be added because S2/S5 already discharge those obligations from field declarations.

- The same review identified the only scoped gap: a single-hole whole-value interpolated constant should inherit numeric constraints from its source field.



### 2026-05-11T20:03:33Z — Plan patch merged

- frank-7 updated the Slice 6 plan to use `GetSlotSource`, raised the estimate to roughly 90 LOC / 10 tests, and documented the no-qualifier-propagation rationale directly in the plan.

- The compile-time guarantee ruling still stands above the plan: simplification-by-runtime-validation is rejected.



### 2026-05-11T22:41:49Z — Squad batch closeout

- The interpolation slot-table decision to exclude `string` was restored and recorded canonically.

- Proof-engine qualifier coverage work remains the architectural backdrop for the typed-constants plan.

- `frank-5` inbox work was merged into the durable decision ledger and cleared from the inbox.





## Learnings



### 2026-05-11 — Q2 Locked: No qualifier inference on derivation operations

- Shane confirmed the ruling. Derivation operations (`MoneyDividePeriod`, `MoneyDivideDuration`, `MoneyDivideQuantity`) produce bare `price` — no temporal dimension qualifier is inferred from the divisor.

- Authors who need temporal chain proofs on a derived price must assign to a field explicitly declared with `of 'time'` or `of 'date'`.

- Rationale: qualifier inference would be implicit behavior violating Precept's explicit-domain-contracts principle. Assignment validation (Slice 10) handles the declared-field side.

- Recorded as D19 in `docs/language/business-domain-types.md`.



### 2026-05-11 — Temporal type system broad design completed

- Wrote `docs/Working/temporal-type-system-design.md` as standalone design document superseding the narrow Slice 11B approach.

- **Core finding: `DimensionCatalog` intentionally has no `"time"` entry.** This is correct — UCUM temporal atoms exist for compound units (`m/s`) but `quantity of 'time'` is and should remain a type error. NodaTime owns temporal semantics through `duration` and `period`.

- **Slice 11B architecture validated.** The type-gated temporal routing in `ExtractQualifiers` (price accepts `of 'time'`/`of 'date'`, quantity does not) is the right approach. `ImpliedQualifiers` on `TypeMeta` for duration's intrinsic time-dimension is the right catalog-metadata pattern.

- **Full interop matrix mapped.** All 30+ temporal operations cataloged with proof requirements. Key gap: `PriceTimesPeriod`/`PriceTimesDuration` need chain proof requirements (Slice 12 scope, depends on this design).

- **No breaking changes.** Exhaustive grep confirmed no existing `.precept` sample uses `quantity of 'time'`, `price of 'time'`, or `price of 'date'`.

- **Spotted missing operations:** `period × integer` and `period ÷ integer` are absent from the catalog despite NodaTime supporting them. Flagged as open question.

- **Three open questions for Shane:** (1) diagnostic guidance for `quantity in 's'` (recommend hint), (2) `money ÷ duration → price` qualifier propagation (recommend no inference), (3) `period × integer` missing operations (recommend add, separate issue).

- Decision record: `.squad/decisions/inbox/frank-temporal-type-system-design.md`.



### 2026-05-11 — Exhaustive proof engine × qualifier audit completed

- **Currency axis has near-total enforcement failure on money operations.** `MoneyPlusMoney`, `MoneyMinusMoney`, and all 6 money comparison operations have NO `QualifierCompatibilityProofRequirement`. Descriptions say "same currency required" but the catalog metadata is silent. `money in 'USD' + money in 'EUR'` compiles clean. Fix: add 8 catalog entries, ~20 LOC.

- **Cross-type qualifier chain validation does not exist.** `ExchangeRateTimesMoney`, `PriceTimesQuantity`, `PriceTimesPeriod` have no mechanism to validate that the from-currency/dimension of one operand matches the currency/dimension of the other. Requires new `QualifierChainProofRequirement` DU subtype (~50 LOC infrastructure).

- **Dimension-only fields produce false positives.** `quantity of 'mass' + quantity of 'mass'` triggers unresolved `QualifierAxis.Unit` obligation because `ResolveQualifierOnAxis` doesn't fall back from Unit to Dimension. Fix: axis fallback in Strategy 5, ~15 LOC.

- **ValidateAssignmentQualifiers** handles Dimension, Unit, Currency but NOT FromCurrency/ToCurrency. Exchange rate field-to-field assignment with different from/to currencies passes silently.

- **Expression results carry no qualifier provenance** — `set usdField = eurField + eurField` bypasses assignment qualifier checks because binary expression results are invisible to `ValidateAssignmentQualifiers`.

- **The S1-S5 strategy architecture is sound.** All gaps are catalog-metadata or resolution-logic gaps, not fundamental architectural gaps. No new strategy tier needed.

- **Numeric modifier subsumption is correct and comprehensive.** positive ⊇ nonzero ⊇ != 0. nonnegative does NOT subsume nonzero. All chains tested.

- Full audit report: `docs/Working/proof-engine-qualifier-audit.md`. Issue specs: `docs/Working/proof-gaps-issues.md`.



### 2026-05-11 — Dimension-qualified unit slot compatibility analysis

- `f1.unit` accessor resolves to bare `TypeKind.UnitOfMeasure` — no dimension qualifier is carried in the static type. `FixedReturnAccessor.Returns` is just a `TypeKind` enum; `ReturnsQualifier` metadata signals "which qualifier axis this extracts" for proof strategy use, not for narrowing the return type itself.

- `TypedMemberAccess` stores only `TypeKind ResultType` — there is no concept of "qualified return types" on accessor results anywhere in the type system.

- The interpolation plan's Slice 2 slot compatibility check is `TypeKind`-only. It will accept any `unitofmeasure` expression in a unit slot regardless of the source field's dimension vs. the target field's dimension.

- This is a real gap: `field f2 as quantity of 'mass' default '1 {f1.unit}'` with `f1 as quantity of 'length'` compiles clean but produces a dimensionally incoherent quantity at runtime.

- The gap is NOT interpolation-specific — static typed constants have the same underlying issue (content validation checks unit syntax/validity but not dimension compatibility against the field's declaration). Fixing this properly requires either type system enrichment (qualified return types on accessors) or a broader dimension-to-unit consistency validation pass.

- Decision: acknowledged gap, deferred to a separate issue. The S6 "no dimension propagation" rationale holds because S6's concern is obligation discharge, not slot dimensional consistency. The fix belongs in a cross-cutting validation feature, not as an S2/S6 bolt-on.



### 2026-05-11 — MCP DTO-free catalog serialization audit

- `tools/Precept.Mcp` currently carries 63 DTO records total: 36 in `LanguageToolDtos.cs`, 14 in `NewToolDtos.cs`, and 13 in `CompileToolDtos.cs`. `LanguageTool.cs` alone contains 33 mapping helpers because the MCP contract is not a raw mirror of core metadata.

- Direct `System.Text.Json` serialization of core catalog records is technically possible for many flat records, but the current raw output is not MCP-ready: enums serialize as numeric values, nested DU/base-typed properties lose subtype data, and object references like `TokenMeta` expand into noisy nested objects.

- Verified breakpoints from the live code: serializing `ModifierMeta` as its base drops `ApplicableTo` / declaration-site / scope-specific data; `OperatorMeta` as its base drops `Token` / `Tokens`; `ContentValidation` as its base drops closed-set and NodaTime subtype fields; `ActionMeta.ProofRequirements` loses comparison / threshold / subject details because `ProofRequirement` is serialized through the abstract base.

- `ImmutableArray<T>` is not the blocker — `ConstructMeta.Entries` serialized successfully. `FrozenSet<T>` also serialized when exposed directly from the closed-set validation path. The real friction is polymorphism and contract shape, not frozen/immutable collection support.

- Direct UCUM exposure is especially ugly today: `UcumExactFactor` contains `BigInteger`, and raw JSON expands numerator/denominator into `BigInteger` implementation detail objects instead of the MCP DTO's clean string numerators/denominators.

- There is no existing JSON contract configuration in `tools/Precept.Mcp`: `Program.cs` only wires the MCP host. No custom `JsonSerializerOptions`, no `JsonStringEnumConverter`, no `[JsonDerivedType]` / `[JsonPolymorphic]`, and no source-generated JSON context are present.



### 2026-05-11 — Dimension-unit consistency validation integrated into interpolation plan

- The earlier analysis in `frank-dimension-proof-propagation.md` was wrong about the static case: `QuantityValidator.Validate()` (lines 30–53) ALREADY checks dimension-to-unit consistency for static typed constants via `TypedConstantContext.DeclaredQualifiers`.

- The interpolated case is a real gap: `TypedMemberAccess` for `.unit` resolves to bare `TypeKind.UnitOfMeasure` with no dimension provenance. Slice 2's TypeKind-only slot compatibility check cannot detect `f1.unit` (length) going into a mass field.

- Fix chosen: Option B — structural AST pattern match. After slot assignment, pattern-match unit-slot holes for `TypedMemberAccess { ResolvedAccessor: FixedReturnAccessor { ReturnsQualifier: QualifierAxis.Unit }, Object: TypedFieldRef/TypedArgRef }`, extract dimension from receiver's `DeclaredQualifiers`, compare to target field's dimension. ~25 lines.

- Option A (type system enrichment) rejected: adding qualified return types to accessors would require a new type system concept with no proportionate benefit. The information is already available one hop away in the AST.

- New diagnostic: `DimensionMismatchInUnitSlot = 124`. Interpolation plan codes renumbered to 121/122/123 (from 120/121/122) because `ConflictingModifiers = 120` was added after the plan was written.

- Scope: physical dimension consistency for `quantity` and `price` unit slots. Temporal dimension and currency qualifier mismatch are excluded (different axes, narrower surfaces, separate tracking).



### 2026-05-11T22:07:10Z — frank-1 dimension-gap verdict recorded

- frank-1 confirmed that `.unit` access resolves to bare `unitofmeasure`, so the existing interpolation slot-compatibility pass cannot prove or reject dimension mismatches from source-field provenance.

- The deferral recommendation is preserved as rejected-path evidence only; Shane overruled it and the batch closed on a compile-time fix path.



### 2026-05-11T22:35:56Z — String exclusion from interpolation restored



- The plan integration work (proof audit merge) re-introduced `string` as a valid hole type with a "string exception" section and full slot compatibility table entries. This contradicted the prior decision (checkpoint 184, `decisions.md` line 48, `history.md` line 10).

- Removed `string` from ALL slot compatibility tables across all 9 typed constant types (money, quantity, price, exchangerate, duration, period, currency, unitofmeasure, dimension).

- Replaced "The `string` Exception" section with "The `string` Exclusion" — string in a hole position is a compile-time error (`InterpolatedTypedConstantHoleTypeMismatch`, code 123).

- Updated the type-grammar matching algorithm step to explicitly reject string.

- Converted all string-valid test cases to string-error test cases.

- The Part B proof engine `string` references (qualifier chain `string Description` parameter) are unaffected — different concept, different scope.

- Decision file: `.squad/decisions/inbox/frank-string-excluded-from-interpolation.md`.

- frank-2 corrected the static-case analysis (`QuantityValidator.Validate()` already covers non-interpolated constants), chose the Slice 2 structural AST match, and added `DimensionMismatchInUnitSlot = 124` with interpolation diagnostics renumbered to 121/122/123.

- The plan now carries the physical-dimension scope boundary plus the added Slice 2 estimate (+25 LOC, 9 tests) for unit-slot consistency.



### 2026-05-11T22:24:12Z — Plan renamed and expanded with proof audit findings

- `docs/Working/interpolation-plan.md` → `docs/Working/typed-constants-and-proof-coverage-plan.md`

- Plan now covers two workstreams: Part A (interpolation typed constants, Slices 1–6) and Part B (proof engine qualifier coverage, Slices 7–12).

- Integrated full audit findings: executive summary, audit matrix (7 tables), gap inventory (G1–G14), 6 new implementation slices (~167 LOC, ~38 tests), test coverage assessment, architecture assessment.

- Key architectural call: S1–S5 proof strategy architecture is sound. All 14 gaps trace to catalog metadata omissions, not structural engine defects. Fixes are catalog entries + one axis fallback + one new DU subtype + one assignment proof obligation.

- Source audit documents (`proof-engine-qualifier-audit.md`, `proof-gaps-issues.md`) retained as reference.

- Decision recorded: `.squad/decisions/inbox/frank-plan-renamed.md`.



### 2026-05-11T23:43:07Z — Slice 12 unblock context recorded

- Frank's temporal price-denominator design is the ready handoff for Slice 12: keep temporal denomination on price `of`, give duration implied temporal-dimension qualifiers, and extend Strategy 5 comparison support accordingly.

- Cross-agent inventory analysis also flagged future sample fallout in `samples/inventory-item.precept`: binary-chain qualifier propagation, ensure-expression coverage, and bare `unitofmeasure` semantics.



### 2026-05-11T20:30:17Z — Full spec coverage audit of proof plan vs canonical docs

- Audited all 12 plan items against business-domain-types.md and temporal-type-system.md. 15 items fully covered, 2 partially covered (derivation-direction chain proofs missing), 0 canonically-required items missing from plan.

- **Q1 (`quantity in 's'`):** Canonical docs close this — UCUM temporal atoms are *excluded* from quantity (error, not hint). My earlier "recommend hint" was wrong. Separate issue, outside proof plan scope.

- **Q2 (`money ÷ duration → price` qualifier propagation):** Canonical docs define the operation but are silent on result qualifier inference. Recommendation: no inference, separate issue. Consistent with physical derivation pattern.

- **Q3 (`period × integer`):** Canonical doc (temporal-type-system.md L665) explicitly prohibits the operation. My earlier "recommend add" was wrong. Permanently out of scope.

- **New gap found (G15):** Derivation-direction chain proofs (`money ÷ period/duration/quantity → price`) carry only NumericProofRequirement, no qualifier chains. Lower priority — Slice 10 assignment validation partially covers practical cases. Tracked for separate issue.

- Decision record: `.squad/decisions/inbox/frank-spec-coverage-audit.md`.



### 2026-05-11T20:21:32Z — Canonical source analysis for Slice 11B/12

- Previous working design (`docs/Working/temporal-type-system-design.md`) was deleted because it was written without reading the canonical design docs. Redid the analysis from authoritative sources.

- **Verdict: Slice 11B plan is architecturally sound, grounded in canonical decisions, and introduces no conflicts.** All five work items are correctly derived from the composition of D15, the `of` preposition semantics, and the existing DU/qualifier/proof infrastructure.

- Key finding: `price of 'time'` / `price of 'date'` is NOT explicitly specified in any canonical doc, but is correctly derived as a natural extension of `of` = "denominator dimension category" (business-domain-types.md L249-255) plus `period of 'time'`/`period of 'date'` temporal dimension vocabulary.

- The `ImpliedQualifiers` concept on `TypeMeta` is genuinely new infrastructure — analogous to existing `ImpliedModifiers` — needed to encode "duration is intrinsically time-dimension" as catalog metadata.

- No open questions block implementation. No conflicts with current source code. George can proceed once Slices 8 and 9 are complete.

- Decision record: `.squad/decisions/inbox/frank-temporal-canonical-analysis.md`.



### 2026-05-11 — G15 plan extension + Part B consistency audit

- **G15 is a false gap.** Derivation-direction operations (`MoneyDivideQuantity`, `MoneyDividePeriod`, `MoneyDivideDuration`) cannot carry `QualifierChainProofRequirement` because the operands share no qualifier axes (money has Currency; quantity/period/duration have Dimension/TemporalDimension). Chain proofs would always fail. Slice 10 assignment validation covers practical cases.

- **Slices 7–11 are already implemented.** Full consistency audit of Part B found all 5 independent slices already present in the current source code. Money currency enforcement (Slice 7), chain infrastructure (Slice 8), dimension fallback (Slice 9), assignment expression propagation (Slice 10), and exchange rate assignment validation (Slice 11) are all in the codebase. Only Slices 11B and 12 remain.

- Added Slice 13 to the plan as verification-only (0 LOC, 0 tests). Updated summary table and dependency graph.

- The plan was written against an earlier source state and never reconciled after implementation. Future plans should verify "Status: Not Started" claims against current source before each slice begins.

- Decision record: `.squad/decisions/inbox/frank-plan-extension-g15.md`.



---



## Archive Batch — 2026-05-13T04:33:30Z (size-gate summarization)



---



## Recent Updates



### 2026-05-13T00:45:00Z — Adopted field-state names were applied to the v3 doc



- Frank updated `docs\Working\field-state-guarantees-v3.md` everywhere the old field-state diagnostic names appeared so the v3 design now uses `OmittedFieldReadInState`, `OmittedFieldSetInTargetState`, and `RequiredFieldUnassignedOnEntry` throughout.

- He checked `src\Precept\Language\SyntaxReference.cs` for overlap with Elaine's prose work and intentionally made no edit there to avoid trampling concurrent changes.



### 2026-05-13T00:32:50Z — Field-state v3 is now canonically D130/D131/D132



- Frank's v3 design now records the canonical numbering after the doc renumber pass: `ReadOfOmittedField` = D130, `WriteToTargetOmittedField` = D131, and `MustSetOmitToNonOmit` = D132; older notes that said D131/D133/D135 should be read through that mapping.

- The design boundary remains unchanged: the blocked from-state target-write, readonly/access-condition, and ProofEngine surfaces stay out because Update access modes do not govern Fire/`set`.

- The companion initialization analysis still locks the three construction scenarios: initial events must assign required fields, no-initial-event precepts cannot contain required no-default fields, and stateless precepts inherit the same constructor guarantees minus state-entry behavior.

- Elaine's UX pass says D130 and D131 are conceptually sound, but D132's canonical name still needs an author-language rewrite before ship; her proposed Problems-panel copy is now part of durable team memory.

- Elaine also proposed a subject-first naming cleanup if the family is normalized in code: `FieldOmittedInStateCannotBeRead`, `FieldOmittedInTargetStateCannotBeSet`, `RequiredFieldNeedsAssignmentWhenBecomingPresent`, and a tighter `InitialEventMissingRequiredFieldAssignments`.



### 2026-05-12T23:50:08Z — Modifier applicability and constructor gaps are durable team memory



- Frank's modifier audit locked the core judgment: `price` bound modifiers and business-magnitude `maxplaces` were missing catalog metadata, while `notempty` on scalar business magnitudes should remain invalid and identity-type `notempty` should be redundancy-only.

- Frank's required-field analysis confirmed PRE0093/PRE0094 are specified but unimplemented, with no emitting pipeline stage, no runtime `Create()` support, and no sample coverage.



### 2026-05-12T23:25:25Z — Final comma-list spike approval is the current architectural baseline



- Frank approved commits `53d68d51` and `cf3c6a81`, locking `ResolvedStateTarget.IsWildcard` and keeping `NormalizeTransitionRow` as the intentional compatibility boundary that projects wildcard rows back to `TypedTransitionRow.FromState = null`.

- Remaining follow-up is proposal hardening, not implementation repair: defend the wildcard boundary, stay honest about localized parser grammar shaping, and strengthen the written rationale around locked decisions `D3` / `D4`.



### 2026-05-12T19:38:00Z — Field-state v2 consistency review stayed blocked on spec drift



- Frank confirmed D133, the parser field-target fix, omit/access-mode unification, and D42/D43 emission are grounded in the canonical spec.

- He blocked D132, D134, and the broader proof-enforcement surface because the spec explicitly says `readonly` / `editable` do not restrict event-driven `set`, and he flagged from-state D130 / guard-read D131 as needing narrower justification or explicit spec extension.



### 2026-05-13T00:08:20Z — Frank's hover B1-B4 review cycle finished fully approved



- Across B2/B3 and B4, Frank's blockers locked the final quality bar: correct rich-construct routing order, omit-aware mutability honesty, explicit `omit all` regression coverage, honest no-obligation proof narration, and duplicate-proof suppression tests.

- Final re-reviews `frank-7` and `frank-9` approved the repaired implementations, closing the full B1-B4 hover program without remaining review debt.

- The approved end state is commit-backed by `c2a38a56`, `47f3068c`, and `9617f39b`, with `279/279` language-server tests and `4973` core tests green.

## Learnings



### 2026-05-12 — v3 gap audit: declared ≠ enforced



- D93 and D94 were declared in `DiagnosticCode.cs` with full `DiagnosticMeta` entries but zero emission sites in the pipeline. The v3 design built its Form 2/Form 1 reasoning on the assumption these diagnostics were enforced. They weren't. Known gaps documented in agent history must always be promoted to tracked implementation slices — prose observations evaporate.

- Every design that depends on existing diagnostics must include a prerequisite audit: grep the pipeline for `DiagnosticCode.X` emission, not just declaration. Declaration with metadata creates false confidence.

- Two remediation slices (10, 11) added to `docs/Working/field-state-guarantees-v3.md`. Decision filed at `.squad/decisions/inbox/frank-v3-gap-audit.md`.



### 2026-05-12 — Modifier applicability by type: catalog-verified



- `nonnegative`, `positive`, and `nonzero` apply to all seven numeric/magnitude types: `integer`, `decimal`, `number`, `money`, `quantity`, `price`, `exchangerate`. Both `nonnegative` and `positive` desugar to rules and carry proof satisfactions.

- The claim "NOT price or exchangerate" is wrong. Both types are in the catalog applicability sets and have been since at least the current MCP snapshot.

- However, `price` and `exchangerate` fields with dynamic qualifiers (e.g., `price in '{CatalogCurrency}' of '{StockingUnit.dimension}'`) legitimately use explicit zero comparisons with dimensionally-qualified literals rather than the modifier. The modifier desugars to `self >= 0` (dimensionless zero); the explicit rule makes both currency and dimension expectations visible. This is intentional, not an anti-pattern.

- InventoryItem's `AverageCost` and `ListPrice` rules (`rule AverageCost >= '0 {CatalogCurrency}/{StockingUnit}'`) are correctly authored — the qualified literal form is the right choice for dynamically-qualified price fields.



- When a design spans Update and Fire behavior, verify the split against the spec and evaluator before planning diagnostics; a single explicit rule can invalidate an otherwise plausible implementation plan.

- Catalog/spec drift around business-domain types should be recorded as metadata gaps, not framed as deep semantic exclusions, when the checker is merely enforcing incomplete applicability tables.

- If a spec guarantee depends on runtime surfaces that do not yet exist (for example constructor enforcement around `Create()`), record the gap and owner decision boundary before pushing implementation work.

- §2.2 rule #6 is the single most important sentence for field-state enforcement: "`set` targeting an `omit` field in the target state is a compile error; `readonly`/`editable` do not restrict `set`." This one rule invalidated three v2 diagnostics (D130, D132, D134) and the entire ProofEngine conditional enforcement phase.

- Canonical v3 D130 scope must extend beyond transition row guards to all state-anchored expression contexts (`in`-state ensures, `from`-state ensures, state action guards). The evaluator confirms guard timing at line ~499: guard evaluates against from-state slots before working copy creation.

- Canonical v3 D132 (`MustSetOmitToNonOmit`) is the structural dual of `InitialEventMissingAssignments` — both prevent required fields from existing without valid values. D132 fills a spec gap where rule #5 covers entering-omit but is silent on leaving-omit.

- The three precept forms (with initial event, without initial event, stateless) have different D132 applicability profiles. Form 2 (no initial event) makes D132 structurally unsatisfiable because `RequiredFieldsNeedInitialEvent` forces all fields to have defaults or be optional.

- §3.5 "All field names" describes name resolution scope, not semantic validity. Canonical v3 D130 operates in the gap between "the name resolves" and "reading it is meaningful." This distinction must be annotated in the spec when D130 ships.



### 2026-07-02T00:00:00Z — Circular static-init review: Tokens ↔ Types



- Reviewed and accepted George's `Lazy<T>` fix for `Tokens.KeywordsValidAsMemberName` (Tokens.cs line 507). The CLR cctor re-entrancy caused `Types.All` to return `null` when `Tokens..cctor()` ran mid-`Types..cctor()`, crashing the MCP server.

- Confirmed the architectural invariant: Tokens is Layer ① (lexical foundation); all other catalogs depend downward on it. The reverse reference (`Tokens → Types.All`) was the only violation and is now deferred via `Lazy<T>`.

- Key constraint to document: no catalog may reference a downstream catalog's static members in its own cctor. Reverse references must use `Lazy<T>`. Currently `KeywordsValidAsMemberName` is the only such case.

- The `Types ↔ Modifiers` bidirectional edge is safe because both sides reference enum values and call `GetMeta()` lazily — no cctor depends on the other catalog's cctor completion.

- `Actions.CollectionCountAccessor` references are safe: they're inside `GetMeta()` arms (not static field initializers), and `CollectionCountAccessor` itself is a simple field initializer that doesn't depend on `Types.All`.

- Required follow-up: add static initialization constraint paragraph to `docs/language/catalog-system.md` after line 896.

- Sentinel defaults (`default 0`, `default false`, `default ""`) are a modeling anti-pattern when the field has no business meaning in the current state; `omit` should carry that meaning structurally instead.

- `omit` is now the preferred guidance for not-yet-meaningful fields because D132 `MustSetOmitToNonOmit` turns the re-entry path into a compile-time assignment guarantee rather than a runtime null/sentinel convention.



### 2026-05-13T01:03:07Z — Circular static-init review closed



- Architecture review accepted George's `Lazy<T>` fix for `Tokens.KeywordsValidAsMemberName` as the correct way to break the narrow `Tokens` ↔ `Types` cctor cycle.

- The required follow-up doc hardening is complete: `docs/language/catalog-system.md` now explicitly states that reverse `Tokens` → downstream catalog static references must defer materialization with `Lazy<T>`.



### 2026-05-12T22:25:49.004-04:00 — Proof engine doc updates written



- Expanded `docs\compiler\proof-engine.md` Strategy 5 coverage with a new `Qualifier Resolution Reference` section documenting `ResolveQualifierFromExpression`, the shared `Unit → Dimension → TemporalDimension` fallback chain, `TranslateCurrencyAxis`, the real `NumericConstraintSubsumes` vs `SatisfactionCovers` tables, and the constant-folder zero-denominator guard.

- Replaced the §5 stub in `docs\language\precept-language-spec.md` with a standalone proof-system overview that states the two-pass model, the meaning of proved qualifier constraints, the range/currency/unit-dimension enforcement surface, and the compile-time rejection rule for unresolved obligations.

- Corrected local proof-engine doc drift that had implied Strategy 2 and Strategy 3 shared one subsumption table and that qualifier compatibility was still pending future type-checker work.



### 2026-05-13T03:56:26Z — Slice 9 review and constructor-gap reanalysis recorded



- `frank-9` deferred the first Slice 9 review because George had not committed yet; the pre-review concerns were later closed by George's final commits `c2d5b8fb` and `32da6a3e`.

- `frank-10` corrected the initial-state/no-default analysis: `RequiredFieldsNeedInitialEvent` (D93) must fire, D94 is also unenforced, and `ProofEngine.Analysis.GetTypeDefault()` is the implementation bug if the sample compiles clean.

- `frank-11` completed the broader v3 gap audit: D93 and D94 are blocking gaps, D132's Form 2 reasoning depends on D93, and the fix belongs in `TypeChecker.Validation.cs` slices 10 and 11.



### 2026-05-13T00:18:23-04:00 — Full diagnostic gap analysis (132 codes)



- Analyzed all 132 diagnostics in `DiagnosticCode.cs`. Found 50 with no pipeline emission (corrected from initial report of 54).

- **Critical correction:** CI enforcement diagnostics (PRE0066, PRE0095, PRE0097, PRE0098) were incorrectly reported as gaps. They ARE emitted via catalog-driven dispatch (`Operations.GetMeta().CIDiagnosticCode` and `Functions.GetMeta().CIDiagnosticCode`) in `TypeChecker.Validation.cs` lines 700-860. The original grep missed indirect emission.

- **Key pattern discovered:** Catalog-driven emission means `DiagnosticCode.X` won't appear literally at the emission site. Grepping for direct references misses catalog-mediated dispatch. Future audits must search for both patterns.

- **Highest integrity risk:** PRE0070-0074 (currency/unit arithmetic). Cross-currency arithmetic compiles clean — directly violates philosophy.md core promise.

- **Five domain clusters** are the bulk of the gap: temporal (8), currency/unit (5), choice (3 type-stage), collection safety (4), plus 17 scattered individual diagnostics.

- **Only 1 truly speculative diagnostic:** PRE0091 `AmbiguousTypedConstant` — unreachable due to single-candidate resolution. All others are specced and should be enforced.

- Working document: `docs/working/diagnostic-gap-analysis.md`. Decision: `.squad/decisions/inbox/frank-diagnostic-gap-analysis.md`.

- Key files: `src/Precept/Language/DiagnosticCode.cs`, `src/Precept/Language/Diagnostics.cs`, `src/Precept/Pipeline/TypeChecker.cs`, `TypeChecker.Validation.cs`, `TypeChecker.Expressions.cs`, `TypeChecker.Expressions.Callables.cs`.



### 2026-05-13T00:32:04-04:00 — Diagnostic coverage enforcement design



- **Emission pattern is uniform.** All pipeline stages emit via `Diagnostics.Create(DiagnosticCode.X, span, args...)` added to a list. Three emission forms exist: (1) direct `Diagnostics.Create()` calls in pipeline stages (~90%), (2) catalog-mediated via `CIDiagnosticCode` properties on `OperationMeta`/`FunctionMeta` in Operations.cs and Functions.cs, (3) ProofEngine dispatch through `GetNumericRequirementDiagnosticCode()` and `CreateDiagnostic()` switch branches. All three forms contain the literal token `DiagnosticCode.{MemberName}` at the reference site.

- **PRECEPT0003 is the architectural anchor.** This existing Roslyn analyzer forces all real emissions through `Diagnostics.Create()` — no direct `new Diagnostic(...)` construction allowed. This means the emission pattern is trustworthy and source-scanning is reliable.

- **Precept.Analyzers is mature.** 26 analyzers already exist including catalog cross-reference checks (PRECEPT0007–0017), pipeline coverage exhaustiveness (PRECEPT0019), and structural invariant enforcement (PRECEPT0018–0026). A Roslyn analyzer for emission coverage would be feasible but disproportionate to the enforcement value.

- **Recommended: convention test (Option B).** Source-scanning xUnit test in `test/Precept.Tests/CatalogTests/DiagnosticEmissionCoverageTests.cs`. Enumerates all `DiagnosticCode` members, scans pipeline + catalog-emission files for references, reports violations. Allow-list for known 50 unemitted codes with inverse staleness check.

- **Coverage bar: emission site exists.** Test coverage and spec documentation are separate concerns for separate enforcement gates.

- Design doc: `docs/working/diagnostic-coverage-enforcement.md`. Decision: `.squad/decisions/inbox/frank-diagnostic-coverage-enforcement.md`.

## Archive Batch — 2026-05-13T23:55:10Z (history summarization)

---

### 2026-05-13T04:32:04Z — Diagnostic coverage enforcement now has two enforced gates
- `docs/working/diagnostic-coverage-enforcement.md` records Gate 1 emission-site coverage plus Gate 2 emitted-code test coverage as separate convention-test allow-list checks.
- Frank confirmed Gate 2's current baseline is clean: all emitted diagnostic codes are referenced in tests, so the emitted-but-untested allow-list starts empty while 7 codes remain neither emitted nor tested.

---

### 2026-05-13 — Field-state guarantees and constructor enforcement are the active baseline
- The v3 field-state design is now canonically D130/D131/D132, with author-language follow-up still needed for D132's shipped name.
- Frank's gap audit established that declaration metadata is not enough: D93/D94 were specified but unenforced, so prerequisite audits must verify real `DiagnosticCode` emission sites before downstream design work assumes behavior exists.
- George's follow-through landed disjunction support, D93 required-field initial-event enforcement, and D94 required-assignment coverage while keeping stateless precepts exempt.
- The diagnostic coverage recommendation remains a convention-test allow-list model using literal `DiagnosticCode.{Member}` references in pipeline and catalog-emission files as the minimum bar.

---

### 2026-05-13T04:52:18Z — Diagnostic gap docs now lock the negative-test quality bar
- `docs/Working/diagnostic-gap-analysis.md` records the D94 row-scoping review, targeted regression coverage recommendations, and the low-priority stateless-initial-event enforcement gap.
- `docs/Working/diagnostic-coverage-enforcement.md` adds the quality bar that each gap closure needs both `CheckExpectingError` and `CheckExpectingClean` coverage.

---

### 2026-05-13T09:38:04Z — Catalog-mediated emission expansion scope documented
- `docs/Working/diagnostic-enforcement.md` §9 now includes the governing policy, prioritized top-3 expansion candidates, and a do-not-apply list for catalog-mediated emission.
- The three-criteria test (stable 1:1 mapping, uniform logic, membership check on resolved artifacts) is the filter for when catalog mediation applies.

---

### 2026-05-13T09:48:49Z — Expansion scope promoted to concrete implementation slices
- `docs/Working/diagnostic-enforcement.md` now carries Slices 9A/9B/9C as full implementation slices with objective, target files, completion gate, and regression anchors.
- Ordering constraints were updated: 9A depends on Slice 8, 9B depends on or subsumes Slice 5, and 9C is independent.

---

### 2026-05-13T09:55:48Z — Expansion slices promoted from long-term to active execution scope
- `docs/Working/diagnostic-enforcement.md` §9 was reframed from long-term to active architectural evolution without changing the selective-adoption policy.
- Key learning: plan framing changes execution expectations; section titles and tier annotations must match actual scope.

## Archive Batch — 2026-05-15T02:26:33Z (15 KB summarization)

---

### 2026-05-14T17:37:50.029-04:00 — Design doc resolution pass: all 6 conditions resolved

- Completed the design document resolution pass on `docs/Working/quantity-normalization-design.md`, resolving all six §5.5.6 conditions in a single edit pass.
- **Condition 1:** SUPERSEDED markers verified on §3.6, §3.7, §7 Q2 — §0's "store both" is the single authoritative bounds-storage design.
- **Condition 2:** Replaced "universal post-step" in §0 Q6 with expression-type-dispatched `TryGetStaticScalingFactor` pseudocode. Added constraint table showing which expression types scale vs. which are excluded. This is the critical double-normalization prevention mechanism.
- **Condition 3:** Added `GetFieldBounds` fix to Slice 16 spec — reads `NormalizedDeclaredMin ?? DeclaredMin` with null fallback.
- **Condition 4:** Added `TryGetStaticNumericValue` fix to Slice 16 spec — normalizes `StaticMagnitude` via `TryGetStaticScalingFactor` before returning trusted facts.
- **Condition 5:** Decided Option (a) for `TypedEventArg` — parallel `NormalizedDeclaredMin/Max` fields, architecturally consistent with `TypedField`. Added Slice 15b spec.
- **Condition 6:** Updated `NumericInterval.Scale` to `Scale(decimal factor)` in Revised Key Types and Slice 14. Factor conversion happens once in `TryGetStaticScalingFactor`.
- Added §5.6 Extended Slice Details (Slices 22–26) from George's gap audit with full objective/files/approach/tests/dependencies for each.
- Replaced George's §0.6 header with Frank's Design Resolution Summary including the condition resolution table and implementation gate clearance.
- Key pattern: the `TryGetStaticScalingFactor` helper is the single dispatch point for all expression-type → scaling-factor decisions. This is the design's core invariant — one function, one place, no scattered switching.

---

### 2026-05-14T17:10:32.283-04:00 — Interpolated normalization review closed with approval conditions

- Completed the exhaustive architectural review of `docs/Working/quantity-normalization-design.md` and approved the direction **with conditions**.
- Locked the design correction that §0 supersedes the competing §3.6 / §3.7 / §7 Q2 descriptions, so the doc must carry one canonical bounds-storage story.
- Confirmed the key follow-up requirements: expression-form-scoped `IntervalOf` scaling, normalized reads in `GetFieldBounds`, normalized `StaticMagnitude` in trusted-fact extraction, and a decision on event-arg bound parity.
- Cross-agent note: George's exhaustive gap audit proves Slices 19–21 are necessary but not exhaustive; implementation planning must account for the wider interpolated qualifier/default surface.

---

### 2026-05-14T17:10:32.283-04:00 — Diagnostic enforcement alignment recorded as a three-layer model

- Confirmed the enforcement mission did **not** compound compiler/runtime duplication; most wired diagnostics are compile-time-only structural checks.
- Recorded the canonical three-layer model: compiler diagnostics, ingress validation, and defense-in-depth faults linked through `[StaticallyPreventable]`.
- Captured two durable follow-ups: ingress validation should become a deliberate surface for quantity/choice/dynamic-qualifier checks, and catalog-mediated dispatch remains the preferred alignment pattern.
- Preserved the companion implementation-notes record so future sessions can recover enforcement reality without re-auditing the mission.

---

### 2026-05-14T17:48:42.442-04:00 — Doc-sync slice (Slice 27) added to quantity normalization design

- Audited all canonical documentation surfaces for staleness after quantity normalization (Slices 14–21).
- **Needs updates:** `docs/language/precept-language-spec.md` (§0.6 + §5 — add unit-aware normalization to proof engine contract), `docs/compiler/proof-engine.md` (obligation record, interval source table, Strategy 6 normalization), `docs/Working/interval-proof-engine-design.md` (tracker cross-ref, interval table annotations, obligation parameter annotations), `docs/runtime/runtime-api.md` (three-layer enforcement model from §0.5 needs a canonical home), MCP DTO (`CompileProofObligationDto` gains `NormalizedDeclaredMin/Max`).
- **Confirmed clean:** `docs/language/catalog-system.md` (catalog describes types, not comparison behavior), `README.md` (no quantity/normalization content), `docs/philosophy.md` (no scope change), `samples/` (bug fix is in compiler, samples unchanged), `docs/mcp/` (directory doesn't exist).
- The three-layer enforcement model (compile-time / ingress / defense-in-depth) identified in §0.5 is the most significant doc gap — it's a named architectural concept without a canonical home in the published docs.

---

### 2026-05-15T00:08:25Z — Typed-constant null-guard decision locked to the proof layer

- Locked the `samples/Test.precept` null-guard gap to PRE0116 via ProofEngine presence obligations rather than a new TypeChecker error.
- Recommended reverting `samples/Test.precept` to its clean literal form and keeping interpolation coverage in a dedicated sample if needed.
- George's adjacent implementation fix (commit `ae19510f`) now lands on the exact traversal gap Frank identified: `TypedInterpolatedTypedConstant` holes must be walked like other optional value reads.

---

## Archive Batch — 2026-05-15T14:55:25Z

---

## Recent Updates
### 2026-05-15T10:55:25Z — Slice N+M final review: APPROVED

- Reviewed George's B3 fix (`70ee2406`): extends `AllowsBareNumericQuantityBound` to suppress PRE0018 for count-dimension fields via `IsCountDimension`.
- `IsCountDimension` is catalog-driven (`DimensionCatalog.All` → `DimensionVector.None` check) — correct and already battle-tested in modifier validation.
- All four cases verified: unit-qualified suppresses ✓, count-dimension suppresses ✓, non-count dimension fires ✓, unqualified fires ✓.
- All 14 `TypeCheckerQualifierCompatibilityTests` pass. 9 failures in full suite all pre-exist on parent commit — zero new regressions.
- Verdict written to `.squad/decisions/inbox/frank-nm-final.md`. Slices N and M are fully closed.

### 2026-05-15T02:57:25Z — George blocked §5.7 implementation slices

- George reviewed `docs/working/quantity-normalization-design.md` §5.7 and marked the current slice plan BLOCKED for revision.
- Slice 33 must target `contains` on the synthetic membership path (`ResolveBinaryOp` → `TryResolveCatalogBinaryWithoutOperation` → `CreateSyntheticBinaryOp`), not `in` / `not in`.
- Slice 32 must cover both successful returns inside `SelectOverload`, and slices 35–36 need "introduce" wording because the currently named normalizer/helper seams do not exist yet.
- Current canonical file seams called out by George: `src/Precept/Language/Ucum/UcumAtomCatalog.cs`, `src/Precept/Language/Diagnostics.cs`, and `src/Precept/Language/Functions.cs`.
- PRE0137 remains the correct next diagnostic ordinal; regression anchors should call out `test/Precept.Tests/ProofEngineTests.cs` and `test/Precept.Tests/TypeChecker/OperatorTypingTests.cs`.
- George also noted that `dotnet test test/Precept.Tests/Precept.Tests.csproj --no-restore` is already red by 7 baseline failures.
### 2026-05-15T02:32:44Z — Affine unit conversion design for temperature units

- Designed `docs/working/quantity-normalization-design.md` §6.8 to support affine temperature normalization for `Cel`, `[degF]`, and `[degRe]` with `base = (value + offset) × scale`.
- Root cause is in UCUM parsing: `StripFunctionWrapper` keeps multiplicative factors but erases the function-name offset encoding, so Celsius currently collapses to Kelvin semantics unless the offset is carried separately.
- Locked the implementation shape around `AffineOffset` metadata, affine proof/interval normalization, and a 24-test matrix; logarithmic units remain explicitly excluded.
- Scribe merged the decision into `.squad/decisions.md` and cleared `.squad/decisions/inbox/frank-affine-conversion-design.md`.

### 2026-05-16 — Comprehensive cross-counting-unit operation analysis: function gap found

- Exhaustive analysis of all 16 operation categories for cross-counting-unit interaction. Prior §6.7 was correct for binary operators but missed function calls entirely.
- **Critical finding (Gap C):** The Functions catalog declares `QualifierMatch.Same` on min/max/clamp/abs quantity+money overloads, but `SelectOverload` in `TypeChecker.Expressions.Callables.cs` never reads the `Match` property. Qualifier enforcement is completely absent for function calls. `max(qty_each, qty_box)` resolves without error.
- Locked: PRE0137 covers both operators and functions (single diagnostic code, adapted message). Fix is `ValidateFunctionQualifierCompatibility` after overload selection, reading existing catalog metadata.
- Lower-priority Gap D identified: `in` membership operator uses `CreateSyntheticBinaryOp` which skips `ValidateQualifierCompatibility`. Deferred to follow-up.
- Architectural principle locked: every `QualifierMatch` constraint declared in any catalog entry must have a corresponding enforcement point.
- Design doc updated: §6.7.9–6.7.11 added to `docs/working/quantity-normalization-design.md`.

### 2026-05-15T02:26:33Z — Cross-counting-unit comparison gap: full solution designed

- Traced the exact root cause in `ValidateQualifierCompatibility`: PRE0070/PRE0071 only apply to `OperatorFamily.Arithmetic`, and the same-dimension fallback treats all counting units as identical `count` quantities.
- Designed the two-tier fix: extend PRE0070/PRE0071 to comparison operators, then add PRE0137 `CrossCountingUnitOperation` when both operands are static count-dimension quantities with different unit codes.
- Locked the architectural boundary: SI units with the same dimension but different codes stay valid because UCUM normalization converts them; the stricter rule is only for business counting units with no universal factor.
- `docs/working/quantity-normalization-design.md` §6.7 now carries the implementation-ready plan, and Scribe merged the result into `.squad/decisions.md`.

### 2026-05-15T01:52:56Z — Counting-unit wording fix exposed a proof gap

- Corrected the counting-unit research note: `count` / `DimensionVector.None` is a shared dimension-family alias for business units such as `each` and `box`; it is not a conversion rule.
- Locked the language distinction between dimensional compatibility and value convertibility so future docs do not imply `1 box = 1 each`.
- Surfaced the deeper architectural issue: binary-op qualifier proof currently falls back through the shared `count` dimension, so explicit-unit comparisons can prove even when no conversion law exists.

### 2026-05-15T01:37:41Z — External normalization research merged

- Frank validated the quantity-normalization design against F#, Rust/uom, JSR-385, FHIR/UCUM, Modelica, and decimal interval-arithmetic practice; the architecture stayed sound, with only medium-priority documentation follow-ups.
- Business units (`each`, `box`, package-family count units) already normalize correctly by construction through factor-1 UCUM atoms and shared count-dimension metadata; no runtime storage change is needed.
- Scribe merged the supporting research records into `.squad/decisions.md`, deleted the inbox notes, and logged the batch for durable recovery.

### 2026-05-14T22:26 — Affine unit conversion design for temperature units
- Designed §6.8 extension to `docs/working/quantity-normalization-design.md` covering affine (scale + offset) conversions for temperature units (°C, °F, °Ré).
- **Key finding:** `UcumAtomCatalog.GetDefinitionExpression` strips UCUM `<function>` wrappers via `StripFunctionWrapper`, capturing scale but discarding the offset encoded in function names (`Cel`, `degF`, `degRe`). Celsius currently normalizes as identity (scale=1, no offset) — indistinguishable from Kelvin.
- **Approach:** Catalog extension — `UcumAtom` gains `decimal? AffineOffset`, `UcumParsedUnit` propagates for single-atom units only. Conversion: `base = (value + offset) × scale`. Linear units have `offset = null` → no regression.
- **Logarithmic units (dB, pH) excluded:** interval arithmetic incompatibility, domain mismatch, reference-level ambiguity.
- **Orthogonal to frank-12:** PRE0137 targets counting units (`DimensionVector.None`); temperature has `DimensionVector.Temperature`.
- Decision record: `.squad/decisions/inbox/frank-affine-conversion-design.md`.

## Learnings

- 2026-05-15T10:55:25.692-04:00 — Re-review of George's B1/B2/W1 fixes (0837ad6f): **BLOCKED**. B2 and W1 are clean. B1 qualifier gate (`qualifiers.Any(q => q is DeclaredQualifierMeta.Unit)`) is conceptually correct but breaks the count-dimension path — `quantity of 'count' max 4` now emits PRE0018 alongside PRE0138, violating the Slice M test contract. The suppression must also cover count-dimension fields since they have a dedicated diagnostic (PRE0138) downstream. **Lesson:** when adding a qualifier gate to a suppression, verify it against ALL diagnostic paths that depend on that suppression — not just the primary happy path.
- 2026-05-15T10:48:22.809-04:00— Formal code review of Wave 2 (Slices 15/15b/16/19/20/31/33/35/36/37): **APPROVED**. Affine formula correct, interval composition order correct (Shift then Scale), PRE0137 gating correct (DimensionVector.None + differing unit codes), no catalog violations. One architectural warning: `DeclaredMin/Max` and `NormalizedDeclaredMin/Max` store identical normalized values — the design called for preserving the original alongside the normalized. Follow-up slice needed to split extraction so `DeclaredMin` keeps the raw user-facing value. Written to `.squad/decisions/inbox/frank-wave2-review.md`.
- 2026-05-15T10:41:34.421-04:00 — Code review of Slices N (ff43d56a) and M (04c16211): BLOCKED.`AllowsBareNumericQuantityBound` in TypeChecker.cs suppresses PRE0018 for ALL quantity fields with bare-numeric bounds regardless of qualifier type — must gate on `DeclaredQualifierMeta.Unit` like its companion `ShouldAllowUnitQualifiedQuantityBareNumericBound` does. Slice M (PRE0138) is clean. **Enforcement principle reinforced:** when a suppression exists at two sites for two diagnostics (PRE0018 in TypeChecker.cs, PRE0133 in Validation.Modifiers), both must carry identical scoping predicates or one becomes a silent escape hatch.
- 2026-05-15T10:38:09.774-04:00— Synced §5.0 progress tracker: marked slices 15, 15b, 16, 19, 20, 31, 33, 35, 36, 37 as ✅ Done (George wave-2a commit 88b1e1f8, wave-2b commits 84a8d9c9/b33b5fa6/5797337c). Updated §5.1 agent table for slices 15/16. Revised critical-path note to reflect remaining gate is 17/18 → 21 onward.
- 2026-05-15T10:35:23.964-04:00 — Added formal design entries for Slices 44 and 45 to `docs/Working/quantity-normalization-design.md` §5.7. Slice 44 (bare-integer bound promotion, commit ff43d56a) documents the two-site false-positive fix for PRE0018+PRE0133 on unit-qualified fields. Slice 45 (PRE0138 CountDimensionBoundsAmbiguous, commit 04c16211) documents why count-dimension-only qualifiers need a dedicated diagnostic rather than generic PRE0133. Both are standalone and placed in a new "Bounds" lane in the §5.7 summary table.
- 2026-05-15T07:59:53.548-04:00 — Corrected `docs/language/business-domain-types.md` to state that business counting units share `DimensionVector.None` / the count dimension while still requiring explicit business conversion factors; static cross-unit count operations belong to PRE0137, not PRE0071. Updated `docs/working/quantity-normalization-design.md` progress tables to reflect shipped wave-1 work (Slices 14, 22, 30, 32, 34, 38–43 done; Slice 31 partial with binary operators still pending wave 2). Follow-up audit found no further PRE0137 wording contradictions beyond the corrected `business-domain-types.md` claim.
- 2026-05-14T23:36:31.558-04:00 — Documentation Slices 38–42 are now carried in the design/spec surfaces: temperature is explicitly in scope via affine `(scale, offset)` normalization; the no-epsilon guarantee is now stated as an exact `UcumExactFactor` + `decimal` contract; and business counting units are documented as shared `DimensionVector.None` / factor-one representations that still require PRE0137 unit-code enforcement.
- 2026-05-14T22:48:46.544-04:00 — Added formal implementation slices 30–43 to `docs/working/quantity-normalization-design.md`, covering the four qualifier gaps, the four-slice affine lane, five pre-implementation documentation slices, and the standalone `TypedInterpolatedTypedConstant` → `InterpolatedTypedConstant` rename.
- 2026-05-14T23:06:08.162-04:00 — George's §5.7 blockers required hard correction of the actual code seams: the diagnostic surfaces are `src/Precept/Language/DiagnosticCode.cs` and `src/Precept/Language/Diagnostics.cs`, the functions catalog is `src/Precept/Language/Functions.cs`, and the membership seam is `src/Precept/Pipeline/TypeChecker.Expressions.cs` via `ResolveBinaryOp` → `TryResolveCatalogBinaryWithoutOperation` → `CreateSyntheticBinaryOp` for `contains` (not `in` / `not in`). There is no existing `TypeChecker.TryGetStaticScalingFactor()` helper in the current codebase; affine helper wording must use introduce/new-helper language instead.
- 2026-05-14T23:17:29.653-04:00 — As of 2026-05-14, George's full codebase audit confirmed ALL slices 14–27 are NOT_STARTED. Not one line of normalization code has been written. `docs/working/quantity-normalization-design.md` now carries a master progress tracker at §5.0 covering all slices 14–43, plus Status columns added to the §5.1, §5.3, §5.6, and §5.7 summary tables.

### 2026-05-15T03:13:42Z — Frank’s §5.7 revisions cleared the review gate

- Frank’s revised §5.7 plan is now the durable baseline: stale catalog/diagnostic/function references were corrected to the real `src/Precept/Language/...` surfaces, Slice 32 names both successful `SelectOverload` return paths, and Slice 33 now targets `contains` through the synthetic membership path.
- George’s re-review approved the revised slice list with no remaining stale path or method references, and PRE0137 remains the next free diagnostic ordinal after `CountBoundViolation = 136`.

### 2026-05-15T03:17:29Z — Scribe recorded doc-tracker update

- Scribe merged Frank's doc-tracker note into `.squad/decisions/decisions.md` and cleared `.squad/decisions/inbox/frank-doc-tracker-update.md`.
- Durable baseline: `docs/Working/quantity-normalization-design.md` now carries §5.0 plus Status columns in §5.1/§5.3 and summary/status tables for §5.6/§5.7, all grounded in George's NOT_STARTED audit for slices 14–27.
- Scribe wrote the orchestration/session logs for the shared slice-audit + doc-tracker batch so the design tracker and codebase audit stay linked.

### 2026-05-15T03:43:11Z — Documentation annotation wave closed with one counting-unit follow-up

- Slices 38–42 doc annotations are committed and remain the live spec baseline for the normalization track.
- One durable follow-up stays open: `docs/language/business-domain-types.md:373` still says business counting units are opaque with no shared dimension, which now contradicts the shared `DimensionVector.None` + factor-one representation model.
- Keep PRE0137 as the enforcing rule for explicit unit-code identity inside that count family; the remaining work is wording correction, not architectural reconsideration.
---

## Archive Batch — 2026-05-15T22:09:58Z

---

### 2026-05-15T18:09:58-04:00 — Qualifier deferred items scoped (3 items)

- Scoped the three items deferred from PRE0141 enforcement work. Full spec in `docs/Working/frank-qualifier-deferred-scoping.md`, decision in `.squad/decisions/inbox/frank-qualifier-deferred-scoping.md`.
- **Item 1 (ProofEngine unification): PARTIAL.** Full entry-point unification is premature — the two resolvers serve different contracts (assignment diagnostics vs proof disposition). Required now: implied-qualifier parity in `ResolveDirectQualifierAxis` and compound-cancellation helper extraction into `UnitDimensionHelper`.
- **Item 2 (Quantity gaps): FULL, one fix.** Four of five gap categories (A/B/D/E) are already handled by the shipped PRE0141 resolver. One gap in category C: `ResolveSlotSourceQualifierAxis` returns `Absent` instead of `Unknown` for bare-but-type-applicable sources in interpolation unit/dimension slots.
- **Item 3 (Function-call qualifier preservation): FULL.** Add `ResultQualifiers` to `TypedFunctionCall`, populate from first argument when `overload.Match == QualifierMatch.Same`, add case arms to both resolvers. Five functions in scope: `abs`, `min`, `max`, `clamp`, `round(v, places)`.
- All three items are independent and can be parallelized. No design review gate needed for any of them.

---

### 2026-05-15T18:02:40-04:00 — Full code review: assignment qualifier enforcement APPROVED

- Reviewed George's axis-aware resolver implementation and Soup Nazi's 19-test regression matrix against the governing analysis in `docs/Working/frank-price-qualifier-full-analysis.md`.
- Verdict: **APPROVED** with zero blocking findings and three non-blocking follow-ups (function-call qualifier preservation, proof-engine unification, language-spec diagnostics appendix).
- Key architectural confirmations: tri-state `Resolved/Unknown/Absent` model matches § 4.2 exactly; all 16 source forms from § 1.2 are covered; empty-array-means-success antipattern is fully eliminated; `PRE0141` is correctly separated from `PRE0068` (definite mismatch) and `PRE0114` (proof-stage).
- Decision recorded in `.squad/decisions/inbox/frank-qualifier-enforcement-review.md`.

---

### 2026-05-15T20:40:13Z — Price qualifier enforcement architecture is now durably closed as shipped work

- George landed the axis-aware assignment resolver with `PRE0141`, matching Frank's rule that constrained qualifier axes must be provably compatible at compile time and that unknown is never silent success on a constrained axis.
- Soup Nazi's 19-test matrix is now the durable regression surface across `set`, field-default, and event-arg-default lanes, and Scribe merged the full batch into `.squad/decisions.md` plus the orchestration/session logs.
- The separate `PriceIn` / `CompoundPrice` qualifier-shape proposal remains recorded as proposal-state only; it is not a prerequisite for the shipped enforcement repair.

---

### 2026-05-15T20:25:45-04:00 — Full price qualifier enforcement model locked

- Wrote the governing analysis in `docs/Working/frank-price-qualifier-full-analysis.md` and recorded the decision in `.squad/decisions/inbox/frank-price-qualifier-full-analysis.md`.
- Durable ruling: assignment qualifier enforcement is **axis-by-axis proof**, not best-effort extraction. If the target constrains a qualifier axis and the source expression cannot prove compatibility on that axis, the assignment must be rejected at compile time.
- `PRE0068` remains the diagnostic for **definite mismatch** only. Unknown-source-to-constrained-target cases need a new assignment-specific unproved-qualifier diagnostic; overloading `PRE0068` would be semantically wrong.
- Verified silent gaps extend well beyond the bare `price` unit-slot repro: bare qualified-type refs, whole-value interpolation, conditional selection, currency-slot interpolation, and exchange-rate from/to-slot interpolation all currently bypass assignment enforcement in representative cases.
- Architectural directive to George: replace the boolean/partial-array `TryGetAssignmentSourceQualifiers(...)` seam with shared per-axis qualifier resolution (`Resolved` / `Unknown` / `Absent`) and close the model across `price`, `money`, `quantity`, and `exchangerate` — not another one-off patch.

---

### 2026-05-15T14:55:25Z — Tracker sync and the Wave 2 / Slice N/M review loop are durably closed

- §5.0 tracker rows 15, 15b, 16, 19, 20, 31, 33, 35, 36, and 37 are recorded as ✅ against commit `f1215192`, and the bounds-validation documentation lane is now numbered as Slices 44 and 45.
- Wave 2 stayed APPROVED after George's `01f255ab` follow-up, which preserved authored-vs-normalized bounds and added the affine-price guard plus regression coverage.
- Slice N/M closed after two blocker passes: B1/B2 were fixed in `0837ad6f`, B3 was fixed in `70ee2406`, and the final verdict is APPROVED.

## Learnings

- Suppression fixes must be reviewed against every downstream diagnostic that depends on the suppression, especially when one path intentionally hands off to a more specific diagnostic.
- Tracker and documentation passes should assign stable slice IDs as soon as standalone fixes appear so later reviews and closeout logs can cite one durable name.
- WholeValue interval tests using same-unit (scale=1.0) for both source and target cannot detect double-normalization bugs. Cross-unit WholeValue tests are mandatory when Slice 19 lands — track as Slice 19 obligation.
- Intentionally-red tests that assert the *correct* expected behavior (not `Skip`) are superior contract pressure — they fail loudly on regression AND on fix, ensuring the fix is noticed and the test transitions to green deliberately.
- Display contract pattern: when a record carries both "math values" and "display values," the construction site must source them from genuinely distinct paths (e.g., `GetFieldBounds()` for normalized, `field.DeclaredMin` for authored). A fallback operator (`AuthoredMin ?? DeclaredMin`) at rendering sites ensures graceful degradation for non-quantity cases.
- `HasSingleMagnitudeSlot`-style positive guards are more robust than negative exclusion lists — new slot kinds automatically fail the check rather than needing maintenance.

---

### 2026-05-15T15:37:42Z — Slice 17 and Slice 18 reviews recorded approved

- Slice 17 review approved the 9-test normalization matrix and preserved the intentionally-red cross-dimension case as honest contract pressure.
- Slice 18 review approved the authored/normalized display contract split across proof requirements, diagnostics, and MCP projection.
- Durable warnings to keep live: Slice 19 still needs a cross-unit WholeValue regression plus MCP normalized-bound coverage, and the Test 6 cross-dimension root causes should stay tracked as debt until implementation closes them.

---

### 2026-05-15T15:37:42Z — Slice 21 review recorded approved

- Slice 21 APPROVED: 10 interpolated quantity integration tests covering all §5.3/G21 required behavioral cases. Conservative-case tests (3, 6, 7) correctly assert `NumericOverflow` fires for unbounded/dynamic-unit inputs.
- W1: conservative tests verify overflow fires but don't explicitly assert `ProofDisposition != Proved` — adding this would make the false-proof prevention invariant explicit in the suite. Not blocking.
- W2: both-holes form (`'{intField} {unitField}'`) not tested — single-hole dynamic-unit test (Test 6) covers the architectural invariant. Not blocking.
- Test 9 cross-unit WholeValue anchor is acceptable for Slice 21 scope; the tighter `max '1 kg'` variant that would actually detect double-normalization is correctly deferred as future obligation.
- Durable learning: happy-path anchors and regression detectors are distinct test categories — a test that passes regardless of bug presence is an anchor, not a guard. Both are valuable but must not be confused.

