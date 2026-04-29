using FluentAssertions;
using Xunit;

namespace Precept.Analyzers.Tests;

public class PRECEPT0018Tests
{
    // ── True positives ────────────────────────────────────────────────────────

    /// <summary>
    /// TP1: Basic semantic enum with unnamed zero slot — flagged.
    /// </summary>
    private const string TP1_BasicZeroSlot = @"
namespace Precept.Language
{
    public enum ActionKind { Set, Add, Remove }
}";

    [Fact]
    public async Task TP1_BasicZeroMember_Reports()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0018SemanticEnumZeroSlot>(TP1_BasicZeroSlot);
        diagnostics.Should().ContainSingle();
        var d = diagnostics[0];
        d.Id.Should().Be("PRECEPT0018");
        d.Severity.Should().Be(Microsoft.CodeAnalysis.DiagnosticSeverity.Error);
        d.GetMessage().Should().Contain("Set");
        d.GetMessage().Should().Contain("ActionKind");
    }

    /// <summary>
    /// TP2: Nested namespace still in Precept.* hierarchy — flagged.
    /// </summary>
    private const string TP2_NestedNamespace = @"
namespace Precept.Pipeline
{
    public enum LexerMode { Normal, String, Interpolation }
}";

    [Fact]
    public async Task TP2_NestedPreceptNamespace_Reports()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0018SemanticEnumZeroSlot>(TP2_NestedNamespace);
        diagnostics.Should().ContainSingle();
        var d = diagnostics[0];
        d.Id.Should().Be("PRECEPT0018");
        d.GetMessage().Should().Contain("Normal");
        d.GetMessage().Should().Contain("LexerMode");
    }

    /// <summary>
    /// TP3: Enum directly in Precept namespace (not nested) — flagged.
    /// </summary>
    private const string TP3_RootPreceptNamespace = @"
namespace Precept
{
    public enum SomeKind { First, Second }
}";

    [Fact]
    public async Task TP3_RootPreceptNamespace_Reports()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0018SemanticEnumZeroSlot>(TP3_RootPreceptNamespace);
        diagnostics.Should().ContainSingle();
        diagnostics[0].GetMessage().Should().Contain("First");
        diagnostics[0].GetMessage().Should().Contain("SomeKind");
    }

    /// <summary>
    /// TP4: Multiple zero-valued members in one enum — each flagged separately.
    /// </summary>
    private const string TP4_MultipleZeroMembers = @"
namespace Precept.Language
{
    public enum WeirdEnum { A = 0, B = 0, C = 1 }
}";

    [Fact]
    public async Task TP4_MultipleZeroValuedMembers_ReportsEach()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0018SemanticEnumZeroSlot>(TP4_MultipleZeroMembers);
        diagnostics.Should().HaveCount(2);
        diagnostics.Should().OnlyContain(d => d.Id == "PRECEPT0018");
    }

    /// <summary>
    /// TP5: Explicit zero assignment is flagged just like implicit zero.
    /// </summary>
    private const string TP5_ExplicitZeroAssignment = @"
namespace Precept.Language
{
    public enum FaultCode { DivisionByZero = 0, TypeMismatch = 1 }
}";

    [Fact]
    public async Task TP5_ExplicitZeroAssignment_Reports()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0018SemanticEnumZeroSlot>(TP5_ExplicitZeroAssignment);
        diagnostics.Should().ContainSingle();
        diagnostics[0].GetMessage().Should().Contain("DivisionByZero");
    }

    /// <summary>
    /// TP6: Diagnostic message includes both member name and enum type name.
    /// </summary>
    [Fact]
    public async Task TP6_DiagnosticMessage_ContainsMemberAndTypeName()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0018SemanticEnumZeroSlot>(TP1_BasicZeroSlot);
        diagnostics.Should().ContainSingle();
        var msg = diagnostics[0].GetMessage();
        msg.Should().Contain("Set");
        msg.Should().Contain("ActionKind");
        msg.Should().Contain("value 0");
    }

    // ── True negatives ────────────────────────────────────────────────────────

    /// <summary>
    /// TN1: Enum named "None = 0" is the structural-sentinel exemption.
    /// </summary>
    private const string TN1_NoneNamedMember = @"
namespace Precept.Language
{
    public enum GraphAnalysisKind { None = 0, IncomingEdge, Reachability }
}";

    [Fact]
    public async Task TN1_NoneNamedMember_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0018SemanticEnumZeroSlot>(TN1_NoneNamedMember);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// TN2: [Flags] enum is entirely exempt regardless of zero-named members.
    /// </summary>
    private const string TN2_FlagsEnum = @"
