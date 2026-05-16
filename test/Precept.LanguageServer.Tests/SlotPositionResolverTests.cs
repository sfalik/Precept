using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.Language;
using Precept.Pipeline;
using Xunit;

namespace Precept.LanguageServer.Tests;

public class SlotPositionResolverTests
{
    [Theory]
    [MemberData(nameof(GetResolverCases))]
    public void Resolve_MapsStructuralPositionsToLegacyContexts(
        string sourceWithCursor,
        int expectedLegacyContext,
        int? expectedSlotKind = null,
        int? expectedPhase = null)
    {
        var (compilation, position) = GetCompilationAtCursor(sourceWithCursor);

        var resolved = SlotPositionResolver.Resolve(compilation, position);
        var mapped = MapToLegacyContext(compilation, position, resolved);

        mapped.Should().Be((SlotContext)expectedLegacyContext);
        if (expectedSlotKind is not null)
        {
            resolved.Should().NotBeNull();
            resolved!.Value.SlotKind.Should().Be((ConstructSlotKind)expectedSlotKind.Value);
        }

        if (expectedPhase is not null)
        {
            resolved.Should().NotBeNull();
            resolved!.Value.Phase.Should().Be((SlotPhase)expectedPhase.Value);
        }
    }

    [Theory]
    [MemberData(nameof(GetShadowCompletionCases))]
    public void Resolve_ShadowRunsAgainstExistingCompletionAnchors(string sourceWithCursor)
    {
        var (compilation, position) = GetCompilationAtCursor(sourceWithCursor);

        var legacy = SlotContextResolver.GetCursorContext(compilation, position);
        var resolved = SlotPositionResolver.Resolve(compilation, position);
        var mapped = MapToLegacyContext(compilation, position, resolved);

        mapped.Should().Be(legacy);
    }

    [Theory]
    [MemberData(nameof(GetPhaseCases))]
    public void Resolve_ReturnsExpectedSlotAndPhase(
        string sourceWithCursor,
        int expectedSlotKind,
        int expectedPhase)
    {
        var (compilation, position) = GetCompilationAtCursor(sourceWithCursor);

        var resolved = SlotPositionResolver.Resolve(compilation, position);

        resolved.Should().NotBeNull();
        resolved!.Value.SlotKind.Should().Be((ConstructSlotKind)expectedSlotKind);
        resolved.Value.Phase.Should().Be((SlotPhase)expectedPhase);
    }

