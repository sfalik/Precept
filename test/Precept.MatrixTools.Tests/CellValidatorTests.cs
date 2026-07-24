using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Precept.MatrixTools.Tests;

/// <summary>
/// Cell-data validator tests: the deliberately-broken fixture files under
/// Fixtures/cells each trip one check, and the real cell data under
/// docs/Working validates clean with every calculator refusal surfacing as a
/// named skip record — never a generic answer, never silence.
/// </summary>
public class CellValidatorTests
{
    private static readonly string RepoRoot = FindRepoRoot();
    private static readonly string SchemaPath = Path.Combine(
        RepoRoot, "docs", "Working", "obligation-discharge-matrix-2026-07-19-cells", "cell.schema.json");
    private static readonly string RealCellsDirectory = Path.Combine(
        RepoRoot, "docs", "Working", "obligation-discharge-matrix-2026-07-19-cells");
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

    private static CellValidationReport ValidateFixture(string fileName) =>
        CellValidator.ValidateFile(Path.Combine(FixtureDirectory, fileName), RepoRoot, SchemaPath);

    private static void AssertHasError(CellValidationReport report, string check, string messageFragment)
    {
        report.Findings.Should().Contain(f =>
            f.Severity == CellFindingSeverity.Error
            && f.Check == check
            && f.Message.Contains(messageFragment),
            because: $"an Error finding for check '{check}' mentioning '{messageFragment}' is expected; "
                + $"actual findings: {string.Join(" | ", report.Findings.Select(f => $"{f.Severity}/{f.Check}: {f.Message}"))}");
    }

    // ── Green baseline: no false positives ───────────────────────────────────

    [Fact]
    public void ValidMinimalFixture_HasNoFindingsAndNoSkips()
    {
        var report = ValidateFixture("valid-minimal.cells.json");
        report.Findings.Should().BeEmpty();
        report.Skips.Should().BeEmpty();
    }

    // ── The real family-1 cell data ──────────────────────────────────────────

    [Fact]
    public void RealCellData_HasNoErrorsAndNoWarnings()
    {
        var report = CellValidator.ValidateDirectory(RealCellsDirectory, RepoRoot);
        report.Findings.Should().BeEmpty();
    }

    [Fact]
    public void RealCellData_EstablishmentCell_YieldsNamedWpSkip()
    {
        var report = CellValidator.ValidateDirectory(RealCellsDirectory, RepoRoot);

        // The standing establishment cell records no rejecting base program, so
        // there is nothing to recompute the WP from — a named skip, not silence.
        // Other families contribute their own named skips; this asserts the
        // establishment cell's skip specifically, not that it is the only one.
        report.Skips.Where(s => s.Location.Contains("wf1/establishment-defaults"))
            .Should().ContainSingle().Which.Should().Match<CellSkip>(s =>
                s.Check == "wp-recompute"
                && s.Reason.Contains("no base witness program"));

        // Every skip names its own specific reason — none is generic or empty.
        report.Skips.Should().OnlyContain(s =>
            !string.IsNullOrWhiteSpace(s.Check)
            && !string.IsNullOrWhiteSpace(s.Location)
            && !string.IsNullOrWhiteSpace(s.Reason));
    }

    [Fact]
    public void RealCellData_PreservationCells_RecomputeToTheirStoredKeys()
    {
        var report = CellValidator.ValidateDirectory(RealCellsDirectory, RepoRoot);
        report.Findings.Where(f => f.Check == "wp-recompute").Should().BeEmpty();
        report.Skips.Where(s =>
                s.Location.Contains("wf1/preservation-reads-args")
                || s.Location.Contains("wf1/preservation-reads-written-decrease")
                || s.Location.Contains("wf1/preservation-reads-written-increase"))
            .Should().BeEmpty("the three single-write preservation cells are in the calculator's scope");
    }

    // ── Definition-version stamp ─────────────────────────────────────────────

