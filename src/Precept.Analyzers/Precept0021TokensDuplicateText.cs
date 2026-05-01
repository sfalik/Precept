using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Precept.Analyzers;

/// <summary>
/// PRECEPT0021 — Tokens catalog duplicate Text check.
///
/// No two <c>TokenMeta</c> entries with non-null <c>Text</c> may share the same
/// text string. Duplicate text values break lexer token matching at runtime.
///
/// Scope: Only fires for <c>GetMeta(TokenKind)</c> switches in <c>Precept.Language</c>.
/// Null <c>Text</c> values are ignored (e.g., synthetic tokens like Identifier, NumberLiteral).
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PRECEPT0021TokensDuplicateText : DiagnosticAnalyzer
{
    public const string DiagnosticId = "PRECEPT0021";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Duplicate Text value in Tokens catalog",
        messageFormat: "TokenKind.{0} duplicates the Text value '{1}' already used by TokenKind.{2} — lexer token matching will fail at runtime",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The Tokens catalog Text property must be unique across all non-null entries. " +
                     "Duplicate text values break lexer token matching.");

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

        if (catalogEnumTypeName != "TokenKind")
            return;

        var textToCase = new Dictionary<string, (string armCase, Location location)>();

        foreach (var arm in switchOp.Arms)
        {
            var armCaseName = CatalogAnalysisHelpers.GetEnumCaseFromArm(arm);
            if (armCaseName == null) continue;

            var creation = FindObjectCreation(arm.Value);
            if (creation == null) continue;

            if (creation.Type?.Name != "TokenMeta")
                continue;

            var textArg = CatalogAnalysisHelpers.GetNamedArgument(creation, "Text");
            if (textArg == null) continue;

            var textValue = CatalogAnalysisHelpers.ResolveStringConstant(textArg);
            if (textValue == null)
                continue; // Null Text is valid for synthetic tokens (Identifier, NumberLiteral, etc.)

            if (textToCase.TryGetValue(textValue, out var existing))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    creation.Syntax.GetLocation(),
                    armCaseName, textValue, existing.armCase));
            }
            else
            {
                textToCase[textValue] = (armCaseName, creation.Syntax.GetLocation());
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
