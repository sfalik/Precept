# Language Server

## 1. Status

| Property | Value |
|---|---|
| Doc maturity | Full |
| Implementation state | Bootstrap only (server boots and waits for exit; no LSP features implemented) |
| Source | `tools/Precept.LanguageServer/` |
| Upstream | Compiler (Compilation artifact), Runtime (Precept artifact for preview) |
| Downstream | VS Code extension (via LSP protocol) |

---

## 2. Overview

The language server implements the LSP protocol for Precept `.precept` files. It provides diagnostics, completions, hover, go-to-definition, semantic tokens, preview (inspect), document outline, and folding capabilities to editors. It consumes pipeline artifacts by responsibility — each feature reads from exactly the artifact that owns the information it needs.

The LS is NOT a pipeline stage. It's a hosting layer that runs the pipeline on demand and projects results to the editor. The compiler and runtime do all semantic work; the LS routes requests, manages document state, and translates between LSP protocol types and Precept artifacts.

---

## 3. Responsibilities and Boundaries

**OWNS:** LSP message handling, per-feature artifact routing, `Compilation` lifecycle (recompile on change via `Interlocked.Exchange`), `Precept` lifecycle (rebuild when error-free), preview/inspect dispatch, document state management.

**Does NOT OWN:** Compilation logic (`src/Precept/`), grammar (generated from catalogs), MCP tooling, diagnostic production (compiler), semantic analysis (type checker), inspection logic (evaluator).

---

## 4. Right-Sizing

The LS is thin — a routing and protocol layer over the compiler and runtime. All intelligence lives in the compiler artifacts and catalogs. The LS does not add semantic knowledge; it projects what the compiler already knows to the editor. An LS feature that reaches into catalog data or re-implements any compiler logic is a design violation.

| Metric | Value | Rationale |
|---|---|---|
| Estimated LOC | 400–600 | Routing + translation, not semantic logic |
| Semantic logic | 0% | All semantic knowledge lives in compiler artifacts |
| Catalog access | Read-only | For completion suggestions and hover text only |
| Custom LSP methods | 1 | `precept/inspect` for preview webview |

---

## 5. Inputs and Outputs

**Input:** LSP requests from editor (document open/change, completion request, hover request, etc.)

**Output:** LSP responses (diagnostics, completion items, hover markup, semantic tokens, go-to-definition locations, document symbols, folding ranges)

**Internal:** Holds one `DocumentState` per open document:

```csharp
sealed class DocumentState
{
    private volatile Compilation? _current;
    private volatile Precept? _precept;  // non-null only when !HasErrors
    
    public Compilation? Current => _current;
    public Precept? Precept => _precept;
    
    public void Update(Compilation compilation)
    {
        // Atomic swap — no locks needed
        Interlocked.Exchange(ref _current, compilation);
        Interlocked.Exchange(ref _precept, 
            compilation.HasErrors ? null : Precept.From(compilation));
    }
}
```

---

## 6. Architecture

### In-Process Compilation Model

The LS calls `Compiler.Compile(source)` directly in the same process. Full-pipeline recompile on every document change. The resulting `Compilation` is stored atomically and read by concurrent LSP request handlers without locking (deep immutability of `Compilation` enables `Interlocked.Exchange`).

When `!HasErrors`, the LS also holds a `Precept` (built from `Compilation`) for preview/inspect operations.

### Document State Lifecycle

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                              Document Lifecycle                              │
├─────────────────────────────────────────────────────────────────────────────┤
│                                                                             │
│  textDocument/didOpen                                                       │
│         │                                                                   │
│         ▼                                                                   │
│  ┌─────────────────┐                                                       │
│  │ Create          │                                                        │
│  │ DocumentState   │                                                        │
│  └────────┬────────┘                                                        │
│           │                                                                 │
│           ▼                                                                 │
│  ┌─────────────────────────────────────────────────────────────────────┐   │
│  │                      Compile Loop (on each change)                   │   │
│  │  ┌─────────────────┐     ┌────────────────────┐     ┌─────────────┐ │   │
│  │  │ Compiler.Compile│ ──▶ │ Interlocked.Exchange│ ──▶ │ Push        │ │   │
│  │  │ (source)        │     │ (Compilation)        │     │ Diagnostics │ │   │
│  │  └─────────────────┘     └────────────────────┘     └─────────────┘ │   │
│  │                                    │                                 │   │
│  │                          (!HasErrors)                                │   │
│  │                                    ▼                                 │   │
│  │                     ┌───────────────────────────┐                    │   │
│  │                     │ Precept.From(compilation) │                    │   │
│  │                     │ Interlocked.Exchange      │                    │   │
│  │                     └───────────────────────────┘                    │   │
│  └─────────────────────────────────────────────────────────────────────┘   │
│           │                                                                 │
│  textDocument/didClose                                                      │
│           ▼                                                                 │
│  ┌─────────────────┐                                                       │
│  │ Remove          │                                                        │
│  │ DocumentState   │                                                        │
│  └─────────────────┘                                                        │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### LSP Feature Routing and Artifact Consumption

#### Consumer Artifact Map

