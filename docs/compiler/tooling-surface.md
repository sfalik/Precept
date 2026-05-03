# Tooling Surface

## 1. Status

| Property | Value |
|---|---|
| Doc maturity | Full |
| Implementation state | Grammar is hand-crafted (designed: catalog-driven generation); semantic tokens Pass 1 implemented, Pass 2 blocked on TypeChecker; completions partially implemented; hover partially implemented |
| Source | `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` (currently hand-crafted), `tools/Precept.LanguageServer/` |
| Upstream | Catalog metadata (Tokens, Types, Constructs, Operators, Actions, Modifiers) |
| Downstream | VS Code syntax highlighting, LS semantic tokens, LS completions, LS hover |

**Implementation Note:** The stub document stated "Grammar generator implemented" — this is aspirational. The current `precept.tmLanguage.json` (23.9KB) is **hand-crafted** with patterns like `machineDeclaration`, `stateDeclaration`, `fieldCollectionDeclaration` that do not derive from catalog metadata. The grammar generator does not exist. This document describes the **designed** state where all editor artifacts derive from catalogs.

---

## 2. Overview

The Tooling Surface is a **cross-cutting layer** that projects catalog metadata into all editor-facing artifacts. It is NOT a pipeline stage — it runs at two execution times:

1. **Build time:** Grammar generation reads catalogs and emits `precept.tmLanguage.json`.
2. **Request time:** The language server uses catalog metadata to produce completions, hover text, and semantic token classifications on every LSP request.

The central architectural principle: **every editor-facing artifact derives from the same catalogs that drive the compiler and runtime.** Syntax highlighting cannot disagree with actual parse behavior because both derive from the same `Tokens.All` metadata.

### Core Surfaces

| Surface | Execution Time | Input | Output |
|---|---|---|---|
| TextMate grammar | Build (extension `npm run` pipeline) | `Tokens`, `Types`, `Operators` catalogs | `precept.tmLanguage.json` |
| Semantic tokens | LSP request | `TokenStream` + `TokenMeta`, `SemanticIndex` | LSP `SemanticTokens` response |
| Completions | LSP request | Catalogs + `SyntaxTree` cursor context + `SemanticIndex` | LSP `CompletionItem[]` response |
| Hover | LSP request | `SemanticIndex` + catalog documentation | LSP `Hover` response |

### VS Code Extension Integration

The extension (`tools/Precept.VsCode/`) bridges the tooling surface to the editor:

1. **Grammar registration:** `package.json` declares the TextMate grammar for the `precept` language. The grammar file is a build output.
2. **Language server hosting:** Launches the LS in dev-build or bundled mode.
3. **Preview webview panel:** Custom `precept.openPreview` command for live precept inspection.
4. **Status bar item:** Shows LS mode (dev/bundled) and active capabilities.

---

## 3. Responsibilities and Boundaries

**OWNS:**

| Responsibility | Description |
|---|---|
| TextMate grammar generation | Build-time tool that reads catalogs and emits `precept.tmLanguage.json` |
| Semantic token classification | Two-pass design: lexical (Pass 1) + semantic (Pass 2) |
| Completion candidate derivation | Catalog-driven completions filtered by `SlotContext` |
| Hover text assembly | Markdown hover from catalog documentation and `SemanticIndex` symbols |
| VS Code extension hosting | Grammar registration, LS launch modes, preview panel |

**Does NOT OWN:**

| Boundary | Owner | Why |
|---|---|---|
| Semantic resolution | TypeChecker | Tooling surface reads `SemanticIndex`, never produces it |
| Diagnostic production | Pipeline stages | Tooling surface reads `Compilation.Diagnostics`, never produces them |
| Preview/inspect runtime | Evaluator | Preview panel dispatches to LS `precept/preview`, which calls evaluator |
| Catalog definitions | `src/Precept/Language/` | Tooling surface is a consumer of catalogs, not a producer |
| LSP protocol handling | Language server | Tooling surface provides data; LS handles protocol |

**Integration Boundary:** The tooling surface never synthesizes language knowledge. If a feature requires knowing "what tokens are valid here," the answer comes from a catalog (`TokenMeta.ValidAfter`, `ConstructMeta.Slots`, `ModifierMeta.ApplicableTo`). If a feature requires knowing "what does this identifier mean," the answer comes from `SemanticIndex`.

---

## 4. Right-Sizing

The tooling surface is intentionally **thin** — a projection layer, not a reasoning layer. All intelligence lives in catalogs. This is a non-negotiable architectural constraint.

### Metrics

| Metric | Grammar Generator | LS Features | Rationale |
|---|---|---|---|
| Estimated LOC | 200–400 | 300–500 | Projection + formatting, no semantic logic |
| Semantic logic | 0% | 0% | All language knowledge in catalogs or SemanticIndex |
| Catalog access | Read-only | Read-only | Never modifies catalog data |
| Language knowledge encoded | 0 lines | 0 lines | All derives from upstream |

### The "One Atom" Test

When a new token is added to `Tokens.All`:
- It **automatically** appears in syntax highlighting (grammar generation derives from catalog)
- It **automatically** appears in completions (completion provider queries catalog)
- It **automatically** appears in MCP vocabulary (`precept_language` tool queries catalog)
- It **automatically** gets semantic token classification (LS reads `TokenMeta.SemanticTokenType`)

If any of these require manual tooling changes, the design is violated. The single atomic act of adding a catalog entry propagates to every surface.

### Anti-Patterns

| Anti-Pattern | Example | Correct Alternative |
|---|---|---|
| Hardcoded keyword lists in LS | `if (token == "state" \|\| token == "event")` | `Tokens.GetMeta(kind).Categories.Contains(TokenCategory.Declaration)` |
| Pattern lists not derived from catalogs | Grammar with `"match": "\\b(state\|event\|field)\\b"` as manual edit | Generate regex from `Tokens.All.Where(t => t.Categories.Contains(Cat))` |
| Completion logic encoding language rules | `if (prevToken == "as") yield return "string"` | Query `Types.All` filtered by `TokenMeta.ValidAfter` |

---

## 5. Inputs and Outputs

### Grammar Generation (Build Time)

```
┌──────────────────────────────────────────────────────────────────────┐
│                    Grammar Generation Pipeline                        │
├──────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  ┌─────────────┐   ┌─────────────┐   ┌─────────────┐                │
│  │ Tokens.All  │   │ Types.All   │   │ Operators   │                │
│  │ .GetMeta()  │   │ .GetMeta()  │   │ .All        │                │
│  └──────┬──────┘   └──────┬──────┘   └──────┬──────┘                │
│         │                 │                 │                        │
│         └────────────────┬┴─────────────────┘                        │
│                          │                                           │
│                          ▼                                           │
│              ┌───────────────────────┐                               │
│              │   Grammar Generator   │                               │
│              │   (build-time tool)   │                               │
│              └───────────┬───────────┘                               │
│                          │                                           │
│                          ▼                                           │
│              ┌───────────────────────┐                               │
│              │ precept.tmLanguage    │                               │
│              │ .json                 │                               │
│              └───────────────────────┘                               │
│                                                                      │
└──────────────────────────────────────────────────────────────────────┘
```

