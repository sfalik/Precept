using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using OmniSharp.Extensions.LanguageServer.Protocol;
using OmniSharp.Extensions.LanguageServer.Protocol.Document;
using OmniSharp.Extensions.LanguageServer.Protocol.Models;
using Precept.LanguageServer.Handlers;
using Xunit;

namespace Precept.LanguageServer.Tests;

public class HoverHandlerTests
{
    private const string Source = """
precept LoanApplication
field Amount as number
state Draft initial
""";

    private const string SourceWithEventArgs = """
    precept LoanApplication
    field Amount as number
    state Draft initial
    state Approved terminal
    event Approve(Note as string optional notempty)
    on Approve ensure Approve.Note is set because "note required"
    from Draft on Approve -> transition Approved
    """;

    private const string RichHoverSource = """
    precept HoverSurface
    field Tags as set of string
    field StartDate as date <- '2026-01-15'
    field Notes as string <- "vip"
    state Draft initial
    state Done terminal
    rule Notes.length >= max(0, 0) because "valid"
    event AddTag
    from Draft on AddTag
        -> add Tags "vip"
        -> transition Done
    """;

    private const string HoverV3Source = """
    precept HoverSurface
    field Price as money in 'USD'
    field Quantity as integer
    field CatalogCode as string optional
    field ReorderRatio as number
    field AverageQuantity as number <- Quantity / 2
    state Draft initial
    state Listed
    state Hidden
    state Archived terminal
    event Publish(NewPrice as money in 'USD')
    event Hide
    event Reopen
    event Archive
    event Cancel(Reason as string)
    rule Price >= '0 USD' because "price stays non-negative"
    in Listed ensure Price > '0 USD' because "listed price must stay positive"
    to Archived ensure CatalogCode is not set because "archived items clear the code"
    from Listed ensure Quantity > 0 because "leaving listed needs stock"
    on Publish ensure Publish.NewPrice > '0 USD' because "published price must be positive"
    in Draft modify CatalogCode, Quantity, Price editable
    in Listed modify CatalogCode, Quantity, Price editable
    in Hidden omit CatalogCode
    from Draft on Publish
        -> set Price = Publish.NewPrice
        -> transition Listed
    from Listed on Publish
        -> set ReorderRatio = Quantity / Quantity
        -> transition Listed
    from Listed on Hide -> transition Hidden
    from Hidden on Reopen -> transition Draft
    from Listed on Archive -> transition Archived
    from Draft on Cancel -> reject "draft items cannot be cancelled"
    """;

    [Fact]
    public void Hover_OnKeyword_ReturnsMarkdownContent()
    {
        var compilation = Precept.Compiler.Compile(Source);

        var hover = HoverHandler.CreateHover(compilation, new Position(1, 1));

        hover.Should().NotBeNull();
        hover!.Contents.HasMarkupContent.Should().BeTrue();

        var markup = hover.Contents.MarkupContent;
        markup.Should().NotBeNull();
        markup!.Kind.Should().Be(MarkupKind.Markdown);
        markup.Value.Should().Contain("**field**");
        markup.Value.Should().Contain("Field declaration");
    }

    [Fact]
    public void Hover_OnDeclaredField_ReturnsIdentifierDoc()
    {
        var compilation = Precept.Compiler.Compile(Source);

        var hover = HoverHandler.CreateHover(compilation, new Position(1, 7));

        hover.Should().NotBeNull();
        hover!.Contents.HasMarkupContent.Should().BeTrue();

        var markup = hover.Contents.MarkupContent;
        markup.Should().NotBeNull();
        markup!.Value.Should().Contain("field `Amount`");
        markup.Value.Should().Contain("number");
    }

    [Fact]
    public void Hover_OnWhitespace_ReturnsNull()
    {
        var compilation = Precept.Compiler.Compile(Source);

        var hover = HoverHandler.CreateHover(compilation, new Position(1, 5));

        hover.Should().BeNull();
    }

    [Fact]
    public void Hover_OnNewLineToken_ReturnsNull()
    {
        var compilation = Precept.Compiler.Compile(Source);
        var newLineSpan = compilation.Tokens.Tokens.First(token => token.Kind == Precept.Language.TokenKind.NewLine).Span;

        var hover = HoverHandler.CreateHover(compilation, new Position(newLineSpan.StartLine - 1, newLineSpan.StartColumn - 1));

        hover.Should().BeNull();
    }

