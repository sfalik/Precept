using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Precept.Analyzers;

/// <summary>
/// PRECEPT0022 — Operators catalog inline Token reference check.
///
/// All <c>SingleTokenOp</c> entries must reference <c>Token</c> via
/// <c>Tokens.GetMeta(TokenKind.X)</c>, not inline <c>new TokenMeta(...)</c>.
/// Inline references bypass the canonical Tokens catalog and break tooling.
///
/// Scope: Only fires for <c>GetMeta(OperatorKind)</c> switches in <c>Precept.Language</c>.
/// <c>MultiTokenOp</c> arms are handled by PRECEPT0023.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PRECEPT0022OperatorsInlineToken : DiagnosticAnalyzer
{
    public const string DiagnosticId = "PRECEPT0022";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Inline Token reference in Operators catalog",
        messageFormat: "OperatorKind.{0} uses inline 'new TokenMeta(...)' instead of 'Tokens.GetMeta(TokenKind.X)' — must reference the canonical Tokens catalog",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "All Operators catalog Token references must use Tokens.GetMeta(TokenKind.X) to ensure " +
                     "consistency with the canonical Tokens catalog. Inline TokenMeta construction bypasses the catalog.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(compilationCtx =>
        {
            compilationCtx.RegisterOperationAction(
                ctx => Analyze(ctx),
                OperationKind.SwitchExpression);
        });
    }

    private static void Analyze(OperationAnalysisContext ctx)
    {
        var switchOp = (ISwitchExpressionOperation)ctx.Operation;

        if (!CatalogAnalysisHelpers.TryGetCatalogSwitchKind(
                switchOp, ctx.ContainingSymbol, out var catalogEnumTypeName))
            return;

        if (catalogEnumTypeName != "OperatorKind")
            return;

        foreach (var arm in switchOp.Arms)
        {
            var armCaseName = CatalogAnalysisHelpers.GetEnumCaseFromArm(arm);
            if (armCaseName == null) continue;

            var creation = FindObjectCreation(arm.Value);
            if (creation == null) continue;

            if (creation.Type?.Name != "SingleTokenOp")
                continue;

            var tokenArg = CatalogAnalysisHelpers.GetNamedArgument(creation, "Token");
            if (tokenArg == null) continue;

            var unwrapped = CatalogAnalysisHelpers.UnwrapConversions(tokenArg);

            // Check if it's an inline TokenMeta construction (not Tokens.GetMeta invocation)
            if (unwrapped is IObjectCreationOperation inlineCreation &&
                inlineCreation.Type?.Name == "TokenMeta")
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    inlineCreation.Syntax.GetLocation(),
                    armCaseName));
            }
        }
    }

    private static IObjectCreationOperation? FindObjectCreation(IOperation op)
    {
        var unwrapped = CatalogAnalysisHelpers.UnwrapConversions(op);
        if (unwrapped is IObjectCreationOperation creation)
            return creation;

        foreach (var child in unwrapped.ChildOperations)
        {
            var found = FindObjectCreation(child);
            if (found != null) return found;
        }

        return null;
    }
}
