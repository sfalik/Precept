# Language Server Implementation Plan

> **Author:** Frank (Lead Architect) · **Date:** 2026-05-09 · **Branch:** Precept-V2-Radical  
> **Design source:** `docs/tooling/language-server.md` (Full maturity) · **Upstream research:** `research/architecture/compiler/language-server-integration-survey.md`

---

## 1. Design Summary

The Precept Language Server is a **thin routing and protocol layer** over `src/Precept/`. It calls `Compiler.Compile(source)` in-process on every document change, stores the resulting `Compilation` via `Interlocked.Exchange`, and projects pipeline artifacts to the editor through standard LSP handlers. All semantic knowledge lives in catalog metadata and compiler artifacts — the LS adds zero domain logic.

### Architecture Constraints (Non-Negotiable)

1. **No domain logic in the LS.** The LS routes requests and translates between LSP types and Precept artifacts.
2. **Catalog-driven completions/hover/semantic tokens.** The LS reads `Tokens`, `Types`, `Functions`, `Operators`, `Modifiers`, `Actions`, `Constructs`, `Diagnostics` catalogs — it never maintains parallel keyword lists.
3. **Feature-to-artifact hard rules.** Each LS feature reads from exactly one designated artifact (see §6 Consumer Artifact Map in `docs/tooling/language-server.md`). Violations are design bugs.
4. **Atomic swap concurrency.** `Compilation` is deeply immutable. `Interlocked.Exchange` for concurrent access. No locks.
5. **Graceful degradation.** When the type checker fails, Pass 2 semantic tokens and identifier-based features degrade — Pass 1 lexical tokens and keyword hover still work.

---

## Status

| Slice | Description | Status | Commit |
|-------|-------------|--------|--------|
| 0a | Catalog prereq — `IsOutlineNode` + `OutlineSymbolTag` | ✅ Done | `d85449ea` |
| 0 | Infrastructure — DocumentState, DocumentStore, compile loop | ✅ Done | `9f6b1fd7` |
| 0b | Delete shim files + 13 legacy tests | ✅ Done | `51d93dc2` |
| 1 | Diagnostics push (tests only — production was in Slice 0) | ✅ Done | `568ab5cc` |
| 2 | Semantic tokens Pass 1 (lexical) | ✅ Done | `9e679ceb` |
| 3 core | `ArgReference` plumbing in core pipeline | ✅ Done | `cba898b7` |
| 3 overlay | SemanticTokensHandler identifier overlays (Pass 2) | ✅ Done | `75b52baa` |
| 4 | Completions — catalog-driven | ✅ Done | `1ec3c7d5` |
| 5 | Hover — keyword + identifier docs | ✅ Done | `1fbecf36` |
| 5 fix | HoverHandler null-ref (TryFindUniqueByName secondary match) | ✅ Done | `69b9517d` |
| 6 | Go-to-definition + Document symbols | ✅ Done | `e144d92e` |
| 7 | Diagnostic enrichment ("did you mean?") | ✅ Done | `3a7c8c6b` |
| 8 | Code actions | ✅ Done | `c752af08` |
| 9 | Folding ranges | ✅ Done | `453e690a` |
| 11 | Program.cs final wiring | 🔄 In progress | — |

---

## 2. Gap Analysis

### 2.1 Design Completeness Assessment

The design at `docs/tooling/language-server.md` is **comprehensive and implementation-ready** for all 10 feature areas. Key findings:

| Capability | Design Status | Implementation Status | Gaps |
|---|---|---|---|
| **7.1 Diagnostics Push** | ✅ Complete | Stub only | None — design is implementation-ready |
| **7.2 Semantic Tokens (Two-Pass)** | ✅ Complete | Stub only | Thin prerequisite: add event-arg reference sites to `SemanticIndex` so Pass 2 stays projection-only |
| **7.3 Catalog-Driven Completions** | ✅ Complete | Stub only | `IsUserFacing` not on `TypeMeta` — resolved: `Token != null` is the permanent filter (no catalog change needed) |
| **7.4 Hover** | ✅ Complete | Stub only | None — use existing catalog doc fields (`Description`, `UsageExample`, `SyntaxReference`, per-type/member `HoverDescription`) rather than adding LS-owned prose |
| **7.5 Go-to-Definition** | ✅ Complete | Stub only | ✅ Resolved — `TypedField.NameSpan` now exists, so navigation and semantic tokens can target the declaration identifier span instead of the whole construct |
| **7.6 Preview/Inspect** | ✅ Complete | Out of scope | Inspect/restore are runtime state operations owned by MCP tools (`precept_inspect`, `precept_fire`); the LS only compiles — it does not manage running instances |
| **7.7 Document Outline** | ✅ Complete | Stub only | **Resolved:** `IsOutlineNode` and `OutlineSymbolTag` added as Slice 0a |
| **7.8 Folding Ranges** | ✅ Complete | Stub only | None — uses `ConstructManifest.Constructs` spans only |
| **7.9 Diagnostic Enrichment** | ✅ Complete | Stub only | None — `SuggestionSource` already on `DiagnosticMeta` |
| **7.10 Code Actions** | ✅ Complete | Stub only | None — George-15's `TriggerCondition`/`RecoverySteps`/`ExampleBefore`/`ExampleAfter` have landed on `DiagnosticMeta` |

### 2.2 Catalog Dependencies

| Gap | Catalog | Required Change | Blocking? |
|---|---|---|---|
| `IsOutlineNode` | `ConstructMeta` | Add `bool IsOutlineNode = false` and `string? OutlineSymbolTag = null` parameters | **Yes** — Document Outline slice depends on this. **Resolved:** Slice 0a adds these fields. |
| `IsUserFacing` | `TypeMeta` | Add `bool IsUserFacing = true` parameter; set `false` for `Error` and `StateRef` | **No** — filter by `Token != null` is structurally equivalent; no catalog change required |
| `TriggerCondition` / `RecoverySteps` / `ExampleBefore` / `ExampleAfter` | `DiagnosticMeta` | George-15 added these | **No** — ✅ **LANDED** (as of 2026-05-09). Fields are present on `DiagnosticMeta` in `src/Precept/Language/Diagnostics.cs` and populated on diagnostic entries. Slice 8 consumes `ExampleBefore` / `ExampleAfter` immediately; the other fields remain available for richer extension UX without forcing LS-local hover scaffolding. |

### 2.3 Clean-Pass Findings

The broad direction is right: the shim layer should not survive. But a clean read from the canonical LS design exposed several places where later slices quietly reintroduced scaffold-shaped APIs.

1. `LanguageServerStubs.cs` and `PreceptPreviewProtocol.cs` are both legacy compatibility surfaces and should be deleted rather than replaced with new LS-local façade types.
2. Diagnostics are publish-only and belong in the compile/update coordinator, not in a standalone request-handler slice.
3. `PreceptDocumentInfo`, `ClassifiedSemanticToken`, `LocationOrDocumentSymbol`, `ExtractConstraintSets`, `BuildConstraintSets`, and static catalog-coverage completion lists are test/scaffolding artifacts, not production LS architecture.
4. Request handlers should read shared document state and return standard LSP protocol objects directly.

**Key decision:** Keep Slice 0b, but remove shim-shaped helper APIs from Slices 1–10 instead of recreating them under new names.

### 2.4 Source-Shape Corrections

To stay aligned with current `src/Precept/` rather than older assumptions in the doc stack:

1. `Compilation.Semantics` is non-nullable; graceful degradation must tolerate empty or partial semantic facts, not null semantics.
2. `TokenMeta` does not carry `HoverDescription`; keyword hover must format from existing token/construct/syntax metadata (`Description`, `UsageExample`, `SyntaxReference`) rather than assuming a new token field.
3. `ConstructKind.Comment` does not exist; folding is construct-span-based and should not carry a comment-kind switch.
4. `SemanticIndex` exposes field/state/event reference collections but not event-arg reference sites; if LS features need arg references, the thin-layer answer is a tiny core prerequisite, not LS-side semantic rediscovery.
5. ✅ Resolved — `TypedField` now carries an exact declaration-name span via `TypedField.NameSpan`, so LS semantic tokens, go-to-definition, and document-symbol selection ranges can target the identifier instead of `field.Syntax.Span`.
6. ✅ Resolved — `TypedField.NameSpan` was added symmetrically with `TypedState` and `TypedEvent`; LS consumers should use it rather than extracting the field name span from `ParsedConstruct.Slots`. 

---

## 3. Implementation Slices

### Slice 0a: Catalog Prerequisite — `ConstructMeta.IsOutlineNode` + `OutlineSymbolTag`

**What:** Add two fields to `ConstructMeta` so the document outline (Slice 6) reads catalog metadata instead of maintaining a `ConstructKind` switch.

**Architectural resolution (2026-05-09):** The core catalog does NOT import LSP protocol types. `ConstructMeta` stores a `string? OutlineSymbolTag` — a plain string tag (e.g., `"Module"`, `"Property"`, `"Enum"`, `"Function"`). The LS projects this to `OmniSharp.Extensions.LanguageServer.Protocol.Models.SymbolKind` via `Enum.Parse<SymbolKind>(tag)`. This follows the same pattern as `TokenMeta.SemanticTokenType` — the catalog stores a string, the consumer projects to a wire type. No LSP dependency in `src/Precept/`.

