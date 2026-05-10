## Core Context

- Owns language research, spec wording, and cross-surface architecture documentation for the Precept DSL and runtime.
- Catalog metadata remains the language truth; runtime, tooling, and docs must derive behavior from durable catalog shape rather than enum-identity switch logic or parallel lists.
- Public API surfaces expose stable CLR/JSON interchange contracts; evaluator internals stay internal.
- Investigation docs can be archived once their outcomes are captured in canonical docs, proposals, or the squad decision ledger.

## Learnings

- Track 2 master implementation plan written at `docs/Working/track2-implementation-plan.md`: 15 slices, all 54 bugs mapped, status tracker ready. Key structural findings: `OperatorMeta` has no `StaticResultType`/`ResultTypePolicy` (root cause of 5 operator-typing bugs at once); `TokenMeta.IsValidAsMemberName` exists but covers only `min`/`max` and must be extended to 9 type-keyword accessor tokens; `AccessModeDto` and `StateHookDto` are already defined in `CompileToolDtos.cs` but orphaned (not wired into `PreceptDefinitionDto`); `ActionSyntaxShape` has all correct shapes (`CollectionValueBy`, `InsertAt`, `RemoveAtIndex`) but the parser ignores them. Fix order is mandatory: catalog fields → pipeline fixes → MCP DTOs → docs → tests.
- 2026-05-09T23:49:11.879-04:00 — Track 2 Phase A is not seven equal implementation slices. Slice 2 is an audit-only checkpoint because `Actions.cs` already carries the needed `ActionSyntaxShape` truth; the real Phase A work is metadata-only on slices 1, 3, 4, 5, 6, and 7, with consumer rewires and integration tests deferred to later slices.


- `SemanticTokenTypes` is now the approved 14th catalog; `TokenMeta` should carry one `VisualCategory`, and token-surface projections must derive from catalog metadata rather than parallel token fields.
- The production LS gap-closure plan is Phase 2 in `docs/Working/language-server-implementation-plan.md`: expression/default-position completions, catalog-complete hover, navigation, selection/document symbols, semantic-token cleanup, version ordering, and related VS Code polish.
- Outline metadata is settled in catalog form: `ConstructMeta.IsOutlineNode` + `OutlineSymbolTag`, with the LS projecting the string tag to `SymbolKind` instead of pulling LSP protocol types into `src/Precept/`.
- `DiagnosticMeta` enrichment and `QuickstartCatalog`/`SyntaxReference` authoring metadata are already present in source and should be consumed rather than re-described elsewhere.
- Typed-literal work stays inside the 12-slice plan: `ContentValidation` is the metadata hook, compile-time literal validation goes through `TypedConstantValidation.Validate(...)`, and runtime JSON lanes go through `TypeRuntime<T>` / `TypeRuntimeMeta`.
- ISO 4217 and UCUM remain embedded external reference datasets, not Precept catalogs; Precept-owned augmentation lives in source metadata on top of those datasets.
- The focused AI-authoring MCP suite remains the durable authoring direction; `precept_language` is fallback/internal while named tools own discovery.
- The 52-bug audit made the current highest-risk gaps explicit: parser routing/disambiguation still ignores catalog grammar in multiple places, MCP definition/docs DTOs still flatten or omit catalog-derived structure, and several type-checker result types still come from hardcoded operator dispatch instead of `Operations` metadata.
- Highest-leverage prevention layer: catalog-reflection fixture tests plus real-catalog contract tests (especially MCP definition matrices, parser routing/disambiguation, keyword-collision accessors, and hook branches).
- Tracker hygiene is now a durable operating rule: status changes at the same boundary as execution state, evidence level must be recorded immediately, and only one slice per track stays active unless an explicit parallel split is logged.

- The pipeline audit found 27 catalog-compliance violations concentrated in Parser, TypeChecker, and ProofEngine: wildcards/broadcasts are still carried as raw names, parser grammar still depends on local token/separator branches, qualifier and modifier meaning still leaks through enum-identity checks, and proof discharge still embeds operator implication/diagnostic tables instead of reading metadata.

- 2026-05-10T09:46Z — t2-2 slice plan: BUG-021/048/049 all share one root cause — `ActionSyntaxShape` tells the parser which structural pattern but not which tokens. `ParseActionTarget` hardcodes a union of all separator tokens (`=`, `into`, `by`, `at`) regardless of active shape, and each shape method hardcodes its own separators. Fix is `ActionShapeMeta` with `ActionSyntaxSlot[]` carrying per-slot separator tokens and optionality, then parser reads from that metadata. Three vertical slices: catalog enrichment, target-terminator rewire, shape-method rewire.

