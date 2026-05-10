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

    [Fact]
    public async Task Completions_FieldDeclarationAfterNamePlusTrailingSpace_SuggestsAsKeyword()
    {
        var completions = await GetCompletionsAsync("""
            precept LoanApplication
            field Amount ¦
            """, " ");
        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().BeEquivalentTo(["as"]);
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

    [Fact]
    public async Task Completions_FieldDeclarationAfterName_OffersAs()
    {
        var completions = await GetCompletionsAsync("""
            precept LoanApplication
            field Amount ¦
            state Draft initial
            """, " ");
        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().BeEquivalentTo(["as"]);
    }

    [Fact]
    public async Task Completions_BooleanFieldModifierPosition_RestrictsToBooleanAppropriateModifiers()
    {
        var completions = await GetCompletionsAsync("""
            precept FeatureFlags
            field Enabled as boolean ¦
            """, " ");
        var labels = completions.Items.Select(item => item.Label).ToArray();
        var expected = Precept.Language.Modifiers.All
            .OfType<Precept.Language.ValueModifierMeta>()
            .Where(meta => meta.ApplicableDeclarationSites.HasFlag(Precept.Language.ValueModifierDeclarationSite.FieldDeclaration))
            .Where(meta =>
                meta.ApplicableTo.Length == 0
                || meta.ApplicableTo.Any(target => target.Kind is null or Precept.Language.TypeKind.Boolean))
            .Select(meta => meta.Token.Text!)
            .ToArray();

        completions.IsIncomplete.Should().BeFalse();
        expected.Should().BeEquivalentTo(["default", "optional", "writable"]);
        labels.Should().BeEquivalentTo(expected);
        labels.Should().NotContain(["max", "maxplaces", "min", "nonnegative", "nonzero", "positive"]);
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

    [Fact]
    public async Task Completions_EventArgumentAfterName_OffersAs()
    {
        var completions = await GetCompletionsAsync("""
            precept LoanApplication
            state Draft initial
            event Submit(Amount ¦)
            """, " ");
        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().BeEquivalentTo(["as"]);
    }

    [Fact]
    public async Task Completions_ActionVerb_UsesActionsCatalog()
    {
        var completions = await GetCompletionsAsync("""
            precept BuildingAccessBadgeRequest
            field BadgePrinted as boolean default false
            state Approved initial
            state Issued terminal
            event PrintBadge
            from Approved on PrintBadge
                ->¦ set BadgePrinted = true
                -> transition Issued
            """);

        var labels = completions.Items.Select(item => item.Label).ToArray();
        var expected = Precept.Language.Actions.All
            .Select(meta => meta.Token.Text)
            .OfType<string>()
            .Distinct(System.StringComparer.Ordinal)
            .ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [MemberData(nameof(ActionFieldTargetCompletionSources))]
    public async Task Completions_ActionFieldTarget_UsesDeclaredFields(string sourceWithCursor)
    {
        var completions = await GetCompletionsAsync(sourceWithCursor);
        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().BeEquivalentTo(["AssignedCrew", "CrewQueue", "DispatchRound"]);
    }

    [Fact]
    public async Task Completions_Expression_IncludeFieldsArgsAndFunctions()
    {
        var completions = await GetCompletionsAsync("""
            precept UtilityOutageReport
            field AssignedCrew as string optional
            field DispatchRound as number default 0 nonnegative
            field Verified as boolean default false
            state VerifiedState initial
            event RegisterCrew(CrewName as string notempty, Priority as number default 1)
            from VerifiedState on RegisterCrew when ¦AssignedCrew is set
                -> set DispatchRound = Priority
                -> no transition
            """);

        var labels = completions.Items.Select(item => item.Label).ToArray();
        var functionNames = Precept.Language.Functions.All
            .Select(meta => meta.Name)
            .Distinct(System.StringComparer.Ordinal)
            .ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain(["AssignedCrew", "DispatchRound", "Verified", "CrewName", "Priority", "true", "false"]);
        labels.Where(functionNames.Contains).Should().BeEquivalentTo(functionNames);
    }

    [Fact]
    public async Task Completions_MemberAccess_UsesTypeAccessors()
    {
        var completions = await GetCompletionsAsync("""
            precept UtilityOutageReport
            field CrewQueue as queue of string
            state VerifiedState initial
            event DispatchCrew
            from VerifiedState on DispatchCrew when CrewQueue.¦count > 0
                -> dequeue CrewQueue
                -> no transition
            """);

        var labels = completions.Items.Select(item => item.Label).ToArray();
        var expected = Precept.Language.Types.GetMeta(Precept.Language.TypeKind.Queue).Accessors
            .Select(accessor => accessor.Name)
            .Distinct(System.StringComparer.Ordinal)
            .ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Completions_ArgDefault_ReusesExpressionCompletions()
    {
        const string expressionSource = """
            precept TravelReimbursement
            field RequestedTotal as number default 0 nonnegative
            field ApprovedTotal as number default 0 nonnegative
            state Draft initial
            state Submitted terminal
            event Submit(Days as number default 1, Amount as number)
            from Draft on Submit when ¦ApprovedTotal <= RequestedTotal
                -> set ApprovedTotal = min(Amount, RequestedTotal)
                -> transition Submitted
            """;

        const string defaultSource = """
            precept TravelReimbursement
            field RequestedTotal as number default 0 nonnegative
            field ApprovedTotal as number default 0 nonnegative
            state Draft initial
            state Submitted terminal
            event Submit(Days as number default ¦1, Amount as number)
            from Draft on Submit when ApprovedTotal <= RequestedTotal
                -> set ApprovedTotal = min(Amount, RequestedTotal)
                -> transition Submitted
            """;

        var expressionCompletions = await GetCompletionsAsync(expressionSource);
        var defaultCompletions = await GetCompletionsAsync(defaultSource);

        var expressionLabels = expressionCompletions.Items.Select(item => item.Label).ToArray();
        var defaultLabels = defaultCompletions.Items.Select(item => item.Label).ToArray();

        defaultCompletions.IsIncomplete.Should().BeFalse();
        defaultLabels.Should().BeEquivalentTo(expressionLabels);
    }

    [Fact]
    public async Task Completions_TypedConstant_UseTypeExamples()
    {
        var completions = await GetCompletionsAsync("""
            precept TravelReimbursement
            field SubmittedOn as date default ¦
            """, "'");

        var labels = completions.Items.Select(item => item.Label).ToArray();
        var expected = Precept.Language.Types.GetMeta(Precept.Language.TypeKind.Date).ContentValidation!.Examples;

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain(expected);
    }

    [Fact]
    public async Task Completions_TypedConstant_SuggestPreviouslyUsedDocumentValues()
    {
        var completions = await GetCompletionsAsync("""
            precept TravelReimbursement
            field SubmittedOn as date <- '2037-08-09'
            field ApprovedOn as date <- ¦
            """, "'");

        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain("2037-08-09");
    }

    [Fact]
    public async Task Completions_TypedConstant_NoExpectedType_ReturnsEmpty()
    {
        var completions = await GetCompletionsAsync("""
            precept TravelReimbursement
            field RequestedTotal as number default 0 nonnegative
            state Draft initial
            event Submit
            from Draft on Submit when ¦RequestedTotal > 0
                -> no transition
            """, "'");

        completions.IsIncomplete.Should().BeFalse();
        completions.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Completions_TopLevelConstruct_UsesSnippetInsertText()
    {
        var meta = Precept.Language.Constructs.GetMeta(Precept.Language.ConstructKind.FieldDeclaration);
        var label = Precept.Language.Tokens.GetMeta(meta.PrimaryLeadingToken).Text!;

        var completions = await GetCompletionsAsync(string.Empty, new Position(0, 0));
        var item = GetItem(completions, label, meta.Description);

        item.InsertText.Should().Be(meta.SnippetTemplate);
        item.InsertTextFormat.Should().Be(InsertTextFormat.Snippet);
        item.Detail.Should().Be(meta.Description);
    }

    [Fact]
    public async Task Completions_Action_UsesSnippetInsertText()
    {
        var meta = Precept.Language.Actions.GetMeta(Precept.Language.ActionKind.Set);

        var completions = await GetCompletionsAsync("""
            precept BadgeRequest
            field BadgePrinted as boolean default false
            state Approved initial
            state Issued terminal
            event PrintBadge
            from Approved on PrintBadge
                ->¦ set BadgePrinted = true
                -> transition Issued
            """);
        var item = GetItem(completions, meta.Token.Text!, meta.Description);

        item.InsertText.Should().Be(meta.SnippetTemplate);
        item.InsertTextFormat.Should().Be(InsertTextFormat.Snippet);
        item.Detail.Should().Be(meta.Description);
    }

    [Fact]
    public async Task Completions_Function_UsesSnippetInsertText()
    {
        var meta = Precept.Language.Functions.GetMeta(Precept.Language.FunctionKind.Min);

        var completions = await GetCompletionsAsync("""
            precept UtilityOutageReport
            field DispatchRound as number default 0 nonnegative
            state VerifiedState initial
            event RegisterCrew(Priority as number default 1)
            from VerifiedState on RegisterCrew when ¦DispatchRound >= Priority
                -> no transition
            """);
        var item = GetItem(completions, meta.Name, meta.Description);

        item.InsertText.Should().Be(meta.SnippetTemplate);
        item.InsertTextFormat.Should().Be(InsertTextFormat.Snippet);
        item.Detail.Should().Be(meta.Description);
    }

    [Fact]
    public async Task Completions_Documentation_UsesHoverDescriptionAndUsageExample()
    {
        var meta = Precept.Language.Functions.GetMeta(Precept.Language.FunctionKind.Min);

        var completions = await GetCompletionsAsync("""
            precept UtilityOutageReport
            field DispatchRound as number default 0 nonnegative
            state VerifiedState initial
            event RegisterCrew(Priority as number default 1)
            from VerifiedState on RegisterCrew when ¦DispatchRound >= Priority
                -> no transition
            """);
        var item = GetItem(completions, meta.Name, meta.Description);

        item.Documentation.Should().NotBeNull();
        item.Documentation!.MarkupContent.Should().NotBeNull();
        item.Documentation.MarkupContent!.Kind.Should().Be(MarkupKind.Markdown);
        item.Documentation.MarkupContent.Value.Should().Contain(meta.HoverDescription!);
        item.Documentation.MarkupContent.Value.Should().Contain(meta.UsageExample!);
    }

    [Fact]
    public async Task Completions_SortsSemanticSymbolsBeforeCatalogItems()
    {
        var completions = await GetCompletionsAsync("""
            precept NamingOverlap
            field Priority as number default 0 nonnegative
            state Draft initial
            event Evaluate(Priority as number default 0)
            from Draft on Evaluate when ¦Priority >= min(Priority, 10)
                -> no transition
            """);

        var items = completions.Items.Where(item => item.Label == "Priority").ToArray();
        var function = GetItem(
            completions,
            Precept.Language.Functions.GetMeta(Precept.Language.FunctionKind.Min).Name,
            Precept.Language.Functions.GetMeta(Precept.Language.FunctionKind.Min).Description);

        items.Should().HaveCount(2);
        items[0].Detail.Should().Be("Event argument");
        items[1].Detail.Should().Be("Field");
        string.CompareOrdinal(items[0].SortText, function.SortText).Should().BeLessThan(0);
        string.CompareOrdinal(items[1].SortText, function.SortText).Should().BeLessThan(0);
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

    private static Task<CompletionList> GetCompletionsAsync(string sourceWithCursor)
    {
        var position = GetCursorPosition(sourceWithCursor);
        var source = sourceWithCursor.Replace(CursorMarker, string.Empty, StringComparison.Ordinal);
        return GetCompletionsAsync(source, position);
    }

    private static Task<CompletionList> GetCompletionsAsync(string sourceWithCursor, string triggerCharacter)
    {
        var position = GetCursorPosition(sourceWithCursor);
        var source = sourceWithCursor.Replace(CursorMarker, string.Empty, StringComparison.Ordinal);
        return GetCompletionsAsync(source, position, triggerCharacter);
    }

    private static async Task<CompletionList> GetCompletionsAsync(string source, Position position, string triggerCharacter)
    {
        var store = new DocumentStore();
        var uri = DocumentUri.FromFileSystemPath(@"C:\completion-test.precept");
        store.GetOrAdd(uri).Update(Precept.Compiler.Compile(source));

        var handler = new CompletionHandler(store);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier(uri),
            Position = position,
            Context = new CompletionContext
            {
                TriggerKind = CompletionTriggerKind.TriggerCharacter,
                TriggerCharacter = triggerCharacter,
            },
        };

        return await handler.Handle(request, CancellationToken.None);
    }

    private static CompletionItem GetItem(CompletionList completions, string label, string detail) =>
        completions.Items.Single(item =>
            string.Equals(item.Label, label, StringComparison.Ordinal)
            && string.Equals(item.Detail, detail, StringComparison.Ordinal));

    private static Position GetCursorPosition(string sourceWithCursor)
    {
        var markerIndex = sourceWithCursor.IndexOf(CursorMarker, StringComparison.Ordinal);
        markerIndex.Should().BeGreaterThanOrEqualTo(0, "each test source must include a cursor marker");

        var line = 0;
        var character = 0;
        for (var index = 0; index < markerIndex; index++)
        {
            if (sourceWithCursor[index] == '\n')
            {
                line++;
                character = 0;
            }
            else if (sourceWithCursor[index] != '\r')
            {
                character++;
            }
        }

        return new Position(line, character);
    }

    public static TheoryData<string> ActionFieldTargetCompletionSources =>
    [
        """
        precept UtilityOutageReport
        field AssignedCrew as string optional
        field CrewQueue as queue of string
        field DispatchRound as number default 0 nonnegative
        state VerifiedState initial
        event DispatchCrew
        from VerifiedState on DispatchCrew
            -> set¦ AssignedCrew = "Crew-01"
            -> no transition
        """,
        """
        precept UtilityOutageReport
        field AssignedCrew as string optional
        field CrewQueue as queue of string
        field DispatchRound as number default 0 nonnegative
        state VerifiedState initial
        event DispatchCrew
        from VerifiedState on DispatchCrew
            -> dequeue CrewQueue into¦ AssignedCrew
            -> no transition
        """,
    ];

    private const string CursorMarker = "¦";
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