namespace Precept.Language
{
    [System.Flags]
    public enum TypeTrait { None = 0, Orderable = 1, EqualityComparable = 2 }
}";

    [Fact]
    public async Task TN2_FlagsEnum_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0018SemanticEnumZeroSlot>(TN2_FlagsEnum);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// TN3: [AllowZeroDefault] on the zero member suppresses the diagnostic.
    /// </summary>
    private const string TN3_AllowZeroDefault = @"
namespace Precept.Language
{
    public enum QualifierMatch
    {
        [Precept.AllowZeroDefault] Any,
        Same,
        Different,
    }
}
namespace Precept
{
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public sealed class AllowZeroDefaultAttribute : System.Attribute { }
}";

    [Fact]
    public async Task TN3_AllowZeroDefaultAttribute_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0018SemanticEnumZeroSlot>(TN3_AllowZeroDefault);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// TN4: Enum with all 1-based values — no zero slot, no diagnostic.
    /// </summary>
    private const string TN4_OneBased = @"
namespace Precept.Language
{
    public enum TokenKind { Precept = 1, Field = 2, State = 3 }
}";

    [Fact]
    public async Task TN4_OneBasedEnum_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0018SemanticEnumZeroSlot>(TN4_OneBased);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// TN5: Enum in a completely unrelated namespace — no diagnostic.
    /// </summary>
    private const string TN5_WrongNamespace = @"
namespace MyCompany.MyApp
{
    public enum StatusKind { Active, Inactive }
}";

    [Fact]
    public async Task TN5_WrongNamespace_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0018SemanticEnumZeroSlot>(TN5_WrongNamespace);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// TN6: Enum in a namespace that contains "Precept" but not as a segment — no diagnostic.
    /// </summary>
    private const string TN6_PreceptInNameButNotSegment = @"
namespace NotPrecept.Language
{
    public enum SomeKind { First, Second }
}";

    [Fact]
    public async Task TN6_PreceptInMiddleOfSegmentName_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0018SemanticEnumZeroSlot>(TN6_PreceptInNameButNotSegment);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// TN7: Global namespace enum — no diagnostic (not in Precept.*).
    /// </summary>
    private const string TN7_GlobalNamespace = @"
public enum GlobalKind { Alpha, Beta }";

    [Fact]
    public async Task TN7_GlobalNamespaceEnum_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0018SemanticEnumZeroSlot>(TN7_GlobalNamespace);
        diagnostics.Should().BeEmpty();
    }

    // ── Edge cases ────────────────────────────────────────────────────────────

    /// <summary>
    /// EC1: Empty enum — no members, no diagnostic.
    /// </summary>
    private const string EC1_EmptyEnum = @"
namespace Precept.Language
{
    public enum EmptyKind { }
}";

    [Fact]
    public async Task EC1_EmptyEnum_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0018SemanticEnumZeroSlot>(EC1_EmptyEnum);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// EC2: Enum with only non-zero explicit values — no diagnostic.
    /// </summary>
    private const string EC2_AllNonZero = @"
namespace Precept.Language
{
    public enum SeverityKind { Info = 1, Warning = 2, Error = 3 }
}";

    [Fact]
    public async Task EC2_AllNonZeroValues_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0018SemanticEnumZeroSlot>(EC2_AllNonZero);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// EC3: [Flags] enum with an explicit zero-named something-other-than-None — still
    /// exempt because [Flags] takes precedence.
    /// </summary>
    private const string EC3_FlagsWithZeroNotNamed = @"
namespace Precept.Language
{
    [System.Flags]
    public enum Options { Empty = 0, A = 1, B = 2 }
}";

    [Fact]
    public async Task EC3_FlagsEnumWithZeroNotNone_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0018SemanticEnumZeroSlot>(EC3_FlagsWithZeroNotNamed);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// EC4: Non-enum type (class) in Precept.* — no diagnostic.
    /// </summary>
    private const string EC4_NonEnumType = @"
namespace Precept.Language
{
    public class NotAnEnum { public int Value; }
}";

    [Fact]
    public async Task EC4_NonEnumClass_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0018SemanticEnumZeroSlot>(EC4_NonEnumType);
        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// EC5: None = 0 co-exists with other zero-valued aliases — only the non-None zero is flagged.
    /// </summary>
    private const string EC5_NoneAndOtherZero = @"
namespace Precept.Language
{
    public enum MixedKind { None = 0, AlsoZero = 0, One = 1 }
}";

    [Fact]
    public async Task EC5_NoneExemptButOtherZeroFlagged()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<PRECEPT0018SemanticEnumZeroSlot>(EC5_NoneAndOtherZero);
        diagnostics.Should().ContainSingle();
        diagnostics[0].GetMessage().Should().Contain("AlsoZero");
        diagnostics[0].GetMessage().Should().NotContain("None");
    }
}
