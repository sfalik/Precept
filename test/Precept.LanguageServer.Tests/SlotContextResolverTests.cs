using System;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Xunit;

namespace Precept.LanguageServer.Tests;

public class SlotContextResolverTests
{
    [Fact]
    public void GetCursorContext_ActionChainAfterArrow_ReturnsInActionVerb()
    {
        var context = GetCursorContext("""
            precept BuildingAccessBadgeRequest
            field BadgePrinted as boolean default false
            state Approved initial
            state Issued terminal
            event PrintBadge
            from Approved on PrintBadge
                ->¦ set BadgePrinted = true
                -> transition Issued
            """);

        context.Should().Be(SlotContext.InActionVerb);
    }

    [Theory]
    [MemberData(nameof(ActionFieldTargetSources))]
    public void GetCursorContext_ActionChainAfterVerbOrInto_ReturnsInFieldTarget(string source)
    {
        var context = GetCursorContext(source);

        context.Should().Be(SlotContext.InFieldTarget);
    }

    [Theory]
    [MemberData(nameof(ActionExpressionSources))]
    public void GetCursorContext_ActionChainAfterAssignByOrAt_ReturnsInExpression(string source)
    {
        var context = GetCursorContext(source);

        context.Should().Be(SlotContext.InExpression);
    }

    [Theory]
    [MemberData(nameof(ExpressionSlotSources))]
    public void GetCursorContext_GuardComputeEnsureAndRuleExpressions_ReturnInExpression(string source)
    {
        var context = GetCursorContext(source);

        context.Should().Be(SlotContext.InExpression);
    }

    [Fact]
    public void GetCursorContext_EventArgDefault_ReturnsInArgDefault()
    {
        var context = GetCursorContext("""
            precept SubscriptionCancellationRetention
            field MonthlyPrice as number default 0 nonnegative
            state Active initial
            event MakeSaveOffer(DiscountPercent as number default¦ 10)
            """);

        context.Should().Be(SlotContext.InArgDefault);
    }

    [Fact]
    public void GetCursorContext_FieldDeclarationAfterName_ReturnsAfterValueName()
    {
        var context = GetCursorContext("""
            precept SubscriptionCancellationRetention
            field MonthlyPrice¦ 
            state Active initial
            """);

        context.Should().Be(SlotContext.AfterValueName);
    }

    [Fact]
    public void GetCursorContext_EventArgAfterName_ReturnsAfterValueName()
    {
        var context = GetCursorContext("""
            precept SubscriptionCancellationRetention
            state Active initial
            event MakeSaveOffer(DiscountPercent¦ )
            """);

        context.Should().Be(SlotContext.AfterValueName);
    }

    [Fact]
    public void GetCursorContext_CollectionInnerTypeAfterOf_ReturnsInTypePosition()
    {
        var context = GetCursorContext("""
            precept BuildingAccessBadgeRequest
            field RequestedFloors as set of¦ number
            state Draft initial
            """);

        context.Should().Be(SlotContext.InTypePosition);
    }

    [Fact]
    public void GetCursorContext_ChoiceElementTypeAfterOf_ReturnsInTypePosition()
    {
        var context = GetCursorContext("""
            precept Ticket
            field Priority as choice of¦ string("Low","High")
            state Open initial
            """);

        context.Should().Be(SlotContext.InTypePosition);
    }

    [Fact]
    public void GetCursorContext_QuantityDimensionQualifierAfterOf_DoesNotReturnInTypePosition()
    {
        // 'quantity of' is a qualifier preposition (expects a typed constant like 'mass'),
        // NOT a collection element-type position. Type keyword completions (bag, boolean, …) must not appear.
        var context = GetCursorContext("""
            precept Shipment
            field Weight as quantity of¦
            state Pending initial
            """);

        context.Should().NotBe(SlotContext.InTypePosition);
    }

    [Fact]
    public void GetCursorContext_FieldModifierDefaultValue_ReturnsInExpression()
    {
        var context = GetCursorContext("""
            precept SubscriptionCancellationRetention
            field MonthlyPrice as number default¦ 0 nonnegative
            state Active initial
            """);

        context.Should().Be(SlotContext.InExpression);
    }

