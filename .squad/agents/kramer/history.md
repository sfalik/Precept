## Core Context

- Owns tooling surfaces: language server, VS Code extension, grammar sync, plugin wiring, and executable developer workflows.
- Keeps grammar, completions, semantic tokens, and tooling docs synchronized with the real DSL surface.
- Historical summary (pre-2026-04-13): handled grammar/completion passes for `when` guards, new scalar/choice types, stateless edit syntax, conditional expressions, and broader preview/tooling audits.
- Owns tooling surfaces: language server, VS Code extension, MCP server, plugin wiring, and developer workflow accuracy.
- Tooling docs must stay synchronized with actual commands, artifacts, sample counts, and installation paths.
- README/tooling polish should improve usability without introducing claims the extension or servers cannot support.

## Recent Updates

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

## Learnings

- Tooling trust depends on precise, runnable instructions and zero stale paths.
- Grammar/completion work is most reliable when specific patterns are ordered before generic catch-alls.
- Public tooling docs should improve usability without claiming behavior the extension or servers do not yet support.
- C93 code actions: extracting structured info (divisor name, field vs event-arg) from diagnostic messages via regex is reliable when the message format is stable. The `Divisor '{name}'` pattern carries enough to distinguish field refs from dotted event-arg refs and drive all three fix variants.
- For `when` guard insertion, splitting the transition row at the first `->` and checking for ` when ` in the prefix is the simplest reliable approach — no need to re-parse the row.

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
