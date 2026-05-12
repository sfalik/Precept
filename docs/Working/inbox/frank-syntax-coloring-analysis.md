# Decision: Syntax Coloring LS Override — Root Cause and Fix Design

**Author:** Frank
**Date:** 2026-05-12T01:04:03-04:00
**Status:** Design complete — pending implementation review

## Key Findings

The recurring syntax coloring breakage on LS connect has a single structural root cause: **the LS lexical pass emits semantic tokens for EVERY keyword in the file, overriding the TM grammar's context-sensitive structural classifications with catalog-fixed visual categories.**

### Specific failure modes

1. **Declaration keywords gain bold** — TM assigns `keyword.declaration.precept` (no bold), LS assigns `preceptKeywordSemantic` (bold). Affects `precept`, `state`, `event`, `field`, `rule`, `ensure`, `initial`.

2. **Grammar keywords change hue in declaration context** — `as`, `default`, `optional` shift from #4338CA (declaration indigo) to #6366F1 (grammar indigo) because TM assigns `keyword.declaration.precept` but catalog assigns `KeywordGrammar`.

3. **State modifiers shift dramatically** — `terminal`, `required`, `irreversible`, `success`, `warning`, `error` go from #9AA8B5 slate gray (`storage.modifier.state.precept`) to #4338CA bold indigo (`preceptKeywordSemantic`).

4. **Preposition keywords change hue** — `from`, `on`, `to`, `in` shift from #4338CA (`keyword.control.precept`) to #6366F1 (`preceptKeywordGrammar`).

5. **Secondary:** `semanticTokenScopes` fallback scopes don't match TM structural scopes (e.g., `entity.name.state.precept` vs `entity.name.type.state.precept`).

### Why prior fixes didn't stick

Every prior fix targeted a specific symptom (gold drift, delta crashes, span fixes) without addressing the architectural mismatch: the LS lexical pass classifies keywords with catalog-fixed roles, while the TM grammar classifies them with context-sensitive structural roles. These are inherently different, and no amount of color-matching can reconcile them because the same keyword gets different scopes in different TM structural contexts.

## Recommended Solution

**Suppress keyword semantic tokens + align remaining scopes + catalog-owned built-in colors.**

Core principle: **TM grammar owns keyword coloring. LS owns identifier coloring. Neither steps on the other's territory.**

1. **`SemanticTokensHandler.ProjectLexicalTokens()`** — Skip all keyword/operator tokens (tokens with non-null `meta.Text`). Only emit semantic tokens for typed constants and the `set`-in-type-position reclassification. ~5 lines of code.

2. **`SemanticTokenTypes.cs`** — Align `TextMateScope` for identifier types (`State`, `Event`, `ArgName`, `Name`) with the actual TM grammar structural scopes.

3. **`package.json`** — Update `semanticTokenScopes` to match aligned catalog scopes.

4. **Color notification** — Add coverage for `function` and `string` built-in types (or better: use catalog-owned `preceptFunction`/`preceptString` names).

5. **Future:** Extend grammar generator to produce `semanticTokenTypes`, `semanticTokenScopes`, `semanticTokenModifiers` from catalog — prevents drift forever.

Full design: `docs/Working/syntax-coloring-fix-design.md`.
