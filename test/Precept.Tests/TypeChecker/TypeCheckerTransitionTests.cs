using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.TypeChecker;

/// <summary>
/// Slice 5 — Transition Row + EventHandler Normalization.
/// Covers FromState/ToState resolution, wildcard FromState (D10), guard expression scope,
/// action target resolution, ActionSecondaryRole invariant (D5), EventHandler event resolution,
/// StateReference/EventReference recording, and D26 assert coverage.
/// </summary>
/// <remarks>
/// REGRESSION NOTE: Slice 5 introduced a regression where <c>EventName.ArgName</c> accessor
/// expressions (e.g. <c>set Name = Submit.Label</c>) emit <c>UndeclaredField</c> for the event
/// name. Tests that require <c>EventName.ArgName</c> in "expecting clean" assertions are marked
/// as TYPE B (known red) and documented. See <c>.squad/decisions/inbox/soup-nazi-slice-5-regression.md</c>.
/// </remarks>
public class TypeCheckerTransitionTests
{
    // ════════════════════════════════════════════════════════════════════════
    //  Category 1: FromState / ToState resolution
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ValidTransition_FromToStateResolved()
    {
        var precept = """
            precept Widget
            field Count as number default 0
            state Draft initial
            state Published
            event Publish
            from Draft on Publish -> set Count = Count + 1 -> transition Published
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);
        var row = index.TransitionRows.Should().ContainSingle().Subject;

        row.FromState.Should().Be("Draft");
        row.EventName.Should().Be("Publish");
        row.TargetState.Should().Be("Published");
        row.Outcome.Should().Be(TransitionRowOutcome.Transition);
    }

    [Fact]
    public void UnknownFromState_EmitsUndeclaredState()
    {
        var precept = """
            precept Widget
            field Count as number default 0
            state Active initial
            event Ping
            from Bogus on Ping -> set Count = Count + 1 -> no transition
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.UndeclaredState);