**Input Catalogs:**

| Catalog | What it contributes |
|---|---|
| `Tokens.All` | All keywords, operators, punctuation → `TokenMeta.TextMateScope` becomes grammar pattern |
| `Types.All` | Type keywords (`string`, `integer`, `money`, etc.) → patterns in `typeKeywords` repository |
| `Operators.All` | Operator symbols → patterns in `operators` repository |

> **Open Question (unresolved):** Grammar input catalog list may be incomplete. `catalog-system.md` lists "Constructs → Tokens → Types" but `Constructs.All` is not here. Do `Modifiers.All` and `Actions.All` keywords get highlighting via their `Token` references in `Tokens.All`?

**Output:**

- **File:** `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json`
- **Format:** TextMate grammar JSON with `$schema`, `name`, `scopeName`, `patterns`, and `repository` sections
- **Header:** Generated file includes `// AUTO-GENERATED — do not edit. Run 'npm run generate-grammar' to regenerate.`

### LS Features (Request Time)

**Semantic Tokens:**

| Input | Output |
|---|---|
| `Compilation.TokenStream` + `TokenMeta.SemanticTokenType` | Pass 1: lexical token classifications |
| `Compilation.SemanticIndex` | Pass 2: identifier classifications (blocked on TypeChecker) |

**Completions:**

| Input | Output |
|---|---|
| Cursor position + `SyntaxTree.FindAt(position)` | `SlotContext` identifying expected value kind |
| `SlotContext` + catalog query | `CompletionItem[]` filtered by context |
| `SemanticIndex` (when available) | Declared state/event/field names for references |

**Hover:**

| Input | Output |
|---|---|
| Cursor position → token kind | Keyword hover from `TokenMeta.Description` |
| Cursor position → `SemanticIndex` symbol | Symbol hover from resolved type + modifiers |

---

## 6. Architecture

### Overall System Architecture

```
┌─────────────────────────────────────────────────────────────────────────────────────┐
│                              Tooling Surface Architecture                            │
├─────────────────────────────────────────────────────────────────────────────────────┤
│                                                                                     │
│  ┌─────────────────────────────────────────────────────────────────────────────┐   │
│  │                            Catalog Metadata Layer                            │   │
│  │  ┌─────────┐ ┌─────────┐ ┌───────────┐ ┌─────────┐ ┌─────────┐ ┌─────────┐ │   │
│  │  │ Tokens  │ │ Types   │ │ Operators │ │ Actions │ │Modifiers│ │Constructs││   │
│  │  │ .All    │ │ .All    │ │ .All      │ │ .All    │ │ .All    │ │ .All    ││   │
│  │  └────┬────┘ └────┬────┘ └─────┬─────┘ └────┬────┘ └────┬────┘ └────┬────┘│   │
│  └───────┼───────────┼───────────┼────────────┼───────────┼───────────┼─────┘   │
│          │           │           │            │           │           │          │
│  ════════╪═══════════╪═══════════╪════════════╪═══════════╪═══════════╪══════    │
│  BUILD   │           │           │            │           │           │          │
│  TIME    ▼           ▼           ▼            │           │           │          │
│      ┌───────────────────────────────────┐   │           │           │          │
│      │       Grammar Generator           │   │           │           │          │
│      │  (npm run generate-grammar)       │   │           │           │          │
│      └─────────────────┬─────────────────┘   │           │           │          │
│                        │                      │           │           │          │
│                        ▼                      │           │           │          │
│      ┌───────────────────────────────────┐   │           │           │          │
│      │   precept.tmLanguage.json         │   │           │           │          │
│      │   (build output, do not edit)     │   │           │           │          │
│      └───────────────────────────────────┘   │           │           │          │
│                                               │           │           │          │
│  ════════════════════════════════════════════╪═══════════╪═══════════╪══════    │
│  REQUEST                                      │           │           │          │
│  TIME                                         ▼           ▼           ▼          │
│                        ┌─────────────────────────────────────────────────────┐   │
│                        │              Language Server                        │   │
│                        │  ┌────────────────┐  ┌────────────────┐            │   │
│                        │  │ Completions    │  │ Semantic       │            │   │
│                        │  │ (catalog query)│  │ Tokens (2-pass)│            │   │
│                        │  └────────────────┘  └────────────────┘            │   │
│                        │  ┌────────────────┐  ┌────────────────┐            │   │
│                        │  │ Hover (catalog │  │ Preview        │            │   │
│                        │  │ + SemanticIndex│  │ (evaluator)    │            │   │
│                        │  └────────────────┘  └────────────────┘            │   │
│                        └─────────────────────────────────────────────────────┘   │
│                                               │                                   │
│                                               ▼                                   │
│                        ┌─────────────────────────────────────────────────────┐   │
│                        │              VS Code Extension                       │   │
│                        │  ┌────────────────┐  ┌────────────────┐            │   │
│                        │  │ Grammar        │  │ Preview Panel  │            │   │
│                        │  │ Registration   │  │ (webview)      │            │   │
│                        │  └────────────────┘  └────────────────┘            │   │
│                        │  ┌────────────────┐  ┌────────────────┐            │   │
│                        │  │ LS Launch      │  │ Status Bar     │            │   │
│                        │  │ (dev/bundled)  │  │ Item           │            │   │
│                        │  └────────────────┘  └────────────────┘            │   │
│                        └─────────────────────────────────────────────────────┘   │
│                                                                                   │
└─────────────────────────────────────────────────────────────────────────────────────┘
```

### VS Code Extension Launch Modes

The extension hosts the language server in one of two modes:

**Dev-Build Shadow Copy Mode:**
- Watches `temp/dev-language-server/bin/Precept.LanguageServer/debug/Precept.LanguageServer.dll`
- On DLL change (after `dotnet build`): auto-restarts LS with 500ms debounce
- Shadow-copies runtime to `temp/dev-language-server/runtime/run-{timestamp}-{seq}/` to avoid file locking
- Status bar shows `$(beaker)` icon indicating dev mode

**Bundled Mode:**
- Uses `server/Precept.LanguageServer.dll` from extension directory
- Used in production VSIX builds
- Status bar shows no special icon

**Launch mode selection:** Extension checks whether dev-build DLL exists at startup. If found and `.csproj` is resolvable, uses dev mode; otherwise falls back to bundled.

```typescript
// From extension.ts — launch configuration resolution
const devLanguageServerRootRelativePath = "temp/dev-language-server";
const devLanguageServerBuildDllRelativePath = 
  "temp/dev-language-server/bin/Precept.LanguageServer/debug/Precept.LanguageServer.dll";
const bundledServerRelativePath = "server/Precept.LanguageServer.dll";
```

### Preview Webview Panel

The `precept.openPreview` command opens a webview panel showing a live preview of the active `.precept` file:

| Feature | Mechanism |
|---|---|
| Opens beside editor | `vscode.ViewColumn.Beside` |
| Follows active editor | `onDidChangeActiveTextEditor` subscription (when unlocked) |
| Lock to specific file | `precept.togglePreviewLocking` command |
| Content source | LS `precept/preview` custom command → evaluator inspection |
| Current state | Placeholder ("Coming in v2") — awaiting evaluator integration |

---

## 7. Component Mechanics

This section documents the full mechanics of each tooling surface component.

### 7.1 TextMate Grammar Generation

**Design goal:** The grammar is a **build output**, never hand-edited. The generator reads catalog metadata and emits TextMate JSON.

#### Input Catalog Fields

Each `TokenMeta` in `Tokens.All` carries:

```csharp
// From src/Precept/Language/Token.cs
public sealed record TokenMeta(
    TokenKind                      Kind,
    string?                        Text,           // keyword/operator text ("state", "==")
    IReadOnlyList<TokenCategory>   Categories,     // grouping for grammar repository sections
    string                         Description,
    string?                        TextMateScope,  // e.g., "keyword.control.precept"
    string?                        SemanticTokenType,
    TokenKind[]?                   ValidAfter = null,
    ...
);
```

**Key fields for grammar generation:**

| Field | Purpose |
|---|---|
| `Text` | The literal text to match (`"state"`, `"=="`) |
| `TextMateScope` | The scope name to assign (`keyword.control.precept`, `keyword.operator.comparison.precept`) |
| `Categories` | Grouping for repository sections (`TokenCategory.Declaration`, `TokenCategory.Operator`) |

#### Output Grammar Structure

```json
{
  "$schema": "https://raw.githubusercontent.com/martinring/tmlanguage/master/tmlanguage.json",
  "name": "Precept",
  "scopeName": "source.precept",
  "patterns": [
    { "include": "#comment" },
    { "include": "#strings" },
    { "include": "#declarationKeywords" },
    { "include": "#controlKeywords" },
    { "include": "#typeKeywords" },
    { "include": "#actionKeywords" },
    { "include": "#outcomeKeywords" },
    { "include": "#operators" },
    { "include": "#literals" },
    { "include": "#identifiers" }
  ],
  "repository": {
    "declarationKeywords": {
      "patterns": [
        {
          "name": "keyword.declaration.precept",
          "match": "\\b(precept|field|state|event|rule|ensure|as|default|optional|writable|because|initial)\\b"
        }
      ]
    },
    "controlKeywords": { ... },
    "typeKeywords": { ... },
    ...
  }
}
```

#### Generation Algorithm

```
FOR EACH TokenCategory c IN [Declaration, Control, Type, Action, Outcome, Operator, ...]:
  keywords ← Tokens.All.Where(t => t.Categories.Contains(c) && t.Text != null)
  
  IF keywords.Any():
    pattern ← keywords.Select(k => Regex.Escape(k.Text)).OrderByDescending(Length).Join("|")
    scope ← keywords.First().TextMateScope  // all in category share scope
    
    EMIT repository entry:
      name: categoryToRepoName(c)
      patterns: [{ name: scope, match: "\\b(" + pattern + ")\\b" }]
```

> **Open Question (unresolved):** `scope ← keywords.First().TextMateScope` assumes all tokens in a category share the same scope. Should the catalog assert this invariant, or should the algorithm group by scope within category?

#### Build Integration

The grammar generator runs as part of the extension build:

```bash
# From package.json scripts
npm run generate-grammar  # Runs generator, outputs to syntaxes/precept.tmLanguage.json
npm run compile           # TypeScript compilation (after grammar generation)
npm run package:local     # Package VSIX
```

> **Open Question (unresolved):** The grammar generator does not currently exist. Should it be:
> - A TypeScript script (`tools/Precept.VsCode/scripts/generate-grammar.ts`) that imports catalog data from a JSON export?
> - A .NET tool (`dotnet run --project tools/Precept.GrammarGenerator`) that reads catalogs directly?
> 
> The TypeScript approach requires a catalog export step; the .NET approach is more direct but adds build complexity.

### 7.2 Semantic Token Two-Pass Design

Semantic tokens enable rich highlighting beyond TextMate patterns. Precept uses a **two-pass design**:

#### Pass 1: Lexical Tokens (from TokenStream)

**Trigger:** `textDocument/semanticTokens/full`

**Input:** `Compilation.TokenStream` + `Tokens.GetMeta(kind).SemanticTokenType`

**Mechanics:**

```csharp
SemanticTokens BuildLexicalTokens(Compilation compilation)
{
    var builder = new SemanticTokensBuilder();
    
    foreach (var token in compilation.TokenStream)
    {
        var meta = Tokens.GetMeta(token.Kind);
        if (meta.SemanticTokenType is null) continue;  // structural tokens (NewLine, EndOfSource)
        
        builder.Push(
            line: token.Span.StartLine,
            character: token.Span.StartColumn,
            length: token.Span.Length,
            tokenType: meta.SemanticTokenType,   // "keyword", "type", "operator", etc.
            tokenModifiers: "");
    }
    
    return builder.Build();
}
```

**SemanticTokenType values in catalog:**

| TokenCategory | SemanticTokenType | Examples |
|---|---|---|
| Declaration | `keyword` | `precept`, `field`, `state`, `event` |
| Control | `keyword` | `when`, `if`, `then`, `else` |
| Type | `type` | `string`, `integer`, `money`, `set` |
| Action | `keyword` | `set`, `add`, `remove`, `enqueue` |
| Outcome | `keyword` | `transition`, `reject`, `no` |
| LogicalOperator | `operator` | `and`, `or`, `not` |
| Operator | `operator` | `==`, `!=`, `>=`, `+`, `-` |
| Constraint | `decorator` | `nonnegative`, `positive`, `min`, `max` |
| StateModifier | `modifier` | `initial`, `terminal`, `required` |
| Literal | `number`, `string` | numeric/string literals |
| Identifier | *(deferred to Pass 2)* | field names, state names |

#### Pass 2: Identifier Tokens (from SemanticIndex)

**Trigger:** `textDocument/semanticTokens/full` (merged with Pass 1)

**Input:** `Compilation.SemanticIndex` symbol/reference bindings

**Mechanics:**

Pass 2 **overlays** Pass 1 results — identifier tokens that Pass 1 skipped get classified by their semantic role:

```csharp
void AddIdentifierTokens(SemanticTokensBuilder builder, SemanticIndex index)
{
    // Field declarations and references
    foreach (var field in index.Fields)
    {
        builder.Push(field.Syntax.NameSpan, "property", "declaration");
    }
    foreach (var fieldRef in index.FieldReferences)
    {
        builder.Push(fieldRef.Span, "property", "");
    }
```

> **Open Question (unresolved):** `SemanticIndex.FieldReferences`, `.StateReferences`, `.EventReferences`, and `.EventArgs` do not exist in type-checker.md §7.1. Should the type checker emit reference-site arrays, or should Pass 2 reconstruct reference sites by walking typed declarations?

