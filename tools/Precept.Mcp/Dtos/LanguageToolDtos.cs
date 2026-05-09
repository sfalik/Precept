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
    string[] FirePipeline
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
    string[] ChoiceLiteralTokens
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
    string? ReturnsQualifier
);

public sealed record ModifierCatalogDto(
    FieldModifierCatalogEntryDto[] Field,
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

public sealed record FieldModifierCatalogEntryDto(
    string Kind,
    string Keyword,
    string Description,
    string Category,
    ModifierTargetDto[] ApplicableTo,
    bool HasValue,
    string[] Subsumes,
    bool DesugarsToRule,
    string[] MutuallyExclusiveWith
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
    string? PrimaryActionKind
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
    string Description
);

public sealed record FunctionCatalogEntryDto(
    string Kind,
    string Name,
    string Category,
    string Description,
    FunctionOverloadDto[] Overloads,
    bool HasCaseInsensitiveVariant,
    string? CaseInsensitiveVariantOf,
    string? CaseInsensitiveDiagnosticCode
);

public sealed record FunctionOverloadDto(
    FunctionParameterDto[] Parameters,
    string ReturnType,
    string QualifierMatch
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
    string[] SuggestionSources
);