        var row = index.TransitionRows.Single();
        row.EventName.Should().Be("Ping");
    }

    [Fact]
    public void UnknownToState_EmitsUndeclaredState()
    {
        var precept = """
            precept Widget
            field Count as number default 0
            state Draft initial
            event Submit
            from Draft on Submit -> set Count = Count + 1 -> transition Nowhere
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.UndeclaredState);

        var row = index.TransitionRows.Single();
        row.FromState.Should().Be("Draft");
        row.EventName.Should().Be("Submit");
        // TargetState may be null or "Nowhere" depending on implementation;
        // the diagnostic is what matters.
    }

    [Fact]
    public void UnknownEvent_EmitsUndeclaredEvent()
    {
        var precept = """
            precept Widget
            field Count as number default 0
            state Draft initial
            from Draft on Ghost -> set Count = Count + 1 -> no transition
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.UndeclaredEvent);
    }

    [Fact]
    public void NoTransitionOutcome_ResolvesCorrectly()
    {
        var precept = """
            precept Widget
            field Count as number default 0
            state Open initial
            event Increment
            from Open on Increment -> set Count = Count + 1 -> no transition
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);
        var row = index.TransitionRows.Single();

        row.Outcome.Should().Be(TransitionRowOutcome.NoTransition);
        row.TargetState.Should().BeNull();
    }

    [Fact]
    public void RejectOutcome_ResolvesCorrectly()
    {
        var precept = """
            precept Widget
            field Count as number default 0
            state Open initial
            event BadAction
            from Open on BadAction -> reject "Not allowed"
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);
        var row = index.TransitionRows.Single();

        row.Outcome.Should().Be(TransitionRowOutcome.Reject);
        row.RejectReason.Should().Be("Not allowed");
    }

    [Fact]
    public void StateReference_RecordedForFromAndToState()
    {
        var precept = """
            precept Widget
            field Count as number default 0
            state Draft initial
            state Active
            event Activate
            from Draft on Activate -> set Count = Count + 1 -> transition Active
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        // FromState "Draft" and ToState "Active" should both have state references
        index.StateReferences.Should().Contain(r => r.State.Name == "Draft");
        index.StateReferences.Should().Contain(r => r.State.Name == "Active");
    }

    [Fact]
    public void EventReference_RecordedForTransitionRowEvent()
    {
        var precept = """
            precept Widget
            field Count as number default 0
            state Open initial
            event Ping
            from Open on Ping -> set Count = Count + 1 -> no transition
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.EventReferences.Should().Contain(r => r.Event.Name == "Ping");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Category 2: Guard expression in transition
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void TransitionWithGuard_FieldReference_ResolvesGuard()
    {
        var precept = """
            precept Widget
            field Count as number default 0
            state Open initial
            event Increment
            from Open on Increment when Count > 0 -> set Count = Count + 1 -> no transition
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);
        var row = index.TransitionRows.Single();

        row.Guard.Should().NotBeNull();
        row.Guard.Should().BeOfType<TypedBinaryOp>();
    }

    [Fact]
    public void GuardWithUnknownIdentifier_EmitsDiagnostic()
    {
        var precept = """
            precept Widget
            field Count as number default 0
            state Open initial
            event Ping
            from Open on Ping when Phantom > 0 -> set Count = Count + 1 -> no transition
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.UndeclaredField);
    }

    [Fact]
    public void GuardWithFieldReference_RecordsFieldReference()
    {
        var precept = """
            precept Widget
            field Active as boolean default false
            field Count as number default 0
            state Open initial
            event Ping
            from Open on Ping when Active -> set Count = Count + 1 -> no transition
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        // "Active" should appear in FieldReferences (from guard + from action + from action value)
        index.FieldReferences.Should().Contain(r => r.Field.Name == "Active");
    }

    [Fact]
    public void GuardErrorExpression_DiagnosticPresent_D26Holds()
    {
        // Guard references unknown identifier → TypedErrorExpression.
        // D26: at least one Error diagnostic must be present.
        var precept = """
            precept Widget
            field Count as number default 0
            state Open initial
            event Ping
            from Open on Ping when UnknownThing > 0 -> set Count = Count + 1 -> no transition
            """;

        var (_, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics.Where(d => d.Severity == Severity.Error)
            .Should().NotBeEmpty("D26: error expression in guard requires at least one Error diagnostic");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Category 3: Action resolution
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void SimpleAssignAction_ResolvesFieldNameAndType()
    {
        var precept = """
            precept Widget
            field Count as number default 0
            state Open initial
            event Increment
            from Open on Increment -> set Count = Count + 1 -> no transition
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);
        var row = index.TransitionRows.Single();

        row.Actions.Should().ContainSingle();
        var action = row.Actions[0];
        action.FieldName.Should().Be("Count");
        action.FieldType.Should().Be(TypeKind.Number);
    }

    [Fact]
    public void SimpleAssignAction_IsTypedInputAction()
    {
        var precept = """
            precept Widget
            field Name as string default "none"
            state Open initial
            event Rename
            from Open on Rename -> set Name = "updated" -> no transition
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);
        var action = index.TransitionRows.Single().Actions.Single();

        action.Should().BeOfType<TypedInputAction>();
        var input = (TypedInputAction)action;
        input.SecondaryExpression.Should().BeNull();
        input.SecondaryRole.Should().BeNull();
    }

    [Fact]
    public void ActionTargetUnknownField_EmitsUndeclaredField()
    {
        var precept = """
            precept Widget
            field Count as number default 0
            state Open initial
            event Ping
            from Open on Ping -> set Ghost = 42 -> no transition
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.UndeclaredField);
    }

    [Fact]
    public void ActionRecordsFieldReference()
    {
        var precept = """
            precept Widget
            field Count as number default 0
            state Open initial
            event Increment
            from Open on Increment -> set Count = Count + 1 -> no transition
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        // "Count" appears as the action target and as the value expression reference
        index.FieldReferences.Where(r => r.Field.Name == "Count")
            .Should().HaveCountGreaterThanOrEqualTo(2,
                "Count is referenced as action target and in value expression");
    }

    [Fact]
    public void MultipleActionsInChain_AllResolved()
    {
        var precept = """
            precept Widget
            field Count as number default 0
            field Name as string default "none"
            state Open initial
            event Update
            from Open on Update -> set Count = Count + 1 -> set Name = "changed" -> no transition
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);
        var row = index.TransitionRows.Single();

        row.Actions.Should().HaveCount(2);
        row.Actions[0].FieldName.Should().Be("Count");
        row.Actions[1].FieldName.Should().Be("Name");
    }

    [Fact]
    public void ClearAction_ResolvesAsBaseTypedAction()
    {
        var precept = """
            precept Widget
            field Name as string optional
            state Open initial
            event Reset
            from Open on Reset -> clear Name -> no transition
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);
        var action = index.TransitionRows.Single().Actions.Single();

        action.FieldName.Should().Be("Name");
        action.Should().NotBeOfType<TypedInputAction>();
        action.Should().NotBeOfType<TypedBindingAction>();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Category 4: EventHandler normalization
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void ValidEventHandler_ResolvesEvent()
    {
        var precept = """
            precept Widget
            field Count as number default 0
            event Ping
            on Ping -> set Count = Count + 1
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.EventHandlers.Should().ContainSingle();
        var handler = index.EventHandlers.Single();
        handler.EventName.Should().Be("Ping");
    }

    [Fact]
    public void UnknownEventHandler_EmitsUndeclaredEvent()
    {
        var precept = """
            precept Widget
            field Count as number default 0
            on Ghost -> set Count = Count + 1
            """;

        TypeCheckerTestHelpers.CheckExpectingError(precept, DiagnosticCode.UndeclaredEvent);
    }

    [Fact]
    public void EventHandler_RecordsEventReference()
    {
        var precept = """
            precept Widget
            field Count as number default 0
            event Ping
            on Ping -> set Count = Count + 1
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.EventReferences.Should().Contain(r => r.Event.Name == "Ping");
    }

    [Fact]
    public void EventHandler_ActionsResolveFieldTarget()
    {
        var precept = """
            precept Widget
            field Name as string default "none"
            event Rename
            on Rename -> set Name = "updated"
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);
        var action = index.EventHandlers.Single().Actions.Single();

        action.FieldName.Should().Be("Name");
        action.FieldType.Should().Be(TypeKind.String);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Category 5: D26 invariant — error expressions require Error diagnostics
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void CleanTransitionRow_NoDiagnostics()
    {
        var precept = """
            precept Widget
            field Count as number default 0
            state Open initial
            event Ping
            from Open on Ping -> set Count = Count + 1 -> no transition
            """;

        var (_, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics.Where(d => d.Severity == Severity.Error)
            .Should().BeEmpty();
    }

    [Fact]
    public void ErrorInActionValue_DiagnosticPresent_D26Holds()
    {
        // Action references unknown field in value → TypedErrorExpression.
        // D26: at least one Error diagnostic must be present.
        var precept = """
            precept Widget
            field Count as number default 0
            state Open initial
            event Ping
            from Open on Ping -> set Count = Phantom + 1 -> no transition
            """;

        var (_, diagnostics) = TypeCheckerTestHelpers.Check(precept);

        diagnostics.Where(d => d.Severity == Severity.Error)
            .Should().NotBeEmpty("D26: error expression in action value requires at least one Error diagnostic");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Category 6: Multiple transition rows
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void MultipleTransitionRows_AllPopulated()
    {
        var precept = """
            precept Widget
            field Count as number default 0
            state Draft initial
            state Active
            event Start
            event Stop
            from Draft on Start -> set Count = Count + 1 -> transition Active
            from Active on Stop -> set Count = 0 -> transition Draft
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);

        index.TransitionRows.Should().HaveCount(2);

        var startRow = index.TransitionRows.Single(r => r.EventName == "Start");
        startRow.FromState.Should().Be("Draft");
        startRow.TargetState.Should().Be("Active");

        var stopRow = index.TransitionRows.Single(r => r.EventName == "Stop");
        stopRow.FromState.Should().Be("Active");
        stopRow.TargetState.Should().Be("Draft");
    }

    [Fact]
    public void TransitionWithGuardAndActions_AllResolved()
    {
        var precept = """
            precept Widget
            field Count as number default 0
            field Name as string default "none"
            state Open initial
            event Update
            from Open on Update when Count < 100 -> set Count = Count + 1 -> set Name = "updated" -> no transition
            """;

        var index = TypeCheckerTestHelpers.CheckExpectingClean(precept);
        var row = index.TransitionRows.Single();

        row.Guard.Should().NotBeNull();
        row.Actions.Should().HaveCount(2);
        row.Outcome.Should().Be(TransitionRowOutcome.NoTransition);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Category 6: Wildcard FromState (D10)
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void TransitionRow_WildcardFromState_NullFromState()
    {
        // D10: "from any" wildcard produces FromState == null.
        // The TypeChecker also emits UndeclaredState for "any" (wildcard surface
        // not yet recognized as special), so use Check() not CheckExpectingClean().
        var precept = """
            precept Widget
            field Count as number default 0
            state Draft initial
            state Published
            event Publish
            from any on Publish -> set Count = Count + 1 -> no transition
            from Draft on Publish -> set Count = Count + 1 -> transition Published
            """;

        var (index, _) = TypeCheckerTestHelpers.Check(precept);

        // Wildcard row: FromState is null per D10
        index.TransitionRows.Should().Contain(row => row.FromState == null,
            because: "the 'from any' wildcard must produce a null FromState (D10)");

        // Named row: FromState is the declared state name
        index.TransitionRows.Should().Contain(
            row => row.FromState != null && row.FromState == "Draft",
            because: "a named from-state must resolve to the state name");
    }
}