- 2026-05-10T09:41Z — BUG-006/BUG-051 triage: PRE0009 on `min(A,B)` in the live editor was caused by a stale extension build (DLL built 28 min before the fix commit). George's parser fix (`IsFunctionCallLeader` routing in `ParseNud`) and regression test are both correct. No code change needed — rebuilding the language server DLL resolves the editor symptom. Verdict written to `.squad/decisions/inbox/frank-bug006-051-triage.md`.

- 2026-05-10T15:26Z — BUG-049a design review (PRE0084 spurious on `insert F V at I` with plain list): `ReturnNonnegative` on `FixedReturnAccessor` is the right abstraction — parallels `FunctionOverload.ReturnNonnegative` with same consumption pattern in Strategy 2. Key blocker: the duplicate `CollectionCount` (Actions.cs) / `CollectionCountAccessor` (Types.cs) must be unified in this PR, not deferred — two accessor instances with identical semantics violates catalog single-source-of-truth. Doc gap: `FunctionReturnSatisfies` is a pre-existing undocumented Strategy 2 discharge path; fix alongside accessor docs.

## Historical Summary

- Earlier May 2026 work locked the typed-literal boundary, the external-data posture for ISO/UCUM, the catalog-driven parser/checker trajectory, and the requirement that durable rationale live in decisions/research instead of scattered implementation switches.
- Recent batches settled the LS baseline: Slice 0/0b infrastructure and shim cleanup, `TypedField.NameSpan`, `ArgReference` recording, snippet-template metadata, and the Phase 2 production gap-closure plan.
- Use `.squad/decisions.md` for the exact batch chronology and `docs/` / `research/` for the surviving canonical rationale.

## Recent Updates

### 2026-05-10T15:34:08Z — BUG-049a design review obligations closed
- Scribe merged your BUG-049a design review with George's Slice 2E completion into one canonical `.squad/decisions.md` closeout entry.
- Both required follow-through items are now durably recorded as shipped: `Actions.cs` reuses the shared `Types.CollectionCountAccessor`, and Strategy 2 docs now cover both `FunctionReturnSatisfies` and accessor `ReturnNonnegative` intrinsic-return discharge.
- George's fix added the accessor metadata path plus 3 regression tests; targeted `dotnet build src\Precept\Precept.csproj` and `dotnet test test\Precept.Tests\Precept.Tests.csproj` closed green at 3857 tests.

### 2026-05-10T13:46:52Z — BUG-006 / BUG-051 stale-build verdict recorded
- Scribe merged your BUG-006 / BUG-051 triage note into `.squad/decisions.md` as the durable ruling: PRE0009 on `min(A,B)` in the editor came from a stale language-server build, not a missing source fix.
- The recorded action is operational only: rebuild the extension/language-server output so the live editor picks up George's already-correct `IsFunctionCallLeader` routing; no new code change is needed.

### 2026-05-10T13:29:53Z — TokenMeta bool-shape ruling executed and recorded
- Your TokenMeta design review is now merged into `.squad/decisions.md`: the flat bools stay, no grouping/flags redesign was approved, and alias properties are treated as forbidden parallel copies.
- George executed the prescribed cleanup in commit `19569dda`, removing both aliases and updating the one language-server callsite plus two test references while keeping 3824/3824 green.


### 2026-05-10T04:20:44Z — Status-hygiene protocol merged and tracker drift cleared
- Scribe merged Frank's status-hygiene rule and the matching user directive into the canonical decision ledger.
- Frank's reconciliation batch closed stale active rows: Track 1 already matched evidence, Track 2 now has only Slice 3 active, and the modifier-model rename remains the only other open edge called out at close.


### 2026-05-10T03:13:51Z — Bug cluster analysis merged and operationalized
- The 52 confirmed toolchain bugs are now durably classified by stage: Parser 17, MCP serialization 15, Type Checker 10, Name Binder 4, Proof Engine 3, MCP docs 3.
- Dominant causes are now locked: parser/catalog drift, MCP DTO projection drift, and hardcoded type-checker operator behavior where catalog metadata should drive the result.
- Scribe merged the analysis with Soup-Nazi's testing verdict into one canonical decision entry, and Kramer's Track 2 status table makes the register executable for follow-up work.


### 2026-05-10T02:50:04Z — Visual taxonomy and LS Phase 2 direction recorded
- `SemanticTokenTypes` is the approved catalog surface for token visual categories, and constrained events stay in the shared italic constraint system.
- LS Phase 2 is the active production gap-closure plan, with `set` in type position called out as the sharpest cross-surface bug to fix contextually.


