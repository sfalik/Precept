## Core Context

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

### 2026-04-05 - Retired legacy proposal labels in sync workflow

- Added `needs-decision` and `decided` to `RETIRED_LABELS` in both the active workflow (`.github/workflows/sync-squad-labels.yml`) and the template copy (`.squad/templates/workflows/sync-squad-labels.yml`).
- Key learning: when a label retirement pass exists, always check it covers *all* superseded label families — the `go:*` cleanup was done, the proposal-state labels were missed. Template sync must always mirror the active workflow or they diverge silently.

- Investigated improving syntax highlighting for DSL code fences in README.
- Research confirmed GitHub Linguist does not support `precept` language identifier.
- Current approach (```precept fence) is already optimal: truthful, future-proof, follows DSL industry practice.
- Key learning: for custom DSLs, using the language name in code fences is standard practice even without Linguist support. Provides documentation value and future-proofs for potential Linguist addition. Alternative approaches (mislabeling as similar language, using no tag) provide no real improvement and introduce misleading claims.
- Decision documented in .squad/decisions/inbox/kramer-readme-syntax-highlighting.md
