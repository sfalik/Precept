using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

/// <summary>
/// Stub type checker — performs semantic validation of the parsed syntax tree and
/// produces a <see cref="SemanticIndex"/>. Not yet implemented.
/// </summary>
internal static class TypeChecker
{
    internal static SemanticIndex Check(SyntaxTree tree) =>
        new(ImmutableArray<Diagnostic>.Empty);
}
