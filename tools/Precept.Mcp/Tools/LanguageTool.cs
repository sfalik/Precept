using System.ComponentModel;
using System.Globalization;
using ModelContextProtocol.Server;
using Precept.Language;
using Precept.Mcp.Dtos;

namespace Precept.Mcp.Tools;

[McpServerToolType]
public static class LanguageTool
{
    private static readonly string[] FirePipeline =
    [
        "RowMatching",
        "GuardEvaluation",
        "PreconditionCheck",
        "MutationApplication",
        "InvariantCheck",
        "StateEnsuresCheck",
        "EventEnsuresCheck",
    ];

    /// <summary>
    /// Returns the complete Precept DSL vocabulary. Not registered as a discoverable MCP tool —
    /// called internally by focused tools (precept_syntax, precept_types, etc.) that project
    /// catalog subsets. Kept intact for internal use only.
    /// </summary>
    public static LanguageReferenceDto Language()
        => new(
            Tokens.All.Select(MapToken).ToArray(),
            Types.All.Select(MapType).ToArray(),
            new ModifierCatalogDto(
                Modifiers.All.OfType<ValueModifierMeta>().Select(MapValueModifier).ToArray(),
                Modifiers.All.OfType<StateModifierMeta>().Select(MapStateModifier).ToArray(),
                Modifiers.All.OfType<EventModifierMeta>().Select(MapEventModifier).ToArray(),
                Modifiers.All.OfType<AccessModifierMeta>().Select(MapAccessModifier).ToArray(),
                Modifiers.All.OfType<AnchorModifierMeta>().Select(MapAnchorModifier).ToArray()),
            Actions.All.Select(MapAction).ToArray(),
            Constructs.All.Select(MapConstruct).ToArray(),
            Constraints.All.Select(MapConstraint).ToArray(),
            Operators.All.Select(MapOperator).ToArray(),
            Functions.All.Select(MapFunction).ToArray(),
            Diagnostics.All.Select(MapDiagnostic).ToArray(),
            SerializeAllOperations(),
            SerializeAllOutcomes(),
            MapSyntaxReference(),
            new DomainCatalogDto(
                CurrencyCatalog.All.Values.OrderBy(entry => entry.AlphaCode).Select(MapCurrency).ToArray(),
                UcumAtomCatalog.BrowseTier1().Select(MapUcumTier1Unit).ToArray(),
                DimensionCatalog.All.Values.OrderBy(entry => entry.Name).Select(MapDimension).ToArray(),
                TemporalUnits.AllEntries.Select(MapTemporalUnit).ToArray()),
            FirePipeline);

    private static TokenCatalogEntryDto MapToken(TokenMeta token)
    {
        var visual = token.VisualCategory.HasValue
            ? SemanticTokenTypes.GetMeta(token.VisualCategory.Value)
            : null;

        return new TokenCatalogEntryDto(
            token.Kind.ToString(),
            token.Text,
            token.Categories.Select(category => category.ToString()).ToArray(),
            token.Description,
            visual?.TextMateScope,
            visual?.CustomType,
            (token.ValidAfter ?? []).Select(previous => previous.ToString()).ToArray(),
            token.IsAccessModeAdjective,
            token.IsValidAsMemberName,
            token.IsMessagePosition);
    }

    private static TypeCatalogEntryDto MapType(TypeMeta type)
        => new(
            type.Kind.ToString(),
            type.Token?.Text,
            type.Description,
            type.Category.ToString(),
            type.DisplayName,
            ExpandFlags(type.Traits),
            type.WidensTo.Select(kind => kind.ToString()).ToArray(),
            type.ImpliedModifiers.Select(RenderModifier).ToArray(),
            type.QualifierShape is null ? null : MapQualifierShape(type.QualifierShape),
            type.Accessors.Select(MapAccessor).ToArray(),
            (type.ChoiceLiteralTokens ?? []).Select(kind => kind.ToString()).ToArray(),
            type.HoverDescription,
            type.UsageExample,
            type.NotemptyApplicable,
            type.ContentValidation is null ? null : MapContentValidation(type.ContentValidation));

    private static QualifierShapeDto MapQualifierShape(QualifierShape shape)
        => new(
            shape.Slots
                .Select(slot => new QualifierSlotDto(RenderToken(slot.Preposition), slot.Axis.ToString()))
                .ToArray(),
            shape.InOfExclusive);

