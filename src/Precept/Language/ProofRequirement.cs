namespace Precept.Language;

// ════════════════════════════════════════════════════════════════════════════════
//  ProofSubject — identifies what a proof obligation targets
// ════════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Identifies the subject of a proof obligation — what must be proven safe
/// before an operation, function, accessor, or action can execute.
/// </summary>
public abstract record ProofSubject;

/// <summary>
/// References a parameter by object identity. Must be reference-equal to one
/// of the <see cref="ParameterMeta"/> instances in the containing overload's
/// <c>Parameters</c> list. Enforced by Roslyn analyzer (PRECEPT0005).
/// </summary>
public sealed record ParamSubject(ParameterMeta Parameter) : ProofSubject;

/// <summary>
/// References the receiver of an accessor or action.
/// <c>Accessor = null</c> means "the field itself" — used for
/// <see cref="PresenceProofRequirement"/>.
/// </summary>
public sealed record SelfSubject(TypeAccessor? Accessor = null) : ProofSubject;

// ════════════════════════════════════════════════════════════════════════════════
//  ProofRequirement — what must be proven (instance obligation values)
// ════════════════════════════════════════════════════════════════════════════════

/// <summary>
/// A proof obligation declared by a catalog entry. The proof engine reads
/// these from catalog metadata — no hardcoded obligation lists.
///
/// The base carries <see cref="Kind"/> (catalog membership identity) and
/// <see cref="Description"/>. Each subtype carries its own subject field(s) —
/// single-subject requirements expose <c>Subject</c>; dual-subject requirements
/// expose <c>LeftSubject</c> and <c>RightSubject</c>. Use <see cref="ProofRequirements.GetMeta"/>
/// to look up static kind metadata.
/// </summary>
public abstract record ProofRequirement(ProofRequirementKind Kind, string Description);

/// <summary>
/// Numeric interval proof: the subject's value compared with
/// <see cref="Comparison"/> against <see cref="Threshold"/> must hold.
/// Examples: divisor != 0, sqrt operand >= 0, collection count > 0.
/// </summary>
public sealed record NumericProofRequirement(
    ProofSubject Subject,
    OperatorKind Comparison,
    decimal      Threshold,
    string       Description
) : ProofRequirement(ProofRequirementKind.Numeric, Description);

/// <summary>
/// Presence proof: an optional field must be set before access.
/// The subject's <see cref="SelfSubject.Accessor"/> is null (the field itself).
/// </summary>
public sealed record PresenceProofRequirement(
    ProofSubject Subject,
    string       Description
) : ProofRequirement(ProofRequirementKind.Presence, Description);

/// <summary>Time dimension for <see cref="DimensionProofRequirement"/>.</summary>
public enum PeriodDimension
{
    /// <summary>Any time dimension is acceptable.</summary>
    Any,
    /// <summary>Must be a date-level dimension (year, month, day).</summary>
    Date,
    /// <summary>Must be a time-level dimension (hour, minute, second).</summary>
    Time,
}

/// <summary>
/// Dimension proof: a period operand must have a specific time dimension.
/// Valid only on <c>BinaryOperationMeta.ProofRequirements</c>.
/// Enforced by Roslyn analyzer (PRECEPT0006).
/// </summary>
public sealed record DimensionProofRequirement(
    ProofSubject    Subject,
    PeriodDimension RequiredDimension,
    string          Description
) : ProofRequirement(ProofRequirementKind.Dimension, Description);

/// <summary>
/// Qualifier compatibility proof: two operands in a binary operation must have
/// matching qualifier values on the specified axis. Used for dimensional
/// homogeneity checks — e.g., <c>quantity of 'kg'</c> cannot be added to
/// <c>quantity of 'miles'</c>.
///
/// Dual-subject: carries both <see cref="LeftSubject"/> and <see cref="RightSubject"/>
/// independently rather than a single <c>Subject</c>.
/// </summary>
public sealed record QualifierCompatibilityProofRequirement(
    ProofSubject  LeftSubject,
    ProofSubject  RightSubject,
    QualifierAxis Axis,
    string        Description
) : ProofRequirement(ProofRequirementKind.QualifierCompatibility, Description);

/// <summary>
/// Modifier proof: the operand(s) matching <see cref="Subject"/> must have
/// the specified modifier declared on their field. Used to enforce that
/// operations requiring a field-level attribute (e.g. <c>ordered</c> for
/// choice ordinal comparison) are only applied to appropriately declared fields.
/// When both operands share the same <see cref="ParameterMeta"/> reference
/// (as with choice ordering operations), the requirement applies to all
/// matching operand positions.
/// </summary>
public sealed record ModifierRequirement(
    ProofSubject Subject,
    ModifierKind Required,
    string       Description
) : ProofRequirement(ProofRequirementKind.Modifier, Description);

// ════════════════════════════════════════════════════════════════════════════════
//  ProofRequirementMeta — catalog meta (DU as identity)
// ════════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Metadata for a <see cref="ProofRequirementKind"/> value. Discriminated union — DU as identity:
/// the subtype IS the semantic signal. <see cref="QualifierCompatibility"/> is explicitly distinct
/// as the only dual-subject kind; consumers can check <c>meta is ProofRequirementMeta.QualifierCompatibility</c>
/// without inspecting a <c>SubjectArity</c> field.
/// </summary>
public abstract record ProofRequirementMeta(ProofRequirementKind Kind, string Description)
{
    /// <summary>Numeric interval check — value comparison against threshold.</summary>
    public sealed record Numeric()
        : ProofRequirementMeta(ProofRequirementKind.Numeric,
            "Numeric interval check — value comparison against threshold (e.g. divisor != 0)");

    /// <summary>Presence check — optional field must be set before access.</summary>
    public sealed record Presence()
        : ProofRequirementMeta(ProofRequirementKind.Presence,
            "Presence check — optional field must be set before access");

    /// <summary>Dimension check — period operand must have required time dimension.</summary>
    public sealed record Dimension()
        : ProofRequirementMeta(ProofRequirementKind.Dimension,
            "Dimension check — period operand must have required time dimension");

    /// <summary>Modifier check — field must declare required modifier (e.g. <c>ordered</c>).</summary>
    public sealed record Modifier()
        : ProofRequirementMeta(ProofRequirementKind.Modifier,
            "Modifier check — field must declare required modifier");

    /// <summary>
    /// Qualifier axis compatibility — two operands must share a qualifier value.
    /// The only dual-subject kind: obligation instances carry both
    /// <see cref="QualifierCompatibilityProofRequirement.LeftSubject"/> and
    /// <see cref="QualifierCompatibilityProofRequirement.RightSubject"/>.
    /// </summary>
    public sealed record QualifierCompatibility()
        : ProofRequirementMeta(ProofRequirementKind.QualifierCompatibility,
            "Qualifier compatibility — two operands must share a qualifier value on the specified axis");
}
