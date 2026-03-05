using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using Precept;
using Precept.LanguageServer;
using Xunit;

namespace Precept.LanguageServer.Tests;

/// <summary>
/// Tests for rules-awareness in the preview layer:
/// <c>EvaluateCurrentRules</c> on <c>PreceptEngine</c> and
/// the rule fields surfaced by <c>SmPreviewHandler</c> in snapshot and fire responses.
/// </summary>
public class SmPreviewRulesTests
{
    // ── EvaluateCurrentRules — runtime API ───────────────────────────────────

    [Fact]
    public void EvaluateCurrentRules_ReturnsEmpty_WhenNoRulesViolated()
    {
        const string dsl = """
            machine M
            number Balance = 100
              rule Balance >= 0 "Balance must not go negative"
            state A initial
            state B
            event Transfer
            from A on Transfer
              transition B
            """;

        var machine = PreceptParser.Parse(dsl);
        var definition = PreceptCompiler.Compile(machine);
        var instance = definition.CreateInstance();

        var violations = definition.EvaluateCurrentRules(instance);

        violations.Should().BeEmpty("the default Balance of 100 satisfies Balance >= 0");
    }

    [Fact]
    public void EvaluateCurrentRules_ReturnsAllReasons_WhenMultipleRulesViolatedSimultaneously()
    {
        const string dsl = """
            machine M
            number Balance = 100
              rule Balance >= 0 "Balance must not go negative"
            number Quantity = 1
              rule Quantity >= 1 "Quantity must stay positive"
            state A initial
            event Go
            from A on Go
              transition A
            """;

        var machine = PreceptParser.Parse(dsl);
        var definition = PreceptCompiler.Compile(machine);

        // CreateInstance does not check rule violations — it only validates types.
        // Passing negative values directly seeds the instance with violating data.
        var instance = definition.CreateInstance(
            new Dictionary<string, object?> { ["Balance"] = -10.0, ["Quantity"] = 0.0 });

        var violations = definition.EvaluateCurrentRules(instance);

        violations.Should().HaveCount(2, "both field rules are violated by the supplied data");
        violations.Should().Contain("Balance must not go negative");
        violations.Should().Contain("Quantity must stay positive");
    }

    // ── SmPreviewHandler — snapshot includes RuleDefinitions ─────────────────

    [Fact]
    public async Task Snapshot_IncludesRuleDefinitions_WhenMachineHasRules()
    {
        const string dsl = """
            machine M
            number Balance = 100
              rule Balance >= 0 "Balance must not go negative"
            state A initial
            state B
            event Go
            from A on Go
              transition B
            """;

        var (handler, uri) = CreateHandler();
        var request = new SmPreviewRequest("snapshot", uri, Text: dsl);
        var response = await handler.Handle(request, CancellationToken.None);

        response.Success.Should().BeTrue();
        response.Snapshot.Should().NotBeNull();

        var ruleDefs = response.Snapshot!.RuleDefinitions;
        ruleDefs.Should().NotBeNullOrEmpty("the machine declares a field rule");
        ruleDefs!.Should().ContainSingle(r =>
            r.Scope == "field:Balance" &&
            r.Expression == "Balance >= 0" &&
            r.Reason == "Balance must not go negative");
    }

    [Fact]
    public async Task Snapshot_ActiveRuleViolations_IsNullOrEmpty_WhenInitialDataSatisfiesRules()
    {
        const string dsl = """
            machine M
            number Balance = 100
              rule Balance >= 0 "Balance must not go negative"
            state A initial
            state B
            event Go
            from A on Go
              transition B
            """;

        var (handler, uri) = CreateHandler();
        var request = new SmPreviewRequest("snapshot", uri, Text: dsl);
        var response = await handler.Handle(request, CancellationToken.None);

        response.Success.Should().BeTrue();
        response.Snapshot.Should().NotBeNull();

        // No violations at the initial state — field is null or omitted in snapshot.
        var violations = response.Snapshot!.ActiveRuleViolations;
        var isEmpty = violations is null || violations.Count == 0;
        isEmpty.Should().BeTrue("the initial Balance of 100 satisfies the rule");
    }

    // ── SmPreviewHandler — HandleFire returns all reasons in Errors ───────────

