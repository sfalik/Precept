using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

public sealed record class SemanticIndex(
    ImmutableArray<Diagnostic> Diagnostics
);
