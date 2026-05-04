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
| Completions | Catalogs + `ConstructManifest` context + `SemanticIndex` | Parser + TypeChecker |
| Hover | `SemanticIndex` + catalog documentation | TypeChecker + Catalogs |
| Go-to-definition | `SemanticIndex` reference → `ParsedConstruct Syntax` back-pointer | TypeChecker |
| Preview/inspect | `Precept` + inspection runtime | Builder + Evaluator |
| Outline | `ConstructManifest.Constructs` | Parser |
| Folding | `ConstructManifest.Constructs` (multi-line spans) | Parser |

#### Hard Rules

1. **Semantic LS features must not walk `ConstructManifest` to answer semantic questions** — use `SemanticIndex` + back-pointers only. The type checker owns semantic identity; the LS reads it.

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

**Artifact:** Catalogs + `ConstructManifest` cursor context + `SemanticIndex` (when available)

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

SlotContext GetCursorContext(ConstructManifest manifest, Position position)
{
    // Find the innermost construct containing the cursor
    var construct = FindConstructAt(manifest, position);
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
> *Source: catalog-gap-register.md #30*

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
> *Source: catalog-gap-register.md #43 (partial)*

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
> *Source: catalog-gap-register.md #31*

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
> *Source: catalog-gap-register.md #33*

The extension calls `precept/preview` whenever the user changes preview state, then renders the result in the preview webview. See `docs/tooling/extension.md` for the webview side.

### 7.7 Document Outline

**Trigger:** `textDocument/documentSymbol`

**Artifact:** `ConstructManifest.Constructs`

**Mechanics:**

Document outline provides hierarchical symbols for the document sidebar. The LS walks `ConstructManifest.Constructs` and maps each to a `DocumentSymbol`.

```csharp
DocumentSymbol[] GetDocumentSymbols(Compilation compilation)
{
    return compilation.ConstructManifest.Constructs
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
> *Source: catalog-gap-register.md #34*

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

**Artifact:** `ConstructManifest.Constructs` (multi-line spans)

**Mechanics:**

Folding enables collapsing of multi-line constructs. The LS identifies constructs spanning multiple lines and reports them as foldable regions.

```csharp
FoldingRange[] GetFoldingRanges(Compilation compilation)
{
    return compilation.ConstructManifest.Constructs
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

### 7.9 Diagnostic Enrichment ("Did You Mean?")

#### 7.9.1 Trigger Conditions

The enrichment pass runs inside `OnCompilationComplete`, after the full pipeline has settled. It processes only the four diagnostic codes listed in §2.4. All other codes are ineligible.

**SemanticIndex guard.** The enrichment pass must only run when `SemanticIndex` is non-null. A null `SemanticIndex` means compilation stopped before the type-check stage (lex or parse errors). The suggestion machinery depends on the symbol tables populated during type-checking; running it without them is undefined.

```
OnCompilationComplete:
  if compilation.Semantics is null → skip enrichment entirely
  for each Diagnostic d in compilation.Diagnostics:
    if d.Code matches a SuggestionSource entry → attempt enrichment
```

Lex-stage diagnostics (`UnterminatedStringLiteral`, `InvalidCharacter`, etc.) and parse-stage diagnostics (`ExpectedToken`, `NonAssociativeComparison`, etc.) never receive "did you mean?" enrichment, even if their messages happen to contain an identifier.

#### 7.9.2 Fuzzy Match Behavior

**Algorithm:** Levenshtein edit distance (insertions, deletions, substitutions each cost 1).

**Threshold:** ≤ 3 edits. A match with edit distance 4 or more is ignored entirely — no suggestion, no lightbulb.

**Candidate selection:**
1. Compute Levenshtein distance between `Diagnostic.Args[0]` and every name in the suggestion pool (§2.4).
2. Discard all candidates with distance > 3.
3. If no candidates remain: no enrichment. The diagnostic message is unchanged, no code action is registered.
4. If one or more candidates remain: pick the one with the lowest distance.
5. **Tie-break:** if two or more candidates share the lowest distance, pick alphabetically by name (case-insensitive, ascending).
6. Append the suggestion suffix to the diagnostic message (§2.3) and register a code action (§3.1).

**Case sensitivity:** Levenshtein operates on the literal casing in the source. `Score` and `score` are distance 1, not 0. Tiebreak is case-insensitive. The replacement text preserves the casing of the matched candidate exactly as declared.

#### 7.9.3 Message Format

The suggestion suffix is appended to the original formatted diagnostic message with an em dash separator.

```
— did you mean 'SuggestedName'?
```

Exact rules:

- One space before the em dash (`—`), one space after.
- The phrase is always lowercase: `did you mean`.
- The suggestion is wrapped in single straight quotes: `'Name'`.
- The phrase ends with a question mark and no trailing space.

**Example — UndeclaredField:**

Original:
```
Field 'ReasonTxt' is not declared
```

Enriched (match: `ReasonText`, distance 1):
```
Field 'ReasonTxt' is not declared — did you mean 'ReasonText'?
```

**Example — UndeclaredState:**

Original:
```
State 'AwaitngReturn' is not declared
```

Enriched (match: `AwaitingReturn`, distance 2):
```
State 'AwaitngReturn' is not declared — did you mean 'AwaitingReturn'?
```

**Example — UndeclaredEvent:**

Original:
```
Event 'Submitt' is not declared
```

Enriched (match: `Submit`, distance 1):
```
Event 'Submitt' is not declared — did you mean 'Submit'?
```

**Example — UndeclaredFunction:**

Original:
```
'roudn' is not a recognized function
```

Enriched (match: `round`, distance 2):
```
'roudn' is not a recognized function — did you mean 'round'?
```

The enriched message replaces `Diagnostic.Message` on the in-flight diagnostic object before it is published via `textDocument/publishDiagnostics`. No separate field or protocol extension is used. The Problems panel and squiggle hover both display the enriched string automatically.

#### 7.9.4 Eligible Diagnostic Codes

Enrichment is driven by `SuggestionSources` catalog metadata. Only these four codes are in scope:

| `DiagnosticCode` | Suggestion Pool | `Args[0]` Content |
|---|---|---|
| `UndeclaredField` | All field names declared in the precept (`SemanticIndex.UserFields`) | The failing field name as written in source |
| `UndeclaredState` | All state names declared in the precept (`SemanticIndex.UserStates`) | The failing state name as written in source |
| `UndeclaredEvent` | All event names declared in the precept (`SemanticIndex.UserEvents`) | The failing event name as written in source |
| `UndeclaredFunction` | All built-in function names (`Functions.All` — `min`, `max`, `abs`, `clamp`, `floor`, `ceil`, `truncate`, `round`, `roundPlaces`, `approximate`, `pow`, `sqrt`, `trim`, `startsWith`, `endsWith`, `toLower`, `toUpper`, `left`, `right`, `mid`, `tildeStartsWith`, `tildeEndsWith`, `now`) | The failing function name as written in source |

**Why only these four?** These are the only `DiagnosticCategory.Naming` errors where a single candidate pool can be enumerated at compile time and where a close match reliably indicates a typo rather than a conceptual error. Other naming errors (`DuplicateFieldName`, `DuplicateStateName`, etc.) are declaration conflicts, not lookup failures; they do not benefit from suggestions. Type-system errors (`TypeMismatch`, `QualifierMismatch`) involve structural mismatches where no single "intended name" exists to suggest.

---

### 7.10 Code Actions

#### 7.10.1 "Did You Mean?" Code Actions

When enrichment produces a suggestion, a code action is registered alongside the enriched diagnostic.

**Code action properties:**

| Property | Value |
|---|---|
| Title | `Rename to 'X'` where X is the suggestion |
| Kind | `quickfix` |
| IsPreferred | `true` (VS Code renders this as the highlighted primary fix) |
| Diagnostics | The originating diagnostic (for correlation in the lightbulb) |

**Text edit:**

Replace the span of `Diagnostic.Span` with the suggestion string verbatim. No surrounding context is modified.

```
WorkspaceEdit:
  DocumentChange for the open document:
    TextEdit:
      range: LSP Range derived from Diagnostic.Span
        start: { line: Span.StartLine - 1, character: Span.StartColumn - 1 }
        end:   { line: Span.EndLine   - 1, character: Span.EndColumn   - 1 }
      newText: "<suggestion>"
```

Note: `SourceSpan` uses 1-based line/column; LSP uses 0-based. Subtract 1 from each component when building the LSP `Range`.

**One action per diagnostic.** Only the best-match suggestion produces a code action. There is no "show all suggestions" secondary menu.

**VS Code editor appearance:**

```
  line 13:   from Submitted on Appove when ...
                             ~~~~~~
                             💡  Rename to 'Approve'
```

The lightbulb appears on the line containing the diagnostic span when the cursor is on that line or anywhere in the span. Clicking it (or pressing `Ctrl+.`) opens the quick-fix menu:

```
  ┌─────────────────────────────────────────────────┐
  │ 💡 Rename to 'Approve'                          │
  │    Quick Fix                                    │
  └─────────────────────────────────────────────────┘
```

After applying, the identifier is replaced in place and the squiggle disappears after the next compile cycle.

#### 7.10.2 FixHint Code Actions

Every diagnostic with a non-null `FixHint` in `DiagnosticMeta` generates a code action, regardless of whether the fix is automatable. The distinction is whether the action carries a `TextEdit` or opens an informational panel.

##### 3.2.1 Mechanical (Text-Edit) Code Actions

These FixHints correspond to unambiguous single-location edits. The LS applies them directly.

| `DiagnosticCode` | Code Action Title | Text Edit |
|---|---|---|
| `UnterminatedStringLiteral` | `Add closing "` | Insert `"` at `Span.End` (offset-based) — after the last character of the literal span |
| `UnterminatedTypedConstant` | `Add closing '` | Insert `'` at `Span.End` |

For `UnterminatedStringLiteral` and `UnterminatedTypedConstant`, the LS derives an insertion point from `Diagnostic.Span.Offset + Diagnostic.Span.Length`. The LSP character position for an insert-only edit has `start == end` (zero-length range, `newText` is the inserted character):

```
TextEdit:
  range: { start: spanEnd, end: spanEnd }
  newText: "\""   (or "'")
```

The "did you mean?" codes (`UndeclaredField`, `UndeclaredState`, `UndeclaredEvent`, `UndeclaredFunction`) also produce mechanical text-edit code actions **when a suggestion is found** — these are the rename actions described in §3.1. The FixHint text (`"Declare the field at the top of the precept using 'field Name as Type'"`) is suppressed in the code action title when a suggestion is available; the rename action takes precedence.

##### 3.2.2 Tooltip-Only (Informational) Code Actions

When a FixHint is present but no automatable text edit can be derived, the code action still appears — it opens a VS Code information panel displaying the FixHint text. This is the VS Code "Show Fix" pattern: the lightbulb is visible, the action is clickable, but clicking it shows guidance rather than mutating the document.

**Affected diagnostics (representative, not exhaustive):**

| `DiagnosticCode` | FixHint (shown in tooltip) |
|---|---|
| `UndeclaredField` (no suggestion) | `Declare the field at the top of the precept using 'field Name as Type'` |
| `UndeclaredState` (no suggestion) | `Declare the state using 'state StateName' before referencing it` |
| `UndeclaredEvent` (no suggestion) | `Declare the event using 'event EventName' before referencing it` |
| `UndeclaredFunction` (no suggestion) | `Use a recognized built-in function name, or check the function catalog` |
| `NoInitialState` | `Add 'initial' to the first state the precept starts in` |
| `CircularComputedField` | `Restructure the computed fields to break the circular dependency` |
| `ConflictingAccessModes` | `Ensure each field has at most one access mode per state` |
| `TypeMismatch` | *(no FixHint — no lightbulb)* |
| `NonChoiceAssignedToChoice` | `Use a string literal from the declared choice set, or an event argument with a compatible choice type` |
| `ChoiceLiteralNotInSet` | `Use one of the declared values of the choice type` |

**Code action properties (tooltip-only):**

| Property | Value |
|---|---|
| Title | `Show fix hint` |
| Kind | `quickfix` |
| IsPreferred | `false` |
| Command | `precept.showFixHint` with the FixHint text as argument |

The `precept.showFixHint` command displays the FixHint text in a VS Code information message (`vscode.window.showInformationMessage`). It does not modify the document.

**Appearance in VS Code:**

```
  line 6:   field Amount as number nonnegative
            ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
            💡  Show fix hint
```

Quick-fix menu:
```
  ┌─────────────────────────────────────────────────────────────────┐
  │ 💡 Show fix hint                                                │
  │    Quick Fix                                                    │
  └─────────────────────────────────────────────────────────────────┘
```

After clicking, a VS Code notification appears:
```
  ┌──────────────────────────────────────────────────────────────────────────────────┐
  │ ℹ Restructure the computed fields to break the circular dependency              │
  └──────────────────────────────────────────────────────────────────────────────────┘
```

#### 7.10.3 When No Action Is Available

A diagnostic receives no code action lightbulb when **both** conditions hold:
- `DiagnosticMeta.FixHint` is null for its code, AND
- No "did you mean?" suggestion was found.

Examples: `TypeMismatch`, `QualifierMismatch`, `UnsatisfiableGuard`, `DivisionByZero`, `UnreachableState`. These diagnostics have no FixHint in the catalog and no suggestion pool. They show squiggles and Problems panel entries only.

---

#### 7.10.4 VS Code Integration

##### 4.1 Protocol Surface

Diagnostic enrichment operates entirely on the `textDocument/publishDiagnostics` notification. The LS mutates `Diagnostic.Message` before publishing; no new LSP capabilities or protocol extensions are required.

Code actions use the standard `textDocument/codeAction` request/response cycle.

##### 4.2 Capability Declaration

The LS must declare code action support in its server capabilities:

```json
"codeActionProvider": {
  "codeActionKinds": ["quickfix"],
  "resolveProvider": false
}
```

`resolveProvider: false` — all code action details (edits, commands) are fully populated at request time. No lazy resolution step is used.

##### 4.3 `textDocument/codeAction` Request Handling

VS Code sends a `textDocument/codeAction` request when the cursor enters a diagnostic span. The request carries:

```json
{
  "textDocument": { "uri": "..." },
  "range": { "start": {...}, "end": {...} },
  "context": {
    "diagnostics": [ /* diagnostics overlapping the range */ ],
    "only": ["quickfix"],
    "triggerKind": 1
  }
}
```

The handler must:

1. For each diagnostic in `context.diagnostics`, look up the corresponding enriched diagnostic and code action set.
2. Return an array of `CodeAction` objects. If no actions apply, return an empty array (not null, not an error).

Code action object shape (mechanical):
```json
{
  "title": "Rename to 'Approve'",
  "kind": "quickfix",
  "isPreferred": true,
  "diagnostics": [ /* the originating diagnostic */ ],
  "edit": {
    "changes": {
      "file:///path/to/file.precept": [
        {
          "range": {
            "start": { "line": 12, "character": 18 },
            "end":   { "line": 12, "character": 24 }
          },
          "newText": "Approve"
        }
      ]
    }
  }
}
```

Code action object shape (tooltip-only):
```json
{
  "title": "Show fix hint",
  "kind": "quickfix",
  "isPreferred": false,
  "diagnostics": [ /* the originating diagnostic */ ],
  "command": {
    "title": "Show fix hint",
    "command": "precept.showFixHint",
    "arguments": ["Restructure the computed fields to break the circular dependency"]
  }
}
```

##### 4.4 Ordering

When multiple code actions apply to the same diagnostic span, they are returned in this order:

1. `isPreferred: true` mechanical actions (rename, insert closing quote) — VS Code auto-applies these on `Fix All`
2. `isPreferred: false` tooltip-only actions
3. Any additional non-preferred actions

Within each group, maintain declaration order from the diagnostic list.

---

#### 7.10.5 Edge Cases and Guard Rails

##### 5.1 No Match Within Threshold

**Condition:** Levenshtein distance to all candidates in the suggestion pool is ≥ 4.

**Behavior:** The diagnostic message is published unchanged. No code action is registered for that diagnostic. The FixHint code action (if any) still applies per §3.2.

**Example:** `Submitttttt` (7 characters away from `Submit`) — no suggestion, no rename lightbulb. The FixHint tooltip lightbulb still appears if `FixHint` is set on the code.

##### 5.2 Multiple Candidates at Same Distance

**Condition:** Two or more candidates in the pool share the minimum Levenshtein distance.

**Behavior:** Pick alphabetically by name, case-insensitive ascending. Only one suggestion is surfaced — in the message and in the code action.

**Example:** A precept has fields `Approved` and `Approvee`. User types `Approve`. Both are distance 1. Tiebreak selects `Approved` (alphabetically before `Approvee`).

```
Field 'Approve' is not declared — did you mean 'Approved'?
```

##### 5.3 SemanticIndex Is Null

**Condition:** Compilation halted at lex or parse stage; `Compilation.Semantics` is null.

**Behavior:** The enrichment pass is skipped entirely. No diagnostics receive suggestion suffixes. FixHint code actions may still be registered for lex/parse diagnostics (e.g., `UnterminatedStringLiteral`, `UnterminatedTypedConstant`) because those actions do not depend on the semantic index — they operate on the raw `Diagnostic.Span` from the lexer.

**Rationale:** Lex errors precede type-checking. The symbol tables that back the suggestion pools (`UserFields`, `UserStates`, `UserEvents`, `Functions.All`) are not available. Attempting enrichment without them is not possible.

##### 5.4 Diagnostic Has No FixHint and No Suggestion

**Condition:** `DiagnosticMeta.FixHint` is null AND the enrichment pass found no match.

**Behavior:** No lightbulb. No code action. The squiggle and Problems panel entry appear normally.

**Examples:** `TypeMismatch`, `QualifierMismatch`, `UnsatisfiableGuard`. These diagnostics carry no FixHint in `Diagnostics.cs` and are not in the suggestion-eligible set. The user sees the diagnostic message only.

##### 5.5 Empty Suggestion Pool

**Condition:** A "did you mean?"-eligible diagnostic fires in a precept that has no declared fields (for `UndeclaredField`), no declared states (for `UndeclaredState`), no declared events (for `UndeclaredEvent`).

**Behavior:** The pool is empty; Levenshtein produces no candidates. Treat identically to §5.1 — no suggestion, no rename lightbulb.

**Example:** A brand-new file with only `precept Draft` and one field reference in a rule before any fields are declared. The `UndeclaredField` diagnostic fires but `UserFields` is empty; no suggestion is possible.

##### 5.6 Suggestion Identical to Failing Name

**Condition:** Levenshtein distance is 0 — the failing name exactly matches a candidate in the pool.

**Behavior:** Distance 0 is within the threshold. However, a distance-0 match means the name *is* declared, which would mean the error fires incorrectly — this is a compiler bug, not an enrichment case. The enrichment pass should not suppress or modify the diagnostic if this occurs; surface it as-is. Do not append a "did you mean 'X'?" suffix when the suggestion equals the failing name.

Implementation guard: `if (suggestion == Args[0]) → skip enrichment`.

##### 5.7 `Diagnostic.Span` Is `SourceSpan.Missing`

**Condition:** `Span.Length == 0 && Span.StartLine == 0` (the sentinel `SourceSpan.Missing`).

**Behavior:** Do not register a text-edit code action (there is no valid insertion point). If a suggestion exists, still enrich the message. If a FixHint is present and is tooltip-only, still register the tooltip action (it carries no `TextEdit`). A zero-extent span is valid for informational actions.

---

#### 7.10.6 Out of Scope

The following are explicitly not part of this spec. They are tracked separately or deferred to a later phase.

**Multi-suggestion menus.** This spec delivers one suggestion per diagnostic. A "pick from N near-matches" secondary menu is Phase 2.

**Partial-word match during active typing.** Suggestions are computed on the settled compilation result. Live-as-you-type fuzzy suggestions (IntelliSense-style) are a completions feature, not a diagnostic enrichment feature.

**Cross-precept symbol resolution.** Suggestion pools are scoped to the current precept file only. If a project-level symbol index ships, pool resolution can be extended; this spec does not define that path.

**Automated multi-step fixes.** `CircularComputedField`, `ConflictingAccessModes`, and similar multi-cause diagnostics cannot be resolved with a single text edit. No automation is attempted for these; they remain tooltip-only per §3.2.2.

**`textDocument/codeAction` kind `refactor`.** Only `quickfix` actions are specified here. Refactor and source-action kinds are out of scope.

**MCP `args` field.** The addition of `args: string[]` to `precept_compile` diagnostic output (Q6) is a separate catalog change with its own implementation path. This spec covers only the language server surfaces.

**`Fix All in File` behavior.** VS Code's built-in "Fix All" applies all `isPreferred` code actions in a file sequentially. No special handling is required from the LS; this falls out of `isPreferred: true` on rename actions. Multi-action ordering and conflict resolution are not specified here.

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

**Rationale:** Clear ownership prevents bugs where features read stale or inappropriate data. If hover walks `ConstructManifest` instead of `SemanticIndex`, it might show wrong types. If preview reads `Compilation` instead of `Precept`, it might show incomplete execution plans.

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
> *Source: catalog-gap-register.md #35 (broader documentation-string strategy)*
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
| Parser and ConstructManifest | `docs/compiler/parser.md` |
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
