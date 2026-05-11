# Syntax Highlighting — Implementation Plan

Date: 2026-04-03
Spec: `docs/SyntaxHighlightingDesign.md`
Prerequisite: None — all required infrastructure (tokenizer, language server, TextMate grammar, extension manifest) is in place.

> **Status:** Not started. All design decisions locked in the design doc.

This plan implements the locked 8-shade semantic palette for `.precept` syntax highlighting. The core runtime and parser are untouched — changes are confined to the token metadata layer, the language server semantic tokens handler, the VS Code extension manifest, and the TextMate grammar.

---

## Guiding Principles

1. **Phase order follows dependency.** Token metadata first, then the handler that reads it, then the extension that maps it to colors.
2. **Build at every checkpoint.** Each phase ends with `dotnet build` and `dotnet test` passing.
3. **No behavioral changes in Phase 0.** The category refactor is a reclassification — highlighting output changes (same generic types, different category labels on some tokens), but no new shades until Phase 1+.
4. **Thin handler.** The semantic tokens handler maps categories to types. It does not contain domain logic. If a classification decision gets complicated, the knowledge belongs in `PreceptToken.cs` metadata, not in handler switch branches.
5. **Extension install after Phase 6+7.** Color changes are only visible after the extension is rebuilt and reloaded.

---

## Phase 0: `TokenCategory` Refactor

**Goal:** Add `Grammar` to the `TokenCategory` enum and re-tag 9 tokens. No highlighting change yet — the handler still maps everything to generic LSP types. This phase is a standalone refactor that can ship independently.

### Steps

- [ ] Add `Grammar` member to the `TokenCategory` enum in `PreceptToken.cs` (after `Outcome`, before `Type`).
- [ ] Re-tag the following 9 tokens from their current category to `Grammar`:

| Token | Old category | Member name in enum |
|-------|-------------|---------------------|
| `as` | Declaration | `As` |
| `with` | Declaration | `With` |
| `nullable` | Declaration | `Nullable` |
| `default` | Declaration | `Default` |
| `because` | Declaration | `Because` |
| `any` | Control | `Any` |
| `of` | Control | `Of` |
| `into` | Action | `Into` |
| `initial` | Control | `Initial` |

- [ ] Update `BuildSemanticTypeMap()` in `PreceptSemanticTokensHandler.cs` — add `TokenCategory.Grammar => "keyword"` case (temporary, maps to the same generic type as before).
- [ ] Update `LanguageTool.cs` — add `grammarKeywords` list and `case TokenCategory.Grammar` branch. Add `GrammarKeywords` property to `VocabularyDto`.
- [ ] Update `CatalogDriftTests.cs` — add `TokenCategory.Grammar` to the `needsSymbol` category list.
- [ ] Update `LanguageToolTests.cs` — add `TokenCategory.Grammar` → `result.Vocabulary.GrammarKeywords` mapping in `EveryKeywordInTokenEnumAppearsInAllMatchingVocabularyLists`.
- [ ] Update `PreceptAnalyzer.cs` — verify `BuildKeywordItems()` still works (it skips only `Structure` and `Value`, so `Grammar` tokens are included automatically — no change needed, just verify).
- [ ] Update `McpServerDesign.md` — document the new `GrammarKeywords` vocabulary section.

### Affected files

| File | Changes |
|------|---------|
| `src/Precept/Dsl/PreceptToken.cs` | Add `Grammar` to enum, change 9 `[TokenCategory]` attributes |
| `tools/Precept.LanguageServer/PreceptSemanticTokensHandler.cs` | Add `Grammar` case in `BuildSemanticTypeMap()` |
| `tools/Precept.Mcp/Tools/LanguageTool.cs` | Add `grammarKeywords` list + case branch |
| `tools/Precept.Mcp/Tools/LanguageTool.cs` (VocabularyDto) | Add `GrammarKeywords` property |
| `test/Precept.Tests/CatalogDriftTests.cs` | Add `Grammar` to `needsSymbol` |
| `test/Precept.Mcp.Tests/LanguageToolTests.cs` | Add `Grammar` → vocabulary mapping |
| `docs/McpServerDesign.md` | Document `GrammarKeywords` |