**Modifies:**
- `src/Precept/Language/Construct.cs` — add two parameters to `ConstructMeta`:
  ```csharp
  public sealed record ConstructMeta(
      ConstructKind                        Kind,
      string                               Name,
      string                               Description,
      string                               UsageExample,
      ConstructKind[]                      AllowedIn,
      IReadOnlyList<ConstructSlot>         Slots,
      ImmutableArray<DisambiguationEntry>  Entries,
      RoutingFamily                        RoutingFamily,
      string?                              SnippetTemplate  = null,
      ModifierDomain                       ModifierDomain   = ModifierDomain.None,
      bool                                 IsOutlineNode    = false,
      string?                              OutlineSymbolTag = null);
  ```

- `src/Precept/Language/Constructs.cs` — set `IsOutlineNode: true` and `OutlineSymbolTag` on the following entries in `GetMeta`:

| ConstructKind | IsOutlineNode | OutlineSymbolTag | Rationale |
|---|---|---|---|
| `PreceptHeader` | `true` | `"Module"` | Top-level container — LSP `SymbolKind.Module` |
| `FieldDeclaration` | `true` | `"Property"` | Data member — LSP `SymbolKind.Property` |
| `StateDeclaration` | `true` | `"Enum"` | Finite set of named values — LSP `SymbolKind.Enum` |
| `EventDeclaration` | `true` | `"Function"` | Named action with arguments — LSP `SymbolKind.Function` |
| `RuleDeclaration` | `true` | `"Boolean"` | Constraint/invariant — LSP `SymbolKind.Boolean` |
| `TransitionRow` | `false` | — | Detail row, not an outline node |
| `StateEnsure` | `false` | — | Constraint detail, nested under state |
| `AccessMode` | `false` | — | Access detail, not an outline node |
| `OmitDeclaration` | `false` | — | Structural detail |
| `StateAction` | `false` | — | Hook detail |
| `EventEnsure` | `false` | — | Constraint detail, nested under event |
| `EventHandler` | `false` | — | Detail row in stateless precepts |

**Example code change in `Constructs.cs`:**

```csharp
ConstructKind.PreceptHeader => new(
    kind,
    "precept header",
    "File-level header that names the precept",
    "precept LoanApplication",
    [],
    [SlotIdentifierList],
    [new(TokenKind.Precept)],
    RoutingFamily.Header,
    IsOutlineNode: true,
    OutlineSymbolTag: "Module"),
```

**Tests:**
- `test/Precept.Tests/Language/ConstructCatalogTests.cs` (or existing catalog test file):
  - `[Fact] IsOutlineNode_TrueForOutlineConstructs` — verify `PreceptHeader`, `FieldDeclaration`, `StateDeclaration`, `EventDeclaration`, `RuleDeclaration` return `IsOutlineNode == true`
  - `[Fact] IsOutlineNode_FalseByDefault` — verify all other `ConstructKind` values return `IsOutlineNode == false`
  - `[Fact] OutlineSymbolTag_NonNullWhenIsOutlineNode` — every entry with `IsOutlineNode == true` has a non-null `OutlineSymbolTag`
  - `[Fact] OutlineSymbolTag_NullWhenNotOutlineNode` — every entry with `IsOutlineNode == false` has `OutlineSymbolTag == null`
- `test/Precept.LanguageServer.Tests/`:
  - `[Fact] DocumentSymbolHandler_ReturnsModuleForPreceptHeader` — `SymbolKind.Module` for precept header
  - `[Fact] DocumentSymbolHandler_ReturnsPropertyForFieldDeclaration` — `SymbolKind.Property` for fields
  - `[Fact] DocumentSymbolHandler_ReturnsEnumForStateDeclaration` — `SymbolKind.Enum` for states
  - `[Fact] DocumentSymbolHandler_ReturnsFunctionForEventDeclaration` — `SymbolKind.Function` for events

**UNBLOCKS:** Slice 6 (Go-to-Definition + Document Symbols). With this catalog prerequisite complete, Slice 6 reads `c.Meta.IsOutlineNode` / `c.Meta.OutlineSymbolTag` directly — no temporary `ConstructKind` switch.

**Dependency ordering:** Must complete before Slice 6. Independent of Slice 0, Slice 0b, Slices 1–5, and 7–11.

---

### Slice 0: Infrastructure — DocumentState + DocumentStore + Compile Loop

**What:** Establish the shared document-state services every request handler uses.

**Creates:**
- `tools/Precept.LanguageServer/DocumentState.cs` — per-document `Compilation` + `Precept` holder with atomic swap
- `tools/Precept.LanguageServer/DocumentStore.cs` — concurrent registry of open documents keyed by `DocumentUri`
- `tools/Precept.LanguageServer/DiagnosticProjector.cs` — `SourceSpan` / severity / code → LSP `Diagnostic` projection helpers
- `tools/Precept.LanguageServer/Handlers/TextDocumentSyncHandler.cs` — OmniSharp `TextDocumentSyncHandlerBase` (or equivalent `ITextDocumentSyncHandler` implementation) configured for full-text sync; compiles on open/change, updates the document store, publishes diagnostics, and removes state on close
- `test/Precept.LanguageServer.Tests/LspTestHost.cs` — reusable in-process LSP client/server harness for protocol-layer tests

**Modifies:**
- `tools/Precept.LanguageServer/Program.cs` — register document-store / projection services + sync handler in DI
- `test/Precept.LanguageServer.Tests/Precept.LanguageServer.Tests.csproj` — add the test-only client/harness dependency needed by `LspTestHost` (OmniSharp test helpers or equivalent)

**Method-level specificity:**

```csharp
sealed class DocumentStore
{
    public DocumentState GetOrAdd(DocumentUri uri) { ... }
    public bool TryGet(DocumentUri uri, out DocumentState state) { ... }
    public void Remove(DocumentUri uri) { ... }
}

sealed class DocumentState
{
    private volatile Compilation? _current;

    public Compilation? Current => _current;

    public void Update(Compilation compilation) { ... }  // Interlocked.Exchange
}

public Task<Unit> Handle(DidOpenTextDocumentParams p, CancellationToken ct) =>
    RecompileAndPublish(p.TextDocument.Uri, p.TextDocument.Text, ct);

public Task<Unit> Handle(DidChangeTextDocumentParams p, CancellationToken ct) =>
    RecompileAndPublish(p.TextDocument.Uri, p.ContentChanges.Single().Text, ct);

public Task<Unit> Handle(DidCloseTextDocumentParams p, CancellationToken ct)
{
    _documents.Remove(p.TextDocument.Uri);
    PublishDiagnostics(p.TextDocument.Uri, []);
}
```

**Tests:**
- No feature assertions yet, but land the reusable protocol test harness in this slice so later feature slices can exercise real LSP requests/responses instead of direct helper calls

**Regression anchors:** None — this is greenfield infrastructure.

**Dependency ordering:** This slice must complete before Slice 0b. All request-handler and wiring slices depend on it transitively.

---

### Slice 0b: Early Shim + Legacy Test Deletion

**Goal:** Delete the legacy shim surface and compiler-redundant LS tests before any clean handler code is written.

**Deletes:**
- `tools/Precept.LanguageServer/LanguageServerStubs.cs` — remove the entire shim surface (`PreceptParser`, `PreceptCompiler`, `PreceptAnalyzer`, `PreceptDocumentIntellisense`, `PreceptSemanticTokensHandler`, `PreceptPreviewHandler`, `PreceptCodeActionHandler`, etc.)
- `tools/Precept.LanguageServer/PreceptPreviewProtocol.cs` — remove the legacy action-based preview DTO graph
- 13 legacy shim-facing test files in `test/Precept.LanguageServer.Tests/` (173 compiler-redundant tests)

**Modifies:**
- None — this slice removes scaffolding only. No new production code is written here.

**Tests:**
- No new tests in Slice 0b. This slice only removes scaffolding.
- Run `dotnet build` to verify the LS project still compiles cleanly after deletion.
- Run `dotnet test test/Precept.Tests/` to verify core compiler tests are unaffected.

**Regression anchors:** `test/Precept.Tests/` remains the canonical compiler correctness suite.

**Dependency ordering:** Depends on Slice 0. All request-handler slices (1–10) and Slice 11 depend on Slice 0b so no new implementation is authored atop the shim layer.

---

### Slice 1: Diagnostics Push

**What:** Finalize the publish path around `Compilation.Diagnostics`. This is a projection/service slice, not a standalone request handler.

**Modifies:**
- `tools/Precept.LanguageServer/Handlers/TextDocumentSyncHandler.cs` — call `PublishDiagnostics(uri, compilation)` after each successful compile/update and clear diagnostics on close
- `tools/Precept.LanguageServer/DiagnosticProjector.cs` — direct 1:1 projection from `Compilation.Diagnostics`

**Method-level specificity:**

```csharp
static Diagnostic[] ProjectDiagnostics(Compilation compilation) =>
    compilation.Diagnostics.Select(d => new Diagnostic
    {
        Range = ToLspRange(d.Span),
        Severity = MapSeverity(d.Severity),
        Code = d.Code.ToString(),
        Source = "precept",
        Message = d.Message
    }).ToArray();

Task RecompileAndPublish(DocumentUri uri, string text, CancellationToken ct)
{
    var compilation = Compiler.Compile(text);
    _documents.GetOrAdd(uri).Update(compilation);
    PublishDiagnostics(uri, ProjectDiagnostics(compilation));
    return Unit.Task;
}
```

**Protocol-layer tests:**
- New LS tests asserting publish-diagnostics range/severity/code/message projection for representative compiler failures and warnings, including collection mismatches, null narrowing, rule warnings, and event-surface warnings

**Regression anchors:** All `test/Precept.Tests/` tests must continue passing (core pipeline unchanged).

