namespace Precept.Language;

/// <summary>
/// The type system's family taxonomy. Each member represents a type family.
/// Metadata lives in <see cref="Types.GetMeta"/>, not on the enum.
/// </summary>
public enum TypeKind
{
    // ── Scalar ─────────────────────────────────────────────────────
    String        =  1,
    Boolean       =  2,
    Integer       =  3,
    Decimal       =  4,
    Number        =  5,
    Choice        =  6,

    // ── Temporal ───────────────────────────────────────────────────
    Date          =  7,
    Time          =  8,
    Instant       =  9,
    Duration      = 10,
    Period        = 11,
    Timezone      = 12,
    ZonedDateTime = 13,
    DateTime      = 14,

    // ── Business-Domain ────────────────────────────────────────────
    Money         = 15,
    Currency      = 16,
    Quantity      = 17,
    UnitOfMeasure = 18,
    Dimension     = 19,
    Price         = 20,
    ExchangeRate  = 21,

    // ── Collection ─────────────────────────────────────────────────
    Set           = 22,
    Queue         = 23,
    Stack         = 24,

    // ── Special ────────────────────────────────────────────────────
    Error         = 25,
    StateRef      = 26,
}
