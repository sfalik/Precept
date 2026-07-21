using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Precept.MatrixTools.Tests;

/// <summary>
/// Cell prose-generator tests: the generator renders each *.cells.json file into
/// a readable per-family markdown table, and the committed generated file is
/// drift-checked — regenerating in memory must reproduce it byte for byte, so
/// the generated table can never silently diverge from the cell data (the same
/// discipline the TextMate grammar tests apply to the generated grammar).
/// </summary>
public class ProseGeneratorTests
{
    private static readonly string RepoRoot = FindRepoRoot();
    private static readonly string RealCellsDirectory = Path.Combine(
        RepoRoot, "docs", "Working", "obligation-discharge-matrix-2026-07-19-cells");
    private static readonly string Family1CellsFile = Path.Combine(
        RealCellsDirectory, "family-1-numeric-single-field.cells.json");
    private static readonly string SchemaPath = Path.Combine(RealCellsDirectory, "cell.schema.json");
    private static readonly string FixtureDirectory = Path.Combine(
        RepoRoot, "test", "Precept.MatrixTools.Tests", "Fixtures", "cells");

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(System.AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Precept.slnx")))
            directory = directory.Parent;
        return directory?.FullName
            ?? throw new Xunit.Sdk.XunitException("repository root (Precept.slnx) not found above the test directory");
    }

    private static string RenderFamily1() => CellProseGenerator.RenderFile(Family1CellsFile);

    // ── The drift check: committed output equals regeneration ────────────────

    [Fact]
    public void CommittedFamily1File_MatchesInMemoryRegeneration()
    {
        var generatedPath = Path.Combine(
            RealCellsDirectory, "generated", "family-1-numeric-single-field.md");

        File.Exists(generatedPath).Should().BeTrue(
            $"the generated table must be committed at {generatedPath}; regenerate with "
            + "'dotnet run --project tools/Precept.MatrixTools -- render-cells "
            + "docs/Working/obligation-discharge-matrix-2026-07-19-cells'");

        var committed = File.ReadAllText(generatedPath).ReplaceLineEndings("\n");
        var regenerated = RenderFamily1().ReplaceLineEndings("\n");

        committed.Should().Be(regenerated,
            "the committed generated file must match regeneration from the cell data exactly — "
            + "generated tables are never hand-edited; edit the .cells.json and regenerate");
    }

    // ── Generated-file header and determinism ────────────────────────────────

    [Fact]
    public void RenderedFile_OpensWithGeneratedHeaderNamingSourceAndOverwriteRule()
    {
        var markdown = RenderFamily1();

        markdown.Should().StartWith("<!--");
        markdown.Should().Contain("GENERATED FILE");
        markdown.Should().Contain("family-1-numeric-single-field.cells.json");
        markdown.Should().Contain("Hand edits will be overwritten");
        markdown.Should().Contain("render-cells");
    }

    [Fact]
    public void Rendering_IsDeterministic()
    {
        RenderFamily1().Should().Be(RenderFamily1(),
            "the drift test depends on regeneration being byte-for-byte reproducible");
    }

    // ── The table carries every required per-cell component ──────────────────

    [Fact]
    public void RenderedFile_CarriesEveryCellId()
    {
        var markdown = RenderFamily1();

        markdown.Should().Contain("wf1/establishment-defaults");
        markdown.Should().Contain("wf1/preservation-reads-args");
        markdown.Should().Contain("wf1/preservation-reads-written-decrease");
        markdown.Should().Contain("wf1/preservation-reads-written-increase");
    }

    [Fact]
    public void RenderedFile_CarriesCoordinatesInPlainWords()
    {
        var markdown = RenderFamily1();

        // Axis names are expanded to readable words; axis values stay verbatim
        // (the schema owns the vocabulary — the generator never rewrites it).
        markdown.Should().Contain("obligation family");
        markdown.Should().Contain("write site category");
        markdown.Should().Contain("RHS read set");
        markdown.Should().Contain("handler-set");
        markdown.Should().Contain("construction-defaults");
    }

    [Fact]
    public void RenderedFile_CarriesObligationStatementsAndCanonicalKeys()
    {
        var markdown = RenderFamily1();

        markdown.Should().Contain("Amount1 + Amount2 <= 1000");
        markdown.Should().Contain("Total_pre - Amount <= 1000");
        markdown.Should().Contain("(le (+ arg(Add.Amount1) arg(Add.Amount2)) #1000)");
        markdown.Should().Contain("(le (+ (neg arg(Subtract.Amount)) pre(Total)) #1000)");
    }

