# Squad Decisions



---



## ACTIVE DECISIONS — Current Sprint



---



### 2025-07-14T12:00:00Z: Priority queue connector review recorded

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (2 files)

**Merged sources:** `frank-queue-connector-review`, `elaine-queue-connector-review`.

- Frank's technical review recommends keeping `by` at priority-queue action sites: `at` remains pre-rejected because it collides with future temporal vocabulary, declaration-site `by` is already locked, and the `into`/`by` dequeue form is parser-safe and type-checker-safe even if its English read is slightly repurposed.
- Elaine's UX review recommends switching action sites from `by` to `with` while leaving declaration-site `by` untouched: the real readability problem is directional mismatch at dequeue capture, not the declaration role connector.
- Elaine also surfaced the stronger teaching pattern: lead docs and samples with the three-line `peek` / `priority` / `dequeue` sequence, and position `dequeue ... into X ... Y` as shorthand rather than the canonical first-read form.
- Combined record: owner sign-off is still pending, both reviews reject `at` as the right fix, and the remaining open decision is whether priority-queue action-site symmetry should optimize for technical continuity (`by`) or directionally neutral readability (`with`).

---
### 2026-04-29T05:34:09Z: Collection type expansion follow-up recorded

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (1 file)

**Merged sources:** `frank-collection-types-research`.

- Frank closed the remaining ordered-choice documentation gaps in `docs/language/collection-types.md`, so `choice(...) ordered` is treated consistently in the grammar, orderability framing, and comparison material.
- The doc now has `§ Proposed Additional Types`, evaluating six candidates with priority bands: `bag`, `log`, and `map` high; `sortedset` and `priorityqueue` medium; `deque` low.
- The new `§ Comparison With Other Collection Systems` cross-language table maps 14 capabilities across 9 ecosystems and reinforces restricted `map of choice(...) to V` as the strongest next collection-type research target.

---
### 2026-04-29T05:18:06Z: Collection types design doc authored and indexed

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (1 file)



**Merged sources:** `frank-collection-types-doc`.



- Frank authored `docs/language/collection-types.md` as the canonical collection-types reference, covering the shipped surface (`set`, `queue`, `stack`), actions, accessors, constraints, emptiness safety, inner-type behavior, `~string`, and diagnostic anchors.

- The new doc also preserves the current design frontier: proposed quantifier predicates plus collection-level modifiers such as `unique`, collection `notempty`, `subset`, and `disjoint`, with eight explicit owner-sign-off questions recorded before implementation.

- `docs/language/README.md` now indexes the new reference in the Documents table and reading order so collection guidance is discoverable from the language-doc hub.



---



### 2026-04-29T04:47:14Z: Vision→spec migration completed and vision archived

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (3 files merged, 1 duplicate skipped)



**Merged sources:** `frank-slice1-preamble`, `frank-slice2-semantic-gaps`, `frank-slice3-4-archive`.

**Deduplicated/skipped:** `frank-vision-archive-audit` (already captured in the 2026-04-29T01:09:17Z vision/spec audit record).



- Frank's vision→spec migration is now durably closed as a complete sequence: §0 Preamble landed with the 11 Design Principles, Language Model, Governance Not Validation, Execution Model Properties, and pre-implementation graph/proof contracts; §3A Language Semantics landed with constraint semantics, outcome/verdict semantics, violation attribution, mutation atomicity, entity construction, and inspection as a first-class operation.

- The migration preserved substance rather than rewriting it: overlapping graph-analysis material was merged into one contract section, mutation atomicity/inspectability were expanded without duplicating their earlier anchors, and the spec now carries the identity-bearing language philosophy that previously lived only in the vision doc.

- Slice 3–4 then removed the two stale contradictions (`with` still listed as a structural preposition, and "root editability" wording left over from retired `write all` semantics), archived `docs/language/precept-language-vision.md` to `docs/archive/language-design/precept-language-vision.md`, updated the spec Status table, and swept 12 cross-references so the archived path never existed half-wired on the branch.

- Net result: the language spec is now the single canonical language document, the vision is preserved as archive material only, and the earlier archive-readiness audit remains the durable rationale for why this migration sequence was necessary.



---



### 2026-04-29T04:47:14Z: No-runtime-faults principles aligned; philosophy gap flagged

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (2 files)



**Merged sources:** `frank-principles-10-11-eval`, `frank-philosophy-eval`.



- Frank's evaluation of §0.1 Design Principles found Principles 10 and 11 incomplete rather than contradictory: Principle 10 previously treated runtime faults as acceptable "definite errors," and Principle 11 only promised compile-time elimination of type errors instead of all evaluator fault classes.

- The spec is now aligned with Shane's no-runtime-faults contract: Principle 10 requires the compiler to prove safety or emit an obligation diagnostic, Principle 11 extends the clean-compile guarantee across type, arithmetic, access, and range fault classes, and runtime traps are positioned only as defensive redundancy for compiler-proven-unreachable paths.

- The proof engine contract in §0.6 already supported the stronger guarantee through prove-safe / proved-dangerous / unresolved classification and obligation diagnostics; no proof-engine design change was needed because the principles were catching up to an already stronger compiler contract.

- Frank's philosophy-grounded follow-up endorsed those revisions but flagged a product-identity gap: `docs/philosophy.md` explicitly scopes "prevention, not detection" to invalid entity configurations and does not yet name evaluation-fault prevention with the same explicitness. Recommended wording was recorded for owner review only and was not applied.

- Net result: the spec now clearly states the no-runtime-faults promise, while the philosophy gap is durably recorded as a flag for Shane rather than an auto-applied philosophy change.



---



### 2026-04-29T03:09:18Z: PRECEPT0018 correctness gate closed and test backfill recorded

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (3 files)



**Merged sources:** `frank-precept0018-review`, `george-precept0018-implemented`, `george-precept0018-tests-added`.



- Frank's correctness-gate review confirmed the PRECEPT0018 analyzer, `AllowZeroDefaultAttribute`, all three intentional zero-value exemptions, and the 23 enum fixes were correct, then blocked merge only on three missing required regression tests: TP3 (zero-valued member not first), EC4 (`byte` underlying type), and EC5 (`long` underlying type).

