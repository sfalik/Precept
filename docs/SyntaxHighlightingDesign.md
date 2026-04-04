# Syntax Highlighting Design

Date: 2026-04-03

Status: **Design locked.** The 8-shade semantic palette is frozen. Implementation has not started.

> Brand source of truth: `brand/brand-decisions.md § Semantic palette` and `brand/brand-spec.html § Color System`.

## Overview

Precept syntax highlighting uses a fixed, dark-mode-only 8-shade palette where every color encodes compiler-known meaning. Nothing is decorative. Typography (bold, italic) adds a second information channel for constraint visibility.

The system has two axes:

1. **Color → category.** What kind of thing is this token? Structure, state, event, data, or rule message.
2. **Typography → constraint pressure.** Is this token under rule constraints? Italic = yes.

## Locked Palette

Background: `#0c0c0f`

| # | Family | Hex | Typography | Tokens |
|---|--------|-----|------------|--------|
| 1 | Structure · Semantic | `#4338CA` | **bold** | `precept`, `field`, `state`, `event`, `invariant`, `from`, `on`, `in`, `to`, `set`, `transition`, `edit`, `assert`, `reject`, `when`, `no` |
| 2 | Structure · Grammar | `#6366F1` | normal | `as`, `with`, `default`, `nullable`, `any`, `of`, `into`, `because`, `initial`, `=`, `->`, operators, punctuation |
| 3 | States | `#A898F5` | normal / *italic if constrained* | State names |
| 4 | Events | `#30B8E8` | normal / *italic if constrained* | Event names |
| 5 | Data · Names | `#B0BEC5` | normal / *italic if guarded* | Field names, argument names |
| 6 | Data · Types | `#9AA8B5` | normal | `string`, `number`, `boolean`, `set`, `queue`, `stack` |
| 7 | Data · Values | `#84929F` | normal | `true`, `false`, `null`, string literals, number literals |
| 8 | Rules · Messages | `#FBBF24` | normal | String content in `because` / `reject`, and the precept name |

Verdict colors (`#34D399` enabled, `#F87171` blocked, `#FDE047` warning) are runtime-only — never in syntax highlighting.

## Current Implementation Pipeline

Three layers produce highlighting today. None currently apply custom colors — all colors inherit from the active VS Code theme.

### Layer 1: TextMate Grammar (static, regex-based)

**File:** `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json`

Regex patterns assign TextMate scopes to tokens. Used as the immediate fallback when semantic tokens haven't loaded, and the permanent source for tokens the language server doesn't cover (comments, strings).

Current scope assignments:

| Token class | TextMate scope |
|-------------|---------------|
| Control keywords | `keyword.control.precept` |
| Declaration keywords | `keyword.other.precept` |
| Action keywords | `keyword.other.precept` |
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
TokenTypes:    keyword, type, function, variable, number, string, operator, comment
TokenModifiers: (none)
```

Current classification:

| `TokenCategory` | Semantic type | Tokens |
|------------------|---------------|--------|
| Control | `keyword` | `state`, `from`, `on`, `initial`, `when`, `if`, `else`, `in`, `to`, `any`, `of` |
| Declaration | `keyword` | `precept`, `field`, `event`, `as`, `with`, `assert`, `because`, `nullable`, `default`, `invariant`, `edit` |
| Action | `keyword` | `set`, `add`, `remove`, `enqueue`, `dequeue`, `push`, `pop`, `clear`, `into`, `contains` |
| Outcome | `keyword` | `transition`, `reject`, `no` |
| Type | `type` | `string`, `number`, `boolean`, `set`, `queue`, `stack` |
| Literal | `keyword` | `true`, `false`, `null` |
| Operator | `operator` | All comparison, logical, arithmetic, assignment operators |

Identifier classification (context-aware via `ClassifyIdentifier()`):

| Context | Semantic type | Example |
|---------|---------------|---------|
| After `precept`, `from`, `state`, `transition`, `in`, `to` | `type` | state names |
| After `event`, `on` | `function` | event names |
| After `field`, `set`, `add`, `remove`, etc. | `variable` | field names |
| After `.` | `variable` | member access |
| Default | `variable` | bare identifiers |

### Layer 3: VS Code Color Mapping

**File:** `tools/Precept.VsCode/package.json`

Currently: `"semanticHighlighting": true` is set, but **no custom colors are defined**. Colors come entirely from the user's active theme.

## Design: Mapping the 8-Shade Palette to the Pipeline

### Problem

The current pipeline classifies tokens into generic LSP semantic types (`keyword`, `type`, `function`, `variable`). These collapse all keywords to one color, all identifiers to another. The locked design requires 8 distinct shades plus italic/bold modifiers — far more granularity than the current generic types provide.

### Strategy

**Semantic tokens as the primary color driver.** The language server has full compiler context — it knows which token is a state name vs. an event name vs. a field name, and it can detect constraint relationships. This is where the 8-shade logic lives.

**TextMate grammar as the fallback.** TextMate scopes provide coloring before the language server loads. The scopes already have enough granularity for a reasonable approximation of the 8-shade system.

**`package.json` `semanticTokenColors` as the color lock.** This is where hex values and font styles are bound to semantic token types. This gives us fixed colors regardless of the user's theme.

### Required Changes

#### A. Language Server — New Token Types and Modifiers

**Current legend expansion needed.** The generic `keyword` type must split into types that the `package.json` color map can target independently. There are two approaches:

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

Then in `package.json`, map each custom type to its hex + style:

```jsonc
"configurationDefaults": {
  "editor.semanticTokenColorCustomizations": {
    "[*]": {
      "rules": {
        "preceptKeywordSemantic": { "foreground": "#4338CA", "bold": true },
        "preceptKeywordGrammar":  { "foreground": "#6366F1" },
        "preceptState":           { "foreground": "#A898F5" },
        "preceptEvent":           { "foreground": "#30B8E8" },
        "preceptFieldName":       { "foreground": "#B0BEC5" },
        "preceptType":            { "foreground": "#9AA8B5" },
        "preceptValue":           { "foreground": "#84929F" },
        "preceptMessage":         { "foreground": "#FBBF24" },
        "preceptState:preceptConstrained":     { "foreground": "#A898F5", "italic": true },
        "preceptEvent:preceptConstrained":     { "foreground": "#30B8E8", "italic": true },
        "preceptFieldName:preceptConstrained": { "foreground": "#B0BEC5", "italic": true }
      }
    }
  }
}
```

**Pros:** Clean, explicit mapping. Each shade has its own token type. No ambiguity.
**Cons:** Custom types are non-standard. Theme authors can't override them with familiar selectors. Requires `semanticTokenTypes` contribution in `package.json` to declare them.

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

**✅ Decided: Add `Grammar` category to `PreceptToken.cs`.** The semantic/grammar split is a first-class DSL concept, not just a visual hack. The `precept_language` MCP tool will surface it automatically.

**Token moves (8 tokens change category):**

| Token | Old category | New category | Rationale |
|-------|-------------|-------------|-----------|
| `as` | Declaration | **Grammar** | Type annotation glue |
| `with` | Declaration | **Grammar** | Argument list introducer |
| `nullable` | Declaration | **Grammar** | Type modifier |
| `default` | Declaration | **Grammar** | Value modifier — lighter weight than behavioral drivers |
| `because` | Declaration | **Grammar** | Links reject/assert to message string |
| `any` | Control | **Grammar** | Wildcard modifier, not flow control |
| `of` | Control | **Grammar** | Type connector ("set of string") |
| `into` | Action | **Grammar** | Capture preposition, connective role |
| `initial` | Control | **Grammar** | State modifier — lighter weight, same as `nullable`/`default` |

**`BuildSemanticTypeMap()` reclassification:**

| `TokenCategory` | Current → New semantic type |
|------------------|-----------------------------|
| Control | `keyword` → `preceptKeywordSemantic` |
| Declaration | `keyword` → `preceptKeywordSemantic` |
| Action | `keyword` → `preceptKeywordSemantic` |
| Outcome | `keyword` → `preceptKeywordSemantic` |
| Grammar | *(new)* → `preceptKeywordGrammar` |
| Type | `type` → `preceptType` |
| Literal | `keyword` → `preceptValue` |
| Operator | `operator` → `preceptKeywordGrammar` |
| Punctuation (Arrow) | `operator` → `preceptKeywordGrammar` |

Every category now maps to exactly one shade — no per-token overrides needed.

**Affected files:**

| File | Change |
|------|--------|
| `PreceptToken.cs` | Add `Grammar` to enum, re-tag 9 tokens |
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
| After `precept` | `type` → **`preceptMessage`** (gold — the contract identity) |
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

#### E. Extension `package.json` — `semanticTokenColors` and Type Declarations

**✅ Decided.** Mechanical — follows directly from A and B. Comments are TextMate-only (tokenizer strips them), so comment color is handled entirely in the TextMate fallback (section F).

Add the custom semantic token type contributions and color mappings:

```jsonc
"contributes": {
  "semanticTokenTypes": [
    { "id": "preceptKeywordSemantic", "description": "Precept behavioral structure keyword" },
    { "id": "preceptKeywordGrammar",  "description": "Precept connective grammar keyword" },
    { "id": "preceptState",           "description": "Precept state name" },
    { "id": "preceptEvent",           "description": "Precept event name" },
    { "id": "preceptFieldName",       "description": "Precept field or argument name" },
    { "id": "preceptType",            "description": "Precept type keyword" },
    { "id": "preceptValue",           "description": "Precept literal value" },
    { "id": "preceptMessage",         "description": "Precept rule message string" }
  ],
  "semanticTokenModifiers": [
    { "id": "preceptConstrained", "description": "Token is under constraint/invariant pressure" }
  ],
  "configurationDefaults": {
    "editor.semanticTokenColorCustomizations": {
      "[*]": {
        "rules": {
          "preceptKeywordSemantic": { "foreground": "#4338CA", "bold": true },
          "preceptKeywordGrammar":  { "foreground": "#6366F1" },
          "preceptState":           { "foreground": "#A898F5" },
          "preceptEvent":           { "foreground": "#30B8E8" },
          "preceptFieldName":       { "foreground": "#B0BEC5" },
          "preceptType":            { "foreground": "#9AA8B5" },
          "preceptValue":           { "foreground": "#84929F" },
          "preceptMessage":         { "foreground": "#FBBF24" },
          "preceptState:preceptConstrained":     { "italic": true },
          "preceptEvent:preceptConstrained":     { "italic": true },
          "preceptFieldName:preceptConstrained": { "italic": true }
        }
      }
    }
  }
}
```

#### F. TextMate Grammar — Fallback Color Anchoring

**✅ Decided.** Strings default to slate (Data · Values) in TextMate fallback. Message strings flash slate→gold briefly when semantic tokens load. This keeps non-message strings (more common in default values) correct immediately.

The TextMate grammar already assigns specific scopes. To lock fallback colors before the language server loads, add `tokenColorCustomizations` in `configurationDefaults`:

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
      { "scope": "comment.line.number-sign.precept",       "settings": { "foreground": "#6B7280", "fontStyle": "italic" } }
    ]
  }
}
```