### Checkpoint

- `dotnet build` passes (entire solution)
- `dotnet test` passes (all 3 test projects)
- `precept_language` MCP tool output includes a `GrammarKeywords` array containing: `as`, `with`, `nullable`, `default`, `because`, `any`, `of`, `into`, `initial`
- No references to the old categories remain for the moved tokens (verify with grep)

### Prompt

```
Read docs/SyntaxHighlightingImplementationPlan.md Phase 0 for the full task.
Read docs/SyntaxHighlightingDesign.md § Decision B for the rationale.

Add Grammar to the TokenCategory enum in src/Precept/Dsl/PreceptToken.cs.
Re-tag 9 tokens: As, With, Nullable, Default, Because, Any, Of, Into, Initial
— change their [TokenCategory] attribute from their current category to Grammar.

Then update all consumers:
1. PreceptSemanticTokensHandler.cs — add TokenCategory.Grammar => "keyword" to the switch
2. LanguageTool.cs — add grammarKeywords list + case TokenCategory.Grammar branch
3. VocabularyDto — add GrammarKeywords property
4. CatalogDriftTests.cs — add TokenCategory.Grammar to needsSymbol
5. LanguageToolTests.cs — add Grammar → vocabulary mapping

Build and run all tests. Verify precept_language output includes GrammarKeywords.
Update McpServerDesign.md to document the new vocabulary section.
```

---

## Phase 1: Custom Semantic Token Types + Modifiers

**Goal:** Replace the generic LSP token legend with Precept-specific types. After this phase, the handler emits custom type names — but no colors are bound yet (colors come in Phase 5).

### Steps

- [ ] Replace the `Legend` in `PreceptSemanticTokensHandler.cs`. New token types:

```
preceptKeywordSemantic, preceptKeywordGrammar, preceptState, preceptEvent,
preceptFieldName, preceptType, preceptValue, preceptMessage
```

Plus standard types retained for compatibility: `keyword`, `type`, `function`, `variable`, `number`, `string`, `operator`, `comment`.

- [ ] Add token modifier: `preceptConstrained` (registered but not emitted until Phase 7).

### Affected files

| File | Changes |
|------|---------|
| `tools/Precept.LanguageServer/PreceptSemanticTokensHandler.cs` | Replace `Legend` TokenTypes and TokenModifiers |

### Checkpoint

- `dotnet build` passes
- Language server starts without error (legend is accepted by VS Code)
- Existing highlighting still works (standard types are retained)

### Prompt

```
Read docs/SyntaxHighlightingImplementationPlan.md Phase 1.

In PreceptSemanticTokensHandler.cs, expand the Legend to include 8 custom
Precept token types alongside the existing standard types:
  preceptKeywordSemantic, preceptKeywordGrammar, preceptState, preceptEvent,
  preceptFieldName, preceptType, preceptValue, preceptMessage

Add one token modifier: preceptConstrained

Keep all existing standard types (keyword, type, function, variable, number,
string, operator, comment) so that unchanged code paths still work.

Build. Start language server and verify no errors.
```

---

## Phase 2: `BuildSemanticTypeMap()` Reclassification

**Goal:** The category → semantic type switch now emits Precept-specific types instead of generic LSP types.

### Steps

- [ ] Update the switch in `BuildSemanticTypeMap()`:

| `TokenCategory` | New semantic type |
|------------------|-------------------|
| Control | `preceptKeywordSemantic` |
| Declaration | `preceptKeywordSemantic` |
| Action | `preceptKeywordSemantic` |
| Outcome | `preceptKeywordSemantic` |
| Grammar | `preceptKeywordGrammar` |
| Type | `preceptType` |
| Literal | `preceptValue` |
| Operator | `preceptKeywordGrammar` |
| Punctuation (Arrow) | `preceptKeywordGrammar` |

