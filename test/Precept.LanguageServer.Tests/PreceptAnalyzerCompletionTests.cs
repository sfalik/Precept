using System;
using System.Linq;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.LanguageServer;
using Xunit;

namespace Precept.LanguageServer.Tests;

public class PreceptAnalyzerCompletionTests
{
    [Fact]
    public void Completions_InvariantScope_ExcludesEventArgs()
    {
        const string text = """
            precept M
            field Balance as number default 0
            state A initial
            event Deposit with Amount as number
            invariant Bal$$
            from A on Deposit -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("Balance");
        completions.Should().NotContain("Deposit.Amount");
        completions.Should().NotContain("Amount");
    }

    [Fact]
    public void Completions_EventAssertScope_IncludesBareArgsAndExcludesFields()
    {
        const string text = """
            precept M
            field Balance as number default 0
            state A initial
            event Deposit with Amount as number
            on Deposit assert $$
            from A on Deposit -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("Amount");
        completions.Should().Contain("Deposit.Amount");
        completions.Should().NotContain("Balance");
    }

    [Fact]
    public void Completions_EventDeclaration_DoesNotSuggestInitial()
    {
        const string text = """
            precept M
            state A initial
            event Deposit $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("with");
        completions.Should().NotContain("initial");
    }

    [Fact]
    public void Completions_FromStateClause_SuggestsOnAssertAndArrow()
    {
        const string text = """
            precept M
            state A initial
            state B
            event Go
            from A $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("on");
        completions.Should().Contain("assert");
        completions.Should().Contain("->");
    }

    [Fact]
    public void Completions_CollectionMembers_AreFilteredByCollectionKind()
    {
        const string text = """
            precept M
            field Floors as set of number
            field Queue as queue of number
            state A initial
            event Go
            from A on Go when Floors.$$ -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var setMembers = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        setMembers.Should().Contain("Floors.count");
        setMembers.Should().Contain("Floors.min");
        setMembers.Should().Contain("Floors.max");
        setMembers.Should().NotContain("Floors.peek");

        const string queueText = """
            precept M
            field Queue as queue of number
            state A initial
            event Go
            from A on Go when Queue.$$ -> no transition
            """;

        var (queueCode, queuePosition) = ExtractPosition(queueText);
        var queueMembers = AnalyzeCompletions(queueCode, queuePosition).Select(static item => item.Label).ToArray();
        queueMembers.Should().Contain("Queue.count");
        queueMembers.Should().Contain("Queue.peek");
        queueMembers.Should().NotContain("Queue.min");
        queueMembers.Should().NotContain("Queue.max");
    }

    private static CompletionItem[] AnalyzeCompletions(string text, Position position)
    {
        var analyzer = new PreceptAnalyzer();
        var uri = DocumentUri.From($"file:///tmp/{Guid.NewGuid():N}.precept");
        analyzer.SetDocumentText(uri, text);
        return analyzer.GetCompletions(uri, position).ToArray();
    }

    [Fact]
    public void Completions_ArrowInTransitionRow_SuggestsActionsNotEvents()
    {
        const string text = """
            precept M
            field Balance as number default 0
            state A initial
            event Deposit with Amount as number
            from A on Deposit -> $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("set");
        completions.Should().Contain("transition");
        completions.Should().Contain("no transition");
        completions.Should().Contain("reject");
        completions.Should().NotContain("Deposit", "events should not appear after ->");
    }

    [Fact]
    public void Completions_AfterTransitionKeywordInArrow_SuggestsStateNames()
    {
        const string text = """
            precept M
            state A initial
            state B
            event Go
            from A on Go -> transition $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("A");
        completions.Should().Contain("B");
        completions.Should().NotContain("set", "action keywords should not appear after 'transition'");
        completions.Should().NotContain("Go", "events should not appear after 'transition'");
    }

    [Fact]
    public void Completions_ArrowSetMidLine_SuggestsFieldNames()
    {
        const string text = """
            precept M
            field Balance as number default 0
            state A initial
            event Deposit with Amount as number
            from A on Deposit -> set $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("Balance");
        completions.Should().NotContain("Deposit", "events should not appear after 'set'");
    }

    [Fact]
    public void Completions_ArrowSetAssignmentMidLine_SuggestsExpressions()
    {
        const string text = """
            precept M
            field Balance as number default 0
            state A initial
            event Deposit with Amount as number
            from A on Deposit -> set Balance = $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("Balance");
        completions.Should().Contain("Deposit.Amount");
    }

    [Fact]
    public void Completions_ArrowSetAssignmentMidLine_UsesTypeContextToFilterIncompatibleSymbols()
    {
        const string text = """
            precept M
            field Value as number default 0
            field RetryCount as number nullable
            field Notes as string default ""
            state A initial
            state B
            event Go with Count as number, Reason as string
            from A on Go when RetryCount != null -> set Value = $$ -> transition B
            from A on Go -> reject "blocked"
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("Value");
        completions.Should().Contain("RetryCount");
        completions.Should().Contain("Go.Count");
        completions.Should().NotContain("Notes");
        completions.Should().NotContain("Go.Reason");
        completions.Should().NotContain("true");
        completions.Should().NotContain("false");
        completions.Should().NotContain("null");
    }

