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
/// 24 of 28 sample files assert zero errors. The remaining 4 carry known pre-existing
/// parser gaps (tracked below). Slice 12 of the parser gap-fixes plan.
/// </summary>
public class SampleFileIntegrationTests
{
    private static readonly string SamplesDir = Path.GetFullPath(Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "samples"));

    // ── Known pre-existing parser gaps ────────────────────────────────────
    //
    // These files are excluded from the zero-error theory because they hit parser
    // gaps that pre-date slices 1–13 and are not fixed by them. Each entry is
    // annotated with the gap category and the specific error produced.
    //
    // GAP-A (fixed Slice 14): 'ensure Cond when Guard because "msg"' now parses correctly.
    //   insurance-claim.precept and loan-application.precept also use `.min` member access
    //   (GAP-C), so they remain in this list under the GAP-C annotation.
    //
    // GAP-B (fixed Slice 15): Field modifiers after computed expression now parsed correctly.
    //   sum-on-rhs-rule, invoice-line-item, transitive-ordering now clean.
    //   travel-reimbursement: GAP-B is fixed BUT ALSO uses min() as a function call
    //   (keyword-as-function-name, same root cause as GAP-C).
    //
    // GAP-C: Reserved keyword used as a collection member name (`.min` / `.max`)
    //   or function name (`min(...)`, `max(...)`).
    //   MemberAccess/Atom parsers expect Identifier, reject Min/Max tokens.
    //   Files affected: insurance-claim.precept, loan-application.precept,
    //                   building-access-badge-request.precept, travel-reimbursement.precept
    private static readonly IReadOnlySet<string> KnownBrokenFiles =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "insurance-claim.precept",           // GAP-C: uses .min/.max member access
            "loan-application.precept",           // GAP-C: uses .min/.max member access
            "building-access-badge-request.precept", // GAP-C: uses .min/.max member access
            "travel-reimbursement.precept",       // GAP-C: uses min() as function call (keyword-as-identifier)
        };

    // ── Main integration theory — zero-error gate ─────────────────────────

    /// <summary>
    /// 24 sample files parse with zero error-severity diagnostics.
    /// The 4 known-broken files are excluded; see KnownBrokenFiles above.
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
    /// Verifies that the 4 known-broken files still produce parse errors.
    /// If this test fails for a file, that file's gap is fixed and it should
    /// be removed from KnownBrokenFiles and added back to the clean set.
    /// </summary>
    [Theory]
    [MemberData(nameof(GetKnownBrokenSampleFiles))]
    public void KnownBrokenSampleFile_StillHasParserErrors(string filePath)
    {
        var source = File.ReadAllText(filePath);
        var tree = Parser.Parse(Lexer.Lex(source));

        tree.Diagnostics.Should().Contain(d => d.Severity == Severity.Error,
            $"'{Path.GetFileName(filePath)}' is a known-broken file — it should still emit errors " +
            $"(if it no longer does, remove it from KnownBrokenFiles and add to the clean set)");
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
    public void KnownBrokenFiles_AccountForExactly4OfThe28Samples()
    {
        var allFiles = Directory.GetFiles(SamplesDir, "*.precept")
            .Select(Path.GetFileName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        KnownBrokenFiles.Should().HaveCount(4,
            "exactly 4 sample files have known pre-existing parser gaps (GAP-C × 4)");
        KnownBrokenFiles.Should().BeSubsetOf(allFiles,
            "every entry in KnownBrokenFiles must be an actual sample file");
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

    public static IEnumerable<object[]> GetKnownBrokenSampleFiles()
    {
        var samplesDir = Path.GetFullPath(Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "samples"));

        return Directory.GetFiles(samplesDir, "*.precept")
            .Where(f => KnownBrokenFiles.Contains(Path.GetFileName(f)))
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .Select(f => new object[] { f });
    }
}
