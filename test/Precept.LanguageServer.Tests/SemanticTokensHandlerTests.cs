global using OmniSharp.Extensions.LanguageServer.Protocol;
global using OmniSharp.Extensions.LanguageServer.Protocol.Document;

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.LanguageServer.Handlers;
using Xunit;
using TokenKind = Precept.Language.TokenKind;
using TokensCatalog = Precept.Language.Tokens;

namespace Precept.LanguageServer.Tests;

public sealed class SemanticTokensHandlerTests
{
    [Fact]
    public void LexicalTokens_Keyword_EmitsSemanticToken()
    {
        var compilation = Compiler.Compile("precept Sample\nfield Name as string\nstate Draft initial");

        var tokens = SemanticTokensHandler.ProjectLexicalTokens(compilation);

        tokens.Should().Contain(token => token.Kind == TokenKind.Field && token.TokenType == "keyword");
        tokens.Should().Contain(token => token.Kind == TokenKind.State && token.TokenType == "keyword");
        tokens.Should().Contain(token => token.Kind == TokenKind.Identifier && token.TokenType == "variable");
    }

    [Fact]
    public void LexicalTokens_SkipsTokensWithNoSemanticType()
    {
        var compilation = Compiler.Compile("precept Sample\n");

        var expectedTokenCount = compilation.Tokens.Tokens.Count(token =>
            TokensCatalog.GetMeta(token.Kind).SemanticTokenType is not null);

        var tokens = SemanticTokensHandler.ProjectLexicalTokens(compilation);

        compilation.Tokens.Tokens
            .Where(token => TokensCatalog.GetMeta(token.Kind).SemanticTokenType is null)
            .Select(token => token.Kind)
            .Should()
            .Contain([TokenKind.NewLine, TokenKind.EndOfSource]);

        tokens.Should().HaveCount(expectedTokenCount);
        tokens.Should().OnlyContain(token => TokensCatalog.GetMeta(token.Kind).SemanticTokenType != null);
    }

    [Fact]
    public void LexicalTokens_SpanConvertedToZeroBasedLines()
    {
        var compilation = Compiler.Compile("precept Sample\n  field Name as string");

        var fieldToken = SemanticTokensHandler.ProjectLexicalTokens(compilation)
            .Single(token => token.Kind == TokenKind.Field);

        fieldToken.Line.Should().Be(1);
        fieldToken.Character.Should().Be(2);
        fieldToken.Length.Should().Be(5);
        fieldToken.TokenType.Should().Be("keyword");
    }
}

internal static class OmniSharpCompatibilityExtensions
{
    public static Task DidOpen(this ITextDocumentLanguageClient client, DidOpenTextDocumentParams @params)
    {
        client.DidOpenTextDocument(@params);
        return Task.CompletedTask;
    }

    public static ILanguageClientRegistry OnPublishDiagnostics(
        this ITextDocumentLanguageClient client,
        Action<PublishDiagnosticsParams> handler)
    {
        var registry = client as ILanguageClientRegistry
            ?? client.GetService(typeof(ILanguageClientRegistry)) as ILanguageClientRegistry
            ?? throw new InvalidOperationException("Unable to resolve ILanguageClientRegistry from text document client.");

        return registry.OnPublishDiagnostics(handler);
    }
}
