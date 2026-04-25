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
//  ProofRequirement — what must be proven
// ════════════════════════════════════════════════════════════════════════════════

/// <summary>
/// A proof obligation declared by a catalog entry. The proof engine reads
/// these from catalog metadata — no hardcoded obligation lists.
/// </summary>
public abstract record ProofRequirement(ProofSubject Subject, string Description);

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
) : ProofRequirement(Subject, Description);

/// <summary>
/// Presence proof: an optional field must be set before access.
/// The subject's <see cref="SelfSubject.Accessor"/> is null (the field itself).
/// </summary>
public sealed record PresenceProofRequirement(
    ProofSubject Subject,
    string       Description
) : ProofRequirement(Subject, Description);

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
) : ProofRequirement(Subject, Description);
