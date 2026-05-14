using FluentAssertions;
using Xunit;

namespace Precept.Analyzers.Tests;

public class PRECEPT0028Tests
{
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

    [Fact]
    public async Task EmittedCodesWithTestReferences_NoDiagnostic()
    {
        var pipelineSource = DiagStubs + @"
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
        var testSource = @"
namespace Precept.Language.Tests
{
    using Precept.Language;

    public class DiagTests
    {
        public void TestAlpha() { var x = DiagnosticCode.Alpha; }
        public void TestBeta() { var x = DiagnosticCode.Beta; }
        public void TestGamma() { var x = DiagnosticCode.Gamma; }
    }
}
";
        var diagnostics = await AnalyzerTestHelper.AnalyzeWithFilePathsAsync<Precept0028DiagnosticTestCoverage>(
            (pipelineSource, "src/Pipeline/TypeChecker.cs"),
            (testSource, "test/Precept.Tests/DiagTests.cs"));
        diagnostics.Where(d => d.Id == Precept0028DiagnosticTestCoverage.DiagnosticId_MissingTest)
            .Should().BeEmpty();
    }

    [Fact]
    public async Task EmittedCodeMissingTestReference_ReportsError()
    {
        var pipelineSource = DiagStubs + @"
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
        var testSource = @"
namespace Precept.Language.Tests
{
    using Precept.Language;

    public class DiagTests
    {
        public void TestAlpha() { var x = DiagnosticCode.Alpha; }
        public void TestBeta() { var x = DiagnosticCode.Beta; }
    }
}
";
        var diagnostics = await AnalyzerTestHelper.AnalyzeWithFilePathsAsync<Precept0028DiagnosticTestCoverage>(
            (pipelineSource, "src/Pipeline/TypeChecker.cs"),
            (testSource, "test/Precept.Tests/DiagTests.cs"));
        var missing = diagnostics.Where(d => d.Id == Precept0028DiagnosticTestCoverage.DiagnosticId_MissingTest).ToList();
        missing.Should().ContainSingle();
        missing[0].GetMessage().Should().Contain("Gamma");
    }

    [Fact]
    public async Task Gate1AllowListedCode_NoTestObligation()
    {
        var source = @"
namespace Precept.Language
{
    public enum DiagnosticCode { NumericOverflow }

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
        public void Emit()
        {
            Diagnostics.Create(DiagnosticCode.NumericOverflow, null);
        }
    }
}
";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0028DiagnosticTestCoverage>(source);
        diagnostics.Where(d => d.Id == Precept0028DiagnosticTestCoverage.DiagnosticId_MissingTest)
            .Should().BeEmpty();
    }

    [Fact]
    public async Task EmptyGate2AllowList_NoStaleWarnings()
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
        }
    }
}
";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0028DiagnosticTestCoverage>(source);
        diagnostics.Where(d => d.Id == Precept0028DiagnosticTestCoverage.DiagnosticId_StaleAllowList)
            .Should().BeEmpty();
    }

    [Fact]
    public async Task UnemittedCode_NoGate2Obligation()
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
        }
    }
}
";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0028DiagnosticTestCoverage>(source);
        diagnostics.Where(d => d.Id == Precept0028DiagnosticTestCoverage.DiagnosticId_MissingTest)
            .Where(d => d.GetMessage().Contains("Gamma"))
            .Should().BeEmpty();
    }
}