    public static IEnumerable<object?[]> GetResolverCases()
    {
        yield return Case("""
            precept LoanApplication
            field Amount as ¦
            """, SlotContext.InTypePosition, ConstructSlotKind.TypeExpression, SlotPhase.AfterSlot);
        yield return Case("""
            precept SubscriptionCancellationRetention
            field MonthlyPrice as number default¦ 10
            state Active initial
            """, SlotContext.InExpression, ConstructSlotKind.ModifierList, SlotPhase.InExpression);
        yield return Case("""
            precept LoanApplication
            field Amount as number
            field DecisionNote as string optional
            state Draft initial
            in Draft omit Amount, ¦
            """, SlotContext.InFieldTarget, ConstructSlotKind.FieldTarget, SlotPhase.InList);
        yield return Case("""
            precept Test
            state off initial
            state running
            state off, ¦
            """, SlotContext.InStateDeclarationName, ConstructSlotKind.StateEntryList, SlotPhase.InList);
        yield return Case("""
            precept Test
            state off initial ¦
            state running
            """, SlotContext.InModifierPosition, ConstructSlotKind.StateEntryList, SlotPhase.AfterSlot);
        yield return Case("""
            precept Test
            state off initial
            event toggle ¦
            """, SlotContext.InModifierPosition, ConstructSlotKind.IdentifierList, SlotPhase.AfterSlot);
        yield return Case("""
            precept BuildingAccessBadgeRequest
            field BadgePrinted as boolean default false
            state Approved initial
            state Issued terminal
            event PrintBadge
            from Approved on PrintBadge
                ->¦ set BadgePrinted = true
                -> transition Issued
            """, SlotContext.InActionVerb, ConstructSlotKind.ActionChain, SlotPhase.InChain);
        yield return Case("""
            precept Test
            field count as integer
            event Increment initial

            on Increment
                -> set count = count + 1
                ->¦
            """, SlotContext.InActionVerb, ConstructSlotKind.ActionChain, SlotPhase.InChain);
        yield return Case("""
            precept UtilityOutageReport
            field CrewQueue as queue of string
            field AssignedCrew as string optional
            state Dispatching initial
            event AssignCrew
            from Dispatching on AssignCrew
                -> dequeue CrewQueue into¦ AssignedCrew
                -> no transition
            """, SlotContext.InFieldTarget, ConstructSlotKind.ActionChain, SlotPhase.LeadingToken);
        yield return Case("""
            precept BuildingAccessBadgeRequest
            field BadgePrinted as boolean default false
            state Approved initial
            state Issued terminal
            event PrintBadge
            from Approved on PrintBadge
                -> set BadgePrinted =¦ true
                -> transition Issued
            """, SlotContext.InExpression, ConstructSlotKind.ActionChain, SlotPhase.InExpression);
        yield return Case("""
            precept QueuePriority
            field ClaimQueue as queue of number by integer
            state Draft initial
            event QueueClaim
            from Draft on QueueClaim
                -> enqueue ClaimQueue 5 by¦ 1
                -> no transition
            """, SlotContext.InExpression, ConstructSlotKind.ActionChain, SlotPhase.InExpression);
        yield return Case("""
            precept ApprovalFlow
            field ApprovalChain as list of string
            state Draft initial
            event InsertReviewer
            from Draft on InsertReviewer
                -> insert ApprovalChain "QA" at¦ 0
                -> no transition
            """, SlotContext.InExpression, ConstructSlotKind.ActionChain, SlotPhase.InExpression);
        yield return Case("""
            precept TravelReimbursement
            field ApprovedTotal as number default 0 nonnegative
            state Draft initial
            state Paid terminal
            in Paid ensure ApprovedTotal¦ > 0 because "Paid reimbursements must have an approved amount"
            """, SlotContext.InExpression, ConstructSlotKind.EnsureClause, SlotPhase.InExpression);
        yield return Case("""
            precept TravelReimbursement
            field RequestedTotal as number default 0 nonnegative
            field ApprovedTotal as number default 0 nonnegative
            rule ApprovedTotal¦ <= RequestedTotal because "Approved total cannot exceed the request"
            state Draft initial
            """, SlotContext.InExpression, ConstructSlotKind.RuleExpression, SlotPhase.InExpression);
        yield return Case("""
            precept LoanApplication
            field Amount as number default 0 nonnegative
            field IsOwner as boolean default false
            state Draft initial
            in Draft when¦ IsOwner modify Amount editable
            """, SlotContext.InExpression, ConstructSlotKind.GuardClause, SlotPhase.InExpression);
        yield return Case("""
            precept BuildingAccessBadgeRequest
            state Approved initial
            event PrintBadge
            from Approved on PrintBadge
                -> no ¦
            """, SlotContext.AfterNo, ConstructSlotKind.Outcome, SlotPhase.AfterSlot);
        yield return Case("""
            precept BuildingAccessBadgeRequest
            state Approved initial
            state Issued terminal
            event PrintBadge
            from Approved on PrintBadge
                -> transition ¦
            """, SlotContext.InStateTarget, ConstructSlotKind.Outcome, SlotPhase.AfterSlot);
    }

