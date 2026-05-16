using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.Language;
using Precept.Pipeline;

namespace Precept.LanguageServer.Handlers;

/// <summary>
/// Carries the expected type and any declared qualifier metadata for a typed-constant completion site.
/// </summary>
internal readonly record struct TypedConstantContext(
    TypeKind ExpectedType,
    ImmutableArray<DeclaredQualifierMeta> Qualifiers)
{
    public static TypedConstantContext FromType(TypeKind type) =>
        new(type, ImmutableArray<DeclaredQualifierMeta>.Empty);

    public static TypedConstantContext FromField(TypedField field) =>
        new(field.ResolvedType, field.DeclaredQualifiers);

    public static TypedConstantContext FromArg(TypedArg arg) =>
        new(arg.ResolvedType, arg.DeclaredQualifiers);
}

internal sealed class CompletionHandler : ICompletionHandler
{
    private readonly DocumentStore _store;

    public CompletionHandler(DocumentStore store)
    {
        _store = store;
    }

    public CompletionRegistrationOptions GetRegistrationOptions(CompletionCapability capability, ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForPattern("**/*.precept"),
            TriggerCharacters = new Container<string>(" ", "'", ".", ">", "~"),
            ResolveProvider = false,
        };

    public Task<CompletionList> Handle(CompletionParams request, CancellationToken cancellationToken)
    {
        if (!_store.TryGet(request.TextDocument.Uri, out var state) || state.Current is null)
        {
            return Task.FromResult(new CompletionList([], true));
        }

        return Task.FromResult(GetCompletions(
            state.Current,
            state.SourceText,
            request.Position,
            request.Context?.TriggerCharacter));
    }

    private static CompletionList GetCompletions(Compilation compilation, string sourceText, Position position, string? triggerCharacter)
    {
        var context = SlotContextResolver.GetCursorContext(compilation, position);
        var lineText = GetLineText(sourceText, position.Line);

        // A single quote always opens a typed constant — never show keyword completions.
        // If type inference succeeds, show typed constant values; otherwise return empty.
        // If auto-close is enabled, VS Code inserts '' and the closing quote is already present —
        // detect this via IsEmptyTypedConstantToken and skip appending to avoid 'length''.
        // If auto-close is disabled, the token is not closed, so we append the closing quote.
        if (triggerCharacter == "'")
        {
            var innerContext = context is SlotContext.InExpression or SlotContext.InArgDefault
                ? context
                : SlotContext.InExpression;
            var appendClosingQuote = !IsEmptyTypedConstantToken(compilation.Tokens.Tokens, position);
            return CreateCompletionList(GetTypedConstantItems(compilation, position, innerContext, appendClosingQuote));
        }

        // Space trigger inside a typed constant: show slot-specific vocabulary (money codes,
        // temporal units, + continuation) instead of falling through to outer grammar routing.
        if (triggerCharacter == " " && IsInsideTypedConstantToken(compilation.Tokens.Tokens, position))
        {
            var innerContext = context is SlotContext.InExpression or SlotContext.InArgDefault
                ? context
                : SlotContext.InExpression;
            if (TryGetTypedConstantContext(compilation, position, innerContext, out var tcCtx)
                && TryGetTypedConstantSlotPhase(compilation.Tokens.Tokens, position, out var phase, out var textBefore))
            {
                var slotItems = tcCtx.ExpectedType switch
                {
                    TypeKind.Duration or TypeKind.Period => GetTemporalSlotItems(tcCtx, textBefore, phase),
                    TypeKind.Money => GetMoneySlotItems(tcCtx, textBefore, phase),
                    TypeKind.Quantity => GetQuantitySlotItems(tcCtx, textBefore, phase),
                    _ => Enumerable.Empty<CompletionItem>(),
                };
                return CreateCompletionList(slotItems);
            }
            return new CompletionList([], true);
        }

        // Dot trigger: always attempt member access completions.
        // GetCursorContext may return TopLevel when the file doesn't parse cleanly (e.g. the user
        // just typed 'Weight.' and the parser error-recovers over the incomplete expression), so
        // we must handle '.' here before the context switch rather than rely on InExpression routing.
        // TryGetReceiverTypeForDotTrigger is used instead of TryGetReceiverType because the latter
        // goes through AdjustTokenIndexForBoundary, which steps back past the dot when the cursor
        // lands exactly at the dot token's start column — causing the dot to be missed entirely.
        if (triggerCharacter == ".")
        {
            return SlotContextResolver.TryGetReceiverTypeForDotTrigger(compilation, position, out var receiverType)
                ? CreateCompletionList(Types.GetMeta(receiverType).Accessors.Select(accessor =>
                    CreateItem(
                        label: GetAccessorLabel(accessor),
                        detail: accessor.Description,
                        kind: accessor.ParameterType is null ? CompletionItemKind.Property : CompletionItemKind.Method,
                        sortGroup: CompletionSortGroup.Keyword,
                        documentation: accessor.Description)))
                : new CompletionList([], true);
        }

        // Ctrl+Space / invoked completion inside an already-open typed constant (e.g. cursor
        // between '' chars). Some clients send an empty trigger character for invoked completion,
        // so normalize null/empty the same way here.
        // SlotContextResolver may return TopLevel if the parse tree doesn't cover the literal
        // span, so detect by inspecting the raw token under the cursor instead.
        if (string.IsNullOrEmpty(triggerCharacter) && IsInsideTypedConstantToken(compilation.Tokens.Tokens, position))
        {
            var innerContext = context is SlotContext.InExpression or SlotContext.InArgDefault
                ? context
                : SlotContext.InExpression;
            // Append closing quote only when the token is empty (cursor right after opening ').
            var appendQuote = IsEmptyTypedConstantToken(compilation.Tokens.Tokens, position);
            return CreateCompletionList(GetTypedConstantItems(compilation, position, innerContext, appendQuote));
        }

        // Ctrl+Space / invoked completion inside a {hole} in an interpolated typed constant.
        // The cursor sits between a TypedConstantStart/Middle and the next TypedConstantMiddle/End,
        // so IsInsideTypedConstantToken returns false — we need a separate check here.
        if (string.IsNullOrEmpty(triggerCharacter) && IsInsideTypedConstantHole(compilation.Tokens.Tokens, position))
        {
            return CreateCompletionList(GetHoleItems(compilation, position));
        }

        return context switch
        {
            SlotContext.TopLevel => CreateCompletionList(GetTopLevelItems(lineText, position.Character)),
            SlotContext.AfterKeyword => CreateCompletionList(Enumerable.Empty<CompletionItem>()),
            SlotContext.AfterValueName => CreateCompletionList(GetTypeAnnotationItems()),
            SlotContext.InTypePosition => CreateCompletionList(GetTypeItems()),
            SlotContext.InModifierPosition => CreateModifierList(compilation, position),
            SlotContext.InStateTarget => CreateCompletionList(GetStateTargetItems(compilation, position)),
            SlotContext.InStateDeclarationName => CreateCompletionList(GetStateItems(compilation, position)),
            SlotContext.InEventTarget => CreateCompletionList(GetEventItems(compilation)),
            SlotContext.InFieldTarget => CreateCompletionList(GetFieldTargetItems(compilation, position)),
            SlotContext.InActionVerb => CreateCompletionList(GetActionOrOutcomeItems(compilation, position)),
            SlotContext.AfterStateTarget => CreateCompletionList(GetPostStateTargetItems(compilation, position)),
            SlotContext.AfterEventTarget => CreateCompletionList(GetPostEventTargetItems(compilation, position)),
            SlotContext.AfterNo => CreateCompletionList(GetNoTransitionItems()),
            SlotContext.InExpression => CreateCompletionList(GetExpressionItems(compilation, position)),
            SlotContext.InArgDefault => CreateCompletionList(GetExpressionItems(compilation, position)),
            SlotContext.InSetAssignment => CreateCompletionList(GetSetAssignmentItem()),
            _ => new CompletionList([], true),
        };
    }

    private static CompletionList CreateModifierList(Compilation compilation, Position position)
    {
        var construct = SlotContextResolver.GetEnclosingConstruct(compilation, position);
        if (construct is null)
        {
            return new CompletionList([], true);
        }

        var domain = GetModifierDomain(compilation, position, construct);
        return CreateCompletionList(GetModifierPositionItems(compilation, position, construct, domain));
    }

    private static IEnumerable<CompletionItem> GetModifierPositionItems(
        Compilation compilation,
        Position position,
        ParsedConstruct construct,
        ModifierDomain domain)
    {
        var items = GetModifierItems(compilation, position, domain);

        if (ShouldOfferDeclarationQualifierItems(compilation, position, construct, domain))
        {
            items = items.Concat(GetDeclarationQualifierItems(compilation, position));
        }

        if (construct.Meta.Kind == ConstructKind.FieldDeclaration)
        {
            items = items.Concat(GetComputedFieldItem());
        }

        return DistinctByLabel(items);
    }

    private static IEnumerable<CompletionItem> GetDeclarationQualifierItems(Compilation compilation, Position position)
    {
        if (!TryGetCurrentDeclarationType(compilation, position, out var declaredType))
        {
            return [];
        }

        var qualifierShape = Types.GetMeta(declaredType).QualifierShape;
        if (qualifierShape is null)
        {
            return [];
        }

        return qualifierShape.Slots.Select(slot =>
        {
            var meta = Tokens.GetMeta(slot.Preposition);
            return CreateItem(
                label: meta.Text ?? slot.Preposition.ToString().ToLowerInvariant(),
                detail: meta.Description,
                kind: CompletionItemKind.Keyword,
                sortGroup: CompletionSortGroup.Keyword,
                documentation: meta.Description,
                insertText: $"{meta.Text} ");
        });
    }

