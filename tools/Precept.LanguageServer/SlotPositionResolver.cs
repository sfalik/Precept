using System.Collections.Immutable;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.Language;
using Precept.Pipeline;

namespace Precept.LanguageServer;

/// <summary>
/// Catalog-driven cursor position resolver.
/// Determines which construct/slot/phase the cursor occupies
/// by reading ConstructMeta.Slots metadata.
/// </summary>
internal static class SlotPositionResolver
{
    internal static ResolvedSlotPosition? Resolve(Compilation compilation, Position position)
    {
        var tokens = compilation.Tokens.Tokens;
        var tokenIndex = FindTokenAtOrBeforeCursor(tokens, position);
        if (tokenIndex < 0)
        {
            return null;
        }

        tokenIndex = AdjustTokenIndexForBoundary(tokens, tokenIndex, position);
        tokenIndex = FindPreviousSignificantToken(tokens, tokenIndex);
        if (tokenIndex < 0)
        {
            return null;
        }

        var construct = CursorSemanticResolver.GetEnclosingConstruct(compilation, position);
        if (construct is null)
        {
            return TryResolveImplicitChainPosition(compilation, tokens, tokenIndex, out var implicitChain)
                ? implicitChain
                : null;
        }

        if (!TryGetOwningSlot(construct, position, out var owner))
        {
            return TryResolveImplicitChainPosition(compilation, tokens, tokenIndex, out var implicitChain)
                ? implicitChain
                : null;
        }

        var phase = DeterminePhase(construct, owner, tokens, tokenIndex, position);
        return new ResolvedSlotPosition(construct.Meta.Kind, owner.Meta.Kind, phase);
    }

    private static bool TryResolveImplicitChainPosition(
        Compilation compilation,
        ImmutableArray<Token> tokens,
        int tokenIndex,
        out ResolvedSlotPosition? position)
    {
        position = null;
        if (tokenIndex < 0
            || tokenIndex >= tokens.Length
            || tokens[tokenIndex].Kind != TokenKind.Arrow)
        {
            return false;
        }

        var currentArrow = tokens[tokenIndex];
        var sawPriorActionVerb = false;
        var significantTokensScanned = 0;

        for (var index = FindPreviousSignificantToken(tokens, tokenIndex - 1);
             index >= 0 && significantTokensScanned < 20;
             index = FindPreviousSignificantToken(tokens, index - 1), significantTokensScanned++)
        {
            var candidate = tokens[index];
            if (candidate.Span.StartLine == currentArrow.Span.StartLine)
            {
                return false;
            }

            if (Actions.ByTokenKind.ContainsKey(candidate.Kind))
            {
                sawPriorActionVerb = true;
                continue;
            }

            if (candidate.Kind != TokenKind.Arrow
                || !sawPriorActionVerb
                || candidate.Span.StartLine >= currentArrow.Span.StartLine
                || candidate.Span.StartColumn < currentArrow.Span.StartColumn)
            {
                continue;
            }

            var construct = CursorSemanticResolver.GetEnclosingConstruct(
                compilation,
                new Position(candidate.Span.StartLine - 1, candidate.Span.StartColumn - 1));
            if (construct is null
                || !construct.Meta.Slots.Any(slot => slot.Kind == ConstructSlotKind.ActionChain))
            {
                return false;
            }

            position = new ResolvedSlotPosition(
                construct.Meta.Kind,
                ConstructSlotKind.ActionChain,
                SlotPhase.InChain);
            return true;
        }

        return false;
    }

    private static bool TryGetOwningSlot(ParsedConstruct construct, Position position, out OwnedSlot owner)
    {
        var presentSlots = construct.Meta.Slots
            .Select(meta => (Meta: meta, Value: construct.GetSlot(meta.Kind)))
            .Where(entry => entry.Value is not null && entry.Value.Span != SourceSpan.Missing)
            .ToArray();

        if (presentSlots.Length == 0)
        {
            owner = default;
            return false;
        }

        var first = presentSlots[0];
        if (IsBefore(position, first.Value!.Span))
        {
            owner = new OwnedSlot(first.Meta, first.Value, SlotRegion.Before);
            return true;
        }

        for (var index = 0; index < presentSlots.Length; index++)
        {
            var current = presentSlots[index];
            var currentSpan = current.Value!.Span;
            if (Contains(currentSpan, position))
            {
                owner = new OwnedSlot(current.Meta, current.Value, SlotRegion.Inside);
                return true;
            }

            var next = index + 1 < presentSlots.Length ? presentSlots[index + 1] : ((ConstructSlot Meta, SlotValue? Value)?)null;
            if (EndsBeforeOrAt(currentSpan, position)
                && (next is null || IsBefore(position, next.Value.Value!.Span)))
            {
                owner = new OwnedSlot(current.Meta, current.Value, SlotRegion.After);
                return true;
            }
        }

        var last = presentSlots[^1];
        if (EndsBeforeOrAt(last.Value!.Span, position))
        {
            owner = new OwnedSlot(last.Meta, last.Value, SlotRegion.After);
            return true;
        }

        owner = default;
        return false;
    }

