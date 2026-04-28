using FluentAssertions;
using Xunit;

namespace Precept.Analyzers.Tests;

public class PRECEPT0004Tests
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
    public async Task Direct_new_Fault_reports_PRECEPT0004()
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
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0004FaultMustUseCreate>(source);
        diagnostics.Where(d => d.Id == PRECEPT0004FaultMustUseCreate.DiagnosticId).Should().HaveCount(1);
    }

    [Fact]
    public async Task Target_typed_new_Fault_reports_PRECEPT0004()
    {
        // Target-typed new (Fault f = new(...)) must be caught — same IObjectCreationOperation.
        var source = FaultTypeDecl + @"
namespace Precept.Runtime
{
    public class Evaluator
    {
        public Fault M()
        {
            Fault f = new(FaultCode.DivisionByZero, ""DivisionByZero"", ""Divisor is zero"");
            return f;
        }
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0004FaultMustUseCreate>(source);
        diagnostics.Where(d => d.Id == PRECEPT0004FaultMustUseCreate.DiagnosticId).Should().HaveCount(1);
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
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0004FaultMustUseCreate>(source);
        diagnostics.Where(d => d.Id == PRECEPT0004FaultMustUseCreate.DiagnosticId).Should().BeEmpty();
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
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0004FaultMustUseCreate>(source);
        diagnostics.Where(d => d.Id == PRECEPT0004FaultMustUseCreate.DiagnosticId).Should().BeEmpty();
    }

    [Fact]    public async Task Fully_qualified_new_Fault_from_external_namespace_reports_PRECEPT0004()
    {
        // Construction from outside Precept.Runtime using a fully qualified type name must still be flagged.
        // The analyzer checks op.Type (resolved type), not the call-site namespace.
        var source = FaultTypeDecl + @"
namespace Some.Consumer
{
    public class Client
    {
        public Precept.Runtime.Fault M()
        {
            return new Precept.Runtime.Fault(
                Precept.Runtime.FaultCode.DivisionByZero,
                ""DivisionByZero"",
                ""Divisor is zero"");
        }
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0004FaultMustUseCreate>(source);
        diagnostics.Where(d => d.Id == PRECEPT0004FaultMustUseCreate.DiagnosticId).Should().HaveCount(1);
    }

    [Fact]
    public async Task Non_Create_method_in_Faults_class_reports_PRECEPT0004()
    {
        // The exemption is scoped to Faults.Create() only.
        // Any other method inside Faults that constructs a Fault directly must still be flagged.
        var source = FaultTypeDecl + @"
namespace Precept.Runtime
{
    public static class Faults
    {
        public static Fault Build(FaultCode code)
        {
            return new Fault(code, code.ToString(), ""message"");
        }
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0004FaultMustUseCreate>(source);
        diagnostics.Where(d => d.Id == PRECEPT0004FaultMustUseCreate.DiagnosticId).Should().HaveCount(1);
    }
}
