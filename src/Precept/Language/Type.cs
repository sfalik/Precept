using System.Collections.Frozen;

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
/// the accessor returns the qualifier value on the named axis. <see cref="ReturnNonnegative"/>
/// marks accessors whose numeric result is structurally guaranteed to be ≥ 0.
/// </summary>
public sealed record FixedReturnAccessor(
    string        Name,
    TypeKind      Returns,
    string        Description,
    bool          ReturnNonnegative = false,
    TypeKind?     ParameterType    = null,
    TypeTrait     RequiredTraits   = TypeTrait.None,
    ProofRequirement[]? ProofRequirements = null,
    QualifierAxis ReturnsQualifier = QualifierAxis.None
) : TypeAccessor(Name, Description, ParameterType, RequiredTraits, ProofRequirements);

/// <summary>
/// An accessor whose parameter is the owning collection's element type.
/// Example: <c>bag.countof(element)</c> — the element is typed as the bag's T.
/// </summary>
public sealed record ElementParameterAccessor(
    string    Name,
    string    Description,
    TypeTrait RequiredTraits = TypeTrait.None,
    ProofRequirement[]? ProofRequirements = null
) : TypeAccessor(Name, Description, null, RequiredTraits, ProofRequirements);

// ── ContentValidation DU ───────────────────────────────────────────────────────

/// <summary>
/// Describes how a typed constant's string content is validated for a given <see cref="TypeKind"/>.
/// Subtypes carry the validation strategy: regex pattern, NodaTime temporal parsing, or closed set membership.
/// </summary>
public abstract record ContentValidation(string FormatDescription, string[] Examples);

/// <summary>
/// Validates typed constant content against a regular expression pattern.
/// </summary>
public sealed record RegexValidation(
    string Pattern, string FormatDescription, string[] Examples
) : ContentValidation(FormatDescription, Examples);

/// <summary>
/// Validates typed constant content by parsing as a temporal literal.
/// <see cref="LiteralKind"/> identifies the temporal parse path while <see cref="NodaTimePattern"/>
/// retains the pattern metadata surfaced to users.
/// </summary>
public sealed record NodaTimeValidation(
    TemporalLiteralKind LiteralKind,
    string NodaTimePattern,
    string FormatDescription,
    string[] Examples
) : ContentValidation(FormatDescription, Examples);

public sealed record UcumValidation(
    string FormatDescription,
    string[] Examples
) : ContentValidation(FormatDescription, Examples);

public sealed record MoneyValidation(
    string FormatDescription,
    string[] Examples
) : ContentValidation(FormatDescription, Examples);

public sealed record QuantityValidation(
    string FormatDescription,
    string[] Examples
) : ContentValidation(FormatDescription, Examples);

public sealed record PriceValidation(
    string FormatDescription,
    string[] Examples
) : ContentValidation(FormatDescription, Examples);

public sealed record ExchangeRateValidation(
    string FormatDescription,
    string[] Examples
) : ContentValidation(FormatDescription, Examples);

/// <summary>
/// Validates typed constant content against a closed set of allowed string values.
/// <see cref="SetName"/> is a human-readable label (e.g., "ISO 4217 currencies").
/// </summary>
public sealed record ClosedSetValidation(
    string SetName, FrozenSet<string> AllowedValues, string FormatDescription, string[] Examples
) : ContentValidation(FormatDescription, Examples);

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
    string?                      UsageExample     = null,
    bool                         NotemptyApplicable = true,
    /// <summary>
    /// The <see cref="TokenKind"/> values that are valid literal tokens for a choice option of
    /// this element type. <c>null</c> for all non-choice-element types. Populated for the five
    /// types that carry <see cref="TypeTrait.ChoiceElement"/>:
    /// <list type="bullet">
    ///   <item><c>integer</c>, <c>decimal</c>, <c>number</c> → <c>[NumberLiteral]</c></item>
    ///   <item><c>string</c> → <c>[StringLiteral]</c></item>
    ///   <item><c>boolean</c> → <c>[True, False]</c></item>
    /// </list>
    /// The parser derives both the signed-prefix path and the literal validity check from this
    /// field — no per-type identity switch in <c>ParseChoiceValue</c>.
    /// </summary>
    IReadOnlyList<TokenKind>?    ChoiceLiteralTokens = null,
    /// <summary>
    /// Content validation strategy for typed constants of this type.
    /// Non-null for types whose typed constant literals require content validation
    /// (temporal types via NodaTime, currency/unit via closed set membership).
    /// </summary>
    ContentValidation?           ContentValidation = null,
    /// <summary>
    /// Qualifier metadata intrinsically carried by this type regardless of explicit field declarations.
    /// Used by the proof engine's <c>ResolveQualifierOnAxis</c> after declared qualifiers are exhausted.
    /// Example: <c>duration</c> carries an implied <c>TemporalDimension(Time, Baseline)</c> because
    /// duration is intrinsically a time-dimension measurement.
    /// </summary>
    DeclaredQualifierMeta[]?     ImpliedQualifiers = null,
    /// <summary>
    /// Qualifier axes required when <c>min</c>/<c>max</c> bounds are declared on this type.
    /// Empty means bounds do not require qualifier context.
    /// </summary>
    IReadOnlyList<QualifierAxis>? RequiredBoundQualifierAxes = null
)
{
    /// <summary>Lossless implicit widening targets. Empty for most types.</summary>
    public IReadOnlyList<TypeKind> WidensTo { get; } = WidensTo ?? Array.Empty<TypeKind>();

    /// <summary>Modifiers intrinsically carried by this type (e.g., currency → notempty).</summary>
    public ModifierKind[] ImpliedModifiers { get; } = ImpliedModifiers ?? [];

    /// <summary>Member accessors available on this type.</summary>
    public IReadOnlyList<TypeAccessor> Accessors { get; } = Accessors ?? Array.Empty<TypeAccessor>();

    /// <summary>Qualifiers intrinsically carried by this type (e.g., duration → TemporalDimension(Time)).</summary>
    public DeclaredQualifierMeta[] ImpliedQualifiers { get; } = ImpliedQualifiers ?? [];

    /// <summary>Qualifier axes that must be present when min/max bounds are declared on this type.</summary>
    public IReadOnlyList<QualifierAxis> RequiredBoundQualifierAxes { get; } = RequiredBoundQualifierAxes ?? Array.Empty<QualifierAxis>();
}