**Note:** TextMate cannot distinguish Structure · Semantic from Structure · Grammar for keywords that share the `keyword.other.precept` scope. This is a known limitation — the semantic tokens layer handles the split when the language server is active. For the TextMate fallback, all keywords get Structure · Semantic (`#4338CA` bold) as the safer default. Grammar-role keywords (`as`, `with`, `default`, etc.) will briefly appear bold until semantic tokens load and correct them. This could be improved by splitting `keyword.other.precept` into dedicated scopes for grammar keywords.

#### G. String Literal Context — Messages vs. Plain Strings

**✅ Decided.** Mechanical — uses existing `previousKind` tracking.

The locked palette assigns `#FBBF24` (gold) to rule message strings (in `because` / `reject`) but treats other string contexts as Data · Values (`#84929F`). The current handler emits all `StringLiteral` tokens as `"string"` without context.

**Required change:** Track whether a string literal follows a `because` or `reject` keyword. If so, emit `preceptMessage`; otherwise emit `preceptValue`.

## Implementation Plan

See `docs/SyntaxHighlightingImplementationPlan.md` for the phased execution plan (9 phases, checkpoints, prompts, and status tracker).

## Risks and Open Questions

### R1: `configurationDefaults` scope precedence — ✅ Accepted

`editor.semanticTokenColorCustomizations` in `configurationDefaults` sets defaults that the user can override in their `settings.json`. This is the correct behavior for most extensions. However, the brand spec says "no user-facing palette overrides." True enforcement would require injecting colors at the decoration level (via `createTextEditorDecorationType`), which is significantly more complex. **Decision:** Accept `configurationDefaults` — users who override are power users making a deliberate choice.

### R2: Theme interaction — ✅ Accepted

The `[*]` selector applies to all themes. On light themes, the dark-mode palette will have poor contrast against a light background. Restricting to dark themes would leave light-theme users with zero Precept-specific coloring. **Decision:** Apply to all themes. Light-theme contrast is a known limitation for v1.

### R3: Constraint detection accuracy — ✅ Accepted

Italic requires knowing which states/events/fields are constrained. This depends on a successful parse. For files with syntax errors, the constraint sets may be incomplete or absent, causing italic to flicker as the user types. **Decision:** Fail open — if the parse is incomplete, skip italic rather than showing stale data. Italic is a progressive enhancement. Deferred to Phase 7.

### R4: Bold in semantic tokens — ✅ Accepted

VS Code semantic token rules support `"bold": true` in `settings.json` / `configurationDefaults`, but this is applied via CSS `font-weight`. The actual rendering depends on whether the editor font has a bold variant. Inconsolata (the brand font) supports variable weight 400–900, so bold will render correctly if the user has Inconsolata configured. For users with other fonts, bold is best-effort. **Decision:** Acceptable — bold is a brand signal, not a correctness requirement.

### R5: TextMate keyword scope granularity — ✅ Accepted

The current grammar uses `keyword.other.precept` for both semantic and grammar keywords. Until Phase 8, TextMate fallback cannot distinguish Structure · Semantic (bold `#4338CA`) from Structure · Grammar (normal `#6366F1`). All keywords will appear as `#4338CA` bold in the 1–2 second cold-start window before semantic tokens load. **Decision:** Acceptable for v1. Phase 8 can add `keyword.other.grammar.precept` later if the flash is noticeable.

### R6: Dual-category tokens — ✅ Accepted

Some tokens have multiple `[TokenCategory]` attributes (e.g., `set` is both `Action` and `Type`). `GetCategory()` returns only the primary (first) attribute. The current design maps the primary category. If a dual-role token needs different colors in different syntactic positions, the handler would need context-aware logic beyond the category map. **Decision:** Already handled implicitly — `set` as a keyword gets Structure · Semantic from its Action category; `set` as a collection type in a field declaration gets Data · Types from the Type category. The tokenizer disambiguates based on position. No change needed.