- George's implementation record is now preserved as the baseline landing: commit `a7b0bb7` created the analyzer and attribute, applied `[AllowZeroDefault]` to `LexerMode.Normal`, `QualifierMatch.Any`, and `PeriodDimension.Any`, and made all 23 semantic enums 1-based with 225 analyzer tests and 2044 core tests green.

- George's follow-up commit `e7a643d` closed Frank's B1 finding and the two advisory anchors by adding TP7–TP9 and EC6–EC7 in `test/Precept.Analyzers.Tests/Precept0018Tests.cs`; analyzer tests rose to 230 while core tests stayed 2044.

- Net result: PRECEPT0018 is now durably recorded as implemented and correctness-cleared, with no post-review code changes beyond the missing regression tests.



---



### 2026-04-29T01:39:22Z: Catalog extensibility plan v3 cleared for George

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (2 files)



**Merged sources:** frank-catalog-ext-plan-v2-review, frank-catalog-extensibility-plan-review.



- Frank's first review blocked the catalog extensibility plan on three structural fixes: split PreceptHeader into RoutingFamily.Header, add Slice 3b to the execution order after Slice 3, and commit Slice 3b to explicit wrong-family ConstructKind listings so CS8509 stays active.

- Frank's second review confirmed those first-round blockers were resolved and approved the architecture, but found two new surgical blockers in the revised text: an unbound k variable in the Slice 3b throw examples and a phantom ErrorStatement(current) call in Slice 5.

- The coordinator patched both plan defects in plan.md: the Slice 3b guard text no longer references an unbound pattern variable, and Slice 5 now specifies the real synthetic-error-node handling instead of a nonexistent helper.

- src/Precept/Language/Token.cs was added to the file inventory because Slice 6 changes TokenMeta.IsAccessModeAdjective there, and the GetMeta wildcard note remains a documented non-blocking follow-up.

- Net result: the plan is now at v3, blockers are cleared, and George can implement from the updated plan.



---

### 2026-04-29T01:09:17Z: Catalog extensibility audit and parser design evaluation recorded

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (3 files)



**Merged sources:** `frank-catalog-extensibility`, `frank-parser-design-eval`, `frank-vision-spec-audit`.



- Frank's catalog extensibility audit confirmed the lexer is already 100% catalog-driven for keywords, operators, and punctuation; the remaining extensibility risk is entirely in parser/catalog enforcement boundaries.

- Eight parser hardening gaps are now the durable follow-up list: `BuildNode()` wildcard, `ParseDirectConstruct()` wildcard, hardcoded `DisambiguateAndParse()` routing, `ParseActionStatement()` switch exhaustiveness, hardcoded `ExpressionBoundaryTokens`, missing `ConstructKind`↔declaration subtype enforcement, missing `ActionKind`↔statement subtype enforcement, and hardcoded access-mode adjectives.

- The preferred remediation path is catalog shape change rather than Roslyn analyzers: remove wildcard fallthroughs for CS8509 coverage, derive boundary tokens from `Constructs.LeadingTokens`, add `RoutingFamily` to `ConstructMeta`, and add `ActionSyntaxShape` to `ActionMeta`.

- Frank's parser design evaluation across v5-v8 approved v8 as the closed canonical baseline: current code matches the parser spec, `OmitDeclaration` is correctly split from `AccessMode`, `FieldTargetNode` is a DU, and the 5-layer parser architecture is complete. Working docs are now audit trail, not pending design debt.

- Frank's vision-versus-spec audit found two live contradictions (`with` still listed as a structural preposition in the vision doc, and stale “root editability” wording after `write all` removal) and concluded the vision doc should not be archived until its language-identity material is migrated into the spec.



---



### 2026-04-29T00:43:25Z: Parser remediation review batch approved and synchronized

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (4 files)



**Merged sources:** frank-audit-cross, frank-parser-review, frank-parser-v2-authored, soup-nazi-parser-coverage.



- Parser remediation slices R1-R6 are architecturally approved against the v8 catalog-driven parser design: top-level dispatch is catalog-owned, rule/state/event routing now flows through slot machinery where intended, preposition disambiguation is metadata-driven, and the cleanup removed the unauthorized header comment.

- The permanent parser reference is now authored in docs/compiler/parser-v2.md. It captures the catalog-driven dispatch model, 5-layer architecture, full 12-node declaration hierarchy, OmitDeclaration separation, FieldTargetNode DU, validation pyramid, and expanded parser diagnostics.

- Cross-surface consistency was re-aligned before the review closed: 8 inconsistencies were fixed across the spec, parser reference, slot comments, and token metadata so secondary sources match catalog-first primaries.

- Coverage for the 6 remediation slices is approved at 2034/2034 passing tests. The audit fixed the stale ConstructSlotKind count, replaced the obsolete StateDeclaration slot-count assertion with an exact slot-shape fact, and added EventDeclaration_HasInitialMarkerSlot as the new catalog regression anchor.



---



### 2026-04-28T06:41:30Z: Access-mode vocabulary locked and catalog fix landed

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (13 files)



**Merged sources:** `copilot-directive-2026-04-28T02-14-omit-vs-access-semantic`, `copilot-directive-2026-04-28T02-41-readonly-editable-vocab`, `copilot-directive-when-position`, `copilot-directive-writable-adjective`, `copilot-directive-writeable-spelling`, `frank-rule-7-closed`, `frank-vocab-B1`, `frank-vocab-B2`, `frank-vocab-B3`, `frank-vocab-B4`, `george-accessmode-guard-slot-fix`, `george-parser-complexity-reeval`, `george-parser-complexity-when`.



- Shane locked the access-mode surface as `in StateTarget modify FieldTarget readonly|editable ("when" BoolExpr)?`, with `omit` preserved as the separate structural-exclusion verb. Earlier B1-B4 exploratory vocabulary rounds now collapse to this canonical surface; `->` and adjective-only forms are not the language.

- Durable semantic framing is now explicit: `omit` removes the field from the state's structural schema, while access modes keep the field present and only constrain mutability. Access-mode guards stay post-field, and the writable/writeable spelling debate is superseded by the locked `readonly`/`editable` pair.

