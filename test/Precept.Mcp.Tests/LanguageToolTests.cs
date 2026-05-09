using System.Text.Json;
using FluentAssertions;
using Precept.Language;
using Precept.Mcp.Tools;
using Xunit;

namespace Precept.Mcp.Tests;

public class LanguageToolTests
{
    [Fact]
    public void Language_SerializesWithExpectedTopLevelSchema()
    {
        var result = LanguageTool.Language();
        var json = JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        using var document = JsonDocument.Parse(json);

        document.RootElement.EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(
            "tokens",
            "types",
            "modifiers",
            "actions",
            "constructs",
            "constraints",
            "operators",
            "functions",
            "diagnostics",
            "firePipeline");

        document.RootElement.GetProperty("modifiers").EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(
            "field",
            "state",
            "event",
            "access",
            "anchor");
    }

    [Fact]
    public void Language_TokensMirrorTokenCatalogInDeclarationOrderAndRepresentativeFields()
    {
        var result = LanguageTool.Language();

        result.Tokens.Select(token => token.Kind).Should().Equal(Tokens.All.Select(token => token.Kind.ToString()));

        var becauseMeta = Tokens.GetMeta(TokenKind.Because);
        var because = result.Tokens.Should().ContainSingle(token => token.Kind == TokenKind.Because.ToString()).Subject;

        because.Kind.Should().Be(becauseMeta.Kind.ToString());
        because.Text.Should().Be(becauseMeta.Text);
        because.Categories.Should().Equal(becauseMeta.Categories.Select(category => category.ToString()));
        because.Description.Should().Be(becauseMeta.Description);
        because.TextMateScope.Should().Be(becauseMeta.TextMateScope);
        because.SemanticTokenType.Should().Be(becauseMeta.SemanticTokenType);
        because.ValidAfter.Should().Equal((becauseMeta.ValidAfter ?? []).Select(previous => previous.ToString()));
        because.IsAccessModeAdjective.Should().Be(becauseMeta.IsAccessModeAdjective);
        because.IsValidAsMemberName.Should().Be(becauseMeta.IsValidAsMemberName);
        because.IsMessagePosition.Should().Be(becauseMeta.IsMessagePosition);
    }

    [Fact]
    public void Language_TypesMirrorTypeCatalogInDeclarationOrderAndRepresentativeFields()
    {
        var result = LanguageTool.Language();

        result.Types.Select(type => type.Kind).Should().Equal(Types.All.Select(type => type.Kind.ToString()));

        var moneyMeta = Types.GetMeta(TypeKind.Money);
        var money = result.Types.Should().ContainSingle(type => type.Kind == TypeKind.Money.ToString()).Subject;
        money.Keyword.Should().Be(moneyMeta.Token?.Text);
        money.Description.Should().Be(moneyMeta.Description);
        money.Category.Should().Be(moneyMeta.Category.ToString());
        money.DisplayName.Should().Be(moneyMeta.DisplayName);
        money.Traits.Should().Equal(Enum.GetValues<TypeTrait>().Where(flag => Convert.ToInt64(flag) != 0 && moneyMeta.Traits.HasFlag(flag)).Select(flag => flag.ToString()));
        money.WidensTo.Should().Equal(moneyMeta.WidensTo.Select(kind => kind.ToString()));
        money.ImpliedModifiers.Should().Equal(moneyMeta.ImpliedModifiers.Select(RenderModifier));
        money.QualifierShape.Should().NotBeNull();
        money.QualifierShape!.Axes.Select(axis => axis.Preposition).Should().Equal(moneyMeta.QualifierShape!.Slots.Select(slot => RenderToken(slot.Preposition)));
        money.QualifierShape.Axes.Select(axis => axis.Axis).Should().Equal(moneyMeta.QualifierShape.Slots.Select(slot => slot.Axis.ToString()));
        money.QualifierShape.InOfExclusive.Should().Be(moneyMeta.QualifierShape.InOfExclusive);

        var setMeta = Types.GetMeta(TypeKind.Set);
        var set = result.Types.Should().ContainSingle(type => type.Kind == TypeKind.Set.ToString()).Subject;
        set.Accessors.Select(accessor => accessor.Name).Should().Equal(setMeta.Accessors.Select(accessor => accessor.Name));
        set.Accessors.Select(accessor => accessor.Description).Should().Equal(setMeta.Accessors.Select(accessor => accessor.Description));
        set.Accessors.Should().Contain(accessor => accessor.ReturnsElementType);
    }