**Dependency ordering:** Depends on Slices 0 and 0b. Slice 7 and Slice 8 build on this publish path.

---

### Slice 2: Semantic Tokens — Pass 1 (Lexical)

**What:** Walk `Compilation.Tokens` and emit standard LSP semantic tokens from `TokenMeta.SemanticTokenType`.

**Creates:**
- `tools/Precept.LanguageServer/Handlers/SemanticTokensHandler.cs` — OmniSharp `SemanticTokensHandlerBase` / `ISemanticTokensFullHandler` implementation with `SemanticTokensRegistrationOptions` (document selector + full provider + legend)
- Shared semantic-token projection helpers that operate on the current `Compilation` from `DocumentStore`

**Method-level specificity:**

```csharp
SemanticTokens BuildTokens(Compilation compilation)
{
    var builder = new SemanticTokensBuilder();
    AddLexicalTokens(builder, compilation.Tokens);
    return builder.Build();
}

void AddLexicalTokens(SemanticTokensBuilder builder, TokenStream tokens)
{
    foreach (var token in tokens)
    {
        var meta = Tokens.GetMeta(token.Kind);
        if (meta.SemanticTokenType is null) continue;
        builder.Push(token.Span.StartLine - 1, token.Span.StartColumn - 1, token.Span.Length, meta.SemanticTokenType, 0);
    }
}
```

**Protocol-layer tests:**
- New LS tests asserting lexical semantic-token classification is emitted as LSP semantic-token data

**Regression anchors:** All `test/Precept.Tests/` tests.

**Dependency ordering:** Depends on Slices 0 and 0b. Independent of Slice 1.

---

### Slice 3: Semantic Tokens — Pass 2 (Identifiers)

**What:** Add identifier overlays from semantic facts. Remove the last place the plan was still shaped by shim tests (`ClassifiedSemanticToken`, `ExtractConstraintSets`, `BuildConstraintSets`).

**Modifies:**
- `src/Precept/Pipeline/CheckContext.cs` — accumulate event-arg reference sites alongside field/state/event references
- `src/Precept/Pipeline/SemanticIndex.cs` — add `ArgReference` + `ImmutableArray<ArgReference> ArgReferences`
- `src/Precept/Pipeline/TypeChecker.Expressions.cs` — populate arg reference sites when resolving `TypedArgRef`
- `tools/Precept.LanguageServer/Handlers/SemanticTokensHandler.cs` — overlay declaration/reference tokens from `SemanticIndex`

**✅ RESOLVED: `TypedField.NameSpan`**
`TypedField` now carries `NameSpan` symmetrically with `TypedState` and `TypedEvent`. Pass 2 semantic tokens and Slice 6 go-to-definition/document-outline work should use `field.NameSpan` directly instead of targeting the full construct span or re-extracting from `Syntax.Slots`. 

**Method-level specificity:**

Pass 2 should stay projection-only. Rather than teaching the LS to rediscover event-arg references, add the missing arg-reference collection to the semantic artifact and project it exactly like the existing field/state/event collections.

```csharp
void AddIdentifierTokens(SemanticTokensBuilder builder, SemanticIndex index)
{
    foreach (var field in index.Fields)
    {
        var nameSpan = field.NameSpan;
        builder.Push(nameSpan.StartLine - 1, nameSpan.StartColumn - 1, field.Name.Length, "property", 0);
    }

    foreach (var state in index.States)
        builder.Push(state.NameSpan.StartLine - 1, state.NameSpan.StartColumn - 1, state.Name.Length, "enum", 0);

    foreach (var evt in index.Events)
    {
        builder.Push(evt.NameSpan.StartLine - 1, evt.NameSpan.StartColumn - 1, evt.Name.Length, "function", 0);
        foreach (var arg in evt.Args)
            builder.Push(arg.Span.StartLine - 1, arg.Span.StartColumn - 1, arg.Name.Length, "parameter", 0);
    }

    foreach (var fr in index.FieldReferences)
        builder.Push(fr.Site.StartLine - 1, fr.Site.StartColumn - 1, fr.Site.Length, "property", 0);
    foreach (var sr in index.StateReferences)
        builder.Push(sr.Site.StartLine - 1, sr.Site.StartColumn - 1, sr.Site.Length, "enum", 0);
    foreach (var er in index.EventReferences)
        builder.Push(er.Site.StartLine - 1, er.Site.StartColumn - 1, er.Site.Length, "function", 0);
    foreach (var ar in index.ArgReferences)
        builder.Push(ar.Site.StartLine - 1, ar.Site.StartColumn - 1, ar.Site.Length, "parameter", 0);
}
```

**Protocol-layer tests:**
- New LS tests asserting identifier semantic-token classification for declarations and references, including event arguments

**Regression anchors:** Slice 2 tests must remain green.

**Dependency ordering:** Depends on Slices 0, 0b, and 2. Slice 6 reuses the same arg-reference prerequisite.

---

### Slice 4: Completions — Catalog-Driven

**What:** Context-aware completions driven by catalogs and `SemanticIndex`.

**Creates:**
- `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs` — OmniSharp `ICompletionHandler` with `CompletionRegistrationOptions` (document selector, conservative trigger characters, `ResolveProvider = false`)
- `tools/Precept.LanguageServer/SlotContext.cs` — cursor-context enum + `GetCursorContext` logic
- Shared completion projection helpers over the current `Compilation`

**Method-level specificity:**

```csharp
enum SlotContext
{
    TopLevel,
    AfterKeyword,
    InTypePosition,
    InModifierPosition,
    InStateTarget,
    InEventTarget,
    InFieldTarget,
    InActionVerb,
    InExpression,
    InArgDefault,
}

static SlotContext GetCursorContext(ConstructManifest manifest, Position position) { ... }

CompletionList GetCompletions(Compilation compilation, Position position)
{
    var context = GetCursorContext(compilation.ConstructManifest, position);
    return context switch
    {
        SlotContext.TopLevel        => FromConstructCatalog(),
        SlotContext.InTypePosition  => FromTypesCatalog(),
        SlotContext.InModifierPosition => FromModifierCatalog(compilation, position),
        SlotContext.InStateTarget   => FromStates(compilation.Semantics),
        SlotContext.InEventTarget   => FromEvents(compilation.Semantics),
        SlotContext.InFieldTarget   => FromFields(compilation.Semantics),
        SlotContext.InActionVerb    => FromActions(compilation, position),
        SlotContext.InExpression    => FromExpressionScope(compilation, position),
        _                           => CompletionList.Empty,
    };
}
```

**Design correction:** Do not expose static catalog-coverage properties (`TypeItems`, etc.) as production API. Tests should validate the actual completion projector / handler outputs, not keep a second LS surface alive for compatibility.

**Protocol-layer tests:**
- New LS tests asserting `CompletionItem[]` for all completion contexts, including edit-mode scenarios

**Regression anchors:** Slices 1–3 tests.

**Dependency ordering:** Depends on Slices 0 and 0b. Independent of Slices 1–3 for implementation.

---

### Slice 5: Hover

**What:** Documentation on hover for keywords and identifiers.

**Creates:**
- `tools/Precept.LanguageServer/Handlers/HoverHandler.cs` — OmniSharp `IHoverHandler`
- Shared hover projection helpers over the current `Compilation`

**Method-level specificity:**

```csharp
Hover? CreateHover(Compilation compilation, Position position)
{
    var token = FindTokenAt(compilation.Tokens, position);
    if (token is null) return null;

    if (TryCreateKeywordHover(token, out var keywordHover))
        return keywordHover;

    return token.Kind == TokenKind.Identifier
        ? TryCreateIdentifierHover(compilation.Semantics, position)
        : null;
}
```

**Keyword-hover source:** use the metadata that actually exists today — `TokenMeta.Description`, related `ConstructMeta.Description` / `UsageExample`, and `SyntaxReference` for grammar-wide rules. Do **not** assume a new `TokenMeta.HoverDescription` field.

**Design correction:** Remove the diagnostic-code hover path. In LSP, diagnostics surface through `publishDiagnostics` and code actions; hover operates on source tokens/symbols.

**Protocol-layer tests:**
- New LS tests asserting `Hover` / `MarkupContent` payloads for keyword docs, identifier docs, and proof-related semantic details already present on the compiled model

**Regression anchors:** Slices 1–4 tests.

**Dependency ordering:** Depends on Slices 0 and 0b. Independent of Slices 1–4 for implementation.

---

### Slice 6: Go-to-Definition + Document Symbols

**What:** Navigate to field/state/event/event-arg declarations. Document outline for the sidebar.

**Creates:**
- `tools/Precept.LanguageServer/Handlers/DefinitionHandler.cs` — OmniSharp `IDefinitionHandler`
- `tools/Precept.LanguageServer/Handlers/DocumentSymbolHandler.cs` — OmniSharp `IDocumentSymbolHandler`
- Shared navigation / outline projection helpers over `SemanticIndex` and `ConstructManifest`

**Method-level specificity:**

```csharp
LocationOrLocationLinks HandleDefinition(DocumentUri uri, Compilation compilation, Position position)
{
    // 1. Find the overlapping FieldReference / StateReference / EventReference / ArgReference
    // 2. Resolve target declaration span via Syntax / NameSpan / arg Span
    // 3. Return the standard definition response type directly (no custom union wrapper)
}

DocumentSymbolContainer BuildDocumentSymbols(Compilation compilation)
{
    // Walk ConstructManifest.Constructs
    // Filter: c.Meta.IsOutlineNode == true
    // Map: Enum.Parse<SymbolKind>(c.Meta.OutlineSymbolTag)
    // Return standard DocumentSymbol objects directly
}
```

