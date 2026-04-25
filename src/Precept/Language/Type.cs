namespace Precept.Language;

// ── TypeCategory ───────────────────────────────────────────────────────────────

/// <summary>
/// Classification of a <see cref="TypeKind"/> by family group.
/// </summary>
public enum TypeCategory
{
    Scalar,
    Temporal,
    BusinessDomain,
    Collection,
    Special,
}

// ── TypeTrait ──────────────────────────────────────────────────────────────────

/// <summary>
/// Behavioral traits of a type family. Flags enum — a type may have multiple traits.
/// </summary>
[Flags]
public enum TypeTrait
{
    None      = 0,
    Orderable = 1 << 0,
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
}

// ── QualifierShape ─────────────────────────────────────────────────────────────

/// <summary>
/// A single qualifier slot: a preposition keyword and the axis it fills.
/// Example: <c>money in 'USD'</c> → <c>QualifierSlot(TokenKind.In, QualifierAxis.Currency)</c>.
/// </summary>
public sealed record QualifierSlot(TokenKind Preposition, QualifierAxis Axis);

/// <summary>
/// The qualifier shape a type accepts. Null on types with no qualifiers.
/// </summary>
public sealed record QualifierShape(IReadOnlyList<QualifierSlot> Slots);

// ── TypeAccessor DU ────────────────────────────────────────────────────────────

/// <summary>
/// A member accessor on a type. Base record = inner-type return (e.g., collection
/// <c>.peek</c>, <c>.min</c>, <c>.max</c> return the collection's element type).
/// </summary>
public record TypeAccessor(
    string    Name,
    string    Description,
    TypeKind? ParameterType  = null,
    TypeTrait RequiredTraits = TypeTrait.None
);

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
    QualifierAxis ReturnsQualifier = QualifierAxis.None
) : TypeAccessor(Name, Description, ParameterType, RequiredTraits);

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
    QualifierShape?              QualifierShape   = null,
    TypeTrait                    Traits           = TypeTrait.None,
    IReadOnlyList<TypeKind>?     WidensTo         = null,
    IReadOnlyList<TypeAccessor>? Accessors        = null
)
{
    /// <summary>Lossless implicit widening targets. Empty for most types.</summary>
    public IReadOnlyList<TypeKind> WidensTo { get; } = WidensTo ?? Array.Empty<TypeKind>();

    /// <summary>Member accessors available on this type.</summary>
    public IReadOnlyList<TypeAccessor> Accessors { get; } = Accessors ?? Array.Empty<TypeAccessor>();
}