    [Fact]
    public void Hover_OnEndOfSourceToken_ReturnsNull()
    {
        var compilation = Precept.Compiler.Compile(Source);
        var endOfSourceSpan = compilation.Tokens.Tokens.First(token => token.Kind == Precept.Language.TokenKind.EndOfSource).Span;

        var hover = HoverHandler.CreateHover(compilation, new Position(endOfSourceSpan.StartLine - 1, endOfSourceSpan.StartColumn - 1));

        hover.Should().BeNull();
    }

    [Fact]
    public void Hover_OnEventArgumentDeclaration_ReturnsIdentifierDoc()
    {
        var compilation = Precept.Compiler.Compile(SourceWithEventArgs);
        var noteToken = compilation.Tokens.Tokens.Single(token =>
            token.Kind == Precept.Language.TokenKind.Identifier
            && token.Text == "Note"
            && token.Span.StartLine == 5);

        var hover = HoverHandler.CreateHover(
            compilation,
            new Position(noteToken.Span.StartLine - 1, noteToken.Span.StartColumn - 1));

        hover.Should().NotBeNull();
        hover!.Contents.MarkupContent!.Value.Should().Contain("argument `Note`");
        hover.Contents.MarkupContent.Value.Should().Contain("Event: `Approve`");
    }

    [Fact]
    public void Hover_OnDeclaredState_ReturnsIdentifierDoc()
    {
        var compilation = Precept.Compiler.Compile(Source);
        // "state Draft initial" is line 3 (1-based) → Position(2, 6) = "Draft" (0-based col 6 = 1-based col 7)
        var hover = HoverHandler.CreateHover(compilation, new Position(2, 6));

        hover.Should().NotBeNull();
        hover!.Contents.MarkupContent!.Value.Should().Contain("state `Draft`");
    }

    [Fact]
    public async Task Hover_NoDocument_ReturnsNull()
    {
        var store = new DocumentStore();
        var handler = new HoverHandler(store);
        var request = new HoverParams
        {
            TextDocument = new TextDocumentIdentifier(DocumentUri.FromFileSystemPath(@"C:\hover-test.precept")),
            Position = new Position(0, 0),
        };

        var hover = await handler.Handle(request, CancellationToken.None);

        hover.Should().BeNull();
    }

    [Fact]
    public void Hover_OnSetInTypePosition_UsesTypeHover()
    {
        var compilation = Precept.Compiler.Compile("""
            precept LoanApplication
            field Tags as set of string
            state Draft initial
            """);
        var setToken = compilation.Tokens.Tokens.Single(t => t.Kind == Precept.Language.TokenKind.Set);

        var hover = HoverHandler.CreateHover(compilation, new Position(setToken.Span.StartLine - 1, setToken.Span.StartColumn - 1));

        hover.Should().NotBeNull();
        hover!.Contents.HasMarkupContent.Should().BeTrue();
        var markup = hover.Contents.MarkupContent!;
        markup.Value.Should().Contain("set");
        markup.Value.Should().Contain("unordered collection", because: "set in type position should show type description");
        markup.Value.Should().NotContain("Field assignment", because: "action description must not appear in type hover");
    }

    [Fact]
    public void Hover_OnTypedConstant_ShowsDeclaredTypeAndFormat()
    {
        var compilation = Precept.Compiler.Compile(RichHoverSource);
        var typedConstant = compilation.Tokens.Tokens.Single(token =>
            token.Kind == Precept.Language.TokenKind.TypedConstant
            && token.Span.StartLine == 3);

        var hover = HoverHandler.CreateHover(compilation, new Position(typedConstant.Span.StartLine - 1, typedConstant.Span.StartColumn - 1));

        hover.Should().NotBeNull();
        hover!.Contents.MarkupContent!.Value.Should().Contain("date typed constant");
        hover.Contents.MarkupContent.Value.Should().Contain("ISO 8601 date (YYYY-MM-DD)");
    }

    [Fact]
    public void Hover_OnQuantityTypedConstant_ShowsResolvedUnitMetadata()
    {
        var compilation = Precept.Compiler.Compile("""
            precept HoverUnits
            field Weight as quantity <- '5 [lb_av]'
            """);
        var typedConstant = compilation.Tokens.Tokens.Single(token => token.Kind == Precept.Language.TokenKind.TypedConstant);

        var hover = HoverHandler.CreateHover(compilation, new Position(typedConstant.Span.StartLine - 1, typedConstant.Span.StartColumn - 1));

        hover.Should().NotBeNull();
        hover!.Contents.MarkupContent!.Value.Should().Contain("quantity typed constant");
        hover.Contents.MarkupContent.Value.Should().Contain("Unit: `[lb_av]` (lb) — pound");
    }