- Implementation follow-through is locked: the access-mode body shape is verb + field target + access adjective + optional guard, catalog/token work needs `modify`, `readonly`, and `editable`, and the `AccessMode` disambiguation family is now `modify`/`omit` rather than the retired `read`/`write`/`omit` set.

- George's follow-through landed: `ConstructKind.AccessMode` now ends with `SlotGuardClause`, `DiagnosticCode.RedundantAccessMode` has catalog metadata, the stale `write all` description is removed, a regression test pins guard-slot presence/position, and the suite stayed green at 1809 passing tests.



---



### 2026-04-28T05:08:10Z: Access-mode and parser-design inbox batch canonicalized

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (14 files)



**Merged sources:** `copilot-directive-2026-04-28T01-08`, `copilot-directive-extensibility-validation-20260427`, `frank-access-mode-design-round`, `frank-access-mode-docs-working`, `frank-r5-validation-layer-20260428`, `frank-r7-implementation-plan`, `frank-redundant-access-mode`, `frank-spec-grammar-fixes`, `george-access-mode-docs-working`, `george-access-mode-feasibility`, `george-lang-simplify`, `george-r4-parser-design`, `george-r6-review`.



- Access-mode design round locked the durable shape: guarded `read` is only valid as a writable-baseline downgrade, guarded `omit` stays prohibited because it would make structural field presence data-dependent, and the vocabulary remains `read` / `write` / `omit`. Frank's and George's working docs are the durable references for the reasoning behind those constraints.

- Redundancy handling is now uniform: dead named-field access declarations are compile errors under `RedundantAccessMode`, including `in S write F` on already-`writable` fields, unguarded `in S read F` on non-`writable` fields, and guarded `read` on non-`writable` fields. `RedundantGuardedRead` is retired; `omit` and broadcast `all` forms remain exempt; rule 7 is still open.

- Parser extensibility direction is validation-first, not generator-first: fail loudly when catalog metadata is incomplete, keep `_slotParsers` exhaustive, give rule bodies their own `RuleExpression` slot, keep `ensure` and `because` separate, and reject pre-event `when` on `from ... on` with a diagnostic instead of silently expanding the language surface.

- Design-loop status is now explicit: the v7 parser working doc remains the implementation-plan anchor, while language-simplification proposals were recorded as analysis input and only owner-approved surface changes should be treated as canonical.



---



### 2026-04-28T04:49:58Z: `write all` removed from language — stateless precepts use `writable` modifier

**By:** Shane (owner directive)

**Status:** Applied



**Merged sources:** `copilot-directive-write-all-removed`.



- `write all` is removed from the Precept language entirely. Stateless precepts now opt into mutability only through field-level `writable`; there is no root-level bulk access mode construct.

- This supersedes any earlier record that `write all` survived as stateless sugar.

- Stale references called out in the inbox covered the spec, vision doc, working docs, samples, and token/tooling vocabulary that still described root-level bulk access as live syntax.

- Canonical follow-through: language docs, samples, and downstream tooling must all treat field-level `writable` as the only stateless mutability opt-in.



---



### 2026-04-28T00:00:00Z: Combined Design v2 Structural Revision

**By:** Frank

**Status:** Applied



- Applied boundary reassessment: replaced "hard line / nothing crosses" claim with correct type dependency direction rule; clarified what crosses the lowering boundary.

- Readability/genre fixes: 13 stage-contract tables converted to labeled prose, two artifact tables merged, "How to read this document" added, §8 split, §9 moved to appendix, decision lead-ins added, problem statement added to §1, assertions moved to doc spine.

- No content dropped; all facts, contracts, and assertions preserved. Comparative tables retained where genuinely comparative.

- Motivation: Shifted from reference spec to design doc genre, making decisions and rationale explicit and readable.



---



### 2026-04-28T00:00:00Z: Combined Design v2 Gap Patch Complete

**By:** Frank

**Status:** Complete



- Added 10 missing design specifics to combined-design-v2.md: action-shape model, constraint activation indexes, constraint evaluation matrix, constraint exposure tiers, proof strategy enumeration, proof/fault chain formula, earliest-knowable kind assignment, named anti-patterns, compile-time vs lowered artifact table, implementation action items.

- Locked: three action shapes, precomputed constraint activation, closed proof strategies, explicit proof/fault chain ownership, five implementation action items.

- No philosophy gaps surfaced; all changes are implementation domain only.



---



### 2026-04-27T00:00:00Z: MCP dual-surface operating model canonicalized

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (4 files)



**Merged sources:** `frank-mcp-operating-model`, `newman-mcp-dual-surface`, `soup-nazi-mcp-validation`, `copilot-directive-2026-04-26T11-13-50-367-04-00`.



- Repo-root `.mcp.json` is the Copilot CLI repo-local surface; `.vscode/mcp.json` remains the VS Code/workspace-local surface; `tools/Precept.Plugin/.mcp.json` remains the shipped/distribution payload.

- The authoritative repo-local development behavior stays source-first via `node tools/scripts/start-precept-mcp.js`. Client-specific files are projections/adapters, not separate contracts.

- The `github` MCP entry is intentionally **not** mirrored into repo-root `.mcp.json`; Copilot CLI provides GitHub MCP natively.

- Directly related docs were updated in the same change (`CONTRIBUTING.md`, `.github/copilot-instructions.md`, `tools/Precept.Plugin/README.md`, `.squad/skills/architecture/SKILL.md`), and the stale `docs/ArtifactOperatingModelDesign.md` reference is retired in favor of `tools/Precept.Plugin/README.md`.

- Validation rerun passed: all three MCP config surfaces parse cleanly, schemas stay separated (`mcpServers` for CLI/plugin, `servers` for VS Code), and no directly related stale live reference remains.

- Team pattern locked: dual-surface config work is only considered landed when the config artifact and at least one directly related doc land together.



---



### 2026-04-26T15:48:53Z: Analyzer expansion plan and catalog conventions canonicalized

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (10 files)