```csharp
    // State declarations and references
    foreach (var state in index.States)
    {
        builder.Push(state.Syntax.NameSpan, "enumMember", "declaration");
    }
    foreach (var stateRef in index.StateReferences)
    {
        builder.Push(stateRef.Span, "enumMember", "");
    }
    
    // Event declarations and references
    foreach (var evt in index.Events)
    {
        builder.Push(evt.Syntax.NameSpan, "function", "declaration");
    }
    foreach (var eventRef in index.EventReferences)
    {
        builder.Push(eventRef.Span, "function", "");
    }
    
    // Event arguments
    foreach (var arg in index.EventArgs)
    {
        builder.Push(arg.Span, "parameter", "");
    }
}
```

**Graceful Degradation:** If TypeChecker fails (compilation has errors), Pass 2 is skipped. The editor still gets Pass 1 — all keywords, types, operators, and literals are highlighted correctly. Only identifier classification degrades.

**Implementation Note:** Pass 2 is blocked until TypeChecker implementation produces `SemanticIndex`.

### 7.3 Completion Design

Completions are **catalog-driven** — the LS identifies the cursor context (slot kind), then queries the appropriate catalog for valid suggestions.

#### Step 1: Identify Cursor Context (SlotContext)

```csharp
enum SlotContext
{
    TopLevel,           // Between constructs at file level
    AfterKeyword,       // After declaration keyword, expecting identifier
    InTypePosition,     // After 'as', expecting type
    InModifierPosition, // In modifier zone, expecting constraint/modifier
    InStateTarget,      // After 'from'/'in'/'to', expecting state name
    InEventTarget,      // After 'on', expecting event name
    InFieldTarget,      // In action, expecting field name
    InActionVerb,       // After '->', expecting action keyword
    InExpression,       // Inside guard/compute/ensure expression
    InArgDefault,       // Default value position in event arg
}

> **Open Question (unresolved):** `SlotContext` is defined here and in `language-server.md` §7.3. This maps `ConstructSlotKind` values; language-server.md maps `SlotKind`. Which document is canonical, and are these the same enum under different names?

SlotContext GetCursorContext(ConstructManifest manifest, Position position)
{
    var construct = manifest.FindConstructAt(position);
    if (construct is null) return SlotContext.TopLevel;
    
    var slotIndex = construct.FindSlotAt(position);
    if (slotIndex < 0) return SlotContext.AfterKeyword;
    
    var slotKind = Constructs.GetMeta(construct.Kind).Slots[slotIndex].Kind;
    
    return slotKind switch
    {
        ConstructSlotKind.TypeExpression => SlotContext.InTypePosition,
        ConstructSlotKind.ModifierList => SlotContext.InModifierPosition,
        ConstructSlotKind.StateTarget or ConstructSlotKind.StateEntryList 
            => SlotContext.InStateTarget,
        ConstructSlotKind.EventTarget => SlotContext.InEventTarget,
        ConstructSlotKind.FieldTarget => SlotContext.InFieldTarget,
        ConstructSlotKind.ActionChain => SlotContext.InActionVerb,
        ConstructSlotKind.GuardClause or ConstructSlotKind.ComputeExpression 
            or ConstructSlotKind.EnsureClause or ConstructSlotKind.RuleExpression
            => SlotContext.InExpression,
        _ => SlotContext.AfterKeyword
    };
}
```

#### Step 2: Query Catalog for Context

| SlotContext | Catalog Query | Filter |
|---|---|---|
| `TopLevel` | `Constructs.All` | `RoutingFamily != StateScoped` (top-level constructs only) |
| `InTypePosition` | `Types.All` | `IsUserFacing == true` |
| `InModifierPosition` | `Modifiers.All` | `ApplicableTo` matches current construct + field type |
| `InStateTarget` | `SemanticIndex.States` | Declared state names |
| `InEventTarget` | `SemanticIndex.Events` | Declared event names |
| `InFieldTarget` | `SemanticIndex.Fields` | Declared field names |
| `InActionVerb` | `Actions.All` | `ApplicableTo` matches target field type |
| `InExpression` | `SemanticIndex.Fields` + `Functions.All` | Field names, function names |

```csharp
IEnumerable<CompletionItem> GetCompletions(SlotContext context, SemanticIndex? index)
{
    return context switch
    {
        SlotContext.TopLevel => GetConstructLeaderCompletions(),
        SlotContext.InTypePosition => GetTypeCompletions(),
        SlotContext.InModifierPosition => GetModifierCompletions(GetCurrentFieldType()),
        SlotContext.InStateTarget => GetStateCompletions(index),
        SlotContext.InEventTarget => GetEventCompletions(index),
        SlotContext.InFieldTarget => GetFieldCompletions(index),
        SlotContext.InActionVerb => GetActionCompletions(GetCurrentFieldType()),
        SlotContext.InExpression => GetExpressionCompletions(index),
        _ => []
    };
}

IEnumerable<CompletionItem> GetTypeCompletions()
{
    return Types.All
        .Where(t => t.IsUserFacing)
        .Select(t => new CompletionItem
        {
            Label = t.Token.Text!,
            Kind = CompletionItemKind.TypeParameter,
            Documentation = t.HoverDescription,
            InsertText = t.SnippetTemplate ?? t.Token.Text,
            InsertTextFormat = t.SnippetTemplate is not null 
                ? InsertTextFormat.Snippet 
                : InsertTextFormat.PlainText
        });
}
```

#### ValidAfter-Based Filtering

`TokenMeta.ValidAfter` carries predecessor token sets for context-sensitive filtering:

```csharp
// From Tokens.cs
private static readonly TokenKind[] VA_TypeRef = [TokenKind.As, TokenKind.Of];
private static readonly TokenKind[] VA_AfterArrow = [TokenKind.Arrow];
private static readonly TokenKind[] VA_FieldModifier = [
    TokenKind.StringType, TokenKind.BooleanType, TokenKind.IntegerType,
    TokenKind.DecimalType, TokenKind.NumberType, ...
];
```

The completion provider uses this to filter candidates:

```csharp
IEnumerable<CompletionItem> GetFilteredCompletions(TokenKind predecessorKind)
{
    return Tokens.All
        .Where(t => t.ValidAfter is null || t.ValidAfter.Contains(predecessorKind))
        .Where(t => t.Text is not null)
        .Select(ToCompletionItem);
}
```

### 7.4 Hover Design

Hover content is assembled from catalog documentation and `SemanticIndex` symbols.

#### Keyword Hover

For keywords, hover comes directly from `TokenMeta`:

```csharp
Hover? GetKeywordHover(Token token)
{
    var meta = Tokens.GetMeta(token.Kind);
    if (meta.Text is null) return null;  // synthetic token
    
    return new Hover
    {
        Contents = new MarkupContent
        {
            Kind = MarkupKind.Markdown,
            Value = $"**{meta.Text}** — {meta.Description}"
        },
        Range = ToLspRange(token.Span)
    };
}
```

#### Identifier Hover

For identifiers, hover comes from `SemanticIndex` resolved symbols:

