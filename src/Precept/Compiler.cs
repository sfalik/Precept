using System.Collections.Immutable;
using System.Linq;
using Precept.Language;
using Precept.Pipeline;

namespace Precept;

public static class Compiler
{
    public static Compilation Compile(string source)
    {
        TokenStream       tokens    = Lexer.Lex(source);
        ConstructManifest manifest  = Parser.Parse(tokens);
        SymbolTable       symbols   = NameBinder.Bind(manifest);
        SemanticIndex     semantics = TypeChecker.Check(manifest, symbols);
        StateGraph        graph     = GraphAnalyzer.Analyze(semantics);
        ProofLedger       proof     = ProofEngine.Prove(semantics, graph);

        ImmutableArray<Diagnostic> diagnostics =
        [
            ..tokens.Diagnostics,
            ..manifest.Diagnostics,
            ..symbols.Diagnostics,
            ..semantics.Diagnostics,
            ..graph.Diagnostics,
            ..proof.Diagnostics,
        ];

        return new Compilation(
            Tokens:            tokens,
            ConstructManifest: manifest,
            Symbols:           symbols,
            Semantics:         semantics,
            Graph:             graph,
            Proof:             proof,
            Diagnostics:       diagnostics,
            HasErrors:         diagnostics.Any(d => d.Severity == Severity.Error)
        );
    }
}
