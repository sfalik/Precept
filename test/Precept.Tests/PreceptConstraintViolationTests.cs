using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Precept;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Tests for structured <see cref="ConstraintViolation"/> content, <see cref="IsSuccess"/> on
/// result types, and the <see cref="TransitionOutcome.ConstraintFailure"/> vs
/// <see cref="TransitionOutcome.Rejected"/> distinction.
/// </summary>
public class PreceptConstraintViolationTests
{
    // ========================================================================================
    // IsSuccess — FireResult
    // ========================================================================================

    [Fact]
    public void FireResult_IsSuccess_True_WhenTransition()
    {
        const string dsl = """
            precept Test
            state A initial
            state B
            event Go
            from A on Go -> transition B
            """;
        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = wf.CreateInstance("A", new Dictionary<string, object?>());

        var result = wf.Fire(instance, "Go");

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(TransitionOutcome.Transition);
    }

    [Fact]
    public void FireResult_IsSuccess_True_WhenNoTransition()
    {
        const string dsl = """
            precept Test
            state A initial
            event Stay
            from A on Stay -> no transition
            """;
        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = wf.CreateInstance("A", new Dictionary<string, object?>());

        var result = wf.Fire(instance, "Stay");

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(TransitionOutcome.NoTransition);
    }

    [Fact]
    public void FireResult_IsSuccess_False_WhenRejected()
    {
        const string dsl = """
            precept Test
            state A initial
            event Go
            from A on Go -> reject "not allowed"
            """;
        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = wf.CreateInstance("A", new Dictionary<string, object?>());

        var result = wf.Fire(instance, "Go");

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(TransitionOutcome.Rejected);
    }

