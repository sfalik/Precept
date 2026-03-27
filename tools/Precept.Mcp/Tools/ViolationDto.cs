namespace Precept.Mcp.Tools;

/// <summary>
/// Shared violation DTO used by inspect, fire, and update tools.
/// Projects the core <see cref="ConstraintViolation"/> hierarchy to a flat JSON-friendly shape.
/// </summary>
public sealed record ViolationDto(
    string Message,
    ViolationSourceDto Source,
    IReadOnlyList<ViolationTargetDto> Targets);

public sealed record ViolationSourceDto(
    string Kind,
    string? StateName,
    string? Anchor,
    string? EventName,
    string? ExpressionText,
    string? Reason,
    int? SourceLine);

public sealed record ViolationTargetDto(
    string Kind,
    string? FieldName,
    string? EventName,
    string? ArgName,
    string? StateName,
    string? Anchor);

internal static class ViolationDtoMapper
{
    public static ViolationDto Map(ConstraintViolation violation)
    {
        return new ViolationDto(
            violation.Message,
            MapSource(violation.Source),
            violation.Targets.Select(MapTarget).ToList());
    }

    private static ViolationSourceDto MapSource(ConstraintSource source) => source switch
    {
        ConstraintSource.InvariantSource inv =>
            new ViolationSourceDto("invariant", null, null, null, inv.ExpressionText, inv.Reason, inv.SourceLine),
        ConstraintSource.StateAssertionSource sa =>
            new ViolationSourceDto("state-assertion", sa.StateName, sa.Anchor.ToString().ToLowerInvariant(), null, sa.ExpressionText, sa.Reason, sa.SourceLine),
        ConstraintSource.EventAssertionSource ea =>
            new ViolationSourceDto("event-assertion", null, null, ea.EventName, ea.ExpressionText, ea.Reason, ea.SourceLine),
        ConstraintSource.TransitionRejectionSource tr =>
            new ViolationSourceDto("transition-rejection", null, null, tr.EventName, null, tr.Reason, tr.SourceLine),
        _ => new ViolationSourceDto("unknown", null, null, null, null, null, source.SourceLine)
    };

    private static ViolationTargetDto MapTarget(ConstraintTarget target) => target switch
    {
        ConstraintTarget.FieldTarget ft =>
            new ViolationTargetDto("field", ft.FieldName, null, null, null, null),
        ConstraintTarget.EventArgTarget ea =>
            new ViolationTargetDto("event-arg", null, ea.EventName, ea.ArgName, null, null),
        ConstraintTarget.EventTarget et =>
            new ViolationTargetDto("event", null, et.EventName, null, null, null),
        ConstraintTarget.StateTarget st =>
            new ViolationTargetDto("state", null, null, null, st.StateName, st.Anchor?.ToString().ToLowerInvariant()),
        ConstraintTarget.DefinitionTarget =>
            new ViolationTargetDto("definition", null, null, null, null, null),
        _ => new ViolationTargetDto("unknown", null, null, null, null, null)
    };
}
