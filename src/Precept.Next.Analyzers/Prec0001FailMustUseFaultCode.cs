using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Precept.Analyzers;

/// <summary>
/// PREC0001 — Every call to Fail() must pass a FaultCode as its first argument.
/// This prevents unclassified evaluator error paths that bypass the FaultCode chain.
/// </summary>
/// <remarks>
/// The check is scoped by method name only — any method named <c>Fail</c> anywhere in the
/// codebase must supply a <c>Precept.Runtime.FaultCode</c> first argument. This is intentional:
/// all evaluator subclasses (in any namespace) must classify their failure paths through the
/// same FaultCode chain. Narrowing to a specific containing type would allow subclasses in
/// other assemblies to silently bypass classification. If a third-party method named Fail is
/// ever needed without a FaultCode, suppress PREC0001 at that call site with a justification.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Prec0001FailMustUseFaultCode : DiagnosticAnalyzer
{
    public const string DiagnosticId = "PREC0001";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "Fail() must use FaultCode",
        messageFormat: "Fail() must be called with a FaultCode argument — use Fail(FaultCode.X) to classify the failure",
        category: "Precept.Runtime",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Every evaluator failure path must be classified via FaultCode so the failure can be " +
                     "traced to its corresponding compile-time diagnostic via [StaticallyPreventable].");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationAction(Analyze, OperationKind.Invocation);
    }

    private static void Analyze(OperationAnalysisContext ctx)
    {
        var op = (IInvocationOperation)ctx.Operation;

        if (op.TargetMethod.Name != "Fail")
            return;

        // The first argument must be of type Precept.Runtime.FaultCode.
        // An empty Fail() or Fail(string) bypasses the classification chain.
        // Namespace is checked to avoid false negatives from third-party types named FaultCode.
        var firstArgType = op.Arguments.Length > 0 ? op.Arguments[0].Value.Type : null;
        var isFaultCode = firstArgType?.Name == "FaultCode" &&
                          firstArgType.ContainingNamespace?.ToDisplayString() == "Precept.Runtime";

        if (!isFaultCode)
        {
            ctx.ReportDiagnostic(Diagnostic.Create(Rule, op.Syntax.GetLocation()));
        }
    }
}
