using FluentAssertions;
using Xunit;

namespace Precept.Analyzers.Tests;

public class Prec0001Tests
{
    private const string FaultCodeDecl = @"
namespace Precept.Runtime
{
    public enum FaultCode { DivisionByZero, TypeMismatch }
}";

    [Fact]
    public async Task Fail_with_FaultCode_reports_nothing()
    {
        var source = FaultCodeDecl + @"
namespace Precept.Runtime
{
    public class Evaluator
    {
        protected object Fail(FaultCode code) => throw new System.Exception();

        public object Divide(int a, int b)
        {
            if (b == 0) return Fail(FaultCode.DivisionByZero);
            return a / b;
        }
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Prec0001FailMustUseFaultCode>(source);
        diagnostics.Where(d => d.Id == Prec0001FailMustUseFaultCode.DiagnosticId).Should().BeEmpty();
    }

    [Fact]
    public async Task Fail_with_string_arg_reports_PREC0001()
    {
        var source = FaultCodeDecl + @"
namespace Precept.Runtime
{
    public class Evaluator
    {
        protected object Fail(FaultCode code) => throw new System.Exception();
        protected object Fail(string message) => throw new System.Exception();

        public object Divide(int a, int b)
        {
            if (b == 0) return Fail(""zero divisor"");
            return a / b;
        }
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Prec0001FailMustUseFaultCode>(source);
        diagnostics.Where(d => d.Id == Prec0001FailMustUseFaultCode.DiagnosticId).Should().HaveCount(1);
    }

    [Fact]
    public async Task Fail_with_no_args_reports_PREC0001()
    {
        var source = FaultCodeDecl + @"
namespace Precept.Runtime
{
    public class Evaluator
    {
        protected object Fail() => throw new System.Exception();

        public object Divide(int a, int b)
        {
            if (b == 0) return Fail();
            return a / b;
        }
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Prec0001FailMustUseFaultCode>(source);
        diagnostics.Where(d => d.Id == Prec0001FailMustUseFaultCode.DiagnosticId).Should().HaveCount(1);
    }
}
