## Core Context

- Owns tooling surfaces: language server, VS Code extension, MCP server, plugin wiring, and developer workflow accuracy.
- Tooling docs must stay synchronized with actual commands, artifacts, sample counts, and installation paths.
- README/tooling polish should improve usability without introducing claims the extension or servers cannot support.

## Recent Updates

### 2026-04-12 — Issue #9: Grammar + language server completions for conditional expressions (if/then/else)

- Grammar (`precept.tmLanguage.json`): added `if|then|else` to `controlKeywords` alternation alongside `when`. Updated regex from `\\bwhen\\b` to `\\b(when|if|then|else)\\b`.
- Completions (`PreceptAnalyzer.cs`): added `if ... then ... else` snippet to `ExpressionOperatorItems` — appears in all expression contexts (set RHS, guard, invariant, event assert, data expression). Excluded `if`, `then`, `else` from `TopLevelItems` (expression-only, not statement keywords). Excluded `then`, `else` from `KeywordItems` (continuation-only keywords).
- Semantic tokens: zero changes needed — `TokenCategory.Control → "preceptKeywordSemantic"` auto-picks up `If`, `Then`, `Else` from the enum. Verified by test.
- Tests: 4 new completion tests (set RHS, invariant, guard, statement-start exclusion) + 1 semantic token test. All 151 LS tests pass. All 9 grammar drift tests pass.
- Build: 0 errors.

### 2026-04-11 — Slices 6+7: Language server completions + grammar verification (conditional `when` guards)

- **Slice 7 (grammar verification):** Confirmed `when` is already in `controlKeywords` in `precept.tmLanguage.json`. All LanguageServer tests pass. Zero grammar changes needed.
- **Slice 6 (completions):** Added `WhenItem` static completion item in `PreceptAnalyzer.cs`. Updated 3 existing "completed expression" branches (invariant, event assert, state assert) to offer `[WhenItem, BecauseItem]` instead of just `[BecauseItem]`. Added 6 new guard-specific branches: `when` guard-complete → `[BecauseItem]` and `when` guard-in-progress → expression completions for all three assert forms. Added 3 Form 4 branches: `in State when <guard> edit` → field names, `in State when <guard>` (complete) → `[edit]`, `in State when` (in progress) → field completions. Updated `in StateName` action list to include `WhenItem` alongside assert/edit/→.
- Branch ordering verified: more-specific `when`-containing patterns placed before less-specific base patterns in all four form groups.
- Build: 0 errors. LanguageServer tests: all pass.

### 2026-04-11 — Slices 4+5: Language Server completions + grammar (integer/decimal/choice)

- Grammar (`precept.tmLanguage.json`): added `integer|decimal|choice` to `typeKeywords` alternation; added `maxplaces|ordered` to `constraintKeywords`; updated `fieldScalarDeclaration` regex capture group to include `integer|decimal` (choice falls through to generic patterns safely since its `(...)` args are already caught by the string literal pattern).
- Completions (`PreceptAnalyzer.cs`): extended `TypeItems` with `integer` (TypeParameter), `decimal` (TypeParameter), and `choice(...)` snippet. Added `DecimalConstraintItems` (nonneg, pos, min, max, maxplaces snippet) and `ChoiceConstraintItems` (ordered). Added per-type branches for `nullable`, `default VALUE`, and "already has constraints" zones for integer and decimal. Added `choice(...)` base-type branch returning `[NullableItem, ..ChoiceConstraintItems]`. Added `round(expr, N)` snippet to `ExpressionOperatorItems` (appears in all expression positions — set RHS and guard).
- Hover: verified automatic via `TokenDescription` attributes on `IntegerType`, `DecimalType`, `ChoiceType`, `Maxplaces`, `Ordered` — no handler changes needed.
- Pre-existing catalog drift failures: `CatalogDriftTests.AllConstructExamples_ParseSuccessfully` and `SampleFiles_CoverAllConstructs` were ALREADY failing before this slice due to a `round-function` construct entry with a bad example. Confirmed by stash + test run.
- Build: 0 errors. LanguageServer tests: 94/94 pass. Core tests: 743/745 (2 pre-existing catalog drift failures).
- Key learning: `TypeItems` uses `SnippetItem()` for `choice(...)` because the type has required argument syntax — this correctly presents as a snippet, not a bare keyword. Constraint completions for `integer` can reuse `NumberConstraintItems` directly (they share nonneg/pos/min/max). The `fieldScalarDeclaration` grammar pattern uses a simple `|` alternation — `choice(...)` cannot fit there due to `(...)` syntax, and the task explicitly says no special named pattern is needed for it.

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

