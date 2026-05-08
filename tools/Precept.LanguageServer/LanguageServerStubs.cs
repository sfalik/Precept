// Stub implementations of Language Server types.
// These provide the API surface required by test/Precept.LanguageServer.Tests/ so
// that all 173 ported tests compile. Implementations throw NotImplementedException
// until the corresponding LS handler slices land (tracked as red tests).

using System.Collections.Concurrent;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.Language;

namespace Precept.LanguageServer;

// ── PreceptAnalyzer ──────────────────────────────────────────────────────────

/// <summary>Document-level analyzer: stores document text, emits LSP diagnostics and completions.</summary>
public sealed class PreceptAnalyzer
{
    private readonly ConcurrentDictionary<DocumentUri, string> _documents = new();

    public void SetDocumentText(DocumentUri uri, string text) => _documents[uri] = text;

    public void RemoveDocument(DocumentUri uri) => _documents.TryRemove(uri, out _);

    public bool TryGetDocumentText(DocumentUri uri, out string text) =>
        _documents.TryGetValue(uri, out text!);

    public IReadOnlyList<OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic> GetDiagnostics(DocumentUri uri) =>
        throw new NotImplementedException("PreceptAnalyzer.GetDiagnostics not yet implemented");

    public IReadOnlyList<CompletionItem> GetCompletions(DocumentUri uri, Position position) =>
        throw new NotImplementedException("PreceptAnalyzer.GetCompletions not yet implemented");

    public IReadOnlyList<CodeAction> GetCodeActions(DocumentUri uri, OmniSharp.Extensions.LanguageServer.Protocol.Models.Range range, IReadOnlyList<OmniSharp.Extensions.LanguageServer.Protocol.Models.Diagnostic> diagnostics) =>
        throw new NotImplementedException("PreceptAnalyzer.GetCodeActions not yet implemented");

    // ── Catalog-coverage completion lists ────────────────────────────────────
    // Exposed as static properties so catalog-coverage tests can verify every
    // Type/Constraint/Action/etc. token appears in at least one completion list.
    // These throw until the real completion logic is implemented.

    public static CompletionItem[] TypeItems =>
        throw new NotImplementedException("PreceptAnalyzer.TypeItems not yet implemented");
    public static CompletionItem[] NumberConstraintItems =>
        throw new NotImplementedException("PreceptAnalyzer.NumberConstraintItems not yet implemented");
    public static CompletionItem[] StringConstraintItems =>
        throw new NotImplementedException("PreceptAnalyzer.StringConstraintItems not yet implemented");
    public static CompletionItem[] CollectionConstraintItems =>
        throw new NotImplementedException("PreceptAnalyzer.CollectionConstraintItems not yet implemented");
    public static CompletionItem[] DecimalConstraintItems =>
        throw new NotImplementedException("PreceptAnalyzer.DecimalConstraintItems not yet implemented");
    public static CompletionItem[] ChoiceConstraintItems =>
        throw new NotImplementedException("PreceptAnalyzer.ChoiceConstraintItems not yet implemented");
    public static CompletionItem[] ArrowItems =>
        throw new NotImplementedException("PreceptAnalyzer.ArrowItems not yet implemented");
    public static CompletionItem[] LiteralItems =>
        throw new NotImplementedException("PreceptAnalyzer.LiteralItems not yet implemented");
    public static CompletionItem[] ExpressionOperatorItems =>
        throw new NotImplementedException("PreceptAnalyzer.ExpressionOperatorItems not yet implemented");
    public static CompletionItem[] ScalarTypeItems =>
        throw new NotImplementedException("PreceptAnalyzer.ScalarTypeItems not yet implemented");
}

// ── PreceptDocumentInfo ──────────────────────────────────────────────────────

/// <summary>Parsed document model — opaque container for hover/completion/definition data.</summary>
public sealed class PreceptDocumentInfo
{
    internal PreceptDocumentInfo() { }
}

// ── PreceptDocumentIntellisense ──────────────────────────────────────────────

/// <summary>Stateless hover/completion/go-to-definition logic over a parsed document.</summary>
public static class PreceptDocumentIntellisense
{
    public static PreceptDocumentInfo Analyze(string text) =>
        throw new NotImplementedException("PreceptDocumentIntellisense.Analyze not yet implemented");

