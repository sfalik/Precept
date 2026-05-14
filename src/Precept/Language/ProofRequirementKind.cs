namespace Precept.Language;

/// <summary>
/// The five proof obligation kinds that catalog entries can declare.
/// </summary>
public enum ProofRequirementKind
{
    // ── Single-subject ──────────────────────────────────────────────────
    /// <summary>Numeric interval check — value comparison against threshold (e.g. divisor != 0).</summary>
    Numeric                =  1,

    /// <summary>Presence check — optional field must be set before access.</summary>
    Presence               =  2,

    /// <summary>Dimension check — period operand must have required time dimension.</summary>
    Dimension              =  3,

    /// <summary>Modifier check — field must declare a required modifier (e.g. <c>ordered</c>).</summary>
    Modifier               =  4,

    // ── Dual-subject ────────────────────────────────────────────────────
    /// <summary>Qualifier axis compatibility — two operands must share a qualifier value (e.g. same currency).</summary>
    QualifierCompatibility =  5,

    QualifierChain         =  6,

    /// <summary>Interval containment check — result interval must fit within target field's declared bounds.</summary>
    IntervalContainment    =  7,

    /// <summary>Length containment check — assigned string's length must fit within the field's declared minlength/maxlength.</summary>
    LengthContainment      =  8,

    /// <summary>Count containment check — collection element count must fit within the field's declared mincount/maxcount.</summary>
    CountContainment       =  9,
}