    [Fact]
    public void EmptyDefinitionVersion_IsError()
    {
        var report = ValidateFixture("broken-structure.cells.json");
        AssertHasError(report, "definition-version", "definitionVersion");
    }

    // ── Disposition ──────────────────────────────────────────────────────────

    [Fact]
    public void DispositionOutsideClosedSet_IsError()
    {
        var report = ValidateFixture("broken-structure.cells.json");
        report.Findings.Should().Contain(f =>
            f.Severity == CellFindingSeverity.Error
            && f.Check == "disposition"
            && f.Location.Contains("bx/bad-disposition")
            && f.Message.Contains("sideways"));
    }

    [Fact]
    public void MissingDisposition_IsError()
    {
        var report = ValidateFixture("broken-structure.cells.json");
        report.Findings.Should().Contain(f =>
            f.Severity == CellFindingSeverity.Error
            && f.Check == "disposition"
            && f.Location.Contains("bx/no-disposition"));
    }

    // ── Axis values ──────────────────────────────────────────────────────────

    [Fact]
    public void AxisValueOutsideVocabulary_IsError()
    {
        var report = ValidateFixture("broken-structure.cells.json");
        AssertHasError(report, "axis", "telepathy");
    }

    [Fact]
    public void AxisArrayMemberOutsideVocabulary_IsError()
    {
        var report = ValidateFixture("broken-structure.cells.json");
        AssertHasError(report, "axis", "vibes");
    }

    [Fact]
    public void CellCaseShapeDisagreeingWithFile_IsError()
    {
        var report = ValidateFixture("broken-structure.cells.json");
        report.Findings.Should().Contain(f =>
            f.Severity == CellFindingSeverity.Error
            && f.Check == "axis"
            && f.Location.Contains("bx/shape-mismatch"));
    }

    // ── Decision procedure per contract entry ────────────────────────────────

    [Fact]
    public void ContractEntryWithoutDecisionProcedure_IsError()
    {
        var report = ValidateFixture("broken-structure.cells.json");
        report.Findings.Count(f =>
                f.Severity == CellFindingSeverity.Error
                && f.Check == "decision-procedure"
                && f.Location.Contains("bx/no-decision-procedure"))
            .Should().Be(2, "one entry omits the procedure and one carries only whitespace");
    }

    // ── Citation resolution ──────────────────────────────────────────────────

    [Fact]
    public void CitationToMissingFile_IsError()
    {
        var report = ValidateFixture("broken-citations.cells.json");
        AssertHasError(report, "citation", "no-such-file-ever-2026.md");
        AssertHasError(report, "citation", "also-not-a-real-file.md");
    }

    [Fact]
    public void CitationAnchorMatchingNoHeading_IsError()
    {
        var report = ValidateFixture("broken-citations.cells.json");
        AssertHasError(report, "citation", "No Such Heading Anywhere");
    }

    [Fact]
    public void CitationAnchorMatchingSeveralHeadings_IsError()
    {
        var report = ValidateFixture("broken-citations.cells.json");
        report.Findings.Should().Contain(f =>
            f.Severity == CellFindingSeverity.Error
            && f.Check == "citation"
            && f.Message.Contains("§ Witness"));
    }

    [Fact]
    public void CitationLineOutsideTheFile_IsWarning()
    {
        var report = ValidateFixture("broken-citations.cells.json");
        report.Findings.Should().Contain(f =>
            f.Severity == CellFindingSeverity.Warning
            && f.Check == "citation"
            && f.Message.Contains("99999"));
    }

    [Fact]
    public void ResolvableCitations_ProduceNoFindings()
    {
        var report = ValidateFixture("broken-citations.cells.json");
        report.Findings.Where(f => f.Message.Contains("what-i-want-2026-07-16.md:19"))
            .Should().BeEmpty("an in-range line citation to an existing file resolves");
    }

