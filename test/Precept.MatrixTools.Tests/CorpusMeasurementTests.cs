using System.IO;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace Precept.MatrixTools.Tests;

/// <summary>
/// The corpus measurement harness classifies every rule × write-site obligation
/// against the discharge derivations the WP calculator licenses today. Each
/// fixture under Fixtures/corpus exercises exactly one classification bucket;
/// misclassification here would corrupt ratification evidence, so every bucket
/// is pinned by a fixture.
/// </summary>
public class CorpusMeasurementTests
{
    private static readonly string RepoRoot = FindRepoRoot();
    private static readonly string FixtureDirectory = Path.Combine(
        RepoRoot, "test", "Precept.MatrixTools.Tests", "Fixtures", "corpus");

    private static string FindRepoRoot()
    {
        var directory = new DirectoryInfo(System.AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Precept.slnx")))
            directory = directory.Parent;
        return directory?.FullName
            ?? throw new Xunit.Sdk.XunitException("repository root (Precept.slnx) not found above the test directory");
    }

    private static CorpusFileReport Measure(string fixtureName)
    {
        var path = Path.Combine(FixtureDirectory, fixtureName);
        return CorpusMeasurement.MeasureFile(fixtureName, File.ReadAllText(path));
    }

    private static CorpusObligationRow Row(CorpusFileReport report, string obligation, string siteContains) =>
        report.Rows.Should().ContainSingle(r => r.Obligation == obligation && r.Site.Contains(siteContains)).Subject;

    // ── Licensed-as-written, one fixture per derivation ──────────────────────

    [Fact]
    public void WholeGuardMatch_IsLicensed()
    {
        var report = Measure("licensed-whole-guard.precept");
        var row = Row(report, "rule[0]", "preservation: on Grow");
        row.Classification.Should().Be(CorpusClassification.LicensedAsWritten);
        row.Derivation.Should().Be(CorpusDerivations.WholeGuardMatch);
    }

    [Fact]
    public void GuardConjunctCoverage_IsLicensed()
    {
        var report = Measure("licensed-guard-coverage.precept");
        var row = Row(report, "rule[0]", "preservation: on Grow");
        row.Classification.Should().Be(CorpusClassification.LicensedAsWritten);
        row.Derivation.Should().Be(CorpusDerivations.GuardConjunctCoverage);
    }

    [Fact]
    public void ArgBoundIntervalSum_IsLicensed()
    {
        var report = Measure("licensed-arg-bounds.precept");
        var row = Row(report, "rule[0]", "preservation: on Add");
        row.Classification.Should().Be(CorpusClassification.LicensedAsWritten);
        row.Derivation.Should().Be(CorpusDerivations.ArgBoundIntervalSum);
    }

    [Fact]
    public void GuardBoundConjunctInterval_IsLicensed()
    {
        var report = Measure("licensed-guard-interval.precept");
        var row = Row(report, "rule[0]", "preservation: on Add");
        row.Classification.Should().Be(CorpusClassification.LicensedAsWritten);
        row.Derivation.Should().Be(CorpusDerivations.GuardBoundConjunctInterval);
    }

    [Fact]
    public void HypothesisDecrease_IsLicensed()
    {
        var report = Measure("licensed-hypothesis-decrease.precept");
        var row = Row(report, "rule[0]", "preservation: on Subtract");
        row.Classification.Should().Be(CorpusClassification.LicensedAsWritten);
        row.Derivation.Should().Be(CorpusDerivations.HypothesisDecrease);
    }

    [Fact]
    public void EstablishmentOverDefaults_FoldsToTrue_IsLicensed()
    {
        var report = Measure("licensed-whole-guard.precept");
        var row = Row(report, "rule[0]", "establishment: defaults");
        row.Classification.Should().Be(CorpusClassification.LicensedAsWritten);
        row.Derivation.Should().Be(CorpusDerivations.EstablishmentLiteralFold);
    }

    [Fact]
    public void EstablishmentThroughConstructionRow_FoldsToTrue_IsLicensed()
    {
        var report = Measure("out-of-scope-editable.precept");
        var row = Row(report, "rule[0]", "establishment: on Open");
        row.Classification.Should().Be(CorpusClassification.LicensedAsWritten);
        row.Derivation.Should().Be(CorpusDerivations.EstablishmentLiteralFold);
    }

    // ── Respell-needed: not licensed, suggestion schema recorded ─────────────

    [Fact]
    public void UnguardedIncrease_IsRespellNeeded_WithBothSuggestions()
    {
        var report = Measure("respell-needed.precept");
        var row = Row(report, "rule[0]", "preservation: on Grow");
        row.Classification.Should().Be(CorpusClassification.RespellNeeded);
        row.Respellings.Should().Contain(s => s.StartsWith("add a guard normal-form-equal to:"));
        row.Respellings.Should().Contain(s => s.StartsWith("bound the args"));
        row.Wp.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void AlgebraicallyRearrangedGuard_IsRespellNeeded()
    {
        // `when Add.Amount1 <= 1000 - Add.Amount2` is sound but connected to the
        // WP only by term movement across the inequality — not a licensed spelling.
        var report = Measure("respell-needed.precept");
        var row = Row(report, "rule[0]", "preservation: on Add");
        row.Classification.Should().Be(CorpusClassification.RespellNeeded);
        row.Respellings.Should().NotBeEmpty();
    }

    // ── No licensed respelling ───────────────────────────────────────────────

    [Fact]
    public void GroundFalseWps_HaveNoLicensedRespelling()
    {
        var report = Measure("no-licensed-respelling.precept");
        Row(report, "rule[0]", "establishment: defaults").Classification
            .Should().Be(CorpusClassification.NoLicensedRespelling);
        Row(report, "rule[0]", "preservation: on Reset").Classification
            .Should().Be(CorpusClassification.NoLicensedRespelling);
        report.ErrorDiagnostics.Should().BeGreaterThan(0);
    }

    // ── Out-of-scope, each with its named reason ─────────────────────────────

    [Fact]
    public void MultiWritePlan_IsOutOfScope_NamingTheWritePlanRuling()
    {
        var report = Measure("out-of-scope-multiwrite.precept");
        var row = Row(report, "rule[0]", "preservation: on Move");
        row.Classification.Should().Be(CorpusClassification.OutOfScope);
        row.Reason.Should().Be(CorpusReasons.MultiWritePlan);
        row.Reason.Should().Contain("write-plan");
    }

    [Fact]
    public void RelationalRule_IsOutOfScope()
    {
        var report = Measure("out-of-scope-relational.precept");
        var row = Row(report, "rule[0]", "preservation: on SetLimit");
        row.Classification.Should().Be(CorpusClassification.OutOfScope);
        row.Reason.Should().Be(CorpusReasons.RelationalRule);
    }

    [Fact]
    public void ActivationConditionedObligations_AreOutOfScope()
    {
        var report = Measure("out-of-scope-conditional.precept");
        Row(report, "rule[0]", "preservation: on SetFee").Reason
            .Should().Be(CorpusReasons.ConditionalRule);
        Row(report, "Fee:nonnegative", "preservation: on SetFee").Reason
            .Should().Be(CorpusReasons.ConditionalRule);
    }

    [Fact]
    public void QuantifiedRule_IsOutOfScope()
    {
        var report = Measure("out-of-scope-quantified.precept");
        var row = Row(report, "rule[0]", "establishment: defaults");
        row.Classification.Should().Be(CorpusClassification.OutOfScope);
        row.Reason.Should().Be(CorpusReasons.QuantifiedRule);
    }

    [Fact]
    public void EditableSiteAndEntryHook_AreOutOfScope()
    {
        var report = Measure("out-of-scope-editable.precept");
        Row(report, "rule[0]", "editable: Limit in Active").Reason
            .Should().Be(CorpusReasons.EditableSite);
        Row(report, "rule[0]", "hook: entry Closed").Reason
            .Should().Be(CorpusReasons.StateHookSite);
    }

    [Fact]
    public void EnumeratorSkips_SurfaceAsNamedRecords()
    {
        var report = Measure("enumeration-skips.precept");
        var min = Row(report, "A:min", "(enumeration)");
        min.Reason.Should().Be(CorpusReasons.EnumerationSkip);
        min.Detail.Should().Contain("cross-field");
        Row(report, "S:maxlength", "(enumeration)").Detail.Should().Contain("accessor-projected");
    }

    [Fact]
    public void Ensures_SurfaceAsNamedRecords()
    {
        var report = Measure("ensure-note.precept");
        var row = Row(report, "ensure[0]", "(enumeration)");
        row.Classification.Should().Be(CorpusClassification.OutOfScope);
        row.Reason.Should().Be(CorpusReasons.EnsureNotEnumerated);
        report.Ensures.Should().Be(1);
    }

    // ── Ruled-but-unimplemented normal-form rules surface as named skips ─────

    [Fact]
    public void UnfoldedLiteralDivision_IsANamedSkip()
    {
        var report = Measure("gap-division.precept");
        var row = Row(report, "rule[0]", "preservation: on Half");
        row.Classification.Should().Be(CorpusClassification.OutOfScope);
        row.Reason.Should().Be(CorpusReasons.LiteralDivisionUnfolded);
    }

    [Fact]
    public void NegatedOrderingComparison_IsANamedSkip()
    {
        var report = Measure("gap-negated-ordering.precept");
        var row = Row(report, "rule[0]", "preservation: on Assign");
        row.Classification.Should().Be(CorpusClassification.OutOfScope);
        row.Reason.Should().Be(CorpusReasons.NegatedOrderingUnflipped);
    }

    // ── Derivations the definition does not state are never licensed ─────────

    [Fact]
    public void LowerBoundIntervalMirror_IsNotLicensed_ButANamedSkip()
    {
        // `0 <= NewLimit` closes only by summing the arg's declared lower bound.
        // The written class-(b) procedure sums declared max bounds upward; the
        // mirror is unstated, so licensing it would inflate the ratification count.
        var report = Measure("gap-lower-bound-mirror.precept");
        var row = Row(report, "rule[0]", "preservation: on Reduce");
        row.Classification.Should().Be(CorpusClassification.OutOfScope);
        row.Reason.Should().Be(CorpusReasons.LowerBoundIntervalUnstated);
        row.Derivation.Should().BeNull();
    }

    [Fact]
    public void NumberLaneObligations_AreOutOfScope_NotLicensed()
    {
        // The interval and fold derivations rest on exact arithmetic; IEEE doubles
        // round, and the outward-rounding side condition is not written.
        var report = Measure("out-of-scope-number-lane.precept");
        Row(report, "Ratio:max", "preservation: on Assign").Reason
            .Should().Be(CorpusReasons.NumberLane);
        Row(report, "Ratio:max", "establishment: defaults").Reason
            .Should().Be(CorpusReasons.NumberLane);
        report.Rows.Should().NotContain(r => r.Classification == CorpusClassification.LicensedAsWritten);
    }

    // ── Frame preservation: pairs where the plan misses the mention set ──────

    [Fact]
    public void WriteOutsideTheMentionSet_IsFramePreserved_NotAnObligation()
    {
        var report = CorpusMeasurement.MeasureFile("frame.precept", """
            field A as decimal default 0.0
            field B as decimal default 0.0
            rule A <= 100 because "cap"
            event Touch(X as decimal)
            on Touch -> set B = Touch.X
            """);
        report.FramePreservedPairs.Should().Be(1);
        report.Rows.Should().NotContain(r => r.Site.Contains("preservation: on Touch"));
    }

    // ── Directory aggregation, JSON, markdown ────────────────────────────────

    [Fact]
    public void MeasureDirectory_CountsFilesAndRuleBearing()
    {
        var report = CorpusMeasurement.MeasureDirectory(FixtureDirectory);
        report.FileCount.Should().Be(18);
        report.RuleBearingFileCount.Should().Be(18);
        report.ClassificationTotals.Values.Sum()
            .Should().Be(report.Files.Sum(f => f.Rows.Length));
    }

    [Fact]
    public void Json_RoundTripsTheHeadlineCounts()
    {
        var report = CorpusMeasurement.MeasureDirectory(FixtureDirectory);
        using var doc = JsonDocument.Parse(CorpusMeasurement.ToJson(report));
        doc.RootElement.GetProperty("fileCount").GetInt32().Should().Be(report.FileCount);
        doc.RootElement.GetProperty("classificationTotals").EnumerateObject().Should().NotBeEmpty();
        doc.RootElement.GetProperty("files").GetArrayLength().Should().Be(report.FileCount);
    }

    [Fact]
    public void Markdown_CarriesTotalsAndPerFileRows()
    {
        var report = CorpusMeasurement.MeasureDirectory(FixtureDirectory);
        var markdown = CorpusMeasurement.ToMarkdown(report);
        markdown.Should().Contain("licensed as written");
        markdown.Should().Contain("respell needed");
        markdown.Should().Contain("licensed-whole-guard.precept");
        markdown.Should().Contain(CorpusReasons.MultiWritePlan);
    }
}
