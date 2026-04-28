namespace Precept.Language;

/// <summary>
/// Catalog of proof obligation kinds. Source of truth for the proof engine,
/// type checker, Roslyn analyzers, and MCP vocabulary.
/// </summary>
public static class ProofRequirements
{
    // ════════════════════════════════════════════════════════════════════════════
    //  GetMeta — exhaustive switch
    // ════════════════════════════════════════════════════════════════════════════

    public static ProofRequirementMeta GetMeta(ProofRequirementKind kind) => kind switch
    {
        ProofRequirementKind.Numeric               => new ProofRequirementMeta.Numeric(),
        ProofRequirementKind.Presence              => new ProofRequirementMeta.Presence(),
        ProofRequirementKind.Dimension             => new ProofRequirementMeta.Dimension(),
        ProofRequirementKind.Modifier              => new ProofRequirementMeta.Modifier(),
        ProofRequirementKind.QualifierCompatibility => new ProofRequirementMeta.QualifierCompatibility(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind,
            $"Unknown ProofRequirementKind: {kind}"),
    };

    // ════════════════════════════════════════════════════════════════════════════
    //  All — every ProofRequirementMeta in declaration order
    // ════════════════════════════════════════════════════════════════════════════

    public static IReadOnlyList<ProofRequirementMeta> All { get; } =
        Enum.GetValues<ProofRequirementKind>().Select(GetMeta).ToArray();
}
