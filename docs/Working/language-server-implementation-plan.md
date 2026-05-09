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

## 2. Gap Analysis

### 2.1 Design Completeness Assessment

The design at `docs/tooling/language-server.md` is **comprehensive and implementation-ready** for all 10 feature areas. Key findings:

| Capability | Design Status | Implementation Status | Gaps |
|---|---|---|---|
| **7.1 Diagnostics Push** | ✅ Complete | Stub only | None — design is implementation-ready |
| **7.2 Semantic Tokens (Two-Pass)** | ✅ Complete | Stub only | None — `TokenMeta.SemanticTokenType` exists, `SemanticIndex` reference sites exist |
| **7.3 Catalog-Driven Completions** | ✅ Complete | Stub only | `IsUserFacing` not on `TypeMeta` — resolved: `Token != null` is the permanent filter (no catalog change needed) |
| **7.4 Hover** | ✅ Complete | Stub only | None — `HoverDescription` exists on Types, Functions, Operators, Modifiers |
| **7.5 Go-to-Definition** | ✅ Complete | Stub only | None — `Syntax` back-pointers and `NameSpan` exist on `TypedField`, `TypedState`, `TypedEvent` |
| **7.6 Preview/Inspect** | ✅ Complete | Stub only | Blocked on runtime evaluator (Phase 3 — entire layer stubbed) |
| **7.7 Document Outline** | ✅ Complete | Stub only | **Resolved:** `IsOutlineNode` and `OutlineSymbolTag` added as Slice 0a |
| **7.8 Folding Ranges** | ✅ Complete | Stub only | None — uses `ConstructManifest.Constructs` spans only |
| **7.9 Diagnostic Enrichment** | ✅ Complete | Stub only | None — `SuggestionSource` already on `DiagnosticMeta` |
| **7.10 Code Actions** | ✅ Complete | Stub only | None — George-15's `TriggerCondition`/`RecoverySteps`/`ExampleBefore`/`ExampleAfter` have landed on `DiagnosticMeta` |

### 2.2 Catalog Dependencies

| Gap | Catalog | Required Change | Blocking? |
|---|---|---|---|
| `IsOutlineNode` | `ConstructMeta` | Add `bool IsOutlineNode = false` and `string? OutlineSymbolTag = null` parameters | **Yes** — Document Outline slice depends on this. **Resolved:** Slice 0a adds these fields. |
| `IsUserFacing` | `TypeMeta` | Add `bool IsUserFacing = true` parameter; set `false` for `Error` and `StateRef` | **No** — filter by `Token != null` is structurally equivalent; no catalog change required |
| `TriggerCondition` / `RecoverySteps` / `ExampleBefore` / `ExampleAfter` | `DiagnosticMeta` | George-15 added these | **No** — ✅ **LANDED** (as of 2026-05-09). Fields are present on `DiagnosticMeta` in `src/Precept/Language/Diagnostics.cs` and populated on diagnostic entries. Slices 5 (Hover) and 8 (Code Actions) can consume them immediately. |

### 2.3 Stub Compatibility Assessment

The `LanguageServerStubs.cs` file defines 15 stub types that the 173 LS tests compile against. These stubs define the **test-facing API contract** — the implementation must satisfy these signatures. Key observations:

1. `PreceptAnalyzer` — document-level façade with `GetDiagnostics`, `GetCompletions`, `GetCodeActions`, plus static catalog-coverage properties (`TypeItems`, `NumberConstraintItems`, etc.)
2. `PreceptDocumentIntellisense` — stateless hover/completion/definition/symbol logic
3. `PreceptSemanticTokensHandler` — classified tokens + constraint set extraction
4. `PreceptParser` / `PreceptCompiler` — v1 compatibility shims wrapping `Compiler.Compile`
5. `PreceptPreviewHandler` — preview protocol handler
6. `PreceptCodeActionHandler` — code action handler

**Key decision:** The stub API is the v1 test contract. The implementation will satisfy these signatures by routing to the v2 `Compilation` pipeline. The stubs will be replaced with real implementations slice by slice — each slice turns a set of red tests green.

### 2.4 Design vs. Existing Stubs — Structural Delta

The design doc (§16) envisions a handler-per-file structure (`Handlers/DiagnosticsHandler.cs`, etc.) while the stubs use a different factoring (`PreceptAnalyzer`, `PreceptDocumentIntellisense`, `PreceptSemanticTokensHandler`). 

