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
/// Diagnostic samples (test/integrationtests/diagnostics/*.precept): must compile and produce
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
    private static string DiagnosticSamplesDir => Path.Combine(FindRepoRoot(), "test", "integrationtests", "diagnostics");

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
            "test/integrationtests/diagnostics/ must exist and contain the proof-engine diagnostic catalog");
    }

    // ════════════════════════════════════════════════════════════════════
    // Test 5: Each diagnostic sample has a # Demonstrates: header
    // ════════════════════════════════════════════════════════════════════

    public static TheoryData<string> AllDiagnosticSampleFiles()
    {
        var data = new TheoryData<string>();
        var dir = Path.Combine(FindRepoRoot(), "test", "integrationtests", "diagnostics");
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
                $"diagnostic sample '{fileName}' must emit {code} (declared in # Demonstrates: header)");
        }

        // No error-severity diagnostics beyond those declared in the header
        var unexpectedErrors = result.Diagnostics
            .Where(d => d.Severity == ConstraintSeverity.Error && !preceptCodes.Contains(d.Code!))
            .ToList();
        unexpectedErrors.Should().BeEmpty(
            $"diagnostic sample '{fileName}' must not emit error-severity diagnostics beyond those declared in its header");
    }

    // ════════════════════════════════════════════════════════════════════
    // Test 7: EXPECT annotations cover all Demonstrates codes
    // ════════════════════════════════════════════════════════════════════

    [Theory]
    [MemberData(nameof(AllDiagnosticSampleFiles))]
    public void DiagnosticSample_ExpectAnnotations_CoverDemonstratedCodes(string fileName)
    {
        var filePath = Path.Combine(DiagnosticSamplesDir, fileName);
        var rawText = File.ReadAllText(filePath);

        var headerLine = FindDemonstratesHeader(rawText);
        if (headerLine is null) return; // Caught by HasDemonstratesHeader test

        var declaredCodes = ParseDeclaredCodes(headerLine)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (declaredCodes.Count == 0) return;

        var expectCodes = ParseExpectations(rawText)
            .Select(e => e.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var code in declaredCodes)
        {
            expectCodes.Should().Contain(code,
                $"'{fileName}': '# Demonstrates: {code}' must have at least one '# EXPECT: {code}' annotation");
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // Test 8: Per-line EXPECT annotations match actual diagnostics
    // ════════════════════════════════════════════════════════════════════

    [Theory]
    [MemberData(nameof(AllDiagnosticSampleFiles))]
    public void DiagnosticSample_PerLineExpectations_MatchDiagnostics(string fileName)
    {
        var filePath = Path.Combine(DiagnosticSamplesDir, fileName);
        var rawText = File.ReadAllText(filePath);

        var expectations = ParseExpectations(rawText);
        if (expectations.Count == 0)
            return; // No EXPECT annotations — nothing to verify

        var result = PreceptCompiler.CompileFromText(rawText);

        foreach (var exp in expectations)
        {
            var preceptCode = DiagnosticCatalog.ToDiagnosticCode(exp.Code);

            var onLine = result.Diagnostics
                .Where(d => d.Code == preceptCode && d.Line == exp.ExecutableLine)
                .ToList();

            onLine.Should().NotBeEmpty(
                $"'{fileName}': expected {exp.Code} on line {exp.ExecutableLine} (contains='{exp.Contains}')," +
                $" but found none. All diagnostics: [{string.Join(", ", result.Diagnostics.Select(d => $"line {d.Line} {d.Code}"))}]");

            onLine.Should().HaveCount(1,
                $"'{fileName}': each # EXPECT annotation should correspond to exactly one {exp.Code} diagnostic on line {exp.ExecutableLine}");

            onLine.Should().Contain(d => d.Severity == exp.Severity,
                $"'{fileName}': {exp.Code} on line {exp.ExecutableLine} should have severity={exp.Severity}," +
                $" but got {onLine.First().Severity}");

            onLine.Should().Contain(d => d.Message.Contains(exp.Contains, StringComparison.OrdinalIgnoreCase),
                $"'{fileName}': {exp.Code} on line {exp.ExecutableLine} message should contain '{exp.Contains}'," +
                $" but got: '{onLine.First().Message}'");
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // Helpers for diagnostic sample discovery
    // ════════════════════════════════════════════════════════════════════

    private sealed record SampleExpectation(
        string Code,
        ConstraintSeverity Severity,
        string Contains,
        int ExecutableLine);

    /// <summary>
    /// Parses all <c># EXPECT: CODE | severity=... | contains=...</c> annotations from
    /// a diagnostic sample file and maps each to the 1-based line number of the next
    /// non-comment, non-blank DSL line (the executable line the diagnostic attaches to).
    /// </summary>
    private static List<SampleExpectation> ParseExpectations(string text)
    {
        var lines = text.Split('\n');
        var result = new List<SampleExpectation>();

        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimEnd('\r').Trim();
            if (!trimmed.StartsWith("# EXPECT:", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!TryParseExpectLine(trimmed, out var code, out var severity, out var contains))
                continue;

            // Scan forward for the next non-comment, non-blank executable line (1-based)
            int execLine = -1;
            for (int j = i + 1; j < lines.Length; j++)
            {
                var candidate = lines[j].TrimEnd('\r').Trim();
                if (candidate.Length == 0) continue;
                if (candidate.StartsWith('#')) continue;
                execLine = j + 1;
                break;
            }

            if (execLine < 0) continue;

            result.Add(new SampleExpectation(code!, severity, contains!, execLine));
        }

        return result;
    }

    /// <summary>
    /// Parses a single <c># EXPECT: CODE | severity=... | contains=...</c> line.
    /// Returns false if the line is malformed or missing required fields.
    /// </summary>
    private static bool TryParseExpectLine(
        string line,
        out string? code,
        out ConstraintSeverity severity,
        out string? contains)
    {
        code = null;
        severity = ConstraintSeverity.Error;
        contains = null;

        var colon = line.IndexOf("EXPECT:", StringComparison.OrdinalIgnoreCase);
        if (colon < 0) return false;

        var rest = line.Substring(colon + "EXPECT:".Length).Trim();
        var parts = rest.Split('|').Select(p => p.Trim()).ToArray();
        if (parts.Length == 0) return false;

        code = parts[0].Trim();
        if (string.IsNullOrEmpty(code) || !code.StartsWith("C", StringComparison.OrdinalIgnoreCase))
            return false;

        foreach (var part in parts.Skip(1))
        {
            if (part.StartsWith("severity=", StringComparison.OrdinalIgnoreCase))
            {
                var sev = part.Substring("severity=".Length).Trim();
                severity = sev.Equals("error", StringComparison.OrdinalIgnoreCase)
                    ? ConstraintSeverity.Error
                    : sev.Equals("warning", StringComparison.OrdinalIgnoreCase)
                        ? ConstraintSeverity.Warning
                        : ConstraintSeverity.Hint;
            }
            else if (part.StartsWith("contains=", StringComparison.OrdinalIgnoreCase))
            {
                contains = part.Substring("contains=".Length).Trim();
            }
        }

        return !string.IsNullOrEmpty(contains);
    }

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
