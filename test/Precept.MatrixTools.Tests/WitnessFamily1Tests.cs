using FluentAssertions;
using Precept.Pipeline;
using Xunit;

namespace Precept.MatrixTools.Tests;

/// <summary>
/// Matrix Witness Family 1 — numeric single-field, handler set
/// (docs/Working/obligation-discharge-matrix-2026-07-19.md, three RHS read-set classes).
/// The obligation is the modifier-desugared rule Total &lt;= 1000 (max on Total).
/// </summary>
public class WitnessFamily1Tests
{
    private const string TotalDecl = "field Total as decimal max 1000 default 0.0\n";
    private const string MaxObligation = "Total:max";

    private static (WpComputed Wp, TypedExpression? Guard) WpFor(string source, string eventName)
    {
        var c = Px.Compile(source);
        var row = Px.EventRow(c, eventName);
        var wp = WpCalculator.ComputePreservationWp(row.Actions, Px.Obligation(c, MaxObligation)).Wp();
        return (wp, row.Guard);
    }

    // ── Base A — RHS reads args only ─────────────────────────────────────────

    private const string BaseA = TotalDecl + """
        event Add(Amount1 as decimal, Amount2 as decimal)
        on Add{GUARD} -> set Total = Add.Amount1 + Add.Amount2
        """;

    private static string BaseAWith(string guard) => BaseA.Replace("{GUARD}", guard);

    [Fact]
    public void BaseA_WpIsArgSumBound()
    {
        var (wp, _) = WpFor(BaseAWith(""), "Add");
        wp.Wp.Key.Should().Be("(le (+ arg(Add.Amount1) arg(Add.Amount2)) #1000)");
    }

    [Fact]
    public void BaseA_LicensedGuardMatchesWp()
    {
        var (wp, guard) = WpFor(BaseAWith(" when Add.Amount1 + Add.Amount2 <= 1000"), "Add");
        WpCalculator.GuardMatchesWp(guard!, wp.Wp).Should().BeTrue();
    }

    [Fact]
    public void BaseA_NearMiss_SingleTermGuardDoesNotMatch()
    {
        // True near-miss: the other term is unbounded; no derivation closes it.
        var (wp, guard) = WpFor(BaseAWith(" when Add.Amount1 <= 1000"), "Add");
        WpCalculator.GuardMatchesWp(guard!, wp.Wp).Should().BeFalse();
        WpCalculator.GuardFactsCoverWp(guard!, wp.Wp).Should().BeFalse();
    }

    [Fact]
    public void BaseA_PerTermBoundGuardIsNotASubstitutionMatch()
    {
        // Owner ruling: per-term bound conjuncts (term-compare-constant) are a
        // LICENSED premise — they discharge via the bound-extraction /
        // interval-arithmetic derivation, exactly like arg max bounds. They are
        // still NOT normal-form-equal to the WP: the whole-condition match is
        // only one derivation, and this guard is genuinely a different statement.
        var (wp, guard) = WpFor(BaseAWith(" when Add.Amount1 <= 500 and Add.Amount2 <= 500"), "Add");
        WpCalculator.GuardMatchesWp(guard!, wp.Wp).Should().BeFalse();
        WpCalculator.GuardFactsCoverWp(guard!, wp.Wp).Should().BeFalse();

        // The licensed derivation's input is exposed by the bound-fact extractor.
        var facts = WpCalculator.ExtractBoundFacts(guard!);
        facts.Should().HaveCount(2);
        facts.Should().ContainSingle(f =>
            f.Term.Key == "arg(Add.Amount1)" && f.Kind == BoundKind.UpperInclusive && f.Bound == 500m);
        facts.Should().ContainSingle(f =>
            f.Term.Key == "arg(Add.Amount2)" && f.Kind == BoundKind.UpperInclusive && f.Bound == 500m);
    }

    // ── Base B — RHS reads the written field, decrease ───────────────────────

    private const string BaseB = TotalDecl + """
        event Subtract(Amount as decimal nonnegative)
        on Subtract{GUARD} -> set Total = Total - Subtract.Amount
        """;

    [Fact]
    public void BaseB_WpReadsPreStateTotal()
    {
        var (wp, _) = WpFor(BaseB.Replace("{GUARD}", ""), "Subtract");
        wp.Wp.Key.Should().Be("(le (+ (neg arg(Subtract.Amount)) pre(Total)) #1000)");
    }

    [Fact]
    public void BaseB_RestatedGuardMatchesWp()
    {
        var (wp, guard) = WpFor(
            BaseB.Replace("{GUARD}", " when Total - Subtract.Amount <= 1000"), "Subtract");
        WpCalculator.GuardMatchesWp(guard!, wp.Wp).Should().BeTrue();
    }

    // ── Base C — RHS reads the written field, increase ───────────────────────

    private const string BaseC = TotalDecl + """
        event Grow(Amount as decimal positive)
        on Grow{GUARD} -> set Total = Total + Grow.Amount
        """;

    [Fact]
    public void BaseC_LicensedGuardMatchesWp()
    {
        var (wp, guard) = WpFor(
            BaseC.Replace("{GUARD}", " when Total + Grow.Amount <= 1000"), "Grow");
        wp.Wp.Key.Should().Be("(le (+ arg(Grow.Amount) pre(Total)) #1000)");
        WpCalculator.GuardMatchesWp(guard!, wp.Wp).Should().BeTrue();
    }

    [Fact]
    public void BaseC_NearMiss_GuardIgnoringPreStateDoesNotMatch()
    {
        var (wp, guard) = WpFor(BaseC.Replace("{GUARD}", " when Grow.Amount <= 1000"), "Grow");
        WpCalculator.GuardMatchesWp(guard!, wp.Wp).Should().BeFalse();
        WpCalculator.GuardFactsCoverWp(guard!, wp.Wp).Should().BeFalse();
    }

    // ── Standing base-case obligation: establishment over defaults ───────────

    [Fact]
    public void Establishment_DefaultSatisfiesMax_FoldsToTrue()
    {
        var c = Px.Compile(BaseAWith(""));
        var wp = WpCalculator
            .ComputeEstablishmentWp(c.Semantics, [], Px.Obligation(c, MaxObligation))
            .Wp();
        wp.Wp.Should().Be(new CanonBool(true));
    }
}