**Decision:** Honor the stub API contract (the tests depend on it) AND implement the OmniSharp handler structure. The stubs become real classes that delegate to shared infrastructure. OmniSharp handlers delegate to the same classes the tests call.

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

**Dependency ordering:** Must complete before Slice 6. Independent of Slices 0–5, 7–11.

---

### Slice 0: Infrastructure — DocumentState + Compile Loop

**What:** Core infrastructure that all features depend on.

**Creates:**
- `tools/Precept.LanguageServer/DocumentState.cs` — per-document `Compilation` + `Precept` holder with atomic swap
- `tools/Precept.LanguageServer/Handlers/TextDocumentSyncHandler.cs` — OmniSharp `ITextDocumentSyncHandler` registering `didOpen`, `didChange`, `didClose`

**Modifies:**
- `tools/Precept.LanguageServer/LanguageServerStubs.cs` — replace `PreceptTextDocumentSyncHandler` stub with real `SharedAnalyzer` wiring
- `tools/Precept.LanguageServer/Program.cs` — register the sync handler + `PreceptAnalyzer` as singleton in the DI container; declare server capabilities

**Method-level specificity:**

```csharp
// DocumentState.cs
sealed class DocumentState
{
    private volatile Compilation? _current;
    private volatile Precept.Runtime.Precept? _precept;
    
    public Compilation? Current => _current;
    public Precept.Runtime.Precept? Precept => _precept;
    
    public void Update(Compilation compilation) { ... }  // Interlocked.Exchange
}

// TextDocumentSyncHandler.cs : ITextDocumentSyncHandler
public Task<Unit> Handle(DidOpenTextDocumentParams p, CancellationToken ct) { ... }
public Task<Unit> Handle(DidChangeTextDocumentParams p, CancellationToken ct) { ... }
public Task<Unit> Handle(DidCloseTextDocumentParams p, CancellationToken ct) { ... }
```

**Tests:**
- Existing `PreceptAnalyzer` tests that call `SetDocumentText` + `GetDiagnostics` will start passing (transition from red to green)
- No new tests in this slice — infrastructure is tested through feature slices

**Regression anchors:** None — this is greenfield infrastructure.

**Dependency ordering:** This slice must complete first. All other slices depend on it.

---

### Slice 1: Diagnostics Push

**What:** Surface `Compilation.Diagnostics` as LSP diagnostics on every document change.

**Modifies:**
- `tools/Precept.LanguageServer/LanguageServerStubs.cs` — implement `PreceptAnalyzer.GetDiagnostics(DocumentUri)`:
  1. Retrieve document text via `TryGetDocumentText`
  2. Call `Compiler.Compile(text)`
  3. Map each `Diagnostic` → LSP `Diagnostic` (severity, range, code, message)
  4. 1-based `SourceSpan` → 0-based LSP `Range` conversion

**Creates:**
- `tools/Precept.LanguageServer/Handlers/DiagnosticsHandler.cs` — if OmniSharp push model needs explicit wiring (may be handled by sync handler)

**Method-level specificity:**

```csharp
// PreceptAnalyzer.GetDiagnostics — replace throw with:
public IReadOnlyList<Diagnostic> GetDiagnostics(DocumentUri uri)
{
    if (!TryGetDocumentText(uri, out var text)) return [];
    var compilation = Compiler.Compile(text);
    return compilation.Diagnostics.Select(d => new Diagnostic
    {
        Range = ToLspRange(d.Span),
        Severity = MapSeverity(d.Severity),
        Code = d.Code,
        Source = "precept",
        Message = d.Message
    }).ToArray();
}

private static OmniSharp.Range ToLspRange(SourceSpan span) => new(
    new Position(span.StartLine - 1, span.StartColumn - 1),
    new Position(span.EndLine - 1, span.EndColumn - 1));

private static DiagnosticSeverity MapSeverity(Severity s) => s switch { ... };
```

**Tests turned green:**
- `PreceptAnalyzerDiagnosticRangeTests` — all tests (diagnostic squiggle line accuracy)
- `PreceptAnalyzerCollectionMutationTests` — all tests (diagnostics for collection type mismatches)
- `PreceptAnalyzerNullNarrowingTests` — all tests (guard narrowing produces no false diagnostics)
- `PreceptAnalyzerRuleWarningTests` — all tests (rule warning diagnostics)
- `PreceptAnalyzerEventSurfaceTests` — all tests (event surface warnings)