    /// <summary>
    /// The cell data cites line ranges, comma-separated line lists, and lines
    /// carrying a trailing parenthetical note — the citation style the matrix
    /// prose and the project's own comment-discipline rule already use. A
    /// checker that only recognises a bare single line reports every one of
    /// them as a missing file, which is a false positive loud enough to bury
    /// the real findings.
    /// </summary>
    [Fact]
    public void ExtendedCitationForms_Resolve()
    {
        var report = ValidateFixture("citation-forms.cells.json");
        report.Findings.Where(f => f.Check == "citation")
            .Should().BeEmpty("ranges, lists, and parenthetical notes are citation forms in use, not broken paths");
    }

    /// <summary>
    /// A pruned contract entry records that a premise class is structurally
    /// empty for this cell — it licenses nothing, so there is no reject side to
    /// decide and no derivation whose truth-preservation could be argued.
    /// Demanding a decision procedure from it is demanding one for a discharge
    /// that does not exist.
    /// </summary>
    [Fact]
    public void PrunedContractEntry_NeedsNoDecisionProcedure()
    {
        var report = ValidateFixture("pruned-contract.cells.json");
        report.Findings.Where(f => f.Check == "decision-procedure")
            .Should().BeEmpty("a pruned entry licenses no discharge, so it has no reject side to decide");
    }

    [Fact]
    public void PrunedContractEntry_MustSayWhyItIsPruned()
    {
        var report = ValidateFixture("broken-pruned-contract.cells.json");
        AssertHasError(report, "decision-procedure", "pruningDerivation");
    }

    [Fact]
    public void CitationRangeEndingOutsideTheFile_IsWarning()
    {
        var report = ValidateFixture("broken-citations.cells.json");
        report.Findings.Should().Contain(f =>
            f.Severity == CellFindingSeverity.Warning
            && f.Check == "citation"
            && f.Message.Contains("88888"),
            because: "a range whose end is past the end of the file has moved just as a single line would have");
    }

    // ── Near-miss pairing ────────────────────────────────────────────────────

    [Fact]
    public void DischargeAdditionWithoutNearMiss_IsError()
    {
        var report = ValidateFixture("broken-nearmiss.cells.json");
        AssertHasError(report, "near-miss", "no-nearmiss");
    }

    [Fact]
    public void DischargeAdditionWithTwoNearMisses_IsError()
    {
        var report = ValidateFixture("broken-nearmiss.cells.json");
        AssertHasError(report, "near-miss", "double-nearmiss");
    }

    [Fact]
    public void NearMissPairingWithUnknownDischarge_IsError()
    {
        var report = ValidateFixture("broken-nearmiss.cells.json");
        AssertHasError(report, "near-miss", "ghost-discharge");
    }

    [Fact]
    public void NearMissAgainstStandingDischarge_IsError()
    {
        var report = ValidateFixture("broken-nearmiss.cells.json");
        AssertHasError(report, "near-miss", "standing");
    }

    // ── Respellability resolution ────────────────────────────────────────────

    [Fact]
    public void CellOverrideWithoutOverrideVerdict_IsError()
    {
        var report = ValidateFixture("broken-respellability.cells.json");
        report.Findings.Should().Contain(f =>
            f.Severity == CellFindingSeverity.Error
            && f.Check == "respellability"
            && f.Location.Contains("rs/override-without-verdict"));
    }

    [Fact]
    public void YesVerdictWithBandMemberButNoRespelling_IsError()
    {
        var report = ValidateFixture("broken-respellability.cells.json");
        report.Findings.Should().Contain(f =>
            f.Severity == CellFindingSeverity.Error
            && f.Check == "respellability"
            && f.Location.Contains("rs/band-member-without-respelling"));
    }

    [Fact]
    public void DefinedNonStructuralCellWithoutRespellability_IsError()
    {
        var report = ValidateFixture("broken-respellability.cells.json");
        report.Findings.Should().Contain(f =>
            f.Severity == CellFindingSeverity.Error
            && f.Check == "respellability"
            && f.Location.Contains("rs/defined-without-respellability"));
    }