| LS Feature | Correct Artifact | Pipeline Stage |
|---|---|---|
| Diagnostics | `Compilation.Diagnostics` | All stages (accumulated) |
| Lexical semantic tokens (Pass 1) | `TokenStream` + `TokenMeta.SemanticTokenType` | Lexer |
| Identifier semantic tokens (Pass 2) | `SemanticIndex` reference bindings | TypeChecker |
| Completions | Catalogs + `SyntaxTree` context + `SemanticIndex` | Parser + TypeChecker |
| Hover | `SemanticIndex` + catalog documentation | TypeChecker + Catalogs |
| Go-to-definition | `SemanticIndex` reference → `ParsedConstruct Syntax` back-pointer | TypeChecker |
| Preview/inspect | `Precept` + inspection runtime | Builder + Evaluator |
| Outline | `SyntaxTree.Constructs` | Parser |
| Folding | `SyntaxTree.Constructs` (multi-line spans) | Parser |

#### Hard Rules

1. **Semantic LS features must not walk `SyntaxTree` to answer semantic questions** — use `SemanticIndex` + back-pointers only. The type checker owns semantic identity; the LS reads it.

2. **Preview/runtime features must not consume `Compilation` after `Precept` is available** — the runtime snapshot is the source of truth for inspection.

3. **Completions and hover must not duplicate catalog knowledge** — query the catalog, format the response.

4. **Diagnostics are push-only after compile** — the LS never synthesizes diagnostics itself.

### Atomic Swap Concurrency

Each document change triggers `Compiler.Compile(newSource)`. The resulting `Compilation` is stored via `Interlocked.Exchange`. Concurrent LSP request handlers read whichever `Compilation` was current when they started — no locking required because `Compilation` is deeply immutable.

```csharp
// Concurrent reads are safe — Compilation is deeply immutable
async Task<Hover> HandleHover(HoverParams p)
{
    var compilation = _documents[p.TextDocument.Uri].Current;
    if (compilation is null) return null;
    
    // Multiple handlers can read the same Compilation concurrently
    var symbol = FindSymbolAt(compilation.SemanticIndex, p.Position);
    return FormatHover(symbol);
}
```

---

## 7. Component Mechanics

This section documents the full mechanics of each LSP feature — how it works, what artifacts it reads, and the exact translation logic.

### 7.1 Diagnostics Push

**Trigger:** `textDocument/didOpen`, `textDocument/didChange`

**Artifact:** `Compilation.Diagnostics`

**Mechanics:**

The LS pushes diagnostics immediately after each compile. There is no polling or pull-based mechanism — the server sends `textDocument/publishDiagnostics` proactively.

```csharp
void OnCompilationComplete(Uri uri, Compilation compilation)
{
    var lspDiagnostics = compilation.Diagnostics
        .Select(d => new Diagnostic
        {
            Range = ToLspRange(d.Span),
            Severity = MapSeverity(d.Severity),
            Code = d.Code.ToString(),
            Source = "precept",
            Message = d.Message
        })
        .ToArray();
    
    _server.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
    {
        Uri = uri,
        Diagnostics = lspDiagnostics
    });
}

DiagnosticSeverity MapSeverity(Precept.DiagnosticSeverity severity) => severity switch
{
    Precept.DiagnosticSeverity.Error => DiagnosticSeverity.Error,
    Precept.DiagnosticSeverity.Warning => DiagnosticSeverity.Warning,
    Precept.DiagnosticSeverity.Info => DiagnosticSeverity.Information,
    Precept.DiagnosticSeverity.Hint => DiagnosticSeverity.Hint,
    _ => DiagnosticSeverity.Error
};
```

**Contract:** Every LSP diagnostic corresponds 1:1 to a `Diagnostic` in `Compilation.Diagnostics`. The LS never synthesizes diagnostics.

### 7.2 Semantic Tokens (Two-Pass Design)

Semantic tokens enable rich syntax highlighting beyond what TextMate grammars can express. Precept uses a **two-pass design** — lexical tokens from Pass 1, semantic identifier classification from Pass 2.

#### Pass 1: Lexical Semantic Tokens

**Trigger:** `textDocument/semanticTokens/full`

**Artifact:** `Compilation.Tokens` + `TokenMeta.SemanticTokenType`

> **Open Question (unresolved):** Should `Compilation` carry a `Tokens` field for lexical semantic token generation? The precept-builder.md `Compilation` record does not currently include it.

**Mechanics:**

Pass 1 walks the token stream and emits semantic tokens based on `TokenMeta.SemanticTokenType`. This pass requires only the lexer — it works even when the type checker fails.

```csharp
SemanticTokens BuildLexicalTokens(Compilation compilation)
{
    var builder = new SemanticTokensBuilder();
    
    foreach (var token in compilation.Tokens)
    {
        var meta = Tokens.GetMeta(token.Kind);
        if (meta.SemanticTokenType is null) continue;
        
        builder.Push(
            line: token.Span.StartLine,
            character: token.Span.StartColumn,
            length: token.Span.Length,
            tokenType: meta.SemanticTokenType,
            tokenModifiers: 0);  // TokenMeta.SemanticTokenModifiers TBD
    }
    
    return builder.Build();
}
```

**TokenMeta.SemanticTokenType values** (already exists in `src/Precept/Language/Token.cs`):

