using System.Collections.Immutable;

namespace Precept.Pipeline;

public sealed record class TokenStream(
    ImmutableArray<Token>      Tokens,
    ImmutableArray<Diagnostic> Diagnostics
);