**Merged sources:** `coordinator-analyzer-implementation-plan`, `coordinator-analyzer-queue-priority`, `coordinator-catalog-audit-findings`, `coordinator-catalog-conventions`, `coordinator-post-infra-analyzer-analysis`, `copilot-directive-2026-04-26T-catalog-lexer`, `frank-cross-catalog-invariants`, `george-cross-catalog-api-design`, `soup-nazi-analyzer-test-infra`, `soup-nazi-analyzer-test-plan`.



- The April 26 catalog audit now splits cleanly into **fixed now** vs **follow-up work**. Fixed in-session: `Period` gained `EqualityComparable`; qualifier modeling now reflects the full `in`/`of` system with exclusivity rules; `DisplayName` is required/populated for surfaced types; `UsageExample` is populated for surfaced types. Deferred follow-up: `TokenMeta.ValidAfter`, catalog-driven language-server completions, and the rest of the analyzer sweep.

- Canonical analyzer scope is 53 statically checkable invariants (37 cross-catalog, 16 intra-catalog) across 11 analyzers `PRECEPT0007`–`PRECEPT0017`; `PRECEPT0018` is dropped because Tokens is a leaf and exhaustiveness is already covered by `PRECEPT0007`.

- Shared analyzer infrastructure is now the center of gravity: `CatalogAnalysisHelpers.cs` plus a multi-source `AnalyzerTestHelper` overload. Test stubs stay minimal, avoid Frozen/Immutable BCL dependencies, and identify catalogs by class name rather than file path.

- Constructor parameters are the canonical way to express optional catalog metadata. `init`-only metadata properties on catalog records are now explicitly rejected because they create a second analyzer extraction path.

- Queue/order dedupe: the earlier simple-patterns-first plan is superseded by Shane's later directive to front-load the trait↔operation consistency path because it builds reusable switch-walker and enum-resolution infrastructure for the rest of the analyzer suite.

- Soup Nazi's test-plan bar stands: helper tests plus analyzer suites total about 298 cases, with the accepted blind spot limited to spread elements inside shared static arrays and guarded by declaration-site validation/regression anchors.

- Owner directive stands: lexer token classification must converge on fully catalog-driven behavior; implementation tactics may vary, but the architectural target is no-exceptions catalog authority.



---



### 2026-04-26T00:00:00Z: Catalog completeness, consumer drift, and analyzer sprint merge

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (7 files)



**Merged sources:** `frank-catalog-audit-findings`, `george-analyzer-expansion`, `george-lexer-scan-tables`, `soup-nazi-catalog-review-2`, `soup-nazi-catalog-baseline-tests`, `soup-nazi-analyzers-complete`, `soup-nazi-precept0007-proposal`, plus the coordinator's consolidated action list.



**Catalog audit outcome (Frank + George):**

- Surfaces are complete: the type catalog covers all surfaced language types. Frank's spec/design pass found 26 of 26 surfaced types represented; George's code-level inventory confirmed no missing surfaced type entries.

- `Period` is a real cross-catalog correctness bug, not a completeness gap. The spec and operations catalog permit equality, but `Types.cs` does not mark `Period` as `EqualityComparable`.

- `Quantity` qualifier modeling remains an open Shane decision. The current `QualifierShape` model expresses conjunctive qualifier slots well, but the language surface allows `quantity in 'kg'` OR `quantity of 'length'`, which is an alternative shape rather than a simple slot list.

- `TypeMeta.UsageExample` is still null across the catalog. This is not a correctness issue, but it keeps hover and MCP grounding thin.



**Doc-sync findings for `docs/catalog-system.md`:**

- The document must reflect `TypeTrait.EqualityComparable`.

- The documented `TypeMeta` full shape must include `DisplayName`, `HoverDescription`, and `UsageExample`.

- `QualifierAxis` documentation must include the temporal-dimension axis.

- `TypeMeta.Token` must stay nullable (`TokenMeta?`) for internal/sentinel types.

- The documented orderable set is stale: `zoneddatetime` should not be listed as orderable, and `price` should be.



**Consumer drift and enforcement status:**

- Consumer drift, not catalog structure, is now the dominant problem. George confirmed 14 hardcoded language-server completion lists still bypass the catalog contract.

- Lexer operator/punctuation scan tables now derive from `Tokens.All`, removing a parallel vocabulary table from `Lexer.cs` while preserving the hand-written scanner.

- Soup Nazi's catalog baseline pass added dedicated Tokens and Diagnostics catalog tests; all new tests were green.

- Soup Nazi's earlier finding that PRECEPT0005/PRECEPT0006 were missing is now superseded: both analyzers are implemented and green.

- PRECEPT0005 immediately caught a real production bug: the `sqrt` overload proof requirements referenced different `ParameterMeta` instances than the overload parameter list. The fix introduced dedicated named sqrt parameters shared by both `Parameters` and `ProofRequirements`.

- PRECEPT0007 remains a follow-up proposal: flag `Enum.GetValues<CatalogEnum>()` outside the owning catalog `All` getter. There are zero current source violations; test projects remain exempt.



**Backlog concentration after deduplication:**

- The consolidated review's correctness and metadata-gap items are recorded as completed in the source/design pass.

- Remaining work is concentrated in tooling-generation drift, broader analyzer expansion (PRECEPT0007-PRECEPT0014), snapshot/golden catalog tests, and generated matrix coverage.



---



### 2026-04-25T12:00:00Z: Full catalog-system review — 10-item metadata-driven design review (owner sign-off)

**By:** Scribe

**Status:** Merged from 17 inbox files (6 reviews + 5 owner gap resolutions + 6 recommendations)



**Context:** Full team review of `docs/language/catalog-system.md` — the metadata-driven catalog architecture design doc. Reviews by Frank (architecture + completeness + pipeline + source-of-truth), George (pipeline feasibility), Kramer (tooling), Newman (AI/MCP), Soup Nazi (testing). Owner (Shane) dispositioned all gaps and design questions.



**Key owner decisions (Shane sign-off, anti-AI-bias lens):**

- **DQ1–DQ2:** `AllowedIn ConstructKind[]` added to both `ConstructMeta` and `ActionMeta` — uniform nesting/context pattern across both catalogs. LS completions filter by parent construct kind.

- **DQ3:** Period dimension legality declared as `DimensionProofRequirement` (new `ProofRequirement` subtype) on `BinaryOperationMeta` entries for `date ± period` and `time ± period`. New `PeriodDimension` enum (`Any`, `Date`, `Time`).

