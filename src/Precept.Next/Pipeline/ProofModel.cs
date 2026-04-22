using System.Collections.Immutable;

namespace Precept.Pipeline;

public sealed record class ProofModel(
    ImmutableArray<Diagnostic> Diagnostics
);