    private static TypeAccessorDto MapAccessor(TypeAccessor accessor)
        => accessor switch
        {
            FixedReturnAccessor fixedReturn => new(
                fixedReturn.Name,
                fixedReturn.Description,
                fixedReturn.Returns.ToString(),
                false,
                fixedReturn.ParameterType?.ToString(),
                false,
                ExpandFlags(fixedReturn.RequiredTraits),
                fixedReturn.ReturnsQualifier == QualifierAxis.None ? null : fixedReturn.ReturnsQualifier.ToString(),
                RenderProofRequirementsOrNull(fixedReturn.ProofRequirements)),
            ElementParameterAccessor elementParameter => new(
                elementParameter.Name,
                elementParameter.Description,
                null,
                true,
                null,
                true,
                ExpandFlags(elementParameter.RequiredTraits),
                null,
                RenderProofRequirementsOrNull(elementParameter.ProofRequirements)),
            _ => new(
                accessor.Name,
                accessor.Description,
                null,
                true,
                accessor.ParameterType?.ToString(),
                false,
                ExpandFlags(accessor.RequiredTraits),
                null,
                RenderProofRequirementsOrNull(accessor.ProofRequirements)),
        };

    private static ContentValidationDto MapContentValidation(ContentValidation validation)
        => validation switch
        {
            RegexValidation regex => new(
                "Regex",
                regex.FormatDescription,
                regex.Examples,
                null,
                null,
                null,
                null),
            NodaTimeValidation nodaTime => new(
                "NodaTime",
                nodaTime.FormatDescription,
                nodaTime.Examples,
                nodaTime.NodaTimePattern,
                nodaTime.LiteralKind.ToString(),
                null,
                null),
            ClosedSetValidation closedSet => new(
                "ClosedSet",
                closedSet.FormatDescription,
                closedSet.Examples,
                null,
                null,
                closedSet.SetName,
                closedSet.AllowedValues.OrderBy(value => value, StringComparer.OrdinalIgnoreCase).ToArray()),
            UcumValidation ucum => new(
                "Ucum",
                ucum.FormatDescription,
                ucum.Examples,
                null,
                null,
                null,
                null),
            MoneyValidation money => new(
                "Money",
                money.FormatDescription,
                money.Examples,
                null,
                null,
                null,
                null),
            QuantityValidation quantity => new(
                "Quantity",
                quantity.FormatDescription,
                quantity.Examples,
                null,
                null,
                null,
                null),
            PriceValidation price => new(
                "Price",
                price.FormatDescription,
                price.Examples,
                null,
                null,
                null,
                null),
            ExchangeRateValidation exchangeRate => new(
                "ExchangeRate",
                exchangeRate.FormatDescription,
                exchangeRate.Examples,
                null,
                null,
                null,
                null),
            _ => throw new ArgumentOutOfRangeException(nameof(validation), validation, null),
        };

    private static ValueModifierCatalogEntryDto MapValueModifier(ValueModifierMeta modifier)
        => new(
            modifier.Kind.ToString(),
            modifier.Token.Text ?? modifier.Kind.ToString(),
            modifier.Description,
            modifier.Category.ToString(),
            MapTargets(modifier.ApplicableTo),
            MapApplicableDeclarationSites(modifier.ApplicableDeclarationSites),
            modifier.HasValue,
            modifier.Subsumes.Select(RenderModifier).ToArray(),
            modifier.DesugarsToRule,
            modifier.MutuallyExclusiveWith.Select(RenderModifier).ToArray(),
            RenderProofSatisfactionsOrNull(modifier.ProofSatisfactions),
            modifier.HoverDescription,
            modifier.UsageExample,
            modifier.SnippetTemplate);

    private static string[] MapApplicableDeclarationSites(ValueModifierDeclarationSite declarationSites)
        => Enum.GetValues<ValueModifierDeclarationSite>()
            .Where(site => site is not ValueModifierDeclarationSite.None && declarationSites.HasFlag(site))
            .Select(site => site.ToString())
            .ToArray();