- **DQ4:** Qualifier-to-accessor identity declared via `QualifierAxis` enum and `ReturnsQualifier` field on `FixedReturnAccessor` — `.currency` on `money` returns the field's currency qualifier value.

- **Gap 1 (Type Accessors):** `TypeAccessor` DU — base record (inner-type return for `.peek`/`.min`/`.max`) + `FixedReturnAccessor` sealed subtype (fixed `Returns: TypeKind` for `.count`/`.currency`/`.inZone(tz)`). New `TypeTrait` flags enum (`Orderable`). New `TypeMeta.Traits` and `TypeMeta.Accessors` fields.

- **Gap 2 (Unary Operations):** `OperationMeta` becomes abstract base with `UnaryOperationMeta` and `BinaryOperationMeta` sealed subtypes. Separate frozen-dictionary indexes. 8 unary operations declared.

- **Gap 3 (Widening):** `TypeMeta.WidensTo TypeKind[]` — only `integer → [Decimal, Number]`. `decimal → number` is NOT implicit (requires `approximate()`).

- **Gap 4 (Subsumption + Implied Modifiers):** `ModifierMeta.Subsumes ModifierKind[]` (e.g., `Positive.Subsumes = [Nonnegative, Nonzero]`). `TypeMeta.ImpliedModifiers ModifierKind[]` (e.g., `ExchangeRate.ImpliedModifiers = [Positive]`). Roslyn analyzer enforces consistency.

- **Gap 5 (Business-type function overloads):** `round`, `min`, `max`, `abs` extended to `money` and `quantity` with `QualifierMatch.Same`. `FunctionOverload` gains `QualifierMatch?` field.

- **Gap 6 (clear dual-target + TypeTarget DU):** `ActionMeta.ApplicableTo` migrates from `TypeKind[]` to `TypeTarget[]`. New DU: `TypeTarget(TypeKind)` base + `ModifiedTypeTarget(TypeKind?, ModifierKind[] RequiredModifiers)` sealed subtype. `clear` targets `[Set, Queue, Stack, ModifiedTypeTarget(null, [Optional])]`.

- **Gap 7 (ProofRequirement system):** `ParameterMeta(TypeKind)` replaces raw `TypeKind[]` in `FunctionOverload.Parameters` and `BinaryOperationMeta.Lhs/Rhs`. `ProofSubject` DU (`ParamSubject` + `SelfSubject`). `ProofRequirement` DU (`NumericProofRequirement` + `PresenceProofRequirement` + `DimensionProofRequirement`). 8 proof obligations declared. Roslyn analyzer enforces valid subject placements.

- **Modifier DU:** 5-subtype `ModifierMeta` hierarchy — `FieldModifierMeta`, `StateModifierMeta`, `EventModifierMeta`, `AccessModifierMeta`, `AnchorModifierMeta`. Absorbs 4 bare enums (`StateModifierKind`, `AccessMode`, `EnsureAnchor`, `StateActionAnchor`). Each subtype carries exactly the metadata its consumers need.

- **FunctionMeta evaluation delegates:** `FunctionDispatch` record with `Func<ReadOnlySpan<object?>, object?> Execute` delegate + `Overload` reference. `FunctionMeta.Dispatches` field. Eliminates parallel switch in evaluator.

- **TokenMeta cross-references:** `TypeKind?` and `OperatorKind?` optional fields on `TokenMeta` — compile-time-verified bridge from tokens to their semantic catalog entries.

- **Newman's syntaxReference:** Structured `syntaxReference` JSON object in MCP `precept_language` output — line-oriented grammar, comments, identifiers, string/number literals, whitespace, null-narrowing, conventional declaration ordering.



**Review corpus (retained for reference, not duplicated here):**

- **frank-catalog-completeness-review:** 10 catalogs confirmed sufficient, no 11th needed. 10 candidate categories evaluated and rejected. Types catalog needs accessor metadata enrichment. Modifiers catalog scope includes event args.

- **frank-catalog-metadata-pipeline-review:** Pipeline feasibility per stage. Parser: vocabulary tables (~40-50%) migrate to frozen dictionaries; grammar stays hand-written. Type checker: ~80% catalog-driveable. Lexer: ~85% already catalog-driven. Proof engine: obligations catalog-driven, strategies algorithmic. Evaluator: dispatch via catalogs, execution logic procedural. String elimination audit: already string-minimal. Cross-catalog dependency graph acyclic.

- **frank-catalog-source-of-truth-analysis:** 8 priority-ranked gaps across all 10 catalogs. Type accessors (gap 1) is highest impact — 30+ accessors across all types. Unary operations unrepresentable in binary-only schema. Widening, subsumption, business-type overloads, clear dual-target, dequeue/pop emptiness all identified.

- **frank-metadata-resolver-architecture:** Dual-layer (declarative + resolver delegate) pattern analysis. Resolvers justified for TypeAccessor and FunctionOverload (generic/polymorphic behavior). 5 of 8 gaps better solved by richer declarative fields. Serialization boundary clean — declarative layer sufficient for all non-compiler consumers.

- **frank-qualifier-propagation-architecture:** QualifierMatch design validated. Four propagation patterns (Homogeneous ±, Scalar scaling, Dimensional cancellation, Same-type ratio). Qualifier propagation is type-checker logic, not catalog data. `Operations.Resolve` returns `OperationMeta?` not `Type?`. MoneyDivideMoney polymorphism: two OperationKind entries recommended.

- **frank-typechecker-implementation-plan:** 16-slice implementation plan with dependency graph. 5 new files (~2500 LOC impl), 5 test files. 13 new DiagnosticCodes. Sequential gates at slices 1, 2, 4, 5. Parallelizable groups identified.

- **george-catalog-metadata-pipeline-review:** Independent feasibility assessment. Lexer: already catalog-driven. Parser: partially feasible (lookup tables yes, productions no). Type checker: biggest win (~60-70% migrates). Graph analyzer: mostly graph-generic. Proof engine: obligations catalog-driven, strategies algorithmic. Evaluator: dispatch feasible, execution logic not.

