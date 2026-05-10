using System.Text.Json.Serialization;

namespace Precept.Mcp.Dtos;

// ════════════════════════════════════════════════════════════════════════════
//  DTOs for precept_compile tool output
//  These are serialization-only shapes — no domain logic.
//  Mapping from SemanticIndex types to these DTOs happens in the tool handler.
// ════════════════════════════════════════════════════════════════════════════

public sealed record CompileResultDto(
    bool HasErrors,
    DiagnosticDto[] Diagnostics,
    PreceptDefinitionDto? Definition
);

public sealed record DiagnosticDto(
    string Code,
    string Severity,
    string Message,
    DiagnosticLocationDto Location
);

public sealed record DiagnosticLocationDto(
    int Line,
    int Column,
    int Length
);

public sealed record PreceptDefinitionDto(
    string Name,
    bool IsStateless,
    PreceptFieldDto[] Fields,
    PreceptStateDto[] States,
    PreceptEventDto[] Events,
    PreceptRuleDto[] Rules,
    StateHookDto[] StateHooks
);

/// <summary>
/// A field declaration projected for MCP output.
/// Maps from <c>TypedField</c> in <c>SemanticIndex</c>.
/// </summary>
public sealed record PreceptFieldDto(
    string Name,
    [property: JsonPropertyName("type")] string TypeName,
    bool IsOptional,
    bool IsWritable,
    string[] Modifiers,
    [property: JsonPropertyName("defaultValue")] string? DefaultExpression,
    string? ComputedExpression,
    string? Qualifier,
    string? ChoiceElementType,
    string[]? ChoiceValues
);

public sealed record PreceptStateDto(
    string Name,
    string[] Modifiers,
    EnsureDto[] Constraints,
    string[]? OmittedFields,
    AccessModeDto[]? AccessModes
);

public sealed record PreceptEventDto(
    string Name,
    EventArgDto[] Args,
    TransitionRowDto[] Rows,
    EnsureDto[]? Constraints
);

public sealed record EventArgDto(
    string Name,
    string Type,
    bool IsOptional
);

public sealed record TransitionRowDto(
    string[] FromStates,
    string? Guard,
    string[] Actions,
    string? ToState,
    string? Outcome,
    string? RejectMessage
);

public sealed record PreceptRuleDto(
    string Expression,
    string? Because,
    string? When
);

/// <summary>
/// An ensure (state- or event-anchored constraint) projected for MCP output.
/// Maps from <c>TypedEnsure</c> in <c>SemanticIndex</c>.
/// </summary>
public sealed record EnsureDto(
    string Kind,
    string Anchor,
    string Expression,
    string? Because,
    string? Guard
);

/// <summary>
/// An access-mode declaration projected for MCP output.
/// Maps from <c>TypedAccessMode</c> in <c>SemanticIndex</c>.
/// Controls field visibility/writability per state.
/// </summary>
public sealed record AccessModeDto(
    string StateName,
    string FieldName,
    string Mode
);

/// <summary>
/// A state lifecycle hook projected for MCP output.
/// Maps from <c>TypedStateHook</c> in <c>SemanticIndex</c>.
/// Represents on-entry or on-exit action chains attached to a state.
/// </summary>
public sealed record StateHookDto(
    string StateName,
    string Kind,
    string[] Actions
);
