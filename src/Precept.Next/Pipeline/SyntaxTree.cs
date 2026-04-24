using System.Collections.Immutable;

namespace Precept.Pipeline;

public sealed record class SyntaxTree(
    PreceptNode                Root,
    ImmutableArray<Diagnostic> Diagnostics
);
