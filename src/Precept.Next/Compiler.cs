using System.Collections.Immutable;
using System.Linq;
using Precept.Pipeline;

namespace Precept;

public static class Compiler
{
    public static CompilationResult Compile(string source)
    {
        TokenStream tokens = Lexer.Lex(source);
        SyntaxTree  tree   = Parser.Parse(tokens);
        TypedModel  model  = TypeChecker.Check(tree);
        GraphResult graph  = GraphAnalyzer.Analyze(model);
        ProofModel  proof  = ProofEngine.Prove(model, graph);

        ImmutableArray<Diagnostic> diagnostics =
        [
            ..tree.Diagnostics,
            ..model.Diagnostics,
            ..graph.Diagnostics,
            ..proof.Diagnostics,
        ];

        return new CompilationResult(
            Tokens:      tokens,
            SyntaxTree:  tree,
            Model:       model,
            Graph:       graph,
            Proof:       proof,
            Diagnostics: diagnostics,
            HasErrors:   diagnostics.Any(d => d.Severity == Severity.Error)
        );
    }
}