**Regression anchors:** All `test/Precept.Tests/` tests must continue passing (core pipeline unchanged).

**Dependency ordering:** Depends on Slice 0. No downstream dependencies — can ship independently.

---

### Slice 2: Semantic Tokens — Pass 1 (Lexical)

**What:** Walk `Compilation.Tokens` and emit semantic tokens based on `TokenMeta.SemanticTokenType`.

**Modifies:**
- `tools/Precept.LanguageServer/LanguageServerStubs.cs` — implement `PreceptSemanticTokensHandler.GetClassifiedTokens(string dsl)`:
  1. Call `Compiler.Compile(dsl)` (or `Lexer.Lex(dsl)` for Pass 1 only)
  2. Walk token stream
  3. For each token where `Tokens.GetMeta(token.Kind).SemanticTokenType` is non-null, emit a `ClassifiedSemanticToken`

**Creates:**
- `tools/Precept.LanguageServer/Handlers/SemanticTokensHandler.cs` — OmniSharp `ISemanticTokensFullHandler` registration + capability declaration

**Method-level specificity:**

```csharp
// PreceptSemanticTokensHandler.GetClassifiedTokens — replace throw:
public static IReadOnlyList<ClassifiedSemanticToken> GetClassifiedTokens(string dsl)
{
    var compilation = Compiler.Compile(dsl);
    var result = new List<ClassifiedSemanticToken>();
    
    foreach (var token in compilation.Tokens)
    {
        var meta = Tokens.GetMeta(token.Kind);
        if (meta.SemanticTokenType is null) continue;
        result.Add(new ClassifiedSemanticToken(token.Text, meta.SemanticTokenType));
    }
    
    // Pass 2: identifier classification from SemanticIndex (if available)
    // ... (see Slice 3)
    
    return result;
}
```

**Tests turned green:**
- `PreceptSemanticTokensClassificationTests` — keyword/comment/message classification tests

**Regression anchors:** All `test/Precept.Tests/` tests.

**Dependency ordering:** Depends on Slice 0. Independent of Slice 1.

---

### Slice 3: Semantic Tokens — Pass 2 (Identifiers) + Constraint Sets

**What:** Walk `SemanticIndex` reference bindings to classify identifiers by semantic role. Implement `ExtractConstraintSets` and `BuildConstraintSets`.

**Modifies:**
- `tools/Precept.LanguageServer/LanguageServerStubs.cs`:
  - Extend `GetClassifiedTokens` with Pass 2: field refs → `"property"`, state refs → `"enum"`, event refs → `"function"`, arg refs → `"parameter"`
  - Implement `ExtractConstraintSets(PreceptDefinition)` — extract states/events/fields referenced in ensures/rules
  - Implement `BuildConstraintSets(string dsl)` — compile + extract

**Method-level specificity:**

Pass 2 walks `SemanticIndex.FieldReferences`, `.StateReferences`, `.EventReferences` and maps each reference site span to its semantic token type. This uses the CC#3 reference site records already present on `SemanticIndex`.

```csharp
// Pass 2 addition inside GetClassifiedTokens:
if (compilation.Semantics is { } index)
{
    foreach (var fr in index.FieldReferences)
        result.Add(new ClassifiedSemanticToken(fr.Field.Name, "property"));
    foreach (var sr in index.StateReferences)
        result.Add(new ClassifiedSemanticToken(sr.State.Name, "enum"));
    foreach (var er in index.EventReferences)
        result.Add(new ClassifiedSemanticToken(er.Event.Name, "function"));
}

// ExtractConstraintSets:
public static (HashSet<string> States, HashSet<string> Events, HashSet<string> Fields)
    ExtractConstraintSets(PreceptDefinition definition) { ... }
// Uses SemanticIndex.Ensures, .Rules to find anchored states/events and referenced fields.
```

**Tests turned green:**
- `PreceptSemanticTokensConstraintTests` — constraint set extraction tests

**Regression anchors:** Slice 2 tests must remain green.

**Dependency ordering:** Depends on Slice 2. The `PreceptDefinition` type needs a bridge to hold the v2 `Compilation` internally.

---

### Slice 4: Completions — Catalog-Driven

**What:** Context-aware completions driven by catalogs and `SemanticIndex`.

