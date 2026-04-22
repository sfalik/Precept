using FluentAssertions;
using Xunit;

namespace Precept.Analyzers.Tests;

public class Prec0004Tests
{
    private const string FaultTypeDecl = @"
namespace Precept.Runtime
{
    public enum FaultCode { DivisionByZero }

    public readonly record struct Fault(
        FaultCode Code,
        string CodeName,
        string Message);
}";

    [Fact]
    public async Task Direct_new_Fault_reports_PREC0004()
    {
        var source = FaultTypeDecl + @"
namespace Precept.Runtime
{
    public class Evaluator
    {
        public Fault M()
        {
            return new Fault(FaultCode.DivisionByZero, ""DivisionByZero"", ""Divisor is zero"");
        }
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Prec0004FaultMustUseCreate>(source);
        diagnostics.Where(d => d.Id == Prec0004FaultMustUseCreate.DiagnosticId).Should().HaveCount(1);
    }

    [Fact]
    public async Task Faults_Create_factory_is_not_flagged()
    {
        var source = FaultTypeDecl + @"
namespace Precept.Runtime
{
    public sealed record FaultMeta(string Code, string MessageTemplate);

    public static class Faults
    {
        public static FaultMeta GetMeta(FaultCode code) => code switch
        {
            FaultCode.DivisionByZero => new(nameof(FaultCode.DivisionByZero), ""Divisor evaluated to zero""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(code), code, null),
        };

        public static Fault Create(FaultCode code, params object?[] args)
        {
            var meta = GetMeta(code);
            return new(code, meta.Code, string.Format(meta.MessageTemplate, args));
        }
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Prec0004FaultMustUseCreate>(source);
        diagnostics.Where(d => d.Id == Prec0004FaultMustUseCreate.DiagnosticId).Should().BeEmpty();
    }

    [Fact]
    public async Task Fault_type_in_different_namespace_is_not_flagged()
    {
        var source = @"
namespace Some.Other.Library
{
    public record struct Fault(string Message);

    public class C
    {
        public Fault M() => new Fault(""oops"");
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Prec0004FaultMustUseCreate>(source);
        diagnostics.Where(d => d.Id == Prec0004FaultMustUseCreate.DiagnosticId).Should().BeEmpty();
    }
}
