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
/// the rule fields surfaced by <c>PreceptPreviewHandler</c> in snapshot and fire responses.
/// </summary>
public class PreceptPreviewRulesTests
{
    // ── EvaluateCurrentRules — runtime API ───────────────────────────────────

    [Fact]
    public void EvaluateCurrentRules_ReturnsEmpty_WhenNoRulesViolated()
    {
        const string dsl = """
            precept M
            field Balance as number default 100
            rule Balance >= 0 because "Balance must not go negative"
            state A initial
            state B
            event Transfer
            from A on Transfer -> transition B
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
            precept M
            field Balance as number default 100
            rule Balance >= 0 because "Balance must not go negative"
            field Quantity as number default 1
            rule Quantity >= 1 because "Quantity must stay positive"
            state A initial
            event Go
            from A on Go -> transition A
            """;

        var machine = PreceptParser.Parse(dsl);
        var definition = PreceptCompiler.Compile(machine);

        // CreateInstance does not check rule violations — it only validates types.
        // Passing negative values directly seeds the instance with violating data.
        var instance = definition.CreateInstance(
            new Dictionary<string, object?> { ["Balance"] = -10.0, ["Quantity"] = 0.0 });

        var violations = definition.EvaluateCurrentRules(instance);

        violations.Should().HaveCount(2, "both field rules are violated by the supplied data");
        violations.Should().Contain(v => v.Message == "Balance must not go negative");
        violations.Should().Contain(v => v.Message == "Quantity must stay positive");
    }

    // ── PreceptPreviewHandler — snapshot includes ActiveRuleViolations ──────────

    [Fact]
    public async Task Snapshot_ActiveRuleViolations_IsNull_WhenCurrentInstanceSatisfiesAllRules()
    {
        // Verifies the BuildSnapshot wiring: EvaluateCurrentRules returns empty → property is null
        // (not an empty array) so the webview banner stays hidden.
        const string dsl = """
            precept M
            field Balance as number default 100
            rule Balance >= 0 because "Balance must not go negative"
            state Active initial
            event Debit with Amount as number
            from Active on Debit -> set Balance = Balance - Debit.Amount -> transition Active
            """;

        var (handler, uri) = CreateHandler();
        var response = await handler.Handle(
            new PreceptPreviewRequest("snapshot", uri, Text: dsl),
            CancellationToken.None);

        response.Success.Should().BeTrue();
        response.Snapshot.Should().NotBeNull();

        // No violations for the default instance (Balance=100 satisfies Balance >= 0)
        response.Snapshot!.ActiveRuleViolations.Should().BeNull(
            "BuildSnapshot should return null (not an empty array) when EvaluateCurrentRules is empty");
    }

    // ── PreceptPreviewHandler — snapshot includes RuleDefinitions ─────────────────

    [Fact]
    public async Task Snapshot_IncludesRuleDefinitions_WhenMachineHasRules()
    {
        const string dsl = """
            precept M
            field Balance as number default 100
            rule Balance >= 0 because "Balance must not go negative"
            state A initial
            state B
            event Go
            from A on Go -> transition B
            """;

        var (handler, uri) = CreateHandler();
        var request = new PreceptPreviewRequest("snapshot", uri, Text: dsl);
        var response = await handler.Handle(request, CancellationToken.None);

        response.Success.Should().BeTrue();
        response.Snapshot.Should().NotBeNull();

        var ruleDefs = response.Snapshot!.RuleDefinitions;
        ruleDefs.Should().NotBeNullOrEmpty("the machine declares a rule");
        ruleDefs!.Should().ContainSingle(r =>
            r.Scope == "rule" &&
            r.Expression == "Balance >= 0" &&
            r.Reason == "Balance must not go negative");
    }

    [Fact]
    public async Task Snapshot_ActiveRuleViolations_IsNullOrEmpty_WhenInitialDataSatisfiesRules()
    {
        const string dsl = """
            precept M
            field Balance as number default 100
            rule Balance >= 0 because "Balance must not go negative"
            state A initial
            state B
            event Go
            from A on Go -> transition B
            """;

        var (handler, uri) = CreateHandler();
        var request = new PreceptPreviewRequest("snapshot", uri, Text: dsl);
        var response = await handler.Handle(request, CancellationToken.None);

        response.Success.Should().BeTrue();
        response.Snapshot.Should().NotBeNull();

        // No violations at the initial state — field is null or omitted in snapshot.
        var violations = response.Snapshot!.ActiveRuleViolations;
        var isEmpty = violations is null || violations.Count == 0;
        isEmpty.Should().BeTrue("the initial Balance of 100 satisfies the rule");
    }

    // ── PreceptPreviewHandler — HandleFire returns all reasons in Errors ───────────

