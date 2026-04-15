# Syntax Highlighting Design

Date: 2026-04-03

Status: **Design locked.** The 9-shade semantic palette is frozen. Implementation now uses custom semantic token types plus `semanticTokenScopes`, with color lock carried by Precept-specific TextMate fallback scopes.

> Brand source of truth: `brand/brand-decisions.md § Semantic palette` and `brand/brand-spec.html § Color System`.

## Overview

Precept syntax highlighting uses a fixed, dark-mode-only 9-shade palette where every color encodes compiler-known meaning. Nothing is decorative. Typography (bold, italic) adds a second information channel for constraint visibility.

The system has two axes:

1. **Color → category.** What kind of thing is this token? Structure, state, event, data, or rule message.
2. **Typography → constraint pressure.** Is this token under rule constraints? Italic = yes.

## Locked Palette

Background: `#0c0c0f`

| # | Family | Hex | Typography | Tokens |
|---|--------|-----|------------|--------|
| 1 | Structure · Semantic | `#4338CA` | **bold** | `precept`, `field`, `state`, `event`, `invariant`, `from`, `on`, `in`, `to`, `set`, `transition`, `edit`, `assert`, `reject`, `when`, `no` |
| 2 | Structure · Grammar | `#6366F1` | normal | `as`, `with`, `default`, `nullable`, `any`, `all`, `of`, `into`, `because`, `initial`, constraint keywords, `=`, `->`, operators, punctuation |
| 3 | Structure · Hero | `#A5B4FC` | normal | Precept name (the contract identity) |
| 4 | States | `#A898F5` | normal / *italic if constrained* | State names |
| 5 | Events | `#30B8E8` | normal / *italic if constrained* | Event names |
| 6 | Data · Names | `#B0BEC5` | normal / *italic if guarded* | Field names, argument names |
| 7 | Data · Types | `#9AA8B5` | normal | `string`, `number`, `boolean`, `integer`, `decimal`, `choice`, `set`, `queue`, `stack` |
| 8 | Data · Values | `#84929F` | normal | `true`, `false`, `null`, string literals, number literals |
| 9 | Rules · Messages | `#FBBF24` | normal | String content in `because` / `reject` |

Verdict colors (`#34D399` enabled, `#F87171` blocked, `#FDE047` warning) are runtime-only — never in syntax highlighting.

## Current Implementation Pipeline

Three layers produce highlighting today. Semantic tokens drive the compiler-aware categorization, TextMate provides the immediate fallback, and `package.json` binds both to the locked palette through Precept-owned scopes.

### Layer 1: TextMate Grammar (static, regex-based)

**File:** `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json`

Regex patterns assign TextMate scopes to tokens. Used as the immediate fallback when semantic tokens haven't loaded, and as the fallback scope map for semantic tokens that intentionally resolve through Precept-owned TextMate scopes.

Current scope assignments:

| Token class | TextMate scope |
|-------------|---------------|
| Control keywords | `keyword.control.precept` |
| Grammar keywords | `keyword.other.grammar.precept` |
| Declaration keywords | `keyword.other.precept` |
| Action keywords | `keyword.other.precept` |
| Constraint keywords | `keyword.other.constraint.precept` |
| Type keywords | `storage.type.precept` |
| State names | `entity.name.type.state.precept` |
| Event names | `entity.name.function.event.precept` |
| Field names | `variable.other.field.precept` |
| Event arg refs | `variable.other.property.precept` |
| Operators | `keyword.operator.{comparison,logical,arithmetic,assignment}.precept` |
| Arrow | `punctuation.separator.arrow.precept` |
| Literals | `constant.language.precept` / `constant.numeric.precept` |
| Strings | `string.quoted.double.precept` |
| Comments | `comment.line.number-sign.precept` |

### Layer 2: Language Server Semantic Tokens (dynamic, compiler-aware)

**File:** `tools/Precept.LanguageServer/PreceptSemanticTokensHandler.cs`

The language server tokenizes `.precept` source and emits LSP semantic tokens. When available, semantic tokens override TextMate scopes on a token-by-token basis.

Current legend:

```
TokenTypes:    keyword, type, function, variable, number, string, operator, comment,
               preceptComment, preceptKeywordSemantic, preceptKeywordGrammar,
               preceptState, preceptEvent, preceptFieldName, preceptType,
               preceptValue, preceptMessage
TokenModifiers: preceptConstrained
```

Current classification:

| `TokenCategory` | Semantic type | Tokens |
|------------------|---------------|--------|
| Control | `preceptKeywordSemantic` | `when` |
| Declaration | `preceptKeywordSemantic` | `precept`, `field`, `invariant`, `state`, `event`, `assert`, `edit`, `in`, `to`, `from`, `on` |
| Action | `preceptKeywordSemantic` | `set`, `add`, `remove`, `enqueue`, `dequeue`, `push`, `pop`, `clear` |
| Outcome | `preceptKeywordSemantic` | `transition`, `reject`, `no` |
| Grammar | `preceptKeywordGrammar` | `as`, `with`, `nullable`, `default`, `because`, `initial`, `any`, `all`, `of`, `into` |
| Constraint | `preceptKeywordGrammar` | `nonnegative`, `positive`, `min`, `max`, `notempty`, `minlength`, `maxlength`, `mincount`, `maxcount`, `maxplaces`, `ordered` |
| Type | `preceptType` | `string`, `number`, `boolean`, `integer`, `decimal`, `choice`, `set`, `queue`, `stack` |
| Literal | `preceptValue` | `true`, `false`, `null` |
| Operator | `preceptKeywordGrammar` | All comparison, logical, arithmetic, assignment, and `contains` operators |
| Punctuation | `preceptKeywordGrammar` | `->` only |

Identifier classification (context-aware via `ClassifyIdentifier()`):

| Context | Semantic type | Example |
|---------|---------------|---------|
| After `state`, `transition`, `in`, `to` | `preceptState` | state names |
| After `event`, `on` | `preceptEvent` | event names |
| After `field`, `set`, `add`, `remove`, etc. | `preceptFieldName` | field names |
| After `precept` | `preceptName` | precept name |
| After `.` | `preceptFieldName` | member access |
| Default | `preceptFieldName` | bare identifiers |

### Layer 3: VS Code Color Mapping

**File:** `tools/Precept.VsCode/package.json`

Currently: `"semanticHighlighting": true` is set, custom semantic token types/modifiers are declared, and `semanticTokenScopes` maps those semantic tokens into Precept-specific TextMate scopes. The locked palette is applied through Precept-owned TextMate scope rules in `configurationDefaults.editor.tokenColorCustomizations`.

## Design: Mapping the 8-Shade Palette to the Pipeline

### Why The Split Was Needed

The earlier pipeline classified tokens into generic LSP semantic types (`keyword`, `type`, `function`, `variable`). Those generic buckets collapsed all keywords to one color and all identifiers to another. The locked design required 8 distinct shades plus italic/bold modifiers, which is why the semantic-token split was introduced.

### Strategy

**Semantic tokens as the primary color driver.** The language server has full compiler context — it knows which token is a state name vs. an event name vs. a field name, and it can detect constraint relationships. This is where the 8-shade logic lives.

**TextMate grammar as the fallback.** TextMate scopes provide coloring before the language server loads. The scopes already have enough granularity for a reasonable approximation of the 8-shade system.

**`semanticTokenScopes` + Precept-owned TextMate scopes as the color lock.** The language server still emits custom semantic token types, but when no theme provides semantic token color rules, VS Code intentionally falls back through the semantic token scope map. By mapping custom semantic tokens to Precept-specific TextMate scopes, then binding those scopes in `editor.tokenColorCustomizations`, the extension gets stable colors without bundling a dedicated theme.

### Implemented Changes

#### A. Language Server — New Token Types and Modifiers

**Implemented legend split.** The generic `keyword` bucket was split into types that the `package.json` color map can target independently. Two approaches were considered:

**Option 1 — Custom semantic token types.** Register Precept-specific token types in the legend:

```csharp
TokenTypes = new Container<SemanticTokenType>(
    // Standard LSP types (retained for compatibility)
    "keyword", "type", "function", "variable",
    "number", "string", "operator", "comment",
    // Precept-specific types
    "preceptKeywordSemantic",    // Structure · Semantic
    "preceptKeywordGrammar",     // Structure · Grammar
    "preceptState",              // States
    "preceptEvent",              // Events
    "preceptFieldName",          // Data · Names
    "preceptType",               // Data · Types
    "preceptValue",              // Data · Values
    "preceptMessage"             // Rules · Messages
),
TokenModifiers = new Container<SemanticTokenModifier>(
    "preceptConstrained"         // italic signal
)
```

Then in `package.json`, declare each custom type and map it to Precept-specific fallback scopes:

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

The actual hex values are then bound to those scopes in `editor.tokenColorCustomizations`.

**Pros:** Keeps custom semantic token types, avoids bundling a theme, and matches how VS Code's semantic token scope-map fallback is designed to work.
**Cons:** The scope inspector typically shows the mapped TextMate scope and a standard token kind such as `String` or `Other`, not the custom semantic token id directly.

**Option 2 — Standard types with custom modifiers.** Keep standard LSP types but add Precept-specific modifiers to disambiguate:

```csharp
TokenTypes = new Container<SemanticTokenType>(
    "keyword", "type", "function", "variable",
    "number", "string", "operator", "comment"
),
TokenModifiers = new Container<SemanticTokenModifier>(
    "declaration",      // re-use standard: for Structure · Semantic bold
    "defaultLibrary",   // re-use standard: for Structure · Grammar
    "preceptConstrained"
)
```

Then the `package.json` rules would use `keyword:declaration` for semantic and `keyword:defaultLibrary` for grammar. However, `variable` still covers both field names and event args, and `type` still covers both states and DSL types — splitting those requires either custom types or custom modifiers anyway.

**✅ Decided: Option 1.** Custom token types give us a clean 1:1 mapping between the 8-shade palette and the token legend. The language server already owns the color semantics — custom types make that ownership explicit. Opaque custom types also prevent user-chosen themes from interfering with the locked palette.

#### B. `TokenCategory` Refactor and `BuildSemanticTypeMap()` Reclassification

**✅ Implemented: Add `Grammar` category to `PreceptToken.cs`.** The semantic/grammar split is a first-class DSL concept, not just a visual hack. The `precept_language` MCP tool surfaces it automatically.

**Grammar keyword realignment (10 tokens):**

| Token | Old category | New category | Rationale |
|-------|-------------|-------------|-----------|
| `as` | Declaration | **Grammar** | Type annotation glue |
| `with` | Declaration | **Grammar** | Argument list introducer |
| `nullable` | Declaration | **Grammar** | Type modifier |
| `default` | Declaration | **Grammar** | Value modifier — lighter weight than behavioral drivers |
| `because` | Declaration | **Grammar** | Links reject/assert to message string |
| `any` | Control | **Grammar** | Wildcard modifier, not flow control |
| `all` | Quantifier-only keyword | **Grammar** | Field-target quantifier in `edit` declarations |
| `of` | Control | **Grammar** | Type connector ("set of string") |
| `into` | Action | **Grammar** | Capture preposition, connective role |
| `initial` | Control | **Grammar** | State modifier — lighter weight, same as `nullable`/`default` |

**Declaration realignment (Frank follow-up):** `state`, `in`, `to`, `from`, and `on` now live under `Declaration`, leaving `when` as the only `Control` keyword. This keeps statement anchors grouped together while reserving `Control` for actual guard flow.

**`BuildSemanticTypeMap()` reclassification:**

| `TokenCategory` | Current → New semantic type |
|------------------|-----------------------------|
| Control | `keyword` → `preceptKeywordSemantic` |
| Declaration | `keyword` → `preceptKeywordSemantic` |
| Action | `keyword` → `preceptKeywordSemantic` |
| Outcome | `keyword` → `preceptKeywordSemantic` |
| Grammar | *(new)* → `preceptKeywordGrammar` |
| Constraint | `keyword` → `preceptKeywordGrammar` |
| Type | `type` → `preceptType` |
| Literal | `keyword` → `preceptValue` |
| Operator | `operator` → `preceptKeywordGrammar` |
| Punctuation (Arrow) | `operator` → `preceptKeywordGrammar` |

