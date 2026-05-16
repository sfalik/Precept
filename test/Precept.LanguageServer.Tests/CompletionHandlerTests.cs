using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Client;
using OmniSharp.Extensions.LanguageServer.Protocol.Client.Capabilities;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.Language;
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
            .Where(meta => meta.AllowedIn.Length == 0 && meta.Entries.Length > 0)
            .Select(meta => Precept.Language.Tokens.GetMeta(meta.PrimaryLeadingToken).Text)
            .OfType<string>()
            .Distinct(System.StringComparer.Ordinal)
            .ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain(expected);
    }

    [Fact]
    public async Task Completions_TopLevel_WithWhitespaceOnlyPrefix_IncludesConstructKeywords()
    {
        var completions = await GetCompletionsAsync("""
            precept LoanApplication
                ¦
            """);
        var labels = completions.Items.Select(item => item.Label).ToArray();

        labels.Should().Contain(["field", "state", "event", "rule", "from", "in", "on"]);
    }

    [Fact]
    public async Task Completions_AfterAs_SuppressesTopLevelKeywords()
    {
        var completions = await GetCompletionsAsync("""
            precept LoanApplication
            field Amount as ¦
            """);
        var labels = completions.Items.Select(item => item.Label).ToArray();

        labels.Should().Contain(["string", "number", "money"]);
        labels.Should().NotContain(["precept", "field", "state", "event", "from", "rule"]);
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
    [InlineData("in ")]
    [InlineData("from ")]
    [InlineData("to ")]
    public async Task Completions_StateTarget_IncludesDeclaredStatesAndAnyWildcard(string prefix)
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
        labels.Should().Contain(["Draft", "UnderReview", "Approved", "any"]);
        labels.Should().NotContain(["precept", "field", "state", "event", "from", "rule"]);
    }

    [Theory]
    [InlineData("from")]
    [InlineData("to")]
    [InlineData("in")]
    public async Task Completions_StateTargetListContinuation_OffersRemainingDeclaredStates(string leadingKeyword)
    {
        var completions = await GetCompletionsAsync($$"""
            precept Test
            state off initial
            state running
            event start
            {{leadingKeyword}} off, ¦
            -> no transition
            """, " ");
        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain("running");
        labels.Should().NotContain(["off", "any", "precept", "field", "state", "event", "from", "rule"]);
    }

    [Fact]
    public async Task Completions_StateDeclarationListContinuation_OffersRemainingDeclaredStates()
    {
        var completions = await GetCompletionsAsync("""
            precept Test
            state off initial
            state running
            state off, ¦
            event start
            from off on start -> transition running
            """, " ");
        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain("running");
        labels.Should().NotContain(["off", "initial", "terminal", "any", "precept", "field", "state", "event", "from", "rule"]);
    }

    [Theory]
    [InlineData("modify ")]
    [InlineData("omit ")]
    public async Task Completions_AccessFieldTarget_IncludesDeclaredFieldsAndAllWildcard(string prefix)
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
        labels.Should().Contain(["Amount", "DecisionNote", "all"]);
    }

    [Theory]
    [InlineData("modify")]
    [InlineData("omit")]
    public async Task Completions_AccessFieldTargetListContinuation_OffersRemainingDeclaredFields(string accessVerb)
    {
        var completions = await GetCompletionsAsync($$"""
            precept LoanApplication
            field Amount as number
            field DecisionNote as string optional
            field RiskScore as integer default 0
            state Draft initial
            in Draft {{accessVerb}} Amount, ¦
            """, " ");
        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain(["DecisionNote", "RiskScore"]);
        labels.Should().NotContain(["Amount", "all", "precept", "field", "state", "event", "from", "rule"]);
    }

    [Theory]
    [InlineData("in Draft", new[] { "ensure", "when", "modify", "omit" }, new[] { "on", "->" })]
    [InlineData("from Draft", new[] { "on", "ensure", "when", "->" }, new[] { "modify", "omit" })]
    [InlineData("to Approved", new[] { "ensure", "when", "->" }, new[] { "on", "modify", "omit" })]
    public async Task Completions_AfterStateTarget_OffersScopedVerbs(string clause, string[] expected, string[] unexpected)
    {
        var completions = await GetCompletionsAsync($$"""
            precept LoanApplication
            state Draft initial
            state Approved terminal
            event Submit
            {{clause}} ¦
            """, " ");
        var labels = completions.Items.Select(item => item.Label).ToArray();

        labels.Should().Contain(expected);
        labels.Should().NotContain(unexpected);
    }

    [Fact]
    public async Task Completions_TransitionRowAfterStateTarget_OffersOnWhenAndArrow()
    {
        var completions = await GetCompletionsAsync("""
            precept BuildingAccessBadgeRequest
            state Approved initial
            state Issued terminal
            event PrintBadge
            from Approved ¦
            """, " ");
        var labels = completions.Items.Select(item => item.Label).ToArray();

        labels.Should().Contain(["on", "when", "->"]);
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
        labels.Should().BeEquivalentTo(expected.Concat(["<- "]));
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
        labels.Should().NotContain(["precept", "field", "state", "event", "from", "rule"]);
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
    public async Task Completions_FieldDeclarationAfterType_OffersQualifiersModifiersAndComputedArrow()
    {
        var completions = await GetCompletionsAsync("""
            precept Inventory
            field Weight as quantity ¦
            """, " ");
        var labels = completions.Items.Select(item => item.Label).ToArray();

        labels.Should().Contain(["in", "of", "default", "min", "max", "<- "]);
        labels.Should().NotContain(["precept", "field", "state", "event", "from", "rule"]);
    }

    [Fact]
    public async Task Completions_EventArgumentAfterType_OffersQualifiersAndValueModifiers()
    {
        var completions = await GetCompletionsAsync("""
            precept Inventory
            event Measure(Weight as quantity ¦)
            """, " ");
        var labels = completions.Items.Select(item => item.Label).ToArray();

        labels.Should().Contain(["in", "of", "default", "min", "max"]);
        labels.Should().NotContain("<- ");
        labels.Should().NotContain(["precept", "field", "state", "event", "from", "rule"]);
    }

    [Fact]
    public async Task Completions_AfterArrow_UsesActionsAndOutcomesCatalog()
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
            .Concat(Precept.Language.Outcomes.All
                .Select(meta => Precept.Language.Tokens.GetMeta(meta.LeadingToken).Text)
                .OfType<string>())
            .Distinct(System.StringComparer.Ordinal)
            .ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Completions_StateActionAfterArrow_SuppressesOutcomes()
    {
        var completions = await GetCompletionsAsync("""
            precept BuildingAccessBadgeRequest
            field BadgePrinted as boolean default false
            state Approved initial
            state Issued terminal
            to Issued
                ->¦ set BadgePrinted = true
            """);
        var labels = completions.Items.Select(item => item.Label).ToArray();

        labels.Should().Contain(Precept.Language.Actions.All
            .Select(meta => meta.Token.Text)
            .OfType<string>()
            .Distinct(System.StringComparer.Ordinal)
            .ToArray());
        labels.Should().NotContain(["transition", "no", "reject"]);
    }

    [Fact]
    public async Task Completions_ActionChainContinuationArrow_UsesActionItems()
    {
        var completions = await GetCompletionsAsync("""
            precept Test
            field count as integer
            event Increment initial

            on Increment
                -> set count = count + 1
                ->¦ 
            """);
        var labels = completions.Items.Select(item => item.Label).ToArray();
        var expected = Precept.Language.Actions.All
            .Where(meta => meta.PrimaryActionKind is null)
            .Select(meta => meta.Token.Text)
            .OfType<string>()
            .Distinct(System.StringComparer.Ordinal)
            .ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().BeEquivalentTo(expected);
        labels.Should().NotContain(["precept", "field", "state", "event", "from", "rule"]);
    }

    [Fact]
    public async Task Completions_TrailingArrowInTransitionRowActionChain_UsesActionItems()
    {
        var completions = await GetCompletionsAsync("""
            precept Test
            field counter as integer
            state off initial
            state running
            event reset
            from off, running on reset
                -> set counter = 0
                -> clear events
                ->¦ 
            """);
        var labels = completions.Items.Select(item => item.Label).ToArray();
        var expected = Precept.Language.Actions.All
            .Where(meta => meta.PrimaryActionKind is null)
            .Select(meta => meta.Token.Text)
            .OfType<string>()
            .Distinct(System.StringComparer.Ordinal)
            .ToArray();
        var unexpectedOutcomes = Precept.Language.Outcomes.All
            .Select(meta => Precept.Language.Tokens.GetMeta(meta.LeadingToken).Text)
            .OfType<string>()
            .Distinct(System.StringComparer.Ordinal)
            .ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().BeEquivalentTo(expected);
        labels.Should().NotContain(["precept", "field", "state", "event", "from", "rule"]);
        labels.Should().NotContain(unexpectedOutcomes);
    }

    [Fact]
    public async Task Completions_TransitionOutcomeTarget_IncludesDeclaredStatesWithoutWildcardOrTopLevelKeywords()
    {
        var completions = await GetCompletionsAsync("""
            precept BuildingAccessBadgeRequest
            state Approved initial
            state Issued terminal
            event PrintBadge
            from Approved on PrintBadge
                -> transition ¦
            """, " ");
        var labels = completions.Items.Select(item => item.Label).ToArray();

        labels.Should().Contain(["Approved", "Issued"]);
        labels.Should().NotContain("any");
        labels.Should().NotContain(["precept", "field", "state", "event", "from", "rule"]);
    }

    [Fact]
    public async Task Completions_TransitionRowAfterEventTarget_OffersWhenAndArrow()
    {
        var completions = await GetCompletionsAsync("""
            precept BuildingAccessBadgeRequest
            state Approved initial
            state Issued terminal
            event PrintBadge
            from Approved on PrintBadge ¦
            """, " ");
        var labels = completions.Items.Select(item => item.Label).ToArray();

        labels.Should().Contain(["when", "->"]);
        labels.Should().NotContain(["ensure", "modify", "omit", "transition", "reject"]);
    }

    [Fact]
    public async Task Completions_AfterNo_OffersOnlyTransition()
    {
        var completions = await GetCompletionsAsync("""
            precept BuildingAccessBadgeRequest
            state Approved initial
            event PrintBadge
            from Approved on PrintBadge
                -> no ¦
            """, " ");
        var labels = completions.Items.Select(item => item.Label).ToArray();

        labels.Should().BeEquivalentTo(["transition"]);
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
    public async Task Completions_SetActionAfterFieldName_NoTopLevelKeywords()
    {
        // Regression: cursor after '-> set FieldName ' (field name typed, space pressed, no '=' yet)
        // was falling through to SlotContext.TopLevel in TryGetActionChainContext because the
        // Identifier token (field name) was not handled, causing top-level keyword completions to appear.
        var completions = await GetCompletionsAsync("""
            precept Test
            field test as integer
            state offState initial
            state onState
            event toggle
            from onState on toggle
                -> set test ¦
                -> transition offState
            """, " ");
        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().NotContain(["precept", "field", "state", "event", "from", "rule"],
            "top-level construct keywords must not appear after the field name in a set action");
        labels.Should().Contain("= ",
            "the '= ' assignment operator must be offered as the only completion after '-> set FieldName '");
    }

    [Fact]
    public async Task Completions_SetAction_NonBooleanField_ExcludesBooleanLiterals()
    {
        // Regression: 'true' and 'false' appeared in the expression completions when the cursor
        // was after '-> set IntegerField = ' even though boolean literals are never valid for integer fields.
        var completions = await GetCompletionsAsync("""
            precept Test
            field test as integer
            state offState initial
            state onState
            event toggle
            from offState on toggle
                -> set test = ¦
                -> transition onState
            """);
        var labels = completions.Items.Select(item => item.Label).ToArray();

        labels.Should().NotContain("true",
            "boolean literal 'true' must not appear in expression completions for an integer field");
        labels.Should().NotContain("false",
            "boolean literal 'false' must not appear in expression completions for an integer field");
    }

    [Fact]
    public async Task Completions_SetAction_BooleanField_IncludesBooleanLiterals()
    {
        // Counter-test: boolean literals must still appear when the target field IS boolean.
        var completions = await GetCompletionsAsync("""
            precept Test
            field Flag as boolean default false
            state Idle initial
            event Flip
            from Idle on Flip
                -> set Flag = ¦
                -> no transition
            """);
        var labels = completions.Items.Select(item => item.Label).ToArray();

        labels.Should().Contain("true",
            "boolean literal 'true' must appear in expression completions for a boolean field");
        labels.Should().Contain("false",
            "boolean literal 'false' must appear in expression completions for a boolean field");
    }

    [Fact]
    public async Task Completions_EventDeclarationName_OffersEventModifiers()
    {
        // Regression: cursor after 'event EventName ' (space after event name) was falling through
        // to SlotContext.TopLevel because no specialized check handled the event name Identifier,
        // causing top-level keyword completions (precept, field, state, etc.) to appear.
        var completions = await GetCompletionsAsync("""
            precept Test
            field test as integer
            state offState initial
            state onState
            event toggle ¦
            from offState on toggle
                -> transition onState
            """, " ");
        var labels = completions.Items.Select(item => item.Label).ToArray();

        labels.Should().NotContain(["precept", "field", "state", "event", "from", "rule"],
            "top-level construct keywords must not appear after an event name");
        labels.Should().Contain("initial",
            "'initial' modifier must be offered after an event name");
    }

    [Fact]
    public async Task Completions_StateDeclarationName_SuppressesAlreadyAppliedModifiers()
    {
        // 'initial' is already on the state declaration — it must not reappear in the completion list.
        var completions = await GetCompletionsAsync("""
            precept Test
            state offState initial ¦
            state onState
            event toggle
            from offState on toggle -> transition onState
            """, " ");
        var labels = completions.Items.Select(item => item.Label).ToArray();

        labels.Should().NotContain("initial",
            "'initial' must not be offered again when it is already present on the state declaration");
    }

    [Fact]
    public async Task Completions_EventDeclarationName_SuppressesAlreadyAppliedModifiers()
    {
        // 'initial' is already on the event declaration — it must not reappear in the completion list.
        var completions = await GetCompletionsAsync("""
            precept Test
            field test as integer
            state offState initial
            state onState
            event toggle initial ¦
            from offState on toggle -> transition onState
            """, " ");
        var labels = completions.Items.Select(item => item.Label).ToArray();

        labels.Should().NotContain("initial",
            "'initial' must not be offered again when it is already present on the event declaration");
    }

    [Fact]
    public async Task Completions_FieldDeclaration_SuppressesAlreadyAppliedModifiers()
    {
        // 'nonnegative' is already on the field — it must not reappear in the completion list.
        var completions = await GetCompletionsAsync("""
            precept Test
            field Count as integer default 0 nonnegative ¦
            state Idle initial
            event Tick
            from Idle on Tick -> no transition
            """, " ");
        var labels = completions.Items.Select(item => item.Label).ToArray();

        labels.Should().NotContain("nonnegative",
            "'nonnegative' must not be offered again when it is already present on the field declaration");
    }

    [Fact]
    public async Task Completions_Expression_IncludeFieldsArgsFunctionsAndOperators()
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
        labels.Should().Contain(["AssignedCrew", "DispatchRound", "Verified", "CrewName", "Priority", "true", "false", "and", "==", "is set"]);
        labels.Should().NotContain(["precept", "field", "state", "event", "from", "rule"]);
        labels.Where(functionNames.Contains).Should().BeEquivalentTo(functionNames);
    }

    [Fact]
    public async Task Completions_Expression_IncludesControlQuantifierAndMembershipVocabulary()
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

        labels.Should().Contain(["and", "or", "not", "contains", "is", "if", "then", "else", "each", "any", "no"]);
        labels.Should().NotContain(["precept", "field", "state", "event", "from", "rule"]);
    }

    [Fact]
    public async Task Completions_ValuedModifierExpression_UsesExpressionItems()
    {
        var completions = await GetCompletionsAsync("""
            precept Pricing
            field Baseline as integer default 5
            field Total as integer min ¦
            """, " ");
        var labels = completions.Items.Select(item => item.Label).ToArray();

        labels.Should().Contain(["Baseline", "abs", ">=", "and"]);
        labels.Should().NotContain(["precept", "field", "state", "event", "from", "rule"]);
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
    public async Task Completions_DotTrigger_EventName_ShowsEventArgs()
    {
        var completions = await GetCompletionsAsync("""
            precept ElevatorDispatch
            state Pending initial
            event Submit(Floor as number, Reason as string)
            from Pending on Submit when Submit.¦
                -> no transition
            """, ".");

        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().BeEquivalentTo(["Floor", "Reason"]);
    }

    [Fact]
    public async Task Completions_DotTrigger_FieldName_ShowsAccessors()
    {
        var completions = await GetCompletionsAsync("""
            precept UtilityOutageReport
            field CrewQueue as queue of string
            state VerifiedState initial
            event DispatchCrew
            from VerifiedState on DispatchCrew when CrewQueue.¦count > 0
                -> no transition
            """, ".");

        var labels = completions.Items.Select(item => item.Label).ToArray();
        var expected = Precept.Language.Types.GetMeta(Precept.Language.TypeKind.Queue).Accessors
            .Select(accessor => accessor.Name)
            .Distinct(System.StringComparer.Ordinal)
            .ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public async Task Completions_DotTrigger_FieldDefaultTrailingDot_ShowsAccessors()
    {
        var completions = await GetCompletionsAsync("""
            precept LabResult
            field Reading as quantity default '2.5 mg/dL'
            field ReadingUnit as unitofmeasure default Reading.¦
            """, ".");

        var labels = completions.Items.Select(item => item.Label).ToArray();
        var expected = Precept.Language.Types.GetMeta(Precept.Language.TypeKind.Quantity).Accessors
            .Select(accessor => accessor.Name)
            .Distinct(System.StringComparer.Ordinal)
            .ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void DotTrigger_FieldDefaultTrailingDot_CompilationKeepsDotTokenAndFieldSemantic()
    {
        var (compilation, position) = GetCompilationAtCursor("""
            precept LabResult
            field Reading as quantity default '2.5 mg/dL'
            field ReadingUnit as unitofmeasure default Reading.¦
            """);
        var dotSearchPos = new Position(position.Line, position.Character - 1);

        compilation.Tokens.Tokens.Any(token => token.Kind == Precept.Language.TokenKind.Dot && Contains(token.Span, dotSearchPos)).Should().BeTrue();
        compilation.Semantics.FieldsByName.ContainsKey("Reading").Should().BeTrue();
        CursorSemanticResolver.TryGetEventForDotTrigger(compilation, position, out _).Should().BeFalse();
    }

    [Fact]
    public void DotTrigger_FieldDefaultTrailingDot_ResolverReturnsFieldType()
    {
        var (compilation, position) = GetCompilationAtCursor("""
            precept LabResult
            field Reading as quantity default '2.5 mg/dL'
            field ReadingUnit as unitofmeasure default Reading.¦
            """);

        CursorSemanticResolver.TryGetReceiverTypeForDotTrigger(compilation, position, out var receiverType).Should().BeTrue();
        receiverType.Should().Be(Precept.Language.TypeKind.Quantity);
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
    public async Task BinaryPeer_MoneyField_QualifiersThreadedToContext()
    {
        var completions = await GetCompletionsAsync("""
            precept LoanApplication
            field ApprovedAmount as money in 'USD' default '0.00 USD'
            rule ApprovedAmount > '100 ¦' because "Approved amount must be positive"
            state Draft initial terminal
            """, " ");

        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().BeEquivalentTo(["USD"]);
    }

    [Fact]
    public async Task BinaryPeer_MoneyArg_QualifiersThreadedToContext()
    {
        var completions = await GetCompletionsAsync("""
            precept PaymentWorkflow
            field ApprovedAmount as money default '0.00 USD'
            state Draft initial
            state Submitted terminal
            event Submit(Amount as money in 'USD')
            from Draft on Submit when Amount > '100 ¦'
                -> set ApprovedAmount = Amount
                -> transition Submitted
            """, " ");

        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().BeEquivalentTo(["USD"]);
    }

    [Fact]
    public void TypedConstantCursorDiagnostic_DefaultEmptyLiteral_ReportsModifierExpressionSlotAndTypedConstantToken()
    {
        var (compilation, position) = GetCompilationAtCursor("""
            precept TravelReimbursement
            field SubmittedOn as date default '2037-08-09'
            field ApprovedOn as date default '¦'
            state Draft initial terminal
            """);

        var slotPosition = SlotPositionResolver.Resolve(compilation, position);
        var tokenIndex = FindTokenAtOrBeforeCursor(compilation, position);
        var token = compilation.Tokens.Tokens[tokenIndex];

        slotPosition.Should().NotBeNull();
        slotPosition!.Value.SlotKind.Should().Be(ConstructSlotKind.ModifierList);
        slotPosition.Value.Phase.Should().Be(SlotPhase.InExpression);
        token.Kind.Should().Be(Precept.Language.TokenKind.TypedConstant);
        Contains(token.Span, position).Should().BeTrue();
    }

    [Fact]
    public void TypedConstantCursorDiagnostic_ExpressionEmptyLiteral_ReportsExpressionSlotAndTypedConstantToken()
    {
        var (compilation, position) = GetCompilationAtCursor("""
            precept LoanApplication
            field ApprovedAmount as money in 'USD' default '0.00 USD'
            rule ApprovedAmount > '¦' because "Approved amount must be positive"
            state Draft initial terminal
            """);

        var slotPosition = SlotPositionResolver.Resolve(compilation, position);
        var tokenIndex = FindTokenAtOrBeforeCursor(compilation, position);
        var token = compilation.Tokens.Tokens[tokenIndex];

        slotPosition.Should().NotBeNull();
        slotPosition!.Value.SlotKind.Should().Be(ConstructSlotKind.RuleExpression);
        slotPosition.Value.Phase.Should().Be(SlotPhase.InExpression);
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
    public void Completions_TypedConstant_SingleQuoteTrigger_PlainTextItem_AppendsClosingQuote()
    {
        var item = new CompletionItem
        {
            Label = "USD",
            InsertText = "USD",
            InsertTextFormat = InsertTextFormat.PlainText,
            Detail = "currency code",
            Kind = CompletionItemKind.Unit,
        };

        var appended = AppendToInsertText(item, "'");

        appended.InsertText.Should().Be("USD'");
        appended.InsertTextFormat.Should().NotBe(InsertTextFormat.Snippet);
    }

    [Fact]
    public void Completions_TypedConstant_SingleQuoteTrigger_SnippetItem_PreservesFormat()
    {
        var item = new CompletionItem
        {
            Label = "date — YYYY-MM-DD",
            InsertText = "${1:2026}-${2:05}-${3:16}",
            InsertTextFormat = InsertTextFormat.Snippet,
            Detail = "date literal",
            Kind = CompletionItemKind.Snippet,
        };

        var appended = AppendToInsertText(item, "'");

        appended.InsertText.Should().Be("${1:2026}-${2:05}-${3:16}'");
        appended.InsertTextFormat.Should().Be(InsertTextFormat.Snippet);
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
    public async Task TypedConstant_QuantityDefault_QuoteTrigger_ShowsQuantityExamples()
    {
        var completions = await GetCompletionsAsync("""
            precept MeasurementTest
            field q as quantity default ¦
            """, "'");

        var labels = completions.Items.Select(item => item.Label).ToArray();
        var expected = Precept.Language.Types.GetMeta(Precept.Language.TypeKind.Quantity).ContentValidation!.Examples;

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain(expected);
    }

    [Fact]
    public async Task TypedConstant_QuantityInQualifier_QuoteTrigger_ShowsUcumUnitCatalog()
    {
        var completions = await GetCompletionsAsync("""
            precept MeasurementTest
            field q as quantity in ¦
            """, "'");

        var labels = completions.Items.Select(item => item.Label).ToArray();
        var quantityExamples = Precept.Language.Types.GetMeta(Precept.Language.TypeKind.Quantity).ContentValidation!.Examples;
        var ounce = GetItem(completions, "oz", "ounce");

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain(["kg", "m", "oz"], "quantity 'in' qualifier sites must offer real UCUM unit labels");
        labels.Should().NotContain(quantityExamples, "qualifier-site completions must not fall back to quantity literal examples");
        labels.Should().NotContain(["field", "state", "event", "rule"], "DSL keywords must not appear in qualifier-site completions");
        labels.Should().NotContain("[oz_av]", "print symbols should be used as labels when available");
        ounce.InsertText.Should().StartWith("[oz_av]", "quantity unit completions must insert the UCUM code");
        ounce.InsertText.Should().EndWith("'", "quote-trigger completions should preserve the closing quote convenience");
    }

    [Fact]
    public async Task TypedConstant_QuantityOfQualifier_QuoteTrigger_ShowsDimensionCatalog()
    {
        var completions = await GetCompletionsAsync("""
            precept MeasurementTest
            field q as quantity of ¦
            """, "'");

        var labels = completions.Items.Select(item => item.Label).ToArray();
        var quantityExamples = Precept.Language.Types.GetMeta(Precept.Language.TypeKind.Quantity).ContentValidation!.Examples;

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain(["mass", "length", "temperature", "count"], "quantity 'of' qualifier sites must offer real dimension names");
        labels.Should().NotContain(quantityExamples, "qualifier-site completions must not fall back to quantity literal examples");
        labels.Should().NotContain(["kg", "m", "[oz_av]"], "dimension qualifier completions must not route to UCUM unit codes");
        labels.Should().NotContain(["field", "state", "event", "rule"], "DSL keywords must not appear in qualifier-site completions");
    }

    [Fact]
    public async Task TypedConstant_QuantityDefault_CtrlSpace_MatchesQuoteTriggerExamples()
    {
        var quoteTriggerCompletions = await GetCompletionsAsync("""
            precept MeasurementTest
            field q as quantity default ¦
            """, "'");
        var ctrlSpaceCompletions = await GetCompletionsAsync("""
            precept MeasurementTest
            field q as quantity default '¦'
            """, new CompletionContext
            {
                TriggerKind = CompletionTriggerKind.Invoked,
                TriggerCharacter = string.Empty,
            });

        var quoteTriggerLabels = quoteTriggerCompletions.Items.Select(item => item.Label).ToArray();
        var ctrlSpaceLabels = ctrlSpaceCompletions.Items.Select(item => item.Label).ToArray();
        var expected = Precept.Language.Types.GetMeta(Precept.Language.TypeKind.Quantity).ContentValidation!.Examples;

        quoteTriggerCompletions.IsIncomplete.Should().BeFalse();
        ctrlSpaceCompletions.IsIncomplete.Should().BeFalse();
        quoteTriggerLabels.Should().Contain(expected);
        ctrlSpaceLabels.Should().Contain(expected);
        ctrlSpaceLabels.Should().BeEquivalentTo(quoteTriggerLabels);
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

    [Fact]
    public async Task TypedConstant_Timezone_ShowsFullTzdbCatalog()
    {
        var completions = await GetCompletionsAsync("""
            precept ScheduleTest
            field LocalTimezone as timezone default '¦'
            """, new CompletionContext
        {
            TriggerKind = CompletionTriggerKind.Invoked,
            TriggerCharacter = string.Empty,
        });

        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Length.Should().BeGreaterThan(100, "the TZDB catalog should expose far more than the two hardcoded examples");
        labels.Should().Contain(["America/New_York", "Europe/London", "UTC"]);
        labels.Should().NotContain(["field", "state", "event", "rule"], "DSL keywords must not appear inside timezone typed constants");
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

    // ─── Deferred: Quote-Close Logic ─────────────────────────────────────────────

    [Fact]
    public async Task TypedConstant_Boolean_StarterInsertTextIncludesClosingQuote()
    {
        var completions = await GetCompletionsAsync("""
            precept FlagTest
            field Active as boolean default ¦
            state Open initial terminal
            """, "'");

        completions.IsIncomplete.Should().BeFalse();
        completions.Items.Should().NotBeEmpty();
        foreach (var item in completions.Items)
        {
            item.InsertText.Should().EndWith("'", $"starter item '{item.Label}' must include a closing quote");
        }
    }

    [Fact]
    public async Task TypedConstant_Temporal_StarterInsertTextIncludesClosingQuote()
    {
        var completions = await GetCompletionsAsync("""
            precept WorkflowTest
            field Delay as duration default ¦
            """, "'");

        var expected = Precept.Language.Types.GetMeta(Precept.Language.TypeKind.Duration).ContentValidation!.Examples;

        completions.IsIncomplete.Should().BeFalse();
        completions.Items.Should().NotBeEmpty();
        foreach (var item in completions.Items)
        {
            item.InsertText.Should().EndWith("'", $"starter item '{item.Label}' must include a closing quote");
        }

        // Labels (display text) must still be the bare content — no closing quote in Label.
        var labels = completions.Items.Select(i => i.Label).ToArray();
        labels.Should().Contain(expected);
    }

    [Fact]
    public async Task TypedConstant_Temporal_SlotUnit_InsertTextNoClosingQuote()
    {
        // Space trigger inside an existing '30 ¦' → unit slot items must NOT have a closing quote.
        var completions = await GetCompletionsAsync("""
            precept WorkflowTest
            field Delay as duration default '30 ¦'
            """, " ");

        completions.IsIncomplete.Should().BeFalse();
        completions.Items.Should().NotBeEmpty();
        foreach (var item in completions.Items)
        {
            item.InsertText.Should().NotEndWith("'", $"slot unit item '{item.Label}' must NOT append a closing quote");
        }
    }

    // ─── Deferred: Singular vs Plural Temporal Preference ────────────────────────

    [Fact]
    public async Task TypedConstant_Temporal_NumberOne_ShowsSingularUnits()
    {
        // Space after '1 ' → singular forms should be shown (day, hour, week, etc.).
        var completions = await GetCompletionsAsync("""
            precept WorkflowTest
            field Delay as duration default '1 ¦'
            """, " ");

        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain("day");
        labels.Should().Contain("hour");
        labels.Should().Contain("week");
        labels.Should().NotContain("days");
        labels.Should().NotContain("hours");
        labels.Should().NotContain("weeks");
    }

    [Fact]
    public async Task TypedConstant_Temporal_NumberTwo_ShowsPluralUnits()
    {
        // Space after '2 ' → plural forms should be shown (days, hours, weeks, etc.).
        var completions = await GetCompletionsAsync("""
            precept WorkflowTest
            field Delay as duration default '2 ¦'
            """, " ");

        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain("days");
        labels.Should().Contain("hours");
        labels.Should().Contain("weeks");
        labels.Should().NotContain("day");
        labels.Should().NotContain("hour");
        labels.Should().NotContain("week");
    }

    [Fact]
    public async Task TypedConstant_Temporal_CompoundSingular_ShowsSingularUnits()
    {
        // Space after '2 hours + 1 ' → second segment number is 1 → singular units.
        var completions = await GetCompletionsAsync("""
            precept WorkflowTest
            field Delay as duration default '2 hours + 1 ¦'
            """, " ");

        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain("day");
        labels.Should().Contain("hour");
        labels.Should().NotContain("days");
        labels.Should().NotContain("hours");
    }

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

    // ─── B2 — Quantity unit slot: full UCUM tier-1 catalog ───────────────────────

    [Fact]
    public async Task TypedConstant_SpaceTrigger_Quantity_AfterAmount_ShowsFullUnitCatalog()
    {
        // B2 fix: space trigger inside '5 ¦' must return the full UCUM tier-1 catalog, not 3 hardcoded examples.
        var completions = await GetCompletionsAsync("""
            precept MeasurementTest
            field Weight as quantity default '5 ¦'
            """, " ");

        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Length.Should().BeGreaterThan(3, "the full UCUM tier-1 catalog has many more than 3 entries");
        labels.Should().Contain(["kg", "m", "g", "L", "m/s", "oz"], "these are representative tier-1 unit labels");
        labels.Should().NotContain(["field", "state", "event", "rule"], "DSL keywords must not appear in unit slot completions");
    }

    [Fact]
    public async Task TypedConstant_SpaceTrigger_Quantity_DimensionQualifier_FiltersToMatchingUnits()
    {
        // B2 fix: when a 'of <dimension>' qualifier is declared, only units of that dimension are returned.
        var completions = await GetCompletionsAsync("""
            precept MeasurementTest
            field Weight as quantity of 'mass' default '5 ¦'
            """, " ");

        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain("kg", "kg is a mass unit");
        labels.Should().Contain("g", "g is a mass unit");
        labels.Should().NotContain("m", "m is a length unit, not mass");
        labels.Should().NotContain("L", "L is a volume unit, not mass");
        labels.Should().NotContain(["field", "state", "event", "rule"]);
    }

    [Fact]
    public async Task TypedConstant_SpaceTrigger_Quantity_UnitQualifier_ShowsOnlyPinnedUnit()
    {
        // B2: 'in <unit>' qualifier pins the completion list to exactly that one unit code.
        var completions = await GetCompletionsAsync("""
            precept MeasurementTest
            field Weight as quantity in '[lb_av]' default '5 ¦'
            """, " ");

        var item = GetItem(completions, "lb", "pound");

        completions.IsIncomplete.Should().BeFalse();
        completions.Items.Should().ContainSingle();
        item.InsertText.Should().Be("[lb_av]", "unit qualifier completions must insert the UCUM code");
    }

    [Fact]
    public async Task TypedConstant_SpaceTrigger_Quantity_ItemsUsePrintSymbolsAndNameDetails()
    {
        var completions = await GetCompletionsAsync("""
            precept MeasurementTest
            field Weight as quantity default '5 ¦'
            """, " ");

        var ounce = GetItem(completions, "oz", "ounce");
        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain("kg");
        labels.Should().Contain("oz");
        labels.Should().NotContain("[oz_av]", "print symbols should be used as labels when available");
        ounce.InsertText.Should().Be("[oz_av]", "quantity unit completions must insert valid UCUM codes");
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

    // ─── Slice 3 — Completions inside typed constant holes ───────────────────────

    [Fact]
    public async Task HoleCompletion_Quantity_MagnitudeHole_ShowsNumericFieldsAndArgsOnly()
    {
        // '{Am¦ount} kg' → QuantityForm M2: H[magnitude] " kg" → Magnitude slot → Integer/Decimal/Number only.
        // Cursor is inside the identifier, past the TypedConstantStart token's span.
        var completions = await GetCompletionsAsync("""
            precept QuantityTest
            field Weight as quantity default '0 kg'
            field Amount as decimal default 0.0
            field Name as text
            state Draft initial terminal
            event Receive(Qty as integer)
            from Draft on Receive
                -> set Weight = '{Am¦ount} kg'
            """, new CompletionContext
            {
                TriggerKind = CompletionTriggerKind.Invoked,
                TriggerCharacter = string.Empty,
            });

        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain("Amount", "decimal field is in the Magnitude slot type set");
        labels.Should().Contain("Qty", "integer event arg is in the Magnitude slot type set");
        labels.Should().NotContain("Name", "text field is not a numeric type");
    }

    [Fact]
    public async Task HoleCompletion_Money_CurrencyHole_ShowsCurrencyFieldsOnly()
    {
        // '100 {Co¦de}' → MoneyForm M3: "100 " H[currency] → Currency slot → Currency fields only.
        var completions = await GetCompletionsAsync("""
            precept MoneyTest
            field Cost as money default '0 USD'
            field Code as currency default 'USD'
            field Amount as decimal default 0.0
            state Draft initial terminal
            event Pay(CurrencyArg as currency)
            from Draft on Pay
                -> set Cost = '100 {Co¦de}'
            """, new CompletionContext
            {
                TriggerKind = CompletionTriggerKind.Invoked,
                TriggerCharacter = string.Empty,
            });

        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain("Code", "currency field is in the Currency slot type set");
        labels.Should().Contain("CurrencyArg", "currency event arg is in the Currency slot type set");
        labels.Should().NotContain("Amount", "decimal field is not a currency type");
    }

    [Fact]
    public async Task HoleCompletion_Quantity_UnitHole_ShowsUnitOfMeasureFieldsOnly()
    {
        // '5 {BaseUn¦it}' → QuantityForm M3: "5 " H[unit] → Unit slot → UnitOfMeasure fields only.
        var completions = await GetCompletionsAsync("""
            precept QuantityTest
            field Weight as quantity default '0 kg'
            field BaseUnit as unitofmeasure default 'kg'
            field Amount as decimal default 0.0
            state Draft initial terminal
            event Ship(UnitArg as unitofmeasure)
            from Draft on Ship
                -> set Weight = '5 {BaseUn¦it}'
            """, new CompletionContext
            {
                TriggerKind = CompletionTriggerKind.Invoked,
                TriggerCharacter = string.Empty,
            });

        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain("BaseUnit", "unit-of-measure field is in the Unit slot type set");
        labels.Should().Contain("UnitArg", "unit-of-measure event arg is in the Unit slot type set");
        labels.Should().NotContain("Amount", "decimal field is not a unit-of-measure type");
    }

    [Fact]
    public async Task HoleCompletion_OutsideHole_NormalExpressionCompletions()
    {
        // Cursor outside any hole: normal expression completions must be returned (regression guard).
        var completions = await GetCompletionsAsync("""
            precept RegressionTest
            field Amount as decimal default 0.0
            field Code as currency default 'USD'
            state Draft initial terminal
            event Submit
            from Draft on Submit when ¦Amount > 0
                -> no transition
            """);

        var labels = completions.Items.Select(item => item.Label).ToArray();

        completions.IsIncomplete.Should().BeFalse();
        labels.Should().Contain("Amount", "field name must appear in expression completions");
        labels.Should().Contain("Code", "field name must appear in expression completions");
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
        store.GetOrAdd(uri).Update(Precept.Compiler.Compile(source), source);

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

    private static CompletionItem AppendToInsertText(CompletionItem item, string suffix)
    {
        var method = typeof(CompletionHandler).GetMethod("AppendToInsertText", BindingFlags.NonPublic | BindingFlags.Static);
        method.Should().NotBeNull();
        var result = method!.Invoke(null, [item, suffix]);
        result.Should().BeOfType<CompletionItem>();
        return (CompletionItem)result!;
    }

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
