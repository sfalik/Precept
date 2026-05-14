using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

/// <summary>
/// Name-binding stage output: all declared symbols and resolved references.
/// Produced from ConstructManifest by walking ParsedConstruct nodes.
/// </summary>
/// <remarks>
/// <para><b>Hard boundary:</b> SymbolTable contains declarations and references ONLY.
/// No typed expressions, no inferred types, no resolved operations.
/// Those belong in SemanticIndex.</para>
/// </remarks>
public sealed record SymbolTable(
    // ── Declarations ──────────────────────────────────────────────
    ImmutableArray<DeclaredField> Fields,
    ImmutableArray<DeclaredState> States,
    ImmutableArray<DeclaredEvent> Events,

    // ── O(1) Name Lookup Dictionaries ─────────────────────────────
    ImmutableDictionary<string, DeclaredField> FieldsByName,
    ImmutableDictionary<string, DeclaredState> StatesByName,
    ImmutableDictionary<string, DeclaredEvent> EventsByName,

    // ── Reference Sites ───────────────────────────────────────────
    ImmutableArray<SymbolReference> References,

    // ── Stage Diagnostics ─────────────────────────────────────────
    ImmutableArray<Diagnostic> Diagnostics
)
{
    /// <summary>Creates an empty SymbolTable with no declarations, references, or diagnostics.</summary>
    public static SymbolTable Empty { get; } = new(
        ImmutableArray<DeclaredField>.Empty,
        ImmutableArray<DeclaredState>.Empty,
        ImmutableArray<DeclaredEvent>.Empty,
        ImmutableDictionary<string, DeclaredField>.Empty,
        ImmutableDictionary<string, DeclaredState>.Empty,
        ImmutableDictionary<string, DeclaredEvent>.Empty,
        ImmutableArray<SymbolReference>.Empty,
        ImmutableArray<Diagnostic>.Empty
    );
}

// ════════════════════════════════════════════════════════════════════════════
//  Declaration Records
// ════════════════════════════════════════════════════════════════════════════

/// <summary>
/// A field declaration discovered during name binding.
/// Carries identity + type (parser-resolved via Types catalog).
/// </summary>
public sealed record DeclaredField(
    string Name,
    ParsedTypeReference Type,
    ImmutableArray<ParsedModifier> Modifiers,
    bool IsComputed,
    ParsedConstruct Syntax,
    SourceSpan NameSpan,
    int DeclarationOrder
);

/// <summary>
/// A state declaration discovered during name binding.
/// </summary>
public sealed record DeclaredState(
    string Name,
    ImmutableArray<ModifierKind> Modifiers,
    ParsedConstruct Syntax,
    SourceSpan NameSpan
);

/// <summary>
/// An event declaration discovered during name binding.
/// </summary>
public sealed record DeclaredEvent(
    string Name,
    ImmutableArray<DeclaredArg> Args,
    bool IsInitial,
    ParsedConstruct Syntax,
    SourceSpan NameSpan
);

/// <summary>
/// An event argument discovered during name binding.
/// </summary>
public sealed record DeclaredArg(
    string Name,
    ParsedTypeReference Type,
    string EventName,
    ImmutableArray<ModifierKind> Modifiers,
    SourceSpan NameSpan,
    ImmutableArray<ParsedModifier> ParsedModifiers = default
);

// ════════════════════════════════════════════════════════════════════════════
//  Symbol Reference Records
// ════════════════════════════════════════════════════════════════════════════

/// <summary>
/// A reference site: an identifier in source that resolved to a declared symbol
/// (or failed to resolve → UnresolvedTarget + diagnostic).
/// </summary>
public sealed record SymbolReference(
    SourceSpan Site,
    string Name,
    SymbolResolution Resolution
);

/// <summary>
/// Discriminated union for reference resolution results.
/// </summary>
public abstract record SymbolResolution;

/// <summary>Reference resolved to a field declaration.</summary>
public sealed record FieldTarget(DeclaredField Field) : SymbolResolution;

/// <summary>Reference resolved to a state declaration.</summary>
public sealed record StateTarget(DeclaredState State) : SymbolResolution;

/// <summary>Reference resolved to an event declaration.</summary>
public sealed record EventTarget(DeclaredEvent Event) : SymbolResolution;

/// <summary>Reference resolved to an event argument (scoped to enclosing event context).</summary>
public sealed record ArgTarget(DeclaredArg Arg) : SymbolResolution;

/// <summary>Reference resolved to a quantifier binding variable (scoped to quantifier predicate).</summary>
public sealed record BindingTarget(string BindingName) : SymbolResolution;

/// <summary>Reference could not be resolved — diagnostic emitted.</summary>
public sealed record UnresolvedTarget(string AttemptedName, SymbolCategory ExpectedCategory) : SymbolResolution;

/// <summary>What kind of symbol was expected at a reference site.</summary>
public enum SymbolCategory { Field = 1, State = 2, Event = 3, Any = 4 }
