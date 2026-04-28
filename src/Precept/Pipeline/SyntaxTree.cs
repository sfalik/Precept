using System.Collections.Immutable;
using Precept.Language;
using Precept.Pipeline.SyntaxNodes;

namespace Precept.Pipeline;

public sealed record class SyntaxTree(
    PreceptHeaderNode? Header,
    ImmutableArray<Declaration> Declarations,
    ImmutableArray<Diagnostic> Diagnostics
);
