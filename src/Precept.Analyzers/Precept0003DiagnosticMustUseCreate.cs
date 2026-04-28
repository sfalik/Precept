using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Precept.Analyzers;

/// <summary>
/// PRECEPT0003 — Diagnostic must be constructed via Diagnostics.Create(), not with new Diagnostic(...).
/// Direct construction bypasses the nameof()-derived Code string in the exhaustive switch.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PRECEPT0003DiagnosticMustUseCreate : DiagnosticAnalyzer
{
    public const string DiagnosticId = "PRECEPT0003";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Diagnostic must be constructed via Diagnostics.Create()",
        messageFormat: "Use Diagnostics.Create(DiagnosticCode, range, args) instead of constructing Diagnostic directly — " +
                       "direct construction bypasses the nameof()-derived Code string in the Diagnostics exhaustive switch",
        category: "Precept.Pipeline",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Diagnostic.Code must be derived from a DiagnosticCode enum member via nameof() in the Diagnostics exhaustive switch. " +
                     "Constructing Diagnostic directly allows arbitrary string codes that escape the registry.");

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

        var type = op.Type;
        if (type?.Name != "Diagnostic")
            return;

        // Scope to Precept.Pipeline.Diagnostic only — avoid false positives on
        // Microsoft.CodeAnalysis.Diagnostic or other types named Diagnostic.
        var ns = type.ContainingNamespace;
        if (ns?.Name != "Pipeline" || ns.ContainingNamespace?.Name != "Precept")
            return;

        // Exempt Diagnostics.Create() — the factory is the one allowed construction site.
        // All other callers must go through the factory to preserve nameof()-derived Code strings.
        var containingMethod = ctx.ContainingSymbol as IMethodSymbol;
        if (containingMethod?.Name == "Create" &&
            containingMethod.ContainingType?.Name == "Diagnostics" &&
            containingMethod.ContainingType.ContainingNamespace?.Name == "Pipeline" &&
            containingMethod.ContainingType.ContainingNamespace.ContainingNamespace?.Name == "Precept")
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(Rule, op.Syntax.GetLocation()));
    }
}
