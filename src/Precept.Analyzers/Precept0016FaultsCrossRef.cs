using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Precept.Analyzers;

/// <summary>
/// PRECEPT0016 — Faults catalog cross-reference consistency (X35, S12).
///
/// X35: The <c>Code</c> (first) argument in each <c>FaultMeta</c> must be a
///      <c>nameof(FaultCode.X)</c> call whose target matches the switch arm's pattern.
///      Catches copy-paste errors like <c>FaultCode.A => new(nameof(FaultCode.B), ...)</c>.
///
/// S12: The <c>Code</c> argument must use <c>nameof()</c> — bare string literals
///      silently drift if the enum member is renamed.
///
/// Scope: Only fires for <c>GetMeta(FaultCode)</c> switches in <c>Precept.Language</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PRECEPT0016FaultsCrossRef : DiagnosticAnalyzer
{
    public const string DiagnosticId_CodeMismatch = "PRECEPT0016a";
    public const string DiagnosticId_NoNameof     = "PRECEPT0016b";

    private static readonly DiagnosticDescriptor CodeMismatchRule = new(
        DiagnosticId_CodeMismatch,
        title: "FaultMeta Code must match switch arm pattern",
        messageFormat: "GetMeta arm for FaultCode.{0} passes nameof(FaultCode.{1}) as Code — must reference '{0}'",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The Code string in each FaultMeta must match the switch arm's " +
                     "FaultCode enum member. A mismatch silently assigns the wrong " +
                     "identity to the fault record.");

    private static readonly DiagnosticDescriptor NoNameofRule = new(
        DiagnosticId_NoNameof,
        title: "FaultMeta Code should use nameof()",
        messageFormat: "GetMeta arm for FaultCode.{0} uses a string literal for Code instead of nameof(FaultCode.{0}) — rename-safe identity requires nameof()",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Using nameof() for the Code field keeps the string in sync with the " +
                     "enum member name across renames. A bare string literal can silently drift.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(CodeMismatchRule, NoNameofRule);

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

        if (catalogEnumTypeName != "FaultCode")
            return;

        foreach (var arm in switchOp.Arms)
        {
            var armCaseName = CatalogAnalysisHelpers.GetEnumCaseFromArm(arm);
            if (armCaseName == null) continue;

            var creation = FindObjectCreation(arm.Value);
            if (creation == null || creation.Arguments.Length == 0) continue;

            CheckCodeIdentity(ctx, creation, armCaseName);
        }
    }

    private static void CheckCodeIdentity(
        OperationAnalysisContext ctx,
        IObjectCreationOperation creation,
        string armCaseName)
    {
        var codeArg = CatalogAnalysisHelpers.GetNamedArgument(creation, "Code");
        if (codeArg == null) return;

        var unwrapped = CatalogAnalysisHelpers.UnwrapConversions(codeArg);

        if (unwrapped is INameOfOperation nameofOp)
        {
            var targetName = ResolveNameofTarget(nameofOp);
            if (targetName != null && targetName != armCaseName)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    CodeMismatchRule,
                    codeArg.Syntax.GetLocation(),
                    armCaseName,
                    targetName));
            }
        }
        else
        {
            ctx.ReportDiagnostic(Diagnostic.Create(
                NoNameofRule,
                codeArg.Syntax.GetLocation(),
                armCaseName));
        }
    }

    private static string? ResolveNameofTarget(INameOfOperation nameofOp)
    {
        foreach (var child in nameofOp.ChildOperations)
        {
            var unwrapped = CatalogAnalysisHelpers.UnwrapConversions(child);
            if (unwrapped is IFieldReferenceOperation fieldRef)
                return fieldRef.Field.Name;
        }
        return null;
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