| TokenKind Category | SemanticTokenType |
|---|---|
| Keywords (`precept`, `state`, `event`, `field`, etc.) | `keyword` |
| Type keywords (`int`, `decimal`, `money`, `string`, etc.) | `type` |
| Operators (`+`, `-`, `*`, `/`, `==`, etc.) | `operator` |
| Modifiers (`optional`, `required`, `positive`, etc.) | `modifier` |
| Literals (`123`, `"hello"`, `true`, `false`) | `number`, `string`, `keyword` |
| Comments (`//`, `/* */`) | `comment` |
| Identifiers | *(deferred to Pass 2)* |

#### Pass 2: Identifier Semantic Tokens

**Trigger:** `textDocument/semanticTokens/full` (merged with Pass 1)

**Artifact:** `Compilation.SemanticIndex` reference bindings

**Mechanics:**

Pass 2 walks `SemanticIndex` symbol tables and classifies identifier tokens by their semantic role. This pass requires the type checker to complete successfully.

```csharp
void AddIdentifierTokens(SemanticTokensBuilder builder, SemanticIndex index)
{
    // Fields
    foreach (var field in index.Fields)
    {
        AddIdentifier(builder, field.Syntax.Span, "property");
    }
    
    // States
    foreach (var state in index.States)
    {
        AddIdentifier(builder, state.Syntax.Span, "enum");
    }
    
    // Events
    foreach (var evt in index.Events)
    {
        AddIdentifier(builder, evt.Syntax.Span, "function");
    }
    
    // Event arguments
    foreach (var evt in index.Events)
    {
        foreach (var arg in evt.Args)
        {
            AddIdentifier(builder, arg.Span, "parameter");
        }
    }
    
    // References (field reads, state refs, event refs in transition rows)
    foreach (var reference in index.References)
    {
        var tokenType = reference.Target switch
        {
            FieldReference => "property",
            StateReference => "enum",
            EventReference => "function",
            ArgReference => "parameter",
            _ => null
        };
        if (tokenType is not null)
            AddIdentifier(builder, reference.Span, tokenType);
    }
}
```

> **Open Question (unresolved):** `SemanticIndex.References` does not exist in the type-checker.md §7.1 canonical shape. Should the type checker emit reference collections, or should Pass 2 reconstruct reference sites by walking typed declarations and pattern-matching on `TypedFieldRef`, `TypedArgRef`, etc.?

**Graceful Degradation:**

If the type checker fails (compilation has errors), Pass 2 is skipped. The editor still gets Pass 1 lexical tokens — keywords, types, operators, and literals are highlighted correctly. Only identifier classification degrades.

### 7.3 Catalog-Driven Completions

**Trigger:** `textDocument/completion`

**Artifact:** Catalogs + `SyntaxTree` cursor context + `SemanticIndex` (when available)

**Mechanics:**

Completions are **catalog-driven** — the LS identifies the cursor context (slot kind), then queries the appropriate catalog for valid suggestions. There is no hardcoded completion list in LS code.

#### Step 1: Identify Cursor Context (SlotContext)

```csharp
enum SlotContext
{
    TopLevel,           // Between constructs
    AfterKeyword,       // After 'precept', 'state', 'event', 'field', etc.
    InTypePosition,     // After field name, expecting type
    InModifierPosition, // Before type, expecting modifiers
    InStateTarget,      // After 'in' in transition row
    InEventTarget,      // After 'on' in transition row
    InFieldTarget,      // Field name in action
    InActionVerb,       // Action position (set, add, remove, clear, etc.)
    InExpression,       // Inside guard, compute, ensure expression
    InArgDefault,       // Default value for event argument
}

> **Open Question (unresolved):** `SlotContext` is defined here and in tooling-surface.md. Which document is the canonical home? Also, this maps `SlotKind` values while tooling-surface.md maps `ConstructSlotKind`. Are these the same enum under different names?

SlotContext GetCursorContext(SyntaxTree tree, Position position)
{
    // Find the innermost construct containing the cursor
    var construct = FindConstructAt(tree, position);
    if (construct is null) return SlotContext.TopLevel;
    
    // Find which slot the cursor is in
    var slotIndex = FindSlotAt(construct, position);
    if (slotIndex < 0) return SlotContext.AfterKeyword;
    
    // Map slot kind to context
    return construct.Meta.Slots[slotIndex].Kind switch
    {
        SlotKind.TypeExpression => SlotContext.InTypePosition,
        SlotKind.ModifierList => SlotContext.InModifierPosition,
        SlotKind.StateTarget => SlotContext.InStateTarget,
        SlotKind.EventTarget => SlotContext.InEventTarget,
        SlotKind.FieldTarget => SlotContext.InFieldTarget,
        SlotKind.ActionChain => SlotContext.InActionVerb,
        SlotKind.GuardClause or SlotKind.ComputeExpression 
            or SlotKind.EnsureClause => SlotContext.InExpression,
        _ => SlotContext.AfterKeyword
    };
}
```

#### Step 2: Query Catalog for Context

