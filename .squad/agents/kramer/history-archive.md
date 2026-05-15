# Kramer History Archive



Archived updates moved from `history.md` during Scribe summarization.



---



## Archive Batch — 2026-05-08T03:29:02Z



---



### 2026-05-08T03:08:18Z — Comprehensive tooling doc review recorded

- Kramer corrected `docs/tooling/extension.md`, `docs/tooling/language-server.md`, and `docs/compiler/tooling-surface.md` to match the branch's actual tooling state.

- Durable follow-ups now logged in `.squad/decisions.md`: clarify design-spec vs. implementation-status docs, recover the missing LS test corpus, and decide the grammar-generator ownership path.



### 2026-04-05 - Comprehensive tooling knowledge refresh

- Consolidated the current toolchain, build/test commands, and major extension/MCP/plugin responsibilities.

- Key learning: the fastest tooling documentation win is precise, executable instructions with no stale paths.



### 2026-04-05 - README badge cleanup and sample count fix

- Tightened badge/presentation details while correcting surfaced counts and tooling-adjacent metadata.

- Key learning: small public inconsistencies erode confidence in larger tooling claims.



### 2026-04-05 - Inspector/Preview panel audit for PRD

- Audited `inspector-preview.html` (3,464 lines), `extension.ts`, mockup, archived spec, and brand review.

- Key finding: implementation is far ahead of what `inspector-panel-review.md` describes. The review predates edit mode, rule violation banners, state-rules indicator, field icons, and null toggle.