**Catalog dependency:** ✅ Resolved by Slice 0a. `ConstructMeta.IsOutlineNode` and `OutlineSymbolTag` are added as catalog fields. Document outline reads `c.Meta.IsOutlineNode` / `c.Meta.OutlineSymbolTag` directly — no `ConstructKind` switches.

**Design correction:** Do not recreate `PreceptDocumentInfo` or `LocationOrDocumentSymbol`. The clean LS returns standard protocol types from handlers over shared document state.

**Protocol-layer tests:**
- New LS tests asserting `Location` go-to-definition results and `DocumentSymbol[]` outline payloads

**Regression anchors:** All prior slice tests.

**Dependency ordering:** Depends on Slices 0, 0b, 0a, and 3. Independent of Slices 1, 4, and 5.

---

### Slice 7: Diagnostic Enrichment ("Did You Mean?")

**What:** Fuzzy-match enrichment for `UndeclaredField`/`UndeclaredState`/`UndeclaredEvent`/`UndeclaredFunction` diagnostics.

**Creates:**
- `tools/Precept.LanguageServer/LevenshteinDistance.cs` — pure Levenshtein implementation (~20 lines)
- `tools/Precept.LanguageServer/DiagnosticEnricher.cs` — enrichment logic

**Method-level specificity:**

```csharp
static class DiagnosticEnricher
{
    // Returns enriched diagnostics + code-action metadata keyed by diagnostic identity
    public static (IReadOnlyList<Diagnostic> Diagnostics, IReadOnlyDictionary<DiagnosticKey, SuggestionInfo> Suggestions)
        Enrich(Compilation compilation)
    {
        // 1. Start from compilation.Diagnostics projected to LSP diagnostics
        // 2. Resolve suggestion pools from compilation.Semantics.Fields / States / Events and Functions.All
        // 3. Compute Levenshtein distance from the failing name to each candidate
        // 4. Filter ≤ 3, pick lowest, tiebreak alphabetically
        // 5. Append " — did you mean 'X'?" to the published message
        // 6. Record suggestion metadata keyed by (code + range + message-shape) for Slice 8
    }
}
```

**Design correction:** Because `Compilation.Semantics` is always present, graceful degradation here means "empty or incomplete symbol facts yield no suggestion," not "semantic index is null."

**Tests:** New tests in `test/Precept.LanguageServer.Tests/`:
- `[Theory] Enrichment_UndeclaredField_SuggestsClosestMatch` — multiple field-name typos
- `[Theory] Enrichment_NoMatchWithin3Edits_NoSuggestion` — threshold enforcement
- `[Fact] Enrichment_TiebreakAlphabetical` — alphabetical tiebreak
- `[Fact] Enrichment_EmptySemanticFacts_NoSuggestion` — graceful degradation with no usable symbols
- `[Fact] Enrichment_EmptyPool_NoSuggestion` — empty field/state/event sets
- `[Fact] Enrichment_IdenticalName_NoSuggestion` — distance-0 guard

**Regression anchors:** All prior slice tests. All `test/Precept.Tests/` pipeline tests.

**Dependency ordering:** Depends on Slices 0, 0b, and 1. Unlocks Slice 8.

---

### Slice 8: Code Actions

**What:** Quick-fix code actions for "did you mean?" renames, unterminated literal fixes, and FixHint tooltips.

**Creates:**
- `tools/Precept.LanguageServer/Handlers/CodeActionHandler.cs` — OmniSharp `ICodeActionHandler` with `CodeActionRegistrationOptions` (`quickfix` only, `ResolveProvider = false`)
- Shared code-action projection over enriched diagnostics and `DiagnosticMeta` fix metadata

**Modifies:**
- `tools/Precept.VsCode/package.json` — register `precept.showFixHint` command for tooltip-only code actions
- `tools/Precept.VsCode/src/extension.ts` — implement `precept.showFixHint` so LS command invocations actually surface the informational payload

**DiagnosticMeta enrichment (George-15 fields — landed):** Code actions read `DiagnosticMeta.ExampleBefore` and `DiagnosticMeta.ExampleAfter` to populate the tooltip-command payload with before/after DSL snippets. These fields are already present on `DiagnosticMeta` — no prerequisite wait.

**Method-level specificity:**

```csharp
// CodeActionHandler / shared code-action projector:
public IReadOnlyList<CodeAction> GetCodeActions(DocumentUri uri, Range range,
    IReadOnlyList<Diagnostic> diagnostics)
{
    var actions = new List<CodeAction>();
    foreach (var diag in diagnostics)
    {
        // 1. Match the incoming diagnostic to DiagnosticKey / SuggestionInfo from Slice 7
        // 2. Check DiagnosticMeta.FixHint → mechanical fix or tooltip-only action
        // 3. Tooltip command payload includes fixHint + optional exampleBefore/exampleAfter
        // 4. Special cases: UnterminatedStringLiteral → insert closing "
        //                   UnterminatedTypedConstant → insert closing '
    }
    return actions;
}
```

**Protocol-layer tests:**
- New LS tests asserting `CodeAction[]` for rename suggestions, unterminated literal fixes, and FixHint surfaces

**Regression anchors:** Slice 7 tests.

**Dependency ordering:** Depends on Slice 0b and Slice 7 (enrichment). Independent of Slices 2–6.

---

### Slice 9: Folding Ranges

**What:** Multi-line construct folding.

**Creates:**
- `tools/Precept.LanguageServer/Handlers/FoldingRangeHandler.cs` — OmniSharp `IFoldingRangeHandler`

**Method-level specificity:**

```csharp
FoldingRange[] GetFoldingRanges(Compilation compilation) =>
    compilation.ConstructManifest.Constructs
        .Where(c => c.Span.EndLine > c.Span.StartLine)
        .Select(c => new FoldingRange
        {
            StartLine = c.Span.StartLine - 1,
            EndLine = c.Span.EndLine - 1,
            Kind = FoldingRangeKind.Region,
        })
        .ToArray();
```

**Design correction:** `ConstructManifest` has no comment constructs, so folding stays construct-span-based. Do not add a nonexistent `ConstructKind.Comment` branch.

**Tests:** Minimal — one integration test verifying multi-line constructs produce folding ranges.

**Regression anchors:** None — new handler, no existing tests.

**Dependency ordering:** Depends on Slices 0 and 0b. Fully independent of all other slices.

---

### Slice 11: Program.cs Wiring

**Goal:** Wire `Program.cs` to the completed handler surface.

**Modifies:**
- `tools/Precept.LanguageServer/Program.cs`:
  - Register all handlers built in Slices 0–9 with `options.WithHandler<T>()`
  - Register shared services (document store, projector/enricher helpers, etc.) needed by those handlers

Capabilities are advertised by each handler's registration options (`TextDocumentSyncHandlerBase`, semantic-token registration options, completion registration options, and so on), not by a separate hand-authored capability object in `Program.cs`.

**Tests:**
- No new tests in Slice 11. Protocol-layer LS tests are authored in Slices 1–9.
- Run `dotnet test` to verify the final handler wiring and handler-advertised capabilities.

**Dependency ordering:** Depends on Slice 0b and all handler slices (1–9). This is the final wiring slice.

---

## 4. File Inventory

| File | Slices | Action |
|---|---|---|
| `tools/Precept.LanguageServer/Program.cs` | 0, 11 | Modify — add shared document-state services in Slice 0; wire remaining handlers/services in Slice 11 |
| `tools/Precept.LanguageServer/LanguageServerStubs.cs` | 0b | Delete — legacy shim surface removed before handler implementation begins |
| `tools/Precept.LanguageServer/PreceptPreviewProtocol.cs` | 0b | Delete — legacy preview DTO graph; no replacement (inspect/restore belong in MCP, not LS) |
| `tools/Precept.LanguageServer/DocumentState.cs` | 0 | Create |
| `tools/Precept.LanguageServer/DocumentStore.cs` | 0 | Create |
| `tools/Precept.LanguageServer/DiagnosticProjector.cs` | 0–1 | Create |
| `tools/Precept.LanguageServer/SlotContext.cs` | 4 | Create |
| `tools/Precept.LanguageServer/LevenshteinDistance.cs` | 7 | Create |
| `tools/Precept.LanguageServer/DiagnosticEnricher.cs` | 7 | Create |
| `tools/Precept.LanguageServer/Handlers/TextDocumentSyncHandler.cs` | 0–1 | Create |
| `tools/Precept.LanguageServer/Handlers/SemanticTokensHandler.cs` | 2–3 | Create |
| `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs` | 4 | Create |
| `tools/Precept.LanguageServer/Handlers/HoverHandler.cs` | 5 | Create |
| `tools/Precept.LanguageServer/Handlers/DefinitionHandler.cs` | 6 | Create |
| `tools/Precept.LanguageServer/Handlers/DocumentSymbolHandler.cs` | 6 | Create |
| `tools/Precept.LanguageServer/Handlers/CodeActionHandler.cs` | 8 | Create |
| `tools/Precept.LanguageServer/Handlers/FoldingRangeHandler.cs` | 9 | Create |
| `test/Precept.LanguageServer.Tests/LspTestHost.cs` | 0 | Create — reusable in-process protocol test harness |
| `test/Precept.LanguageServer.Tests/Precept.LanguageServer.Tests.csproj` | 0 | Modify — add test-only client/harness package for protocol tests |
| `test/Precept.LanguageServer.Tests/*` | 0b, 1–10 | Delete 13 legacy compiler-redundant files (173 tests) in Slice 0b; add protocol-layer LS tests per slice in Slices 1–10 |
| `src/Precept/Language/Construct.cs` | 0a | Modify — add `IsOutlineNode` and `OutlineSymbolTag` parameters to `ConstructMeta` |
| `src/Precept/Language/Constructs.cs` | 0a | Modify — set `IsOutlineNode: true` and `OutlineSymbolTag` on 5 construct entries |
| `src/Precept/Pipeline/CheckContext.cs` | 3 | Modify — accumulate event-arg reference sites |
| `src/Precept/Pipeline/SemanticIndex.cs` | 3 | Modify — add `ArgReference` + `ArgReferences` |
| `src/Precept/Pipeline/TypeChecker.Expressions.cs` | 3 | Modify — record arg reference sites when resolving `TypedArgRef` |
| `tools/Precept.VsCode/package.json` | 8 | Modify — register `precept.showFixHint` command |
| `tools/Precept.VsCode/src/extension.ts` | 8 | Modify — implement `precept.showFixHint` command handler |

