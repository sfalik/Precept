using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Precept.Language;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.Pipeline;

namespace Precept.LanguageServer;

internal enum SlotContext
{
    TopLevel,
    AfterKeyword,
    AfterValueName,
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

        tokenIndex = AdjustTokenIndexForBoundary(tokens, tokenIndex, position);
        tokenIndex = FindPreviousSignificantToken(tokens, tokenIndex);
        if (tokenIndex < 0)
        {
            return SlotContext.TopLevel;
        }

        var token = tokens[tokenIndex];
        var meta = Precept.Language.Tokens.GetMeta(token.Kind);
        var construct = GetRelevantConstruct(compilation, token);

        if (TryGetSpecializedContext(tokens, tokenIndex, token, position, construct, out var specializedContext))
        {
            return specializedContext;
        }

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
            && construct?.Meta.ModifierDomain == Precept.Language.ModifierDomain.Field)
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

    internal static bool TryGetReceiverType(Compilation compilation, Position position, out TypeKind receiverType)
    {
        receiverType = default;

        if (!TryGetMemberAccessDotIndex(compilation.Tokens.Tokens, position, out var dotIndex))
        {
            return false;
        }

        var dot = compilation.Tokens.Tokens[dotIndex];
        if (!TryGetPositionBefore(dot.Span, out var receiverPosition)
            || !TryGetInnermostExpressionType(compilation, receiverPosition, out receiverType))
        {
            return false;
        }

        return true;
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

    private static bool TryGetSpecializedContext(
        ImmutableArray<Precept.Language.Token> tokens,
        int tokenIndex,
        Precept.Language.Token token,
        Position position,
        Precept.Pipeline.ParsedConstruct? construct,
        out SlotContext context)
    {
        if (TryGetActionChainContext(tokens, tokenIndex, token, position, construct, out context))
        {
            return true;
        }

        if (IsStateDeclarationNameToken(tokens, tokenIndex, token, construct))
        {
            context = SlotContext.InModifierPosition;
            return true;
        }

        if (IsTypeAnnotationKeywordContext(tokens, tokenIndex, token, position, construct))
        {
            context = SlotContext.AfterValueName;
            return true;
        }

        if (IsExpressionContext(token, position, construct))
        {
            context = SlotContext.InExpression;
            return true;
        }

        if (IsEventArgumentDefaultContext(token, construct))
        {
            context = SlotContext.InArgDefault;
            return true;
        }

        if (IsTypePositionContext(token, construct))
        {
            context = SlotContext.InTypePosition;
            return true;
        }

        context = default;
        return false;
    }

    private static bool IsTypeAnnotationKeywordContext(
        ImmutableArray<Precept.Language.Token> tokens,
        int tokenIndex,
        Precept.Language.Token token,
        Position position,
        Precept.Pipeline.ParsedConstruct? construct)
    {
        if (token.Kind != Precept.Language.TokenKind.Identifier
            || construct is null
            || Contains(token.Span, position))
        {
            return false;
        }

        return construct.Meta.Kind switch
        {
            Precept.Language.ConstructKind.FieldDeclaration => IsFieldDeclarationNameToken(tokens, tokenIndex),
            Precept.Language.ConstructKind.EventDeclaration => IsEventArgumentNameToken(tokens, tokenIndex),
            _ => false,
        };
    }

    /// <summary>
    /// Returns true when an Identifier token is a state name in a state declaration,
    /// i.e. the token immediately after <c>state</c> or a comma in a multi-state declaration.
    /// The cursor is past the identifier (not inside it), so modifier completions apply.
    /// Discriminates via <see cref="Precept.Language.ModifierDomain.State"/> from catalog metadata,
    /// not by hardcoding <see cref="Precept.Language.ConstructKind.StateDeclaration"/>.
    /// </summary>
    private static bool IsStateDeclarationNameToken(
        ImmutableArray<Precept.Language.Token> tokens,
        int tokenIndex,
        Precept.Language.Token token,
        Precept.Pipeline.ParsedConstruct? construct)
    {
        if (token.Kind != Precept.Language.TokenKind.Identifier
            || construct?.Meta.ModifierDomain != Precept.Language.ModifierDomain.State)
        {
            return false;
        }

        var previousTokenIndex = FindPreviousSignificantToken(tokens, tokenIndex - 1);
        return previousTokenIndex >= 0
            && (tokens[previousTokenIndex].Kind == Precept.Language.TokenKind.Comma
                || tokens[previousTokenIndex].Kind == construct.Meta.PrimaryLeadingToken);
    }

    private static bool IsFieldDeclarationNameToken(
        ImmutableArray<Precept.Language.Token> tokens,
        int tokenIndex)
    {
        var previousTokenIndex = FindPreviousSignificantToken(tokens, tokenIndex - 1);
        if (previousTokenIndex < 0
            || tokens[previousTokenIndex].Kind is not (Precept.Language.TokenKind.Field or Precept.Language.TokenKind.Comma))
        {
            return false;
        }

        var nextTokenIndex = FindNextSignificantToken(tokens, tokenIndex + 1);
        return nextTokenIndex < 0
            || tokens[nextTokenIndex].Kind is Precept.Language.TokenKind.As
                or Precept.Language.TokenKind.EndOfSource
            || Precept.Language.Constructs.LeadingTokens.Contains(tokens[nextTokenIndex].Kind);
    }

    private static bool IsEventArgumentNameToken(
        ImmutableArray<Precept.Language.Token> tokens,
        int tokenIndex)
    {
        var previousTokenIndex = FindPreviousSignificantToken(tokens, tokenIndex - 1);
        if (previousTokenIndex < 0)
        {
            return false;
        }

        if (tokens[previousTokenIndex].Kind is not (Precept.Language.TokenKind.LeftParen or Precept.Language.TokenKind.Comma))
        {
            return false;
        }

        var nextTokenIndex = FindNextSignificantToken(tokens, tokenIndex + 1);
        return nextTokenIndex < 0
            || tokens[nextTokenIndex].Kind is Precept.Language.TokenKind.As
                or Precept.Language.TokenKind.Comma
                or Precept.Language.TokenKind.RightParen
                or Precept.Language.TokenKind.EndOfSource;
    }

    private static bool TryGetActionChainContext(
        ImmutableArray<Precept.Language.Token> tokens,
        int tokenIndex,
        Precept.Language.Token token,
        Position position,
        Precept.Pipeline.ParsedConstruct? construct,
        out SlotContext context)
    {
        if (!IsActionChainContext(token, position, construct))
        {
            context = default;
            return false;
        }

        if (token.Kind == Precept.Language.TokenKind.Arrow)
        {
            context = SlotContext.InActionVerb;
            return true;
        }

        if (IsActionVerbToken(token.Kind, out var actionMeta)
            && ExpectsFieldTargetAfterActionVerb(actionMeta.SyntaxShape))
        {
            context = SlotContext.InFieldTarget;
            return true;
        }

        if (token.Kind == Precept.Language.TokenKind.Into
            && TryGetCurrentActionMeta(tokens, tokenIndex, out var intoAction)
            && ExpectsFieldTargetAfterInto(intoAction.SyntaxShape))
        {
            context = SlotContext.InFieldTarget;
            return true;
        }

        if (token.Kind is Precept.Language.TokenKind.Assign or Precept.Language.TokenKind.By or Precept.Language.TokenKind.At)
        {
            context = SlotContext.InExpression;
            return true;
        }

        context = default;
        return false;
    }

    private static bool IsActionChainContext(
        Precept.Language.Token token,
        Position position,
        Precept.Pipeline.ParsedConstruct? construct)
    {
        if (construct is null || !ConstructHasSlot(construct, Precept.Language.ConstructSlotKind.ActionChain))
        {
            return false;
        }

        var actionChainSlot = construct.GetSlot<Precept.Pipeline.ActionChainSlot>(Precept.Language.ConstructSlotKind.ActionChain);
        if (actionChainSlot is not null && Contains(actionChainSlot.Span, position))
        {
            return true;
        }

        if (actionChainSlot is not null
            && actionChainSlot.Span.StartLine > 0
            && token.Span.StartLine == actionChainSlot.Span.EndLine
            && token.Span.StartColumn >= actionChainSlot.Span.EndColumn
            && token.Kind is Precept.Language.TokenKind.Assign or Precept.Language.TokenKind.By or Precept.Language.TokenKind.At)
        {
            return true;
        }

        return !ConstructHasSlot(construct, Precept.Language.ConstructSlotKind.Outcome);
    }

    private static bool IsExpressionContext(
        Precept.Language.Token token,
        Position position,
        Precept.Pipeline.ParsedConstruct? construct)
    {
        if (construct is null)
        {
            return false;
        }

        if (IsFieldModifierDefaultContext(token, construct))
        {
            return true;
        }

        if (token.Kind == Precept.Language.TokenKind.When
            && ConstructHasSlot(construct, Precept.Language.ConstructSlotKind.GuardClause))
        {
            return true;
        }

        if (token.Kind == Precept.Language.TokenKind.BackArrow
            && ConstructHasSlot(construct, Precept.Language.ConstructSlotKind.ComputeExpression))
        {
            return true;
        }

        if (token.Kind == Precept.Language.TokenKind.Ensure
            && ConstructHasSlot(construct, Precept.Language.ConstructSlotKind.EnsureClause))
        {
            return true;
        }

        if (token.Kind == Precept.Language.TokenKind.Rule
            && ConstructHasSlot(construct, Precept.Language.ConstructSlotKind.RuleExpression))
        {
            return true;
        }

        return construct.Slots.Any(slot =>
            slot.Kind is Precept.Language.ConstructSlotKind.GuardClause
                or Precept.Language.ConstructSlotKind.ComputeExpression
                or Precept.Language.ConstructSlotKind.EnsureClause
                or Precept.Language.ConstructSlotKind.RuleExpression
            && Contains(slot.Span, position));
    }

    private static bool IsEventArgumentDefaultContext(
        Precept.Language.Token token,
        Precept.Pipeline.ParsedConstruct? construct)
    {
        return IsDefaultModifierToken(token.Kind)
            && construct?.Meta.Kind == Precept.Language.ConstructKind.EventDeclaration
            && ConstructHasSlot(construct, Precept.Language.ConstructSlotKind.ArgumentList);
    }

    private static bool IsFieldModifierDefaultContext(
        Precept.Language.Token token,
        Precept.Pipeline.ParsedConstruct? construct)
    {
        return IsDefaultModifierToken(token.Kind)
            && construct?.Meta.Kind == Precept.Language.ConstructKind.FieldDeclaration
            && ConstructHasSlot(construct, Precept.Language.ConstructSlotKind.ModifierList);
    }

    private static bool IsTypePositionContext(
        Precept.Language.Token token,
        Precept.Pipeline.ParsedConstruct? construct)
    {
        return token.Kind == Precept.Language.TokenKind.Of
            && ConstructHasSlot(construct, Precept.Language.ConstructSlotKind.TypeExpression);
    }

    private static bool ConstructHasSlot(
        Precept.Pipeline.ParsedConstruct? construct,
        Precept.Language.ConstructSlotKind slotKind) =>
        construct?.Meta.Slots.Any(slot => slot.Kind == slotKind) == true;

    private static bool IsDefaultModifierToken(Precept.Language.TokenKind tokenKind) =>
        Precept.Language.Modifiers.ByValueToken.TryGetValue(tokenKind, out var modifierMeta)
        && modifierMeta.Kind == Precept.Language.ModifierKind.Default;

    private static bool IsActionVerbToken(
        Precept.Language.TokenKind tokenKind,
        out Precept.Language.ActionMeta actionMeta) =>
        Precept.Language.Actions.ByTokenKind.TryGetValue(tokenKind, out actionMeta!);

    private static bool TryGetCurrentActionMeta(
        ImmutableArray<Precept.Language.Token> tokens,
        int tokenIndex,
        out Precept.Language.ActionMeta actionMeta)
    {
        for (var index = tokenIndex; index >= 0; index--)
        {
            if (IsActionVerbToken(tokens[index].Kind, out actionMeta))
            {
                return true;
            }
        }

        actionMeta = null!;
        return false;
    }

    private static bool ExpectsFieldTargetAfterActionVerb(Precept.Language.ActionSyntaxShape syntaxShape) => syntaxShape switch
    {
        Precept.Language.ActionSyntaxShape.AssignValue => true,
        Precept.Language.ActionSyntaxShape.CollectionValue => true,
        Precept.Language.ActionSyntaxShape.CollectionInto => true,
        Precept.Language.ActionSyntaxShape.FieldOnly => true,
        Precept.Language.ActionSyntaxShape.CollectionValueBy => true,
        Precept.Language.ActionSyntaxShape.InsertAt => true,
        Precept.Language.ActionSyntaxShape.RemoveAtIndex => true,
        Precept.Language.ActionSyntaxShape.PutKeyValue => true,
        Precept.Language.ActionSyntaxShape.CollectionIntoBy => true,
        _ => false,
    };

    private static bool ExpectsFieldTargetAfterInto(Precept.Language.ActionSyntaxShape syntaxShape) => syntaxShape switch
    {
        Precept.Language.ActionSyntaxShape.CollectionInto => true,
        Precept.Language.ActionSyntaxShape.CollectionIntoBy => true,
        _ => false,
    };

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

    private static int AdjustTokenIndexForBoundary(
        ImmutableArray<Precept.Language.Token> tokens,
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
        ImmutableArray<Precept.Language.Token> tokens,
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

    private static int FindNextSignificantToken(ImmutableArray<Precept.Language.Token> tokens, int startIndex)
    {
        for (var index = startIndex; index < tokens.Length; index++)
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

            foreach (var expression in EnumerateActionExpressions(row.Actions))
            {
                yield return expression;
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
            foreach (var expression in EnumerateActionExpressions(handler.Actions))
            {
                yield return expression;
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