    [Fact]
    public void Completions_ArrowRejectMidLine_SuggestsStringSnippet()
    {
        const string text = """
            precept M
            state A initial
            event Go
            from A on Go -> reject $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("reject reason");
        completions.Should().NotContain("Go");
    }

    [Fact]
    public void Completions_ArrowAddMidLine_SuggestsCollectionFields()
    {
        const string text = """
            precept M
            field Items as set of string
            state A initial
            event AddItem with Name as string
            from A on AddItem -> add $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("Items");
    }

    [Fact]
    public void Completions_ArrowAddValueMidLine_UsesTypeContextToFilterIncompatibleSymbols()
    {
        const string text = """
            precept M
            field Names as set of string
            field Name as string default ""
            field Count as number default 0
            field Scores as set of number
            state A initial
            state B
            event AddName with RequestedName as string, Amount as number
            from A on AddName -> add Names $$ -> transition B
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("Name");
        completions.Should().Contain("AddName.RequestedName");
        completions.Should().NotContain("Count");
        completions.Should().NotContain("AddName.Amount");
        completions.Should().NotContain("Scores.count");
        completions.Should().NotContain("true");
        completions.Should().NotContain("false");
        completions.Should().NotContain("null");
    }

    [Fact]
    public void Completions_ArrowNoMidLine_SuggestsTransition()
    {
        const string text = """
            precept M
            state A initial
            event Go
            from A on Go -> no $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("transition");
        completions.Should().HaveCount(1);
    }

    [Fact]
    public void Completions_AfterFieldName_SuggestsAs()
    {
        const string text = """
            precept M
            state A initial
            event Go
            field Approvers $$
            from A on Go -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("as");
        completions.Should().Contain(",");
        completions.Should().HaveCount(2);
    }

    [Fact]
    public void Completions_AfterFieldAs_SuggestsTypes()
    {
        const string text = """
            precept M
            state A initial
            event Go
            field Approvers as $$
            from A on Go -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("string");
        completions.Should().Contain("number");
        completions.Should().Contain("boolean");
        completions.Should().Contain("set");
        completions.Should().Contain("queue");
        completions.Should().Contain("stack");
    }

    [Fact]
    public void Completions_AfterEventArgName_SuggestsAs()
    {
        const string text = """
            precept M
            state A initial
            event Submit with Comment $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("as");
        completions.Should().HaveCount(1);
    }

    [Fact]
    public void Completions_AfterEventArgAs_SuggestsScalarTypes()
    {
        const string text = """
            precept M
            state A initial
            event Submit with Comment as $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("string");
        completions.Should().Contain("number");
        completions.Should().Contain("boolean");
        completions.Should().NotContain("set");
    }

    [Fact]
    public void Completions_AfterEventArgScalarType_SuggestsNullableDefaultAndComma()
    {
        const string text = """
            precept M
            state A initial
            event Submit with Comment as string $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("nullable");
        completions.Should().Contain("default");
        completions.Should().Contain(",");
    }

    [Fact]
    public void Completions_AfterEventArgNullable_SuggestsDefaultAndComma()
    {
        const string text = """
            precept M
            state A initial
            event Submit with Comment as string nullable $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("default");
        completions.Should().Contain(",");
    }

    [Fact]
    public void Completions_AfterEventArgDefault_SuggestsComma()
    {
        const string text = """
            precept M
            state A initial
            event Submit with Comment as string default "" $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain(",");
        completions.Should().HaveCount(1);
    }

    [Fact]
    public void Completions_AfterEventArgComma_StartsNextArgumentNameWithoutKeywordNoise()
    {
        const string text = """
            precept M
            state A initial
            event Submit with Comment as string, $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().NotContain("as");
        completions.Should().NotContain("with");
        completions.Should().NotContain("field");
    }

    [Fact]
    public void Completions_AfterFieldScalarType_SuggestsNullableDefaultAndConstraints()
    {
        const string text = """
            precept M
            state A initial
            event Go
            field Name as string $$
            from A on Go -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("nullable");
        completions.Should().Contain("default");
        completions.Should().Contain("notempty");
        completions.Should().Contain("minlength");
        completions.Should().Contain("maxlength");
        completions.Should().NotContain("nonnegative", "number constraints should not appear on string fields");
        completions.Should().HaveCount(5);
    }

    [Fact]
    public void Completions_AfterFieldNullable_SuggestsDefaultAndConstraints()
    {
        const string text = """
            precept M
            state A initial
            event Go
            field Name as string nullable $$
            from A on Go -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("default");
        completions.Should().Contain("notempty");
        completions.Should().Contain("minlength");
        completions.Should().Contain("maxlength");
        completions.Should().NotContain("nullable", "nullable already set");
        completions.Should().HaveCount(4);
    }