    [Fact]
    public void GetCursorContext_FromClauseStateListContinuation_ReturnsInStateTarget()
    {
        var context = GetCursorContext("""
            precept Test
            state off initial
            state running
            from off, ¦
            """);

        context.Should().Be(SlotContext.InStateTarget);
    }

    [Fact]
    public void GetCursorContext_AccessModePreVerbWhenGuard_RoutesGuardExpressionBeforeModifyFieldTarget()
    {
        var guardContext = GetCursorContext("""
            precept LoanApplication
            field Amount as number default 0 nonnegative
            field IsOwner as boolean default false
            state Draft initial
            in Draft when¦ IsOwner modify Amount editable
            """);

        var fieldTargetContext = GetCursorContext("""
            precept LoanApplication
            field Amount as number default 0 nonnegative
            field IsOwner as boolean default false
            state Draft initial
            in Draft when IsOwner modify¦ Amount editable
            """);

        guardContext.Should().Be(SlotContext.InExpression);
        fieldTargetContext.Should().Be(SlotContext.InFieldTarget);
    }

    public static TheoryData<string> ActionFieldTargetSources =>
    [
        """
        precept BuildingAccessBadgeRequest
        field BadgePrinted as boolean default false
        state Approved initial
        state Issued terminal
        event PrintBadge
        from Approved on PrintBadge
            -> set¦ BadgePrinted = true
            -> transition Issued
        """,
        """
        precept UtilityOutageReport
        field CrewQueue as queue of string
        field AssignedCrew as string optional
        state Dispatching initial
        event AssignCrew
        from Dispatching on AssignCrew
            -> dequeue CrewQueue into¦ AssignedCrew
            -> no transition
        """,
    ];

    public static TheoryData<string> ActionExpressionSources =>
    [
        """
        precept BuildingAccessBadgeRequest
        field BadgePrinted as boolean default false
        state Approved initial
        state Issued terminal
        event PrintBadge
        from Approved on PrintBadge
            -> set BadgePrinted =¦ true
            -> transition Issued
        """,
        """
        precept QueuePriority
        field ClaimQueue as queue of number by integer
        state Draft initial
        event QueueClaim
        from Draft on QueueClaim
            -> enqueue ClaimQueue 5 by¦ 1
            -> no transition
        """,
        """
        precept ApprovalFlow
        field ApprovalChain as list of string
        state Draft initial
        event InsertReviewer
        from Draft on InsertReviewer
            -> insert ApprovalChain "QA" at¦ 0
            -> no transition
        """,
    ];

    public static TheoryData<string> ExpressionSlotSources =>
    [
        """
        precept LoanApplication
        field RequestedAmount as number default 0 nonnegative
        state Draft initial
        state Submitted terminal
        event Submit
        from Draft on Submit when RequestedAmount¦ > 0
            -> transition Submitted
        """,
        """
        precept TravelReimbursement
        field LodgingTotal as decimal default 0
        field MealsTotal as decimal default 0
        field MileageTotal as decimal default 0 maxplaces 2
        field RequestedTotal as number nonnegative <- LodgingTotal¦ + MealsTotal + MileageTotal
        state Draft initial
        """,
        """
        precept TravelReimbursement
        field ApprovedTotal as number default 0 nonnegative
        state Draft initial
        state Paid terminal
        in Paid ensure ApprovedTotal¦ > 0 because "Paid reimbursements must have an approved amount"
        """,
        """
        precept TravelReimbursement
        field RequestedTotal as number default 0 nonnegative
        field ApprovedTotal as number default 0 nonnegative
        rule ApprovedTotal¦ <= RequestedTotal because "Approved total cannot exceed the request"
        state Draft initial
        """,
    ];

    private static SlotContext GetCursorContext(string sourceWithCursor)
    {
        var position = GetCursorPosition(sourceWithCursor);
        var source = sourceWithCursor.Replace(CursorMarker, string.Empty, StringComparison.Ordinal);
        var compilation = Precept.Compiler.Compile(source);

        return SlotContextResolver.GetCursorContext(compilation, position);
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