- Color system mismatch (review's Priority 1) is still 100% unaddressed — all 7 color tokens remain on the custom palette, not the brand system.

- The mockup's round pill event buttons became skewed parallelograms in the live implementation — deliberate design evolution.

- Header current-state label was present in mockup, removed in implementation (state shown only in SVG diagram).

- Key file paths: `tools/Precept.VsCode/webview/inspector-preview.html` (source of truth), `tools/Precept.VsCode/src/extension.ts` (host/protocol).

- Decision inbox: `.squad/decisions/inbox/kramer-preview-audit.md`



### 2026-04-08 - Slice 5 — Grammar, completions, semantic tokens (issue-22 data-only precepts)



- Grammar (`precept.tmLanguage.json`): added `all` to `controlKeywords` alternation (sibling of `any`).

- Grammar: added `rootEditDeclaration` repository pattern — matches `edit all` and `edit Field1, Field2` at line start; highlights `edit` as `keyword.other`, `all` as `keyword.control`, fields as `variable.other.field`; inserted before `controlKeywords` catch-all for correct priority.

- Completions (`PreceptAnalyzer.cs`): new root-level `edit` branch suggests `all` + field names (stateless precept context).

- Completions: updated `in State edit` branch to also suggest `all` (supports `in any edit all`).

- Semantic tokens: no changes — `PreceptToken.All` auto-picked up via `[TokenCategory(Grammar)]` from Slice 1.

- Both builds green: LS 0 errors, npm compile clean.

- Key learning: always check `node_modules` before running `npm run compile` — directory may not exist on a fresh checkout.



### 2026-04-05 - Retired legacy proposal labels in sync workflow



- Added `needs-decision` and `decided` to `RETIRED_LABELS` in both the active workflow (`.github/workflows/sync-squad-labels.yml`) and the template copy (`.squad/templates/workflows/sync-squad-labels.yml`).

- Key learning: when a label retirement pass exists, always check it covers *all* superseded label families — the `go:*` cleanup was done, the proposal-state labels were missed. Template sync must always mirror the active workflow or they diverge silently.



- Investigated improving syntax highlighting for DSL code fences in README.

- Research confirmed GitHub Linguist does not support `precept` language identifier.

- Current approach (```precept fence) is already optimal: truthful, future-proof, follows DSL industry practice.

- Key learning: for custom DSLs, using the language name in code fences is standard practice even without Linguist support. Provides documentation value and future-proofs for potential Linguist addition. Alternative approaches (mislabeling as similar language, using no tag) provide no real improvement and introduce misleading claims.

- Decision documented in .squad/decisions/inbox/kramer-readme-syntax-highlighting.md



### 2026-05-18 - GitHub README width contract clarified

- Split README sizing research into two separate ceilings: the broader repo/article layout (`~1280px` shell, `~1012px` article) and the actual repo-view README image display cap (`~830px`) that governs the DSL hero asset.

- Recorded the reusable audit workflow at `.squad/skills/github-readme-width-audit/SKILL.md` and preserved the merged sizing outcome in `.squad/decisions.md`.

- Key learning: for README hero images, composition guidance and final image-display limits are different measurements; size the shipped asset to the image cap, not the wider article container.



### 2026-04-10 — Issue #31 shipped

- PR #50 merged to main (squash SHA `305ec03`). Issue #31 closed. 775 tests passing.



### 2026-04-10 - Slice 5: Grammar + Language Server (issue #31 — and/or/not keywords)



- Grammar (`precept.tmLanguage.json`): added `and`, `or`, `not` to `actionKeywords` alternation (same group as `contains`) — these are operator-category tokens used in expression positions, so they fit naturally alongside `contains`.

- Grammar: removed the `keyword.operator.logical.precept` block (`&&|\\|\\||!`) from `operators` entirely; `!=` lives in the comparison block and was untouched.

- Completions (`PreceptAnalyzer.cs`): replaced `&&`, `||`, `!` `Operator` items with `and`, `or`, `not` `Keyword` items in `ExpressionOperatorItems` — the static list consumed by `BuildGuardCompletions`, `BuildExpressionCompletions`, and `BuildDataExpressionCompletions`.

- Global `KeywordItems` required no change — `BuildKeywordItems()` auto-discovers `And`/`Or`/`Not` from `PreceptToken` enum via `[TokenCategory(Operator)]` + alphabetic symbol filter.

- Semantic tokens: verified `BuildSemanticTypeMap()` iterates all enum values; `TokenCategory.Operator → "preceptKeywordGrammar"` covers `And`/`Or`/`Not` automatically. Zero handler changes.

- Build: 0 errors. Tests: 87/87 pass.

- Commit: `8f3bdab` — "feat(#31): grammar and language server — and/or/not keywords (slice 5)"



### 2026-05-07 — Comprehensive tooling doc review



- Audited `docs/tooling/extension.md`, `docs/tooling/language-server.md`, and `docs/compiler/tooling-surface.md` against actual implementation (`extension.ts`, `Program.cs`, test directories).

- Key finding: `extension.md` was the most stale — stated "no custom commands or webview implemented" when all three commands, LS client lifecycle, status bar, and preview scaffold are fully implemented.

- Key finding: `tooling-surface.md` conflated catalog metadata readiness (TokenMeta fields populated) with LS handler implementation. "Semantic tokens Pass 1 implemented" was wrong — no LS handlers exist.

- Key finding: `language-server.md` §2 used present tense to describe features the bootstrap-only LS doesn't provide. Status table was accurate; the body contradicted it.

- Resolved two stale Open Questions in tooling-surface.md (SlotContext/ConstructSlotKind naming, SemanticIndex reference arrays) using resolutions already recorded in language-server.md.

- Flagged three design issues: dual-purpose doc pattern (status vs. design spec), empty LS test directory, and the unbuilt grammar generator drifting from its "One Atom Test" promise.

- Commit: `2ed7628`



## Learnings



- Tooling docs drift fastest at status tables — the body can be aspirational design spec (acceptable) but the status row must reflect current branch state.

- "Implemented" means handler code exists and runs, not "the metadata fields that would power this handler are ready." Catalog metadata readiness ≠ feature implementation.

- When a doc has both a status table and a present-tense body, a developer reading only the body gets a false picture. Add callout boxes or section markers to distinguish design spec from live behavior.

- The extension is further ahead than its doc claimed. Always diff `extension.ts` against `extension.md` before writing off the extension as "not yet done."



- Tooling trust depends on precise, runnable instructions and zero stale paths.

- Grammar/completion work is most reliable when specific patterns are ordered before generic catch-alls.

- Public tooling docs should improve usability without claiming behavior the extension or servers do not yet support.

- C93 code actions: extracting structured info (divisor name, field vs event-arg) from diagnostic messages via regex is reliable when the message format is stable. The `Divisor '{name}'` pattern carries enough to distinguish field refs from dotted event-arg refs and drive all three fix variants.

- For `when` guard insertion, splitting the transition row at the first `->` and checking for ` when ` in the prefix is the simplest reliable approach — no need to re-parse the row.

- Tooling trust collapses faster from false-positive-heavy proof diagnostics than from selective under-approximation; if Principle #8 tightens, proof-gap diagnostics need distinct wording and actionable fixes instead of generic red squiggles.

- Proof diagnostics already have a working Problems-panel publication path through `PreceptAnalyzer` and `PreceptTextDocumentSyncHandler`; the real tooling risk is missing structured proof metadata, because hover and quick fixes both become brittle as soon as diagnostic prose shifts to truth-based/natural-language rendering.



## Recent Updates



### 2026-04-17 — C93 divisor safety code actions (Slice 7 of #106)

- Added three quick-fix code actions for C93 unproven-divisor warnings:

  1. "Add `positive` constraint" — inserts `positive` after the type keyword in field or event-arg declarations.

  2. "Add `ensure > 0`" — inserts an event ensure line (event-arg divisors only).

  3. "Add `when != 0` guard" — prepends or appends to the transition row's guard clause.

- 4 new tests covering field-positive, arg-positive, arg-ensure, and guard-append scenarios.

- All 173 LS tests + 1290 core tests pass.



### 2026-04-12 — Conditional expression tooling sync

- Added `if/then/else` grammar keywords and expression-context completions while preserving statement-level keyword discipline.



### 2026-04-11 — `when` guard completions + grammar verification

- Confirmed grammar support and added context-aware completions for declaration guards and guarded edit forms.



### 2026-04-19 — Diagnostic range mapping depends on upstream span fidelity

- When editor ranges look wrong, first inspect the upstream diagnostic payload; a language-server mapping bug and a coarse source span can present the same symptom.

- Honor `EndColumn` when it is present and reserve full-line fallback for diagnostics that are genuinely line-scoped, otherwise tooling precision regresses silently.

- Focused LS span tests should pin both the precise-range path and the line-level fallback path so later runtime precision work does not get flattened in the editor.



### 2026-04-25 — M7 + M8: TextMateScope and SemanticTokenType on TokenMeta



- Added `TextMateScope: string?` and `SemanticTokenType: string?` fields to `TokenMeta` in `src/Precept/Language/Token.cs`.

- Populated all ~90 token entries in `src/Precept/Language/Tokens.cs` with their scope and semantic type values using named arguments.

- Scope strategy: Declaration/Preposition/Control → `keyword.*.precept`; StateModifiers → `storage.modifier.state.precept`; Types (all ~21 type tokens + Set dual) → `storage.type.precept`; Constraints/min/max → `keyword.other.constraint.precept`; Operators → `keyword.operator.precept`; Arrow → `keyword.operator.arrow.precept`; Punctuation → `punctuation.precept`; Booleans → `constant.language.boolean.precept`; NumberLiteral → `constant.numeric.precept`; Strings → `string.quoted.double.precept`; TypedConstants → `string.quoted.single.precept`; Identifiers → `entity.name.precept`; Comments → `comment.line.precept`; NewLine/EndOfSource → null.

- LSP semantic type strategy: types → `"type"`, state modifiers → `"modifier"`, constraints → `"decorator"`, operators/logical/punct → `"operator"`, everything keyword-shaped → `"keyword"`, literals → `"number"`/`"string"`, identifiers → `"variable"`, comments → `"comment"`, structural null-tokens → null.

- Build: 0 errors, 0 warnings.



### 2026-04-25 — Catalog metadata tooling impact review

- Audited `PreceptAnalyzer.cs` (completions), `precept.tmLanguage.json` (grammar), `PreceptSemanticTokensHandler.cs` (semantic tokens), and `PreceptDocumentIntellisense.cs` (hover) against the 10-catalog system design in `docs.next/catalog-system.md`.

- Found 14 hardcoded completion lists, 12 hand-maintained grammar alternations, and 1 hardcoded function hover dictionary — all replaceable by catalog metadata as catalogs land.

- Semantic tokens already ~90% catalog-driven via `BuildSemanticTypeMap()`. Hover ~80% catalog-driven (function hover is the gap).

- Recommended drift tests over grammar auto-generation; incremental per-catalog migration over big-bang rewrite; keeping regex-based context detection until Construct Slot Model matures.

- Decision inbox: `.squad/decisions/inbox/kramer-catalog-metadata-tooling-review.md`



### 2026-05-02T21:58:21Z — Canonical type checker batch closed

- Kramer's tooling review is now durably linked to the canonical checker plan: no tooling-level blockers, 2 medium LS-derivable gaps, and 7 recommendations remain the consumer-facing follow-through list.

- Frank accepted the placeholder-only TypedEditDeclaration direction and rejected mirroring ResolvedArgs on TypedTransitionRow as anti-mirroring, preserving the rule that tooling should derive from the canonical typed model rather than cached duplicate payloads.

- The checker design is now implementation-ready; future LS/hover/completion work should project from the typed model and catalog metadata once the real checker slices land.



---



## Archive Batch — 2026-05-10T12:15:36Z



---



### 2026-05-10T05:25:00Z — Slice 20 shared symbol navigation landed

- `tools/Precept.LanguageServer/SymbolNavigation.cs` now centralizes occurrence lookup plus reference-span projection for fields, states, events, and event args, and `DefinitionHandler`, `ReferencesHandler`, and `DocumentHighlightHandler` all route through it.

- Slice 20 also locked the capability surface by registering references + document-highlight handlers in `LanguageServerComposition` and updating `ServerCapabilityTests` to expect both providers.

- Test-fixture lesson: `TypedArg.Span` currently covers the enclosing event-arg declaration region rather than just the arg name, so qualified-reference selection in LS tests must key off site width vs. `Arg.Name.Length`, not vs. `Arg.Span.Length`.



### 2026-05-10T04:36:29Z — Slice 13 slot-context routing closed

- `SlotContextResolver` now covers action-chain verb/target/expression positions, guard/compute/ensure/rule expressions, event-arg defaults, field default values, and `of` inner-type positions in `tools/Precept.LanguageServer/SlotContext.cs`.

- Durable implementation rule: action-chain expression routing cannot trust `ActionChainSlot.Span` alone because `enqueue ... by ...` and `remove ... at ...` separators can fall outside the parsed span; token-aware tail routing is required.

- Validation stayed green: `dotnet test test\Precept.LanguageServer.Tests\Precept.LanguageServer.Tests.csproj --no-restore` passed at 88/88.



### 2026-05-10T04:35:22Z — Slice 13 slot-context coverage landed



- `SlotContextResolver` now routes action-chain verbs/targets/expressions, expression-bearing construct slots, event-arg defaults, field default values, and `of`-introduced inner collection types into the promised `SlotContext` values.



- Added `SlotContextResolverTests` with cursor-marker fixtures covering arrow/verb/into/separator action positions plus guard/compute/ensure/rule/default/type regressions; `dotnet test test\Precept.LanguageServer.Tests\Precept.LanguageServer.Tests.csproj --no-restore` passed at 88/88.



### 2026-05-10T04:20:44Z — Slice 11 final wiring recorded



- `Program.cs` now registers the full Phase 1 handler surface over shared `DocumentStore` and publishes semantic-token color rules at startup through `SemanticTokensHandler.SendColorNotification(server)`.



- Durable boundaries from the slice are now locked: capabilities stay registration-driven, tests keep the delegate color-notification seam, `CompletionHandler` must read `ValueModifierMeta`, and `LspTestHost` remains intentionally partial until Slice 29.



### 2026-05-10T03:13:51Z — Bug register operationalized for Track 2



- Added the status tracker table to `docs/Working/precept-toolchain-bugs.md`, giving the 52 confirmed bugs durable status/assignee/notes columns for implementation triage.



- Frank's stage analysis and Soup-Nazi's test-strategy verdict are now merged into the canonical decision ledger, so the tracker can point follow-up slices at the highest-value parser/MCP/type-checker gaps and the matching regression layers.



### 2026-05-10T02:50:04Z — Team update: visual taxonomy and LS prerequisites locked



- 📌 Team update (2026-05-10T02:50:04Z): `SemanticTokenTypes` is now the approved 14th catalog, `TokenMeta` keeps one `VisualCategory`, and token-surface projections (custom type, TextMate scope, base styles, constrained-modifier support) now belong to catalog metadata instead of parallel token fields or manifest copies — decided by Frank and Shane Falik.



- 📌 Team update (2026-05-10T02:50:04Z): George's outline metadata plus snippet-template catalog work are now durably recorded as the completion/outline prerequisite surface that your LS slices consume on top of Slice 0's protocol foundation — decided by George and captured by Scribe.



### 2026-05-10T01:55:00Z — Slice 12 committed: trigger characters + dual-use `set` type context



- Commit `d962d0cb` fixed `CompletionHandler` trigger characters to `[" ", "'", ".", ">", "~"]`, added `SlotContextResolver.IsSetInTypePosition`, and wired the dual-use `set` reclassification into hover and semantic-token projection.



- Durable lessons from the slice stay locked: token-stream lookups should use `StartLine` + `StartColumn` rather than `SourceSpan.Offset`, and type-position `set` must project the type semantic token path rather than the action keyword path.



### 2026-05-10T00:41:09Z — Slices 1/2/4/5/9 handler batch recorded



- The LS now has concrete diagnostics, semantic-token, completion, hover, and folding handlers, with the matching tests that locked the publication, projection, and projection-only semantic contracts.



- Validation stayed green except for the pre-existing `SemanticTokensHandler.CreateRegistrationOptions` access-modifier mismatch in the shared LS baseline.



### 2026-05-10T00:23:31Z — Slice 0b legacy LS cleanup committed



- Commit `51d93dc2` deleted the stub layer (`LanguageServerStubs.cs`, `PreceptPreviewProtocol.cs`, `LegacyHandlerCompat.cs`) and removed 173 legacy shim-facing tests so the LS project keeps only the real harness surface.



- The cleanup gate remained `dotnet build` for the LS projects plus `dotnet test test/Precept.Tests/`, all green in the validated slice run.



### 2026-05-10T03:39:49Z — Slice 10-color follow-up spec recorded



- Slice 10-color spec authored and inserted into `docs/Working/language-server-implementation-plan.md`.



- Approach: custom LSP notification `precept/semanticTokenColors` → extension.ts handler → `editor.semanticTokenColorCustomizations` workspace config API.



### 2026-05-10T04:33:18Z — Shared LS composition follow-up closed and Track 1 stays exclusive

- The Slice 11 no-deferral audit closed the Phase 1 mirroring gap: `Program.cs` and `LspTestHost` now share `LanguageServerComposition.ConfigurePreceptLanguageServer(...)`, and `ServerCapabilityTests` lock the shipped Phase 1 capability surface.

- The old "leave host mirroring to Slice 29" note is no longer valid; Slice 29 is back to future Phase 2 protocol growth only.

- Shane's latest directive pauses Track 2 execution, so the active tooling lane remains Track 1 and the next ready slice is still 13 unless focus changes again.



### 2026-05-10T05:11:00Z — Slice 14 completion surface landed



- Expression completions stay catalog-driven when member access is incomplete by resolving the receiver type from typed-expression spans and token adjacency around `.` instead of LS-local accessor lists.

- Current event-scope completion is safest when it keys off semantic constructs (`TypedEvent`, `TypedTransitionRow`, `TypedEventHandler`, event-anchored `TypedEnsure`) rather than hardcoded keyword heuristics, so arg suggestions survive across declaration, transition, and handler contexts.



## Archive Batch — 2026-05-10T12:15:36Z



---



### 2026-05-10T06:33:00Z — Slice 26 document version ordering landed

- `DocumentState` now keeps a versioned snapshot (`Current`, `Suggestions`, `Version`) and only accepts `TryUpdate(...)` calls whose version is strictly newer than the stored one.

- `TextDocumentSyncHandler` now threads document versions through open/change recompiles, suppresses stale diagnostic publishes for rejected versions, and still falls back to unversioned `Update(...)` behavior if a client omits version data.

- Validation stayed green: targeted Slice 26 coverage passed 7/7, then `dotnet test test\Precept.LanguageServer.Tests\Precept.LanguageServer.Tests.csproj --no-restore --nologo` passed 115/115.



### 2026-05-10T06:20:00Z — Slice 24 workspace symbols landed

- Added `WorkspaceSymbolHandler` plus `DocumentStore.Snapshot()` / `EnumerateOpenDocuments()` so `workspace/symbol` can search all open `.precept` documents without duplicating outline rules.

- `OutlineSymbolProjector` now exposes a shared projection shape consumed by both document symbols and workspace symbols, and `LanguageServerComposition` advertises the workspace-symbol capability.

- Validation stayed green: targeted workspace-symbol coverage passed, then `dotnet test test\Precept.LanguageServer.Tests\Precept.LanguageServer.Tests.csproj --no-restore --nologo` passed 112/112.



### 2026-05-10T06:00:00Z — Slice 23 document-symbol selection ranges landed

- Added `tools/Precept.LanguageServer/OutlineSymbolProjector.cs` so document-outline projection stays reusable for Slice 24 while routing selection ranges to semantic declaration spans instead of full construct spans.

- `DocumentSymbolHandler` now delegates to the projector, and `dotnet test test\Precept.LanguageServer.Tests\Precept.LanguageServer.Tests.csproj --no-restore --logger "console;verbosity=minimal"` passed 110/110 with the new selection-range coverage green.



### 2026-05-10T05:55:00Z — Slice 21 rename support landed

- `tools/Precept.LanguageServer/Handlers/RenameHandler.cs` now registers prepare-rename plus rename over `SymbolNavigation`, and `LanguageServerComposition` advertises the rename capability through the shared host path.

- Durable rename rule: event-arg declarations and qualified arg references must normalize to identifier-only spans before emitting `WorkspaceEdit` text edits; the shared occurrence lookup can stay broad for references/highlights, but rename must trim arg spans so `JoinWaitlist.PartyName` rewrites only `PartyName`.

- Slice 21 validation: targeted rename/navigation capability coverage (`RenameHandlerTests`, `ServerCapabilityTests`, definition/references/highlights) passed 18/18 via `dotnet test test\Precept.LanguageServer.Tests\Precept.LanguageServer.Tests.csproj --no-restore --filter "FullyQualifiedName~RenameHandlerTests|FullyQualifiedName~ServerCapabilityTests|FullyQualifiedName~ReferencesHandlerTests|FullyQualifiedName~DocumentHighlightHandlerTests|FullyQualifiedName~DefinitionHandlerTests"`.

---

## Archive Batch — 2026-05-15T22:18:38Z

### 2026-05-15 — Earlier hover and cleanup detail summarized out of history.md

- Archived the older hover-specific and cleanup updates (line-break fix, B1/B2/B3/B4 closeout, state-card V7 pass, and the 2026-05-13 cleanup note).
- The live file now keeps only the durable completion-routing guidance and the latest completion-provider closeout.
