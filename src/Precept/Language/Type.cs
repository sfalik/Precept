namespace Precept.Language;

// ── TypeCategory ───────────────────────────────────────────────────────────────

/// <summary>
/// Classification of a <see cref="TypeKind"/> by family group.
/// </summary>
public enum TypeCategory
{
    Scalar         = 1,
    Temporal       = 2,
    BusinessDomain = 3,
    Collection     = 4,
    Special        = 5,
}

// ── TypeTrait ──────────────────────────────────────────────────────────────────

/// <summary>
/// Behavioral traits of a type family. Flags enum — a type may have multiple traits.
/// </summary>
[Flags]
public enum TypeTrait
{
    None               = 0,
    Orderable          = 1 << 0,
    EqualityComparable = 1 << 1,
    ChoiceElement      = 1 << 2,  // valid as element type in 'choice of T(...)'
}

// ── QualifierAxis ──────────────────────────────────────────────────────────────

/// <summary>
/// Names the semantic axis a type qualifier narrows.
/// Used by <see cref="QualifierSlot"/> and <see cref="FixedReturnAccessor.ReturnsQualifier"/>.
/// </summary>
public enum QualifierAxis
{
    None,
    Currency,
    Unit,
    Dimension,
    FromCurrency,
    ToCurrency,
    Timezone,
    /// <summary>Temporal dimension category for <c>period of 'date'</c> / <c>period of 'time'</c>. Distinct from <see cref="TemporalUnit"/>.</summary>
    TemporalDimension,
    /// <summary>Specific temporal unit basis for <c>period in 'days'</c> / <c>period in 'months'</c>. Distinct from <see cref="TemporalDimension"/> (category).</summary>
    TemporalUnit,
}

// ── QualifierShape ─────────────────────────────────────────────────────────────

/// <summary>
/// A single qualifier slot: a preposition keyword and the axis it fills.
/// Example: <c>money in 'USD'</c> → <c>QualifierSlot(TokenKind.In, QualifierAxis.Currency)</c>.
/// </summary>
public sealed record QualifierSlot(TokenKind Preposition, QualifierAxis Axis);

/// <summary>
/// The qualifier shape a type accepts. Lists all possible qualifier slots.
/// When <see cref="InOfExclusive"/> is true, the <c>in</c> and <c>of</c> prepositions
/// are mutually exclusive — a field may carry at most one. When false (e.g., <c>price</c>),
/// both may appear on the same declaration.
/// Null on types with no qualifiers.
/// </summary>
public sealed record QualifierShape(IReadOnlyList<QualifierSlot> Slots, bool InOfExclusive = false);

// ── TypeAccessor DU ────────────────────────────────────────────────────────────

/// <summary>
/// A member accessor on a type. Base record = inner-type return (e.g., collection
/// <c>.peek</c>, <c>.min</c>, <c>.max</c> return the collection's element type).
/// </summary>
public record TypeAccessor(
    string    Name,
    string    Description,
    TypeKind? ParameterType  = null,
    TypeTrait RequiredTraits = TypeTrait.None,
    ProofRequirement[]? ProofRequirements = null
)
{
    /// <summary>Proof obligations the type checker must verify at call sites.</summary>
    public ProofRequirement[] ProofRequirements { get; } = ProofRequirements ?? [];
}

/// <summary>
/// An accessor with a fixed return type (e.g., <c>.count → integer</c>,
/// <c>.currency → currency</c>). <see cref="ReturnsQualifier"/> indicates
/// the accessor returns the qualifier value on the named axis.
/// </summary>
public sealed record FixedReturnAccessor(
    string        Name,
    TypeKind      Returns,
    string        Description,
    TypeKind?     ParameterType    = null,
    TypeTrait     RequiredTraits   = TypeTrait.None,
    ProofRequirement[]? ProofRequirements = null,
    QualifierAxis ReturnsQualifier = QualifierAxis.None
) : TypeAccessor(Name, Description, ParameterType, RequiredTraits, ProofRequirements);

// ── TypeMeta ───────────────────────────────────────────────────────────────────

/// <summary>
/// Metadata for a single <see cref="TypeKind"/> value. The Token field holds a
/// direct reference to the <see cref="TokenMeta"/> instance from the Tokens catalog.
/// Null for internal types (<see cref="TypeKind.Error"/>, <see cref="TypeKind.StateRef"/>)
/// that have no surface keyword.
/// </summary>
public record TypeMeta(
    TypeKind                     Kind,
    TokenMeta?                   Token,
    string                       Description,
    TypeCategory                 Category,
    string                       DisplayName,
    QualifierShape?              QualifierShape   = null,
    TypeTrait                    Traits           = TypeTrait.None,
    IReadOnlyList<TypeKind>?     WidensTo         = null,
    ModifierKind[]?              ImpliedModifiers = null,
    IReadOnlyList<TypeAccessor>? Accessors        = null,
    string?                      HoverDescription = null,
    string?                      UsageExample     = null
)
{
    /// <summary>Lossless implicit widening targets. Empty for most types.</summary>
    public IReadOnlyList<TypeKind> WidensTo { get; } = WidensTo ?? Array.Empty<TypeKind>();

    /// <summary>Modifiers intrinsically carried by this type (e.g., currency → notempty).</summary>
    public ModifierKind[] ImpliedModifiers { get; } = ImpliedModifiers ?? [];

    /// <summary>Member accessors available on this type.</summary>
    public IReadOnlyList<TypeAccessor> Accessors { get; } = Accessors ?? Array.Empty<TypeAccessor>();
}
