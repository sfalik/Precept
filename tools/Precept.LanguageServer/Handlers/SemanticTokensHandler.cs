using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using OmniSharp.Extensions.LanguageServer.Protocol.Server.Capabilities;
using Precept.Language;
using Precept.Pipeline;
using Compilation = Precept.Pipeline.Compilation;
using SemanticTokenTypesCatalog = Precept.Language.SemanticTokenTypes;
using TokenKind = Precept.Language.TokenKind;
using TokensCatalog = Precept.Language.Tokens;

namespace Precept.LanguageServer.Handlers;

internal sealed class SemanticTokensHandler : SemanticTokensHandlerBase
{
    private static readonly SemanticTokensLegend Legend = BuildLegend();
    internal const string SemanticTokenColorsNotificationName = "precept/semanticTokenColors";
    internal const string BuiltInFunctionTokenType = "function";
    internal const string BuiltInStringTokenType = "string";

    private readonly ConcurrentDictionary<DocumentUri, SemanticTokensDocument> _documents = new();
    private readonly DocumentStore _store;

    public SemanticTokensHandler(DocumentStore store)
    {
        _store = store;
    }

    protected override SemanticTokensRegistrationOptions CreateRegistrationOptions(
        SemanticTokensCapability capability,
        ClientCapabilities clientCapabilities) =>
        new()
        {
            DocumentSelector = TextDocumentSelector.ForPattern("**/*.precept"),
            Legend = Legend,
            Full = new SemanticTokensCapabilityRequestFull
            {
                Delta = true,
            },
            Range = new SemanticTokensCapabilityRequestRange(),
        };

    protected override Task Tokenize(
        SemanticTokensBuilder builder,
        ITextDocumentIdentifierParams identifier,
        CancellationToken cancellationToken)
    {
        if (!_store.TryGet(identifier.TextDocument.Uri, out var state) || state.Current is not { } compilation)
        {
            return Task.CompletedTask;
        }

        foreach (var token in ProjectMergedTokens(compilation))
        {
            builder.Push(token.Line, token.Character, token.Length, token.TokenType);
        }

        return Task.CompletedTask;
    }

    protected override Task<SemanticTokensDocument> GetSemanticTokensDocument(
        ITextDocumentIdentifierParams @params,
        CancellationToken cancellationToken) =>
        Task.FromResult(_documents.GetOrAdd(@params.TextDocument.Uri, static _ => new SemanticTokensDocument(Legend)));

    internal static ImmutableArray<LexicalSemanticToken> ProjectLexicalTokens(Compilation compilation)
    {
        var projected = ImmutableArray.CreateBuilder<LexicalSemanticToken>();

        foreach (var token in compilation.Tokens.Tokens)
        {
            var meta = TokensCatalog.GetMeta(token.Kind);
            string? effectiveTokenType = null;

            // Contextual reclassification: 'set' emits the type custom token in type-expression position.
            // SetType.VisualCategory is intentionally null (parser-synthesized; never in the lexer stream),
            // so we derive the type token from the catalog rather than the synthetic token kind.
            if (token.Kind == TokenKind.Set && SlotContextResolver.IsSetInTypePosition(compilation, token))
            {
                effectiveTokenType = SemanticTokenTypesCatalog.GetMeta(SemanticTokenTypeKind.Type).CustomType;
            }
            else if (meta.VisualCategory.HasValue)
            {
                effectiveTokenType = SemanticTokenTypesCatalog.GetMeta(meta.VisualCategory.Value).CustomType;
            }
            else if (IsTypedConstantToken(token.Kind))
            {
                effectiveTokenType = BuiltInStringTokenType;
            }

            if (effectiveTokenType is null)
            {
                continue;
            }

            projected.Add(new LexicalSemanticToken(
                token.Kind,
                token.Span.StartLine - 1,
                token.Span.StartColumn - 1,
                token.Span.Length,
                effectiveTokenType));
        }

        return projected.ToImmutable();
    }

    internal static ImmutableArray<LexicalSemanticToken> ProjectMergedTokens(Compilation compilation)
    {
        return ProjectLexicalTokens(compilation)
            .Select(static token => (Token: token, OverlayOrder: 0))
            .Concat(ProjectIdentifierTokens(compilation.Semantics).Select(static token => (Token: token, OverlayOrder: 1)))
            .Where(static entry => entry.Token.Line >= 0 && entry.Token.Character >= 0 && entry.Token.Length > 0)
            .OrderBy(static entry => entry.Token.Line)
            .ThenBy(static entry => entry.Token.Character)
            .ThenBy(static entry => entry.Token.Length)
            .ThenBy(static entry => entry.OverlayOrder)
            .GroupBy(static entry => (entry.Token.Line, entry.Token.Character))
            .Select(static group => group.Last().Token)
            .ToImmutableArray();
    }

