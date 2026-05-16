using System;
using System.Collections.Immutable;
using System.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.Language;
using Precept.Pipeline;

namespace Precept.LanguageServer;

internal readonly record struct ActiveCallContext(
    string Name,
    bool IsAccessor,
    int ActiveParameter,
    TypeKind? ReceiverType);

internal static class CallContextResolver
{
    internal static bool TryFindActiveCall(Compilation compilation, Position position, out ActiveCallContext call)
    {
        var tokens = compilation.Tokens.Tokens;
        var tokenIndex = FindTokenAtOrBeforeCursor(tokens, position);
        if (tokenIndex < 0)
        {
            call = default;
            return false;
        }

        tokenIndex = AdjustTokenIndexForBoundary(tokens, tokenIndex, position);

        var activeParameter = 0;
        var nesting = 0;
        var openParenIndex = -1;

        for (var index = tokenIndex; index >= 0; index--)
        {
            var token = tokens[index];
            if (IsStructural(token.Kind))
            {
                continue;
            }

            switch (token.Kind)
            {
                case TokenKind.RightParen:
                    nesting++;
                    break;

                case TokenKind.LeftParen:
                    if (nesting == 0)
                    {
                        openParenIndex = index;
                        index = -1;
                        break;
                    }

                    nesting--;
                    break;

                case TokenKind.Comma when nesting == 0:
                    activeParameter++;
                    break;
            }
        }

        if (openParenIndex < 0)
        {
            call = default;
            return false;
        }

        return TryCreateCallContext(compilation, tokens, openParenIndex, activeParameter, out call);
    }

    private static bool TryCreateCallContext(
        Compilation compilation,
        ImmutableArray<Token> tokens,
        int openParenIndex,
        int activeParameter,
        out ActiveCallContext call)
    {
        var nameIndex = FindPreviousSignificantToken(tokens, openParenIndex - 1);
        if (nameIndex < 0 || !TryGetCallableName(tokens[nameIndex], out var callName))
        {
            call = default;
            return false;
        }

        var qualifierIndex = FindPreviousSignificantToken(tokens, nameIndex - 1);
        if (qualifierIndex >= 0 && tokens[qualifierIndex].Kind == TokenKind.Dot)
        {
            if (!TryResolveReceiverType(compilation, tokens, qualifierIndex, nameIndex, out var receiverType))
            {
                call = default;
                return false;
            }

            call = new ActiveCallContext(callName, IsAccessor: true, activeParameter, receiverType);
            return true;
        }

        if (qualifierIndex >= 0 && tokens[qualifierIndex].Kind == TokenKind.Tilde)
        {
            callName = $"~{callName}";
        }

        call = new ActiveCallContext(callName, IsAccessor: false, activeParameter, ReceiverType: null);
        return true;
    }

    private static bool TryResolveReceiverType(
        Compilation compilation,
        ImmutableArray<Token> tokens,
        int dotIndex,
        int memberNameIndex,
        out TypeKind receiverType)
    {
        var receiverPosition = new Position(
            Math.Max(tokens[memberNameIndex].Span.StartLine - 1, 0),
            Math.Max(tokens[memberNameIndex].Span.StartColumn - 1, 0));

        if (CursorSemanticResolver.TryGetReceiverType(compilation, receiverPosition, out receiverType)
            && receiverType != TypeKind.Error)
        {
            return true;
        }

        var receiverTokenIndex = FindPreviousSignificantToken(tokens, dotIndex - 1);
        if (receiverTokenIndex < 0)
        {
            receiverType = default;
            return false;
        }

        var receiverToken = tokens[receiverTokenIndex];
        if (receiverToken.Kind == TokenKind.Identifier)
        {
            var eventName = CursorSemanticResolver.GetCurrentEventName(compilation, receiverPosition);
            if (eventName is not null
                && compilation.Semantics.EventsByName.TryGetValue(eventName, out var currentEvent))
            {
                var arg = currentEvent.Args.FirstOrDefault(candidate =>
                    string.Equals(candidate.Name, receiverToken.Text, StringComparison.Ordinal));

                if (arg is not null)
                {
                    receiverType = arg.ResolvedType;
                    return true;
                }
            }

            if (compilation.Semantics.FieldsByName.TryGetValue(receiverToken.Text, out var field))
            {
                receiverType = field.ResolvedType;
                return true;
            }
        }

        receiverType = default;
        return false;
    }

    private static bool TryGetCallableName(Token token, out string name)
    {
        var meta = Tokens.GetMeta(token.Kind);
        if (token.Kind == TokenKind.Identifier || meta.IsFunctionCallLeader || meta.IsValidAsMemberName)
        {
            name = token.Text;
            return true;
        }

        name = string.Empty;
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
            if (!IsStructural(tokens[index].Kind))
            {
                return index;
            }
        }

        return -1;
    }

    private static bool IsStructural(TokenKind kind) =>
        Tokens.GetMeta(kind).Categories.Contains(TokenCategory.Structural);

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
}

