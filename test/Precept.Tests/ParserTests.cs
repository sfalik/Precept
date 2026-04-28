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
}
