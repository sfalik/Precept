using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

public sealed record class SyntaxTree(
    PreceptNode                Root,
    ImmutableArray<Diagnostic> Diagnostics
);