    [Fact]
    public void RenderedFile_CarriesContractsWithDecisionProcedures()
    {
        var markdown = RenderFamily1();

        markdown.Should().Contain("Decision procedure");
        markdown.Should().Contain("Shape-match the RHS as written-field-minus-term");
        markdown.Should().Contain("Constant-fold the rule over the default environment");
        markdown.Should().Contain("exact-decimal upper-bound sum");
    }

    [Fact]
    public void RenderedFile_CarriesWitnessRowsWithStatuses()
    {
        var markdown = RenderFamily1();

        // Discharge statuses — the model-vs-built split stays visible.
        markdown.Should().Contain("proven-today");
        markdown.Should().Contain("unresolved-today");
        markdown.Should().Contain("multi-term fact shape unbuilt");
        markdown.Should().Contain("provable-under-model");

        // Base and near-miss rows with their required outcomes.
        markdown.Should().Contain("field Total as decimal max 1000 default 0.0");
        markdown.Should().Contain("600 + 600 = 1200 > 1000");
        markdown.Should().Contain("Amount1 max 600, Amount2 max 600");
    }

    [Fact]
    public void RenderedFile_CarriesDispositionsWithCitations()
    {
        var markdown = RenderFamily1();

        markdown.Should().Contain("defined");
        markdown.Should().Contain("docs/Working/what-i-want-2026-07-16.md:18");
        markdown.Should().Contain("docs/Working/what-i-want-2026-07-16.md:19");
    }

    [Fact]
    public void RenderedFile_CarriesRespellabilityVerdictsAndBandMembers()
    {
        var markdown = RenderFamily1();

        markdown.Should().Contain("Respellability");
        markdown.Should().Contain("when Add.Amount1 <= 1000 - Add.Amount2");
        markdown.Should().Contain("per-term bounds");
        markdown.Should().Contain("family-inherited")
            .And.Contain("yes", "the family verdict the cells inherit must be stated");
    }

    [Fact]
    public void RenderedFile_CarriesPremiseClassLegendFromSchema()
    {
        // The legend is read from cell.schema.json's premiseClass description —
        // the schema stays the single owner of the class vocabulary.
        var markdown = RenderFamily1();

        markdown.Should().Contain("field modifiers");
        markdown.Should().Contain("arg constraints");
    }

    [Fact]
    public void PremiseClassLegend_FollowsTheSchemaText_NotAHardcodedCopy()
    {
        // Substring presence alone cannot tell a schema-read legend from a hardcoded
        // parallel copy of the class vocabulary. Mutate the schema in a scratch copy:
        // the render must follow the mutation.
        var scratch = Directory.CreateTempSubdirectory("prose-gen-schema");
        try
        {
            var schema = File.ReadAllText(SchemaPath)
                .Replace("(a) field modifiers", "(a) MUTATED-CLASS-A");
            var schemaCopy = Path.Combine(scratch.FullName, "cell.schema.json");
            File.WriteAllText(schemaCopy, schema);

            var markdown = CellProseGenerator.RenderFile(Family1CellsFile, schemaCopy);

            markdown.Should().Contain("MUTATED-CLASS-A",
                "the legend must be read from the schema, never restated in the generator");
            markdown.Should().NotContain("(a) field modifiers",
                "a hardcoded copy of the class vocabulary would survive the schema mutation");
        }
        finally
        {
            scratch.Delete(recursive: true);
        }
    }

    [Fact]
    public void AxisNames_FollowTheSchemaTitles_NotAHardcodedCopy()
    {
        // Same provenance check for the readable axis names: they come from each
        // coordinate property's schema title, so the schema owns that vocabulary too.
        var scratch = Directory.CreateTempSubdirectory("prose-gen-axis");
        try
        {
            var schema = File.ReadAllText(SchemaPath)
                .Replace("\"title\": \"write site category\"", "\"title\": \"MUTATED-AXIS-NAME\"");
            var schemaCopy = Path.Combine(scratch.FullName, "cell.schema.json");
            File.WriteAllText(schemaCopy, schema);

            var markdown = CellProseGenerator.RenderFile(Family1CellsFile, schemaCopy);

            markdown.Should().Contain("MUTATED-AXIS-NAME");
            markdown.Should().NotContain("write site category");
        }
        finally
        {
            scratch.Delete(recursive: true);
        }
    }