    public static Hover? CreateHover(PreceptDocumentInfo info, Position position) =>
        throw new NotImplementedException("PreceptDocumentIntellisense.CreateHover not yet implemented");

    public static IEnumerable<CompletionItem> GetCompletions(PreceptDocumentInfo info, Position position) =>
        throw new NotImplementedException("PreceptDocumentIntellisense.GetCompletions not yet implemented");

    public static IEnumerable<LocationOrDocumentSymbol> GetDefinitions(PreceptDocumentInfo info, Position position) =>
        throw new NotImplementedException("PreceptDocumentIntellisense.GetDefinitions not yet implemented");

    public static IEnumerable<LocationOrDocumentSymbol> CreateDefinition(DocumentUri uri, PreceptDocumentInfo info, Position position) =>
        throw new NotImplementedException("PreceptDocumentIntellisense.CreateDefinition not yet implemented");

    public static IEnumerable<LocationOrDocumentSymbol> CreateDocumentSymbols(PreceptDocumentInfo info) =>
        throw new NotImplementedException("PreceptDocumentIntellisense.CreateDocumentSymbols not yet implemented");

    public static DocumentSymbol? GetDocumentSymbols(PreceptDocumentInfo info) =>
        throw new NotImplementedException("PreceptDocumentIntellisense.GetDocumentSymbols not yet implemented");
}

// ── ClassifiedSemanticToken / PreceptSemanticTokensHandler ──────────────────

/// <summary>A token with its TextMate-style type and optional modifier, used in semantic token tests.</summary>
public readonly record struct ClassifiedSemanticToken(string Text, string Type, string? Modifier = null);

/// <summary>Semantic token classification for the Precept language server.</summary>
public static class PreceptSemanticTokensHandler
{
    public static IReadOnlyList<ClassifiedSemanticToken> GetClassifiedTokens(string dsl) =>
        throw new NotImplementedException("PreceptSemanticTokensHandler.GetClassifiedTokens not yet implemented");

    /// <summary>Returns (constrainedStates, constrainedEvents, guardedFields) from a parsed definition.</summary>
    public static (HashSet<string> States, HashSet<string> Events, HashSet<string> Fields)
        ExtractConstraintSets(PreceptDefinition definition) =>
        throw new NotImplementedException("PreceptSemanticTokensHandler.ExtractConstraintSets not yet implemented");

    /// <summary>Builds constraint sets directly from DSL source text.</summary>
    public static (HashSet<string> States, HashSet<string> Events, HashSet<string> Fields)
        BuildConstraintSets(string dsl) =>
        throw new NotImplementedException("PreceptSemanticTokensHandler.BuildConstraintSets not yet implemented");
}

// ── PreceptDefinition / PreceptParser / PreceptCompiler ─────────────────────

/// <summary>
/// Parsed precept definition — the v1 model type returned by PreceptParser.ParseWithDiagnostics.
/// Stub: holds no data; exists to make ported v1 tests compile while the v2 LS handlers are built.
/// </summary>
public sealed class PreceptDefinition
{
    internal PreceptDefinition() { }
}

/// <summary>
/// Parse diagnostic emitted by ParseWithDiagnostics.
/// </summary>
public sealed record ParseDiagnostic(string Message, int Line, int Column, string? Code = null);

/// <summary>
/// Compatibility shim — wraps Compiler.Compile so v1-style test code compiles against the v2 core.
/// </summary>
public static class PreceptParser
{
    /// <summary>Parse and type-check DSL text; return (definition, diagnostics).</summary>
    public static (PreceptDefinition? Definition, IReadOnlyList<ParseDiagnostic> Diagnostics)
        ParseWithDiagnostics(string source) =>
        throw new NotImplementedException("PreceptParser.ParseWithDiagnostics not yet implemented");

    /// <summary>Parse DSL without error handling — used by v1 tests that pre-date structured diagnostics.</summary>
    public static PreceptMachineAst Parse(string source) =>
        throw new NotImplementedException("PreceptParser.Parse not yet implemented");
}

/// <summary>V1 AST type returned by PreceptParser.Parse — stub to satisfy v1 test code.</summary>
public sealed class PreceptMachineAst
{
    internal PreceptMachineAst() { }
}

