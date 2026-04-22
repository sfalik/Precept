using System.Collections.Immutable;

namespace Precept.Pipeline;

public sealed record class SyntaxTree(
    ImmutableArray<Diagnostic> Diagnostics
);