    [Fact]
    public void Language_ModifiersMirrorModifierCatalogBySubtype()
    {
        var result = LanguageTool.Language();

        result.Modifiers.Field.Select(modifier => modifier.Kind).Should().Equal(Modifiers.All.OfType<FieldModifierMeta>().Select(modifier => modifier.Kind.ToString()));
        result.Modifiers.State.Select(modifier => modifier.Kind).Should().Equal(Modifiers.All.OfType<StateModifierMeta>().Select(modifier => modifier.Kind.ToString()));
        result.Modifiers.Event.Select(modifier => modifier.Kind).Should().Equal(Modifiers.All.OfType<EventModifierMeta>().Select(modifier => modifier.Kind.ToString()));
        result.Modifiers.Access.Select(modifier => modifier.Kind).Should().Equal(Modifiers.All.OfType<AccessModifierMeta>().Select(modifier => modifier.Kind.ToString()));
        result.Modifiers.Anchor.Select(modifier => modifier.Kind).Should().Equal(Modifiers.All.OfType<AnchorModifierMeta>().Select(modifier => modifier.Kind.ToString()));

        var anyTypeFieldMeta = Modifiers.All.OfType<FieldModifierMeta>().First(modifier => modifier.ApplicableTo.Length == 0);
        var anyTypeField = result.Modifiers.Field.Should().ContainSingle(modifier => modifier.Kind == anyTypeFieldMeta.Kind.ToString()).Subject;
        anyTypeField.ApplicableTo.Should().ContainSingle();
        anyTypeField.ApplicableTo[0].Type.Should().BeNull();
        anyTypeField.ApplicableTo[0].AnyType.Should().BeTrue();
        anyTypeField.ApplicableTo[0].RequiredModifiers.Should().BeEmpty();

        var stateMeta = Modifiers.All.OfType<StateModifierMeta>().First(modifier => !modifier.AllowsOutgoing || modifier.RequiresDominator || modifier.PreventsBackEdge);
        var state = result.Modifiers.State.Should().ContainSingle(modifier => modifier.Kind == stateMeta.Kind.ToString()).Subject;
        state.AllowsOutgoing.Should().Be(stateMeta.AllowsOutgoing);
        state.RequiresDominator.Should().Be(stateMeta.RequiresDominator);
        state.PreventsBackEdge.Should().Be(stateMeta.PreventsBackEdge);
        state.MutuallyExclusiveWith.Should().Equal(stateMeta.MutuallyExclusiveWith.Select(RenderModifier));

        var eventMeta = Modifiers.All.OfType<EventModifierMeta>().First();
        var eventModifier = result.Modifiers.Event.Should().ContainSingle(modifier => modifier.Kind == eventMeta.Kind.ToString()).Subject;
        eventModifier.RequiredAnalysis.Should().Be(eventMeta.RequiredAnalysis.ToString());
        eventModifier.DesugarsToRule.Should().Be(eventMeta.DesugarsToRule);

        var accessMeta = Modifiers.All.OfType<AccessModifierMeta>().First(modifier => modifier.IsPresent || modifier.IsWritable);
        var access = result.Modifiers.Access.Should().ContainSingle(modifier => modifier.Kind == accessMeta.Kind.ToString()).Subject;
        access.IsPresent.Should().Be(accessMeta.IsPresent);
        access.IsWritable.Should().Be(accessMeta.IsWritable);
        access.MutuallyExclusiveWith.Should().Equal(accessMeta.MutuallyExclusiveWith.Select(RenderModifier));

        var anchorMeta = Modifiers.All.OfType<AnchorModifierMeta>().First();
        var anchor = result.Modifiers.Anchor.Should().ContainSingle(modifier => modifier.Kind == anchorMeta.Kind.ToString()).Subject;
        anchor.Scope.Should().Be(anchorMeta.Scope.ToString());
        anchor.Target.Should().Be(anchorMeta.Target.ToString());
        anchor.DesugarsToRule.Should().Be(anchorMeta.DesugarsToRule);
    }

    [Fact]
    public void Language_ActionsMirrorActionCatalogInDeclarationOrderAndRepresentativeFields()
    {
        var result = LanguageTool.Language();

        result.Actions.Select(action => action.Kind).Should().Equal(Actions.All.Select(action => action.Kind.ToString()));

        var actionMeta = Actions.All.First(action => action.PrimaryActionKind is not null || action.IntoSupported || action.AllowedIn.Length > 1);
        var action = result.Actions.Should().ContainSingle(entry => entry.Kind == actionMeta.Kind.ToString()).Subject;
        action.Keyword.Should().Be(actionMeta.Token.Text ?? actionMeta.Kind.ToString());
        action.Description.Should().Be(actionMeta.Description);
        action.AllowedIn.Should().Equal(actionMeta.AllowedIn.Select(kind => kind.ToString()));
        action.SyntaxShape.Should().Be(actionMeta.SyntaxShape.ToString());
        action.ValueRequired.Should().Be(actionMeta.ValueRequired);
        action.IntoSupported.Should().Be(actionMeta.IntoSupported);
        action.PrimaryActionKind.Should().Be(actionMeta.PrimaryActionKind?.ToString());
        action.ApplicableTo.Should().HaveCount(actionMeta.ApplicableTo.Length == 0 ? 1 : actionMeta.ApplicableTo.Length);
    }