---

## 5. Dependency Graph

```
Slice 0 (Infrastructure)
└── Slice 0b (Early Shim + Legacy Test Deletion)
    ├── Slice 1 (Diagnostics publish path) → Slice 7 (Enrichment) → Slice 8 (Code Actions)
    ├── Slice 2 (Semantic Tokens Pass 1) → Slice 3 (Semantic Tokens Pass 2 + ArgReferences) → Slice 6 (Go-to-Def + Outline)
    ├── Slice 4 (Completions)
    ├── Slice 5 (Hover)
    ├── Slice 9 (Folding)
    └── Slice 11 (Program.cs Wiring — depends on all)

Slice 0a (Catalog Prerequisite)
└── Slice 6 (Go-to-Def + Outline)
```

**Parallelizable groups after Slice 0b:**
- Group A: Slice 1 → Slice 7 → Slice 8 (diagnostics chain)
- Group B: Slice 2 → Slice 3 → Slice 6 (semantic tokens + navigation); Slice 6 also requires Slice 0a
- Group C: Slice 4 (completions)
- Group D: Slice 5 (hover)
- Group E: Slice 9 (folding)
- Slice 0a: Independent — can run in parallel with Slice 0, Slice 0b, or any other group

Groups A–E can proceed in parallel after Slice 0b. Slice 11 is the final wiring slice.

---

## 6. Phase 2 — Production LS Gap Closure

### 6.1 First-Principles Findings

The current LS is no longer bootstrap-only, but it is still not production-complete. The blocking gaps are concentrated in four areas:

1. **Expression authoring is still skeletal.** `CompletionHandler` stops at top-level/type/modifier/target names; action verbs, action-chain field targets, expression/default positions, typed constants, function names, and accessor names are still dark.
2. **Hover and semantic projection stop short of catalog truth.** Functions, operators, action verbs, collection types, typed constants, and dual-use `set` are not projected from the richer catalogs that already exist.
3. **Core navigation surface is incomplete.** There is no references, document-highlight, rename, signature help, workspace symbol, or selection-range support. `DocumentSymbolHandler` still selects whole constructs instead of declaration identifiers.
4. **The document lifecycle is missing production hardening.** `TextDocumentSyncHandler` has no version ordering, so stale compiles can overwrite newer state under overlapping change delivery.

### 6.2 Explicit Non-Gaps (Do Not Add Slices)

| Evaluated item | Decision | Rationale |
|---|---|---|
| Pull diagnostics (`textDocument/diagnostic`) | **No Phase 2 slice** | OmniSharp `0.19.9` does not natively support LSP 3.17 pull diagnostics. Push `publishDiagnostics` remains the correct production path for the current toolchain. |
| Workspace diagnostics for unopened files | **No Phase 2 slice** | Precept authoring is file-local. `DocumentStore` over open `.precept` files is sufficient; there is no multi-file semantic graph to keep in sync. |
| `DidSave`, `WillSave`, `WillSaveWaitUntil` | **No Phase 2 slice** | Full-text sync already compiles the authoritative in-memory document on every open/change; save adds no new semantic signal. |
| Incremental sync | **No Phase 2 slice** | `.precept` files are small, `Compiler.Compile(source)` is whole-document, and full-sync keeps the server thin. |
| Inlay hints | **No Phase 2 slice** | Precept declarations are already explicit (`field X as Type`, `event E(Arg as Type)`). Inferred-type hints would just restate source text. |
| CodeLens | **No Phase 2 slice** | Transition-count or proof-count lenses would add noisy summaries, not new compiler-owned facts. Outline/hover/references cover the needed navigation surface. |
| State-target de-dup filtering | **No Phase 2 slice** | Hiding names because a row already exists would encode routing policy in the LS. Sort useful candidates, but do not suppress compiler-legal symbols. |
| VS Code status bar | **No Phase 2 slice** | Already implemented in `tools/Precept.VsCode/src/extension.ts`. |
| Grammar generation | **No Phase 2 slice** | The TextMate grammar is already catalog-generated. Phase 2 only needs editor-configuration polish, not grammar rewrites. |

### Slice 12 [Kramer]: Trigger Characters + Dual-Use `set` Type Context

**What:** Fix the registration and projection bug where type-position `set` still behaves like the action keyword, and advertise trigger characters that match actual Precept authoring.

**Modifies:**
- `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs`
  - `GetRegistrationOptions(...)` — replace the bogus `":"`-only registration with `" "`, `"'"`, `"."`, `">"`, and `"~"`.
  - **Important:** LSP trigger characters are character-based; the action-chain trigger is `">"` (the second character of `->`), not the literal two-character string `"->"`.
- `tools/Precept.LanguageServer/SlotContext.cs`
  - Add a small contextual reclassifier so `TokenKind.Set` is treated as the type keyword when the token sits inside `ConstructSlotKind.TypeExpression`.
- `tools/Precept.LanguageServer/Handlers/HoverHandler.cs`
  - Use the same contextual `set` reclassification before deciding between action/type hover.
- `tools/Precept.LanguageServer/Handlers/SemanticTokensHandler.cs`
  - `ProjectLexicalTokens(Compilation compilation)` — emit `type` for `set` in type position instead of `keyword`.

**Catalog / artifacts driving it:**
- `ConstructSlotKind.TypeExpression`
- `Types.ByToken` (`TokenKind.SetType` alias)
- `TokenMeta.SemanticTokenType`

**Tests:**
- `test/Precept.LanguageServer.Tests/CompletionHandlerTests.cs`
  - `[Fact] GetRegistrationOptions_AdvertisesSpaceQuoteDotArrowAndTildeTriggers`
- `test/Precept.LanguageServer.Tests/HoverHandlerTests.cs`
  - `[Fact] Hover_OnSetInTypePosition_UsesTypeHover`
- `test/Precept.LanguageServer.Tests/SemanticTokensHandlerTests.cs`
  - `[Fact] LexicalTokens_SetInTypePosition_EmitsTypeToken`

**Dependency ordering:** Must land before Slices 13, 15, 18, and 19.

---

### Slice 13 [Kramer]: SlotContext Coverage — Action Chains, Expressions, Defaults

**What:** Finish `SlotContextResolver` so cursor routing covers the contexts the enum already promises and the action/default positions Phase 1 never wired.

**Modifies:**
- `tools/Precept.LanguageServer/SlotContext.cs`
  - `GetCursorContext(Compilation compilation, Position position)` — route:
    - `ConstructSlotKind.ActionChain` + cursor after `->` → `InActionVerb`
    - cursor after an action verb or `into` inside an action chain → `InFieldTarget`
    - cursor after `=`, `by`, or `at` inside an action chain → `InExpression`
    - `ConstructSlotKind.GuardClause`, `ConstructSlotKind.ComputeExpression`, `ConstructSlotKind.EnsureClause`, and `ConstructSlotKind.RuleExpression` → `InExpression`
    - cursor after `default` in an event `ArgumentListSlot` → `InArgDefault`
    - cursor after `of` inside `ConstructSlotKind.TypeExpression` → `InTypePosition`
    - cursor after `default` in a field modifier value → `InExpression`
  - Add helper methods that consult `Actions.ByTokenKind` + `ActionMeta.SyntaxShape` instead of LS-local verb lists.

**Catalog / artifacts driving it:**
- `ConstructSlotKind.ActionChain`, `GuardClause`, `ComputeExpression`, `EnsureClause`, `RuleExpression`, `TypeExpression`, `ArgumentList`
- `Actions.ByTokenKind`
- `ActionMeta.SyntaxShape`

**Tests:**
- `test/Precept.LanguageServer.Tests/SlotContextResolverTests.cs`
  - `[Fact] GetCursorContext_ActionChainAfterArrow_ReturnsInActionVerb`
  - `[Fact] GetCursorContext_ActionChainAfterVerb_ReturnsInFieldTarget`
  - `[Fact] GetCursorContext_Guard_ReturnsInExpression`
  - `[Fact] GetCursorContext_EventArgDefault_ReturnsInArgDefault`
  - `[Fact] GetCursorContext_CollectionInnerTypeAfterOf_ReturnsInTypePosition`

**Dependency ordering:** Depends on Slice 12. Unblocks Slices 14, 15, and 18.

---

### Slice 14 [Kramer]: Expression / Action / Default Completion Surface

**What:** Implement the production completion contexts Phase 1 left empty: action verbs, action-chain field targets, expression positions, member access, and argument defaults.

