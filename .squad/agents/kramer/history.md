## Core Context

- Owns tooling surfaces: language server, VS Code extension, grammar sync, plugin wiring, MCP ergonomics, and executable developer workflows.
- Keeps grammar, completions, semantic tokens, tests, and tooling docs synchronized with the actual DSL and server surface.
- Favors catalog-driven and semantic-model-driven editor behavior over LS-local keyword lists or parser-span guesses.

## Learnings

- **Kramer cleanup: only the hover V7 assertion was mine; 6 LS failures are older semantic-token/diagnostic debt (2026-05-13):** Re-ran `dotnet test test\Precept.LanguageServer.Tests\ --nologo` against the dirty tree and isolated the 7 failures. `Hover_OnDeclaredState_ReturnsIdentifierDoc` was my regression from the shipped V7 state-card format, and the fix was test-only: assert the compact state card badges (`⚠️ Gap · initial state`, `📍 Draft graph position`) instead of the pre-V7 `state \`Draft\`` text. The remaining 6 failures stayed unchanged after the hover fix and trace back to older semantic-token/diagnostic files, not the hover/completion work: semantic-token paths were last touched in commits like `d7556365`/`3c3681ea`, and the diagnostic projector/publish tests map to older diagnostic infrastructure commits like `568ab5cc`/`10de4133` rather than the current V7 or completion diffs.

- **`set FieldName ` completion: `InSetAssignment` context and `= ` completion (2026-05-13):** The previous fix used `SlotContext.AfterKeyword` (empty completion list) for the cursor position after `-> set FieldName `. That suppressed bogus top-level keywords correctly, but also suppressed the valid `=` operator completion. Refinement: renamed the returned context from `AfterKeyword` to the new narrower `SlotContext.InSetAssignment` (in `TryGetActionChainContext`, `SlotContext.cs`) — both the action-verb-identifier and `into`-identifier branches were updated. Added `InSetAssignment => CreateCompletionList(GetSetAssignmentItem())` to the context switch in `CompletionHandler.cs`. `GetSetAssignmentItem()` yields a single `CompletionItemKind.Operator` item with label and insertText `"= "` (trailing space lands the cursor ready to type the expression). Regression test `Completions_SetActionAfterFieldName_NoTopLevelKeywords` updated to also assert `labels.Should().Contain("= ")`. Build passed; targeted test passed; 10 pre-existing LS failures unchanged.

- **State card V7 spec gap (2026-05-12):** `CreateStateMarkdown` (line 1020 in `RichHoverFactory.cs`) produces 7 lines instead of the V7 spec's 3-line compact format. All data is assembled correctly — the problem is formatting only. Key divergences: (1) extra bold title line (line 1062), (2) extra `Modifiers:` line (line 1064), (3) `Incoming:` / `Outgoing:` on separate verbose lines instead of one `🔁 In: ... · Out: ...` line (lines 1065–1066), (4) `Writable here:` field list instead of `✏️ N fields (unconditional)` count on the summary line (line 1067), (5) terminal/ensures line missing `🧭`/`⚡` icons and `✏️` writable-count prefix (line 1068). B4 (`CreateStateGraphEdgeProofCard`) is correct. Fix is medium-scope: rewrite the `lines` builder in `CreateStateMarkdown` plus update 6+ assertions in `HoverHandlerTests.cs`. Same title-line pattern also exists in `CreateFieldMarkdown` (line 978) and `CreateEventMarkdown` (line 1093).