Every category now maps to exactly one shade — no per-token overrides needed.

**Affected files:**

| File | Change |
|------|--------|
| `PreceptToken.cs` | Add `Grammar` to enum, re-tag grammar keywords and statement-anchor keywords |
| `PreceptSemanticTokensHandler.cs` | Add `Grammar` case in `BuildSemanticTypeMap()` switch |
| `LanguageTool.cs` + `VocabularyDto` | Add `grammarKeywords` list + `case TokenCategory.Grammar` |
| `CatalogDriftTests.cs` | Add `TokenCategory.Grammar` to `needsSymbol` list |
| `LanguageToolTests.cs` | Add `Grammar` → vocabulary list mapping |
| `PreceptAnalyzer.cs` | No change needed (skips Structure/Value, includes everything else) |
| `McpServerDesign.md` | Document new `GrammarKeywords` vocabulary section |

#### C. Language Server — `ClassifyIdentifier()` Reclassification

**✅ Decided.**

| Context | Current → New |
|---------|---------------|
| After state-context tokens | `type` → `preceptState` |
| After event-context tokens | `function` → `preceptEvent` |
| After field-context tokens | `variable` → `preceptFieldName` |
| After `precept` | `type` → **`preceptName`** (Structure · Hero — the contract identity) |
| After `.` | `variable` → `preceptFieldName` (member access) |
| Default (bare identifier) | `variable` → `preceptFieldName` |

#### D. Language Server — Constraint Detection for Italic Modifier

**✅ Decided.** Deferred to Phase 7 (after colors ship). Analysis uses the parsed `PreceptDefinition` from `SharedAnalyzer`. Fail open — no italic when parse is incomplete.

This is new capability. The handler must determine which states, events, and fields are constrained and emit the `preceptConstrained` modifier for those tokens.

**Required analysis (performed once per document):**

1. **Constrained states:** Collect all state names that appear in `in <State> assert`, `to <State> assert`, or `from <State> assert` blocks.
2. **Constrained events:** Collect all event names that have `on <Event> assert` blocks.
3. **Guarded fields:** Collect all field names referenced in `invariant` expressions.

**Implementation approach:** Before the per-token loop, extract constraint sets from the analyzer's `PreceptDefinition` for the document, building three `HashSet<string>` collections: `constrainedStates`, `constrainedEvents`, `guardedFields`. Then during token emission, when an identifier is classified as `preceptState`/`preceptEvent`/`preceptFieldName`, check membership and add the modifier. If the parse is incomplete or failed, skip italic entirely (fail open).

#### E. Extension `package.json` — Type Declarations, `semanticTokenScopes`, and Fallback Scope Rules

**✅ Decided.** Mechanical — follows directly from A and B. Comments are scanned from raw text and emitted as a Precept-specific semantic token (`preceptComment`). In practice, many themes still define aggressive styling for the standard semantic selector `comment`, so the extension also ships a targeted semantic token color override for `preceptComment` and `comment:precept` to force the Precept comment color to win.

Similarly, `preceptMessage` (rule/rejection message strings) and `preceptName` (the precept declaration name) each get direct semantic color overrides. The scope-map fallback for `preceptMessage` resolves to gold via `string.quoted.double.message.precept` TextMate rules. `preceptName` resolves via `entity.name.type.precept.precept` to the Structure · Hero color (`#A5B4FC`), ensuring the dedicated precept-name treatment wins regardless of theme semantic token interference.

Add the custom semantic token type contributions, semantic-token-to-scope mappings, and Precept-owned fallback scope rules:

```jsonc
"contributes": {
  "semanticTokenTypes": [
    { "id": "preceptComment",         "description": "Precept comment" },
    { "id": "preceptKeywordSemantic", "description": "Precept behavioral structure keyword" },
    { "id": "preceptKeywordGrammar",  "description": "Precept connective grammar keyword" },
    { "id": "preceptState",           "description": "Precept state name" },
    { "id": "preceptEvent",           "description": "Precept event name" },
    { "id": "preceptFieldName",       "description": "Precept field or argument name" },
    { "id": "preceptName",            "description": "Precept declaration name" },
    { "id": "preceptType",            "description": "Precept type keyword" },
    { "id": "preceptValue",           "description": "Precept literal value" },
    { "id": "preceptMessage",         "description": "Precept rule message string" }
  ],
  "semanticTokenModifiers": [
    { "id": "preceptConstrained", "description": "Token is under constraint/invariant pressure" }
  ],
  "semanticTokenScopes": [
    {
      "language": "precept",
      "scopes": {
        "preceptComment": ["comment.line.number-sign.precept"],
        "preceptKeywordSemantic": ["keyword.other.semantic.precept"],
        "preceptKeywordGrammar": ["keyword.other.grammar.precept"],
        "preceptState": ["entity.name.type.state.precept"],
        "preceptState.preceptConstrained": ["entity.name.type.state.constrained.precept"],
        "preceptEvent": ["entity.name.function.event.precept"],
        "preceptEvent.preceptConstrained": ["entity.name.function.event.constrained.precept"],
        "preceptFieldName": ["variable.other.field.precept"],
        "preceptFieldName.preceptConstrained": ["variable.other.field.constrained.precept"],
        "preceptName": ["entity.name.type.precept.precept"],
        "preceptType": ["storage.type.precept"],
        "preceptValue": ["constant.other.value.precept"],
        "preceptMessage": ["string.quoted.double.message.precept"]
      }
    }
  ],
  "configurationDefaults": {
    "[precept]": { "editor.semanticHighlighting.enabled": true },
    "editor.semanticTokenColorCustomizations": {
      "rules": {
        "preceptComment": { "foreground": "#9096A6", "italic": true },
        "comment:precept": { "foreground": "#9096A6", "italic": true },
        "preceptName": { "foreground": "#A5B4FC" },
        "preceptMessage": { "foreground": "#FBBF24" }
      }
    },
    "editor.tokenColorCustomizations": {
      "[*]": {
        "textMateRules": [
          { "scope": "keyword.other.semantic.precept",              "settings": { "foreground": "#4338CA", "fontStyle": "bold" } },
          { "scope": "keyword.other.grammar.precept",               "settings": { "foreground": "#6366F1" } },
          { "scope": "entity.name.type.state.precept",              "settings": { "foreground": "#A898F5" } },
          { "scope": "entity.name.type.state.constrained.precept",  "settings": { "foreground": "#A898F5", "fontStyle": "italic" } },
          { "scope": "entity.name.function.event.precept",          "settings": { "foreground": "#30B8E8" } },
          { "scope": "entity.name.function.event.constrained.precept", "settings": { "foreground": "#30B8E8", "fontStyle": "italic" } },
          { "scope": "variable.other.field.precept",                "settings": { "foreground": "#B0BEC5" } },
          { "scope": "variable.other.field.constrained.precept",    "settings": { "foreground": "#B0BEC5", "fontStyle": "italic" } },
          { "scope": "entity.name.type.precept.precept",            "settings": { "foreground": "#A5B4FC" } },
          { "scope": "storage.type.precept",                        "settings": { "foreground": "#9AA8B5" } },
          { "scope": "constant.other.value.precept",                "settings": { "foreground": "#84929F" } },
          { "scope": "string.quoted.double.message.precept",        "settings": { "foreground": "#FBBF24" } }
        ]
      }
    }
  }
}
```

#### F. TextMate Grammar — Fallback Color Anchoring

**✅ Decided.** Strings default to slate (Data · Values) in generic fallback, but dedicated message-string scopes (`string.quoted.double.message.precept`) are colored gold immediately, and the dedicated precept-name scope (`entity.name.type.precept.precept`) is colored Structure · Hero (`#A5B4FC`). This keeps the critical cases correct even when VS Code is rendering through the semantic-token scope map fallback.

The TextMate grammar already assigns specific scopes. To lock fallback colors before the language server loads, add `tokenColorCustomizations` in `configurationDefaults`:

