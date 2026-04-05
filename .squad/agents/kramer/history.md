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

### 2026-04-05 - README syntax highlighting analysis
- Investigated improving syntax highlighting for DSL code fences in README.
- Research confirmed GitHub Linguist does not support `precept` language identifier.
- Current approach (```precept fence) is already optimal: truthful, future-proof, follows DSL industry practice.
- Key learning: for custom DSLs, using the language name in code fences is standard practice even without Linguist support. Provides documentation value and future-proofs for potential Linguist addition. Alternative approaches (mislabeling as similar language, using no tag) provide no real improvement and introduce misleading claims.
- Decision documented in .squad/decisions/inbox/kramer-readme-syntax-highlighting.md