### Affected files

| File | Changes |
|------|---------|
| `tools/Precept.LanguageServer/PreceptSemanticTokensHandler.cs` | Rewrite `BuildSemanticTypeMap()` switch |

### Checkpoint

- `dotnet build` passes
- Keywords, operators, types, and literals emit custom Precept token types
- Highlighting temporarily loses theme colors (custom types have no color mapping yet — expected)

### Prompt

```
Read docs/SyntaxHighlightingImplementationPlan.md Phase 2.

Rewrite BuildSemanticTypeMap() to emit Precept-specific types.
All Control/Declaration/Action/Outcome → preceptKeywordSemantic.
Grammar → preceptKeywordGrammar.
Type → preceptType.
Literal → preceptValue.
Operator → preceptKeywordGrammar.
Punctuation Arrow → preceptKeywordGrammar.
Other Punctuation → null (unchanged).

Build. No test changes needed — semantic token tests assert on token presence,
not on specific type names.
```

---

## Phase 3: `ClassifyIdentifier()` Reclassification

**Goal:** Identifiers now emit Precept-specific types. Precept name gets gold.

### Steps

- [ ] Update `ClassifyIdentifier()` return values:

| Context | New return |
|---------|-----------|
| After `precept` | `preceptMessage` (gold — the contract identity) |
| After state-context tokens | `preceptState` |
| After event-context tokens | `preceptEvent` |
| After field-context tokens | `preceptFieldName` |
| After `.` | `preceptFieldName` |
| Comma in state context | `preceptState` |
| Comma in event context | `preceptEvent` |
| Comma in field context | `preceptFieldName` |
| Default | `preceptFieldName` |

### Affected files

| File | Changes |
|------|---------|
| `tools/Precept.LanguageServer/PreceptSemanticTokensHandler.cs` | Rewrite `ClassifyIdentifier()` return values |

### Checkpoint

- `dotnet build` passes
- State names, event names, field names, and the precept name each emit distinct custom types

### Prompt

```
Read docs/SyntaxHighlightingImplementationPlan.md Phase 3.

Rewrite ClassifyIdentifier() to return Precept-specific types:
- After Precept → "preceptMessage" (gold for the contract name)
- State context → "preceptState"
- Event context → "preceptEvent"
- Field context, dot access, default → "preceptFieldName"

Build and verify.
```

---

## Phase 4: String Literal Context

**Goal:** Message strings (after `because`/`reject`) emit `preceptMessage` (gold). All other strings emit `preceptValue` (slate).

### Steps

- [ ] In the `Tokenize()` method, update the `StringLiteral` branch to check `previousKind`:
  - If `previousKind` is `PreceptToken.Because` or `PreceptToken.Reject` → emit `preceptMessage`
  - Otherwise → emit `preceptValue`

### Affected files

| File | Changes |
|------|---------|
| `tools/Precept.LanguageServer/PreceptSemanticTokensHandler.cs` | Update `StringLiteral` classification |

### Checkpoint

- `dotnet build` passes
- String after `because` → `preceptMessage`; string after `default` → `preceptValue`

### Prompt

```
Read docs/SyntaxHighlightingImplementationPlan.md Phase 4.

In the Tokenize() loop, update the StringLiteral branch:
- If previousKind is Because or Reject → emit "preceptMessage"
- Otherwise → emit "preceptValue"

Also update NumberLiteral to emit "preceptValue" instead of "number".

Build and verify.
```

---

## Phase 5: Constraint Detection

**Goal:** Emit the `preceptConstrained` modifier for states, events, and fields under constraint pressure. This adds italic to those tokens — the "second axis" of the 8-shade system.

### Steps

- [ ] In `PreceptSemanticTokensHandler.cs`, before the per-token loop, extract constraint sets from `SharedAnalyzer`:
  - `HashSet<string> constrainedStates` — state names in `in/to/from <State> assert` blocks
  - `HashSet<string> constrainedEvents` — event names with `on <Event> assert` blocks
  - `HashSet<string> guardedFields` — field names referenced by `invariant` expressions