    [Fact]
    public void Language_ConstructsMirrorConstructCatalogInDeclarationOrderAndRepresentativeFields()
    {
        var result = LanguageTool.Language();

        result.Constructs.Select(construct => construct.Kind).Should().Equal(Constructs.All.Select(construct => construct.Kind.ToString()));

        var constructMeta = Constructs.All.First(construct => construct.Slots.Count() > 1 || construct.Entries.Count() > 1 || construct.SnippetTemplate is not null);
        var construct = result.Constructs.Should().ContainSingle(entry => entry.Kind == constructMeta.Kind.ToString()).Subject;
        construct.Name.Should().Be(constructMeta.Name);
        construct.Description.Should().Be(constructMeta.Description);
        construct.UsageExample.Should().Be(constructMeta.UsageExample);
        construct.PrimaryLeadingToken.Should().Be(RenderToken(constructMeta.PrimaryLeadingToken));
        construct.AllowedIn.Should().Equal(constructMeta.AllowedIn.Select(kind => kind.ToString()));
        construct.Slots.Select(slot => slot.Kind).Should().Equal(constructMeta.Slots.Select(slot => slot.Kind.ToString()));
        construct.Entries.Select(entry => entry.LeadingToken).Should().Equal(constructMeta.Entries.Select(entry => RenderToken(entry.LeadingToken)));
        construct.RoutingFamily.Should().Be(constructMeta.RoutingFamily.ToString());
        construct.ModifierDomain.Should().Be(constructMeta.ModifierDomain.ToString());
        construct.SnippetTemplate.Should().Be(constructMeta.SnippetTemplate);
    }

    [Fact]
    public void Language_ConstraintsMirrorConstraintCatalog()
    {
        var result = LanguageTool.Language();

        var actual = result.Constraints.Select(constraint => new
        {
            constraint.Kind,
            constraint.Description,
            constraint.Scope,
            constraint.Tokens,
        });

        var expected = Constraints.All.Select(constraint => new
        {
            Kind = constraint.Kind.ToString(),
            constraint.Description,
            Scope = ExpectedConstraintScope(constraint),
            Tokens = ExpectedConstraintTokens(constraint),
        });

        actual.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
    }

    [Fact]
    public void Language_OperatorsMirrorOperatorCatalogInDeclarationOrder()
    {
        var result = LanguageTool.Language();

        result.Operators.Select(op => op.Kind).Should().Equal(Operators.All.Select(op => op.Kind.ToString()));

        var isSetMeta = Operators.All.Single(op => op.Kind == OperatorKind.IsSet);
        var isSetTokens = isSetMeta switch
        {
            SingleTokenOp single => [RenderToken(single.Token.Kind)],
            MultiTokenOp multi => multi.Tokens.Select(token => RenderToken(token.Kind)).ToArray(),
            _ => throw new ArgumentOutOfRangeException(nameof(isSetMeta), isSetMeta, null),
        };
        var isSet = result.Operators.Should().ContainSingle(op => op.Kind == OperatorKind.IsSet.ToString()).Subject;
        isSet.Text.Should().Be(string.Join(" ", isSetTokens));
        isSet.Tokens.Should().Equal(isSetTokens);
        isSet.Arity.Should().Be(isSetMeta.Arity.ToString());
        isSet.Associativity.Should().Be(isSetMeta.Associativity.ToString());
        isSet.Precedence.Should().Be(isSetMeta.Precedence);
        isSet.Family.Should().Be(isSetMeta.Family.ToString());
        isSet.IsKeywordOperator.Should().Be(isSetMeta.IsKeywordOperator);
        isSet.Description.Should().Be(isSetMeta.Description);
    }