    // ── Nothing is silently dropped ──────────────────────────────────────────

    [Fact]
    public void UnknownCellProperty_IsCarriedVerbatimNotDropped()
    {
        var scratch = Directory.CreateTempSubdirectory("prose-gen-unknown");
        try
        {
            var source = File.ReadAllText(Path.Combine(FixtureDirectory, "valid-minimal.cells.json"))
                .Replace("\"id\": \"fx/args-only\",",
                    "\"id\": \"fx/args-only\",\n      \"futureUnknownField\": { \"payload\": 42 },");
            var path = Path.Combine(scratch.FullName, "unknown.cells.json");
            File.WriteAllText(path, source);

            var markdown = CellProseGenerator.RenderFile(path, SchemaPath);

            markdown.Should().Contain("futureUnknownField",
                "a data field the generator has no rendering rule for must surface by name, never vanish");
            markdown.Should().Contain("42");
        }
        finally
        {
            scratch.Delete(recursive: true);
        }
    }

    // ── Table safety ─────────────────────────────────────────────────────────

    [Fact]
    public void PipeCharactersInData_AreEscapedInsideTables()
    {
        var scratch = Directory.CreateTempSubdirectory("prose-gen-pipes");
        try
        {
            var source = File.ReadAllText(Path.Combine(FixtureDirectory, "valid-minimal.cells.json"))
                .Replace("guard does not imply the WP", "guard does not imply the WP | trailing");
            var path = Path.Combine(scratch.FullName, "pipes.cells.json");
            File.WriteAllText(path, source);

            var markdown = CellProseGenerator.RenderFile(path, SchemaPath);

            markdown.Should().Contain("guard does not imply the WP \\| trailing",
                "an unescaped pipe would split the markdown table cell");
        }
        finally
        {
            scratch.Delete(recursive: true);
        }
    }

    // ── CLI subcommand ───────────────────────────────────────────────────────

    [Fact]
    public void RenderCellsCommand_WritesEachFamilyIntoTheGeneratedDirectory()
    {
        var scratch = Directory.CreateTempSubdirectory("prose-gen-cli");
        try
        {
            File.Copy(
                Path.Combine(FixtureDirectory, "valid-minimal.cells.json"),
                Path.Combine(scratch.FullName, "valid-minimal.cells.json"));
            File.Copy(SchemaPath, Path.Combine(scratch.FullName, "cell.schema.json"));

            var exitCode = Cli.Run(["render-cells", scratch.FullName]);

            exitCode.Should().Be(0);
            var outputPath = Path.Combine(scratch.FullName, "generated", "valid-minimal.md");
            File.Exists(outputPath).Should().BeTrue("render-cells writes <family>.md under generated/");
            File.ReadAllText(outputPath).Should().Be(
                CellProseGenerator.RenderFile(Path.Combine(scratch.FullName, "valid-minimal.cells.json")),
                "the CLI writes exactly what the in-memory renderer produces");
        }
        finally
        {
            scratch.Delete(recursive: true);
        }
    }

    [Fact]
    public void RenderCellsCommand_OnUnparseableFile_ReportsTheFileAndFails()
    {
        var scratch = Directory.CreateTempSubdirectory("prose-gen-cli-bad");
        try
        {
            File.WriteAllText(Path.Combine(scratch.FullName, "broken.cells.json"), "{ not json");

            var stderr = new StringWriter();
            var previous = Console.Error;
            int exitCode;
            try
            {
                Console.SetError(stderr);
                exitCode = Cli.Run(["render-cells", scratch.FullName]);
            }
            finally
            {
                Console.SetError(previous);
            }

            // Exit 1, not the usage code 2 — otherwise this test passes on a
            // subcommand that does not exist at all.
            exitCode.Should().Be(1, "an unrenderable cell file must fail the generation run distinguishably "
                + "from a usage error (exit 2)");
            stderr.ToString().Should().Contain("broken.cells.json",
                "the failure must name the file it could not render");
        }
        finally
        {
            scratch.Delete(recursive: true);
        }
    }
}