    [Fact]
    public async Task HandleFire_BlockedByMultipleRules_ReturnsAllReasonsInErrors()
    {
        // Both Balance and Quantity field rules are violated by firing Reduce with a large Amount.
        const string dsl = """
            precept M
            field Balance as number default 100
            rule Balance >= 0 because "Balance must not go negative"
            field Quantity as number default 1
            rule Quantity >= 1 because "Quantity must stay positive"
            state A initial
            event Reduce with Amount as number
            on Reduce ensure Amount > 0 because "Amount must be positive"
            from A on Reduce -> set Balance = Balance - Reduce.Amount -> set Quantity = Quantity - Reduce.Amount -> transition A
            """;

        var (handler, uri) = CreateHandler();

        // Seed the session with an initial snapshot request.
        var snapshot = new PreceptPreviewRequest("snapshot", uri, Text: dsl);
        await handler.Handle(snapshot, CancellationToken.None);

        // Amount = 200 → Balance goes to -100 (violates rule 1), Quantity goes to -199 (violates rule 2).
        var fire = new PreceptPreviewRequest(
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
            precept Test
            field balance as number default 0
            field overdraftLimit as number default 100
            rule balance >= 0 - overdraftLimit because "Balance must be within overdraft limit"
            state GoodStanding initial
            state Overdrawn
            event Withdraw with amount as number
            on Withdraw ensure amount > 0 because "Withdraw amount must be positive"
            from GoodStanding on Withdraw when balance - Withdraw.amount >= 0 -> set balance = balance - Withdraw.amount -> no transition
            from GoodStanding on Withdraw -> set balance = balance - Withdraw.amount -> transition Overdrawn
            """;

        var (handler, uri) = CreateHandler();

        // Seed session via snapshot.
        await handler.Handle(new PreceptPreviewRequest("snapshot", uri, Text: dsl), CancellationToken.None);

        // Inspect Withdraw with amount=500 — should be blocked by the top-level rule.
        var response = await handler.Handle(
            new PreceptPreviewRequest("inspect", uri, Text: dsl,
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
            precept Test
            field balance as number default 0
            field overdraftLimit as number default 100
            rule balance >= 0 - overdraftLimit because "Balance must be within overdraft limit"
            state GoodStanding initial
            state Overdrawn
            event Withdraw with amount as number
            from GoodStanding on Withdraw when balance - Withdraw.amount >= 0 -> set balance = balance - Withdraw.amount -> no transition
            from GoodStanding on Withdraw -> set balance = balance - Withdraw.amount -> transition Overdrawn
            """;

        var (handler, uri) = CreateHandler();
        await handler.Handle(new PreceptPreviewRequest("snapshot", uri, Text: dsl), CancellationToken.None);

        var response = await handler.Handle(
            new PreceptPreviewRequest("inspect", uri, Text: dsl,
                EventName: "Withdraw",
                Args: new Dictionary<string, object?> { ["amount"] = 30.0 }),
            CancellationToken.None);

        response.Success.Should().BeTrue();
        response.InspectResult.Should().NotBeNull();
        response.InspectResult!.Outcome.Should().Be("enabled",
            "simulating balance − 30 = −30 satisfies the top-level rule balance >= −100");
    }

    // ── PreceptPreviewHandler — inspectUpdate (draft validation) ─────────────────

    [Fact]
    public async Task HandleInspectUpdate_SingleChangedFieldViolation_OnlyThatFieldHasViolation()
    {
        const string dsl = """
            precept DraftEdit
            field Author as string nullable
            field Age as number default 12
            rule Age >= 12 because "Age must be at least 12 years old"
            state Draft initial
            in Draft edit Author, Age
            """;

        var (handler, uri) = CreateHandler();
        await handler.Handle(new PreceptPreviewRequest("snapshot", uri, Text: dsl), CancellationToken.None);

        var response = await handler.Handle(
            new PreceptPreviewRequest(
                "inspectUpdate",
                uri,
                Text: dsl,
                FieldUpdates: new Dictionary<string, object?> { ["Age"] = 11.0 }),
            CancellationToken.None);

        response.Success.Should().BeTrue();
        response.EditableFields.Should().NotBeNull();
        response.FieldErrors.Should().NotBeNull();
        response.FormErrors.Should().BeNull();
        response.CanSave.Should().BeFalse();

        var age = response.EditableFields!.Single(f => f.FieldName == "Age");
        var author = response.EditableFields!.Single(f => f.FieldName == "Author");

        age.Violation.Should().Contain("Age must be at least 12 years old");
        author.Violation.Should().BeNull("only patched fields should be marked with this simulated violation");
        response.FieldErrors!["Age"].Should().ContainSingle(r => r.Contains("Age must be at least 12 years old"));
        response.FieldErrors.Should().NotContainKey("Author");

        response.Errors.Should().NotBeNull();
        response.Errors!.Should().ContainSingle(r => r.Contains("Age must be at least 12 years old"));
    }

    [Fact]
    public async Task HandleInspectUpdate_EmptyPatch_ReturnsNoViolations()
    {
        const string dsl = """
            precept DraftEdit
            field Author as string nullable
            field Age as number default 12
            rule Age >= 12 because "Age must be at least 12 years old"
            state Draft initial
            in Draft edit Author, Age
            """;

        var (handler, uri) = CreateHandler();
        await handler.Handle(new PreceptPreviewRequest("snapshot", uri, Text: dsl), CancellationToken.None);

        var response = await handler.Handle(
            new PreceptPreviewRequest(
                "inspectUpdate",
                uri,
                Text: dsl,
                FieldUpdates: new Dictionary<string, object?>()),
            CancellationToken.None);

        response.Success.Should().BeTrue();
        response.EditableFields.Should().NotBeNull();
        response.EditableFields!.Should().OnlyContain(f => f.Violation == null);
        response.Errors.Should().BeNull();
        response.FieldErrors.Should().BeNull();
        response.FormErrors.Should().BeNull();
        response.CanSave.Should().BeTrue();
    }

    [Fact]
    public async Task HandleInspectUpdate_DoesNotCommitData()
    {
        const string dsl = """
            precept DraftEdit
            field Author as string nullable
            field Age as number default 12
            state Draft initial
            in Draft edit Author, Age
            """;

        var (handler, uri) = CreateHandler();
        await handler.Handle(new PreceptPreviewRequest("snapshot", uri, Text: dsl), CancellationToken.None);

        var inspectResponse = await handler.Handle(
            new PreceptPreviewRequest(
                "inspectUpdate",
                uri,
                Text: dsl,
                FieldUpdates: new Dictionary<string, object?>
                {
                    ["Author"] = "Alice",
                    ["Age"] = 30.0
                }),
            CancellationToken.None);

        inspectResponse.Success.Should().BeTrue();

        var snapshotAfterInspect = await handler.Handle(
            new PreceptPreviewRequest("snapshot", uri, Text: dsl),
            CancellationToken.None);

        snapshotAfterInspect.Success.Should().BeTrue();
        snapshotAfterInspect.Snapshot.Should().NotBeNull();

        snapshotAfterInspect.Snapshot!.Data["Author"].Should().BeNull("inspectUpdate is preview-only and must not persist draft values");
        Convert.ToDouble(snapshotAfterInspect.Snapshot!.Data["Age"]).Should().Be(12.0);
    }

    [Fact]
    public async Task HandleInspectUpdate_MultiplePatchedFields_OnlyCausalFieldIsMarked()
    {
        const string dsl = """
            precept DraftEdit
            field Author as string nullable
            field Age as number default 12
            rule Age >= 12 because "Age must be at least 12 years old"
            state Draft initial
            in Draft edit Author, Age
            """;

        var (handler, uri) = CreateHandler();
        await handler.Handle(new PreceptPreviewRequest("snapshot", uri, Text: dsl), CancellationToken.None);

        var response = await handler.Handle(
            new PreceptPreviewRequest(
                "inspectUpdate",
                uri,
                Text: dsl,
                FieldUpdates: new Dictionary<string, object?>
                {
                    ["Author"] = "Alice",
                    ["Age"] = 11.0
                }),
            CancellationToken.None);

        response.Success.Should().BeTrue();
        response.EditableFields.Should().NotBeNull();
        response.FieldErrors.Should().NotBeNull();
        response.FormErrors.Should().BeNull();
        response.CanSave.Should().BeFalse();

        var age = response.EditableFields!.Single(f => f.FieldName == "Age");
        var author = response.EditableFields!.Single(f => f.FieldName == "Author");

        age.Violation.Should().Contain("Age must be at least 12 years old");
        author.Violation.Should().BeNull("inline draft validation should stay attached only to the field that caused the violation");
        response.FieldErrors!["Age"].Should().ContainSingle(r => r.Contains("Age must be at least 12 years old"));
        response.FieldErrors.Should().NotContainKey("Author");
    }

    [Fact]
    public async Task HandleInspectUpdate_OnlyAttributedFieldCarriesViolationText()
    {
        const string dsl = """
            precept DraftEdit
            field Author as string nullable
            field Age as number default 12
            rule Age >= 12 because "Age must be at least 12 years old"
            state Draft initial
            in Draft edit Author, Age
            """;

        var (handler, uri) = CreateHandler();
        await handler.Handle(new PreceptPreviewRequest("snapshot", uri, Text: dsl), CancellationToken.None);

        var response = await handler.Handle(
            new PreceptPreviewRequest(
                "inspectUpdate",
                uri,
                Text: dsl,
                FieldUpdates: new Dictionary<string, object?>
                {
                    ["Author"] = "Alice",
                    ["Age"] = 9.0
                }),
            CancellationToken.None);

        response.Success.Should().BeTrue();
        response.EditableFields.Should().NotBeNull();

        var age = response.EditableFields!.Single(f => f.FieldName == "Age");
        var author = response.EditableFields!.Single(f => f.FieldName == "Author");

        age.Violation.Should().Contain("Age must be at least 12 years old");
        author.Violation.Should().BeNull();
        response.FieldErrors.Should().NotBeNull();
        response.FieldErrors!.Keys.Should().ContainSingle().Which.Should().Be("Age");
        response.FormErrors.Should().BeNull();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    private static (PreceptPreviewHandler handler, DocumentUri uri) CreateHandler()
    {
        var handler = new PreceptPreviewHandler();
        var uri = DocumentUri.From($"file:///tmp/preview-rules-test-{Guid.NewGuid():N}.precept");
        return (handler, uri);
    }
}
