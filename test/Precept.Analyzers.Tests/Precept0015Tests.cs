using FluentAssertions;
using Xunit;

namespace Precept.Analyzers.Tests;

public class PRECEPT0015Tests
{
    // ════════════════════════════════════════════════════════════════════════════
    //  Shared stub scaffolding for DiagnosticCode catalog
    // ════════════════════════════════════════════════════════════════════════════

    private const string DiagStubs = @"
namespace Precept.Language
{
    public enum DiagnosticCode { Alpha, Beta, Gamma }
    public enum DiagnosticStage { Lex, Parse, Type }
    public enum Severity { Error, Warning }
    public enum DiagnosticCategory { Structure, TypeSystem }
    public enum FaultCode { DivisionByZero }

    public sealed record DiagnosticMeta(
        string Code,
        DiagnosticStage Stage,
        Severity Severity,
        string MessageTemplate,
        DiagnosticCategory Category,
        DiagnosticCode[]? RelatedCodes = null,
        string? FixHint = null,
        FaultCode? PreventsFault = null);
";

    private const string CloseBrace = @"
}";

    // ════════════════════════════════════════════════════════════════════════════
    //  X33 — Code identity (nameof target must match arm)
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task CodeIdentity_NameofMatchesArm_NoDiagnostic()
    {
        var source = DiagStubs + @"
    public static class Diagnostics
    {
        public static DiagnosticMeta GetMeta(DiagnosticCode code) => code switch
        {
            DiagnosticCode.Alpha => new(nameof(DiagnosticCode.Alpha), DiagnosticStage.Lex, Severity.Error, ""msg"", DiagnosticCategory.Structure),
            DiagnosticCode.Beta  => new(nameof(DiagnosticCode.Beta),  DiagnosticStage.Lex, Severity.Error, ""msg"", DiagnosticCategory.Structure),
            DiagnosticCode.Gamma => new(nameof(DiagnosticCode.Gamma), DiagnosticStage.Lex, Severity.Error, ""msg"", DiagnosticCategory.Structure),
            _ => throw new System.ArgumentOutOfRangeException(nameof(code)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0015DiagnosticsCrossRef>(source);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task CodeIdentity_NameofMismatch_ReportsError()
    {
        var source = DiagStubs + @"
    public static class Diagnostics
    {
        public static DiagnosticMeta GetMeta(DiagnosticCode code) => code switch
        {
            DiagnosticCode.Alpha => new(nameof(DiagnosticCode.Beta), DiagnosticStage.Lex, Severity.Error, ""msg"", DiagnosticCategory.Structure),
            DiagnosticCode.Beta  => new(nameof(DiagnosticCode.Beta), DiagnosticStage.Lex, Severity.Error, ""msg"", DiagnosticCategory.Structure),
            DiagnosticCode.Gamma => new(nameof(DiagnosticCode.Gamma), DiagnosticStage.Lex, Severity.Error, ""msg"", DiagnosticCategory.Structure),
            _ => throw new System.ArgumentOutOfRangeException(nameof(code)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0015DiagnosticsCrossRef>(source);
        var codeDiags = diagnostics.Where(d => d.Id == PRECEPT0015DiagnosticsCrossRef.DiagnosticId_CodeMismatch).ToList();
        codeDiags.Should().ContainSingle();
        codeDiags[0].Severity.Should().Be(Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
        codeDiags[0].GetMessage().Should().Contain("Alpha");
        codeDiags[0].GetMessage().Should().Contain("Beta");
    }

    [Fact]
    public async Task CodeIdentity_MultipleMismatches_ReportsEach()
    {
        var source = DiagStubs + @"
    public static class Diagnostics
    {
        public static DiagnosticMeta GetMeta(DiagnosticCode code) => code switch
        {
            DiagnosticCode.Alpha => new(nameof(DiagnosticCode.Gamma), DiagnosticStage.Lex, Severity.Error, ""msg"", DiagnosticCategory.Structure),
            DiagnosticCode.Beta  => new(nameof(DiagnosticCode.Alpha), DiagnosticStage.Lex, Severity.Error, ""msg"", DiagnosticCategory.Structure),
            DiagnosticCode.Gamma => new(nameof(DiagnosticCode.Gamma), DiagnosticStage.Lex, Severity.Error, ""msg"", DiagnosticCategory.Structure),
            _ => throw new System.ArgumentOutOfRangeException(nameof(code)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0015DiagnosticsCrossRef>(source);
        diagnostics.Where(d => d.Id == PRECEPT0015DiagnosticsCrossRef.DiagnosticId_CodeMismatch)
            .Should().HaveCount(2);
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  S11 — Code must use nameof()
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Code_StringLiteral_ReportsWarning()
    {
        var source = DiagStubs + @"
    public static class Diagnostics
    {
        public static DiagnosticMeta GetMeta(DiagnosticCode code) => code switch
        {
            DiagnosticCode.Alpha => new(""Alpha"", DiagnosticStage.Lex, Severity.Error, ""msg"", DiagnosticCategory.Structure),
            DiagnosticCode.Beta  => new(nameof(DiagnosticCode.Beta), DiagnosticStage.Lex, Severity.Error, ""msg"", DiagnosticCategory.Structure),
            DiagnosticCode.Gamma => new(nameof(DiagnosticCode.Gamma), DiagnosticStage.Lex, Severity.Error, ""msg"", DiagnosticCategory.Structure),
            _ => throw new System.ArgumentOutOfRangeException(nameof(code)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0015DiagnosticsCrossRef>(source);
        var noNameofDiags = diagnostics.Where(d => d.Id == PRECEPT0015DiagnosticsCrossRef.DiagnosticId_NoNameof).ToList();
        noNameofDiags.Should().ContainSingle();
        noNameofDiags[0].Severity.Should().Be(Microsoft.CodeAnalysis.DiagnosticSeverity.Warning);
        noNameofDiags[0].GetMessage().Should().Contain("Alpha");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  X34 — RelatedCodes must not self-reference
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task RelatedCodes_NoSelfRef_NoDiagnostic()
    {
        var source = DiagStubs + @"
    public static class Diagnostics
    {
        public static DiagnosticMeta GetMeta(DiagnosticCode code) => code switch
        {
            DiagnosticCode.Alpha => new(nameof(DiagnosticCode.Alpha), DiagnosticStage.Lex, Severity.Error, ""msg"", DiagnosticCategory.Structure,
                RelatedCodes: [DiagnosticCode.Beta]),
            DiagnosticCode.Beta  => new(nameof(DiagnosticCode.Beta),  DiagnosticStage.Lex, Severity.Error, ""msg"", DiagnosticCategory.Structure,
                RelatedCodes: [DiagnosticCode.Alpha, DiagnosticCode.Gamma]),
            DiagnosticCode.Gamma => new(nameof(DiagnosticCode.Gamma), DiagnosticStage.Lex, Severity.Error, ""msg"", DiagnosticCategory.Structure),
            _ => throw new System.ArgumentOutOfRangeException(nameof(code)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0015DiagnosticsCrossRef>(source);
        diagnostics.Where(d => d.Id == PRECEPT0015DiagnosticsCrossRef.DiagnosticId_SelfRelated)
            .Should().BeEmpty();
    }

    [Fact]
    public async Task RelatedCodes_SelfReference_ReportsError()
    {
        var source = DiagStubs + @"
    public static class Diagnostics
    {
        public static DiagnosticMeta GetMeta(DiagnosticCode code) => code switch
        {
            DiagnosticCode.Alpha => new(nameof(DiagnosticCode.Alpha), DiagnosticStage.Lex, Severity.Error, ""msg"", DiagnosticCategory.Structure,
                RelatedCodes: [DiagnosticCode.Alpha, DiagnosticCode.Beta]),
            DiagnosticCode.Beta  => new(nameof(DiagnosticCode.Beta),  DiagnosticStage.Lex, Severity.Error, ""msg"", DiagnosticCategory.Structure),
            DiagnosticCode.Gamma => new(nameof(DiagnosticCode.Gamma), DiagnosticStage.Lex, Severity.Error, ""msg"", DiagnosticCategory.Structure),
            _ => throw new System.ArgumentOutOfRangeException(nameof(code)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0015DiagnosticsCrossRef>(source);
        var selfRefDiags = diagnostics.Where(d => d.Id == PRECEPT0015DiagnosticsCrossRef.DiagnosticId_SelfRelated).ToList();
        selfRefDiags.Should().ContainSingle();
        selfRefDiags[0].Severity.Should().Be(Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
        selfRefDiags[0].GetMessage().Should().Contain("Alpha");
    }

    [Fact]
    public async Task RelatedCodes_NullRelatedCodes_NoDiagnostic()
    {
        var source = DiagStubs + @"
    public static class Diagnostics
    {
        public static DiagnosticMeta GetMeta(DiagnosticCode code) => code switch
        {
            DiagnosticCode.Alpha => new(nameof(DiagnosticCode.Alpha), DiagnosticStage.Lex, Severity.Error, ""msg"", DiagnosticCategory.Structure),
            DiagnosticCode.Beta  => new(nameof(DiagnosticCode.Beta),  DiagnosticStage.Lex, Severity.Error, ""msg"", DiagnosticCategory.Structure),
            DiagnosticCode.Gamma => new(nameof(DiagnosticCode.Gamma), DiagnosticStage.Lex, Severity.Error, ""msg"", DiagnosticCategory.Structure),
            _ => throw new System.ArgumentOutOfRangeException(nameof(code)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0015DiagnosticsCrossRef>(source);
        diagnostics.Where(d => d.Id == PRECEPT0015DiagnosticsCrossRef.DiagnosticId_SelfRelated)
            .Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Scope guards
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task NonDiagnosticCodeSwitch_NoDiagnostic()
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
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0015DiagnosticsCrossRef>(source);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task WrongNamespace_NoDiagnostic()
    {
        var source = @"
namespace Other
{
    public enum DiagnosticCode { Alpha, Beta }
    public enum DiagnosticStage { Lex }
    public enum Severity { Error }
    public enum DiagnosticCategory { Structure }

    public sealed record DiagnosticMeta(
        string Code, DiagnosticStage Stage, Severity Severity,
        string MessageTemplate, DiagnosticCategory Category,
        DiagnosticCode[]? RelatedCodes = null);

    public static class Diagnostics
    {
        public static DiagnosticMeta GetMeta(DiagnosticCode code) => code switch
        {
            DiagnosticCode.Alpha => new(nameof(DiagnosticCode.Beta), DiagnosticStage.Lex, Severity.Error, ""msg"", DiagnosticCategory.Structure),
            DiagnosticCode.Beta  => new(nameof(DiagnosticCode.Beta), DiagnosticStage.Lex, Severity.Error, ""msg"", DiagnosticCategory.Structure),
            _ => throw new System.ArgumentOutOfRangeException(nameof(code)),
        };
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0015DiagnosticsCrossRef>(source);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task WrongMethodName_NoDiagnostic()
    {
        var source = DiagStubs + @"
    public static class Diagnostics
    {
        public static DiagnosticMeta Lookup(DiagnosticCode code) => code switch
        {
            DiagnosticCode.Alpha => new(nameof(DiagnosticCode.Beta), DiagnosticStage.Lex, Severity.Error, ""msg"", DiagnosticCategory.Structure),
            DiagnosticCode.Beta  => new(nameof(DiagnosticCode.Beta), DiagnosticStage.Lex, Severity.Error, ""msg"", DiagnosticCategory.Structure),
            DiagnosticCode.Gamma => new(nameof(DiagnosticCode.Gamma), DiagnosticStage.Lex, Severity.Error, ""msg"", DiagnosticCategory.Structure),
            _ => throw new System.ArgumentOutOfRangeException(nameof(code)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0015DiagnosticsCrossRef>(source);
        diagnostics.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Edge: discard arm → no diagnostic
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task DiscardArm_NoDiagnostic()
    {
        var source = DiagStubs + @"
    public static class Diagnostics
    {
        public static DiagnosticMeta GetMeta(DiagnosticCode code) => code switch
        {
            _ => new(""fallback"", DiagnosticStage.Lex, Severity.Error, ""msg"", DiagnosticCategory.Structure),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0015DiagnosticsCrossRef>(source);
        diagnostics.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Combined: Code mismatch + self-ref in the same arm
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task Combined_CodeMismatchAndSelfRef_ReportsBoth()
    {
        var source = DiagStubs + @"
    public static class Diagnostics
    {
        public static DiagnosticMeta GetMeta(DiagnosticCode code) => code switch
        {
            DiagnosticCode.Alpha => new(nameof(DiagnosticCode.Beta), DiagnosticStage.Lex, Severity.Error, ""msg"", DiagnosticCategory.Structure,
                RelatedCodes: [DiagnosticCode.Alpha]),
            DiagnosticCode.Beta  => new(nameof(DiagnosticCode.Beta), DiagnosticStage.Lex, Severity.Error, ""msg"", DiagnosticCategory.Structure),
            DiagnosticCode.Gamma => new(nameof(DiagnosticCode.Gamma), DiagnosticStage.Lex, Severity.Error, ""msg"", DiagnosticCategory.Structure),
            _ => throw new System.ArgumentOutOfRangeException(nameof(code)),
        };
    }" + CloseBrace;

        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0015DiagnosticsCrossRef>(source);
        diagnostics.Where(d => d.Id == PRECEPT0015DiagnosticsCrossRef.DiagnosticId_CodeMismatch)
            .Should().ContainSingle();
        diagnostics.Where(d => d.Id == PRECEPT0015DiagnosticsCrossRef.DiagnosticId_SelfRelated)
            .Should().ContainSingle();
    }
}