```csharp
IEnumerable<CompletionItem> GetCompletions(SlotContext context, SemanticIndex? index)
{
    return context switch
    {
        SlotContext.TopLevel => GetConstructLeaderCompletions(),
        SlotContext.InTypePosition => GetTypeCompletions(),
        SlotContext.InModifierPosition => GetModifierCompletions(GetCurrentConstructKind()),
        SlotContext.InStateTarget => GetStateCompletions(index),
        SlotContext.InEventTarget => GetEventCompletions(index),
        SlotContext.InFieldTarget => GetFieldCompletions(index),
        SlotContext.InActionVerb => GetActionCompletions(GetCurrentFieldType()),
        SlotContext.InExpression => GetExpressionCompletions(index),
        _ => Enumerable.Empty<CompletionItem>()
    };
}

IEnumerable<CompletionItem> GetTypeCompletions()
{
    return Types.All
        .Where(t => t.IsUserFacing)  // Filter internal types
        .Select(t => new CompletionItem
        {
            Label = t.Token.Text,
            Kind = CompletionItemKind.TypeParameter,
            Documentation = t.HoverDescription,
            InsertText = t.SnippetTemplate ?? t.Token.Text,
            InsertTextFormat = t.SnippetTemplate is not null 
                ? InsertTextFormat.Snippet 
                : InsertTextFormat.PlainText
        });
}

> **Open Question (unresolved):** `TypeMeta.IsUserFacing` and `TypeMeta.SnippetTemplate` are used here but not in the catalog-system.md shape. Should these properties be added to `TypeMeta`?

IEnumerable<CompletionItem> GetActionCompletions(TypeKind? fieldType)
{
    return Actions.All
        .Where(a => fieldType is null || a.ApplicableTo.Contains(fieldType.Value))
        .Select(a => new CompletionItem
        {
            Label = a.Token.Text,
            Kind = CompletionItemKind.Method,
            Documentation = a.Description,  // ActionMeta has Description, not HoverDescription
            InsertText = a.Token.Text,
            InsertTextFormat = InsertTextFormat.PlainText
        });
}

> **Open Question (unresolved):** `ActionMeta.HoverDescription` and `ActionMeta.SnippetTemplate` are used in the original design but not in the catalog-system.md shape. Should these properties be added to `ActionMeta`?

IEnumerable<CompletionItem> GetStateCompletions(SemanticIndex? index)
{
    if (index is null) return Enumerable.Empty<CompletionItem>();
    
    return index.States.Select(s => new CompletionItem
    {
        Label = s.Name,
        Kind = CompletionItemKind.EnumMember,
        Detail = FormatStateModifiers(s.Modifiers)
    });
}
```

#### Catalog Properties Required for Completions

Each catalog entry that appears in completions needs:

| Property | Purpose |
|---|---|
| `Token.Text` | Completion label |
| `HoverDescription` | Completion documentation popup |
| `SnippetTemplate` | Insert text with placeholders (optional) |
| `ApplicableTo` | Filter by context (actions → field types, modifiers → construct kinds) |

### 7.4 Hover

**Trigger:** `textDocument/hover`

**Artifact:** `SemanticIndex` + catalog documentation

**Mechanics:**

Hover finds the symbol at the cursor position via `SemanticIndex`, then formats documentation from catalog metadata.

```csharp
Hover? GetHover(Compilation compilation, Position position)
{
    var token = FindTokenAt(compilation.TokenStream, position);
    if (token is null) return null;
    
    // Keyword hover — catalog lookup
    var meta = Tokens.GetMeta(token.Kind);
    if (meta.HoverDescription is not null)
    {
        return new Hover
        {
            Contents = new MarkupContent
            {
                Kind = MarkupKind.Markdown,
                Value = meta.HoverDescription
            },
            Range = ToLspRange(token.Span)
        };
    }
    
    // Identifier hover — semantic lookup
    if (token.Kind == TokenKind.Identifier && compilation.SemanticIndex is { } index)
    {
        var symbol = FindSymbolAt(index, position);
        if (symbol is not null)
        {
            return new Hover
            {
                Contents = FormatSymbolHover(symbol),
                Range = ToLspRange(token.Span)
            };
        }
    }
    
    return null;
}

MarkupContent FormatSymbolHover(object symbol) => symbol switch
{
    TypedField f => new MarkupContent
    {
        Kind = MarkupKind.Markdown,
        Value = $"**field** `{f.Name}` : `{Types.GetMeta(f.ResolvedType).Token.Text}`\n\n" +
                FormatModifiers(f.Modifiers) +
                (f.IsComputed ? "\n\n*Computed field*" : "")
    },
    
    TypedState s => new MarkupContent
    {
        Kind = MarkupKind.Markdown,
        Value = $"**state** `{s.Name}`\n\n" +
                FormatModifiers(s.Modifiers)
    },
    
    TypedEvent e => new MarkupContent
    {
        Kind = MarkupKind.Markdown,
        Value = $"**event** `{e.Name}`\n\n" +
                (e.IsInitial ? "*Initial event*\n\n" : "") +
                FormatArgs(e.Args)
    },
    
    TypedArg a => new MarkupContent
    {
        Kind = MarkupKind.Markdown,
        Value = $"**argument** `{a.Name}` : `{Types.GetMeta(a.ResolvedType).Token.Text}`"
        // Note: owning event lookup needed separately via ArgReference
    },
    
    _ => new MarkupContent { Kind = MarkupKind.PlainText, Value = "" }
};
```

> **Open Question (unresolved):** `TypedArg` has no `EventName` back-reference. Hover for an event arg needs to look up the owning event from an `ArgReference`. Should `TypedArg` carry an `EventName` field, or should hover look it up separately?

### 7.5 Go-to-Definition

**Trigger:** `textDocument/definition`

