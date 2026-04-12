using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept;
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
        completions.Should().Contain("nullable", "any-order: nullable is valid after default");
        completions.Should().NotContain("default", "already present in the declaration");
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
        completions.Should().Contain("nullable", "any-order: nullable is valid after default");
        completions.Should().NotContain("default", "already present in the declaration");
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

        completions.Should().Contain("positive");
        completions.Should().Contain("min");
        completions.Should().Contain("max");
        completions.Should().Contain("nullable", "any-order: nullable is valid after constraints");
        completions.Should().Contain("default", "any-order: default is valid after constraints");
        completions.Should().NotContain("nonnegative", "already present in the declaration");
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
        completions.Should().Contain("nullable", "any-order: nullable is valid after default");
        completions.Should().NotContain("default", "already present in the declaration");
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

    // ════════════════════════════════════════════════════════════════════
    // Pre-precept scope: blank / new files without a precept declaration
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Completions_BlankFile_OnlyOffersPrecept()
    {
        const string text = """
            $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().ContainSingle()
            .Which.Should().Be("precept");
    }

    [Fact]
    public void Completions_FileWithCommentOnly_OnlyOffersPrecept()
    {
        const string text = """
            # This is a new precept file
            $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().ContainSingle()
            .Which.Should().Be("precept");
    }

    [Fact]
    public void Completions_TypingPreceptName_SuppressesAll()
    {
        const string text = """
            precept My$$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().BeEmpty(because: "user is naming the precept — no suggestions");
    }

    // ════════════════════════════════════════════════════════════════════
    // Gap fixes: pipeline continuation, on + events, top-level, comments,
    //            scalar types, event arg types
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Completions_AfterCompletedSetExpression_SuggestsArrow()
    {
        const string text = """
            precept M
            field Count as number default 0
            state A initial
            state B
            event Go
            from A on Go -> set Count = 42 $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("->",
            because: "after a completed set expression, the pipeline arrow should be offered");
    }

    [Fact]
    public void Completions_AfterCompletedAddExpression_SuggestsArrow()
    {
        const string text = """
            precept M
            field Tags as set of string
            state A initial
            state B
            event Go
            from A on Go -> add Tags "important" $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("->",
            because: "after a completed add expression, the pipeline arrow should be offered");
    }

    [Fact]
    public void Completions_OnAtLineStart_SuggestsEventNames()
    {
        const string text = """
            precept M
            state A initial
            event Submit
            event Cancel
            on $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("Submit");
        completions.Should().Contain("Cancel");
        completions.Should().NotContain("set", because: "action keywords should not appear in event name position");
    }

    [Fact]
    public void Completions_TopLevelBlankLine_OnlySuggestsDeclarationKeywords()
    {
        const string text = """
            precept M
            state A initial
            event Go
            $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        // Top-level declarations should be present
        completions.Should().Contain("field");
        completions.Should().Contain("state");
        completions.Should().Contain("event");
        completions.Should().Contain("from");
        completions.Should().Contain("invariant");

        // Action/outcome/grammar keywords should NOT be at top level
        completions.Should().NotContain("set", because: "set is an action keyword, not a top-level declaration");
        completions.Should().NotContain("transition", because: "transition is an outcome keyword, not a top-level declaration");
        completions.Should().NotContain("nullable", because: "nullable is a grammar modifier, not a top-level declaration");
        completions.Should().NotContain("nonnegative", because: "constraint keywords should not appear at top level");
    }

    [Fact]
    public void Completions_CommentLine_SuppressesAll()
    {
        const string text = """
            precept M
            state A initial
            # This is a comment $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().BeEmpty(because: "comment lines should suppress all completions");
    }

    [Fact]
    public void Completions_EventArgAsPosition_SuggestsAllScalarTypes()
    {
        const string text = """
            precept M
            state A initial
            event Go with Amount as $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("string");
        completions.Should().Contain("number");
        completions.Should().Contain("boolean");
        completions.Should().Contain("integer");
        completions.Should().Contain("decimal");
        completions.Should().Contain(c => c.StartsWith("choice", StringComparison.Ordinal),
            because: "choice type should be offered for event args");
    }

    [Fact]
    public void Completions_EventArgIntegerType_SuggestsNumberConstraints()
    {
        const string text = """
            precept M
            state A initial
            event Go with Count as integer $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("nonnegative");
        completions.Should().Contain("positive");
        completions.Should().Contain("min");
        completions.Should().Contain("max");
        completions.Should().Contain("nullable");
        completions.Should().Contain(",");
    }

    [Fact]
    public void Completions_EventArgDecimalType_SuggestsDecimalConstraints()
    {
        const string text = """
            precept M
            state A initial
            event Go with Amount as decimal $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("nonnegative");
        completions.Should().Contain("positive");
        completions.Should().Contain("min");
        completions.Should().Contain("max");
        completions.Should().Contain(c => c.StartsWith("maxplaces", StringComparison.Ordinal),
            because: "maxplaces must be offered for decimal event args");
        completions.Should().Contain("nullable");
        completions.Should().Contain(",");
    }

    [Fact]
    public void Completions_CollectionOfPosition_SuggestsAllScalarTypes()
    {
        const string text = """
            precept M
            state A initial
            field Items as set of $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("string");
        completions.Should().Contain("number");
        completions.Should().Contain("boolean");
        completions.Should().Contain("integer");
        completions.Should().Contain("decimal");
        completions.Should().Contain(c => c.StartsWith("choice", StringComparison.Ordinal),
            because: "choice type should be offered as collection inner type");
    }

    // ════════════════════════════════════════════════════════════════════
    // Conditional Expression Completions (if / then / else)
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Completions_SetAssignmentExpr_SuggestsIfSnippet()
    {
        const string text = """
            precept M
            field Status as string default ""
            state A initial
            event Go
            from A on Go -> set Status = $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain(c => c.StartsWith("if", StringComparison.Ordinal),
            because: "conditional expression must be offered in set-assignment RHS");
    }

    [Fact]
    public void Completions_InvariantExpr_SuggestsIfSnippet()
    {
        const string text = """
            precept M
            field Balance as number default 0
            state A initial
            event Go
            invariant $$
            from A on Go -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain(c => c.StartsWith("if", StringComparison.Ordinal),
            because: "conditional expression must be offered in invariant body");
    }

    [Fact]
    public void Completions_GuardExpr_SuggestsIfSnippet()
    {
        const string text = """
            precept M
            field Balance as number default 0
            state A initial
            event Go with Amount as number
            from A on Go when $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain(c => c.StartsWith("if", StringComparison.Ordinal),
            because: "conditional expression must be offered in guard position");
    }

    [Fact]
    public void Completions_StatementStart_DoesNotSuggestIfThenElse()
    {
        const string text = """
            precept M
            field Balance as number default 0
            state A initial
            event Go
            $$
            from A on Go -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().NotContain("if", because: "if is an expression keyword, not a statement keyword");
        completions.Should().NotContain("then", because: "then is a continuation keyword, not a statement keyword");
        completions.Should().NotContain("else", because: "else is a continuation keyword, not a statement keyword");
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

    // ════════════════════════════════════════════════════════════════════
    // Completions ↔ Token Catalog Drift Tests
    // ════════════════════════════════════════════════════════════════════
    //
    // These tests enforce that the static completion lists in PreceptAnalyzer
    // stay in sync with the PreceptToken enum. When someone adds a new token
    // with [TokenCategory(Type)] or [TokenCategory(Constraint)] etc., these
    // tests fail until the corresponding completion list is updated.

    [Fact]
    public void AllTypeTokens_AppearInTypeItems()
    {
        var typeSymbols = PreceptTokenMeta.GetByCategory(TokenCategory.Type)
            .Select(t => PreceptTokenMeta.GetSymbol(t))
            .Where(s => s is not null)
            .ToHashSet(StringComparer.Ordinal);

        var completionLabels = PreceptAnalyzer.TypeItems
            .Select(i => i.Label)
            .ToHashSet(StringComparer.Ordinal);

        // Snippet labels like "choice(...)" won't match the raw symbol "choice",
        // so also check whether the label starts with the symbol.
        var missing = typeSymbols
            .Where(sym => !completionLabels.Contains(sym!)
                && !completionLabels.Any(label => label.StartsWith(sym!, StringComparison.Ordinal)))
            .ToList();

        missing.Should().BeEmpty(
            "every token with [TokenCategory(Type)] must appear in PreceptAnalyzer.TypeItems — "
            + "add new type keywords there when extending the language");
    }

    [Fact]
    public void AllConstraintTokens_AppearInAtLeastOneConstraintList()
    {
        var constraintSymbols = PreceptTokenMeta.GetByCategory(TokenCategory.Constraint)
            .Select(t => PreceptTokenMeta.GetSymbol(t))
            .Where(s => s is not null)
            .ToHashSet(StringComparer.Ordinal);

        var allConstraintLabels = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in PreceptAnalyzer.NumberConstraintItems) allConstraintLabels.Add(item.Label);
        foreach (var item in PreceptAnalyzer.StringConstraintItems) allConstraintLabels.Add(item.Label);
        foreach (var item in PreceptAnalyzer.CollectionConstraintItems) allConstraintLabels.Add(item.Label);
        foreach (var item in PreceptAnalyzer.DecimalConstraintItems) allConstraintLabels.Add(item.Label);
        foreach (var item in PreceptAnalyzer.ChoiceConstraintItems) allConstraintLabels.Add(item.Label);

        // Snippet labels like "maxplaces N" start with the symbol "maxplaces"
        var missing = constraintSymbols
            .Where(sym => !allConstraintLabels.Contains(sym!)
                && !allConstraintLabels.Any(label => label.StartsWith(sym!, StringComparison.Ordinal)))
            .ToList();

        missing.Should().BeEmpty(
            "every token with [TokenCategory(Constraint)] must appear in at least one constraint "
            + "completion list (Number/String/Collection/Decimal/Choice) — add new constraint "
            + "keywords to the appropriate list when extending the language");
    }

    [Fact]
    public void AllActionTokens_AppearInArrowItems()
    {
        var actionSymbols = PreceptTokenMeta.GetByCategory(TokenCategory.Action)
            .Select(t => PreceptTokenMeta.GetSymbol(t))
            .Where(s => s is not null)
            .ToHashSet(StringComparer.Ordinal);

        var arrowLabels = PreceptAnalyzer.ArrowItems
            .Select(i => i.Label)
            .ToHashSet(StringComparer.Ordinal);

        var missing = actionSymbols
            .Where(sym => !arrowLabels.Contains(sym!))
            .ToList();

        missing.Should().BeEmpty(
            "every token with [TokenCategory(Action)] must appear in PreceptAnalyzer.ArrowItems — "
            + "add new action keywords there when extending the language");
    }

    [Fact]
    public void AllOutcomeTokens_AppearInArrowItems()
    {
        var outcomeSymbols = PreceptTokenMeta.GetByCategory(TokenCategory.Outcome)
            .Select(t => PreceptTokenMeta.GetSymbol(t))
            .Where(s => s is not null)
            .ToHashSet(StringComparer.Ordinal);

        var arrowLabels = PreceptAnalyzer.ArrowItems
            .Select(i => i.Label)
            .ToHashSet(StringComparer.Ordinal);

        // "no" appears as the composite "no transition" in ArrowItems
        var missing = outcomeSymbols
            .Where(sym => !arrowLabels.Contains(sym!)
                && !arrowLabels.Any(label => label.StartsWith(sym!, StringComparison.Ordinal)))
            .ToList();

        missing.Should().BeEmpty(
            "every token with [TokenCategory(Outcome)] must appear in PreceptAnalyzer.ArrowItems — "
            + "add new outcome keywords there when extending the language");
    }

    [Fact]
    public void AllLiteralTokens_AppearInLiteralItems()
    {
        var literalSymbols = PreceptTokenMeta.GetByCategory(TokenCategory.Literal)
            .Select(t => PreceptTokenMeta.GetSymbol(t))
            .Where(s => s is not null)
            .ToHashSet(StringComparer.Ordinal);

        var literalLabels = PreceptAnalyzer.LiteralItems
            .Select(i => i.Label)
            .ToHashSet(StringComparer.Ordinal);

        var missing = literalSymbols
            .Where(sym => !literalLabels.Contains(sym!))
            .ToList();

        missing.Should().BeEmpty(
            "every token with [TokenCategory(Literal)] must appear in PreceptAnalyzer.LiteralItems — "
            + "add new literal keywords there when extending the language");
    }

    [Fact]
    public void AllKeywordOperatorTokens_AppearInExpressionOperatorItems()
    {
        // Keyword operators (alphabetic symbols with Operator category) must be offered
        // in expression contexts. Non-alphabetic operators (==, >=, etc.) are also there
        // but this test focuses on the ones that could drift as new keyword operators are added.
        var keywordOperatorSymbols = PreceptTokenMeta.GetByCategory(TokenCategory.Operator)
            .Select(t => PreceptTokenMeta.GetSymbol(t))
            .Where(s => s is not null && s.All(char.IsLetter))
            .ToHashSet(StringComparer.Ordinal);

        var operatorLabels = PreceptAnalyzer.ExpressionOperatorItems
            .Select(i => i.Label)
            .ToHashSet(StringComparer.Ordinal);

        var missing = keywordOperatorSymbols
            .Where(sym => !operatorLabels.Contains(sym!))
            .ToList();

        missing.Should().BeEmpty(
            "every keyword operator (alphabetic symbol with [TokenCategory(Operator)]) must appear "
            + "in PreceptAnalyzer.ExpressionOperatorItems — add new keyword operators there "
            + "when extending the language");
    }

    [Fact]
    public void AllScalarTypeTokens_AppearInScalarTypeItems()
    {
        // ScalarTypeItems is used after "of" (collection inner type) and for event arg types.
        // It must include every scalar type the parser accepts — excluding collection-only types
        // (set, queue, stack) which are not valid as event arg types or collection inner types.
        var collectionOnlySymbols = new HashSet<string>(StringComparer.Ordinal) { "set", "queue", "stack" };

        var scalarTypeSymbols = PreceptTokenMeta.GetByCategory(TokenCategory.Type)
            .Select(t => PreceptTokenMeta.GetSymbol(t))
            .Where(s => s is not null && !collectionOnlySymbols.Contains(s))
            .ToHashSet(StringComparer.Ordinal);

        var scalarLabels = PreceptAnalyzer.ScalarTypeItems
            .Select(i => i.Label)
            .ToHashSet(StringComparer.Ordinal);

        // Snippet labels like "choice(...)" won't match the raw symbol "choice",
        // so also check whether the label starts with the symbol.
        var missing = scalarTypeSymbols
            .Where(sym => !scalarLabels.Contains(sym!)
                && !scalarLabels.Any(label => label.StartsWith(sym!, StringComparison.Ordinal)))
            .ToList();

        missing.Should().BeEmpty(
            "every scalar type token must appear in PreceptAnalyzer.ScalarTypeItems — "
            + "this list is used for event arg types and collection inner types. "
            + "Add new scalar type keywords there when extending the language");
    }

    // ════════════════════════════════════════════════════════════════════
    // Slice 9c: when-guard completion contexts
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Completions_AfterInvariantExpression_OffersWhen()
    {
        const string text = """
            precept M
            field X as number default 0
            invariant X > 0 $$
            state A initial
            event Go
            from A on Go -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("when");
    }

    [Fact]
    public void Completions_AfterStateAssertExpression_OffersWhen()
    {
        const string text = """
            precept M
            field X as number default 0
            state Open initial
            in Open assert X > 0 $$
            event Go
            from Open on Go -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("when");
    }

    [Fact]
    public void Completions_AfterEventAssertExpression_OffersWhen()
    {
        const string text = """
            precept M
            state A initial
            event Submit with Amount as number
            on Submit assert Amount > 0 $$
            from A on Submit -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("when");
    }

    [Fact]
    public void Completions_InEditBlock_AfterInState_OffersWhen()
    {
        const string text = """
            precept M
            field X as number default 0
            state Open initial
            in Open $$
            event Go
            from Open on Go -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("when");
    }

    [Fact]
    public void Completions_AfterWhenKeyword_InInvariant_OffersFields()
    {
        const string text = """
            precept M
            field X as number default 0
            field Active as boolean default true
            invariant X > 0 when $$
            state A initial
            event Go
            from A on Go -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("Active");
        completions.Should().Contain("X");
    }

    [Fact]
    public void Completions_AfterWhenGuard_InInvariant_OffersBecause()
    {
        const string text = """
            precept M
            field X as number default 0
            field Active as boolean default true
            invariant X > 0 when Active $$
            state A initial
            event Go
            from A on Go -> no transition
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("because");
    }

    // ════════════════════════════════════════════════════════════════════
    // COMPLETIONS — Any-order field modifier suggestions
    // ════════════════════════════════════════════════════════════════════

    [Fact]
    public void Completions_FieldAfterConstraintThenDefault_OffersNullableAndRemainingConstraints()
    {
        const string text = """
            precept M
            field Amount as number nonnegative default 5 $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("nullable", "any-order: nullable valid after default");
        completions.Should().Contain("positive");
        completions.Should().Contain("min");
        completions.Should().Contain("max");
        completions.Should().NotContain("default", "already present");
        completions.Should().NotContain("nonnegative", "already present");
    }

    [Fact]
    public void Completions_FieldNullableThenConstraint_OffersDefaultAndRemainingConstraints()
    {
        const string text = """
            precept M
            field Notes as string nullable notempty $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("default", "any-order: default valid after constraints");
        completions.Should().Contain("minlength");
        completions.Should().Contain("maxlength");
        completions.Should().NotContain("nullable", "already present");
        completions.Should().NotContain("notempty", "already present");
    }

    [Fact]
    public void Completions_FieldAllModifiersExhausted_OffersNothing()
    {
        const string text = """
            precept M
            field Active as boolean nullable default false $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().BeEmpty("boolean has no constraints; nullable and default already present");
    }

    [Fact]
    public void Completions_EventArgAfterConstraint_OffersNullableDefaultAndComma()
    {
        const string text = """
            precept M
            state A initial
            event Submit with Amount as number nonnegative $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("nullable");
        completions.Should().Contain("default");
        completions.Should().Contain(",");
        completions.Should().Contain("positive");
        completions.Should().NotContain("nonnegative", "already present");
    }

    [Fact]
    public void Completions_ChoiceFieldAfterOrdered_OffersNullableDefaultNotOrdered()
    {
        const string text = """
            precept M
            field Priority as choice("Low", "Medium", "High") ordered $$
            """;

        var (code, position) = ExtractPosition(text);
        var completions = AnalyzeCompletions(code, position).Select(static item => item.Label).ToArray();

        completions.Should().Contain("nullable");
        completions.Should().Contain("default");
        completions.Should().NotContain("ordered", "already present in the declaration");
    }
}