**Modifies:**
- `tools/Precept.LanguageServer/LanguageServerStubs.cs`:
  - Implement `PreceptAnalyzer.GetCompletions(DocumentUri, Position)` — identify cursor context (SlotContext), query appropriate catalog
  - Implement static catalog-coverage properties: `TypeItems`, `NumberConstraintItems`, `StringConstraintItems`, `CollectionConstraintItems`, `DecimalConstraintItems`, `ChoiceConstraintItems`, `ArrowItems`, `LiteralItems`, `ExpressionOperatorItems`, `ScalarTypeItems`
  - Implement `PreceptDocumentIntellisense.GetCompletions(info, position)`

**Creates:**
- `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs` — OmniSharp `ICompletionHandler`
- `tools/Precept.LanguageServer/SlotContext.cs` — cursor context enum + `GetCursorContext` logic

**Method-level specificity:**

```csharp
// SlotContext.cs
enum SlotContext { TopLevel, AfterKeyword, InTypePosition, InModifierPosition, 
                   InStateTarget, InEventTarget, InFieldTarget, InActionVerb, 
                   InExpression, InArgDefault }

static SlotContext GetCursorContext(ConstructManifest manifest, Position position) { ... }

// PreceptAnalyzer.GetCompletions — delegates to catalog queries per SlotContext:
// TopLevel → Constructs.All.Select(c => c.PrimaryLeadingToken) via Tokens
// InTypePosition → Types.All.Where(t => t.Token != null).Select(...)
// InModifierPosition → Modifiers.All filtered by ConstructMeta.ModifierDomain
// InStateTarget → SemanticIndex.States
// InEventTarget → SemanticIndex.Events
// InFieldTarget → SemanticIndex.Fields
// InActionVerb → Actions.All filtered by field type
// InExpression → SemanticIndex.Fields + Functions.All + Operators.All

// Static catalog-coverage properties:
public static CompletionItem[] TypeItems =>
    Types.All.Where(t => t.Token != null)
        .Select(t => new CompletionItem { Label = t.Token!.Text, Kind = CompletionItemKind.TypeParameter,
            Documentation = t.HoverDescription }).ToArray();
```

**Tests turned green:**
- `PreceptAnalyzerCompletionTests` — all completion context tests
- `PreceptAnalyzerStatelessCompletionTests` — edit-mode completion tests

**Regression anchors:** Slices 1–3 tests.

**Dependency ordering:** Depends on Slices 0 and 1 (needs `Compilation` to identify cursor context). Independent of Slices 2–3.

---

### Slice 5: Hover

**What:** Documentation on hover for keywords, types, identifiers, and diagnostic codes.

**Modifies:**
- `tools/Precept.LanguageServer/LanguageServerStubs.cs`:
  - Implement `PreceptDocumentIntellisense.Analyze(text)` — wraps `Compiler.Compile` into a `PreceptDocumentInfo`
  - Implement `PreceptDocumentIntellisense.CreateHover(info, position)` — keyword hover via `TokenMeta.HoverDescription`, identifier hover via `SemanticIndex` lookup

**Creates:**
- `tools/Precept.LanguageServer/Handlers/HoverHandler.cs` — OmniSharp `IHoverHandler`

**Method-level specificity:**

```csharp
// PreceptDocumentInfo wraps a Compilation internally
public sealed class PreceptDocumentInfo
{
    internal Compilation? Compilation { get; init; }
}

// Analyze:
public static PreceptDocumentInfo Analyze(string text) =>
    new() { Compilation = Compiler.Compile(text) };

// CreateHover:
public static Hover? CreateHover(PreceptDocumentInfo info, Position position)
{
    // 1. Find token at position in TokenStream
    // 2. If token has TokenMeta.HoverDescription → return keyword hover
    // 3. If token is Identifier and SemanticIndex available:
    //    - Lookup in FieldsByName/StatesByName/EventsByName
    //    - Format per design §7.4: field type + modifiers, state modifiers, event args
    //    - For fields with proof obligations, include proven-safe section
    // 4. If token is a diagnostic code → lookup DiagnosticMeta, show description
    //    (enriched with TriggerCondition/RecoverySteps — George-15's additions have landed in DiagnosticMeta)
}
```

**Tests turned green:**
- `PreceptIntellisenseNavigationTests` — hover tests (field reference shows type + default)
- `PreceptHoverProofTests` — hover proof section tests (proven-safe attribution)

**Regression anchors:** Slices 1–4 tests.

