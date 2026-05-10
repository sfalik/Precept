using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.Pipeline;

namespace Precept.LanguageServer;

internal enum SlotContext
{
    TopLevel,
    AfterKeyword,
    InTypePosition,
    InModifierPosition,
    InStateTarget,
    InEventTarget,
    InFieldTarget,
    InActionVerb,
    InExpression,
    InArgDefault,
}

internal static class SlotContextResolver
{
    internal static SlotContext GetCursorContext(Compilation compilation, Position position)
    {
        var tokens = compilation.Tokens.Tokens;
        var tokenIndex = FindTokenAtOrBeforeCursor(tokens, position);
        if (tokenIndex < 0)
        {
            return SlotContext.TopLevel;
        }

        tokenIndex = FindPreviousSignificantToken(tokens, tokenIndex);
        if (tokenIndex < 0)
        {
            return SlotContext.TopLevel;
        }

        var token = tokens[tokenIndex];
        var meta = Precept.Language.Tokens.GetMeta(token.Kind);

        if (token.Kind == Precept.Language.TokenKind.As)
        {
            return SlotContext.InTypePosition;
        }

        if (token.Kind is Precept.Language.TokenKind.Modify or Precept.Language.TokenKind.Omit)
        {
            return SlotContext.InFieldTarget;
        }

        if (token.Kind == Precept.Language.TokenKind.On)
        {
            return SlotContext.InEventTarget;
        }

        if (token.Kind is Precept.Language.TokenKind.In or Precept.Language.TokenKind.From or Precept.Language.TokenKind.To
            && HasSlotContext(compilation, token, Precept.Language.ConstructSlotKind.StateTarget))
        {
            return SlotContext.InStateTarget;
        }

        if (token.Kind == Precept.Language.TokenKind.When
            && HasSlotContext(compilation, token, Precept.Language.ConstructSlotKind.EventTarget))
        {
            return SlotContext.InEventTarget;
        }

        if (IsModifierToken(token.Kind, meta.Categories))
        {
            return SlotContext.InModifierPosition;
        }

        if (meta.Categories.Contains(Precept.Language.TokenCategory.Type)
            && GetRelevantConstruct(compilation, token)?.Meta.ModifierDomain == Precept.Language.ModifierDomain.Field)
        {
            return SlotContext.InModifierPosition;
        }

        if (meta.Categories.Contains(Precept.Language.TokenCategory.Declaration)
            || meta.Categories.Contains(Precept.Language.TokenCategory.Preposition)
            || meta.Categories.Contains(Precept.Language.TokenCategory.Control))
        {
            return SlotContext.AfterKeyword;
        }

        return SlotContext.TopLevel;
    }

    /// <summary>
    /// Returns true when a <see cref="Precept.Language.TokenKind.Set"/> token occupies a
    /// <see cref="Precept.Language.ConstructSlotKind.TypeExpression"/> slot, meaning it is the
    /// type keyword (set collection) rather than the action keyword.
    /// Determined by checking whether the previous non-structural token is <c>as</c> or <c>of</c>,
    /// the two prepositions that introduce type-expression positions in the grammar.
    /// </summary>
    internal static bool IsSetInTypePosition(Compilation compilation, Precept.Language.Token token)
    {
        if (token.Kind != Precept.Language.TokenKind.Set)
        {
            return false;
        }

        var tokens = compilation.Tokens.Tokens;

        // Find index of this token by source position (line/column are always populated by the lexer)
        var setIndex = -1;
        for (var i = 0; i < tokens.Length; i++)
        {
            if (tokens[i].Span.StartLine == token.Span.StartLine && tokens[i].Span.StartColumn == token.Span.StartColumn)
            {
                setIndex = i;
                break;
            }
        }

        if (setIndex <= 0)
        {
            return false;
        }

        // Walk backward to find the preceding non-structural token.
        // 'set' after 'as' or 'of' is in a ConstructSlotKind.TypeExpression position.
        for (var i = setIndex - 1; i >= 0; i--)
        {
            var prevMeta = Precept.Language.Tokens.GetMeta(tokens[i].Kind);
            if (prevMeta.Categories.Contains(Precept.Language.TokenCategory.Structural))
            {
                continue;
            }

            return tokens[i].Kind is Precept.Language.TokenKind.As or Precept.Language.TokenKind.Of;
        }

        return false;
    }