**Artifact:** `SemanticIndex` reference binding → `ParsedConstruct.Syntax` back-pointer

**Mechanics:**

Go-to-definition resolves the reference at the cursor to its declaration using `SemanticIndex` back-pointers. Every `Typed*` record carries a `Syntax` property pointing to its declaring `ParsedConstruct`, which carries a `SourceSpan`.

```csharp
Location? GetDefinition(Compilation compilation, Position position)
{
    if (compilation.SemanticIndex is not { } index) return null;
    
    // Find what the cursor is on
    var reference = FindReferenceAt(index, position);
    if (reference is null) return null;
    
    // Resolve to declaration via back-pointer
    var declaration = reference.Target switch
    {
        FieldReference fr => index.FieldsByName.GetValueOrDefault(fr.FieldName)?.Syntax,
        StateReference sr => index.StatesByName.GetValueOrDefault(sr.StateName)?.Syntax,
        EventReference er => index.EventsByName.GetValueOrDefault(er.EventName)?.Syntax,
        ArgReference ar => index.EventsByName.GetValueOrDefault(ar.EventName)?
            .Args.FirstOrDefault(a => a.Name == ar.ArgName)?.Span,
        _ => null
    };
    
    if (declaration is null) return null;
    
    return new Location
    {
        Uri = compilation.Uri,
        Range = ToLspRange(declaration.Span)
    };
}
```

**Back-Pointer Design:**

The type checker maintains back-pointers from semantic artifacts to their source constructs:

```csharp
public sealed record TypedField(
    // ... semantic properties ...
    ParsedConstruct Syntax  // ← back-pointer to source
);

public sealed record TypedState(
    // ... semantic properties ...
    ParsedConstruct Syntax  // ← back-pointer to source
);

public sealed record TypedEvent(
    // ... semantic properties ...
    ParsedConstruct Syntax  // ← back-pointer to source
);
```

### 7.6 Preview/Inspect (Custom LSP Extension)

**Trigger:** `precept/preview` (custom method, not standard LSP)

> **Open Question (unresolved):** This method is named `precept/inspect` here but `precept/preview` in tooling-surface.md §7.4. Which name is canonical? Using `precept/preview` pending resolution.

**Artifact:** `Precept` + inspection runtime (`EventInspection`, `UpdateInspection`)

**Mechanics:**

Preview is a non-standard LSP extension that enables the VS Code extension's preview webview. It shows what each event would do if fired, without actually executing it.

```csharp
// Custom request type
[Method("precept/inspect")]
record InspectParams(
    Uri Uri,
    string CurrentState,
    Dictionary<string, object?> Data,
    string? Event,
    Dictionary<string, object?>? EventArgs);

[Method("precept/inspect")]
InspectResult? HandleInspect(InspectParams p)
{
    var state = _documents[p.Uri];
    if (state.Precept is not { } precept) return null;  // Only when error-free
    
    // Build version from provided data
    var version = BuildVersion(precept, p.CurrentState, p.Data);
    
    // If event specified, inspect that event
    if (p.Event is not null)
    {
        var args = p.EventArgs ?? new();
        var inspection = precept.Inspect(version, p.Event, args);
        return MapToResult(inspection);
    }
    
    // Otherwise, return inspection of all available events
    var inspections = precept.Events
        .Select(e => precept.Inspect(version, e.Name, new()))
        .ToArray();
    return MapToResult(inspections);
}
```

**EventInspection Shape** (from evaluator):

```csharp
public sealed record EventInspection(
    string EventName,
    Prospect Outcome,          // Certain, Possible, Impossible
    string? Explanation,       // Why this outcome
    ImmutableArray<TransitionRowInspection> Rows,
    ImmutableArray<FieldSnapshot> BeforeFields,
    ImmutableArray<FieldSnapshot> AfterFields);

public enum Prospect { Certain, Possible, Impossible }
```

> **Open Question (unresolved):** The `EventInspection` shape here differs from evaluator.md. This doc has `BeforeFields`/`AfterFields`; evaluator.md has `EventEnsures`/`ConstraintResult`. Which is canonical? Should this doc reference evaluator.md's shape?

The extension calls `precept/preview` whenever the user changes preview state, then renders the result in the preview webview. See `docs/tooling/extension.md` for the webview side.

### 7.7 Document Outline

**Trigger:** `textDocument/documentSymbol`

**Artifact:** `SyntaxTree.Constructs`

**Mechanics:**

Document outline provides hierarchical symbols for the document sidebar. The LS walks `SyntaxTree.Constructs` and maps each to a `DocumentSymbol`.

