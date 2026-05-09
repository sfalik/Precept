using System.Collections.Immutable;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace Precept.Analyzers.Tests;

/// <summary>
/// Tests for PRECEPT0025 — CatalogDU Wildcard Prohibition.
///
/// Verifies that wildcard/discard arms in type-pattern switch expressions over
/// abstract records marked with [CatalogDU] emit PRECEPT0025 at Error severity,
/// while exhaustive switches and non-[CatalogDU] types stay silent.
/// </summary>
public class Precept0025Tests
{
    // ── Shared stubs ──────────────────────────────────────────────────────────
    // Minimal type definitions that give the analyzer a compilable [CatalogDU]
    // hierarchy to work with. Each test compilation is self-contained.

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

    // ── True positives ────────────────────────────────────────────────────────

    /// <summary>
    /// TP1: Switch with pure discard arm (_ =>) over [CatalogDU] type → PRECEPT0025.
    /// The canonical case: adding a new subtype is silently absorbed.
    /// </summary>
    private const string TP1_DiscardArm = @"
public class Consumer
{
    public string Describe(Shape shape) => shape switch
    {
        Circle  => ""circle"",
        Square  => ""square"",
        _       => ""unknown""
    };
}
";

    [Fact]
    public async Task TP1_DiscardArm_OverCatalogDUType_Reports()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0025CatalogDUWildcard>(
            CommonStubs, TP1_DiscardArm);

        diagnostics.Should().ContainSingle();
        var d = diagnostics[0];
        d.Id.Should().Be("PRECEPT0025");
        d.Severity.Should().Be(DiagnosticSeverity.Error);
        d.GetMessage().Should().Contain("Shape");
        d.GetMessage().Should().ContainEquivalentOf("wildcard");
    }

    /// <summary>
    /// TP2: Switch with abstract-base binding declaration pattern (Shape x =>) → PRECEPT0025.
    /// A named binding over the base type is still a catch-all.
    /// </summary>
    private const string TP2_DeclarationCatchAll = @"
public class Consumer
{
    public string Describe(Shape shape) => shape switch
    {
        Circle    => ""circle"",
        Square    => ""square"",
        Shape x   => ""fallback""
    };
}
";

    [Fact]
    public async Task TP2_DeclarationPattern_OverAbstractBase_Reports()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0025CatalogDUWildcard>(
            CommonStubs, TP2_DeclarationCatchAll);

        diagnostics.Should().ContainSingle();
        var d = diagnostics[0];
        d.Id.Should().Be("PRECEPT0025");
        d.Severity.Should().Be(DiagnosticSeverity.Error);
        d.GetMessage().Should().Contain("Shape");
    }

    /// <summary>
    /// TP3: Both a catch-all declaration arm AND a discard arm in the same switch → two PRECEPT0025.
    /// Each offending arm is reported independently.
    /// </summary>
    private const string TP3_MultipleOffendingArms = @"
public class Consumer
{
    public string Describe(Shape shape) => shape switch
    {
        Circle    => ""circle"",
        Shape x   => ""other shape"",
        _         => ""fallback""
    };
}
";

    [Fact]
    public async Task TP3_MultipleWildcardArms_ReportsEach()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0025CatalogDUWildcard>(
            CommonStubs, TP3_MultipleOffendingArms);

        diagnostics.Should().HaveCount(2);
        diagnostics.Should().AllSatisfy(d =>
        {
            d.Id.Should().Be("PRECEPT0025");
            d.Severity.Should().Be(DiagnosticSeverity.Error);
            d.GetMessage().Should().Contain("Shape");
        });
    }

    /// <summary>
    /// TP4: Switch is over a concrete subtype variable (Circle c), but the switch
    /// input's declared type walks up to Shape which has [CatalogDU]. Still fires.
    /// </summary>
    private const string TP4_SwitchOverDerivedType = @"
public class Consumer
{
    public string Describe(Circle c) => c switch
    {
        Circle cc => ""a circle"",
        _         => ""impossible""
    };
}
";

    [Fact]
    public async Task TP4_SwitchOverDerivedType_WalksHierarchyAndReports()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0025CatalogDUWildcard>(
            CommonStubs, TP4_SwitchOverDerivedType);

        // Circle's base is Shape which has [CatalogDU], so the discard fires.
        diagnostics.Should().ContainSingle();
        var d = diagnostics[0];
        d.Id.Should().Be("PRECEPT0025");
        // Message names the [CatalogDU] base, not the switch expression's concrete type.
        d.GetMessage().Should().Contain("Shape");
    }

    // ── True negatives ────────────────────────────────────────────────────────

    /// <summary>
    /// TN1: Switch WITHOUT any wildcard arm and all subtypes covered → no diagnostic.
    /// This is the target state that PRECEPT0025 enforces.
    /// </summary>
    private const string TN1_ExhaustiveSwitch = @"
