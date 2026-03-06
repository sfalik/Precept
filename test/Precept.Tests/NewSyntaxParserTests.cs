using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Precept;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Tests for the new Superpower-based parser (Precept language redesign syntax).
/// Validates that the new syntax is parsed correctly into the model records
/// (field/as, invariant/because, state asserts, event asserts, state actions,
/// transition rows, edit blocks, and the complete pipeline through compile+fire).
/// </summary>
public class NewSyntaxParserTests
{
    // ════════════════════════════════════════════════════════════════════
    // PARSING — Minimal file
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_MinimalNewSyntax_Succeeds()
    {
        const string dsl = """
            precept Minimal
            state Idle initial
            """;

        var model = PreceptParser.Parse(dsl);

        model.Name.Should().Be("Minimal");
        model.States.Should().HaveCount(1);
        model.InitialState.Name.Should().Be("Idle");
    }

    [Fact]
    public void Parse_NoInitialState_Throws()
    {
        const string dsl = """
            precept NoInit
            state A
            state B
            """;

        var act = () => PreceptParser.Parse(dsl);
        act.Should().Throw<InvalidOperationException>().WithMessage("*initial*");
    }

    [Fact]
    public void Parse_DuplicateState_Throws()
    {
        const string dsl = """
            precept Dup
            state A initial
            state A
            """;

        var act = () => PreceptParser.Parse(dsl);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Duplicate state*");
    }

    // ════════════════════════════════════════════════════════════════════
    // PARSING — Field declarations (field <Name> as <Type>)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_ScalarField_NumberWithDefault()
    {
        const string dsl = """
            precept Test
            field Score as number default 42
            state Active initial
            """;

        var model = PreceptParser.Parse(dsl);

        model.Fields.Should().HaveCount(1);
        var f = model.Fields[0];
        f.Name.Should().Be("Score");
        f.Type.Should().Be(PreceptScalarType.Number);
        f.IsNullable.Should().BeFalse();
        f.HasDefaultValue.Should().BeTrue();
        f.DefaultValue.Should().Be(42.0);
    }

    [Fact]
    public void Parse_ScalarField_StringNullable()
    {
        const string dsl = """
            precept Test
            field Name as string nullable
            state Active initial
            """;

        var model = PreceptParser.Parse(dsl);

        var f = model.Fields[0];
        f.Name.Should().Be("Name");
        f.Type.Should().Be(PreceptScalarType.String);
        f.IsNullable.Should().BeTrue();
        f.HasDefaultValue.Should().BeTrue(); // nullable without default → default is null
        f.DefaultValue.Should().BeNull();
    }

    [Fact]
    public void Parse_ScalarField_BooleanWithDefault()
    {
        const string dsl = """
            precept Test
            field IsActive as boolean default true
            state Active initial
            """;

        var model = PreceptParser.Parse(dsl);

        var f = model.Fields[0];
        f.Type.Should().Be(PreceptScalarType.Boolean);
        f.HasDefaultValue.Should().BeTrue();
        f.DefaultValue.Should().Be(true);
    }

    [Fact]
    public void Parse_CollectionField_SetOfNumber()
    {
        const string dsl = """
            precept Test
            field Scores as set of number
            state Active initial
            """;

        var model = PreceptParser.Parse(dsl);

        model.CollectionFields.Should().HaveCount(1);
        var c = model.CollectionFields[0];
        c.Name.Should().Be("Scores");
        c.CollectionKind.Should().Be(PreceptCollectionKind.Set);
        c.InnerType.Should().Be(PreceptScalarType.Number);
    }

    [Fact]
    public void Parse_CollectionField_QueueOfString()
    {
        const string dsl = """
            precept Test
            field Messages as queue of string
            state Active initial
            """;

        var model = PreceptParser.Parse(dsl);

        var c = model.CollectionFields[0];
        c.CollectionKind.Should().Be(PreceptCollectionKind.Queue);
        c.InnerType.Should().Be(PreceptScalarType.String);
    }

    [Fact]
    public void Parse_CollectionField_StackOfBoolean()
    {
        const string dsl = """
            precept Test
            field Flags as stack of boolean
            state Active initial
            """;

        var model = PreceptParser.Parse(dsl);

        var c = model.CollectionFields[0];
        c.CollectionKind.Should().Be(PreceptCollectionKind.Stack);
        c.InnerType.Should().Be(PreceptScalarType.Boolean);
    }

