namespace Precept.Mcp.Dtos;

// ── QuickstartTool DTOs ──────────────────────────────────────────────────────

public sealed record QuickstartDto(
    string WhatIsPrecept,
    string CoreGuarantee,
    CoreConceptDto[] CoreConcepts,
    ToolGuideDto[] ToolGuide,
    MinimalExampleDto[] MinimalExamples
);

public sealed record CoreConceptDto(
    string Name,
    string Summary,
    string Example
);

public sealed record ToolGuideDto(
    string ToolName,
    string WhenToCall,
    string ReturnsSummary
);

public sealed record MinimalExampleDto(
    string Title,
    string Description,
    string DslSnippet
);

// ── SyntaxTool DTOs ──────────────────────────────────────────────────────────

public sealed record SyntaxDto(
    ConstructCatalogEntryDto[] Constructs,
    ActionCatalogEntryDto[] Actions,
    OutcomeDto[] Outcomes,
    OperatorCatalogEntryDto[] Operators,
    SyntaxReferenceDto SyntaxReference
);

// ── TypesTool DTOs ───────────────────────────────────────────────────────────

public sealed record TypesDto(
    TypeCatalogEntryDto[] Types,
    ModifierCatalogDto Modifiers,
    FunctionCatalogEntryDto[] Functions
);

// ── OperationsTool DTOs ──────────────────────────────────────────────────────

public sealed record OperationsResultDto(
    string[] Categories,
    OperationDto[] Operations,
    int Count,
    string? FilteredByCategory
);

// ── ProofsTool DTOs ──────────────────────────────────────────────────────────

public sealed record ProofsDto(
    ProofRequirementMetaDto[] ProofRequirements,
    FaultMetaDto[] RuntimeFaults
);

public sealed record ProofRequirementMetaDto(
    string Kind,
    string Description,
    bool IsDualSubject
);

public sealed record FaultMetaDto(
    string Code,
    string MessageTemplate,
    string Severity,
    string? RecoveryHint
);

// ── PatternsTool DTOs ────────────────────────────────────────────────────────

public sealed record PatternsDto(
    CommonPatternDto[] CommonPatterns,
    AntiPatternDto[] AntiPatterns
);

// ── DiagnosticTool DTOs ──────────────────────────────────────────────────────

public sealed record DiagnosticLookupResultDto(
    bool Found,
    DiagnosticCatalogEntryDto? Diagnostic,
    string? Error
);

// ── DomainsTool DTOs ─────────────────────────────────────────────────────────

public sealed record DomainsDto(
    CurrencyDomainEntryDto[] Currencies,
    UcumTier1UnitDto[] UcumTier1Units,
    UcumPrefixDto[] UcumPrefixes,
    DimensionDomainEntryDto[] Dimensions
);

public sealed record UcumPrefixDto(
    string Code,
    string Name,
    string Numerator,
    string Denominator,
    int Base10Exponent
);