    public static IEnumerable<object?[]> GetPhaseCases()
    {
        yield return PhaseCase("""
            precept LoanApplication
            field Amount as ¦
            """, ConstructSlotKind.TypeExpression, SlotPhase.AfterSlot);
        yield return PhaseCase("""
            precept SubscriptionCancellationRetention
            field MonthlyPrice as number default¦ 10
            state Active initial
            """, ConstructSlotKind.ModifierList, SlotPhase.InExpression);
        yield return PhaseCase("""
            precept Test
            state off initial
            state running
            state off, ¦
            """, ConstructSlotKind.StateEntryList, SlotPhase.InList);
        yield return PhaseCase("""
            precept BuildingAccessBadgeRequest
            field BadgePrinted as boolean default false
            state Approved initial
            state Issued terminal
            event PrintBadge
            from Approved on PrintBadge
                ->¦ set BadgePrinted = true
                -> transition Issued
            """, ConstructSlotKind.ActionChain, SlotPhase.InChain);
        yield return PhaseCase("""
            precept Test
            field count as integer
            event Increment initial

            on Increment
                -> set count = count + 1
                ->¦
            """, ConstructSlotKind.ActionChain, SlotPhase.InChain);
        yield return PhaseCase("""
            precept UtilityOutageReport
            field CrewQueue as queue of string
            field AssignedCrew as string optional
            state Dispatching initial
            event AssignCrew
            from Dispatching on AssignCrew
                -> dequeue CrewQueue into¦ AssignedCrew
                -> no transition
            """, ConstructSlotKind.ActionChain, SlotPhase.LeadingToken);
        yield return PhaseCase("""
            precept ApprovalFlow
            field ApprovalChain as list of string
            state Draft initial
            event InsertReviewer
            from Draft on InsertReviewer
                -> insert ApprovalChain "QA" at¦ 0
                -> no transition
            """, ConstructSlotKind.ActionChain, SlotPhase.InExpression);
        yield return PhaseCase("""
            precept TravelReimbursement
            field RequestedTotal as number default 0 nonnegative
            field ApprovedTotal as number default 0 nonnegative
            rule ApprovedTotal¦ <= RequestedTotal because "Approved total cannot exceed the request"
            state Draft initial
            """, ConstructSlotKind.RuleExpression, SlotPhase.InExpression);
    }

    public static IEnumerable<object?[]> GetShadowCompletionCases()
    {
        yield return new object[]
        {
            """
            precept LoanApplication
            field Amount as ¦
            """
        };
        yield return new object[]
        {
            """
            precept Test
            state off initial
            state running
            state off, ¦
            event start
            from off on start -> transition running
            """
        };
        yield return new object[]
        {
            """
            precept BuildingAccessBadgeRequest
            field BadgePrinted as boolean default false
            state Approved initial
            state Issued terminal
            event PrintBadge
            from Approved on PrintBadge
                ->¦ set BadgePrinted = true
                -> transition Issued
            """
        };
        yield return new object[]
        {
            """
            precept Test
            field count as integer
            event Increment initial

            on Increment
                -> set count = count + 1
                ->¦
            """
        };
        yield return new object[]
        {
            """
            precept BuildingAccessBadgeRequest
            state Approved initial
            event PrintBadge
            from Approved on PrintBadge
                -> no ¦
            """
        };
        yield return new object[]
        {
            """
            precept UtilityOutageReport
            field AssignedCrew as string optional
            field DispatchRound as number default 0 nonnegative
            field Verified as boolean default false
            state VerifiedState initial
            event RegisterCrew(CrewName as string notempty, Priority as number default 1)
            from VerifiedState on RegisterCrew when ¦AssignedCrew is set
                -> set DispatchRound = Priority
                -> no transition
            """
        };
    }

    private static object?[] Case(string sourceWithCursor, SlotContext legacyContext, ConstructSlotKind? slotKind = null, SlotPhase? phase = null) =>
    [
        sourceWithCursor,
        (int)legacyContext,
        slotKind is null ? null : (int?)slotKind.Value,
        phase is null ? null : (int?)phase.Value,
    ];

    private static object?[] PhaseCase(string sourceWithCursor, ConstructSlotKind slotKind, SlotPhase phase) =>
    [
        sourceWithCursor,
        (int)slotKind,
        (int)phase,
    ];

