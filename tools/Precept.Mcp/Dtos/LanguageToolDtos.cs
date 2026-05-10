namespace Precept.Mcp.Dtos;

public sealed record LanguageReferenceDto(
    TokenCatalogEntryDto[] Tokens,
    TypeCatalogEntryDto[] Types,
    ModifierCatalogDto Modifiers,
    ActionCatalogEntryDto[] Actions,
    ConstructCatalogEntryDto[] Constructs,
    ConstraintCatalogEntryDto[] Constraints,
    OperatorCatalogEntryDto[] Operators,
    FunctionCatalogEntryDto[] Functions,
    DiagnosticCatalogEntryDto[] Diagnostics,
    OperationDto[] Operations,
    OutcomeDto[] Outcomes,
    SyntaxReferenceDto SyntaxReference,
    DomainCatalogDto Domains,
    string[] FirePipeline
);

public sealed record DomainCatalogDto(
    CurrencyDomainEntryDto[] Currencies,
    UcumTier1UnitDto[] UcumTier1Units,
    DimensionDomainEntryDto[] Dimensions,
    TemporalUnitDomainEntryDto[] TemporalUnits
);

public sealed record CurrencyDomainEntryDto(
    string AlphaCode,
    int NumericCode,
    string Name,
    int MinorUnit,
    string Symbol
);

public sealed record UcumTier1UnitDto(
    string Code,
    string Name,
    DimensionVectorDto Dimension,
    string? DimensionName,
    UcumExactFactorDto Scale,
    bool Prefixable,
    string? AnnotationClass
);

public sealed record DimensionDomainEntryDto(
    string Name,
    DimensionVectorDto Dimension,
    string Description
);

public sealed record TemporalUnitDomainEntryDto(
    string Singular,
    string Plural,
    bool IsCalendarBased,
    bool IsPeriod,
    bool IsDuration
);

public sealed record DimensionVectorDto(
    int Length,
    int Mass,
    int Time,
    int ElectricCurrent,
    int Temperature,
    int AmountOfSubstance,
    int LuminousIntensity
);

public sealed record UcumExactFactorDto(
    string Numerator,
    string Denominator,
    int Base10Exponent
);

public sealed record TokenCatalogEntryDto(
    string Kind,
    string? Text,
    string[] Categories,
    string Description,
    string? TextMateScope,
    string? SemanticTokenType,
    string[] ValidAfter,
    bool IsAccessModeAdjective,
    bool IsValidAsMemberName,
    bool IsMessagePosition
);

public sealed record TypeCatalogEntryDto(
    string Kind,
    string? Keyword,
    string Description,
    string Category,
    string DisplayName,
    string[] Traits,
    string[] WidensTo,
    string[] ImpliedModifiers,
    QualifierShapeDto? QualifierShape,
    TypeAccessorDto[] Accessors,
    string[] ChoiceLiteralTokens,
    string? HoverDescription,
    string? UsageExample,
    bool NotemptyApplicable,
    ContentValidationDto? ContentValidation
);

public sealed record QualifierShapeDto(
    QualifierSlotDto[] Axes,
    bool InOfExclusive
);

public sealed record QualifierSlotDto(
    string Preposition,
    string Axis
);

public sealed record TypeAccessorDto(
    string Name,
    string Description,
    string? ReturnType,
    bool ReturnsElementType,
    string? ParameterType,
    bool UsesElementParameter,
    string[] RequiredTraits,
    string? ReturnsQualifier,
    string[]? ProofRequirements
);

public sealed record ContentValidationDto(
    string Kind,
    string FormatDescription,
    string[] Examples,
    string? NodaTimePattern,
    string? LiteralKind,
    string? SetName,
    string[]? AllowedValues
);

public sealed record ModifierCatalogDto(
    ValueModifierCatalogEntryDto[] Value,
    StateModifierCatalogEntryDto[] State,
    EventModifierCatalogEntryDto[] Event,
    AccessModifierCatalogEntryDto[] Access,
    AnchorModifierCatalogEntryDto[] Anchor
);

public sealed record ModifierTargetDto(
    string? Type,
    bool AnyType,
    string[] RequiredModifiers
);

public sealed record ValueModifierCatalogEntryDto(
    string Kind,
    string Keyword,
    string Description,
    string Category,
    ModifierTargetDto[] ApplicableTypes,
    string[] ApplicableDeclarationSites,
    bool HasValue,
    string[] Subsumes,
    bool DesugarsToRule,
    string[] MutuallyExclusiveWith,
    string[]? ProofSatisfactions,
    string? HoverDescription,
    string? UsageExample,
    string? SnippetTemplate
);

