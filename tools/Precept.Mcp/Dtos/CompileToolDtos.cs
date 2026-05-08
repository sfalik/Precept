namespace Precept.Mcp.Dtos;

// ════════════════════════════════════════════════════════════════════════════
//  DTOs for precept_compile tool output
//  These are serialization-only shapes — no domain logic.
//  Mapping from SemanticIndex types to these DTOs happens in the tool handler.
// ════════════════════════════════════════════════════════════════════════════

/// <summary>
/// A field declaration projected for MCP output.
/// Maps from <c>TypedField</c> in <c>SemanticIndex</c>.
/// </summary>
/// <param name="Name">Field identifier as declared in the precept.</param>
/// <param name="TypeName">Resolved type name (e.g., "money", "string", "choice").</param>
/// <param name="IsOptional">Whether the field carries the <c>optional</c> modifier.</param>
/// <param name="DefaultExpression">
/// Text representation of the field's default expression, or <c>null</c> if no default is declared.
/// Derived from <c>TypedField.DefaultExpression</c> via expression rendering.
/// </param>
/// <param name="ComputedExpression">
/// Text representation of the field's computed expression, or <c>null</c> if the field is not computed.
/// Derived from <c>TypedField.ComputedExpression</c> via expression rendering.
/// </param>
/// <param name="Qualifier">
/// Qualifier binding text (e.g., <c>"in 'USD'"</c>), or <c>null</c> if no qualifier is bound.
/// Derived from <c>TypedField.Qualifier</c>.
/// </param>
public sealed record PreceptFieldDto(
    string Name,
    string TypeName,
    bool IsOptional,
    string? DefaultExpression,
    string? ComputedExpression,
    string? Qualifier
);

/// <summary>
/// An ensure (state- or event-anchored constraint) projected for MCP output.
/// Maps from <c>TypedEnsure</c> in <c>SemanticIndex</c>.
/// </summary>
/// <param name="Kind">
/// The constraint kind as a string (e.g., "StateResident", "EventPrecondition").
/// Derived from <c>ConstraintKind</c> enum.
/// </param>
/// <param name="Anchor">
/// What the ensure is anchored to — a state name, event name, or <c>"global"</c>
/// when neither <c>AnchorState</c> nor <c>AnchorEvent</c> is set.
/// </param>
/// <param name="Expression">Text representation of the constraint condition expression.</param>
/// <param name="Because">The reason string from the <c>because</c> clause, or <c>null</c> if omitted.</param>
/// <param name="Guard">Text representation of the guard expression, or <c>null</c> if no guard is declared.</param>
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
/// <param name="StateName">The state in which this access mode applies.</param>
/// <param name="FieldName">The field whose access is being controlled.</param>
/// <param name="Mode">
/// The access mode as a string (e.g., "ReadOnly", "Writable", "Hidden").
/// Derived from <c>ModifierKind</c> enum on the <c>TypedAccessMode.Mode</c> property.
/// </param>
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
/// <param name="StateName">The state this hook is attached to.</param>
/// <param name="Kind">
/// The hook scope as a string (e.g., "OnEntry", "OnExit", "InState").
/// Derived from <c>AnchorScope</c> enum on the <c>TypedStateHook.Scope</c> property.
/// </param>
/// <param name="Actions">
/// Text representations of the actions in the hook's action chain,
/// or an empty array if no actions are declared.
/// </param>
public sealed record StateHookDto(
    string StateName,
    string Kind,
    string[] Actions
);
