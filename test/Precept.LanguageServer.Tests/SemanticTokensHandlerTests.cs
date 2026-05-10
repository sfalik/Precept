global using OmniSharp.Extensions.LanguageServer.Protocol;
global using OmniSharp.Extensions.LanguageServer.Protocol.Document;

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
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

    [Fact]
    public void Legend_IncludesIdentifierOverlayTypes()
    {
        var legend = SemanticTokensHandler.BuildLegend();

        legend.TokenTypes.Select(static type => type.ToString()).Should().Contain([
            "property",
            "enum",
            "function",
            "parameter",
        ]);
    }

    [Fact]
    public void IdentifierTokens_FieldDeclaration_EmitsPropertyToken()
    {
        var compilation = Compiler.Compile("""
            precept Sample
            field Name as string
            state Draft initial
            """);
        var field = compilation.Semantics.Fields.Single();

        compilation.HasErrors.Should().BeFalse();
        SemanticTokensHandler.ProjectIdentifierTokens(compilation.Semantics)
            .Should()
            .Contain(token =>
                token.Line == field.NameSpan.StartLine - 1 &&
                token.Character == field.NameSpan.StartColumn - 1 &&
                token.Length == field.Name.Length &&
                token.TokenType == "property");
    }

    [Fact]
    public void IdentifierTokens_EventArgDeclaration_EmitsParameterToken()
    {
        var compilation = Compiler.Compile("""
            precept Sample
            field Stored as string optional
            state Draft initial
            event Submit(Amount as decimal)
            """);
        var arg = compilation.Semantics.Events.Single().Args.Single();

        compilation.HasErrors.Should().BeFalse();
        SemanticTokensHandler.ProjectIdentifierTokens(compilation.Semantics)
            .Should()
            .Contain(token =>
                token.Line == arg.Span.StartLine - 1 &&
                token.Character == arg.Span.StartColumn - 1 &&
                token.Length == arg.Name.Length &&
                token.TokenType == "parameter");
    }

    [Fact]
    public void IdentifierTokens_ArgReference_EmitsParameterToken()
    {
        var compilation = Compiler.Compile("""
            precept LoanWorkflow
            field StoredAmount as decimal default 0
            state Draft initial
            state Approved
            event Submit(Amount as decimal)
            from Draft on Submit when Submit.Amount > 0 -> transition Approved
            """);
        var argReference = compilation.Semantics.ArgReferences.Single(r => r.Arg.Name == "Amount");

        compilation.HasErrors.Should().BeFalse();
        SemanticTokensHandler.ProjectIdentifierTokens(compilation.Semantics)
            .Should()
            .Contain(token =>
                token.Line == argReference.Site.StartLine - 1 &&
                token.Character == argReference.Site.StartColumn - 1 &&
                token.Length == argReference.Site.Length &&
                token.TokenType == "parameter");
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
