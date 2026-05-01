using FluentAssertions;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Precept.Analyzers.Tests;

/// <summary>
/// Tests for PRECEPT0019 — Pipeline Coverage Exhaustiveness.
/// Verifies that classes marked with [HandlesCatalogExhaustively(typeof(T))]
/// emit PRECEPT0019 when any member of T lacks a [HandlesCatalogMember] annotation.
/// </summary>
public class Precept0019Tests
{
    // ── Shared attribute stubs ────────────────────────────────────────────────
    // Each test compilation is standalone; attribute types must be defined inline.

    private const string AttributeStubs = @"
using System;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
public sealed class HandlesCatalogExhaustivelyAttribute : Attribute
{
    public HandlesCatalogExhaustivelyAttribute(Type catalogEnum) => CatalogEnum = catalogEnum;
    public Type CatalogEnum { get; }
}

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
public sealed class HandlesCatalogMemberAttribute : Attribute
{
    public HandlesCatalogMemberAttribute(object kind) => Kind = kind;
    public object Kind { get; }
}

public enum WidgetKind { Alpha = 1, Beta = 2, Gamma = 3 }
";

    // ── True positives ────────────────────────────────────────────────────────

    /// <summary>
    /// TP1: Class missing [HandlesCatalogMember] for two of three enum members → PRECEPT0019.
    /// </summary>
    private const string TP1_MissingHandlers = @"
[HandlesCatalogExhaustively(typeof(WidgetKind))]
public class MyPipeline
{
    [HandlesCatalogMember(WidgetKind.Alpha)]
    public void HandleAlpha() { }
    // Beta and Gamma have no [HandlesCatalogMember] annotation
}
";

    [Fact]
    public async Task TP1_MissingFormHandlers_Reports()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0019PipelineCoverageExhaustiveness>(
            AttributeStubs, TP1_MissingHandlers);

        diagnostics.Should().ContainSingle();
        var d = diagnostics[0];
        d.Id.Should().Be("PRECEPT0019");
        d.Severity.Should().Be(DiagnosticSeverity.Error);
        d.GetMessage().Should().Contain("MyPipeline");
        d.GetMessage().Should().Contain("Beta");
        d.GetMessage().Should().Contain("Gamma");
    }

    /// <summary>
    /// TP2: Struct missing all [HandlesCatalogMember] annotations → PRECEPT0019.
    /// Verifies the attribute works on structs (ParseSession pattern).
    /// </summary>
    private const string TP2_StructMissingHandlers = @"
[HandlesCatalogExhaustively(typeof(WidgetKind))]
public struct MyParseSession
{
    // No [HandlesCatalogMember] annotations at all
    public void ParseSomething() { }
}
";

    [Fact]
    public async Task TP2_StructMissingHandlers_Reports()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0019PipelineCoverageExhaustiveness>(
            AttributeStubs, TP2_StructMissingHandlers);

        diagnostics.Should().ContainSingle();
        var d = diagnostics[0];
        d.Id.Should().Be("PRECEPT0019");
        d.Severity.Should().Be(DiagnosticSeverity.Error);
        d.GetMessage().Should().Contain("MyParseSession");
        d.GetMessage().Should().Contain("3"); // 3 missing members
    }

    // ── True negatives ────────────────────────────────────────────────────────

    /// <summary>
    /// TN1: All enum members have at least one [HandlesCatalogMember] method → no PRECEPT0019.
    /// </summary>
    private const string TN1_AllHandled = @"
[HandlesCatalogExhaustively(typeof(WidgetKind))]
public class MyPipeline
{
    [HandlesCatalogMember(WidgetKind.Alpha)]
    public void HandleAlpha() { }

    [HandlesCatalogMember(WidgetKind.Beta)]
    public void HandleBeta() { }

    [HandlesCatalogMember(WidgetKind.Gamma)]
    public void HandleGamma() { }
}
";

    [Fact]
    public async Task TN1_AllFormHandled_NoDiagnostics()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0019PipelineCoverageExhaustiveness>(
            AttributeStubs, TN1_AllHandled);

        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// TN2: Multiple [HandlesCatalogMember] on a single method — all covered → no PRECEPT0019.
    /// </summary>
    private const string TN2_MultipleAnnotationsOnOneMethod = @"
[HandlesCatalogExhaustively(typeof(WidgetKind))]
public class MyPipeline
{
    [HandlesCatalogMember(WidgetKind.Alpha)]
    [HandlesCatalogMember(WidgetKind.Beta)]
    [HandlesCatalogMember(WidgetKind.Gamma)]
    public void HandleAll() { }
}
";

    [Fact]
    public async Task TN2_MultipleAnnotationsOnOneMethod_NoDiagnostics()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0019PipelineCoverageExhaustiveness>(
            AttributeStubs, TN2_MultipleAnnotationsOnOneMethod);

        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// TN3: Class without [HandlesCatalogExhaustively] is never checked → no PRECEPT0019.
    /// </summary>
    private const string TN3_NoClassMarker = @"
public class UnannotatedPipeline
{
    // No [HandlesCatalogExhaustively] — analyzer must not fire
    public void DoNothing() { }
}
";

    [Fact]
    public async Task TN3_NoClassMarker_NoDiagnostics()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0019PipelineCoverageExhaustiveness>(
            AttributeStubs, TN3_NoClassMarker);

        diagnostics.Should().BeEmpty();
    }
}

