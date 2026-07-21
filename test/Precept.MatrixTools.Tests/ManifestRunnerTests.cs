using System.IO;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace Precept.MatrixTools.Tests;

/// <summary>
/// Manifest execution tests: run the family-1 manifest through the real
/// compiler pipeline and assert every verdict from structured compile results —
/// proof-ledger obligation records and diagnostic codes, never message text.
/// Rows whose expected strategy is unbuilt at HEAD must come back as confirmed
/// expected failures, keeping delivered-vs-claimed honest.
/// </summary>
public class ManifestRunnerTests
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

    private static System.Collections.Immutable.ImmutableArray<RowResult> Family1Results() =>
        ManifestRunner.Execute(CellTestConverter.Convert(Family1Path));

    [Fact]
    public void Family1_EveryRowGetsExactlyOneResult()
    {
        var results = Family1Results();
        results.Should().HaveCount(11);
        results.Select(r => r.RowId).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Family1_BaseRows_RejectNamingTheObligation()
    {
        var baseResults = Family1Results().Where(r => r.Kind == ManifestRowKind.Base).ToArray();

        baseResults.Should().HaveCount(3);
        foreach (var result in baseResults)
        {
            result.Outcome.Should().Be(RowOutcome.Pass, because:
                $"base row '{result.RowId}' must reject naming the obligation; detail: {result.Detail}");
            result.CompiledClean.Should().BeFalse();
            result.ObligationUnresolved.Should().BeTrue();
        }
    }

    [Fact]
    public void Family1_ArgBoundDischarge_ProvesViaIntervalContainment()
    {
        var result = Family1Results().Single(
            r => r.RowId == "wf1/preservation-reads-args#discharge:arg-bounds");

        result.Outcome.Should().Be(RowOutcome.Pass, because: result.Detail);
        result.CompiledClean.Should().BeTrue();
        result.ProvedStrategy.Should().Be("IntervalContainment");
    }

    [Fact]
    public void Family1_UnbuiltStrategyDischarges_AreConfirmedExpectedFailures()
    {
        var results = Family1Results();

        results.Single(r => r.RowId == "wf1/preservation-reads-args#discharge:guard-nf")
            .Outcome.Should().Be(RowOutcome.ExpectedFailure);
        results.Single(r => r.RowId == "wf1/preservation-reads-written-decrease#discharge:nonnegative-arg")
            .Outcome.Should().Be(RowOutcome.ExpectedFailure);
    }

    [Fact]
    public void Family1_UnstatedBuiltStatusDischarge_IsRecordedAsObservation()
    {
        var result = Family1Results().Single(
            r => r.RowId == "wf1/preservation-reads-written-increase#discharge:guard-nf");

        // The cell data leaves this addition's built status unstated; the runner
        // records what HEAD does without claiming a pass or a failure against it.
        result.Outcome.Should().Be(RowOutcome.UnstatedObservation);
        result.CompiledClean.Should().BeFalse();
    }

    [Fact]
    public void Family1_NearMisses_AllStillRejectTheSameObligation()
    {
        var nearMissResults = Family1Results().Where(r => r.Kind == ManifestRowKind.NearMiss).ToArray();

        nearMissResults.Should().HaveCount(4);
        foreach (var result in nearMissResults)
        {
            result.Outcome.Should().Be(RowOutcome.Pass, because:
                $"near-miss row '{result.RowId}' must still reject, same obligation; detail: {result.Detail}");
            result.CompiledClean.Should().BeFalse();
            result.ObligationUnresolved.Should().BeTrue();
        }
    }

    [Fact]
    public void Family1_HasNoOutrightFailuresAndNoUnexpectedPasses()
    {
        var results = Family1Results();

        results.Should().NotContain(r => r.Outcome == RowOutcome.Fail,
            because: string.Join(" | ", results
                .Where(r => r.Outcome == RowOutcome.Fail)
                .Select(r => $"{r.RowId}: {r.Detail}")));
        results.Should().NotContain(r => r.Outcome == RowOutcome.UnexpectedPass);
    }

    // ── Negative controls: a rejection must be the right rejection ───────────

    private static ManifestRow RejectRow(string id, string program, bool requireMinimality = false) =>
        new(
            Id: id,
            CellId: "negative-control",
            Kind: ManifestRowKind.NearMiss,
            Program: program,
            Expected: ExpectedOutcome.Reject,
            Obligation: new ObligationKey("Total", BoundKind.UpperInclusive, 1000m, "rule[0]",
                ObligationContextClass.WriteSite),
            Head: HeadExpectation.MustPass,
            HeadNote: null,
            ExpectedStrategy: null,
            PairsWith: "guard-nf",
            RequireOnlyObligationDiagnostics: requireMinimality);

    [Fact]
    public void IllFormedWitnessProgram_DoesNotPassAsARejection()
    {
        // A type error is not a rejection for the target obligation. Accepting it
        // would let a broken addition masquerade as a near-miss.
        var result = ManifestRunner.ExecuteRow(RejectRow("negative#type-error", """
            field Total as decimal max 1000 default 0.0
            event Add(Amount1 as decimal, Amount2 as decimal)
            on Add when Add.Amount1 <= "oops" -> set Total = Add.Amount1 + Add.Amount2
            """));

        result.Outcome.Should().Be(RowOutcome.Fail, because: result.Detail);
        result.Detail.Should().Contain("wellFormed=False");
    }

    [Fact]
    public void SecondMatchingWriteSite_DoesNotPassAsSameObligation()
    {
        // The structural key carries no site. With two sites minting a matching
        // obligation, "the same obligation" cannot be established — and the row
        // must say so rather than pass on a different site's rejection.
        var result = ManifestRunner.ExecuteRow(RejectRow("negative#two-sites", """
            field Total as decimal max 1000 default 0.0
            event Add(Amount1 as decimal max 250, Amount2 as decimal max 750)
            event Other(Amount as decimal)
            on Add -> set Total = Add.Amount1 + Add.Amount2
            on Other -> set Total = Other.Amount
            """));

        result.Outcome.Should().Be(RowOutcome.Fail, because: result.Detail);
        result.Detail.Should().Contain("distinct write sites");
    }

    [Fact]
    public void BaseMinimality_FailsWhenAnotherObligationIsAlsoUnresolved()
    {
        // Base minimality is "rejects only for the target obligation" — a second
        // unresolved obligation elsewhere in the program breaks it.
        var result = ManifestRunner.ExecuteRow(RejectRow("negative#second-unresolved", """
            field Total as decimal max 1000 default 0.0
            field Other as decimal max 50 default 0.0
            event Add(Amount1 as decimal, Amount2 as decimal)
            event Bump(Amount as decimal)
            on Add -> set Total = Add.Amount1 + Add.Amount2
            on Bump -> set Other = Bump.Amount
            """, requireMinimality: true));

        result.Outcome.Should().Be(RowOutcome.Fail, because: result.Detail);
        result.Detail.Should().Contain("otherUnresolvedObligations=True");
    }

    [Fact]
    public void UnverifiableContractElements_AreReportedOnTheRow()
    {
        // The base's missing-premise-class list and the discharges' premise-list
        // requirement cannot be checked at HEAD; they must be visible as unchecked.
        var results = Family1Results();

        results.Single(r => r.RowId == "wf1/preservation-reads-args#base")
            .Detail.Should().Contain("unverified at HEAD")
            .And.Contain("missing premise classes");
        results.Single(r => r.RowId == "wf1/preservation-reads-args#discharge:arg-bounds")
            .Detail.Should().Contain("premise list");
    }

    [Fact]
    public void Results_SerializeToJson()
    {
        var json = ManifestRunner.ToJson(Family1Results());

        json.Should().Contain("wf1/preservation-reads-args#discharge:arg-bounds");
        json.Should().Contain("\"outcome\"");
    }
}