**Modifies:**
- `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs`
  - `GetCompletions(...)` — handle `InActionVerb`, `InExpression`, and `InArgDefault`.
  - Add `GetActionItems(Compilation compilation, Position position)` — project from `Actions.All`, filtering by the current field type when the target field is already known.
  - Add `GetExpressionItems(Compilation compilation, Position position)` — project:
    - fields from `compilation.Semantics.Fields`
    - event args from the current event scope in `compilation.Semantics.Events`
    - built-in functions from `Functions.All`
    - member accessors from `Types.GetMeta(receiverType).Accessors` after `.`
    - boolean literals from `Tokens.GetMeta(TokenKind.True/False)`
  - `InArgDefault` reuses expression completions.
- `tools/Precept.LanguageServer/SlotContext.cs`
  - Add any helper needed to recover current event name / receiver symbol without LS-local keyword lists.

**Catalog / artifacts driving it:**
- `Actions.All`
- `Functions.All`
- `TypeMeta.Accessors`
- `TokenMeta` for `true` / `false`
- `SemanticIndex.Fields`, `SemanticIndex.Events`

**Tests:**
- `test/Precept.LanguageServer.Tests/CompletionHandlerTests.cs`
  - `[Fact] Completions_ActionVerb_UsesActionsCatalog`
  - `[Fact] Completions_ActionFieldTarget_UsesDeclaredFields`
  - `[Fact] Completions_Expression_IncludeFieldsArgsAndFunctions`
  - `[Fact] Completions_MemberAccess_UsesTypeAccessors`
  - `[Fact] Completions_ArgDefault_ReusesExpressionCompletions`

**Dependency ordering:** Depends on Slice 13.

---

### Slice 15 [Kramer]: Typed-Constant Completions

**What:** Add typed-literal authoring support so `'` is no longer a dead end.

**Creates:**
- `tools/Precept.LanguageServer/TypedConstantCollector.cs`
  - `CollectByType(SemanticIndex index, TypeKind type)` — walk expression-bearing semantic nodes and collect distinct `TypedTypedConstant.RawText` values already present in the document.

**Modifies:**
- `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs`
  - Add `GetTypedConstantItems(Compilation compilation, Position position)`.
  - When an expected type can be determined, suggest:
    - document-local typed constants from `TypedConstantCollector`
    - `Types.GetMeta(expectedType).ContentValidation?.Examples`
  - Route `'`-triggered requests in type/default/expression contexts to this provider.

**Catalog / artifacts driving it:**
- `TypeMeta.ContentValidation`
- `ContentValidation.Examples`
- `TypedTypedConstant`
- `SemanticIndex` expression-bearing nodes (`TypedField.DefaultExpression`, `TypedField.ComputedExpression`, `TypedRule`, `TypedEnsure`, `TypedTransitionRow`, `TypedStateHook`, `TypedEventHandler`)

**Tests:**
- `test/Precept.LanguageServer.Tests/CompletionHandlerTests.cs`
  - `[Fact] Completions_TypedConstant_UseTypeExamples`
  - `[Fact] Completions_TypedConstant_SuggestPreviouslyUsedDocumentValues`
  - `[Fact] Completions_TypedConstant_NoExpectedType_ReturnsEmpty`

**Dependency ordering:** Depends on Slices 12 and 13.

---

### Slice 16 [George]: Snippet Metadata for Constructs and Actions

**What:** Populate the snippet metadata that the LS should have been consuming from day one.

**Modifies:**
- `src/Precept/Language/Constructs.cs`
  - Set `SnippetTemplate` on the top-level completion constructs:
    - `PreceptHeader`
    - `FieldDeclaration`
    - `StateDeclaration`
    - `EventDeclaration`
    - `RuleDeclaration`
- `src/Precept/Language/Actions.cs`
  - Set `SnippetTemplate` on the primary author-facing action verbs in `GetMeta(...)`:
    - `set`, `add`, `remove`, `enqueue`, `dequeue`, `push`, `pop`, `clear`, `append`, `insert`, `put`

**Catalog driving it:**
- `ConstructMeta.SnippetTemplate`
- `ActionMeta.SnippetTemplate`

**Tests:**
- `test/Precept.Tests/Language/ConstructCatalogTests.cs`
  - `[Fact] SnippetTemplate_PresentForTopLevelCompletionConstructs`
- `test/Precept.Tests/Language/ActionCatalogTests.cs`
  - `[Fact] SnippetTemplate_PresentForPrimaryActionVerbs`

**Dependency ordering:** Independent of Slices 12–15. Unblocks Slice 17.

---

### Slice 17 [Kramer]: Completion Item Quality — Snippets, Docs, Sort Order

**What:** Make the completion items worth accepting.

**Modifies:**
- `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs`
  - Replace the current `CreateItem(string label, string detail, CompletionItemKind kind)` helper with a richer factory that sets:
    - `InsertText`
    - `InsertTextFormat`
    - `Documentation`
    - `SortText`
    - `Detail`
  - Consume:
    - `ConstructMeta.SnippetTemplate`
    - `ActionMeta.SnippetTemplate`
    - existing `FunctionMeta.SnippetTemplate`
    - `HoverDescription ?? Description`
    - `UsageExample`
  - Sort semantic symbols before catalog keywords/functions/actions, but do not hide compiler-legal names.

**Catalog / artifacts driving it:**
- `ConstructMeta.SnippetTemplate`
- `ActionMeta.SnippetTemplate`
- `FunctionMeta.SnippetTemplate`
- `HoverDescription`
- `UsageExample`

**Tests:**
- `test/Precept.LanguageServer.Tests/CompletionHandlerTests.cs`
  - `[Fact] Completions_TopLevelConstruct_UsesSnippetInsertText`
  - `[Fact] Completions_Action_UsesSnippetInsertText`
  - `[Fact] Completions_Function_UsesSnippetInsertText`
  - `[Fact] Completions_Documentation_UsesHoverDescriptionAndUsageExample`
  - `[Fact] Completions_SortsSemanticSymbolsBeforeCatalogItems`

**Dependency ordering:** Depends on Slices 14 and 16. Slice 15 enriches the typed-constant branch but is not a hard blocker.

---

### Slice 18 [Kramer]: Hover Surface Completion

**What:** Finish hover so it projects the actual catalogs and semantic expression shapes, not the shallow token description fallback.

**Creates:**
- `tools/Precept.LanguageServer/SemanticExpressionLocator.cs`
  - `TryFindExpressionAt(SemanticIndex index, Position position, out TypedExpression expression)`
  - `TryFindFunctionAt(Compilation compilation, Position position, out IReadOnlyList<FunctionMeta> overloads)`
  - `TryFindAccessorAt(Compilation compilation, Position position, out TypeMeta ownerType, out TypeAccessor accessor)`

**Modifies:**
- `tools/Precept.LanguageServer/Handlers/HoverHandler.cs`
  - `CreateHover(...)` — route to:
    - type hover from `Types.ByToken` / `TypeMeta.HoverDescription`
    - action hover from `Actions.ByTokenKind` / `ActionMeta.HoverDescription`
    - operator hover from `Operators.ByToken` / `Operators.ByTokenSequence` / `OperatorMeta.HoverDescription`
    - function hover from `Functions.All` / overload list
    - typed-constant hover from `TypedTypedConstant` + `Types.GetMeta(resultType)` + `ContentValidation.FormatDescription`
    - accessor hover from `TypeMeta.Accessors`
  - Keep token-description fallback only for tokens with no richer catalog owner.

**Catalog / artifacts driving it:**
- `Types.All`
- `Actions.All`
- `Operators.All`
- `Functions.All`
- `TypeMeta.Accessors`
- `TypeMeta.ContentValidation`
- `TypedTypedConstant`

**Tests:**
- `test/Precept.LanguageServer.Tests/HoverHandlerTests.cs`
  - `[Fact] Hover_OnTypedConstant_ShowsDeclaredTypeAndFormat`
  - `[Fact] Hover_OnFunctionCall_ShowsSignatureAndDescription`
  - `[Fact] Hover_OnOperator_UsesOperatorHoverDescription`
  - `[Fact] Hover_OnCollectionType_UsesTypeHoverDescription`
  - `[Fact] Hover_OnActionVerb_UsesActionHoverDescription`
  - `[Fact] Hover_OnAccessor_UsesAccessorDescription`

**Dependency ordering:** Depends on Slices 12 and 13.

---

### Slice 19 [Kramer]: Semantic Token Expression Overlays

**What:** Close the remaining semantic-highlighting gap by classifying built-in function call names and pinning the literal/operator/action behavior with explicit tests.

**Modifies:**
- `tools/Precept.LanguageServer/Handlers/SemanticTokensHandler.cs`
  - Rename or extend `ProjectIdentifierTokens(...)` so the overlay pass can also emit function-name tokens for `TypedFunctionCall` spans.
  - Use `Functions.GetMeta(call.ResolvedFunction).Name.Length` with `TypedFunctionCall.Span.Start*` to project the call leader as token type `function`.
  - Preserve the Slice 12 type-context reclassification for `set`.

**Catalog / artifacts driving it:**
- `TypedFunctionCall.ResolvedFunction`
- `Functions.GetMeta(...)`
- `TokenMeta.SemanticTokenType`

**Tests:**
- `test/Precept.LanguageServer.Tests/SemanticTokensHandlerTests.cs`
  - `[Fact] ExpressionTokens_BuiltInFunctionCall_EmitFunctionToken`
  - `[Fact] LexicalTokens_TypedConstant_EmitStringToken`
  - `[Fact] LexicalTokens_Operator_EmitOperatorToken`
  - `[Fact] LexicalTokens_ActionVerb_EmitKeywordToken`
  - `[Fact] LexicalTokens_BooleanLiteral_KeepKeywordTokenType`

