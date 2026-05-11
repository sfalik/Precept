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
    public async Task Completions_TypedConstant_RuleComparison_UsesPeerOperandType()
    {
        var completions = await GetCompletionsAsync("""
            precept LoanApplication
            field ApprovedAmount as money in 'USD' default '0.00 USD'
            rule ApprovedAmount > ¦ because "Approved amount must be positive"
            state Draft initial terminal
            """, "'");

        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain("0.00 USD");
        labels.Should().Contain(Precept.Language.Types.GetMeta(Precept.Language.TypeKind.Money).ContentValidation!.Examples);
    }

    [Fact]
    public void TypedConstantCursorDiagnostic_DefaultEmptyLiteral_ReportsTopLevelContextAndTypedConstantToken()
    {
        var (compilation, position) = GetCompilationAtCursor("""
            precept TravelReimbursement
            field SubmittedOn as date default '2037-08-09'
            field ApprovedOn as date default '¦'
            state Draft initial terminal
            """);

        var context = SlotContextResolver.GetCursorContext(compilation, position);
        var tokenIndex = FindTokenAtOrBeforeCursor(compilation, position);
        var token = compilation.Tokens.Tokens[tokenIndex];

        context.Should().Be(SlotContext.TopLevel);
        token.Kind.Should().Be(Precept.Language.TokenKind.TypedConstant);
        Contains(token.Span, position).Should().BeTrue();
    }

    [Fact]
    public void TypedConstantCursorDiagnostic_ExpressionEmptyLiteral_ReportsExpressionContextAndTypedConstantToken()
    {
        var (compilation, position) = GetCompilationAtCursor("""
            precept LoanApplication
            field ApprovedAmount as money in 'USD' default '0.00 USD'
            rule ApprovedAmount > '¦' because "Approved amount must be positive"
            state Draft initial terminal
            """);

        var context = SlotContextResolver.GetCursorContext(compilation, position);
        var tokenIndex = FindTokenAtOrBeforeCursor(compilation, position);
        var token = compilation.Tokens.Tokens[tokenIndex];

        context.Should().Be(SlotContext.InExpression);
        token.Kind.Should().Be(Precept.Language.TokenKind.TypedConstant);
        Contains(token.Span, position).Should().BeTrue();
    }

    [Fact]
    public async Task Completions_TypedConstant_InvokedInsideEmptyDefaultLiteral_UsesTypedConstantValues()
    {
        const string sourceWithCursor = """
            precept TravelReimbursement
            field SubmittedOn as date default '2037-08-09'
            field ApprovedOn as date default '¦'
            state Draft initial terminal
            """;

        var completions = await GetCompletionsAsync(sourceWithCursor, new CompletionContext
        {
            TriggerKind = CompletionTriggerKind.Invoked,
            TriggerCharacter = string.Empty,
        });
        var noContextCompletions = await GetCompletionsAsync(sourceWithCursor);

        var labels = completions.Items.Select(item => item.Label).ToArray();
        var noContextLabels = noContextCompletions.Items.Select(item => item.Label).ToArray();
        var keywords = completions.Items
            .Where(item => item.Kind == CompletionItemKind.Keyword)
            .Select(item => item.Label)
            .ToArray();

        completions.IsIncomplete.Should().BeFalse();
        noContextCompletions.IsIncomplete.Should().BeFalse();
        keywords.Should().BeEmpty();
        labels.Should().Contain("2037-08-09");
        labels.Should().Contain(Precept.Language.Types.GetMeta(Precept.Language.TypeKind.Date).ContentValidation!.Examples);
        labels.Should().NotContain(["field", "rule", "state"]);
        labels.Should().BeEquivalentTo(noContextLabels);
    }

    [Fact]
    public async Task Completions_TypedConstant_InvokedInsideEmptyExpressionLiteral_UsesTypedConstantValues()
    {
        const string sourceWithCursor = """
            precept LoanApplication
            field ApprovedAmount as money in 'USD' default '0.00 USD'
            rule ApprovedAmount > '¦' because "Approved amount must be positive"
            state Draft initial terminal
            """;

        var completions = await GetCompletionsAsync(sourceWithCursor, new CompletionContext
        {
            TriggerKind = CompletionTriggerKind.Invoked,
            TriggerCharacter = string.Empty,
        });
        var noContextCompletions = await GetCompletionsAsync(sourceWithCursor);

        var labels = completions.Items.Select(item => item.Label).ToArray();
        var noContextLabels = noContextCompletions.Items.Select(item => item.Label).ToArray();
        var keywords = completions.Items
            .Where(item => item.Kind == CompletionItemKind.Keyword)
            .Select(item => item.Label)
            .ToArray();

        completions.IsIncomplete.Should().BeFalse();
        noContextCompletions.IsIncomplete.Should().BeFalse();
        keywords.Should().BeEmpty();
        labels.Should().Contain("0.00 USD");
        labels.Should().Contain(Precept.Language.Types.GetMeta(Precept.Language.TypeKind.Money).ContentValidation!.Examples);
        labels.Should().NotContain(["ApprovedAmount", "false", "true", "abs", "approximate", "ceil", "clamp"]);
        labels.Should().BeEquivalentTo(noContextLabels);
    }

    [Fact]
    public async Task Completions_TypedConstant_NoKeywordsInsideTypedConstantSpan()
    {
        // Ctrl+Space (no trigger char) inside '' must not bleed top-level keyword completions.
        var completions = await GetCompletionsAsync("""
            precept LoanApplication
            field Status as text
            rule Status == '¦' because "Status must be set"
            state Draft initial terminal
            """);

        var keywords = completions.Items
            .Where(item => item.Kind == CompletionItemKind.Keyword)
            .Select(item => item.Label)
            .ToArray();

        keywords.Should().BeEmpty("top-level construct keywords must not appear inside a typed constant");
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

    // ─── Slice 1 — Type-Branching on ' Trigger ──────────────────────────────────

    [Fact]
    public async Task TypedConstant_Boolean_ShowsTrueAndFalse()
    {
        var completions = await GetCompletionsAsync("""
            precept FlagTest
            field Active as boolean default ¦
            state Open initial terminal
            """, "'");

        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().BeEquivalentTo(["true", "false"]);
    }

    [Fact]
    public async Task TypedConstant_Temporal_ShowsStarters()
    {
        var completions = await GetCompletionsAsync("""
            precept WorkflowTest
            field Delay as duration default ¦
            """, "'");

        var labels = completions.Items.Select(item => item.Label).ToArray();
        var expected = Precept.Language.Types.GetMeta(Precept.Language.TypeKind.Duration).ContentValidation!.Examples;

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain(expected);
    }

    [Fact]
    public async Task TypedConstant_Money_ShowsExamples()
    {
        var completions = await GetCompletionsAsync("""
            precept PaymentTest
            field Cost as money default ¦
            """, "'");

        var labels = completions.Items.Select(item => item.Label).ToArray();
        var expected = Precept.Language.Types.GetMeta(Precept.Language.TypeKind.Money).ContentValidation!.Examples;

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain(expected);
    }

    [Fact]
    public async Task TypedConstant_Text_NoAutoPopup()
    {
        // text is free-form; no examples exist and no reused values → empty on ' trigger.
        var completions = await GetCompletionsAsync("""
            precept ProfileTest
            field FullName as text default ¦
            """, "'");

        completions.IsIncomplete.Should().BeFalse();
        completions.Items.Should().BeEmpty("text fields have no enumerable vocabulary; autocomplete noise is unwanted");
    }

    [Fact]
    public async Task TypedConstant_Currency_ShowsAllCodes()
    {
        var completions = await GetCompletionsAsync("""
            precept CurrencyTest
            field BaseCurrency as currency default ¦
            """, "'");

        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain(["USD", "EUR", "GBP"]);
    }

    // ─── Slice 2 — Space Trigger Slot Detection ──────────────────────────────────

    [Fact]
    public async Task TypedConstant_SpaceTrigger_Temporal_AfterNumber_ShowsUnits()
    {
        var completions = await GetCompletionsAsync("""
            precept WorkflowTest
            field Delay as duration default '3 ¦'
            """, " ");

        var labels = completions.Items.Select(item => item.Label).ToArray();
        var unitPluralNames = Precept.Language.TemporalUnits.AllEntries.Select(e => e.Plural).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain(unitPluralNames);
        labels.Should().NotContain(["field", "state", "event", "rule"]);
    }

    [Fact]
    public async Task TypedConstant_SpaceTrigger_Money_AfterAmount_ShowsCurrencyCodes()
    {
        var completions = await GetCompletionsAsync("""
            precept PaymentTest
            field Cost as money default '100 ¦'
            """, " ");

        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain(["USD", "EUR", "GBP"]);
        labels.Should().NotContain(["field", "state", "event", "rule"]);
    }

    [Fact]
    public async Task TypedConstant_SpaceTrigger_SegmentComplete_ShowsPlusContinuation()
    {
        // Ctrl+Space right after a complete temporal unit → only the + continuation item is offered.
        var completions = await GetCompletionsAsync("""
            precept WorkflowTest
            field Delay as duration default '3 days¦'
            """, new CompletionContext
            {
                TriggerKind = CompletionTriggerKind.Invoked,
                TriggerCharacter = string.Empty,
            });

        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain("+");
    }

    // ─── Slice 3 — Qualifier-Aware Filtering ─────────────────────────────────────

    [Fact]
    public async Task TypedConstant_Qualifier_Money_InUSD_SpaceAfterAmount_ShowsOnlyUSD()
    {
        var completions = await GetCompletionsAsync("""
            precept PaymentTest
            field Cost as money in 'USD' default '100 ¦'
            """, " ");

        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().BeEquivalentTo(["USD"], "qualifier hard-filter must reduce the candidate set to exactly the declared currency code");
    }

    [Fact]
    public async Task TypedConstant_Qualifier_Temporal_InDays_AfterNumber_ShowsOnlyDays()
    {
        var completions = await GetCompletionsAsync("""
            precept WorkflowTest
            field Grace as period in 'days' default '30 ¦'
            """, " ");

        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain("days");
        labels.Should().NotContain(["hours", "minutes", "seconds", "weeks", "months", "years"],
            "qualifier 'in days' must hard-filter temporal units to the declared unit only");
    }

    // ─── Slice 4 — Compound Temporal Full Cycle ───────────────────────────────────

    [Fact]
    public async Task TypedConstant_Compound_AfterFirstSegment_ShowsPlus()
    {
        // Ctrl+Space after a complete temporal segment shows the + continuation affordance.
        var completions = await GetCompletionsAsync("""
            precept RetryTest
            field RetryAfter as duration default '2 hours¦'
            """, new CompletionContext
            {
                TriggerKind = CompletionTriggerKind.Invoked,
                TriggerCharacter = string.Empty,
            });

        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain("+", "after a complete temporal segment the only proactive continuation choice is +");
    }

    [Fact]
    public async Task TypedConstant_Compound_AfterPlus_ShowsNothing()
    {
        // Space typed immediately after + (before the next number) → nothing until a number is typed.
        var completions = await GetCompletionsAsync("""
            precept RetryTest
            field RetryAfter as duration default '2 hours + ¦'
            """, " ");

        completions.IsIncomplete.Should().BeFalse();
        completions.Items.Should().BeEmpty("after '+' the author must type a number before units are offered");
    }

    [Fact]
    public async Task TypedConstant_Compound_AfterPlusNumber_Space_ShowsUnits()
    {
        // Space after <number> in the second segment → temporal units again, same as the first segment.
        var completions = await GetCompletionsAsync("""
            precept RetryTest
            field RetryAfter as duration default '2 hours + 30 ¦'
            """, " ");

        var labels = completions.Items.Select(item => item.Label).ToArray();
        var unitPluralNames = Precept.Language.TemporalUnits.AllEntries.Select(e => e.Plural).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain(unitPluralNames);
    }

    // ─── Slice 5 — Edge Cases ─────────────────────────────────────────────────────

    [Fact]
    public async Task TypedConstant_SpaceInsideText_NoCompletions()
    {
        // Space inside a text literal must not open completion — space is not a slot boundary in text.
        var completions = await GetCompletionsAsync("""
            precept ProfileTest
            field FullName as text default 'hello ¦'
            """, " ");

        completions.IsIncomplete.Should().BeFalse();
        completions.Items.Should().BeEmpty("space inside a text literal must not trigger any completions");
    }

    [Fact]
    public async Task TypedConstant_CtrlSpaceInsidePartialTemporal_ShowsUnitsForCurrentSlot()
    {
        // Cursor is mid-unit ('3 da|ys') — slot phase is UnitTyping → full temporal unit list returned;
        // VS Code handles prefix filtering for 'da' client-side.
        var completions = await GetCompletionsAsync("""
            precept WorkflowTest
            field Delay as duration default '3 da¦ys'
            """, new CompletionContext
            {
                TriggerKind = CompletionTriggerKind.Invoked,
                TriggerCharacter = string.Empty,
            });

        var labels = completions.Items.Select(item => item.Label).ToArray();
        var unitPluralNames = Precept.Language.TemporalUnits.AllEntries.Select(e => e.Plural).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain(unitPluralNames);
    }

    [Fact]
    public async Task TypedConstant_BooleanInRule_SameBehaviorAsDefault()
    {
        // A boolean literal inside a rule guard must show the same closed set as a boolean field default.
        var completions = await GetCompletionsAsync("""
            precept FlagTest
            field Active as boolean default false
            state Open initial terminal
            event Toggle
            from Open on Toggle when Active == ¦
                -> no transition
            """, "'");

        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().BeEquivalentTo(["true", "false"]);
    }

    [Fact]
    public async Task TypedConstant_MoneyQualifier_SpaceAfterAmount_ShowsOnlyDeclaredCurrency()
    {
        var completions = await GetCompletionsAsync("""
            precept PaymentTest
            field Cost as money in 'USD' default '100 ¦'
            """, " ");

        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().BeEquivalentTo(["USD"]);
    }

    [Theory]
    [InlineData("boolean")]
    [InlineData("duration")]
    [InlineData("money")]
    public async Task TypedConstant_NoKeywordsInAnyTypedConstant(string typeName)
    {
        // Ctrl+Space inside '' for any typed-constant type must never bleed keyword items.
        var source = $$"""
            precept NoKeywordTest
            field Value as {{typeName}} default '¦'
            state Open initial terminal
            """;

        var completions = await GetCompletionsAsync(source, new CompletionContext
        {
            TriggerKind = CompletionTriggerKind.Invoked,
            TriggerCharacter = string.Empty,
        });

        var keywords = completions.Items
            .Where(item => item.Kind == CompletionItemKind.Keyword)
            .Select(item => item.Label)
            .ToArray();

        keywords.Should().BeEmpty($"top-level construct keywords must never appear inside a {typeName} typed constant");
    }

    // ─── Slice 5 — Ctrl+Space Recovery ───────────────────────────────────────────

    [Fact]
    public async Task TypedConstant_CtrlSpace_EmptyBoolean_ShowsTrueFalse()
    {
        var completions = await GetCompletionsAsync("""
            precept FlagTest
            field Active as boolean default '¦'
            state Open initial terminal
            """, new CompletionContext
            {
                TriggerKind = CompletionTriggerKind.Invoked,
                TriggerCharacter = string.Empty,
            });

        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().BeEquivalentTo(["true", "false"]);
    }

    [Fact]
    public async Task TypedConstant_CtrlSpace_EmptyTemporal_ShowsStarters()
    {
        var completions = await GetCompletionsAsync("""
            precept WorkflowTest
            field Delay as duration default '¦'
            """, new CompletionContext
            {
                TriggerKind = CompletionTriggerKind.Invoked,
                TriggerCharacter = string.Empty,
            });

        var labels = completions.Items.Select(item => item.Label).ToArray();
        var expected = Precept.Language.Types.GetMeta(Precept.Language.TypeKind.Duration).ContentValidation!.Examples;

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain(expected);
    }

    [Fact]
    public async Task TypedConstant_CtrlSpace_EmptyMoney_ShowsExamples()
    {
        var completions = await GetCompletionsAsync("""
            precept PaymentTest
            field Cost as money default '¦'
            """, new CompletionContext
            {
                TriggerKind = CompletionTriggerKind.Invoked,
                TriggerCharacter = string.Empty,
            });

        var labels = completions.Items.Select(item => item.Label).ToArray();
        var expected = Precept.Language.Types.GetMeta(Precept.Language.TypeKind.Money).ContentValidation!.Examples;

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain(expected);
    }

    [Fact]
    public async Task TypedConstant_CtrlSpace_PartialMoneyCode_ShowsFilteredCodes()
    {
        // Ctrl+Space at '100 U| (UnitTyping phase) → currency codes returned; VS Code filters by 'U' prefix client-side.
        var completions = await GetCompletionsAsync("""
            precept PaymentTest
            field Cost as money default '100 U¦'
            """, new CompletionContext
            {
                TriggerKind = CompletionTriggerKind.Invoked,
                TriggerCharacter = string.Empty,
            });

        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain(["USD", "GBP"]);
        labels.Should().NotContain(["field", "state", "event", "rule"]);
    }

    private static async Task<CompletionList> GetCompletionsAsync(string source, Position position)
    {
        return await GetCompletionsAsync(source, position, context: null);
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
        return GetCompletionsAsync(source, position, new CompletionContext
        {
            TriggerKind = CompletionTriggerKind.TriggerCharacter,
            TriggerCharacter = triggerCharacter,
        });
    }

    private static Task<CompletionList> GetCompletionsAsync(string sourceWithCursor, CompletionContext? context)
    {
        var position = GetCursorPosition(sourceWithCursor);
        var source = sourceWithCursor.Replace(CursorMarker, string.Empty, StringComparison.Ordinal);
        return GetCompletionsAsync(source, position, context);
    }

    private static async Task<CompletionList> GetCompletionsAsync(string source, Position position, CompletionContext? context)
    {
        var store = new DocumentStore();
        var uri = DocumentUri.FromFileSystemPath(@"C:\completion-test.precept");
        store.GetOrAdd(uri).Update(Precept.Compiler.Compile(source));

        var handler = new CompletionHandler(store);
        var request = new CompletionParams
        {
            TextDocument = new TextDocumentIdentifier(uri),
            Position = position,
            Context = context,
        };

        return await handler.Handle(request, CancellationToken.None);
    }

    private static CompletionItem GetItem(CompletionList completions, string label, string detail) =>
        completions.Items.Single(item =>
            string.Equals(item.Label, label, StringComparison.Ordinal)
            && string.Equals(item.Detail, detail, StringComparison.Ordinal));

    private static (Precept.Pipeline.Compilation Compilation, Position Position) GetCompilationAtCursor(string sourceWithCursor)
    {
        var position = GetCursorPosition(sourceWithCursor);
        var source = sourceWithCursor.Replace(CursorMarker, string.Empty, StringComparison.Ordinal);
        return (Precept.Compiler.Compile(source), position);
    }

    private static int FindTokenAtOrBeforeCursor(Precept.Pipeline.Compilation compilation, Position position)
    {
        var candidate = -1;
        var tokens = compilation.Tokens.Tokens;
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

        candidate.Should().BeGreaterThanOrEqualTo(0);
        return candidate;
    }

    private static bool IsBefore(Position position, Precept.Pipeline.SourceSpan span)
    {
        var line = position.Line + 1;
        var character = position.Character + 1;
        if (line != span.StartLine)
        {
            return line < span.StartLine;
        }

        return character < span.StartColumn;
    }

    private static bool Contains(Precept.Pipeline.SourceSpan span, Position position)
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
