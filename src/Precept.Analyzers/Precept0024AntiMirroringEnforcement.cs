using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Precept.Analyzers;

/// <summary>
/// PRECEPT0024 — Anti-mirroring enforcement for Typed* record <c>.Syntax</c> back-pointers.
///
/// The <c>.Syntax</c> property on <c>SemanticIndex</c> typed records (<c>TypedField</c>,
/// <c>TypedState</c>, etc.) is a debugging/diagnostics aid only. Downstream pipeline stages
/// (GraphAnalyzer, ProofEngine, Builder) must consume typed semantic data — never parse-tree
/// back-pointers. Only the <c>TypeChecker</c> class (which creates the records) is permitted
/// to access <c>.Syntax</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Precept0024AntiMirroringEnforcement : DiagnosticAnalyzer
{
    public const string DiagnosticId = "PRECEPT0024";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Typed record .Syntax back-pointer accessed outside TypeChecker",
        messageFormat: "{0}.Syntax must not be accessed outside TypeChecker — use typed fields instead",
        category: "Precept.Pipeline",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description:
            "The .Syntax property on Typed* records is a ParsedConstruct back-pointer for " +
            "debugging only. GraphAnalyzer, ProofEngine, Builder, and all other downstream " +
            "consumers must use the typed semantic fields — never the parse-tree back-pointer.");

    /// <summary>
    /// The set of Typed* record names in <c>Precept.Pipeline</c> that carry a
    /// <c>ParsedConstruct Syntax</c> property. Derived from <c>SemanticIndex.cs</c>.
    /// </summary>
    private static readonly ImmutableHashSet<string> GuardedTypeNames = ImmutableHashSet.Create(
        "TypedField",
        "TypedState",
        "TypedEvent",
        "TypedTransitionRow",
        "TypedRule",
        "TypedEnsure",
        "TypedAccessMode",
        "TypedStateHook",
        "TypedEventHandler",
        "TypedEditDeclaration");

    private const string PipelineNamespace = "Precept.Pipeline";
    private const string TypeCheckerTypeName = "TypeChecker";

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(Analyze, OperationKind.PropertyReference);
    }

    private static void Analyze(OperationAnalysisContext ctx)
    {
        var propertyRef = (IPropertyReferenceOperation)ctx.Operation;

        // Only guard accesses to ".Syntax".
        if (propertyRef.Property.Name != "Syntax")
            return;

        // Resolve the receiver's type to check if it's one of the guarded Typed* records.
        var receiverType = propertyRef.Instance?.Type;
        if (receiverType == null)
            return;

        if (!IsGuardedType(receiverType))
            return;

        // Check if we're inside the TypeChecker class — if so, access is allowed.
        if (IsInsideTypeChecker(ctx.ContainingSymbol))
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(
            Rule,
            propertyRef.Syntax.GetLocation(),
            receiverType.Name));
    }

    private static bool IsGuardedType(ITypeSymbol type)
    {
        // Check the type name is in the guarded set.
        if (!GuardedTypeNames.Contains(type.Name))
            return false;

        // Verify the type is in the Precept.Pipeline namespace.
        return type.ContainingNamespace != null &&
               type.ContainingNamespace.ToDisplayString() == PipelineNamespace;
    }

    private static bool IsInsideTypeChecker(ISymbol? symbol)
    {
        // Walk up the containing type chain to find if any enclosing type is TypeChecker.
        while (symbol != null)
        {
            if (symbol is INamedTypeSymbol namedType &&
                namedType.Name == TypeCheckerTypeName &&
                namedType.ContainingNamespace?.ToDisplayString() == PipelineNamespace)
            {
                return true;
            }

            symbol = symbol.ContainingSymbol;
        }

        return false;
    }
}