- **kramer-catalog-metadata-tooling-review:** 14 hardcoded LS completion lists mapped to catalog replacements. 12 TextMate grammar alternations mapped to catalog derivation. Semantic tokens ~90% catalog-driven. Hover: function signatures from FunctionMeta.Overloads. Drift tests over auto-generation recommended.

- **newman-catalog-metadata-ai-review:** `precept_language` covers ~40% of AI needs. Critical gaps: no operation legality table, no widening rules, no modifier applicability matrix, no accessor documentation. Serialization: single response, catalog-keyed grouping, `$type` tagged unions. `syntaxReference` for meta-grammar. One-shot complete language, no per-catalog endpoints.

- **shane-catalog-dq-resolution:** DQ1 (AllowedIn on ConstructMeta), DQ2 (AllowedIn on ActionMeta), DQ3 (DimensionProofRequirement), DQ4 (QualifierAxis + ReturnsQualifier on FixedReturnAccessor).

- **shane-catalog-gap-resolution-1-4:** TypeAccessor DU, OperationMeta DU, widening rules, modifier subsumption + implied modifiers. Full TypeMeta shape decided.

- **shane-catalog-gap-resolution-5-6:** Business-type function overloads with QualifierMatch. TypeTarget DU replacing TypeKind[] on ApplicableTo. ActionMeta.ApplicableTo with clear dual-target.

- **shane-catalog-gap-resolution-7:** ProofRequirement system — ParameterMeta, ProofSubject DU, ProofRequirement DU, 8 proof obligations, Roslyn analyzer enforcement rules.

- **shane-modifier-du-and-review-synthesis:** 5-subtype Modifier DU absorbing 4 bare enums + 10 consolidated review insights with disposition (5 consensus, 3 pending → decided, 2 already resolved).

- **soup-nazi-binary-null-context-test-gap:** Binary sub-expression null-context test strategy for Slice 5. OperatorTable unit tests primary surface. Integration uses field+field not field+literal. Typed-context paths deferred to first assignment surface.

- **soup-nazi-catalog-metadata-test-review:** ~15,500 auto-generated test cases projected. Operations matrix (13,520 cells) is P0. Per-catalog snapshot golden files. Exhaustive matrix before old logic replaced. Cross-catalog referential integrity. Non-negotiable: no catalog without snapshot test.

- **coordinator-design-doc-mandatory-reads:** Directive — every agent spawn for implementation work MUST include relevant design docs as required reading. Triggered by George implementing OperatorTable without reading catalog-system.md.



---



### 2026-04-24T00:00:00Z: Decision inbox merge — Precept.Next v2 design review corpus and pre-TypeChecker gate

**By:** Scribe

**Status:** Merged, deduplicated, inbox cleared (15 files)



**Precept.Next pre-TypeChecker gate (Frank, George, Soup Nazi — 2026-04-24)**



Four blockers block TypeChecker implementation:

- **F1 / G1 / SN-B1 (convergent):** `TypedModel` is a one-field stub (`ImmutableArray<Diagnostic> Diagnostics` only). The type-checker doc specifies a 12-property record with full symbol tables (FieldSymbol, StateSymbol, EventSymbol, ArgSymbol), resolved declaration arrays (Rules, Ensures, TransitionRows, AccessModes, StateActions, StatelessHooks), InitialState, TypedExpression annotation, ResolvedModifiers, and a 27-type ResolvedType hierarchy. None of these types exist in the codebase. Resolution: define the full TypedModel shape before TypeChecker implementation begins. Doc is authoritative.

- **F2:** `SyntaxTree.Root` is typed `PreceptNode?` in code; type-checker doc guarantees it is always non-null (IsMissing, never null). Resolution: change to `PreceptNode` (non-nullable); parser must synthesize an IsMissing PreceptNode on catastrophic failure.

- **F3 / SN-B2 (convergent):** `DiagnosticCode.cs` is missing at least 13 type-stage codes: DuplicateFieldName, DuplicateStateName, DuplicateEventName, DuplicateArgName, UndeclaredState, UndeclaredEvent, MultipleInitialStates, NoInitialState, CaseInsensitiveStringOnNonCollection, InvalidModifierBounds, UnguardedCollectionAccess, UnguardedCollectionMutation, NonOrderableCollectionExtreme (and likely more in temporal/business-domain sections). Resolution: audit full type-checker doc, add all missing `DiagnosticCode` values, add `GetMeta()` entries in `Diagnostics.cs`.

- **F4:** No SourceSpan→SourceRange bridge defined. TypeChecker receives SyntaxTree with SourceSpan nodes; `Diagnostics.Create` requires SourceRange. Two options: (a) `TypeChecker.Check(SyntaxTree, string source)` — pass source text and build line map internally; (b) store `SourceRange` on `SyntaxNode` alongside `SourceSpan` at parse time. One must be chosen before implementation.



George's additional findings (not converged with Frank):

- **G2:** GraphResult and ProofModel are equally hollow (only `Diagnostics`). GraphResult must carry reachability sets, dominator trees, edge classifications. ProofModel must carry proof attribution. Both must be designed before GraphAnalyzer/ProofEngine implementation.

- **G3:** `pipeline-artifacts-and-consumer-contracts.md` has stale Version API (`Edit()` does not exist; `Fire()` returns `Version` not `EventOutcome`). Newer `docs.next/runtime/runtime-api.md` is correct. Fix the stale doc independently.

- **G5:** `~string` CaseInsensitive: syntax exists (ScalarTypeRef.CaseInsensitive, TokenKind.Tilde, BinaryOp.CaseInsensitiveEqual); type system behavior unspecified. Lock before TypeChecker implementation: (a) Is `~=` valid between plain `string` and `~string`? (b) Does `set of ~string` make `contains` case-insensitive? (c) Is `~string` assignable from `string`?



Soup Nazi's additional findings:

- No test files exist for TypeChecker, GraphAnalyzer, ProofEngine, Compiler, or Runtime.

- Non-blocking gaps: FaultsTests.cs (~10 tests warranted today), CompilerTests.cs (smoke placeholder appropriate), ~string tests belong in TypeCheckerTests.cs, GraphAnalyzerTests/ProofEngineTests deferred.