```csharp
Hover? GetIdentifierHover(Position position, SemanticIndex index)
{
    var symbol = index.FindSymbolAt(position);
    if (symbol is null) return null;
    
    var content = symbol switch
    {
        TypedField f => FormatFieldHover(f),
        TypedState s => FormatStateHover(s),
        TypedEvent e => FormatEventHover(e),
        TypedArg a => FormatArgHover(a),
        _ => null
    };
    
    if (content is null) return null;
    
    return new Hover
    {
        Contents = new MarkupContent { Kind = MarkupKind.Markdown, Value = content },
        Range = ToLspRange(symbol.Syntax.NameSpan)
    };
}

string FormatFieldHover(TypedField field)
{
    var sb = new StringBuilder();
    sb.AppendLine($"**{field.Name}**: {FormatType(field.Type)}");
    
    if (field.Modifiers.Any())
        sb.AppendLine($"*Modifiers:* {string.Join(", ", field.Modifiers.Select(m => m.Token.Text))}");
    
    if (field.InitialValue is not null)
        sb.AppendLine($"*Default:* `{field.InitialValue}`");
    
    return sb.ToString();
}

string FormatStateHover(TypedState state)
{
    var modifiers = state.Modifiers.Any() 
        ? string.Join(" ", state.Modifiers.Select(m => m.Token.Text)) 
        : "";
    return $"**state** {state.Name} {modifiers}".Trim();
}

string FormatEventHover(TypedEvent evt)
{
    var args = evt.Args.Any()
        ? $"({string.Join(", ", evt.Args.Select(a => $"{a.Name}: {FormatType(a.Type)}"))})"
        : "";
    return $"**event** {evt.Name}{args}";
}
```

---

## 8. Dependencies and Integration Points

### Upstream Dependencies

| Dependency | Layer | What Tooling Surface Reads |
|---|---|---|
| `Tokens.All` | Catalog | `TokenMeta.TextMateScope`, `SemanticTokenType`, `ValidAfter`, `Text`, `Description` |
| `Types.All` | Catalog | `TypeMeta.Token`, `HoverDescription`, `SnippetTemplate`, `IsUserFacing` |
| `Operators.All` | Catalog | `OperatorMeta.Token`, `Precedence`, `Associativity` |
| `Actions.All` | Catalog | `ActionMeta.Token`, `ApplicableTo`, `HoverDescription` |
| `Modifiers.All` | Catalog | `ModifierMeta.Token`, `ApplicableTo`, `HoverDescription` |
| `Constructs.All` | Catalog | `ConstructMeta.Slots`, `LeadingTokens`, `RoutingFamily` |
| `TokenStream` | Lexer | Token sequence for Pass 1 semantic tokens |
| `SyntaxTree` | Parser | Cursor context for completions |
| `SemanticIndex` | TypeChecker | Resolved symbols for Pass 2, identifier completions, hover |
| `Precept` (runtime) | Builder | Preview/inspect via LS `precept/preview` |

### Downstream Consumers

| Consumer | What It Receives |
|---|---|
| VS Code extension | `precept.tmLanguage.json` (grammar registration) |
| VS Code extension | LS launch configuration (dev/bundled mode) |
| VS Code syntax highlighting | TextMate grammar patterns → token colorization |
| VS Code semantic highlighting | LSP `SemanticTokens` → identifier colorization |
| VS Code IntelliSense | LSP `CompletionItem[]` → completion popup |
| VS Code editor | LSP `Hover` → hover tooltip |
| Preview webview | LS `precept/preview` → inspection rendering |

### Integration Points

```
                      ┌──────────────────────────────────────────┐
                      │            src/Precept/Language/          │
                      │   Tokens · Types · Operators · Actions   │
                      │   Modifiers · Constructs · Functions      │
                      └─────────────────────┬────────────────────┘
                                            │
              ┌─────────────────────────────┼─────────────────────────────┐
              │                             │                             │
              ▼                             ▼                             ▼
    ┌─────────────────────┐     ┌─────────────────────┐     ┌─────────────────────┐
    │  Grammar Generator  │     │   Language Server   │     │    MCP Server       │
    │  (build time)       │     │   (request time)    │     │ precept_language    │
    └──────────┬──────────┘     └──────────┬──────────┘     └──────────┬──────────┘
               │                           │                           │
               ▼                           ▼                           ▼
    ┌─────────────────────┐     ┌─────────────────────┐     ┌─────────────────────┐
    │ tmLanguage.json     │     │ LSP Responses       │     │ JSON vocabulary     │
    └─────────────────────┘     └─────────────────────┘     └─────────────────────┘
```

### Extension ↔ Language Server Protocol

| Direction | Messages |
|---|---|
| Client → Server | `textDocument/didOpen`, `textDocument/didChange`, `textDocument/completion`, `textDocument/hover`, `textDocument/semanticTokens/full` |
| Server → Client | `textDocument/publishDiagnostics`, completion responses, hover responses, semantic token responses |
| Custom | `precept/preview` (client → server → evaluator → JSON response) |

---

## 9. Failure Modes and Recovery

### Grammar Generation Failures

| Failure Mode | Cause | Recovery |
|---|---|---|
| Generator script not found | Missing `npm run generate-grammar` script | Build fails with clear error; no partial output |
| Catalog read failure | .NET build error in `src/Precept/` | Build fails before grammar generation |
| Invalid regex pattern | Token text with unescaped regex metacharacters | Generator escapes all token text via `Regex.Escape()` |
| File write failure | Permission issue or locked file | Build fails; retry after fixing permissions |

**Constraint:** Grammar generation is a build-time tool — failures are build failures, not runtime failures. The extension never runs with a stale grammar because the grammar is regenerated on every build.

### Semantic Token Failures

| Failure Mode | Cause | Recovery |
|---|---|---|
| Pass 1 returns empty | `TokenStream` is empty (source is empty or all whitespace) | Valid empty response — no tokens to classify |
| Pass 2 skipped | `SemanticIndex` is null (TypeChecker not implemented or compilation has errors) | Graceful degradation: Pass 1 lexical tokens still returned |
| Token span out of bounds | Bug in span calculation | Log warning, skip that token, continue |

**Graceful Degradation:** The two-pass design ensures the editor always gets *something*. Pass 1 (lexical) works even when Pass 2 (semantic) cannot run. Keywords, types, operators, and literals are always highlighted.

### Completion Failures

| Failure Mode | Cause | Recovery |
|---|---|---|
| Empty completions | Cursor in unrecognized context | Return empty array — valid "no completions" state |
| `SemanticIndex` unavailable | TypeChecker not implemented or compilation has errors | Return catalog-only completions (keywords, types, actions) — no declared names |
| Invalid cursor position | Position beyond document end | Return empty array |
| Slow catalog query | Shouldn't happen (catalogs are static, in-memory) | If observed, profile and optimize |

### Hover Failures

| Failure Mode | Cause | Recovery |
|---|---|---|
| No hover content | Cursor on whitespace or unknown token | Return null — valid "no hover" state |
| Symbol not found | Identifier token but `SemanticIndex` missing or symbol unresolved | Return null for identifier, keyword hover still works |
| Malformed Markdown | Bug in hover formatter | Escape user-provided content; log if format is invalid |

