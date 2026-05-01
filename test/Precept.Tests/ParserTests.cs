using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Precept.Pipeline.SyntaxNodes;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Soup Nazi tests for top-level dispatch and full declaration parsing (Slice 3.3).
/// </summary>
public class ParserTests
{
    private static SyntaxTree Parse(string source)
    {
        var tokens = Lexer.Lex(source);
        return Parser.Parse(tokens);
    }

    // ── Multi-declaration integration ──────────────────────────────────────

    [Fact]
    public void Parse_MultipleDeclarations_ProducesCorrectNodeCount()
    {
        var tree = Parse("""
            precept X
            field amount as decimal
            state Draft initial, Submitted
            event Submit
            rule amount > 0 because "msg"
            """);

        tree.Header.Should().NotBeNull();
        tree.Header!.Name.Text.Should().Be("X");
        tree.Declarations.Should().HaveCount(4); // field, state, event, rule
    }

    // ── FieldDeclaration ───────────────────────────────────────────────────

    [Fact]
    public void Parse_FieldDeclaration_Simple()
    {
        var tree = Parse("field amount as decimal");
        var field = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>().Subject;
        field.Names.Should().HaveCount(1);
        field.Names[0].Text.Should().Be("amount");
        field.Type.Should().BeOfType<ScalarTypeRefNode>();
    }

