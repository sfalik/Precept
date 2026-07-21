using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Precept.MatrixTools.Tests;

/// <summary>
/// Cell-to-test converter tests over the real family-1 cell data: every defined
/// cell with a base witness yields a base row (must reject naming the cell's
/// obligation), one row per discharge addition (must accept via the expected
/// strategy, with the built-at-HEAD expectation recorded separately from the
/// definitional expectation), and one row per near-miss (must still reject,
/// same obligation). Cells the converter cannot turn into runnable rows yield
/// named skip records — never a generic answer, never silence.
/// </summary>
public class CellTestConverterTests
{
    private static readonly string RepoRoot = FindRepoRoot();
    private static readonly string Family1Path = Path.Combine(
        RepoRoot, "docs", "Working", "obligation-discharge-matrix-2026-07-19-cells",
        "family-1-numeric-single-field.cells.json");

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(System.AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Precept.slnx")))
            directory = directory.Parent;
        return directory?.FullName
            ?? throw new Xunit.Sdk.XunitException("repository root (Precept.slnx) not found above the test directory");
    }

    private static TestManifest Family1() => CellTestConverter.Convert(Family1Path);

    // ── Row inventory ────────────────────────────────────────────────────────

    [Fact]
    public void Family1_YieldsElevenRows_ThreeBasesFourDischargesFourNearMisses()
    {
        var manifest = Family1();

        manifest.Rows.Should().HaveCount(11);
        manifest.Rows.Count(r => r.Kind == ManifestRowKind.Base).Should().Be(3);
        manifest.Rows.Count(r => r.Kind == ManifestRowKind.Discharge).Should().Be(4);
        manifest.Rows.Count(r => r.Kind == ManifestRowKind.NearMiss).Should().Be(4);
    }

    [Fact]
    public void Family1_CarriesFamilyIdentityAndDefinitionVersion()
    {
        var manifest = Family1();

        manifest.FamilyId.Should().Be("witness-family-1");
        manifest.DefinitionVersion.Should().NotBeNullOrWhiteSpace();
        manifest.SourceFile.Should().Be("family-1-numeric-single-field.cells.json");
    }

    // ── Named skips, never silence ───────────────────────────────────────────

    [Fact]
    public void Family1_EstablishmentCell_YieldsNamedSkipForMissingBaseProgram()
    {
        var manifest = Family1();

        var skip = manifest.Skips.Should().ContainSingle(
            s => s.CellId == "wf1/establishment-defaults").Subject;
        skip.Reason.Should().Contain("no base witness program");
        manifest.Rows.Should().NotContain(r => r.CellId == "wf1/establishment-defaults");
    }

    // ── Base rows: reject naming the obligation ──────────────────────────────

    [Fact]
    public void BaseRows_ExpectRejectionNamingTheDesugaredMaxObligation()
    {
        var baseRows = Family1().Rows.Where(r => r.Kind == ManifestRowKind.Base).ToArray();

        baseRows.Should().HaveCount(3);
        foreach (var row in baseRows)
        {
            row.Expected.Should().Be(ExpectedOutcome.Reject);
            row.Head.Should().Be(HeadExpectation.MustPass);
            row.Obligation.Field.Should().Be("Total");
            row.Obligation.Kind.Should().Be(BoundKind.UpperInclusive);
            row.Obligation.Bound.Should().Be(1000m);
            row.Obligation.Label.Should().Be("Total:max");
            row.Obligation.ContextClass.Should().Be(ObligationContextClass.WriteSite);
            // The cell data states base minimality (noOtherDiagnostics), so the
            // row requires every error to come from the proof stage.
            row.RequireOnlyObligationDiagnostics.Should().BeTrue();
        }
    }

    // ── Discharge rows: addition application ─────────────────────────────────

    [Fact]
    public void GuardAddition_IsInsertedIntoTheRowBeforeTheArrow()
    {
        var row = Family1().Rows.Single(r => r.Id == "wf1/preservation-reads-args#discharge:guard-nf");

        row.Program.Should().Contain("on Add when Add.Amount1 + Add.Amount2 <= 1000 ->");
        row.Expected.Should().Be(ExpectedOutcome.Accept);
    }

    [Fact]
    public void ArgBoundAddition_ReplacesTheArgDeclarations()
    {
        var row = Family1().Rows.Single(r => r.Id == "wf1/preservation-reads-args#discharge:arg-bounds");

        row.Program.Should().Contain("Amount1 as decimal max 250");
        row.Program.Should().Contain("Amount2 as decimal max 750");
        row.Expected.Should().Be(ExpectedOutcome.Accept);
    }

