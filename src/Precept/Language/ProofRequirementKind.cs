namespace Precept.Language;

/// <summary>
/// The eleven proof obligation kinds that catalog entries can declare.
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

    /// <summary>Key presence check — collection must contain (or not contain) a specific key before access/mutation.</summary>
    KeyPresence            = 10,

    /// <summary>Index bounds check — parameter (an index N) must satisfy 0 &lt;= N &lt; F.count (or 0 &lt;= N &lt;= F.count for inserts).</summary>
    IndexBounds            = 11,

    /// <summary>
    /// Dimensional product check — the multiplicative product of two operand
    /// unit-dimension vectors must resolve to a known curated business-domain
    /// dimension (length, mass, volume, area, temperature, energy, pressure,
    /// force, speed, count). Used by <c>QuantityTimesQuantity</c> to reject
    /// products outside the curated set (e.g., <c>kg × m</c> is `mass·length`,
    /// not a business-domain dimension).
    /// </summary>
    DimensionalProduct     = 12,

    /// <summary>Assignment qualifier compatibility — an open field assigned to a qualified target must, under a guard, narrow to the target's qualifier value on the axis (the relocated PRE0141 open-field case; discharged at the proof stage).</summary>
    AssignmentQualifier    = 13,
}