**Dependency ordering:** Depends on Slice 12. Independent of Slices 20–29.

---

### Slice 20 [Kramer]: Shared Symbol Navigation + References / Highlights

**What:** Build the reusable symbol-location helper the LS now needs across definition, references, highlight, and rename.

**Creates:**
- `tools/Precept.LanguageServer/SymbolNavigation.cs`
  - `TryFindOccurrence(Compilation compilation, Position position, out SymbolOccurrence occurrence)`
  - `GetReferenceSpans(SemanticIndex index, SymbolOccurrence occurrence, bool includeDeclaration)`
- `tools/Precept.LanguageServer/Handlers/ReferencesHandler.cs`
- `tools/Precept.LanguageServer/Handlers/DocumentHighlightHandler.cs`

**Modifies:**
- `tools/Precept.LanguageServer/Handlers/DefinitionHandler.cs`
  - Replace the duplicated field/state/event/arg loops with `SymbolNavigation`.

**Artifacts driving it:**
- `SemanticIndex.Fields`, `States`, `Events`
- `FieldReferences`, `StateReferences`, `EventReferences`, `ArgReferences`
- exact declaration spans (`NameSpan`, `TypedArg.Span`)

**Tests:**
- `test/Precept.LanguageServer.Tests/ReferencesHandlerTests.cs`
  - `[Fact] References_Field_ReturnDeclarationAndAllSites`
  - `[Fact] References_State_ReturnDeclarationAndAllSites`
  - `[Fact] References_Event_ReturnDeclarationAndAllSites`
  - `[Fact] References_Argument_ReturnDeclarationAndAllSites`
- `test/Precept.LanguageServer.Tests/DocumentHighlightHandlerTests.cs`
  - `[Fact] DocumentHighlight_Field_ReturnsDeclarationAndReferences`
  - `[Fact] DocumentHighlight_EventArgument_ReturnsDeclarationAndReferences`

**Dependency ordering:** Independent of Slices 12–19. Unblocks Slice 21.

---

### Slice 21 [Kramer]: Rename

**What:** Add production rename support for user-declared identifiers.

**Creates:**
- `tools/Precept.LanguageServer/Handlers/RenameHandler.cs`
  - Implement both prepare-rename and rename handling on top of `SymbolNavigation`.

**Method-level specificity:**
- `Handle(PrepareRenameParams request, CancellationToken ct)` — allow only field/state/event/arg identifiers; reject keywords, functions, accessors, and typed constants.
- `Handle(RenameParams request, CancellationToken ct)` — return a single-document `WorkspaceEdit` containing the declaration span plus every reference span from `SymbolNavigation.GetReferenceSpans(...)`.

**Tests:**
- `test/Precept.LanguageServer.Tests/RenameHandlerTests.cs`
  - `[Fact] PrepareRename_FieldReference_ReturnsIdentifierRange`
  - `[Fact] Rename_Field_UpdatesDeclarationAndAllReferences`
  - `[Fact] Rename_State_UpdatesDeclarationAndTransitionSites`
  - `[Fact] Rename_Event_UpdatesDeclarationAndEventSites`
  - `[Fact] Rename_Argument_UpdatesDeclarationAndQualifiedArgumentSites`
  - `[Fact] PrepareRename_OnKeyword_ReturnsNull`

**Dependency ordering:** Depends on Slice 20.

---

### Slice 22 [Kramer]: Signature Help

**What:** Add call-site guidance for built-in functions and parameterized type accessors.

**Creates:**
- `tools/Precept.LanguageServer/CallContextResolver.cs`
  - `TryFindActiveCall(Compilation compilation, Position position, out ActiveCallContext call)` — token-based scan for the nearest unmatched `(` and active-parameter index.
- `tools/Precept.LanguageServer/Handlers/SignatureHelpHandler.cs`

**Modifies:**
- `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs`
  - Ensure completion + signature help share function/accessor naming conventions.

**Catalog / artifacts driving it:**
- `Functions.All` / `FunctionMeta.Overloads`
- `TypeMeta.Accessors` for method-like accessors (`at(...)`, etc.)
- `ParameterMeta`

**Tests:**
- `test/Precept.LanguageServer.Tests/SignatureHelpHandlerTests.cs`
  - `[Fact] SignatureHelp_Round_ShowsBothOverloads`
  - `[Fact] SignatureHelp_StartsWith_ShowsNamedParameters`
  - `[Fact] SignatureHelp_AccessorCall_ShowsAccessorSignature`
  - `[Fact] SignatureHelp_NoActiveCall_ReturnsNull`

**Dependency ordering:** Independent of Slice 20. Can proceed in parallel with Slices 21, 23–27.

---

### Slice 23 [Kramer]: Document Symbol Selection Ranges

**What:** Fix `DocumentSymbolHandler` so symbol selection lands on declaration identifiers, not full construct spans.

**Creates:**
- `tools/Precept.LanguageServer/OutlineSymbolProjector.cs`
  - `BuildDocumentSymbols(Compilation compilation)`
  - `GetSelectionSpan(Compilation compilation, ParsedConstruct construct)`

**Modifies:**
- `tools/Precept.LanguageServer/Handlers/DocumentSymbolHandler.cs`
  - Delegate to `OutlineSymbolProjector`.

**Selection-span rules:**
- Precept header → identifier span from `IdentifierListSlot`
- Field / state / event → semantic `NameSpan`
- Rule declaration → construct span (unnamed)

**Tests:**
- `test/Precept.LanguageServer.Tests/DocumentSymbolHandlerTests.cs`
  - `[Fact] BuildDocumentSymbols_FieldSelectionRange_UsesNameSpan`
  - `[Fact] BuildDocumentSymbols_StateSelectionRange_UsesNameSpan`
  - `[Fact] BuildDocumentSymbols_EventSelectionRange_UsesNameSpan`
  - `[Fact] BuildDocumentSymbols_PreceptSelectionRange_UsesHeaderIdentifierSpan`

**Dependency ordering:** Independent. Unblocks Slice 24.

---

### Slice 24 [Kramer]: Workspace Symbols

**What:** Add `Ctrl+T`/`workspace/symbol` support across all open `.precept` documents.

**Modifies:**
- `tools/Precept.LanguageServer/DocumentStore.cs`
  - Add `Snapshot()` / `EnumerateOpenDocuments()` so workspace-symbol queries can read all open document states safely.
- `tools/Precept.LanguageServer/Handlers/WorkspaceSymbolHandler.cs`
  - Project open-document outline symbols via `OutlineSymbolProjector` and filter by query.

**Artifacts driving it:**
- `DocumentStore`
- `ConstructManifest.Constructs`
- `ConstructMeta.IsOutlineNode` / `OutlineSymbolTag`

**Tests:**
- `test/Precept.LanguageServer.Tests/WorkspaceSymbolHandlerTests.cs`
  - `[Fact] WorkspaceSymbol_Query_ReturnsMatchingSymbolsAcrossOpenDocuments`
  - `[Fact] WorkspaceSymbol_Result_CarriesDocumentUriAndSymbolKind`

**Dependency ordering:** Depends on Slice 23.

---

### Slice 25 [Kramer]: Selection Ranges

**What:** Add editor expand-selection support derived from Precept syntax structure.

**Creates:**
- `tools/Precept.LanguageServer/SyntaxSelectionBuilder.cs`
  - Build parent chains token span → parsed expression span → slot span → construct span.
- `tools/Precept.LanguageServer/Handlers/SelectionRangeHandler.cs`

**Artifacts driving it:**
- `Compilation.Tokens`
- `SlotValue.Span`
- nested `ParsedExpression` spans held by `ComputeExpressionSlot`, `GuardClauseSlot`, `EnsureClauseSlot`, `RuleExpressionSlot`, `ActionChainSlot`
- `ConstructManifest.Constructs`

**Tests:**
- `test/Precept.LanguageServer.Tests/SelectionRangeHandlerTests.cs`
  - `[Fact] SelectionRange_Identifier_ExpandsToExpressionThenConstruct`
  - `[Fact] SelectionRange_MultiplePositions_ReturnAlignedChains`

**Dependency ordering:** Independent of Slices 20–24.

---

### Slice 26 [Kramer]: Document Version Ordering

**What:** Prevent stale compile results from overwriting newer document state.

**Modifies:**
- `tools/Precept.LanguageServer/DocumentState.cs`
  - Add `Version` tracking and `TryUpdate(int version, Compilation compilation, IReadOnlyDictionary<DiagnosticKey, SuggestionInfo> suggestions)`.
- `tools/Precept.LanguageServer/Handlers/TextDocumentSyncHandler.cs`
  - Pass `request.TextDocument.Version` through open/change handling.
  - Publish diagnostics only for accepted versions.
  - Keep full-sync; do **not** add debounce timers.

**Tests:**
- `test/Precept.LanguageServer.Tests/DocumentStateVersioningTests.cs`
  - `[Fact] TryUpdate_OlderVersion_DoesNotReplaceCurrentCompilation`
  - `[Fact] TryUpdate_NewerVersion_ReplacesCurrentCompilationAndSuggestions`
- `test/Precept.LanguageServer.Tests/DiagnosticPublishIntegrationTests.cs`
  - `[Fact] DidChange_OutOfOrderVersions_PublishesNewestDiagnosticsOnly`

**Dependency ordering:** Independent. Must land before Slice 29 so final wiring/protocol smoke tests reflect ordered updates.

---

### Slice 27 [Kramer]: VS Code Typed-Constant Editor Polish