    [Fact]
    public void Parse_DuplicateField_Throws()
    {
        const string dsl = """
            precept Test
            field X as number default 0
            field X as string default "dup"
            state Active initial
            """;

        var act = () => PreceptParser.Parse(dsl);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Duplicate field*");
    }

    // ════════════════════════════════════════════════════════════════════
    // PARSING — Event declarations (event <Name> [with ...])
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_Event_NoArgs()
    {
        const string dsl = """
            precept Test
            state Active initial
            event Ping
            """;

        var model = PreceptParser.Parse(dsl);

        model.Events.Should().HaveCount(1);
        model.Events[0].Name.Should().Be("Ping");
        model.Events[0].Args.Should().BeEmpty();
    }

    [Fact]
    public void Parse_Event_WithArgs()
    {
        const string dsl = """
            precept Test
            state Active initial
            event Submit with Amount as number, Label as string nullable default "none"
            """;

        var model = PreceptParser.Parse(dsl);

        var evt = model.Events[0];
        evt.Name.Should().Be("Submit");
        evt.Args.Should().HaveCount(2);

        evt.Args[0].Name.Should().Be("Amount");
        evt.Args[0].Type.Should().Be(PreceptScalarType.Number);
        evt.Args[0].IsNullable.Should().BeFalse();
        evt.Args[0].HasDefaultValue.Should().BeFalse();

        evt.Args[1].Name.Should().Be("Label");
        evt.Args[1].Type.Should().Be(PreceptScalarType.String);
        evt.Args[1].IsNullable.Should().BeTrue();
        evt.Args[1].HasDefaultValue.Should().BeTrue();
        evt.Args[1].DefaultValue.Should().Be("none");
    }

    [Fact]
    public void Parse_DuplicateEvent_Throws()
    {
        const string dsl = """
            precept Test
            state Active initial
            event Go
            event Go
            """;

        var act = () => PreceptParser.Parse(dsl);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Duplicate event*");
    }

    // ════════════════════════════════════════════════════════════════════
    // PARSING — Invariant declarations
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_Invariant()
    {
        const string dsl = """
            precept Test
            field Balance as number default 100
            invariant Balance >= 0 because "Balance must be non-negative"
            state Active initial
            """;

        var model = PreceptParser.Parse(dsl);

        model.Invariants.Should().HaveCount(1);
        var inv = model.Invariants![0];
        inv.ExpressionText.Should().Be("Balance >= 0");
        inv.Reason.Should().Be("Balance must be non-negative");
        inv.Expression.Should().BeOfType<PreceptBinaryExpression>();
    }

    [Fact]
    public void Parse_MultipleInvariants()
    {
        const string dsl = """
            precept Test
            field X as number default 0
            field Y as number default 0
            invariant X >= 0 because "X non-negative"
            invariant Y >= 0 because "Y non-negative"
            state Active initial
            """;

        var model = PreceptParser.Parse(dsl);

        model.Invariants.Should().HaveCount(2);
    }

    // ════════════════════════════════════════════════════════════════════
    // PARSING — State asserts (in/to/from <State> assert ...)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_StateAssert_In()
    {
        const string dsl = """
            precept Test
            field Score as number default 10
            state Active initial
            in Active assert Score > 0 because "Score must be positive while active"
            """;

        var model = PreceptParser.Parse(dsl);

        model.StateAsserts.Should().HaveCount(1);
        var sa = model.StateAsserts![0];
        sa.Preposition.Should().Be(PreceptAssertPreposition.In);
        sa.State.Should().Be("Active");
        sa.Reason.Should().Be("Score must be positive while active");
    }

    [Fact]
    public void Parse_StateAssert_To()
    {
        const string dsl = """
            precept Test
            field Score as number default 10
            state Active initial
            state Done
            to Done assert Score > 5 because "Need enough score to finish"
            """;

        var model = PreceptParser.Parse(dsl);

        var sa = model.StateAsserts![0];
        sa.Preposition.Should().Be(PreceptAssertPreposition.To);
        sa.State.Should().Be("Done");
    }

