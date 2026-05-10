using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.Language;
using Precept.Pipeline;

namespace Precept.LanguageServer.Handlers;

internal sealed class HoverHandler : IHoverHandler
{
    private readonly DocumentStore _store;

    public HoverHandler(DocumentStore store)
    {
        _store = store;
    }

    public HoverRegistrationOptions GetRegistrationOptions(HoverCapability capability, ClientCapabilities clientCapabilities) =>
        new() { DocumentSelector = TextDocumentSelector.ForPattern("**/*.precept") };

    public void SetCapability(HoverCapability capability)
    {
    }

    public Task<Hover?> Handle(HoverParams request, CancellationToken cancellationToken)
    {
        if (!_store.TryGet(request.TextDocument.Uri, out var state) || state.Current is null)
        {
            return Task.FromResult<Hover?>(null);
        }

        return Task.FromResult(CreateHover(state.Current, request.Position));
    }

    internal static Hover? CreateHover(Compilation compilation, Position position)
    {
        var token = FindTokenAt(compilation.Tokens.Tokens, position);
        if (token is null)
        {
            return null;
        }

        var meta = Tokens.GetMeta(token.Value.Kind);
        if (meta.Text is not null)
        {
            return MakeHover($"**{meta.Text}**\n\n{meta.Description}", token.Value.Span);
        }

        if (token.Value.Kind == TokenKind.Identifier)
        {
            return TryIdentifierHover(compilation.Semantics, token.Value, position);
        }

        return null;
    }

    private static Token? FindTokenAt(ImmutableArray<Token> tokens, Position position)
    {
        foreach (var token in tokens)
        {
            if (token.Kind is TokenKind.NewLine or TokenKind.EndOfSource)
            {
                continue;
            }

            if (Contains(token.Span, position))
            {
                return token;
            }
        }

        return null;
    }

    private static Hover? TryIdentifierHover(SemanticIndex semantics, Token token, Position position)
    {
        if (TryFindArgument(semantics, token.Text, position, out var arg))
        {
            return MakeHover(CreateArgumentMarkdown(arg), token.Span);
        }

        if (TryFindField(semantics, token.Text, position, out var field))
        {
            return MakeHover(CreateFieldMarkdown(field), token.Span);
        }

        if (TryFindState(semantics, token.Text, position, out var state))
        {
            return MakeHover(CreateStateMarkdown(state), token.Span);
        }

        if (TryFindEvent(semantics, token.Text, position, out var evt))
        {
            return MakeHover(CreateEventMarkdown(evt), token.Span);
        }

        return null;
    }

    private static bool TryFindArgument(SemanticIndex semantics, string name, Position position, out TypedArg arg)
    {
        var candidate = semantics.Events
            .SelectMany(evt => evt.Args)
            .FirstOrDefault(argument =>
                string.Equals(argument.Name, name, StringComparison.Ordinal)
                && Contains(argument.Span, position));

        if (candidate is null)
        {
            arg = null!;
            return false;
        }

        arg = candidate;
        return true;
    }

    private static bool TryFindField(SemanticIndex semantics, string name, Position position, out TypedField field)
    {
        var candidate = semantics.Fields.FirstOrDefault(symbol =>
            string.Equals(symbol.Name, name, StringComparison.Ordinal)
            && Contains(symbol.NameSpan, position));

        if (candidate is not null)
        {
            field = candidate;
            return true;
        }

        var reference = semantics.FieldReferences.FirstOrDefault(candidate =>
            string.Equals(candidate.Field.Name, name, StringComparison.Ordinal)
            && Contains(candidate.Site, position));

        if (reference is not null)
        {
            field = reference.Field;
            return true;
        }

        return TryFindUniqueByName(
            name,
            semantics.FieldsByName,
            semantics.StatesByName,
            semantics.EventsByName,
            out field);
    }

    private static bool TryFindState(SemanticIndex semantics, string name, Position position, out TypedState state)
    {
        var candidate = semantics.States.FirstOrDefault(symbol =>
            string.Equals(symbol.Name, name, StringComparison.Ordinal)
            && Contains(symbol.NameSpan, position));

        if (candidate is not null)
        {
            state = candidate;
            return true;
        }

        var reference = semantics.StateReferences.FirstOrDefault(candidate =>
            string.Equals(candidate.State.Name, name, StringComparison.Ordinal)
            && Contains(candidate.Site, position));

        if (reference is not null)
        {
            state = reference.State;
            return true;
        }

        return TryFindUniqueByName(
            name,
            semantics.StatesByName,
            semantics.FieldsByName,
            semantics.EventsByName,
            out state);
    }

