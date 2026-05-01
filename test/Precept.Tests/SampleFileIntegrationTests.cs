using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// End-to-end integration tests that load every sample .precept file through the
/// full lex + parse pipeline and assert zero error-severity diagnostics.
///
/// Purpose: catch regressions that unit tests miss — a parser fix that silently
/// breaks a downstream construct will surface here before it ships.
///
/// All 28 of 28 sample files assert zero errors after GAP-A, GAP-B, GAP-C fixes
/// (Slices 14–16 of the parser gap-fixes plan).
/// </summary>
public class SampleFileIntegrationTests
{
    private static readonly string SamplesDir = Path.GetFullPath(Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "samples"));

    // ── Known pre-existing parser gaps ────────────────────────────────────
    //
    // All three parser gaps (GAP-A, GAP-B, GAP-C) are now fixed. No files are excluded.
    //   GAP-A (Slice 14): when-guard on ensure post-conditions
    //   GAP-B (Slice 15): field modifiers after computed ('->') expressions
    //   GAP-C (Slice 16): keywords (min/max) as member names or function-call names
    private static readonly IReadOnlySet<string> KnownBrokenFiles =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    // ── Main integration theory — zero-error gate ─────────────────────────

    /// <summary>
    /// All 28 sample files parse with zero error-severity diagnostics (no exclusions).
    /// </summary>
    [Theory]
    [MemberData(nameof(GetCleanSampleFiles))]
    public void SampleFile_ParsesWithZeroErrors(string filePath)
    {
        var source = File.ReadAllText(filePath);
        var tokens = Lexer.Lex(source);
        var tree = Parser.Parse(tokens);

        var errors = tree.Diagnostics.Where(d => d.Severity == Severity.Error).ToList();
        errors.Should().BeEmpty(
            $"sample file '{Path.GetFileName(filePath)}' should parse with zero error-severity diagnostics");
    }

    // ── Structural smoke — all 28 files produce a non-null header ─────────

    [Theory]
    [MemberData(nameof(GetAllSampleFiles))]
    public void SampleFile_HasNonNullHeader(string filePath)
    {
        var source = File.ReadAllText(filePath);
        var tree = Parser.Parse(Lexer.Lex(source));

        tree.Header.Should().NotBeNull(
            $"sample file '{Path.GetFileName(filePath)}' must declare a precept header");
    }

    // ── Gap regression — known-broken files do produce expected errors ─────

    /// <summary>
    /// Verifies that no known-broken files remain (all gaps are fixed).
    /// If a new regression is discovered, add it to KnownBrokenFiles with
    /// a comment explaining the gap, then remove it once fixed.
    /// </summary>
    [Fact]
    public void KnownBrokenSampleFile_SetIsEmpty()
    {
        KnownBrokenFiles.Should().BeEmpty(
            "all parser gaps are fixed — no sample files should produce errors");
    }

    // ── Coverage counts ───────────────────────────────────────────────────

    [Fact]
    public void AllSampleFiles_TotalCountIs28()
    {
        var allFiles = Directory.GetFiles(SamplesDir, "*.precept");
        allFiles.Should().HaveCount(28,
            "samples/ directory should contain exactly 28 .precept files");
    }

    [Fact]
    public void KnownBrokenFiles_AccountForExactly0OfThe28Samples()
    {
        KnownBrokenFiles.Should().BeEmpty(
            "all GAP-A, GAP-B, GAP-C fixes are applied — KnownBrokenFiles must be empty");
    }

    // ── Data sources ──────────────────────────────────────────────────────

    public static IEnumerable<object[]> GetCleanSampleFiles()
    {
        var samplesDir = Path.GetFullPath(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "samples"));

        return Directory.GetFiles(samplesDir, "*.precept")
            .Where(f => !KnownBrokenFiles.Contains(Path.GetFileName(f)))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .Select(f => new object[] { f });
    }

    public static IEnumerable<object[]> GetAllSampleFiles()
    {
        var samplesDir = Path.GetFullPath(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "samples"));

        return Directory.GetFiles(samplesDir, "*.precept")
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .Select(f => new object[] { f });
    }
}