**Dependency ordering:** Depends on Slice 0. Independent of Slices 1–4 for implementation, but tests may overlap.

---

### Slice 6: Go-to-Definition + Document Symbols

**What:** Navigate to field/state/event declarations. Document outline for the sidebar.

**Modifies:**
- `tools/Precept.LanguageServer/LanguageServerStubs.cs`:
  - Implement `PreceptDocumentIntellisense.CreateDefinition(uri, info, position)` — find identifier at position, resolve via `SemanticIndex.*sByName` → `Syntax.Span`
  - Implement `PreceptDocumentIntellisense.GetDefinitions(info, position)` (variant without URI)
  - Implement `PreceptDocumentIntellisense.CreateDocumentSymbols(info)` and `GetDocumentSymbols(info)`

**Creates:**
- `tools/Precept.LanguageServer/Handlers/DefinitionHandler.cs` — OmniSharp `IDefinitionHandler`
- `tools/Precept.LanguageServer/Handlers/DocumentSymbolHandler.cs` — OmniSharp `IDocumentSymbolHandler`

**Method-level specificity:**

```csharp
// CreateDefinition:
public static IEnumerable<LocationOrDocumentSymbol> CreateDefinition(
    DocumentUri uri, PreceptDocumentInfo info, Position position)
{
    // 1. Find token at position
    // 2. If Identifier, search SemanticIndex.FieldReferences/StateReferences/EventReferences
    //    for a reference site overlapping position
    // 3. Resolve target → FieldsByName[name].Syntax.Span / StatesByName[name].NameSpan / etc.
    // 4. Return LocationOrDocumentSymbol.From(new Location { Uri = uri, Range = ToLspRange(span) })
}

// CreateDocumentSymbols:
public static IEnumerable<LocationOrDocumentSymbol> CreateDocumentSymbols(PreceptDocumentInfo info)
{
    // Walk ConstructManifest.Constructs
    // Filter: c.Meta.IsOutlineNode == true
    // Map: Enum.Parse<SymbolKind>(c.Meta.OutlineSymbolTag) — catalog-driven, no ConstructKind switch
    // Extract name from construct tokens (first identifier token)
    // Return DocumentSymbol with Name, Kind, Range, SelectionRange
}
```

**Catalog dependency:** ✅ Resolved by Slice 0a. `ConstructMeta.IsOutlineNode` and `OutlineSymbolTag` are added as catalog fields. Document outline reads `c.Meta.IsOutlineNode` / `c.Meta.OutlineSymbolTag` directly — no `ConstructKind` switches.

**Tests turned green:**
- `PreceptIntellisenseNavigationTests` — go-to-definition tests

**Regression anchors:** All prior slice tests.

**Dependency ordering:** Depends on Slice 0 (infrastructure), Slice 0a (catalog prerequisite), and Slice 5 (`PreceptDocumentInfo` infrastructure). Independent of Slices 1–4.

---

### Slice 7: Diagnostic Enrichment ("Did You Mean?")

**What:** Fuzzy-match enrichment for `UndeclaredField`/`UndeclaredState`/`UndeclaredEvent`/`UndeclaredFunction` diagnostics.

**Creates:**
- `tools/Precept.LanguageServer/LevenshteinDistance.cs` — pure Levenshtein implementation (~20 lines)
- `tools/Precept.LanguageServer/DiagnosticEnricher.cs` — enrichment logic:

**Method-level specificity:**

```csharp
// DiagnosticEnricher.cs
static class DiagnosticEnricher
{
    // Returns enriched diagnostics + suggestion map for code actions
    public static (IReadOnlyList<Diagnostic> Diagnostics, Dictionary<string, string> Suggestions)
        Enrich(IReadOnlyList<Diagnostic> diagnostics, SemanticIndex? index)
    {
        // For each diagnostic with SuggestionSources in DiagnosticMeta:
        // 1. Resolve suggestion pool (UserFields, UserStates, UserEvents, Functions.All names)
        // 2. Compute Levenshtein distance from Args[0] to each candidate
        // 3. Filter ≤ 3, pick lowest, tiebreak alphabetically
        // 4. Append " — did you mean 'X'?" to message
        // 5. Record suggestion in map for code action slice
    }
}
```

