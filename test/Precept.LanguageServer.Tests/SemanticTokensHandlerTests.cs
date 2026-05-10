global using OmniSharp.Extensions.LanguageServer.Protocol;
global using OmniSharp.Extensions.LanguageServer.Protocol.Document;

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.Language;
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
        var keywordSemantic = SemanticTokenTypes.GetMeta(SemanticTokenTypeKind.KeywordSemantic).CustomType;
        var name = SemanticTokenTypes.GetMeta(SemanticTokenTypeKind.Name).CustomType;

        var tokens = SemanticTokensHandler.ProjectLexicalTokens(compilation);

        tokens.Should().Contain(token => token.Kind == TokenKind.Field && token.TokenType == keywordSemantic);
        tokens.Should().Contain(token => token.Kind == TokenKind.State && token.TokenType == keywordSemantic);
        tokens.Should().Contain(token => token.Kind == TokenKind.Identifier && token.TokenType == name);
    }

    [Fact]
    public void LexicalTokens_SkipsTokensWithNoSemanticType()
    {
        var compilation = Compiler.Compile("precept Sample\n");

        var expectedTokenCount = compilation.Tokens.Tokens.Count(token =>
            TokensCatalog.GetMeta(token.Kind).VisualCategory.HasValue);

        var tokens = SemanticTokensHandler.ProjectLexicalTokens(compilation);

        compilation.Tokens.Tokens
            .Where(token => !TokensCatalog.GetMeta(token.Kind).VisualCategory.HasValue)
            .Select(token => token.Kind)
            .Should()
            .Contain([TokenKind.NewLine, TokenKind.EndOfSource]);

        tokens.Should().HaveCount(expectedTokenCount);
        tokens.Should().OnlyContain(token => TokensCatalog.GetMeta(token.Kind).VisualCategory.HasValue);
    }

    [Fact]
    public void LexicalTokens_SpanConvertedToZeroBasedLines()
    {
        var compilation = Compiler.Compile("precept Sample\n  field Name as string");
        var keywordSemantic = SemanticTokenTypes.GetMeta(SemanticTokenTypeKind.KeywordSemantic).CustomType;

        var fieldToken = SemanticTokensHandler.ProjectLexicalTokens(compilation)
            .Single(token => token.Kind == TokenKind.Field);

        fieldToken.Line.Should().Be(1);
        fieldToken.Character.Should().Be(2);
        fieldToken.Length.Should().Be(5);
        fieldToken.TokenType.Should().Be(keywordSemantic);
    }

    [Fact]
    public void BuildLegend_TokenTypes_MatchCatalogCustomTypes()
    {
        var expected = SemanticTokenTypes.All.Select(m => m.CustomType).ToArray();
        var legend = SemanticTokensHandler.BuildLegend();

        legend.TokenTypes.Select(t => t.ToString()).Should().Equal(expected);
    }

    [Fact]
    public void BuildLegend_IncludesPreceptConstrainedModifier()
    {
        var legend = SemanticTokensHandler.BuildLegend();

        legend.TokenModifiers.Select(m => m.ToString()).Should().Contain("preceptConstrained");
    }

    [Fact]
    public void LexicalTokens_MultipleKeywordsOnSameLine_EmitDistinctZeroBasedCharacters()
    {
        var compilation = Compiler.Compile("precept Sample\nstate Draft initial terminal");

        var tokens = SemanticTokensHandler.ProjectLexicalTokens(compilation)
            .Where(token => token.Kind is TokenKind.State or TokenKind.Initial or TokenKind.Terminal)
            .ToArray();

        tokens.Should().HaveCount(3);
        tokens.Select(token => token.Line).Should().Equal(1, 1, 1);
        tokens.Select(token => token.Character).Should().Equal(0, 12, 20);
    }

    [Fact]
    public void Legend_IncludesIdentifierOverlayTypes()
    {
        var legend = SemanticTokensHandler.BuildLegend();

        legend.TokenTypes.Select(static type => type.ToString()).Should().Contain([
            SemanticTokenTypes.GetMeta(SemanticTokenTypeKind.State).CustomType,
            SemanticTokenTypes.GetMeta(SemanticTokenTypeKind.Event).CustomType,
            SemanticTokenTypes.GetMeta(SemanticTokenTypeKind.FieldName).CustomType,
            SemanticTokenTypes.GetMeta(SemanticTokenTypeKind.ArgName).CustomType,
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
        var expected = SemanticTokenTypes.GetMeta(SemanticTokenTypeKind.FieldName).CustomType;

        compilation.HasErrors.Should().BeFalse();
        SemanticTokensHandler.ProjectIdentifierTokens(compilation.Semantics)
            .Should()
            .Contain(token =>
                token.Line == field.NameSpan.StartLine - 1 &&
                token.Character == field.NameSpan.StartColumn - 1 &&
                token.Length == field.Name.Length &&
                token.TokenType == expected);
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
        var expected = SemanticTokenTypes.GetMeta(SemanticTokenTypeKind.ArgName).CustomType;

        compilation.HasErrors.Should().BeFalse();
        SemanticTokensHandler.ProjectIdentifierTokens(compilation.Semantics)
            .Should()
            .Contain(token =>
                token.Line == arg.Span.StartLine - 1 &&
                token.Character == arg.Span.StartColumn - 1 &&
                token.Length == arg.Name.Length &&
                token.TokenType == expected);
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
        var expected = SemanticTokenTypes.GetMeta(SemanticTokenTypeKind.ArgName).CustomType;

        compilation.HasErrors.Should().BeFalse();
        SemanticTokensHandler.ProjectIdentifierTokens(compilation.Semantics)
            .Should()
            .Contain(token =>
                token.Line == argReference.Site.StartLine - 1 &&
                token.Character == argReference.Site.StartColumn - 1 &&
                token.Length == argReference.Site.Length &&
                token.TokenType == expected);
    }

    [Fact]
    public void Pass1_TokenWithVisualCategory_EmitsCorrectCustomType()
    {
        var compilation = Compiler.Compile("precept Sample\nfield Name as string\nstate Draft initial");
        var expected = SemanticTokenTypes.GetMeta(SemanticTokenTypeKind.KeywordSemantic).CustomType;

        var tokens = SemanticTokensHandler.ProjectLexicalTokens(compilation);

        tokens.Should().Contain(token => token.Kind == TokenKind.Field && token.TokenType == expected);
    }

    [Fact]
    public void Pass2_StateName_EmitsPreceptState()
    {
        var compilation = Compiler.Compile("precept Sample\nfield Name as string\nstate Draft initial");
        var expected = SemanticTokenTypes.GetMeta(SemanticTokenTypeKind.State).CustomType;

        compilation.HasErrors.Should().BeFalse();
        var tokens = SemanticTokensHandler.ProjectIdentifierTokens(compilation.Semantics);

        tokens.Should().Contain(t => t.Kind == TokenKind.Identifier && t.TokenType == expected);
    }

    [Fact]
    public void Pass2_EventName_EmitsPreceptEvent()
    {
        var compilation = Compiler.Compile("""
            precept Sample
            field Name as string
            state Draft initial
            event Submit
            """);
        var expected = SemanticTokenTypes.GetMeta(SemanticTokenTypeKind.Event).CustomType;

        compilation.HasErrors.Should().BeFalse();
        var tokens = SemanticTokensHandler.ProjectIdentifierTokens(compilation.Semantics);

        tokens.Should().Contain(t => t.TokenType == expected);
    }

    [Fact]
    public void Pass2_FieldName_EmitsPreceptFieldName()
    {
        var compilation = Compiler.Compile("precept Sample\nfield Name as string\nstate Draft initial");
        var expected = SemanticTokenTypes.GetMeta(SemanticTokenTypeKind.FieldName).CustomType;

        compilation.HasErrors.Should().BeFalse();
        var tokens = SemanticTokensHandler.ProjectIdentifierTokens(compilation.Semantics);

        tokens.Should().Contain(t => t.TokenType == expected);
    }

    [Fact]
    public void Pass2_ArgName_EmitsPreceptArgName()
    {
        var compilation = Compiler.Compile("""
            precept Sample
            field Stored as string optional
            state Draft initial
            event Submit(Amount as decimal)
            """);
        var expected = SemanticTokenTypes.GetMeta(SemanticTokenTypeKind.ArgName).CustomType;

        compilation.HasErrors.Should().BeFalse();
        var tokens = SemanticTokensHandler.ProjectIdentifierTokens(compilation.Semantics);

        tokens.Should().Contain(t => t.TokenType == expected);
    }

    [Fact]
    public void LexicalTokens_SetInTypePosition_EmitsTypeToken()
    {
        var compilation = Compiler.Compile("""
            precept Sample
            field Tags as set of string
            state Draft initial
            """);
        var type = SemanticTokenTypes.GetMeta(SemanticTokenTypeKind.Type).CustomType;
        var keywordSemantic = SemanticTokenTypes.GetMeta(SemanticTokenTypeKind.KeywordSemantic).CustomType;

        compilation.HasErrors.Should().BeFalse();

        var tokens = SemanticTokensHandler.ProjectLexicalTokens(compilation);

        tokens.Should().Contain(token => token.Kind == TokenKind.Set && token.TokenType == type,
            because: "set in a type-expression slot must emit the type semantic token");
        tokens.Should().NotContain(token => token.Kind == TokenKind.Set && token.TokenType == keywordSemantic,
            because: "set in type position must not be classified as an action keyword");
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
