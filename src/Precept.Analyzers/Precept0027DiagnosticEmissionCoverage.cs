using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Precept.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Precept0027DiagnosticEmissionCoverage : DiagnosticAnalyzer
{
    public const string DiagnosticId_MissingEmission = "PRECEPT0027";
    public const string DiagnosticId_StaleAllowList  = "PRECEPT0029";

    private static readonly DiagnosticDescriptor MissingEmissionRule = new(
        DiagnosticId_MissingEmission,
        title: "DiagnosticCode member has no emission site",
        messageFormat: "DiagnosticCode.{0} has no emission site and is not in the Gate 1 allow-list",
        category: "Precept.Diagnostics",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description:
            "Every DiagnosticCode member must have at least one emission site in the pipeline " +
            "(Diagnostics.Create, CIDiagnosticCode assignment, or ProofEngine dispatch), " +
            "or be listed in the Gate 1 allow-list with a tracking comment.",
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    private static readonly DiagnosticDescriptor StaleAllowListRule = new(
        DiagnosticId_StaleAllowList,
        title: "Gate 1 allow-list entry is stale",
        messageFormat: "DiagnosticCode.{0} is in the Gate 1 allow-list but now has an emission site - remove it from the allow-list",
        category: "Precept.Diagnostics",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "A Gate 1 allow-list entry has become stale because the code now has an emission " +
            "site. This typically means a gap-closure slice shipped but forgot to remove " +
            "the allow-list entry.",
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(MissingEmissionRule, StaleAllowListRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private static void AnalyzeCompilation(CompilationAnalysisContext ctx)
    {
        var result = DiagnosticCoverageScanner.Scan(ctx.Compilation);

        foreach (var code in result.AllCodes)
        {
            if (result.EmittedCodes.Contains(code))
                continue;
            if (DiagnosticCoverageAllowLists.Gate1AllowList.Contains(code))
                continue;

            ctx.ReportDiagnostic(Diagnostic.Create(
                MissingEmissionRule,
                Location.None,
                code));
        }

        foreach (var code in DiagnosticCoverageAllowLists.Gate1AllowList)
        {
            if (result.EmittedCodes.Contains(code) && result.AllCodes.Contains(code))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    StaleAllowListRule,
                    Location.None,
                    code));
            }
        }
    }
}
