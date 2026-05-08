## Core Context

- Owns tooling surfaces: language server, VS Code extension, grammar sync, plugin wiring, MCP ergonomics, and executable developer workflows.
- Keeps grammar, completions, semantic tokens, tests, and tooling docs synchronized with the actual DSL and server surface.
- The durable tooling rule: prefer precise, runnable guidance and avoid claims the extension, language server, or generators cannot currently support.

## Learnings

- Tooling trust depends on accurate status language: metadata readiness is not the same thing as implemented handlers.
- Grammar and completion changes are safest when specific patterns land before generic catch-alls and are backed by regression tests.
- Proof diagnostics become brittle when they depend on prose instead of structured metadata; keep publication paths but prefer durable data contracts.
- For custom DSL documentation, truthful code-fence labels and accurate path/build guidance matter more than cosmetic approximations.

## Recent Updates

### 2026-05-08T05:27:37Z — Grammar generator implementation durably recorded
- Scribe merged Kramer's PR #139 implementation note into `.squad/decisions.md`, capturing the 16 must-fix closures, 42 repository patterns, stale pattern and keyword removals, and the remaining function-argument message-string metadata block.
- Active follow-up stays narrow: the generator cannot gold-scope function-argument message strings until message-position metadata exists, so promotion to the canonical grammar remains gated.

### 2026-05-08T03:29:02Z — Wave 2 tooling closeout recorded
- `kramer-invest` confirmed the Precept Language Server test corpus still exists on `main`; the v2 branch is intentionally stubbed rather than accidentally regressed.
- All six Wave 2 design gates D1–D6 are now closed in `.squad/decisions.md`, and Kramer's active continuation work remains the grammar-generator scaffold plus LS test-port follow-through.

### 2026-05-08T03:08:18Z — Comprehensive tooling doc review recorded
- Kramer corrected `docs/tooling/extension.md`, `docs/tooling/language-server.md`, and `docs/compiler/tooling-surface.md` to match the branch's actual tooling state.
- Durable follow-ups now logged in `.squad/decisions.md`: clarify design-spec vs. implementation-status docs, recover the missing LS test corpus, and decide the grammar-generator ownership path.

### Historical summary through 2026-05-07
- Prior active work covered grammar and completion sync for guards, conditional expressions, `and` / `or` / `not`, stateless edit forms, semantic-token metadata propagation, README/tooling accuracy passes, and the C93 divisor-safety tooling fixes.
- The standing tooling baseline is unchanged: docs must reflect reality, tests are the safest spec anchor for language-server behavior, and future tooling derivation should come from catalog metadata rather than hand-maintained lists.