```jsonc
"editor.tokenColorCustomizations": {
  "[*]": {
    "textMateRules": [
      { "scope": "keyword.control.precept",                "settings": { "foreground": "#4338CA", "fontStyle": "bold" } },
      { "scope": "keyword.other.grammar.precept",         "settings": { "foreground": "#6366F1" } },
      { "scope": "keyword.other.constraint.precept",      "settings": { "foreground": "#6366F1" } },
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
      { "scope": "entity.name.type.precept.precept",       "settings": { "foreground": "#A5B4FC" } },
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

**Note:** The TextMate grammar now splits grammar-role keywords into `keyword.other.grammar.precept` and constraint keywords into `keyword.other.constraint.precept`, so cold-start fallback matches the lighter Structure · Grammar treatment immediately. Behavioral declaration and action keywords still use `keyword.other.precept`, which keeps the semantic-vs-grammar distinction explicit without waiting for the language server. Comments remain the exception to the pure scope-map path: they also get a targeted semantic override because themes commonly hard-style the standard `comment` semantic selector.

#### G. String Literal Context — Messages vs. Plain Strings

**✅ Decided.** Mechanical — uses existing `previousKind` tracking.

The locked palette assigns `#FBBF24` (gold) to rule message strings (in `because` / `reject`) but treats other string contexts as Data · Values (`#84929F`). The current handler emits all `StringLiteral` tokens as `"string"` without context.

**Required change:** Track whether a string literal follows a `because` or `reject` keyword. If so, emit `preceptMessage`; otherwise emit `preceptValue`.

## Implementation Plan

See `docs/SyntaxHighlightingImplementationPlan.md` for the phased execution plan (9 phases, checkpoints, prompts, and status tracker).

## Risks and Open Questions

### R1: `configurationDefaults` scope precedence — ✅ Accepted

The extension does not bundle a theme. Instead, it relies on `semanticTokenScopes` and Precept-owned TextMate scopes. This is the intended VS Code fallback path when no theme-provided `semanticTokenColors` rule matches a custom semantic token. **Decision:** Accept the scope-map fallback as the non-theme implementation path.

### R2: Theme interaction — ✅ Accepted

The `[*]` selector applies to all themes. On light themes, the dark-mode palette will have poor contrast against a light background. Restricting to dark themes would leave light-theme users with zero Precept-specific coloring. **Decision:** Apply to all themes. Light-theme contrast is a known limitation for v1.

### R3: Constraint detection accuracy — ✅ Accepted

Italic requires knowing which states/events/fields are constrained. This depends on a successful parse. For files with syntax errors, the constraint sets may be incomplete or absent, causing italic to flicker as the user types. **Decision:** Fail open — if the parse is incomplete, skip italic rather than showing stale data. Italic is a progressive enhancement. Deferred to Phase 7.

### R4: Bold in semantic tokens — ✅ Accepted

VS Code semantic token rules support `"bold": true` in `settings.json` / `configurationDefaults`, but this is applied via CSS `font-weight`. The actual rendering depends on whether the editor font has a bold variant. Cascadia Cove (the brand font) has a usable weight range for this, so bold will render correctly when that family is available. For users with other fonts, bold is best-effort. **Decision:** Acceptable — bold is a brand signal, not a correctness requirement.

### R5: TextMate keyword scope granularity — ✅ Resolved

The grammar now assigns grammar-role keywords to `keyword.other.grammar.precept` and constraint keywords to `keyword.other.constraint.precept`, so TextMate fallback no longer collapses them into the heavier semantic keyword treatment during cold start. **Decision:** Keep the split in sync with `TokenCategory.Grammar` and `TokenCategory.Constraint` so fallback and semantic highlighting stay aligned.

### R6: Dual-category tokens — ✅ Accepted

Some tokens have multiple `[TokenCategory]` attributes (e.g., `set` is both `Action` and `Type`). `GetCategory()` returns only the primary (first) attribute. The current design maps the primary category. If a dual-role token needs different colors in different syntactic positions, the handler would need context-aware logic beyond the category map. **Decision:** Already handled implicitly — `set` as a keyword gets Structure · Semantic from its Action category; `set` as a collection type in a field declaration gets Data · Types from the Type category. The tokenizer disambiguates based on position. No change needed.