    [Fact]
    public void Parse_StateAssert_From()
    {
        const string dsl = """
            precept Test
            field Score as number default 10
            state Active initial
            state Done
            from Active assert Score >= 0 because "Cannot leave with negative score"
            """;

        var model = PreceptParser.Parse(dsl);

        var sa = model.StateAsserts![0];
        sa.Preposition.Should().Be(PreceptAssertPreposition.From);
        sa.State.Should().Be("Active");
    }

    [Fact]
    public void Parse_StateAssert_MultiState()
    {
        const string dsl = """
            precept Test
            field Score as number default 10
            state Active initial
            state Paused
            in Active, Paused assert Score > 0 because "Score must be positive"
            """;

        var model = PreceptParser.Parse(dsl);

        model.StateAsserts.Should().HaveCount(2);
        model.StateAsserts![0].State.Should().Be("Active");
        model.StateAsserts![1].State.Should().Be("Paused");
    }

    [Fact]
    public void Parse_StateAssert_Any()
    {
        const string dsl = """
            precept Test
            field Score as number default 10
            state Active initial
            state Paused
            state Done
            in any assert Score >= 0 because "Score never negative"
            """;

        var model = PreceptParser.Parse(dsl);

        // 'any' expands to all 3 states
        model.StateAsserts.Should().HaveCount(3);
        model.StateAsserts!.Select(s => s.State).Should().BeEquivalentTo("Active", "Paused", "Done");
    }

    // ════════════════════════════════════════════════════════════════════
    // PARSING — Event asserts (on <Event> assert ...)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_EventAssert()
    {
        const string dsl = """
            precept Test
            state Active initial
            event Pay with Amount as number
            on Pay assert Amount > 0 because "Amount must be positive"
            """;

        var model = PreceptParser.Parse(dsl);

        model.EventAsserts.Should().HaveCount(1);
        var ea = model.EventAsserts![0];
        ea.EventName.Should().Be("Pay");
        ea.Reason.Should().Be("Amount must be positive");
    }

    // ════════════════════════════════════════════════════════════════════
    // PARSING — State actions (to/from <State> -> ...)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_StateAction_EntrySet()
    {
        const string dsl = """
            precept Test
            field Counter as number default 0
            state Active initial
            state Done
            to Done -> set Counter = Counter + 1
            """;

        var model = PreceptParser.Parse(dsl);

        model.StateActions.Should().HaveCount(1);
        var sa = model.StateActions![0];
        sa.Preposition.Should().Be(PreceptAssertPreposition.To);
        sa.State.Should().Be("Done");
        sa.SetAssignments.Should().HaveCount(1);
        sa.SetAssignments[0].Key.Should().Be("Counter");
    }

    [Fact]
    public void Parse_StateAction_ExitSet()
    {
        const string dsl = """
            precept Test
            field Counter as number default 0
            state Active initial
            state Done
            from Active -> set Counter = Counter - 1
            """;

        var model = PreceptParser.Parse(dsl);

        var sa = model.StateActions![0];
        sa.Preposition.Should().Be(PreceptAssertPreposition.From);
        sa.State.Should().Be("Active");
    }

    [Fact]
    public void Parse_StateAction_MultipleActions()
    {
        const string dsl = """
            precept Test
            field X as number default 0
            field Y as number default 0
            state A initial
            state B
            to B -> set X = 1 -> set Y = 2
            """;

        var model = PreceptParser.Parse(dsl);

        var sa = model.StateActions![0];
        sa.SetAssignments.Should().HaveCount(2);
        sa.SetAssignments[0].Key.Should().Be("X");
        sa.SetAssignments[1].Key.Should().Be("Y");
    }

    [Fact]
    public void Parse_StateAction_CollectionMutation()
    {
        const string dsl = """
            precept Test
            field Log as queue of string
            state A initial
            state B
            to B -> enqueue Log "entered B"
            """;

        var model = PreceptParser.Parse(dsl);

        var sa = model.StateActions![0];
        sa.CollectionMutations.Should().HaveCount(1);
        sa.CollectionMutations![0].Verb.Should().Be(PreceptCollectionMutationVerb.Enqueue);
        sa.CollectionMutations![0].TargetField.Should().Be("Log");
    }