**Tests:** New tests in `test/Precept.LanguageServer.Tests/`:
- `[Theory] Enrichment_UndeclaredField_SuggestsClosestMatch` — multiple field name typos
- `[Theory] Enrichment_NoMatchWithin3Edits_NoSuggestion` — threshold enforcement
- `[Fact] Enrichment_TiebreakAlphabetical` — alphabetical tiebreak
- `[Fact] Enrichment_NullSemanticIndex_SkipsEnrichment` — graceful degradation
- `[Fact] Enrichment_EmptyPool_NoSuggestion` — empty field/state/event sets
- `[Fact] Enrichment_IdenticalName_NoSuggestion` — distance-0 guard

**Regression anchors:** All prior slice tests. All `test/Precept.Tests/` pipeline tests.

**Dependency ordering:** Depends on Slice 1 (diagnostics infrastructure). Unlocks Slice 8.

---

### Slice 8: Code Actions

**What:** Quick-fix code actions for "did you mean?" renames, unterminated literal fixes, and FixHint tooltips.

**Modifies:**
- `tools/Precept.LanguageServer/LanguageServerStubs.cs`:
  - Implement `PreceptAnalyzer.GetCodeActions(uri, range, diagnostics)` — dispatch to enrichment suggestions and FixHint catalog
  - Implement `PreceptCodeActionHandler.HandleAsync`

**Creates:**
- `tools/Precept.LanguageServer/Handlers/CodeActionHandler.cs` — OmniSharp `ICodeActionHandler`
- `tools/Precept.VsCode/package.json` — register `precept.showFixHint` command for tooltip-only code actions

**DiagnosticMeta enrichment (George-15 fields — landed):** Code actions read `DiagnosticMeta.ExampleBefore` and `DiagnosticMeta.ExampleAfter` to populate fix-hint panels with before/after DSL snippets. These fields are already present on `DiagnosticMeta` — no prerequisite wait.

**Method-level specificity:**

```csharp
// PreceptAnalyzer.GetCodeActions:
public IReadOnlyList<CodeAction> GetCodeActions(DocumentUri uri, Range range, 
    IReadOnlyList<Diagnostic> diagnostics)
{
    var actions = new List<CodeAction>();
    foreach (var diag in diagnostics)
    {
        // 1. Check enrichment suggestions map → mechanical rename action
        // 2. Check DiagnosticMeta.FixHint → mechanical fix or tooltip-only action
        // 3. Special cases: UnterminatedStringLiteral → insert closing "
        //                   UnterminatedTypedConstant → insert closing '
    }
    return actions;
}
```

**Tests turned green:**
- `PreceptCodeActionTests` — all code action tests

**Regression anchors:** Slice 7 tests.

**Dependency ordering:** Depends on Slice 7 (enrichment). Independent of Slices 2–6.

---

### Slice 9: Folding Ranges

**What:** Multi-line construct folding.

**Creates:**
- `tools/Precept.LanguageServer/Handlers/FoldingRangeHandler.cs` — OmniSharp `IFoldingRangeHandler`

**Method-level specificity:**

```csharp
// FoldingRangeHandler:
FoldingRange[] GetFoldingRanges(Compilation compilation) =>
    compilation.ConstructManifest.Constructs
        .Where(c => c.Span.EndLine > c.Span.StartLine)
        .Select(c => new FoldingRange
        {
            StartLine = c.Span.StartLine - 1,  // 0-based
            EndLine = c.Span.EndLine - 1,
            Kind = c.Meta.Kind == ConstructKind.Comment 
                ? FoldingRangeKind.Comment 
                : FoldingRangeKind.Region
        }).ToArray();
```

**Tests:** Minimal — folding is visual only. One integration test verifying multi-line constructs produce folding ranges.

**Regression anchors:** None — new handler, no existing tests.

**Dependency ordering:** Depends on Slice 0. Fully independent of all other slices.

---

### Slice 10: Preview/Inspect — DEFERRED

> **Deferred until the runtime evaluator is implemented.** `PreceptPreviewHandler` remains a stub. `PreceptPreviewRulesTests` remains red. This slice will be planned and executed as a follow-on once the runtime evaluator (Phase 3) ships.

---

### Slice 11: v1 Compatibility Shims + Program.cs Registration

**What:** Wire `PreceptParser.ParseWithDiagnostics`, `PreceptParser.Parse`, `PreceptCompiler.Compile` as thin wrappers around `Compiler.Compile`. Register all OmniSharp handlers in `Program.cs`.