- Recommended TypeChecker slice order: expand DiagnosticCodes → define TypedModel shape → add GetMeta entries → create TypeCheckerTests.cs stubs → implement TypeChecker.Check.



**Precept.Next v2 design review corpus (Frank, George, Soup Nazi, Elaine — 2026-04-23/24)**



Cross-document blockers affecting all v2 stages (converged across Frank, George, Soup Nazi):

- **Numeric-lane contract contradiction:** `precept-language-spec.md` §3.6 says `decimal op number → number` via common-type widening. `type-checker.md` §4.2a says `decimal + number` is a type error requiring `approximate()`. These are mutually exclusive. Resolution (Frank's recommendation): spec §3.6 should add an explicit exception — `decimal op number → type error` in arithmetic expression context; the widening chain `integer → decimal → number` holds for assignment context only. Spec §3.7 already describes `approximate()` as the bridge.

- **Type-checker function catalog incomplete:** Spec §3.7 lists 14 functions; type-checker doc Built-in Function Catalog lists only 8. Missing: `floor`, `ceil`, `truncate`, `round(value)`, `approximate`, `pow`. Bridge functions are described in §4.2a but not registered in the catalog table. Resolution: make the catalog table match §3.7 exactly.

- **Typed-constant validation stage contradiction:** temporal-type-system.md and business-domain-types.md promise compile-time errors for invalid constants (e.g., `'2026-02-30'`); type-checker.md Deliberate Exclusions says these are runtime-only. Resolution: lock stage ownership, assign diagnostic codes.

- **`nullable` surface leakage (George + Soup Nazi):** temporal-type-system.md and business-domain-types.md still show `field X as <type> nullable` throughout. v2 uses `optional`. Update both docs.



Frank's docs.next/ architecture review findings (2026-04-24):

- **Language README NodaTime name leakage:** `docs.next/language/README.md` uses `localdate`/`localtime`/`localdatetime` (NodaTime class names) instead of DSL surface names `date`/`time`/`datetime`; `zoneddatetime` is missing. Fix.

- **Spec §3.6 / type-checker doc numeric-lane contradiction** (same convergent issue above).

- **Function catalog incompleteness** (same convergent issue above).

- **B4 (Frank-docs):** Language README has stale description of docs.next structure; minor, fix alongside B1.



Frank's v2 AST & parser design review findings (2026-04-23):

- **No method-call expression support:** `.inZone(tz)` cannot be represented. No `MethodCallExpression` node; no Led handler for `(`; nud Identifier peek approach prevents chained `expr.method(args)`. Decision needed: add `MethodCallExpression(Expression Object, Token Method, ImmutableArray<Expression> Args)` + Led handler (recommended if any temporal/domain method needs args), or redesign `.inZone(tz)` as prefix `inZone(expr, tz)`.

- **Transition row action-chain grammar incorrectly specified:** double-consumption bug in the grammar mapping; misaligned with stateless hook pattern. Fix: `Expect Arrow; while ActionKeywords → ParseActionStatement()` (single arrow, consecutive actions, then outcome keyword).

- **Non-blocking:** nud Identifier peek logic is non-standard Pratt; computed field modifier ordering unclear in XML comment; event arg syntax vision (`event Create(...)`) vs parser (`event ... with ...`) needs decision; guarded-write ordering vision (`when Guard write Fields`) vs parser (`write Fields when Guard`) needs decision; `(` in binding power table has no Led handler.



George's v2 design review findings (2026-04-23/24):

- **TypedModel qualifier incompleteness:** `MoneyType(string? CurrencyBasis)` and `QuantityType(string? DimensionFamily)` exist but `PeriodType`, `PriceType`, `ExchangeRateType` are bare. Declaration-position interpolation (`in '{BaseCurrency}'`) makes qualifier depend on data fields — not representable as static ResolvedType. Qualifiers must be first-class; dynamic interpolation needs a decision on representability.

- **Typed-constant family registry incomplete:** literal-system.md only covers temporal families; business-domain types (`money`, `quantity`, `currency`, `unitofmeasure`, `dimension`, `price`, `exchangerate`) lack matcher entries. `Word/Word` already means timezone; business docs need slash-based families too. One authoritative registry-backed matcher table is required.

- **v2 AST stale grammar (George finding):** `SyntaxNodes.cs` and `parser.md` still show `event ... with ...` and `in State write Fields when Guard` — vision uses `event Name(...) initial` and `in State when Guard write Fields`.



Soup Nazi's v2 design review findings (2026-04-24):

- **Diagnostic identity model unresolved:** Core compiler docs use name-based codes (`TypeMismatch`, `UndeclaredField`); business-domain doc reserves numeric `C99`–`C110`; diagnostic-system doc rejects numeric codes. `TypeMismatch` shared across structurally distinct errors. Must lock one model before regression tests can be stable.

- **Business-domain/temporal type integration deferred in checker blueprint:** temporal accessors (`.inZone(tz)`), operator tables, qualifier checks, and `in`/`of` enforcement are not folded into the main type-checker semantic tables. Core checker blueprint is not the single authoritative test matrix.

- **Collection totality unspecified:** vision doc requires emptiness guards for `.min`/`.max`/`.peek`; checker blueprint only exposes `.peek`; no emptiness-check rule or diagnostic defined. `dequeue`/`pop` empty-collection behavior also unspecified.



Elaine's diagnostic UX review (2026-04-23):

- **BLOCKED:** 8 messages use vocabulary a domain expert cannot act on. Key findings: `UnterminatedStringLiteral`/`UnterminatedTypedConstant`/`UnterminatedInterpolation` use compiler vocab ("string literal", "typed constant", "interpolation expression"); `InvalidCharacter` is structurally broken — fires for 3 distinct problems (unrecognized char, unrecognized escape, lone `}`) with one undifferentiated message. Requires 3 separate codes. `InputTooLarge` exposes raw power-of-two number with no actionable direction. `NullInNonNullableContext` uses "non-nullable." `InvalidMemberAccess` uses "member accessor." `UnsatisfiableGuard` uses "provably unsatisfiable."

- All 8 proposed replacement messages documented in inbox file with rationale.



Lexer reviews (Frank, Soup Nazi — 2026-04-23):

- **Frank's lexer design review — APPROVED WITH CONDITIONS:** (a) TokenStream missing token array — add `ImmutableArray<Token> Tokens` to `TokenStream`; (b) DiagnosticCode missing `UnterminatedTypedConstant` and `UnterminatedInterpolation` — add with GetMeta arms; (c) spec §1.3 "no exponent notation" contradicts literal-system.md listing `1.5e2` as valid — spec governs, update literal-system.md. Non-blocking gaps: `EndOfSource`/`EndOfFile` name mismatch; `set` dual-use strategy implicit, needs documentation comment.

- **Soup Nazi lexer design sync:** 3 test gaps found and closed — inclusive 65,536-character security ceiling, empty string interpolation boundary-token emission, nested string literal lexing inside interpolation. `dotnet test` passed 42/42. Focused Lexer.cs coverage: 517/519 lines (99.6%). No remaining meaningful lexer design/spec coverage gaps.

- **Soup Nazi lexer tail coverage:** Added contract-level tests for invisible invalid characters, mid-literal bad-escape recovery, plain EOF-unterminated literals, typed-constant middle segments, numeric lookahead fallback. Did not add filler for unreachable branches.

- **Soup Nazi lexer test plan — BLOCKED:** (a) input-too-large contract not locked — spec says lex abort with no tokens; code still returns EndOfSource token; (b) quoted-literal recovery not stable for lone backslash at EOF. First test slice must lock malformed quoted-literal tail before broader golden assertions. Full test plan: 100–110 cases across 10 files (keywords/identifiers, numbers/operators, structural tokens, string literals, typed constants, interpolation/nesting, diagnostics/recovery, boundary/adversarial, sample smoke). First slice: scaffold + quoted-literal tokens and recovery (~18–20 tests) + one boundary test for input-too-large.

- **Coordinator lexer directive:** Lexer does NOT emit synthetic closing tokens on unterminated interpolated literals. Parser MUST handle missing closing token as error-recovery path. Check for EndOfSource when walking Start/End pairs.



**Emitter architecture decision (Shane directive, 2026-04-22):**

No "emitter" as a named component. Lowering lives inside `Precept.From(CompilationResult)` — it is the runtime's construction logic, not a separate pipeline stage. `Precept.From()` guards on `CompilationResult.HasErrors` and throws on failure. Pipeline: 5 stages (no emitter). Architecture docs updated: `architecture-planning.md` §3 reframed as "Executable Model", emitter.md row removed from design doc map, all emitter references replaced with runtime/executable model/Precept.From() language.



---



### 2026-05-18T00:25:00Z: README DSL Hero Image Width Contract

**By:** Elaine (UX), Kramer (Tooling), with Frank's sizing analysis preserved

**Status:** Applied



The README DSL hero remains an image-based branded treatment, but it must now be sized against GitHub's actual repo-view image ceiling instead of the wider article frame.



**Decision:**

- Keep the README DSL hero as an image for now

- Regenerate/capture it at **1660px** source width from an **830px** viewport at **2×** device scale

- Treat **830px** as the effective GitHub repo README image display cap for this asset

- Tune the rendered code text for about **13px** apparent size at display

- Spend any extra composition room on whitespace rather than on additional contract width

- Preserve `design/brand/capture-hero-dsl.mjs` as the repeatable regeneration path



**Tradeoffs and retained learning:**

- Native README text/fenced code remains the only fully robust way to keep DSL text scaling in lockstep with surrounding prose across viewport and zoom changes.

- GitHub page-geometry research still matters: the repo shell tops out around **1280px** and the README/article frame around **1012px**, but the displayed README image for this treatment clamps earlier at about **830px**.

- Do not rely on custom CSS, sanitizer-sensitive HTML, or viewport-specific image swapping as a stable README contract.



---



### 2026-04-22: R1 RESOLVED — Static evaluator with pure functions

**By:** Shane (owner), via findings walk-through

**Status:** Accepted — implemented



Finding 1 accepted. The runtime evaluator (`Evaluator.cs`) is a static utility class with pure functions — no instance state, no inheritance hierarchy. Single choke point for fault production via `Fail()` method. Fire/Edit/Inspect signatures deferred pending R2 (result type taxonomy) and R4 (executable model contract).



**Artifact:** `src/Precept.Next/Runtime/Evaluator.cs`



---



### 2026-04-22: R3 accepted — Immutable entity snapshots confirm existing Version shape

**By:** Shane (owner), via findings walk-through

**Status:** Accepted — no code changes needed



Finding 2 accepted. R3 (immutable snapshots) confirms the existing `Version` type shape is correct. This is a public API contract belonging in `runtime-api.md`, not an evaluator implementation detail in `evaluator.md`.



**Design doc placement:** `docs.next/runtime/runtime-api.md` (Phase 4)



---



### 2026-04-22: Unified architecture proposal — merge compiler + runtime planning docs

**By:** Shane (owner directive)

**Status:** Accepted — implemented



Merged `compiler-architecture-proposal.md` and `runtime-architecture-proposal.md` into a single `architecture-proposal.md`. Rationale: D8 (compiler emitter) and R4 (runtime executable model) are two halves of the same specification — phasing can't be read independently. A single document makes the dependency graph visible.



**Key structural decisions:**

- §3 "Emitter & Executable Model" sits at the boundary — not forced into compiler or runtime

- Design doc for the boundary contract: `docs.next/executable-model.md` (peer to both subdirectories)

- Decision identifiers preserved: D1-D8 (compiler), R1-R7 (runtime)

- Unified 4-phase design work sequencing with 16-document map

- Design docs split: `docs.next/compiler/`, `docs.next/runtime/`, `docs.next/executable-model.md`



**Artifacts:** `docs.next/architecture-proposal.md` (created), old files removed



---



### 2026-04-22: Design doc placement — R3 belongs in runtime-api.md

**By:** Shane (owner, challenged during walk-through)

**Status:** Standing decision



Shane challenged the initial placement of R3 (immutability guarantees). Resolution: immutability is a public API contract, not an evaluator implementation detail. R3 documentation belongs in `docs.next/runtime/runtime-api.md`, not `docs.next/runtime/evaluator.md`.

