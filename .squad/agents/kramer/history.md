## Core Context

- Owns tooling surfaces: language server, VS Code extension, grammar sync, plugin wiring, MCP ergonomics, and executable developer workflows.
- Keeps grammar, completions, semantic tokens, tests, and tooling docs synchronized with the actual DSL and server surface.
- Favors catalog-driven and semantic-model-driven editor behavior over LS-local keyword lists or parser-span guesses.

## Learnings

- **Completion pass 2 closeout (2026-05-15T18:18:38.540-04:00):** the remaining completion gaps were routing problems, not missing catalogs. Use narrow slot lanes (`AfterStateTarget`, `AfterEventTarget`, `AfterNo`) and derive state-target verbs, `->` outcomes, and expression keywords from catalog metadata; gate `any` and `all` by target family.
- **Completion audit + routing hardening (2026-05-15T18:04:26.860-04:00):** keep raw document text available to completion so top-level constructs can be limited to whitespace-only line starts; route `transition`, valued modifiers, and event-arg modifier domains through the correct contexts.
- **Completion test pattern reminder (2026-05-15T18:04:26.860-04:00):** use a `¦` cursor marker and assert both wanted labels and forbidden noise. Negative assertions catch fallback-routing leaks.
- **Set-assignment operator context (2026-05-13):** `set FieldName ` is its own `SlotContext.InSetAssignment`; completion should offer only `= ` there, never top-level constructs.
- **Action-chain continuation arrow (2026-05-15T18:47:10.829-04:00):** when a fresh `->` line sits outside the parsed construct span, recover `InActionVerb` with a short backward scan over significant tokens; require both a prior action verb and a prior same-or-deeper-indented `->` so continuation recovery stays precise.

## Historical Summary

- Early May through 2026-05-15 established the tooling baseline: hover-card compaction, grammar/completion routing, semantic-token precision, and catalog-driven language-server behavior.
- Older hover-specific batch detail now lives in `history-archive.md` and `.squad/decisions.md`; this file stays focused on the durable completion contract and current guidance.

## Recent Updates

### 2026-05-15T22:18:38Z — Completion provider audit fully closed

- Pass 1 and Pass 2 closed the top-level keyword leak and the six spec-driven completion gaps: state/event-target continuations, `any`/`all` gating, `no -> transition`, `->` outcome keywords, and expanded expression operator/keyword vocabulary.
- All fixes remained catalog-sourced through `Constructs`, `Modifiers`, `Actions`, `Operators`, `Outcomes`, `ExpressionForms`, and token metadata rather than language-server-local keyword arrays.
- Validation endpoint: `dotnet test test\Precept.LanguageServer.Tests\ --nologo` → 319/319 passing.
