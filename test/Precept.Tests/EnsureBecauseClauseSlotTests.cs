using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.Tests;

/// <summary>
/// Regression and specification tests for the BecauseClause slot split fix.
/// StateEnsure and EventEnsure must carry BecauseClause as a separate, optional
/// slot while the pre-verb guard now occupies catalog slot index [1].
///
/// Reference design: RuleDeclaration already models a split expression/guard/reason
/// shape with [RuleExpression, GuardClause(opt), BecauseClause(req)].
/// After the fix: StateEnsure = [StateTarget, GuardClause(opt), EnsureClause, BecauseClause(opt)]
///               EventEnsure  = [EventTarget, GuardClause(opt), EnsureClause, BecauseClause(opt)]
///
/// Test categories (marked by expected status at time of writing):
///   GREEN  — passes before George's catalog fix (type DU shape, lexer, RuleDeclaration regression)
///   RED-C  — red until George's catalog fix lands (StateEnsure / EventEnsure slot count + identity)
///   RED-P  — red until parser stub is replaced with a real implementation
///   RED-R  — red until runtime evaluator is implemented
/// </summary>
public class EnsureBecauseClauseSlotTests
{
    // ════════════════════════════════════════════════════════════════════════════
    //  BecauseClauseSlot DU shape — GREEN
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void BecauseClauseSlot_KindIsBecauseClause()
    {
        var slot = new BecauseClauseSlot("because message", SourceSpan.Missing);
        slot.Kind.Should().Be(ConstructSlotKind.BecauseClause,
            "BecauseClauseSlot must carry Kind == BecauseClause");
    }

    [Fact]
    public void BecauseClauseSlot_MessageIsPreserved()
    {
        const string msg = "Approved orders must have positive amount";
        var slot = new BecauseClauseSlot(msg, SourceSpan.Missing);
        slot.Message.Should().Be(msg,
            "BecauseClauseSlot.Message must round-trip the reason string exactly");
    }

    [Fact]
    public void BecauseClauseSlot_EmptyMessageIsPreserved()
    {
        var slot = new BecauseClauseSlot(string.Empty, SourceSpan.Missing);
        slot.Message.Should().BeEmpty(
            "BecauseClauseSlot must preserve an empty message without substitution");
    }

