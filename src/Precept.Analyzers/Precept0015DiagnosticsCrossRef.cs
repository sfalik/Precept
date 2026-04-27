using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Precept.Analyzers;

/// <summary>
/// PRECEPT0015 — Diagnostics catalog cross-reference consistency (X33, X34, S11).
///
/// X33: The <c>Code</c> (first) argument in each <c>DiagnosticMeta</c> must be a
///      <c>nameof(DiagnosticCode.X)</c> call whose target matches the switch arm's pattern.
///      Catches copy-paste errors like <c>DiagnosticCode.A => new(nameof(DiagnosticCode.B), ...)</c>.
///
/// X34: <c>RelatedCodes</c> array entries must not include the arm's own <c>DiagnosticCode</c>.
///      A diagnostic cannot be its own related diagnostic.
///
/// S11: The <c>Code</c> argument must use <c>nameof()</c> — bare string literals like
///      <c>"InputTooLarge"</c> would silently drift if the enum member is renamed.
///
/// Scope: Only fires for <c>GetMeta(DiagnosticCode)</c> switches in <c>Precept.Language</c>.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PRECEPT0015DiagnosticsCrossRef : DiagnosticAnalyzer
{
    public const string DiagnosticId_CodeMismatch = "PRECEPT0015a";
    public const string DiagnosticId_SelfRelated  = "PRECEPT0015b";
    public const string DiagnosticId_NoNameof     = "PRECEPT0015c";

    private static readonly DiagnosticDescriptor CodeMismatchRule = new(
        DiagnosticId_CodeMismatch,
        title: "DiagnosticMeta Code must match switch arm pattern",
        messageFormat: "GetMeta arm for DiagnosticCode.{0} passes nameof(DiagnosticCode.{1}) as Code — must reference '{0}'",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The Code string in each DiagnosticMeta must match the switch arm's " +
                     "DiagnosticCode enum member. A mismatch silently assigns the wrong " +
                     "identity to the diagnostic, corrupting error reporting.");

    private static readonly DiagnosticDescriptor SelfRelatedRule = new(
        DiagnosticId_SelfRelated,
        title: "DiagnosticMeta RelatedCodes must not self-reference",
        messageFormat: "DiagnosticCode.{0} lists itself in RelatedCodes — a diagnostic cannot be its own related code",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A diagnostic's RelatedCodes array lists diagnostics that are " +
                     "conceptually related. Self-references are meaningless.");

    private static readonly DiagnosticDescriptor NoNameofRule = new(
        DiagnosticId_NoNameof,
        title: "DiagnosticMeta Code should use nameof()",
        messageFormat: "GetMeta arm for DiagnosticCode.{0} uses a string literal for Code instead of nameof(DiagnosticCode.{0}) — rename-safe identity requires nameof()",
        category: "Precept.Language",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Using nameof() for the Code field keeps the string in sync with the " +
                     "enum member name across renames. A bare string literal can silently drift.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(CodeMismatchRule, SelfRelatedRule, NoNameofRule);

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

        if (catalogEnumTypeName != "DiagnosticCode")
            return;

        foreach (var arm in switchOp.Arms)
        {
            var armCaseName = CatalogAnalysisHelpers.GetEnumCaseFromArm(arm);
            if (armCaseName == null) continue; // discard arm

            var creation = FindObjectCreation(arm.Value);
            if (creation == null || creation.Arguments.Length == 0) continue;

            // ── X33 + S11: Code identity check ──────────────────────────────
            CheckCodeIdentity(ctx, creation, armCaseName);

            // ── X34: RelatedCodes self-reference check ──────────────────────
            CheckRelatedCodesSelfRef(ctx, creation, armCaseName);
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

        // Check if it's a nameof() call.
        if (unwrapped is INameOfOperation nameofOp)
        {
            // X33: The nameof target must reference the same enum member as the arm.
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
            // S11: Not using nameof() — warn.
            ctx.ReportDiagnostic(Diagnostic.Create(
                NoNameofRule,
                codeArg.Syntax.GetLocation(),
                armCaseName));
        }
    }

    private static void CheckRelatedCodesSelfRef(
        OperationAnalysisContext ctx,
        IObjectCreationOperation creation,
        string armCaseName)
    {
        var relatedArg = CatalogAnalysisHelpers.GetNamedArgument(creation, "RelatedCodes");
        if (relatedArg == null)
        {
            // Also check the initializer syntax for named arguments passed via object initializer.
            relatedArg = CatalogAnalysisHelpers.GetInitializerAssignment(creation, "RelatedCodes");
        }
        if (relatedArg == null) return;

        foreach (var element in CatalogAnalysisHelpers.EnumerateCollectionElements(relatedArg))
        {
            var fieldName = CatalogAnalysisHelpers.ResolveEnumFieldName(element);
            if (fieldName != null && fieldName == armCaseName)
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    SelfRelatedRule,
                    element.Syntax.GetLocation(),
                    armCaseName));
            }
        }
    }

    /// <summary>
    /// Resolves the enum field name from a nameof() operation target.
    /// E.g., <c>nameof(DiagnosticCode.InputTooLarge)</c> → <c>"InputTooLarge"</c>.
    /// </summary>
    private static string? ResolveNameofTarget(INameOfOperation nameofOp)
    {
        // The argument to nameof is typically a member access like DiagnosticCode.X.
        // Roslyn models it as an IFieldReferenceOperation (or IMemberReferenceOperation).
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
