using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

public sealed record class ConstructManifest(
    ImmutableArray<ParsedConstruct> Constructs,
    ImmutableArray<Diagnostic>      Diagnostics
)
{
    private ILookup<ConstructKind, ParsedConstruct>? _byKind;

    /// <summary>
    /// Lookup of constructs grouped by their kind. Provides O(1) access to all
    /// constructs of a given kind for type checker, graph analyzer, etc.
    /// </summary>
    public ILookup<ConstructKind, ParsedConstruct> ByKind =>
        _byKind ??= Constructs.ToLookup(c => c.Meta.Kind);
}
