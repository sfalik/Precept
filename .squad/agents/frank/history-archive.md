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