    [Fact]
    public void Hover_OnFunctionCall_ShowsSignatureAndDescription()
    {
        var compilation = Precept.Compiler.Compile(RichHoverSource);
        var maxToken = compilation.Tokens.Tokens.Single(token => token.Text == "max");

        var hover = HoverHandler.CreateHover(compilation, new Position(maxToken.Span.StartLine - 1, maxToken.Span.StartColumn - 1));

        hover.Should().NotBeNull();
        hover!.Contents.MarkupContent!.Value.Should().Contain("function `max`");
        hover.Contents.MarkupContent.Value.Should().Contain("max(value as integer, value as integer) -> integer");
        hover.Contents.MarkupContent.Value.Should().Contain("Returns the larger of two values");
    }

    [Fact]
    public void Hover_OnOperator_UsesOperatorHoverDescription()
    {
        var compilation = Precept.Compiler.Compile(RichHoverSource);
        var operatorToken = compilation.Tokens.Tokens.Single(token => token.Kind == Precept.Language.TokenKind.GreaterThanOrEqual);

        var hover = HoverHandler.CreateHover(compilation, new Position(operatorToken.Span.StartLine - 1, operatorToken.Span.StartColumn - 1));

        hover.Should().NotBeNull();
        hover!.Contents.MarkupContent!.Value.Should().Contain(">=");
        hover.Contents.MarkupContent.Value.Should().Contain("Greater-than-or-equal comparison. Requires orderable types. Cannot be chained.");
    }

    [Fact]
    public void Hover_OnCollectionType_UsesTypeHoverDescription()
    {
        var compilation = Precept.Compiler.Compile(RichHoverSource);
        var setToken = compilation.Tokens.Tokens.Single(token =>
            token.Kind == Precept.Language.TokenKind.Set
            && token.Span.StartLine == 2);

        var hover = HoverHandler.CreateHover(compilation, new Position(setToken.Span.StartLine - 1, setToken.Span.StartColumn - 1));

        hover.Should().NotBeNull();
        hover!.Contents.MarkupContent!.Value.Should().Contain("set");
        hover.Contents.MarkupContent.Value.Should().Contain("unordered collection of unique elements");
    }

    [Fact]
    public void Hover_OnActionVerb_UsesActionHoverDescription()
    {
        var compilation = Precept.Compiler.Compile(RichHoverSource);
        var addToken = compilation.Tokens.Tokens.Single(token => token.Kind == Precept.Language.TokenKind.Add);

        var hover = HoverHandler.CreateHover(compilation, new Position(addToken.Span.StartLine - 1, addToken.Span.StartColumn - 1));

        hover.Should().NotBeNull();
        hover!.Contents.MarkupContent!.Value.Should().Contain("add");
        hover.Contents.MarkupContent.Value.Should().Contain("Adds an element to a set or bag field.");
    }

    [Fact]
    public void Hover_OnAccessor_UsesAccessorDescription()
    {
        var compilation = Precept.Compiler.Compile(RichHoverSource);
        var accessorToken = compilation.Tokens.Tokens.Single(token => token.Text == "length");

        var hover = HoverHandler.CreateHover(compilation, new Position(accessorToken.Span.StartLine - 1, accessorToken.Span.StartColumn - 1));

        hover.Should().NotBeNull();
        hover!.Contents.MarkupContent!.Value.Should().Contain("string.length");
        hover.Contents.MarkupContent.Value.Should().Contain("Character count");
        hover.Contents.MarkupContent.Value.Should().Contain("Returns: `integer`");
    }

    [Fact]
    public void Hover_OnStoredField_ShowsWriteMapGovernanceAndResolvedQualifiers()
    {
        var markup = GetHoverMarkdown(HoverV3Source, "Price as money");

        markup.Should().Contain("**field `Price`**");
        markup.Should().Contain("Type: `money` · not nullable · `in USD`");
        markup.Should().Contain("Declared qualifier: `in USD`");
        markup.Should().Contain("Resolved qualifier: `'USD'`");
        markup.Should().Contain("Qualifier source: Currency declared explicitly on this type");
        markup.Should().Contain("Writable:");
        markup.Should().Contain("`Draft`");
        markup.Should().Contain("`Listed`");
        markup.Should().Contain("Governed by:");
        markup.Should().Contain("rule");
        markup.Should().Contain("ensure");
    }

