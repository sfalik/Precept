using System.Collections.Immutable;

namespace Precept.Pipeline;

public sealed record class GraphResult(
    ImmutableArray<Diagnostic> Diagnostics
);
