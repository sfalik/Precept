using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using FluentAssertions;
using Precept;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Drift defense tests for the three-tier catalog infrastructure.
/// These tests ensure that token attributes, construct registrations,
/// constraint enforcement, and sample files stay in sync.
/// See docs/CatalogInfrastructureDesign.md § Drift Defense for rationale.
/// </summary>
public class CatalogDriftTests
{
    // ════════════════════════════════════════════════════════════════════
    // Test 1: Construct examples parse
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void AllConstructExamples_ParseSuccessfully()
    {
        PreceptParser.EnsureInitialized();
        var constructs = ConstructCatalog.Constructs;
        constructs.Should().NotBeEmpty("ConstructCatalog should have registrations");

        var failures = new List<string>();

        foreach (var construct in constructs)
        {
            // Wrap example in a minimal valid precept file
            var dsl = BuildMinimalFile(construct);
            var (model, diags) = PreceptParser.ParseWithDiagnostics(dsl);
            if (diags.Count > 0 || model is null)
            {
                var msgs = string.Join("; ", diags.Select(d => d.Message));
                failures.Add($"Construct '{construct.Name}' example '{construct.Example}' failed: {msgs}");
            }
        }

        failures.Should().BeEmpty(
            "every ConstructInfo.Example must parse successfully within a valid file");
    }

    /// <summary>
    /// Wraps a construct example in a minimal precept file that provides the
    /// required context (states, events, fields) for the example to parse.
    /// </summary>
    private static string BuildMinimalFile(ConstructInfo construct)
    {
        // Base file with required structure
        var header = "precept DriftTest";
        var states = new List<string> { "state Idle initial", "state Open", "state Closed" };
        var fields = new List<string> { "field Priority as number default 1", "field Assignee as string nullable", "field Resolution as string nullable" };
        var events = new List<string> { "event Submit with Comment as string" };

        return construct.Name switch
        {
            "precept-header" =>
                // The example IS the header
                $"{construct.Example}\nstate Idle initial",

            "state-declaration" =>
                // The example declares a state; add header + another state if needed
                $"{header}\n{construct.Example}",

            "field-declaration" =>
                $"{header}\n{construct.Example}\n{string.Join("\n", states)}",

            "invariant" =>
                $"{header}\nfield Priority as number default 1\n{construct.Example}\n{string.Join("\n", states)}",

            "event-declaration" =>
                $"{header}\n{string.Join("\n", states)}\n{construct.Example}",

            "state-assert" =>
                $"{header}\n{string.Join("\n", fields)}\n{string.Join("\n", states)}\n{construct.Example}",

            "event-assert" =>
                $"{header}\n{string.Join("\n", states)}\nevent Submit with Comment as string\n{construct.Example}",

            "state-action" =>
                $"{header}\n{string.Join("\n", fields)}\n{string.Join("\n", states)}\n{construct.Example}",

            "edit-declaration" =>
                $"{header}\nfield Priority as number default 1\n{string.Join("\n", states)}\n{construct.Example}",

            "transition-row" =>
                $"{header}\n{string.Join("\n", states)}\nevent Submit\n{construct.Example}",

            _ =>
                // Generic fallback: include everything
                $"{header}\n{string.Join("\n", fields)}\n{string.Join("\n", states)}\n{string.Join("\n", events)}\n{construct.Example}"
        };
    }

    // ════════════════════════════════════════════════════════════════════
    // Test 2: SYNC comment ↔ ConstraintCatalog bidirectional check
    // ════════════════════════════════════════════════════════════════════

    private static readonly Regex SyncCommentRegex = new(
        @"//\s*SYNC:CONSTRAINT:(?<id>C\d+)", RegexOptions.Compiled);

    [Fact]
    public void SyncComments_MatchConstraintCatalog()
    {
        // Find source files that may contain SYNC comments
        var srcRoot = FindRepoRoot();
        var sourceFiles = Directory.GetFiles(Path.Combine(srcRoot, "src", "Precept"), "*.cs", SearchOption.AllDirectories);

        var syncIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in sourceFiles)
        {
            var content = File.ReadAllText(file);
            foreach (Match match in SyncCommentRegex.Matches(content))
                syncIds.Add(match.Groups["id"].Value);
        }

        var catalogIds = ConstraintCatalog.Constraints.Select(c => c.Id).ToHashSet(StringComparer.Ordinal);

        // Every SYNC comment must have a catalog entry
        var orphanedSyncs = syncIds.Except(catalogIds).ToList();
        orphanedSyncs.Should().BeEmpty(
            "every // SYNC:CONSTRAINT:Cnn comment should have a matching ConstraintCatalog entry");

