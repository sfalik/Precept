using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.Language;
using Precept.Pipeline;

namespace Precept.LanguageServer;

internal static class CursorSemanticResolver
{
    /// <summary>
    /// Returns true when a <see cref="TokenKind.Set"/> token occupies a
    /// <see cref="ConstructSlotKind.TypeExpression"/> slot, meaning it is the
    /// type keyword (set collection) rather than the action keyword.
    /// Determined by checking whether the previous non-structural token is <c>as</c> or <c>of</c>,
    /// the two prepositions that introduce type-expression positions in the grammar.
    /// </summary>
    internal static bool IsSetInTypePosition(Compilation compilation, Token token)
    {
        if (token.Kind != TokenKind.Set)
        {
            return false;
        }

        var tokens = compilation.Tokens.Tokens;
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

        for (var i = setIndex - 1; i >= 0; i--)
        {
            var prevMeta = Tokens.GetMeta(tokens[i].Kind);
            if (prevMeta.Categories.Contains(TokenCategory.Structural))
            {
                continue;
            }

            return tokens[i].Kind is TokenKind.As or TokenKind.Of;
        }

        return false;
    }

    internal static ParsedConstruct? GetEnclosingConstruct(Compilation compilation, Position position)
    {
        var tokens = compilation.Tokens.Tokens;
        var tokenIndex = FindTokenAtOrBeforeCursor(tokens, position);
        if (tokenIndex < 0)
        {
            return null;
        }

        tokenIndex = AdjustTokenIndexForBoundary(tokens, tokenIndex, position);
        tokenIndex = FindPreviousSignificantToken(tokens, tokenIndex);
        return tokenIndex < 0 ? null : GetRelevantConstruct(compilation, tokens[tokenIndex]);
    }

    internal static string? GetCurrentEventName(Compilation compilation, Position position)
    {
        var construct = GetEnclosingConstruct(compilation, position);
        if (construct is null)
        {
            return null;
        }

        return compilation.Semantics.Events.FirstOrDefault(candidate => HasMatchingSyntax(candidate.Syntax, construct))?.Name
            ?? compilation.Semantics.TransitionRows.FirstOrDefault(candidate => HasMatchingSyntax(candidate.Syntax, construct))?.EventName
            ?? compilation.Semantics.EventHandlers.FirstOrDefault(candidate => HasMatchingSyntax(candidate.Syntax, construct))?.EventName
            ?? compilation.Semantics.Ensures.FirstOrDefault(candidate =>
                candidate.AnchorEvent is not null
                && HasMatchingSyntax(candidate.Syntax, construct))?.AnchorEvent;
    }

    /// <summary>
    /// Resolves the receiver type when the completion trigger was a literal '.' keystroke.
    /// Unlike <see cref="TryGetReceiverType"/>, this method does not go through
    /// <see cref="TryGetMemberAccessDotIndex"/> — which uses AdjustTokenIndexForBoundary and
    /// can step back past the dot when the cursor lands exactly at the dot's start position.
    /// Instead it locates the dot directly via Contains at (line, character-1) and then
    /// resolves the receiver by name lookup + expression tree.
    /// </summary>
    internal static bool TryGetReceiverTypeForDotTrigger(Compilation compilation, Position position, out TypeKind receiverType)
    {
        receiverType = default;
        var tokens = compilation.Tokens.Tokens;

        if (position.Character <= 0)
        {
            return false;
        }

        var dotSearchPos = new Position(position.Line, position.Character - 1);
        var dotIndex = -1;
        for (var i = 0; i < tokens.Length; i++)
        {
            if (tokens[i].Kind == TokenKind.Dot && Contains(tokens[i].Span, dotSearchPos))
            {
                dotIndex = i;
                break;
            }
        }

        if (dotIndex <= 0)
        {
            return false;
        }

        if (TryGetPositionBefore(tokens[dotIndex].Span, out var receiverPosition)
            && TryGetInnermostExpressionType(compilation, receiverPosition, out receiverType))
        {
            return true;
        }

        var receiverTokenIndex = FindPreviousSignificantToken(tokens, dotIndex - 1);
        if (receiverTokenIndex < 0)
        {
            return false;
        }

        var receiverToken = tokens[receiverTokenIndex];
        if (receiverToken.Kind != TokenKind.Identifier)
        {
            return false;
        }

        if (compilation.Semantics.FieldsByName.TryGetValue(receiverToken.Text, out var field))
        {
            receiverType = field.ResolvedType;
            return true;
        }

        var argDotIdx = FindPreviousSignificantToken(tokens, receiverTokenIndex - 1);
        if (argDotIdx >= 0 && tokens[argDotIdx].Kind == TokenKind.Dot)
        {
            var eventNameIdx = FindPreviousSignificantToken(tokens, argDotIdx - 1);
            if (eventNameIdx >= 0
                && tokens[eventNameIdx].Kind == TokenKind.Identifier
                && compilation.Semantics.EventsByName.TryGetValue(tokens[eventNameIdx].Text, out var evt))
            {
                var arg = evt.Args.FirstOrDefault(a => string.Equals(a.Name, receiverToken.Text, StringComparison.Ordinal));
                if (arg is not null)
                {
                    receiverType = arg.ResolvedType;
                    return true;
                }
            }
        }

        return false;
    }

