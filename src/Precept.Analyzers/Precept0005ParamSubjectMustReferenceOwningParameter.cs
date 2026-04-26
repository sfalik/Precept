using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Precept.Analyzers;

/// <summary>
/// PRECEPT0005 — ParamSubject.Parameter must be reference-equal to a ParameterMeta instance
/// that is actually used as a parameter in the same enclosing BinaryOperationMeta,
/// UnaryOperationMeta, or FunctionOverload construction.
///
/// The proof engine resolves proof obligations by matching the ParameterMeta object identity
/// to the operand slots. If someone mistakenly writes new ParamSubject(PDecimal) inside a
/// BinaryOperationMeta where the operands are PInteger, the engine silently targets a
/// parameter that has no corresponding slot — the obligation is never satisfied.
///
/// Scoped to Precept.Language to avoid false positives on unrelated code.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PRECEPT0005ParamSubjectMustReferenceOwningParameter : DiagnosticAnalyzer
{
    public const string DiagnosticId = "PRECEPT0005";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "ParamSubject.Parameter must reference an owning-overload parameter",
        messageFormat: "The ParameterMeta passed to ParamSubject must be reference-equal to a parameter " +
                       "declared in the enclosing BinaryOperationMeta, UnaryOperationMeta, or FunctionOverload — " +
                       "a mismatched reference silently targets the wrong parameter in the proof engine",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "ParamSubject.Parameter must be one of the named ParameterMeta instances used as " +
                     "Lhs/Rhs/Operand in the enclosing operation overload, or as an element of the " +
                     "Parameters array in the enclosing FunctionOverload. Inline constructions and " +
                     "references to parameters from a different overload are always wrong.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(Analyze, OperationKind.ObjectCreation);
    }

    private static void Analyze(OperationAnalysisContext ctx)
    {
        var op = (IObjectCreationOperation)ctx.Operation;

        // Scope to Precept.Language.ParamSubject only.
        var type = op.Type;
        if (type?.Name != "ParamSubject") return;
        var ns = type.ContainingNamespace;
        if (ns?.Name != "Language" || ns.ContainingNamespace?.Name != "Precept") return;

        // Get the ParameterMeta argument.
        if (op.Arguments.Length == 0) return;
        var argValue = UnwrapConversions(op.Arguments[0].Value);

        // Resolve the symbol referenced by the argument.
        var argSymbol = GetReferencedSymbol(argValue);

        // Walk up the IOperation tree to find the nearest enclosing overload construction.
        var enclosingOverload = FindEnclosingOverload(op);

        if (enclosingOverload == null)
        {
            // No enclosing overload — ParamSubject outside any overload context is always wrong.
            ctx.ReportDiagnostic(Diagnostic.Create(Rule, op.Syntax.GetLocation()));
            return;
        }

        // Collect all ParameterMeta symbols declared as parameters in the enclosing overload.
        var paramSymbols = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        CollectParameterSymbols(enclosingOverload, paramSymbols);

        // The argument must be a named symbol that appears in the owning parameter set.
        if (argSymbol == null || !paramSymbols.Contains(argSymbol))
        {
            ctx.ReportDiagnostic(Diagnostic.Create(Rule, op.Syntax.GetLocation()));
        }
    }

    private static ISymbol? GetReferencedSymbol(IOperation op) => op switch
    {
        ILocalReferenceOperation local => local.Local,
        IFieldReferenceOperation field => field.Field,
        IParameterReferenceOperation param => param.Parameter,
        _ => null,
    };

    private static IObjectCreationOperation? FindEnclosingOverload(IOperation op)
    {
        var current = op.Parent;
        while (current != null)
        {
            if (current is IObjectCreationOperation creation &&
                creation.Type?.Name is "BinaryOperationMeta" or "UnaryOperationMeta" or "FunctionOverload")
            {
                return creation;
            }
            current = current.Parent;
        }
        return null;
    }

    private static void CollectParameterSymbols(IObjectCreationOperation creation, HashSet<ISymbol> symbols)
    {
        foreach (var arg in creation.Arguments)
        {
            // Skip the ProofRequirements argument — collecting ParameterMeta symbols from inside
            // the proof requirement subtree would cause false negatives (the subject under validation
            // would be found in its own subtree and incorrectly validate itself).
            if (arg.Parameter?.Name == "ProofRequirements") continue;

            CollectParameterMetaSymbols(arg.Value, symbols);
        }
    }

    /// <summary>
    /// Recursively walks the operation subtree and collects all ParameterMeta-typed symbol
    /// references (field, local, parameter). When a ParameterMeta-typed node is found and
    /// resolves to a named symbol, collection stops at that node (no need to recurse further).
    /// When it doesn't resolve (e.g. an implicit conversion wrapping a reference), recursion
    /// continues into children so the inner reference is found.
    /// </summary>
    private static void CollectParameterMetaSymbols(IOperation op, HashSet<ISymbol> symbols)
    {
        if (op.Type?.Name == "ParameterMeta")
        {
            var sym = GetReferencedSymbol(op);
            if (sym != null)
            {
                // Named symbol reference — collect it and stop descending this subtree.
                symbols.Add(sym);
                return;
            }
            // Not a direct reference (inline construction or implicit conversion wrapping one).
            // Fall through to recurse into children so inner references are found.
        }

        foreach (var child in op.ChildOperations)
            CollectParameterMetaSymbols(child, symbols);
    }

    private static IOperation UnwrapConversions(IOperation op)
    {
        while (op is IConversionOperation conv && conv.IsImplicit)
            op = conv.Operand;
        return op;
    }
}
