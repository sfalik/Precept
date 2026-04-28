using FluentAssertions;
using Xunit;

namespace Precept.Analyzers.Tests;

public class PRECEPT0002Tests
{
    private const string Preamble = @"
namespace Precept.Pipeline
{
    public enum DiagnosticCode { DivisionByZero, TypeMismatch }
}

namespace Precept.Runtime
{
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public sealed class StaticallyPreventableAttribute : System.Attribute
    {
        public StaticallyPreventableAttribute(Precept.Pipeline.DiagnosticCode code) { }
    }
}";

    [Fact]
    public async Task All_members_have_attribute_reports_nothing()
    {
        var source = Preamble + @"
namespace Precept.Runtime
{
    public enum FaultCode
    {
        [StaticallyPreventable(Precept.Pipeline.DiagnosticCode.DivisionByZero)]
        DivisionByZero,

        [StaticallyPreventable(Precept.Pipeline.DiagnosticCode.TypeMismatch)]
        TypeMismatch,
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0002FaultCodeMustHaveStaticallyPreventable>(source);
        diagnostics.Where(d => d.Id == PRECEPT0002FaultCodeMustHaveStaticallyPreventable.DiagnosticId).Should().BeEmpty();
    }

    [Fact]
    public async Task Member_missing_attribute_reports_PRECEPT0002()
    {
        var source = Preamble + @"
namespace Precept.Runtime
{
    public enum FaultCode
    {
        [StaticallyPreventable(Precept.Pipeline.DiagnosticCode.DivisionByZero)]
        DivisionByZero,

        TypeMismatch,
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0002FaultCodeMustHaveStaticallyPreventable>(source);
        diagnostics
            .Where(d => d.Id == PRECEPT0002FaultCodeMustHaveStaticallyPreventable.DiagnosticId)
            .Should().ContainSingle()
            .Which.GetMessage().Should().Contain("TypeMismatch");
    }

    [Fact]
    public async Task All_members_missing_attribute_each_report_PRECEPT0002()
    {
        var source = Preamble + @"
namespace Precept.Runtime
{
    public enum FaultCode
    {
        DivisionByZero,
        TypeMismatch,
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0002FaultCodeMustHaveStaticallyPreventable>(source);
        diagnostics
            .Where(d => d.Id == PRECEPT0002FaultCodeMustHaveStaticallyPreventable.DiagnosticId)
            .Should().HaveCount(2);
    }

    [Fact]
    public async Task Non_FaultCode_enum_is_not_checked()
    {
        var source = Preamble + @"
namespace Precept.Runtime
{
    public enum SomeOtherEnum
    {
        Value1,
        Value2,
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0002FaultCodeMustHaveStaticallyPreventable>(source);
        diagnostics.Where(d => d.Id == PRECEPT0002FaultCodeMustHaveStaticallyPreventable.DiagnosticId).Should().BeEmpty();
    }

    [Fact]
    public async Task Empty_FaultCode_enum_reports_nothing()
    {
        // An empty FaultCode enum has no members to validate — analyzer must not crash or
        // produce spurious diagnostics. Zero members means zero violations.
        var source = Preamble + @"
namespace Precept.Runtime
{
    public enum FaultCode { }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0002FaultCodeMustHaveStaticallyPreventable>(source);
        diagnostics.Where(d => d.Id == PRECEPT0002FaultCodeMustHaveStaticallyPreventable.DiagnosticId).Should().BeEmpty();
    }
}
