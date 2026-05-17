## Core Context

- Owns tooling surfaces: language server, VS Code extension, grammar sync, plugin wiring, MCP ergonomics, and executable developer workflows.
- Keeps grammar, completions, semantic tokens, tests, and tooling docs synchronized with the actual DSL and server surface.
- Favors catalog-driven and semantic-model-driven editor behavior over LS-local keyword lists or parser-span guesses.

## Learnings

- Event-modifier completion needs an explicit post-argument-list boundary guard so `event Name(Arg as type) ` stays on the event-modifier lane and offers `initial`.
- Dot-trigger event receivers must be checked before generic type-accessor routing; Ctrl+Space and trigger-character paths are separate surfaces and both need direct coverage.
- Trigger-character completions at token ends often need a one-column-back span check because LSP cursor positions sit just outside exclusive token spans.
- Slot metadata should drive the main routing path, but small fallback shims are still appropriate for pre-slot and malformed-document gaps.

## Historical Summary

- Early May through 2026-05-16 established the tooling baseline: hover-card compaction, grammar/completion routing hardening, typed-constant snippet infrastructure, and the catalog-driven slot-position resolver path.
- Detailed slice-by-slice chronology now lives in `.squad\decisions.md`; this file keeps the durable tooling posture plus the latest closeout.

## Recent Updates

### 2026-05-17T12:46:26Z — Initial modifier tooling follow-through recorded

- Commit `2373d8c7` aligned grammar generation and language-server behavior with declaration-level `initial` semantics, including regenerated TextMate output, completion routing after event arg lists, updated modifier hover text, and semantic-token stability.
- Validation held at 12 grammar tests and 404 language-server tests green.
- The constructor-semantics vertical slice is now closed across parser/checker, MCP, grammar generation, language-server UX, docs, and samples.

## Durable Posture

- Keep visible modifier coloring in the TextMate grammar unless a new semantic-token distinction is truly required; do not invent parallel lanes when the grammar already owns the rendered surface.
- Prefer narrow boundary checks and semantic-model routing to broad heuristic widening when repairing completion bugs.