```csharp
DocumentSymbol[] GetDocumentSymbols(Compilation compilation)
{
    return compilation.SyntaxTree.Constructs
        .Where(c => IsOutlineConstruct(c.Meta.Kind))
        .Select(c => ToDocumentSymbol(c))
        .ToArray();
}

bool IsOutlineConstruct(ConstructKind kind) => kind switch
{
    ConstructKind.PreceptDeclaration => true,
    ConstructKind.FieldDeclaration => true,
    ConstructKind.StateDeclaration => true,
    ConstructKind.EventDeclaration => true,
    ConstructKind.TransitionRow => true,
    ConstructKind.RuleDeclaration => true,
    ConstructKind.EditDeclaration => true,
    _ => false
};

> **Open Question (unresolved):** `IsOutlineConstruct` and `MapSymbolKind` hardcode `ConstructKind` values. By catalog-driven architecture, `ConstructMeta` should carry `IsOutlineNode` and `LspSymbolKind` properties. Should these be added to the catalog?

DocumentSymbol ToDocumentSymbol(ParsedConstruct construct) => new()
{
    Name = ExtractName(construct),
    Kind = MapSymbolKind(construct.Meta.Kind),
    Range = ToLspRange(construct.Span),
    SelectionRange = ToLspRange(ExtractNameSpan(construct))
};

SymbolKind MapSymbolKind(ConstructKind kind) => kind switch
{
    ConstructKind.PreceptDeclaration => SymbolKind.Class,
    ConstructKind.FieldDeclaration => SymbolKind.Property,
    ConstructKind.StateDeclaration => SymbolKind.Enum,
    ConstructKind.EventDeclaration => SymbolKind.Function,
    ConstructKind.TransitionRow => SymbolKind.Event,
    ConstructKind.RuleDeclaration => SymbolKind.Constant,
    ConstructKind.EditDeclaration => SymbolKind.Interface,
    _ => SymbolKind.Null
};
```

### 7.8 Folding Ranges

**Trigger:** `textDocument/foldingRange`

**Artifact:** `SyntaxTree.Constructs` (multi-line spans)

**Mechanics:**

Folding enables collapsing of multi-line constructs. The LS identifies constructs spanning multiple lines and reports them as foldable regions.

```csharp
FoldingRange[] GetFoldingRanges(Compilation compilation)
{
    return compilation.SyntaxTree.Constructs
        .Where(c => c.Span.EndLine > c.Span.StartLine)  // Multi-line only
        .Select(c => new FoldingRange
        {
            StartLine = c.Span.StartLine,
            StartCharacter = c.Span.StartColumn,
            EndLine = c.Span.EndLine,
            EndCharacter = c.Span.EndColumn,
            Kind = GetFoldingKind(c.Meta.Kind)
        })
        .ToArray();
}

FoldingRangeKind? GetFoldingKind(ConstructKind kind) => kind switch
{
    ConstructKind.Comment => FoldingRangeKind.Comment,
    _ => FoldingRangeKind.Region
};
```

---

## 8. Dependencies and Integration Points

- **Compiler** (`src/Precept/`): `Compiler.Compile(source)` — called in-process on every document change
- **Runtime** (`src/Precept/`): `Precept.From(Compilation)` — called when `!HasErrors` for preview
- **Catalogs** (upstream at request time): for completions and hover text
- **VS Code extension** (downstream): hosts the LS process; receives LSP responses
- **OmniSharp.Extensions.LanguageServer** (library): LSP protocol implementation

---

## 9. Failure Modes and Recovery

| Failure | Detection | Recovery |
|---|---|---|
| `Compiler.Compile` throws | Exception caught in compile handler | Report single top-level diagnostic; retain previous `Compilation` |
| Type checker fails (errors) | `Compilation.HasErrors == true` | Pass 2 semantic tokens skipped; completions/hover degrade gracefully |
| `Precept.From` throws | Exception caught in compile handler | `_precept` remains null; preview disabled |
| LSP request handler throws | OmniSharp exception handling | Return null/empty response; log error |
| Document not found | `_documents.TryGetValue` returns false | Return null response |

**Key Principle:** The LS never crashes. Engine bugs become diagnostics. Missing artifacts become graceful degradation.

```csharp
void OnDocumentChanged(Uri uri, string source)
{
    try
    {
        var compilation = Compiler.Compile(source);
        _documents[uri].Update(compilation);
        PublishDiagnostics(uri, compilation.Diagnostics);
    }
    catch (Exception ex)
    {
        // Engine bug — report as diagnostic, don't crash
        var diagnostic = new Diagnostic
        {
            Range = new Range(0, 0, 0, 0),
            Severity = DiagnosticSeverity.Error,
            Source = "precept",
            Message = $"Internal compiler error: {ex.Message}"
        };
        _server.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams
        {
            Uri = uri,
            Diagnostics = [diagnostic]
        });
    }
}
```

---

## 10. Contracts and Guarantees

| Guarantee | Mechanism |
|---|---|
| The LS never holds a stale `Compilation` longer than one document change cycle | Atomic swap on every `didChange` |
| Every LSP diagnostic corresponds 1:1 to a `Diagnostic` in `Compilation.Diagnostics` | Direct mapping in `PublishDiagnostics` |
| Preview features are never invoked when `Compilation.HasErrors` is true | `_precept` is null when errors exist |
| Concurrent LSP requests see a consistent `Compilation` snapshot | Deep immutability + volatile reference |
| Semantic features gracefully degrade when type checker fails | Null checks before `SemanticIndex` access |
| Catalog queries are read-only | Catalogs are immutable static registries |

**Concurrency Contract:**

```csharp
// Multiple threads can call these concurrently — no data races
Compilation? GetCurrent(Uri uri) => _documents[uri].Current;

// The atomic swap guarantees all readers see a complete, consistent Compilation
void Update(Compilation compilation)
{
    Interlocked.Exchange(ref _current, compilation);  // Atomic publish
}
```

---

## 11. Design Rationale and Decisions

