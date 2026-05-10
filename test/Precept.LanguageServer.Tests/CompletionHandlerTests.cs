using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.LanguageServer.Handlers;
using Xunit;

namespace Precept.LanguageServer.Tests;

public class CompletionHandlerTests
{
    [Fact]
    public void GetRegistrationOptions_AdvertisesSpaceQuoteDotArrowAndTildeTriggers()
    {
        var handler = new CompletionHandler(new DocumentStore());
        var options = handler.GetRegistrationOptions(new CompletionCapability(), new ClientCapabilities());

        options.TriggerCharacters.Should().BeEquivalentTo([" ", "'", ".", ">", "~"]);
    }

    [Fact]
    public async Task Completions_TopLevel_IncludesConstructKeywords()
    {
        var completions = await GetCompletionsAsync(string.Empty, new Position(0, 0));
        var labels = completions.Items.Select(item => item.Label).ToArray();
        var expected = Precept.Language.Constructs.All
            .Where(meta => meta.AllowedIn.Length == 0)
            .Select(meta => Precept.Language.Tokens.GetMeta(meta.PrimaryLeadingToken).Text)
            .OfType<string>()
            .Distinct(System.StringComparer.Ordinal)
            .ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain(expected);
    }

    [Fact]
    public async Task Completions_NoDocument_ReturnsIncomplete()
    {
        var handler = new CompletionHandler(new DocumentStore());
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(@"C:\completion-test.precept")),
            Position = new Position(0, 0),
        };

        var completions = await handler.Handle(request, CancellationToken.None);

        completions.IsIncomplete.Should().BeTrue();
        completions.Items.Should().BeEmpty();
    }

    [Theory]
    [InlineData("from ")]
    [InlineData("to ")]
    public async Task Completions_StateTarget_IncludesDeclaredStates(string prefix)
    {
        var source = $$"""
            precept LoanApplication
            state Draft initial
            state UnderReview
            state Approved terminal
            event Submit
            {{prefix}}
            """;

        var completions = await GetCompletionsAsync(source, new Position(5, prefix.Length));
        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain(["Draft", "UnderReview", "Approved"]);
    }

    [Theory]
    [InlineData("modify ")]
    [InlineData("omit ")]
    public async Task Completions_FieldTarget_IncludesDeclaredFields(string prefix)
    {
        var source = $$"""
            precept LoanApplication
            field Amount as number
            field DecisionNote as string optional
            state Draft initial
            {{prefix}}
            """;

        var completions = await GetCompletionsAsync(source, new Position(4, prefix.Length));
        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain(["Amount", "DecisionNote"]);
    }

    [Theory]
    [InlineData("on ")]
    [InlineData("when ")]
    public async Task Completions_EventTarget_IncludesDeclaredEvents(string prefix)
    {
        var source = $$"""
            precept LoanApplication
            state Draft initial
            event Submit
            event Approve(Note as string optional notempty)
            {{prefix}}
            """;

        var completions = await GetCompletionsAsync(source, new Position(4, prefix.Length));
        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain(["Submit", "Approve"]);
    }

    private static async Task<CompletionList> GetCompletionsAsync(string source, Position position)
    {
        var store = new DocumentStore();
        var uri = DocumentUri.FromFileSystemPath(@"C:\completion-test.precept");
        store.GetOrAdd(uri).Update(Precept.Compiler.Compile(source));

        var handler = new CompletionHandler(store);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier(uri),
            Position = position,
        };

        return await handler.Handle(request, CancellationToken.None);
    }
}

internal static class LanguageClientTestExtensions
{
    public static void DidOpen(this ITextDocumentLanguageClient client, DidOpenTextDocumentParams @params) =>
        DidOpenTextDocumentExtensions.DidOpenTextDocument(client, @params);

    public static ILanguageClientRegistry OnPublishDiagnostics(
        this ITextDocumentLanguageClient client,
        Action<PublishDiagnosticsParams> handler)
    {
        var registry = ((IServiceProvider)client).GetService(typeof(ILanguageClientRegistry)) as ILanguageClientRegistry;
        registry.Should().NotBeNull();
        return PublishDiagnosticsExtensions.OnPublishDiagnostics(registry!, handler);
    }
}
