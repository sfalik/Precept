## Core Context

- Owns tooling surfaces: language server, VS Code extension, grammar sync, plugin wiring, and executable developer workflows.
- Keeps grammar, completions, semantic tokens, and tooling docs synchronized with the real DSL surface.
- Historical summary (pre-2026-04-13): handled grammar/completion passes for `when` guards, new scalar/choice types, stateless edit syntax, conditional expressions, and broader preview/tooling audits.

## Learnings

- Tooling trust depends on precise, runnable instructions and zero stale paths.
- Grammar/completion work is most reliable when specific patterns are ordered before generic catch-alls.
- Public tooling docs should improve usability without claiming behavior the extension or servers do not yet support.

## Recent Updates

### 2026-04-12 — Conditional expression tooling sync
- Added `if/then/else` grammar keywords and expression-context completions while preserving statement-level keyword discipline.

### 2026-04-11 — `when` guard completions + grammar verification
- Confirmed grammar support and added context-aware completions for declaration guards and guarded edit forms.