    [Fact]
    public void Completions_AfterInvariantExpression_SuggestsBecause()
    {
        const string text = """
            precept M
            field Balance as number default 0
            invariant Balance >= 0 $$
            state A initial
            event Go
            from A on Go -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("because");
    }

    [Fact]
    public void Completions_AfterEventAssertExpression_SuggestsBecause()
    {
        const string text = """
            precept M
            state A initial
            event Submit with Comment as string
            on Submit assert Comment != "" $$
            from A on Submit -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("because");
    }

    [Fact]
    public void Completions_AfterStateAssertExpression_SuggestsBecause()
    {
        const string text = """
            precept M
            field Balance as number default 0
            state Open initial
            in Open assert Balance >= 0 $$
            event Go
            from Open on Go -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("because");
    }

    [Fact]
    public void Completions_AfterGuardExpression_SuggestsArrow()
    {
        const string text = """
            precept M
            field Balance as number default 0
            state Open initial
            event Go
            from Open on Go when Balance >= 0 $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("->");
    }

    [Fact]
    public void Completions_InEditClause_IncludeCollectionFields()
    {
        const string text = """
            precept M
            field Title as string nullable
            field Approvers as set of string
            state Open initial
            in Open edit $$
            event Go
            from Open on Go -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("Title");
        completions.Should().Contain("Approvers");
    }

    [Fact]
    public void Completions_AfterCollectionKind_SuggestsOf()
    {
        const string text = """
            precept M
            state A initial
            event Go
            field Items as set $$
            from A on Go -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("of");
        completions.Should().HaveCount(1);
    }

    [Fact]
    public void Completions_AfterCollectionOf_SuggestsScalarTypes()
    {
        const string text = """
            precept M
            state A initial
            event Go
            field Items as set of $$
            from A on Go -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("string");
        completions.Should().Contain("number");
        completions.Should().Contain("boolean");
        completions.Should().NotContain("set", "collection types not valid as inner type");
    }

    [Fact]
    public void Completions_DequeueInto_FiltersToMatchingFieldType()
    {
        const string text = """
            precept M
            field Name as string default ""
            field Count as number default 0
            field Active as boolean default false
            field Log as queue of string
            state A initial
            event Process
            from A on Process -> dequeue Log into $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("Name");
        completions.Should().NotContain("Count");
        completions.Should().NotContain("Active");
    }

    [Fact]
    public void Completions_PopInto_FiltersToMatchingFieldType()
    {
        const string text = """
            precept M
            field Name as string default ""
            field Score as number default 0
            field Steps as stack of number
            state A initial
            event Undo
            from A on Undo -> pop Steps into $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("Score");
        completions.Should().NotContain("Name");
    }

    [Fact]
    public void Completions_DequeueInto_NullableFieldAcceptsNonNullableInnerType()
    {
        const string text = """
            precept M
            field Result as string nullable
            field Exact as string default ""
            field Queue as queue of string
            state A initial
            event Process
            from A on Process -> dequeue Queue into $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("Result");
        completions.Should().Contain("Exact");
    }

    // ── Constraint zone completions (issue #13) ──────────────────────────

    [Fact]
    public void Completions_NumberFieldAfterType_SuggestsNullableDefaultAndNumberConstraints()
    {
        const string text = """
            precept M
            field Amount as number $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("nullable");
        completions.Should().Contain("default");
        completions.Should().Contain("nonnegative");
        completions.Should().Contain("positive");
        completions.Should().Contain("min");
        completions.Should().Contain("max");
        completions.Should().NotContain("notempty", "string constraints should not appear on number fields");
        completions.Should().NotContain("mincount", "collection constraints should not appear on number fields");
    }

    [Fact]
    public void Completions_NumberFieldAfterDefault_SuggestsNumberConstraints()
    {
        const string text = """
            precept M
            field Amount as number default 0 $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("nonnegative");
        completions.Should().Contain("positive");
        completions.Should().Contain("min");
        completions.Should().Contain("max");
        completions.Should().NotContain("nullable");
        completions.Should().NotContain("default");
        completions.Should().NotContain("notempty");
    }

