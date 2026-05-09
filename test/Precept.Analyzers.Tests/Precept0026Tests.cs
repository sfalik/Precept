using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Xunit;

namespace Precept.Analyzers.Tests;

/// <summary>
/// Tests for PRECEPT0026 — CatalogDU switch arm completeness.
///
/// Verifies that switches over a [CatalogDU] hierarchy must contain an explicit
/// type-pattern arm for each sealed subtype in the current compilation.
/// </summary>
public class Precept0026Tests
{
    private const string CommonStubs = @"
using System;

[AttributeUsage(AttributeTargets.Class)]
public sealed class CatalogDUAttribute : Attribute { }

[CatalogDU]
public abstract record Shape;
public sealed record Circle : Shape;
public sealed record Square : Shape;
public sealed record Triangle : Shape;
";

    private const string TP1_SwitchStatementMissingArm = @"
public class Consumer
{
    public string Describe(Shape shape)
    {
        switch (shape)
        {
            case Circle circle:
                return nameof(circle);
            case Square square:
                return nameof(square);
        }

        return ""fallback"";
    }
}
";

    [Fact]
    public async Task TP1_SwitchStatementMissingArm_Reports()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0026CatalogDUCompleteness>(
            CommonStubs, TP1_SwitchStatementMissingArm);

        diagnostics.Should().ContainSingle();
        var diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be("PRECEPT0026");
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Error);
        diagnostic.GetMessage().Should().Contain("Shape");
        diagnostic.GetMessage().Should().Contain("Triangle");
    }

    private const string TP2_SwitchExpressionMissingArm = @"
public class Consumer
{
    public string Describe(Shape shape) => shape switch
    {
        Circle => ""circle"",
        Square => ""square""
    };
}
";

    [Fact]
    public async Task TP2_SwitchExpressionMissingArm_Reports()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0026CatalogDUCompleteness>(
            CommonStubs, TP2_SwitchExpressionMissingArm);

        diagnostics.Should().ContainSingle();
        var diagnostic = diagnostics[0];
        diagnostic.Id.Should().Be("PRECEPT0026");
        diagnostic.Severity.Should().Be(DiagnosticSeverity.Error);
        diagnostic.GetMessage().Should().Contain("Triangle");
    }

    private const string TP3_MultipleMissingArms = @"
public class Consumer
{
    public string Describe(Shape shape)
    {
        switch (shape)
        {
            case Circle circle:
                return nameof(circle);
        }

        return ""fallback"";
    }
}
";

    [Fact]
    public async Task TP3_MultipleMissingArms_ReportIndependently()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0026CatalogDUCompleteness>(
            CommonStubs, TP3_MultipleMissingArms);

        diagnostics.Should().HaveCount(2);
        diagnostics.Select(d => d.GetMessage()).Should().Contain(message => message.Contains("Square"));
        diagnostics.Select(d => d.GetMessage()).Should().Contain(message => message.Contains("Triangle"));
    }

    private const string TN1_AllSubtypesHandled = @"
using System;

public class Consumer
{
    public string Describe(Shape shape)
    {
        switch (shape)
        {
            case Circle circle:
                return nameof(circle);
            case Square square:
                return nameof(square);
            case Triangle triangle:
                return nameof(triangle);
        }

        throw new InvalidOperationException();
    }
}
";

    [Fact]
    public async Task TN1_AllSubtypesHandled_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0026CatalogDUCompleteness>(
            CommonStubs, TN1_AllSubtypesHandled);

        diagnostics.Should().BeEmpty();
    }

    private const string TN2_NonCatalogDUType = @"
public abstract record Vehicle;
public sealed record Car : Vehicle;
public sealed record Bike : Vehicle;

public class Consumer
{
    public string Describe(Vehicle vehicle)
    {
        switch (vehicle)
        {
            case Car car:
                return nameof(car);
        }

        return ""fallback"";
    }
}
";

    [Fact]
    public async Task TN2_NonCatalogDUType_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0026CatalogDUCompleteness>(
            TN2_NonCatalogDUType);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task TN3_TestFile_Suppressed()
    {
        const string source = @"
using System;

[AttributeUsage(AttributeTargets.Class)]
public sealed class CatalogDUAttribute : Attribute { }

[CatalogDU]
public abstract record Widget;
public sealed record Gizmo : Widget;
public sealed record Gadget : Widget;

public class WidgetTests
{
    public string Describe(Widget widget)
    {
        switch (widget)
        {
            case Gizmo gizmo:
                return nameof(gizmo);
        }

        return ""fallback"";
    }
}
";

        var diagnostics = await AnalyzerTestHelper.AnalyzeWithFilePathAsync<Precept0026CatalogDUCompleteness>(
            source,
            "Widget.Tests.cs");

        diagnostics.Should().BeEmpty();
    }
}
