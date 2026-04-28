using System.Collections.Immutable;
using System.Linq;
using Precept.Language;
using Precept.Pipeline;

namespace Precept;

public static class Compiler
{
    public static Compilation Compile(string source)
    {
        TokenStream   tokens    = Lexer.Lex(source);
        SyntaxTree    tree      = Parser.Parse(tokens);
        SemanticIndex semantics = TypeChecker.Check(tree);
        StateGraph    graph     = GraphAnalyzer.Analyze(semantics);
        ProofLedger   proof     = ProofEngine.Prove(semantics, graph);

        ImmutableArray<Diagnostic> diagnostics =
        [
            ..tokens.Diagnostics,
            ..tree.Diagnostics,
            ..semantics.Diagnostics,
            ..graph.Diagnostics,
            ..proof.Diagnostics,
        ];

        return new Compilation(
            Tokens:      tokens,
            SyntaxTree:  tree,
            Semantics:   semantics,
            Graph:       graph,
            Proof:       proof,
            Diagnostics: diagnostics,
            HasErrors:   diagnostics.Any(d => d.Severity == Severity.Error)
        );
    }
}
