using System.Linq;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests.Parser;

/// <summary>
/// Tests for the ParsedOutcome discriminated union.
/// Verifies that ParseOutcome() produces the correct DU subtype for each
/// outcome form, handles error recovery with MalformedOutcome, and tracks
/// span coverage correctly.
/// </summary>
public class ParserOutcomeTests
{
    // ════════════════════════════════════════════════════════════════════════════
    //  §1. Happy-path DU subtype extraction
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void TransitionOutcome_HappyPath_ExtractsStateName()
    {
        var tokens = Lexer.Lex("from Draft on Submit -> transition Submitted");
        var manifest = Pipeline.Parser.Parse(tokens);

        var row = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.TransitionRow);
        var outcomeSlot = row.Slots.OfType<OutcomeSlot>().Single();

        outcomeSlot.Outcome.Should().BeOfType<TransitionOutcome>()
            .Which.StateName.Should().Be("Submitted");
    }

    [Fact]
    public void NoTransitionOutcome_HappyPath()
    {
        var tokens = Lexer.Lex("from Draft on Submit -> no transition");
        var manifest = Pipeline.Parser.Parse(tokens);

        var row = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.TransitionRow);
        var outcomeSlot = row.Slots.OfType<OutcomeSlot>().Single();

        outcomeSlot.Outcome.Should().BeOfType<NoTransitionOutcome>();
    }

    [Fact]
    public void RejectOutcome_HappyPath_ExtractsReason()
    {
        var tokens = Lexer.Lex("from Draft on Submit -> reject \"invalid\"");
        var manifest = Pipeline.Parser.Parse(tokens);

        var row = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.TransitionRowReject);
        var rejectSlot = row.Slots.OfType<RejectClauseSlot>().Single();

        rejectSlot.Reason.Should().Be("invalid");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §2. Error recovery — MalformedOutcome
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void TransitionOutcome_MissingStateName_IsMalformed()
    {
        var tokens = Lexer.Lex("from Draft on Submit -> transition");
        var manifest = Pipeline.Parser.Parse(tokens);

        var row = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.TransitionRow);
        var outcomeSlot = row.Slots.OfType<OutcomeSlot>().Single();

        outcomeSlot.Outcome.Should().BeOfType<MalformedOutcome>();
    }

    [Fact]
    public void NoTransitionOutcome_MissingTransitionKeyword_IsMalformed()
    {
        var tokens = Lexer.Lex("from Draft on Submit -> no");
        var manifest = Pipeline.Parser.Parse(tokens);

        var row = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.TransitionRow);
        var outcomeSlot = row.Slots.OfType<OutcomeSlot>().Single();

        outcomeSlot.Outcome.Should().BeOfType<MalformedOutcome>();
    }

    [Fact]
    public void RejectOutcome_MissingReason_IsMalformed()
    {
        var tokens = Lexer.Lex("from Draft on Submit -> reject");
        var manifest = Pipeline.Parser.Parse(tokens);

        var row = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.TransitionRow);
        var outcomeSlot = row.Slots.OfType<OutcomeSlot>().Single();

        outcomeSlot.Outcome.Should().BeOfType<MalformedOutcome>();
    }

    [Fact]
    public void Outcome_UnexpectedTokenAfterArrow_IsMalformed()
    {
        var tokens = Lexer.Lex("from Draft on Submit -> 42");
        var manifest = Pipeline.Parser.Parse(tokens);

        var row = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.TransitionRow);
        var outcomeSlot = row.Slots.OfType<OutcomeSlot>().Single();

        outcomeSlot.Outcome.Should().BeOfType<MalformedOutcome>();
    }

    [Fact]
    public void Outcome_UnexpectedTokenAfterArrow_EmitsDiagnostic()
    {
        var tokens = Lexer.Lex("from Draft on Submit -> unknown");
        var manifest = Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().ContainSingle(d => d.Code == nameof(DiagnosticCode.ExpectedOutcome));
    }

    [Fact]
    public void Outcome_BareArrow_IsMalformed()
    {
        // Bare arrow with nothing following — should parse as malformed and emit diagnostic
        var tokens = Lexer.Lex("from Draft on Submit ->");
        var manifest = Pipeline.Parser.Parse(tokens);

        var row = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.TransitionRow);
        var outcomeSlot = row.Slots.OfType<OutcomeSlot>().Single();

        outcomeSlot.Outcome.Should().BeOfType<MalformedOutcome>();
        manifest.Diagnostics.Should().ContainSingle(d => d.Code == nameof(DiagnosticCode.ExpectedOutcome));
    }

    [Fact]
    public void Outcome_MissingArrow_SentinelIsMalformed()
    {
        var tokens = Lexer.Lex("from Draft on Submit");
        var manifest = Pipeline.Parser.Parse(tokens);

        var row = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.TransitionRow);
        var outcomeSlot = row.Slots.OfType<OutcomeSlot>().Single();

        outcomeSlot.Outcome.Should().BeOfType<MalformedOutcome>();
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §3. Span coverage
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void TransitionOutcome_SpanCoversArrowThroughStateName()
    {
        var input = "from Draft on Submit -> transition Submitted";
        var tokens = Lexer.Lex(input);
        var manifest = Pipeline.Parser.Parse(tokens);

        var row = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.TransitionRow);
        var outcomeSlot = row.Slots.OfType<OutcomeSlot>().Single();
        var outcome = outcomeSlot.Outcome.Should().BeOfType<TransitionOutcome>().Subject;

        var arrowStart = input.IndexOf("->");
        var submittedEnd = input.IndexOf("Submitted") + "Submitted".Length;

        outcome.Span.Offset.Should().Be(arrowStart);
        outcome.Span.End.Should().Be(submittedEnd);
    }

    [Fact]
    public void NoTransitionOutcome_SpanCoversArrowThroughTransition()
    {
        var input = "from Draft on Submit -> no transition";
        var tokens = Lexer.Lex(input);
        var manifest = Pipeline.Parser.Parse(tokens);

        var row = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.TransitionRow);
        var outcomeSlot = row.Slots.OfType<OutcomeSlot>().Single();
        var outcome = outcomeSlot.Outcome.Should().BeOfType<NoTransitionOutcome>().Subject;

        var arrowStart = input.IndexOf("->");
        var transitionEnd = input.IndexOf("transition") + "transition".Length;

        outcome.Span.Offset.Should().Be(arrowStart);
        outcome.Span.End.Should().Be(transitionEnd);
    }

    [Fact]
    public void RejectOutcome_SpanCoversArrowThroughReason()
    {
        var input = "from Draft on Submit -> reject \"invalid\"";
        var tokens = Lexer.Lex(input);
        var manifest = Pipeline.Parser.Parse(tokens);

        var row = manifest.Constructs.Single(c => c.Meta.Kind == ConstructKind.TransitionRowReject);
        var rejectSlot = row.Slots.OfType<RejectClauseSlot>().Single();

        var arrowStart = input.IndexOf("->");
        var reasonEnd = input.IndexOf("\"invalid\"") + "\"invalid\"".Length;

        rejectSlot.Span.Offset.Should().Be(arrowStart);
        rejectSlot.Span.End.Should().Be(reasonEnd);
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  §4. OutcomesCatalog coverage
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void OutcomesCatalog_All_HasExpectedCount()
    {
        Outcomes.All.Should().HaveCount(3, "there are exactly 3 outcome forms: Transition, NoTransition, Reject");
    }

    [Fact]
    public void OutcomesCatalog_ByLeadingToken_ContainsAllForms()
    {
        Outcomes.ByLeadingToken.Should().ContainKey(TokenKind.Transition);
        Outcomes.ByLeadingToken.Should().ContainKey(TokenKind.No);
        Outcomes.ByLeadingToken.Should().ContainKey(TokenKind.Reject);
    }

    [Fact]
    public void OutcomesCatalog_GetMeta_TransitionKind_HasCorrectMetadata()
    {
        var meta = Outcomes.GetMeta(OutcomeKind.Transition);
        meta.LeadingToken.Should().Be(TokenKind.Transition);
        meta.ArgumentKind.Should().Be(OutcomeArgumentKind.RequiredIdentifier);
        meta.ParsedSubtype.Should().Be(typeof(TransitionOutcome));
    }

    [Fact]
    public void OutcomesCatalog_GetMeta_NoTransitionKind_HasCorrectMetadata()
    {
        var meta = Outcomes.GetMeta(OutcomeKind.NoTransition);
        meta.LeadingToken.Should().Be(TokenKind.No);
        meta.ArgumentKind.Should().Be(OutcomeArgumentKind.SecondaryToken);
        meta.ParsedSubtype.Should().Be(typeof(NoTransitionOutcome));
    }

    [Fact]
    public void OutcomesCatalog_GetMeta_RejectKind_HasCorrectMetadata()
    {
        var meta = Outcomes.GetMeta(OutcomeKind.Reject);
        meta.LeadingToken.Should().Be(TokenKind.Reject);
        meta.ArgumentKind.Should().Be(OutcomeArgumentKind.RequiredStringLiteral);
        meta.ParsedSubtype.Should().Be(typeof(RejectOutcome));
    }

    [Fact]
    public void OutcomesCatalog_NoTransitionSecondaryToken_IsTransition()
    {
        Outcomes.NoTransitionSecondaryToken.Should().Be(TokenKind.Transition);
    }
}
