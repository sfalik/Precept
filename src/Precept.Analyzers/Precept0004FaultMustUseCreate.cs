using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Precept.Analyzers;

/// <summary>
/// PRECEPT0004 — Fault must be constructed via Faults.Create(), not with new Fault(...).
/// Direct construction bypasses the nameof()-derived CodeName string in the exhaustive switch.
/// Fault is a public output type (MCP, preview inspector, external consumers) — the same
/// bypass pressure that justifies PRECEPT0003 on Diagnostic applies here.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class PRECEPT0004FaultMustUseCreate : DiagnosticAnalyzer
{
    public const string DiagnosticId = "PRECEPT0004";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Fault must be constructed via Faults.Create()",
        messageFormat: "Use Faults.Create(FaultCode, args) instead of constructing Fault directly — " +
                       "direct construction bypasses the nameof()-derived CodeName string in the Faults exhaustive switch",
        category: "Precept.Runtime",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Fault.CodeName must be derived from a FaultCode enum member via nameof() in the Faults exhaustive switch. " +
                     "Constructing Fault directly allows arbitrary string codes that escape the registry.");

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
        if (type?.Name != "Fault")
            return;

        // Scope to Precept.Runtime.Fault only.
        var ns = type.ContainingNamespace;
        if (ns?.Name != "Runtime" || ns.ContainingNamespace?.Name != "Precept")
            return;

        // Exempt Faults.Create() — the factory is the one allowed construction site.
        // All other callers must go through the factory to preserve nameof()-derived CodeName strings.
        var containingMethod = ctx.ContainingSymbol as IMethodSymbol;
        if (containingMethod?.Name == "Create" &&
            containingMethod.ContainingType?.Name == "Faults" &&
            containingMethod.ContainingType.ContainingNamespace?.Name == "Runtime" &&
            containingMethod.ContainingType.ContainingNamespace.ContainingNamespace?.Name == "Precept")
            return;

        ctx.ReportDiagnostic(Diagnostic.Create(Rule, op.Syntax.GetLocation()));
    }
}
