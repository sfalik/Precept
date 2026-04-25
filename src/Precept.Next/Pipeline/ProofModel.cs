using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

public sealed record class ProofModel(
    ImmutableArray<Diagnostic> Diagnostics
);
