using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Precept.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class Precept0028DiagnosticTestCoverage : DiagnosticAnalyzer
{
    public const string DiagnosticId_MissingTest   = "PRECEPT0028";
    public const string DiagnosticId_StaleAllowList = "PRECEPT0030";

    private static readonly DiagnosticDescriptor MissingTestRule = new(
        DiagnosticId_MissingTest,
        title: "Emitted DiagnosticCode has no test reference",
        messageFormat: "DiagnosticCode.{0} is emitted but has no test reference and is not in the Gate 2 allow-list",
        category: "Precept.Diagnostics",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description:
            "Every emitted diagnostic code must be referenced in at least one test file " +
            "(DiagnosticCode.MemberName in test source). Codes on the Gate 1 allow-list " +
            "are exempt because they have no emission to test.",
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);

    private static readonly DiagnosticDescriptor StaleAllowListRule = new(
        DiagnosticId_StaleAllowList,
        title: "Gate 2 allow-list entry is stale",
        messageFormat: "DiagnosticCode.{0} is in the Gate 2 allow-list but now has a test reference - remove it from the allow-list",
        category: "Precept.Diagnostics",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description:
            "A Gate 2 allow-list entry has become stale because test coverage now exists. " +
            "Remove the entry to keep the allow-list representing only genuine exceptions.",
        customTags: [WellKnownDiagnosticTags.CompilationEnd]);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(MissingTestRule, StaleAllowListRule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationAction(AnalyzeCompilation);
    }

    private static void AnalyzeCompilation(CompilationAnalysisContext ctx)
    {
        var result = DiagnosticCoverageScanner.Scan(ctx.Compilation);

        foreach (var code in result.EmittedCodes)
        {
            if (DiagnosticCoverageAllowLists.Gate1AllowList.Contains(code))
                continue;
            if (result.TestReferencedCodes.Contains(code))
                continue;
            if (DiagnosticCoverageAllowLists.Gate2AllowList.Contains(code))
                continue;

            ctx.ReportDiagnostic(Diagnostic.Create(
                MissingTestRule,
                Location.None,
                code));
        }

        foreach (var code in DiagnosticCoverageAllowLists.Gate2AllowList)
        {
            if (result.TestReferencedCodes.Contains(code) && result.EmittedCodes.Contains(code))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(
                    StaleAllowListRule,
                    Location.None,
                    code));
            }
        }
    }
}
