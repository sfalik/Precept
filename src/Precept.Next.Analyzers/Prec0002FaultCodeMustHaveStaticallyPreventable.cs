using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Precept.Analyzers;

/// <summary>
/// PREC0002 — Every FaultCode enum member must carry [StaticallyPreventable(DiagnosticCode.X)].
/// This ensures every runtime fault maps to a compile-time diagnostic that prevents it.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Prec0002FaultCodeMustHaveStaticallyPreventable : DiagnosticAnalyzer
{
    public const string DiagnosticId = "PREC0002";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        title: "FaultCode member must have [StaticallyPreventable]",
        messageFormat: "FaultCode.{0} is missing [StaticallyPreventable] — every fault must reference the DiagnosticCode that prevents it at compile time",
        category: "Precept.Runtime",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "Every FaultCode member must carry [StaticallyPreventable(DiagnosticCode.X)] to " +
                     "assert that the corresponding compile-time diagnostic makes this fault unreachable at runtime.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(Analyze, SymbolKind.Field);
    }

    private static void Analyze(SymbolAnalysisContext ctx)
    {
        var field = (IFieldSymbol)ctx.Symbol;

        // Only check members of an enum named FaultCode.
        if (field.ContainingType.TypeKind != TypeKind.Enum ||
            field.ContainingType.Name != "FaultCode")
            return;

        var hasAttribute = field.GetAttributes().Any(a =>
            a.AttributeClass?.Name == "StaticallyPreventableAttribute" ||
            a.AttributeClass?.Name == "StaticallyPreventable");

        if (!hasAttribute)
        {
            var location = field.Locations.Length > 0 ? field.Locations[0] : Location.None;
            ctx.ReportDiagnostic(Diagnostic.Create(Rule, location, field.Name));
        }
    }
}
