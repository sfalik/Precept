using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Precept.Analyzers;

/// <summary>
/// PRECEPT0026 — CatalogDU switch arm completeness.
///
/// Every switch expression or switch statement over a type in a <c>[CatalogDU]</c>
/// hierarchy must contain an explicit type-pattern arm for each currently known
/// sealed subtype. PRECEPT0025 prevents wildcard/default catch-alls; PRECEPT0026
/// ensures the explicit subtype arms are actually present.
///
/// Severity: Error. Missing a subtype arm means the switch is not exhaustive over
/// the current DU hierarchy.
/// Suppressed only in test files (file path contains ".Tests").
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Precept0026CatalogDUCompleteness : DiagnosticAnalyzer
{
    public const string DiagnosticId = "PRECEPT0026";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "CatalogDU switch missing subtype arm",
        messageFormat: "Switch over [CatalogDU] type '{0}' is missing arm(s) for subtype(s): {1}",
        category: "Precept.Pipeline",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description:
            "Every switch expression and switch statement over a [CatalogDU] hierarchy must " +
            "include an explicit type-pattern arm for each sealed subtype currently present in " +
            "the compilation. Missing subtype arms make the switch incomplete.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(Analyze, OperationKind.SwitchExpression, OperationKind.Switch);
    }

    private static void Analyze(OperationAnalysisContext ctx)
    {
        if (CatalogAnalysisHelpers.IsTestFile(ctx.Operation))
            return;

        switch (ctx.Operation)
        {
            case ISwitchExpressionOperation switchExpression:
                AnalyzeSwitch(ctx, switchExpression.Value.Type, switchExpression.Syntax.GetLocation(), switchExpression.Arms);
                return;

            case ISwitchOperation switchStatement:
                AnalyzeSwitch(ctx, switchStatement.Value.Type, switchStatement.Syntax.GetLocation(), switchStatement.Cases);
                return;
        }
    }

    private static void AnalyzeSwitch(
        OperationAnalysisContext ctx,
        ITypeSymbol? switchValueType,
        Location switchLocation,
        ImmutableArray<ISwitchExpressionArmOperation> arms)
    {
        var catalogDUBase = CatalogAnalysisHelpers.FindCatalogDUBase(switchValueType);
        if (catalogDUBase == null)
            return;

        var handledSubtypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var arm in arms)
        {
            AddHandledSubtype(handledSubtypes, catalogDUBase, arm.Pattern);
        }

        ReportMissingSubtypes(ctx, catalogDUBase, switchLocation, handledSubtypes);
    }

    private static void AnalyzeSwitch(
        OperationAnalysisContext ctx,
        ITypeSymbol? switchValueType,
        Location switchLocation,
        ImmutableArray<ISwitchCaseOperation> cases)
    {
        var catalogDUBase = CatalogAnalysisHelpers.FindCatalogDUBase(switchValueType);
        if (catalogDUBase == null)
            return;

        var handledSubtypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var switchCase in cases)
        {
            foreach (var clause in switchCase.Clauses)
            {
                if (clause is IPatternCaseClauseOperation patternClause)
                    AddHandledSubtype(handledSubtypes, catalogDUBase, patternClause.Pattern);
            }
        }

        ReportMissingSubtypes(ctx, catalogDUBase, switchLocation, handledSubtypes);
    }

    private static void AddHandledSubtype(
        ISet<INamedTypeSymbol> handledSubtypes,
        INamedTypeSymbol catalogDUBase,
        IPatternOperation pattern)
    {
        if (GetMatchedType(pattern) is not { IsSealed: true } matchedType)
            return;

        if (!CatalogAnalysisHelpers.InheritsFrom(matchedType, catalogDUBase))
            return;

        handledSubtypes.Add(matchedType);
    }

    private static INamedTypeSymbol? GetMatchedType(IPatternOperation pattern) =>
        pattern switch
        {
            IDeclarationPatternOperation declarationPattern => declarationPattern.MatchedType as INamedTypeSymbol,
            ITypePatternOperation typePattern => typePattern.MatchedType as INamedTypeSymbol,
            _ => null,
        };

    private static void ReportMissingSubtypes(
        OperationAnalysisContext ctx,
        INamedTypeSymbol catalogDUBase,
        Location switchLocation,
        ISet<INamedTypeSymbol> handledSubtypes)
    {
        foreach (var subtype in CatalogAnalysisHelpers.FindSealedCatalogDUSubtypes(ctx.Compilation, catalogDUBase))
        {
            if (handledSubtypes.Contains(subtype))
                continue;

            ctx.ReportDiagnostic(Diagnostic.Create(
                Rule,
                switchLocation,
                catalogDUBase.Name,
                subtype.Name));
        }
    }
}
