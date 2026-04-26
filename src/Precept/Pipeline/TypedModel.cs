using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

public sealed record class TypedModel(
    ImmutableArray<Diagnostic> Diagnostics
);
