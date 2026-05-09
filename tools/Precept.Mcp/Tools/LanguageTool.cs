using System.ComponentModel;
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

    [McpServerTool(Name = "precept_language")]
    [Description("Return the complete Precept DSL vocabulary derived from language catalogs.")]
    public static LanguageReferenceDto Language()
        => new(
            Tokens.All.Select(MapToken).ToArray(),
            Types.All.Select(MapType).ToArray(),
            new ModifierCatalogDto(
                Modifiers.All.OfType<FieldModifierMeta>().Select(MapFieldModifier).ToArray(),
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
            new DomainCatalogDto(
                CurrencyCatalog.All.Values.OrderBy(entry => entry.AlphaCode).Select(MapCurrency).ToArray(),
                UcumCatalog.All.Values.OrderBy(entry => entry.Code).Select(MapUcumTier1Unit).ToArray(),
                DimensionCatalog.All.Values.OrderBy(entry => entry.Name).Select(MapDimension).ToArray(),
                TemporalUnits.AllEntries.Select(MapTemporalUnit).ToArray()),
            FirePipeline);

    private static TokenCatalogEntryDto MapToken(TokenMeta token)
        => new(
            token.Kind.ToString(),
            token.Text,
            token.Categories.Select(category => category.ToString()).ToArray(),
            token.Description,
            token.TextMateScope,
            token.SemanticTokenType,
            (token.ValidAfter ?? []).Select(previous => previous.ToString()).ToArray(),
            token.IsAccessModeAdjective,
            token.IsValidAsMemberName,
            token.IsMessagePosition);

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
            (type.ChoiceLiteralTokens ?? []).Select(kind => kind.ToString()).ToArray());

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
                fixedReturn.ReturnsQualifier == QualifierAxis.None ? null : fixedReturn.ReturnsQualifier.ToString()),
            ElementParameterAccessor elementParameter => new(
                elementParameter.Name,
                elementParameter.Description,
                null,
                true,
                null,
                true,
                ExpandFlags(elementParameter.RequiredTraits),
                null),
            _ => new(
                accessor.Name,
                accessor.Description,
                null,
                true,
                accessor.ParameterType?.ToString(),
                false,
                ExpandFlags(accessor.RequiredTraits),
                null),
        };

    private static FieldModifierCatalogEntryDto MapFieldModifier(FieldModifierMeta modifier)
        => new(
            modifier.Kind.ToString(),
            modifier.Token.Text ?? modifier.Kind.ToString(),
            modifier.Description,
            modifier.Category.ToString(),
            MapTargets(modifier.ApplicableTo),
            modifier.HasValue,
            modifier.Subsumes.Select(RenderModifier).ToArray(),
            modifier.DesugarsToRule,
            modifier.MutuallyExclusiveWith.Select(RenderModifier).ToArray());

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
            action.IntoSupported,
            action.PrimaryActionKind?.ToString());

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
        var tokens = op switch
        {
            SingleTokenOp single => [RenderToken(single.Token.Kind)],
            MultiTokenOp multi => multi.Tokens.Select(token => RenderToken(token.Kind)).ToArray(),
            _ => throw new ArgumentOutOfRangeException(nameof(op), op, null),
        };

        return new(
            op.Kind.ToString(),
            string.Join(" ", tokens),
            tokens,
            op.Arity.ToString(),
            op.Associativity.ToString(),
            op.Precedence,
            op.Family.ToString(),
            op.IsKeywordOperator,
            op.Description);
    }

    private static FunctionCatalogEntryDto MapFunction(FunctionMeta function)
        => new(
            function.Kind.ToString(),
            function.Name,
            function.Category.ToString(),
            function.Description,
            function.Overloads.Select(MapOverload).ToArray(),
            function.HasCIVariant,
            function.CIVariantOf?.ToString(),
            function.CIDiagnosticCode?.ToString());

    private static FunctionOverloadDto MapOverload(FunctionOverload overload)
        => new(
            overload.Parameters.Select(parameter => new FunctionParameterDto(parameter.Name, parameter.Kind.ToString())).ToArray(),
            overload.ReturnType.ToString(),
            overload.Match?.ToString() ?? QualifierMatch.Any.ToString());

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
            diagnostic.SuggestionSources?.Select(source => source.ToString()).ToArray() ?? []);

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