    internal static ImmutableArray<LexicalSemanticToken> ProjectIdentifierTokens(SemanticIndex index)
    {
        var projected = ImmutableArray.CreateBuilder<LexicalSemanticToken>();

        foreach (var field in index.Fields)
        {
            var ns = field.NameSpan;
            projected.Add(new LexicalSemanticToken(
                TokenKind.Identifier,
                ns.StartLine - 1,
                ns.StartColumn - 1,
                field.Name.Length,
                SemanticTokenTypesCatalog.GetMeta(SemanticTokenTypeKind.FieldName).CustomType));
        }

        foreach (var state in index.States)
        {
            var ns = state.NameSpan;
            projected.Add(new LexicalSemanticToken(
                TokenKind.Identifier,
                ns.StartLine - 1,
                ns.StartColumn - 1,
                state.Name.Length,
                SemanticTokenTypesCatalog.GetMeta(SemanticTokenTypeKind.State).CustomType));
        }

        foreach (var evt in index.Events)
        {
            var ns = evt.NameSpan;
            projected.Add(new LexicalSemanticToken(
                TokenKind.Identifier,
                ns.StartLine - 1,
                ns.StartColumn - 1,
                evt.Name.Length,
                SemanticTokenTypesCatalog.GetMeta(SemanticTokenTypeKind.Event).CustomType));

            foreach (var arg in evt.Args)
            {
                projected.Add(new LexicalSemanticToken(
                    TokenKind.Identifier,
                    arg.Span.StartLine - 1,
                    arg.Span.StartColumn - 1,
                    arg.Name.Length,
                    SemanticTokenTypesCatalog.GetMeta(SemanticTokenTypeKind.ArgName).CustomType));
            }
        }

        foreach (var fr in index.FieldReferences)
        {
            projected.Add(new LexicalSemanticToken(
                TokenKind.Identifier,
                fr.Site.StartLine - 1,
                fr.Site.StartColumn - 1,
                fr.Site.Length,
                SemanticTokenTypesCatalog.GetMeta(SemanticTokenTypeKind.FieldName).CustomType));
        }

        foreach (var sr in index.StateReferences)
        {
            projected.Add(new LexicalSemanticToken(
                TokenKind.Identifier,
                sr.Site.StartLine - 1,
                sr.Site.StartColumn - 1,
                sr.Site.Length,
                SemanticTokenTypesCatalog.GetMeta(SemanticTokenTypeKind.State).CustomType));
        }

        foreach (var er in index.EventReferences)
        {
            projected.Add(new LexicalSemanticToken(
                TokenKind.Identifier,
                er.Site.StartLine - 1,
                er.Site.StartColumn - 1,
                er.Site.Length,
                SemanticTokenTypesCatalog.GetMeta(SemanticTokenTypeKind.Event).CustomType));
        }

        foreach (var ar in index.ArgReferences)
        {
            projected.Add(new LexicalSemanticToken(
                TokenKind.Identifier,
                ar.Site.StartLine - 1,
                ar.Site.StartColumn - 1,
                ar.Site.Length,
                SemanticTokenTypesCatalog.GetMeta(SemanticTokenTypeKind.ArgName).CustomType));
        }

        foreach (var expression in EnumerateTypedExpressions(index).OfType<TypedFunctionCall>())
        {
            var functionName = Functions.GetMeta(expression.ResolvedFunction).Name;
            projected.Add(new LexicalSemanticToken(
                TokenKind.Identifier,
                expression.Span.StartLine - 1,
                expression.Span.StartColumn - 1,
                functionName.Length,
                BuiltInFunctionTokenType));
        }

        return projected.ToImmutable();
    }

    internal static void AddIdentifierTokens(SemanticTokensBuilder builder, SemanticIndex index)
    {
        foreach (var token in ProjectIdentifierTokens(index))
        {
            builder.Push(token.Line, token.Character, token.Length, token.TokenType);
        }
    }

    internal static SemanticTokensLegend BuildLegend()
    {
        return new SemanticTokensLegend
        {
            TokenTypes = new Container<SemanticTokenType>(
                SemanticTokenTypesCatalog.All
                    .Select(static m => m.CustomType)
                    .Concat([BuiltInFunctionTokenType, BuiltInStringTokenType])
                    .Distinct()
                    .Select(static tokenType => new SemanticTokenType(tokenType))
                    .ToArray()),
            TokenModifiers = new Container<SemanticTokenModifier>(
                new SemanticTokenModifier("preceptConstrained")),
        };
    }

    internal static ImmutableArray<SemanticTokenColorRule> BuildColorNotificationPayload() =>
        SemanticTokenTypesCatalog.All
            .Select(static meta => new SemanticTokenColorRule(
                meta.CustomType,
                meta.ForegroundHex,
                meta.Bold,
                meta.Italic))
            .ToImmutableArray();

    internal static void SendColorNotification(OmniSharp.Extensions.LanguageServer.Server.LanguageServer server) =>
        SendColorNotification((method, payload) => server.SendNotification(method, payload));

    internal static void SendColorNotification(Action<string, SemanticTokenColorNotificationPayload> sendNotification) =>
        sendNotification(
            SemanticTokenColorsNotificationName,
            new SemanticTokenColorNotificationPayload(BuildColorNotificationPayload().ToArray()));

    private static bool IsTypedConstantToken(TokenKind kind) =>
        kind is TokenKind.TypedConstant
            or TokenKind.TypedConstantStart
            or TokenKind.TypedConstantMiddle
            or TokenKind.TypedConstantEnd;

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

    internal readonly record struct LexicalSemanticToken(
        TokenKind Kind,
        int Line,
        int Character,
        int Length,
        string TokenType);

    internal readonly record struct SemanticTokenColorNotificationPayload(
        [property: JsonPropertyName("rules")] SemanticTokenColorRule[] Rules);

    internal readonly record struct SemanticTokenColorRule(
        [property: JsonPropertyName("tokenType")] string TokenType,
        [property: JsonPropertyName("hexColor")] string HexColor,
        [property: JsonPropertyName("bold")] bool Bold,
        [property: JsonPropertyName("italic")] bool Italic);
}
