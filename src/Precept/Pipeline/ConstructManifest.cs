using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

public sealed record class ConstructManifest(
    ImmutableArray<ParsedConstruct> Constructs,
    ImmutableArray<Diagnostic>      Diagnostics
);
