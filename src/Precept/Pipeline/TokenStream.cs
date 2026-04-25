using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

public sealed record class TokenStream(
    ImmutableArray<Token>      Tokens,
    ImmutableArray<Diagnostic> Diagnostics
);
