# Language Server

## 1. Status

| Property | Value |
|---|---|
| Doc maturity | Full |
| Implementation state | Phase 2 surface shipped: diagnostics, semantic tokens, completions, hover, definition, references, document highlights, rename, signature help, document symbols, workspace symbols across open documents, folding, selection ranges, and code actions. Preview/inspect remains design-only in this document. |
| Source | `tools/Precept.LanguageServer/` |
| Upstream | Compiler (`Compilation` artifact); runtime preview remains planned |
| Downstream | VS Code extension (via LSP protocol) |

---

## Contents

- [2. Overview](#2-overview)
- [3. Responsibilities and Boundaries](#3-responsibilities-and-boundaries)
- [4. Right-Sizing](#4-right-sizing)
- [5. Inputs and Outputs](#5-inputs-and-outputs)
- [6. Architecture](#6-architecture)
  - [In-Process Compilation Model](#in-process-compilation-model)
  - [Document State Lifecycle](#document-state-lifecycle)
  - [LSP Feature Routing and Artifact Consumption](#lsp-feature-routing-and-artifact-consumption)
  - [Atomic Swap Concurrency](#atomic-swap-concurrency)
- [7. Component Mechanics](#7-component-mechanics)
  - [7.1 Diagnostics Push](#71-diagnostics-push)
  - [7.2 Semantic Tokens (Two-Pass Design)](#72-semantic-tokens-two-pass-design)
  - [7.3 Catalog-Driven Completions](#73-catalog-driven-completions)
  - [7.4 Hover](#74-hover)
  - [7.5 Go-to-Definition](#75-go-to-definition)
  - [7.6 Preview/Inspect (Custom LSP Extension)](#76-previewinspect-custom-lsp-extension)
  - [7.7 Document Outline](#77-document-outline)
  - [7.8 Folding Ranges](#78-folding-ranges)
  - [7.9 Diagnostic Enrichment ("Did You Mean?")](#79-diagnostic-enrichment-did-you-mean)
  - [7.10 Code Actions](#710-code-actions)
- [8. Dependencies and Integration Points](#8-dependencies-and-integration-points)
- [9. Failure Modes and Recovery](#9-failure-modes-and-recovery)
- [10. Contracts and Guarantees](#10-contracts-and-guarantees)
- [11. Design Rationale and Decisions](#11-design-rationale-and-decisions)
  - [Decision 1: In-Process Compilation](#decision-1-in-process-compilation)
  - [Decision 2: Full-Pipeline Recompile on Change](#decision-2-full-pipeline-recompile-on-change)
  - [Decision 3: Atomic Swap Concurrency](#decision-3-atomic-swap-concurrency)
  - [Decision 4: Catalog-Driven Completions](#decision-4-catalog-driven-completions)
  - [Decision 5: Two-Pass Semantic Tokens](#decision-5-two-pass-semantic-tokens)
  - [Decision 6: Feature-to-Artifact Hard Rules](#decision-6-feature-to-artifact-hard-rules)
- [12. Innovation](#12-innovation)
- [13. Open Questions / Implementation Notes](#13-open-questions-implementation-notes)
  - [Implementation Status](#implementation-status)
  - [Open Questions](#open-questions)
  - [Implementation Notes](#implementation-notes)
- [14. Deliberate Exclusions](#14-deliberate-exclusions)
- [15. Cross-References](#15-cross-references)
- [16. Source Files](#16-source-files)

## 2. Overview

The language server implements the LSP protocol for Precept `.precept` files. The shipped surface includes diagnostics, semantic tokens, completions, hover, go-to-definition, references, document highlights, rename, signature help, document outline, workspace symbols across open documents, folding, selection ranges, and code actions. It consumes pipeline artifacts by responsibility — each feature reads from exactly the artifact that owns the information it needs.

> **Current state:** The OmniSharp host and protocol test host share the same handler registration surface through `LanguageServerComposition`. Preview/inspect is intentionally not part of the shipped language-server track yet; the rest of this document remains the design contract for the implemented and planned feature surfaces.

The LS is NOT a pipeline stage. It's a hosting layer that runs the pipeline on demand and projects results to the editor. The compiler does all semantic work; the LS routes requests, manages document state, and translates between LSP protocol types and Precept artifacts.

---

## 3. Responsibilities and Boundaries

**OWNS:** LSP message handling, per-feature artifact routing, `Compilation` lifecycle, version-ordered document state management, and translation between compiler artifacts and LSP protocol shapes.

**Does NOT OWN:** Compilation logic (`src/Precept/`), grammar (generated from catalogs), MCP tooling, diagnostic production (compiler), semantic analysis (type checker), or preview/inspection runtime behavior.

---

## 4. Right-Sizing

The LS is thin — a routing and protocol layer over the compiler and runtime. All intelligence lives in the compiler artifacts and catalogs. The LS does not add semantic knowledge; it projects what the compiler already knows to the editor. An LS feature that reaches into catalog data or re-implements any compiler logic is a design violation.

| Metric | Value | Rationale |
|---|---|---|
| Estimated LOC | 400–600 | Routing + translation, not semantic logic |
| Semantic logic | 0% | All semantic knowledge lives in compiler artifacts |
| Catalog access | Read-only | For completion suggestions and hover text only |
| Custom LSP methods | 0 shipped (1 planned) | `precept/inspect` remains preview-design work, not part of the current LS surface |

---

## 5. Inputs and Outputs

**Input:** LSP requests from editor (document open/change, completion request, hover request, etc.)

**Output:** LSP responses (diagnostics, completion items, hover markup, semantic tokens, go-to-definition locations, references, document highlights, rename edits, signature help, document/workspace symbols, folding ranges, selection ranges, and code actions)

**Internal:** Holds one `DocumentState` per open document:

```csharp
sealed class DocumentState
{
    public Compilation? Current => Volatile.Read(ref _snapshot).Current;
    public IReadOnlyDictionary<DiagnosticKey, SuggestionInfo>? Suggestions => Volatile.Read(ref _snapshot).Suggestions;
    public int Version => Volatile.Read(ref _snapshot).Version;

    public bool TryUpdate(
        int version,
        Compilation compilation,
        IReadOnlyDictionary<DiagnosticKey, SuggestionInfo> suggestions);
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
| Lexical semantic tokens (Pass 1) | `TokenStream` + `TokenMeta.VisualCategory` → `SemanticTokenTypes` | Lexer |
| Identifier semantic tokens (Pass 2) | `SemanticIndex` reference bindings | TypeChecker |
| Completions | Catalogs + `ConstructManifest` context + `SymbolTable` + `SemanticIndex` | Parser + NameBinder + TypeChecker |
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

DiagnosticSeverity MapSeverity(Severity severity) => severity switch
{
    Severity.Error   => DiagnosticSeverity.Error,
    Severity.Warning => DiagnosticSeverity.Warning,
    Severity.Info    => DiagnosticSeverity.Information,
    _                => DiagnosticSeverity.Error
};
```

**Contract:** Every LSP diagnostic corresponds 1:1 to a `Diagnostic` in `Compilation.Diagnostics`. The LS never synthesizes diagnostics.

### 7.2 Semantic Tokens (Two-Pass Design)

Semantic tokens enable rich syntax highlighting beyond what TextMate grammars can express. Precept uses a **two-pass design** — lexical tokens from Pass 1, semantic identifier classification from Pass 2.

#### Pass 1: Lexical Semantic Tokens

**Trigger:** `textDocument/semanticTokens/full`

**Artifact:** `Compilation.Tokens` + `TokenMeta.VisualCategory` + `SemanticTokenTypes`

`Compilation.Tokens` carries the `TokenStream` from the lexer pass — added to the `Compilation` record (CC#4). Lexical semantic tokens have no semantic-index dependency and work even when type checking fails.

**Mechanics:**

Pass 1 walks the token stream and emits semantic tokens by projecting each token's `VisualCategory` through the `SemanticTokenTypes` catalog. This pass requires only the lexer — it works even when the type checker fails. The OmniSharp handler still must advertise a semantic-token legend and full-document registration options; `SemanticTokensBuilder` handles the LSP wire-format delta encoding against that legend.

```csharp
SemanticTokens BuildLexicalTokens(Compilation compilation)
{
    var builder = new SemanticTokensBuilder();
    
    foreach (var token in compilation.Tokens)
    {
        var meta = Tokens.GetMeta(token.Kind);
        if (!meta.VisualCategory.HasValue) continue;
        
        builder.Push(
            line: token.Span.StartLine - 1,
            character: token.Span.StartColumn - 1,
            length: token.Span.Length,
            tokenType: SemanticTokenTypes.GetMeta(meta.VisualCategory.Value).CustomType,
            tokenModifiers: 0);  // No SemanticTokenModifiers on TokenMeta — lexical tokens carry zero modifier bits
    }
    
    return builder.Build();
}
```

`SemanticTokenTypes.All` in `src/Precept/Language/SemanticTokenTypes.cs` is the semantic-token legend source. The same catalog also drives the startup `precept/semanticTokenColors` notification, so custom token types plus runtime color/bold/italic styling stay in one catalog-owned metadata surface.

#### Pass 2: Identifier Semantic Tokens

**Trigger:** `textDocument/semanticTokens/full` (merged with Pass 1)

**Artifact:** `Compilation.Semantics` reference bindings

**Mechanics:**

Pass 2 walks `SemanticIndex` symbol tables and classifies identifier tokens by their semantic role. This pass requires the type checker to complete successfully. Declaration and reference spans must be the bare identifier token spans (for example the `PartyName` segment of `JoinWaitlist.PartyName`), because OmniSharp's semantic-token delta encoder assumes the emitted stream is strictly ordered and non-overlapping.

```csharp
void AddIdentifierTokens(SemanticTokensBuilder builder, SemanticIndex index)
{
    // Declaration spans
    foreach (var field in index.Fields)
    {
        // TypedField has Syntax but not NameSpan — extract name span from ParsedConstruct
        var nameSpan = ExtractFieldNameSpan(field);
        AddIdentifier(builder, nameSpan, "property");
    }
    
    foreach (var state in index.States)
    {
        AddIdentifier(builder, state.NameSpan, "enum");
    }
    
    foreach (var evt in index.Events)
    {
        AddIdentifier(builder, evt.NameSpan, "function");
        foreach (var arg in evt.Args)
        {
            AddIdentifier(builder, arg.Span, "parameter");
        }
    }
    
    // Reference sites (CC#3 — first-class collections on SemanticIndex)
    foreach (var fr in index.FieldReferences)
        AddIdentifier(builder, fr.Site, "property");
    
    foreach (var sr in index.StateReferences)
        AddIdentifier(builder, sr.Site, "enum");
    
    foreach (var er in index.EventReferences)
        AddIdentifier(builder, er.Site, "function");
    
    // ArgReferences — thin core prerequisite (Slice 3)
    foreach (var ar in index.ArgReferences)
        AddIdentifier(builder, ar.Site, "parameter");
}
```

**Pass 2 reference sites:** `SemanticIndex` carries first-class reference-site collections populated by the type checker at resolution time (CC#3): `FieldReferences` (`ImmutableArray<FieldReference>`), `StateReferences` (`ImmutableArray<StateReference>`), `EventReferences` (`ImmutableArray<EventReference>`), and `ArgReferences` (`ImmutableArray<ArgReference>`). Each reference record holds the resolved typed declaration and the reference `Site` span. Pass 2 projects these collections directly — no expression-tree walking needed. Qualified event-argument references record only the member identifier span, not the enclosing `Event.Arg` expression span, so semantic-token overlays, definition, references, highlights, and rename all target the actual editable identifier token.

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

> **✅ Resolved (CC#14) — SlotContext vs ConstructSlotKind naming**
> `SlotContext` is the canonical cursor-context enum name. The mapping switch uses `ConstructSlotKind` members (the catalog type name for construct schema slots) in the switch arms, not a `SlotKind` alias. Two distinct concepts: `SlotContext` = where is the cursor (completion routing); `ConstructSlotKind` = what schema slot is this (catalog). The mapping function maps `ConstructSlotKind` → `SlotContext`.
> *Resolved: 2026-05-06 — CC#14*

SlotContext GetCursorContext(ConstructManifest manifest, Position position)
{
    // Find the innermost construct containing the cursor
    var construct = FindConstructAt(manifest, position);
    if (construct is null) return SlotContext.TopLevel;
    
    // Find which slot the cursor is in
    var slotIndex = FindSlotAt(construct, position);
    if (slotIndex < 0) return SlotContext.AfterKeyword;
    
    // Map ConstructSlotKind (catalog schema slot) → SlotContext (cursor completion context)
    return construct.Meta.Slots[slotIndex].Kind switch
    {
        ConstructSlotKind.TypeExpression => SlotContext.InTypePosition,
        ConstructSlotKind.ModifierList => SlotContext.InModifierPosition,
        ConstructSlotKind.StateTarget => SlotContext.InStateTarget,
        ConstructSlotKind.EventTarget => SlotContext.InEventTarget,
        ConstructSlotKind.FieldTarget => SlotContext.InFieldTarget,
        ConstructSlotKind.ActionChain => SlotContext.InActionVerb,
        ConstructSlotKind.GuardClause or ConstructSlotKind.ComputeExpression
            or ConstructSlotKind.EnsureClause => SlotContext.InExpression,
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
        SlotContext.InModifierPosition => GetModifierCompletions(GetCurrentConstructKind(), GetCurrentValueType()),
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
        .Where(t => t.Token is not null)  // CC#16: Token != null is structurally equivalent to user-facing (internal types Error, StateRef have Token = null)
        .Select(t => new CompletionItem
        {
            Label = t.Token!.Text,
            Kind = CompletionItemKind.TypeParameter,
            Documentation = t.HoverDescription,
            InsertText = t.Token!.Text,
            InsertTextFormat = InsertTextFormat.PlainText
        });
}

IEnumerable<CompletionItem> GetActionCompletions(TypeKind? fieldType)
{
    return Actions.All
        .Where(a => fieldType is null || a.ApplicableTo.Any(t => t.Kind == fieldType))
        .Select(a => new CompletionItem
        {
            Label = a.Token.Text,
            Kind = CompletionItemKind.Method,
            Documentation = a.HoverDescription ?? a.Description,
            InsertText = a.SnippetTemplate ?? a.Token.Text,
            InsertTextFormat = a.SnippetTemplate is not null
                ? InsertTextFormat.Snippet
                : InsertTextFormat.PlainText
        });
}

// ActionMeta already carries HoverDescription, UsageExample, and SnippetTemplate.

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
| `ApplicableTo` | Filter by context (actions and value modifiers → current field/argument type) |

### 7.4 Hover

**Trigger:** `textDocument/hover`

**Artifact:** `SemanticIndex` + catalog documentation

**Mechanics:**

Hover finds the symbol at the cursor position via `SemanticIndex`, then formats documentation from catalog metadata.

```csharp
Hover? GetHover(Compilation compilation, Position position)
{
    var token = FindTokenAt(compilation.Tokens, position);
    if (token is null) return null;
    
    // Surface-token hover — catalog lookup
    var meta = Tokens.GetMeta(token.Kind);
    if (token.Kind != TokenKind.Identifier && meta.Text is not null)
    {
        return new Hover
        {
            Contents = new MarkupContent
            {
                Kind = MarkupKind.Markdown,
                Value = meta.Description
            },
            Range = ToLspRange(token.Span)
        };
    }
    
    // Identifier hover — semantic lookup
    if (token.Kind == TokenKind.Identifier)
    {
        var symbol = FindSymbolAt(compilation.Semantics, position);
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
        Value = $"**argument** `{a.Name}` : `{Types.GetMeta(a.ResolvedType).Token.Text}` on event `{a.EventName}`"
        // a.EventName is a first-class back-reference on TypedArg — see CC#17
    },
    
    _ => new MarkupContent { Kind = MarkupKind.PlainText, Value = "" }
};
```

> **✅ Resolved (CC#17) — TypedArg.EventName back-reference**
> `TypedArg.EventName` is already present as a first-class field in the canonical `type-checker.md §7.1` shape. The hover code above accesses `a.EventName` directly. No reconstruction via `ArgReference` traversal needed.
> *Resolved: 2026-05-06 — CC#17*

### 7.5 Go-to-Definition

**Trigger:** `textDocument/definition`

**Artifact:** `SemanticIndex` reference binding → `ParsedConstruct.Syntax` back-pointer

**Mechanics:**

Go-to-definition resolves the reference at the cursor to its declaration using `SemanticIndex` back-pointers. Every `Typed*` record carries a `Syntax` property pointing to its declaring `ParsedConstruct`, which carries a `SourceSpan`.

```csharp
Location? GetDefinition(Compilation compilation, Position position)
{
    var index = compilation.Semantics;
    
    // Find which reference site the cursor overlaps
    // FieldReference, StateReference, EventReference each carry the resolved declaration
    SourceSpan? declarationSpan = null;
    
    foreach (var fr in index.FieldReferences)
        if (Contains(fr.Site, position))
        { declarationSpan = ExtractFieldNameSpan(fr.Field); break; }
    
    foreach (var sr in index.StateReferences)
        if (Contains(sr.Site, position))
        { declarationSpan = sr.State.NameSpan; break; }
    
    foreach (var er in index.EventReferences)
        if (Contains(er.Site, position))
        { declarationSpan = er.Event.NameSpan; break; }
    
    // ArgReferences — thin core prerequisite (Slice 3)
    foreach (var ar in index.ArgReferences)
        if (Contains(ar.Site, position))
        { declarationSpan = ar.Arg.Span; break; }
    
    if (declarationSpan is null) return null;
    
    return new Location
    {
        Uri = documentUri,  // tracked by DocumentStore, not on Compilation
        Range = ToLspRange(declarationSpan.Value)
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

**Trigger:** `precept/inspect` (custom method, not standard LSP)

> **✅ Resolved (CC#15) — precept/inspect is canonical**
> `precept/inspect` aligns with the MCP tool name `precept_inspect`. The trigger line `precept/preview` has been corrected here. All extension handlers and docs use `precept/inspect`.
> *Resolved: 2026-05-06 — CC#15*

**Artifact:** `Precept` + inspection runtime (`EventInspection`, `UpdateInspection`)

**Mechanics:**

Preview is a non-standard LSP extension that enables the VS Code extension's preview webview. It shows what each event would do if fired, without actually executing it.

The LS is a thin wrapper: it serializes the runtime result directly. It does not compute, diff, generate prose, or patch field names. `EventName` is on every `EventInspection` from the runtime — no name-patching needed.

```csharp
// Custom request type
[Method("precept/inspect")]
record InspectParams(
    DocumentUri Uri,
    string? CurrentState,
    JsonElement Data,
    string? Event,
    JsonElement? EventArgs);

[Method("precept/inspect")]
object? HandleInspect(InspectParams p)
{
    if (!_documents.TryGetValue(p.Uri, out var state) || state.Precept is not { } precept)
        return null;

    if (precept.Restore(p.CurrentState, p.Data) is not Restored restored)
        return null;

    var version = restored.Result;

    return p.Event is not null
        ? version.InspectFire(p.Event, p.EventArgs)
        : version.InspectUpdate(null);
}
```

**Canonical `EventInspection` shape:** Defined in `docs/runtime/result-types.md`. The runtime produces a complete, self-describing `EventInspection` with `EventName`, `DeclaredArgs`, `ArgErrors`, `CurrentFields` (pre-mutation, captured once before row loop), `Transitions` (each with `RowEffect`, `GuardSummary`, and `PostFields`), and `EventEnsures`. The LS handler serializes this result without transformation.

**What was removed from the LS surface:**
- `Explanation` — prose generation is a display concern, not a data contract. The webview synthesizes display text from the structured result.
- `BeforeFields` / `AfterFields` — superseded by `CurrentFields` (pre-mutation, top-level) + `TransitionInspection.PostFields` (per-row projected post-state). The previous shape collapsed per-row post-state into a single `AfterFields`, losing the distinction between rows.
- `MapToResult(inspection)` enrichment — eliminated. The runtime result is the primary artifact; LS and MCP serialize, they do not compute.

**Calling patterns:**
- `version.InspectFire(eventName, args?)` — single-event inspection with optional args
- `version.InspectUpdate(null)` — full landscape; `UpdateInspection.Events` contains a complete `EventInspection` for every event in the current state

The extension calls `precept/inspect` whenever the user changes preview state, then renders the result in the preview webview. See `docs/tooling/extension.md` for the webview side.

### 7.7 Document Outline

**Trigger:** `textDocument/documentSymbol`

**Artifact:** `ConstructManifest.Constructs`

**Mechanics:**

Document outline provides hierarchical symbols for the document sidebar. The LS walks `ConstructManifest.Constructs` and maps each to a `DocumentSymbol` using `ConstructMeta.IsOutlineNode` and `ConstructMeta.OutlineSymbolTag` — no hardcoded `ConstructKind` switches.

```csharp
DocumentSymbol[] GetDocumentSymbols(Compilation compilation)
{
    return compilation.ConstructManifest.Constructs
        .Where(c => c.Meta.IsOutlineNode)    // CC#18: catalog-driven, no ConstructKind switch
        .Select(c => ToDocumentSymbol(c))
        .ToArray();
}

> **✅ Resolved (CC#18) — ConstructMeta.IsOutlineNode and OutlineSymbolTag**
> Outline eligibility and LSP symbol kind are first-class `ConstructMeta` fields. `IsOutlineNode = false` by default; outline constructs set `IsOutlineNode = true` and `OutlineSymbolTag = "Module"` etc. This matches the semantic-token catalog pattern — the LS reads catalog metadata rather than maintaining a per-`ConstructKind` switch. Adding a new outline-able construct only requires updating its catalog entry.
> *Resolved: 2026-05-06 — CC#18*

DocumentSymbol ToDocumentSymbol(ParsedConstruct construct) => new()
{
    Name = ExtractName(construct),
    Kind = Enum.Parse<SymbolKind>(construct.Meta.OutlineSymbolTag ?? "Null"),  // catalog-driven
    Range = ToLspRange(construct.Span),
    SelectionRange = ToLspRange(ExtractNameSpan(construct))
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
            Kind = FoldingRangeKind.Region  // Precept has no comment constructs
        })
        .ToArray();
}
```

---

### 7.9 Diagnostic Enrichment ("Did You Mean?")

#### 7.9.1 Trigger Conditions

The enrichment pass runs inside `OnCompilationComplete`, after the full pipeline has settled. It processes only the four diagnostic codes listed in §2.4. All other codes are ineligible.

**Suggestion-pool resolution.** `Compilation.Semantics` is non-nullable (always present), so enrichment is a per-diagnostic decision rather than a null-guarded pass. For each eligible diagnostic, resolve the candidate pool from `DiagnosticMeta.SuggestionSources`: fields from `Semantics.Fields`, states from `Semantics.States`, events from `Semantics.Events`, or built-ins from `Functions.All`. If the chosen pool is empty, publish the diagnostic unchanged.

```
OnCompilationComplete:
  for each Diagnostic d in compilation.Diagnostics:
    if d.Code has SuggestionSources metadata:
      resolve candidate pool for that source
      if pool is empty → publish unchanged
      else → attempt fuzzy match
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
| `UndeclaredField` | All field names declared in the precept (`SemanticIndex.Fields`) | The failing field name as written in source |
| `UndeclaredState` | All state names declared in the precept (`SemanticIndex.States`) | The failing state name as written in source |
| `UndeclaredEvent` | All event names declared in the precept (`SemanticIndex.Events`) | The failing event name as written in source |
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
| Command | `precept.showFixHint` with a structured payload containing `fixHint` and optional `exampleBefore` / `exampleAfter` |

The `precept.showFixHint` command is implemented by the VS Code extension. It displays the `FixHint` text and may render richer before/after examples when `DiagnosticMeta.ExampleBefore` and `ExampleAfter` are available. It does not modify the document.

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

VS Code sends a `textDocument/codeAction` request on demand for the current range (for example via the lightbulb or `Ctrl+.`). The request carries:

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

1. For each diagnostic in `context.diagnostics`, match it back to the server-side enrichment/code-action metadata by a stable diagnostic identity (at minimum code + range; if needed include message-shape/original args to avoid collisions).
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
    "arguments": [
      {
        "fixHint": "Restructure the computed fields to break the circular dependency",
        "exampleBefore": null,
        "exampleAfter": null
      }
    ]
  }
}
```

##### 4.4 Ordering

When multiple code actions apply to the same diagnostic span, they are returned in this order:

1. `isPreferred: true` mechanical actions (rename, insert closing quote) — VS Code presents these as the primary quick fix for the diagnostic
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

##### 5.3 SemanticIndex Has No Usable Symbols

**Condition:** Compilation halted at lex or parse stage; `Compilation.Semantics` contains empty symbol tables.

**Behavior:** The enrichment pass is skipped entirely. No diagnostics receive suggestion suffixes. FixHint code actions may still be registered for lex/parse diagnostics (e.g., `UnterminatedStringLiteral`, `UnterminatedTypedConstant`) because those actions do not depend on the semantic index — they operate on the raw `Diagnostic.Span` from the lexer.

**Rationale:** Lex errors precede type-checking. The symbol tables that back the suggestion pools (`Fields`, `States`, `Events`, `Functions.All`) are empty. Attempting enrichment against empty tables is harmless but produces no suggestions.

##### 5.4 Diagnostic Has No FixHint and No Suggestion

**Condition:** `DiagnosticMeta.FixHint` is null AND the enrichment pass found no match.

**Behavior:** No lightbulb. No code action. The squiggle and Problems panel entry appear normally.

**Examples:** `TypeMismatch`, `QualifierMismatch`, `UnsatisfiableGuard`. These diagnostics carry no FixHint in `Diagnostics.cs` and are not in the suggestion-eligible set. The user sees the diagnostic message only.

##### 5.5 Empty Suggestion Pool

**Condition:** A "did you mean?"-eligible diagnostic fires in a precept that has no declared fields (for `UndeclaredField`), no declared states (for `UndeclaredState`), no declared events (for `UndeclaredEvent`).

**Behavior:** The pool is empty; Levenshtein produces no candidates. Treat identically to §5.1 — no suggestion, no rename lightbulb.

**Example:** A brand-new file with only `precept Draft` and one field reference in a rule before any fields are declared. The `UndeclaredField` diagnostic fires but `Fields` is empty; no suggestion is possible.

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

**`Fix All in File` behavior.** Out of scope. `isPreferred` affects which quick fix VS Code highlights for a diagnostic; it does not define a `source.fixAll` surface. If Precept later ships file-wide fix-all behavior, it needs its own `source.fixAll` design.

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

**Choice:** Every `didChange` event triggers a complete lexer → parser → name binder → type checker → graph analyzer → proof engine pass.

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

**Choice:** Pass 1 (lexical) uses `TokenStream` + `TokenMeta.VisualCategory` projected through `SemanticTokenTypes`. Pass 2 (semantic) uses `SemanticIndex` reference bindings.

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

1. Text sync, diagnostics push, semantic tokens, completions, hover, go-to-definition, references, document highlights, rename, signature help, document symbols, workspace symbols, folding, selection ranges, and code actions are implemented in the current OmniSharp host.
2. Preview/inspect remains pending and is intentionally outside the shipped LS track described by Slice 28.
3. Semantic-token colors now publish at startup through the custom `precept/semanticTokenColors` notification, and the VS Code extension applies the catalog-projected rules into workspace `editor.semanticTokenColorCustomizations`.
4. Two-pass semantic token design remains the contract: Pass 1 uses `TokenStream` + `TokenMeta.VisualCategory` projected through `SemanticTokenTypes`; Pass 2 uses `SemanticIndex` bindings.

### Open Questions

> **Documentation strings across catalog entries — pattern settled:**
> `TokenMeta` uses `Description` (not `HoverDescription`) for token-level docs. `FieldModifierMeta.HoverDescription`, `TypeMeta.HoverDescription`, `FunctionMeta.HoverDescription`, and `OperatorMeta.HoverDescription` already exist. `ActionMeta.Description` / `HoverDescription` are the action-doc sources, and `ActionMeta.SnippetTemplate` already exists for insertion text. LS/MCP alignment for `ActionMeta`: documentation comes from catalog metadata; `SyntaxShape` is internal routing only. The design pattern is settled; remaining items are implementation milestones, not design questions.

> **Preview / inspect:** `precept/inspect` remains a design surface. The shipped language server stops at standard authoring/navigation/editor features; preview work is intentionally excluded from the current implementation track.

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
| **No project-wide symbol index** | `workspace/symbol` is limited to open `.precept` documents; the LS does not build a repository-wide index. |
| **No formatter / formatting-on-type** | Formatting remains out of scope. Quick fixes and code actions are covered by §7.10. |
| **No preview/inspect handler in the shipped host** | Preview remains a separate design track and is explicitly excluded from the current LS implementation surface. |
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
| Semantic token catalog | `src/Precept/Language/SemanticTokenTypes.cs` |

---

## 16. Source Files

| File | Purpose |
|---|---|
| `tools/Precept.LanguageServer/Program.cs` | Server bootstrap (OmniSharp initialization) |
| `tools/Precept.LanguageServer/LanguageServerComposition.cs` | Shared handler registration for `Program.cs` and `LspTestHost` |
| `tools/Precept.LanguageServer/DocumentState.cs` | Per-document versioned compilation snapshot + suggestion state |
| `tools/Precept.LanguageServer/DocumentStore.cs` | Open-document registry + workspace-symbol snapshot support |
| `tools/Precept.LanguageServer/Handlers/TextDocumentSyncHandler.cs` | Compile-on-open/change, diagnostics push, version ordering |
| `tools/Precept.LanguageServer/Handlers/SemanticTokensHandler.cs` | Two-pass semantic tokens + runtime semantic-token color notification |
| `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs` | Catalog-driven completions + typed-constant suggestions |
| `tools/Precept.LanguageServer/Handlers/SignatureHelpHandler.cs` | Function/accessor signature help |
| `tools/Precept.LanguageServer/Handlers/HoverHandler.cs` | Catalog-driven hover + semantic symbol hover |
| `tools/Precept.LanguageServer/Handlers/DefinitionHandler.cs` | Go-to-definition |
| `tools/Precept.LanguageServer/Handlers/ReferencesHandler.cs` | Symbol references |
| `tools/Precept.LanguageServer/Handlers/DocumentHighlightHandler.cs` | In-document symbol highlighting |
| `tools/Precept.LanguageServer/Handlers/RenameHandler.cs` | Single-document rename |
| `tools/Precept.LanguageServer/Handlers/DocumentSymbolHandler.cs` | Outline/document symbols |
| `tools/Precept.LanguageServer/Handlers/WorkspaceSymbolHandler.cs` | Workspace symbols across open documents |
| `tools/Precept.LanguageServer/Handlers/SelectionRangeHandler.cs` | Syntax-driven selection ranges |
| `tools/Precept.LanguageServer/Handlers/FoldingRangeHandler.cs` | Folding ranges |
| `tools/Precept.LanguageServer/Handlers/CodeActionHandler.cs` | Quick-fix code actions |
| `src/Precept/Language/SemanticTokenTypes.cs` | Semantic-token custom types plus runtime color/style metadata |
| `src/Precept/Pipeline/SemanticIndex.cs` | `SemanticIndex` shape consumed by LS |