public sealed record StateModifierCatalogEntryDto(
    string Kind,
    string Keyword,
    string Description,
    string Category,
    bool AllowsOutgoing,
    bool RequiresDominator,
    bool PreventsBackEdge,
    bool DesugarsToRule,
    string[] MutuallyExclusiveWith
);

public sealed record EventModifierCatalogEntryDto(
    string Kind,
    string Keyword,
    string Description,
    string Category,
    string RequiredAnalysis,
    bool DesugarsToRule
);

public sealed record AccessModifierCatalogEntryDto(
    string Kind,
    string Keyword,
    string Description,
    string Category,
    bool IsPresent,
    bool IsWritable,
    bool DesugarsToRule,
    string[] MutuallyExclusiveWith
);

public sealed record AnchorModifierCatalogEntryDto(
    string Kind,
    string Keyword,
    string Description,
    string Category,
    string Scope,
    string Target,
    bool DesugarsToRule
);

public sealed record ActionCatalogEntryDto(
    string Kind,
    string Keyword,
    string Description,
    ModifierTargetDto[] ApplicableTo,
    string[] AllowedIn,
    string SyntaxShape,
    bool ValueRequired,
    bool IntoSupported,
    string? PrimaryActionKind,
    string[]? ProofRequirements,
    string? HoverDescription,
    string? UsageExample,
    string? SnippetTemplate
);

public sealed record ConstructCatalogEntryDto(
    string Kind,
    string Name,
    string Description,
    string UsageExample,
    string PrimaryLeadingToken,
    string[] AllowedIn,
    ConstructSlotDto[] Slots,
    DisambiguationEntryDto[] Entries,
    string RoutingFamily,
    string ModifierDomain,
    string? SnippetTemplate
);

public sealed record ConstructSlotDto(
    string Kind,
    bool IsRequired,
    string? Description,
    string[] TerminationTokens
);

public sealed record DisambiguationEntryDto(
    string LeadingToken,
    string[] DisambiguationTokens,
    string? LeadingTokenSlot
);

public sealed record ConstraintCatalogEntryDto(
    string Kind,
    string Description,
    string Scope,
    string[] Tokens
);

public sealed record OperatorCatalogEntryDto(
    string Kind,
    string Text,
    string[] Tokens,
    string Arity,
    string Associativity,
    int Precedence,
    string Family,
    bool IsKeywordOperator,
    string Description,
    string? HoverDescription,
    string? UsageExample
);

public sealed record OperationDto(
    string Kind,
    string Operator,
    string LhsType,
    string RhsType,
    string ResultType,
    string Description,
    string QualifierMatch,
    object? ProofRequirements,
    bool HasCIVariant,
    string? CIDiagnosticCode,
    bool BidirectionalLookup
);

public sealed record OutcomeDto(
    string Kind,
    string LeadingToken,
    string ArgumentKind,
    string Description,
    string Example
);

public sealed record SyntaxReferenceDto(
    string GrammarModel,
    string CommentSyntax,
    string IdentifierRules,
    string StringLiteralRules,
    string NumberLiteralRules,
    string WhitespaceRules,
    string NullNarrowing,
    string TypedConstantRules,
    string ExpressionRules,
    string[] PrecedenceTable,
    CommonPatternDto[] CommonPatterns,
    AntiPatternDto[] AntiPatterns,
    string[] ConventionalOrder
);

public sealed record CommonPatternDto(
    string Name,
    string Description,
    string DslSnippet
);

public sealed record AntiPatternDto(
    string Name,
    string Description,
    string BadSnippet,
    string GoodSnippet,
    string WhyItFails
);

public sealed record FunctionCatalogEntryDto(
    string Kind,
    string Name,
    string Category,
    string Description,
    FunctionOverloadDto[] Overloads,
    bool HasCaseInsensitiveVariant,
    string? CaseInsensitiveVariantOf,
    string? CaseInsensitiveDiagnosticCode,
    string? UsageExample,
    string? SnippetTemplate,
    string? HoverDescription,
    bool IsMessagePosition
);

public sealed record FunctionOverloadDto(
    FunctionParameterDto[] Parameters,
    string ReturnType,
    string QualifierMatch,
    string[]? ProofRequirements
);

public sealed record FunctionParameterDto(
    string? Name,
    string Type
);

public sealed record DiagnosticCatalogEntryDto(
    string Code,
    string Stage,
    string Severity,
    string Category,
    string MessageTemplate,
    string[] RelatedCodes,
    string? FixHint,
    string? PreventsFault,
    string[] SuggestionSources,
    string? TriggerCondition,
    string[] RecoverySteps,
    string? ExampleBefore,
    string? ExampleAfter
);
