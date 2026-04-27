namespace Precept.Language;

/// <summary>
/// The five proof obligation kinds that catalog entries can declare.
/// </summary>
public enum ProofRequirementKind
{
    // ── Single-subject ──────────────────────────────────────────────────
    /// <summary>Numeric interval check — value comparison against threshold (e.g. divisor != 0).</summary>
    Numeric,

    /// <summary>Presence check — optional field must be set before access.</summary>
    Presence,

    /// <summary>Dimension check — period operand must have required time dimension.</summary>
    Dimension,

    /// <summary>Modifier check — field must declare a required modifier (e.g. <c>ordered</c>).</summary>
    Modifier,

    // ── Dual-subject ────────────────────────────────────────────────────
    /// <summary>Qualifier axis compatibility — two operands must share a qualifier value (e.g. same currency).</summary>
    QualifierCompatibility,
}
