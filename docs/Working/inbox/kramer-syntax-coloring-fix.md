# Kramer — Syntax Coloring Fix

- What changed:
  - `SemanticTokensHandler.ProjectLexicalTokens()` now suppresses catalog keyword semantic tokens (`KeywordSemantic` / `KeywordGrammar`) so the language server stops overriding TextMate keyword coloring on connect, while still keeping typed constants, operators, type keywords, comments, values, and identifier overlays.
  - `SemanticTokenTypes` and `tools/Precept.VsCode/package.json` now align fallback identifier scopes with the actual grammar scopes for precept names, state names, event names, and arg names; the constrained state fallback scope was aligned too.
  - `tools/Precept.GrammarGen/Program.cs` now recognizes the updated precept-name scope for future generator runs.

- Commit SHA: `3c3681ea7df1039b2f59615c88b0fa86940094fa`

- Test coverage added:
  - New semantic-token regressions prove keywords are suppressed while identifier semantic tokens still survive in merged output.
  - New manifest coverage proves semantic-token fallback scopes match the grammar-aligned identifier scopes.
  - Validation run: targeted semantic tests passed; LS suite passed with the known 5 pre-existing failures filtered out; full LS suite still reports the same 5 pre-existing failures.

- Surprises:
  - The fallback mismatch also included `preceptState.preceptConstrained`, so I aligned that entry while fixing the requested identifier scopes.
  - The full language-server suite is still blocked by the same pre-existing 5 failures tied to `loan-application` / typed-constant-hole tests, not by this fix.