### Decision 1: In-Process Compilation

**Choice:** The LS calls `Compiler.Compile(source)` directly in the same process.

**Rationale:** At Precept's scale (64KB ceiling, flat grammar, sub-millisecond lex), in-process compilation is faster, simpler, and more correct than IPC-based compiler integration. No serialization boundary means no schema drift risk between LS and compiler.

**Alternatives Rejected:**
- **Out-of-process compiler daemon** — adds IPC latency, versioning complexity, and deployment friction with no benefit at DSL scale
- **Compiler-as-a-service with process recycling** — complexity not justified for sub-millisecond compiles

### Decision 2: Full-Pipeline Recompile on Change

**Choice:** Every `didChange` event triggers a complete lexer → parser → type checker → graph analyzer → proof engine pass.

**Rationale:** At DSL scale, full recompile is fast enough (<5ms typical). Incremental compilation adds an entire class of invalidation bugs (stale symbol tables, partial updates, reference counting). The cost/benefit ratio strongly favors simplicity.

**Alternatives Rejected:**
- **Incremental parsing** — requires change tracking, partial tree updates, and careful invalidation; not needed at this scale
- **Lazy type checking** — defers errors until hover/completion; poor UX

### Decision 3: Atomic Swap Concurrency

**Choice:** Store `Compilation` in a `volatile` field and update via `Interlocked.Exchange`. No locks.

**Rationale:** `Compilation` is deeply immutable — once created, it never changes. Concurrent readers all see a complete, consistent snapshot. No lock contention, no deadlock risk, no lock ordering bugs. Deep immutability is enforced by using `ImmutableArray<>` and sealed record types throughout the compilation artifact graph.

**Alternatives Rejected:**
- **Reader-writer lock** — adds contention and complexity for no benefit when data is immutable
- **Copy-on-write with version numbers** — unnecessary; single atomic reference swap is sufficient

### Decision 4: Catalog-Driven Completions

**Choice:** Completions query catalogs directly; no hardcoded completion lists in LS code.

**Rationale:** The language surface is defined by catalogs. If completions hardcoded keyword lists, they would drift from the actual language. Catalog-driven completions are automatically correct and automatically updated when the language evolves.

**Alternatives Rejected:**
- **Static completion lists** — would drift from catalogs; maintenance burden
- **Completion providers per construct type** — scatters language knowledge; harder to maintain

### Decision 5: Two-Pass Semantic Tokens

**Choice:** Pass 1 (lexical) uses `TokenStream` + `TokenMeta.SemanticTokenType`. Pass 2 (semantic) uses `SemanticIndex` reference bindings.

**Rationale:** Lexical tokens are available even when the type checker fails — users get keyword/operator highlighting regardless of errors. Semantic tokens (identifier classification) degrade gracefully when type checking is incomplete.

**Alternatives Rejected:**
- **Single-pass semantic tokens** — fails completely when type checker has errors
- **TextMate-only highlighting** — can't distinguish field references from state references

### Decision 6: Feature-to-Artifact Hard Rules

**Choice:** Each LS feature reads from exactly one designated artifact (see §6 Consumer Artifact Map). Violations are design bugs.

**Rationale:** Clear ownership prevents bugs where features read stale or inappropriate data. If hover walks `SyntaxTree` instead of `SemanticIndex`, it might show wrong types. If preview reads `Compilation` instead of `Precept`, it might show incomplete execution plans.

**Alternatives Rejected:**
- **Ad-hoc artifact access** — leads to subtle bugs and inconsistent behavior
- **Single "document model" façade** — hides which data is authoritative for each feature

---

## 12. Innovation

- **Atomic swap concurrency model:** Deep immutability of `Compilation` enables `Interlocked.Exchange` — no locks needed for concurrent LSP requests. This pattern is rare in language servers; most use reader-writer locks or mutex-protected state.

- **Single-process integration:** The LS calls `Compiler.Compile` directly — same process, no IPC, no serialization boundary. This is the dominant pattern at DSL scale but uncommon in general-purpose LS implementations.

- **Full-pipeline recompile on every change:** Correct at DSL scale; no incremental infrastructure needed. Most language servers invest heavily in incremental compilation; Precept's size ceiling makes this unnecessary.

- **Catalog-driven completions:** Completions are derived from catalog metadata, not hardcoded lists. New language features get completions automatically. This is unusual — most language servers maintain separate completion data.

- **Two-pass semantic tokens with graceful degradation:** Lexical highlighting works even when semantic analysis fails. Users get partial highlighting during active editing. Most LS implementations fail completely or show stale tokens.

- **Back-pointer-based go-to-definition:** Every `Typed*` record carries a `Syntax` back-pointer to its declaring construct. No separate "definition index" needed; the type checker's output is the definition index.

---

## 13. Open Questions / Implementation Notes

### Implementation Status

1. Only server boot is implemented — all LSP feature handlers are not yet written.
2. Implement diagnostics push first (simplest feature, most visible value to users).
3. Implement lexical semantic tokens second (uses `TokenStream`, no `SemanticIndex` dependency).
4. Completions and hover depend on `SemanticIndex` — blocked on TypeChecker implementation.
5. Preview/inspect depends on `Precept` runtime — blocked on Precept Builder and Evaluator implementation.
6. Two-pass semantic token design confirmed: Pass 1 uses `TokenStream` + `TokenMeta.SemanticTokenType`; Pass 2 uses `SemanticIndex` bindings.