### Extension Launch Failures

| Failure Mode | Cause | Recovery |
|---|---|---|
| LS not found | Neither dev nor bundled DLL exists | Show error in status bar and output channel; extension activates but LS features are disabled |
| Dev build stale | `dotnet build` not run after code change | File watcher triggers rebuild notification; user runs build task |
| Shadow copy fails | Permission issue or disk full | Log error, fall back to bundled mode |
| LS crash | Bug in LS code | Extension detects process exit, shows error in status bar; user can manually restart |

### Preview Panel Failures

| Failure Mode | Cause | Recovery |
|---|---|---|
| Preview unavailable | LS not running | Show placeholder with "LS not connected" message |
| Inspect request fails | Compilation has errors or evaluator not implemented | Show placeholder with error message |
| Webview disposed | User closed panel | Panel is re-creatable via `precept.openPreview` command |

---

## 10. Contracts and Guarantees

### Grammar Contracts

| Contract | Guarantee |
|---|---|
| **Catalog derivation** | Every pattern in `precept.tmLanguage.json` derives from a catalog entry with `TextMateScope != null`. No patterns exist that are not in a catalog. |
| **Completeness** | Every `TokenKind` in `Tokens.All` with `TextMateScope != null` appears in the generated grammar. |
| **No manual editing** | The grammar file is always overwritten on build. Manual edits are lost. A header comment warns: `// AUTO-GENERATED — do not edit.` |
| **Idempotence** | Running the generator twice with unchanged catalogs produces identical output. |

### Semantic Token Contracts

| Contract | Guarantee |
|---|---|
| **Pass 1 availability** | Pass 1 lexical tokens are always available if `TokenStream` is non-empty. |
| **Pass 2 correctness** | Pass 2 identifier classifications are always backed by a resolved `SemanticIndex` entry — no speculative classifications. |
| **No overlap violations** | Pass 2 overlays Pass 1 for identifier tokens only; non-identifier tokens retain Pass 1 classification. |
| **Graceful degradation** | If Pass 2 cannot run (no `SemanticIndex`), response includes Pass 1 tokens only — never fails entirely. |

### Completion Contracts

| Contract | Guarantee |
|---|---|
| **Catalog source** | Every completion item for keywords/types/actions/modifiers derives from the corresponding catalog's `All` property. |
| **No hardcoded lists** | The LS code contains no hardcoded keyword lists. All come from catalogs. |
| **Context filtering** | Completions are filtered by `SlotContext` — no invalid suggestions (e.g., no type keywords in action position). |
| **ValidAfter honoring** | If `TokenMeta.ValidAfter` is non-null, the token is only suggested after a predecessor in that set. |

### Hover Contracts

| Contract | Guarantee |
|---|---|
| **Keyword source** | Keyword hover text comes from `TokenMeta.Description`. |
| **Symbol source** | Identifier hover text comes from `SemanticIndex` resolved symbol, never synthesized. |
| **Null safety** | Hover returns null for unrecognized positions — never crashes. |

### Extension Launch Contracts

| Contract | Guarantee |
|---|---|
| **Mode selection** | Dev mode is selected if and only if the dev-build DLL exists and the `.csproj` is resolvable. |
| **Shadow copy isolation** | Each dev-build restart uses a fresh shadow copy directory; old copies are pruned. |
| **File locking avoidance** | The running LS uses shadow-copied DLLs, never the build output directly. |
| **Restart debounce** | Multiple rapid file changes trigger at most one restart (500ms debounce). |

---

## 11. Design Rationale and Decisions

### R1: Grammar as Build Output, Not Source

**Decision:** The TextMate grammar is generated from catalog metadata, not hand-authored.

**Rationale:**
- Eliminates drift between syntax highlighting and actual parser behavior
- Adding a keyword to the catalog automatically adds it to highlighting
- No risk of forgetting to update the grammar when changing the language
- The grammar cannot disagree with the parser because both derive from the same `Tokens.All`

**Trade-offs accepted:**
- Requires build step before extension packaging
- Grammar generation tool must be maintained
- Complex constructs (multi-line patterns, nested scopes) may need special handling

### R2: Two-Pass Semantic Tokens

**Decision:** Semantic tokens use two passes — lexical (Pass 1) and semantic (Pass 2).

**Rationale:**
- Pass 1 works immediately with only the lexer — no blocking on TypeChecker
- Pass 2 provides rich identifier classification once TypeChecker is available
- Graceful degradation: Pass 1 always works even when Pass 2 cannot

**Alternatives considered and rejected:**
- Single-pass with SemanticIndex only: Would provide no highlighting until TypeChecker is fully implemented
- Single-pass lexical only: Would never distinguish field names from state names from event names

### R3: Catalog-Driven Completions with ValidAfter

**Decision:** Completions derive from catalogs, filtered by `TokenMeta.ValidAfter` predecessor sets.

**Rationale:**
- Zero-maintenance completions: new language features appear automatically
- No hardcoded keyword lists in LS code
- `ValidAfter` enables context-sensitive suggestions without full parse-state tracking

**Trade-offs accepted:**
- `ValidAfter` must be manually populated for each token (catalog maintenance burden)
- Coarse-grained: `ValidAfter` is token-based, not position-in-grammar-based

### R4: Dev-Build Shadow Copy Mode

**Decision:** In development, the LS DLL is shadow-copied to avoid file locking.

**Rationale:**
- Allows `dotnet build` to overwrite the DLL while LS is running
- File watcher detects change → restarts LS with new build
- No manual extension reinstall or window reload required

**Trade-offs accepted:**
- Shadow copy adds startup latency (~100ms)
- Requires pruning of old shadow copies to avoid disk accumulation
- More complex than direct DLL launch

### R5: Graceful Degradation Over Hard Failure

**Decision:** Tooling features degrade gracefully when upstream artifacts are unavailable.

**Rationale:**
- User always gets *something* useful even during partial implementation
- Pass 1 semantic tokens work without TypeChecker
- Catalog-only completions work without SemanticIndex
- Preview shows placeholder until evaluator is ready

**Consistency with catalog-driven architecture:**
- Catalogs are always available (static, compiled in)
- Dynamic artifacts (SemanticIndex, Precept) may be absent
- Features that only need catalogs always work

---

## 12. Innovation

### I1: Grammar Generation from Catalogs

Traditional DSL tooling maintains TextMate grammars as hand-edited JSON files. Precept generates its grammar from the same catalog metadata the compiler uses.

**Innovation:**
- The grammar is a **build output**, not a source artifact
- Drift between highlighting and actual parsing is **structurally impossible**
- Adding a language feature is one atomic act: add the catalog entry, grammar updates automatically

**No other DSL tooling in this category has this level of surface coherence.**

### I2: Single Source for All Editor Surfaces

Grammar, completions, hover, semantic tokens, and MCP vocabulary all derive from the same catalog definitions:

| Surface | Catalog Source |
|---|---|
| TextMate grammar patterns | `Tokens.All` with `TextMateScope` |
| Completion items | `Types.All`, `Actions.All`, `Modifiers.All`, etc. |
| Hover text | `TokenMeta.Description`, `SemanticIndex` symbols |
| Semantic token types | `TokenMeta.SemanticTokenType` |
| MCP `precept_language` | All 13 catalogs |

**Innovation:** A single source of truth eliminates parallel maintenance and synchronization bugs.

### I3: Zero-Drift Guarantee

The grammar cannot disagree with the parser because both derive from the same `Tokens.All` metadata. This is not a policy — it's a structural impossibility.

**Contrast with traditional tooling:**
- Traditional: Parser has keyword list, grammar has separate keyword regex, LS has third keyword enum → drift possible
- Precept: All three read `Tokens.All` → drift impossible

### I4: Two-Pass Semantic Tokens with Graceful Degradation

The two-pass semantic token design (lexical → semantic) ensures the editor always gets highlighting:

- During TypeChecker development: Pass 1 provides keyword/operator/literal highlighting
- After TypeChecker completion: Pass 2 adds identifier classification
- On compilation errors: Pass 2 is skipped, Pass 1 still works

**Innovation:** Progressive enhancement of editor features as compiler implementation matures.

### I5: Live Reload Development Loop

The dev-build shadow copy mode with file watching creates a tight development loop:

1. Edit LS code
2. Run `dotnet build`
3. File watcher detects DLL change
4. LS auto-restarts with new code
5. No window reload, no extension reinstall

**Innovation:** Sub-second feedback for language server development.

---

## 13. Open Questions / Implementation Notes

### Implementation State vs. Designed State

This document describes the **designed** state. Current implementation differs in several areas:

| Component | Designed State | Current Implementation State |
|---|---|---|
| TextMate grammar | Generated from catalogs | **Hand-crafted** (23.9KB with patterns like `machineDeclaration`, `stateDeclaration`) |
| Grammar generator | TypeScript or .NET tool | **Does not exist** |
| Semantic tokens Pass 1 | Reads `TokenMeta.SemanticTokenType` | Implemented |
| Semantic tokens Pass 2 | Reads `SemanticIndex` | **Blocked on TypeChecker** |
| Completions | Catalog-driven with `SlotContext` | Partially implemented (basic keyword completions) |
| Hover | Catalog + `SemanticIndex` | Partially implemented (keyword hover only) |
| Preview panel | Live inspection via evaluator | **Placeholder only** ("Coming in v2") |

### Open Questions

> **Open Question (unresolved):** Should the grammar generator be a TypeScript script or a .NET tool?
> 
> **TypeScript approach:**
> - Fits naturally in `npm run` build pipeline
> - Requires catalog data export (JSON file or build-time generation)
> - Extension build is self-contained
> 
> **.NET approach:**
> - Can read `Tokens.All` directly from compiled assembly
> - No separate export step
> - Adds .NET dependency to extension build
> 
> Recommendation: Start with .NET tool for directness; consider TypeScript if build complexity becomes problematic.

> **Open Question (unresolved):** How should complex TextMate patterns (multi-line, nested scopes) be represented in catalog metadata?
> 
> The current hand-crafted grammar has patterns like `machineDeclaration` that span multiple captures and reference other patterns. A simple "token → scope" mapping may be insufficient.
> 
> Options:
> 1. Extend `TokenMeta` with optional `TextMatePatternJson` for complex cases
> 2. Generate simple patterns from catalogs, hand-maintain complex patterns in a separate file merged at build time
> 3. Use `Constructs.All` to generate structural patterns with slot-level scopes
> 
> Recommendation: Option 3 aligns best with catalog-driven architecture but requires construct-level grammar generation design.

### Implementation Notes

1. **Grammar file header:** When the generator is implemented, add this comment header to the generated file:
   ```json
   {
     "_comment": "AUTO-GENERATED by tools/Precept.GrammarGenerator — do not edit manually. Run 'npm run generate-grammar' to regenerate.",
     "$schema": "...",
     ...
   }
   ```

2. **CI check:** Add a CI step that regenerates the grammar and fails if it differs from the committed file. This catches manual edits.

3. **Catalog metadata completeness:** Verify all `TokenMeta` entries have appropriate values for:
   - `TextMateScope` (for grammar generation)
   - `SemanticTokenType` (for LS semantic tokens)
   - `ValidAfter` (for completion filtering)
   
   The following token kinds currently have `TextMateScope` populated (verified from `src/Precept/Language/Tokens.cs`):
   - Declaration keywords: `keyword.declaration.precept`
   - Control keywords: `keyword.control.precept`
   - Type keywords: `storage.type.precept`
   - Action keywords: `keyword.other.action.precept`
   - Outcome keywords: `keyword.other.outcome.precept`
   - Access mode keywords: `keyword.other.access-mode.precept`
   - Logical operators: `keyword.operator.logical.precept`
   - Constraint keywords: `keyword.other.constraint.precept`
   - State modifiers: `storage.modifier.state.precept`

4. **Extension package.json already declares semantic token types:**
   ```json
   "semanticTokenTypes": [
     { "id": "preceptComment", "description": "Precept comment" },
     { "id": "preceptKeywordSemantic", "description": "Precept behavioral structure keyword" },
     { "id": "preceptState", "description": "Precept state name" },
     { "id": "preceptEvent", "description": "Precept event name" },
     { "id": "preceptFieldName", "description": "Precept field or argument name" },
     ...
   ]
   ```
   These should be reconciled with `TokenMeta.SemanticTokenType` values when implementing full semantic token support.

5. **Preview panel integration:** The current placeholder shows a "Coming in v2" message. When the evaluator is implemented:
   - Add LS `precept/preview` custom command
   - Call `Precept.InspectFire` / `Precept.InspectUpdate` 
   - Serialize result as JSON for webview rendering
   - Update webview HTML to render the inspection result

---

## 14. Deliberate Exclusions

### E1: No Language Semantics in Editor Code

**Excluded:** Encoding language rules in LS or extension code.

**Rationale:** All language intelligence lives in catalogs. Editor code is projection-only. This ensures:
- No parallel maintenance of language rules
- No risk of editor and compiler disagreeing
- Single atomic change propagates to all surfaces

**Examples of what is excluded:**
- `if (keyword == "state") { /* special handling */ }` in LS code
- Hardcoded arrays of valid keywords in completion providers
- Type-specific hover formatting that encodes type semantics

### E2: No Hand-Editing of Generated Files

**Excluded:** Manual edits to `precept.tmLanguage.json`.

**Rationale:** The grammar is a build output. Manual edits are overwritten on every build. The grammar cannot drift from the catalog because it is regenerated from the catalog.

**Enforcement:**
- Header comment: `// AUTO-GENERATED — do not edit`
- CI check: regenerate and diff, fail if different

### E3: No Speculative Semantic Classifications

**Excluded:** Guessing identifier classifications without `SemanticIndex`.

**Rationale:** Pass 2 semantic tokens only emit classifications that are backed by resolved `SemanticIndex` entries. No heuristic "this looks like a state name" classifications.