    // ── WP recomputation ─────────────────────────────────────────────────────

    [Fact]
    public void StoredKeyDisagreeingWithRecomputedWp_IsError()
    {
        var report = ValidateFixture("broken-wp.cells.json");
        report.Findings.Should().Contain(f =>
            f.Severity == CellFindingSeverity.Error
            && f.Check == "wp-recompute"
            && f.Location.Contains("wp/wrong-key")
            && f.Message.Contains("(le arg(Add.Amount1) #1)"));
    }

    [Fact]
    public void MultiWritePlan_YieldsNamedSkipCarryingTheCalculatorsRefusal()
    {
        var report = ValidateFixture("broken-wp.cells.json");

        // The calculator's refusal names the queued write-plan ruling — the skip
        // must carry that specific reason, never a generic unsupported.
        var skip = report.Skips.Should().ContainSingle(s => s.Location.Contains("wp/multi-write")).Subject;
        skip.Check.Should().Be("wp-recompute");
        skip.Reason.Should().Contain("write-plan decomposition");
    }

    [Fact]
    public void CellWithoutBaseProgram_YieldsNamedSkip()
    {
        var report = ValidateFixture("broken-wp.cells.json");
        report.Skips.Should().Contain(s =>
            s.Check == "wp-recompute"
            && s.Location.Contains("wp/no-base")
            && s.Reason.Contains("no base witness program"));
    }

    // ── File handling ────────────────────────────────────────────────────────

    [Fact]
    public void MalformedJson_IsError()
    {
        var report = ValidateFixture("malformed.cells.json");
        report.Findings.Should().ContainSingle().Which.Severity.Should().Be(CellFindingSeverity.Error);
    }

    // ── A defined cell is its components: nothing may pass hollow ────────────

    [Fact]
    public void DefinedCellMissingItsComponents_IsError_NotASilentPass()
    {
        // "Defined" names five components. A cell claiming the status while
        // carrying none of them must not reach ratification green.
        var report = ValidateFixture("broken-hollow-defined.cells.json");

        AssertHasError(report, "schema-arm", "'obligation'");
        AssertHasError(report, "schema-arm", "'dischargeContract'");
        AssertHasError(report, "schema-arm", "'suggestions'");
        AssertHasError(report, "schema-arm", "'witnesses'");
    }

    [Fact]
    public void NonStandingDischargeWithNoAddition_IsError()
    {
        // An omitted `addition` is schema-legal JSON; treating it as the standing
        // case would silently drop the discharge's must-reject near-miss.
        var report = ValidateFixture("broken-hollow-defined.cells.json");

        AssertHasError(report, "near-miss", "records no addition");
    }

    [Fact]
    public void SchemaArmEvaluation_DoesNotFireOnAWellFormedDefinedCell()
    {
        var report = ValidateFixture("valid-minimal.cells.json");
        report.Findings.Should().NotContain(f => f.Check == "schema-arm");
    }

    [Fact]
    public void ValidateDirectory_LoadsEveryCellsFile()
    {
        var report = CellValidator.ValidateDirectory(FixtureDirectory, RepoRoot, SchemaPath);

        // One defect from every broken fixture proves every *.cells.json was loaded.
        report.Findings.Should().Contain(f => f.Location.Contains("broken-structure.cells.json"));
        report.Findings.Should().Contain(f => f.Location.Contains("broken-citations.cells.json"));
        report.Findings.Should().Contain(f => f.Location.Contains("broken-nearmiss.cells.json"));
        report.Findings.Should().Contain(f => f.Location.Contains("broken-respellability.cells.json"));
        report.Findings.Should().Contain(f => f.Location.Contains("broken-wp.cells.json"));
        report.Findings.Should().Contain(f => f.Location.Contains("broken-hollow-defined.cells.json"));
        report.Findings.Should().Contain(f => f.Location.Contains("malformed.cells.json"));
        report.Findings.Should().NotContain(f => f.Location.Contains("valid-minimal.cells.json"));
    }
}