### 2026-04-10 — Issue #10: String `.length` completions + grammar (tooling slice)

- Completions (`PreceptAnalyzer.cs`): added string member branch inside the existing `collectionMemberPrefixMatch.Success` block — same regex match, same pattern as collection branch. Checks `info.FieldTypeKinds` for `StaticValueKind.String` flag (covers non-nullable `String` and nullable `String | Null`). Added `BuildStringMemberItems(fieldName, isNullable)` returning one `.length` `Property` item with `Detail = "number"` and documentation that adjusts for nullable fields.
- Grammar (`precept.tmLanguage.json`): `.length` is NOT naturally caught by the identifier catch-all with the same scoping as `.count`. The `collectionMemberAccess` pattern already explicitly scopes these members as `variable.other.property.precept`. Added `length` to the alternation (`count|min|max|peek|length`) so `.length` gets the same token scope. Updated comment to "Dotted member accessors: collection (.count, .min, .max, .peek) and string (.length)."
- Build: 0 errors. Tests: 87/87 pass.
- Key learning: When a grammar already has a named pattern for specific dotted accessors (not relying on catch-all), new accessors must be added explicitly to that pattern — the catch-all produces a semantically different token scope.

## Learnings

### 2026-04-12 — Issue #9: Conditional expression tooling

- Expression-only control keywords (`if`, `then`, `else`) must be excluded from `TopLevelItems` even though they share `TokenCategory.Control` with statement-level keywords like `when`. The `BuildTopLevelItems()` method auto-includes all Control tokens — add explicit symbol-name exclusions.
- Continuation keywords (`then`, `else`) should also be excluded from `KeywordItems` — they're never meaningful standalone, only as part of `if ... then ... else`.
- `ExpressionOperatorItems` is the correct insertion point for expression-level keywords — it feeds into `BuildGuardCompletions`, `BuildExpressionCompletions`, `BuildDataExpressionCompletions`, and `BuildEventAssertCompletions` (all four expression contexts).
- Using a snippet (`if ${1:condition} then ${2:value} else ${3:value}`) for conditional expressions provides better UX than a bare keyword since the full form is always required.

### 2026-04-11 — Issue #14 final tooling spec (all 4 forms)

- Form 4 (`in State when guard edit`) has one unique intermediate step: "guard complete → suggest `edit`". Detected by `^\s*in\s+\w+\s+when\s+.+\s+$` + `EndsWithCompletedExpression`. This step fires ONLY when no `edit` is yet present on the line.
- **Critical ordering**: `in State edit` branch (step 4) must stay BEFORE the new "guard complete → EditItem" branch (step 3). Reason: `EndsWithCompletedExpression` matches `edit ` because `edit` matches `[A-Za-z0-9_]+\s+$`. Without the ordering guard, step 3 incorrectly fires for `in Draft when X edit `.
- Step 4 ("after edit → fields") is already handled by the existing `in State edit` branch with zero modification. Its broad regex (`^\s*in\s+[^\n]*\s+edit\s+[^\n]*$`) already matches Form 4 lines because `[^\n]*` consumes `when guard` in the middle.
- `WhenItem` static does not yet exist. Must be added alongside `BecauseItem` before Forms 1–3 modifications can compile.
- Scope differentiation for guards is free: invariant/state-assert guards reuse `BuildDataExpressionCompletions`; event-assert guards reuse `BuildEventAssertCompletions`. Both already embed the correct scope. No new helpers.
- Grammar: zero changes for all 4 forms. `when` catch-all covers all positions. `rootEditDeclaration` is anchored to `edit` at line start — no conflict with `in State when guard edit`.
- Final branch count: 7 new branches + 4 mods + 1 static + 0 grammar = ~33–40 lines total across `PreceptAnalyzer.cs`.
- Findings filed: `.squad/decisions/inbox/kramer-issue14-final-tooling.md`

