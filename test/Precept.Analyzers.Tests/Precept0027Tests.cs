using FluentAssertions;
using Xunit;

namespace Precept.Analyzers.Tests;

public class PRECEPT0027Tests
{
    // ════════════════════════════════════════════════════════════════════════════
    //  Shared scaffolding
    // ════════════════════════════════════════════════════════════════════════════

    private const string DiagStubs = @"
namespace Precept.Language
{
    public enum DiagnosticCode { Alpha, Beta, Gamma }

    public static class Diagnostics
    {
        public static object Create(DiagnosticCode code, object span, params object[] args) => null;
        public static object GetMeta(DiagnosticCode code) => null;
    }
}
";

    // ════════════════════════════════════════════════════════════════════════════
    //  Positive: all codes emitted — no diagnostics
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AllCodesEmitted_NoDiagnostic()
    {
        var source = DiagStubs + @"
namespace Pipeline
{
    using Precept.Language;

    public class TypeChecker
    {
        public void Check()
        {
            Diagnostics.Create(DiagnosticCode.Alpha, null);
            Diagnostics.Create(DiagnosticCode.Beta, null);
            Diagnostics.Create(DiagnosticCode.Gamma, null);
        }
    }
}
";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0027DiagnosticEmissionCoverage>(source);
        diagnostics.Where(d => d.Id == Precept0027DiagnosticEmissionCoverage.DiagnosticId_MissingEmission)
            .Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Negative: code not emitted and not allow-listed — reports error
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CodeNotEmitted_ReportsError()
    {
        // Only Alpha and Beta are emitted; Gamma has no emission site.
        var source = DiagStubs + @"
namespace Pipeline
{
    using Precept.Language;

    public class TypeChecker
    {
        public void Check()
        {
            Diagnostics.Create(DiagnosticCode.Alpha, null);
            Diagnostics.Create(DiagnosticCode.Beta, null);
        }
    }
}
";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0027DiagnosticEmissionCoverage>(source);
        var missing = diagnostics.Where(d => d.Id == Precept0027DiagnosticEmissionCoverage.DiagnosticId_MissingEmission).ToList();
        missing.Should().ContainSingle();
        missing[0].GetMessage().Should().Contain("Gamma");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  GetMeta references are NOT emission — code remains unemitted
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetMetaReference_NotCountedAsEmission()
    {
        var source = DiagStubs + @"
namespace Pipeline
{
    using Precept.Language;

    public class Catalog
    {
        public void Read()
        {
            Diagnostics.Create(DiagnosticCode.Alpha, null);
            Diagnostics.Create(DiagnosticCode.Beta, null);
            // Gamma only appears in GetMeta — not an emission
            var meta = Diagnostics.GetMeta(DiagnosticCode.Gamma);
        }
    }
}
";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0027DiagnosticEmissionCoverage>(source);
        var missing = diagnostics.Where(d => d.Id == Precept0027DiagnosticEmissionCoverage.DiagnosticId_MissingEmission).ToList();
        missing.Should().ContainSingle();
        missing[0].GetMessage().Should().Contain("Gamma");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  CIDiagnosticCode assignment counts as emission
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CIDiagnosticCodeAssignment_CountsAsEmission()
    {
        var source = DiagStubs + @"
namespace Precept.Language
{
    public class OperationMeta
    {
        public DiagnosticCode? CIDiagnosticCode { get; init; }
    }
}

namespace Pipeline
{
    using Precept.Language;

    public class Operations
    {
        public void Setup()
        {
            Diagnostics.Create(DiagnosticCode.Alpha, null);
            Diagnostics.Create(DiagnosticCode.Beta, null);
            var meta = new OperationMeta { CIDiagnosticCode = DiagnosticCode.Gamma };
        }
    }
}
";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0027DiagnosticEmissionCoverage>(source);
        diagnostics.Where(d => d.Id == Precept0027DiagnosticEmissionCoverage.DiagnosticId_MissingEmission)
            .Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Stale allow-list entry — code is now emitted
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task StaleAllowListEntry_ReportsWarning()
    {
        // Use a code that's on the real Gate 1 allow-list.
        // If the source emits it, the stale check should flag it.
        var source = @"
namespace Precept.Language
{
    public enum DiagnosticCode { OutOfRange }

    public static class Diagnostics
    {
        public static object Create(DiagnosticCode code, object span, params object[] args) => null;
    }
}

namespace Pipeline
{
    using Precept.Language;

    public class TypeChecker
    {
        public void Check()
        {
            Diagnostics.Create(DiagnosticCode.OutOfRange, null);
        }
    }
}
";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0027DiagnosticEmissionCoverage>(source);
        var stale = diagnostics.Where(d => d.Id == Precept0027DiagnosticEmissionCoverage.DiagnosticId_StaleAllowList).ToList();
        stale.Should().ContainSingle();
        stale[0].GetMessage().Should().Contain("OutOfRange");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Allow-listed code — no error even without emission
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AllowListedCode_NoError()
    {
        // Define only an allow-listed code with no emission — should not report.
        var source = @"
namespace Precept.Language
{
    public enum DiagnosticCode { OutOfRange }

    public static class Diagnostics
    {
        public static object Create(DiagnosticCode code, object span, params object[] args) => null;
    }
}

namespace Pipeline
{
    using Precept.Language;

    public class Stub
    {
        // No emission of OutOfRange anywhere.
    }
}
";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0027DiagnosticEmissionCoverage>(source);
        diagnostics.Where(d => d.Id == Precept0027DiagnosticEmissionCoverage.DiagnosticId_MissingEmission)
            .Should().BeEmpty();
    }
}
