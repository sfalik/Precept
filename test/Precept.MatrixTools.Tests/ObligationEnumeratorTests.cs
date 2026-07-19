using System.Linq;
using FluentAssertions;
using Xunit;

namespace Precept.MatrixTools.Tests;

public class ObligationEnumeratorTests
{
    [Fact]
    public void Test2Shape_YieldsTheDesugaredMaxRule()
    {
        var c = Px.Compile("""
            field Total as decimal max 1000 default 0.0
            event Add(Amount1 as decimal, Amount2 as decimal)
            on Add -> set Total = Add.Amount1 + Add.Amount2
            """);

        var specs = ObligationEnumerator.Enumerate(c.Semantics)
            .OfType<ObligationSpec>().ToArray();
        specs.Should().ContainSingle().Which.Label.Should().Be("Total:max");

        // Modifiers are sugar for rules: max N desugars to rule Total <= N.
        WpCalculator.Canonicalize(specs[0].Condition).Key
            .Should().Be("(le pre(Total) #1000)");
    }

    [Fact]
    public void Bank_YieldsExplicitRulesAndAllDesugaredModifierRules()
    {
        var c = Px.Compile(BankExampleTests.Bank);
        var labels = ObligationEnumerator.Enumerate(c.Semantics)
            .OfType<ObligationSpec>().Select(o => o.Label).ToArray();

        labels.Should().BeEquivalentTo(
        [
            "rule[0]",
            "rule[1]",
            "OverdraftLimit:nonnegative",
            "MonthlyRepayment:nonnegative",
            "RepaymentCapPercent:positive",
            "RepaymentCapPercent:max",
            "DailyWithdrawalLimit:positive",
            "DailyWithdrawalLimit:max",
        ]);
    }

    [Fact]
    public void DesugaredSignFlag_UsesTheCatalogComparison()
    {
        var c = Px.Compile(BankExampleTests.Bank);

        // nonnegative: value >= 0 → canonical (le #0 pre(F))
        WpCalculator.Canonicalize(Px.Obligation(c, "OverdraftLimit:nonnegative").Condition).Key
            .Should().Be("(le #0 pre(OverdraftLimit))");

        // positive: value > 0 → canonical (lt #0 pre(F))
        WpCalculator.Canonicalize(Px.Obligation(c, "RepaymentCapPercent:positive").Condition).Key
            .Should().Be("(lt #0 pre(RepaymentCapPercent))");
    }

    [Fact]
    public void ExplicitRules_CarryTheirActivationCondition()
    {
        var c = Px.Compile(BankExampleTests.Bank);
        Px.Obligation(c, "rule[0]").ActivationCondition.Should().BeNull();
        Px.Obligation(c, "rule[1]").ActivationCondition.Should().NotBeNull();
    }

    [Fact]
    public void OptionalFieldModifiers_ActivateOnPresence()
    {
        // A modifier on an optional field is presence-conditioned: the value is
        // constrained only when one is present.
        var c = Px.Compile(BankExampleTests.Bank);
        var activation = Px.Obligation(c, "MonthlyRepayment:nonnegative").ActivationCondition;
        activation.Should().NotBeNull();
        WpCalculator.Canonicalize(activation!).Key.Should().Be("(isset pre(MonthlyRepayment))");
    }

    [Fact]
    public void OutOfScopeDesugars_AreExplicitSkipsNotSilences()
    {
        // Cross-field bounds, accessor-projected bounds (length), and
        // satisfaction-less desugars (maxplaces) must each surface as a skip
        // record — never vanish.
        var c = Px.Compile("""
            field B as decimal default 0.0
            field A as decimal min B default 1.0
            field S as string maxlength 10 default "x"
            field P as decimal maxplaces 2 default 0.5
            event Touch(Amount as decimal)
            on Touch -> set B = Touch.Amount
            """);

        var skips = ObligationEnumerator.Enumerate(c.Semantics)
            .OfType<ObligationSkipped>().ToArray();

        skips.Should().Contain(s => s.Label == "A:min")
            .Which.Reason.Should().Contain("cross-field");
        skips.Should().Contain(s => s.Label == "S:maxlength")
            .Which.Reason.Should().Contain("accessor-projected");
        skips.Should().Contain(s => s.Label == "P:maxplaces")
            .Which.Reason.Should().Contain("no proof-satisfaction");
    }
}