    private static StateModifierCatalogEntryDto MapStateModifier(StateModifierMeta modifier)
        => new(
            modifier.Kind.ToString(),
            modifier.Token.Text ?? modifier.Kind.ToString(),
            modifier.Description,
            modifier.Category.ToString(),
            modifier.AllowsOutgoing,
            modifier.RequiresDominator,
            modifier.PreventsBackEdge,
            modifier.DesugarsToRule,
            modifier.MutuallyExclusiveWith.Select(RenderModifier).ToArray());

    private static EventModifierCatalogEntryDto MapEventModifier(EventModifierMeta modifier)
        => new(
            modifier.Kind.ToString(),
            modifier.Token.Text ?? modifier.Kind.ToString(),
            modifier.Description,
            modifier.Category.ToString(),
            modifier.RequiredAnalysis.ToString(),
            modifier.DesugarsToRule);

    private static AccessModifierCatalogEntryDto MapAccessModifier(AccessModifierMeta modifier)
        => new(
            modifier.Kind.ToString(),
            modifier.Token.Text ?? modifier.Kind.ToString(),
            modifier.Description,
            modifier.Category.ToString(),
            modifier.IsPresent,
            modifier.IsWritable,
            modifier.DesugarsToRule,
            modifier.MutuallyExclusiveWith.Select(RenderModifier).ToArray());

    private static AnchorModifierCatalogEntryDto MapAnchorModifier(AnchorModifierMeta modifier)
        => new(
            modifier.Kind.ToString(),
            modifier.Token.Text ?? modifier.Kind.ToString(),
            modifier.Description,
            modifier.Category.ToString(),
            modifier.Scope.ToString(),
            modifier.Target.ToString(),
            modifier.DesugarsToRule);

    private static ActionCatalogEntryDto MapAction(ActionMeta action)
        => new(
            action.Kind.ToString(),
            action.Token.Text ?? action.Kind.ToString(),
            action.Description,
            MapTargets(action.ApplicableTo),
            action.AllowedIn.Select(kind => kind.ToString()).ToArray(),
            action.SyntaxShape.ToString(),
            action.ValueRequired,
            Actions.GetShapeMeta(action.SyntaxShape).Slots.Any(s => s.Role == ActionSlotRole.IntoTarget),
            action.PrimaryActionKind?.ToString(),
            RenderProofRequirementsOrNull(action.ProofRequirements),
            action.HoverDescription,
            action.UsageExample,
            action.SnippetTemplate);

    private static ConstructCatalogEntryDto MapConstruct(ConstructMeta construct)
        => new(
            construct.Kind.ToString(),
            construct.Name,
            construct.Description,
            construct.UsageExample,
            RenderToken(construct.PrimaryLeadingToken),
            construct.AllowedIn.Select(kind => kind.ToString()).ToArray(),
            construct.Slots.Select(MapSlot).ToArray(),
            construct.Entries.Select(MapEntry).ToArray(),
            construct.RoutingFamily.ToString(),
            construct.ModifierDomain.ToString(),
            construct.SnippetTemplate);

    private static ConstructSlotDto MapSlot(ConstructSlot slot)
        => new(
            slot.Kind.ToString(),
            slot.IsRequired,
            slot.Description,
            (slot.TerminationTokens ?? []).Select(RenderToken).ToArray());

    private static DisambiguationEntryDto MapEntry(DisambiguationEntry entry)
        => new(
            RenderToken(entry.LeadingToken),
            (entry.DisambiguationTokens ?? []).Select(RenderToken).ToArray(),
            entry.LeadingTokenSlot?.ToString());

    private static ConstraintCatalogEntryDto MapConstraint(ConstraintMeta constraint)
        => constraint switch
        {
            ConstraintMeta.Invariant => new(
                constraint.Kind.ToString(),
                constraint.Description,
                "Definition",
                [RenderToken(TokenKind.Rule)]),
            ConstraintMeta.StateAnchored stateAnchored => new(
                constraint.Kind.ToString(),
                constraint.Description,
                "State",
                [RenderToken(stateAnchored.LeadingToken), RenderToken(TokenKind.Ensure)]),
            ConstraintMeta.EventPrecondition => new(
                constraint.Kind.ToString(),
                constraint.Description,
                "Event",
                [RenderToken(TokenKind.On), RenderToken(TokenKind.Ensure)]),
            _ => throw new ArgumentOutOfRangeException(nameof(constraint), constraint, null),
        };

