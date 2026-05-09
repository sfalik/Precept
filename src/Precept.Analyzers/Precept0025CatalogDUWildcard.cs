using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Precept.Analyzers;

/// <summary>
/// PRECEPT0025 — CatalogDU Wildcard Prohibition.
///
/// A type-pattern switch over an abstract record marked with <c>[CatalogDU]</c>
/// must not contain a wildcard/discard arm (<c>_ =></c>), a <c>default:</c> case,
/// or a catch-all type arm matching the abstract base. Such arms silently absorb
/// new sealed subtypes added to the hierarchy without forcing an explicit branch —
/// the same class of bug that caused diagnostic code 116
/// (<c>UnprovedPresenceRequirement</c>) to be unreachable when <c>PresenceProofRequirement</c>
/// was added to the <c>ProofRequirement</c> DU.
///
/// Covers:
/// <list type="bullet">
/// <item>Discard pattern: <c>_ =></c></item>
/// <item>Default case: <c>default:</c></item>
/// <item>Declaration pattern over the abstract base: <c>SomeDUBase x =></c> / <c>case SomeDUBase x:</c></item>
/// <item>Type pattern over the abstract base: <c>SomeDUBase =></c></item>
/// </list>
///
/// Severity: Error. Missing an explicit arm for a new subtype is a correctness defect.
/// Suppressed only in test files (file path contains ".Tests").
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Precept0025CatalogDUWildcard : DiagnosticAnalyzer
{
    public const string DiagnosticId = "PRECEPT0025";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Wildcard/default arm in switch over [CatalogDU] type silently swallows new subtypes",
        messageFormat: "Wildcard/default arm in switch over [CatalogDU] type '{0}' silently swallows new subtypes — add an explicit arm for each subtype instead",
        category: "Precept.Pipeline",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description:
            "Type-pattern switches over abstract records marked with [CatalogDU] must not contain " +
            "wildcard/discard arms (_ =>), default cases, or catch-all type arms matching the " +
            "abstract base. These arms silently absorb new sealed subtypes added to the hierarchy, " +
            "bypassing exhaustiveness. Add an explicit arm for each concrete subtype instead.");

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
                AnalyzeSwitchExpression(ctx, switchExpression);
                return;

            case ISwitchOperation switchStatement:
                AnalyzeSwitchStatement(ctx, switchStatement);
                return;
        }
    }

    private static void AnalyzeSwitchExpression(
        OperationAnalysisContext ctx,
        ISwitchExpressionOperation switchExpression)
    {
        var catalogDUBase = CatalogAnalysisHelpers.FindCatalogDUBase(switchExpression.Value.Type);
        if (catalogDUBase == null)
            return;

        foreach (var arm in switchExpression.Arms)
        {
            if (!IsProhibitedPattern(arm.Pattern, catalogDUBase))
                continue;

            ctx.ReportDiagnostic(Diagnostic.Create(
                Rule,
                arm.Pattern.Syntax.GetLocation(),
                catalogDUBase.Name));
        }
    }

    private static void AnalyzeSwitchStatement(
        OperationAnalysisContext ctx,
        ISwitchOperation switchStatement)
    {
        var catalogDUBase = CatalogAnalysisHelpers.FindCatalogDUBase(switchStatement.Value.Type);
        if (catalogDUBase == null)
            return;

        foreach (var switchCase in switchStatement.Cases)
        {
            foreach (var clause in switchCase.Clauses)
            {
                if (!IsProhibitedPattern(clause, catalogDUBase))
                    continue;

                ctx.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    clause.Syntax.GetLocation(),
                    catalogDUBase.Name));
            }
        }
    }

    /// <summary>
    /// Returns true if the pattern is a prohibited wildcard or catch-all over the abstract base.
    /// </summary>
    private static bool IsProhibitedPattern(
        IPatternOperation pattern,
        INamedTypeSymbol catalogDUBase)
    {
        switch (pattern)
        {
            case IDiscardPatternOperation:
                return true;

            case IDeclarationPatternOperation declarationPattern:
                return declarationPattern.MatchedType != null &&
                       SymbolEqualityComparer.Default.Equals(declarationPattern.MatchedType, catalogDUBase);

            case ITypePatternOperation typePattern:
                return SymbolEqualityComparer.Default.Equals(typePattern.MatchedType, catalogDUBase);

            default:
                return false;
        }
    }

    private static bool IsProhibitedPattern(
        ICaseClauseOperation clause,
        INamedTypeSymbol catalogDUBase)
    {
        switch (clause)
        {
            case IDefaultCaseClauseOperation:
                return true;

            case IPatternCaseClauseOperation patternClause:
                return IsProhibitedPattern(patternClause.Pattern, catalogDUBase);

            default:
                return false;
        }
    }
}