    private static bool ShouldOfferDeclarationQualifierItems(
        Compilation compilation,
        Position position,
        ParsedConstruct construct,
        ModifierDomain domain)
    {
        if (construct.Meta.Kind is not (ConstructKind.FieldDeclaration or ConstructKind.EventDeclaration)
            || !TryGetCurrentDeclarationType(compilation, position, out var declaredType)
            || Types.GetMeta(declaredType).QualifierShape is null)
        {
            return false;
        }

        if (!GetAlreadyAppliedModifierKinds(compilation.Tokens.Tokens, position, domain).IsEmpty
            || HasDeclarationQualifierBeforeCursor(compilation.Tokens.Tokens, position))
        {
            return false;
        }

        var tokenIndex = FindTokenAtOrBeforeCursor(compilation.Tokens.Tokens, position);
        if (tokenIndex < 0)
        {
            return false;
        }

        tokenIndex = AdjustTokenIndexForBoundary(compilation.Tokens.Tokens, tokenIndex, position);
        tokenIndex = FindPreviousSignificantToken(compilation.Tokens.Tokens, tokenIndex);
        if (tokenIndex < 0)
        {
            return false;
        }

        return Tokens.GetMeta(compilation.Tokens.Tokens[tokenIndex].Kind).Categories.Contains(TokenCategory.Type);
    }