**Modifies:**
- `tools/Precept.LanguageServer/LanguageServerStubs.cs`:
  - `PreceptParser.ParseWithDiagnostics` → call `Compiler.Compile`, wrap result in `PreceptDefinition`, extract diagnostics as `ParseDiagnostic` list
  - `PreceptParser.Parse` → call `Compiler.Compile`, wrap in `PreceptMachineAst`
  - `PreceptCompiler.Compile` → call `Compiler.Compile` via the AST shim
  - `PreceptDefinition` and `PreceptMachineAst` hold `Compilation` internally

- `tools/Precept.LanguageServer/Program.cs`:
  - Register all handlers with `options.WithHandler<T>()` calls
  - Declare full server capabilities (completion, hover, definition, semanticTokens, codeAction, foldingRange, documentSymbol)

**Tests turned green:**
- `PreceptSemanticTokensConstraintTests` — tests that use `PreceptParser.ParseWithDiagnostics` + `ExtractConstraintSets`
- Remaining compatibility tests

**Dependency ordering:** Depends on all handler slices (1–9). This is the integration/wiring slice.

---

## 4. File Inventory

| File | Slices | Action |
|---|---|---|
| `tools/Precept.LanguageServer/Program.cs` | 0, 11 | Modify — add handler registration + capability declaration |
| `tools/Precept.LanguageServer/LanguageServerStubs.cs` | 1–8, 11 | Modify — replace `throw NotImplementedException` with real implementations, slice by slice |
| `tools/Precept.LanguageServer/DocumentState.cs` | 0 | Create |
| `tools/Precept.LanguageServer/SlotContext.cs` | 4 | Create |
| `tools/Precept.LanguageServer/LevenshteinDistance.cs` | 7 | Create |
| `tools/Precept.LanguageServer/DiagnosticEnricher.cs` | 7 | Create |
| `tools/Precept.LanguageServer/Handlers/TextDocumentSyncHandler.cs` | 0 | Create |
| `tools/Precept.LanguageServer/Handlers/DiagnosticsHandler.cs` | 1 | Create (if needed for push wiring) |
| `tools/Precept.LanguageServer/Handlers/SemanticTokensHandler.cs` | 2–3 | Create |
| `tools/Precept.LanguageServer/Handlers/CompletionHandler.cs` | 4 | Create |
| `tools/Precept.LanguageServer/Handlers/HoverHandler.cs` | 5 | Create |
| `tools/Precept.LanguageServer/Handlers/DefinitionHandler.cs` | 6 | Create |
| `tools/Precept.LanguageServer/Handlers/DocumentSymbolHandler.cs` | 6 | Create |
| `tools/Precept.LanguageServer/Handlers/CodeActionHandler.cs` | 8 | Create |
| `tools/Precept.LanguageServer/Handlers/FoldingRangeHandler.cs` | 9 | Create |
| `tools/Precept.LanguageServer/Handlers/InspectHandler.cs` | 10 (deferred) | Not created in this phase |
| `test/Precept.LanguageServer.Tests/*` | 1–8 | Existing — tests transition from red to green |
| `src/Precept/Language/Construct.cs` | 0a | Modify — add `IsOutlineNode` and `OutlineSymbolTag` parameters to `ConstructMeta` |
| `src/Precept/Language/Constructs.cs` | 0a | Modify — set `IsOutlineNode: true` and `OutlineSymbolTag` on 5 construct entries |
| `tools/Precept.VsCode/package.json` | 8 | Modify — register `precept.showFixHint` command |

---

## 5. Dependency Graph

```
Slice 0 (Infrastructure)
├── Slice 1 (Diagnostics) → Slice 7 (Enrichment) → Slice 8 (Code Actions)
├── Slice 2 (Semantic Tokens Pass 1) → Slice 3 (Semantic Tokens Pass 2)
├── Slice 4 (Completions)
├── Slice 5 (Hover) → Slice 6 (Go-to-Def + Outline) ← Slice 0a (Catalog: IsOutlineNode)
├── Slice 9 (Folding)
└── Slice 11 (Wiring + v1 Shims — depends on all)

Slice 10 (Preview/Inspect): DEFERRED — implement after runtime evaluator ships

Slice 0a (Catalog Prerequisite)
└── Slice 6 (Go-to-Def + Outline)
```

