using FluentAssertions;
using Precept.Pipeline;
using System.Linq;
using Xunit;

namespace Precept.MatrixTools.Tests;

/// <summary>
/// Establishment WPs: the obligation over the default configuration, with
/// defaults substituted for every field the initial write plan does not cover.
/// </summary>
public class EstablishmentTests
{
    private static WpComputed DefaultsWp(Compilation c, string obligation) =>
        WpCalculator.ComputeEstablishmentWp(c.Semantics, [], Px.Obligation(c, obligation)).Wp();

    // ── Default-value checks: declared defaults must satisfy the field's rules ──

    [Fact]
    public void Bank_DailyWithdrawalLimitDefault_SatisfiesPositive()
    {
        var c = Px.Compile(BankExampleTests.Bank);
        DefaultsWp(c, "DailyWithdrawalLimit:positive").Wp.Should().Be(new CanonBool(true));
    }

    [Fact]
    public void Bank_DailyWithdrawalLimitDefault_SatisfiesMax()
    {
        var c = Px.Compile(BankExampleTests.Bank);
        DefaultsWp(c, "DailyWithdrawalLimit:max").Wp.Should().Be(new CanonBool(true));
    }

    [Fact]
    public void Bank_RepaymentCapPercentDefault_SatisfiesPositiveAndMax()
    {
        var c = Px.Compile(BankExampleTests.Bank);
        DefaultsWp(c, "RepaymentCapPercent:positive").Wp.Should().Be(new CanonBool(true));
        DefaultsWp(c, "RepaymentCapPercent:max").Wp.Should().Be(new CanonBool(true));
    }

    [Fact]
    public void Bank_ViolatingDefault_FoldsToFalse()
    {
        // A default violating its own max: the base case has a counterexample,
        // and the establishment WP evaluates to false.
        var c = Px.Compile(BankExampleTests.Bank.Replace("default 500.0", "default 20000.0"));
        DefaultsWp(c, "DailyWithdrawalLimit:max").Wp.Should().Be(new CanonBool(false));
    }

    // ── Fields without default or write stay symbolic (unset marker) ─────────

    [Fact]
    public void Bank_OverdraftRuleOverDefaults_StaysSymbolic()
    {
        var c = Px.Compile(BankExampleTests.Bank);
        var wp = DefaultsWp(c, "rule[0]");
        wp.Wp.Should().NotBeOfType<CanonBool>();
        wp.Wp.Key.Should().Contain("unset");
    }

    [Fact]
    public void OptionalFieldModifier_EstablishmentIsPresenceConditioned()
    {
        // A modifier on an optional field constrains the value only when one is
        // present, so its desugared rule activates on `Field is set`. Over the
        // default configuration of an optional no-default field, the activation
        // folds to false and the implication survives (vacuous discharge is the
        // discharge contract's call, not the calculator's).
        var c = Px.Compile("""
            field Total as decimal default 0.0
            field Extra as decimal optional nonnegative
            event Touch(Amount as decimal)
            on Touch -> set Total = Touch.Amount
            """);

        var implies = DefaultsWp(c, "Extra:nonnegative").Wp
            .Should().BeOfType<CanonImplies>().Subject;
        implies.Antecedent.Should().Be(new CanonBool(false));
    }

    [Fact]
    public void Bank_ConditionalRuleOverDefaults_AntecedentFoldsButImplicationSurvives()
    {
        // MonthlyRepayment is optional with no default: `MonthlyRepayment is set`
        // folds to false over the default configuration — but the implication is
        // never folded away (vacuity semantics is cell content, not normalization).
        var c = Px.Compile(BankExampleTests.Bank);
        var implies = DefaultsWp(c, "rule[1]").Wp.Should().BeOfType<CanonImplies>().Subject;
        implies.Antecedent.Should().Be(new CanonBool(false));
    }

    // ── Establishment through a single-write initial plan ────────────────────

    private const string SingleWriteInitial = """
        precept T
        field A as decimal nonnegative
        field B as decimal default 5.0 max 10.0
        state S initial
        state Done terminal
        event Boot(X as decimal nonnegative) initial
        on Boot -> set A = Boot.X
        event Stop
        from S on Stop -> transition Done
        """;

    [Fact]
    public void SingleWritePlan_SubstitutesTheWrittenArg()
    {
        var c = Px.Compile(SingleWriteInitial);
        var boot = c.Semantics.EventHandlers.OfType<TypedEventRowSuccess>()
            .First(r => r.EventName == "Boot");

        var wp = WpCalculator
            .ComputeEstablishmentWp(c.Semantics, boot.Actions, Px.Obligation(c, "A:nonnegative"))
            .Wp();

        wp.Wp.Key.Should().Be("(le #0 arg(Boot.X))");
    }

    [Fact]
    public void SingleWritePlan_UncoveredFieldTakesItsDefault()
    {
        var c = Px.Compile(SingleWriteInitial);
        var boot = c.Semantics.EventHandlers.OfType<TypedEventRowSuccess>()
            .First(r => r.EventName == "Boot");

        var wp = WpCalculator
            .ComputeEstablishmentWp(c.Semantics, boot.Actions, Px.Obligation(c, "B:max"))
            .Wp();

        wp.Wp.Should().Be(new CanonBool(true)); // 5.0 <= 10.0
    }
}