### Open Questions

> **Open Question 1 (unresolved):** Should catalog entries carry a `Documentation` string for hover/completion tooltips?
>
> **Context:** The completions and hover features documented in §7.3 and §7.4 reference `HoverDescription` on catalog entries. Some catalog records (`TypeMeta`, `ActionMeta`, `FunctionMeta`) may not currently have this property.
>
> **Candidates for `HoverDescription` / `Documentation` addition:**
> - `ModifierMeta` subtypes (`FieldModifierMeta`, `StateModifierMeta`, `EventModifierMeta`, `ArgModifierMeta`)
> - `ActionMeta`
> - `TypeMeta`
> - `OperationMeta`
> - `FunctionMeta`
>
> **Trade-off:** Adding documentation strings to catalogs is ~2 hours of work but enables rich tooltips. Without them, hover shows only the keyword text.
>
> **Recommendation:** Add `HoverDescription` to all user-facing catalog entries. The investment is low and UX benefit is high.

> **Open Question 2 (unresolved):** Should the LS support workspace/symbol for cross-file symbol search?
>
> **Context:** Single-file precepts don't need cross-file search. But if precepts ever support imports or multi-file definitions, workspace/symbol becomes relevant.
>
> **Recommendation:** Defer. Precepts are single-file by design. Revisit only if the language surface expands to multi-file.

> **Open Question 3 (unresolved):** Should the LS support rename refactoring (`textDocument/rename`)?
>
> **Context:** Rename requires identifying all references to a symbol, which `SemanticIndex` provides. Implementation is straightforward.
>
> **Recommendation:** Implement after core features. Rename is high-value for usability but not blocking.

### Implementation Notes

- **OmniSharp.Extensions.LanguageServer** handles LSP protocol details; the LS focuses on Precept-specific logic
- **Custom method registration** for `precept/inspect`: use `[Method("precept/inspect")]` attribute
- **Debouncing**: Consider debouncing rapid `didChange` events (OmniSharp may handle this)

---

## 14. Deliberate Exclusions

| Exclusion | Rationale |
|---|---|
| **No semantic logic in LS code** | All language knowledge lives in compiler artifacts and catalogs. The LS routes and translates; it doesn't analyze. |
| **No separate compiler process** | In-process compilation is the right model at DSL scale. No benefit from process isolation. |
| **No incremental compilation** | Full recompile is fast enough (<5ms). Incremental adds invalidation complexity with no user-visible benefit. |
| **No workspace-wide operations** | Precepts are single-file. No cross-file references, no workspace symbol search. |
| **No code actions / quick fixes** | Diagnostics are informational. Quick fixes would require understanding fixes, which is compiler domain. |
| **No formatting** | Precept files are short. Users format manually. Defer until demand emerges. |
| **No signature help** | Event args are simple. Signature help adds complexity for marginal benefit. |
| **No inlay hints** | Type inference is explicit. No hidden types to reveal. |

---

## 15. Cross-References

| Topic | Document |
|---|---|
| LS consumer artifact map and hard rules | `docs/compiler-and-runtime-design.md §15` |
| Immutability + atomic swap model | `docs/compiler-and-runtime-design.md §12` |
| Compiler pipeline overview | `docs/compiler/` |
| Lexer and TokenStream | `docs/compiler/lexer.md` |
| Parser and SyntaxTree | `docs/compiler/parser.md` |
| Type Checker and SemanticIndex | `docs/compiler/type-checker.md` |
| Evaluator and EventInspection | `docs/runtime/evaluator.md` |
| Precept Builder | `docs/runtime/precept-builder.md` |
| VS Code extension that hosts the LS | `docs/tooling/extension.md` |
| Catalog system architecture | `docs/language/catalog-system.md` |
| TokenMeta.SemanticTokenType | `src/Precept/Language/Token.cs` |

---

## 16. Source Files

| File | Purpose |
|---|---|
| `tools/Precept.LanguageServer/Program.cs` | Server bootstrap (OmniSharp initialization) |
| `tools/Precept.LanguageServer/DocumentState.cs` | Per-document compilation/precept state (pending) |
| `tools/Precept.LanguageServer/Handlers/DiagnosticsHandler.cs` | Diagnostics push (pending) |
| `tools/Precept.LanguageServer/Handlers/SemanticTokensHandler.cs` | Two-pass semantic tokens (pending) |
| `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs` | Catalog-driven completions (pending) |
| `tools/Precept.LanguageServer/Handlers/HoverHandler.cs` | Symbol/keyword hover (pending) |
| `tools/Precept.LanguageServer/Handlers/DefinitionHandler.cs` | Go-to-definition (pending) |
| `tools/Precept.LanguageServer/Handlers/DocumentSymbolHandler.cs` | Outline (pending) |
| `tools/Precept.LanguageServer/Handlers/FoldingRangeHandler.cs` | Folding ranges (pending) |
| `tools/Precept.LanguageServer/Handlers/InspectHandler.cs` | Custom preview command (pending) |
| `src/Precept/Language/Token.cs` | `TokenMeta.SemanticTokenType` definition |
| `src/Precept/Pipeline/SemanticIndex.cs` | `SemanticIndex` shape consumed by LS |