    private static SlotContext MapToLegacyContext(
        Compilation compilation,
        Position position,
        ResolvedSlotPosition? pos)
    {
        if (pos is null)
        {
            return SlotContext.TopLevel;
        }

        if (pos.Value.Construct == ConstructKind.PreceptHeader
            && pos.Value.SlotKind == ConstructSlotKind.IdentifierList)
        {
            return SlotContext.TopLevel;
        }

        if (pos.Value.SlotKind == ConstructSlotKind.ActionChain)
        {
            if (IsActionExpressionPosition(compilation, position))
            {
                return SlotContext.InExpression;
            }

            if (IsActionFieldTargetPosition(compilation, position))
            {
                return SlotContext.InFieldTarget;
            }
        }

        if (pos.Value.SlotKind == ConstructSlotKind.TypeExpression)
        {
            return IsTypeKeywordContext(compilation, position)
                ? SlotContext.InTypePosition
                : SlotContext.InModifierPosition;
        }

        return (pos.Value.SlotKind, pos.Value.Phase) switch
        {
            (ConstructSlotKind.StateTarget, SlotPhase.LeadingToken or SlotPhase.InList) => SlotContext.InStateTarget,
            (ConstructSlotKind.StateTarget, SlotPhase.AfterSlot) => SlotContext.AfterStateTarget,
            (ConstructSlotKind.EventTarget, SlotPhase.LeadingToken) => SlotContext.InEventTarget,
            (ConstructSlotKind.EventTarget, SlotPhase.AfterSlot) => SlotContext.AfterEventTarget,
            (ConstructSlotKind.FieldTarget, _) => SlotContext.InFieldTarget,
            (ConstructSlotKind.ActionChain, SlotPhase.InChain or SlotPhase.LeadingToken) => SlotContext.InActionVerb,
            (ConstructSlotKind.ModifierList, SlotPhase.InExpression) => SlotContext.InExpression,
            (ConstructSlotKind.ModifierList, _) => SlotContext.InModifierPosition,
            (_, SlotPhase.InExpression) => SlotContext.InExpression,
            (ConstructSlotKind.StateEntryList, SlotPhase.InList) => SlotContext.InStateDeclarationName,
            (ConstructSlotKind.StateEntryList, _) => SlotContext.InModifierPosition,
            (ConstructSlotKind.IdentifierList, SlotPhase.AfterSlot) => SlotContext.InModifierPosition,
            (ConstructSlotKind.Outcome, SlotPhase.AfterSlot) when IsAfterNoOutcomeKeyword(compilation, position) => SlotContext.AfterNo,
            (ConstructSlotKind.Outcome, SlotPhase.AfterSlot) => SlotContext.InStateTarget,
            _ => SlotContext.AfterKeyword,
        };
    }

    private static bool IsActionFieldTargetPosition(Compilation compilation, Position position)
    {
        var tokens = compilation.Tokens.Tokens;
        var tokenIndex = GetCurrentTriggerTokenIndex(tokens, position);
        if (tokenIndex < 0)
        {
            return false;
        }

        var token = tokens[tokenIndex];
        if (Actions.ByTokenKind.TryGetValue(token.Kind, out var actionMeta))
        {
            return ExpectsFieldTargetAfterActionVerb(actionMeta.SyntaxShape);
        }

        if (token.Kind != TokenKind.Into)
        {
            return false;
        }

        for (var index = tokenIndex - 1; index >= 0; index = FindPreviousSignificantToken(tokens, index - 1))
        {
            if (!Actions.ByTokenKind.TryGetValue(tokens[index].Kind, out var currentAction))
            {
                continue;
            }

            return ExpectsFieldTargetAfterInto(currentAction.SyntaxShape);
        }

        return false;
    }

    private static bool IsActionExpressionPosition(Compilation compilation, Position position)
    {
        var tokens = compilation.Tokens.Tokens;
        var tokenIndex = GetCurrentTriggerTokenIndex(tokens, position);
        return tokenIndex >= 0 && tokens[tokenIndex].Kind is TokenKind.Assign or TokenKind.By or TokenKind.At;
    }

    private static bool IsAfterNoOutcomeKeyword(Compilation compilation, Position position)
    {
        var tokens = compilation.Tokens.Tokens;
        var tokenIndex = GetCurrentTriggerTokenIndex(tokens, position);
        if (tokenIndex < 0 || tokens[tokenIndex].Kind != TokenKind.No)
        {
            return false;
        }

        return !Contains(tokens[tokenIndex].Span, position);
    }