- [ ] During token emission, when an identifier is classified as `preceptState`/`preceptEvent`/`preceptFieldName`, check membership and add the `preceptConstrained` modifier.
- [ ] If the parse is incomplete or failed, skip italic entirely (empty constraint sets — fail open).
- [ ] Add tests: constrained state gets modifier, unconstrained does not; same pattern for events and fields.

### Affected files

| File | Changes |
|------|--------|
| `tools/Precept.LanguageServer/PreceptSemanticTokensHandler.cs` | Constraint set extraction + modifier emission |
| `test/Precept.LanguageServer.Tests/` | New tests for constraint detection |

### Checkpoint

- `dotnet build` and `dotnet test` pass
- States in `in State assert` blocks render with `preceptConstrained` modifier
- Events with `on Event assert` blocks render with modifier
- Fields referenced by `invariant` render with modifier
- Unconstrained tokens have no modifier
- Files with parse errors show no modifier (fail open)

### Prompt

```
Read docs/SyntaxHighlightingImplementationPlan.md Phase 5.
Read docs/SyntaxHighlightingDesign.md § Decision D.

Before the per-token loop in Tokenize(), extract constraint sets from the
SharedAnalyzer's PreceptDefinition:
1. constrainedStates — states in in/to/from X assert blocks
2. constrainedEvents — events with on X assert blocks
3. guardedFields — field names in invariant expressions

During emission, add "preceptConstrained" modifier when the identifier's
text matches a constrained set. If parse is incomplete, use empty sets.

Build and run all tests. Add new tests for constrained vs unconstrained tokens.
```

---

## Phase 6: Extension Color Lock

**Goal:** Declare the custom semantic token types/modifiers and map them to Precept-specific fallback scopes in `package.json`. After this phase + extension install, the semantic layer has a stable scope-map path even without a bundled theme.

### Steps

- [ ] Add `semanticTokenTypes` contribution to `package.json` under `contributes`:

```jsonc
"semanticTokenTypes": [
  { "id": "preceptKeywordSemantic", "description": "Precept behavioral structure keyword" },
  { "id": "preceptKeywordGrammar",  "description": "Precept connective grammar keyword" },
  { "id": "preceptState",           "description": "Precept state name" },
  { "id": "preceptEvent",           "description": "Precept event name" },
  { "id": "preceptFieldName",       "description": "Precept field or argument name" },
  { "id": "preceptType",            "description": "Precept type keyword" },
  { "id": "preceptValue",           "description": "Precept literal value" },
  { "id": "preceptMessage",         "description": "Precept rule message string" }
]
```

- [ ] Add `semanticTokenModifiers` contribution:

```jsonc
"semanticTokenModifiers": [
  { "id": "preceptConstrained", "description": "Token is under constraint/invariant pressure" }
]
```

- [ ] Add `semanticTokenScopes` so each custom semantic token resolves to a Precept-owned fallback scope:

```jsonc
"semanticTokenScopes": [
  {
    "language": "precept",
    "scopes": {
      "preceptKeywordSemantic": ["keyword.other.semantic.precept"],
      "preceptKeywordGrammar": ["keyword.other.grammar.precept"],
      "preceptState": ["entity.name.type.state.precept"],
      "preceptState.preceptConstrained": ["entity.name.type.state.constrained.precept"],
      "preceptEvent": ["entity.name.function.event.precept"],
      "preceptEvent.preceptConstrained": ["entity.name.function.event.constrained.precept"],
      "preceptFieldName": ["variable.other.field.precept"],
      "preceptFieldName.preceptConstrained": ["variable.other.field.constrained.precept"],
      "preceptType": ["storage.type.precept"],
      "preceptValue": ["constant.other.value.precept"],
      "preceptMessage": ["entity.name.precept.message.precept", "string.quoted.double.message.precept"]
    }
  }
]
```

