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
[CatalogDU]
public abstract record ProofRequirement(ProofRequirementKind Kind, string Description);

/// <summary>
/// Numeric interval proof: the subject's value compared with
/// <see cref="Comparison"/> against <see cref="Threshold"/> must hold.
/// Examples: divisor != 0, sqrt operand >= 0, collection count > 0.
///
/// <see cref="BoundModifierLabel"/> and <see cref="DisplayValue"/> are populated only when this
/// obligation carries a declared-value bound on a field/arg default (the OutOfRange family) — the
/// label names the violated modifier (e.g. "max", "nonnegative") and the display value is the
/// authored default as written, so the OutOfRange diagnostic can be reconstructed verbatim at the
/// proof stage. Null for every operation/function/action-safety Numeric obligation.
/// </summary>
public sealed record NumericProofRequirement(
    ProofSubject Subject,
    OperatorKind Comparison,
    decimal      Threshold,
    string       Description,
    string?      BoundModifierLabel = null,
    string?      DisplayValue = null
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
    [Precept.AllowZeroDefault]
    Any,
    /// <summary>Must be a date-level dimension (year, month, day).</summary>
    Date,
    /// <summary>Must be a time-level dimension (hour, minute, second).</summary>
    Time,
    /// <summary>
    /// Spans both date and time components — the dimension a composite period basis
    /// (e.g. <c>'days + hours'</c>) reports. Return/comparison value only: it is what
    /// <c>.dimension</c> returns and what <c>when X.dimension == 'datetime'</c> compares
    /// against. It is NOT a declarable <c>of</c> constraint — <c>period of 'datetime'</c>
    /// admits all components and so proves nothing, and stays a QualifierMismatch.
    /// </summary>
    Datetime,
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
/// Qualifier chain proof: cross-type, cross-axis qualifier validation.
/// </summary>
public sealed record QualifierChainProofRequirement(
    ProofSubject  LeftSubject,
    QualifierAxis LeftAxis,
    ProofSubject  RightSubject,
    QualifierAxis RightAxis,
    string        Description
) : ProofRequirement(ProofRequirementKind.QualifierChain, Description);

/// <summary>
/// Assignment qualifier compatibility: an OPEN field (no declared qualifier on
/// <see cref="Axis"/>) assigned to a target field that requires <see cref="TargetQualifier"/>
/// must be narrowed — by an enclosing guard — to a value satisfying the target on that axis.
/// This is the open-field assignment case relocated from the type-checker-immediate PRE0141
/// emit to a stamped proof obligation discharged at the proof stage (see
/// <c>docs/compiler/proof-engine.md</c> § Sequential proof flow). <see cref="Subject"/>
/// resolves to the assigned source expression (the obligation's Site); discharge consults the
/// guard-narrowing facts.
/// </summary>
public sealed record AssignmentQualifierProofRequirement(
    ProofSubject          Subject,
    string                TargetFieldName,
    DeclaredQualifierMeta TargetQualifier,
    QualifierAxis         Axis,
    string                Description
) : ProofRequirement(ProofRequirementKind.AssignmentQualifier, Description);

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

/// <summary>
/// Dimensional product proof: the multiplicative product of two quantity
/// operands' unit dimension vectors must resolve to a known curated
/// business-domain dimension (per <c>DimensionCatalog</c>). Used by
/// <c>QuantityTimesQuantity</c> — `kg × m` (mass·length) is rejected because
/// the product dimension is outside the curated set. Cancelling pairs (e.g.,
/// `kg × (1/kg)` → dimensionless) satisfy because the count alias is part of
/// the curated set.
/// </summary>
public sealed record DimensionalProductProofRequirement(
    ProofSubject  LeftSubject,
    ProofSubject  RightSubject,
    string        Description
) : ProofRequirement(ProofRequirementKind.DimensionalProduct, Description);

/// <summary>
/// Interval containment proof: the computed value interval of an expression
/// assigned to a decimal/number field must fit within the field's declared
/// bounds (if any). Used to prevent compile-time-provable numeric overflow
/// on field assignment operations (set, add, etc.).
///
/// <see cref="DeclaredMin"/>/<see cref="DeclaredMax"/> carry the normalized (UCUM base-unit)
/// bounds used by the proof engine for interval math.
/// <see cref="AuthoredMin"/>/<see cref="AuthoredMax"/> carry the raw authored values as written
/// by the user (e.g. 5 for <c>min '5 kg'</c>) and are used exclusively for diagnostic display.
/// For non-quantity fields the two pairs are identical.
/// </summary>
public sealed record IntervalContainmentProofRequirement(
    ProofSubject Subject,
    string TargetField,
    decimal? DeclaredMin,
    decimal? DeclaredMax,
    decimal? AuthoredMin,
    decimal? AuthoredMax,
    string Description
) : ProofRequirement(ProofRequirementKind.IntervalContainment, Description);

/// <summary>
/// Length containment proof: a string value being assigned to a bounded string
/// field must have its character length within the declared minlength/maxlength.
/// Conservative strategy: literal string assignments are checked statically;
/// non-literal assignments remain unresolved.
/// </summary>
public sealed record LengthContainmentProofRequirement(
    ProofSubject Subject,
    string TargetField,
    int? DeclaredMinLength,
    int? DeclaredMaxLength,
    string Description
) : ProofRequirement(ProofRequirementKind.LengthContainment, Description);

/// <summary>
/// Count containment proof: a collection's element count must stay within
/// the field's declared mincount/maxcount bounds. Static proof is limited
/// to cases where the element count is statically knowable (e.g., literal lists).
/// </summary>
public sealed record CountContainmentProofRequirement(
    ProofSubject Subject,
    string TargetField,
    int? DeclaredMinCount,
    int? DeclaredMaxCount,
    string Description
) : ProofRequirement(ProofRequirementKind.CountContainment, Description);

/// <summary>
/// Key presence proof: a collection must contain (or must NOT contain) a specific
/// element/key before the action is safe to perform.
/// <see cref="RequireAbsence"/> = false → element must be present (PRE0099 on failure).
/// <see cref="RequireAbsence"/> = true → element must NOT be present (PRE0101 on failure).
/// </summary>
public sealed record KeyPresenceProofRequirement(
    ProofSubject Subject,
    bool RequireAbsence,
    string Description
) : ProofRequirement(ProofRequirementKind.KeyPresence, Description);

/// <summary>Bounds inclusion mode for an <see cref="IndexBoundsProofRequirement"/>.</summary>
public enum IndexBoundsMode
{
    /// <summary>Index N must satisfy <c>0 &lt;= N &lt; F.count</c> — for <c>.at(N)</c> and <c>remove at N</c>.</summary>
    StrictlyBefore = 1,

    /// <summary>Index N must satisfy <c>0 &lt;= N &lt;= F.count</c> — for <c>insert at N</c> (positions 0..count are insertable).</summary>
    AtOrBefore = 2,
}

/// <summary>
/// Index bounds proof: a parameter (an index N) must satisfy <c>0 &lt;= N &lt; F.count</c>
/// (or <c>0 &lt;= N &lt;= F.count</c> per <see cref="Mode"/>) before access/mutation at that
/// index is safe. <see cref="Subject"/> resolves to the index parameter; the upper-bound
/// receiver is the action/accessor's <c>SelfSubject</c> via the <see cref="UpperBoundAccessor"/>.
/// </summary>
public sealed record IndexBoundsProofRequirement(
    ProofSubject Subject,
    IndexBoundsMode Mode,
    TypeAccessor UpperBoundAccessor,
    string Description
) : ProofRequirement(ProofRequirementKind.IndexBounds, Description);

// ════════════════════════════════════════════════════════════════════════════════
//  ProofRequirementMeta — catalog meta (DU as identity)
// ════════════════════════════════════════════════════════════════════════════════

/// <summary>
/// Metadata for a <see cref="ProofRequirementKind"/> value. Discriminated union — DU as identity:
/// the subtype IS the semantic signal. <see cref="QualifierCompatibility"/> is explicitly distinct
/// as the only dual-subject kind; consumers can check <c>meta is ProofRequirementMeta.QualifierCompatibility</c>
/// without inspecting a <c>SubjectArity</c> field.
///
/// <see cref="DiagnosticCode"/> is the catalog-mediated diagnostic for this obligation kind.
/// Null only for <see cref="Numeric"/> — which has a 1:many mapping that requires per-obligation
/// context dispatch (see <c>GetNumericRequirementDiagnosticCode</c>).
/// </summary>
public abstract record ProofRequirementMeta(
    ProofRequirementKind Kind,
    string Description,
    DiagnosticCode? DiagnosticCode)
{
    /// <summary>
    /// Numeric interval check — value comparison against threshold.
    /// DiagnosticCode is null: Numeric obligations map to DivisionByZero, SqrtOfNegative,
    /// UnguardedCollectionAccess, or UnguardedCollectionMutation depending on context.
    /// </summary>
    public sealed record Numeric()
        : ProofRequirementMeta(ProofRequirementKind.Numeric,
            "Numeric interval check — value comparison against threshold (e.g. divisor != 0)",
            null);

    /// <summary>Presence check — optional field must be set before access.</summary>
    public sealed record Presence()
        : ProofRequirementMeta(ProofRequirementKind.Presence,
            "Presence check — optional field must be set before access",
            Language.DiagnosticCode.UnprovedPresenceRequirement);

    /// <summary>Dimension check — period operand must have required time dimension.</summary>
    public sealed record Dimension()
        : ProofRequirementMeta(ProofRequirementKind.Dimension,
            "Dimension check — period operand must have required time dimension",
            Language.DiagnosticCode.UnprovedDimensionRequirement);

    /// <summary>Modifier check — field must declare required modifier (e.g. <c>ordered</c>).</summary>
    public sealed record Modifier()
        : ProofRequirementMeta(ProofRequirementKind.Modifier,
            "Modifier check — field must declare required modifier",
            Language.DiagnosticCode.UnprovedModifierRequirement);

    /// <summary>
    /// Qualifier axis compatibility — two operands must share a qualifier value.
    /// The only dual-subject kind: obligation instances carry both
    /// <see cref="QualifierCompatibilityProofRequirement.LeftSubject"/> and
    /// <see cref="QualifierCompatibilityProofRequirement.RightSubject"/>.
    /// </summary>
    public sealed record QualifierCompatibility()
        : ProofRequirementMeta(ProofRequirementKind.QualifierCompatibility,
            "Qualifier compatibility — two operands must share a qualifier value on the specified axis",
            Language.DiagnosticCode.UnprovedQualifierCompatibility);

    /// <summary>Qualifier chain — cross-type, cross-axis qualifier validation.</summary>
    public sealed record QualifierChain()
        : ProofRequirementMeta(ProofRequirementKind.QualifierChain,
            "Qualifier chain — cross-type, cross-axis qualifier validation",
            Language.DiagnosticCode.UnprovedQualifierCompatibility);

    /// <summary>Assignment qualifier compatibility — open field assigned to a qualified target must narrow (under a guard) to the target value on the axis.</summary>
    public sealed record AssignmentQualifier()
        : ProofRequirementMeta(ProofRequirementKind.AssignmentQualifier,
            "Assignment qualifier compatibility — open field assigned to a qualified target must narrow to the target value on the axis",
            Language.DiagnosticCode.UnprovedAssignmentQualifierCompatibility);

    /// <summary>Interval containment — result interval must fit within target field's declared bounds.</summary>
    public sealed record IntervalContainment()
        : ProofRequirementMeta(ProofRequirementKind.IntervalContainment,
            "Interval containment — result interval must fit within target field's declared bounds",
            Language.DiagnosticCode.NumericOverflow);

    /// <summary>Length containment — assigned string's character length must fit within declared minlength/maxlength.</summary>
    public sealed record LengthContainment()
        : ProofRequirementMeta(ProofRequirementKind.LengthContainment,
            "Length containment — assigned string's character length must fit within declared minlength/maxlength",
            Language.DiagnosticCode.LengthBoundViolation);

    /// <summary>Count containment — collection element count must fit within declared mincount/maxcount.</summary>
    public sealed record CountContainment()
        : ProofRequirementMeta(ProofRequirementKind.CountContainment,
            "Count containment — collection element count must fit within declared mincount/maxcount",
            Language.DiagnosticCode.CountBoundViolation);

    /// <summary>Key presence — collection must contain (or not contain) a specific element before access/mutation.</summary>
    public sealed record KeyPresence()
        : ProofRequirementMeta(ProofRequirementKind.KeyPresence,
            "Key presence — collection must contain or not contain a specific element",
            null);  // Routes to PRE0099 or PRE0101 depending on RequireAbsence

    /// <summary>Index bounds — parameter (an index N) must satisfy bounds against a collection's count.</summary>
    public sealed record IndexBounds()
        : ProofRequirementMeta(ProofRequirementKind.IndexBounds,
            "Index bounds — parameter (index N) must satisfy 0 <= N < F.count (or <= F.count for inserts)",
            Language.DiagnosticCode.IndexBoundsGuard);

    /// <summary>
    /// Dimensional product — operand dimension vectors multiply to a curated
    /// business-domain dimension (per <see cref="DimensionCatalog"/>).
    /// </summary>
    public sealed record DimensionalProduct()
        : ProofRequirementMeta(ProofRequirementKind.DimensionalProduct,
            "Dimensional product — operand quantities' dimensions must compose to a known business-domain dimension",
            Language.DiagnosticCode.IncompatibleDimensionalProduct);
}

// ProofSatisfaction DU — positive carrier fact that can satisfy a ProofRequirement
public abstract record ProofSatisfaction(ProofRequirementKind RequirementKind)
{
    public sealed record Numeric(
        SatisfactionProjection Projection,
        OperatorKind Comparison,
        NumericBoundSource Bound)
        : ProofSatisfaction(ProofRequirementKind.Numeric);

    public sealed record Presence()
        : ProofSatisfaction(ProofRequirementKind.Presence);

    public sealed record Dimension(DimensionSource Source)
        : ProofSatisfaction(ProofRequirementKind.Dimension);

    public sealed record Modifier(ModifierKind RequiredModifier)
        : ProofSatisfaction(ProofRequirementKind.Modifier);

    public sealed record QualifierCompatibility(QualifierAxis Axis)
        : ProofSatisfaction(ProofRequirementKind.QualifierCompatibility);

    public sealed record IntervalContainment()
        : ProofSatisfaction(ProofRequirementKind.IntervalContainment);

    public sealed record LengthContainment()
        : ProofSatisfaction(ProofRequirementKind.LengthContainment);

    public sealed record CountContainment()
        : ProofSatisfaction(ProofRequirementKind.CountContainment);

    public sealed record KeyPresence(bool Negated)
        : ProofSatisfaction(ProofRequirementKind.KeyPresence);
}

public abstract record SatisfactionProjection
{
    public sealed record SelfValue() : SatisfactionProjection;
    public sealed record Accessor(string Name) : SatisfactionProjection;
}

public abstract record NumericBoundSource
{
    public sealed record Constant(decimal Value) : NumericBoundSource;
    public sealed record DeclarationValue() : NumericBoundSource;
}

public abstract record DimensionSource
{
    public sealed record Constant(PeriodDimension Value) : DimensionSource;
    public sealed record DeclaredTemporalDimension() : DimensionSource;
}