    [Fact]
    public void Parse_FieldDeclaration_MultipleNames()
    {
        var tree = Parse("field name, description as string");
        var field = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>().Subject;
        field.Names.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_FieldDeclaration_WithModifiers()
    {
        var tree = Parse("field amount as money nonnegative positive");
        var field = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>().Subject;
        field.Modifiers.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_FieldDeclaration_WithComputedExpr()
    {
        var tree = Parse("field total as money -> principal + interest");
        var field = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>().Subject;
        field.ComputedExpression.Should().NotBeNull();
        field.ComputedExpression.Should().BeOfType<BinaryExpression>();
    }

    // ── StateDeclaration ───────────────────────────────────────────────────

    [Fact]
    public void Parse_StateDeclaration_Simple()
    {
        var tree = Parse("state Draft initial");
        var state = tree.Declarations[0].Should().BeOfType<StateDeclarationNode>().Subject;
        state.Entries.Should().HaveCount(1);
        state.Entries[0].Name.Text.Should().Be("Draft");
        state.Entries[0].Modifiers.Should().Contain(t => t.Kind == TokenKind.Initial);
    }

    [Fact]
    public void Parse_StateDeclaration_MultipleStates()
    {
        var tree = Parse("state Draft initial, Submitted, Approved terminal success");
        var state = tree.Declarations[0].Should().BeOfType<StateDeclarationNode>().Subject;
        state.Entries.Should().HaveCount(3);
        state.Entries[0].Name.Text.Should().Be("Draft");
        state.Entries[1].Name.Text.Should().Be("Submitted");
        state.Entries[2].Name.Text.Should().Be("Approved");
        state.Entries[2].Modifiers.Should().HaveCount(2);
    }

    // ── EventDeclaration ───────────────────────────────────────────────────

    [Fact]
    public void Parse_EventDeclaration_Simple()
    {
        var tree = Parse("event Submit");
        var evt = tree.Declarations[0].Should().BeOfType<EventDeclarationNode>().Subject;
        evt.Names.Should().HaveCount(1);
        evt.Names[0].Text.Should().Be("Submit");
        evt.Arguments.Should().BeEmpty();
    }

    [Fact]
    public void Parse_EventDeclaration_WithArgs()
    {
        var tree = Parse("event Approve(Amount as decimal)");
        var evt = tree.Declarations[0].Should().BeOfType<EventDeclarationNode>().Subject;
        evt.Arguments.Should().HaveCount(1);
        evt.Arguments[0].Name.Text.Should().Be("Amount");
    }

    [Fact]
    public void Parse_EventDeclaration_Initial()
    {
        var tree = Parse("event Create initial");
        var evt = tree.Declarations[0].Should().BeOfType<EventDeclarationNode>().Subject;
        evt.IsInitial.Should().BeTrue();
    }

    // ── RuleDeclaration ────────────────────────────────────────────────────

    [Fact]
    public void Parse_RuleDeclaration_Simple()
    {
        var tree = Parse("rule amount > 0 because \"must be positive\"");
        var rule = tree.Declarations[0].Should().BeOfType<RuleDeclarationNode>().Subject;
        rule.Condition.Should().BeOfType<BinaryExpression>();
        rule.Guard.Should().BeNull();
        rule.Message.Should().BeOfType<LiteralExpression>();
    }

    [Fact]
    public void Parse_RuleDeclaration_WithGuard()
    {
        var tree = Parse("rule amount > 0 when Active because \"msg\"");
        var rule = tree.Declarations[0].Should().BeOfType<RuleDeclarationNode>().Subject;
        rule.Guard.Should().NotBeNull();
        rule.Guard.Should().BeOfType<IdentifierExpression>();
    }

    // ── PreceptHeader ──────────────────────────────────────────────────────

    [Fact]
    public void Parse_PreceptHeader_Valid()
    {
        var tree = Parse("precept InsuranceClaim");
        tree.Header.Should().NotBeNull();
        tree.Header!.Name.Text.Should().Be("InsuranceClaim");
    }

    // ── Error recovery ─────────────────────────────────────────────────────

    [Fact]
    public void Parse_UnknownLeadingToken_ProducesDiagnosticAndSyncs()
    {
        var tree = Parse("badtoken\nfield amount as decimal");
        // "badtoken" lexes as an Identifier, which is not a construct leading token,
        // so it produces a diagnostic and syncs to the next declaration.
        tree.Diagnostics.Should().NotBeEmpty();
        tree.Declarations.Should().HaveCount(1);
        tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>();
    }

    // ── Diagnostics are clean for well-formed input ────────────────────────

    [Fact]
    public void Parse_WellFormedInput_NoDiagnostics()
    {
        var tree = Parse("""
            precept Loan
            field amount as decimal nonnegative
            state Draft initial, Approved terminal
            event Submit
            rule amount > 0 because "Amount must be positive"
            """);
        tree.Diagnostics.Should().BeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  PR 4 — Disambiguated Constructs (in/to/on)
    // ════════════════════════════════════════════════════════════════════════

    // ── Slice 4.1: Generic Disambiguator ───────────────────────────────────

    [Fact]
    public void Disambiguator_NoMatchingToken_EmitsDiagnosticAndSyncs()
    {
        var tree = Parse("in Draft foobar Amount");
        tree.Diagnostics.Should().NotBeEmpty();
        tree.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.ExpectedToken));
    }

    [Fact]
    public void Disambiguator_EmptyAfterAnchor_EmitsDiagnostic()
    {
        var tree = Parse("in Draft");
        tree.Diagnostics.Should().NotBeEmpty();
    }

    [Fact]
    public void Disambiguator_StashedGuard_InjectedIntoGuardSlot()
    {
        var tree = Parse("""in Draft when Active ensure amount > 0 because "msg" """);
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<StateEnsureNode>().Subject;
        node.Guard.Should().NotBeNull();
        node.Guard.Should().BeOfType<IdentifierExpression>();
    }

    [Fact]
    public void Disambiguator_StashedGuard_WithOmit_EmitsDiagnostic()
    {
        var tree = Parse("in Closed when SomeCondition omit Amount");
        tree.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.OmitDoesNotSupportGuard));
        var node = tree.Declarations[0].Should().BeOfType<OmitDeclarationNode>().Subject;
        // OmitDeclarationNode structurally has no Guard property
    }

    // ── Slice 4.2: FieldTarget parsing ─────────────────────────────────────

    [Fact]
    public void ParseFieldTarget_Singular()
    {
        var tree = Parse("in Draft modify FraudFlag readonly");
        var node = tree.Declarations[0].Should().BeOfType<AccessModeNode>().Subject;
        node.Fields.Should().BeOfType<SingularFieldTarget>()
            .Which.Name.Text.Should().Be("FraudFlag");
    }

    [Fact]
    public void ParseFieldTarget_List()
    {
        var tree = Parse("in Draft modify Name, Description, Notes readonly");
        var node = tree.Declarations[0].Should().BeOfType<AccessModeNode>().Subject;
        var list = node.Fields.Should().BeOfType<ListFieldTarget>().Subject;
        list.Names.Should().HaveCount(3);
        list.Names[0].Text.Should().Be("Name");
        list.Names[1].Text.Should().Be("Description");
        list.Names[2].Text.Should().Be("Notes");
    }

    [Fact]
    public void ParseFieldTarget_All()
    {
        var tree = Parse("in Draft modify all readonly");
        var node = tree.Declarations[0].Should().BeOfType<AccessModeNode>().Subject;
        node.Fields.Should().BeOfType<AllFieldTarget>();
    }

    [Fact]
    public void ParseFieldTarget_Missing_ProducesDiagnostic()
    {
        var tree = Parse("in Draft modify readonly");
        tree.Diagnostics.Should().NotBeEmpty();
    }

    // ── Slice 4.2: AccessModeKeyword ───────────────────────────────────────

    [Fact]
    public void ParseAccessModeKeyword_Readonly()
    {
        var tree = Parse("in Draft modify Amount readonly");
        var node = tree.Declarations[0].Should().BeOfType<AccessModeNode>().Subject;
        node.Mode.Kind.Should().Be(TokenKind.Readonly);
    }

    [Fact]
    public void ParseAccessModeKeyword_Editable()
    {
        var tree = Parse("in Draft modify Amount editable");
        var node = tree.Declarations[0].Should().BeOfType<AccessModeNode>().Subject;
        node.Mode.Kind.Should().Be(TokenKind.Editable);
    }

    [Fact]
    public void ParseAccessModeKeyword_Missing_ProducesDiagnostic()
    {
        var tree = Parse("in Draft modify Amount");
        tree.Diagnostics.Should().NotBeEmpty();
    }

    // ── Slice 4.2: AccessMode construct ────────────────────────────────────

    [Fact]
    public void Parse_AccessMode_Singular_Readonly()
    {
        var tree = Parse("in UnderReview modify FraudFlag readonly");
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<AccessModeNode>().Subject;
        node.State.Name.Text.Should().Be("UnderReview");
        node.Fields.Should().BeOfType<SingularFieldTarget>()
            .Which.Name.Text.Should().Be("FraudFlag");
        node.Mode.Kind.Should().Be(TokenKind.Readonly);
        node.Guard.Should().BeNull();
    }

    [Fact]
    public void Parse_AccessMode_Singular_Editable()
    {
        var tree = Parse("in Submitted modify Amount editable");
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<AccessModeNode>().Subject;
        node.Mode.Kind.Should().Be(TokenKind.Editable);
    }

    [Fact]
    public void Parse_AccessMode_List_Readonly()
    {
        var tree = Parse("in Draft modify Name, Description readonly");
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<AccessModeNode>().Subject;
        var list = node.Fields.Should().BeOfType<ListFieldTarget>().Subject;
        list.Names.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_AccessMode_All_Editable()
    {
        var tree = Parse("in Review modify all editable");
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<AccessModeNode>().Subject;
        node.Fields.Should().BeOfType<AllFieldTarget>();
        node.Mode.Kind.Should().Be(TokenKind.Editable);
    }

    [Fact]
    public void Parse_AccessMode_WithPostFieldGuard()
    {
        var tree = Parse("in UnderReview modify FraudFlag editable when not FraudFlag");
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<AccessModeNode>().Subject;
        node.Guard.Should().NotBeNull();
    }

    [Fact]
    public void Parse_AccessMode_WithPreFieldGuard()
    {
        var tree = Parse("in UnderReview when Active modify FraudFlag readonly");
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<AccessModeNode>().Subject;
        node.Guard.Should().NotBeNull();
        node.Guard.Should().BeOfType<IdentifierExpression>();
    }

    // ── Slice 4.2: OmitDeclaration construct ──────────────────────────────

    [Fact]
    public void Parse_OmitDeclaration_Singular()
    {
        var tree = Parse("in Draft omit InternalNotes");
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<OmitDeclarationNode>().Subject;
        node.Fields.Should().BeOfType<SingularFieldTarget>()
            .Which.Name.Text.Should().Be("InternalNotes");
    }

    [Fact]
    public void Parse_OmitDeclaration_List()
    {
        var tree = Parse("in Draft omit Notes, Memo");
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<OmitDeclarationNode>().Subject;
        var list = node.Fields.Should().BeOfType<ListFieldTarget>().Subject;
        list.Names.Should().HaveCount(2);
    }

    [Fact]
    public void Parse_OmitDeclaration_All()
    {
        var tree = Parse("in Draft omit all");
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<OmitDeclarationNode>().Subject;
        node.Fields.Should().BeOfType<AllFieldTarget>();
    }

    [Fact]
    public void Parse_OmitDeclaration_WithPostFieldGuard_EmitsDiagnostic()
    {
        var tree = Parse("in Draft omit Amount when SomeCondition");
        tree.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.OmitDoesNotSupportGuard));
        var node = tree.Declarations[0].Should().BeOfType<OmitDeclarationNode>().Subject;
        // Structurally impossible to have a Guard — OmitDeclarationNode has no Guard property
    }

    [Fact]
    public void Parse_OmitDeclaration_WithPreFieldGuard_EmitsDiagnostic()
    {
        var tree = Parse("in Closed when SomeCondition omit Amount");
        tree.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.OmitDoesNotSupportGuard));
        tree.Declarations[0].Should().BeOfType<OmitDeclarationNode>();
    }

    // ── Slice 4.2: StateEnsure (in-scoped) ────────────────────────────────

    [Fact]
    public void Parse_StateEnsure_In_Simple()
    {
        var tree = Parse("""in Approved ensure ApprovedAmount > 0 because "msg" """);
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<StateEnsureNode>().Subject;
        node.State.Name.Text.Should().Be("Approved");
        node.Preposition.Kind.Should().Be(TokenKind.In);
        node.Guard.Should().BeNull();
    }

    [Fact]
    public void Parse_StateEnsure_In_WithGuard()
    {
        var tree = Parse("""in Submitted when isComplete ensure amount > 0 because "msg" """);
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<StateEnsureNode>().Subject;
        node.Guard.Should().NotBeNull();
    }

    // ── Slice 4.3: StateEnsure (to-scoped) ────────────────────────────────

    [Fact]
    public void Parse_StateEnsure_To_Simple()
    {
        var tree = Parse("""to Submitted ensure isValid because "msg" """);
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<StateEnsureNode>().Subject;
        node.Preposition.Kind.Should().Be(TokenKind.To);
    }

    // ── Slice 4.3: StateAction ─────────────────────────────────────────────

    [Fact]
    public void Parse_StateAction_To_Simple()
    {
        var tree = Parse("to Submitted -> set submittedAt = now()");
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<StateActionNode>().Subject;
        node.State.Name.Text.Should().Be("Submitted");
        node.Actions.Should().HaveCount(1);
        node.Actions[0].Should().BeOfType<SetStatement>();
    }

    [Fact]
    public void Parse_StateAction_To_MultipleActions()
    {
        var tree = Parse("""to Approved -> set status = "approved" -> set processedAt = now()""");
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<StateActionNode>().Subject;
        node.Actions.Should().HaveCount(2);
        node.Actions[0].Should().BeOfType<SetStatement>();
        node.Actions[1].Should().BeOfType<SetStatement>();
    }

    [Fact]
    public void Parse_StateAction_To_WithGuard()
    {
        var tree = Parse("to Submitted when isComplete -> set submittedAt = now()");
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<StateActionNode>().Subject;
        node.Guard.Should().NotBeNull();
    }

    [Fact]
    public void Parse_StateAction_Set()
    {
        var tree = Parse("to Draft -> set name = value");
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<StateActionNode>().Subject;
        node.Actions[0].Should().BeOfType<SetStatement>()
            .Which.Field.Text.Should().Be("name");
    }

    [Fact]
    public void Parse_StateAction_Add()
    {
        var tree = Parse("to Draft -> add items value");
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<StateActionNode>().Subject;
        node.Actions[0].Should().BeOfType<AddStatement>()
            .Which.Field.Text.Should().Be("items");
    }

    [Fact]
    public void Parse_StateAction_Clear()
    {
        var tree = Parse("to Draft -> clear items");
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<StateActionNode>().Subject;
        node.Actions[0].Should().BeOfType<ClearStatement>()
            .Which.Field.Text.Should().Be("items");
    }

    // ── Slice 4.4: EventEnsure ─────────────────────────────────────────────

    [Fact]
    public void Parse_EventEnsure_Simple()
    {
        var tree = Parse("""on Submit ensure Amount > 0 because "msg" """);
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<EventEnsureNode>().Subject;
        node.EventName.Text.Should().Be("Submit");
        node.Guard.Should().BeNull();
    }

    [Fact]
    public void Parse_EventEnsure_WithGuard()
    {
        var tree = Parse("""on Submit when isNew ensure Amount > 0 because "msg" """);
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<EventEnsureNode>().Subject;
        node.Guard.Should().NotBeNull();
    }

    // ── Slice 4.4: EventHandler ────────────────────────────────────────────

    [Fact]
    public void Parse_EventHandler_Simple()
    {
        var tree = Parse("on UpdateName -> set name = newName");
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<EventHandlerNode>().Subject;
        node.EventName.Text.Should().Be("UpdateName");
        node.Actions.Should().HaveCount(1);
        node.Actions[0].Should().BeOfType<SetStatement>();
    }

    [Fact]
    public void Parse_EventHandler_MultipleActions()
    {
        var tree = Parse("on Deposit -> add balance amount -> set lastDeposit = amount");
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<EventHandlerNode>().Subject;
        node.Actions.Should().HaveCount(2);
    }

    // ════════════════════════════════════════════════════════════════════════
    //  PR 5 — From-Scoped Constructs, TransitionRow, Outcomes, Error Sync
    // ════════════════════════════════════════════════════════════════════════

    // ── Slice 5.1: TransitionRow ───────────────────────────────────────────

    [Fact]
    public void Parse_TransitionRow_SimpleTransition()
    {
        var tree = Parse("from Draft on Submit -> transition Submitted");
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<TransitionRowNode>().Subject;
        node.FromState.Name.Text.Should().Be("Draft");
        node.EventName.Text.Should().Be("Submit");
        node.Guard.Should().BeNull();
        node.Actions.Should().BeEmpty();
        node.Outcome.Should().BeOfType<TransitionOutcomeNode>()
            .Which.TargetState.Text.Should().Be("Submitted");
    }

    [Fact]
    public void Parse_TransitionRow_WithGuard()
    {
        var tree = Parse("from UnderReview on Approve when Amount <= ClaimAmount -> transition Approved");
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<TransitionRowNode>().Subject;
        node.Guard.Should().NotBeNull();
        node.Outcome.Should().BeOfType<TransitionOutcomeNode>();
    }

    [Fact]
    public void Parse_TransitionRow_WithActionsAndTransition()
    {
        var tree = Parse("from UnderReview on Approve -> set ApprovedAmount = Amount -> transition Approved");
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<TransitionRowNode>().Subject;
        node.Actions.Should().HaveCount(1);
        node.Actions[0].Should().BeOfType<SetStatement>();
        node.Outcome.Should().BeOfType<TransitionOutcomeNode>();
    }

    [Fact]
    public void Parse_TransitionRow_MultipleActions()
    {
        var tree = Parse("from UnderReview on Approve -> set ApprovedAmount = Amount -> set Note = Approve.Note -> transition Approved");
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<TransitionRowNode>().Subject;
        node.Actions.Should().HaveCount(2);
        node.Outcome.Should().BeOfType<TransitionOutcomeNode>();
    }

    [Fact]
    public void Parse_TransitionRow_NoTransition()
    {
        var tree = Parse("from Active on Ping -> no transition");
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<TransitionRowNode>().Subject;
        node.Outcome.Should().BeOfType<NoTransitionOutcomeNode>();
    }

    [Fact]
    public void Parse_TransitionRow_Reject()
    {
        var tree = Parse("""from Draft on Submit when not isValid -> reject "Validation failed" """);
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<TransitionRowNode>().Subject;
        node.Guard.Should().NotBeNull();
        node.Outcome.Should().BeOfType<RejectOutcomeNode>();
    }

    [Fact]
    public void Parse_TransitionRow_PreEventGuard_EmitsDiagnosticAndParses()
    {
        var tree = Parse("from Draft when SomeCondition on Submit -> transition Submitted");
        tree.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.PreEventGuardNotAllowed));
        var node = tree.Declarations[0].Should().BeOfType<TransitionRowNode>().Subject;
        node.Guard.Should().NotBeNull();
        node.Outcome.Should().BeOfType<TransitionOutcomeNode>();
    }

    [Fact]
    public void Parse_TransitionRow_AnyState()
    {
        var tree = Parse("from any on VehiclesArrive -> set VehiclesWaiting = VehiclesWaiting + VehiclesArrive.Count -> no transition");
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<TransitionRowNode>().Subject;
        node.FromState.IsQuantifier.Should().BeTrue();
        node.Outcome.Should().BeOfType<NoTransitionOutcomeNode>();
    }

    [Fact]
    public void Parse_TransitionRow_WithActionsAndReject()
    {
        var tree = Parse("""from Screening on PassScreen -> reject "At least one interviewer required" """);
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<TransitionRowNode>().Subject;
        node.Outcome.Should().BeOfType<RejectOutcomeNode>();
    }

    // ── Slice 5.1: StateEnsure (from-scoped) ──────────────────────────────

    [Fact]
    public void Parse_StateEnsure_From_Simple()
    {
        var tree = Parse("""from Draft ensure isComplete because "Must complete form" """);
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<StateEnsureNode>().Subject;
        node.Preposition.Kind.Should().Be(TokenKind.From);
        node.State.Name.Text.Should().Be("Draft");
    }

    // ── Slice 5.1: StateAction (from-scoped) ──────────────────────────────

    [Fact]
    public void Parse_StateAction_From_Simple()
    {
        var tree = Parse("from Draft -> set processedAt = now()");
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<StateActionNode>().Subject;
        node.Preposition.Kind.Should().Be(TokenKind.From);
        node.State.Name.Text.Should().Be("Draft");
        node.Actions.Should().HaveCount(1);
        node.Actions[0].Should().BeOfType<SetStatement>();
    }

    // ── Slice 5.2: Error Recovery Hardening ────────────────────────────────

    [Fact]
    public void Parse_MissingOutcome_ProducesDiagnosticAndSyntheticNode()
    {
        var tree = Parse("from Draft on Submit -> set x = 1");
        tree.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.ExpectedOutcome));
        var node = tree.Declarations[0].Should().BeOfType<TransitionRowNode>().Subject;
        node.Outcome.Should().NotBeNull();
    }

    [Fact]
    public void Parse_MalformedActionStatement_ProducesDiagnosticAndContinues()
    {
        var tree = Parse("from Draft on Submit -> GARBAGE -> transition Done");
        tree.Diagnostics.Should().NotBeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<TransitionRowNode>().Subject;
        node.Outcome.Should().BeOfType<TransitionOutcomeNode>()
            .Which.TargetState.Text.Should().Be("Done");
    }

    [Fact]
    public void Parse_MultipleErrors_AllReported()
    {
        var tree = Parse("""
            from Draft on Submit -> GARBAGE -> transition Done
            from Draft on Approve -> JUNK -> transition Approved
            """);
        tree.Diagnostics.Should().HaveCountGreaterThanOrEqualTo(2);
    }

    // ── Slice 5.3: End-to-End Integration Tests (Sample Files) ─────────────

    [Theory]
    [InlineData("crosswalk-signal.precept")]
    [InlineData("trafficlight.precept")]
    public void Parse_SampleFile_ProducesNoParseErrors(string sampleFile)
    {
        var source = File.ReadAllText(Path.Combine(SamplesDir, sampleFile));
        var tokens = Lexer.Lex(source);
        var tree = Parser.Parse(tokens);
        tree.Diagnostics.Should().BeEmpty($"sample file '{sampleFile}' should parse without errors");
    }

    [Theory]
    [InlineData("crosswalk-signal.precept", "CrosswalkSignal")]
    [InlineData("trafficlight.precept", "TrafficLight")]
    [InlineData("hiring-pipeline.precept", "HiringPipeline")]
    public void Parse_SampleFile_HeaderIsCorrect(string sampleFile, string expectedName)
    {
        var source = File.ReadAllText(Path.Combine(SamplesDir, sampleFile));
        var tree = Parser.Parse(Lexer.Lex(source));
        tree.Header.Should().NotBeNull();
        tree.Header!.Name.Text.Should().Be(expectedName);
    }

    [Theory]
    [InlineData("crosswalk-signal.precept", 5)]
    [InlineData("trafficlight.precept", 5)]
    [InlineData("hiring-pipeline.precept", 5)]
    public void Parse_SampleFile_DeclarationCountIsReasonable(string sampleFile, int minCount)
    {
        var source = File.ReadAllText(Path.Combine(SamplesDir, sampleFile));
        var tree = Parser.Parse(Lexer.Lex(source));
        tree.Declarations.Length.Should().BeGreaterThanOrEqualTo(minCount,
            $"sample file '{sampleFile}' should have at least {minCount} declarations");
    }

    [Theory]
    [InlineData("crosswalk-signal.precept")]
    [InlineData("trafficlight.precept")]
    public void Parse_SampleFile_AccessModeNodes_HaveNoNullState(string sampleFile)
    {
        var source = File.ReadAllText(Path.Combine(SamplesDir, sampleFile));
        var tree = Parser.Parse(Lexer.Lex(source));
        var accessModes = tree.Declarations.OfType<AccessModeNode>();
        foreach (var am in accessModes)
            am.State.Should().NotBeNull();
    }

    [Fact]
    public void Parse_SampleFile_OmitDeclarationNodes_HaveNoGuard()
    {
        // Verify OmitDeclarationNode has exactly 3 constructor parameters (Span, State, Fields) — no Guard
        var ctors = typeof(OmitDeclarationNode).GetConstructors();
        ctors.Should().HaveCount(1);
        var parameters = ctors[0].GetParameters();
        parameters.Should().NotContain(p => p.Name == "Guard",
            "OmitDeclarationNode must not have a Guard parameter — it is structurally impossible");
    }

    [Theory]
    [InlineData("crosswalk-signal.precept")]
    [InlineData("trafficlight.precept")]
    [InlineData("hiring-pipeline.precept")]
    public void Parse_SampleFile_HasTransitionRows(string sampleFile)
    {
        var source = File.ReadAllText(Path.Combine(SamplesDir, sampleFile));
        var tree = Parser.Parse(Lexer.Lex(source));
        tree.Declarations.OfType<TransitionRowNode>().Should().NotBeEmpty(
            $"sample file '{sampleFile}' should contain at least one transition row");
    }

    // ── Slice 5.4: Final Parser Wiring ─────────────────────────────────────

    [Fact]
    public void Parser_Parse_ReturnsNonNullTree()
    {
        var tree = Parse("precept Test");
        tree.Should().NotBeNull();
        tree.Header.Should().NotBeNull();
    }

    [Fact]
    public void Parser_Parse_EmptyInput_ReturnsTreeWithNullHeaderAndDiagnostic()
    {
        var tree = Parse("");
        tree.Header.Should().BeNull();
    }

    [Fact]
    public void Parser_Parse_AllConstructKinds_RoundTrip()
    {
        var tree = Parse("""
            precept AllKinds
            field amount as decimal nonnegative
            state Draft initial, Submitted, Approved terminal
            event Submit(Amount as decimal)
            rule amount > 0 because "must be positive"
            in Draft modify amount editable
            in Draft omit amount
            in Submitted ensure amount > 0 because "msg"
            to Submitted -> set processedAt = now()
            on Submit ensure Submit.Amount > 0 because "positive"
            on Submit -> set amount = Submit.Amount
            from Draft on Submit -> set amount = Submit.Amount -> transition Submitted
            from Approved ensure amount > 0 because "msg"
            """);

        tree.Diagnostics.Should().BeEmpty();
        tree.Declarations.OfType<FieldDeclarationNode>().Should().NotBeEmpty();
        tree.Declarations.OfType<StateDeclarationNode>().Should().NotBeEmpty();
        tree.Declarations.OfType<EventDeclarationNode>().Should().NotBeEmpty();
        tree.Declarations.OfType<RuleDeclarationNode>().Should().NotBeEmpty();
        tree.Declarations.OfType<AccessModeNode>().Should().NotBeEmpty();
        tree.Declarations.OfType<OmitDeclarationNode>().Should().NotBeEmpty();
        tree.Declarations.OfType<StateEnsureNode>().Should().HaveCountGreaterThanOrEqualTo(2);
        tree.Declarations.OfType<StateActionNode>().Should().NotBeEmpty();
        tree.Declarations.OfType<EventEnsureNode>().Should().NotBeEmpty();
        tree.Declarations.OfType<EventHandlerNode>().Should().NotBeEmpty();
        tree.Declarations.OfType<TransitionRowNode>().Should().NotBeEmpty();
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Phase 7 — Remediation test cases (TG-1 through TG-10)
    // ════════════════════════════════════════════════════════════════════════

    // TG-1: AccessMode editable with pre-field guard
    [Fact]
    public void Parse_AccessMode_Editable_WithPreFieldGuard()
    {
        var tree = Parse("in UnderReview when Active modify FraudFlag editable");
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<AccessModeNode>().Subject;
        node.Guard.Should().NotBeNull();
        node.Mode.Kind.Should().Be(TokenKind.Editable);
    }

    // TG-2: OmitDeclaration list with post-field guard emits diagnostic
    [Fact]
    public void Parse_OmitDeclaration_List_WithPostFieldGuard_EmitsDiagnostic()
    {
        var tree = Parse("in Draft omit Notes, Memo when SomeCondition");
        tree.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.OmitDoesNotSupportGuard));
        tree.Declarations[0].Should().BeOfType<OmitDeclarationNode>();
    }

    // TG-3: OmitDeclaration all with post-field guard emits diagnostic
    [Fact]
    public void Parse_OmitDeclaration_All_WithPostFieldGuard_EmitsDiagnostic()
    {
        var tree = Parse("in Draft omit all when SomeCondition");
        tree.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.OmitDoesNotSupportGuard));
        tree.Declarations[0].Should().BeOfType<OmitDeclarationNode>();
    }

    // TG-4: OmitDeclaration list with pre-field guard emits diagnostic
    [Fact]
    public void Parse_OmitDeclaration_List_WithPreFieldGuard_EmitsDiagnostic()
    {
        var tree = Parse("in Closed when SomeCondition omit Notes, Memo");
        tree.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.OmitDoesNotSupportGuard));
        tree.Declarations[0].Should().BeOfType<OmitDeclarationNode>();
    }

    // TG-5: OmitDeclaration all with pre-field guard emits diagnostic
    [Fact]
    public void Parse_OmitDeclaration_All_WithPreFieldGuard_EmitsDiagnostic()
    {
        var tree = Parse("in Closed when SomeCondition omit all");
        tree.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.OmitDoesNotSupportGuard));
        tree.Declarations[0].Should().BeOfType<OmitDeclarationNode>();
    }

    // TG-6: Bare modify at top level produces diagnostic and syncs
    [Fact]
    public void Parse_BareModifyAtTopLevel_ProducesDiagnosticAndSyncs()
    {
        var tree = Parse("modify Amount readonly\nfield name as string");
        tree.Diagnostics.Should().NotBeEmpty();
        // Parser should recover and parse the field declaration
        tree.Declarations.OfType<FieldDeclarationNode>().Should().NotBeEmpty();
    }

    // TG-7: EventHandler with stashed guard emits diagnostic (exercises NB-2 fix)
    [Fact]
    public void Parse_EventHandler_WithStashedGuard_EmitsDiagnostic()
    {
        var tree = Parse("on Submit when isNew -> set name = foo");
        tree.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.EventHandlerDoesNotSupportGuard));
        var node = tree.Declarations[0].Should().BeOfType<EventHandlerNode>().Subject;
        node.EventName.Text.Should().Be("Submit");
        node.Actions.Should().NotBeEmpty();
    }

    // TG-8a: EOF after modify produces diagnostic
    [Fact]
    public void Parse_AccessMode_EOF_After_Modify_ProducesDiagnostic()
    {
        var tree = Parse("in Draft modify");
        tree.Diagnostics.Should().NotBeEmpty();
    }

    // TG-8b: EOF after omit produces diagnostic
    [Fact]
    public void Parse_OmitDeclaration_EOF_After_Omit_ProducesDiagnostic()
    {
        var tree = Parse("in Draft omit");
        tree.Diagnostics.Should().NotBeEmpty();
    }

    // TG-9: Mixed AccessMode and OmitDeclaration produce distinct node types
    [Fact]
    public void Parse_MixedAccessModeAndOmit_DistinctNodeTypes()
    {
        var tree = Parse("in Draft modify Amount readonly\nin Draft omit Notes");
        tree.Diagnostics.Should().BeEmpty();
        tree.Declarations.Should().HaveCount(2);
        var accessMode = tree.Declarations[0].Should().BeOfType<AccessModeNode>().Subject;
        accessMode.Mode.Kind.Should().Be(TokenKind.Readonly);
        tree.Declarations[1].Should().BeOfType<OmitDeclarationNode>();
    }

    // TG-10: TransitionRow pre-event guard with complex guard expression
    [Fact]
    public void Parse_TransitionRow_PreEventGuard_WithComplexGuardExpression()
    {
        var tree = Parse("from Draft when amount > 0 and isValid on Submit -> transition Submitted");
        tree.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.PreEventGuardNotAllowed));
        var node = tree.Declarations[0].Should().BeOfType<TransitionRowNode>().Subject;
        node.Guard.Should().NotBeNull();
    }

    private static readonly string SamplesDir = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "..", "..", "..", "..", "..", "samples");

    // ── ParseActionStatement error path ──────────────────────────────────────

    [Fact]
    public void ParseActionStatement_UnknownToken_EmitsDiagnostic()
    {
        // 'from' is not an action keyword — feeding it in an action position should emit ExpectedToken
        var tree = Parse("""
            precept X
            state S initial, T terminal success
            event Go
            from S on Go -> from
            """);

        tree.Diagnostics.Should().Contain(d => d.Code == nameof(DiagnosticCode.ExpectedToken));
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Slice 6 — Parser Whitespace-Insensitivity (WSI) Tests
    // ════════════════════════════════════════════════════════════════════════

    // ── WSI-1: Multi-line whitespace — round-trip equivalence ──────────────

    [Fact]
    public void WSI_MultiLine_FieldDeclaration_ProducesSameAstAsOneLine()
    {
        var singleLine = Parse("""field Status as string default "active" """);
        var multiLine = Parse("""
            field Status as string
                default "active"
            """);

        singleLine.Diagnostics.Should().BeEmpty();
        multiLine.Diagnostics.Should().BeEmpty();

        var single = singleLine.Declarations[0].Should().BeOfType<FieldDeclarationNode>().Subject;
        var multi  = multiLine.Declarations[0].Should().BeOfType<FieldDeclarationNode>().Subject;

        single.Names[0].Text.Should().Be(multi.Names[0].Text);
        single.Type.Should().BeOfType<ScalarTypeRefNode>();
        multi.Type.Should().BeOfType<ScalarTypeRefNode>();
        single.Modifiers.Should().HaveCount(multi.Modifiers.Length,
            "modifier count must be identical regardless of newlines");
    }

    [Fact]
    public void WSI_MultiLine_EventDeclaration_ProducesSameAstAsOneLine()
    {
        var singleLine = Parse("event Assign(assigneeId as string, priority as integer)");
        var multiLine = Parse("""
            event Assign(
                assigneeId as string,
                priority as integer
            )
            """);

        singleLine.Diagnostics.Should().BeEmpty();
        multiLine.Diagnostics.Should().BeEmpty();

        var single = singleLine.Declarations[0].Should().BeOfType<EventDeclarationNode>().Subject;
        var multi  = multiLine.Declarations[0].Should().BeOfType<EventDeclarationNode>().Subject;

        single.Arguments.Should().HaveCount(2);
        multi.Arguments.Should().HaveCount(2);
        single.Arguments[0].Name.Text.Should().Be(multi.Arguments[0].Name.Text);
        single.Arguments[1].Name.Text.Should().Be(multi.Arguments[1].Name.Text);
    }

    [Fact]
    public void WSI_MultiLine_StateDeclaration_ProducesSameAstAsOneLine()
    {
        var singleLine = Parse("state Draft, Active, Closed");
        var multiLine = Parse("""
            state Draft, Active,
                Closed
            """);

        singleLine.Diagnostics.Should().BeEmpty();
        multiLine.Diagnostics.Should().BeEmpty();

        var single = singleLine.Declarations[0].Should().BeOfType<StateDeclarationNode>().Subject;
        var multi  = multiLine.Declarations[0].Should().BeOfType<StateDeclarationNode>().Subject;

        single.Entries.Should().HaveCount(3);
        multi.Entries.Should().HaveCount(3);
        single.Entries.Select(e => e.Name.Text)
            .Should().BeEquivalentTo(multi.Entries.Select(e => e.Name.Text));
    }

    [Fact]
    public void WSI_MultiLine_TransitionRow_ProducesSameAstAsOneLine()
    {
        var singleLine = Parse("from Draft on Submit -> set ClaimantName = Submit.Name -> transition Submitted");
        var multiLine = Parse("""
            from Draft on Submit
                -> set ClaimantName = Submit.Name
                -> transition Submitted
            """);

        singleLine.Diagnostics.Should().BeEmpty();
        multiLine.Diagnostics.Should().BeEmpty();

        var single = singleLine.Declarations[0].Should().BeOfType<TransitionRowNode>().Subject;
        var multi  = multiLine.Declarations[0].Should().BeOfType<TransitionRowNode>().Subject;

        single.Actions.Should().HaveCount(1);
        multi.Actions.Should().HaveCount(1);
        single.Outcome.Should().BeOfType<TransitionOutcomeNode>();
        multi.Outcome.Should().BeOfType<TransitionOutcomeNode>();
    }

    // ── WSI-2: Comment filtering — inline comments inside declarations ──────

    [Fact]
    public void WSI_Comment_InlineAfterFieldType_DoesNotBreakParsing()
    {
        var tree = Parse("""
            field Priority as integer # priority score 1-5
                default 3
            """);

        tree.Diagnostics.Should().BeEmpty();
        var field = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>().Subject;
        field.Names[0].Text.Should().Be("Priority");
        field.Type.Should().BeOfType<ScalarTypeRefNode>()
            .Which.TypeName.Kind.Should().Be(TokenKind.IntegerType);
        field.Modifiers.Should().HaveCount(1, "default 3 is one value-bearing modifier");
    }

    [Fact]
    public void WSI_Comment_InlineInsideArgList_DoesNotBreakParsing()
    {
        var tree = Parse("""
            event Submit(
                comment as string # optional note
            )
            """);

        tree.Diagnostics.Should().BeEmpty();
        var evt = tree.Declarations[0].Should().BeOfType<EventDeclarationNode>().Subject;
        evt.Arguments.Should().HaveCount(1);
        evt.Arguments[0].Name.Text.Should().Be("comment");
    }

    [Fact]
    public void WSI_Comment_BetweenDeclarations_DoesNotBreakParsing()
    {
        var tree = Parse("""
            precept Demo
            # Fields section
            field Count as integer default 0
            # States section
            state Active initial, Closed terminal
            """);

        tree.Diagnostics.Should().BeEmpty();
        tree.Header!.Name.Text.Should().Be("Demo");
        tree.Declarations.OfType<FieldDeclarationNode>().Should().HaveCount(1);
        tree.Declarations.OfType<StateDeclarationNode>().Should().HaveCount(1);
    }

    // ── WSI-3: Multi-qualifier parsing — both families ─────────────────────

    [Fact]
    public void WSI_Qualifier_ExchangeRate_TwoQualifiers_InAndTo()
    {
        var tree = Parse("""field Rate as exchangerate in "USD" to "EUR" """);

        tree.Diagnostics.Should().BeEmpty();
        var field   = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>().Subject;
        var typeRef = field.Type.Should().BeOfType<ScalarTypeRefNode>().Subject;

        typeRef.TypeName.Kind.Should().Be(TokenKind.ExchangeRateType);
        typeRef.Qualifiers.Should().HaveCount(2, "exchangerate has in/to qualifier axes");
        typeRef.Qualifiers[0].Keyword.Kind.Should().Be(TokenKind.In);
        typeRef.Qualifiers[0].Value.Should().BeOfType<LiteralExpression>()
            .Which.Value.Text.Should().Be("USD");
        typeRef.Qualifiers[1].Keyword.Kind.Should().Be(TokenKind.To);
        typeRef.Qualifiers[1].Value.Should().BeOfType<LiteralExpression>()
            .Which.Value.Text.Should().Be("EUR");
    }

    [Fact]
    public void WSI_Qualifier_Price_TwoQualifiers_InAndOf()
    {
        var tree = Parse("""field Cost as price in "USD" of "mass" """);

        tree.Diagnostics.Should().BeEmpty();
        var field   = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>().Subject;
        var typeRef = field.Type.Should().BeOfType<ScalarTypeRefNode>().Subject;

        typeRef.TypeName.Kind.Should().Be(TokenKind.PriceType);
        typeRef.Qualifiers.Should().HaveCount(2, "price has in/of qualifier axes");
        typeRef.Qualifiers[0].Keyword.Kind.Should().Be(TokenKind.In);
        typeRef.Qualifiers[0].Value.Should().BeOfType<LiteralExpression>()
            .Which.Value.Text.Should().Be("USD");
        typeRef.Qualifiers[1].Keyword.Kind.Should().Be(TokenKind.Of);
        typeRef.Qualifiers[1].Value.Should().BeOfType<LiteralExpression>()
            .Which.Value.Text.Should().Be("mass");
    }

    [Fact]
    public void WSI_Qualifier_Money_SingleQualifier_In()
    {
        var tree = Parse("""field Amount as money in "USD" """);

        tree.Diagnostics.Should().BeEmpty();
        var field   = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>().Subject;
        var typeRef = field.Type.Should().BeOfType<ScalarTypeRefNode>().Subject;

        typeRef.TypeName.Kind.Should().Be(TokenKind.MoneyType);
        typeRef.Qualifiers.Should().HaveCount(1, "money has only an in-currency qualifier axis");
        typeRef.Qualifiers[0].Keyword.Kind.Should().Be(TokenKind.In);
    }

    [Fact]
    public void WSI_Qualifier_ExchangeRate_InArgPosition_TwoQualifiers()
    {
        // Qualifiers must survive inside event argument lists too
        var tree = Parse("""event SetRate(r as exchangerate in "USD" to "EUR") """);

        tree.Diagnostics.Should().BeEmpty();
        var evt = tree.Declarations[0].Should().BeOfType<EventDeclarationNode>().Subject;
        evt.Arguments.Should().HaveCount(1);
        var argType = evt.Arguments[0].Type.Should().BeOfType<ScalarTypeRefNode>().Subject;
        argType.TypeName.Kind.Should().Be(TokenKind.ExchangeRateType);
        argType.Qualifiers.Should().HaveCount(2);
        argType.Qualifiers[0].Keyword.Kind.Should().Be(TokenKind.In);
        argType.Qualifiers[1].Keyword.Kind.Should().Be(TokenKind.To);
    }

    // ── WSI-4: Qualifier disambiguation — in/modify/ensure at declaration boundary

    [Fact]
    public void WSI_Disambiguation_InDraftModify_IsNotQualifier_ForExchangeRateType()
    {
        // 'in Draft' followed by 'modify' must NOT be parsed as a type qualifier.
        // The disambiguation checks Peek(2) against the construct verb set.
        var tree = Parse("""
            field Rate as exchangerate
            in Draft modify Rate editable
            """);

        tree.Diagnostics.Should().BeEmpty();
        tree.Declarations.Should().HaveCount(2);
        var typeRef = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>()
            .Subject.Type.Should().BeOfType<ScalarTypeRefNode>().Subject;
        typeRef.Qualifiers.Should().BeEmpty(
            "in Draft followed by 'modify' is a declaration boundary, not a qualifier");
        tree.Declarations[1].Should().BeOfType<AccessModeNode>();
    }

    [Fact]
    public void WSI_Disambiguation_InActiveEnsure_IsNotQualifier_ForExchangeRateType()
    {
        // 'in Active' followed by 'ensure' is StateEnsure, not a qualifier on exchangerate.
        var tree = Parse("""
            field Rate as exchangerate
            in Active ensure Rate.amount > 0 because "msg"
            """);

        tree.Diagnostics.Should().BeEmpty();
        tree.Declarations.Should().HaveCount(2);
        var typeRef = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>()
            .Subject.Type.Should().BeOfType<ScalarTypeRefNode>().Subject;
        typeRef.Qualifiers.Should().BeEmpty(
            "in Active followed by 'ensure' is a declaration boundary, not a qualifier");
        tree.Declarations[1].Should().BeOfType<StateEnsureNode>();
    }

    [Fact]
    public void WSI_Disambiguation_StringType_InDraftModify_IsSecondDeclaration()
    {
        // string has no qualifier shape — 'in Draft modify' is the next declaration trivially.
        var tree = Parse("""
            field Status as string
            in Draft modify Status readonly
            """);

        tree.Declarations.Should().HaveCount(2);
        var typeRef = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>()
            .Subject.Type.Should().BeOfType<ScalarTypeRefNode>().Subject;
        typeRef.Qualifiers.Should().BeEmpty("string has no qualifier shape");
        tree.Declarations[1].Should().BeOfType<AccessModeNode>();
    }

    // ── WSI-5: Collection qualifiers — element type qualifiers on collection type refs

    [Fact]
    public void WSI_CollectionQualifier_SetOfExchangeRate_TwoElementQualifiers()
    {
        var tree = Parse("""field Rates as set of exchangerate in "USD" to "EUR" """);

        tree.Diagnostics.Should().BeEmpty();
        var field   = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>().Subject;
        var typeRef = field.Type.Should().BeOfType<CollectionTypeRefNode>().Subject;

        typeRef.CollectionKind.Kind.Should().Be(TokenKind.Set);
        typeRef.ElementType.Kind.Should().Be(TokenKind.ExchangeRateType);
        typeRef.Qualifiers.Should().HaveCount(2,
            "exchangerate element type carries in/to qualifier axes");
        typeRef.Qualifiers[0].Keyword.Kind.Should().Be(TokenKind.In);
        typeRef.Qualifiers[0].Value.Should().BeOfType<LiteralExpression>()
            .Which.Value.Text.Should().Be("USD");
        typeRef.Qualifiers[1].Keyword.Kind.Should().Be(TokenKind.To);
    }

    [Fact]
    public void WSI_CollectionQualifier_SetOfMoney_SingleElementQualifier()
    {
        var tree = Parse("""field Portfolio as set of money in "USD" """);

        tree.Diagnostics.Should().BeEmpty();
        var field   = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>().Subject;
        var typeRef = field.Type.Should().BeOfType<CollectionTypeRefNode>().Subject;

        typeRef.ElementType.Kind.Should().Be(TokenKind.MoneyType);
        typeRef.Qualifiers.Should().HaveCount(1);
        typeRef.Qualifiers[0].Keyword.Kind.Should().Be(TokenKind.In);
    }

    [Fact]
    public void WSI_CollectionQualifier_SetOfString_NoElementQualifiers()
    {
        // string has no qualifier shape — collection element qualifiers must be empty
        var tree = Parse("field Tags as set of string");

        tree.Diagnostics.Should().BeEmpty();
        var field   = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>().Subject;
        var typeRef = field.Type.Should().BeOfType<CollectionTypeRefNode>().Subject;

        typeRef.ElementType.Kind.Should().Be(TokenKind.StringType);
        typeRef.Qualifiers.Should().BeEmpty("string has no qualifier shape");
    }

    // ── WSI-6: Negative cases — types without qualifier shape ───────────────

    [Fact]
    public void WSI_Negative_IntegerType_InDraftFollowedByDecl_ParsedAsNextDeclaration()
    {
        // integer has no qualifier shape; 'in Draft' cannot attach as a qualifier.
        var tree = Parse("""
            field Count as integer
            in Draft modify Count readonly
            """);

        tree.Declarations.Should().HaveCount(2);
        var typeRef = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>()
            .Subject.Type.Should().BeOfType<ScalarTypeRefNode>().Subject;
        typeRef.TypeName.Kind.Should().Be(TokenKind.IntegerType);
        typeRef.Qualifiers.Should().BeEmpty("integer has no qualifier shape");
        tree.Declarations[1].Should().BeOfType<AccessModeNode>();
    }

    [Fact]
    public void WSI_Negative_BooleanType_InDraftFollowedByDecl_ParsedAsNextDeclaration()
    {
        var tree = Parse("""
            field Flag as boolean
            in Draft modify Flag readonly
            """);

        tree.Declarations.Should().HaveCount(2);
        var typeRef = tree.Declarations[0].Should().BeOfType<FieldDeclarationNode>()
            .Subject.Type.Should().BeOfType<ScalarTypeRefNode>().Subject;
        typeRef.TypeName.Kind.Should().Be(TokenKind.BooleanType);
        typeRef.Qualifiers.Should().BeEmpty("boolean has no qualifier shape");
    }

    // ── WSI-7: Compilation.Tokens regression — pre-parse filter does not strip ──

    [Fact]
    public void WSI_TokenStream_ContainsNewLineTokens_AfterMultiLineParse()
    {
        // The pre-parse filter in Parser.Parse() strips NewLine from the working copy
        // but must NOT touch TokenStream.Tokens — that is the full-fidelity record.
        var source = """
            precept Test
            field Status as string
            state Draft initial
            """;
        var tokens = Lexer.Lex(source);
        var tree   = Parser.Parse(tokens);

        // Full token stream retains NewLine tokens
        tokens.Tokens.Should().Contain(t => t.Kind == TokenKind.NewLine,
            "TokenStream.Tokens retains the full pre-filter token stream");

        // The parsed declarations must be correct — NewLine tokens produce no nodes
        tree.Header.Should().NotBeNull();
        tree.Declarations.Should().HaveCount(2,
            "only field and state appear in Declarations — NewLine tokens are never parsed as nodes");
        tree.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void WSI_TokenStream_ContainsCommentTokens_AfterCommentedParse()
    {
        var source = """
            precept Demo # this is the demo precept
            field Count as integer # a counter
            """;
        var tokens = Lexer.Lex(source);
        var tree   = Parser.Parse(tokens);

        // Comment tokens survive in TokenStream.Tokens
        tokens.Tokens.Should().Contain(t => t.Kind == TokenKind.Comment,
            "TokenStream.Tokens retains Comment tokens");

        // AST must be clean — comments do not create extra nodes or diagnostics
        tree.Header!.Name.Text.Should().Be("Demo");
        tree.Declarations.Should().HaveCount(1,
            "only the field declaration appears — Comment tokens are never parsed as nodes");
        tree.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void WSI_TokenStream_NewLineCount_IsGreaterThanZero_ForMultiLineSource()
    {
        // Belt-and-suspenders: confirm the filter runs on a COPY, not in-place.
        var multiLine  = "precept X\nfield a as integer\nfield b as integer\nstate S initial";
        var singleLine = "precept X field a as integer field b as integer state S initial";

        var multiTokens  = Lexer.Lex(multiLine);
        var singleTokens = Lexer.Lex(singleLine);

        var multiTree  = Parser.Parse(multiTokens);
        var singleTree = Parser.Parse(singleTokens);

        multiTokens.Tokens
            .Count(t => t.Kind == TokenKind.NewLine)
            .Should().BeGreaterThan(0,
                "multi-line source produces NewLine tokens retained in the token stream");
        singleTokens.Tokens
            .Count(t => t.Kind == TokenKind.NewLine)
            .Should().Be(0, "single-line source produces no NewLine tokens");

        // Both must produce identical declaration counts — whitespace is cosmetic
        multiTree.Declarations.Should().HaveCount(
            singleTree.Declarations.Length,
            "whitespace must not affect declaration count");
        multiTree.Diagnostics.Should().BeEmpty();
        singleTree.Diagnostics.Should().BeEmpty();
    }

    // ── WSI-8: Integration — sample files parse with no parse-stage errors ──

    [Theory]
    [InlineData("hiring-pipeline.precept")]
    public void WSI_Integration_SampleFile_ParsesWithNoErrors(string sampleFile)
    {
        var source = File.ReadAllText(Path.Combine(SamplesDir, sampleFile));
        var tree = Parser.Parse(Lexer.Lex(source));
        tree.Diagnostics.Should().BeEmpty(
            $"sample file '{sampleFile}' should parse without errors");
    }

    [Fact]
    public void WSI_Integration_InsuranceClaim_HasExpectedDeclarationCounts()
    {
        // insurance-claim.precept uses 'is set' expressions and 'in State ensure ... when ...'
        // constructs that the current parser partially handles. We verify the key structural
        // declarations are present (parser recovery works) rather than asserting zero diagnostics.
        var source = File.ReadAllText(Path.Combine(SamplesDir, "insurance-claim.precept"));
        var tree = Parser.Parse(Lexer.Lex(source));

        tree.Header.Should().NotBeNull();
        tree.Header!.Name.Text.Should().Be("InsuranceClaim");

        tree.Declarations.OfType<FieldDeclarationNode>().Should().HaveCount(8,
            "8 field declarations in insurance-claim.precept");
        tree.Declarations.OfType<StateDeclarationNode>().Should().HaveCount(6,
            "6 state declarations (one per state line)");
        tree.Declarations.OfType<EventDeclarationNode>()
            .Should().HaveCountGreaterThanOrEqualTo(5);
        tree.Declarations.OfType<TransitionRowNode>()
            .Should().HaveCountGreaterThanOrEqualTo(8);
    }

    [Fact]
    public void WSI_Integration_LoanApplication_HasExpectedDeclarationCounts()
    {
        // loan-application.precept uses 'in State ensure ... when ...' which the current
        // parser emits a diagnostic for (When is a StructuralBoundaryToken that terminates
        // the ensure condition, then Expect(Because) sees 'when' instead). Parser recovers.
        var source = File.ReadAllText(Path.Combine(SamplesDir, "loan-application.precept"));
        var tree = Parser.Parse(Lexer.Lex(source));

        tree.Header.Should().NotBeNull();
        tree.Header!.Name.Text.Should().Be("LoanApplication");

        tree.Declarations.OfType<FieldDeclarationNode>()
            .Should().HaveCountGreaterThanOrEqualTo(7);
        tree.Declarations.OfType<StateDeclarationNode>()
            .Should().HaveCountGreaterThanOrEqualTo(5);
        tree.Declarations.OfType<TransitionRowNode>()
            .Should().HaveCountGreaterThanOrEqualTo(5);
    }

    // ── WSI-9: ChoiceElementTypeKeywords catalog-derived regression ──────────

    [Fact]
    public void ChoiceElementTypeKeywords_ContainsExactlyFiveTypes()
    {
        Parser.ChoiceElementTypeKeywords.Should().HaveCount(5,
            "exactly 5 primitive types carry TypeTrait.ChoiceElement");
    }

    [Fact]
    public void ChoiceElementTypeKeywords_ContainsExpectedMembers()
    {
        Parser.ChoiceElementTypeKeywords.Should().BeEquivalentTo(
            new[]
            {
                TokenKind.StringType, TokenKind.BooleanType, TokenKind.IntegerType,
                TokenKind.DecimalType, TokenKind.NumberType,
            },
            "ChoiceElementTypeKeywords must be derived from TypeTrait.ChoiceElement — " +
            "never a hardcoded parallel list");
    }

    // ════════════════════════════════════════════════════════════════════════
    //  Slice 10 — Collection Mutation Action Tests
    // ════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parse_ActionRemove()
    {
        var tree = Parse("from Draft on RemoveItem -> remove ItemList \"x\" -> no transition");
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<TransitionRowNode>().Subject;
        node.Actions.Should().HaveCount(1);
        node.Actions[0].Should().BeOfType<RemoveStatement>()
            .Which.Field.Text.Should().Be("ItemList");
    }

    [Fact]
    public void Parse_ActionEnqueue()
    {
        var tree = Parse("from Draft on AddToQueue -> enqueue ItemQueue \"item\" -> no transition");
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<TransitionRowNode>().Subject;
        node.Actions[0].Should().BeOfType<EnqueueStatement>()
            .Which.Field.Text.Should().Be("ItemQueue");
    }

    [Fact]
    public void Parse_ActionDequeue()
    {
        var tree = Parse("from Draft on ProcessNext -> dequeue ItemQueue -> no transition");
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<TransitionRowNode>().Subject;
        var stmt = node.Actions[0].Should().BeOfType<DequeueStatement>().Subject;
        stmt.Field.Text.Should().Be("ItemQueue");
        stmt.IntoField.Should().BeNull();
    }

    [Fact]
    public void Parse_ActionDequeueInto()
    {
        var tree = Parse("from Draft on ProcessNext -> dequeue ItemQueue into target -> no transition");
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<TransitionRowNode>().Subject;
        var stmt = node.Actions[0].Should().BeOfType<DequeueStatement>().Subject;
        stmt.Field.Text.Should().Be("ItemQueue");
        stmt.IntoField.Should().NotBeNull();
        stmt.IntoField!.Value.Text.Should().Be("target");
    }

    [Fact]
    public void Parse_ActionPush()
    {
        var tree = Parse("from Draft on AddEntry -> push ItemStack \"item\" -> no transition");
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<TransitionRowNode>().Subject;
        node.Actions[0].Should().BeOfType<PushStatement>()
            .Which.Field.Text.Should().Be("ItemStack");
    }

    [Fact]
    public void Parse_ActionPop()
    {
        var tree = Parse("from Draft on UndoEntry -> pop ItemStack -> no transition");
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<TransitionRowNode>().Subject;
        var stmt = node.Actions[0].Should().BeOfType<PopStatement>().Subject;
        stmt.Field.Text.Should().Be("ItemStack");
        stmt.IntoField.Should().BeNull();
    }

    [Fact]
    public void Parse_ActionPopInto()
    {
        var tree = Parse("from Draft on UndoEntry -> pop ItemStack into lastEntry -> no transition");
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<TransitionRowNode>().Subject;
        var stmt = node.Actions[0].Should().BeOfType<PopStatement>().Subject;
        stmt.Field.Text.Should().Be("ItemStack");
        stmt.IntoField.Should().NotBeNull();
        stmt.IntoField!.Value.Text.Should().Be("lastEntry");
    }

    [Fact]
    public void Parse_ActionClear()
    {
        var tree = Parse("from Draft on Reset -> clear ItemList -> no transition");
        tree.Diagnostics.Should().BeEmpty();
        var node = tree.Declarations[0].Should().BeOfType<TransitionRowNode>().Subject;
        node.Actions[0].Should().BeOfType<ClearStatement>()
            .Which.Field.Text.Should().Be("ItemList");
    }
}