### Affected files

| File | Changes |
|------|---------|
| `tools/Precept.VsCode/package.json` | Add `semanticTokenTypes`, `semanticTokenModifiers`, `semanticTokenScopes` |

### Checkpoint

- `npm run compile` passes in `tools/Precept.VsCode/`
- After `extension: install` task + reload, `.precept` files show the 8-shade palette
- Structure · Semantic keywords are deep indigo bold
- Structure · Grammar keywords are bright indigo normal
- State names are violet, event names are cyan
- Field names are slate, types are medium slate, values/literals are dark slate
- Message strings (after `because`/`reject`) are gold
- Precept name is gold

### Prompt

```
Read docs/SyntaxHighlightingImplementationPlan.md Phase 6.

Add three new sections to the "contributes" object in
tools/Precept.VsCode/package.json:

1. "semanticTokenTypes" — 8 custom type declarations
2. "semanticTokenModifiers" — 1 modifier (preceptConstrained)
3. "semanticTokenScopes" for custom semantic-token-to-scope fallback mapping
   mapping each type to its hex + font style

Use the exact hex values and font styles from the plan.
Merge into the existing configurationDefaults if one exists, or create it.

Build (npm run compile). Run extension: install task. Verify colors.
```

---

## Phase 7: TextMate Fallback Colors

**Goal:** Lock fallback colors for the 1–2 second cold-start window before semantic tokens load, and for the semantic-token scope-map fallback path when no theme provides semantic token colors.

### Steps

- [ ] Add `editor.tokenColorCustomizations` to `configurationDefaults` in `package.json`:

```jsonc
"editor.tokenColorCustomizations": {
  "[*]": {
    "textMateRules": [
      { "scope": "keyword.control.precept",                "settings": { "foreground": "#4338CA", "fontStyle": "bold" } },
      { "scope": "keyword.other.precept",                  "settings": { "foreground": "#4338CA", "fontStyle": "bold" } },
      { "scope": "entity.name.type.state.precept",         "settings": { "foreground": "#A898F5" } },
      { "scope": "entity.name.function.event.precept",     "settings": { "foreground": "#30B8E8" } },
      { "scope": "variable.other.field.precept",           "settings": { "foreground": "#B0BEC5" } },
      { "scope": "variable.other.property.precept",        "settings": { "foreground": "#B0BEC5" } },
      { "scope": "variable.other.precept",                 "settings": { "foreground": "#B0BEC5" } },
      { "scope": "storage.type.precept",                   "settings": { "foreground": "#9AA8B5" } },
      { "scope": "constant.language.precept",              "settings": { "foreground": "#84929F" } },
      { "scope": "constant.numeric.precept",               "settings": { "foreground": "#84929F" } },
      { "scope": "string.quoted.double.precept",           "settings": { "foreground": "#84929F" } },
      { "scope": "keyword.operator.comparison.precept",    "settings": { "foreground": "#6366F1" } },
      { "scope": "keyword.operator.logical.precept",       "settings": { "foreground": "#6366F1" } },
      { "scope": "keyword.operator.arithmetic.precept",    "settings": { "foreground": "#6366F1" } },
      { "scope": "keyword.operator.assignment.precept",    "settings": { "foreground": "#6366F1" } },
      { "scope": "punctuation.separator.arrow.precept",    "settings": { "foreground": "#6366F1" } },
      { "scope": "punctuation.separator.comma.precept",    "settings": { "foreground": "#6366F1" } },
      { "scope": "punctuation.accessor.precept",           "settings": { "foreground": "#6366F1" } },
      { "scope": "comment.line.number-sign.precept",       "settings": { "foreground": "#9096A6", "fontStyle": "italic" } }
    ]
  }
}
```

**Note:** Strings default to slate (`#84929F`) in generic fallback. Dedicated message-string and precept-name scopes are colored gold immediately, so the highest-signal Rules · Messages cases do not depend on semantic theming.

### Affected files