    private static bool TryFindEvent(SemanticIndex semantics, string name, Position position, out TypedEvent evt)
    {
        var candidate = semantics.Events.FirstOrDefault(symbol =>
            string.Equals(symbol.Name, name, StringComparison.Ordinal)
            && Contains(symbol.NameSpan, position));

        if (candidate is not null)
        {
            evt = candidate;
            return true;
        }

        var reference = semantics.EventReferences.FirstOrDefault(candidate =>
            string.Equals(candidate.Event.Name, name, StringComparison.Ordinal)
            && Contains(candidate.Site, position));

        if (reference is not null)
        {
            evt = reference.Event;
            return true;
        }

        return TryFindUniqueByName(
            name,
            semantics.EventsByName,
            semantics.FieldsByName,
            semantics.StatesByName,
            out evt);
    }

    private static bool TryFindUniqueByName<TPrimary, TOther1, TOther2>(
        string name,
        IReadOnlyDictionary<string, TPrimary> primary,
        IReadOnlyDictionary<string, TOther1> other1,
        IReadOnlyDictionary<string, TOther2> other2,
        out TPrimary value)
        where TPrimary : class
        where TOther1 : class
        where TOther2 : class
    {
        value = null!;

        var matchCount = 0;
        if (primary.TryGetValue(name, out var primaryValue))
        {
            matchCount++;
            value = primaryValue;
        }

        if (other1.ContainsKey(name))
        {
            matchCount++;
        }

        if (other2.ContainsKey(name))
        {
            matchCount++;
        }

        return matchCount == 1 && value is not null;
    }

    private static Hover MakeHover(string markdown, SourceSpan span) => new()
    {
        Contents = new MarkedStringsOrMarkupContent(new MarkupContent
        {
            Kind = MarkupKind.Markdown,
            Value = markdown,
        }),
        Range = DiagnosticProjector.ToRange(span),
    };

    private static string CreateFieldMarkdown(TypedField field)
    {
        var lines = new List<string>
        {
            $"**field `{field.Name}`**",
            $"Type: `{FormatType(field.ResolvedType, field.ElementType, field.KeyType)}`",
        };

        if (!field.Modifiers.IsDefaultOrEmpty)
        {
            lines.Add($"Modifiers: {FormatModifiers(field.Modifiers)}");
        }

        if (field.IsComputed)
        {
            lines.Add("Computed field");
        }

        return string.Join("\n\n", lines);
    }

    private static string CreateStateMarkdown(TypedState state)
    {
        var lines = new List<string>
        {
            $"**state `{state.Name}`**",
        };

        if (!state.Modifiers.IsDefaultOrEmpty)
        {
            lines.Add($"Modifiers: {FormatModifiers(state.Modifiers)}");
        }

        return string.Join("\n\n", lines);
    }

    private static string CreateEventMarkdown(TypedEvent evt)
    {
        var lines = new List<string>
        {
            $"**event `{evt.Name}`**",
        };

        if (evt.IsInitial)
        {
            lines.Add("Initial event");
        }

        if (!evt.Args.IsDefaultOrEmpty)
        {
            var args = string.Join(", ", evt.Args.Select(arg => $"`{arg.Name}`: `{FormatType(arg.ResolvedType, arg.ElementType)}`"));
            lines.Add($"Arguments: {args}");
        }

        return string.Join("\n\n", lines);
    }

    private static string CreateArgumentMarkdown(TypedArg arg) => string.Join("\n\n", new[]
    {
        $"**argument `{arg.Name}`**",
        $"Event: `{arg.EventName}`",
        $"Type: `{FormatType(arg.ResolvedType, arg.ElementType)}`",
    });

    private static string FormatModifiers(ImmutableArray<ModifierKind> modifiers) =>
        string.Join(", ", modifiers.Select(modifier => $"`{modifier.ToString().ToLowerInvariant()}`"));

    private static string FormatType(TypeKind kind, TypeKind? elementType = null, TypeKind? keyType = null)
    {
        var displayName = Types.GetMeta(kind).DisplayName;

        if (kind == TypeKind.Lookup && keyType is { } lookupKeyType && elementType is { } lookupValueType)
        {
            return $"{displayName} of {FormatType(lookupKeyType)} to {FormatType(lookupValueType)}";
        }

        if ((kind == TypeKind.QueueBy || kind == TypeKind.LogBy) && elementType is { } orderedItemType && keyType is { } orderKeyType)
        {
            return $"{displayName} of {FormatType(orderedItemType)} by {FormatType(orderKeyType)}";
        }

        if (elementType is { } itemType && kind is TypeKind.Set or TypeKind.Queue or TypeKind.Stack or TypeKind.Log or TypeKind.Bag or TypeKind.List)
        {
            return $"{displayName} of {FormatType(itemType)}";
        }

        return displayName;
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
