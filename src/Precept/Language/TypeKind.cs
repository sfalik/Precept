namespace Precept.Language;

/// <summary>
/// The type system's family taxonomy. Each member represents a type family.
/// Metadata lives in <see cref="Types.GetMeta"/>, not on the enum.
/// </summary>
public enum TypeKind
{
    // ── Scalar ─────────────────────────────────────────────────────
    String,
    Boolean,
    Integer,
    Decimal,
    Number,
    Choice,

    // ── Temporal ───────────────────────────────────────────────────
    Date,
    Time,
    Instant,
    Duration,
    Period,
    Timezone,
    ZonedDateTime,
    DateTime,

    // ── Business-Domain ────────────────────────────────────────────
    Money,
    Currency,
    Quantity,
    UnitOfMeasure,
    Dimension,
    Price,
    ExchangeRate,

    // ── Collection ─────────────────────────────────────────────────
    Set,
    Queue,
    Stack,

    // ── Special ────────────────────────────────────────────────────
    Error,
    StateRef,
}