- `TypedConstantContext` remains the durable carrier for expected typed-constant slot context; declaration-site qualifier recovery must consult parsed qualifier metadata before enclosing-expression fallback.
- Semantic-token delta stability depends on exact identifier spans and cache invalidation of both `_documents` and `_latestResults` when token layouts change.
- Hover, definition, highlight, references, and rename all depend on the same precise semantic span contracts; container spans are only acceptable when the consumer explicitly wants them.
- Keyword semantic tokens should stay out of grammar-owned context-sensitive positions; the extension manifest and grammar must stay aligned on the fallback scopes they actually emit.
- Qualifier hover V3 should derive its status detail from resolved qualifier metadata: simple interpolated templates like `{StockingUnit.dimension}` should collapse to the owning symbol for `qualifier resolves from ...`, and reject rows must keep precedence over generic transition hover because both come from the same `TransitionRows` projection.
- Proof hover routing works best as a two-pass check in `HoverHandler.cs`: first ask `RichHoverFactory.TryCreateProofHover(...)` for proof diagnostics and proof-bearing expressions, then fall back to generic operator/type/accessor hover so PRE0114/PRE0116 explanation wins over catalog help.
- `tools/Precept.LanguageServer/Handlers/RichHoverFactory.cs` is now the compile-time proof-hover composition point: field proof summaries come from `Compilation.Proof.Obligations`, diagnostic cards join proof diagnostics back to `ProofObligation` via `FaultSiteLinks`, and expression cards resolve qualifier evidence from typed expressions rather than raw token text.
- For hover evidence, reuse typed spans plus qualifier metadata together: `FormatSnippet(...)` gives the authored expression text, while `ResolveDeclarationQualifier(...)` / `ResolveQualifierFromExpression(...)` recover resolved qualifier values, sources, and proof-chain fields without reaching for parser back-pointers.

- **State card V7 fix shipped (2026-05-13):** `CreateStateMarkdown` now matches the V7 compact card: status line first, a single `🔁 In ... · Out ...` edge summary, and a single `✏️ ... · 🧭 terminal ✓/✗ · ⚡ ... (⚠️)` summary line before the preserved B4 graph block. The change was rendering-only in `tools/Precept.LanguageServer/Handlers/RichHoverFactory.cs`; targeted state-hover regressions in `HoverHandlerTests.cs` were updated and the focused state-hover test slice passed, while the full LS suite still had the same 10 unrelated pre-existing failures already present on `spike/Precept-V2-Radical`.

- **Hover V7 closeout tracker + compact-card patterns (2026-05-13):** The final hover pass landed as a sequence of card-family rewrites in `RichHoverFactory.cs`, and the durable rule is now clear: V7 cards should prefer one badge-first line plus 1-2 evidence lines, not title blocks or prose sections. In practice that meant (1) field/event/rule/ensure/transition/reject/access/omit cards all needed bespoke compact line builders instead of the old generic `FormatStatus + details` stack, (2) proof fallbacks need their own 3-line summaries even when they are not qualifier-compatibility cases, and (3) omit declarations must route before access declarations because both spans can overlap on `in State omit Field`. The live tracker in `docs/Working/hover-design.md` is now the canonical progress ledger for future hover card alignment work.

## Historical Summary

- Early May through 2026-05-11 established the current tooling baseline: typed-constant completion and semantic-token fixes, delta-baseline guards, UCUM tier-1 completion curation, modifier-span precision, and catalog-driven hover/completion behavior.
- The hover/color audit cycle already confirmed no open Elaine-listed color implementation gaps; remaining risk is test depth and hover-surface parity rather than missing shipping behavior.

## Recent Updates

### 2026-05-12T22:25:28Z — Frank review approved B4’s shape but blocked the next placement step

- Frank’s review of B4 approved `EdgeProofStatus` as the right `StateGraph` projection and approved the shipped regression coverage, so the data shape itself is now durable context.
- The next fix is architectural, not hover-surface: move proof-status enrichment/domain logic out of `Compiler.cs` orchestration and avoid duplicating edge-expansion knowledge already owned by `GraphAnalyzer`.
- Elaine’s doc sync also landed, so `docs/Working/hover-design.md` now records the shipped B4 badge vocabulary and the fact that the proof narrative appends to rich state hover instead of introducing a standalone hover kind.

### 2026-05-12T07:12:56Z — Hover markdown line-break fix landed
- Changed all 11 `Create*Markdown` builders in `RichHoverFactory.cs` from `string.Join("\n", lines)` to `string.Join("\n\n", lines)` so VS Code renders paragraph breaks instead of a single run-on line.
- The change shipped in commit `af6e563c`; no hover assertions needed updates because tests already match by substring.
- Validation snapshot remained fully green: **5471/5471 tests passing**.

### 2026-05-12T07:12:56Z — Hover/color audit narrowed the remaining follow-up work
- Real hover gaps are now explicit: qualifier hover still hides the resolved-source meaning line, and field/state/event still use the generic symbol path instead of explicit construct entrypoints.
- No active Elaine-listed color gap remains in-tree; only Gap 1 field-vs-arg split evidence is partially unverified, and grammar-level regression depth could still improve.
- Recommended follow-up stays focused on extra hover regression coverage, qualifier resolved-source rendering, optional construct-parity refactors, and explicit field/arg plus grammar-scope tests.