**What:** Finish the one missing editor-configuration piece for production typed-literal authoring.

**Modifies:**
- `tools/Precept.VsCode/language-configuration.json`
  - Add single-quote auto-closing / surrounding pairs for typed constants.
  - Keep `lineComment` as `#` — do **not** change this to `//`.

**Tests:**
- `test/Precept.LanguageServer.Tests/ExtensionManifestTests.cs`
  - `[Fact] LanguageConfiguration_TypedConstantSingleQuote_AutoCloses`

**Dependency ordering:** Independent.

---

### Slice 28 [Kramer]: Documentation Sync

**What:** When Phase 2 lands, the docs must describe the real LS surface — and must not reintroduce preview as if it shipped.

**Modifies:**
- `docs/language/precept-language-spec.md`
  - Add a concise LS/tooling subsection documenting completions, hover, semantic tokens, diagnostics, definition, references, rename, signature help, workspace symbols, outline, folding, selection ranges, and code actions.
- `docs/tooling/language-server.md`
  - Update the status block and feature sections so the implemented surface matches reality; keep preview explicitly out of this implementation track.
- `README.md`
  - Replace the current vague LS claims with the concrete Phase 2 capability list; do not describe the preview placeholder as shipped functionality.

**Tests:** None — documentation sync only.

**Dependency ordering:** Must land after the behavior slices it documents (12–27) and before final closeout.

---

### Slice 29 [Kramer]: Slice 11 Wiring Amendment — Final Handler Surface

**What:** Expand the final wiring slice so `Program.cs` and the protocol test host register the entire completed surface, not just the original Phase 1 handlers.

**Modifies:**
- `tools/Precept.LanguageServer/Program.cs`
  - Register all existing Phase 1 handlers **plus**:
    - `ReferencesHandler`
    - `DocumentHighlightHandler`
    - `RenameHandler`
    - `SignatureHelpHandler`
    - `WorkspaceSymbolHandler`
    - `SelectionRangeHandler`
  - Register any shared helpers created above (`OutlineSymbolProjector`, `SymbolNavigation`, `CallContextResolver`, `SyntaxSelectionBuilder`, `SemanticExpressionLocator`, etc.) if they are not static.
- `test/Precept.LanguageServer.Tests/LspTestHost.cs`
  - Mirror the same handler registrations so protocol-layer tests exercise the real advertised capability set.
- `test/Precept.LanguageServer.Tests/ServerCapabilityTests.cs`
  - Add an initialize-result smoke test asserting the server advertises completion, hover, definition, references, document highlights, rename, signature help, workspace symbols, selection ranges, semantic tokens, folding, diagnostics sync, and code actions.

**Dependency ordering:** Depends on every behavioral Phase 1 and Phase 2 slice. This is the new terminal slice.

### 6.3 Phase 2 Dependency Ordering

```
Slice 12 → Slice 13 → Slices 14, 15, 18
Slice 16 → Slice 17
Slice 12 → Slice 19
Slice 20 → Slice 21
Slice 23 → Slice 24
Slice 26 is independent but must finish before Slice 29
Slice 27 is independent
Slice 28 follows Slices 12–27
Slice 29 depends on all Phase 1 handler slices plus Slices 12–28
```

---

## 7. Tooling / MCP Sync Assessment

| Category | Impact | Detail |
|---|---|---|
| **TextMate grammar** | No changes needed | Grammar is generated from catalogs; LS does not affect it |
| **Completions** | Catalog-driven — no LS-side hardcoding | Existing catalog metadata already drives labels/docs; future `SnippetTemplate` additions flow through automatically |
| **Semantic tokens** | No grammar changes | Pass 1 reads `TokenMeta.SemanticTokenType`; Pass 2 reads `SemanticIndex` reference collections, including the new arg-reference prerequisite from Slice 3 |
| **MCP tools** | No changes needed | MCP and LS both consume the same catalogs; they are independent consumers |
| **VS Code extension** | Minor | Extension already expects an LS process on stdio. Slice 8 must update both `package.json` and `src/extension.ts` so `precept.showFixHint` is registered **and** implemented. Preview/inspect is out of scope for the LS — the VS Code preview surface should call the MCP tools directly. |

---

## 8. Catalog Prerequisites (Resolved)

### ✅ Resolved: `ConstructMeta.IsOutlineNode` + `OutlineSymbolTag`

**Status:** Concrete implementation task — Slice 0a in §3.

The design doc (CC#18) resolved that outline eligibility and LSP symbol kind should be `ConstructMeta` fields. Slice 0a adds `bool IsOutlineNode = false` and `string? OutlineSymbolTag = null` to `ConstructMeta` in `src/Precept/Language/Construct.cs`, sets them on entries in `src/Precept/Language/Constructs.cs`, and includes catalog-level tests. This unblocks Slice 6.

**Architectural resolution:** The catalog stores a plain `string?` tag (e.g., `"Module"`, `"Property"`). The LS projects it to `SymbolKind` via `Enum.Parse<SymbolKind>(tag)`. No LSP protocol dependency in `src/Precept/`. This follows the `TokenMeta.SemanticTokenType` pattern.

See Slice 0a for the complete field map, entry-by-entry values, and test specifications.

### ✅ Landed: `DiagnosticMeta.TriggerCondition` / `RecoverySteps` / `ExampleBefore` / `ExampleAfter`

**Status:** ✅ LANDED (as of 2026-05-09). George-15's catalog authoring pass is complete. Fields are present on `DiagnosticMeta` in `src/Precept/Language/Diagnostics.cs` and populated on diagnostic entries (e.g., `InputTooLarge`, `UnterminatedStringLiteral`, `UnterminatedTypedConstant`).

**Fields available for immediate use:**
- `DiagnosticMeta.TriggerCondition` — human-readable description of when the diagnostic fires
- `DiagnosticMeta.RecoverySteps` — ordered list of recovery actions
- `DiagnosticMeta.ExampleBefore` — DSL snippet showing the error state
- `DiagnosticMeta.ExampleAfter` — DSL snippet showing the corrected state

**Consumer slices:**
- **Slice 8 (Code Actions):** `GetCodeActions` reads `ExampleBefore` / `ExampleAfter` for enriched fix-hint panels. No prerequisite wait — implement immediately.
- `TriggerCondition` / `RecoverySteps` remain useful extension-facing metadata, but they are not a prerequisite for the clean LSP hover surface and should not force a diagnostic-code hover path into Slice 5.

### Resolved: `TypeMeta.IsUserFacing`

CC#16 resolved that `Error` and `StateRef` types should not appear in completions. The field isn't on `TypeMeta`, but `Token != null` is a structural equivalent for filtering internal types (internal types have `Token = null`). No catalog change required — this is the permanent solution.

---

## 9. Risk Assessment

| Risk | Likelihood | Mitigation |
|---|---|---|
| `Compilation` / `SemanticIndex` shape changes mid-implementation | Medium | LS consumes core artifacts read-only; where the thin layer needs a missing fact (`ArgReferences`, exact field-name spans), add it once in core rather than rediscovering it in handlers |
| Legacy compiler-redundant LS tests obscure final ownership boundaries | High | Delete the 173 shim-facing tests in Slice 0b before any handler code is written; compiler correctness stays in `test/Precept.Tests`, LS tests stay protocol-layer only |
| OmniSharp.Extensions.LanguageServer API surprises | Low | Version 0.19.9 is stable; handler patterns are well-documented |
| `ConstructMeta.IsOutlineNode` implementation | Low | Slice 0a is a concrete task with method-level specificity; blocks only Slice 6 |

---

## 10. Implementation Order (Recommended)

1. **Slice 0a** — Catalog prerequisite: `IsOutlineNode` + `OutlineSymbolTag` (independent, unblocks Slice 6)
2. **Slice 0** — Infrastructure (`DocumentState`, `DocumentStore`, compile loop, full-text sync)
3. **Slice 0b** — Delete `LanguageServerStubs.cs`, `PreceptPreviewProtocol.cs`, and the 13 legacy LS test files before any clean handler code is written
4. **Slice 1** — Diagnostics publish path (highest user-visible value, establishes the real compile→publish flow)
5. **Slice 2** — Semantic Tokens Pass 1 (visual value, no semantic prerequisite)
6. **Slice 3** — Semantic Tokens Pass 2 + `ArgReferences` prerequisite (keeps LS projection-only)
7. **Slice 5** — Hover (high user value; reads current source-shape metadata rather than shim helpers)
8. **Slice 4** — Completions (catalog-driven, over shared document state)
9. **Slice 6** — Go-to-Definition + Outline (reads Slice 0a + Slice 3 prerequisites)
10. **Slice 7** — Diagnostic Enrichment
11. **Slice 8** — Code Actions (includes `DiagnosticMeta.ExampleBefore`/`ExampleAfter` enrichment; wires `precept.showFixHint` in both `package.json` and `src/extension.ts`)
12. **Slice 9** — Folding Ranges
13. **Slice 11** — Final `Program.cs` wiring

---

## 11. Estimated Scope

| Metric | Estimate |
|---|---|
| New files | ~13–15 |
| Modified / deleted files | 5 modified core files + 2 deleted legacy LS files (`LanguageServerStubs.cs`, `PreceptPreviewProtocol.cs`) |
| Estimated LOC (production) | 550–750 |
| Estimated LOC (tests — new) | Protocol-layer LS tests added per slice in Slices 1–10 |
| Legacy tests removed | 173 compiler-redundant LS tests across 13 files, deleted in Slice 0b |
| Calendar estimate | 4–6 slice sessions |
