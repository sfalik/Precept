using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

/// <summary>
/// Stub parser — transforms a <see cref="TokenStream"/> into a <see cref="ConstructManifest"/>.
/// Not yet implemented.
/// </summary>
public static class Parser
{
    public static ConstructManifest Parse(TokenStream tokens) =>
        new(ImmutableArray<ParsedConstruct>.Empty, ImmutableArray<Diagnostic>.Empty);
}