    [Fact]
    public void FireResult_IsSuccess_False_WhenConstraintFailure()
    {
        const string dsl = """
            precept Test
            field Balance as number default 100
            invariant Balance >= 0 because "Balance cannot go negative"
            state A initial
            event Debit with Amount as number
            from A on Debit -> set Balance = Balance - Debit.Amount -> transition A
            """;
        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = wf.CreateInstance("A", new Dictionary<string, object?> { ["Balance"] = 100.0 });

        var result = wf.Fire(instance, "Debit", new Dictionary<string, object?> { ["Amount"] = 200.0 });

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);
    }

    // ========================================================================================
    // IsSuccess — EventInspectionResult
    // ========================================================================================

    [Fact]
    public void InspectResult_IsSuccess_True_WhenTransitionPredicted()
    {
        const string dsl = """
            precept Test
            state A initial
            state B
            event Go
            from A on Go -> transition B
            """;
        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = wf.CreateInstance("A", new Dictionary<string, object?>());

        var result = wf.Inspect(instance, "Go");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void InspectResult_IsSuccess_False_WhenRejected()
    {
        const string dsl = """
            precept Test
            state A initial
            event Go with Amount as number
            on Go assert Amount > 0 because "must be positive"
            from A on Go -> no transition
            """;
        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = wf.CreateInstance("A", new Dictionary<string, object?>());

        var result = wf.Inspect(instance, "Go", new Dictionary<string, object?> { ["Amount"] = -1.0 });

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(TransitionOutcome.Rejected);
    }

    // ========================================================================================
    // IsSuccess — UpdateResult
    // ========================================================================================

    [Fact]
    public void UpdateResult_IsSuccess_True_WhenEditSucceeds()
    {
        const string dsl = """
            precept Test
            field Priority as number default 3
            state Open initial
            in Open edit Priority
            """;
        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = wf.CreateInstance("Open", new Dictionary<string, object?> { ["Priority"] = 3.0 });

        var result = wf.Update(instance, p => p.Set("Priority", 4.0));

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(UpdateOutcome.Update);
    }

    [Fact]
    public void UpdateResult_IsSuccess_False_WhenConstraintFailure()
    {
        const string dsl = """
            precept Test
            field Priority as number default 3
            invariant Priority >= 1 && Priority <= 5 because "Priority must be between 1 and 5"
            state Open initial
            in Open edit Priority
            """;
        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = wf.CreateInstance("Open", new Dictionary<string, object?> { ["Priority"] = 3.0 });

        var result = wf.Update(instance, p => p.Set("Priority", 99.0));

        result.IsSuccess.Should().BeFalse();
        result.Violations.Should().ContainSingle().Which.Message.Should().Contain("Priority must be between 1 and 5");
    }

    // ========================================================================================
    // ConstraintFailure vs Rejected — outcome distinction
    // ========================================================================================

    [Fact]
    public void Fire_ExplicitReject_ProducesRejectedOutcome_NotConstraintFailure()
    {
        const string dsl = """
            precept Test
            state A initial
            event Go
            from A on Go -> reject "author decided to block this"
            """;
        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = wf.CreateInstance("A", new Dictionary<string, object?>());

        var result = wf.Fire(instance, "Go");

        result.Outcome.Should().Be(TransitionOutcome.Rejected);
        result.Outcome.Should().NotBe(TransitionOutcome.ConstraintFailure);
    }

    [Fact]
    public void Fire_PostMutationInvariantFailure_ProducesConstraintFailureOutcome_NotRejected()
    {
        const string dsl = """
            precept Test
            field Balance as number default 100
            invariant Balance >= 0 because "Balance cannot go negative"
            state A initial
            event Debit with Amount as number
            from A on Debit -> set Balance = Balance - Debit.Amount -> transition A
            """;
        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = wf.CreateInstance("A", new Dictionary<string, object?> { ["Balance"] = 100.0 });

        var result = wf.Fire(instance, "Debit", new Dictionary<string, object?> { ["Amount"] = 200.0 });

        result.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);
        result.Outcome.Should().NotBe(TransitionOutcome.Rejected);
    }

    // ========================================================================================
    // Scenario 1: Event assert → EventArgTarget + EventTarget (Rejected)
    // ========================================================================================

    [Fact]
    public void Violation_EventAssert_HasEventArgAndEventTargets()
    {
        const string dsl = """
            precept Payment
            state Active initial
            event MakePayment with Amount as number
            on MakePayment assert Amount > 0 because "Amount must be positive"
            from Active on MakePayment -> no transition
            """;
        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = wf.CreateInstance("Active", new Dictionary<string, object?>());

        var result = wf.Inspect(instance, "MakePayment", new Dictionary<string, object?> { ["Amount"] = 0.0 });

        result.Outcome.Should().Be(TransitionOutcome.Rejected);
        var v = result.Violations.Should().ContainSingle().Subject;
        v.Message.Should().Contain("Amount must be positive");
        v.Source.Kind.Should().Be(ConstraintSourceKind.EventAssertion);
        v.Targets.OfType<ConstraintTarget.EventArgTarget>().Should().Contain(ea => ea.EventName == "MakePayment" && ea.ArgName == "Amount");
        v.Targets.OfType<ConstraintTarget.EventTarget>().Should().Contain(et => et.EventName == "MakePayment");
    }

    // ========================================================================================
    // Scenario 2: Post-mutation invariant → FieldTarget + DefinitionTarget (ConstraintFailure)
    // ========================================================================================

    [Fact]
    public void Violation_PostMutationInvariant_HasFieldAndDefinitionTargets()
    {
        const string dsl = """
            precept Payment
            field Balance as number default 1000
            invariant Balance >= 0 because "Balance cannot go negative"
            state Active initial
            event MakePayment with Amount as number
            from Active on MakePayment -> set Balance = Balance - MakePayment.Amount -> transition Active
            """;
        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = wf.CreateInstance("Active", new Dictionary<string, object?> { ["Balance"] = 1000.0 });

        var result = wf.Inspect(instance, "MakePayment", new Dictionary<string, object?> { ["Amount"] = 5000.0 });

        result.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);
        var v = result.Violations.Should().ContainSingle().Subject;
        v.Message.Should().Contain("Balance cannot go negative");
        v.Source.Kind.Should().Be(ConstraintSourceKind.Invariant);
        v.Targets.OfType<ConstraintTarget.FieldTarget>().Should().Contain(ft => ft.FieldName == "Balance");
        v.Targets.OfType<ConstraintTarget.DefinitionTarget>().Should().ContainSingle();
    }

    // ========================================================================================
    // Scenario 3: State assert on entry → FieldTarget + StateTarget (ConstraintFailure)
    // ========================================================================================

    [Fact]
    public void Violation_StateAssertOnEntry_HasFieldAndStateTargets()
    {
        const string dsl = """
            precept Ticket
            field AssignedAgent as string nullable
            state Open initial
            state Assigned
            in Assigned assert AssignedAgent != null because "Must have an assigned agent"
            event StartWork
            from Open on StartWork -> transition Assigned
            """;
        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = wf.CreateInstance("Open", new Dictionary<string, object?> { ["AssignedAgent"] = null });

        var result = wf.Inspect(instance, "StartWork");

        result.Outcome.Should().Be(TransitionOutcome.ConstraintFailure);
        var v = result.Violations.Should().ContainSingle().Subject;
        v.Message.Should().Contain("Must have an assigned agent");
        v.Source.Kind.Should().Be(ConstraintSourceKind.StateAssertion);
        v.Targets.OfType<ConstraintTarget.FieldTarget>().Should().Contain(ft => ft.FieldName == "AssignedAgent");
        v.Targets.OfType<ConstraintTarget.StateTarget>().Should().Contain(st => st.StateName == "Assigned");
    }

    // ========================================================================================
    // Scenario 4: Reject row → TransitionRejectionSource + EventTarget (Rejected)
    // ========================================================================================

    [Fact]
    public void Violation_RejectRow_HasTransitionRejectionSourceAndEventTarget()
    {
        const string dsl = """
            precept Loan
            field CreditScore as number default 0
            state UnderReview initial
            state Approved
            event Approve with ApprovedAmount as number
            from UnderReview on Approve when CreditScore >= 700 -> transition Approved
            from UnderReview on Approve -> reject "Credit score too low"
            """;
        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = wf.CreateInstance("UnderReview", new Dictionary<string, object?> { ["CreditScore"] = 500.0 });

        var result = wf.Inspect(instance, "Approve", new Dictionary<string, object?> { ["ApprovedAmount"] = 10000.0 });

        result.Outcome.Should().Be(TransitionOutcome.Rejected);
        var v = result.Violations.Should().ContainSingle().Subject;
        v.Message.Should().Contain("Credit score too low");
        v.Source.Kind.Should().Be(ConstraintSourceKind.TransitionRejection);
        v.Targets.OfType<ConstraintTarget.EventTarget>().Should().Contain(et => et.EventName == "Approve");
    }

    // ========================================================================================
    // Scenario 5: Unmatched — no violations emitted
    // ========================================================================================

    [Fact]
    public void Unmatched_ProducesNoViolations()
    {
        const string dsl = """
            precept Document
            field ExpiryExtended as boolean default false
            state Expired initial
            state Signing
            event ExtendExpiry
            from Expired on ExtendExpiry when !ExpiryExtended -> set ExpiryExtended = true -> transition Signing
            """;
        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = wf.CreateInstance("Expired", new Dictionary<string, object?> { ["ExpiryExtended"] = true });

        var result = wf.Inspect(instance, "ExtendExpiry");

        result.Outcome.Should().Be(TransitionOutcome.Unmatched);
        result.Violations.Should().BeEmpty();
    }

    // ========================================================================================
    // Scenario 7: Multi-subject invariant → multiple FieldTargets + DefinitionTarget
    // ========================================================================================

    [Fact]
    public void Violation_MultiSubjectInvariant_HasAllReferencedFieldTargets()
    {
        const string dsl = """
            precept TimeWindow
            field StartHour as number default 0
            field EndHour as number default 24
            invariant StartHour <= EndHour because "Start must not exceed end"
            state Active initial
            in Active edit StartHour, EndHour
            """;
        var wf = PreceptCompiler.Compile(PreceptParser.Parse(dsl));
        var instance = wf.CreateInstance("Active", new Dictionary<string, object?> { ["StartHour"] = 0.0, ["EndHour"] = 24.0 });

        var result = wf.Update(instance, p => p.Set("StartHour", 20.0).Set("EndHour", 10.0));

        result.IsSuccess.Should().BeFalse();
        var v = result.Violations.Should().ContainSingle().Subject;
        v.Message.Should().Contain("Start must not exceed end");
        v.Source.Kind.Should().Be(ConstraintSourceKind.Invariant);
        v.Targets.OfType<ConstraintTarget.FieldTarget>().Should().Contain(ft => ft.FieldName == "StartHour");
        v.Targets.OfType<ConstraintTarget.FieldTarget>().Should().Contain(ft => ft.FieldName == "EndHour");
        v.Targets.OfType<ConstraintTarget.DefinitionTarget>().Should().ContainSingle();
    }
}
