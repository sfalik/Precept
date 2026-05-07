using System.Collections.Immutable;
using Precept.Language;

namespace Precept.Pipeline;

public sealed record class Compilation(
    TokenStream                Tokens,
    ConstructManifest          ConstructManifest,
    SymbolTable                Symbols,
    SemanticIndex              Semantics,
    StateGraph                 Graph,
    ProofLedger                Proof,
    ImmutableArray<Diagnostic> Diagnostics,
    bool                       HasErrors
);