    internal static Precept.Pipeline.ParsedConstruct? GetEnclosingConstruct(Compilation compilation, Position position)
    {
        var tokens = compilation.Tokens.Tokens;
        var tokenIndex = FindTokenAtOrBeforeCursor(tokens, position);
        if (tokenIndex < 0)
        {
            return null;
        }

        tokenIndex = FindPreviousSignificantToken(tokens, tokenIndex);
        return tokenIndex < 0 ? null : GetRelevantConstruct(compilation, tokens[tokenIndex]);
    }

    private static bool HasSlotContext(
        Compilation compilation,
        Precept.Language.Token token,
        Precept.Language.ConstructSlotKind slotKind)
    {
        var construct = GetRelevantConstruct(compilation, token);
        if (construct is not null)
        {
            return construct.Meta.Slots.Any(slot => slot.Kind == slotKind);
        }

        return slotKind switch
        {
            Precept.Language.ConstructSlotKind.StateTarget => token.Kind is Precept.Language.TokenKind.In or Precept.Language.TokenKind.From or Precept.Language.TokenKind.To,
            Precept.Language.ConstructSlotKind.EventTarget => token.Kind is Precept.Language.TokenKind.On or Precept.Language.TokenKind.When,
            _ => false,
        };
    }

    private static bool IsModifierToken(
        Precept.Language.TokenKind tokenKind,
        IReadOnlyList<Precept.Language.TokenCategory> categories)
    {
        if (categories.Contains(Precept.Language.TokenCategory.StateModifier)
            || categories.Contains(Precept.Language.TokenCategory.Constraint))
        {
            return true;
        }

        return Precept.Language.Modifiers.All.Any(modifier => modifier.Token.Kind == tokenKind);
    }

    private static Precept.Pipeline.ParsedConstruct? GetRelevantConstruct(Compilation compilation, Precept.Language.Token token)
    {
        Precept.Pipeline.ParsedConstruct? best = null;
        foreach (var construct in compilation.ConstructManifest.Constructs)
        {
            if (!StartsBeforeOrAt(construct.Span, token.Span.StartLine, token.Span.StartColumn))
            {
                continue;
            }

            if (!Contains(construct.Span, token.Span.StartLine, token.Span.StartColumn)
                && construct.Span.EndLine != token.Span.EndLine)
            {
                continue;
            }

            if (best is null || construct.Span.Offset >= best.Span.Offset)
            {
                best = construct;
            }
        }

        return best;
    }

    private static int FindTokenAtOrBeforeCursor(ImmutableArray<Precept.Language.Token> tokens, Position position)
    {
        var candidate = -1;
        foreach (var (token, index) in tokens.Select((token, index) => (token, index)))
        {
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

    private static int FindPreviousSignificantToken(ImmutableArray<Precept.Language.Token> tokens, int startIndex)
    {
        for (var index = startIndex; index >= 0; index--)
        {
            var token = tokens[index];
            var categories = Precept.Language.Tokens.GetMeta(token.Kind).Categories;
            if (!categories.Contains(Precept.Language.TokenCategory.Structural))
            {
                return index;
            }
        }

        return -1;
    }

    private static bool StartsBeforeOrAt(SourceSpan span, int line, int column)
    {
        if (line != span.StartLine)
        {
            return line > span.StartLine;
        }

        return column >= span.StartColumn;
    }

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

    private static bool Contains(SourceSpan span, Position position) =>
        Contains(span, position.Line + 1, position.Character + 1);

    private static bool Contains(SourceSpan span, int line, int character)
    {
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
}
