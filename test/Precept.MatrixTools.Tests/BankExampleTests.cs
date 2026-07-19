using System.Linq;
using FluentAssertions;
using Precept.Pipeline;
using Xunit;

namespace Precept.MatrixTools.Tests;

/// <summary>
/// A bank-account invariant precept in which every handler needs a different
/// premise class (arg bounds, the inductive hypothesis, guard restatement,
/// symmetric write sites, non-linear guard restatement) — the known-answer
/// source for preservation WPs, guard matching, and the multi-write gate.
/// </summary>
public class BankExampleTests
{
    internal const string Bank = """
        precept BankAccountInvariant

        field OverdraftLimit as decimal nonnegative
        field Balance as decimal
        field MonthlyRepayment as decimal optional nonnegative
        field RepaymentCapPercent as decimal default 0.25 positive max 1.0 editable
        field DailyWithdrawalLimit as decimal default 500.0 positive max 10000.0

        rule Balance >= -OverdraftLimit because "Balance cannot go below the account's overdraft limit"
        rule MonthlyRepayment <= OverdraftLimit * RepaymentCapPercent when MonthlyRepayment is set because "A planned repayment may not exceed the capped fraction of the overdraft facility"

        state Active initial
        state Frozen
        state Closed terminal

        in Active modify DailyWithdrawalLimit editable

        event OpenAccount(OpeningBalance as decimal min 0.0, OverdraftLimit as decimal nonnegative) initial
        on OpenAccount
          -> set Balance = OpenAccount.OpeningBalance
          -> set OverdraftLimit = OpenAccount.OverdraftLimit

        event Deposit(Amount as decimal nonnegative)
        from Active on Deposit
          -> set Balance = Balance + Deposit.Amount
          -> no transition

        event Withdraw(Amount as decimal)
        from Active on Withdraw when Balance - Withdraw.Amount >= -OverdraftLimit and Withdraw.Amount <= DailyWithdrawalLimit
          -> set Balance = Balance - Withdraw.Amount
          -> no transition
        from Active on Withdraw when Withdraw.Amount > DailyWithdrawalLimit
          -> reject "Withdrawal of {Withdraw.Amount} exceeds the daily limit of {DailyWithdrawalLimit}"
        from Active on Withdraw
          -> reject "Withdrawing {Withdraw.Amount} would exceed the overdraft limit (balance {Balance}, limit {OverdraftLimit})"

        event PlanRepayment(Months as integer positive)
        from Active on PlanRepayment when Balance < 0.0 and -Balance / PlanRepayment.Months <= OverdraftLimit * RepaymentCapPercent
          -> set MonthlyRepayment = -Balance / PlanRepayment.Months
          -> no transition
        from Active on PlanRepayment when Balance < 0.0
          -> reject "A monthly repayment of {-Balance / PlanRepayment.Months} would exceed the capped fraction ({RepaymentCapPercent}) of the overdraft facility (limit {OverdraftLimit})"

        event ReduceLimit(NewLimit as decimal nonnegative)
        from Active on ReduceLimit when Balance >= -ReduceLimit.NewLimit and MonthlyRepayment <= ReduceLimit.NewLimit * RepaymentCapPercent
          -> set OverdraftLimit = ReduceLimit.NewLimit
          -> no transition
        from Active on ReduceLimit when Balance < -ReduceLimit.NewLimit
          -> reject "Cannot reduce the overdraft limit to {ReduceLimit.NewLimit} while the balance is {Balance}"
        from Active on ReduceLimit
          -> reject "Cannot reduce the overdraft limit to {ReduceLimit.NewLimit}: a planned repayment of {MonthlyRepayment} would exceed the capped fraction ({RepaymentCapPercent}) of the reduced facility"

        event Freeze
        from Active on Freeze
          -> transition Frozen

        event Unfreeze
        from Frozen on Unfreeze
          -> transition Active

        event CloseAccount
        from Active on CloseAccount when Balance == 0.0
          -> transition Closed
        from Active on CloseAccount
          -> reject "Cannot close the account while the balance is {Balance} - settle to exactly zero first"
        """;

    private const string OverdraftRule = "rule[0]";   // Balance >= -OverdraftLimit
    private const string CapRule       = "rule[1]";   // MonthlyRepayment <= OverdraftLimit * RepaymentCapPercent when MonthlyRepayment is set

    private static WpComputed PreservationWp(Compilation c, string eventName, string obligation) =>
        WpCalculator
            .ComputePreservationWp(Px.TransitionRow(c, eventName).Actions, Px.Obligation(c, obligation))
            .Wp();

    // ── Withdraw — the guard's first conjunct IS the WP ──────────────────────

    [Fact]
    public void Withdraw_GuardFactCoversWp()
    {
        var c = Px.Compile(Bank);
        var wp = PreservationWp(c, "Withdraw", OverdraftRule);
        var guard = Px.TransitionRow(c, "Withdraw").Guard!;

        // Whole-guard match fails (the guard carries a second, daily-limit fact) …
        WpCalculator.GuardMatchesWp(guard, wp.Wp).Should().BeFalse();
        // … but the WP is one of the guard's facts.
        WpCalculator.GuardFactsCoverWp(guard, wp.Wp).Should().BeTrue();
    }

    // ── ReduceLimit — the symmetric write site; substitution into the rule ───

