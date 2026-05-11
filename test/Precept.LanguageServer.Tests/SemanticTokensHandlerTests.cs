global using OmniSharp.Extensions.LanguageServer.Protocol;
global using OmniSharp.Extensions.LanguageServer.Protocol.Document;

using System;
using System.Collections;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.Language;
using Precept.LanguageServer.Handlers;
using Precept.Pipeline;
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
        var expected = SemanticTokenTypes.All
            .Select(m => m.CustomType)
            .Concat([SemanticTokensHandler.BuiltInFunctionTokenType, SemanticTokensHandler.BuiltInStringTokenType])
            .Distinct()
            .ToArray();
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
        argReference.Site.Length.Should().Be(argReference.Arg.Name.Length);
        SemanticTokensHandler.ProjectIdentifierTokens(compilation.Semantics)
            .Should()
            .Contain(token =>
                token.Line == argReference.Site.StartLine - 1 &&
                token.Character == argReference.Site.StartColumn - 1 &&
                token.Length == argReference.Site.Length &&
                token.TokenType == expected);
    }

    [Fact]
    public void IdentifierTokens_TransitionOutcomeStateReference_UseStateIdentifierSpan()
    {
        var compilation = Compiler.Compile("""
            precept LoanWorkflow
            state Draft initial
            state Approved
            event Submit
            from Draft on Submit
                -> transition Approved
            """);
        var expected = SemanticTokenTypes.GetMeta(SemanticTokenTypeKind.State).CustomType;

        compilation.HasErrors.Should().BeFalse();

        var stateReference = compilation.Semantics.StateReferences.Single(reference =>
            reference.State.Name == "Approved"
            && reference.Site.StartLine == 6);

        stateReference.Site.StartColumn.Should().Be(19);
        stateReference.Site.Length.Should().Be("Approved".Length);
        SemanticTokensHandler.ProjectIdentifierTokens(compilation.Semantics)
            .Should()
            .Contain(token =>
                token.Line == stateReference.Site.StartLine - 1
                && token.Character == stateReference.Site.StartColumn - 1
                && token.Length == stateReference.Site.Length
                && token.TokenType == expected);
    }

    [Fact]
    public void IdentifierTokens_FieldTargetList_UseFirstFieldIdentifierSpan()
    {
        var compilation = Compiler.Compile("""
            precept BadgeRequest
            field EmployeeName as string optional
            field Department as string optional
            state Draft initial
            in Draft modify EmployeeName, Department editable
            """);
        var expected = SemanticTokenTypes.GetMeta(SemanticTokenTypeKind.FieldName).CustomType;

        compilation.HasErrors.Should().BeFalse();

        var fieldReference = compilation.Semantics.FieldReferences.Single(reference =>
            reference.Field.Name == "EmployeeName"
            && reference.Site.StartLine == 5);

        fieldReference.Site.StartColumn.Should().Be(17);
        fieldReference.Site.Length.Should().Be("EmployeeName".Length);
        SemanticTokensHandler.ProjectIdentifierTokens(compilation.Semantics)
            .Should()
            .Contain(token =>
                token.Line == fieldReference.Site.StartLine - 1
                && token.Character == fieldReference.Site.StartColumn - 1
                && token.Length == fieldReference.Site.Length
                && token.TokenType == expected);
    }

    [Fact]
    public void MergedTokens_LoanApplicationSample_AreStrictlyOrdered()
    {
        var compilation = Compiler.Compile(ReadSample("loan-application.precept"));

        compilation.HasErrors.Should().BeFalse();

        AssertMergedTokensAreStrictlyOrdered(compilation);
    }

    [Fact]
    public void MergedTokens_BuildingAccessBadgeRequestSample_AreStrictlyOrdered()
    {
        var compilation = Compiler.Compile(ReadSample("building-access-badge-request.precept"));

        compilation.HasErrors.Should().BeFalse();

        AssertMergedTokensAreStrictlyOrdered(compilation);
    }

    [Fact]
    public void SemanticTokensDelta_LoanApplicationSample_DoesNotThrow()
    {
        var before = Compiler.Compile(ReadSample("loan-application.precept"));
        var after = Compiler.Compile(ReadSample("loan-application.precept").Replace("UnderReview", "ManualReview"));

        before.HasErrors.Should().BeFalse();
        after.HasErrors.Should().BeFalse();

        AssertSemanticTokensDeltaDoesNotThrow(before, after, @"C:\\loan-application.precept");
    }

    [Fact]
    public void MergedTokens_QualifiedEventArgReference_AreStrictlyOrdered()
    {
        var compilation = Compiler.Compile("""
            precept RestaurantWaitlist
            field CurrentParty as string optional
            state Accepting initial
            state Joined terminal
            event JoinWaitlist(PartyName as string notempty, PartySize as number)
            on JoinWaitlist ensure JoinWaitlist.PartyName != "" because "Party name is required"
            from Accepting on JoinWaitlist when JoinWaitlist.PartyName != ""
                -> set CurrentParty = PartyName
                -> transition Joined
            """);

        compilation.HasErrors.Should().BeFalse();

        AssertMergedTokensAreStrictlyOrdered(compilation);
    }

    [Fact]
    public void SemanticTokensDelta_QualifiedEventArgReference_DoesNotThrow()
    {
        var before = Compiler.Compile("""
            precept RestaurantWaitlist
            field CurrentParty as string optional
            state Accepting initial
            state Joined terminal
            event JoinWaitlist(PartyName as string notempty, PartySize as number)
            on JoinWaitlist ensure JoinWaitlist.PartyName != "" because "Party name is required"
            from Accepting on JoinWaitlist when JoinWaitlist.PartyName != ""
                -> set CurrentParty = PartyName
                -> transition Joined
            """);
        var after = Compiler.Compile("""
            precept RestaurantWaitlist
            field CurrentParty as string optional
            state Accepting initial
            state Joined terminal
            event JoinWaitlist(GuestName as string notempty, PartySize as number)
            on JoinWaitlist ensure JoinWaitlist.GuestName != "" because "Party name is required"
            from Accepting on JoinWaitlist when JoinWaitlist.GuestName != ""
                -> set CurrentParty = GuestName
                -> transition Joined
            """);
        before.HasErrors.Should().BeFalse();
        after.HasErrors.Should().BeFalse();

        AssertSemanticTokensDeltaDoesNotThrow(before, after, @"C:\\semantic-tokens-test.precept");
    }

    [Fact]
    public void NormalizeMergedTokens_SameStartPrefersShortestOverlayRange()
    {
        var normalized = SemanticTokensHandler.NormalizeMergedTokens([
            (new SemanticTokensHandler.LexicalSemanticToken(TokenKind.Identifier, 0, 5, 13, "name"), 0),
            (new SemanticTokensHandler.LexicalSemanticToken(TokenKind.Identifier, 0, 5, 6, "event"), 1),
        ]);

        normalized.Should().ContainSingle();
        normalized.Single().Should().Be(new SemanticTokensHandler.LexicalSemanticToken(TokenKind.Identifier, 0, 5, 6, "event"));
    }

    [Fact]
    public void NormalizeMergedTokens_TruncatesEarlierSameLineOverlap()
    {
        var normalized = SemanticTokensHandler.NormalizeMergedTokens([
            (new SemanticTokensHandler.LexicalSemanticToken(TokenKind.Identifier, 0, 3, 10, "event"), 0),
            (new SemanticTokensHandler.LexicalSemanticToken(TokenKind.Identifier, 0, 8, 6, "arg"), 1),
            (new SemanticTokensHandler.LexicalSemanticToken(TokenKind.Identifier, 0, 16, 4, "state"), 1),
        ]);

        normalized.Should().Equal([
            new SemanticTokensHandler.LexicalSemanticToken(TokenKind.Identifier, 0, 3, 5, "event"),
            new SemanticTokensHandler.LexicalSemanticToken(TokenKind.Identifier, 0, 8, 6, "arg"),
            new SemanticTokensHandler.LexicalSemanticToken(TokenKind.Identifier, 0, 16, 4, "state"),
        ]);

        AssertMergedTokensAreStrictlyOrdered(normalized);
    }

    [Fact]
    public void SemanticTokensDelta_OverlappingSyntheticTokens_DoesNotThrow()
    {
        var before = SemanticTokensHandler.NormalizeMergedTokens([
            (new SemanticTokensHandler.LexicalSemanticToken(TokenKind.Identifier, 0, 3, 10, "event"), 0),
            (new SemanticTokensHandler.LexicalSemanticToken(TokenKind.Identifier, 0, 8, 6, "arg"), 1),
        ]);
        var after = SemanticTokensHandler.NormalizeMergedTokens([
            (new SemanticTokensHandler.LexicalSemanticToken(TokenKind.Identifier, 0, 3, 11, "event"), 0),
            (new SemanticTokensHandler.LexicalSemanticToken(TokenKind.Identifier, 0, 9, 6, "arg"), 1),
        ]);

        AssertSemanticTokensDeltaDoesNotThrow(before, after, @"C:\\overlap.precept");
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

    [Fact]
    public void ExpressionTokens_BuiltInFunctionCall_EmitFunctionToken()
    {
        var compilation = Compiler.Compile("""
            precept Sample
            field Input as decimal default 1
            field Rounded as integer <- round(Input)
            """);
        var call = (TypedFunctionCall)compilation.Semantics.Fields.Single(field => field.Name == "Rounded").ComputedExpression!;
        var expectedLength = Functions.GetMeta(call.ResolvedFunction).Name.Length;

        compilation.HasErrors.Should().BeFalse();

        var tokens = SemanticTokensHandler.ProjectIdentifierTokens(compilation.Semantics);

        tokens.Should().Contain(token =>
            token.Line == call.Span.StartLine - 1 &&
            token.Character == call.Span.StartColumn - 1 &&
            token.Length == expectedLength &&
            token.TokenType == SemanticTokensHandler.BuiltInFunctionTokenType);
    }

    [Fact]
    public void LexicalTokens_TypedConstant_EmitStringToken()
    {
        var compilation = Compiler.Compile("""
            precept Sample
            field Due as date <- '2026-01-01'
            """);

        compilation.HasErrors.Should().BeFalse();

        var tokens = SemanticTokensHandler.ProjectLexicalTokens(compilation);

        tokens.Should().Contain(token =>
            token.Kind == TokenKind.TypedConstant &&
            token.TokenType == SemanticTokensHandler.BuiltInStringTokenType);
    }

    [Fact]
    public void LexicalTokens_Operator_EmitOperatorToken()
    {
        var compilation = Compiler.Compile("""
            precept Sample
            field Total as number <- 1 + 2
            """);
        var expected = SemanticTokenTypes.GetMeta(SemanticTokenTypeKind.Operator).CustomType;

        compilation.HasErrors.Should().BeFalse();

        var tokens = SemanticTokensHandler.ProjectLexicalTokens(compilation);

        tokens.Should().Contain(token => token.Kind == TokenKind.Plus && token.TokenType == expected);
    }

    [Fact]
    public void LexicalTokens_ActionVerb_EmitKeywordToken()
    {
        var compilation = Compiler.Compile("""
            precept Sample
            field Value as number default 0
            state Draft initial
            event Submit
            from Draft on Submit -> set Value = 1 -> no transition
            """);
        var expected = SemanticTokenTypes.GetMeta(SemanticTokenTypeKind.KeywordSemantic).CustomType;

        compilation.HasErrors.Should().BeFalse();

        var tokens = SemanticTokensHandler.ProjectLexicalTokens(compilation);

        tokens.Should().Contain(token => token.Kind == TokenKind.Set && token.TokenType == expected);
    }

    [Fact]
    public void LexicalTokens_AsAndDefault_StayInGrammarKeywordLane()
    {
        var compilation = Compiler.Compile("""
            precept Sample
            field Name as string default "Kramer"
            """);
        var grammarKeyword = SemanticTokenTypes.GetMeta(SemanticTokenTypeKind.KeywordGrammar).CustomType;
        var message = SemanticTokenTypes.GetMeta(SemanticTokenTypeKind.Message).CustomType;

        compilation.HasErrors.Should().BeFalse();

        var tokens = SemanticTokensHandler.ProjectLexicalTokens(compilation);

        tokens.Should().Contain(token => token.Kind == TokenKind.As && token.TokenType == grammarKeyword);
        tokens.Should().Contain(token => token.Kind == TokenKind.Default && token.TokenType == grammarKeyword);
        tokens.Should().NotContain(token =>
            (token.Kind == TokenKind.As || token.Kind == TokenKind.Default) &&
            token.TokenType == message);
    }

    [Fact]
    public void LexicalTokens_BooleanLiteral_KeepKeywordTokenType()
    {
        var compilation = Compiler.Compile("""
            precept Sample
            field Enabled as boolean default true
            """);
        var expected = SemanticTokenTypes.GetMeta(SemanticTokenTypeKind.Value).CustomType;

        compilation.HasErrors.Should().BeFalse();

        var tokens = SemanticTokensHandler.ProjectLexicalTokens(compilation);

        tokens.Should().Contain(token => token.Kind == TokenKind.True && token.TokenType == expected);
    }

    [Fact]
    public void SendColorNotification_EmitsOneEntryPerCatalogMember()
    {
        string? method = null;
        SemanticTokensHandler.SemanticTokenColorNotificationPayload? payload = null;

        SemanticTokensHandler.SendColorNotification((notificationMethod, rules) =>
        {
            method = notificationMethod;
            payload = rules;
        });

        method.Should().Be(SemanticTokensHandler.SemanticTokenColorsNotificationName);
        payload.Should().NotBeNull();
        payload!.Value.Rules.Should().HaveCount(SemanticTokenTypes.All.Count);
        payload.Value.Rules.Select(entry => entry.TokenType)
            .Should()
            .Equal(SemanticTokenTypes.All.Select(meta => meta.CustomType));
    }

    [Fact]
    public void SendColorNotification_EntryHexColorsMatchCatalog()
    {
        SemanticTokensHandler.SemanticTokenColorNotificationPayload? payload = null;

        SemanticTokensHandler.SendColorNotification((_, rules) => payload = rules);

        payload.Should().NotBeNull();
        payload!.Value.Rules
            .Select(entry => (entry.TokenType, entry.HexColor, entry.Bold, entry.Italic))
            .Should()
            .Equal(SemanticTokenTypes.All.Select(meta => (meta.CustomType, meta.ForegroundHex, meta.Bold, meta.Italic)));
    }

    [Fact]
    public void SemanticTokensDelta_TypedConstantSpanGrows_DoesNotThrow()
    {
        // Simulates user typing inside a typed literal, growing the span by one character.
        var before = Compiler.Compile("""
            precept Sample
            field Due as date <- '2026-01-01'
            """);
        var after = Compiler.Compile("""
            precept Sample
            field Due as date <- '2026-01-012'
            """);

        before.HasErrors.Should().BeFalse();

        AssertSemanticTokensDeltaDoesNotThrow(before, after, @"C:\\typed-constant-span.precept");
    }

    [Fact]
    public void SemanticTokensDelta_TypedConstantTokenCountChanges_DoesNotThrow()
    {
        // Simulates a typed literal being removed entirely between versions.
        var before = Compiler.Compile("""
            precept Sample
            field Due as date <- '2026-01-01'
            """);
        var after = Compiler.Compile("""
            precept Sample
            field Due as date optional
            """);

        AssertSemanticTokensDeltaDoesNotThrow(before, after, @"C:\\typed-constant-removed.precept");
    }

    [Fact]
    public void SemanticTokensDelta_NonTypedConstantEdit_DoesNotThrow()
    {
        // Normal non-typed-constant edits must continue to work correctly.
        var before = Compiler.Compile("""
            precept Sample
            field Name as string optional
            state Draft initial
            state Active
            """);
        var after = Compiler.Compile("""
            precept Sample
            field Name as string optional
            state Pending initial
            state Active
            """);

        before.HasErrors.Should().BeFalse();
        after.HasErrors.Should().BeFalse();

        AssertSemanticTokensDeltaDoesNotThrow(before, after, @"C:\\non-typed-constant.precept");
    }

    [Fact]
    public async Task HandleDelta_WhenPreviousResultIdIsStale_ReturnsFullResponse()
    {
        var store = new DocumentStore();
        var handler = new SemanticTokensHandler(store);
        var uri = DocumentUri.FromFileSystemPath(@"C:\test.precept");
        var state = store.GetOrAdd(uri);

        var before = Compiler.Compile("precept Sample\nfield Name as string optional\nstate Draft initial\n");
        var after = Compiler.Compile("precept Sample\nfield Title as string optional\nstate Draft initial\n");
        var latest = Compiler.Compile("precept Sample\nfield Title as string optional\nstate Ready initial\n");

        state.Update(before);
        var full = await handler.Handle(new SemanticTokensParams
        {
            TextDocument = new TextDocumentIdentifier(uri),
        }, default);

        full.Should().NotBeNull();
        var originalResultId = full!.ResultId;
        originalResultId.Should().NotBeNull();

        state.Update(after);
        var firstDelta = await handler.Handle(new SemanticTokensDeltaParams
        {
            TextDocument = new TextDocumentIdentifier(uri),
            PreviousResultId = originalResultId!,
        }, default);

        firstDelta.Should().NotBeNull();
        firstDelta!.IsDelta.Should().BeTrue();

        state.Update(latest);
        var stale = await handler.Handle(new SemanticTokensDeltaParams
        {
            TextDocument = new TextDocumentIdentifier(uri),
            PreviousResultId = originalResultId!,
        }, default);

        stale.Should().NotBeNull();
        stale!.IsDelta.Should().BeFalse(because: "a stale baseline must fall back to a full semantic tokens response");
        stale.Full.Should().NotBeNull();
        stale.Full!.Data.Should().Equal(GetFullSemanticTokens(latest).Data);
    }

    [Fact]
    public async Task HandleDelta_WhenTypedConstantSpanChanges_ReturnsFullResponse()
    {
        var store = new DocumentStore();
        var handler = new SemanticTokensHandler(store);
        var uri = DocumentUri.FromFileSystemPath(@"C:\typed-constant.precept");
        var state = store.GetOrAdd(uri);

        var before = Compiler.Compile("precept Sample\nfield Due as date <- '2026-01-01'\n");
        var after = Compiler.Compile("precept Sample\nfield Due as date <- '2026-01-012'\n");

        state.Update(before);
        var full = await handler.Handle(new SemanticTokensParams
        {
            TextDocument = new TextDocumentIdentifier(uri),
        }, default);

        full.Should().NotBeNull();
        state.Update(after);
        var delta = await handler.Handle(new SemanticTokensDeltaParams
        {
            TextDocument = new TextDocumentIdentifier(uri),
            PreviousResultId = full!.ResultId!,
        }, default);

        delta.Should().NotBeNull();
        delta!.IsDelta.Should().BeFalse(because: "typed-constant span changes invalidate the cached semantic tokens document");
        delta.Full.Should().NotBeNull();
        delta.Full!.Data.Should().Equal(GetFullSemanticTokens(after).Data);
    }

    [Fact]
    public void TryInvalidateForTypedConstantSpanChange_WhenSpanUnchanged_ReturnsFalse()
    {
        var store = new DocumentStore();
        var handler = new SemanticTokensHandler(store);
        var uri = DocumentUri.FromFileSystemPath(@"C:\test.precept");
        var compilation = Compiler.Compile("precept Sample\nfield Due as date <- '2026-01-01'\n");

        var firstCall = handler.TryInvalidateForTypedConstantSpanChange(uri, compilation);
        var secondCall = handler.TryInvalidateForTypedConstantSpanChange(uri, compilation);

        firstCall.Should().BeFalse(because: "no previous state on first call");
        secondCall.Should().BeFalse(because: "typed-constant spans are identical on second call");
    }

    [Fact]
    public void TryInvalidateForTypedConstantSpanChange_WhenSpanChanged_ReturnsTrue()
    {
        var store = new DocumentStore();
        var handler = new SemanticTokensHandler(store);
        var uri = DocumentUri.FromFileSystemPath(@"C:\test.precept");
        var compilationBefore = Compiler.Compile("precept Sample\nfield Due as date <- '2026-01-01'\n");
        var compilationAfter = Compiler.Compile("precept Sample\nfield Due as date <- '2026-01-012'\n");

        handler.TryInvalidateForTypedConstantSpanChange(uri, compilationBefore);
        var result = handler.TryInvalidateForTypedConstantSpanChange(uri, compilationAfter);

        result.Should().BeTrue(because: "the typed-constant token span grew between compilations");
    }

    [Fact]
    public async Task TryInvalidateForTypedConstantSpanChange_WhenSpanChanged_ClearsCachedBaselineStateAsync()
    {
        var store = new DocumentStore();
        var handler = new SemanticTokensHandler(store);
        var uri = DocumentUri.FromFileSystemPath(@"C:\test.precept");
        var state = store.GetOrAdd(uri);
        var compilationBefore = Compiler.Compile("precept Sample\nfield Due as date <- '2026-01-01'\n");
        var compilationAfter = Compiler.Compile("precept Sample\nfield Due as date <- '2026-01-012'\n");

        state.Update(compilationBefore);
        var full = await handler.Handle(new SemanticTokensParams
        {
            TextDocument = new TextDocumentIdentifier(uri),
        }, default);

        var documents = GetPrivateDictionary(handler, "_documents");
        var latestResults = GetPrivateDictionary(handler, "_latestResults");

        documents.Contains(uri).Should().BeTrue(because: "the initial full request seeds the cached semantic token document");
        latestResults.Contains(uri).Should().BeTrue(because: "the initial full request seeds the latest semantic token baseline");

        state.Update(compilationAfter);
        handler.TryInvalidateForTypedConstantSpanChange(uri, compilationAfter)
            .Should().BeTrue(because: "typed-constant span changes must invalidate the cached semantic token baseline");

        documents.Contains(uri).Should().BeFalse(because: "typed-constant span changes remove the cached semantic token document");
        latestResults.Contains(uri).Should().BeFalse(because: "typed-constant span changes must also clear the latest result guard state");

        var delta = await handler.Handle(new SemanticTokensDeltaParams
        {
            TextDocument = new TextDocumentIdentifier(uri),
            PreviousResultId = full!.ResultId!,
        }, default);

        delta.Should().NotBeNull();
        delta!.IsDelta.Should().BeFalse(because: "clearing the cached baseline forces the next request down the full-response path");
        delta.Full.Should().NotBeNull();
        delta.Full!.Data.Should().Equal(GetFullSemanticTokens(compilationAfter).Data);

        handler.TryInvalidateForTypedConstantSpanChange(uri, compilationAfter)
            .Should().BeFalse(because: "after the invalidation pass, the new typed-constant span layout becomes the baseline");
    }

    [Fact]
    public void TryInvalidateForTypedConstantSpanChange_NoTypedConstantTokens_NeverInvalidates()
    {
        var store = new DocumentStore();
        var handler = new SemanticTokensHandler(store);
        var uri = DocumentUri.FromFileSystemPath(@"C:\test.precept");
        var before = Compiler.Compile("precept Sample\nfield Name as string\nstate Draft initial\n");
        var after = Compiler.Compile("precept Sample\nfield Label as string\nstate Draft initial\n");

        // No typed-constant tokens in either compilation.
        handler.TryInvalidateForTypedConstantSpanChange(uri, before);
        var result = handler.TryInvalidateForTypedConstantSpanChange(uri, after);

        result.Should().BeFalse(because: "no typed-constant tokens means no span change to detect");
    }

    private static IDictionary GetPrivateDictionary(SemanticTokensHandler handler, string fieldName)
    {
        var field = typeof(SemanticTokensHandler).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        field.Should().NotBeNull($"SemanticTokensHandler should expose the private {fieldName} cache for this regression assertion.");
        return field!.GetValue(handler).Should().BeAssignableTo<IDictionary>().Which;
    }

    private static string SamplesRoot =>
        Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "samples"));

    private static string ReadSample(string fileName) =>
        File.ReadAllText(Path.Combine(SamplesRoot, fileName));

    private static SemanticTokens GetFullSemanticTokens(Compilation compilation)
    {
        var document = new SemanticTokensDocument(SemanticTokensHandler.BuildLegend());
        CommitSemanticTokens(document.Create(), compilation);
        return document.GetSemanticTokens();
    }

    private static void AssertMergedTokensAreStrictlyOrdered(Compilation compilation) =>
        AssertMergedTokensAreStrictlyOrdered(SemanticTokensHandler.ProjectMergedTokens(compilation));

    private static void AssertMergedTokensAreStrictlyOrdered(ImmutableArray<SemanticTokensHandler.LexicalSemanticToken> merged)
    {
        for (var i = 1; i < merged.Length; i++)
        {
            var previous = merged[i - 1];
            var current = merged[i];

            (current.Line > previous.Line || current.Character >= previous.Character).Should().BeTrue();

            if (current.Line == previous.Line)
            {
                current.Character.Should().BeGreaterThanOrEqualTo(previous.Character + previous.Length,
                    because: "semantic tokens on one line must not overlap");
            }
        }
    }

    private static void AssertSemanticTokensDeltaDoesNotThrow(Compilation before, Compilation after, string path) =>
        AssertSemanticTokensDeltaDoesNotThrow(
            SemanticTokensHandler.ProjectMergedTokens(before),
            SemanticTokensHandler.ProjectMergedTokens(after),
            path);

    private static void AssertSemanticTokensDeltaDoesNotThrow(
        ImmutableArray<SemanticTokensHandler.LexicalSemanticToken> before,
        ImmutableArray<SemanticTokensHandler.LexicalSemanticToken> after,
        string path)
    {
        var document = new SemanticTokensDocument(SemanticTokensHandler.BuildLegend());

        CommitSemanticTokens(document.Create(), before);
        var full = document.GetSemanticTokens();
        full.ResultId.Should().NotBeNull();

        CommitSemanticTokens(document.Edit(new SemanticTokensDeltaParams
        {
            PreviousResultId = full.ResultId!,
            TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(path)),
        }), after);

        var act = () => document.GetSemanticTokensEdits();

        act.Should().NotThrow();
    }

    private static void CommitSemanticTokens(SemanticTokensBuilder builder, Compilation compilation) =>
        CommitSemanticTokens(builder, SemanticTokensHandler.ProjectMergedTokens(compilation));

    private static void CommitSemanticTokens(
        SemanticTokensBuilder builder,
        ImmutableArray<SemanticTokensHandler.LexicalSemanticToken> tokens)
    {
        foreach (var token in tokens)
        {
            builder.Push(token.Line, token.Character, token.Length, token.TokenType);
        }

        builder.Commit();
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
