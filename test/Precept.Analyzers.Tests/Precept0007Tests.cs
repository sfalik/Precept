using FluentAssertions;
using Xunit;

namespace Precept.Analyzers.Tests;

public class PRECEPT0007Tests
{
    // ── Minimal type stubs ──────────────────────────────────────────────────
    // The analyzer identifies GetMeta by: method name == "GetMeta", containing type
    // in Precept.Language, switch value type is a known catalog enum.

    /// <summary>
    /// Small enum + exhaustive switch → no diagnostic.
    /// </summary>
    private const string ExhaustiveSource = @"
namespace Precept.Language
{
    public enum FaultCode { DivisionByZero, SqrtOfNegative, TypeMismatch }

    public sealed record FaultMeta(string Code, string MessageTemplate);

    public static class Faults
    {
        public static FaultMeta GetMeta(FaultCode code) => code switch
        {
            FaultCode.DivisionByZero => new(""DivisionByZero"", ""Divisor is zero""),
            FaultCode.SqrtOfNegative => new(""SqrtOfNegative"", ""Negative sqrt""),
            FaultCode.TypeMismatch   => new(""TypeMismatch"", ""Type mismatch""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(code)),
        };
    }
}";

    /// <summary>
    /// Missing one member → diagnostic listing the missing member.
    /// </summary>
    private const string MissingOneSource = @"
namespace Precept.Language
{
    public enum FaultCode { DivisionByZero, SqrtOfNegative, TypeMismatch }

    public sealed record FaultMeta(string Code, string MessageTemplate);

    public static class Faults
    {
        public static FaultMeta GetMeta(FaultCode code) => code switch
        {
            FaultCode.DivisionByZero => new(""DivisionByZero"", ""Divisor is zero""),
            FaultCode.SqrtOfNegative => new(""SqrtOfNegative"", ""Negative sqrt""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(code)),
        };
    }
}";

    /// <summary>
    /// Missing two members → diagnostic listing both.
    /// </summary>
    private const string MissingTwoSource = @"
namespace Precept.Language
{
    public enum FaultCode { DivisionByZero, SqrtOfNegative, TypeMismatch }

    public sealed record FaultMeta(string Code, string MessageTemplate);

    public static class Faults
    {
        public static FaultMeta GetMeta(FaultCode code) => code switch
        {
            FaultCode.DivisionByZero => new(""DivisionByZero"", ""Divisor is zero""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(code)),
        };
    }
}";

    /// <summary>
    /// Discard-only switch (no explicit arms) → diagnostic listing ALL members.
    /// </summary>
    private const string DiscardOnlySource = @"
namespace Precept.Language
{
    public enum FaultCode { DivisionByZero, SqrtOfNegative }

    public sealed record FaultMeta(string Code, string MessageTemplate);

    public static class Faults
    {
        public static FaultMeta GetMeta(FaultCode code) => code switch
        {
            _ => new(""Unknown"", ""Unknown""),
        };
    }
}";

    /// <summary>
    /// Uses TypeKind enum (different catalog) → still detected.
    /// </summary>
    private const string TypeKindExhaustiveSource = @"
namespace Precept.Language
{
    public enum TypeKind { String, Boolean, Integer }

    public sealed record TypeMeta(string Name);