    // ── Delivered vs claimed: built-at-HEAD expectations from the cell data ──

    [Fact]
    public void ProvenTodayDischarge_IsMustPassWithItsExpectedStrategy()
    {
        var row = Family1().Rows.Single(r => r.Id == "wf1/preservation-reads-args#discharge:arg-bounds");

        row.Head.Should().Be(HeadExpectation.MustPass);
        row.ExpectedStrategy.Should().Be("IntervalContainment");
    }

    [Fact]
    public void UnresolvedTodayDischarges_AreRecordedAsExpectedFailures()
    {
        var manifest = Family1();

        var guardNf = manifest.Rows.Single(r => r.Id == "wf1/preservation-reads-args#discharge:guard-nf");
        guardNf.Head.Should().Be(HeadExpectation.ExpectedFailure);
        guardNf.HeadNote.Should().Contain("multi-term fact shape unbuilt");

        var nonNegative = manifest.Rows.Single(
            r => r.Id == "wf1/preservation-reads-written-decrease#discharge:nonnegative-arg");
        nonNegative.Head.Should().Be(HeadExpectation.ExpectedFailure);
        nonNegative.HeadNote.Should().Contain("pre-state rule not carried as a premise");
    }

    [Fact]
    public void DischargeWithoutStatedBuiltStatus_IsRecordedUnstated_NotInvented()
    {
        var row = Family1().Rows.Single(
            r => r.Id == "wf1/preservation-reads-written-increase#discharge:guard-nf");

        row.Head.Should().Be(HeadExpectation.Unstated);
        row.Expected.Should().Be(ExpectedOutcome.Accept);
    }

    // ── Near-miss rows: still reject, same obligation ────────────────────────

    [Fact]
    public void NearMissRows_ExpectRejectionOfTheSameObligation()
    {
        var nearMisses = Family1().Rows.Where(r => r.Kind == ManifestRowKind.NearMiss).ToArray();

        nearMisses.Should().HaveCount(4);
        foreach (var row in nearMisses)
        {
            row.Expected.Should().Be(ExpectedOutcome.Reject);
            row.Head.Should().Be(HeadExpectation.MustPass);
            row.PairsWith.Should().NotBeNullOrWhiteSpace();
            // Same obligation as the cell's base row, by structural key.
            var baseRow = Family1().Rows.Single(
                b => b.CellId == row.CellId && b.Kind == ManifestRowKind.Base);
            row.Obligation.Should().Be(baseRow.Obligation);
        }
    }

    [Fact]
    public void NearMissPrograms_CarryTheWeakenedAdditions()
    {
        var manifest = Family1();

        manifest.Rows.Single(r => r.Id == "wf1/preservation-reads-args#near-miss:arg-bounds")
            .Program.Should().Contain("Amount1 as decimal max 600");
        manifest.Rows.Single(r => r.Id == "wf1/preservation-reads-args#near-miss:guard-nf")
            .Program.Should().Contain("on Add when Add.Amount1 <= 1000 ->");
        manifest.Rows.Single(r => r.Id == "wf1/preservation-reads-written-decrease#near-miss:nonnegative-arg")
            .Program.Should().Contain("Amount as decimal max 100");
        manifest.Rows.Single(r => r.Id == "wf1/preservation-reads-written-increase#near-miss:guard-nf")
            .Program.Should().Contain("on Grow when Grow.Amount <= 1000 ->");
    }

    // ── Every emitted program is a runnable precept ──────────────────────────

    [Fact]
    public void EveryEmittedProgram_HasNoPreProofErrors()
    {
        foreach (var row in Family1().Rows)
        {
            var compilation = Px.Compile(row.Program);
            var preProofErrors = compilation.Diagnostics
                .Where(d => d.Severity == Precept.Language.Severity.Error
                    && d.Stage != Precept.Language.DiagnosticStage.Proof)
                .ToArray();
            preProofErrors.Should().BeEmpty(
                because: $"row '{row.Id}' must differ from its base only in proof outcome; "
                    + $"codes: {string.Join(", ", preProofErrors.Select(d => d.Code))}");
        }
    }

    // ── Manifest round-trip ──────────────────────────────────────────────────

    [Fact]
    public void Manifest_RoundTripsThroughJson()
    {
        var manifest = Family1();

        var roundTripped = CellTestConverter.FromJson(CellTestConverter.ToJson(manifest));

        roundTripped.Should().BeEquivalentTo(manifest);
    }
}