    private static bool HasDeclarationQualifierBeforeCursor(ImmutableArray<Token> tokens, Position position)
    {
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

        var cursorLine = position.Line + 1;
        for (var index = tokenIndex; index >= 0; index--)
        {
            var token = tokens[index];
            if (token.Span.StartLine < cursorLine)
            {
                break;
            }

            if (token.Kind == TokenKind.As)
            {
                return false;
            }

            if (token.Kind is TokenKind.In or TokenKind.Of or TokenKind.To)
            {
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<CompletionItem> GetComputedFieldItem()
    {
        yield return CreateItem(
            label: "<- ",
            detail: "Define a computed field expression",
            kind: CompletionItemKind.Operator,
            sortGroup: CompletionSortGroup.Keyword,
            insertText: "<- ");
    }

    private static IEnumerable<CompletionItem> GetTopLevelItems(string lineText, int character) =>
        IsAtLineStart(lineText, character)
            ? GetTopLevelItemsCore()
            : Enumerable.Empty<CompletionItem>();

    private static IEnumerable<CompletionItem> GetTopLevelItemsCore() =>
        Constructs.All
            .Where(meta => meta.AllowedIn.Length == 0)
            .Select(meta => CreateItem(
                label: Tokens.GetMeta(meta.PrimaryLeadingToken).Text ?? meta.Name,
                detail: meta.Description,
                kind: CompletionItemKind.Keyword,
                sortGroup: CompletionSortGroup.Keyword,
                documentation: meta.Description,
                usageExample: meta.UsageExample,
                snippetTemplate: meta.SnippetTemplate));

    private static string GetLineText(string sourceText, int zeroBasedLine)
    {
        if (zeroBasedLine < 0 || string.IsNullOrEmpty(sourceText))
        {
            return string.Empty;
        }

        var normalized = sourceText.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace('\r', '\n');
        var lines = normalized.Split('\n');
        return zeroBasedLine < lines.Length ? lines[zeroBasedLine] : string.Empty;
    }

    private static bool IsAtLineStart(string lineText, int character)
    {
        var prefixLength = Math.Clamp(character, 0, lineText.Length);
        return character == 0 || lineText[..prefixLength].Trim().Length == 0;
    }

    private static ModifierDomain GetModifierDomain(
        Compilation compilation,
        Position position,
        ParsedConstruct construct)
    {
        if (construct.Meta.Kind == ConstructKind.EventDeclaration
            && TryGetCurrentEventArg(compilation, position, out _))
        {
            return ModifierDomain.Field;
        }

        return construct.Meta.ModifierDomain;
    }

    private static IEnumerable<CompletionItem> GetTypeAnnotationItems()
    {
        var meta = Tokens.GetMeta(TokenKind.As);
        return
        [
            CreateItem(
                label: meta.Text ?? meta.Kind.ToString().ToLowerInvariant(),
                detail: meta.Description,
                kind: CompletionItemKind.Keyword,
                sortGroup: CompletionSortGroup.Keyword,
                documentation: meta.Description),
        ];
    }

    private static IEnumerable<CompletionItem> GetSetAssignmentItem()
    {
        yield return CreateItem(
            label: "= ",
            detail: "Assign a value to the field",
            kind: CompletionItemKind.Operator,
            sortGroup: CompletionSortGroup.Keyword,
            insertText: "= ");
    }

    private static IEnumerable<CompletionItem> GetTypeItems() =>
        Types.All
            .Where(meta => meta.Token is not null)
            .Select(meta => CreateItem(
                label: meta.Token!.Text ?? meta.DisplayName,
                detail: meta.Description,
                kind: CompletionItemKind.Class,
                sortGroup: CompletionSortGroup.Type,
                documentation: meta.Description));

    private static IEnumerable<CompletionItem> GetModifierItems(Compilation compilation, Position position, ModifierDomain domain) =>
        GetModifiers(compilation, position, domain)
            .Select(meta => CreateItem(
                label: meta.Token.Text ?? meta.Kind.ToString().ToLowerInvariant(),
                detail: meta.Description,
                kind: CompletionItemKind.Keyword,
                sortGroup: CompletionSortGroup.Keyword,
                documentation: meta.Description));

    private static IEnumerable<CompletionItem> GetActionItems(Compilation compilation, Position position)
    {
        var targetField = SlotContextResolver.GetCurrentActionTargetField(compilation, position);
        var targetModifiers = targetField is null
            ? ImmutableArray<ModifierKind>.Empty
            : targetField.Modifiers.Concat(targetField.ImpliedModifiers).ToImmutableArray();

        return DistinctByLabel(
            Actions.All
                .Where(meta => meta.PrimaryActionKind is null)
                .Where(meta =>
                    targetField is null
                    || meta.ApplicableTo.Length == 0
                    || IsTypeApplicable(meta.ApplicableTo, targetField.ResolvedType, targetModifiers))
                .Select(meta => CreateItem(
                    label: meta.Token.Text ?? meta.Kind.ToString().ToLowerInvariant(),
                    detail: meta.Description,
                    kind: CompletionItemKind.Keyword,
                    sortGroup: CompletionSortGroup.Action,
                    documentation: meta.HoverDescription ?? meta.Description,
                    usageExample: meta.UsageExample,
                    snippetTemplate: meta.SnippetTemplate)));
    }

    private static IEnumerable<CompletionItem> GetActionOrOutcomeItems(Compilation compilation, Position position)
    {
        var items = GetActionItems(compilation, position);
        if (SlotContextResolver.IsImplicitActionChainContinuationPosition(compilation, position))
        {
            return items;
        }

        var construct = SlotContextResolver.GetEnclosingConstruct(compilation, position);
        if (construct is null || !construct.Meta.Slots.Any(slot => slot.Kind == ConstructSlotKind.Outcome))
        {
            return items;
        }

        return DistinctByLabel(items.Concat(GetOutcomeItems()));
    }

    private static IEnumerable<CompletionItem> GetOutcomeItems() =>
        Outcomes.All.Select(CreateOutcomeItem);

    private static IEnumerable<CompletionItem> GetStateTargetItems(Compilation compilation, Position position)
    {
        var items = GetStateItems(compilation, position);
        if (SlotContextResolver.IsTransitionOutcomeTargetPosition(compilation, position)
            || SlotContextResolver.IsStateTargetListContinuationPosition(compilation, position))
        {
            return items;
        }

        return DistinctByLabel(items.Concat([CreateTokenItem(TokenKind.Any)]));
    }

    private static IEnumerable<CompletionItem> GetStateItems(Compilation compilation, Position position)
    {
        var alreadySelectedStates = GetAlreadySelectedStateNames(compilation, position);
        return compilation.Semantics.States
            .Select(state => state.Name)
            .Distinct(StringComparer.Ordinal)
            .Where(name => !alreadySelectedStates.Contains(name))
            .Select(name => CreateItem(name, "State", CompletionItemKind.EnumMember, CompletionSortGroup.SemanticSymbol));
    }

    private static ImmutableHashSet<string> GetAlreadySelectedStateNames(Compilation compilation, Position position)
    {
        if (!SlotContextResolver.IsStateTargetListContinuationPosition(compilation, position)
            && !SlotContextResolver.IsStateDeclarationNameListContinuationPosition(compilation, position))
        {
            return ImmutableHashSet<string>.Empty;
        }

        var construct = SlotContextResolver.GetEnclosingConstruct(compilation, position);
        var builder = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);

        var stateTargetSlot = construct?.GetSlot<StateTargetSlot>(ConstructSlotKind.StateTarget);
        if (stateTargetSlot is not null)
        {
            for (var index = 0; index < stateTargetSlot.StateNames.Length && index < stateTargetSlot.NameSpans.Length; index++)
            {
                if (EndsBeforeCursor(stateTargetSlot.NameSpans[index], position))
                {
                    builder.Add(stateTargetSlot.StateNames[index]);
                }
            }
        }

        var stateEntrySlot = construct?.GetSlot<StateEntryListSlot>(ConstructSlotKind.StateEntryList);
        if (stateEntrySlot is not null)
        {
            foreach (var entry in stateEntrySlot.Entries)
            {
                if (EndsBeforeCursor(entry.NameSpan, position))
                {
                    builder.Add(entry.Name);
                }
            }
        }

        return builder.ToImmutable();
    }

    private static bool EndsBeforeCursor(SourceSpan span, Position position)
    {
        var cursorLine = position.Line + 1;
        var cursorCharacter = position.Character + 1;
        return span.EndLine < cursorLine
            || (span.EndLine == cursorLine && span.EndColumn <= cursorCharacter);
    }

    private static IEnumerable<CompletionItem> GetPostStateTargetItems(Compilation compilation, Position position)
    {
        if (!SlotContextResolver.TryGetCompletedStateTargetLeadingToken(compilation, position, out var leadingToken))
        {
            return [];
        }

        var tokenKinds = Constructs.All
            .Where(meta => meta.Slots.Count > 0 && meta.Slots[0].Kind == ConstructSlotKind.StateTarget)
            .SelectMany(meta => meta.Entries
                .Where(entry => entry.LeadingToken == leadingToken)
                .SelectMany(entry =>
                {
                    var disambiguationTokens = entry.DisambiguationTokens ?? ImmutableArray<TokenKind>.Empty;
                    if (meta.Slots.Count > 1 && meta.Slots[1].Kind == ConstructSlotKind.GuardClause)
                    {
                        return disambiguationTokens.Add(TokenKind.When);
                    }

                    return disambiguationTokens;
                }))
            .Distinct();

        return tokenKinds.Select(CreateTokenItem);
    }

    private static IEnumerable<CompletionItem> GetEventItems(Compilation compilation) =>
        compilation.Semantics.Events
            .Select(evt => evt.Name)
            .Distinct(StringComparer.Ordinal)
            .Select(name => CreateItem(name, "Event", CompletionItemKind.Event, CompletionSortGroup.SemanticSymbol));

    private static IEnumerable<CompletionItem> GetPostEventTargetItems(Compilation compilation, Position position)
    {
        var construct = SlotContextResolver.GetEnclosingConstruct(compilation, position);
        if (construct is null)
        {
            return [];
        }

        var eventTargetIndex = -1;
        for (var index = 0; index < construct.Meta.Slots.Count; index++)
        {
            if (construct.Meta.Slots[index].Kind == ConstructSlotKind.EventTarget)
            {
                eventTargetIndex = index;
                break;
            }
        }

        if (eventTargetIndex < 0 || eventTargetIndex >= construct.Meta.Slots.Count - 1)
        {
            return [];
        }

        var nextIndex = eventTargetIndex + 1;
        var tokenKinds = ImmutableArray.CreateBuilder<TokenKind>();
        if (construct.Meta.Slots[nextIndex].Kind == ConstructSlotKind.GuardClause)
        {
            tokenKinds.Add(TokenKind.When);
            nextIndex++;
        }

        if (nextIndex < construct.Meta.Slots.Count)
        {
            switch (construct.Meta.Slots[nextIndex].Kind)
            {
                case ConstructSlotKind.EnsureClause:
                    tokenKinds.Add(TokenKind.Ensure);
                    break;
                case ConstructSlotKind.ActionChain:
                case ConstructSlotKind.Outcome:
                    tokenKinds.Add(TokenKind.Arrow);
                    break;
            }
        }

        return tokenKinds.Select(CreateTokenItem);
    }

    private static IEnumerable<CompletionItem> GetFieldTargetItems(Compilation compilation, Position position)
    {
        var items = GetFieldItems(compilation, position);
        if (!SlotContextResolver.IsAccessFieldTargetPosition(compilation, position))
        {
            return items;
        }

        return DistinctByLabel(items.Concat([CreateTokenItem(TokenKind.All)]));
    }

    private static IEnumerable<CompletionItem> GetFieldItems(Compilation compilation, Position position)
    {
        var alreadySelectedFields = GetAlreadySelectedFieldNames(compilation, position);
        return compilation.Semantics.Fields
            .Select(field => field.Name)
            .Distinct(StringComparer.Ordinal)
            .Where(name => !alreadySelectedFields.Contains(name))
            .Select(name => CreateItem(name, "Field", CompletionItemKind.Field, CompletionSortGroup.SemanticSymbol));
    }

    private static ImmutableHashSet<string> GetAlreadySelectedFieldNames(Compilation compilation, Position position)
    {
        if (!SlotContextResolver.IsFieldTargetListContinuationPosition(compilation, position))
        {
            return ImmutableHashSet<string>.Empty;
        }

        var fieldTargetSlot = SlotContextResolver.GetEnclosingConstruct(compilation, position)
            ?.GetSlot<FieldTargetSlot>(ConstructSlotKind.FieldTarget);
        if (fieldTargetSlot is null)
        {
            return ImmutableHashSet<string>.Empty;
        }

        var builder = ImmutableHashSet.CreateBuilder<string>(StringComparer.Ordinal);
        if (fieldTargetSlot.FieldName is not null && EndsBeforeCursor(fieldTargetSlot.NameSpan, position))
        {
            builder.Add(fieldTargetSlot.FieldName);
        }

        foreach (var (name, span) in fieldTargetSlot.AdditionalFields)
        {
            if (EndsBeforeCursor(span, position))
            {
                builder.Add(name);
            }
        }

        return builder.ToImmutable();
    }

    private static IEnumerable<CompletionItem> GetFieldItems(Compilation compilation) =>
        compilation.Semantics.Fields
            .Select(field => field.Name)
            .Distinct(StringComparer.Ordinal)
            .Select(name => CreateItem(name, "Field", CompletionItemKind.Field, CompletionSortGroup.SemanticSymbol));

    private static IEnumerable<CompletionItem> GetNoTransitionItems() =>
        [CreateTokenItem(Outcomes.NoTransitionSecondaryToken)];

    private static IEnumerable<CompletionItem> GetExpressionItems(Compilation compilation, Position position)
    {
        if (SlotContextResolver.TryGetReceiverType(compilation, position, out var receiverType))
        {
            return DistinctByLabel(Types.GetMeta(receiverType).Accessors.Select(accessor =>
                CreateItem(
                    label: GetAccessorLabel(accessor),
                    detail: accessor.Description,
                    kind: accessor.ParameterType is null ? CompletionItemKind.Property : CompletionItemKind.Method,
                    sortGroup: CompletionSortGroup.Keyword,
                    documentation: accessor.Description)));
        }

        var eventArgItems = GetCurrentEventArgItems(compilation, position);
        var fieldItems = GetFieldItems(compilation);
        var functionItems = DistinctByLabel(Functions.All.Select(meta => CreateItem(
            label: GetFunctionLabel(meta),
            detail: meta.Description,
            kind: CompletionItemKind.Function,
            sortGroup: CompletionSortGroup.Function,
            documentation: meta.HoverDescription ?? meta.Description,
            usageExample: meta.UsageExample,
            snippetTemplate: meta.SnippetTemplate)));
        // Boolean literals are only valid in boolean-typed positions.
        // Suppress them when the set-action target field has a known non-boolean type.
        var actionTarget = SlotContextResolver.GetCurrentActionTargetField(compilation, position);
        var includeBooleanLiterals = actionTarget is null || actionTarget.ResolvedType == TypeKind.Boolean;
        var booleanLiteralItems = includeBooleanLiterals
            ? new[] { TokenKind.True, TokenKind.False }
                .Select(Tokens.GetMeta)
                .Select(meta => CreateItem(
                    label: meta.Text ?? meta.Kind.ToString().ToLowerInvariant(),
                    detail: meta.Description,
                    kind: CompletionItemKind.Constant,
                    sortGroup: CompletionSortGroup.Keyword,
                    documentation: meta.Description))
            : Enumerable.Empty<CompletionItem>();

        var operatorItems = GetExpressionOperatorItems();

        return eventArgItems
            .Concat(fieldItems)
            .Concat(functionItems)
            .Concat(operatorItems)
            .Concat(booleanLiteralItems);
    }

    private static IEnumerable<CompletionItem> GetExpressionOperatorItems()
    {
        var operatorItems = Operators.All.Select(CreateOperatorItem);
        var tokenItems = GetExpressionKeywordTokenKinds().Select(CreateTokenItem);
        return DistinctByLabel(operatorItems.Concat(tokenItems));
    }

    private static IEnumerable<TokenKind> GetExpressionKeywordTokenKinds()
    {
        var conditionalDescription = Tokens.GetMeta(TokenKind.If).Description;

        var operatorTokenKinds = Operators.All
            .SelectMany(GetOperatorTokenKinds)
            .Where(IsExpressionKeywordTokenKind);
        var expressionLeadTokenKinds = ExpressionForms.All
            .SelectMany(meta => meta.LeadTokens)
            .Where(IsExpressionKeywordTokenKind);
        var conditionalTokenKinds = Tokens.All
            .Where(meta => meta.Categories.Contains(TokenCategory.Control))
            .Where(meta => string.Equals(meta.Description, conditionalDescription, StringComparison.Ordinal))
            .Select(meta => meta.Kind);

        return operatorTokenKinds
            .Concat(expressionLeadTokenKinds)
            .Concat(conditionalTokenKinds)
            .Distinct();
    }

    private static IEnumerable<TokenKind> GetOperatorTokenKinds(OperatorMeta meta) => meta switch
    {
        SingleTokenOp single => [single.Token.Kind],
        MultiTokenOp multi => multi.Tokens.Select(token => token.Kind),
        _ => [],
    };

    private static bool IsExpressionKeywordTokenKind(TokenKind tokenKind)
    {
        var categories = Tokens.GetMeta(tokenKind).Categories;
        return categories.Contains(TokenCategory.LogicalOperator)
            || categories.Contains(TokenCategory.Membership)
            || categories.Contains(TokenCategory.Quantifier)
            || ExpressionForms.GetMeta(ExpressionFormKind.Quantifier).LeadTokens.Contains(tokenKind);
    }

    private static CompletionItem CreateOperatorItem(OperatorMeta meta)
    {
        var label = meta switch
        {
            SingleTokenOp single => single.Token.Text ?? meta.Kind.ToString().ToLowerInvariant(),
            MultiTokenOp multi => string.Join(" ", multi.Tokens.Select(token => token.Text ?? token.Kind.ToString().ToLowerInvariant())),
            _ => meta.Kind.ToString().ToLowerInvariant(),
        };

        return CreateItem(
            label: label,
            detail: meta.Description,
            kind: CompletionItemKind.Operator,
            sortGroup: CompletionSortGroup.Keyword,
            documentation: meta.HoverDescription ?? meta.Description,
            usageExample: meta.UsageExample);
    }

    private static CompletionItem CreateOutcomeItem(OutcomeMeta meta) =>
        CreateItem(
            label: Tokens.GetMeta(meta.LeadingToken).Text ?? meta.SerializedKind,
            detail: meta.Description,
            kind: CompletionItemKind.Keyword,
            sortGroup: CompletionSortGroup.Action,
            documentation: meta.Description,
            usageExample: meta.Example);

    private static CompletionItem CreateTokenItem(TokenKind tokenKind)
    {
        var meta = Tokens.GetMeta(tokenKind);
        var kind = meta.Categories.Contains(TokenCategory.Operator)
            ? CompletionItemKind.Operator
            : CompletionItemKind.Keyword;

        return CreateItem(
            label: meta.Text ?? tokenKind.ToString().ToLowerInvariant(),
            detail: meta.Description,
            kind: kind,
            sortGroup: CompletionSortGroup.Keyword,
            documentation: meta.Description);
    }

    private static IEnumerable<CompletionItem> GetTypedConstantItems(
        Compilation compilation, Position position, SlotContext context, bool appendClosingQuote = false)
    {
        if (!TryGetTypedConstantContext(compilation, position, context, out var tcContext))
            return [];

        var items = tcContext.ExpectedType switch
        {
            TypeKind.Boolean => GetBooleanLiteralItems(tcContext),
            TypeKind.Duration or TypeKind.Period => GetTemporalLiteralItems(compilation, tcContext, position),
            TypeKind.Money => GetMoneyLiteralItems(compilation, tcContext, position),
            TypeKind.Date or TypeKind.Time or TypeKind.Instant or TypeKind.DateTime
                or TypeKind.ZonedDateTime or TypeKind.Timezone => GetStructuredExampleItems(compilation, tcContext),
            TypeKind.Currency => GetCurrencyCodeItems(tcContext),
            TypeKind.UnitOfMeasure => GetUnitOfMeasureItems(tcContext),
            TypeKind.Dimension => GetDimensionItems(tcContext),
            TypeKind.Quantity => GetQuantityLiteralItems(compilation, tcContext, position),
            _ => GetFreeFormItems(compilation, tcContext),
        };

        if (appendClosingQuote)
            items = items.Select(item => AppendToInsertText(item, "'"));

        return items;
    }

    private static CompletionItem AppendToInsertText(CompletionItem item, string suffix) =>
        new()
        {
            Label = item.Label,
            InsertText = (item.InsertText ?? item.Label) + suffix,
            InsertTextFormat = InsertTextFormat.PlainText,
            Documentation = item.Documentation,
            SortText = item.SortText,
            Detail = item.Detail,
            Kind = item.Kind,
        };

    private static IEnumerable<CompletionItem> GetBooleanLiteralItems(TypedConstantContext tcContext)
    {
        string[] candidates = ["true", "false"];
        var qualifierValues = GetQualifierAllowedValues(tcContext);
        if (qualifierValues is not null)
            candidates = candidates.Where(v => qualifierValues.Contains(v, StringComparer.OrdinalIgnoreCase)).ToArray();

        return candidates.Select(v => CreateItem(
            label: v,
            detail: "boolean literal",
            kind: CompletionItemKind.Value,
            sortGroup: CompletionSortGroup.TypedConstant));
    }

    private static IEnumerable<CompletionItem> GetTemporalLiteralItems(Compilation compilation, TypedConstantContext tcContext, Position position)
    {
        if (TryGetTypedConstantSlotPhase(compilation.Tokens.Tokens, position, out var phase, out var textBefore)
            && phase != TypedConstantPhase.Empty)
        {
            return GetTemporalSlotItems(tcContext, textBefore, phase);
        }

        var typeMeta = Types.GetMeta(tcContext.ExpectedType);
        var examples = typeMeta.ContentValidation?.Examples ?? [];
        var reused = TypedConstantCollector.CollectByType(compilation.Semantics, tcContext.ExpectedType);
        return DistinctByLabel(
            reused.Select(v => CreateItem(v, "temporal literal", CompletionItemKind.Value, CompletionSortGroup.TypedConstant))
            .Concat(examples.Select(e => CreateItem(e, "example format", CompletionItemKind.Snippet, CompletionSortGroup.TypedConstant))));
    }

    private static IEnumerable<CompletionItem> GetMoneyLiteralItems(Compilation compilation, TypedConstantContext tcContext, Position position)
    {
        if (TryGetTypedConstantSlotPhase(compilation.Tokens.Tokens, position, out var phase, out var textBefore)
            && phase != TypedConstantPhase.Empty)
        {
            return GetMoneySlotItems(tcContext, textBefore, phase);
        }

        var typeMeta = Types.GetMeta(tcContext.ExpectedType);
        var examples = typeMeta.ContentValidation?.Examples ?? [];
        var reused = TypedConstantCollector.CollectByType(compilation.Semantics, tcContext.ExpectedType);
        return DistinctByLabel(
            reused.Select(v => CreateItem(v, "money literal", CompletionItemKind.Value, CompletionSortGroup.TypedConstant))
            .Concat(examples.Select(e => CreateItem(e, "example format", CompletionItemKind.Snippet, CompletionSortGroup.TypedConstant))));
    }

    private static IEnumerable<CompletionItem> GetStructuredExampleItems(Compilation compilation, TypedConstantContext tcContext)
    {
        var typeMeta = Types.GetMeta(tcContext.ExpectedType);
        var examples = typeMeta.ContentValidation?.Examples ?? [];
        var reused = TypedConstantCollector.CollectByType(compilation.Semantics, tcContext.ExpectedType);
        var detail = typeMeta.ContentValidation?.FormatDescription ?? $"{typeMeta.DisplayName} literal";
        return DistinctByLabel(
            reused.Select(v => CreateItem(v, detail, CompletionItemKind.Value, CompletionSortGroup.TypedConstant))
            .Concat(examples.Select(e => CreateItem(e, detail, CompletionItemKind.Constant, CompletionSortGroup.TypedConstant))));
    }

    private static IEnumerable<CompletionItem> GetCurrencyCodeItems(TypedConstantContext tcContext)
    {
        var codes = CurrencyCatalog.All.Keys.AsEnumerable();
        var currencyQualifier = tcContext.Qualifiers.OfType<DeclaredQualifierMeta.Currency>().FirstOrDefault();
        if (currencyQualifier is not null)
            codes = codes.Where(c => c.Equals(currencyQualifier.CurrencyCode, StringComparison.OrdinalIgnoreCase));

        return codes.Select(code => CreateItem(code, "ISO 4217 currency code", CompletionItemKind.Unit, CompletionSortGroup.TypedConstantSegment));
    }

    private static IEnumerable<CompletionItem> GetUnitOfMeasureItems(TypedConstantContext tcContext)
    {
        return GetQuantitySlotItems(tcContext, string.Empty, TypedConstantPhase.AfterNumberSpace);
    }

    private static IEnumerable<CompletionItem> GetDimensionItems(TypedConstantContext _)
    {
        return DimensionCatalog.All.Keys.Select(name =>
            CreateItem(name, "dimension family", CompletionItemKind.Unit, CompletionSortGroup.TypedConstantSegment));
    }

    private static IEnumerable<CompletionItem> GetQuantityLiteralItems(Compilation compilation, TypedConstantContext tcContext, Position position)
    {
        if (TryGetTypedConstantSlotPhase(compilation.Tokens.Tokens, position, out var phase, out var textBefore)
            && phase != TypedConstantPhase.Empty)
        {
            return GetQuantitySlotItems(tcContext, textBefore, phase);
        }

        var typeMeta = Types.GetMeta(tcContext.ExpectedType);
        var examples = typeMeta.ContentValidation?.Examples ?? [];
        var reused = TypedConstantCollector.CollectByType(compilation.Semantics, tcContext.ExpectedType);
        return DistinctByLabel(
            reused.Select(v => CreateItem(v, "quantity literal", CompletionItemKind.Value, CompletionSortGroup.TypedConstant))
            .Concat(examples.Select(e => CreateItem(e, "example format", CompletionItemKind.Snippet, CompletionSortGroup.TypedConstant))));
    }

    private static IEnumerable<CompletionItem> GetFreeFormItems(Compilation compilation, TypedConstantContext tcContext)
    {
        var reused = TypedConstantCollector.CollectByType(compilation.Semantics, tcContext.ExpectedType);
        if (tcContext.ExpectedType == TypeKind.String)
        {
            // text is mostly free-form: only suggest reused values, no noisy examples
            return reused.Select(v => CreateItem(v, "used elsewhere in this file", CompletionItemKind.Text, CompletionSortGroup.TypedConstant));
        }

        var typeMeta = Types.GetMeta(tcContext.ExpectedType);
        var examples = typeMeta.ContentValidation?.Examples ?? [];
        return DistinctByLabel(
            reused.Select(v => CreateItem(v, "used elsewhere in this file", CompletionItemKind.Value, CompletionSortGroup.TypedConstant))
            .Concat(examples.Select(e => CreateItem(e, "example format", CompletionItemKind.Snippet, CompletionSortGroup.TypedConstant))));
    }

    // ── Typed-constant slot phase detection ───────────────────────────────────

    private enum TypedConstantPhase
    {
        Empty,
        NumberTyping,
        AfterNumberSpace,
        UnitTyping,
        SegmentComplete,
        AfterPlus,
        AfterPlusNumber,
        AfterPlusNumberSpace,
    }

    private static readonly Regex NumberOnlyPattern =
        new(@"^-?\d+$", RegexOptions.Compiled);

    private static readonly Regex NumberSpacePattern =
        new(@"^-?\d+\s$", RegexOptions.Compiled);

    private static readonly Regex NumberSpaceAlphaPattern =
        new(@"^(-?\d+)\s+([A-Za-z/]+(?:\^-?\d+)?)$", RegexOptions.Compiled);

    private static bool TryGetTypedConstantSlotPhase(
        ImmutableArray<Token> tokens,
        Position position,
        out TypedConstantPhase phase,
        out string textBeforeCursor)
    {
        phase = TypedConstantPhase.Empty;
        textBeforeCursor = string.Empty;

        var tokenIndex = FindTokenAtOrBeforeCursor(tokens, position);
        if (tokenIndex < 0)
            return false;

        var token = tokens[tokenIndex];
        if (!IsTypedConstantToken(token.Kind) || !Contains(token.Span, position))
            return false;

        // token.Text has quotes stripped; opening quote is at token.Span.StartColumn (1-based).
        // position.Character is 0-based. Content starts one column after the opening quote.
        var cursorContentOffset = Math.Clamp(
            position.Character - token.Span.StartColumn,
            0,
            token.Text.Length);

        textBeforeCursor = token.Text[..cursorContentOffset];
        phase = ClassifyPhase(textBeforeCursor);
        return true;
    }

    private static TypedConstantPhase ClassifyPhase(string text)
    {
        if (text.Length == 0)
            return TypedConstantPhase.Empty;

        // Handle compound: look for last '+' in the text
        var lastPlusIdx = text.LastIndexOf('+');
        if (lastPlusIdx >= 0)
        {
            var beforePlus = text[..lastPlusIdx].TrimEnd();
            // Ensure there's a complete segment before the +
            if (IsCompleteTemporalSegment(beforePlus))
            {
                var afterPlus = text[(lastPlusIdx + 1)..];
                var trimmed = afterPlus.TrimStart();
                if (trimmed.Length == 0)
                    return TypedConstantPhase.AfterPlus;
                return ClassifyAfterPlusContent(trimmed);
            }
        }

        return ClassifySimplePhase(text);
    }

    private static TypedConstantPhase ClassifyAfterPlusContent(string trimmedAfterPlus)
    {
        if (NumberOnlyPattern.IsMatch(trimmedAfterPlus))
            return TypedConstantPhase.AfterPlusNumber;

        if (NumberSpacePattern.IsMatch(trimmedAfterPlus))
            return TypedConstantPhase.AfterPlusNumberSpace;

        var match = NumberSpaceAlphaPattern.Match(trimmedAfterPlus);
        if (match.Success)
        {
            var unitPart = match.Groups[2].Value;
            return TemporalUnits.TryGet(unitPart, out _)
                ? TypedConstantPhase.SegmentComplete
                : TypedConstantPhase.UnitTyping;
        }

        // Partial alpha with no space
        return trimmedAfterPlus.Any(char.IsLetter)
            ? TypedConstantPhase.UnitTyping
            : TypedConstantPhase.AfterPlusNumber;
    }

    private static TypedConstantPhase ClassifySimplePhase(string text)
    {
        if (NumberOnlyPattern.IsMatch(text))
            return TypedConstantPhase.NumberTyping;

        if (NumberSpacePattern.IsMatch(text))
            return TypedConstantPhase.AfterNumberSpace;

        var match = NumberSpaceAlphaPattern.Match(text);
        if (match.Success)
        {
            var unitPart = match.Groups[2].Value;
            return TemporalUnits.TryGet(unitPart, out _)
                ? TypedConstantPhase.SegmentComplete
                : TypedConstantPhase.UnitTyping;
        }

        // Partial alpha with no space yet (e.g. "3d")
        return TypedConstantPhase.UnitTyping;
    }

    private static bool IsCompleteTemporalSegment(string text)
    {
        var match = NumberSpaceAlphaPattern.Match(text.Trim());
        return match.Success && TemporalUnits.TryGet(match.Groups[2].Value, out _);
    }

    // ── Slot-specific item generators ─────────────────────────────────────────

    private static IEnumerable<CompletionItem> GetTemporalSlotItems(
        TypedConstantContext tcContext,
        string textBeforeCursor,
        TypedConstantPhase phase)
    {
        return phase switch
        {
            TypedConstantPhase.AfterNumberSpace or TypedConstantPhase.UnitTyping
                or TypedConstantPhase.AfterPlusNumberSpace => BuildTemporalUnitItems(tcContext, textBeforeCursor),
            TypedConstantPhase.SegmentComplete => BuildTemporalContinuationItem(),
            _ => [],
        };
    }

    private static IEnumerable<CompletionItem> BuildTemporalUnitItems(TypedConstantContext tcContext, string textBeforeCursor)
    {
        IEnumerable<TemporalUnits.TemporalUnitEntry> units = TemporalUnits.AllEntries;

        // Qualifier hard-filtering: TemporalUnit pins to a specific unit; TemporalDimension
        // restricts to calendar or clock units.
        var unitQualifier = tcContext.Qualifiers.OfType<DeclaredQualifierMeta.TemporalUnit>().FirstOrDefault();
        if (unitQualifier is not null)
        {
            units = units.Where(u => u.Singular.Equals(unitQualifier.UnitName, StringComparison.OrdinalIgnoreCase)
                || u.Plural.Equals(unitQualifier.UnitName, StringComparison.OrdinalIgnoreCase));
        }
        else
        {
            var dimQualifier = tcContext.Qualifiers.OfType<DeclaredQualifierMeta.TemporalDimension>().FirstOrDefault();
            if (dimQualifier is not null)
                units = units.Where(u => UnitMatchesDimension(u, dimQualifier.Value));
        }

        // Show singular form when the last typed number is exactly 1; plural otherwise.
        var lastNumber = ExtractLastNumber(textBeforeCursor);
        var preferSingular = lastNumber == 1;

        return units.Select(entry =>
        {
            var label = preferSingular ? entry.Singular : entry.Plural;
            return CreateItem(label, "temporal unit", CompletionItemKind.Unit, CompletionSortGroup.TypedConstantSegment);
        });
    }

    private static bool UnitMatchesDimension(TemporalUnits.TemporalUnitEntry entry, PeriodDimension dimension) =>
        dimension switch
        {
            PeriodDimension.Date => entry.IsCalendarBased,
            PeriodDimension.Time => !entry.IsCalendarBased,
            _ => true,
        };

    private static IEnumerable<CompletionItem> BuildTemporalContinuationItem()
    {
        yield return new CompletionItem
        {
            Label = "+",
            InsertText = " + ",
            InsertTextFormat = InsertTextFormat.PlainText,
            Detail = "continue temporal literal",
            Documentation = new StringOrMarkupContent("Add another <number> <temporal unit> segment."),
            Kind = CompletionItemKind.Operator,
            SortText = CreateSortText(CompletionSortGroup.TypedConstantSegment, "+"),
        };
    }

    private static IEnumerable<CompletionItem> GetMoneySlotItems(
        TypedConstantContext tcContext,
        string textBeforeCursor,
        TypedConstantPhase phase)
    {
        if (phase is not (TypedConstantPhase.AfterNumberSpace or TypedConstantPhase.UnitTyping))
            return [];

        IEnumerable<string> codes = CurrencyCatalog.All.Keys;
        var currencyQualifier = tcContext.Qualifiers.OfType<DeclaredQualifierMeta.Currency>().FirstOrDefault();
        if (currencyQualifier is not null)
            codes = codes.Where(c => c.Equals(currencyQualifier.CurrencyCode, StringComparison.OrdinalIgnoreCase));

        return codes.Select(code => CreateItem(code, "money code", CompletionItemKind.Unit, CompletionSortGroup.TypedConstantSegment));
    }

    private static IEnumerable<CompletionItem> GetQuantitySlotItems(
        TypedConstantContext tcContext,
        string textBeforeCursor,
        TypedConstantPhase phase)
    {
        if (phase is not (TypedConstantPhase.AfterNumberSpace or TypedConstantPhase.UnitTyping))
            return [];

        var unitQualifier = tcContext.Qualifiers.OfType<DeclaredQualifierMeta.Unit>().FirstOrDefault();
        if (unitQualifier is not null)
        {
            var atom = UcumCatalog.LookupAtom(unitQualifier.UnitCode);
            var label = atom?.PrintSymbol ?? unitQualifier.UnitCode;
            var detail = atom?.Name ?? "quantity unit";
            return [CreateItem(label, detail, CompletionItemKind.Unit, CompletionSortGroup.TypedConstantSegment, insertText: unitQualifier.UnitCode)];
        }

        IEnumerable<UcumAtom> units = UcumCatalog.BrowseTier1();

        var dimQualifier = tcContext.Qualifiers.OfType<DeclaredQualifierMeta.Dimension>().FirstOrDefault();
        if (dimQualifier is not null && DimensionCatalog.All.TryGetValue(dimQualifier.DimensionName, out var dimAlias))
            units = units.Where(u => u.Vector == dimAlias.Vector);

        return units.Select(u =>
        {
            var label = u.PrintSymbol ?? u.Code;
            return CreateItem(label, u.Name, CompletionItemKind.Unit, CompletionSortGroup.TypedConstantSegment, insertText: u.Code);
        });
    }

    private static string[]? GetQualifierAllowedValues(TypedConstantContext tcContext)
    {
        // For qualifier-aware filtering on closed-set types (boolean), return allowed values if qualifiers constrain them
        // Currently no qualifier axes pin boolean values, so return null (no filtering).
        return null;
    }

    private static IEnumerable<CompletionItem> GetCurrentEventArgItems(Compilation compilation, Position position)
    {
        var eventName = SlotContextResolver.GetCurrentEventName(compilation, position);
        if (eventName is null || !compilation.Semantics.EventsByName.TryGetValue(eventName, out var currentEvent))
        {
            return [];
        }

        return currentEvent.Args
            .Select(arg => CreateItem(arg.Name, "Event argument", CompletionItemKind.Variable, CompletionSortGroup.SemanticArgument));
    }

    private static IEnumerable<ModifierMeta> GetModifiers(ModifierDomain domain) => domain switch
    {
        ModifierDomain.Field => Modifiers.All.OfType<ValueModifierMeta>(),
        ModifierDomain.State => Modifiers.All.OfType<StateModifierMeta>(),
        ModifierDomain.Event => Modifiers.All.OfType<EventModifierMeta>(),
        ModifierDomain.Access => Modifiers.All.OfType<AccessModifierMeta>(),
        ModifierDomain.Anchor => Modifiers.All.OfType<AnchorModifierMeta>(),
        _ => [],
    };

    private static IEnumerable<ModifierMeta> GetModifiers(Compilation compilation, Position position, ModifierDomain domain)
    {
        var modifiers = GetModifiers(domain);

        // Collect modifier token kinds that are already present on the current declaration line
        // so we can suppress already-applied modifiers from the completion list.
        var alreadyApplied = GetAlreadyAppliedModifierKinds(compilation.Tokens.Tokens, position, domain);

        if (domain != ModifierDomain.Field
            || !TryGetCurrentValueModifierContext(compilation, position, out var resolvedType, out var appliedModifiers, out var declarationSite))
        {
            return modifiers
                .Where(meta => !alreadyApplied.Contains(meta.Token.Kind));
        }

        return modifiers
            .OfType<ValueModifierMeta>()
            .Where(meta => meta.ApplicableDeclarationSites.HasFlag(declarationSite))
            .Where(meta => meta.ApplicableTo.Length == 0 || IsTypeApplicable(meta.ApplicableTo, resolvedType, appliedModifiers))
            .Where(meta => !alreadyApplied.Contains(meta.Token.Kind));
    }

    /// <summary>
    /// Scans backward from the cursor through tokens on the same declaration line to find
    /// which modifier token kinds are already present. Stops when it hits the construct's
    /// primary leading token (e.g. 'state', 'event', 'field') or a different line.
    /// </summary>
    private static ImmutableHashSet<TokenKind> GetAlreadyAppliedModifierKinds(
        ImmutableArray<Token> tokens,
        Position position,
        ModifierDomain domain)
    {
        // Collect the token kinds for all modifiers in this domain so we can recognise them.
        var domainModifierTokenKinds = GetModifiers(domain)
            .Select(meta => meta.Token.Kind)
            .ToImmutableHashSet();

        var result = ImmutableHashSet.CreateBuilder<TokenKind>();

        var tokenIndex = FindTokenAtOrBeforeCursor(tokens, position);
        if (tokenIndex < 0)
        {
            return result.ToImmutable();
        }

        var cursorLine = position.Line;

        // Walk backward, collecting modifier tokens on the same line.
        // Stop when we reach a token on a different line (newline boundary) or a non-modifier
        // leading keyword that started this declaration.
        for (var i = tokenIndex; i >= 0; i--)
        {
            var tok = tokens[i];
            if (tok.Span.StartLine < cursorLine)
            {
                break;
            }

            if (domainModifierTokenKinds.Contains(tok.Kind))
            {
                result.Add(tok.Kind);
            }
        }

        return result.ToImmutable();
    }

    private static bool IsTypeApplicable(TypeTarget[] applicableTo, TypeKind resolvedType, ImmutableArray<ModifierKind> modifiers)
    {
        foreach (var target in applicableTo)
        {
            if (target.Kind is not null && target.Kind != resolvedType)
            {
                continue;
            }

            if (target is ModifiedTypeTarget modified)
            {
                if (modified.RequiredModifiers.All(modifiers.Contains))
                {
                    return true;
                }

                continue;
            }

            return true;
        }

        return false;
    }

    internal static string GetFunctionLabel(FunctionMeta meta) => meta.Name;

    internal static string GetAccessorLabel(TypeAccessor accessor) => accessor.Name;

    private static bool TryGetTypedConstantContext(
        Compilation compilation,
        Position position,
        SlotContext context,
        out TypedConstantContext tcContext)
    {
        var currentConstant = TypedConstantCollector.FindAtPosition(compilation.Semantics, position);
        if (currentConstant is not null)
        {
            // Typed constant already resolved — we know the type but not the declaring field's qualifiers.
            // Attempt to recover qualifiers from the enclosing field if possible.
            if (TryGetEnclosingField(compilation, position, out var enclosingField))
            {
                tcContext = TypedConstantContext.FromField(enclosingField);
                return true;
            }
            tcContext = TypedConstantContext.FromType(currentConstant.ResultType);
            return true;
        }

        if (context == SlotContext.InArgDefault && TryGetCurrentEventArg(compilation, position, out var arg))
        {
            tcContext = TypedConstantContext.FromArg(arg);
            return true;
        }

        if (TryGetCallParameterType(compilation, position, out var callParamType))
        {
            tcContext = TypedConstantContext.FromType(callParamType);
            return true;
        }

        var targetField = SlotContextResolver.GetCurrentActionTargetField(compilation, position);
        if (targetField is not null)
        {
            tcContext = TypedConstantContext.FromField(targetField);
            return true;
        }

        if (TryGetDeclarationQualifierContext(compilation, position, out tcContext))
        {
            return true;
        }

        if (context == SlotContext.InExpression && TryGetEnclosingField(compilation, position, out var exprField))
        {
            tcContext = TypedConstantContext.FromField(exprField);
            return true;
        }

        if (context == SlotContext.InExpression
            && TryGetBinaryPeerOperandType(compilation, position, out var peerType, out var peerQualifiers))
        {
            tcContext = new TypedConstantContext(peerType, peerQualifiers);
            return true;
        }

        tcContext = default;
        return false;
    }

    private static bool TryGetDeclarationQualifierContext(
        Compilation compilation,
        Position position,
        out TypedConstantContext tcContext)
    {
        if (!TryGetCurrentDeclarationType(compilation, position, out var declaredType)
            || !TryGetQualifierSitePreposition(compilation.Tokens.Tokens, position, out var preposition))
        {
            tcContext = default;
            return false;
        }

        var qualifierSlot = Types.GetMeta(declaredType).QualifierShape?.Slots
            .FirstOrDefault(slot => slot.Preposition == preposition);
        if (qualifierSlot is null
            || !TryMapQualifierAxisToExpectedType(qualifierSlot.Axis, out var expectedType))
        {
            tcContext = default;
            return false;
        }

        tcContext = TypedConstantContext.FromType(expectedType);
        return true;
    }

    private static bool TryGetCurrentDeclarationType(Compilation compilation, Position position, out TypeKind declaredType)
    {
        if (TryGetEnclosingField(compilation, position, out var field))
        {
            declaredType = field.ResolvedType;
            return true;
        }

        if (TryGetCurrentEventArg(compilation, position, out var arg))
        {
            declaredType = arg.ResolvedType;
            return true;
        }

        declaredType = default;
        return false;
    }

    private static bool TryGetQualifierSitePreposition(
        ImmutableArray<Token> tokens,
        Position position,
        out TokenKind preposition)
    {
        var tokenIndex = FindTokenAtOrBeforeCursor(tokens, position);
        if (tokenIndex < 0)
        {
            preposition = default;
            return false;
        }

        tokenIndex = AdjustTokenIndexForBoundary(tokens, tokenIndex, position);
        tokenIndex = FindPreviousSignificantToken(tokens, tokenIndex);
        if (tokenIndex < 0)
        {
            preposition = default;
            return false;
        }

        if (IsTypedConstantToken(tokens[tokenIndex].Kind))
        {
            tokenIndex = FindPreviousSignificantToken(tokens, tokenIndex - 1);
            if (tokenIndex < 0)
            {
                preposition = default;
                return false;
            }
        }

        preposition = tokens[tokenIndex].Kind;
        return preposition is TokenKind.In or TokenKind.Of or TokenKind.To;
    }

    private static bool TryMapQualifierAxisToExpectedType(QualifierAxis axis, out TypeKind expectedType)
    {
        switch (axis)
        {
            case QualifierAxis.Currency:
            case QualifierAxis.FromCurrency:
            case QualifierAxis.ToCurrency:
                expectedType = TypeKind.Currency;
                return true;
            case QualifierAxis.Unit:
                expectedType = TypeKind.UnitOfMeasure;
                return true;
            case QualifierAxis.Dimension:
                expectedType = TypeKind.Dimension;
                return true;
            case QualifierAxis.Timezone:
                expectedType = TypeKind.Timezone;
                return true;
            // TODO: PriceIn is polymorphic — should offer both currency and unit completions.
            // Currently mapped to Currency only as a pragmatic first pass; follow-up required.
            case QualifierAxis.PriceIn:
                expectedType = TypeKind.Currency;
                return true;
            default:
                expectedType = default;
                return false;
        }
    }

    private static bool TryGetCallParameterType(Compilation compilation, Position position, out TypeKind expectedType)
    {
        if (!CallContextResolver.TryFindActiveCall(compilation, position, out var call))
        {
            expectedType = default;
            return false;
        }

        if (call.IsAccessor)
        {
            if (call.ReceiverType is not { } receiverType)
            {
                expectedType = default;
                return false;
            }

            var accessor = Types.GetMeta(receiverType).Accessors.FirstOrDefault(candidate =>
                string.Equals(candidate.Name, call.Name, StringComparison.Ordinal));
            if (accessor is null)
            {
                expectedType = default;
                return false;
            }

            if (accessor is FixedReturnAccessor fixedReturn && fixedReturn.ParameterType is { } fixedParameterType)
            {
                expectedType = fixedParameterType;
                return true;
            }

            if (accessor.ParameterType is { } parameterType)
            {
                expectedType = parameterType;
                return true;
            }

            expectedType = default;
            return false;
        }

        var parameterKinds = Functions.FindByName(call.Name)
            .ToArray()
            .SelectMany(meta => meta.Overloads)
            .Where(overload => overload.Parameters.Count > call.ActiveParameter)
            .Select(overload => overload.Parameters[call.ActiveParameter].Kind)
            .Distinct()
            .ToArray();

        if (parameterKinds.Length == 1)
        {
            expectedType = parameterKinds[0];
            return true;
        }

        expectedType = default;
        return false;
    }

    private static bool TryGetBinaryPeerOperandType(
        Compilation compilation,
        Position position,
        out TypeKind expectedType,
        out ImmutableArray<DeclaredQualifierMeta> qualifiers)
    {
        var tokens = compilation.Tokens.Tokens;
        var tokenIndex = FindTokenAtOrBeforeCursor(tokens, position);
        if (tokenIndex < 0)
        {
            expectedType = default;
            qualifiers = ImmutableArray<DeclaredQualifierMeta>.Empty;
            return false;
        }

        tokenIndex = AdjustTokenIndexForBoundary(tokens, tokenIndex, position);
        tokenIndex = FindPreviousSignificantToken(tokens, tokenIndex);
        if (tokenIndex >= 0 && IsTypedConstantToken(tokens[tokenIndex].Kind))
        {
            tokenIndex = FindPreviousSignificantToken(tokens, tokenIndex - 1);
        }

        if (tokenIndex < 0
            || !Operators.ByToken.ContainsKey((tokens[tokenIndex].Kind, Arity.Binary)))
        {
            expectedType = default;
            qualifiers = ImmutableArray<DeclaredQualifierMeta>.Empty;
            return false;
        }

        return TryResolveExpressionTypeEndingAtToken(
            compilation,
            FindPreviousSignificantToken(tokens, tokenIndex - 1),
            out expectedType,
            out qualifiers);
    }

    private static bool TryResolveExpressionTypeEndingAtToken(
        Compilation compilation,
        int tokenIndex,
        out TypeKind expectedType,
        out ImmutableArray<DeclaredQualifierMeta> qualifiers)
    {
        var tokens = compilation.Tokens.Tokens;
        tokenIndex = FindPreviousSignificantToken(tokens, tokenIndex);
        if (tokenIndex < 0)
        {
            expectedType = default;
            qualifiers = ImmutableArray<DeclaredQualifierMeta>.Empty;
            return false;
        }

        var token = tokens[tokenIndex];
        switch (token.Kind)
        {
            case TokenKind.Identifier:
                var dotIndex = FindPreviousSignificantToken(tokens, tokenIndex - 1);
                if (dotIndex >= 0 && tokens[dotIndex].Kind == TokenKind.Dot)
                {
                    var receiverIndex = FindPreviousSignificantToken(tokens, dotIndex - 1);
                    if (receiverIndex >= 0
                        && TryResolveMemberExpressionType(compilation, receiverIndex, token.Text, out expectedType, out qualifiers))
                    {
                        return true;
                    }
                }

                return TryResolveIdentifierType(compilation, token.Text, token.Span, out expectedType, out qualifiers);

            case TokenKind.RightParen:
                return TryResolveParenthesizedExpressionType(compilation, tokenIndex, out expectedType, out qualifiers);

            case TokenKind.StringLiteral:
                expectedType = TypeKind.String;
                qualifiers = ImmutableArray<DeclaredQualifierMeta>.Empty;
                return true;

            case TokenKind.True:
            case TokenKind.False:
                expectedType = TypeKind.Boolean;
                qualifiers = ImmutableArray<DeclaredQualifierMeta>.Empty;
                return true;

            case TokenKind.NumberLiteral:
                expectedType = token.Text.Contains('.') ? TypeKind.Decimal : TypeKind.Integer;
                qualifiers = ImmutableArray<DeclaredQualifierMeta>.Empty;
                return true;

            default:
                expectedType = default;
                qualifiers = ImmutableArray<DeclaredQualifierMeta>.Empty;
                return false;
        }
    }

    private static bool TryResolveParenthesizedExpressionType(
        Compilation compilation,
        int closeParenIndex,
        out TypeKind expectedType,
        out ImmutableArray<DeclaredQualifierMeta> qualifiers)
    {
        var tokens = compilation.Tokens.Tokens;
        var openParenIndex = FindMatchingOpenParen(tokens, closeParenIndex);
        if (openParenIndex < 0)
        {
            expectedType = default;
            qualifiers = ImmutableArray<DeclaredQualifierMeta>.Empty;
            return false;
        }

        var nameIndex = FindPreviousSignificantToken(tokens, openParenIndex - 1);
        if (nameIndex >= 0 && TryGetCallableName(tokens[nameIndex], out var callableName))
        {
            var qualifierIndex = FindPreviousSignificantToken(tokens, nameIndex - 1);
            if (qualifierIndex >= 0 && tokens[qualifierIndex].Kind == TokenKind.Dot)
            {
                var receiverIndex = FindPreviousSignificantToken(tokens, qualifierIndex - 1);
                if (receiverIndex >= 0
                    && TryResolveExpressionTypeEndingAtToken(compilation, receiverIndex, out var receiverType, out _)
                    && TryResolveAccessorResultType(receiverType, callableName, out expectedType))
                {
                    qualifiers = ImmutableArray<DeclaredQualifierMeta>.Empty;
                    return true;
                }
            }
            else
            {
                if (qualifierIndex >= 0 && tokens[qualifierIndex].Kind == TokenKind.Tilde)
                {
                    callableName = "~" + callableName;
                }

                if (TryResolveFunctionResultType(callableName, CountArguments(tokens, openParenIndex, closeParenIndex), out expectedType))
                {
                    qualifiers = ImmutableArray<DeclaredQualifierMeta>.Empty;
                    return true;
                }
            }
        }

        return TryResolveExpressionTypeEndingAtToken(compilation, closeParenIndex - 1, out expectedType, out qualifiers);
    }

    private static bool TryResolveIdentifierType(
        Compilation compilation,
        string name,
        SourceSpan span,
        out TypeKind expectedType,
        out ImmutableArray<DeclaredQualifierMeta> qualifiers)
    {
        var position = new Position(span.StartLine - 1, Math.Max(span.StartColumn - 1, 0));
        var eventName = SlotContextResolver.GetCurrentEventName(compilation, position);
        if (eventName is not null
            && compilation.Semantics.EventsByName.TryGetValue(eventName, out var currentEvent))
        {
            var arg = currentEvent.Args.FirstOrDefault(candidate => string.Equals(candidate.Name, name, StringComparison.Ordinal));
            if (arg is not null)
            {
                expectedType = arg.ResolvedType;
                qualifiers = arg.DeclaredQualifiers;
                return true;
            }
        }

        if (compilation.Semantics.FieldsByName.TryGetValue(name, out var field))
        {
            expectedType = field.ResolvedType;
            qualifiers = field.DeclaredQualifiers;
            return true;
        }

        expectedType = default;
        qualifiers = ImmutableArray<DeclaredQualifierMeta>.Empty;
        return false;
    }

    private static bool TryResolveMemberExpressionType(
        Compilation compilation,
        int receiverIndex,
        string memberName,
        out TypeKind expectedType,
        out ImmutableArray<DeclaredQualifierMeta> qualifiers)
    {
        var tokens = compilation.Tokens.Tokens;
        receiverIndex = FindPreviousSignificantToken(tokens, receiverIndex);
        if (receiverIndex < 0)
        {
            expectedType = default;
            qualifiers = ImmutableArray<DeclaredQualifierMeta>.Empty;
            return false;
        }

        var receiverToken = tokens[receiverIndex];
        if (receiverToken.Kind == TokenKind.Identifier
            && compilation.Semantics.EventsByName.TryGetValue(receiverToken.Text, out var evt))
        {
            var arg = evt.Args.FirstOrDefault(candidate => string.Equals(candidate.Name, memberName, StringComparison.Ordinal));
            if (arg is not null)
            {
                expectedType = arg.ResolvedType;
                qualifiers = arg.DeclaredQualifiers;
                return true;
            }
        }

        if (!TryResolveExpressionTypeEndingAtToken(compilation, receiverIndex, out var receiverType, out _))
        {
            expectedType = default;
            qualifiers = ImmutableArray<DeclaredQualifierMeta>.Empty;
            return false;
        }

        qualifiers = ImmutableArray<DeclaredQualifierMeta>.Empty;
        return TryResolveAccessorResultType(receiverType, memberName, out expectedType);
    }

    private static bool TryResolveAccessorResultType(TypeKind receiverType, string memberName, out TypeKind expectedType)
    {
        var accessor = Types.GetMeta(receiverType).Accessors.FirstOrDefault(candidate =>
            string.Equals(candidate.Name, memberName, StringComparison.Ordinal));
        if (accessor is FixedReturnAccessor fixedReturn)
        {
            expectedType = fixedReturn.Returns;
            return true;
        }

        expectedType = default;
        return false;
    }

    private static bool TryResolveFunctionResultType(string functionName, int arity, out TypeKind expectedType)
    {
        var returnTypes = Functions.FindByName(functionName)
            .ToArray()
            .SelectMany(meta => meta.Overloads)
            .Where(overload => overload.Parameters.Count == arity)
            .Select(overload => overload.ReturnType)
            .Distinct()
            .ToArray();

        if (returnTypes.Length == 1)
        {
            expectedType = returnTypes[0];
            return true;
        }

        expectedType = default;
        return false;
    }

    private static int FindMatchingOpenParen(ImmutableArray<Token> tokens, int closeParenIndex)
    {
        var depth = 0;
        for (var index = closeParenIndex; index >= 0; index--)
        {
            switch (tokens[index].Kind)
            {
                case TokenKind.RightParen:
                    depth++;
                    break;
                case TokenKind.LeftParen:
                    depth--;
                    if (depth == 0)
                    {
                        return index;
                    }
                    break;
            }
        }

        return -1;
    }

    private static int CountArguments(ImmutableArray<Token> tokens, int openParenIndex, int closeParenIndex)
    {
        var argumentCount = 0;
        var sawArgumentToken = false;
        var nesting = 0;

        for (var index = openParenIndex + 1; index < closeParenIndex; index++)
        {
            var token = tokens[index];
            switch (token.Kind)
            {
                case TokenKind.LeftParen:
                    nesting++;
                    sawArgumentToken = true;
                    break;
                case TokenKind.RightParen when nesting > 0:
                    nesting--;
                    break;
                case TokenKind.Comma when nesting == 0:
                    argumentCount++;
                    break;
                default:
                    if (!Tokens.GetMeta(token.Kind).Categories.Contains(TokenCategory.Structural))
                    {
                        sawArgumentToken = true;
                    }
                    break;
            }
        }

        return sawArgumentToken ? argumentCount + 1 : 0;
    }

    private static bool TryGetEnclosingFieldType(Compilation compilation, Position position, out TypeKind expectedType)
    {
        if (!TryGetEnclosingField(compilation, position, out var field))
        {
            expectedType = default;
            return false;
        }

        expectedType = field.ResolvedType;
        return true;
    }

    private static bool TryGetCurrentEventArgType(Compilation compilation, Position position, out TypeKind expectedType)
    {
        if (!TryGetCurrentEventArg(compilation, position, out var arg))
        {
            expectedType = default;
            return false;
        }

        expectedType = arg.ResolvedType;
        return true;
    }

    private static bool TryGetCurrentValueModifierContext(
        Compilation compilation,
        Position position,
        out TypeKind resolvedType,
        out ImmutableArray<ModifierKind> appliedModifiers,
        out ValueModifierDeclarationSite declarationSite)
    {
        if (TryGetEnclosingField(compilation, position, out var field))
        {
            resolvedType = field.ResolvedType;
            appliedModifiers = field.Modifiers.Concat(field.ImpliedModifiers).ToImmutableArray();
            declarationSite = ValueModifierDeclarationSite.FieldDeclaration;
            return true;
        }

        if (TryGetCurrentEventArg(compilation, position, out var arg))
        {
            resolvedType = arg.ResolvedType;
            appliedModifiers = arg.Modifiers;
            declarationSite = ValueModifierDeclarationSite.EventArgDeclaration;
            return true;
        }

        resolvedType = default;
        appliedModifiers = ImmutableArray<ModifierKind>.Empty;
        declarationSite = default;
        return false;
    }

    private static bool TryGetEnclosingField(Compilation compilation, Position position, out TypedField field)
    {
        var construct = SlotContextResolver.GetEnclosingConstruct(compilation, position);
        field = compilation.Semantics.Fields.FirstOrDefault(candidate => candidate.Syntax == construct)!;
        return field is not null;
    }

    private static bool TryGetCurrentEventArg(Compilation compilation, Position position, out TypedArg arg)
    {
        var construct = SlotContextResolver.GetEnclosingConstruct(compilation, position);
        var currentEvent = compilation.Semantics.Events.FirstOrDefault(candidate => candidate.Syntax == construct);
        if (currentEvent is null)
        {
            arg = null!;
            return false;
        }

        var argName = TryGetCurrentEventArgName(compilation, position);
        if (argName is null)
        {
            arg = null!;
            return false;
        }

        arg = currentEvent.Args.FirstOrDefault(candidate => string.Equals(candidate.Name, argName, StringComparison.Ordinal))!;
        if (arg is null)
        {
            arg = null!;
            return false;
        }

        return true;
    }

    private static string? TryGetCurrentEventArgName(Compilation compilation, Position position)
    {
        var tokens = compilation.Tokens.Tokens;
        var tokenIndex = FindTokenAtOrBeforeCursor(tokens, position);
        if (tokenIndex < 0)
        {
            return null;
        }

        tokenIndex = AdjustTokenIndexForBoundary(tokens, tokenIndex, position);
        var sawAs = false;

        for (var index = tokenIndex; index >= 0; index--)
        {
            var token = tokens[index];
            var categories = Tokens.GetMeta(token.Kind).Categories;
            if (categories.Contains(TokenCategory.Structural))
            {
                continue;
            }

            if (token.Kind is TokenKind.Comma or TokenKind.LeftParen)
            {
                break;
            }

            if (sawAs && token.Kind == TokenKind.Identifier)
            {
                return token.Text;
            }

            if (token.Kind == TokenKind.As)
            {
                sawAs = true;
            }
        }

        return null;
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
            if (!Tokens.GetMeta(tokens[index].Kind).Categories.Contains(TokenCategory.Structural))
            {
                return index;
            }
        }

        return -1;
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

    private static bool IsInsideTypedConstantToken(ImmutableArray<Token> tokens, Position position)
    {
        var tokenIndex = FindTokenAtOrBeforeCursor(tokens, position);
        if (tokenIndex < 0)
        {
            return false;
        }

        var token = tokens[tokenIndex];
        return IsTypedConstantToken(token.Kind)
            && Contains(token.Span, position);
    }

    private static bool IsEmptyTypedConstantToken(ImmutableArray<Token> tokens, Position position) =>
        TryGetTypedConstantSlotPhase(tokens, position, out var phase, out _)
        && phase == TypedConstantPhase.Empty;

    // ── Typed-constant hole detection ─────────────────────────────────────────

    /// <summary>
    /// Returns true when the cursor sits inside a <c>{…}</c> interpolation hole within
    /// a typed constant — i.e., between a TypedConstantStart/Middle token (ending at the
    /// opening <c>{</c>) and the next TypedConstantMiddle/End token (starting at <c>}</c>).
    /// </summary>
    private static bool IsInsideTypedConstantHole(ImmutableArray<Token> tokens, Position position)
    {
        var inHole = false;

        foreach (var token in tokens)
        {
            switch (token.Kind)
            {
                case TokenKind.TypedConstant:
                    if (Contains(token.Span, position)) return false;
                    inHole = false;
                    break;

                case TokenKind.TypedConstantStart:
                    if (Contains(token.Span, position)) return false;
                    if (IsBefore(position, token.Span)) return false;
                    // Cursor is past this token's end (past `{`) → entered hole 0.
                    inHole = true;
                    break;

                case TokenKind.TypedConstantMiddle:
                    // If we're in a hole and cursor precedes this token's start (`}`) → cursor is in the hole.
                    if (inHole && IsBefore(position, token.Span)) return true;
                    if (Contains(token.Span, position)) return false;
                    if (IsBefore(position, token.Span)) return false;
                    // Cursor is past this token's end → entered the next hole.
                    inHole = true;
                    break;

                case TokenKind.TypedConstantEnd:
                    if (inHole && IsBefore(position, token.Span)) return true;
                    inHole = false;
                    break;
            }
        }

        return false;
    }

    /// <summary>
    /// Returns the 0-based index of the hole the cursor is currently inside, or -1 if the
    /// cursor is not inside any hole. Assumes <see cref="IsInsideTypedConstantHole"/> is true.
    /// </summary>
    private static int GetHoleIndex(ImmutableArray<Token> tokens, Position position)
    {
        var holeIndex = -1;

        foreach (var token in tokens)
        {
            switch (token.Kind)
            {
                case TokenKind.TypedConstant:
                    holeIndex = -1;
                    break;

                case TokenKind.TypedConstantStart:
                    if (IsBefore(position, token.Span) || Contains(token.Span, position)) return holeIndex;
                    holeIndex = 0;
                    break;

                case TokenKind.TypedConstantMiddle:
                    if (IsBefore(position, token.Span)) return holeIndex;
                    if (Contains(token.Span, position)) return holeIndex;
                    holeIndex = holeIndex < 0 ? 0 : holeIndex + 1;
                    break;

                case TokenKind.TypedConstantEnd:
                    if (IsBefore(position, token.Span)) return holeIndex;
                    holeIndex = -1;
                    break;
            }
        }

        return holeIndex;
    }

    // ── Hole completion items ──────────────────────────────────────────────────

    private static IEnumerable<CompletionItem> GetHoleItems(Compilation compilation, Position position)
    {
        var tokens = compilation.Tokens.Tokens;
        var holeIndex = GetHoleIndex(tokens, position);

        // Look up the already-computed InterpolatedTypedConstant from the semantic model.
        var interpolated = TypedConstantCollector.FindInterpolatedAtPosition(compilation.Semantics, position);
        if (interpolated is not null && holeIndex >= 0 && holeIndex < interpolated.Slots.Length)
        {
            var slotKind = interpolated.Slots[holeIndex].SlotKind;
            return GetHoleItemsForSlot(compilation, position, slotKind, interpolated.ResultType);
        }

        // Fallback: semantic model unavailable (file has errors) — return all fields/args.
        return GetCurrentEventArgItems(compilation, position)
            .Concat(GetFieldItems(compilation));
    }

    private static IEnumerable<CompletionItem> GetHoleItemsForSlot(
        Compilation compilation,
        Position position,
        InterpolationSlotKind slotKind,
        TypeKind outerType)
    {
        return slotKind switch
        {
            InterpolationSlotKind.Magnitude =>
                GetHoleFieldsOfTypes(compilation, position, TypeKind.Integer, TypeKind.Decimal, TypeKind.Number),
            InterpolationSlotKind.Currency or InterpolationSlotKind.FromCurrency or InterpolationSlotKind.ToCurrency =>
                GetHoleFieldsOfTypes(compilation, position, TypeKind.Currency),
            InterpolationSlotKind.Unit =>
                GetHoleFieldsOfTypes(compilation, position, TypeKind.UnitOfMeasure),
            InterpolationSlotKind.WholeValue =>
                GetHoleFieldsOfTypes(compilation, position, outerType),
            _ => GetCurrentEventArgItems(compilation, position).Concat(GetFieldItems(compilation)),
        };
    }

    private static IEnumerable<CompletionItem> GetHoleFieldsOfTypes(
        Compilation compilation,
        Position position,
        params TypeKind[] allowedTypes)
    {
        var typeSet = new HashSet<TypeKind>(allowedTypes);

        var eventName = SlotContextResolver.GetCurrentEventName(compilation, position);
        IEnumerable<CompletionItem> argItems = [];
        if (eventName is not null && compilation.Semantics.EventsByName.TryGetValue(eventName, out var currentEvent))
        {
            argItems = currentEvent.Args
                .Where(arg => typeSet.Contains(arg.ResolvedType))
                .Select(arg => CreateItem(arg.Name, "Event argument", CompletionItemKind.Variable, CompletionSortGroup.SemanticArgument));
        }

        var fieldItems = compilation.Semantics.Fields
            .Where(field => typeSet.Contains(field.ResolvedType))
            .Select(field => CreateItem(field.Name, "Field", CompletionItemKind.Field, CompletionSortGroup.SemanticSymbol));

        return argItems.Concat(fieldItems);
    }

    private static string WithClosingQuote(string content) => content + "'";

    private static int ExtractLastNumber(string textBeforeCursor)
    {
        // For compound segments (e.g. "2 hours + 1 " or "2 hours + 1 da"), look at the part after the last '+'.
        var lastPlusIdx = textBeforeCursor.LastIndexOf('+');
        var relevant = lastPlusIdx >= 0
            ? textBeforeCursor[(lastPlusIdx + 1)..].TrimStart()
            : textBeforeCursor;

        // Extract the leading integer from the relevant segment.
        var match = Regex.Match(relevant, @"^-?(\d+)");
        return match.Success && int.TryParse(match.Groups[1].Value, out var n) ? n : 0;
    }

    private static bool IsTypedConstantToken(TokenKind kind) =>
        kind is TokenKind.TypedConstant
            or TokenKind.TypedConstantStart
            or TokenKind.TypedConstantMiddle
            or TokenKind.TypedConstantEnd;

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

    private static CompletionList CreateCompletionList(IEnumerable<CompletionItem> items) =>
        new(items
            .OrderBy(item => item.SortText, StringComparer.Ordinal)
            .ThenBy(item => item.Label, StringComparer.Ordinal)
            .ToArray());

    private static CompletionItem CreateItem(
        string label,
        string detail,
        CompletionItemKind kind,
        CompletionSortGroup sortGroup,
        string? documentation = null,
        string? usageExample = null,
        string? snippetTemplate = null,
        string? insertText = null) =>
        new()
        {
            Label = label,
            InsertText = snippetTemplate ?? insertText ?? label,
            InsertTextFormat = snippetTemplate is null ? InsertTextFormat.PlainText : InsertTextFormat.Snippet,
            Documentation = CreateDocumentation(documentation, usageExample),
            SortText = CreateSortText(sortGroup, insertText ?? label),
            Detail = detail,
            Kind = kind,
        };

    private static StringOrMarkupContent? CreateDocumentation(string? description, string? usageExample)
    {
        if (string.IsNullOrWhiteSpace(description) && string.IsNullOrWhiteSpace(usageExample))
        {
            return null;
        }

        var markdown = string.IsNullOrWhiteSpace(usageExample)
            ? description!
            : $"{description}{Environment.NewLine}{Environment.NewLine}```precept{Environment.NewLine}{usageExample}{Environment.NewLine}```";

        return new StringOrMarkupContent(new MarkupContent
        {
            Kind = MarkupKind.Markdown,
            Value = markdown,
        });
    }

    private static string CreateSortText(CompletionSortGroup sortGroup, string label) =>
        $"{(int)sortGroup:D2}:{label}";

    private static IEnumerable<CompletionItem> DistinctByLabel(IEnumerable<CompletionItem> items) =>
        items.GroupBy(item => item.Label, StringComparer.Ordinal)
            .Select(group => group.First());

    private enum CompletionSortGroup
    {
        SemanticArgument = 0,
        SemanticSymbol = 1,
        TypedConstant = 2,
        TypedConstantSegment = 3,
        Type = 10,
        Keyword = 11,
        Function = 12,
        Action = 13,
    }
}