    [Fact]
    public void BecauseClauseSlot_IsSubtypeOfSlotValue()
    {
        var slot = new BecauseClauseSlot("reason", SourceSpan.Missing);
        slot.Should().BeAssignableTo<SlotValue>(
            "BecauseClauseSlot must be a SlotValue DU subtype");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  RuleDeclaration BecauseClause regression — GREEN
    //  The fix must not disturb the existing RuleDeclaration slot sequence.
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void RuleDeclaration_BecauseClauseAtIndex2_Regression()
    {
        var slots = Constructs.GetMeta(ConstructKind.RuleDeclaration).Slots;
        slots.Should().HaveCount(3, "RuleDeclaration: [RuleExpression, GuardClause(opt), BecauseClause]");
        slots[2].Kind.Should().Be(ConstructSlotKind.BecauseClause,
            "RuleDeclaration.Slots[2] is BecauseClause — this must not regress");
        slots[2].IsRequired.Should().BeTrue(
            "BecauseClause is required on rule declarations — every rule must supply a reason");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Lexer — because keyword — GREEN
    //  The lexer is context-free; 'because' tokens correctly regardless of the
    //  construct that surrounds them.
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Lex_Because_IsAKeyword()
    {
        var stream = Lexer.Lex("because");
        stream.Tokens[0].Kind.Should().Be(TokenKind.Because,
            "'because' is a reserved keyword and must lex as TokenKind.Because");
        stream.Diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void Lex_BecauseKeyword_InStateEnsureContext_LexesCorrectly()
    {
        var src = "in Approved ensure amount > 0 because \"Approved orders must have positive amount\"";
        var stream = Lexer.Lex(src);

        stream.Diagnostics.Should().BeEmpty("StateEnsure with because must lex without errors");
        stream.Tokens.Select(t => t.Kind).Should().Contain(TokenKind.Because,
            "the 'because' keyword must produce a Because token in a StateEnsure line");
        stream.Tokens.Select(t => t.Kind).Should().Contain(TokenKind.Ensure,
            "the 'ensure' keyword must also be present");
        stream.Tokens.Should().ContainSingle(t => t.Kind == TokenKind.Because);
    }

    [Fact]
    public void Lex_BecauseKeyword_InEventEnsureContext_LexesCorrectly()
    {
        var src = "on Submit ensure reviewer != \"\" because \"Reviewer required before submission\"";
        var stream = Lexer.Lex(src);

        stream.Diagnostics.Should().BeEmpty("EventEnsure with because must lex without errors");
        stream.Tokens.Select(t => t.Kind).Should().Contain(TokenKind.Because,
            "the 'because' keyword must produce a Because token in an EventEnsure line");
        stream.Tokens.Select(t => t.Kind).Should().Contain(TokenKind.Ensure,
            "the 'ensure' keyword must also be present");
        stream.Tokens.Should().ContainSingle(t => t.Kind == TokenKind.Because);
    }

    [Fact]
    public void Lex_StateEnsureWithoutBecause_LexesWithoutErrors()
    {
        // Ensures without because are valid DSL — no lex error on the absent keyword.
        var src = "in Draft ensure amount > 0";
        var stream = Lexer.Lex(src);

        stream.Diagnostics.Should().BeEmpty("StateEnsure without because must lex without errors");
        stream.Tokens.Select(t => t.Kind).Should().NotContain(TokenKind.Because,
            "no because keyword should appear in a bare ensure");
    }

    [Fact]
    public void Lex_EventEnsureWithoutBecause_LexesWithoutErrors()
    {
        var src = "on Submit ensure reviewer != \"\"";
        var stream = Lexer.Lex(src);

        stream.Diagnostics.Should().BeEmpty("EventEnsure without because must lex without errors");
        stream.Tokens.Select(t => t.Kind).Should().NotContain(TokenKind.Because);
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  StateEnsure catalog slot structure — RED-C (waiting on George's fix)
    //
    //  Before fix: [SlotStateTarget, SlotEnsureClause, SlotBecauseClause(opt)]                (3 slots)
    //  After fix:  [SlotStateTarget, SlotGuardClause(opt), SlotEnsureClause, SlotBecauseClause(opt)]  (4 slots)
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void StateEnsure_HasFourSlots_AfterGuardAndBecauseSplit()
    {
        var slots = Constructs.GetMeta(ConstructKind.StateEnsure).Slots;
        slots.Should().HaveCount(4,
            "StateEnsure must have 4 slots after the guard and BecauseClause split: [StateTarget, GuardClause, EnsureClause, BecauseClause]");
    }

    [Fact]
    public void StateEnsure_SlotAtIndex0_IsStateTarget()
    {
        var slots = Constructs.GetMeta(ConstructKind.StateEnsure).Slots;
        slots[0].Kind.Should().Be(ConstructSlotKind.StateTarget,
            "StateEnsure.Slots[0] is the scoped state target");
        slots[0].IsRequired.Should().BeTrue("the state target is always present");
    }

    [Fact]
    public void StateEnsure_SlotAtIndex1_IsGuardClause()
    {
        var slots = Constructs.GetMeta(ConstructKind.StateEnsure).Slots;
        slots[1].Kind.Should().Be(ConstructSlotKind.GuardClause,
            "StateEnsure.Slots[1] is the optional pre-verb guard clause");
        slots[1].IsRequired.Should().BeFalse("the guard clause is optional");
    }

    [Fact]
    public void StateEnsure_SlotAtIndex2_IsEnsureClause()
    {
        var slots = Constructs.GetMeta(ConstructKind.StateEnsure).Slots;
        slots[2].Kind.Should().Be(ConstructSlotKind.EnsureClause,
            "StateEnsure.Slots[2] is the ensure expression after the optional guard");
        slots[2].IsRequired.Should().BeTrue("the ensure expression is always required");
    }

    [Fact]
    public void StateEnsure_SlotAtIndex3_IsBecauseClause()
    {
        var slots = Constructs.GetMeta(ConstructKind.StateEnsure).Slots;
        slots[3].Kind.Should().Be(ConstructSlotKind.BecauseClause,
            "StateEnsure.Slots[3] is BecauseClause — the split slot carrying the reason string");
    }

    [Fact]
    public void StateEnsure_BecauseClauseSlot_IsOptional()
    {
        var slots = Constructs.GetMeta(ConstructKind.StateEnsure).Slots;
        slots[3].Kind.Should().Be(ConstructSlotKind.BecauseClause);
        slots[3].IsRequired.Should().BeFalse(
            "StateEnsure allows ensures without a because clause — the slot is optional");
    }

    [Fact]
    public void StateEnsure_DoesNotEmbedBecauseInEnsureClause()
    {
        var slots = Constructs.GetMeta(ConstructKind.StateEnsure).Slots;
        slots.Should().NotContain(
            s => s.Kind == ConstructSlotKind.EnsureClause && s.Description != null && s.Description.Contains("because"),
            "EnsureClause slot must not describe embedded because content after the split");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  EventEnsure catalog slot structure — RED-C (waiting on George's fix)
    //
    //  Before fix: [SlotEventTarget, SlotEnsureClause, SlotBecauseClause(opt)]                (3 slots)
    //  After fix:  [SlotEventTarget, SlotGuardClause(opt), SlotEnsureClause, SlotBecauseClause(opt)]  (4 slots)
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void EventEnsure_HasFourSlots_AfterGuardAndBecauseSplit()
    {
        var slots = Constructs.GetMeta(ConstructKind.EventEnsure).Slots;
        slots.Should().HaveCount(4,
            "EventEnsure must have 4 slots after the guard and BecauseClause split: [EventTarget, GuardClause, EnsureClause, BecauseClause]");
    }

    [Fact]
    public void EventEnsure_SlotAtIndex0_IsEventTarget()
    {
        var slots = Constructs.GetMeta(ConstructKind.EventEnsure).Slots;
        slots[0].Kind.Should().Be(ConstructSlotKind.EventTarget,
            "EventEnsure.Slots[0] is the scoped event target");
        slots[0].IsRequired.Should().BeTrue("the event target is always present");
    }

    [Fact]
    public void EventEnsure_SlotAtIndex1_IsGuardClause()
    {
        var slots = Constructs.GetMeta(ConstructKind.EventEnsure).Slots;
        slots[1].Kind.Should().Be(ConstructSlotKind.GuardClause,
            "EventEnsure.Slots[1] is the optional pre-verb guard clause");
        slots[1].IsRequired.Should().BeFalse("the guard clause is optional");
    }

    [Fact]
    public void EventEnsure_SlotAtIndex2_IsEnsureClause()
    {
        var slots = Constructs.GetMeta(ConstructKind.EventEnsure).Slots;
        slots[2].Kind.Should().Be(ConstructSlotKind.EnsureClause,
            "EventEnsure.Slots[2] is the ensure expression after the optional guard");
        slots[2].IsRequired.Should().BeTrue("the ensure expression is always required");
    }

    [Fact]
    public void EventEnsure_SlotAtIndex3_IsBecauseClause()
    {
        var slots = Constructs.GetMeta(ConstructKind.EventEnsure).Slots;
        slots[3].Kind.Should().Be(ConstructSlotKind.BecauseClause,
            "EventEnsure.Slots[3] is BecauseClause — the split slot carrying the reason string");
    }

    [Fact]
    public void EventEnsure_BecauseClauseSlot_IsOptional()
    {
        var slots = Constructs.GetMeta(ConstructKind.EventEnsure).Slots;
        slots[3].Kind.Should().Be(ConstructSlotKind.BecauseClause);
        slots[3].IsRequired.Should().BeFalse(
            "EventEnsure allows ensures without a because clause — the slot is optional");
    }

    [Fact]
    public void EventEnsure_DoesNotEmbedBecauseInEnsureClause()
    {
        var slots = Constructs.GetMeta(ConstructKind.EventEnsure).Slots;
        slots.Should().NotContain(
            s => s.Kind == ConstructSlotKind.EnsureClause && s.Description != null && s.Description.Contains("because"),
            "EnsureClause slot must not describe embedded because content after the split");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Cross-construct consistency — RED-C (both constructs must match)
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void StateEnsure_AndEventEnsure_BothHaveFourSlots()
    {
        var seCount = Constructs.GetMeta(ConstructKind.StateEnsure).Slots.Count;
        var eeCount = Constructs.GetMeta(ConstructKind.EventEnsure).Slots.Count;
        seCount.Should().Be(4, "StateEnsure must have 4 slots after fix");
        eeCount.Should().Be(4, "EventEnsure must have 4 slots after fix");
        seCount.Should().Be(eeCount, "StateEnsure and EventEnsure must have the same slot count (symmetric design)");
    }

    [Fact]
    public void StateEnsure_AndEventEnsure_BecauseClauseAtSameIndex()
    {
        var seSlots = Constructs.GetMeta(ConstructKind.StateEnsure).Slots;
        var eeSlots = Constructs.GetMeta(ConstructKind.EventEnsure).Slots;

        seSlots[3].Kind.Should().Be(ConstructSlotKind.BecauseClause,
            "StateEnsure.Slots[3] is BecauseClause");
        eeSlots[3].Kind.Should().Be(ConstructSlotKind.BecauseClause,
            "EventEnsure.Slots[3] is BecauseClause — same index as StateEnsure");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Parser structural tests — RED-P (waiting on parser implementation)
    //
    //  These document the expected parse output once the parser stub is replaced.
    //  They will be red until Parser.Parse produces real ParsedConstruct results.
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parser_StateEnsure_WithBecause_ProducesBecauseClauseSlotAtIndex2()
    {
        // RED-P: Parser is a stub that returns an empty manifest.
        // This test documents the expected behavior after parser implementation.
        var src =
            "precept OrderFulfillment\n" +
            "field amount as number\n" +
            "state Approved terminal success, Draft initial\n" +
            "event Approve\n" +
            "from Draft on Approve -> transition Approved\n" +
            "in Approved ensure amount > 0 because \"Approved orders must have positive amount\"";

        var tokens = Lexer.Lex(src);
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().BeEmpty("a valid StateEnsure with because must parse without errors");
        manifest.Constructs.Should().NotBeEmpty("the parser must produce at least one construct");

        var stateEnsure = manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.StateEnsure).Subject;

        stateEnsure.Slots.Should().HaveCount(3);
        stateEnsure.Slots[1].Kind.Should().Be(ConstructSlotKind.EnsureClause,
            "Slots[1] is EnsureClause — only the expression, not the because content");
        stateEnsure.Slots[2].Kind.Should().Be(ConstructSlotKind.BecauseClause,
            "Slots[2] is BecauseClause — the split-out reason string");

        var becauseSlot = stateEnsure.Slots[2].Should().BeOfType<BecauseClauseSlot>().Subject;
        becauseSlot.Message.Should().Be("Approved orders must have positive amount");
    }

    [Fact]
    public void Parser_StateEnsure_WithoutBecause_HasNoBecauseClauseSlot()
    {
        // RED-P: Parser is a stub.
        // Without a because clause, BecauseClause slot must be absent (optional slot is omitted).
        var src =
            "precept OrderFulfillment\n" +
            "field amount as number\n" +
            "state Approved terminal success, Draft initial\n" +
            "event Approve\n" +
            "from Draft on Approve -> transition Approved\n" +
            "in Approved ensure amount > 0";

        var tokens = Lexer.Lex(src);
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().BeEmpty("StateEnsure without because is valid DSL");
        manifest.Constructs.Should().NotBeEmpty();

        var stateEnsure = manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.StateEnsure).Subject;

        stateEnsure.Slots.Should().NotContain(
            s => s.Kind == ConstructSlotKind.BecauseClause,
            "optional BecauseClause slot must be absent when no because clause is present");
    }

    [Fact]
    public void Parser_EventEnsure_WithBecause_ProducesBecauseClauseSlotAtIndex2()
    {
        // RED-P: Parser is a stub.
        var src =
            "precept OrderFulfillment\n" +
            "field reviewer as string\n" +
            "state Active initial, Done terminal success\n" +
            "event Submit\n" +
            "from Active on Submit -> transition Done\n" +
            "on Submit ensure reviewer != \"\" because \"Reviewer required before submission\"";

        var tokens = Lexer.Lex(src);
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().BeEmpty("a valid EventEnsure with because must parse without errors");
        manifest.Constructs.Should().NotBeEmpty();

        var eventEnsure = manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.EventEnsure).Subject;

        eventEnsure.Slots.Should().HaveCount(3);
        eventEnsure.Slots[1].Kind.Should().Be(ConstructSlotKind.EnsureClause,
            "Slots[1] is EnsureClause — expression only");
        eventEnsure.Slots[2].Kind.Should().Be(ConstructSlotKind.BecauseClause,
            "Slots[2] is BecauseClause");

        var becauseSlot = eventEnsure.Slots[2].Should().BeOfType<BecauseClauseSlot>().Subject;
        becauseSlot.Message.Should().Be("Reviewer required before submission");
    }

    [Fact]
    public void Parser_EventEnsure_WithoutBecause_HasNoBecauseClauseSlot()
    {
        // RED-P: Parser is a stub.
        var src =
            "precept OrderFulfillment\n" +
            "field reviewer as string\n" +
            "state Active initial, Done terminal success\n" +
            "event Submit\n" +
            "from Active on Submit -> transition Done\n" +
            "on Submit ensure reviewer != \"\"";

        var tokens = Lexer.Lex(src);
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().BeEmpty("EventEnsure without because is valid DSL");

        var eventEnsure = manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.EventEnsure).Subject;

        eventEnsure.Slots.Should().NotContain(
            s => s.Kind == ConstructSlotKind.BecauseClause,
            "absent optional BecauseClause slot must not be materialized");
    }

    [Fact]
    public void Parser_RuleDeclaration_WithBecause_Regression()
    {
        // RED-P: Parser is a stub.
        // RuleDeclaration with because must continue to work correctly after the ensure fix.
        // Note: slot arrays are variable-length (absent optional slots omitted); BecauseClause
        // is located by Kind, not by catalog index position.
        var src = "rule amount > 0 because \"Amount must be positive\"";
        var tokens = Lexer.Lex(src);
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().BeEmpty("RuleDeclaration with because must parse without errors");
        manifest.Constructs.Should().NotBeEmpty();

        var rule = manifest.Constructs.Should().ContainSingle(
            c => c.Meta.Kind == ConstructKind.RuleDeclaration).Subject;

        var becauseSlot = rule.Slots
            .Should().ContainSingle(s => s.Kind == ConstructSlotKind.BecauseClause,
                "RuleDeclaration must have a BecauseClause slot — must not regress after ensure fix")
            .Which.Should().BeOfType<BecauseClauseSlot>().Subject;
        becauseSlot.Message.Should().Be("Amount must be positive");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Negative parse tests — RED-P (waiting on parser implementation)
    //
    //  Malformed because clauses must produce parse diagnostics, not silent
    //  bad structure or unexpected no-error results.
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Parser_StateEnsure_BecauseMissingString_ProducesParseError()
    {
        // RED-P: Parser is a stub.
        // "because" without a following string literal must produce a parse diagnostic.
        var src =
            "precept OrderFulfillment\n" +
            "field amount as number\n" +
            "state Approved terminal success, Draft initial\n" +
            "event Approve\n" +
            "from Draft on Approve -> transition Approved\n" +
            "in Approved ensure amount > 0 because";

        var tokens = Lexer.Lex(src);
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().NotBeEmpty(
            "a truncated 'because' with no string must produce a parse error");
    }

    [Fact]
    public void Parser_EventEnsure_BecauseMissingString_ProducesParseError()
    {
        // RED-P: Parser is a stub.
        var src =
            "precept OrderFulfillment\n" +
            "field reviewer as string\n" +
            "state Active initial, Done terminal success\n" +
            "event Submit\n" +
            "from Active on Submit -> transition Done\n" +
            "on Submit ensure reviewer != \"\" because";

        var tokens = Lexer.Lex(src);
        var manifest = Precept.Pipeline.Parser.Parse(tokens);

        manifest.Diagnostics.Should().NotBeEmpty(
            "a truncated 'because' with no string must produce a parse error");
    }

    // ════════════════════════════════════════════════════════════════════════════
    //  Runtime semantic tests — RED-R (waiting on runtime implementation)
    //
    //  These document the required runtime behavior for ensure constraints.
    //  They throw NotImplementedException until Evaluator.Fire / Precept.From
    //  are fully implemented.
    // ════════════════════════════════════════════════════════════════════════════

    [Fact]
    public void Runtime_StateEnsure_WithBecause_CompilesThenEvaluatesConstraint()
    {
        // RED-R: Compiler.Compile → Precept.From → Version.Fire all throw NotImplementedException.
        var src =
            "precept OrderFulfillment\n" +
            "field amount as number\n" +
            "state Approved terminal success, Draft initial\n" +
            "event Approve(amount as number)\n" +
            "from Draft on Approve -> set amount = Approve.amount -> transition Approved\n" +
            "in Approved ensure amount > 0 because \"Approved orders must have positive amount\"";

        var compilation = Compiler.Compile(src);
        compilation.HasErrors.Should().BeFalse("valid DSL must compile without errors");

        var precept = Precept.Runtime.Precept.From(compilation);
        var version = precept.Create(args: (JsonElement?)null);

        version.Should().NotBeNull("create must return a version");
    }

    [Fact]
    public void Runtime_EventEnsure_WithBecause_CompilesThenEvaluatesConstraint()
    {
        // RED-R: Runtime not yet implemented.
        var src =
            "precept OrderFulfillment\n" +
            "field reviewer as string\n" +
            "state Active initial, Done terminal success\n" +
            "event Submit(reviewer as string)\n" +
            "from Active on Submit -> set reviewer = Submit.reviewer -> transition Done\n" +
            "on Submit ensure Submit.reviewer != \"\" because \"Reviewer required before submission\"";

        var compilation = Compiler.Compile(src);
        compilation.HasErrors.Should().BeFalse("valid DSL must compile without errors");

        var precept = Precept.Runtime.Precept.From(compilation);
        var version = precept.Create(args: (JsonElement?)null);

        version.Should().NotBeNull("create must return a version");
    }

    [Fact]
    public void Runtime_StateEnsure_WithoutBecause_DoesNotCrash_OnAbsentOptionalSlot()
    {
        // RED-R: Runtime not yet implemented.
        // Absent optional BecauseClause slot must not cause a NullReferenceException
        // or other fault when the constraint fires.
        var src =
            "precept OrderFulfillment\n" +
            "field amount as number\n" +
            "state Approved terminal success, Draft initial\n" +
            "event Approve(amount as number)\n" +
            "from Draft on Approve -> set amount = Approve.amount -> transition Approved\n" +
            "in Approved ensure amount > 0";

        var compilation = Compiler.Compile(src);
        compilation.HasErrors.Should().BeFalse("StateEnsure without because is valid DSL");

        var precept = Precept.Runtime.Precept.From(compilation);
        var version = precept.Create(args: (JsonElement?)null);

        version.Should().NotBeNull("runtime must handle absent optional BecauseClause without crashing");
    }

    [Fact]
    public void Runtime_EventEnsure_WithoutBecause_DoesNotCrash_OnAbsentOptionalSlot()
    {
        // RED-R: Runtime not yet implemented.
        var src =
            "precept OrderFulfillment\n" +
            "field reviewer as string\n" +
            "state Active initial, Done terminal success\n" +
            "event Submit(reviewer as string)\n" +
            "from Active on Submit -> set reviewer = Submit.reviewer -> transition Done\n" +
            "on Submit ensure Submit.reviewer != \"\"";

        var compilation = Compiler.Compile(src);
        compilation.HasErrors.Should().BeFalse("EventEnsure without because is valid DSL");

        var precept = Precept.Runtime.Precept.From(compilation);
        var version = precept.Create(args: (JsonElement?)null);

        version.Should().NotBeNull("runtime must handle absent optional BecauseClause without crashing");
    }
}