    [Fact]
    public void Completions_NumberFieldAfterConstraint_SuggestsMoreNumberConstraints()
    {
        const string text = """
            precept M
            field Amount as number nonnegative $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("nonnegative");
        completions.Should().Contain("positive");
        completions.Should().Contain("min");
        completions.Should().Contain("max");
        completions.Should().NotContain("notempty");
    }

    [Fact]
    public void Completions_StringFieldAfterDefault_SuggestsStringConstraints()
    {
        const string text = """
            precept M
            field Name as string default "" $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("notempty");
        completions.Should().Contain("minlength");
        completions.Should().Contain("maxlength");
        completions.Should().NotContain("nullable");
        completions.Should().NotContain("default");
        completions.Should().NotContain("nonnegative");
    }

    [Fact]
    public void Completions_CollectionFieldAfterType_SuggestsCollectionConstraints()
    {
        const string text = """
            precept M
            field Tags as set of string $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("notempty");
        completions.Should().Contain("mincount");
        completions.Should().Contain("maxcount");
        completions.Should().NotContain("nonnegative");
        completions.Should().NotContain("minlength");
    }

    [Fact]
    public void Completions_EventArgNumberType_SuggestsNumberConstraints()
    {
        const string text = """
            precept M
            state A initial
            event Score with Value as number $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("nullable");
        completions.Should().Contain("default");
        completions.Should().Contain("nonnegative");
        completions.Should().Contain("positive");
        completions.Should().Contain("min");
        completions.Should().Contain("max");
        completions.Should().Contain(",");
        completions.Should().NotContain("notempty");
    }

    [Fact]
    public void Completions_EventArgStringType_SuggestsStringConstraints()
    {
        const string text = """
            precept M
            state A initial
            event Submit with Comment as string $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("nullable");
        completions.Should().Contain("default");
        completions.Should().Contain("notempty");
        completions.Should().Contain("minlength");
        completions.Should().Contain("maxlength");
        completions.Should().Contain(",");
        completions.Should().NotContain("nonnegative");
    }

    // ── Choice field completions (issue #25) ──────────────────────────

    [Fact]
    public void Completions_ChoiceFieldAfterType_SuggestsNullableAndOrdered()
    {
        const string text = """
            precept M
            field Priority as choice("Low","High") $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("nullable");
        completions.Should().Contain("ordered",
            because: "'ordered' is the choice-specific constraint and must be offered after choice(...)");
        completions.Should().NotContain("nonnegative", "numeric constraints must not appear on choice fields");
        completions.Should().NotContain("notempty", "string constraints must not appear on choice fields");
    }

    [Fact]
    public void Completions_ChoiceFieldSetAssignment_OffersChoiceMemberLiterals()
    {
        const string text = """
            precept M
            field Status as choice("Draft","Active","Closed") default "Draft"
            state A initial
            event Go
            from A on Go -> set Status = $$ -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("\"Draft\"",
            because: "choice member literals must be offered as completions in set-assignment value position");
        completions.Should().Contain("\"Active\"");
        completions.Should().Contain("\"Closed\"");
    }

    [Fact]
    public void Completions_GuardExpression_SuggestsKeywordLogicalOperators()
    {
        const string text = """
            precept M
            field Active as boolean default true
            state A initial
            event Go
            from A on Go when $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("and");
        completions.Should().Contain("or");
        completions.Should().Contain("not");
        completions.Should().NotContain("&&", "symbolic logical operators were replaced by keyword forms");
        completions.Should().NotContain("||", "symbolic logical operators were replaced by keyword forms");
        completions.Should().NotContain("!", "symbolic logical operators were replaced by keyword forms");
    }

    [Fact]
    public void Completions_StringFieldDotTrigger_SuggestsLength()
    {
        const string text = """
            precept M
            field Name as string default ""
            state A initial
            event Go
            from A on Go when Name.$$ -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("Name.length",
            because: ".length must be offered after string-typed field identifiers");
    }

    [Fact]
    public void Completions_NonStringFieldDotTrigger_DoesNotSuggestLength()
    {
        const string text = """
            precept M
            field Count as number default 0
            field Active as boolean default true
            state A initial
            event Go
            from A on Go when Count.$$ -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().NotContain("Count.length",
            because: ".length must not be offered after non-string identifiers");
    }

    private static (string text, Position position) ExtractPosition(string textWithMarker)
    {
        var index = textWithMarker.IndexOf("$$", StringComparison.Ordinal);
        index.Should().BeGreaterThanOrEqualTo(0);

        var text = textWithMarker.Replace("$$", string.Empty, StringComparison.Ordinal);
        var prefix = textWithMarker[..index];
        var line = prefix.Count(static ch => ch == '\n');
        var lastNewLine = prefix.LastIndexOf('\n');
        var character = lastNewLine >= 0 ? prefix.Length - lastNewLine - 1 : prefix.Length;
        return (text, new Position(line, character));
    }
}