/// <summary>
/// V1 compiler type — compiles a PreceptMachineAst into an executable definition.
/// Stub to satisfy v1 test code.
/// </summary>
public static class PreceptCompiler
{
    public static PreceptExecutableDefinition Compile(PreceptMachineAst machine) =>
        throw new NotImplementedException("PreceptCompiler.Compile not yet implemented");
}

/// <summary>V1 compiled/executable definition — stub for test code that calls CreateInstance / EvaluateCurrentRules.</summary>
public sealed class PreceptExecutableDefinition
{
    public PreceptInstance CreateInstance(Dictionary<string, object?>? initialData = null) =>
        throw new NotImplementedException("PreceptExecutableDefinition.CreateInstance not yet implemented");

    public IReadOnlyList<RuleViolation> EvaluateCurrentRules(PreceptInstance instance) =>
        throw new NotImplementedException("PreceptExecutableDefinition.EvaluateCurrentRules not yet implemented");
}

/// <summary>V1 runtime instance — stub.</summary>
public sealed class PreceptInstance
{
    internal PreceptInstance() { }
}

/// <summary>Rule violation result — stub.</summary>
public sealed record RuleViolation(string Message);

// ── PreceptPreviewHandler ────────────────────────────────────────────────────

/// <summary>Preview handler — processes snapshot/fire/inspect/update requests.</summary>
public sealed class PreceptPreviewHandler
{
    public Task<PreceptPreviewResponse> Handle(PreceptPreviewRequest request, CancellationToken cancellationToken) =>
        throw new NotImplementedException("PreceptPreviewHandler.Handle not yet implemented");
}

// ── PreceptTextDocumentSyncHandler ──────────────────────────────────────────

/// <summary>Document sync handler — manages the shared analyzer instance.</summary>
public static class PreceptTextDocumentSyncHandler
{
    /// <summary>Shared analyzer instance, accessible to code-action tests.</summary>
    public static PreceptAnalyzer SharedAnalyzer { get; } = new PreceptAnalyzer();
}

// ── LocationOrDocumentSymbol ─────────────────────────────────────────────────

/// <summary>
/// Discriminated union of Location and DocumentSymbol — matches the OmniSharp
/// TextDocument/Definition and TextDocument/DocumentSymbol response shapes.
/// Defined here because OmniSharp 0.19.x does not expose this union type directly.
/// </summary>
public sealed class LocationOrDocumentSymbol
{
    private readonly Location? _location;
    private readonly DocumentSymbol? _documentSymbol;

    private LocationOrDocumentSymbol(Location? location, DocumentSymbol? symbol)
    {
        _location = location;
        _documentSymbol = symbol;
    }

    public static LocationOrDocumentSymbol From(Location location) => new(location, null);
    public static LocationOrDocumentSymbol From(DocumentSymbol symbol) => new(null, symbol);

    public bool IsLocation => _location is not null;
    public bool IsDocumentSymbol => _documentSymbol is not null;

    public Location? Location => _location;
    public DocumentSymbol? DocumentSymbol => _documentSymbol;
}

// ── PreceptCodeActionHandler ─────────────────────────────────────────────────

/// <summary>Code action handler — computes quick fixes for diagnostics.</summary>
public sealed class PreceptCodeActionHandler
{
    public Task<CommandOrCodeActionContainer?> Handle(CodeActionParams request, CancellationToken cancellationToken) =>
        throw new NotImplementedException("PreceptCodeActionHandler.Handle not yet implemented");
}

// ── PreceptTokenMeta ─────────────────────────────────────────────────────────

/// <summary>
/// Compatibility shim for v1 PreceptTokenMeta — wraps <see cref="Tokens"/> catalog so
/// catalog-coverage tests can query tokens by <see cref="TokenCategory"/> without
/// knowing the v2 API shape.
/// </summary>
public static class PreceptTokenMeta
{
    /// <summary>Returns all tokens belonging to the given category.</summary>
    public static IEnumerable<TokenMeta> GetByCategory(TokenCategory category) =>
        Tokens.All.Where(t => t.Categories.Contains(category));

    /// <summary>Returns the keyword/operator text for a token, or null for synthetic tokens.</summary>
    public static string? GetSymbol(TokenMeta token) => token.Text;
}