    private static OperatorCatalogEntryDto MapOperator(OperatorMeta op)
    {
        var tokens = RenderOperatorTokens(op);

        return new(
            op.Kind.ToString(),
            string.Join(" ", tokens),
            tokens,
            op.Arity.ToString(),
            op.Associativity.ToString(),
            op.Precedence,
            op.Family.ToString(),
            op.IsKeywordOperator,
            op.Description,
            op.HoverDescription,
            op.UsageExample);
    }

    private static OperationDto[] SerializeAllOperations()
        => Operations.All.Select(MapOperation).ToArray();

    private static OperationDto MapOperation(OperationMeta op)
        => op switch
        {
            UnaryOperationMeta unary => new(
                unary.Kind.ToString(),
                unary.Op.ToString(),
                unary.Operand.Kind.ToString(),
                string.Empty,
                unary.Result.ToString(),
                unary.Description,
                QualifierMatch.Any.ToString(),
                null,
                false,
                null,
                false),
            BinaryOperationMeta binary => new(
                binary.Kind.ToString(),
                binary.Op.ToString(),
                binary.Lhs.Kind.ToString(),
                binary.Rhs.Kind.ToString(),
                binary.Result.ToString(),
                binary.Description,
                binary.Match.ToString(),
                RenderProofRequirementsOrNull(binary.ProofRequirements),
                binary.HasCIVariant,
                binary.CIDiagnosticCode?.ToString(),
                binary.BidirectionalLookup),
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null),
        };

    private static OutcomeDto[] SerializeAllOutcomes()
        => Outcomes.All.Select(MapOutcome).ToArray();

    private static OutcomeDto MapOutcome(OutcomeMeta outcome)
        => new(
            outcome.Kind.ToString(),
            RenderToken(outcome.LeadingToken),
            outcome.ArgumentKind.ToString(),
            outcome.Description,
            outcome.Example);

    private static SyntaxReferenceDto MapSyntaxReference()
        => new(
            SyntaxReference.GrammarModel,
            SyntaxReference.CommentSyntax,
            SyntaxReference.IdentifierRules,
            SyntaxReference.StringLiteralRules,
            SyntaxReference.NumberLiteralRules,
            SyntaxReference.WhitespaceRules,
            SyntaxReference.NullNarrowing,
            SyntaxReference.TypedConstantRules,
            SyntaxReference.ExpressionRules,
            SyntaxReference.PrecedenceTable.ToArray(),
            SyntaxReference.CommonPatterns.Select(MapCommonPattern).ToArray(),
            SyntaxReference.AntiPatterns.Select(MapAntiPattern).ToArray(),
            SyntaxReference.ConventionalOrder.ToArray());

    private static CommonPatternDto MapCommonPattern(CommonPattern pattern)
        => new(pattern.Name, pattern.Description, pattern.DslSnippet);

    private static AntiPatternDto MapAntiPattern(AntiPattern pattern)
        => new(pattern.Name, pattern.Description, pattern.BadSnippet, pattern.GoodSnippet, pattern.WhyItFails);

    private static FunctionCatalogEntryDto MapFunction(FunctionMeta function)
        => new(
            function.Kind.ToString(),
            function.Name,
            function.Category.ToString(),
            function.Description,
            function.Overloads.Select(MapOverload).ToArray(),
            function.HasCIVariant,
            function.CIVariantOf?.ToString(),
            function.CIDiagnosticCode?.ToString(),
            function.UsageExample,
            function.SnippetTemplate,
            function.HoverDescription,
            function.IsMessagePosition);

    private static FunctionOverloadDto MapOverload(FunctionOverload overload)
        => new(
            overload.Parameters.Select(parameter => new FunctionParameterDto(parameter.Name, parameter.Kind.ToString())).ToArray(),
            overload.ReturnType.ToString(),
            overload.Match?.ToString() ?? QualifierMatch.Any.ToString(),
            RenderProofRequirementsOrNull(overload.ProofRequirements));

    private static DiagnosticCatalogEntryDto MapDiagnostic(DiagnosticMeta diagnostic)
        => new(
            diagnostic.Code,
            diagnostic.Stage.ToString(),
            diagnostic.Severity.ToString(),
            diagnostic.Category.ToString(),
            diagnostic.MessageTemplate,
            diagnostic.RelatedCodes?.Select(code => code.ToString()).ToArray() ?? [],
            diagnostic.FixHint,
            diagnostic.PreventsFault?.ToString(),
            diagnostic.SuggestionSources?.Select(source => source.ToString()).ToArray() ?? [],
            diagnostic.TriggerCondition,
            diagnostic.RecoverySteps ?? [],
            diagnostic.ExampleBefore,
            diagnostic.ExampleAfter);

    private static CurrencyDomainEntryDto MapCurrency(CurrencyEntry currency)
        => new(currency.AlphaCode, currency.NumericCode, currency.Name, currency.MinorUnit, currency.Symbol);

    private static UcumTier1UnitDto MapUcumTier1Unit(UcumAtom atom)
        => new(
            atom.Code,
            atom.Name,
            MapDimensionVector(atom.Vector),
            ResolveDimensionName(atom.Vector),
            MapScale(atom.Scale),
            atom.Prefixable,
            atom.AnnotationClass);

    private static DimensionDomainEntryDto MapDimension(DimensionCatalog.DimensionAlias alias)
        => new(alias.Name, MapDimensionVector(alias.Vector), alias.Description);

    private static TemporalUnitDomainEntryDto MapTemporalUnit(TemporalUnits.TemporalUnitEntry entry)
        => new(entry.Singular, entry.Plural, entry.IsCalendarBased, entry.IsPeriod, entry.IsDuration);

    private static DimensionVectorDto MapDimensionVector(DimensionVector vector)
        => new(
            vector.Length,
            vector.Mass,
            vector.Time,
            vector.ElectricCurrent,
            vector.Temperature,
            vector.AmountOfSubstance,
            vector.LuminousIntensity);

    private static UcumExactFactorDto MapScale(UcumExactFactor factor)
        => new(factor.Numerator.ToString(), factor.Denominator.ToString(), factor.Base10Exponent);

    private static string? ResolveDimensionName(DimensionVector vector)
        => DimensionCatalog.TryGetAlias(vector, out var alias) && alias is not null ? alias.Name : null;

    private static ModifierTargetDto[] MapTargets(TypeTarget[] targets)
    {
        if (targets.Length == 0)
        {
            return [new ModifierTargetDto(null, true, [])];
        }

        return targets.Select(MapTarget).ToArray();
    }

    private static ModifierTargetDto MapTarget(TypeTarget target)
        => target switch
        {
            ModifiedTypeTarget modified => new(
                modified.Kind?.ToString(),
                modified.Kind is null,
                modified.RequiredModifiers.Select(RenderModifier).ToArray()),
            _ => new(
                target.Kind?.ToString(),
                target.Kind is null,
                []),
        };

    private static string[]? RenderProofRequirementsOrNull(IReadOnlyList<ProofRequirement> proofRequirements)
    {
        var rendered = proofRequirements.Select(RenderProofRequirement).ToArray();
        return rendered.Length == 0 ? null : rendered;
    }

    private static string RenderProofRequirement(ProofRequirement proofRequirement)
        => proofRequirement switch
        {
            NumericProofRequirement numeric => $"{RenderProofSubject(numeric.Subject)} {RenderComparison(numeric.Comparison)} {numeric.Threshold.ToString(CultureInfo.InvariantCulture)} — {numeric.Description}",
            PresenceProofRequirement presence => $"{RenderProofSubject(presence.Subject)} is present — {presence.Description}",
            DimensionProofRequirement dimension => $"{RenderProofSubject(dimension.Subject)} has {dimension.RequiredDimension.ToString().ToLowerInvariant()} temporal dimension — {dimension.Description}",
            QualifierCompatibilityProofRequirement qualifierCompatibility => $"{RenderCompatibleSubjects(qualifierCompatibility.LeftSubject, qualifierCompatibility.RightSubject)} share {qualifierCompatibility.Axis.ToString().ToLowerInvariant()} qualifiers — {qualifierCompatibility.Description}",
            ModifierRequirement modifier => $"{RenderProofSubject(modifier.Subject)} declares {RenderModifier(modifier.Required)} — {modifier.Description}",
            _ => throw new ArgumentOutOfRangeException(nameof(proofRequirement), proofRequirement, null),
        };

    private static string[]? RenderProofSatisfactionsOrNull(IReadOnlyList<ProofSatisfaction> proofSatisfactions)
    {
        var rendered = proofSatisfactions.Select(RenderProofSatisfaction).ToArray();
        return rendered.Length == 0 ? null : rendered;
    }

    private static string RenderProofSatisfaction(ProofSatisfaction proofSatisfaction)
        => proofSatisfaction switch
        {
            ProofSatisfaction.Numeric numeric => $"{RenderProjection(numeric.Projection)} {RenderComparison(numeric.Comparison)} {RenderBoundSource(numeric.Bound)}",
            ProofSatisfaction.Presence _ => "self is present",
            ProofSatisfaction.Dimension dimension => $"self has {RenderDimensionSource(dimension.Source)} temporal dimension",
            ProofSatisfaction.Modifier modifier => $"self declares {RenderModifier(modifier.RequiredModifier)}",
            ProofSatisfaction.QualifierCompatibility qualifierCompatibility => $"operands share {qualifierCompatibility.Axis.ToString().ToLowerInvariant()} qualifiers",
            _ => throw new ArgumentOutOfRangeException(nameof(proofSatisfaction), proofSatisfaction, null),
        };

    private static string RenderProjection(SatisfactionProjection projection)
        => projection switch
        {
            SatisfactionProjection.SelfValue _ => "self",
            SatisfactionProjection.Accessor accessor => $"self.{accessor.Name}",
            _ => throw new ArgumentOutOfRangeException(nameof(projection), projection, null),
        };

    private static string RenderBoundSource(NumericBoundSource bound)
        => bound switch
        {
            NumericBoundSource.Constant constant => constant.Value.ToString(CultureInfo.InvariantCulture),
            NumericBoundSource.DeclarationValue _ => "declaration value",
            _ => throw new ArgumentOutOfRangeException(nameof(bound), bound, null),
        };

    private static string RenderDimensionSource(DimensionSource source)
        => source switch
        {
            DimensionSource.Constant constant => constant.Value.ToString().ToLowerInvariant(),
            DimensionSource.DeclaredTemporalDimension _ => "declared",
            _ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
        };

    private static string RenderCompatibleSubjects(ProofSubject left, ProofSubject right)
    {
        var leftText = RenderProofSubject(left);
        var rightText = RenderProofSubject(right);
        return leftText == rightText ? "operands" : $"{leftText} and {rightText}";
    }

    private static string RenderProofSubject(ProofSubject subject)
        => subject switch
        {
            ParamSubject parameter => parameter.Parameter.Name ?? parameter.Parameter.Kind.ToString().ToLowerInvariant(),
            SelfSubject self => self.Accessor is null ? "self" : $"self.{self.Accessor.Name}",
            _ => throw new ArgumentOutOfRangeException(nameof(subject), subject, null),
        };

    private static string RenderComparison(OperatorKind comparison)
        => comparison switch
        {
            OperatorKind.Equals => "==",
            OperatorKind.NotEquals => "!=",
            OperatorKind.LessThan => "<",
            OperatorKind.GreaterThan => ">",
            OperatorKind.LessThanOrEqual => "<=",
            OperatorKind.GreaterThanOrEqual => ">=",
            _ => comparison.ToString(),
        };

    private static string[] RenderOperatorTokens(OperatorMeta op)
        => op switch
        {
            SingleTokenOp single => [RenderToken(single.Token.Kind)],
            MultiTokenOp multi => multi.Tokens.Select(token => RenderToken(token.Kind)).ToArray(),
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null),
        };

    private static string[] ExpandFlags<TEnum>(TEnum value)
        where TEnum : struct, Enum
        => Enum.GetValues<TEnum>()
            .Where(flag => Convert.ToInt64(flag) != 0 && value.HasFlag(flag))
            .Select(flag => flag.ToString())
            .ToArray();

    private static string RenderModifier(ModifierKind kind)
        => Modifiers.GetMeta(kind).Token.Text ?? kind.ToString();

    private static string RenderToken(TokenKind kind)
        => Tokens.GetMeta(kind).Text ?? kind.ToString();
}