### 2026-04-11 — Issue #14 tooling feasibility: `when <guard>` on declaration forms

- Grammar: `when` is already a global catch-all in `controlKeywords` (`\bwhen\b` → `keyword.control.precept`). Zero grammar changes needed for all four declaration forms (invariant, state assert, event assert, in-state edit).
- `rootEditDeclaration` is anchored to `edit` at line start — does NOT conflict with `in State when guard edit` (which starts with `in`). Safe.
- Completions: all four declaration contexts are already detected in `PreceptAnalyzer.cs`. The work is ~14 targeted branches: modifying 4 `[BecauseItem]` returns to `[WhenItem, BecauseItem]`, adding 2 branches per declaration for guard-expression and guard-completed states.
- Scope differentiation (data fields vs event args for guards) is already handled: `BuildDataExpressionCompletions` covers invariant/state-assert/edit guards; `BuildEventAssertCompletions` covers event-assert guards. Zero new helpers needed.
- `in State when guard edit` is the unique structural form (guard precedes action keyword). Requires a new intermediate "guard completed → offer `edit`" branch — the only novel pattern in the whole feature.
- `when not` is zero additional work — `not` already in `ExpressionOperatorItems` since #31 slice.
- Semantic tokens and hover: zero changes. `when` is already in `PreceptToken` enum and auto-discovered by `BuildSemanticTypeMap()`.
- Verdict: Medium effort. Grammar is free; completions are ~14 mechanical branches using existing infrastructure.

- GitHub README rendering gives reliable control over image assets, not over text-inside-image scaling relative to surrounding prose. If size parity with nearby copy matters, real Markdown text or fenced code is the only robust answer.
- For image-based README treatments, external SVG rendered through `<img>` with an explicit width is the strongest compromise; PNG plus `<img width>` can be tuned, but it remains more fragile across mobile, zoom, and density changes.
- GitHub officially supports `<picture>` for light/dark asset swaps, but viewport-specific mobile/desktop swapping and custom CSS/media-query tricks are not a dependable README strategy.
- Live GitHub repo pages clamp the overall content frame before ultra-wide browsers run out of space: the repo page tops out around a 1280px shell, and the rendered README/article column tops out around 1012px. For README hero images, optimize the meaningful artwork for roughly 880-920 displayed pixels and spend any extra width on whitespace instead of extra content columns.

### 2026-04-10 — Issue #13: Field-level constraints — tooling slice

- Grammar (precept.tmLanguage.json): added constraintKeywords repository entry — all 9 keywords (
onnegative, positive, 
otempty, min, max, minlength, maxlength, mincount, maxcount) scoped as keyword.other.precept. Inserted into top-level patterns between ctionKeywords and outcomeKeywords (before identifierReference catch-all). Also included in ieldScalarDeclaration capture 9 and ventWithArgsDeclaration capture 8 so constraints on those lines get keyword scope rather than identifier catch-all.
- Conflict check: min/max in collectionMemberAccess use dotted form (identifier.min) — no conflict with standalone min/max constraint keywords.
- Completions (PreceptAnalyzer.cs): replaced the single (?:string|number|boolean) scalar field patterns with type-specific branches for 
umber, string, and oolean. Returned type-appropriate constraint items (NumberConstraintItems, StringConstraintItems, CollectionConstraintItems) at each field/event-arg declaration zone: after type, after nullable, after default, after existing constraints, and after collection of TYPE. Static arrays carry Detail and Documentation.
- Tests: updated 2 existing count-sensitive tests; added 7 new constraint zone tests. 94/94 pass.
- Decision inbox: .squad/decisions/inbox/kramer-issue13-tooling.md
- Key learning: when splitting a combined type-alternative regex into type-specific branches for richer responses, ensure all three types are covered and that more-specific patterns (with constraints already present) appear BEFORE the base 	ype  patterns in method ordering.