    private static bool IsTypeKeywordContext(Compilation compilation, Position position)
    {
        var tokens = compilation.Tokens.Tokens;
        var tokenIndex = GetCurrentTriggerTokenIndex(tokens, position);
        if (tokenIndex < 0)
        {
            return false;
        }

        var kind = tokens[tokenIndex].Kind;
        if (kind == TokenKind.As)
        {
            return true;
        }

        if (kind != TokenKind.Of)
        {
            return Tokens.GetMeta(kind).Categories.Contains(TokenCategory.Type);
        }

        var previousIndex = FindPreviousSignificantToken(tokens, tokenIndex - 1);
        if (previousIndex < 0)
        {
            return false;
        }

        var lookupKind = tokens[previousIndex].Kind == TokenKind.Set
            ? TokenKind.SetType
            : tokens[previousIndex].Kind;
        if (!Types.ByToken.TryGetValue(lookupKind, out var previousType))
        {
            return false;
        }

        return previousType.Category == TypeCategory.Collection
            || previousType.Kind == TypeKind.Choice;
    }

    private static int GetCurrentTriggerTokenIndex(ImmutableArray<Token> tokens, Position position)
    {
        var tokenIndex = FindTokenAtOrBeforeCursor(tokens, position);
        if (tokenIndex < 0)
        {
            return -1;
        }

        tokenIndex = AdjustTokenIndexForBoundary(tokens, tokenIndex, position);
        return FindPreviousSignificantToken(tokens, tokenIndex);
    }

    private static bool ExpectsFieldTargetAfterActionVerb(ActionSyntaxShape syntaxShape) => syntaxShape switch
    {
        ActionSyntaxShape.AssignValue => true,
        ActionSyntaxShape.CollectionValue => true,
        ActionSyntaxShape.CollectionInto => true,
        ActionSyntaxShape.FieldOnly => true,
        ActionSyntaxShape.CollectionValueBy => true,
        ActionSyntaxShape.InsertAt => true,
        ActionSyntaxShape.RemoveAtIndex => true,
        ActionSyntaxShape.PutKeyValue => true,
        ActionSyntaxShape.CollectionIntoBy => true,
        _ => false,
    };

    private static bool ExpectsFieldTargetAfterInto(ActionSyntaxShape syntaxShape) => syntaxShape switch
    {
        ActionSyntaxShape.CollectionInto => true,
        ActionSyntaxShape.CollectionIntoBy => true,
        _ => false,
    };

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

    private static int FindTokenAtOrBeforeCursor(ImmutableArray<Token> tokens, Position position)
    {
        var candidate = -1;
        for (var index = 0; index < tokens.Length; index++)
        {
            var token = tokens[index];
            if (IsBefore(position, token.Span))
            {
                break;
            }

            candidate = index;
            if (Contains(token.Span, position))
            {
                break;
            }
        }

        return candidate;
    }

    private static int AdjustTokenIndexForBoundary(ImmutableArray<Token> tokens, int tokenIndex, Position position)
    {
        if (tokenIndex < 0 || tokenIndex >= tokens.Length)
        {
            return tokenIndex;
        }

        return StartsAt(tokens[tokenIndex].Span, position)
            ? tokenIndex - 1
            : tokenIndex;
    }

    private static int FindPreviousSignificantToken(ImmutableArray<Token> tokens, int startIndex)
    {
        for (var index = startIndex; index >= 0; index--)
        {
            var token = tokens[index];
            var categories = Tokens.GetMeta(token.Kind).Categories;
            if (!categories.Contains(TokenCategory.Structural))
            {
                return index;
            }
        }

        return -1;
    }

    private static bool StartsAt(SourceSpan span, Position position) =>
        span.StartLine == position.Line + 1
        && span.StartColumn == position.Character + 1;

    private static bool IsBefore(Position position, SourceSpan span)
    {
        var line = position.Line + 1;
        var character = position.Character + 1;
        if (line != span.StartLine)
        {
            return line < span.StartLine;
        }

        return character < span.StartColumn;
    }

    private static bool Contains(SourceSpan span, Position position)
    {
        var line = position.Line + 1;
        var character = position.Character + 1;
        if (line < span.StartLine || line > span.EndLine)
        {
            return false;
        }

        if (span.StartLine == span.EndLine)
        {
            return character >= span.StartColumn && character < span.EndColumn;
        }

        if (line == span.StartLine)
        {
            return character >= span.StartColumn;
        }

        if (line == span.EndLine)
        {
            return character < span.EndColumn;
        }

        return true;
    }

    private const string CursorMarker = "¦";
}
