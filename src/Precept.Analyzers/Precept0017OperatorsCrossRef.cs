using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Precept.Analyzers;

/// <summary>
/// PRECEPT0017 — Operators catalog cross-reference consistency (invariant X09).
///
/// In the <c>Operators.GetMeta</c> switch, the first constructor argument (<c>Kind</c>) of each
/// <c>OperatorMeta</c> must match the switch arm's pattern constant. This prevents copy-paste
/// errors where the Kind field silently points to a different operator than the arm handles,
/// which would corrupt the <c>ByToken</c> frozen-dictionary index and break runtime lookups.
///
/// The check applies to all catalog GetMeta switches (not just Operators) because the
/// Kind-identity invariant is universal: every catalog record's <c>Kind</c> field must equal
/// the switch arm that produces it.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PRECEPT0017OperatorsCrossRef : DiagnosticAnalyzer
{
    public const string DiagnosticId = "PRECEPT0017";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "GetMeta Kind argument must match switch arm pattern",
        messageFormat: "GetMeta arm for {0}.{1} passes {2} as the Kind argument — must be '{1}' or the switch value",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The first constructor argument in a catalog GetMeta switch arm must be the switch " +
                     "discriminant variable (e.g. 'kind') or the explicit enum constant matching the arm's " +
                     "pattern. A mismatch silently assigns the wrong identity to the record, corrupting " +
                     "downstream dictionary indexes and runtime lookups.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(Analyze, OperationKind.SwitchExpression);
    }

    private static void Analyze(OperationAnalysisContext ctx)
    {
        var switchOp = (ISwitchExpressionOperation)ctx.Operation;

        if (!CatalogAnalysisHelpers.TryGetCatalogSwitchKind(
                switchOp, ctx.ContainingSymbol, out var catalogEnumTypeName))
            return;

        // Resolve the switch value symbol (typically the 'kind' parameter).
        var switchValueSymbol = GetReferencedSymbol(
            CatalogAnalysisHelpers.UnwrapConversions(switchOp.Value));

        foreach (var arm in switchOp.Arms)
        {
            var armCaseName = CatalogAnalysisHelpers.GetEnumCaseFromArm(arm);
            if (armCaseName == null) continue; // discard arm — skip

            // Find the object creation in this arm's body.
            var creation = FindObjectCreation(arm.Value);
            if (creation == null || creation.Arguments.Length == 0) continue;

            // The first argument should be the Kind — but only if the parameter type
            // matches the catalog enum. Some catalogs (e.g. Faults) use strings as
            // the first parameter, not the enum Kind.
            var firstArgOp = creation.Arguments[0];
            if (firstArgOp.Parameter?.Type?.Name != catalogEnumTypeName)
                continue;

            var firstArg = CatalogAnalysisHelpers.UnwrapConversions(firstArgOp.Value);

            // Check 1: If it references the switch value parameter, it's always correct.
            var argSymbol = GetReferencedSymbol(firstArg);
            if (argSymbol != null && switchValueSymbol != null &&
                SymbolEqualityComparer.Default.Equals(argSymbol, switchValueSymbol))
                continue;

            // Check 2: If it's an explicit enum constant, it must match the arm case.
            var argEnumName = CatalogAnalysisHelpers.ResolveEnumFieldName(firstArg);
            if (argEnumName != null && argEnumName == armCaseName)
                continue;

            // Neither check passed — report diagnostic.
            var actualName = argEnumName ?? argSymbol?.Name ?? "unknown";
            ctx.ReportDiagnostic(Diagnostic.Create(
                Rule,
                firstArgOp.Syntax.GetLocation(),
                catalogEnumTypeName,
                armCaseName,
                actualName));
        }
    }

    private static ISymbol? GetReferencedSymbol(IOperation op) => op switch
    {
        IParameterReferenceOperation param => param.Parameter,
        ILocalReferenceOperation local => local.Local,
        IFieldReferenceOperation field => field.Field,
        _ => null,
    };

    private static IObjectCreationOperation? FindObjectCreation(IOperation op)
    {
        // The arm value might be the creation directly, or wrapped in a conversion.
        var unwrapped = CatalogAnalysisHelpers.UnwrapConversions(op);
        if (unwrapped is IObjectCreationOperation creation)
            return creation;

        // Walk children for cases like conditional expressions or null-coalescing.
        foreach (var child in unwrapped.ChildOperations)
        {
            var found = FindObjectCreation(child);
            if (found != null) return found;
        }

        return null;
    }
}