    public static class Types
    {
        public static TypeMeta GetMeta(TypeKind kind) => kind switch
        {
            TypeKind.String  => new(""string""),
            TypeKind.Boolean => new(""boolean""),
            TypeKind.Integer => new(""integer""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";

    /// <summary>
    /// TypeKind missing one → diagnostic.
    /// </summary>
    private const string TypeKindMissingSource = @"
namespace Precept.Language
{
    public enum TypeKind { String, Boolean, Integer }

    public sealed record TypeMeta(string Name);

    public static class Types
    {
        public static TypeMeta GetMeta(TypeKind kind) => kind switch
        {
            TypeKind.String  => new(""string""),
            TypeKind.Boolean => new(""boolean""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";

    /// <summary>
    /// OperatorKind enum — verifies another catalog enum is recognized.
    /// </summary>
    private const string OperatorKindExhaustiveSource = @"
namespace Precept.Language
{
    public enum OperatorKind { Equals, NotEquals, LessThan }

    public sealed record OperatorMeta(string Name);

    public static class Operators
    {
        public static OperatorMeta GetMeta(OperatorKind kind) => kind switch
        {
            OperatorKind.Equals    => new(""==""),
            OperatorKind.NotEquals => new(""!=""),
            OperatorKind.LessThan  => new(""<""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";

    /// <summary>
    /// Not a GetMeta method (different name) → no diagnostic even if missing cases.
    /// </summary>
    private const string NotGetMetaMethodSource = @"
namespace Precept.Language
{
    public enum FaultCode { DivisionByZero, SqrtOfNegative, TypeMismatch }

    public static class Faults
    {
        public static string Describe(FaultCode code) => code switch
        {
            FaultCode.DivisionByZero => ""zero"",
            _ => ""other"",
        };
    }
}";

    /// <summary>
    /// GetMeta method but outside Precept.Language namespace → no diagnostic.
    /// </summary>
    private const string WrongNamespaceSource = @"
namespace Other.Library
{
    public enum FaultCode { DivisionByZero, SqrtOfNegative }

    public sealed record FaultMeta(string Code, string MessageTemplate);

    public static class Faults
    {
        public static FaultMeta GetMeta(FaultCode code) => code switch
        {
            FaultCode.DivisionByZero => new(""DivisionByZero"", ""Divisor is zero""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(code)),
        };
    }
}";

    /// <summary>
    /// Switch on a non-catalog enum type inside GetMeta → no diagnostic.
    /// </summary>
    private const string NonCatalogEnumSource = @"
namespace Precept.Language
{
    public enum MyCustomEnum { Alpha, Beta }

    public static class MyClass
    {
        public static string GetMeta(MyCustomEnum kind) => kind switch
        {
            MyCustomEnum.Alpha => ""a"",
            _ => ""other"",
        };
    }
}";

    /// <summary>
    /// TokenKind — verifies another catalog.
    /// </summary>
    private const string TokenKindMissingSource = @"
namespace Precept.Language
{
    public enum TokenKind { Precept, State, Event, Arrow }

    public sealed record TokenMeta(string Name);

    public static class Tokens
    {
        public static TokenMeta GetMeta(TokenKind kind) => kind switch
        {
            TokenKind.Precept => new(""precept""),
            TokenKind.State   => new(""state""),
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind)),
        };
    }
}";

    // ── Tests ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Exhaustive_FaultCode_switch_produces_no_diagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0007GetMetaExhaustiveness>(ExhaustiveSource);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Missing_one_FaultCode_member_produces_diagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0007GetMetaExhaustiveness>(MissingOneSource);
        diagnostics.Should().ContainSingle()
            .Which.GetMessage().Should().Contain("TypeMismatch");
    }

    [Fact]
    public async Task Missing_two_FaultCode_members_lists_both()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0007GetMetaExhaustiveness>(MissingTwoSource);
        diagnostics.Should().ContainSingle();
        var msg = diagnostics[0].GetMessage();
        msg.Should().Contain("SqrtOfNegative").And.Contain("TypeMismatch");
    }

    [Fact]
    public async Task Discard_only_switch_flags_all_members()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0007GetMetaExhaustiveness>(DiscardOnlySource);
        diagnostics.Should().ContainSingle();
        var msg = diagnostics[0].GetMessage();
        msg.Should().Contain("DivisionByZero").And.Contain("SqrtOfNegative");
    }

    [Fact]
    public async Task Exhaustive_TypeKind_switch_produces_no_diagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0007GetMetaExhaustiveness>(TypeKindExhaustiveSource);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Missing_TypeKind_member_produces_diagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0007GetMetaExhaustiveness>(TypeKindMissingSource);
        diagnostics.Should().ContainSingle()
            .Which.GetMessage().Should().Contain("Integer");
    }

    [Fact]
    public async Task Exhaustive_OperatorKind_switch_produces_no_diagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0007GetMetaExhaustiveness>(OperatorKindExhaustiveSource);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Non_GetMeta_method_is_ignored()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0007GetMetaExhaustiveness>(NotGetMetaMethodSource);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Wrong_namespace_is_ignored()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0007GetMetaExhaustiveness>(WrongNamespaceSource);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task Non_catalog_enum_in_GetMeta_is_ignored()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0007GetMetaExhaustiveness>(NonCatalogEnumSource);
        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task TokenKind_missing_two_members_lists_both()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0007GetMetaExhaustiveness>(TokenKindMissingSource);
        diagnostics.Should().ContainSingle();
        var msg = diagnostics[0].GetMessage();
        msg.Should().Contain("Arrow").And.Contain("Event");
    }

    [Fact]
    public async Task Diagnostic_has_correct_id()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0007GetMetaExhaustiveness>(MissingOneSource);
        diagnostics.Should().ContainSingle()
            .Which.Id.Should().Be("PRECEPT0007");
    }

    [Fact]
    public async Task Diagnostic_severity_is_error()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0007GetMetaExhaustiveness>(MissingOneSource);
        diagnostics.Should().ContainSingle()
            .Which.Severity.Should().Be(Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
    }

    [Fact]
    public async Task Message_format_includes_enum_type_name()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0007GetMetaExhaustiveness>(MissingOneSource);
        diagnostics.Should().ContainSingle()
            .Which.GetMessage().Should().Contain("FaultCode");
    }
}
