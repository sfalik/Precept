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
/// Diagnostic samples (test/integrationtests/diagnostics/*.precept): must declare
/// their exact expected diagnostics through <c># EXPECT:</c> comments and compile to
/// exactly that diagnostic set, with no extra errors, warnings, or hints.
///
/// EXPECT contract:
///   # EXPECT: C94 | severity=error | match=exact | message=... | line=12 | start=34 | end=47
///
/// See CONTRIBUTING.md § Diagnostic Samples and docs/ProofEngineDesign.md.
/// </summary>
public class DiagnosticSampleDriftTests
{
    private enum MessageMatchMode
    {
        Exact,
        Contains,
    }

    private sealed record SampleExpectation(
        string Code,
        ConstraintSeverity Severity,
        MessageMatchMode MatchMode,
        string Message,
        int Line,
        int Start,
        int End,
        int ExpectationLine)
    {
        public string DiagnosticCode => DiagnosticCatalog.ToDiagnosticCode(Code);

        public bool Matches(PreceptDiagnostic diagnostic)
            => string.Equals(diagnostic.Code, DiagnosticCode, StringComparison.Ordinal)
                && diagnostic.Severity == Severity
                && diagnostic.Line == Line
                && diagnostic.Column == Start
                && diagnostic.EndColumn == End
                && MatchesMessage(diagnostic.Message);

        public string Describe()
            => $"{DiagnosticCode}|{Severity}|line={Line}|start={Start}|end={End}|{MatchMode.ToString().ToLowerInvariant()}=\"{Message}\"";

        private bool MatchesMessage(string actualMessage)
            => MatchMode == MessageMatchMode.Exact
                ? string.Equals(actualMessage, Message, StringComparison.Ordinal)
                : actualMessage.Contains(Message, StringComparison.Ordinal);
    }

    private sealed record ParsedExpectations(
        IReadOnlyList<SampleExpectation> Expectations,
        IReadOnlyList<string> Errors);

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

    [Fact]
    public void SampleDirectory_ContainsExpectedFileCount()
    {
        var files = Directory.GetFiles(SamplesDir, "*.precept");
        files.Length.Should().BeGreaterThanOrEqualTo(27,
            "samples/ should contain the baseline 25 files plus the Step 15 proof samples");
    }

