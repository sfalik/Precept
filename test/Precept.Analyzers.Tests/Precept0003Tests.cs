using FluentAssertions;
using Xunit;

namespace Precept.Analyzers.Tests;

public class PRECEPT0003Tests
{
    private const string DiagnosticTypeDecl = @"
namespace Precept.Pipeline
{
    public enum DiagnosticStage { Lex, Parse, Type, Graph, Proof }
    public enum Severity { Info, Warning, Error }
    public readonly struct SourceRange { }

    public readonly record struct Diagnostic(
        Severity Severity,
        DiagnosticStage Stage,
        string Code,
        string Message,
        SourceRange Range);
}";

    [Fact]
    public async Task Diagnostic_type_in_other_namespace_is_not_flagged()
    {
        var source = @"
namespace Some.Other.Lib
{
    public record struct Diagnostic(string Code);

    public class C
    {
        public Diagnostic M() => new Diagnostic(""x"");
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0003DiagnosticMustUseCreate>(source);
        diagnostics.Where(d => d.Id == PRECEPT0003DiagnosticMustUseCreate.DiagnosticId).Should().BeEmpty();
    }

    [Fact]
    public async Task Direct_new_Precept_Pipeline_Diagnostic_reports_PRECEPT0003()
    {
        var source = DiagnosticTypeDecl + @"
namespace Precept.Pipeline
{
    public class SomePipelineStage
    {
        public void M()
        {
            var range = new SourceRange();
            var d = new Diagnostic(Severity.Error, DiagnosticStage.Type, ""SomeCode"", ""message"", range);
        }
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0003DiagnosticMustUseCreate>(source);
        diagnostics.Where(d => d.Id == PRECEPT0003DiagnosticMustUseCreate.DiagnosticId).Should().HaveCount(1);
    }

    [Fact]
    public async Task Diagnostics_Create_factory_is_not_flagged()
    {
        var source = DiagnosticTypeDecl + @"
namespace Precept.Pipeline
{
    public enum DiagnosticCode { UndeclaredField }

    public sealed record DiagnosticMeta(string Code, DiagnosticStage Stage, Severity Severity, string MessageTemplate);

    public static class Diagnostics
    {
        public static DiagnosticMeta GetMeta(DiagnosticCode code) => code switch
        {
            DiagnosticCode.UndeclaredField => new(nameof(DiagnosticCode.UndeclaredField), DiagnosticStage.Type, Severity.Error, ""Field not found""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(code), code, null),
        };

        public static Diagnostic Create(DiagnosticCode code, SourceRange range, params object?[] args)
        {
            var meta = GetMeta(code);
            return new(meta.Severity, meta.Stage, meta.Code,
                string.Format(meta.MessageTemplate, args), range);
        }
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0003DiagnosticMustUseCreate>(source);
        diagnostics.Where(d => d.Id == PRECEPT0003DiagnosticMustUseCreate.DiagnosticId).Should().BeEmpty();
    }

    [Fact]    public async Task Target_typed_new_Diagnostic_reports_PRECEPT0003()
    {
        // Target-typed new (Diagnostic d = new(...)) must be caught — same IObjectCreationOperation.
        var source = DiagnosticTypeDecl + @"
namespace Precept.Pipeline
{
    public class SomePipelineStage
    {
        public void M()
        {
            var range = new SourceRange();
            Diagnostic d = new(Severity.Error, DiagnosticStage.Type, ""SomeCode"", ""message"", range);
        }
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0003DiagnosticMustUseCreate>(source);
        diagnostics.Where(d => d.Id == PRECEPT0003DiagnosticMustUseCreate.DiagnosticId).Should().HaveCount(1);
    }

    [Fact]    public async Task Non_Create_method_in_Diagnostics_class_reports_PRECEPT0003()
    {
        // The exemption is scoped to Diagnostics.Create() only.
        // Any other method inside Diagnostics that constructs a Diagnostic directly must still be flagged.
        var source = DiagnosticTypeDecl + @"
namespace Precept.Pipeline
{
    public static class Diagnostics
    {
        public static Diagnostic Build()
        {
            var range = new SourceRange();
            return new Diagnostic(Severity.Error, DiagnosticStage.Type, ""SomeCode"", ""message"", range);
        }
    }
}";
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0003DiagnosticMustUseCreate>(source);
        diagnostics.Where(d => d.Id == PRECEPT0003DiagnosticMustUseCreate.DiagnosticId).Should().HaveCount(1);
    }
}