    [Fact]
    public void Language_FunctionsMirrorFunctionCatalogInDeclarationOrderAndRepresentativeFields()
    {
        var result = LanguageTool.Language();

        result.Functions.Select(function => function.Kind).Should().Equal(Functions.All.Select(function => function.Kind.ToString()));

        var functionMeta = Functions.All.First(function => function.Overloads.Count > 1 || function.HasCIVariant);
        var function = result.Functions.Should().ContainSingle(entry => entry.Kind == functionMeta.Kind.ToString()).Subject;
        function.Name.Should().Be(functionMeta.Name);
        function.Category.Should().Be(functionMeta.Category.ToString());
        function.Description.Should().Be(functionMeta.Description);
        function.HasCaseInsensitiveVariant.Should().Be(functionMeta.HasCIVariant);
        function.CaseInsensitiveVariantOf.Should().Be(functionMeta.CIVariantOf?.ToString());
        function.CaseInsensitiveDiagnosticCode.Should().Be(functionMeta.CIDiagnosticCode?.ToString());
        function.Overloads.Select(overload => new { Parameters = overload.Parameters.Select(parameter => parameter.Type).ToArray(), overload.ReturnType, overload.QualifierMatch })
            .Should()
            .BeEquivalentTo(
                functionMeta.Overloads.Select(overload => new
                {
                    Parameters = overload.Parameters.Select(parameter => parameter.Kind.ToString()).ToArray(),
                    ReturnType = overload.ReturnType.ToString(),
                    QualifierMatch = overload.Match?.ToString() ?? QualifierMatch.Any.ToString(),
                }),
                options => options.WithStrictOrdering());
    }

    [Fact]
    public void Language_DiagnosticsMirrorDiagnosticCatalogInDeclarationOrderAndRepresentativeFields()
    {
        var result = LanguageTool.Language();

        result.Diagnostics.Select(diagnostic => diagnostic.Code).Should().Equal(Diagnostics.All.Select(diagnostic => diagnostic.Code));

        var diagnosticMeta = Diagnostics.All.First(diagnostic =>
            (diagnostic.RelatedCodes?.Length ?? 0) > 0 ||
            (diagnostic.SuggestionSources?.Length ?? 0) > 0 ||
            diagnostic.PreventsFault is not null ||
            diagnostic.FixHint is not null);
        var diagnostic = result.Diagnostics.Should().ContainSingle(entry => entry.Code == diagnosticMeta.Code).Subject;
        diagnostic.Stage.Should().Be(diagnosticMeta.Stage.ToString());
        diagnostic.Severity.Should().Be(diagnosticMeta.Severity.ToString());
        diagnostic.Category.Should().Be(diagnosticMeta.Category.ToString());
        diagnostic.MessageTemplate.Should().Be(diagnosticMeta.MessageTemplate);
        diagnostic.RelatedCodes.Should().Equal(diagnosticMeta.RelatedCodes?.Select(code => code.ToString()) ?? []);
        diagnostic.FixHint.Should().Be(diagnosticMeta.FixHint);
        diagnostic.PreventsFault.Should().Be(diagnosticMeta.PreventsFault?.ToString());
        diagnostic.SuggestionSources.Should().Equal(diagnosticMeta.SuggestionSources?.Select(source => source.ToString()) ?? []);
    }

    [Fact]
    public void Language_FirePipelineIsPopulatedInExecutionOrder()
    {
        var result = LanguageTool.Language();

        result.FirePipeline.Should().Equal(
            "RowMatching",
            "GuardEvaluation",
            "PreconditionCheck",
            "MutationApplication",
            "InvariantCheck",
            "StateEnsuresCheck",
            "EventEnsuresCheck");
    }

    [Fact]
    public void Language_TokenCountMeetsSpecFloor()
    {
        var result = LanguageTool.Language();

        result.Tokens.Should().HaveCountGreaterThanOrEqualTo(80);
    }

    private static string ExpectedConstraintScope(ConstraintMeta constraint)
        => constraint switch
        {
            ConstraintMeta.Invariant => "Definition",
            ConstraintMeta.StateAnchored => "State",
            ConstraintMeta.EventPrecondition => "Event",
            _ => throw new ArgumentOutOfRangeException(nameof(constraint), constraint, null),
        };

    private static string[] ExpectedConstraintTokens(ConstraintMeta constraint)
        => constraint switch
        {
            ConstraintMeta.Invariant => [RenderToken(TokenKind.Rule)],
            ConstraintMeta.StateAnchored stateAnchored => [RenderToken(stateAnchored.LeadingToken), RenderToken(TokenKind.Ensure)],
            ConstraintMeta.EventPrecondition => [RenderToken(TokenKind.On), RenderToken(TokenKind.Ensure)],
            _ => throw new ArgumentOutOfRangeException(nameof(constraint), constraint, null),
        };

    private static string RenderModifier(ModifierKind kind)
        => Modifiers.GetMeta(kind).Token.Text ?? kind.ToString();

    private static string RenderToken(TokenKind kind)
        => Tokens.GetMeta(kind).Text ?? kind.ToString();
}