    public static TheoryData<string> AllSampleFiles()
    {
        var data = new TheoryData<string>();
        foreach (var file in Directory.GetFiles(SamplesDir, "*.precept"))
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

    [Fact]
    public void DiagnosticSamplesDirectory_Exists()
    {
        Directory.Exists(DiagnosticSamplesDir).Should().BeTrue(
            "test/integrationtests/diagnostics/ must exist and contain the proof-engine diagnostic catalog");
    }

    public static TheoryData<string> AllDiagnosticSampleFiles()
    {
        var data = new TheoryData<string>();
        if (!Directory.Exists(DiagnosticSamplesDir))
            return data;

        foreach (var file in Directory.GetFiles(DiagnosticSamplesDir, "*.precept"))
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

    [Theory]
    [MemberData(nameof(AllDiagnosticSampleFiles))]
    public void DiagnosticSample_StrictExpectations_AreWellFormed(string fileName)
    {
        var filePath = Path.Combine(DiagnosticSamplesDir, fileName);
        var rawText = File.ReadAllText(filePath);

        var parsed = ParseExpectations(rawText);
        parsed.Errors.Should().BeEmpty(
            $"diagnostic sample '{fileName}' has malformed '# EXPECT:' metadata:{Environment.NewLine}{string.Join(Environment.NewLine, parsed.Errors)}");

        parsed.Expectations.Should().NotBeEmpty(
            $"diagnostic sample '{fileName}' must declare at least one '# EXPECT:' contract row");
    }

    [Theory]
    [MemberData(nameof(AllDiagnosticSampleFiles))]
    public void DiagnosticSample_Expectations_CoverDemonstratedCodes(string fileName)
    {
        var filePath = Path.Combine(DiagnosticSamplesDir, fileName);
        var rawText = File.ReadAllText(filePath);

        var headerLine = FindDemonstratesHeader(rawText);
        if (headerLine is null)
            return;

        var parsed = ParseExpectations(rawText);
        if (parsed.Errors.Count > 0)
            return;

        var declaredCodes = ParseDeclaredCodes(headerLine)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var expectCodes = parsed.Expectations
            .Select(expectation => expectation.Code)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        expectCodes.SetEquals(declaredCodes).Should().BeTrue(
            $"'{fileName}': '# Demonstrates:' codes must exactly match the distinct codes declared in '# EXPECT:' annotations. " +
            $"Header=[{string.Join(", ", declaredCodes.OrderBy(code => code))}] Expect=[{string.Join(", ", expectCodes.OrderBy(code => code))}]");
    }

    [Theory]
    [MemberData(nameof(AllDiagnosticSampleFiles))]
    public void DiagnosticSample_ExactExpectations_MatchDiagnostics_AndNoExtraDiagnostics(string fileName)
    {
        var filePath = Path.Combine(DiagnosticSamplesDir, fileName);
        var rawText = File.ReadAllText(filePath);
        var parsed = ParseExpectations(rawText);

        if (parsed.Errors.Count > 0)
            return;

        var result = PreceptCompiler.CompileFromText(rawText);
        var unmatchedActual = result.Diagnostics
            .Select((diagnostic, index) => (diagnostic, index))
            .ToList();

        foreach (var expectation in parsed.Expectations)
        {
            var matches = unmatchedActual
                .Where(candidate => expectation.Matches(candidate.diagnostic))
                .ToList();

            matches.Should().HaveCount(1,
                $"'{fileName}': EXPECT on line {expectation.ExpectationLine} must match exactly one diagnostic. " +
                $"Expected: {expectation.Describe()}{Environment.NewLine}" +
                $"Actual diagnostics:{Environment.NewLine}{FormatDiagnostics(result.Diagnostics)}");

            unmatchedActual.Remove(matches[0]);
        }

        unmatchedActual.Should().BeEmpty(
            $"'{fileName}': every emitted diagnostic must be declared by '# EXPECT:' metadata. " +
            $"Unexpected actual diagnostics:{Environment.NewLine}{FormatDiagnostics(unmatchedActual.Select(candidate => candidate.diagnostic))}");
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

    private static ParsedExpectations ParseExpectations(string text)
    {
        var expectations = new List<SampleExpectation>();
        var errors = new List<string>();
        var lines = text.Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].TrimEnd('\r').Trim();
            if (!trimmed.StartsWith("# EXPECT:", StringComparison.OrdinalIgnoreCase))
                continue;

            if (TryParseExpectation(trimmed, i + 1, out var expectation, out var error))
            {
                expectations.Add(expectation!);
                continue;
            }

            errors.Add(error!);
        }

        return new ParsedExpectations(expectations, errors);
    }

    private static bool TryParseExpectation(
        string line,
        int expectationLine,
        out SampleExpectation? expectation,
        out string? error)
    {
        expectation = null;
        error = null;

        var colon = line.IndexOf("EXPECT:", StringComparison.OrdinalIgnoreCase);
        if (colon < 0)
        {
            error = $"line {expectationLine}: missing 'EXPECT:' marker";
            return false;
        }

        var rest = line.Substring(colon + "EXPECT:".Length).Trim();
        var parts = rest.Split('|').Select(static part => part.Trim()).ToArray();
        if (parts.Length < 7)
        {
            error = $"line {expectationLine}: EXPECT must include code plus severity, match, message, line, start, and end";
            return false;
        }

        var code = parts[0];
        if (string.IsNullOrWhiteSpace(code) || !code.StartsWith("C", StringComparison.OrdinalIgnoreCase))
        {
            error = $"line {expectationLine}: first EXPECT segment must be a diagnostic code like C94";
            return false;
        }

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var part in parts.Skip(1))
        {
            var separator = part.IndexOf('=');
            if (separator <= 0 || separator == part.Length - 1)
            {
                error = $"line {expectationLine}: malformed EXPECT segment '{part}'";
                return false;
            }

            var key = part.Substring(0, separator).Trim();
            var value = part.Substring(separator + 1).Trim();
            metadata[key] = value;
        }

        if (!TryParseSeverity(metadata, expectationLine, out var severity, out error)
            || !TryParseMatchMode(metadata, expectationLine, out var matchMode, out error)
            || !TryGetRequired(metadata, "message", expectationLine, out var message, out error)
            || !TryParseInt(metadata, "line", expectationLine, out var lineNumber, out error)
            || !TryParseInt(metadata, "start", expectationLine, out var start, out error)
            || !TryParseInt(metadata, "end", expectationLine, out var end, out error))
        {
            return false;
        }

        expectation = new SampleExpectation(
            code.ToUpperInvariant(),
            severity,
            matchMode,
            message!,
            lineNumber,
            start,
            end,
            expectationLine);
        return true;
    }

