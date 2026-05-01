## Core Context

- Owns language research, spec wording, and cross-surface documentation for Precept's DSL and architecture.
- Converts owner/design decisions into implementable language-surface guidance for parser, catalog, and tooling work.
- Historical summary: drove collection-surface research, parser/catalog design review cycles, vision-to-spec migration, and whitespace-insensitivity docs alignment.

## Learnings

- Named types are right when the choice changes storage or behavioral contract; modifiers are right when they narrow values without redefining the type.
- Collection docs work best per kind with shared cross-cutting sections because that mirrors how authors encounter the surface in `.precept` files.
- When the type system already disambiguates an operation, surface keywords should not restate the distinction.
- Philosophy and spec copy must say what the runtime actually guarantees; if they drift, flag the gap instead of silently rewriting either side.
- Durable research needs rationale, rejected alternatives, and concrete examples, not just a winning syntax.
- Whitespace-insensitivity is a language-identity rule, not a parser convenience; examples should prove that vertical layout is cosmetic.
- Qualifier docs must model real multi-qualifier types rather than simplified one-qualifier prose.
- Inline pending-decision callouts are better than silently adopting unsettled syntax in canonical docs.

## Recent Updates

### 2026-05-01 — GAP-1/2/3 spec analysis recorded
- Inbox analysis on typed constants, guarded `ensure`, and `is set` / `is not set` was merged into `.squad/decisions/decisions.md`.
- Durable recommendation captured: canonical guarded-ensure surface stays post-condition (`ensure Condition when Guard because ...`), while GAP-3 remains blocked on catalog-backed presence-operator design.

### 2026-05-01 — WSI docs sync recorded
- Updated `docs/language/precept-language-spec.md` (§0.1.5 and §1.4), `docs/compiler/parser.md`, `docs/working/parser-implementation-notes.md`, and `docs/language/collection-types.md` to align docs with trivia-free parsing and multi-line qualifier examples.
- Locked wording: line-oriented structure is keyword-anchored, not newline-delimited.
- Preserved multi-qualifier scalar examples and catalog-driven qualifier disambiguation guidance.

### 2026-04-29 — Collection surface research consolidated
- Authored and reconciled the collection-surface research that fed `docs/language/collection-types.md`, including quantifier direction, field-level rule rollout, and candidate-type evaluation.
- Durable rule: decide named-type vs modifier proposals by asking whether the change alters behavioral contract or only admissible values.

### 2026-04-29 — Lookup/queue surface simplification approved
- Approved `containskey` → `contains`, `removekey` → `remove`, and dequeue-capture `priority` → `by`.
- Key principle: when collection type already determines key/value role, surface vocabulary should stay shared and minimal.

### 2026-04-28 — Design/doc revision bar clarified
- Helped shift combined-design and parser-design docs toward decision-led, implementation-ready prose with explicit rationale and cross-surface sync.
