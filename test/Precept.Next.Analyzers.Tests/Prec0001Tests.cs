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

    [Fact]
    public async Task Fail_with_FaultCode_and_extra_args_reports_nothing()
    {
        // First arg is FaultCode — additional args must not cause over-firing.
        var source = FaultCodeDecl + @"
namespace Precept.Runtime
{
    public class Evaluator
    {
        protected object Fail(FaultCode code, string detail) => throw new System.Exception();

        public object M(int a)
        {
            if (a < 0) return Fail(FaultCode.DivisionByZero, ""extra detail"");
            return a;
        }
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Prec0001FailMustUseFaultCode>(source);
        diagnostics.Where(d => d.Id == Prec0001FailMustUseFaultCode.DiagnosticId).Should().BeEmpty();
    }

    [Fact]
    public async Task Multiple_Fail_violations_in_one_method_each_report_PREC0001()
    {
        var source = FaultCodeDecl + @"
namespace Precept.Runtime
{
    public class Evaluator
    {
        protected object Fail(string message) => throw new System.Exception();

        public object M(int a, int b)
        {
            if (a < 0) return Fail(""negative a"");
            if (b == 0) return Fail(""zero b"");
            return a / b;
        }
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Prec0001FailMustUseFaultCode>(source);
        diagnostics.Where(d => d.Id == Prec0001FailMustUseFaultCode.DiagnosticId).Should().HaveCount(2);
    }

    [Fact]
    public async Task Fail_in_unrelated_namespace_is_not_flagged()
    {
        // PREC0001 only applies to Fail() methods defined in Precept.Runtime.
        // A Fail() method in a third-party library must not be flagged.
        var source = @"
namespace ThirdParty.Logging
{
    public class Logger
    {
        public void Fail(string message) { }

        public void M()
        {
            Fail(""something went wrong"");
        }
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Prec0001FailMustUseFaultCode>(source);
        diagnostics.Where(d => d.Id == Prec0001FailMustUseFaultCode.DiagnosticId).Should().BeEmpty();
    }

    [Fact]
    public async Task Fail_with_third_party_FaultCode_reports_PREC0001()
    {
        // A type named FaultCode from a different namespace must not bypass PREC0001.
        var source = @"
namespace ThirdParty
{
    public enum FaultCode { SomeError }
}
namespace Precept.Runtime
{
    public class Evaluator
    {
        protected object Fail(ThirdParty.FaultCode code) => throw new System.Exception();

        public object M()
        {
            return Fail(ThirdParty.FaultCode.SomeError);
        }
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Prec0001FailMustUseFaultCode>(source);
        diagnostics.Where(d => d.Id == Prec0001FailMustUseFaultCode.DiagnosticId).Should().HaveCount(1);
    }
}
