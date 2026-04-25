using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

public sealed record class CompilationResult(
    TokenStream                Tokens,
    SyntaxTree                 SyntaxTree,
    TypedModel                 Model,
    GraphResult                Graph,
    ProofModel                 Proof,
    ImmutableArray<Diagnostic> Diagnostics,
    bool                       HasErrors
);
