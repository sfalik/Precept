using FluentAssertions;
using Xunit;

namespace Precept.Analyzers.Tests;

public class PRECEPT0016Tests
{
    // ── Shared stubs ────────────────────────────────────────────────────────

    private const string FaultStubs = @"
namespace Precept.Language
{
    public enum FaultCode { DivisionByZero, SqrtOfNegative, TypeMismatch }
    public enum FaultSeverity { Fatal, Recoverable }

    public sealed record FaultMeta(
        string Code,
        string MessageTemplate,
        FaultSeverity Severity = FaultSeverity.Fatal,
        string? RecoveryHint = null);
";

    private const string CloseBrace = @"
}";

    // ════════════════════════════════════════════════════════════════════════════
    //  X35 — Code identity (nameof target must match arm)
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CodeIdentity_NameofMatchesArm_NoDiagnostic()
    {
        var source = FaultStubs + @"
    public static class Faults
    {
        public static FaultMeta GetMeta(FaultCode code) => code switch
        {
            FaultCode.DivisionByZero => new(nameof(FaultCode.DivisionByZero), ""Divisor is zero""),
            FaultCode.SqrtOfNegative => new(nameof(FaultCode.SqrtOfNegative), ""Negative sqrt""),
            FaultCode.TypeMismatch   => new(nameof(FaultCode.TypeMismatch),   ""Type mismatch""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(code)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0016FaultsCrossRef>(source);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task CodeIdentity_NameofMismatch_ReportsError()
    {
        var source = FaultStubs + @"
    public static class Faults
    {
        public static FaultMeta GetMeta(FaultCode code) => code switch
        {
            FaultCode.DivisionByZero => new(nameof(FaultCode.SqrtOfNegative), ""Divisor is zero""),
            FaultCode.SqrtOfNegative => new(nameof(FaultCode.SqrtOfNegative), ""Negative sqrt""),
            FaultCode.TypeMismatch   => new(nameof(FaultCode.TypeMismatch),   ""Type mismatch""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(code)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0016FaultsCrossRef>(source);
        var codeDiags = diagnostics.Where(d => d.Id == PRECEPT0016FaultsCrossRef.DiagnosticId_CodeMismatch).ToList();
        codeDiags.Should().ContainSingle();
        codeDiags[0].Severity.Should().Be(Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
        codeDiags[0].GetMessage().Should().Contain("DivisionByZero");
        codeDiags[0].GetMessage().Should().Contain("SqrtOfNegative");
    }

    [Fact]
    public async Task CodeIdentity_MultipleMismatches_ReportsEach()
    {
        var source = FaultStubs + @"
    public static class Faults
    {
        public static FaultMeta GetMeta(FaultCode code) => code switch
        {
            FaultCode.DivisionByZero => new(nameof(FaultCode.TypeMismatch),   ""Divisor is zero""),
            FaultCode.SqrtOfNegative => new(nameof(FaultCode.DivisionByZero), ""Negative sqrt""),
            FaultCode.TypeMismatch   => new(nameof(FaultCode.TypeMismatch),   ""Type mismatch""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(code)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0016FaultsCrossRef>(source);
        diagnostics.Where(d => d.Id == PRECEPT0016FaultsCrossRef.DiagnosticId_CodeMismatch)
            .Should().HaveCount(2);
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  S12 — Code must use nameof()
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Code_StringLiteral_ReportsWarning()
    {
        var source = FaultStubs + @"
    public static class Faults
    {
        public static FaultMeta GetMeta(FaultCode code) => code switch
        {
            FaultCode.DivisionByZero => new(""DivisionByZero"", ""Divisor is zero""),
            FaultCode.SqrtOfNegative => new(nameof(FaultCode.SqrtOfNegative), ""Negative sqrt""),
            FaultCode.TypeMismatch   => new(nameof(FaultCode.TypeMismatch),   ""Type mismatch""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(code)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0016FaultsCrossRef>(source);
        var noNameofDiags = diagnostics.Where(d => d.Id == PRECEPT0016FaultsCrossRef.DiagnosticId_NoNameof).ToList();
        noNameofDiags.Should().ContainSingle();
        noNameofDiags[0].Severity.Should().Be(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);
        noNameofDiags[0].GetMessage().Should().Contain("DivisionByZero");
    }

    [Fact]
    public async Task Code_AllStringLiterals_ReportsAll()
    {
        var source = FaultStubs + @"
    public static class Faults
    {
        public static FaultMeta GetMeta(FaultCode code) => code switch
        {
            FaultCode.DivisionByZero => new(""DivisionByZero"", ""Divisor is zero""),
            FaultCode.SqrtOfNegative => new(""SqrtOfNegative"", ""Negative sqrt""),
            FaultCode.TypeMismatch   => new(""TypeMismatch"",   ""Type mismatch""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(code)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0016FaultsCrossRef>(source);
        diagnostics.Where(d => d.Id == PRECEPT0016FaultsCrossRef.DiagnosticId_NoNameof)
            .Should().HaveCount(3);
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Scope guards
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task NonFaultCodeSwitch_NoDiagnostic()
    {
        var source = @"
namespace Precept.Language
{
    public enum OperatorKind { Plus, Minus }
    public sealed record OperatorMeta(OperatorKind Kind, string Description);

    public static class Operators
    {
        public static OperatorMeta GetMeta(OperatorKind kind) => kind switch
        {
            OperatorKind.Plus  => new(kind, ""Addition""),
            OperatorKind.Minus => new(kind, ""Subtraction""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0016FaultsCrossRef>(source);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task WrongNamespace_NoDiagnostic()
    {
        var source = @"
namespace Other
{
    public enum FaultCode { A, B }
    public enum FaultSeverity { Fatal }
    public sealed record FaultMeta(string Code, string MessageTemplate, FaultSeverity Severity = FaultSeverity.Fatal);

    public static class Faults
    {
        public static FaultMeta GetMeta(FaultCode code) => code switch
        {
            FaultCode.A => new(nameof(FaultCode.B), ""wrong""),
            FaultCode.B => new(nameof(FaultCode.B), ""ok""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(code)),
        };
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0016FaultsCrossRef>(source);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task WrongMethodName_NoDiagnostic()
    {
        var source = FaultStubs + @"
    public static class Faults
    {
        public static FaultMeta Lookup(FaultCode code) => code switch
        {
            FaultCode.DivisionByZero => new(nameof(FaultCode.SqrtOfNegative), ""wrong""),
            FaultCode.SqrtOfNegative => new(nameof(FaultCode.SqrtOfNegative), ""ok""),
            FaultCode.TypeMismatch   => new(nameof(FaultCode.TypeMismatch),   ""ok""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(code)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0016FaultsCrossRef>(source);
        diagnostics.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Edge cases
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DiscardArm_NoDiagnostic()
    {
        var source = FaultStubs + @"
    public static class Faults
    {
        public static FaultMeta GetMeta(FaultCode code) => code switch
        {
            _ => new(""fallback"", ""unknown fault""),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0016FaultsCrossRef>(source);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Combined_MismatchAndLiteral_ReportsBoth()
    {
        var source = FaultStubs + @"
    public static class Faults
    {
        public static FaultMeta GetMeta(FaultCode code) => code switch
        {
            FaultCode.DivisionByZero => new(nameof(FaultCode.TypeMismatch), ""wrong identity""),
            FaultCode.SqrtOfNegative => new(""SqrtOfNegative"",             ""bare literal""),
            FaultCode.TypeMismatch   => new(nameof(FaultCode.TypeMismatch), ""ok""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(code)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0016FaultsCrossRef>(source);
        diagnostics.Where(d => d.Id == PRECEPT0016FaultsCrossRef.DiagnosticId_CodeMismatch)
            .Should().ContainSingle();
        diagnostics.Where(d => d.Id == PRECEPT0016FaultsCrossRef.DiagnosticId_NoNameof)
            .Should().ContainSingle();
    }

    [Fact]
    public async Task DiagnosticMessage_ContainsBothNames()
    {
        var source = FaultStubs + @"
    public static class Faults
    {
        public static FaultMeta GetMeta(FaultCode code) => code switch
        {
            FaultCode.DivisionByZero => new(nameof(FaultCode.SqrtOfNegative), ""wrong""),
            FaultCode.SqrtOfNegative => new(nameof(FaultCode.SqrtOfNegative), ""ok""),
            FaultCode.TypeMismatch   => new(nameof(FaultCode.TypeMismatch),   ""ok""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(code)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0016FaultsCrossRef>(source);
        var d = diagnostics.Single(x => x.Id == PRECEPT0016FaultsCrossRef.DiagnosticId_CodeMismatch);
        var msg = d.GetMessage();
        msg.Should().Contain("DivisionByZero");
        msg.Should().Contain("SqrtOfNegative");
        msg.Should().Contain("must reference");
    }
}