    // ════════════════════════════════════════════════════════════════════
    // PARSING — Edit declarations (in <State> edit <Fields>)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_EditBlock()
    {
        const string dsl = """
            precept Test
            field Name as string default "unnamed"
            field Score as number default 0
            state Active initial
            in Active edit Name, Score
            """;

        var model = PreceptParser.Parse(dsl);

        model.EditBlocks.Should().HaveCount(1);
        var eb = model.EditBlocks![0];
        eb.State.Should().Be("Active");
        eb.FieldNames.Should().BeEquivalentTo("Name", "Score");
    }

    [Fact]
    public void Parse_EditBlock_Any()
    {
        const string dsl = """
            precept Test
            field Name as string default "unnamed"
            state A initial
            state B
            in any edit Name
            """;

        var model = PreceptParser.Parse(dsl);

        model.EditBlocks.Should().HaveCount(2);
        model.EditBlocks!.Select(e => e.State).Should().BeEquivalentTo("A", "B");
    }

    // ════════════════════════════════════════════════════════════════════
    // PARSING — Transition rows (from <State> on <Event> ... -> outcome)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_TransitionRow_SimpleTransition()
    {
        const string dsl = """
            precept Test
            state A initial
            state B
            event Go
            from A on Go -> transition B
            """;

        var model = PreceptParser.Parse(dsl);

        model.TransitionRows.Should().HaveCount(1);
        var row = model.TransitionRows![0];
        row.FromState.Should().Be("A");
        row.EventName.Should().Be("Go");
        row.Outcome.Should().BeOfType<PreceptStateTransition>()
            .Which.TargetState.Should().Be("B");
        row.WhenGuard.Should().BeNull();
        row.SetAssignments.Should().BeEmpty();
    }

    [Fact]
    public void Parse_TransitionRow_WithSetAndTransition()
    {
        const string dsl = """
            precept Test
            field Counter as number default 0
            state A initial
            state B
            event Go
            from A on Go -> set Counter = Counter + 1 -> transition B
            """;

        var model = PreceptParser.Parse(dsl);

        var row = model.TransitionRows![0];
        row.SetAssignments.Should().HaveCount(1);
        row.SetAssignments[0].Key.Should().Be("Counter");
    }

    [Fact]
    public void Parse_TransitionRow_WithWhenGuard()
    {
        const string dsl = """
            precept Test
            field Score as number default 10
            state A initial
            state B
            event Go
            from A on Go when Score > 5 -> transition B
            """;

        var model = PreceptParser.Parse(dsl);

        var row = model.TransitionRows![0];
        row.WhenGuard.Should().NotBeNull();
        row.WhenText.Should().Be("Score > 5");
    }

    [Fact]
    public void Parse_TransitionRow_NoTransition()
    {
        const string dsl = """
            precept Test
            state A initial
            event Ping
            from A on Ping -> no transition
            """;

        var model = PreceptParser.Parse(dsl);

        var row = model.TransitionRows![0];
        row.Outcome.Should().BeOfType<PreceptNoTransition>();
    }

    [Fact]
    public void Parse_TransitionRow_Reject()
    {
        const string dsl = """
            precept Test
            state A initial
            event Bad
            from A on Bad -> reject "Not allowed"
            """;

        var model = PreceptParser.Parse(dsl);

        var row = model.TransitionRows![0];
        row.Outcome.Should().BeOfType<PreceptRejection>()
            .Which.Reason.Should().Be("Not allowed");
    }

    [Fact]
    public void Parse_TransitionRow_MultipleRows_SameStateEvent()
    {
        const string dsl = """
            precept Test
            field Score as number default 10
            state A initial
            state B
            state C
            event Move
            from A on Move when Score > 5 -> transition B
            from A on Move -> transition C
            """;

        var model = PreceptParser.Parse(dsl);

        model.TransitionRows.Should().HaveCount(2);
        model.TransitionRows![0].WhenGuard.Should().NotBeNull();
        model.TransitionRows![1].WhenGuard.Should().BeNull();
    }

    [Fact]
    public void Parse_TransitionRow_Any()
    {
        const string dsl = """
            precept Test
            field X as number default 0
            state A initial
            state B
            event Reset
            from any on Reset -> set X = 0 -> transition A
            """;

        var model = PreceptParser.Parse(dsl);

        // 'any' expands to both states
        model.TransitionRows.Should().HaveCount(2);
        model.TransitionRows!.Select(r => r.FromState).Should().BeEquivalentTo("A", "B");
    }

