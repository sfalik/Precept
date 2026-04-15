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
/// (field/as, rule/because, state ensures, event ensures, state actions,
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
        model.InitialState!.Name.Should().Be("Idle");
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
    public void Parse_Rule()
    {
        const string dsl = """
            precept Test
            field Balance as number default 100
            rule Balance >= 0 because "Balance must be non-negative"
            state Active initial
            """;

        var model = PreceptParser.Parse(dsl);

        model.Rules.Should().HaveCount(1);
        var inv = model.Rules![0];
        inv.ExpressionText.Should().Be("Balance >= 0");
        inv.Reason.Should().Be("Balance must be non-negative");
        inv.Expression.Should().BeOfType<PreceptBinaryExpression>();
    }

    [Fact]
    public void Parse_MultipleRules()
    {
        const string dsl = """
            precept Test
            field X as number default 0
            field Y as number default 0
            rule X >= 0 because "X non-negative"
            rule Y >= 0 because "Y non-negative"
            state Active initial
            """;

        var model = PreceptParser.Parse(dsl);

        model.Rules.Should().HaveCount(2);
    }

    // ════════════════════════════════════════════════════════════════════
    // PARSING — State ensures (in/to/from <State> assert ...)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_StateEnsure_In()
    {
        const string dsl = """
            precept Test
            field Score as number default 10
            state Active initial
            in Active ensure Score > 0 because "Score must be positive while active"
            """;

        var model = PreceptParser.Parse(dsl);

        model.StateEnsures.Should().HaveCount(1);
        var sa = model.StateEnsures![0];
        sa.Anchor.Should().Be(EnsureAnchor.In);
        sa.State.Should().Be("Active");
        sa.Reason.Should().Be("Score must be positive while active");
    }

    [Fact]
    public void Parse_StateEnsure_To()
    {
        const string dsl = """
            precept Test
            field Score as number default 10
            state Active initial
            state Done
            to Done ensure Score > 5 because "Need enough score to finish"
            """;

        var model = PreceptParser.Parse(dsl);

        var sa = model.StateEnsures![0];
        sa.Anchor.Should().Be(EnsureAnchor.To);
        sa.State.Should().Be("Done");
    }

    [Fact]
    public void Parse_StateEnsure_From()
    {
        const string dsl = """
            precept Test
            field Score as number default 10
            state Active initial
            state Done
            from Active ensure Score >= 0 because "Cannot leave with negative score"
            """;

        var model = PreceptParser.Parse(dsl);

        var sa = model.StateEnsures![0];
        sa.Anchor.Should().Be(EnsureAnchor.From);
        sa.State.Should().Be("Active");
    }

    [Fact]
    public void Parse_StateEnsure_MultiState()
    {
        const string dsl = """
            precept Test
            field Score as number default 10
            state Active initial
            state Paused
            in Active, Paused ensure Score > 0 because "Score must be positive"
            """;

        var model = PreceptParser.Parse(dsl);

        model.StateEnsures.Should().HaveCount(2);
        model.StateEnsures![0].State.Should().Be("Active");
        model.StateEnsures![1].State.Should().Be("Paused");
    }

    [Fact]
    public void Parse_StateEnsure_Any()
    {
        const string dsl = """
            precept Test
            field Score as number default 10
            state Active initial
            state Paused
            state Done
            in any ensure Score >= 0 because "Score never negative"
            """;

        var model = PreceptParser.Parse(dsl);

        // 'any' expands to all 3 states
        model.StateEnsures.Should().HaveCount(3);
        model.StateEnsures!.Select(s => s.State).Should().BeEquivalentTo("Active", "Paused", "Done");
    }

    // ════════════════════════════════════════════════════════════════════
    // PARSING — Event ensures (on <Event> assert ...)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_EventEnsure()
    {
        const string dsl = """
            precept Test
            state Active initial
            event Pay with Amount as number
            on Pay ensure Amount > 0 because "Amount must be positive"
            """;

        var model = PreceptParser.Parse(dsl);

        model.EventEnsures.Should().HaveCount(1);
        var ea = model.EventEnsures![0];
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
        sa.Anchor.Should().Be(EnsureAnchor.To);
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
        sa.Anchor.Should().Be(EnsureAnchor.From);
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
        row.Outcome.Should().BeOfType<StateTransition>()
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
        row.Outcome.Should().BeOfType<NoTransition>();
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
        row.Outcome.Should().BeOfType<Rejection>()
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
            from A on Go when X > 5 and X < 20 -> transition B
            """;

        var model = PreceptParser.Parse(dsl);

        var guard = model.TransitionRows![0].WhenGuard as PreceptBinaryExpression;
        guard.Should().NotBeNull();
        guard!.Operator.Should().Be("and");
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
            from A on Go when not Active -> transition B
            """;

        var model = PreceptParser.Parse(dsl);

        var guard = model.TransitionRows![0].WhenGuard as PreceptUnaryExpression;
        guard.Should().NotBeNull();
        guard!.Operator.Should().Be("not");
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
            rule Total >= 0 because "Total cannot be negative"
            state Draft initial
            state Submitted
            state Approved
            event Submit with Amount as number
            event Approve
            on Submit ensure Amount > 0 because "Amount must be positive"
            in Submitted ensure Total > 0 because "Submitted orders must have a total"
            to Approved ensure Total >= 10 because "Need minimum total to approve"
            from Draft ensure Total >= 0 because "Cannot leave draft with negative total"
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
        model.Rules.Should().HaveCount(1);
        model.States.Should().HaveCount(3);
        model.Events.Should().HaveCount(2);
        model.EventEnsures.Should().HaveCount(1);
        model.StateEnsures.Should().HaveCount(3);
        model.StateActions.Should().HaveCount(2);
        model.EditBlocks.Should().HaveCount(1);
        model.TransitionRows.Should().HaveCount(2);
    }

    // ════════════════════════════════════════════════════════════════════
    // PARSING — Source line tracking on asserts (Category A gaps)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_StateEnsure_SourceLineIsCorrect()
    {
        const string dsl = """
            precept Test
            field AmountPaid as number default 0
            state Idle initial
            state Paid
            in Paid ensure AmountPaid > 0 because "Must have paid"
            """;

        var model = PreceptParser.Parse(dsl);

        var sa = model.StateEnsures.Should().ContainSingle().Subject;
        sa.SourceLine.Should().Be(5);
    }

    [Fact]
    public void Parse_EventEnsure_SourceLineIsCorrect()
    {
        const string dsl = """
            precept Test
            state Idle initial
            event Pay with Amount as number
            on Pay ensure Amount > 0 because "Amount must be positive"
            """;

        var model = PreceptParser.Parse(dsl);

        var ea = model.EventEnsures.Should().ContainSingle().Subject;
        ea.SourceLine.Should().Be(4);
    }

    // ════════════════════════════════════════════════════════════════════
    // PARSING — ParseWithDiagnostics API (Category D)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void ParseWithDiagnostics_ValidSource_ReturnsModelAndZeroDiagnostics()
    {
        const string dsl = """
            precept Test
            field Balance as number default 100
            rule Balance >= 0 because "Must be non-negative"
            state Active initial
            event Debit with Amount as number
            from Active on Debit -> set Balance = Balance - Debit.Amount -> transition Active
            """;

        var (model, diagnostics) = PreceptParser.ParseWithDiagnostics(dsl);

        model.Should().NotBeNull();
        diagnostics.Should().BeEmpty();
        model!.Name.Should().Be("Test");
    }

    [Fact]
    public void ParseWithDiagnostics_InvalidSyntax_ReturnsDiagnosticAndNullModel()
    {
        const string dsl = """
            precept Test
            state Active initial
            from Active on UNKNOWNKEYWORDTHATFAILS
            """;

        var (model, diagnostics) = PreceptParser.ParseWithDiagnostics(dsl);

        model.Should().BeNull();
        diagnostics.Should().NotBeEmpty();
    }

    [Fact]
    public void ParseWithDiagnostics_EmptyInput_ReturnsDiagnosticAndNullModel()
    {
        var (model, diagnostics) = PreceptParser.ParseWithDiagnostics("");

        model.Should().BeNull();
        diagnostics.Should().ContainSingle(d => d.Message.Contains("empty"));
    }

    [Fact]
    public void ParseWithDiagnostics_DuplicateState_ReturnsDiagnosticAndNullModel()
    {
        const string dsl = """
            precept Test
            state Active initial
            state Active
            """;

        var (model, diagnostics) = PreceptParser.ParseWithDiagnostics(dsl);

        model.Should().BeNull();
        diagnostics.Should().NotBeEmpty();
        diagnostics[0].Message.Should().Contain("Duplicate state");
    }

    // ════════════════════════════════════════════════════════════════════
    // PARSING — Diagnostic source line accuracy (regression guard)
    // Ensures constraint violations squiggle the offending declaration,
    // not the precept header line.
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void ParseWithDiagnostics_C17_NonNullableFieldWithoutDefault_DiagnosticOnFieldLine()
    {
        // Line 1: precept Task
        // Line 2: field Title as string nullable
        // Line 3: field Description as string nullable
        // Line 4: field Blah as choice("A", "B")  ← C17 violation here
        const string dsl = """
            precept Task
            field Title as string nullable
            field Description as string nullable
            field Blah as choice("A", "B")
            """;

        var (_, diagnostics) = PreceptParser.ParseWithDiagnostics(dsl);

        diagnostics.Should().ContainSingle();
        diagnostics[0].Message.Should().Contain("Blah");
        diagnostics[0].Line.Should().Be(4);
    }

    [Fact]
    public void ParseWithDiagnostics_C6_DuplicateField_DiagnosticOnSecondFieldLine()
    {
        // Line 4 is the duplicate field declaration that triggers C6.
        const string dsl = """
            precept Test
            field A as number default 0
            state Open initial
            field A as string nullable
            """;

        var (_, diagnostics) = PreceptParser.ParseWithDiagnostics(dsl);

        diagnostics.Should().ContainSingle();
        diagnostics[0].Message.Should().Contain("Duplicate field");
        diagnostics[0].Line.Should().Be(4);
    }

    [Fact]
    public void ParseWithDiagnostics_C7_DuplicateState_DiagnosticOnSecondStateLine()
    {
        // Line 3 is the duplicate state declaration.
        const string dsl = """
            precept Test
            state Active initial
            state Active
            """;

        var (_, diagnostics) = PreceptParser.ParseWithDiagnostics(dsl);

        diagnostics.Should().ContainSingle();
        diagnostics[0].Message.Should().Contain("Duplicate state");
        diagnostics[0].Line.Should().Be(3);
    }

    [Fact]
    public void ParseWithDiagnostics_C9_DuplicateEvent_DiagnosticOnSecondEventLine()
    {
        // Line 4 is the duplicate event declaration.
        const string dsl = """
            precept Test
            state A initial
            event Go
            event Go
            from A on Go -> no transition
            """;

        var (_, diagnostics) = PreceptParser.ParseWithDiagnostics(dsl);

        diagnostics.Should().ContainSingle();
        diagnostics[0].Message.Should().Contain("Duplicate event");
        diagnostics[0].Line.Should().Be(4);
    }

    // ════════════════════════════════════════════════════════════════════
    // PARSING — Multi-name state declarations
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_MultiState_Succeeds()
    {
        const string dsl = """
            precept Test
            state A initial, B, C
            """;

        var model = PreceptParser.Parse(dsl);

        model.States.Should().HaveCount(3);
        model.States[0].Name.Should().Be("A");
        model.States[1].Name.Should().Be("B");
        model.States[2].Name.Should().Be("C");
        model.InitialState!.Name.Should().Be("A");
    }

    [Fact]
    public void Parse_MultiState_WithInitial_Succeeds()
    {
        const string dsl = """
            precept Test
            state A, B initial, C
            """;

        var model = PreceptParser.Parse(dsl);

        model.States.Should().HaveCount(3);
        model.InitialState!.Name.Should().Be("B");
    }

    [Fact]
    public void Parse_MultiState_FirstInitial_Succeeds()
    {
        const string dsl = """
            precept Test
            state A initial, B, C
            """;

        var model = PreceptParser.Parse(dsl);

        model.States.Should().HaveCount(3);
        model.InitialState!.Name.Should().Be("A");
    }

    [Fact]
    public void Parse_MultiState_LastInitial_Succeeds()
    {
        const string dsl = """
            precept Test
            state A, B, C initial
            """;

        var model = PreceptParser.Parse(dsl);

        model.States.Should().HaveCount(3);
        model.InitialState!.Name.Should().Be("C");
    }

    [Fact]
    public void Parse_MultiState_DuplicateFails()
    {
        const string dsl = """
            precept Test
            state A initial, B, A
            """;

        var act = () => PreceptParser.Parse(dsl);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Duplicate state*");
    }

    [Fact]
    public void Parse_MultiState_CrossLineDuplicateFails()
    {
        const string dsl = """
            precept Test
            state A initial
            state B, A
            """;

        var act = () => PreceptParser.Parse(dsl);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Duplicate state*");
    }

    [Fact]
    public void Parse_MultiState_TwoInitialsFails()
    {
        const string dsl = """
            precept Test
            state A initial, B initial
            """;

        var act = () => PreceptParser.Parse(dsl);
        act.Should().Throw<InvalidOperationException>().WithMessage("*initial*");
    }

    [Fact]
    public void Parse_MultiState_CrossLineInitialFails()
    {
        const string dsl = """
            precept Test
            state A initial
            state B initial, C
            """;

        var act = () => PreceptParser.Parse(dsl);
        act.Should().Throw<InvalidOperationException>().WithMessage("*initial*");
    }

    // ════════════════════════════════════════════════════════════════════
    // PARSING — Multi-name event declarations
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_MultiEvent_Succeeds()
    {
        const string dsl = """
            precept Test
            state Idle initial
            event Foo, Bar, Baz
            """;

        var model = PreceptParser.Parse(dsl);

        model.Events.Should().HaveCount(3);
        model.Events[0].Name.Should().Be("Foo");
        model.Events[1].Name.Should().Be("Bar");
        model.Events[2].Name.Should().Be("Baz");
        model.Events[0].Args.Should().BeEmpty();
        model.Events[1].Args.Should().BeEmpty();
        model.Events[2].Args.Should().BeEmpty();
    }

    [Fact]
    public void Parse_MultiEvent_WithSharedArgs_Succeeds()
    {
        const string dsl = """
            precept Test
            state Idle initial
            event Approve, Reject with Note as string
            """;

        var model = PreceptParser.Parse(dsl);

        model.Events.Should().HaveCount(2);
        model.Events[0].Name.Should().Be("Approve");
        model.Events[1].Name.Should().Be("Reject");
        model.Events[0].Args.Should().HaveCount(1);
        model.Events[0].Args[0].Name.Should().Be("Note");
        model.Events[1].Args.Should().HaveCount(1);
        model.Events[1].Args[0].Name.Should().Be("Note");
    }

    [Fact]
    public void Parse_MultiEvent_DuplicateFails()
    {
        const string dsl = """
            precept Test
            state Idle initial
            event A, B, A
            """;

        var act = () => PreceptParser.Parse(dsl);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Duplicate event*");
    }

    [Fact]
    public void Parse_MultiEvent_CrossLineDuplicateFails()
    {
        const string dsl = """
            precept Test
            state Idle initial
            event A
            event B, A
            """;

        var act = () => PreceptParser.Parse(dsl);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Duplicate event*");
    }

    [Fact]
    public void Parse_MixedSingleAndMulti_Succeeds()
    {
        const string dsl = """
            precept Test
            state Draft initial
            state UnderReview, Approved, Declined
            event Submit with Amount as number
            event Approve, Reject with Note as string
            event Cancel
            """;

        var model = PreceptParser.Parse(dsl);

        model.States.Should().HaveCount(4);
        model.Events.Should().HaveCount(4);
        model.Events.Select(e => e.Name).Should().BeEquivalentTo("Submit", "Approve", "Reject", "Cancel");
    }

    [Fact]
    public void Parse_SourceColumn_MultiState()
    {
        const string dsl = """
            precept Test
            state AlphaState initial, BetaState, GammaState
            """;

        var model = PreceptParser.Parse(dsl);

        model.States.Should().HaveCount(3);
        // All states share the same SourceLine
        model.States[0].SourceLine.Should().Be(model.States[1].SourceLine);
        model.States[1].SourceLine.Should().Be(model.States[2].SourceLine);
        // Each state has a distinct SourceColumn matching its position
        model.States[0].SourceColumn.Should().BeGreaterThan(0);
        model.States[1].SourceColumn.Should().BeGreaterThan(model.States[0].SourceColumn);
        model.States[2].SourceColumn.Should().BeGreaterThan(model.States[1].SourceColumn);
    }

    [Fact]
    public void Parse_SourceColumn_MultiEvent()
    {
        const string dsl = """
            precept Test
            state Idle initial
            event AlphaEvent, BetaEvent, GammaEvent
            """;

        var model = PreceptParser.Parse(dsl);

        model.Events.Should().HaveCount(3);
        // All events share the same SourceLine
        model.Events[0].SourceLine.Should().Be(model.Events[1].SourceLine);
        model.Events[1].SourceLine.Should().Be(model.Events[2].SourceLine);
        // Each event has a distinct SourceColumn matching its position
        model.Events[0].SourceColumn.Should().BeGreaterThan(0);
        model.Events[1].SourceColumn.Should().BeGreaterThan(model.Events[0].SourceColumn);
        model.Events[2].SourceColumn.Should().BeGreaterThan(model.Events[1].SourceColumn);
    }

    // ════════════════════════════════════════════════════════════════════
    // PARSING — Multi-name field declarations
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_MultiField_Succeeds()
    {
        const string dsl = """
            precept Test
            state Idle initial
            field A, B, C as number default 0
            """;

        var model = PreceptParser.Parse(dsl);

        model.Fields.Should().HaveCount(3);
        model.Fields[0].Name.Should().Be("A");
        model.Fields[1].Name.Should().Be("B");
        model.Fields[2].Name.Should().Be("C");
        model.Fields[0].Type.Should().Be(PreceptScalarType.Number);
        model.Fields[1].Type.Should().Be(PreceptScalarType.Number);
        model.Fields[2].Type.Should().Be(PreceptScalarType.Number);
        model.Fields[0].DefaultValue.Should().Be(0m);
        model.Fields[1].DefaultValue.Should().Be(0m);
        model.Fields[2].DefaultValue.Should().Be(0m);
    }

    [Fact]
    public void Parse_MultiField_Nullable_Succeeds()
    {
        const string dsl = """
            precept Test
            state Idle initial
            field A, B as string nullable
            """;

        var model = PreceptParser.Parse(dsl);

        model.Fields.Should().HaveCount(2);
        model.Fields[0].Name.Should().Be("A");
        model.Fields[1].Name.Should().Be("B");
        model.Fields[0].IsNullable.Should().BeTrue();
        model.Fields[1].IsNullable.Should().BeTrue();
        model.Fields[0].DefaultValue.Should().BeNull();
        model.Fields[1].DefaultValue.Should().BeNull();
    }

    [Fact]
    public void Parse_MultiField_DuplicateFails()
    {
        const string dsl = """
            precept Test
            state Idle initial
            field A, B, A as number default 0
            """;

        var act = () => PreceptParser.Parse(dsl);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Duplicate field*");
    }

    [Fact]
    public void Parse_MultiField_CrossLineDuplicateFails()
    {
        const string dsl = """
            precept Test
            state Idle initial
            field A as number default 0
            field B, A as number default 1
            """;

        var act = () => PreceptParser.Parse(dsl);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Duplicate field*");
    }

    [Fact]
    public void Parse_MultiCollectionField_Succeeds()
    {
        const string dsl = """
            precept Test
            state Idle initial
            field A, B as set of string
            """;

        var model = PreceptParser.Parse(dsl);

        model.CollectionFields.Should().HaveCount(2);
        model.CollectionFields[0].Name.Should().Be("A");
        model.CollectionFields[1].Name.Should().Be("B");
        model.CollectionFields[0].CollectionKind.Should().Be(PreceptCollectionKind.Set);
        model.CollectionFields[1].CollectionKind.Should().Be(PreceptCollectionKind.Set);
        model.CollectionFields[0].InnerType.Should().Be(PreceptScalarType.String);
        model.CollectionFields[1].InnerType.Should().Be(PreceptScalarType.String);
    }

    [Fact]
    public void Parse_MultiCollectionField_WithDefault_Succeeds()
    {
        const string dsl = """
            precept Test
            state Idle initial
            field A, B as set of string default ["x"]
            """;

        var model = PreceptParser.Parse(dsl);

        model.CollectionFields.Should().HaveCount(2);
        model.CollectionFields[0].Name.Should().Be("A");
        model.CollectionFields[1].Name.Should().Be("B");
    }

    [Fact]
    public void Parse_MixedSingleAndMultiField_Succeeds()
    {
        const string dsl = """
            precept Test
            state Idle initial
            field Solo as boolean default false
            field X, Y as number default 0
            field Tags as set of string
            field P, Q as queue of number
            """;

        var model = PreceptParser.Parse(dsl);

        model.Fields.Should().HaveCount(3);
        model.Fields.Select(f => f.Name).Should().BeEquivalentTo("Solo", "X", "Y");
        model.CollectionFields.Should().HaveCount(3);
        model.CollectionFields.Select(f => f.Name).Should().BeEquivalentTo("Tags", "P", "Q");
    }

    [Fact]
    public void Parse_MultiField_ScalarAndCollectionDuplicateFails()
    {
        const string dsl = """
            precept Test
            state Idle initial
            field A as number default 0
            field B, A as set of string
            """;

        var act = () => PreceptParser.Parse(dsl);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Duplicate field*");
    }

    // ════════════════════════════════════════════════════════════════════
    // PARSING — Undeclared state references in transition rows (C54)
    // ════════════════════════════════════════════════════════════════════

    // ════════════════════════════════════════════════════════════════════
    // PARSING — Conditional when guards (Issue #14 Slice 9)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_ConditionalRule_WhenGuardParsed()
    {
        const string dsl = """
            precept Test
            field X as number default 0
            field Active as boolean default false
            state A initial
            rule X >= 0 when Active because "Only when active"
            """;

        var model = PreceptParser.Parse(dsl);

        model.Rules.Should().HaveCount(1);
        var inv = model.Rules![0];
        inv.WhenGuard.Should().NotBeNull();
        inv.WhenText.Should().Be("Active");
        inv.Reason.Should().Be("Only when active");
    }

    [Fact]
    public void Parse_ConditionalStateEnsure_WhenGuardParsed()
    {
        const string dsl = """
            precept Test
            field X as number default 0
            field Active as boolean default false
            state Open initial
            in Open ensure X > 0 when Active because "Only when active"
            """;

        var model = PreceptParser.Parse(dsl);

        model.StateEnsures.Should().HaveCount(1);
        var sa = model.StateEnsures![0];
        sa.WhenGuard.Should().NotBeNull();
        sa.WhenText.Should().Be("Active");
        sa.Reason.Should().Be("Only when active");
    }

    [Fact]
    public void Parse_ConditionalEventEnsure_WhenGuardParsed()
    {
        const string dsl = """
            precept Test
            state Active initial
            event Submit with Amount as number, Priority as number
            on Submit ensure Amount > 0 when Priority > 1 because "High priority needs amount"
            """;

        var model = PreceptParser.Parse(dsl);

        model.EventEnsures.Should().HaveCount(1);
        var ea = model.EventEnsures![0];
        ea.WhenGuard.Should().NotBeNull();
        ea.WhenText.Should().Be("Priority > 1");
        ea.Reason.Should().Be("High priority needs amount");
    }

    [Fact]
    public void Parse_ConditionalEdit_WhenGuardParsed()
    {
        const string dsl = """
            precept Test
            field Priority as number default 0
            field Active as boolean default false
            state Open initial
            in Open when Active edit Priority
            """;

        var model = PreceptParser.Parse(dsl);

        model.EditBlocks.Should().HaveCount(1);
        var eb = model.EditBlocks![0];
        eb.State.Should().Be("Open");
        eb.WhenGuard.Should().NotBeNull();
        eb.WhenText.Should().Be("Active");
        eb.FieldNames.Should().Contain("Priority");
    }

    [Fact]
    public void Parse_ConditionalRule_WhenNotGuard()
    {
        const string dsl = """
            precept Test
            field X as number default 0
            field Active as boolean default false
            state A initial
            rule X >= 0 when not Active because "When not active"
            """;

        var model = PreceptParser.Parse(dsl);

        var inv = model.Rules![0];
        inv.WhenGuard.Should().NotBeNull();
        inv.WhenGuard.Should().BeOfType<PreceptUnaryExpression>()
            .Which.Operator.Should().Be("not");
    }

    [Fact]
    public void Parse_RuleWithoutWhen_GuardIsNull()
    {
        const string dsl = """
            precept Test
            field X as number default 0
            state A initial
            rule X >= 0 because "Always"
            """;

        var model = PreceptParser.Parse(dsl);

        model.Rules.Should().HaveCount(1);
        model.Rules![0].WhenGuard.Should().BeNull();
        model.Rules![0].WhenText.Should().BeNull();
    }

    [Fact]
    public void Parse_ConditionalEdit_InAny_ExpandsToAllStates()
    {
        const string dsl = """
            precept Test
            field Priority as number default 0
            field Active as boolean default false
            state A initial
            state B
            in any when Active edit Priority
            """;

        var model = PreceptParser.Parse(dsl);

        model.EditBlocks.Should().HaveCount(2);
        model.EditBlocks!.Select(e => e.State).Should().BeEquivalentTo("A", "B");
        model.EditBlocks![0].WhenGuard.Should().NotBeNull();
        model.EditBlocks![1].WhenGuard.Should().NotBeNull();
        model.EditBlocks![0].WhenText.Should().Be("Active");
        model.EditBlocks![1].WhenText.Should().Be("Active");
    }

    [Fact]
    public void Parse_EditWithoutWhen_GuardIsNull()
    {
        const string dsl = """
            precept Test
            field Priority as number default 0
            state Open initial
            in Open edit Priority
            """;

        var model = PreceptParser.Parse(dsl);

        model.EditBlocks.Should().HaveCount(1);
        model.EditBlocks![0].WhenGuard.Should().BeNull();
        model.EditBlocks![0].WhenText.Should().BeNull();
    }

    // ════════════════════════════════════════════════════════════════════
    // PARSING — Undeclared state references in transition rows (C54)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_TransitionTargetUndeclaredState_Throws()
    {
        const string dsl = """
            precept Test
            state Open initial
            event Go
            from Open on Go -> transition Nowhere
            """;

        var act = () => PreceptParser.Parse(dsl);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Undeclared state 'Nowhere'*");
    }

    [Fact]
    public void Parse_TransitionSourceUndeclaredState_Throws()
    {
        const string dsl = """
            precept Test
            state Open initial
            state Closed
            event Go
            from Bogus on Go -> transition Closed
            """;

        var act = () => PreceptParser.Parse(dsl);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Undeclared state 'Bogus'*");
    }

    [Fact]
    public void ParseWithDiagnostics_UndeclaredTargetState_ReturnsDiagnosticAndNullModel()
    {
        const string dsl = """
            precept Test
            state Open initial
            event Go
            from Open on Go -> transition Nowhere
            """;

        var (model, diagnostics) = PreceptParser.ParseWithDiagnostics(dsl);

        model.Should().BeNull();
        diagnostics.Should().NotBeEmpty();
        diagnostics[0].Message.Should().Contain("Undeclared state 'Nowhere'");
    }

    // ════════════════════════════════════════════════════════════════════
    // PARSING — Conditional state ensures with anchor-specific when guards
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_ConditionalStateEnsure_To_WhenGuardParsed()
    {
        const string dsl = """
            precept Test
            field Amount as number default 0
            field Active as boolean default false
            state Draft initial
            state Approved
            event Approve
            to Approved ensure Amount > 0 when Active because "Amount required when active"
            from Draft on Approve -> transition Approved
            """;

        var model = PreceptParser.Parse(dsl);

        model.StateEnsures.Should().HaveCount(1);
        var sa = model.StateEnsures![0];
        sa.Anchor.Should().Be(EnsureAnchor.To);
        sa.State.Should().Be("Approved");
        sa.WhenGuard.Should().NotBeNull();
        sa.WhenText.Should().NotBeNull();
        sa.Reason.Should().Be("Amount required when active");
    }

    [Fact]
    public void Parse_ConditionalStateEnsure_From_WhenGuardParsed()
    {
        const string dsl = """
            precept Test
            field Notes as string nullable
            field RequiresNotes as boolean default false
            state Draft initial
            state Review
            event Submit
            from Draft ensure Notes != null when RequiresNotes because "Notes required before leaving Draft"
            from Draft on Submit -> transition Review
            """;

        var model = PreceptParser.Parse(dsl);

        model.StateEnsures.Should().HaveCount(1);
        var sa = model.StateEnsures![0];
        sa.Anchor.Should().Be(EnsureAnchor.From);
        sa.State.Should().Be("Draft");
        sa.WhenGuard.Should().NotBeNull();
        sa.WhenText.Should().NotBeNull();
        sa.Reason.Should().Be("Notes required before leaving Draft");
    }

    // ════════════════════════════════════════════════════════════════════
    // PARSING — Any-order field modifiers
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_FieldModifiers_DefaultBeforeNullable()
    {
        const string dsl = """
            precept Test
            field Name as string default "unknown" nullable
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);
        var f = model.Fields[0];

        f.Name.Should().Be("Name");
        f.IsNullable.Should().BeTrue();
        f.HasDefaultValue.Should().BeTrue();
        f.DefaultValue.Should().Be("unknown");
    }

    [Fact]
    public void Parse_FieldModifiers_ConstraintBeforeDefault()
    {
        const string dsl = """
            precept Test
            field Amount as number nonnegative default 5
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);
        var f = model.Fields[0];

        f.HasDefaultValue.Should().BeTrue();
        f.DefaultValue.Should().Be(5.0);
        f.Constraints.Should().Contain(c => c is FieldConstraint.Nonnegative);
    }

    [Fact]
    public void Parse_FieldModifiers_ConstraintBeforeNullable()
    {
        const string dsl = """
            precept Test
            field Notes as string maxlength 500 nullable
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);
        var f = model.Fields[0];

        f.IsNullable.Should().BeTrue();
        f.Constraints.OfType<FieldConstraint.Maxlength>().Should().ContainSingle().Which.Value.Should().Be(500);
    }

    [Fact]
    public void Parse_FieldModifiers_ConstraintThenDefaultThenNullable()
    {
        const string dsl = """
            precept Test
            field Score as number min 0 default 10 nullable
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);
        var f = model.Fields[0];

        f.IsNullable.Should().BeTrue();
        f.HasDefaultValue.Should().BeTrue();
        f.DefaultValue.Should().Be(10.0);
        f.Constraints.OfType<FieldConstraint.Min>().Should().ContainSingle().Which.Value.Should().Be(0);
    }

    [Fact]
    public void Parse_FieldModifiers_NullableBeforeConstraint()
    {
        const string dsl = """
            precept Test
            field Label as string nullable notempty
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);
        var f = model.Fields[0];

        f.IsNullable.Should().BeTrue();
        f.Constraints.Should().Contain(c => c is FieldConstraint.Notempty);
    }

    [Fact]
    public void Parse_FieldModifiers_DefaultBetweenConstraints()
    {
        const string dsl = """
            precept Test
            field Amount as number min 0 default 50 max 100
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);
        var f = model.Fields[0];

        f.HasDefaultValue.Should().BeTrue();
        f.DefaultValue.Should().Be(50.0);
        f.Constraints.Should().HaveCount(2);
        f.Constraints.OfType<FieldConstraint.Min>().Should().ContainSingle().Which.Value.Should().Be(0);
        f.Constraints.OfType<FieldConstraint.Max>().Should().ContainSingle().Which.Value.Should().Be(100);
    }

    [Fact]
    public void Parse_FieldModifiers_MultipleConstraintsThenNullableThenDefault()
    {
        const string dsl = """
            precept Test
            field Bio as string notempty maxlength 1000 nullable default "N/A"
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);
        var f = model.Fields[0];

        f.IsNullable.Should().BeTrue();
        f.HasDefaultValue.Should().BeTrue();
        f.DefaultValue.Should().Be("N/A");
        f.Constraints.Should().HaveCount(2);
        f.Constraints.Should().Contain(c => c is FieldConstraint.Notempty);
        f.Constraints.Should().Contain(c => c is FieldConstraint.Maxlength);
    }

    [Fact]
    public void Parse_FieldModifiers_ChoiceOrderedBeforeDefault()
    {
        const string dsl = """
            precept Test
            field Priority as choice("Low", "Medium", "High") ordered default "Low"
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);
        var f = model.Fields[0];

        f.HasDefaultValue.Should().BeTrue();
        f.DefaultValue.Should().Be("Low");
        f.IsOrdered.Should().BeTrue();
    }

    [Fact]
    public void Parse_FieldModifiers_DecimalMaxplacesBeforeDefault()
    {
        const string dsl = """
            precept Test
            field Price as decimal maxplaces 2 default 0.00
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);
        var f = model.Fields[0];

        f.HasDefaultValue.Should().BeTrue();
        f.DefaultValue.Should().Be(0.00);
        f.Constraints.OfType<FieldConstraint.Maxplaces>().Should().ContainSingle().Which.Places.Should().Be(2);
    }

    [Fact]
    public void Parse_FieldModifiers_MultiNameWithNonStandardOrder()
    {
        const string dsl = """
            precept Test
            field X, Y as number nonnegative default 0
            state A initial
            """;

        var model = PreceptParser.Parse(dsl);

        model.Fields.Should().HaveCount(2);
        foreach (var f in model.Fields)
        {
            f.HasDefaultValue.Should().BeTrue();
            f.DefaultValue.Should().Be(0.0);
            f.Constraints.Should().Contain(c => c is FieldConstraint.Nonnegative);
        }
    }

    // ════════════════════════════════════════════════════════════════════
    // PARSING — Any-order event argument modifiers
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_EventArgModifiers_DefaultBeforeNullable()
    {
        const string dsl = """
            precept Test
            state A initial
            event Submit with Name as string default "" nullable
            from A on Submit -> no transition
            """;

        var model = PreceptParser.Parse(dsl);
        var arg = model.Events[0].Args[0];

        arg.IsNullable.Should().BeTrue();
        arg.HasDefaultValue.Should().BeTrue();
        arg.DefaultValue.Should().Be("");
    }

    [Fact]
    public void Parse_EventArgModifiers_ConstraintBeforeDefault()
    {
        const string dsl = """
            precept Test
            state A initial
            event Deposit with Amount as number nonnegative default 0
            from A on Deposit -> no transition
            """;

        var model = PreceptParser.Parse(dsl);
        var arg = model.Events[0].Args[0];

        arg.HasDefaultValue.Should().BeTrue();
        arg.DefaultValue.Should().Be(0.0);
        arg.Constraints.Should().Contain(c => c is FieldConstraint.Nonnegative);
    }

    [Fact]
    public void Parse_EventArgModifiers_NullableAfterConstraint()
    {
        const string dsl = """
            precept Test
            state A initial
            event Update with Label as string notempty nullable
            from A on Update -> no transition
            """;

        var model = PreceptParser.Parse(dsl);
        var arg = model.Events[0].Args[0];

        arg.IsNullable.Should().BeTrue();
        arg.Constraints.Should().Contain(c => c is FieldConstraint.Notempty);
    }

    [Fact]
    public void Parse_EventArgModifiers_MultiArgsMixedOrder()
    {
        const string dsl = """
            precept Test
            state A initial
            event Submit with Amount as number min 0 default 10, Label as string nullable notempty
            from A on Submit -> no transition
            """;

        var model = PreceptParser.Parse(dsl);

        var amount = model.Events[0].Args.Single(a => a.Name == "Amount");
        amount.HasDefaultValue.Should().BeTrue();
        amount.DefaultValue.Should().Be(10.0);
        amount.Constraints.Should().Contain(c => c is FieldConstraint.Min);

        var label = model.Events[0].Args.Single(a => a.Name == "Label");
        label.IsNullable.Should().BeTrue();
        label.Constraints.Should().Contain(c => c is FieldConstraint.Notempty);
    }

    // ════════════════════════════════════════════════════════════════════
    // PARSING — C70: Duplicate modifier detection
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void ParseWithDiagnostics_DuplicateDefault_ProducesC70()
    {
        const string dsl = """
            precept Test
            field X as number default 0 default 1
            state A initial
            """;

        var (model, diagnostics) = PreceptParser.ParseWithDiagnostics(dsl);

        model.Should().BeNull();
        diagnostics.Should().ContainSingle(d => d.Code == "PRECEPT070");
    }

    [Fact]
    public void ParseWithDiagnostics_DuplicateNullable_ProducesC70()
    {
        const string dsl = """
            precept Test
            field X as string nullable nullable
            state A initial
            """;

        var (model, diagnostics) = PreceptParser.ParseWithDiagnostics(dsl);

        model.Should().BeNull();
        diagnostics.Should().ContainSingle(d => d.Code == "PRECEPT070");
    }

    [Fact]
    public void ParseWithDiagnostics_DuplicateEventArgModifier_ProducesC70()
    {
        const string dsl = """
            precept Test
            state A initial
            event Go with X as number default 0 default 1
            from A on Go -> no transition
            """;

        var (model, diagnostics) = PreceptParser.ParseWithDiagnostics(dsl);

        model.Should().BeNull();
        diagnostics.Should().ContainSingle(d => d.Code == "PRECEPT070");
    }

    [Fact]
    public void ParseWithDiagnostics_DuplicateOrdered_ProducesC70()
    {
        const string dsl = """
            precept Test
            field Priority as choice("Low", "Medium", "High") ordered ordered
            state A initial
            """;

        var (model, diagnostics) = PreceptParser.ParseWithDiagnostics(dsl);

        model.Should().BeNull();
        diagnostics.Should().ContainSingle(d => d.Code == "PRECEPT070");
    }
}