public class Consumer
{
    public string Describe(Shape shape) => shape switch
    {
        Circle   => ""circle"",
        Square   => ""square"",
        Triangle => ""triangle""
    };
}
";

    [Fact]
    public async Task TN1_ExhaustiveSwitch_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0025CatalogDUWildcard>(
            CommonStubs, TN1_ExhaustiveSwitch);

        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// TN2: Switch with _ arm over a type that does NOT have [CatalogDU] → no diagnostic.
    /// PRECEPT0025 is scoped to [CatalogDU]-marked hierarchies only.
    /// </summary>
    private const string TN2_NonCatalogDUType = @"
using System;

// No [CatalogDU] attribute — plain abstract record
public abstract record Vehicle;
public sealed record Car  : Vehicle;
public sealed record Bike : Vehicle;

public class Consumer
{
    public string Describe(Vehicle v) => v switch
    {
        Car  => ""car"",
        _    => ""other""
    };
}
";

    [Fact]
    public async Task TN2_DiscardArm_OverNonCatalogDUType_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0025CatalogDUWildcard>(
            TN2_NonCatalogDUType);

        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// TN3: Specific subtype patterns are not wildcards; a discard arm on a concrete
    /// subtype switch (Circle c when c != null =>) does not fire. Only the _ arm fires.
    /// Verifies the analyzer doesn't over-report.
    /// </summary>
    private const string TN3_GuardedPatternNotWildcard = @"
public class Consumer
{
    public string Describe(Shape shape) => shape switch
    {
        Circle c  => ""circle"",
        Square    => ""square"",
        Triangle  => ""triangle""
    };
}
";

    [Fact]
    public async Task TN3_GuardedAndConcretePatterns_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0025CatalogDUWildcard>(
            CommonStubs, TN3_GuardedPatternNotWildcard);

        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// TN4: _ arm in a switch over a non-record type (plain enum) → no diagnostic.
    /// PRECEPT0025 only guards [CatalogDU]-marked types.
    /// </summary>
    private const string TN4_DiscardOnEnum = @"
using System;

public enum Color { Red, Green, Blue }

public class Consumer
{
    public string Describe(Color c) => c switch
    {
        Color.Red   => ""red"",
        Color.Green => ""green"",
        _           => ""other""
    };
}
";

    [Fact]
    public async Task TN4_DiscardArm_OnEnum_NoDiagnostic()
    {
        var diagnostics = await AnalyzerTestHelper.AnalyzeAsync<Precept0025CatalogDUWildcard>(
            TN4_DiscardOnEnum);

        diagnostics.Should().BeEmpty();
    }

    /// <summary>
    /// TN5: _ arm in a file whose path contains ".Tests" → suppressed.
    /// PRECEPT0025 is suppressed in test files to permit scaffolded partial switches.
    /// </summary>
    [Fact]
    public async Task TN5_DiscardArm_InTestFile_Suppressed()
    {
        const string source = @"
using System;

[AttributeUsage(AttributeTargets.Class)]
public sealed class CatalogDUAttribute : Attribute { }

[CatalogDU]
public abstract record Widget;
public sealed record Gizmo  : Widget;
public sealed record Gadget : Widget;

public class WidgetTests
{
    public string Describe(Widget w) => w switch
    {
        Gizmo  => ""gizmo"",
        _      => ""fallback""
    };
}
";
        // Compile with a file path that contains ".Tests" to trigger suppression.
        var diagnostics = await AnalyzeWithFilePathAsync(source, "Widget.Tests.cs");

        diagnostics.Should().BeEmpty();
    }

    // ── Helper for file-path-based tests ─────────────────────────────────────

    /// <summary>
    /// Runs <see cref="Precept0025CatalogDUWildcard"/> against <paramref name="source"/>
    /// compiled under the specified <paramref name="filePath"/>. Used to test suppression
    /// behavior that depends on the file's path containing ".Tests".
    /// </summary>
    private static async Task<IReadOnlyList<Diagnostic>> AnalyzeWithFilePathAsync(
        string source, string filePath)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source, path: filePath);

        var dotnetDir = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        };
        var systemRuntime = Path.Combine(dotnetDir, "System.Runtime.dll");
        if (File.Exists(systemRuntime))
            references.Add(MetadataReference.CreateFromFile(systemRuntime));

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var compilerErrors = compilation.GetDiagnostics()
            .Where(d => d.Severity == DiagnosticSeverity.Error)
            .ToList();

        if (compilerErrors.Count > 0)
        {
            var messages = string.Join("\n", compilerErrors.Select(d => $"  {d.Id}: {d.GetMessage()}"));
            throw new InvalidOperationException(
                $"Test source has {compilerErrors.Count} compiler error(s):\n{messages}");
        }

        var withAnalyzers = compilation.WithAnalyzers(
            ImmutableArray.Create<DiagnosticAnalyzer>(new Precept0025CatalogDUWildcard()));

        return (await withAnalyzers.GetAnalyzerDiagnosticsAsync())
            .Where(d => d.Severity != DiagnosticSeverity.Hidden)
            .OrderBy(d => d.Location.SourceSpan.Start)
            .ToList();
    }
}