    [Fact]
    public void Parse_TransitionRow_MultiState()
    {
        const string dsl = """
            precept Test
            state A initial
            state B
            state C
            event Go
            from A, B on Go -> transition C
            """;

        var model = PreceptParser.Parse(dsl);

        model.TransitionRows.Should().HaveCount(2);
        model.TransitionRows!.Select(r => r.FromState).Should().BeEquivalentTo("A", "B");
    }

    [Fact]
    public void Parse_TransitionRow_WithCollectionMutation()
    {
        const string dsl = """
            precept Test
            field Items as set of string
            state A initial
            state B
            event Go with Item as string
            from A on Go -> add Items Go.Item -> transition B
            """;

        var model = PreceptParser.Parse(dsl);

        var row = model.TransitionRows![0];
        row.CollectionMutations.Should().HaveCount(1);
        row.CollectionMutations![0].Verb.Should().Be(PreceptCollectionMutationVerb.Add);
        row.CollectionMutations![0].TargetField.Should().Be("Items");
    }

    [Fact]
    public void Parse_TransitionRow_MissingOutcome_Throws()
    {
        const string dsl = """
            precept Test
            field X as number default 0
            state A initial
            event Go
            from A on Go -> set X = 1
            """;

        var act = () => PreceptParser.Parse(dsl);
        act.Should().Throw<InvalidOperationException>().WithMessage("*missing an outcome*");
    }

    // ════════════════════════════════════════════════════════════════════
    // PARSING — Backward compat: old-style transitions also populated
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_TransitionRow_AlsoPopulatesOldTransitions()
    {
        const string dsl = """
            precept Test
            state A initial
            state B
            event Go
            from A on Go -> transition B
            """;

        var model = PreceptParser.Parse(dsl);

        // New model
        model.TransitionRows.Should().HaveCount(1);

        // Also populated for backward compat
        model.Transitions.Should().HaveCount(1);
        model.Transitions[0].FromState.Should().Be("A");
        model.Transitions[0].EventName.Should().Be("Go");
        model.Transitions[0].Clauses.Should().HaveCount(1);
        model.Transitions[0].Clauses[0].Outcome.Should().BeOfType<PreceptStateTransition>()
            .Which.TargetState.Should().Be("B");
    }

    // ════════════════════════════════════════════════════════════════════
    // PARSING — Invariant also adds old-style top-level rule
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_Invariant_AlsoPopulatesTopLevelRules()
    {
        const string dsl = """
            precept Test
            field X as number default 0
            invariant X >= 0 because "X non-negative"
            state Active initial
            """;

        var model = PreceptParser.Parse(dsl);

        model.TopLevelRules.Should().HaveCount(1);
        model.TopLevelRules![0].ExpressionText.Should().Be("X >= 0");
        model.TopLevelRules![0].Reason.Should().Be("X non-negative");
    }

    // ════════════════════════════════════════════════════════════════════
    // PARSING — Expression parser
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_Expression_Arithmetic()
    {
        const string dsl = """
            precept Test
            field X as number default 5
            state A initial
            state B
            event Go
            from A on Go -> set X = X + 1 * 2 -> transition B
            """;

        var model = PreceptParser.Parse(dsl);

        var row = model.TransitionRows![0];
        row.SetAssignments[0].Expression.Should().BeOfType<PreceptBinaryExpression>();
    }

    [Fact]
    public void Parse_Expression_DottedIdentifier()
    {
        const string dsl = """
            precept Test
            field X as number default 0
            state A initial
            state B
            event Go with Amount as number
            from A on Go -> set X = Go.Amount -> transition B
            """;

        var model = PreceptParser.Parse(dsl);

        var expr = model.TransitionRows![0].SetAssignments[0].Expression as PreceptIdentifierExpression;
        expr.Should().NotBeNull();
        expr!.Name.Should().Be("Go");
        expr.Member.Should().Be("Amount");
    }

    [Fact]
    public void Parse_Expression_BooleanGuardWithComparison()
    {
        const string dsl = """
            precept Test
            field X as number default 10
            state A initial
            state B
            event Go
            from A on Go when X > 5 && X < 20 -> transition B
            """;

        var model = PreceptParser.Parse(dsl);

        var guard = model.TransitionRows![0].WhenGuard as PreceptBinaryExpression;
        guard.Should().NotBeNull();
        guard!.Operator.Should().Be("&&");
    }