**Why this matters:**
- Wrong classifications are worse than missing classifications
- Users learn to trust the highlighting as accurate
- Graceful degradation (no Pass 2) is preferable to incorrect highlighting

### E4: No Diagnostic Synthesis in Editor Code

**Excluded:** LS or extension producing diagnostics.

**Rationale:** Diagnostics are push-only from `Compilation.Diagnostics`. The LS never synthesizes its own diagnostics. This ensures:
- All diagnostics come from the compiler
- No inconsistency between "Problems" panel and actual compilation state
- Diagnostic ownership is clear (pipeline stages produce; LS routes)

### E5: No Runtime Logic in Preview

**Excluded:** Preview panel implementing its own evaluation logic.

**Rationale:** The preview panel dispatches to the LS `precept/preview` command, which calls the evaluator. The webview is a rendering layer only. Evaluation logic lives in `src/Precept/Runtime/`.

### E6: No IDE-Specific Grammar Variants

**Excluded:** Maintaining separate grammars for VS Code, JetBrains, Vim, etc.

**Rationale:** TextMate grammar is the single grammar artifact. Other editors that support TextMate grammars can use the same file. If an editor requires a different format (e.g., JetBrains TextAttributesKey), that's a separate generation target from the same catalog metadata — not a hand-maintained parallel artifact.

---

## 15. Cross-References

| Topic | Document | Section |
|---|---|---|
| Catalog system overview | `docs/language/catalog-system.md` | §Overview, §Pattern Definition |
| Tokens catalog metadata | `docs/language/catalog-system.md` | §2. Meta record |
| Language server features | `docs/tooling/language-server.md` | §7. Component Mechanics |
| Semantic tokens two-pass | `docs/tooling/language-server.md` | §7.2 Semantic Tokens |
| Catalog-driven completions | `docs/tooling/language-server.md` | §7.3 Catalog-Driven Completions |
| Hover implementation | `docs/tooling/language-server.md` | §7.4 Hover |
| MCP `precept_language` tool | `docs/tooling/McpServerDesign.md` | §precept_language |
| VS Code extension details | `tools/Precept.VsCode/package.json` | contributes.grammars, semanticTokenTypes |
| Diagnostic system | `docs/compiler/diagnostic-system.md` | — |
| Runtime evaluator (preview) | `docs/runtime/runtime-api.md` | §Inspection API |

### Related Catalog Files

| Catalog | File | Key Fields for Tooling |
|---|---|---|
| Tokens | `src/Precept/Language/Tokens.cs` | `TextMateScope`, `SemanticTokenType`, `ValidAfter` |
| Types | `src/Precept/Language/Types.cs` | `Token`, `HoverDescription`, `IsUserFacing` |
| Operators | `src/Precept/Language/Operators.cs` | `Token`, `Precedence`, `Associativity` |
| Actions | `src/Precept/Language/Actions.cs` | `Token`, `ApplicableTo`, `HoverDescription` |
| Modifiers | `src/Precept/Language/Modifiers.cs` | `Token`, `ApplicableTo`, `HoverDescription` |
| Constructs | `src/Precept/Language/Constructs.cs` | `Slots`, `LeadingTokens`, `RoutingFamily` |

### Extension Source Files

| File | Purpose |
|---|---|
| `tools/Precept.VsCode/src/extension.ts` | Extension host: LS launch, preview panel, commands |
| `tools/Precept.VsCode/package.json` | VS Code manifest: grammars, semantic token types, commands |
| `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` | TextMate grammar (currently hand-crafted) |
| `tools/Precept.VsCode/language-configuration.json` | Bracket matching, comment toggling |

---

## 16. Source Files

### Build-Time Tooling (Grammar Generation)

| File | Purpose | Status |
|---|---|---|
| `tools/Precept.GrammarGenerator/` | Grammar generator tool (reads catalogs, emits tmLanguage.json) | **Not yet implemented** |
| `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` | TextMate grammar (should be generated, currently hand-crafted) | **Hand-crafted** (23.9KB) |

### Catalog Metadata (Input to Tooling Surface)

| File | Purpose |
|---|---|
| `src/Precept/Language/Token.cs` | `TokenMeta` record with `TextMateScope`, `SemanticTokenType`, `ValidAfter` |
| `src/Precept/Language/Tokens.cs` | `Tokens.All`, `Tokens.GetMeta()` — source of truth for keywords/operators |
| `src/Precept/Language/Type.cs` | `TypeMeta` record with `HoverDescription`, `SnippetTemplate` |
| `src/Precept/Language/Types.cs` | `Types.All`, `Types.GetMeta()` — source of truth for types |
| `src/Precept/Language/Action.cs` | `ActionMeta` record with `ApplicableTo`, `HoverDescription` |
| `src/Precept/Language/Actions.cs` | `Actions.All` — source of truth for action verbs |
| `src/Precept/Language/Modifier.cs` | `ModifierMeta` DU with subtypes |
| `src/Precept/Language/Modifiers.cs` | `Modifiers.All` — source of truth for modifiers |
| `src/Precept/Language/Construct.cs` | `ConstructMeta` with `Slots`, `LeadingTokens` |
| `src/Precept/Language/Constructs.cs` | `Constructs.All` — source of truth for grammar constructs |
| `src/Precept/Language/Operator.cs` | `OperatorMeta` with precedence, associativity |
| `src/Precept/Language/Operators.cs` | `Operators.All` — source of truth for operators |

### Language Server (Request-Time Tooling)

| File | Purpose |
|---|---|
| `tools/Precept.LanguageServer/` | Language server project root |
| `tools/Precept.LanguageServer/Precept.LanguageServer.csproj` | Project file |
| `tools/Precept.LanguageServer/Program.cs` | Entry point, LSP message loop |
| `tools/Precept.LanguageServer/DocumentState.cs` | Per-document compilation cache |
| `tools/Precept.LanguageServer/Handlers/` | LSP request handlers |

### VS Code Extension

| File | Purpose |
|---|---|
| `tools/Precept.VsCode/` | Extension project root |
| `tools/Precept.VsCode/src/extension.ts` | Extension activation, LS launch, preview panel, commands |
| `tools/Precept.VsCode/package.json` | Manifest: language, grammar, commands, semantic token types |
| `tools/Precept.VsCode/language-configuration.json` | Bracket matching, comment toggling, auto-close |
| `tools/Precept.VsCode/syntaxes/precept.tmLanguage.json` | TextMate grammar (build output) |
| `tools/Precept.VsCode/scripts/install-local.ps1` | Local installation script |
| `tools/Precept.VsCode/scripts/uninstall-local.ps1` | Local uninstallation script |

### Build Output Paths

| Path | Description |
|---|---|
| `temp/dev-language-server/bin/Precept.LanguageServer/debug/` | Dev-build LS output |
| `temp/dev-language-server/runtime/run-{ts}-{seq}/` | Shadow-copied runtime for dev mode |
| `tools/Precept.VsCode/server/` | Bundled LS for VSIX packaging |
| `tools/Precept.VsCode/out/` | Compiled TypeScript (extension JS) |
| `tools/Precept.VsCode/*.vsix` | Packaged extension |
