using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Precept;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Drift defense for the proof-engine sample catalog.
///
/// Root samples (samples/*.precept): must parse and compile without errors.
/// Diagnostic samples (samples/diagnostics/*.precept): must compile and produce
    /// exactly the diagnostic codes declared in their <c># Demonstrates: Cxx</c> header.
/// A discovery test fails if any sample file lacks the header.
///
/// Cross-surface consistency (D9):
///   computed-tax-net.precept demonstrates consistent proof data across hover,
///   diagnostics, and MCP precept_compile output. Verified by inspection.
///
/// See temp/specs/step-15.md and CONTRIBUTING.md § Diagnostic Samples.
/// </summary>
public class DiagnosticSampleDriftTests
{
    // ════════════════════════════════════════════════════════════════════
    // Helpers
    // ════════════════════════════════════════════════════════════════════

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir, "Precept.slnx")))
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        throw new InvalidOperationException("Could not find repo root (Precept.slnx)");
    }

    private static string SamplesDir => Path.Combine(FindRepoRoot(), "samples");
    private static string DiagnosticSamplesDir => Path.Combine(SamplesDir, "diagnostics");

    // ════════════════════════════════════════════════════════════════════
    // Test 1: Root sample file count guard
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void SampleDirectory_ContainsExpectedFileCount()
    {
        var files = Directory.GetFiles(SamplesDir, "*.precept");
        files.Length.Should().BeGreaterThanOrEqualTo(27,
            "samples/ should contain the baseline 25 files plus the Step 15 proof samples");
    }

    // ════════════════════════════════════════════════════════════════════
    // Test 2: Root samples parse without errors
    // ════════════════════════════════════════════════════════════════════

    public static TheoryData<string> AllSampleFiles()
    {
        var data = new TheoryData<string>();
        var samplesDir = Path.Combine(FindRepoRoot(), "samples");
        foreach (var file in Directory.GetFiles(samplesDir, "*.precept"))
            data.Add(Path.GetFileName(file));
        return data;
    }

    [Theory]
    [MemberData(nameof(AllSampleFiles))]
    public void SampleFile_ParsesWithoutErrors(string fileName)
    {
        var filePath = Path.Combine(SamplesDir, fileName);
        var dsl = File.ReadAllText(filePath);

        var (model, diags) = PreceptParser.ParseWithDiagnostics(dsl);

        diags.Should().BeEmpty(
            $"sample file '{fileName}' must parse without errors");
        model.Should().NotBeNull(
            $"sample file '{fileName}' must produce a valid model");
    }

    // ════════════════════════════════════════════════════════════════════
    // Test 3: Root samples compile without errors
    // ════════════════════════════════════════════════════════════════════

    [Theory]
    [MemberData(nameof(AllSampleFiles))]
    public void SampleFile_CompilesWithoutErrors(string fileName)
    {
        var filePath = Path.Combine(SamplesDir, fileName);
        var dsl = File.ReadAllText(filePath);

        var model = PreceptParser.Parse(dsl);
        var validation = PreceptCompiler.Validate(model);

        var errors = validation.Diagnostics
            .Where(d => d.Constraint.Severity == ConstraintSeverity.Error)
            .ToList();
        errors.Should().BeEmpty(
            $"sample file '{fileName}' must compile without errors (warnings OK)");
    }

    // ════════════════════════════════════════════════════════════════════
    // Proof-demonstrating root samples — gap coverage (Step 15 spec §2–§4)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void ComputedTaxNetSample_ExistsAndCompiles()
    {
        AssertSampleExistsAndCompiles("computed-tax-net.precept");
    }

    [Fact]
    public void TransitiveOrderingSample_ExistsAndCompiles()
    {
        AssertSampleExistsAndCompiles("transitive-ordering.precept");
    }

    private static void AssertSampleExistsAndCompiles(string fileName)
    {
        var filePath = Path.Combine(SamplesDir, fileName);
        File.Exists(filePath).Should().BeTrue(
            $"sample file '{fileName}' must exist in samples/");

        var dsl = File.ReadAllText(filePath);
        var model = PreceptParser.Parse(dsl);
        var validation = PreceptCompiler.Validate(model);

        var errors = validation.Diagnostics
            .Where(d => d.Constraint.Severity == ConstraintSeverity.Error)
            .ToList();
        errors.Should().BeEmpty(
            $"sample file '{fileName}' must compile without errors");
    }

    // ════════════════════════════════════════════════════════════════════
    // Test 4: Diagnostic samples directory exists
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void DiagnosticSamplesDirectory_Exists()
    {
        Directory.Exists(DiagnosticSamplesDir).Should().BeTrue(
            "samples/diagnostics/ must exist and contain the proof-engine diagnostic catalog");
    }

    // ════════════════════════════════════════════════════════════════════
    // Test 5: Each diagnostic sample has a // Demonstrates: header
    // ════════════════════════════════════════════════════════════════════

    public static TheoryData<string> AllDiagnosticSampleFiles()
    {
        var data = new TheoryData<string>();
        var dir = Path.Combine(FindRepoRoot(), "samples", "diagnostics");
        if (!Directory.Exists(dir)) return data;
        foreach (var file in Directory.GetFiles(dir, "*.precept"))
            data.Add(Path.GetFileName(file));
        return data;
    }

    [Theory]
    [MemberData(nameof(AllDiagnosticSampleFiles))]
    public void DiagnosticSample_HasDemonstratesHeader(string fileName)
    {
        var filePath = Path.Combine(DiagnosticSamplesDir, fileName);
        var rawText = File.ReadAllText(filePath);

        var headerLine = FindDemonstratesHeader(rawText);
        headerLine.Should().NotBeNull(
            $"diagnostic sample '{fileName}' must have a '# Demonstrates: Cxx[, Cyy...]' header");

        ParseDeclaredCodes(headerLine!).Should().NotBeEmpty(
            $"diagnostic sample '{fileName}' '# Demonstrates:' header must list at least one code");
    }

    // ════════════════════════════════════════════════════════════════════
    // Test 6: Each diagnostic sample emits declared codes, no unexpected errors
    // ════════════════════════════════════════════════════════════════════

    [Theory]
    [MemberData(nameof(AllDiagnosticSampleFiles))]
    public void DiagnosticSample_EmitsDeclaredCodes_AndNoUnexpectedErrors(string fileName)
    {
        var filePath = Path.Combine(DiagnosticSamplesDir, fileName);
        var rawText = File.ReadAllText(filePath);

        var headerLine = FindDemonstratesHeader(rawText);
        headerLine.Should().NotBeNull(
            $"diagnostic sample '{fileName}' must have a '# Demonstrates:' header");

        var declaredCodes = ParseDeclaredCodes(headerLine!);
        var preceptCodes = declaredCodes
            .Select(DiagnosticCatalog.ToDiagnosticCode)
            .ToHashSet(StringComparer.Ordinal);

        var result = PreceptCompiler.CompileFromText(rawText);

        // Every declared code must appear in the compilation diagnostics
        foreach (var code in preceptCodes)
        {
            result.Diagnostics.Any(d => d.Code == code).Should().BeTrue(
                $"diagnostic sample '{fileName}' must emit {code} (declared in // Demonstrates: header)");
        }

        // No error-severity diagnostics beyond those declared in the header
        var unexpectedErrors = result.Diagnostics
            .Where(d => d.Severity == ConstraintSeverity.Error && !preceptCodes.Contains(d.Code!))
            .ToList();
        unexpectedErrors.Should().BeEmpty(
            $"diagnostic sample '{fileName}' must not emit error-severity diagnostics beyond those declared in its header");
    }

    // ════════════════════════════════════════════════════════════════════
    // Helpers for diagnostic sample discovery
    // ════════════════════════════════════════════════════════════════════

    private static string? FindDemonstratesHeader(string text)
    {
        foreach (var line in text.Split('\n'))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("# Demonstrates:", StringComparison.OrdinalIgnoreCase))
                return trimmed;
        }
        return null;
    }

    private static string[] ParseDeclaredCodes(string headerLine)
    {
        // "# Demonstrates: C92, C93 — some description" → ["C92", "C93"]
        var colon = headerLine.IndexOf("Demonstrates:", StringComparison.OrdinalIgnoreCase);
        var after = headerLine.Substring(colon + "Demonstrates:".Length);
        // Strip any trailing description after an em-dash
        var dash = after.IndexOf('—');
        if (dash >= 0) after = after.Substring(0, dash);
        return after.Split(',')
            .Select(c => c.Trim())
            .Where(c => c.Length > 0 && c.StartsWith("C", StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

}