    [Fact]
    public void Hover_OnComputedField_ShowsExpressionAndSuppressesWriteMap()
    {
        var markup = GetHoverMarkdown(HoverV3Source, "AverageQuantity as number");

        markup.Should().Contain("**computed field `AverageQuantity`**");
        markup.Should().Contain("Computed from:");
        markup.Should().Contain("Quantity / 2");
        markup.Should().NotContain("Writable:");
    }

    [Fact]
    public void Hover_OnRule_ShowsScopeAndReferencedFields()
    {
        var markup = GetHoverMarkdown(HoverV3Source, "rule Price >=");

        markup.Should().Contain("**rule**");
        markup.Should().Contain("> price stays non-negative");
        markup.Should().Contain("Scope: global — enforced after every mutation");
        markup.Should().Contain("Referenced fields: `Price`");
    }

    [Fact]
    public void Hover_OnGuardedRule_ShowsGuardCondition()
    {
        const string source = """
            precept GuardedRuleHover
            field ApprovedAmount as money in 'USD'
            field CoverageLimit as money in 'USD'
            field InForce as boolean
            state Draft initial
            state Done terminal
            event Submit
            rule ApprovedAmount <= CoverageLimit when InForce because "guarded cap"
            from Draft on Submit -> transition Done
            """;

        var markup = GetHoverMarkdown(source, "rule ApprovedAmount <=");

        markup.Should().Contain("**rule** `when InForce: ApprovedAmount <= CoverageLimit`");
        markup.Should().Contain("Scope: global when `InForce`");
    }

    [Fact]
    public void Hover_OnState_ShowsReachabilityModifiersAndEnsures()
    {
        var markup = GetHoverMarkdown(HoverV3Source, "Archived terminal");

        markup.Should().Contain("**state `Archived`**");
        markup.Should().Contain("Modifiers: `terminal`");
        markup.Should().Contain("Incoming:");
        markup.Should().Contain("`Archive`");
        markup.Should().Contain("Writable here:");
        markup.Should().Contain("active ensures: 1");
    }

    [Fact]
    public void Hover_OnEvent_ShowsSignatureAndEligibleStates()
    {
        var markup = GetHoverMarkdown(HoverV3Source, "Publish(NewPrice as money");

        markup.Should().Contain("**event `Publish(NewPrice as money in USD)`**");
        markup.Should().Contain("Can fire from:");
        markup.Should().Contain("`Draft`");
        markup.Should().Contain("`Listed`");
        markup.Should().Contain("Arg: `NewPrice` is `money` · not nullable · `in USD`");
    }

    [Fact]
    public void Hover_OnInitialEvent_ShowsConstructorWording()
    {
        const string source = """
            precept InitialEventHover
            event Create(Name as string) initial
            """;

        var markup = GetHoverMarkdown(source, "Create(Name as string)");

        markup.Should().Contain("**event `initial Create(Name as string)`**");
        markup.Should().Contain("constructor event (invoked via `CreateInstance`, not `Fire`)");
    }

    [Fact]
    public void Hover_OnTransitionRow_ShowsGapSummary()
    {
        var markup = GetHoverMarkdown(HoverV3Source, "from Listed on Publish");

        markup.Should().Contain("**transition**");
        markup.Should().Contain("⚠️ Gap · 1 unresolved proof obligation");
        markup.Should().Contain("set ReorderRatio");
        markup.Should().Contain("Graph:");
        markup.Should().Contain("Gap: 1 unresolved obligation");
        markup.Should().NotContain("Proof gap:");
    }

    [Theory]
    [InlineData("in Listed ensure", "Scope: residency (`in Listed`)", "Referenced fields: `Price`")]
    [InlineData("to Archived ensure", "Scope: entry gate (`to Archived`)", "Referenced fields: `CatalogCode`")]
    [InlineData("from Listed ensure", "Scope: exit gate (`from Listed`)", "Referenced fields: `Quantity`")]
    [InlineData("on Publish ensure", "Scope: event args (`on Publish`)", "Referenced args: `NewPrice`")]
    public void Hover_OnEnsure_ShowsAnchorSpecificScope(string needle, string expectedScope, string expectedReference)
    {
        var markup = GetHoverMarkdown(HoverV3Source, needle);

        markup.Should().Contain("**ensure**");
        markup.Should().Contain("> ");
        markup.Should().Contain(expectedScope);
        markup.Should().Contain(expectedReference);
    }

