using System;
using System.Collections.Generic;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.LanguageServer.Tests;

public class SlotPositionResolverTests
{
    [Theory]
    [MemberData(nameof(GetResolveCases))]
    public void Resolve_ReturnsExpectedSlotAndPhase(
        string sourceWithCursor,
        ConstructSlotKind expectedSlotKind,
        int expectedPhase)
    {
        var (compilation, position) = GetCompilationAtCursor(sourceWithCursor);

        var resolved = SlotPositionResolver.Resolve(compilation, position);

        resolved.Should().NotBeNull();
        resolved!.Value.SlotKind.Should().Be(expectedSlotKind);
        resolved.Value.Phase.Should().Be((SlotPhase)expectedPhase);
    }

    [Fact]
    public void Resolve_DefaultTypedConstantHole_ReturnsModifierExpressionSlot()
    {
        var (compilation, position) = GetCompilationAtCursor("""
            precept TravelReimbursement
            field SubmittedOn as date default '2037-08-09'
            field ApprovedOn as date default '¦'
            state Draft initial terminal
            """);

        var resolved = SlotPositionResolver.Resolve(compilation, position);

        resolved.Should().NotBeNull();
        resolved!.Value.SlotKind.Should().Be(ConstructSlotKind.ModifierList);
        resolved.Value.Phase.Should().Be(SlotPhase.InExpression);
    }

    public static IEnumerable<object?[]> GetResolveCases()
    {
        yield return Case("""
            precept LoanApplication
            field Amount as ¦
            """, ConstructSlotKind.TypeExpression, SlotPhase.AfterSlot);
        yield return Case("""
            precept SubscriptionCancellationRetention
            field MonthlyPrice as number default¦ 10
            state Active initial
            """, ConstructSlotKind.ModifierList, SlotPhase.InExpression);
        yield return Case("""
            precept Test
            state off initial
            state running
            state off, ¦
            """, ConstructSlotKind.StateEntryList, SlotPhase.InList);
        yield return Case("""
            precept BuildingAccessBadgeRequest
            field BadgePrinted as boolean default false
            state Approved initial
            state Issued terminal
            event PrintBadge
            from Approved on PrintBadge
                ->¦ set BadgePrinted = true
                -> transition Issued
            """, ConstructSlotKind.ActionChain, SlotPhase.InChain);
        yield return Case("""
            precept Test
            field count as integer
            event Increment initial

            on Increment
                -> set count = count + 1
                ->¦
            """, ConstructSlotKind.ActionChain, SlotPhase.InChain);
        yield return Case("""
            precept UtilityOutageReport
            field CrewQueue as queue of string
            field AssignedCrew as string optional
            state Dispatching initial
            event AssignCrew
            from Dispatching on AssignCrew
                -> dequeue CrewQueue into¦ AssignedCrew
                -> no transition
            """, ConstructSlotKind.ActionChain, SlotPhase.LeadingToken);
        yield return Case("""
            precept ApprovalFlow
            field ApprovalChain as list of string
            state Draft initial
            event InsertReviewer
            from Draft on InsertReviewer
                -> insert ApprovalChain "QA" at¦ 0
                -> no transition
            """, ConstructSlotKind.ActionChain, SlotPhase.InExpression);
        yield return Case("""
            precept TravelReimbursement
            field ApprovedTotal as number default 0 nonnegative
            state Draft initial
            state Paid terminal
            in Paid ensure ApprovedTotal¦ > 0 because "Paid reimbursements must have an approved amount"
            """, ConstructSlotKind.EnsureClause, SlotPhase.InExpression);
        yield return Case("""
            precept TravelReimbursement
            field RequestedTotal as number default 0 nonnegative
            field ApprovedTotal as number default 0 nonnegative
            rule ApprovedTotal¦ <= RequestedTotal because "Approved total cannot exceed the request"
            state Draft initial
            """, ConstructSlotKind.RuleExpression, SlotPhase.InExpression);
        yield return Case("""
            precept LoanApplication
            field Amount as number default 0 nonnegative
            field IsOwner as boolean default false
            state Draft initial
            in Draft when¦ IsOwner modify Amount editable
            """, ConstructSlotKind.GuardClause, SlotPhase.InExpression);
        yield return Case("""
            precept BuildingAccessBadgeRequest
            state Approved initial
            event PrintBadge
            from Approved ¦
            """, ConstructSlotKind.EventTarget, SlotPhase.AfterSlot);
        yield return Case("""
            precept BuildingAccessBadgeRequest
            state Approved initial
            state Issued terminal
            event PrintBadge
            from Approved on PrintBadge ¦
            """, ConstructSlotKind.EventTarget, SlotPhase.AfterSlot);
        yield return Case("""
            precept BuildingAccessBadgeRequest
            state Approved initial
            event PrintBadge
            from Approved on PrintBadge
                -> no ¦
            """, ConstructSlotKind.Outcome, SlotPhase.AfterSlot);
        yield return Case("""
            precept BuildingAccessBadgeRequest
            state Approved initial
            state Issued terminal
            event PrintBadge
            from Approved on PrintBadge
                -> transition ¦
            """, ConstructSlotKind.Outcome, SlotPhase.AfterSlot);
    }

    private static object?[] Case(string sourceWithCursor, ConstructSlotKind slotKind, SlotPhase phase) =>
    [
        sourceWithCursor,
        slotKind,
        (int)phase,
    ];

    private static (Compilation Compilation, Position Position) GetCompilationAtCursor(string sourceWithCursor)
    {
        var position = GetCursorPosition(sourceWithCursor);
        var source = sourceWithCursor.Replace(CursorMarker, string.Empty, StringComparison.Ordinal);
        return (Precept.Compiler.Compile(source), position);
    }

    private static Position GetCursorPosition(string sourceWithCursor)
    {
        var markerIndex = sourceWithCursor.IndexOf(CursorMarker, StringComparison.Ordinal);
        markerIndex.Should().BeGreaterThanOrEqualTo(0, "each test source must include a cursor marker");

        var line = 0;
        var character = 0;
        for (var index = 0; index < markerIndex; index++)
        {
            if (sourceWithCursor[index] == '\n')
            {
                line++;
                character = 0;
            }
            else if (sourceWithCursor[index] != '\r')
            {
                character++;
            }
        }

        return new Position(line, character);
    }

    private const string CursorMarker = "¦";
}
