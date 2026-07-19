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

        var obligations = ObligationEnumerator.Enumerate(c.Semantics);
        obligations.Should().ContainSingle().Which.Label.Should().Be("Total:max");

        // max N desugars to rule Total <= N (want doc :22).
        WpCalculator.Canonicalize(obligations[0].Condition).Key
            .Should().Be("(le pre(Total) #1000)");
    }

    [Fact]
    public void Bank_YieldsExplicitRulesAndAllDesugaredModifierRules()
    {
        var c = Px.Compile(BankExampleTests.Bank);
        var labels = ObligationEnumerator.Enumerate(c.Semantics).Select(o => o.Label).ToArray();

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
}
