using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

/// <summary>
/// Stub type checker — performs semantic validation of the parsed construct manifest and
/// produces a <see cref="SemanticIndex"/>. Not yet implemented.
/// </summary>
internal static class TypeChecker
{
    internal static SemanticIndex Check(ConstructManifest manifest) =>
        new(ImmutableArray<Diagnostic>.Empty,
            ImmutableArray<FieldReference>.Empty,
            ImmutableArray<StateReference>.Empty,
            ImmutableArray<EventReference>.Empty);
}