    [Fact]
    public void Parse_Expression_ContainsOperator()
    {
        const string dsl = """
            precept Test
            field Tags as set of string
            state A initial
            state B
            event Go with Tag as string
            from A on Go when Tags contains Go.Tag -> transition B
            """;

        var model = PreceptParser.Parse(dsl);

        var guard = model.TransitionRows![0].WhenGuard as PreceptBinaryExpression;
        guard.Should().NotBeNull();
        guard!.Operator.Should().Be("contains");
    }

    [Fact]
    public void Parse_Expression_NullLiteral()
    {
        const string dsl = """
            precept Test
            field Name as string nullable
            state A initial
            state B
            event Go
            from A on Go when Name != null -> transition B
            """;

        var model = PreceptParser.Parse(dsl);

        var guard = model.TransitionRows![0].WhenGuard as PreceptBinaryExpression;
        guard.Should().NotBeNull();
        guard!.Operator.Should().Be("!=");
        guard.Right.Should().BeOfType<PreceptLiteralExpression>()
            .Which.Value.Should().BeNull();
    }

    [Fact]
    public void Parse_Expression_UnaryNot()
    {
        const string dsl = """
            precept Test
            field Active as boolean default true
            state A initial
            state B
            event Go
            from A on Go when !Active -> transition B
            """;

        var model = PreceptParser.Parse(dsl);

        var guard = model.TransitionRows![0].WhenGuard as PreceptUnaryExpression;
        guard.Should().NotBeNull();
        guard!.Operator.Should().Be("!");
    }

    [Fact]
    public void Parse_Expression_Parenthesized()
    {
        const string dsl = """
            precept Test
            field X as number default 5
            state A initial
            state B
            event Go
            from A on Go -> set X = (X + 1) * 2 -> transition B
            """;

        var model = PreceptParser.Parse(dsl);

        var expr = model.TransitionRows![0].SetAssignments[0].Expression as PreceptBinaryExpression;
        expr.Should().NotBeNull();
        expr!.Left.Should().BeOfType<PreceptParenthesizedExpression>();
    }

    // ════════════════════════════════════════════════════════════════════
    // PARSING — Comments in new syntax
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_WithComments_IgnoresComments()
    {
        const string dsl = """
            # This is a file comment
            precept Test  # inline comment
            field X as number default 0  # field comment
            state Active initial  # state comment
            event Go  # event comment
            from Active on Go -> transition Active  # transition comment
            """;

        var model = PreceptParser.Parse(dsl);

        model.Name.Should().Be("Test");
        model.Fields.Should().HaveCount(1);
        model.States.Should().HaveCount(1);
        model.Events.Should().HaveCount(1);
    }

    // ════════════════════════════════════════════════════════════════════
    // PARSING — Complete model with all constructs
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_FullModel_AllConstructs()
    {
        const string dsl = """
            precept OrderWorkflow
            field Total as number default 0
            field Notes as string nullable
            field Items as set of string
            invariant Total >= 0 because "Total cannot be negative"
            state Draft initial
            state Submitted
            state Approved
            event Submit with Amount as number
            event Approve
            on Submit assert Amount > 0 because "Amount must be positive"
            in Submitted assert Total > 0 because "Submitted orders must have a total"
            to Approved assert Total >= 10 because "Need minimum total to approve"
            from Draft assert Total >= 0 because "Cannot leave draft with negative total"
            to Submitted -> set Notes = "submitted"
            from Draft -> set Notes = "left draft"
            in Draft edit Notes
            from Draft on Submit -> set Total = Submit.Amount -> add Items "order" -> transition Submitted
            from Submitted on Approve -> transition Approved
            """;

        var model = PreceptParser.Parse(dsl);

        model.Name.Should().Be("OrderWorkflow");
        model.Fields.Should().HaveCount(2);
        model.CollectionFields.Should().HaveCount(1);
        model.Invariants.Should().HaveCount(1);
        model.States.Should().HaveCount(3);
        model.Events.Should().HaveCount(2);
        model.EventAsserts.Should().HaveCount(1);
        model.StateAsserts.Should().HaveCount(3);
        model.StateActions.Should().HaveCount(2);
        model.EditBlocks.Should().HaveCount(1);
        model.TransitionRows.Should().HaveCount(2);
    }
}