| File | Changes |
|------|---------|
| `tools/Precept.VsCode/package.json` | Add `editor.tokenColorCustomizations` to `configurationDefaults` |

### Checkpoint

- `npm run compile` passes
- After extension install, `.precept` files show correct Precept colors immediately on open (within the TextMate-only window)
- Once semantic tokens load, grammar keywords lighten and unbold; message strings turn gold

### Prompt

```
Read docs/SyntaxHighlightingImplementationPlan.md Phase 7.

Add "editor.tokenColorCustomizations" to "configurationDefaults" in
tools/Precept.VsCode/package.json. Use the exact textMateRules from the plan.

Strings default to #84929F (slate). All keywords default to #4338CA bold.
Comments to #9096A6 italic.
Comments must use a Precept-specific semantic token mapped to comment.line.number-sign.precept, not the standard semantic token type comment.
Also add targeted semantic token color rules for preceptComment and comment:precept so theme comment selectors cannot override Precept comments.

Build (npm run compile). Run extension: install task. Verify fallback colors
appear before semantic tokens load.
```

---

## Phase 7: Constraint Detection (Deferred)

**Goal:** Emit the `preceptConstrained` modifier for states, events, and fields under constraint pressure. This adds italic to those tokens.

**Deferred** — shipped separately after Phases 0–6 are stable.

### Steps

- [ ] In `PreceptSemanticTokensHandler.cs`, before the per-token loop, extract constraint sets from `SharedAnalyzer`:
  - `HashSet<string> constrainedStates` — state names in `in/to/from <State> assert` blocks
  - `HashSet<string> constrainedEvents` — event names with `on <Event> assert` blocks
  - `HashSet<string> guardedFields` — field names referenced by `invariant` expressions
- [ ] During token emission, when an identifier is classified as `preceptState`/`preceptEvent`/`preceptFieldName`, check membership and add the `preceptConstrained` modifier.
- [ ] If the parse is incomplete or failed, skip italic entirely (empty constraint sets — fail open).
- [ ] Add tests: constrained state gets modifier, unconstrained does not; same pattern for events and fields.

### Affected files

| File | Changes |
|------|---------|
| `tools/Precept.LanguageServer/PreceptSemanticTokensHandler.cs` | Constraint set extraction + modifier emission |
| `test/Precept.LanguageServer.Tests/` | New tests for constraint detection |

### Checkpoint

- `dotnet build` and `dotnet test` pass
- States in `in State assert` blocks render italic
- Events with `on Event assert` blocks render italic
- Fields referenced by `invariant` render italic
- Unconstrained tokens remain normal weight
- Files with parse errors show no italic (fail open)

### Prompt

```
Read docs/SyntaxHighlightingImplementationPlan.md Phase 7.
Read docs/SyntaxHighlightingDesign.md § Decision D.

Before the per-token loop in Tokenize(), extract constraint sets from the
SharedAnalyzer's PreceptDefinition:
1. constrainedStates — states in in/to/from X assert blocks
2. constrainedEvents — events with on X assert blocks
3. guardedFields — field names in invariant expressions

During emission, add "preceptConstrained" modifier when the identifier's
text matches a constrained set. If parse is incomplete, use empty sets.

Build and run all tests. Add new tests for constrained vs unconstrained tokens.
```

---

## Phase 8: TextMate Grammar Scope Split (Deferred)

**Goal:** Split `keyword.other.precept` into separate scopes for semantic and grammar keywords, reducing the cold-start flash where grammar keywords briefly appear bold.

**Deferred** — the cold-start flash is a 1–2 second window on first file open only. Not worth the sync maintenance cost for v1.

### Steps

- [ ] In `precept.tmLanguage.json`, split the keyword patterns:
  - `keyword.other.semantic.precept` for Declaration/Action/Outcome keywords that stay Semantic
  - `keyword.other.grammar.precept` for Grammar keywords (`as`, `with`, `nullable`, `default`, `because`, `any`, `of`, `into`, `initial`)