### 2026-05-09T23:46:43Z — LS docs reconciled and field-span prerequisite closed
- The LS design/plan docs were reconciled to the live source, and `TypedField.NameSpan` landed as the thin-core prerequisite that unblocks name-based editor projections.
- The remaining open LS contract question from that batch is still the `precept/inspect` restore-failure surface.

### 2026-05-10T04:33:18Z — Track 2 plan language and pipeline audit are now canonical, but the lane is paused
- The no-deferral rule now applies explicitly to implementation-plan wording and to plan-cleanup prompts: required work must sit in its owning slice with no "skip for now" language.
- Your Track 2 master-plan/Phase A guardrail notes and the pipeline audit findings are now merged into `.squad/decisions.md` as the durable architecture record.
- Execution priority changed immediately afterward: Track 2 is paused until Shane explicitly reopens it.

### 2026-05-10T11:52:58Z — Track 2 Phase A doc sync D1-D8 + T1 test gap closed

- Fixed all 8 doc gaps in `docs/language/catalog-system.md` identified in the Phase A audit.
- `TokenMeta`: Added `IsStateWildcard`, `IsFieldBroadcast`, `IsFunctionCallLeader`; moved `IsValidAsMemberName` to computed property note.
- `OperatorMeta`: Added `StaticResultType`/`ResultTypePolicy` fields; documented `ResultTypePolicy` enum with assignment rules.
- `ConstructMeta`: Fixed stale `LspSymbolKind` → `OutlineSymbolTag`; added `SupportsPreVerbWhenGuard`/`SupportsPostActionEnsure`; updated to show `Entries`/`RoutingFamily`.
- `OutcomeMeta`: Added `SerializedKind: string`; updated summary table with values.
- `FunctionOverload`: Added `ReturnNonnegative: bool = false` with usage note.
- `FixedReturnAccessor`: Added `ReturnNonnegative: bool = false` with proof-engine Strategy 2 note.
- `ValueModifierMeta`: Renamed `ProofDischarges` → `ProofSatisfactions`; replaced `ProofDischarge` flat record with `ProofSatisfaction` DU; added `BoundCounterpart`/`DesugarsToRule`; documented `ApplicableDeclarationSites` rules.
- `ActionMeta`: Removed `IntoSupported`; added `HoverDescription`/`SnippetTemplate`/`PrimaryActionKind`; updated `ActionKind` count 8→15; added `ActionShapeMeta`/`ActionSyntaxSlot`/`ActionSlotRole` section with 9-shape slot table.
- T1: Added `Writable_ExcludesEventArgumentDeclarations` and `Default_IncludesEventArgumentDeclarations` named tests to `ModifierCatalogCapabilityTests.cs`. 3871/3871 green.
- Structural observation: `catalog-system.md` had drifted from the Open Questions pattern — the ProofDischarges section still carried implementation checklist items (including unchecked ones) after the work shipped. Moving forward, completed catalog work should close its checklist items in the same commit that lands the source changes.

- Full audit of all 7 Phase A slices against the toolchain plan, catalog-system.md, proof-engine.md, and source code.
- Source code verdict: **APPROVED** — all fields correctly implemented, all members correctly assigned, test coverage meets or exceeds plan for all slices.
- Doc verdict: **NEEDS FIXES** — 8 doc gaps in `catalog-system.md` where new catalog fields are absent from schema sections. Additionally, stale field names (`ProofDischarges` → `ProofSatisfactions`, `LspSymbolKind` → `OutlineSymbolTag`, `IntoSupported` removed) and outdated ActionKind member count (doc says 8, source has 15).
- `ActionShapeMeta`/`ActionSyntaxSlot`/`ActionSlotRole` are entirely undocumented in catalog-system.md.
- Phase B (Slice 8+) can proceed on source code; doc sync should be batched before Track 2 closes.
- One minor test gap: two explicitly named per-modifier tests for `ApplicableDeclarationSites` are missing (shape test exists but doesn't assert specific modifier values).
- Findings written to `.squad/decisions/inbox/frank-slice-1-7-audit.md`.

### 2026-05-10T13:53:14Z — t2-2 Slice A scope ruling executed
- Shane's directive is now durable: no deferrals inside the slice; Frank owns defer-vs-now scope calls, and typed operand roles plus `IntoSupported` removal were both approved for immediate inclusion in t2-2.
- George completed Slice A accordingly: `ActionShapeMeta`/`ActionSyntaxSlot` landed, roles are now typed via 1-based `ActionSlotRole`, `CollectionIntoBy` distinguishes `OrderingCapture`, and validation closed green at 4322 tests.
