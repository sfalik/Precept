namespace Precept.Language;

/// <summary>
/// Catalog of constraint declaration forms. Source of truth for the plan router,
/// evaluator, LS completions/hover, and MCP vocabulary.
/// </summary>
public static class Constraints
{
    // ════════════════════════════════════════════════════════════════════════════
    //  GetMeta — exhaustive switch
    // ════════════════════════════════════════════════════════════════════════════

    public static ConstraintMeta GetMeta(ConstraintKind kind) => kind switch
    {
        ConstraintKind.Invariant         => new ConstraintMeta.Invariant(),
        ConstraintKind.StateResident     => new ConstraintMeta.StateResident(),
        ConstraintKind.StateEntry        => new ConstraintMeta.StateEntry(),
        ConstraintKind.StateExit         => new ConstraintMeta.StateExit(),
        ConstraintKind.EventPrecondition => new ConstraintMeta.EventPrecondition(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind,
            $"Unknown ConstraintKind: {kind}"),
    };

    // ════════════════════════════════════════════════════════════════════════════
    //  All — every ConstraintMeta in declaration order
    // ════════════════════════════════════════════════════════════════════════════

    public static IReadOnlyList<ConstraintMeta> All { get; } =
        Enum.GetValues<ConstraintKind>().Select(GetMeta).ToArray();
}