### 2026-05-12T15:15:10Z — George shipped G1 compound-unit qualifier repair
- George fixed `ResolveQualifierFromInterpolatedConstant` in `ProofEngine.cs` so interpolated compound-unit constants resolve the full `{A}/{B}` qualifier string before the denominator fallback path.
- Commit `cb4fbf57` plus docs/history follow-up `1ee54bdb` cleared the RC1 PRE0114 and cascading DivisionByZero fallout in `samples/inventory-item.precept`, leaving only BUG-C / later proof work outside Kramers hover scope.

### 2026-05-12T17:45:51-04:00 — B1 compact proof-gap cards landed
- Reworked the proof diagnostic/expression builders in `tools/Precept.LanguageServer/Handlers/RichHoverFactory.cs` to emit the badge-first compact cards from `docs/Working/hover-design.md` instead of verbose `Status:` / `Reason:` blocks.
- Added compact evidence formatting so qualifier gaps now read inline (`Left ... has no known ... · right ... carries ...`), presence cards render the optional/access reason on one line, and proved expression cards collapse to clean 3-line summaries.
- Updated `test/Precept.LanguageServer.Tests/HoverHandlerTests.cs` for the new PRE0114 / PRE0116 / proved-expression card text, including direct formatter coverage for the presence card path; validation passed with `dotnet test test\\Precept.LanguageServer.Tests\\Precept.LanguageServer.Tests.csproj` and `dotnet test test\\Precept.Tests\\Precept.Tests.csproj`.

### 2026-05-12T18:01:17.648-04:00 — Hover B1 landed; B2/B3 follow-up active

- B1 compact proof-gap cards are shipped in `RichHoverFactory.cs`; hover proof diagnostics and proof-expression cards now use the badge-first compact format from `docs/Working/hover-design.md`, with updated `HoverHandlerTests` and green LS/core suites (272/272, 4938/4938).
- Frank’s follow-up ruling is now the active V1 contract for the next pass: fix construct routing before generic token help, keep guarded access out of mutability counts/state lists, and defer the state-card missing-path narrative.
- Kramer-2 is currently applying the B2/B3 routing + mutability honesty changes in `HoverHandler.cs` and `RichHoverFactory.cs`.

### 2026-05-12T19:26:05.9065969-04:00 — B2 construct routing + B3 mutability honesty landed

- `HoverHandler.cs` now routes state symbols to the rich state card before generic construct rows can steal them, and it evaluates rich construct hovers before generic operator/function/accessor fallbacks so rule/ensure/transition/reject/access/omit cards win where the spec requires.
- `RichHoverFactory.cs` now exposes shared rich symbol-card builders for field/state/event/arg paths and filters guarded access declarations out of V1 writable summaries, producing honest field mutability lines with `✏️` unconditional states and `🔒` locked-or-omitted states.
- `HoverHandlerTests.cs` now covers state-reference routing, reject-over-transition precedence, qualifier-over-symbol routing, guarded-access omission from mutability summaries, and the updated routing expectations; validation passed with `dotnet test test\Precept.LanguageServer.Tests\ --nologo` (271/271) and `dotnet build tools\Precept.LanguageServer\Precept.LanguageServer.csproj --artifacts-path temp/dev-language-server --nologo`.

### 2026-05-13T00:08:20Z — Hover B2/B3/B4 closeout is fully approved

- Commit `4a3abe77` shipped Kramer's initial B2/B3 pass, establishing rich-construct-first routing and honest `✏️` / `🔒` mutability summaries for hover cards.
- Commit `9617f39b` closed B4's remaining blockers by adding `HasObligations` for no-proof edges and the missing duplicate-proof regression coverage.
- Frank's final re-reviews approved the repaired B2/B3 and B4 work, leaving the hover program green at `279/279` language-server tests and `4973` core tests.

### 2026-05-13T04:22:01Z — State card V7 formatting pass is active

- Kramer-1 is still running on `RichHoverFactory.cs` `CreateStateMarkdown` to match the V7 compact state card: drop the title and modifiers lines, then merge transition and summary data onto the compact lines.
- Current scope is formatting-only plus matching `HoverHandlerTests.cs` assertion updates; no commit or validation result is recorded yet.