        // Every catalog entry should have at least one SYNC comment (for parse/compile phase constraints)
        var parseCompileIds = ConstraintCatalog.Constraints
            .Where(c => c.Phase is "parse" or "compile")
            .Select(c => c.Id)
            .ToHashSet(StringComparer.Ordinal);
        var missingSyncs = parseCompileIds.Except(syncIds).ToList();
        missingSyncs.Should().BeEmpty(
            "every parse/compile-phase constraint should have at least one // SYNC:CONSTRAINT:Cnn comment in source");
    }

    // ════════════════════════════════════════════════════════════════════
    // Test 3: Token attributes complete
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void AllTokens_HaveCategoryAndDescription()
    {
        var missing = new List<string>();

        foreach (var token in Enum.GetValues<PreceptToken>())
        {
            if (PreceptTokenMeta.GetCategory(token) is null)
                missing.Add($"{token}: missing [TokenCategory]");
            if (PreceptTokenMeta.GetDescription(token) is null)
                missing.Add($"{token}: missing [TokenDescription]");
        }

        missing.Should().BeEmpty("every PreceptToken member must have [TokenCategory] and [TokenDescription]");
    }

    [Fact]
    public void KeywordAndOperatorTokens_HaveSymbol()
    {
        var missing = new List<string>();

        foreach (var token in Enum.GetValues<PreceptToken>())
        {
            var category = PreceptTokenMeta.GetCategory(token);
            if (category is null) continue;

            // Keyword, operator, and punctuation tokens must have [TokenSymbol]
            var needsSymbol = category.Value is
                TokenCategory.Control or TokenCategory.Declaration or
                TokenCategory.Action or TokenCategory.Outcome or
                TokenCategory.Type or TokenCategory.Literal or
                TokenCategory.Operator or TokenCategory.Punctuation;

            if (needsSymbol && PreceptTokenMeta.GetSymbol(token) is null)
                missing.Add($"{token} ({category.Value}): missing [TokenSymbol]");
        }

        missing.Should().BeEmpty(
            "keyword, operator, and punctuation tokens must have [TokenSymbol]");
    }

    // ════════════════════════════════════════════════════════════════════
    // Test 4: Reference sample coverage
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void SampleFiles_CoverAllConstructs()
    {
        PreceptParser.EnsureInitialized();
        var constructs = ConstructCatalog.Constructs;
        constructs.Should().NotBeEmpty();

        var srcRoot = FindRepoRoot();
        var samplesDir = Path.Combine(srcRoot, "samples");
        var sampleFiles = Directory.GetFiles(samplesDir, "*.precept");
        sampleFiles.Should().NotBeEmpty("samples/ directory must contain .precept files");

        // Concatenate all sample content
        var allSampleContent = string.Join("\n", sampleFiles.Select(File.ReadAllText));

        var uncovered = new List<string>();
        foreach (var construct in constructs)
        {
            // Extract leading keyword(s) from form
            var spaceIdx = construct.Form.IndexOf(' ');
            var firstGroup = spaceIdx >= 0 ? construct.Form[..spaceIdx] : construct.Form;
            var keywords = firstGroup.Split('|');

            // At least one keyword must appear in at least one sample
            if (!keywords.Any(kw => allSampleContent.Contains(kw, StringComparison.OrdinalIgnoreCase)))
                uncovered.Add($"Construct '{construct.Name}' (keyword(s): {firstGroup}) — not found in any sample file");
        }

        uncovered.Should().BeEmpty(
            "at least one .precept sample file should use every registered construct");
    }

    // ════════════════════════════════════════════════════════════════════
    // Test 5: Diagnostic code format
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void DiagnosticCodes_FollowPreceptNNNFormat()
    {
        foreach (var constraint in ConstraintCatalog.Constraints)
        {
            var code = ConstraintCatalog.ToDiagnosticCode(constraint.Id);
            code.Should().MatchRegex(@"^PRECEPT\d{3}$",
                $"constraint {constraint.Id} diagnostic code should be PRECEPTnnn");
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // Helpers
    // ════════════════════════════════════════════════════════════════════

    private static string FindRepoRoot()
    {
        // Walk up from the test assembly's directory to find the repo root (contains Precept.slnx)
        var dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            if (File.Exists(Path.Combine(dir, "Precept.slnx")))
                return dir;
            dir = Directory.GetParent(dir)?.FullName;
        }
        throw new InvalidOperationException("Could not find repo root (Precept.slnx)");
    }
}