    [Fact]
    public void ReduceLimit_WpSubstitutesNewLimit()
    {
        var c = Px.Compile(Bank);
        var wp = PreservationWp(c, "ReduceLimit", OverdraftRule);

        // Balance >= -NewLimit, canonicalized (flip): -NewLimit <= Balance
        wp.Wp.Key.Should().Be("(le (neg arg(ReduceLimit.NewLimit)) pre(Balance))");
        WpCalculator.GuardFactsCoverWp(Px.TransitionRow(c, "ReduceLimit").Guard!, wp.Wp)
            .Should().BeTrue();
    }

    [Fact]
    public void ReduceLimit_NearMiss_GuardingTheOldLimitDoesNotCover()
    {
        // Near-miss: the guard reads the *old* limit instead of the incoming one.
        var nearMiss = Bank.Replace(
            "when Balance >= -ReduceLimit.NewLimit and",
            "when Balance >= -OverdraftLimit and");
        var c = Px.Compile(nearMiss);
        var wp = PreservationWp(c, "ReduceLimit", OverdraftRule);

        WpCalculator.GuardFactsCoverWp(Px.TransitionRow(c, "ReduceLimit").Guard!, wp.Wp)
            .Should().BeFalse();
    }

    // ── Deposit — WP needs the inductive hypothesis; guard restatement matches ──

    [Fact]
    public void Deposit_WpIsPreStateBalancePlusAmount()
    {
        var c = Px.Compile(Bank);
        var wp = PreservationWp(c, "Deposit", OverdraftRule);

        // Twin fixture: the same row spelled with the WP as its guard must match.
        var twin = Px.Compile(Bank.Replace(
            "from Active on Deposit\n",
            "from Active on Deposit when Balance + Deposit.Amount >= -OverdraftLimit\n"));
        var twinGuard = Px.TransitionRow(twin, "Deposit").Guard!;

        WpCalculator.GuardMatchesWp(twinGuard, wp.Wp).Should().BeTrue();
    }

    // ── Non-linear rows — substitution over a product/quotient ───────────────

    [Fact]
    public void PlanRepayment_CapRuleWp_IsImplicationWhoseConsequentIsTheGuardConjunct()
    {
        var c = Px.Compile(Bank);
        var wp = PreservationWp(c, "PlanRepayment", CapRule);

        var implies = wp.Wp.Should().BeOfType<CanonImplies>().Subject;
        // Vacuity is not decided by the calculator: the substituted activation
        // condition stays symbolic (the written quotient is not a literal).
        implies.Antecedent.Should().BeOfType<CanonIsSet>();

        // The guard's restating conjunct is exactly the WP's consequent.
        var guardFacts = WpCalculator.ConjunctsOf(
            WpCalculator.Canonicalize(Px.TransitionRow(c, "PlanRepayment").Guard!));
        guardFacts.Select(f => f.Key).Should().Contain(implies.Consequent.Key);
    }

    [Fact]
    public void ReduceLimit_CapRuleWp_ConsequentMatchesRestatingConjunct()
    {
        var c = Px.Compile(Bank);
        var wp = PreservationWp(c, "ReduceLimit", CapRule);

        var implies = wp.Wp.Should().BeOfType<CanonImplies>().Subject;
        // MonthlyRepayment is untouched by this write: activation stays on the pre-state field.
        implies.Antecedent.Key.Should().Be("(isset pre(MonthlyRepayment))");
        implies.Consequent.Key.Should().Be(
            "(le pre(MonthlyRepayment) (* arg(ReduceLimit.NewLimit) pre(RepaymentCapPercent)))");

        var guardFacts = WpCalculator.ConjunctsOf(
            WpCalculator.Canonicalize(Px.TransitionRow(c, "ReduceLimit").Guard!));
        guardFacts.Select(f => f.Key).Should().Contain(implies.Consequent.Key);
    }

    // ── No-write rows frame-preserve ─────────────────────────────────────────

    [Fact]
    public void Freeze_WritesNothing_WpIsTheRuleItself()
    {
        var c = Px.Compile(Bank);
        var freeze = c.Semantics.TransitionRows
            .OfType<TypedTransitionRowSuccess>()
            .First(r => r.EventName == "Freeze");
        var ob = Px.Obligation(c, OverdraftRule);

        var wp = WpCalculator.ComputePreservationWp(freeze.Actions, ob).Wp();
        wp.Wp.Key.Should().Be(WpCalculator.Canonicalize(ob.Condition).Key);
    }

    // ── The multi-write gate: sequential composition is not implemented ──────

    [Fact]
    public void OpenAccount_TwoWritePlan_IsNotSupported()
    {
        var c = Px.Compile(Bank);
        var open = c.Semantics.EventHandlers.OfType<TypedEventRowSuccess>()
            .First(r => r.EventName == "OpenAccount");

        var result = WpCalculator.ComputeEstablishmentWp(
            c.Semantics, open.Actions, Px.Obligation(c, OverdraftRule));

        result.Should().BeOfType<WpNotSupported>()
            .Which.Reason.Should().Contain("write-plan decomposition");
    }

    [Fact]
    public void MultiWritePreservationPlan_IsNotSupported()
    {
        var c = Px.Compile(Bank);
        var open = c.Semantics.EventHandlers.OfType<TypedEventRowSuccess>()
            .First(r => r.EventName == "OpenAccount");

        WpCalculator.ComputePreservationWp(open.Actions, Px.Obligation(c, OverdraftRule))
            .Should().BeOfType<WpNotSupported>();
    }
}
