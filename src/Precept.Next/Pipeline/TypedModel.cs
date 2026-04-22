using System.Collections.Immutable;

namespace Precept.Pipeline;

public sealed record class TypedModel(
    ImmutableArray<Diagnostic> Diagnostics
);
