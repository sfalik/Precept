## Core Context

- Owns tooling surfaces: language server, VS Code extension, grammar sync, plugin wiring, and executable developer workflows.
- Keeps grammar, completions, semantic tokens, and tooling docs synchronized with the real DSL surface.
- Historical summary (pre-2026-04-13): handled grammar/completion passes for `when` guards, new scalar/choice types, stateless edit syntax, conditional expressions, and broader preview/tooling audits.

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
