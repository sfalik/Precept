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
            "operations",
            "outcomes",
            "syntaxReference",
            "domains",
            "firePipeline");

        document.RootElement.GetProperty("modifiers").EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(
            "value",
            "state",
            "event",
            "access",
            "anchor");

        document.RootElement.GetProperty("domains").EnumerateObject().Select(property => property.Name).Should().BeEquivalentTo(
            "currencies",
            "ucumTier1Units",
            "dimensions",
            "temporalUnits");
    }

    [Fact]
    public void Language_TokensMirrorTokenCatalogInDeclarationOrderAndRepresentativeFields()
    {
        var result = LanguageTool.Language();

        result.Tokens.Select(token => token.Kind).Should().Equal(Tokens.All.Select(token => token.Kind.ToString()));

        var becauseMeta = Tokens.GetMeta(TokenKind.Because);
        var becauseVisual = SemanticTokenTypes.GetMeta(becauseMeta.VisualCategory!.Value);
        var because = result.Tokens.Should().ContainSingle(token => token.Kind == TokenKind.Because.ToString()).Subject;

        because.Kind.Should().Be(becauseMeta.Kind.ToString());
        because.Text.Should().Be(becauseMeta.Text);
        because.Categories.Should().Equal(becauseMeta.Categories.Select(category => category.ToString()));
        because.Description.Should().Be(becauseMeta.Description);
        because.TextMateScope.Should().Be(becauseVisual.TextMateScope);
        because.SemanticTokenType.Should().Be(becauseVisual.CustomType);
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
    public void Language_TypesIncludeAuthoringMetadataAndContentValidation()
    {
        var result = LanguageTool.Language();

        var dateMeta = Types.GetMeta(TypeKind.Date);
        var date = result.Types.Should().ContainSingle(type => type.Kind == TypeKind.Date.ToString()).Subject;
        date.HoverDescription.Should().Be(dateMeta.HoverDescription);
        date.UsageExample.Should().Be(dateMeta.UsageExample);
        date.NotemptyApplicable.Should().Be(dateMeta.NotemptyApplicable);
        date.ContentValidation.Should().NotBeNull();
        date.ContentValidation!.Kind.Should().Be("NodaTime");
        date.ContentValidation.FormatDescription.Should().Be(dateMeta.ContentValidation!.FormatDescription);
        date.ContentValidation.Examples.Should().Equal(dateMeta.ContentValidation.Examples);
        date.ContentValidation.NodaTimePattern.Should().Be(((NodaTimeValidation)dateMeta.ContentValidation).NodaTimePattern);
        date.ContentValidation.LiteralKind.Should().Be(((NodaTimeValidation)dateMeta.ContentValidation).LiteralKind.ToString());

        var currency = result.Types.Should().ContainSingle(type => type.Kind == TypeKind.Currency.ToString()).Subject;
        currency.ContentValidation.Should().NotBeNull();
        currency.ContentValidation!.Kind.Should().Be("ClosedSet");
        currency.ContentValidation.SetName.Should().Be("ISO 4217");
        currency.ContentValidation.AllowedValues.Should().Contain(new[] { "USD", "EUR" });

        var lookup = result.Types.Should().ContainSingle(type => type.Kind == TypeKind.Lookup.ToString()).Subject;
        lookup.NotemptyApplicable.Should().BeFalse();

        var set = result.Types.Should().ContainSingle(type => type.Kind == TypeKind.Set.ToString()).Subject;
        var minAccessor = set.Accessors.Should().ContainSingle(accessor => accessor.Name == "min").Subject;
        minAccessor.ProofRequirements.Should().Equal("self.count > 0 — Collection must be non-empty");
    }

    [Fact]
    public void Language_ModifiersMirrorModifierCatalogBySubtype()
    {
        var result = LanguageTool.Language();
        var valueModifiers = ValueModifierDtoTestAccess.GetEntries(result.Modifiers);
        var valueModifierMetas = ValueModifierDtoTestAccess.GetCatalogMetas();

        ValueModifierDtoTestAccess.GetKinds(result.Modifiers).Should().Equal(ValueModifierDtoTestAccess.GetCatalogKinds());
        result.Modifiers.State.Select(modifier => modifier.Kind).Should().Equal(Modifiers.All.OfType<StateModifierMeta>().Select(modifier => modifier.Kind.ToString()));
        result.Modifiers.Event.Select(modifier => modifier.Kind).Should().Equal(Modifiers.All.OfType<EventModifierMeta>().Select(modifier => modifier.Kind.ToString()));
        result.Modifiers.Access.Select(modifier => modifier.Kind).Should().Equal(Modifiers.All.OfType<AccessModifierMeta>().Select(modifier => modifier.Kind.ToString()));
        result.Modifiers.Anchor.Select(modifier => modifier.Kind).Should().Equal(Modifiers.All.OfType<AnchorModifierMeta>().Select(modifier => modifier.Kind.ToString()));

        var anyTypeFieldMeta = valueModifierMetas.First(modifier => modifier.ApplicableTo.Length == 0);
        var anyTypeField = valueModifiers.Single(modifier => (string)modifier.Kind == anyTypeFieldMeta.Kind.ToString());
        var anyTypeTargets = ValueModifierDtoTestAccess.GetApplicableTypes((object)anyTypeField);
        anyTypeTargets.Should().ContainSingle();
        ValueModifierDtoTestAccess.GetProperty<string?>(anyTypeTargets[0], "Type").Should().BeNull();
        ValueModifierDtoTestAccess.GetProperty<bool>(anyTypeTargets[0], "AnyType").Should().BeTrue();
        ValueModifierDtoTestAccess.GetProperty<string[]>(anyTypeTargets[0], "RequiredModifiers").Should().BeEmpty();
        ValueModifierDtoTestAccess.GetApplicableDeclarationSites((object)anyTypeField).Should().Equal("FieldDeclaration", "EventArgDeclaration");

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
    public void Language_ValueModifiersIncludeAuthoringMetadataAndDeclarationSiteApplicabilityAndProofSatisfactions()
    {
        var result = LanguageTool.Language();
        var valueModifiers = ValueModifierDtoTestAccess.GetEntries(result.Modifiers);

        var notemptyMeta = ValueModifierDtoTestAccess.GetCatalogMeta(ModifierKind.Notempty);
        var notempty = valueModifiers.Single(modifier => (string)modifier.Kind == ModifierKind.Notempty.ToString());
        ValueModifierDtoTestAccess.GetApplicableDeclarationSites((object)notempty).Should().Equal("FieldDeclaration", "EventArgDeclaration");
        ValueModifierDtoTestAccess.GetProofSatisfactions((object)notempty).Should().Equal("self.length > 0", "self.count > 0");
        ValueModifierDtoTestAccess.GetProperty<string?>((object)notempty, "HoverDescription").Should().Be(notemptyMeta.HoverDescription);
        ValueModifierDtoTestAccess.GetProperty<string?>((object)notempty, "UsageExample").Should().Be(notemptyMeta.UsageExample);
        ValueModifierDtoTestAccess.GetProperty<string?>((object)notempty, "SnippetTemplate").Should().Be(notemptyMeta.SnippetTemplate);

        var writable = valueModifiers.Single(modifier => (string)modifier.Kind == ModifierKind.Writable.ToString());
        ValueModifierDtoTestAccess.GetApplicableDeclarationSites((object)writable).Should().Equal("FieldDeclaration");
    }

    [Fact]
    public void Language_ActionsMirrorActionCatalogInDeclarationOrderAndRepresentativeFields()
    {
        var result = LanguageTool.Language();

        result.Actions.Select(action => action.Kind).Should().Equal(Actions.All.Select(action => action.Kind.ToString()));

        var actionMeta = Actions.All.First(action => action.PrimaryActionKind is not null || Actions.GetShapeMeta(action.SyntaxShape).Slots.Any(s => s.Role == ActionSlotRole.IntoTarget) || action.AllowedIn.Length > 1);
        var action = result.Actions.Should().ContainSingle(entry => entry.Kind == actionMeta.Kind.ToString()).Subject;
        action.Keyword.Should().Be(actionMeta.Token.Text ?? actionMeta.Kind.ToString());
        action.Description.Should().Be(actionMeta.Description);
        action.AllowedIn.Should().Equal(actionMeta.AllowedIn.Select(kind => kind.ToString()));
        action.SyntaxShape.Should().Be(actionMeta.SyntaxShape.ToString());
        action.ValueRequired.Should().Be(actionMeta.ValueRequired);
        action.IntoSupported.Should().Be(Actions.GetShapeMeta(actionMeta.SyntaxShape).Slots.Any(s => s.Role == ActionSlotRole.IntoTarget));
        action.PrimaryActionKind.Should().Be(actionMeta.PrimaryActionKind?.ToString());
        action.ProofRequirements.Should().BeNull();
        action.HoverDescription.Should().Be(actionMeta.HoverDescription);
        action.UsageExample.Should().Be(actionMeta.UsageExample);
        action.SnippetTemplate.Should().Be(actionMeta.SnippetTemplate);
        action.ApplicableTo.Should().HaveCount(actionMeta.ApplicableTo.Length == 0 ? 1 : actionMeta.ApplicableTo.Length);
    }

    [Fact]
    public void Language_ActionsIncludeProofRequirements()
    {
        var result = LanguageTool.Language();

        var dequeueMeta = Actions.GetMeta(ActionKind.Dequeue);
        var dequeue = result.Actions.Should().ContainSingle(action => action.Kind == ActionKind.Dequeue.ToString()).Subject;
        dequeue.ProofRequirements.Should().Equal("self.count > 0 — Queue must be non-empty");
        dequeue.HoverDescription.Should().Be(dequeueMeta.HoverDescription);
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
        isSet.HoverDescription.Should().Be(isSetMeta.HoverDescription);
        isSet.UsageExample.Should().Be(isSetMeta.UsageExample);
    }

    [Fact]
    public void Language_OperationsMirrorOperationCatalogInDeclarationOrderAndRepresentativeFields()
    {
        var result = LanguageTool.Language();

        result.Operations.Select(operation => operation.Kind).Should().Equal(Operations.All.Select(operation => operation.Kind.ToString()));

        var negateInteger = result.Operations.Should().ContainSingle(operation => operation.Kind == OperationKind.NegateInteger.ToString()).Subject;
        negateInteger.Operator.Should().Be(OperatorKind.Negate.ToString());
        negateInteger.LhsType.Should().Be(TypeKind.Integer.ToString());
        negateInteger.RhsType.Should().BeEmpty();
        negateInteger.ResultType.Should().Be(TypeKind.Integer.ToString());
        negateInteger.ProofRequirements.Should().BeNull();

        var stringEquals = result.Operations.Should().ContainSingle(operation => operation.Kind == OperationKind.StringEqualsString.ToString()).Subject;
        stringEquals.HasCIVariant.Should().BeTrue();
        stringEquals.CIDiagnosticCode.Should().Be(DiagnosticCode.CaseInsensitiveFieldRequiresTildeEquals.ToString());

        var quantityDivide = result.Operations.Should().ContainSingle(operation => operation.Kind == OperationKind.QuantityDivideQuantityCrossDimension.ToString()).Subject;
        quantityDivide.QualifierMatch.Should().Be(QualifierMatch.Different.ToString());
        ((string[]?)quantityDivide.ProofRequirements).Should().Equal("quantity != 0 — Divisor must be non-zero");

        var pricePlus = result.Operations.Should().ContainSingle(operation => operation.Kind == OperationKind.PricePlusPrice.ToString()).Subject;
        ((string[]?)pricePlus.ProofRequirements).Should().Equal(
            "operands share unit qualifiers — Operands must have matching unit qualifiers",
            "operands share currency qualifiers — Operands must have matching currency qualifiers");
    }

    [Fact]
    public void Language_OutcomesMirrorOutcomeCatalog()
    {
        var result = LanguageTool.Language();

        result.Outcomes.Select(outcome => outcome.Kind).Should().Equal(Outcomes.All.Select(outcome => outcome.Kind.ToString()));

        var rejectMeta = Outcomes.GetMeta(OutcomeKind.Reject);
        var reject = result.Outcomes.Should().ContainSingle(outcome => outcome.Kind == OutcomeKind.Reject.ToString()).Subject;
        reject.LeadingToken.Should().Be(RenderToken(rejectMeta.LeadingToken));
        reject.ArgumentKind.Should().Be(rejectMeta.ArgumentKind.ToString());
        reject.Description.Should().Be(rejectMeta.Description);
        reject.Example.Should().Be(rejectMeta.Example);
    }

    [Fact]
    public void Language_SyntaxReferenceMirrorsSourceAndExamplesCompile()
    {
        var result = LanguageTool.Language();

        result.SyntaxReference.GrammarModel.Should().Be(SyntaxReference.GrammarModel);
        result.SyntaxReference.CommentSyntax.Should().Be(SyntaxReference.CommentSyntax);
        result.SyntaxReference.IdentifierRules.Should().Be(SyntaxReference.IdentifierRules);
        result.SyntaxReference.StringLiteralRules.Should().Be(SyntaxReference.StringLiteralRules);
        result.SyntaxReference.NumberLiteralRules.Should().Be(SyntaxReference.NumberLiteralRules);
        result.SyntaxReference.WhitespaceRules.Should().Be(SyntaxReference.WhitespaceRules);
        result.SyntaxReference.NullNarrowing.Should().Be(SyntaxReference.NullNarrowing);
        result.SyntaxReference.TypedConstantRules.Should().Be(SyntaxReference.TypedConstantRules);
        result.SyntaxReference.ExpressionRules.Should().Be(SyntaxReference.ExpressionRules);
        result.SyntaxReference.PrecedenceTable.Should().Equal(SyntaxReference.PrecedenceTable);
        result.SyntaxReference.ConventionalOrder.Should().Equal(SyntaxReference.ConventionalOrder);
        result.SyntaxReference.CommonPatterns.Select(pattern => pattern.Name).Should().Equal(SyntaxReference.CommonPatterns.Select(pattern => pattern.Name));
        result.SyntaxReference.AntiPatterns.Select(pattern => pattern.Name).Should().Equal(SyntaxReference.AntiPatterns.Select(pattern => pattern.Name));
        result.SyntaxReference.CommonPatterns.Should().Contain(pattern => pattern.Name == "Computed field" && pattern.DslSnippet.Contains("<-"));

        foreach (var pattern in result.SyntaxReference.CommonPatterns)
        {
            var source = BuildPatternDocument(pattern.Name, pattern.DslSnippet);
            var compilation = CompileTool.Compile(source);
            compilation.HasErrors.Should().BeFalse($"syntaxReference pattern '{pattern.Name}' should compile cleanly");
        }

        foreach (var pattern in result.SyntaxReference.AntiPatterns)
        {
            var source = BuildPatternDocument(pattern.Name, pattern.GoodSnippet);
            var compilation = CompileTool.Compile(source);
            compilation.HasErrors.Should().BeFalse($"syntaxReference anti-pattern fix '{pattern.Name}' should compile cleanly");
        }
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
        function.UsageExample.Should().Be(functionMeta.UsageExample);
        function.SnippetTemplate.Should().Be(functionMeta.SnippetTemplate);
        function.HoverDescription.Should().Be(functionMeta.HoverDescription);
        function.IsMessagePosition.Should().Be(functionMeta.IsMessagePosition);
        function.Overloads.Select(overload => new
            {
                Parameters = overload.Parameters.Select(parameter => parameter.Type).ToArray(),
                overload.ReturnType,
                overload.QualifierMatch,
            })
            .Should()
            .BeEquivalentTo(
                functionMeta.Overloads.Select(overload => new
                {
                    Parameters = overload.Parameters.Select(parameter => parameter.Kind.ToString()).ToArray(),
                    ReturnType = overload.ReturnType.ToString(),
                    QualifierMatch = overload.Match?.ToString() ?? QualifierMatch.Any.ToString(),
                }),
                options => options.WithStrictOrdering());

        var sqrt = result.Functions.Should().ContainSingle(entry => entry.Kind == FunctionKind.Sqrt.ToString()).Subject;
        sqrt.Overloads.Should().ContainSingle();
        sqrt.Overloads[0].ProofRequirements.Should().Equal("value >= 0 — Argument must be non-negative");
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
        diagnostic.TriggerCondition.Should().Be(diagnosticMeta.TriggerCondition);
        diagnostic.RecoverySteps.Should().Equal(diagnosticMeta.RecoverySteps ?? []);
        diagnostic.ExampleBefore.Should().Be(diagnosticMeta.ExampleBefore);
        diagnostic.ExampleAfter.Should().Be(diagnosticMeta.ExampleAfter);
    }

    [Fact]
    public void Language_DomainsMirrorRuntimeRegistries()
    {
        var result = LanguageTool.Language();

        result.Domains.Currencies.Select(entry => entry.AlphaCode)
            .Should()
            .Equal(CurrencyCatalog.All.Values.OrderBy(entry => entry.AlphaCode).Select(entry => entry.AlphaCode));
        var usd = result.Domains.Currencies.Should().ContainSingle(entry => entry.AlphaCode == "USD").Subject;
        usd.NumericCode.Should().Be(CurrencyCatalog.All["USD"].NumericCode);
        usd.Name.Should().Be(CurrencyCatalog.All["USD"].Name);
        usd.MinorUnit.Should().Be(CurrencyCatalog.All["USD"].MinorUnit);
        usd.Symbol.Should().Be(CurrencyCatalog.All["USD"].Symbol);

        result.Domains.UcumTier1Units.Select(entry => entry.Code)
            .Should()
            .Equal(UcumAtomCatalog.BrowseTier1().Select(entry => entry.Code));
        var newton = result.Domains.UcumTier1Units.Should().ContainSingle(entry => entry.Code == "N").Subject;
        newton.Name.Should().Be(UcumAtomCatalog.All["N"].Name);
        newton.DimensionName.Should().Be("force");
        newton.Dimension.Length.Should().Be(UcumAtomCatalog.All["N"].Vector.Length);
        newton.Dimension.Mass.Should().Be(UcumAtomCatalog.All["N"].Vector.Mass);
        newton.Dimension.Time.Should().Be(UcumAtomCatalog.All["N"].Vector.Time);
        newton.Scale.Numerator.Should().Be(UcumAtomCatalog.All["N"].Scale.Numerator.ToString());
        newton.Scale.Denominator.Should().Be(UcumAtomCatalog.All["N"].Scale.Denominator.ToString());
        newton.Scale.Base10Exponent.Should().Be(UcumAtomCatalog.All["N"].Scale.Base10Exponent);

        result.Domains.Dimensions.Select(entry => entry.Name)
            .Should()
            .Equal(DimensionCatalog.All.Values.OrderBy(entry => entry.Name).Select(entry => entry.Name));
        var force = result.Domains.Dimensions.Should().ContainSingle(entry => entry.Name == "force").Subject;
        force.Description.Should().Be(DimensionCatalog.All["force"].Description);
        force.Dimension.Length.Should().Be(DimensionCatalog.All["force"].Vector.Length);
        force.Dimension.Mass.Should().Be(DimensionCatalog.All["force"].Vector.Mass);
        force.Dimension.Time.Should().Be(DimensionCatalog.All["force"].Vector.Time);

        result.Domains.TemporalUnits.Select(entry => entry.Singular)
            .Should()
            .Equal(TemporalUnits.AllEntries.Select(entry => entry.Singular));
        var year = result.Domains.TemporalUnits.Should().ContainSingle(entry => entry.Singular == "year").Subject;
        year.Plural.Should().Be(TemporalUnits.All["year"].Plural);
        year.IsCalendarBased.Should().BeTrue();
        year.IsPeriod.Should().BeTrue();
        year.IsDuration.Should().BeFalse();
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

    private static string BuildPatternDocument(string patternName, string snippet)
    {
        if (snippet.TrimStart().StartsWith("precept "))
            return snippet;

        return patternName switch
        {
            "Guarded transition" => $"""
                precept LoanApplication
                field DocumentsVerified as boolean default true
                field CreditScore as number default 700
                field ApprovedAmount as number default 0
                state UnderReview initial
                state Approved terminal
                event Approve(Amount as number)
                {snippet}
                """,
            "Computed field" => $"""
                precept InvoiceLineItem
                field UnitPrice as number default 0 nonnegative writable
                field Quantity as number default 1 min 1 writable
                field DiscountPercent as number default 0 nonnegative max 100 writable
                {snippet}
                """,
            "Conditional action" => $"""
                precept LoanDecision
                field CreditScore as number default 700
                field DecisionNote as string optional
                state UnderReview initial
                state Approved terminal
                event Approve
                {snippet}
                """,
            "Collection state gate" => $"""
                precept HiringPipeline
                field PendingInterviewers as set of string
                field CurrentInterviewer as string
                field FeedbackCount as integer default 0 nonnegative
                state InterviewLoop initial
                state Decision terminal
                event RecordFeedback
                {snippet}
                """,
            "Stateless write-only precept" => snippet,
            _ => throw new ArgumentOutOfRangeException(nameof(patternName), patternName, null),
        };
    }

    private static string RenderModifier(ModifierKind kind)
        => Modifiers.GetMeta(kind).Token.Text ?? kind.ToString();

    private static string RenderToken(TokenKind kind)
        => Tokens.GetMeta(kind).Text ?? kind.ToString();
}