    [Fact]
    public void Hover_OnEditableAccess_ShowsWriteSetAndPeerStates()
    {
        var markup = GetHoverMarkdown(HoverV3Source, "in Draft modify");

        markup.Should().Contain("**access**");
        markup.Should().Contain("Editable here:");
        markup.Should().Contain("`CatalogCode`");
        markup.Should().Contain("`Quantity`");
        markup.Should().Contain("`Price`");
        markup.Should().Contain("Same write set in `Listed`");
        markup.Should().Contain("locked in");
    }

    [Fact]
    public void Hover_OnOmitDeclaration_ShowsRestorationStates()
    {
        var markup = GetHoverMarkdown(HoverV3Source, "in Hidden omit");

        markup.Should().Contain("**omit**");
        markup.Should().Contain("`CatalogCode` does not exist in this state");
        markup.Should().Contain("Restored on transition to: `Draft`");
    }

    [Fact]
    public void Hover_OnRejectRow_ShowsReasonAndOutcome()
    {
        var markup = GetHoverMarkdown(HoverV3Source, "from Draft on Cancel");

        markup.Should().Contain("**reject**");
        markup.Should().Contain("> draft items cannot be cancelled");
        markup.Should().Contain("Result: state unchanged · no field mutations commit");
    }

    [Fact]
    public void Hover_OnRequiredState_ShowsRequiredReachability()
    {
        const string source = """
            precept RequiredStateHover
            state Draft initial
            state Approved required
            state Done terminal
            event Approve
            event Finish
            from Draft on Approve -> transition Approved
            from Approved on Finish -> transition Done
            """;

        var markup = GetHoverMarkdown(source, "Approved required");

        markup.Should().Contain("**state `Approved`** · `required`");
        markup.Should().Contain("reachable; every initial→terminal path visits here");
    }

    [Fact]
    public void Hover_OnProvenTransition_ShowsProvenBadge()
    {
        const string source = """
            precept VerifiedTransitionHover
            state Draft initial
            state Done terminal
            event Submit
            from Draft on Submit -> transition Done
            """;

        var markup = GetHoverMarkdown(source, "from Draft on Submit");

        markup.Should().Contain("✅ Proven · source reachable · target `Done` reachable");
        markup.Should().NotContain("Proof gap:");
    }

    [Fact]
    public void Hover_OnQualifiedField_ShowsUnresolvedProofUseSummary()
    {
        const string source = """
            precept FieldProofHover
            field A as money in 'USD'
            field B as money in 'USD'
            field C as money in 'EUR'
            field Result as money <- (A - B) + C
            """;

        var markup = GetHoverMarkdown(source, "A as money");

        markup.Should().Contain("Status: Proof contract active · 1 unresolved use");
        markup.Should().Contain("Open proof issues: computed field `Result`");
    }

    [Fact]
    public void Hover_OnProofDiagnosticSpan_WinsOverOperatorHover()
    {
        const string source = """
            precept ProofDiagnosticHover
            field A as money in 'USD'
            field B as money in 'USD'
            field C as money in 'EUR'
            field Result as money <- (A - B) + C
            """;

        var markup = GetHoverMarkdown(source, "+");

        markup.Should().Contain("⚠️ `PRE0114` · Can't confirm currencies match");
        markup.Should().Contain("🔬 `Result` · `(A - B) + C`");
        markup.Should().Contain("Left `A - B` carries `'USD'` · right `C` carries `'EUR'`");
    }

    [Fact]
    public void Hover_OnProofBearingExpression_ShowsProvenQualifierDetails()
    {
        const string source = """
            precept GrossProfitHover
            field CatalogCurrency as currency default 'USD'
            field TotalRevenue as money in '{CatalogCurrency}' default '10.00 {CatalogCurrency}'
            field TotalReturns as money in '{CatalogCurrency}' default '1.00 {CatalogCurrency}'
            field TotalCostOfGoods as money in '{CatalogCurrency}' default '2.00 {CatalogCurrency}'
            field GrossProfit as money in '{CatalogCurrency}' <- (TotalRevenue - TotalReturns) - TotalCostOfGoods
            """;

        var markup = GetHoverMarkdown(source, "TotalRevenue - TotalReturns", offset: 13);

        markup.Should().Contain("✅ Proven · result keeps `'{CatalogCurrency}'`");
        markup.Should().Contain("🔬 `TotalRevenue - TotalReturns`");
        markup.Should().Contain("Left/Right: `'{CatalogCurrency}'` · Result: `'{CatalogCurrency}'`");
        markup.Should().NotContain("Proved");
    }

