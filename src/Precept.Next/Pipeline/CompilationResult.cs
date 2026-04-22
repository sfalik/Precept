using System.Collections.Immutable;

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
