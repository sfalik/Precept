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

        if (RichHoverFactory.TryCreateProofHover(compilation, position, token.Value, out var proofHover))
        {
            return proofHover;
        }

        if (TryCreateTypeHover(compilation, token.Value, out var typeHover))
        {
            return typeHover;
        }

        if (TryCreateActionHover(token.Value, out var actionHover))
        {
            return actionHover;
        }

        if (TryCreateOperatorHover(compilation, position, token.Value, out var operatorHover))
        {
            return operatorHover;
        }

        if (TryCreateFunctionHover(compilation, position, token.Value, out var functionHover))
        {
            return functionHover;
        }

        if (TryCreateTypedConstantHover(compilation.Semantics, position, out var constantHover))
        {
            return constantHover;
        }

        if (TryCreateAccessorHover(compilation, position, token.Value, out var accessorHover))
        {
            return accessorHover;
        }

        if (RichHoverFactory.TryCreateHover(compilation, position, token.Value, out var richHover))
        {
            return richHover;
        }

        if (token.Value.Kind == TokenKind.Identifier)
        {
            return TryIdentifierHover(compilation.Semantics, token.Value, position);
        }

        if (HasRicherCatalogOwner(compilation, position, token.Value))
        {
            return null;
        }

        var meta = Tokens.GetMeta(token.Value.Kind);
        if (meta.Text is not null)
        {
            return MakeHover($"**{meta.Text}**\n\n{meta.Description}", token.Value.Span);
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

    private static bool TryCreateTypeHover(Compilation compilation, Token token, out Hover hover)
    {
        hover = null!;

        if (token.Kind == TokenKind.Set && !SlotContextResolver.IsSetInTypePosition(compilation, token))
        {
            return false;
        }

        if (!Types.ByToken.TryGetValue(token.Kind, out var typeMeta))
        {
            return false;
        }

        hover = MakeHover(CreateTypeMarkdown(typeMeta), token.Span);
        return true;
    }

    private static bool TryCreateActionHover(Token token, out Hover hover)
    {
        hover = null!;

        if (!Actions.ByTokenKind.TryGetValue(token.Kind, out var actionMeta))
        {
            return false;
        }

        hover = MakeHover(CreateActionMarkdown(actionMeta), token.Span);
        return true;
    }

    private static bool TryCreateOperatorHover(Compilation compilation, Position position, Token token, out Hover hover)
    {
        hover = null!;

        if (!TryGetOperatorMeta(compilation, position, token, out var operatorMeta))
        {
            return false;
        }

        hover = MakeHover(CreateOperatorMarkdown(operatorMeta), token.Span);
        return true;
    }

    private static bool TryCreateFunctionHover(Compilation compilation, Position position, Token token, out Hover hover)
    {
        hover = null!;

        if (!SemanticExpressionLocator.TryFindFunctionAt(compilation, position, out var overloads))
        {
            return false;
        }

        hover = MakeHover(CreateFunctionMarkdown(overloads), token.Span);
        return true;
    }

    private static bool TryCreateTypedConstantHover(SemanticIndex semantics, Position position, out Hover hover)
    {
        hover = null!;

        var constant = TypedConstantCollector.FindAtPosition(semantics, position);
        if (constant is null)
        {
            return false;
        }

        hover = MakeHover(CreateTypedConstantMarkdown(constant), constant.Span);
        return true;
    }

    private static bool TryCreateAccessorHover(Compilation compilation, Position position, Token token, out Hover hover)
    {
        hover = null!;

        if (!SemanticExpressionLocator.TryFindAccessorAt(compilation, position, out var ownerType, out var accessor))
        {
            return false;
        }

        TypeKind? resultType = null;
        if (SemanticExpressionLocator.TryFindExpressionAt(compilation.Semantics, position, out var expression)
            && expression is TypedMemberAccess memberAccess)
        {
            resultType = memberAccess.ResultType;
        }

        hover = MakeHover(CreateAccessorMarkdown(ownerType, accessor, resultType), token.Span);
        return true;
    }

    private static bool TryGetOperatorMeta(Compilation compilation, Position position, Token token, out OperatorMeta operatorMeta)
    {
        operatorMeta = null!;

        if (SemanticExpressionLocator.TryFindExpressionAt(compilation.Semantics, position, out var expression))
        {
            if (expression is TypedPostfixOp && TryGetMultiTokenOperatorMeta(compilation.Tokens.Tokens, position, out operatorMeta))
            {
                return true;
            }

            var arity = expression switch
            {
                TypedUnaryOp => Arity.Unary,
                TypedBinaryOp => Arity.Binary,
                _ => (Arity?)null,
            };

            if (arity is { } resolvedArity
                && Operators.ByToken.TryGetValue((token.Kind, resolvedArity), out var resolvedOperator))
            {
                operatorMeta = resolvedOperator;
                return true;
            }
        }

        if (TryGetMultiTokenOperatorMeta(compilation.Tokens.Tokens, position, out operatorMeta))
        {
            return true;
        }

        return TryGetUniqueSingleTokenOperatorMeta(token.Kind, out operatorMeta);
    }

    private static bool TryGetMultiTokenOperatorMeta(
        ImmutableArray<Token> tokens,
        Position position,
        out OperatorMeta operatorMeta)
    {
        operatorMeta = null!;

        var tokenIndex = FindTokenIndexAt(tokens, position);
        if (tokenIndex < 0)
        {
            return false;
        }

        for (var start = tokenIndex - 2; start <= tokenIndex; start++)
        {
            if (start < 0)
            {
                continue;
            }

            var significant = new List<(int Index, TokenKind Kind)>(3);
            for (var index = start; index < tokens.Length && significant.Count < 3; index++)
            {
                if (tokens[index].Kind is TokenKind.NewLine or TokenKind.EndOfSource)
                {
                    continue;
                }

                significant.Add((index, tokens[index].Kind));
            }

            for (var length = Math.Min(significant.Count, 3); length >= 2; length--)
            {
                if (!significant.Take(length).Any(candidate => candidate.Index == tokenIndex))
                {
                    continue;
                }

                var candidate = Operators.ByTokenSequence(significant.Take(length).Select(value => value.Kind).ToArray());
                if (candidate is not null)
                {
                    operatorMeta = candidate;
                    return true;
                }
            }
        }

        return false;
    }

    private static bool TryGetUniqueSingleTokenOperatorMeta(TokenKind tokenKind, out OperatorMeta operatorMeta)
    {
        operatorMeta = null!;

        var matches = new[]
        {
            TryGetOperatorMeta(tokenKind, Arity.Unary),
            TryGetOperatorMeta(tokenKind, Arity.Binary),
            TryGetOperatorMeta(tokenKind, Arity.Postfix),
        }
        .Where(meta => meta is not null)
        .Cast<OperatorMeta>()
        .Distinct()
        .ToArray();

        if (matches.Length != 1)
        {
            return false;
        }

        operatorMeta = matches[0];
        return true;
    }

    private static OperatorMeta? TryGetOperatorMeta(TokenKind tokenKind, Arity arity) =>
        Operators.ByToken.TryGetValue((tokenKind, arity), out var operatorMeta)
            ? operatorMeta
            : null;

    private static bool HasRicherCatalogOwner(Compilation compilation, Position position, Token token)
    {
        if (token.Kind == TokenKind.Set && SlotContextResolver.IsSetInTypePosition(compilation, token))
        {
            return true;
        }

        if (token.Kind != TokenKind.Set && Types.ByToken.ContainsKey(token.Kind))
        {
            return true;
        }

        if (Actions.ByTokenKind.ContainsKey(token.Kind))
        {
            return true;
        }

        return TryGetOperatorMeta(compilation, position, token, out _);
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

    private static string CreateTypeMarkdown(TypeMeta typeMeta) =>
        string.Join("\n\n", new[]
        {
            $"**{typeMeta.DisplayName}**",
            typeMeta.HoverDescription ?? typeMeta.Description,
        });

    private static string CreateActionMarkdown(ActionMeta actionMeta) =>
        string.Join("\n\n", new[]
        {
            $"**{actionMeta.Token.Text ?? actionMeta.Kind.ToString().ToLowerInvariant()}**",
            actionMeta.HoverDescription ?? actionMeta.Description,
        });

    private static string CreateOperatorMarkdown(OperatorMeta operatorMeta) =>
        string.Join("\n\n", new[]
        {
            $"**{FormatOperatorLabel(operatorMeta)}**",
            operatorMeta.HoverDescription ?? operatorMeta.Description,
        });

    private static string CreateFunctionMarkdown(IReadOnlyList<FunctionMeta> overloads)
    {
        var name = overloads[0].Name;
        var signatures = overloads
            .SelectMany(meta => meta.Overloads.Select(overload => $"`{FormatFunctionSignature(meta, overload)}`"))
            .Distinct()
            .ToArray();
        var descriptions = overloads
            .Select(meta => meta.HoverDescription ?? meta.Description)
            .Distinct()
            .ToArray();

        var sections = new List<string>
        {
            $"**function `{name}`**",
            string.Join("\n", signatures),
            string.Join("\n", descriptions),
        };

        return string.Join("\n\n", sections);
    }

    private static string CreateTypedConstantMarkdown(TypedTypedConstant constant)
    {
        var typeMeta = Types.GetMeta(constant.ResultType);
        var sections = new List<string>
        {
            $"**{typeMeta.DisplayName} typed constant**",
        };

        if (typeMeta.ContentValidation is { } validation)
        {
            sections.Add($"Format: {validation.FormatDescription}");
        }

        if (constant.ResultType is TypeKind.Quantity or TypeKind.UnitOfMeasure)
        {
            var unitCode = ExtractUnitCode(constant.RawText);
            if (unitCode is not null && UcumCatalog.LookupAtom(unitCode) is { } atom)
            {
                var symbolPart = atom.PrintSymbol is not null ? $" ({atom.PrintSymbol})" : string.Empty;
                sections.Add($"Unit: `{atom.Code}`{symbolPart} — {atom.Name}");
            }
        }

        return string.Join("\n\n", sections);
    }

    private static string? ExtractUnitCode(string rawText)
    {
        var lastSpace = rawText.LastIndexOf(' ');
        var unitCode = lastSpace >= 0 ? rawText[(lastSpace + 1)..] : rawText;
        unitCode = unitCode.Trim();
        return string.IsNullOrWhiteSpace(unitCode) ? null : unitCode;
    }

    private static string CreateAccessorMarkdown(TypeMeta ownerType, TypeAccessor accessor, TypeKind? resultType)
    {
        var sections = new List<string>
        {
            $"**{FormatAccessorLabel(ownerType, accessor)}**",
            accessor.Description,
        };

        if (resultType is { } resolvedResultType)
        {
            sections.Add($"Returns: `{Types.GetMeta(resolvedResultType).DisplayName}`");
        }

        return string.Join("\n\n", sections);
    }

    private static string FormatOperatorLabel(OperatorMeta operatorMeta) => operatorMeta switch
    {
        SingleTokenOp single => single.Token.Text ?? single.Kind.ToString(),
        MultiTokenOp multi => string.Join(" ", multi.Tokens.Select(token => token.Text ?? token.Kind.ToString())),
        _ => operatorMeta.Kind.ToString(),
    };

    private static string FormatFunctionSignature(FunctionMeta meta, FunctionOverload overload) =>
        $"{meta.Name}({string.Join(", ", overload.Parameters.Select(FormatParameter))}) -> {Types.GetMeta(overload.ReturnType).DisplayName}";

    private static string FormatParameter(ParameterMeta parameter) =>
        $"{parameter.Name ?? "value"} as {Types.GetMeta(parameter.Kind).DisplayName}";

    private static string FormatAccessorLabel(TypeMeta ownerType, TypeAccessor accessor) => accessor switch
    {
        FixedReturnAccessor fixedReturn when fixedReturn.ParameterType is { } parameterType =>
            $"{ownerType.DisplayName}.{accessor.Name}({Types.GetMeta(parameterType).DisplayName})",
        ElementParameterAccessor =>
            $"{ownerType.DisplayName}.{accessor.Name}(value)",
        _ when accessor.ParameterType is { } parameterType =>
            $"{ownerType.DisplayName}.{accessor.Name}({Types.GetMeta(parameterType).DisplayName})",
        _ => $"{ownerType.DisplayName}.{accessor.Name}",
    };

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

    private static int FindTokenIndexAt(ImmutableArray<Token> tokens, Position position)
    {
        for (var index = 0; index < tokens.Length; index++)
        {
            if (Contains(tokens[index].Span, position))
            {
                return index;
            }
        }

        return -1;
    }
}