    private static SlotPhase DeterminePhase(
        ParsedConstruct construct,
        OwnedSlot owner,
        ImmutableArray<Token> tokens,
        int tokenIndex,
        Position position)
    {
        if (tokenIndex >= 0)
        {
            var previousToken = tokens[tokenIndex];
            if (owner.Meta.IsList && previousToken.Kind == TokenKind.Comma)
            {
                return SlotPhase.InList;
            }

            if (owner.Meta.IsChainable
                && owner.Meta.ItemIntroducerToken is TokenKind itemIntroducer
                && previousToken.Kind == itemIntroducer)
            {
                return SlotPhase.InChain;
            }

            if (IsExpressionPhase(construct, owner, previousToken, position))
            {
                return SlotPhase.InExpression;
            }

            if (owner.Region == SlotRegion.After
                || owner.Meta.TerminationTokens?.Contains(previousToken.Kind) == true)
            {
                return SlotPhase.AfterSlot;
            }
        }

        return owner.Meta.Vocabulary == SlotVocabulary.Expression
            ? SlotPhase.InExpression
            : SlotPhase.LeadingToken;
    }

    private static bool IsExpressionPhase(
        ParsedConstruct construct,
        OwnedSlot owner,
        Token previousToken,
        Position position)
    {
        if (owner.Meta.Vocabulary == SlotVocabulary.Expression)
        {
            return true;
        }

        if (owner.Meta.Kind == ConstructSlotKind.ActionChain)
        {
            // TODO: derive from ActionSyntaxSlot.Vocabulary annotations once that catalog gap is filled
            return previousToken.Kind is TokenKind.Assign or TokenKind.By or TokenKind.At;
        }

        if (owner.Meta.Kind == ConstructSlotKind.ModifierList)
        {
            if (Modifiers.ByValueToken.TryGetValue(previousToken.Kind, out var modifierMeta)
                && modifierMeta.HasValue)
            {
                return true;
            }

            var modifierSlot = construct.GetSlot<ModifierListSlot>(ConstructSlotKind.ModifierList);
            return modifierSlot is not null
                && modifierSlot.Modifiers.Any(modifier => modifier.Value is not null && Contains(modifier.Span, position));
        }

        if (owner.Meta.Kind == ConstructSlotKind.ArgumentList)
        {
            if (Modifiers.ByValueToken.TryGetValue(previousToken.Kind, out var modifierMeta)
                && modifierMeta.Kind == ModifierKind.Default)
            {
                return true;
            }

            var argumentSlot = construct.GetSlot<ArgumentListSlot>(ConstructSlotKind.ArgumentList);
            return argumentSlot is not null
                && argumentSlot.Args.Any(arg => arg.ParsedModifiers.Any(modifier =>
                    modifier.Kind == ModifierKind.Default
                    && (modifier.Value is not null && Contains(modifier.Span, position))));
        }

        return false;
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

    private static int AdjustTokenIndexForBoundary(
        ImmutableArray<Token> tokens,
        int tokenIndex,
        Position position)
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

    private static bool EndsBeforeOrAt(SourceSpan span, Position position)
    {
        var cursorLine = position.Line + 1;
        var cursorCharacter = position.Character + 1;
        return span.EndLine < cursorLine
            || (span.EndLine == cursorLine && span.EndColumn <= cursorCharacter);
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

    private readonly record struct OwnedSlot(
        ConstructSlot Meta,
        SlotValue Value,
        SlotRegion Region);

    private enum SlotRegion
    {
        Before,
        Inside,
        After,
    }
}

internal readonly record struct ResolvedSlotPosition(
    ConstructKind     Construct,
    ConstructSlotKind SlotKind,
    SlotPhase         Phase);

internal enum SlotPhase
{
    LeadingToken,
    InList,
    InChain,
    AfterSlot,
    InExpression,
}