    [Fact]
    public void Hover_OnQualifierExpression_ShowsCompactQualifierCard()
    {
        var markup = GetHoverMarkdown(HoverV3Source, "money in 'USD'", offset: 9);

        markup.Should().Contain("⚖️ Currency · `'USD'`");
        markup.Should().Contain("Declared explicitly on this type");
        markup.Should().Contain("Mixed currencies aren't allowed");
    }

    [Fact]
    public void Hover_OnInterpolatedDimensionQualifier_ShowsResolvedSource()
    {
        const string source = """
            precept QualifierHover
            field StockingUnit as unitofmeasure
            field QuantityOnHand as quantity of '{StockingUnit.dimension}'
            state Draft initial
            """;

        var markup = GetHoverMarkdown(source, "'{StockingUnit.dimension}'", offset: 2);

        markup.Should().Contain("⚖️ Physical dimension · `'{StockingUnit.dimension}'`");
        markup.Should().Contain("Resolves from field `StockingUnit`");
        markup.Should().Contain("Mixed physical dimensions aren't allowed");
    }

    [Fact]
    public void Hover_OnUnitQualifierExpression_ShowsUnitAxis()
    {
        const string source = """
            precept QualifierHover
            field StockingUnit as unitofmeasure
            field QuantityOnHand as quantity in '{StockingUnit}'
            state Draft initial
            """;

        var markup = GetHoverMarkdown(source, "'{StockingUnit}'", offset: 2);

        markup.Should().Contain("⚖️ Unit of measure · `'{StockingUnit}'`");
        markup.Should().Contain("Resolves from field `StockingUnit`");
    }

    [Fact]
    public void Hover_OnExchangeRateField_ExplainsMissingQualifierAnnotations()
    {
        const string source = """
            precept ExchangeRateHover
            field Rate as exchangerate
            state Draft initial
            """;

        var markup = GetHoverMarkdown(source, "Rate as exchangerate");

        markup.Should().Contain("Resolved qualifiers: Source currency `<unresolved>` · Target currency `<unresolved>`");
        markup.Should().Contain("Reason: exchange rate has no `in ... to ...` annotation");
    }

    [Fact]
    public void Hover_OnQualifierExpression_ShowsResolvedValueSource()
    {
        const string source = """
            precept CurrencyQualifierHover
            field CatalogCurrency as currency default 'USD'
            field TotalRevenue as money in '{CatalogCurrency}'
            state Draft initial
            """;

        var markup = GetHoverMarkdown(source, "'{CatalogCurrency}'", offset: 2);

        markup.Should().Contain("⚖️ Currency · `'{CatalogCurrency}'`");
        markup.Should().Contain("Resolves from field `CatalogCurrency`");
        markup.Should().Contain("Mixed currencies aren't allowed");
    }

    private static string GetHoverMarkdown(string source, string needle, int offset = 0, int occurrence = 1)
    {
        var compilation = Precept.Compiler.Compile(source);
        var hover = HoverHandler.CreateHover(compilation, PositionOf(source, needle, offset, occurrence));

        hover.Should().NotBeNull();
        hover!.Contents.MarkupContent.Should().NotBeNull();
        return hover.Contents.MarkupContent!.Value;
    }

    private static Position PositionOf(string source, string needle, int offset = 0, int occurrence = 1)
    {
        var index = -1;
        var searchStart = 0;
        for (var i = 0; i < occurrence; i++)
        {
            index = source.IndexOf(needle, searchStart, StringComparison.Ordinal);
            index.Should().BeGreaterThanOrEqualTo(0, $"expected to find '{needle}' in test source");
            searchStart = index + needle.Length;
        }

        index += offset;
        var line = 0;
        var character = 0;
        for (var i = 0; i < index; i++)
        {
            if (source[i] == '\n')
            {
                line++;
                character = 0;
                continue;
            }

            if (source[i] != '\r')
            {
                character++;
            }
        }

        return new Position(line, character);
    }
}