    [Fact]
    public async Task HandleFire_BlockedByMultipleRules_ReturnsAllReasonsInErrors()
    {
        // Both Balance and Quantity field rules are violated by firingReduce with a large Amount.
        const string dsl = """
            machine M
            number Balance = 100
              rule Balance >= 0 "Balance must not go negative"
            number Quantity = 1
              rule Quantity >= 1 "Quantity must stay positive"
            state A initial
            event Reduce
              number Amount = 0
            from A on Reduce
              set Balance = Balance - Reduce.Amount
              set Quantity = Quantity - Reduce.Amount
              transition A
            """;

        var (handler, uri) = CreateHandler();

        // Seed the session with an initial snapshot request.
        var snapshot = new SmPreviewRequest("snapshot", uri, Text: dsl);
        await handler.Handle(snapshot, CancellationToken.None);

        // Amount = 200 → Balance goes to -100 (violates rule 1), Quantity goes to -199 (violates rule 2).
        var fire = new SmPreviewRequest(
            "fire", uri, Text: dsl,
            EventName: "Reduce",
            Args: new Dictionary<string, object?> { ["Amount"] = 200.0 });

        var response = await handler.Handle(fire, CancellationToken.None);

        response.Success.Should().BeFalse("both field rules are violated");
        response.Errors.Should().NotBeNull("all violation reasons must be in the Errors list");
        response.Errors!.Should().HaveCount(2, "two rules are violated simultaneously");
        response.Errors.Should().Contain("Balance must not go negative");
        response.Errors.Should().Contain("Quantity must stay positive");

        // Backward-compat: the single Error field still carries the first reason.
        response.Error.Should().NotBeNullOrEmpty("Error is set for backward-compatible consumers");
    }

    [Fact]
    public async Task HandleInspect_WithArgs_TopLevelRuleViolated_ReturnsBlocked()
    {
        // Reproduces the test.sm scenario: balance=0, overdraftLimit=100, withdraw 500.
        // The top-level rule "balance >= 0 - overdraftLimit" is violated after simulation.
        const string dsl = """
            machine Test
            number balance = 0
            number overdraftLimit = 100
            rule balance >= 0 - overdraftLimit "Balance must be within overdraft limit"
            state GoodStanding initial
            state Overdrawn
            event Withdraw
              number amount
                rule amount > 0 "Withdraw amount must be positive"
            from GoodStanding on Withdraw
              if balance - Withdraw.amount >= 0
                set balance = balance - Withdraw.amount
                no transition
              else
                set balance = balance - Withdraw.amount
                transition Overdrawn
            """;

        var (handler, uri) = CreateHandler();

        // Seed session via snapshot.
        await handler.Handle(new SmPreviewRequest("snapshot", uri, Text: dsl), CancellationToken.None);

        // Inspect Withdraw with amount=500 — should be blocked by the top-level rule.
        var response = await handler.Handle(
            new SmPreviewRequest("inspect", uri, Text: dsl,
                EventName: "Withdraw",
                Args: new Dictionary<string, object?> { ["amount"] = 500.0 }),
            CancellationToken.None);

        response.Success.Should().BeTrue("inspect always succeeds as a server call");
        response.InspectResult.Should().NotBeNull();
        response.InspectResult!.Outcome.Should().Be("blocked",
            "simulating balance − 500 = −500 violates the top-level rule balance >= −100");
        response.InspectResult.Reasons.Should().Contain("Balance must be within overdraft limit");
    }

    [Fact]
    public async Task HandleInspect_WithArgs_RuleSatisfied_ReturnsEnabled()
    {
        // With amount=30, balance stays at −30 which is >= −100, rule passes.
        const string dsl = """
            machine Test
            number balance = 0
            number overdraftLimit = 100
            rule balance >= 0 - overdraftLimit "Balance must be within overdraft limit"
            state GoodStanding initial
            state Overdrawn
            event Withdraw
              number amount
            from GoodStanding on Withdraw
              if balance - Withdraw.amount >= 0
                set balance = balance - Withdraw.amount
                no transition
              else
                set balance = balance - Withdraw.amount
                transition Overdrawn
            """;

        var (handler, uri) = CreateHandler();
        await handler.Handle(new SmPreviewRequest("snapshot", uri, Text: dsl), CancellationToken.None);

        var response = await handler.Handle(
            new SmPreviewRequest("inspect", uri, Text: dsl,
                EventName: "Withdraw",
                Args: new Dictionary<string, object?> { ["amount"] = 30.0 }),
            CancellationToken.None);

        response.Success.Should().BeTrue();
        response.InspectResult.Should().NotBeNull();
        response.InspectResult!.Outcome.Should().Be("enabled",
            "simulating balance − 30 = −30 satisfies the top-level rule balance >= −100");
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static (SmPreviewHandler handler, DocumentUri uri) CreateHandler()
    {
        var handler = new SmPreviewHandler();
        var uri = DocumentUri.From($"file:///tmp/preview-rules-test-{Guid.NewGuid():N}.precept");
        return (handler, uri);
    }
}