- [ ] Update the TextMate fallback rules in `package.json` to target the new scopes:
  - `keyword.other.semantic.precept` → `#4338CA` bold
  - `keyword.other.grammar.precept` → `#6366F1` normal
- [ ] Update `brand-decisions.md` and/or `brand-spec.html` if they reference TextMate scopes.

### Affected files

| File | Changes |
|------|---------|
| `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` | Split keyword patterns |
| `tools/Precept.VsCode/package.json` | Update TextMate fallback rules |

### Checkpoint

- `npm run compile` passes
- Grammar keywords (`as`, `with`, etc.) appear as bright indigo normal immediately on file open (no bold flash)
- Semantic keywords remain deep indigo bold
- After semantic tokens load, no visual change (TextMate and semantic tokens agree)

### Prompt

```
Read docs/SyntaxHighlightingImplementationPlan.md Phase 8.
Read docs/SyntaxHighlightingDesign.md for the grammar keyword list.

In precept.tmLanguage.json, split keyword.other.precept into two patterns:
- keyword.other.semantic.precept — for structure semantic keywords
- keyword.other.grammar.precept — for grammar/connector keywords

Grammar keywords: as, with, nullable, default, because, any, of, into, initial.

Update the textMateRules in package.json to target the new scopes separately.

Build (npm run compile). Run extension: install. Verify no bold flash
on grammar keywords during cold start.
```

---

## Status Tracker

| Phase | Description | Status | Notes |
|-------|------------|--------|-------|
| 0 | `TokenCategory` refactor | Not started | |
| 1 | Custom semantic token types + modifiers | Not started | |
| 2 | `BuildSemanticTypeMap()` reclassification | Not started | |
| 3 | `ClassifyIdentifier()` reclassification | Not started | |
| 4 | String literal context | Not started | |
| 5 | Constraint detection (italic) | Not started | |
| 6 | Extension color lock | Not started | |
| 7 | TextMate fallback colors | Not started | |
| 8 | Grammar scope split | Not started | Deferred |

---

## File Change Summary

| File | Action | Phase |
|------|--------|-------|
| `src/Precept/Dsl/PreceptToken.cs` | Edit — add `Grammar` category, re-tag 9 tokens | 0 |
| `tools/Precept.LanguageServer/PreceptSemanticTokensHandler.cs` | Major edit — legend, type map, identifier classification, string context, constraint detection | 1, 2, 3, 4, 5 |
| `tools/Precept.Mcp/Tools/LanguageTool.cs` | Edit — add `grammarKeywords` list + case, update `VocabularyDto` | 0 |
| `tools/Precept.VsCode/package.json` | Major edit — semantic token types/modifiers, color mapping, TextMate fallback | 6, 7 |
| `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` | Edit — split keyword scopes | 8 |
| `test/Precept.Tests/CatalogDriftTests.cs` | Edit — add `Grammar` to category list | 0 |
| `test/Precept.Mcp.Tests/LanguageToolTests.cs` | Edit — add `Grammar` vocabulary mapping | 0 |
| `test/Precept.LanguageServer.Tests/` | New tests — constraint detection | 5 |
| `docs/McpServerDesign.md` | Edit — document `GrammarKeywords` | 0 |

## Estimated Scope

| Phase | Changed LOC (est.) | Risk | Status |
|-------|-------------------|------|--------|
| 0. Category refactor | ~60 | Low (mechanical) | Not started |
| 1. Custom token types | ~20 | Low | Not started |
| 2. Type map reclassification | ~30 | Low | Not started |
| 3. Identifier split | ~30 | Low | Not started |
| 4. String context | ~15 | Low | Not started |
| 5. Constraint detection | ~80 | Medium (parsed model access) | Not started |
| 6. Extension color lock | ~40 | Low | Not started |
| 7. TextMate fallback | ~30 | Low | Not started |
| 8. Grammar scope split | ~40 | Low | Deferred |
| **Total** | **~345** | | |