    internal static bool TryGetEventForDotTrigger(Compilation compilation, Position position, out TypedEvent dotEvent)
    {
        dotEvent = default!;
        var tokens = compilation.Tokens.Tokens;

        if (position.Character <= 0)
        {
            return false;
        }

        var dotSearchPos = new Position(position.Line, position.Character - 1);
        var dotIndex = -1;
        for (var i = 0; i < tokens.Length; i++)
        {
            if (tokens[i].Kind == TokenKind.Dot && Contains(tokens[i].Span, dotSearchPos))
            {
                dotIndex = i;
                break;
            }
        }

        if (dotIndex <= 0)
        {
            return false;
        }

        var receiverTokenIndex = FindPreviousSignificantToken(tokens, dotIndex - 1);
        if (receiverTokenIndex < 0 || tokens[receiverTokenIndex].Kind != TokenKind.Identifier)
        {
            return false;
        }

        if (compilation.Semantics.EventsByName.TryGetValue(tokens[receiverTokenIndex].Text, out var evt))
        {
            dotEvent = evt;
            return true;
        }

        return false;
    }

    internal static bool TryGetReceiverType(Compilation compilation, Position position, out TypeKind receiverType)
    {
        receiverType = default;

        if (!TryGetMemberAccessDotIndex(compilation.Tokens.Tokens, position, out var dotIndex))
        {
            return false;
        }

        var dot = compilation.Tokens.Tokens[dotIndex];
        if (!TryGetPositionBefore(dot.Span, out var receiverPosition))
        {
            return false;
        }

        if (TryGetInnermostExpressionType(compilation, receiverPosition, out receiverType))
        {
            return true;
        }

        var tokenBeforeDotIndex = FindPreviousSignificantToken(compilation.Tokens.Tokens, dotIndex - 1);
        if (tokenBeforeDotIndex >= 0)
        {
            var tokenBeforeDot = compilation.Tokens.Tokens[tokenBeforeDotIndex];
            if (tokenBeforeDot.Kind == TokenKind.Identifier)
            {
                if (compilation.Semantics.FieldsByName.TryGetValue(tokenBeforeDot.Text, out var field))
                {
                    receiverType = field.ResolvedType;
                    return true;
                }

                var argNameIndex = tokenBeforeDotIndex;
                var argDotIndex = FindPreviousSignificantToken(compilation.Tokens.Tokens, argNameIndex - 1);
                var eventNameIndex = argDotIndex >= 0 && compilation.Tokens.Tokens[argDotIndex].Kind == TokenKind.Dot
                    ? FindPreviousSignificantToken(compilation.Tokens.Tokens, argDotIndex - 1)
                    : -1;
                if (eventNameIndex >= 0
                    && compilation.Tokens.Tokens[eventNameIndex].Kind == TokenKind.Identifier
                    && compilation.Semantics.EventsByName.TryGetValue(compilation.Tokens.Tokens[eventNameIndex].Text, out var evt))
                {
                    var arg = evt.Args.FirstOrDefault(a => string.Equals(a.Name, tokenBeforeDot.Text, StringComparison.Ordinal));
                    if (arg is not null)
                    {
                        receiverType = arg.ResolvedType;
                        return true;
                    }
                }
            }
        }

        return false;
    }

    internal static TypedField? GetCurrentActionTargetField(Compilation compilation, Position position)
    {
        var construct = GetEnclosingConstruct(compilation, position);
        var actionChain = construct?.GetSlot<ActionChainSlot>(ConstructSlotKind.ActionChain);
        if (actionChain is null)
        {
            return null;
        }

        var action = FindRelevantAction(actionChain.Actions, position);
        var targetName = action is null ? null : GetActionTargetIdentifier(action);
        if (targetName is null)
        {
            return null;
        }

        return compilation.Semantics.FieldsByName.TryGetValue(targetName, out var field)
            ? field
            : null;
    }