**Parallelizable groups after Slice 0:**
- Group A: Slices 1 → 7 → 8 (diagnostics chain)
- Group B: Slices 2 → 3 (semantic tokens)
- Group C: Slice 4 (completions)
- Group D: Slices 5 → 6 (hover + navigation); Slice 6 also requires Slice 0a
- Group E: Slice 9 (folding)
- Slice 0a: Independent — can run in parallel with Slice 0 or any other group

Groups A–E can proceed in parallel. Slice 11 is the final integration.

---

## 6. Tooling / MCP Sync Assessment

| Category | Impact | Detail |
|---|---|---|
| **TextMate grammar** | No changes needed | Grammar is generated from catalogs; LS does not affect it |
| **Completions** | Catalog-driven — no LS-side hardcoding | When catalog entries gain `SnippetTemplate` or `HoverDescription`, completions automatically pick them up |
| **Semantic tokens** | No grammar changes | LS Pass 1 reads `TokenMeta.SemanticTokenType` already present; Pass 2 reads `SemanticIndex` references already present |
| **MCP tools** | No changes needed | MCP and LS both consume the same catalogs; they are independent consumers |
| **VS Code extension** | Minor | Extension already expects an LS process on stdio; handler registration completes the contract. `precept.showFixHint` command registration in `package.json` is required for tooltip-only code actions (Slice 8) — add it in Slice 8's creates list. |

---

## 7. Catalog Prerequisites (Resolved)

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
- **Slice 5 (Hover):** `CreateHover` reads `TriggerCondition` and `RecoverySteps` for diagnostic code hover popups. No prerequisite wait — implement immediately.
- **Slice 8 (Code Actions):** `GetCodeActions` reads `ExampleBefore` / `ExampleAfter` for enriched fix-hint panels. No prerequisite wait — implement immediately.

### Resolved: `TypeMeta.IsUserFacing`

CC#16 resolved that `Error` and `StateRef` types should not appear in completions. The field isn't on `TypeMeta`, but `Token != null` is a structural equivalent for filtering internal types (internal types have `Token = null`). No catalog change required — this is the permanent solution.

---

## 8. Risk Assessment

| Risk | Likelihood | Mitigation |
|---|---|---|
| `Compilation` record shape changes mid-implementation | Medium | LS consumes `Compilation` read-only; structural changes in core pipeline are independent |
| 173 ported tests expect v1 API shapes | High | Stub classes preserve the API contract; implementations satisfy the same signatures |
| OmniSharp.Extensions.LanguageServer API surprises | Low | Version 0.19.9 is stable; handler patterns are well-documented |
| Preview/Inspect blocked indefinitely | Medium | Slice 10 ships the handler shell; tests remain red. No downstream dependency. |
| `ConstructMeta.IsOutlineNode` implementation | Low | Slice 0a is a concrete task with method-level specificity; blocks only Slice 6 |

---

## 9. Implementation Order (Recommended)

1. **Slice 0a** — Catalog Prerequisite: `IsOutlineNode` + `OutlineSymbolTag` (independent, unblocks Slice 6)
2. **Slice 0** — Infrastructure (must be first for all LS slices)
3. **Slice 1** — Diagnostics (highest user-visible value, unlocks most test files)
4. **Slice 2** — Semantic Tokens Pass 1 (visual value, no SemanticIndex dependency)
5. **Slice 5** — Hover (high user value; includes `DiagnosticMeta.TriggerCondition`/`RecoverySteps` enrichment)
6. **Slice 4** — Completions (complex but high value)
7. **Slice 3** — Semantic Tokens Pass 2 (enriches Pass 1)
8. **Slice 6** — Go-to-Definition + Outline (reads `IsOutlineNode`/`OutlineSymbolTag` from Slice 0a)
9. **Slice 7** — Diagnostic Enrichment
10. **Slice 8** — Code Actions (includes `DiagnosticMeta.ExampleBefore`/`ExampleAfter` enrichment; registers `precept.showFixHint` in `package.json`)
11. **Slice 9** — Folding Ranges
12. **Slice 10** — Preview (when runtime is available)
13. **Slice 11** — Final wiring + v1 shims

---

## 10. Estimated Scope

| Metric | Estimate |
|---|---|
| New files | ~14 |
| Modified files | 5 (stubs, Program.cs, Construct.cs, Constructs.cs, package.json) |
| Estimated LOC (production) | 500–700 |
| Estimated LOC (tests — new) | ~200 (Slice 7 enrichment tests) |
| Existing tests turned green | 173 (across 13 test files) |
| Calendar estimate | 4–6 slice sessions |
