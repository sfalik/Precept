using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.Language;
using Precept.Pipeline;

namespace Precept.LanguageServer.Handlers;

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
            request.Position,
            request.Context?.TriggerCharacter));
    }

    private static CompletionList GetCompletions(Compilation compilation, Position position, string? triggerCharacter)
    {
        var context = SlotContextResolver.GetCursorContext(compilation, position);
        if (triggerCharacter == "'" && context is SlotContext.InTypePosition or SlotContext.InExpression or SlotContext.InArgDefault)
        {
            return CreateCompletionList(GetTypedConstantItems(compilation, position, context));
        }

        return context switch
        {
            SlotContext.TopLevel => CreateCompletionList(GetTopLevelItems()),
            SlotContext.AfterValueName => CreateCompletionList(GetTypeAnnotationItems()),
            SlotContext.InTypePosition => CreateCompletionList(GetTypeItems()),
            SlotContext.InModifierPosition => CreateModifierList(compilation, position),
            SlotContext.InStateTarget => CreateCompletionList(GetStateItems(compilation)),
            SlotContext.InEventTarget => CreateCompletionList(GetEventItems(compilation)),
            SlotContext.InFieldTarget => CreateCompletionList(GetFieldItems(compilation)),
            SlotContext.InActionVerb => CreateCompletionList(GetActionItems(compilation, position)),
            SlotContext.InExpression => CreateCompletionList(GetExpressionItems(compilation, position)),
            SlotContext.InArgDefault => CreateCompletionList(GetExpressionItems(compilation, position)),
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

        return CreateCompletionList(GetModifierItems(compilation, position, construct.Meta.ModifierDomain));
    }

    private static IEnumerable<CompletionItem> GetTopLevelItems() =>
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

    private static IEnumerable<CompletionItem> GetStateItems(Compilation compilation) =>
        compilation.Semantics.States
            .Select(state => state.Name)
            .Distinct(StringComparer.Ordinal)
            .Select(name => CreateItem(name, "State", CompletionItemKind.EnumMember, CompletionSortGroup.SemanticSymbol));

    private static IEnumerable<CompletionItem> GetEventItems(Compilation compilation) =>
        compilation.Semantics.Events
            .Select(evt => evt.Name)
            .Distinct(StringComparer.Ordinal)
            .Select(name => CreateItem(name, "Event", CompletionItemKind.Event, CompletionSortGroup.SemanticSymbol));

    private static IEnumerable<CompletionItem> GetFieldItems(Compilation compilation) =>
        compilation.Semantics.Fields
            .Select(field => field.Name)
            .Distinct(StringComparer.Ordinal)
            .Select(name => CreateItem(name, "Field", CompletionItemKind.Field, CompletionSortGroup.SemanticSymbol));

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
        var booleanLiteralItems = new[] { TokenKind.True, TokenKind.False }
            .Select(Tokens.GetMeta)
            .Select(meta => CreateItem(
                label: meta.Text ?? meta.Kind.ToString().ToLowerInvariant(),
                detail: meta.Description,
                kind: CompletionItemKind.Constant,
                sortGroup: CompletionSortGroup.Keyword,
                documentation: meta.Description));

        return eventArgItems
            .Concat(fieldItems)
            .Concat(functionItems)
            .Concat(booleanLiteralItems);
    }

    private static IEnumerable<CompletionItem> GetTypedConstantItems(Compilation compilation, Position position, SlotContext context)
    {
        if (!TryGetExpectedTypedConstantType(compilation, position, context, out var expectedType))
        {
            return [];
        }

        var typeMeta = Types.GetMeta(expectedType);
        var examples = typeMeta.ContentValidation?.Examples ?? [];
        var detail = typeMeta.ContentValidation?.FormatDescription ?? $"{typeMeta.DisplayName} typed constant";

        return DistinctByLabel(TypedConstantCollector
            .CollectByType(compilation.Semantics, expectedType)
            .Concat(examples)
            .Select(value => CreateItem(
                label: value,
                detail: detail,
                kind: CompletionItemKind.Constant,
                sortGroup: CompletionSortGroup.TypedConstant,
                documentation: detail)));
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
        if (domain != ModifierDomain.Field
            || !TryGetCurrentValueModifierContext(compilation, position, out var resolvedType, out var appliedModifiers, out var declarationSite))
        {
            return modifiers;
        }

        return modifiers
            .OfType<ValueModifierMeta>()
            .Where(meta => meta.ApplicableDeclarationSites.HasFlag(declarationSite))
            .Where(meta => meta.ApplicableTo.Length == 0 || IsTypeApplicable(meta.ApplicableTo, resolvedType, appliedModifiers));
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

    private static bool TryGetExpectedTypedConstantType(
        Compilation compilation,
        Position position,
        SlotContext context,
        out TypeKind expectedType)
    {
        var currentConstant = TypedConstantCollector.FindAtPosition(compilation.Semantics, position);
        if (currentConstant is not null)
        {
            expectedType = currentConstant.ResultType;
            return true;
        }

        if (context == SlotContext.InArgDefault && TryGetCurrentEventArgType(compilation, position, out expectedType))
        {
            return true;
        }

        if (context == SlotContext.InExpression && TryGetEnclosingFieldType(compilation, position, out expectedType))
        {
            return true;
        }

        var targetField = SlotContextResolver.GetCurrentActionTargetField(compilation, position);
        if (targetField is not null)
        {
            expectedType = targetField.ResolvedType;
            return true;
        }

        expectedType = default;
        return false;
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
        string? snippetTemplate = null) =>
        new()
        {
            Label = label,
            InsertText = snippetTemplate ?? label,
            InsertTextFormat = snippetTemplate is null ? InsertTextFormat.PlainText : InsertTextFormat.Snippet,
            Documentation = CreateDocumentation(documentation, usageExample),
            SortText = CreateSortText(sortGroup, label),
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
        Type = 10,
        Keyword = 11,
        Function = 12,
        Action = 13,
    }
}