    private static ParsedConstruct? GetRelevantConstruct(Compilation compilation, Token token)
    {
        ParsedConstruct? best = null;
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

    private static int FindTokenAtOrBeforeCursor(ImmutableArray<Token> tokens, Position position)
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

    private static bool TryGetMemberAccessDotIndex(
        ImmutableArray<Token> tokens,
        Position position,
        out int dotIndex)
    {
        dotIndex = -1;

        var tokenIndex = FindTokenAtOrBeforeCursor(tokens, position);
        if (tokenIndex < 0)
        {
            return false;
        }

        tokenIndex = AdjustTokenIndexForBoundary(tokens, tokenIndex, position);
        tokenIndex = FindPreviousSignificantToken(tokens, tokenIndex);
        if (tokenIndex < 0)
        {
            return false;
        }

        if (tokens[tokenIndex].Kind == TokenKind.Dot)
        {
            dotIndex = tokenIndex;
            return true;
        }

        var previousIndex = FindPreviousSignificantToken(tokens, tokenIndex - 1);
        if (previousIndex >= 0 && tokens[previousIndex].Kind == TokenKind.Dot)
        {
            dotIndex = previousIndex;
            return true;
        }

        return false;
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

    private static bool StartsBeforeOrAt(SourceSpan span, int line, int column)
    {
        if (line != span.StartLine)
        {
            return line > span.StartLine;
        }

        return column >= span.StartColumn;
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

    private static bool TryGetPositionBefore(SourceSpan span, out Position position)
    {
        if (span.StartColumn <= 1)
        {
            position = new Position(0, 0);
            return false;
        }

        position = new Position(span.StartLine - 1, span.StartColumn - 2);
        return true;
    }

    private static bool TryGetInnermostExpressionType(Compilation compilation, Position position, out TypeKind receiverType)
    {
        var candidate = EnumerateTypedExpressions(compilation.Semantics)
            .Where(expression => Contains(expression.Span, position))
            .OrderBy(expression => GetSpanWidth(expression.Span))
            .ThenByDescending(expression => expression.Span.StartLine)
            .ThenByDescending(expression => expression.Span.StartColumn)
            .FirstOrDefault();

        if (candidate is null)
        {
            receiverType = default;
            return false;
        }

        receiverType = candidate.ResultType;
        return true;
    }

    private static IEnumerable<TypedExpression> EnumerateTypedExpressions(SemanticIndex semantics)
    {
        foreach (var field in semantics.Fields)
        {
            if (field.DefaultExpression is not null)
            {
                foreach (var expression in EnumerateExpressionTree(field.DefaultExpression))
                {
                    yield return expression;
                }
            }

            if (field.ComputedExpression is not null)
            {
                foreach (var expression in EnumerateExpressionTree(field.ComputedExpression))
                {
                    yield return expression;
                }
            }
        }

        foreach (var evt in semantics.Events)
        {
            foreach (var arg in evt.Args)
            {
                if (arg.DefaultExpression is null)
                {
                    continue;
                }

                foreach (var expression in EnumerateExpressionTree(arg.DefaultExpression))
                {
                    yield return expression;
                }
            }
        }

        foreach (var row in semantics.TransitionRows)
        {
            if (row.Guard is not null)
            {
                foreach (var expression in EnumerateExpressionTree(row.Guard))
                {
                    yield return expression;
                }
            }

            if (row is TypedTransitionRowSuccess successRow)
            {
                foreach (var expression in EnumerateActionExpressions(successRow.Actions))
                {
                    yield return expression;
                }
            }
        }

        foreach (var rule in semantics.Rules)
        {
            foreach (var expression in EnumerateExpressionTree(rule.Condition))
            {
                yield return expression;
            }

            if (rule.Guard is not null)
            {
                foreach (var expression in EnumerateExpressionTree(rule.Guard))
                {
                    yield return expression;
                }
            }

            foreach (var expression in EnumerateExpressionTree(rule.Message))
            {
                yield return expression;
            }
        }

        foreach (var ensure in semantics.Ensures)
        {
            foreach (var expression in EnumerateExpressionTree(ensure.Condition))
            {
                yield return expression;
            }

            if (ensure.Guard is not null)
            {
                foreach (var expression in EnumerateExpressionTree(ensure.Guard))
                {
                    yield return expression;
                }
            }

            foreach (var expression in EnumerateExpressionTree(ensure.Message))
            {
                yield return expression;
            }
        }

        foreach (var accessMode in semantics.AccessModes)
        {
            if (accessMode.Guard is null)
            {
                continue;
            }

            foreach (var expression in EnumerateExpressionTree(accessMode.Guard))
            {
                yield return expression;
            }
        }

        foreach (var hook in semantics.StateHooks)
        {
            if (hook.Guard is not null)
            {
                foreach (var expression in EnumerateExpressionTree(hook.Guard))
                {
                    yield return expression;
                }
            }

            foreach (var expression in EnumerateActionExpressions(hook.Actions))
            {
                yield return expression;
            }
        }

        foreach (var handler in semantics.EventHandlers)
        {
            if (handler is TypedEventRowSuccess successHandler)
            {
                foreach (var expression in EnumerateActionExpressions(successHandler.Actions))
                {
                    yield return expression;
                }
            }
        }
    }

    private static IEnumerable<TypedExpression> EnumerateActionExpressions(ImmutableArray<TypedAction> actions)
    {
        foreach (var action in actions)
        {
            if (action is not TypedInputAction input)
            {
                continue;
            }

            foreach (var expression in EnumerateExpressionTree(input.InputExpression))
            {
                yield return expression;
            }

            if (input.SecondaryExpression is null)
            {
                continue;
            }

            foreach (var expression in EnumerateExpressionTree(input.SecondaryExpression))
            {
                yield return expression;
            }
        }
    }

    private static IEnumerable<TypedExpression> EnumerateExpressionTree(TypedExpression expression)
    {
        yield return expression;

        switch (expression)
        {
            case TypedBinaryOp binary:
                foreach (var nested in EnumerateExpressionTree(binary.Left))
                {
                    yield return nested;
                }

                foreach (var nested in EnumerateExpressionTree(binary.Right))
                {
                    yield return nested;
                }
                break;

            case TypedUnaryOp unary:
                foreach (var nested in EnumerateExpressionTree(unary.Operand))
                {
                    yield return nested;
                }
                break;

            case TypedFunctionCall functionCall:
                foreach (var argument in functionCall.Arguments)
                {
                    foreach (var nested in EnumerateExpressionTree(argument))
                    {
                        yield return nested;
                    }
                }
                break;

            case TypedMemberAccess memberAccess:
                foreach (var nested in EnumerateExpressionTree(memberAccess.Object))
                {
                    yield return nested;
                }
                break;

            case TypedConditional conditional:
                foreach (var nested in EnumerateExpressionTree(conditional.Condition))
                {
                    yield return nested;
                }

                foreach (var nested in EnumerateExpressionTree(conditional.ThenBranch))
                {
                    yield return nested;
                }

                foreach (var nested in EnumerateExpressionTree(conditional.ElseBranch))
                {
                    yield return nested;
                }
                break;

            case TypedQuantifier quantifier:
                foreach (var nested in EnumerateExpressionTree(quantifier.Collection))
                {
                    yield return nested;
                }

                foreach (var nested in EnumerateExpressionTree(quantifier.Predicate))
                {
                    yield return nested;
                }
                break;

            case TypedInterpolatedString interpolatedString:
                foreach (var hole in interpolatedString.Segments.OfType<TypedHoleSegment>())
                {
                    foreach (var nested in EnumerateExpressionTree(hole.Expression))
                    {
                        yield return nested;
                    }
                }
                break;

            case TypedListLiteral listLiteral:
                foreach (var element in listLiteral.Elements)
                {
                    foreach (var nested in EnumerateExpressionTree(element))
                    {
                        yield return nested;
                    }
                }
                break;

            case TypedPostfixOp postfix:
                foreach (var nested in EnumerateExpressionTree(postfix.Operand))
                {
                    yield return nested;
                }
                break;
        }
    }

    private static ParsedAction? FindRelevantAction(ImmutableArray<ParsedAction> actions, Position position)
    {
        foreach (var action in actions)
        {
            if (Contains(action.Span, position))
            {
                return action;
            }
        }

        return null;
    }

    private static string? GetActionTargetIdentifier(ParsedAction action) =>
        GetIdentifierText(GetActionTargetExpression(action));

    private static ParsedExpression? GetActionTargetExpression(ParsedAction action) => action switch
    {
        AssignAction assign => assign.Target,
        CollectionValueAction collectionValue => collectionValue.Target,
        CollectionIntoAction collectionInto => collectionInto.Target,
        FieldOnlyAction fieldOnly => fieldOnly.Target,
        CollectionValueByAction collectionValueBy => collectionValueBy.Target,
        InsertAtAction insertAt => insertAt.Target,
        RemoveAtAction removeAt => removeAt.Target,
        PutKeyValueAction putKeyValue => putKeyValue.Target,
        CollectionIntoByAction collectionIntoBy => collectionIntoBy.Target,
        _ => null,
    };

    private static string? GetIdentifierText(ParsedExpression? expression) => expression switch
    {
        IdentifierExpression identifier => identifier.Name,
        _ => null,
    };

    private static bool HasMatchingSyntax(ParsedConstruct candidate, ParsedConstruct construct) =>
        ReferenceEquals(candidate, construct)
        || (candidate.Meta.Kind == construct.Meta.Kind && candidate.Span.Equals(construct.Span));

    private static long GetSpanWidth(SourceSpan span) =>
        ((long)span.EndLine - span.StartLine) * 1_000_000L
        + (span.EndColumn - span.StartColumn);

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
