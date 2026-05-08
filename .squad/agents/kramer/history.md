## Core Context

- Owns tooling surfaces: language server, VS Code extension, grammar sync, plugin wiring, MCP ergonomics, and executable developer workflows.
- Keeps grammar, completions, semantic tokens, tests, and tooling docs synchronized with the actual DSL and server surface.
- The durable tooling rule: prefer precise, runnable guidance and avoid claims the extension, language server, or generators cannot currently support.

## Learnings

- Tooling trust depends on accurate status language: metadata readiness is not the same thing as implemented handlers.
- Grammar/completion changes are safest when specific patterns land before generic catch-alls and are backed by regression tests.
- Proof diagnostics become brittle when they depend on prose instead of structured metadata; keep publication paths but prefer durable data contracts.
- For custom DSL documentation, truthful code-fence labels and accurate path/build guidance matter more than cosmetic approximations.

## Recent Updates

### 2026-05-08T03:29:02Z — Wave 2 tooling closeout recorded
- `kramer-invest` confirmed the Precept Language Server test corpus still exists on `main`; the v2 branch is intentionally stubbed rather than accidentally regressed.
- All six Wave 2 design gates D1–D6 are now closed in `.squad/decisions.md`, and Kramer's active continuation work remains the grammar-generator scaffold plus LS test-port follow-through.

### 2026-05-08T03:08:18Z — Comprehensive tooling doc review recorded
- Kramer corrected `docs/tooling/extension.md`, `docs/tooling/language-server.md`, and `docs/compiler/tooling-surface.md` to match the branch's actual tooling state.
- Durable follow-ups now logged in `.squad/decisions.md`: clarify design-spec vs. implementation-status docs, recover the missing LS test corpus, and decide the grammar-generator ownership path.

### 2026-05-08T01:15:57-04:00 — Grammar generator implementation complete

**Task:** Implement grammar generator per frank-grammar-spec.md (docs/working/).  
**Branch:** `feature/grammar-generator-implementation` | **PR:** #139

**What was wrong with the generator scaffold:**
- stateDeclaration only recognized `initial`; 6 other state modifiers absent
- eventWithArgsDeclaration used retired `with` syntax; broken `$ref` usage
- assertStatement used retired `assert` keyword; should be `ensure`
- fieldScalarDeclaration hardcoded 6 types; should be all 27 from catalog
- fieldCollectionDeclaration inner type list missing `integer`, `decimal`
- 8 construct-level patterns missing entirely: ruleDeclaration, stateAction, stateEnsure, eventHandler, eventEnsure, accessMode, omitDeclaration, noTransition
- functionCalls pattern missing; no catalog-derived function name list
- rootEditDeclaration present but stale (edit not in TokenKind, no samples use it)
- Top-level pattern ordering didn't match Frank's spec priority rules
- No punctuation pattern for parens/brackets

**What was implemented:**
- All 16 must-fix items from frank-grammar-spec.md completed
- messageStrings gold pattern present from prior commit
- ScopeToRepositoryKey descriptive names present from prior commit
- All structural patterns derive type/modifier/function alternations from catalog
- 42 repository keys, 41 top-level patterns in correct spec-defined order
- Stale patterns removed: eventWithArgsDeclaration, assertStatement, machineDeclaration, rootEditDeclaration
- Stale keywords absent from output: nullable, invariant, assert, with
- TODO comment in generator at exact function-arg message-string wire-in point (blocked on FunctionMeta positional flag)

**Key files:**
- `tools/Precept.GrammarGen/Program.cs` — generator implementation
- `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` — generated output
- `.squad/decisions/inbox/kramer-grammar-gen-impl.md` — implementation decisions

**Test status:** LS tests were pre-existing 194 failures (stubs, not regressions). Generator builds and produces valid JSON.

### Historical summary through 2026-05-07
- Prior active work covered grammar/completion sync for guards, conditional expressions, `and`/`or`/`not`, stateless edit forms, semantic-token metadata propagation, README/tooling accuracy passes, and the C93 divisor-safety tooling fixes.
- The standing tooling baseline is unchanged: docs must reflect reality, tests are the safest spec anchor for language-server behavior, and future tooling derivation should come from catalog metadata rather than hand-maintained lists.