    private static bool TryParseSeverity(
        IReadOnlyDictionary<string, string> metadata,
        int expectationLine,
        out ConstraintSeverity severity,
        out string? error)
    {
        severity = ConstraintSeverity.Error;
        error = null;

        if (!TryGetRequired(metadata, "severity", expectationLine, out var rawSeverity, out error))
            return false;

        if (rawSeverity!.Equals("error", StringComparison.OrdinalIgnoreCase))
        {
            severity = ConstraintSeverity.Error;
            return true;
        }

        if (rawSeverity.Equals("warning", StringComparison.OrdinalIgnoreCase))
        {
            severity = ConstraintSeverity.Warning;
            return true;
        }

        if (rawSeverity.Equals("hint", StringComparison.OrdinalIgnoreCase))
        {
            severity = ConstraintSeverity.Hint;
            return true;
        }

        error = $"line {expectationLine}: unsupported severity '{rawSeverity}'";
        return false;
    }

    private static bool TryParseMatchMode(
        IReadOnlyDictionary<string, string> metadata,
        int expectationLine,
        out MessageMatchMode matchMode,
        out string? error)
    {
        matchMode = MessageMatchMode.Exact;
        error = null;

        if (!TryGetRequired(metadata, "match", expectationLine, out var rawMode, out error))
            return false;

        if (rawMode!.Equals("exact", StringComparison.OrdinalIgnoreCase))
        {
            matchMode = MessageMatchMode.Exact;
            return true;
        }

        if (rawMode.Equals("contains", StringComparison.OrdinalIgnoreCase))
        {
            matchMode = MessageMatchMode.Contains;
            return true;
        }

        error = $"line {expectationLine}: unsupported match mode '{rawMode}'";
        return false;
    }

    private static bool TryParseInt(
        IReadOnlyDictionary<string, string> metadata,
        string key,
        int expectationLine,
        out int value,
        out string? error)
    {
        value = 0;
        error = null;

        if (!TryGetRequired(metadata, key, expectationLine, out var rawValue, out error))
            return false;

        if (int.TryParse(rawValue, out value) && value >= 0)
            return true;

        error = $"line {expectationLine}: '{key}' must be a non-negative integer";
        return false;
    }

    private static bool TryGetRequired(
        IReadOnlyDictionary<string, string> metadata,
        string key,
        int expectationLine,
        out string? value,
        out string? error)
    {
        value = null;
        error = null;

        if (!metadata.TryGetValue(key, out value) || string.IsNullOrWhiteSpace(value))
        {
            error = $"line {expectationLine}: EXPECT is missing required '{key}=' metadata";
            return false;
        }

        return true;
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
        var colon = headerLine.IndexOf("Demonstrates:", StringComparison.OrdinalIgnoreCase);
        var after = headerLine.Substring(colon + "Demonstrates:".Length);
        var dash = after.IndexOf('—');
        if (dash >= 0)
            after = after.Substring(0, dash);

        return after.Split(',')
            .Select(static code => code.Trim())
            .Where(code => code.Length > 0 && code.StartsWith("C", StringComparison.OrdinalIgnoreCase))
            .ToArray();
    }

    private static string FormatDiagnostics(IEnumerable<PreceptDiagnostic> diagnostics)
    {
        var rows = diagnostics
            .Select(diagnostic =>
                $"- {diagnostic.Code}|{diagnostic.Severity}|line={diagnostic.Line}|start={diagnostic.Column}|end={diagnostic.EndColumn}|message=\"{diagnostic.Message}\"")
            .ToArray();

        return rows.Length == 0 ? "- <none>" : string.Join(Environment.NewLine, rows);
    }
}
