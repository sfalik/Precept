using System;
using System.Collections.Generic;
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
        graph = EnrichGraphWithProofStatus(graph, semantics, proof);

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

    private static StateGraph EnrichGraphWithProofStatus(StateGraph graph, SemanticIndex semantics, ProofLedger proof)
    {
        if (graph.Edges.IsEmpty)
        {
            return graph with { EdgeProofStatuses = ImmutableArray<EdgeProofStatus>.Empty };
        }

        HashSet<(string FromState, string EventName)> explicitStateEvents = semantics.TransitionRows
            .Where(row => row.FromState is not null
                && semantics.StatesByName.ContainsKey(row.FromState)
                && semantics.EventsByName.ContainsKey(row.EventName))
            .Select(row => (row.FromState!, row.EventName))
            .ToHashSet();

        ImmutableArray<EdgeProofStatus> edgeProofStatuses = graph.Edges
            .Select(edge => CreateEdgeProofStatus(edge, semantics, proof, explicitStateEvents))
            .ToImmutableArray();

        return graph with { EdgeProofStatuses = edgeProofStatuses };
    }

    private static EdgeProofStatus CreateEdgeProofStatus(
        GraphEdge edge,
        SemanticIndex semantics,
        ProofLedger proof,
        HashSet<(string FromState, string EventName)> explicitStateEvents)
    {
        ImmutableHashSet<SourceSpan> matchingRowSpans = semantics.TransitionRows
            .Where(row => RowProducesEdge(row, edge, semantics, explicitStateEvents))
            .Select(row => row.RowSpan)
            .ToImmutableHashSet();

        ImmutableArray<string> unresolvedObligationSummaries = proof.Obligations
            .Where(obligation => obligation.Disposition == ProofDisposition.Unresolved
                && obligation.Context is TransitionRowContext context
                && matchingRowSpans.Contains(context.Row.RowSpan))
            .Select(obligation => obligation.Requirement.Description)
            .Where(summary => !string.IsNullOrWhiteSpace(summary))
            .Distinct(StringComparer.Ordinal)
            .ToImmutableArray();

        return new EdgeProofStatus(
            FromState: edge.FromState,
            EventName: edge.EventName,
            ToState: edge.ToState,
            IsProven: unresolvedObligationSummaries.IsEmpty,
            UnresolvedObligationSummaries: unresolvedObligationSummaries);
    }

    private static bool RowProducesEdge(
        TypedTransitionRow row,
        GraphEdge edge,
        SemanticIndex semantics,
        HashSet<(string FromState, string EventName)> explicitStateEvents)
    {
        if (!string.Equals(row.EventName, edge.EventName, StringComparison.Ordinal))
        {
            return false;
        }

        if (row.FromState is null)
        {
            if (explicitStateEvents.Contains((edge.FromState, row.EventName)))
            {
                return false;
            }
        }
        else if (!string.Equals(row.FromState, edge.FromState, StringComparison.Ordinal))
        {
            return false;
        }

        return row.Outcome == edge.Outcome
            && string.Equals(ResolveEdgeTargetState(row, edge.FromState, semantics), edge.ToState, StringComparison.Ordinal);
    }

    private static string? ResolveEdgeTargetState(TypedTransitionRow row, string fromState, SemanticIndex semantics)
    {
        if (!semantics.EventsByName.ContainsKey(row.EventName))
        {
            return null;
        }

        if (row.FromState is not null && !semantics.StatesByName.ContainsKey(row.FromState))
        {
            return null;
        }

        if (!semantics.StatesByName.ContainsKey(fromState))
        {
            return null;
        }

        return row.Outcome switch
        {
            TransitionRowOutcome.Transition when row.TargetState is not null && semantics.StatesByName.ContainsKey(row.TargetState)
                => row.TargetState,
            TransitionRowOutcome.NoTransition or TransitionRowOutcome.Reject
                => fromState,
            _ => null,
        };
    }
}
